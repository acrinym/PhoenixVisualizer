using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.App.ViewModels;

/// <summary>
/// ViewModel for displaying loaded Winamp plugins in the UI
/// </summary>
public class LoadedPluginViewModel
{
    public string FileName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ModuleCount { get; set; }
    public SimpleWinampHost.LoadedPlugin? Plugin { get; set; }
}
