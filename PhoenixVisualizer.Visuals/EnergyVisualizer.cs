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

        float size = Math.Min(_width, _height) * 0.4f;
        // Energy can be tiny, so give it a little boost and clamp
        float norm = Math.Clamp(features.Rms * 10f, 0f, 1f);
        float radius = size * norm;
        uint color = features.Beat ? 0xFFFFFF00 : 0xFF00FFFF;
        canvas.FillCircle(_width / 2f, _height / 2f, radius, color);
    }

    public void Dispose()
    {
        // Nothing to clean up here 😊
    }
}
