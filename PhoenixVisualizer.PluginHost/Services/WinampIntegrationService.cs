namespace PhoenixVisualizer.PluginHost.Services;

/// <summary>
/// Integrates Winamp visualization plugins with the main PhoenixVisualizer system
/// </summary>
public class WinampIntegrationService : IDisposable
{
    private SimpleWinampHost? _winampHost;
    private SimpleWinampHost.LoadedPlugin? _activePlugin;
    private int _activeModuleIndex = 0;
    private bool _isInitialized = false;
    private bool _disposed = false;



    public bool IsInitialized => _isInitialized;
    public SimpleWinampHost.LoadedPlugin? ActivePlugin => _activePlugin;
    public int ActiveModuleIndex => _activeModuleIndex;

    public WinampIntegrationService()
    {
        InitializeWinampHost();
    }

    private void InitializeWinampHost()
    {
        try
        {
            Console.WriteLine($"🔍 InitializeWinampHost called");
            var pluginDir = ResolvePluginDirectory();
            Console.WriteLine($"🔍 Resolved plugin directory: {pluginDir}");
            
            if (string.IsNullOrWhiteSpace(pluginDir))
            {
                Console.WriteLine($"❌ No plugin directory found - this is not an error, just no plugins available");
                return;
            }
            
            Directory.CreateDirectory(pluginDir);
            var dllFiles = Directory.EnumerateFiles(pluginDir, "*.dll").ToArray();
            Console.WriteLine($"🔍 Found {dllFiles.Length} DLL files in plugin directory:");
            foreach (var dll in dllFiles)
            {
                Console.WriteLine($"  - {Path.GetFileName(dll)}");
            }
            
            Console.WriteLine($"🔍 Creating SimpleWinampHost with directory: {pluginDir}");
            _winampHost = new SimpleWinampHost(pluginDir);
            _isInitialized = true;
            Console.WriteLine($"✅ Winamp host initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Initialization failed: {ex.Message}");
            Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            // Initialization failed - error will be handled by IsInitialized property
        }
    }

    public static async Task<string?> ResolvePluginDirectoryAsync()
    {
        return await Task.FromResult(ResolvePluginDirectory());
    }

