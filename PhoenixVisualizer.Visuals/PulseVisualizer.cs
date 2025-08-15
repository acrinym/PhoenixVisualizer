using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

// Pulsing circle visualizer driven by energy 🚨
public sealed class PulseVisualizer : IVisualizerPlugin
{
    public string Id => "pulse";
    public string DisplayName => "Pulse Circle";

    private int _width;
    private int _height;

    public void Initialize(int width, int height) => Resize(width, height);

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        canvas.Clear(0xFF000000);
        float size = Math.Min(_width, _height);
        float baseRadius = size * 0.15f;
        float radius = baseRadius + features.Energy * size * 0.35f;
        uint color = features.Beat ? 0xFFFFFFFFu : 0xFFFFAA00u;
        canvas.FillCircle(_width / 2f, _height / 2f, radius, color);
    }

    public void Dispose() { }
}

