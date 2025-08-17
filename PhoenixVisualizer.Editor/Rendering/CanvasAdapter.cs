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

    // Implement required interface properties
    public int Width => (int)_width;
    public int Height => (int)_height;

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

    public void DrawLine(float x1, float y1, float x2, float y2, uint color, float thickness = 1.0f)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromUInt32(color)), thickness);
        _context.DrawLine(pen, new Point(x1, y1), new Point(x2, y2));
    }

    public void DrawLines(System.Span<(float x, float y)> points, float thickness, uint color)
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
        var pen = new Pen(new SolidColorBrush(Color.FromUInt32(color)), thickness);
        _context.DrawGeometry(null, pen, geometry);
    }

    public void DrawRect(float x, float y, float width, float height, uint color, bool filled = false)
    {
        var rect = new Rect(x, y, width, height);
        if (filled)
        {
            var brush = new SolidColorBrush(Color.FromUInt32(color));
            _context.FillRectangle(brush, rect);
        }
        else
        {
            var pen = new Pen(new SolidColorBrush(Color.FromUInt32(color)), 1.0f);
            _context.DrawRectangle(null, pen, rect);
        }
    }

    public void FillRect(float x, float y, float width, float height, uint color)
    {
        var brush = new SolidColorBrush(Color.FromUInt32(color));
        _context.FillRectangle(brush, new Rect(x, y, width, height));
    }

    public void DrawCircle(float x, float y, float radius, uint color, bool filled = false)
    {
        var center = new Point(x, y);
        if (filled)
        {
            var brush = new SolidColorBrush(Color.FromUInt32(color));
            _context.DrawEllipse(brush, null, center, radius, radius);
        }
        else
        {
            var pen = new Pen(new SolidColorBrush(Color.FromUInt32(color)), 1.0f);
            _context.DrawEllipse(null, pen, center, radius, radius);
        }
    }

    public void FillCircle(float cx, float cy, float radius, uint argb)
    {
        var brush = new SolidColorBrush(Color.FromUInt32(argb));
        _context.DrawEllipse(brush, null, new Point(cx, cy), radius, radius);
    }

    public void DrawText(string text, float x, float y, uint color, float size = 12.0f)
    {
        var brush = new SolidColorBrush(Color.FromUInt32(color));
        // Use a simpler approach for text rendering in Avalonia
        var point = new Point(x, y);
        // For now, just draw a placeholder since FormattedText is complex
        // TODO: Implement proper text rendering
        _context.DrawEllipse(brush, null, point, size/2, size/2);
    }

    public void DrawPoint(float x, float y, uint color, float size = 1.0f)
    {
        var brush = new SolidColorBrush(Color.FromUInt32(color));
        var rect = new Rect(x - size/2, y - size/2, size, size);
        _context.FillRectangle(brush, rect);
    }

    public void Fade(uint color, float alpha)
    {
        // Extract RGB components and apply alpha
        var r = (color >> 16) & 0xFF;
        var g = (color >> 8) & 0xFF;
        var b = color & 0xFF;
        var a = (uint)(alpha * 255);
        var fadedColor = (a << 24) | (r << 16) | (g << 8) | b;
        
        // Apply fade effect by drawing a semi-transparent overlay
        var fadeBrush = new SolidColorBrush(Color.FromUInt32(fadedColor));
        _context.FillRectangle(fadeBrush, new Rect(0, 0, _width, _height));
    }
}
