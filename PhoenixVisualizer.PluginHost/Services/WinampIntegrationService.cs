using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

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

    public event Action<string>? StatusChanged;
    public event Action<Exception>? ErrorOccurred;

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
            var pluginDir = Path.Combine(AppContext.BaseDirectory, "plugins", "vis");
            
            // Check if directory exists
            if (!Directory.Exists(pluginDir))
            {
                throw new DirectoryNotFoundException($"Plugin directory not found: {pluginDir}");
            }
            
            // Check if any DLL files exist
            var dllFiles = Directory.GetFiles(pluginDir, "*.dll");
            if (dllFiles.Length == 0)
            {
                throw new FileNotFoundException($"No DLL files found in plugin directory: {pluginDir}");
            }
            
            StatusChanged?.Invoke($"🔍 Found {dllFiles.Length} DLL files in {pluginDir}");
            
            _winampHost = new SimpleWinampHost(pluginDir);
            _isInitialized = true;
            StatusChanged?.Invoke("✅ Winamp host initialized");
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
            StatusChanged?.Invoke($"❌ Failed to initialize Winamp host: {ex.Message}");
        }
    }

    /// <summary>
    /// Scan for available Winamp plugins
    /// </summary>
    public async Task<(IReadOnlyList<SimpleWinampHost.LoadedPlugin> Plugins, string Status, Exception? Error)> ScanForPluginsAsync()
    {
        if (_winampHost == null || !_isInitialized)
        {
            throw new InvalidOperationException("Winamp host not initialized");
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
            throw new InvalidOperationException("Winamp host not initialized");
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
