using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Visuals;

namespace PhoenixVisualizer.Core.Services;

/// <summary>
/// Manages visualizer plugins and provides plugin discovery and loading
/// </summary>
public class PluginManager : IDisposable
{
    private readonly Dictionary<string, IVisualizerPlugin> _plugins = new();
    private readonly List<string> _pluginPaths = new();
    private readonly object _pluginLock = new object();
    private bool _isDisposed = false;

    public IReadOnlyDictionary<string, IVisualizerPlugin> Plugins => _plugins;

    public PluginManager()
    {
        // Add built-in plugins
        RegisterBuiltInPlugins();
        
        // Discover plugins from directories
        DiscoverPlugins();
    }

    private void RegisterBuiltInPlugins()
    {
        try
        {
            // Register our test visualizer
            var testVisualizer = new VlcAudioTestVisualizer();
            _plugins[testVisualizer.Id] = testVisualizer;
            Debug.WriteLine($"[PluginManager] Registered built-in plugin: {testVisualizer.DisplayName} ({testVisualizer.Id})");
            
            // Register other built-in visualizers
            RegisterPlugin(new BarsVisualizer());
            RegisterPlugin(new WaveformVisualizer());
            RegisterPlugin(new EnergyVisualizer());
            RegisterPlugin(new SpectrumVisualizer());
            RegisterPlugin(new SuperScopePlugin());
            
            Debug.WriteLine($"[PluginManager] Registered {_plugins.Count} built-in plugins");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PluginManager] Error registering built-in plugins: {ex.Message}");
        }
    }

    private void DiscoverPlugins()
    {
        try
        {
            var pluginDirectories = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "plugins"),
                Path.Combine(AppContext.BaseDirectory, "plugins", "vis"),
                Path.Combine(AppContext.BaseDirectory, "plugins", "managed")
            };

            foreach (var directory in pluginDirectories)
            {
                if (Directory.Exists(directory))
                {
                    DiscoverPluginsInDirectory(directory);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PluginManager] Error discovering plugins: {ex.Message}");
        }
    }

    private void DiscoverPluginsInDirectory(string directory)
    {
        try
        {
            Debug.WriteLine($"[PluginManager] Discovering plugins in: {directory}");
            
            var pluginFiles = Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly);
            
            foreach (var pluginFile in pluginFiles)
            {
                try
                {
                    var plugin = LoadPluginFromFile(pluginFile);
                    if (plugin != null)
                    {
                        RegisterPlugin(plugin);
                        Debug.WriteLine($"[PluginManager] Loaded plugin: {plugin.DisplayName} from {pluginFile}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PluginManager] Failed to load plugin from {pluginFile}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PluginManager] Error discovering plugins in {directory}: {ex.Message}");
        }
    }

    private IVisualizerPlugin? LoadPluginFromFile(string filePath)
    {
        try
        {
            var assembly = Assembly.LoadFrom(filePath);
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IVisualizerPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .ToList();

            foreach (var pluginType in pluginTypes)
            {
                try
                {
                    var plugin = Activator.CreateInstance(pluginType) as IVisualizerPlugin;
                    if (plugin != null)
                    {
                        return plugin;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PluginManager] Failed to instantiate plugin type {pluginType.Name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PluginManager] Failed to load assembly from {filePath}: {ex.Message}");
        }

        return null;
    }

    private void RegisterPlugin(IVisualizerPlugin plugin)
    {
        if (plugin?.Id == null) return;

        lock (_pluginLock)
        {
            if (_plugins.ContainsKey(plugin.Id))
            {
                Debug.WriteLine($"[PluginManager] Plugin with ID '{plugin.Id}' already registered, skipping");
                return;
            }

            _plugins[plugin.Id] = plugin;
            Debug.WriteLine($"[PluginManager] Registered plugin: {plugin.DisplayName} ({plugin.Id})");
        }
    }

    public IVisualizerPlugin? GetPlugin(string id)
    {
        lock (_pluginLock)
        {
            return _plugins.TryGetValue(id, out var plugin) ? plugin : null;
        }
    }

    public IVisualizerPlugin? GetPluginByDisplayName(string displayName)
    {
        lock (_pluginLock)
        {
            return _plugins.Values.FirstOrDefault(p => p.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase));
        }
    }

    public List<IVisualizerPlugin> GetAllPlugins()
    {
        lock (_pluginLock)
        {
            return _plugins.Values.ToList();
        }
    }

    public bool HasPlugin(string id)
    {
        lock (_pluginLock)
        {
            return _plugins.ContainsKey(id);
        }
    }

    public void RefreshPlugins()
    {
        lock (_pluginLock)
        {
            // Clear existing plugins (except built-ins)
            var builtInIds = new[] { "vlc_audio_test", "bars", "waveform", "energy", "spectrum", "superscope" };
            var pluginsToRemove = _plugins.Keys.Where(id => !builtInIds.Contains(id)).ToList();
            
            foreach (var id in pluginsToRemove)
            {
                _plugins.Remove(id);
            }
            
            // Re-discover plugins
            DiscoverPlugins();
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        try
        {
            lock (_pluginLock)
            {
                foreach (var plugin in _plugins.Values)
                {
                    try
                    {
                        plugin.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[PluginManager] Error disposing plugin {plugin.Id}: {ex.Message}");
                    }
                }
                _plugins.Clear();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PluginManager] Error during disposal: {ex.Message}");
        }
        finally
        {
            _isDisposed = true;
        }
    }
}