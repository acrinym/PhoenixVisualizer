using System;
using Avalonia;
using Avalonia.Media;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Editor.Rendering;

public sealed class CanvasAdapter : ISkiaCanvas
{
    private readonly DrawingContext _context;
    private readonly double _width;
    private readonly double _height;

    public float FrameBlend { get; set; }

    public CanvasAdapter(DrawingContext context, double width, double height)
    {
        _context = context;
        _width = width;
        _height = height;
    }

    public void Clear(uint argb)
    {
        var color = Color.FromUInt32(argb);
        _context.FillRectangle(new SolidColorBrush(color), new Rect(0, 0, _width, _height));
    }

    public void DrawLines(ReadOnlySpan<(float x, float y)> points, float thickness, uint argb)
    {
        if (points.Length < 2) return;
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(points[0].x, points[0].y), false);
            for (int i = 1; i < points.Length; i++)
            {
                ctx.LineTo(new Point(points[i].x, points[i].y));
            }
            ctx.EndFigure(false);
        }
        var pen = new Pen(new SolidColorBrush(Color.FromUInt32(argb)), thickness);
        _context.DrawGeometry(null, pen, geometry);
    }

    public void FillCircle(float cx, float cy, float radius, uint argb)
    {
        var brush = new SolidColorBrush(Color.FromUInt32(argb));
        _context.DrawEllipse(brush, null, new Point(cx, cy), radius, radius);
    }
}
