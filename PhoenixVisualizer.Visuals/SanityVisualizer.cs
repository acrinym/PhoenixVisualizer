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
        try
        {
            // Solid background so the line stands out
            canvas.Clear(0xFF000000);

            // Bounce a vertical line left/right based on TimeSeconds
            // Handle case when no audio is loaded (TimeSeconds might be 0 or invalid)
            double timeSeconds = f.TimeSeconds;
            if (timeSeconds <= 0 || double.IsNaN(timeSeconds) || double.IsInfinity(timeSeconds))
            {
                // Fallback to system time if audio time is invalid
                timeSeconds = DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond;
            }
            
            float phase = (float)(timeSeconds % 2.0);     // 0..2
            float t = phase <= 1f ? phase : 2f - phase;     // ping-pong
            float x = t * _w;

            Span<(float x, float y)> seg = stackalloc (float, float)[2]
            {
                (x, 0),
                (x, _h)
            };
            canvas.DrawLines(seg, 3f, 0xFF40C4FF);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SanityVisualizer] RenderFrame failed: {ex.Message}");
            // Fallback: draw a simple static line in the center
            canvas.Clear(0xFF000000);
            var seg = new (float x, float y)[2]
            {
                (_w * 0.5f, 0),
                (_w * 0.5f, _h)
            };
            canvas.DrawLines(seg, 3f, 0xFF40C4FF);
        }
    }

    public void Dispose() { }
}

