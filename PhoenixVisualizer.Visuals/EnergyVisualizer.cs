using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

// Ring visualizer that swells with audio energy 🎵
public sealed class EnergyVisualizer : IVisualizerPlugin
{
    public string Id => "energy";
    public string DisplayName => "Energy Ring";

    private int _width;
    private int _height;

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        // Keep the background dark so the glow pops ✨
        canvas.Clear(0xFF000000);

        // Validate RMS data - check if it's stuck
        float rms = features.Rms;
        if (rms < 0.001f || float.IsNaN(rms) || float.IsInfinity(rms))
        {
            // If RMS is stuck, use a fallback animated pattern
            var time = DateTime.Now.Ticks / 10000000.0; // Current time in seconds
            rms = MathF.Sin((float)(time * 3.0)) * 0.3f + 0.3f; // Animated sine wave between 0-0.6
        }

        float size = Math.Min(_width, _height) * 0.4f;
        // Energy can be tiny, so give it a little boost and clamp
        float norm = Math.Clamp(rms * 10f, 0f, 1f);
        float radius = size * norm;
        uint color = features.Beat ? 0xFFFFFF00 : 0xFF00FFFF;
        canvas.FillCircle(_width / 2f, _height / 2f, radius, color);
    }

    public void Dispose()
    {
        // Nothing to clean up here 😊
    }
}
