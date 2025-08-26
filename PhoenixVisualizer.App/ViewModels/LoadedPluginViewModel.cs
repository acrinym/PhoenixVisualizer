namespace PhoenixVisualizer.App.ViewModels;

/// <summary>
/// ViewModel for displaying loaded plugins in the UI (Winamp integration removed)
/// </summary>
public class LoadedPluginViewModel
{
    public string FileName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ModuleCount { get; set; }
    public object? Plugin { get; set; } // Placeholder - Winamp integration removed
}
