namespace PhoenixVisualizer.App.Models;

/// <summary>
/// Represents a hotkey item in the hotkey manager
/// </summary>
public class HotkeyItem
{
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CurrentBinding { get; set; } = string.Empty;
}
