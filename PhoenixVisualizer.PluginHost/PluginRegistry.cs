using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoenixVisualizer.PluginHost;

/// <summary>
/// Enhanced plugin information for the registry
/// </summary>
public class PluginMetadata
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public string Author { get; set; } = "Unknown";
    public bool IsEnabled { get; set; } = true;
    public DateTime LastUsed { get; set; } = DateTime.MinValue;
    public int UsageCount { get; set; } = 0;
}

/// <summary>
/// Enhanced runtime registry to discover and create visualizer plugins.
/// </summary>
public static class PluginRegistry
{
    private static readonly Dictionary<string, (PluginMetadata metadata, Func<IVisualizerPlugin> factory)> _plugins = new();
    private static readonly Dictionary<string, PluginMetadata> _metadataCache = new();

    public static void Register(string id, string displayName, Func<IVisualizerPlugin> factory, string? description = null, string? version = null, string? author = null)
    {
        var metadata = new PluginMetadata
        {
            Id = id,
            DisplayName = displayName,
            Description = description ?? $"Visualizer plugin: {displayName}",
            Version = version ?? "1.0",
            Author = author ?? "Unknown"
        };
        
        _plugins[id] = (metadata, factory);
        _metadataCache[id] = metadata;
    }

    public static IVisualizerPlugin? Create(string id)
    {
        if (_plugins.TryGetValue(id, out var entry))
        {
            // Update usage statistics
            entry.metadata.LastUsed = DateTime.UtcNow;
            entry.metadata.UsageCount++;
            return entry.factory();
        }
        return null;
    }

    public static IEnumerable<PluginMetadata> AvailablePlugins
        => _plugins.Values.Select(entry => entry.metadata);

    public static PluginMetadata? GetMetadata(string id)
        => _metadataCache.TryGetValue(id, out var metadata) ? metadata : null;

    public static bool IsPluginAvailable(string id)
        => _plugins.ContainsKey(id);

    public static void SetPluginEnabled(string id, bool enabled)
    {
        if (_metadataCache.TryGetValue(id, out var metadata))
        {
            metadata.IsEnabled = enabled;
        }
    }

    public static void ClearUsageStats(string id)
    {
        if (_metadataCache.TryGetValue(id, out var metadata))
        {
            metadata.UsageCount = 0;
            metadata.LastUsed = DateTime.MinValue;
        }
    }
}
