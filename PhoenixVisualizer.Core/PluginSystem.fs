// PluginSystem.fs - Advanced Plugin System with Hot Reloading
// Dynamic plugin loading, dependency resolution, and runtime updates
// Enables modular architecture for audio processing effects

namespace PhoenixVisualizer.Core

open System
open System.Collections.Concurrent
open System.IO
open System.Reflection
open System.Runtime.Loader
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open System.Threading
open System.Collections.Generic

/// <summary>
/// Advanced plugin system with hot reloading and dependency management
/// Enables dynamic loading and updating of audio processing components
/// </summary>
module PluginSystem =

    /// <summary>
    /// Plugin metadata
    /// </summary>
    type PluginMetadata = {
        Id: string
        Name: string
        Version: string
        Description: string
        Author: string
        Dependencies: string list
        Interfaces: Type list
        FilePath: string
        LoadTime: DateTime
        Checksum: string
    }

    /// <summary>
    /// Plugin instance wrapper
    /// </summary>
    type PluginInstance = {
        Metadata: PluginMetadata
        Instance: obj
        Assembly: Assembly
        Context: AssemblyLoadContext
        IsActive: bool
    }

    /// <summary>
    /// Plugin load result
    /// </summary>
    type PluginLoadResult =
        | Success of PluginInstance
        | Failure of string
        | DependencyFailure of string list

    /// <summary>
    /// Plugin registry for managing loaded plugins
    /// </summary>
    type PluginRegistry = {
        Plugins: ConcurrentDictionary<string, PluginInstance>
        Dependencies: ConcurrentDictionary<string, string list>
        LoadContexts: ConcurrentDictionary<string, AssemblyLoadContext>
        FileWatchers: ConcurrentDictionary<string, FileSystemWatcher>
        PluginPaths: string list
        EnableHotReload: bool
    }

    /// <summary>
    /// Plugin configuration
    /// </summary>
    type PluginConfig = {
        PluginDirectories: string list
        EnableHotReload: bool
        DependencyResolutionTimeout: TimeSpan
        MaxPluginLoadAttempts: int
        EnableSandboxing: bool
    }

    // Plugin loading and management

    /// <summary>
    /// Create plugin registry
    /// </summary>
    let createRegistry config : PluginRegistry =
        {
            Plugins = ConcurrentDictionary<string, PluginInstance>()
            Dependencies = ConcurrentDictionary<string, string list>()
            LoadContexts = ConcurrentDictionary<string, AssemblyLoadContext>()
            FileWatchers = ConcurrentDictionary<string, FileSystemWatcher>()
            PluginPaths = config.PluginDirectories
            EnableHotReload = config.EnableHotReload
        }

    /// <summary>
    /// Calculate file checksum for change detection
    /// </summary>
    let calculateChecksum (filePath: string) : string =
        use stream = File.OpenRead(filePath)
        use sha256 = System.Security.Cryptography.SHA256.Create()
        let hash = sha256.ComputeHash(stream)
        BitConverter.ToString(hash).Replace("-", "").ToLower()

    /// <summary>
    /// Extract plugin metadata from assembly
    /// </summary>
    let extractMetadata (assembly: Assembly) (filePath: string) : PluginMetadata option =
        try
            let types = assembly.GetTypes()
            let pluginAttribute =
                assembly.GetCustomAttributes()
                |> Seq.tryPick (fun attr ->
                    match attr with
                    | :? PluginAttribute as pluginAttr -> Some pluginAttr
                    | _ -> None)

            match pluginAttribute with
            | Some attr ->
                let interfaces = types |> Array.filter (fun t -> t.IsInterface) |> Array.toList
                Some {
                    Id = attr.Id
                    Name = attr.Name
                    Version = attr.Version
                    Description = attr.Description
                    Author = attr.Author
                    Dependencies = attr.Dependencies |> Array.toList
                    Interfaces = interfaces
                    FilePath = filePath
                    LoadTime = DateTime.Now
                    Checksum = calculateChecksum filePath
                }
            | None -> None
        with
        | ex -> printfn "Error extracting metadata from %s: %s" filePath ex.Message; None

    /// <summary>
    /// Resolve plugin dependencies
    /// </summary>
    let resolveDependencies (registry: PluginRegistry) (pluginId: string) (dependencies: string list) : Result<unit, string list> =
        let missingDeps = ResizeArray<string>()
        let circularDeps = ResizeArray<string>()

        let rec checkDependency depId visited =
            if Set.contains depId visited then
                circularDeps.Add(depId)
            elif not (registry.Plugins.ContainsKey(depId)) then
                missingDeps.Add(depId)
            else
                match registry.Dependencies.TryGetValue(depId) with
                | true, deps -> deps |> List.iter (fun d -> checkDependency d (Set.add depId visited))
                | false, _ -> ()

        dependencies |> List.iter (fun dep -> checkDependency dep Set.empty)

        if not circularDeps.IsEmpty then
            Error (circularDeps |> Seq.map (fun d -> sprintf "Circular dependency: %s" d) |> Seq.toList)
        elif not missingDeps.IsEmpty then
            Error (missingDeps |> Seq.map (fun d -> sprintf "Missing dependency: %s" d) |> Seq.toList)
        else
            Success ()

    /// <summary>
    /// Load plugin from assembly file
    /// </summary>
    let loadPluginFromFile (registry: PluginRegistry) (config: PluginConfig) (filePath: string) : PluginLoadResult =
        try
            let context = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(filePath), true)
            let assembly = context.LoadFromAssemblyPath(filePath)

            match extractMetadata assembly filePath with
            | Some metadata ->
                // Resolve dependencies
                match resolveDependencies registry metadata.Id metadata.Dependencies with
                | Error errors -> DependencyFailure errors
                | Success () ->
                    // Find and instantiate plugin class
                    let pluginType = assembly.GetTypes() |> Array.tryFind (fun t ->
                        not t.IsAbstract && not t.IsInterface &&
                        metadata.Interfaces |> List.exists (fun iface -> iface.IsAssignableFrom(t)))

                    match pluginType with
                    | Some pluginType ->
                        let instance = Activator.CreateInstance(pluginType)
                        let pluginInstance = {
                            Metadata = metadata
                            Instance = instance
                            Assembly = assembly
                            Context = context
                            IsActive = true
                        }

                        // Register plugin
                        registry.Plugins.[metadata.Id] <- pluginInstance
                        registry.Dependencies.[metadata.Id] <- metadata.Dependencies

                        // Set up hot reload if enabled
                        if config.EnableHotReload then
                            setupHotReload registry config filePath metadata.Id

                        Success pluginInstance
                    | None -> Failure (sprintf "No suitable plugin class found in %s" filePath)
            | None -> Failure (sprintf "Invalid plugin metadata in %s" filePath)
        with
        | ex -> Failure (sprintf "Error loading plugin from %s: %s" filePath ex.Message)

    /// <summary>
    /// Set up hot reload for plugin
    /// </summary>
    let setupHotReload (registry: PluginRegistry) (config: PluginConfig) (filePath: string) (pluginId: string) : unit =
        let watcher = new FileSystemWatcher()
        watcher.Path <- Path.GetDirectoryName(filePath)
        watcher.Filter <- Path.GetFileName(filePath)
        watcher.EnableRaisingEvents <- true

        watcher.Changed.Add(fun args ->
            async {
                try
                    printfn "Detected change in plugin %s, reloading..." pluginId

                    // Calculate new checksum
                    let newChecksum = calculateChecksum filePath

                    // Only reload if checksum changed
                    match registry.Plugins.TryGetValue(pluginId) with
                    | true, currentPlugin when currentPlugin.Metadata.Checksum <> newChecksum ->
                        // Unload old plugin
                        unloadPlugin registry pluginId |> ignore

                        // Load new version
                        match loadPluginFromFile registry config filePath with
                        | Success newPlugin ->
                            printfn "Successfully reloaded plugin %s" pluginId
                        | Failure error ->
                            printfn "Failed to reload plugin %s: %s" pluginId error
                        | DependencyFailure errors ->
                            printfn "Failed to reload plugin %s due to dependencies: %A" pluginId errors
                    | _ -> ()
                with
                | ex -> printfn "Error during hot reload of plugin %s: %s" pluginId ex.Message
            } |> Async.Start)

        registry.FileWatchers.[pluginId] <- watcher

    /// <summary>
    /// Unload plugin
    /// </summary>
    let unloadPlugin (registry: PluginRegistry) (pluginId: string) : bool =
        match registry.Plugins.TryGetValue(pluginId) with
        | true, pluginInstance ->
            try
                // Deactivate plugin
                pluginInstance.IsActive <- false

                // Clean up dependencies
                registry.Dependencies.TryRemove(pluginId) |> ignore

                // Clean up file watcher
                match registry.FileWatchers.TryGetValue(pluginId) with
                | true, watcher ->
                    watcher.Dispose()
                    registry.FileWatchers.TryRemove(pluginId) |> ignore
                | false, _ -> ()

                // Unload assembly context
                pluginInstance.Context.Unload()

                // Remove from registry
                registry.Plugins.TryRemove(pluginId) |> ignore

                printfn "Successfully unloaded plugin %s" pluginId
                true
            with
            | ex ->
                printfn "Error unloading plugin %s: %s" pluginId ex.Message
                false
        | false, _ -> false

    /// <summary>
    /// Scan directory for plugins
    /// </summary>
    let scanDirectory (registry: PluginRegistry) (config: PluginConfig) (directory: string) : PluginLoadResult list =
        if not (Directory.Exists(directory)) then
            [Failure (sprintf "Plugin directory does not exist: %s" directory)]
        else
            let pluginFiles = Directory.GetFiles(directory, "*.dll") |> Array.toList
            pluginFiles |> List.map (loadPluginFromFile registry config)

    /// <summary>
    /// Load all plugins from configured directories
    /// </summary>
    let loadAllPlugins (registry: PluginRegistry) (config: PluginConfig) : unit =
        registry.PluginPaths |> List.iter (fun dir ->
            printfn "Scanning plugin directory: %s" dir
            let results = scanDirectory registry config dir
            results |> List.iter (function
                | Success plugin -> printfn "Loaded plugin: %s" plugin.Metadata.Name
                | Failure error -> printfn "Failed to load plugin: %s" error
                | DependencyFailure errors -> printfn "Plugin dependency errors: %A" errors))

    /// <summary>
    /// Get plugin by ID
    /// </summary>
    let getPlugin (registry: PluginRegistry) (pluginId: string) : PluginInstance option =
        match registry.Plugins.TryGetValue(pluginId) with
        | true, plugin when plugin.IsActive -> Some plugin
        | _ -> None

    /// <summary>
    /// Get all active plugins
    /// </summary>
    let getActivePlugins (registry: PluginRegistry) : PluginInstance list =
        registry.Plugins.Values |> Seq.filter (fun p -> p.IsActive) |> Seq.toList

    /// <summary>
    /// Get plugins implementing specific interface
    /// </summary>
    let getPluginsByInterface<'T> (registry: PluginRegistry) : PluginInstance list =
        registry.Plugins.Values
        |> Seq.filter (fun p -> p.IsActive && typeof<'T>.IsAssignableFrom(p.Instance.GetType()))
        |> Seq.toList

    /// <summary>
    /// Execute method on plugin with error handling
    /// </summary>
    let executePluginMethod<'TInput, 'TOutput> (plugin: PluginInstance) (methodName: string) (input: 'TInput) : 'TOutput option =
        try
            let pluginType = plugin.Instance.GetType()
            let method = pluginType.GetMethod(methodName)
            if method <> null then
                let result = method.Invoke(plugin.Instance, [| box input |])
                Some (unbox result)
            else
                printfn "Method %s not found on plugin %s" methodName plugin.Metadata.Name
                None
        with
        | ex ->
            printfn "Error executing method %s on plugin %s: %s" methodName plugin.Metadata.Name ex.Message
            None

    // Advanced plugin features

    /// <summary>
    /// Plugin sandboxing for security
    /// </summary>
    module Sandboxing =

        open System.Security
        open System.Security.Permissions
        open System.Security.Policy

        /// <summary>
        /// Create sandboxed assembly load context
        /// </summary>
        let createSandboxedContext (pluginPath: string) : AssemblyLoadContext =
            // In a real implementation, this would set up security policies
            // For now, just create a regular context
            new AssemblyLoadContext(Path.GetFileNameWithoutExtension(pluginPath), true)

    /// <summary>
    /// Plugin dependency injection
    /// </summary>
    module DependencyInjection =

        /// <summary>
        /// Service registry for dependency injection
        /// </summary>
        type ServiceRegistry = {
            Services: ConcurrentDictionary<Type, obj>
        }

        /// <summary>
        /// Create service registry
        /// </summary>
        let createServiceRegistry () : ServiceRegistry =
            { Services = ConcurrentDictionary<Type, obj>() }

        /// <summary>
        /// Register service
        /// </summary>
        let registerService<'T> (registry: ServiceRegistry) (service: 'T) : unit =
            registry.Services.[typeof<'T>] <- box service

        /// <summary>
        /// Resolve service
        /// </summary>
        let resolveService<'T> (registry: ServiceRegistry) : 'T option =
            match registry.Services.TryGetValue(typeof<'T>) with
            | true, service -> Some (unbox service)
            | false, _ -> None

        /// <summary>
        /// Inject dependencies into plugin
        /// </summary>
        let injectDependencies (registry: ServiceRegistry) (plugin: PluginInstance) : unit =
            let pluginType = plugin.Instance.GetType()

            // Find properties with dependency injection attributes
            let injectableProperties =
                pluginType.GetProperties()
                |> Array.filter (fun prop ->
                    prop.CanWrite &&
                    prop.GetCustomAttributes(typeof<InjectAttribute>, true).Length > 0)

            for prop in injectableProperties do
                match resolveService registry with
                | Some service -> prop.SetValue(plugin.Instance, service)
                | None -> printfn "Could not resolve service for property %s" prop.Name

    /// <summary>
    /// Plugin communication and messaging
    /// </summary>
    module Messaging =

        /// <summary>
        /// Message types for plugin communication
        /// </summary>
        type PluginMessage =
            | Command of string * obj
            | Event of string * obj
            | Request of string * obj * AsyncReplyChannel<obj>

        /// <summary>
        /// Plugin message bus
        /// </summary>
        type MessageBus = {
            Mailbox: MailboxProcessor<PluginMessage>
            Subscribers: ConcurrentDictionary<string, (obj -> unit) list>
        }

        /// <summary>
        /// Create message bus
        /// </summary>
        let createMessageBus () : MessageBus =
            let subscribers = ConcurrentDictionary<string, (obj -> unit) list>()

            let mailbox = MailboxProcessor.Start(fun inbox ->
                async {
                    while true do
                        let! message = inbox.Receive()
                        match message with
                        | Command (topic, payload) ->
                            match subscribers.TryGetValue(topic) with
                            | true, handlers -> handlers |> List.iter (fun handler -> handler payload)
                            | false, _ -> ()
                        | Event (topic, payload) ->
                            match subscribers.TryGetValue(topic) with
                            | true, handlers -> handlers |> List.iter (fun handler -> handler payload)
                            | false, _ -> ()
                        | Request (topic, payload, replyChannel) ->
                            // Handle requests (simplified)
                            replyChannel.Reply(null)
                })

            { Mailbox = mailbox; Subscribers = subscribers }

        /// <summary>
        /// Subscribe to messages
        /// </summary>
        let subscribe (bus: MessageBus) topic handler : unit =
            bus.Subscribers.AddOrUpdate(topic,
                [handler],
                fun _ existing -> handler :: existing) |> ignore

        /// <summary>
        /// Publish message
        /// </summary>
        let publish (bus: MessageBus) message : unit =
            bus.Mailbox.Post(message)

    /// <summary>
    /// Plugin performance monitoring
    /// </summary>
    module Monitoring =

        /// <summary>
        /// Plugin performance metrics
        /// </summary>
        type PluginMetrics = {
            PluginId: string
            MethodCalls: int64
            TotalExecutionTime: TimeSpan
            AverageExecutionTime: TimeSpan
            ErrorCount: int64
            LastExecutionTime: DateTime option
        }

        /// <summary>
        /// Performance monitor
        /// </summary>
        let createPerformanceMonitor () =
            let metrics = ConcurrentDictionary<string, PluginMetrics>()

            let recordExecution pluginId methodName executionTime hadError =
                let key = sprintf "%s.%s" pluginId methodName
                metrics.AddOrUpdate(key,
                    {
                        PluginId = pluginId
                        MethodCalls = 1L
                        TotalExecutionTime = executionTime
                        AverageExecutionTime = executionTime
                        ErrorCount = if hadError then 1L else 0L
                        LastExecutionTime = Some DateTime.Now
                    },
                    fun _ existing ->
                        let newCalls = existing.MethodCalls + 1L
                        {
                            existing with
                                MethodCalls = newCalls
                                TotalExecutionTime = existing.TotalExecutionTime + executionTime
                                AverageExecutionTime = (existing.TotalExecutionTime + executionTime) / TimeSpan.FromTicks(newCalls)
                                ErrorCount = existing.ErrorCount + (if hadError then 1L else 0L)
                                LastExecutionTime = Some DateTime.Now
                        }) |> ignore

            let getMetrics () = metrics.Values |> Seq.toList

            recordExecution, getMetrics

    /// <summary>
    /// Plugin versioning and compatibility
    /// </summary>
    module Versioning =

        /// <summary>
        /// Version compatibility check
        /// </summary>
        let isCompatible (requiredVersion: string) (availableVersion: string) : bool =
            // Simple version comparison - in reality, you'd use proper semver
            let parseVersion (version: string) =
                version.Split('.')
                |> Array.map (fun s -> Int32.Parse(s))
                |> fun arr -> (arr.[0], arr.[1], arr.[2])

            let (reqMajor, reqMinor, reqPatch) = parseVersion requiredVersion
            let (availMajor, availMinor, availPatch) = parseVersion availableVersion

            availMajor > reqMajor ||
            (availMajor = reqMajor && availMinor > reqMinor) ||
            (availMajor = reqMajor && availMinor = reqMinor && availPatch >= reqPatch)

        /// <summary>
        /// Check plugin compatibility
        /// </summary>
        let checkCompatibility (plugin: PluginInstance) (requiredVersions: Map<string, string>) : bool =
            plugin.Metadata.Dependencies
            |> List.forall (fun dep ->
                match requiredVersions.TryFind(dep) with
                | Some requiredVersion ->
                    // In a real implementation, you'd look up the actual version of the dependency
                    true // Placeholder
                | None -> false)

    /// <summary>
    /// Plugin marketplace and distribution
    /// </summary>
    module Marketplace =

        /// <summary>
        /// Plugin package metadata
        /// </summary>
        type PluginPackage = {
            Metadata: PluginMetadata
            DownloadUrl: string
            PackageSize: int64
            Rating: float
            DownloadCount: int
            LastUpdated: DateTime
        }

        /// <summary>
        /// Plugin repository
        /// </summary>
        type PluginRepository = {
            Packages: ConcurrentDictionary<string, PluginPackage>
            BaseUrl: string
        }

        /// <summary>
        /// Create plugin repository
        /// </summary>
        let createRepository baseUrl : PluginRepository =
            {
                Packages = ConcurrentDictionary<string, PluginPackage>()
                BaseUrl = baseUrl
            }

        /// <summary>
        /// Search plugins
        /// </summary>
        let searchPlugins (repository: PluginRepository) (query: string) : PluginPackage list =
            repository.Packages.Values
            |> Seq.filter (fun pkg ->
                pkg.Metadata.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                pkg.Metadata.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            |> Seq.toList

        /// <summary>
        /// Download and install plugin
        /// </summary>
        let downloadPlugin (repository: PluginRepository) (pluginId: string) (targetDirectory: string) : Async<bool> =
            async {
                match repository.Packages.TryGetValue(pluginId) with
                | true, package ->
                    try
                        // In a real implementation, this would download the package
                        printfn "Downloading plugin %s..." pluginId
                        return true
                    with
                    | ex ->
                        printfn "Error downloading plugin %s: %s" pluginId ex.Message
                        return false
                | false, _ ->
                    printfn "Plugin %s not found in repository" pluginId
                    return false
            }

// Plugin attribute for metadata
[<AttributeUsage(AttributeTargets.Assembly)>]
type PluginAttribute(id: string, name: string, version: string, description: string, author: string, dependencies: string[]) =
    inherit Attribute()
    member _.Id = id
    member _.Name = name
    member _.Version = version
    member _.Description = description
    member _.Author = author
    member _.Dependencies = dependencies

// Dependency injection attribute
[<AttributeUsage(AttributeTargets.Property)>]
type InjectAttribute() =
    inherit Attribute()