    private static string? ResolvePluginDirectory()
    {
        static string? SearchUp(string start)
        {
            const int MaxDepth = 6;
            var d = new DirectoryInfo(start);
            for (int i = 0; i < MaxDepth && d != null; i++, d = d.Parent)
            {
                var candidate = Path.Combine(d.FullName, "plugins", "vis");
                if (Directory.Exists(candidate)) 
                {
                    Console.WriteLine($"🔍 Found plugin directory: {candidate}");
                    return candidate;
                }
            }
            return null;
        }

        Console.WriteLine($"🔍 Resolving plugin directory...");
        Console.WriteLine($"🔍 AppContext.BaseDirectory: {AppContext.BaseDirectory}");
        Console.WriteLine($"🔍 Current Directory: {Directory.GetCurrentDirectory()}");

        var env = Environment.GetEnvironmentVariable("PHOENIX_WINAMP_VIS_DIR");
        if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env!)) 
        {
            Console.WriteLine($"🔍 Using environment variable: {env}");
            return env!;
        }

        // 1) Common dev-layout: repoRoot\plugins\vis (search upward from bin and CWD)
        var fromBase = SearchUp(AppContext.BaseDirectory);
        if (fromBase != null) 
        {
            Console.WriteLine($"🔍 Using base directory search result: {fromBase}");
            return fromBase;
        }
        
        var fromCwd = SearchUp(Directory.GetCurrentDirectory());
        if (fromCwd != null) 
        {
            Console.WriteLine($"🔍 Using current directory search result: {fromCwd}");
            return fromCwd;
        }

        // 2) Traditional Winamp install
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var winampVis = Path.Combine(programFiles, "Winamp", "Plugins");
        if (Directory.Exists(winampVis)) 
        {
            Console.WriteLine($"🔍 Using traditional Winamp install: {winampVis}");
            return winampVis;
        }

        // 3) Last-ditch: bin\plugins\vis under the app
        var defaultUnderBin = Path.Combine(AppContext.BaseDirectory, "plugins", "vis");
        if (Directory.Exists(defaultUnderBin))
        {
            Console.WriteLine($"🔍 Using default under bin: {defaultUnderBin}");
            return defaultUnderBin;
        }
        
        Console.WriteLine($"❌ No plugin directory found!");
        return null;
    }

    /// <summary>
    /// Scan for available Winamp plugins
    /// </summary>
    public async Task<(IReadOnlyList<SimpleWinampHost.LoadedPlugin> Plugins, string Status, Exception? Error)> ScanForPluginsAsync()
    {
        Console.WriteLine($"🔍 ScanForPluginsAsync called");
        Console.WriteLine($"🔍 _winampHost: {_winampHost != null}");
        Console.WriteLine($"🔍 _isInitialized: {_isInitialized}");
        
        if (_winampHost == null || !_isInitialized)
        {
            Console.WriteLine($"🔍 Attempting to reinitialize Winamp host...");
            // Try (re)initialize if a folder becomes available later
            InitializeWinampHost();
            if (_winampHost == null || !_isInitialized)
            {
                Console.WriteLine($"❌ Winamp host still not initialized after reinit attempt");
                return (new List<SimpleWinampHost.LoadedPlugin>(), "Winamp host not initialized", null);
            }
        }

        return await Task.Run<(IReadOnlyList<SimpleWinampHost.LoadedPlugin>, string, Exception?)>(() =>
        {
            try
            {
                Console.WriteLine($"🔍 Scanning for plugins in Winamp host...");
                _winampHost.ScanForPlugins();
                var plugins = _winampHost.GetAvailablePlugins();
                Console.WriteLine($"🔍 Found {plugins.Count} plugins");
                foreach (var plugin in plugins)
                {
                    Console.WriteLine($"  - {plugin.FileName}: {plugin.Header.Description}");
                }
                return (plugins, $"✅ Found {plugins.Count} Winamp plugins", null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error scanning plugins: {ex.Message}");
                Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                return (new List<SimpleWinampHost.LoadedPlugin>(), $"❌ Error scanning plugins: {ex.Message}", ex);
            }
        });
    }

    /// <summary>
    /// Select and activate a Winamp plugin
    /// </summary>
    public async Task<(bool Success, string Status, Exception? Error)> SelectPluginAsync(SimpleWinampHost.LoadedPlugin plugin, int moduleIndex = 0)
    {
        if (_winampHost == null || !_isInitialized)
        {
            return (false, "Winamp host not initialized", new InvalidOperationException("Winamp host not initialized"));
        }

        return await Task.Run<(bool, string, Exception?)>(() =>
        {
            try
            {
                _activePlugin = plugin;
                _activeModuleIndex = moduleIndex;
                return (true, $"✅ Selected plugin: {plugin.FileName} (module {moduleIndex})", null);
            }
            catch (Exception ex)
            {
                return (false, $"❌ Error selecting plugin: {ex.Message}", ex);
            }
        });
    }

    /// <summary>
    /// Get the currently active plugin
    /// </summary>
    public SimpleWinampHost.LoadedPlugin? GetActivePlugin()
    {
        return _activePlugin;
    }

    /// <summary>
    /// Get the currently active module index
    /// </summary>
    public int GetActiveModuleIndex()
    {
        return _activeModuleIndex;
    }

    /// <summary>
    /// Check if a plugin is currently active
    /// </summary>
    public bool IsPluginActive()
    {
        return _activePlugin != null;
    }

    /// <summary>
    /// Get plugin information
    /// </summary>
    public string? GetPluginInfo()
    {
        if (_activePlugin == null) return null;
        return $"{_activePlugin.FileName} - {_activePlugin.Header.Description}";
    }

    /// <summary>
    /// Get available plugins from the host
    /// </summary>
    public IReadOnlyList<SimpleWinampHost.LoadedPlugin> GetAvailablePlugins()
    {
        return _winampHost?.GetAvailablePlugins() ?? new List<SimpleWinampHost.LoadedPlugin>();
    }

    /// <summary>
    /// Update audio data for a specific plugin
    /// </summary>
    public void UpdateAudioData(int pluginIndex, int moduleIndex, byte[] spectrumData, byte[] waveformData)
    {
        _winampHost?.UpdateAudioData(pluginIndex, moduleIndex, spectrumData, waveformData);
    }

    /// <summary>
    /// Render a specific plugin module
    /// </summary>
    public bool RenderPlugin(int pluginIndex, int moduleIndex)
    {
        return _winampHost?.RenderModule(pluginIndex, moduleIndex) ?? false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        try
        {
            _winampHost?.Dispose();
            _winampHost = null;
            _activePlugin = null;
        }
        catch
        {
            // Ignore disposal errors
        }
        finally
        {
            _disposed = true;
        }
    }
}
