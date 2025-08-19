namespace PhoenixVisualizer.PluginHost;

/// <summary>
/// Simple Winamp Visualizer Plugin Host
/// Directly loads Winamp visualizer plugins without BASS_WA
/// </summary>
public sealed class SimpleWinampHost : IDisposable
{
    // Winamp visualizer plugin structures (matching the Winamp SDK)
    [StructLayout(LayoutKind.Sequential)]
    public struct WinampVisModule
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string Description;
        public IntPtr HwndParent;
        public IntPtr HDllInstance;
        public int SampleRate;
        public int Channels;
        public int LatencyMs;
        public int DelayMs;
        public int SpectrumChannels;
        public int WaveformChannels;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[,] SpectrumData;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[,] WaveformData;
        public IntPtr ConfigFunc;
        public IntPtr InitFunc;
        public IntPtr RenderFunc;
        public IntPtr QuitFunc;
        public IntPtr UserData;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WinampVisHeader
    {
        public int Version;
        [MarshalAs(UnmanagedType.LPStr)]
        public string Description;
        public IntPtr GetModuleFunc;
    }

    // Plugin management
    private readonly List<LoadedPlugin> _loadedPlugins = new();
    private readonly string _pluginDirectory;
    private bool _disposed;

    // Function delegates for plugin functions
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr GetModuleDelegate(int index);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int InitDelegate(IntPtr module);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int RenderDelegate(IntPtr module);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void QuitDelegate(IntPtr module);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ConfigDelegate(IntPtr module);

