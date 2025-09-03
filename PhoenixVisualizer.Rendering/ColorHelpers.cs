namespace PhoenixVisualizer.App.Rendering;

/// <summary>🎨 Small helpers for color/brush conversion.</summary>
public static class ColorHelpers
{
    public static IBrush BrushFromRgba(uint rgba)
        => new SolidColorBrush(Color.FromUInt32(rgba));
}
