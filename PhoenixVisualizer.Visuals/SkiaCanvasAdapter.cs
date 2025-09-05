using PhoenixVisualizer.Core.Interfaces;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Adapter to convert PluginHost.ISkiaCanvas to Core.Interfaces.ISkiaCanvas
/// </summary>
public class SkiaCanvasAdapter : PhoenixVisualizer.Core.Interfaces.ISkiaCanvas
{
    private readonly PluginHost.ISkiaCanvas _inner;

    public SkiaCanvasAdapter(PluginHost.ISkiaCanvas inner)
    {
        _inner = inner;
    }

    public int Width => _inner.Width;
    public int Height => _inner.Height;

    public void Clear(uint color) => _inner.Clear(color);
    
    public void DrawLine(float x1, float y1, float x2, float y2, uint color, float thickness = 1.0f)
        => _inner.DrawLine(x1, y1, x2, y2, color, thickness);
    
    public void FillRectangle(float x, float y, float width, float height, uint color)
        => _inner.FillRect(x, y, width, height, color);
    
    public void DrawCircle(float x, float y, float radius, uint color, bool filled = false)
        => _inner.DrawCircle(x, y, radius, color, filled);
    
    public void FillCircle(float x, float y, float radius, uint color)
        => _inner.FillCircle(x, y, radius, color);
    
    public void DrawRect(float x, float y, float width, float height, uint color, bool filled = false)
        => _inner.DrawRect(x, y, width, height, color, filled);
    
    public void DrawPoint(float x, float y, uint color, float size = 1.0f)
        => _inner.DrawPoint(x, y, color, size);
    
    public void Fade(uint color, float amount) => _inner.Fade(color, amount);
    
    public void SetLineWidth(float width) => _inner.SetLineWidth(width);
    
    public void DrawPolyline(System.Span<(float x, float y)> points, uint color)
        => _inner.DrawPolygon(points, color, false);
    
    public void DrawPolygon(System.Span<(float x, float y)> points, uint color, bool filled = false)
        => _inner.DrawPolygon(points, color, filled);
    
    public void FillRect(float x, float y, float width, float height, uint color)
        => _inner.FillRect(x, y, width, height, color);
}
