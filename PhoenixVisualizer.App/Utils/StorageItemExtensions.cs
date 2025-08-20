using System;
using Avalonia.Platform.Storage;

namespace PhoenixVisualizer.App.Utils;

/// <summary>
/// Extension helpers for Avalonia IStorageItem
/// </summary>
public static class StorageItemExtensions
{
    /// <summary>
    /// Get a guaranteed usable local path from an IStorageItem.
    /// Throws InvalidOperationException if not available.
    /// </summary>
    public static string RequireLocalPath(this IStorageItem item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item), "Storage item was null.");

        var path = item.TryGetLocalPath();
        if (string.IsNullOrEmpty(path))
        {
            var msg = $"Storage item '{item?.Name}' has no local path (provider={item?.GetType().Name}).";
            Console.Error.WriteLine(msg); // also dump to CLI
            throw new InvalidOperationException(msg);
        }

        return path;
    }
}
