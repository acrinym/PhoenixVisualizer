using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoenixVisualizer.PluginHost;

/// <summary>
/// Simple runtime registry to discover and create visualizer plugins.
/// </summary>
public static class PluginRegistry
{
    private static readonly Dictionary<string, (string displayName, Func<IVisualizerPlugin> factory)> _plugins = new();

    public static void Register(string id, string displayName, Func<IVisualizerPlugin> factory)
    {
        _plugins[id] = (displayName, factory);
    }

    public static IVisualizerPlugin? Create(string id)
        => _plugins.TryGetValue(id, out var entry) ? entry.factory() : null;

    public static IEnumerable<(string id, string displayName)> Available
        => _plugins.Select(kvp => (kvp.Key, kvp.Value.displayName));
}
