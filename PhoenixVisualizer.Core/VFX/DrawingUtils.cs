using System;
using Avalonia.Media;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.VFX;

public static class DrawingUtils
{
    public static void DrawLine(ImageBuffer buf, int x1, int y1, int x2, int y2, int c)
    {
        int dx = Math.Abs(x2 - x1), sx = x1 < x2 ? 1 : -1;
        int dy = -Math.Abs(y2 - y1), sy = y1 < y2 ? 1 : -1;
        int err = dx + dy, e2;
        while (true)
        {
            if (x1 >= 0 && x1 < buf.Width && y1 >= 0 && y1 < buf.Height)
                buf.SetPixel(x1, y1, c);
            if (x1 == x2 && y1 == y2) break;
            e2 = 2 * err;
            if (e2 >= dy) { err += dy; x1 += sx; }
            if (e2 <= dx) { err += dx; y1 += sy; }
        }
    }

    public static void DrawCircle(ImageBuffer buf, int cx, int cy, int r, int c)
    {
        int x = r, y = 0, err = 0;
        while (x >= y)
        {
            PlotCircle(buf, cx, cy, x, y, c);
            y++;
            if (err <= 0) { err += 2 * y + 1; }
            else { x--; err -= 2 * x + 1; }
        }
    }

    private static void PlotCircle(ImageBuffer buf, int cx, int cy, int x, int y, int c)
    {
        void Plot(int px, int py)
        {
            if (px >= 0 && px < buf.Width && py >= 0 && py < buf.Height)
                buf.SetPixel(px, py, c);
        }
        Plot(cx + x, cy + y);
        Plot(cx + y, cy + x);
        Plot(cx - y, cy + x);
        Plot(cx - x, cy + y);
        Plot(cx - x, cy - y);
        Plot(cx - y, cy - x);
        Plot(cx + y, cy - x);
        Plot(cx + x, cy - y);
    }

    public static void DrawText(ImageBuffer buf, string text, Typeface typeface, int size, int c, int x, int y)
    {
        var color = System.Drawing.Color.FromArgb(c);
        buf.DrawText(text, typeface, size, color, new System.Drawing.Point(x, y));
    }
}

public class DrawingContextHelper
{
    public Typeface Typeface { get; set; } = new Typeface(new FontFamily("Arial"));
    public bool Antialias { get; set; } = true;

    public void DrawText(ImageBuffer buf, string text, int size, int c, int x, int y)
    {
        DrawingUtils.DrawText(buf, text, Typeface, size, c, x, y);
    }
}
