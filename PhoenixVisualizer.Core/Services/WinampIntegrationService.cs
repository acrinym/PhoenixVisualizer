using PhoenixVisualizer.PluginHost;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Services;

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

        if (plugin == null)
        {
            throw new ArgumentNullException(nameof(plugin));
        }

        if (moduleIndex < 0 || moduleIndex >= plugin.Modules.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(moduleIndex));
        }

        return await Task.Run(() =>
        {
            try
            {
                // Deactivate current plugin if any
                if (_activePlugin != null)
                {
                    DeactivateCurrentPlugin();
                }

                // Initialize the new plugin
                var plugins = _winampHost.GetAvailablePlugins();
                var pluginIndex = plugins.ToList().IndexOf(plugin);
                var success = _winampHost.InitializeModule(pluginIndex, moduleIndex);

                if (success)
                {
                    _activePlugin = plugin;
                    _activeModuleIndex = moduleIndex;
                    StatusChanged?.Invoke($"✅ Activated plugin: {plugin.FileName} (module {moduleIndex})");
                    return true;
                }
                else
                {
                    StatusChanged?.Invoke($"❌ Failed to initialize plugin: {plugin.FileName}");
                    return false;
                }
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
    /// Render the active plugin with audio data
    /// </summary>
    public async Task<bool> RenderActivePluginAsync(byte[] spectrumData, byte[] waveformData)
    {
        if (_winampHost == null || _activePlugin == null || !_isInitialized)
        {
            return false;
        }

        return await Task.Run(() =>
        {
            try
            {
                // Update audio data for the plugin
                var plugins = _winampHost.GetAvailablePlugins();
                var pluginIndex = plugins.ToList().IndexOf(_activePlugin);
                _winampHost.UpdateAudioData(pluginIndex, _activeModuleIndex, spectrumData, waveformData);

                // Render the plugin
                var success = _winampHost.RenderModule(pluginIndex, _activeModuleIndex);
                return success;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
                return false;
            }
        });
    }

    /// <summary>
    /// Configure the active plugin
    /// </summary>
    public void ConfigureActivePlugin()
    {
        if (_winampHost == null || _activePlugin == null || !_isInitialized)
        {
            return;
        }

        try
        {
            var plugins = _winampHost.GetAvailablePlugins();
            var pluginIndex = plugins.ToList().IndexOf(_activePlugin);
            _winampHost.ConfigureModule(pluginIndex, _activeModuleIndex);
            StatusChanged?.Invoke($"⚙️ Configured plugin: {_activePlugin.FileName}");
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
            StatusChanged?.Invoke($"❌ Error configuring plugin: {ex.Message}");
        }
    }

    /// <summary>
    /// Deactivate the current plugin
    /// </summary>
    public void DeactivateCurrentPlugin()
    {
        if (_activePlugin == null || _winampHost == null)
        {
            return;
        }

        try
        {
            // Plugin cleanup is handled by SimpleWinampHost.Dispose()
            _activePlugin = null;
            _activeModuleIndex = 0;
            StatusChanged?.Invoke("🔄 Plugin deactivated");
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
            StatusChanged?.Invoke($"❌ Error deactivating plugin: {ex.Message}");
        }
    }

    /// <summary>
    /// Get the plugin directory path
    /// </summary>
    public string GetPluginDirectory()
    {
        return Path.Combine(AppContext.BaseDirectory, "plugins", "vis");
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            DeactivateCurrentPlugin();
            _winampHost?.Dispose();
            _disposed = true;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
        }
    }
}
