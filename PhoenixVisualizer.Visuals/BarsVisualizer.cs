using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

public sealed class BarsVisualizer : IVisualizerPlugin
{
    public string Id => "bars";
    public string DisplayName => "Simple Bars";

    private int _w, _h;

    public void Initialize(int width, int height) { _w = width; _h = height; }
    public void Resize(int width, int height)     { _w = width; _h = height; }

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        canvas.Clear(0xFF101010); // opaque background

        if (f.Fft is null || f.Fft.Length == 0) return;

        int n = Math.Min(64, f.Fft.Length);
        float barW = Math.Max(1f, (float)_w / n);
        Span<(float x, float y)> seg = stackalloc (float, float)[2];

        for (int i = 0; i < n; i++)
        {
            // log-ish scale + clamp
            float v = f.Fft[i];
            float mag = MathF.Min(1f, (float)Math.Log(1 + 8 * Math.Max(0, v)));
            float h = mag * (_h - 10);

            float x = i * barW;
            seg[0] = (x + barW * 0.5f, _h - 5);
            seg[1] = (x + barW * 0.5f, _h - 5 - h);
            canvas.DrawLines(seg, Math.Max(1f, barW * 0.6f), 0xFF40C4FF);
        }
    }

    public void Dispose() { }
}
