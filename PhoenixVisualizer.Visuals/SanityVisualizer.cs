using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// A tiny plugin that draws a bouncing line based on time so you can
/// confirm the render pipeline even when no audio is playing. 🎧
/// </summary>
public sealed class SanityVisualizer : IVisualizerPlugin
{
    public string Id => "sanity";
    public string DisplayName => "Sanity Check";

    private int _w;
    private int _h;

    public void Initialize(int width, int height) => (_w, _h) = (width, height);
    public void Resize(int width, int height) => (_w, _h) = (width, height);

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        // Solid background so the line stands out
        canvas.Clear(0xFF000000);

        // Bounce a vertical line left/right based on TimeSeconds
        float phase = (float)(f.TimeSeconds % 2.0);     // 0..2
        float t = phase <= 1f ? phase : 2f - phase;     // ping-pong
        float x = t * _w;

        Span<(float x, float y)> seg = stackalloc (float, float)[2]
        {
            (x, 0),
            (x, _h)
        };
        canvas.DrawLines(seg, 3f, 0xFF40C4FF);
    }

    public void Dispose() { }
}

