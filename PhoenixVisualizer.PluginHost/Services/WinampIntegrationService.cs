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
            var pluginDir = ResolvePluginDirectory();
            if (string.IsNullOrWhiteSpace(pluginDir))
            {
                // No plugin directory found - this is not an error, just no plugins available
                return;
            }
            
            Directory.CreateDirectory(pluginDir);
            var dllFiles = Directory.EnumerateFiles(pluginDir, "*.dll").ToArray();
            
            _winampHost = new SimpleWinampHost(pluginDir);
            _isInitialized = true;
        }
        catch
        {
            // Initialization failed - error will be handled by IsInitialized property
        }
    }

    public async Task<string?> ResolvePluginDirectoryAsync()
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
                if (Directory.Exists(candidate)) return candidate;
            }
            return null;
        }

        var env = Environment.GetEnvironmentVariable("PHOENIX_WINAMP_VIS_DIR");
        if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env!)) return env!;

        // 1) Common dev-layout: repoRoot\plugins\vis (search upward from bin and CWD)
        var fromBase = SearchUp(AppContext.BaseDirectory);
        if (fromBase != null) return fromBase;
        var fromCwd = SearchUp(Directory.GetCurrentDirectory());
        if (fromCwd != null) return fromCwd;

        // 2) Traditional Winamp install
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var winampVis = Path.Combine(programFiles, "Winamp", "Plugins");
        if (Directory.Exists(winampVis)) return winampVis;

        // 3) Last-ditch: bin\plugins\vis under the app
        var defaultUnderBin = Path.Combine(AppContext.BaseDirectory, "plugins", "vis");
        return Directory.Exists(defaultUnderBin) ? defaultUnderBin : null;
    }

    /// <summary>
    /// Scan for available Winamp plugins
    /// </summary>
    public async Task<(IReadOnlyList<SimpleWinampHost.LoadedPlugin> Plugins, string Status, Exception? Error)> ScanForPluginsAsync()
    {
        if (_winampHost == null || !_isInitialized)
        {
            // Try (re)initialize if a folder becomes available later
            InitializeWinampHost();
            if (_winampHost == null || !_isInitialized)
                return (new List<SimpleWinampHost.LoadedPlugin>(), "Winamp host not initialized", null);
        }

        return await Task.Run<(IReadOnlyList<SimpleWinampHost.LoadedPlugin>, string, Exception?)>(() =>
        {
            try
            {
                _winampHost.ScanForPlugins();
                var plugins = _winampHost.GetAvailablePlugins();
                return (plugins, $"✅ Found {plugins.Count} Winamp plugins", null);
            }
            catch (Exception ex)
            {
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
