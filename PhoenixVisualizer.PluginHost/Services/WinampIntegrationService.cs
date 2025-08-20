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
    public async Task<IReadOnlyList<SimpleWinampHost.LoadedPlugin>> ScanForPluginsAsync()
    {
        if (_winampHost == null || !_isInitialized)
        {
            throw new InvalidOperationException("Winamp host not initialized");
        }

        return await Task.Run(() =>
        {
            try
            {
                _winampHost.ScanForPlugins();
                var plugins = _winampHost.GetAvailablePlugins();
                StatusChanged?.Invoke($"✅ Found {plugins.Count} Winamp plugins");
                return plugins;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
                StatusChanged?.Invoke($"❌ Error scanning plugins: {ex.Message}");
                return new List<SimpleWinampHost.LoadedPlugin>();
            }
        });
    }

    /// <summary>
    /// Select and activate a Winamp plugin
    /// </summary>
    public async Task<bool> SelectPluginAsync(SimpleWinampHost.LoadedPlugin plugin, int moduleIndex = 0)
    {
        if (_winampHost == null || !_isInitialized)
        {
            throw new InvalidOperationException("Winamp host not initialized");
        }

        return await Task.Run(() =>
        {
            try
            {
                _activePlugin = plugin;
                _activeModuleIndex = moduleIndex;
                StatusChanged?.Invoke($"✅ Selected plugin: {plugin.FileName} (module {moduleIndex})");
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
                StatusChanged?.Invoke($"❌ Error selecting plugin: {ex.Message}");
                return false;
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