    public class LoadedPlugin
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public IntPtr LibraryHandle { get; set; }
        public WinampVisHeader Header { get; set; }
        public List<WinampVisModule> Modules { get; set; } = new();
        public bool IsInitialized { get; set; }
        public IntPtr ParentWindow { get; set; }
    }

    public SimpleWinampHost(string pluginDirectory = "")
    {
        _pluginDirectory = string.IsNullOrEmpty(pluginDirectory) 
            ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "vis") 
            : pluginDirectory;
    }

    /// <summary>
    /// Scan for and load available Winamp visualizer plugins
    /// </summary>
    public void ScanForPlugins()
    {
        if (_disposed) return;

        try
        {
            if (!Directory.Exists(_pluginDirectory))
            {
                Directory.CreateDirectory(_pluginDirectory);
                Console.WriteLine($"[SimpleWinampHost] Created plugin directory: {_pluginDirectory}");
                return;
            }

            var pluginFiles = Directory.GetFiles(_pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly);
            Console.WriteLine($"[SimpleWinampHost] Found {pluginFiles.Length} potential plugin files");

            foreach (var pluginFile in pluginFiles)
            {
                try
                {
                    if (LoadPlugin(pluginFile))
                    {
                        Console.WriteLine($"[SimpleWinampHost] Successfully loaded plugin: {Path.GetFileName(pluginFile)}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SimpleWinampHost] Failed to load plugin {pluginFile}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimpleWinampHost] Error scanning for plugins: {ex.Message}");
        }
    }

    /// <summary>
    /// Load a single Winamp visualizer plugin
    /// </summary>
    private bool LoadPlugin(string pluginPath)
    {
        try
        {
            // Load the DLL
            var libraryHandle = LoadLibrary(pluginPath);
            if (libraryHandle == IntPtr.Zero)
            {
                Console.WriteLine($"[SimpleWinampHost] Failed to load library: {GetLastError()}");
                return false;
            }

            // Get the visHeader function
            var visHeaderPtr = GetProcAddress(libraryHandle, "visHeader");
            if (visHeaderPtr == IntPtr.Zero)
            {
                // Try alternative names
                visHeaderPtr = GetProcAddress(libraryHandle, "winampVisGetHeader");
                if (visHeaderPtr == IntPtr.Zero)
                {
                    Console.WriteLine($"[SimpleWinampHost] No visHeader function found in {Path.GetFileName(pluginPath)}");
                    FreeLibrary(libraryHandle);
                    return false;
                }
            }

            // Get the header
            var getHeaderFunc = Marshal.GetDelegateForFunctionPointer<GetModuleDelegate>(visHeaderPtr);
            var headerPtr = getHeaderFunc(0);
            if (headerPtr == IntPtr.Zero)
            {
                Console.WriteLine($"[SimpleWinampHost] Failed to get header from {Path.GetFileName(pluginPath)}");
                FreeLibrary(libraryHandle);
                return false;
            }

            var header = Marshal.PtrToStructure<WinampVisHeader>(headerPtr);
            
            // Validate header
            if (header.Version != 0x101) // VIS_HDRVER
            {
                Console.WriteLine($"[SimpleWinampHost] Invalid header version: {header.Version:X}");
                FreeLibrary(libraryHandle);
                return false;
            }

            // Get modules
            var modules = new List<WinampVisModule>();
            var getModuleFunc = Marshal.GetDelegateForFunctionPointer<GetModuleDelegate>(header.GetModuleFunc);
            
            for (int i = 0; i < 10; i++) // Limit to 10 modules
            {
                var modulePtr = getModuleFunc(i);
                if (modulePtr == IntPtr.Zero) break;
                
                var module = Marshal.PtrToStructure<WinampVisModule>(modulePtr);
                if (string.IsNullOrEmpty(module.Description)) break;
                
                modules.Add(module);
            }

            if (modules.Count == 0)
            {
                Console.WriteLine($"[SimpleWinampHost] No modules found in {Path.GetFileName(pluginPath)}");
                FreeLibrary(libraryHandle);
                return false;
            }

            // Create loaded plugin entry
            var loadedPlugin = new LoadedPlugin
            {
                FilePath = pluginPath,
                FileName = Path.GetFileName(pluginPath),
                LibraryHandle = libraryHandle,
                Header = header,
                Modules = modules
            };

            _loadedPlugins.Add(loadedPlugin);
            Console.WriteLine($"[SimpleWinampHost] Loaded {modules.Count} modules from {Path.GetFileName(pluginPath)}");
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimpleWinampHost] Error loading plugin {pluginPath}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get all available plugins
    /// </summary>
    public IReadOnlyList<LoadedPlugin> GetAvailablePlugins()
    {
        return _loadedPlugins.AsReadOnly();
    }

    /// <summary>
    /// Initialize a plugin module
    /// </summary>
    public bool InitializeModule(int pluginIndex, int moduleIndex)
    {
        if (_disposed || pluginIndex < 0 || pluginIndex >= _loadedPlugins.Count) return false;
        if (moduleIndex < 0 || moduleIndex >= _loadedPlugins[pluginIndex].Modules.Count) return false;

        try
        {
            var plugin = _loadedPlugins[pluginIndex];
            var module = plugin.Modules[moduleIndex];

            if (module.InitFunc == IntPtr.Zero) return false;

            var initFunc = Marshal.GetDelegateForFunctionPointer<InitDelegate>(module.InitFunc);
            var result = initFunc(Marshal.UnsafeAddrOfPinnedArrayElement(plugin.Modules.ToArray(), moduleIndex));

            if (result == 0) // Success
            {
                plugin.IsInitialized = true;
                Console.WriteLine($"[SimpleWinampHost] Initialized module {moduleIndex} of plugin {pluginIndex}");
                return true;
            }
            else
            {
                Console.WriteLine($"[SimpleWinampHost] Failed to initialize module {moduleIndex} of plugin {pluginIndex}: {result}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimpleWinampHost] Error initializing module: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Render a plugin module
    /// </summary>
    public bool RenderModule(int pluginIndex, int moduleIndex)
    {
        if (_disposed || pluginIndex < 0 || pluginIndex >= _loadedPlugins.Count) return false;
        if (moduleIndex < 0 || moduleIndex >= _loadedPlugins[pluginIndex].Modules.Count) return false;

        try
        {
            var plugin = _loadedPlugins[pluginIndex];
            if (!plugin.IsInitialized) return false;

            var module = plugin.Modules[moduleIndex];
            if (module.RenderFunc == IntPtr.Zero) return false;

            var renderFunc = Marshal.GetDelegateForFunctionPointer<RenderDelegate>(module.RenderFunc);
            var result = renderFunc(Marshal.UnsafeAddrOfPinnedArrayElement(plugin.Modules.ToArray(), moduleIndex));

            return result == 0; // 0 = success
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimpleWinampHost] Error rendering module: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Configure a plugin module
    /// </summary>
    public void ConfigureModule(int pluginIndex, int moduleIndex)
    {
        if (_disposed || pluginIndex < 0 || pluginIndex >= _loadedPlugins.Count) return;
        if (moduleIndex < 0 || moduleIndex >= _loadedPlugins[pluginIndex].Modules.Count) return;

        try
        {
            var plugin = _loadedPlugins[pluginIndex];
            var module = plugin.Modules[moduleIndex];

            if (module.ConfigFunc != IntPtr.Zero)
            {
                var configFunc = Marshal.GetDelegateForFunctionPointer<ConfigDelegate>(module.ConfigFunc);
                configFunc(Marshal.UnsafeAddrOfPinnedArrayElement(plugin.Modules.ToArray(), moduleIndex));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimpleWinampHost] Error configuring module: {ex.Message}");
        }
    }

    /// <summary>
    /// Set parent window for a plugin
    /// </summary>
    public void SetParentWindow(int pluginIndex, IntPtr hwnd)
    {
        if (_disposed || pluginIndex < 0 || pluginIndex >= _loadedPlugins.Count) return;

        try
        {
            var plugin = _loadedPlugins[pluginIndex];
            plugin.ParentWindow = hwnd;

            // Update all modules with the new parent window
            for (int i = 0; i < plugin.Modules.Count; i++)
            {
                var module = plugin.Modules[i];
                module.HwndParent = hwnd;
                plugin.Modules[i] = module;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimpleWinampHost] Error setting parent window: {ex.Message}");
        }
    }

    /// <summary>
    /// Update audio data for plugins
    /// </summary>
    public void UpdateAudioData(int pluginIndex, int moduleIndex, byte[] spectrumData, byte[] waveformData)
    {
        if (_disposed || pluginIndex < 0 || pluginIndex >= _loadedPlugins.Count) return;
        if (moduleIndex < 0 || moduleIndex >= _loadedPlugins[pluginIndex].Modules.Count) return;

        try
        {
            var plugin = _loadedPlugins[pluginIndex];
            var module = plugin.Modules[moduleIndex];

            // Copy spectrum data
            if (spectrumData != null && spectrumData.Length > 0)
            {
                var spectrumSize = Math.Min(spectrumData.Length, module.SpectrumData.GetLength(1));
                for (int ch = 0; ch < Math.Min(2, module.SpectrumData.GetLength(0)); ch++)
                {
                    for (int i = 0; i < spectrumSize; i++)
                    {
                        module.SpectrumData[ch, i] = spectrumData[i];
                    }
                }
            }

            // Copy waveform data
            if (waveformData != null && waveformData.Length > 0)
            {
                var waveformSize = Math.Min(waveformData.Length, module.WaveformData.GetLength(1));
                for (int ch = 0; ch < Math.Min(2, module.WaveformData.GetLength(0)); ch++)
                {
                    for (int i = 0; i < waveformSize; i++)
                    {
                        module.WaveformData[ch, i] = waveformData[i];
                    }
                }
            }

            // Update the module in the list
            plugin.Modules[moduleIndex] = module;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimpleWinampHost] Error updating audio data: {ex.Message}");
        }
    }

    // P/Invoke declarations
    [DllImport("kernel32.dll")]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [DllImport("kernel32.dll")]
    private static extern int GetLastError();

    [DllImport("kernel32.dll")]
    private static extern bool FreeLibrary(IntPtr hModule);

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            // Clean up all loaded plugins
            foreach (var plugin in _loadedPlugins)
            {
                try
                {
                    if (plugin.IsInitialized)
                    {
                        // Quit all modules
                        for (int i = 0; i < plugin.Modules.Count; i++)
                        {
                            var module = plugin.Modules[i];
                            if (module.QuitFunc != IntPtr.Zero)
                            {
                                var quitFunc = Marshal.GetDelegateForFunctionPointer<QuitDelegate>(module.QuitFunc);
                                quitFunc(Marshal.UnsafeAddrOfPinnedArrayElement(plugin.Modules.ToArray(), i));
                            }
                        }
                    }

                    // Free the library
                    if (plugin.LibraryHandle != IntPtr.Zero)
                    {
                        FreeLibrary(plugin.LibraryHandle);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SimpleWinampHost] Error disposing plugin {plugin.FileName}: {ex.Message}");
                }
            }

            _loadedPlugins.Clear();
            _disposed = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SimpleWinampHost] Error during disposal: {ex.Message}");
        }
    }
}
