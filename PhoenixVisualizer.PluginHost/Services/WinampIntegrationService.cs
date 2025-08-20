using System.IO;
using System.Linq;

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

    private static string ResolvePluginDirectory()
    {
        // 1) Explicit override via env var
        var fromEnv = Environment.GetEnvironmentVariable("PHOENIX_WINAMP_VIS_DIR");
        if (!string.IsNullOrWhiteSpace(fromEnv) &&
            Directory.Exists(fromEnv) &&
            Directory.EnumerateFiles(fromEnv, "*.dll").Any())
        {
            return fromEnv!;
        }

        // 2) Known Winamp locations (Windows)
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var candidates = new[]
                {
                    Path.Combine(pf86, "Winamp", "Plugins"),
                    Path.Combine(pf,  "Winamp", "Plugins")
                };
                var hit = candidates.FirstOrDefault(d =>
                    Directory.Exists(d) && Directory.EnumerateFiles(d, "*.dll").Any());
                if (hit != null) return hit!;
            }
        }
        catch { /* safe fallback */ }

        // 3) Local app folder (default)
        return Path.Combine(AppContext.BaseDirectory, "plugins", "vis");
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
