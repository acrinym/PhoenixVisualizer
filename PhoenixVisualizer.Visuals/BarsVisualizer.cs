using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

// Simple visualizer showing bass/mid/treble bars 😄
public sealed class BarsVisualizer : IVisualizerPlugin
{
    public string Id => "bars";
    public string DisplayName => "Simple Bars";

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
        // Clear to black 🧼
        canvas.Clear(0xFF000000);

        float totalWidth = _width;
        float barWidth = totalWidth / 3f * 0.6f; // leave some spacing
        float gap = (totalWidth - 3 * barWidth) / 4f;
        float maxHeight = _height * 0.9f;

        DrawBar(features.Bass, features.Volume, gap, barWidth, maxHeight, canvas, 0xFFFF5555);
        DrawBar(features.Mid, features.Volume, 2 * gap + barWidth, barWidth, maxHeight, canvas, 0xFF55FF55);
        DrawBar(features.Treble, features.Volume, 3 * gap + 2 * barWidth, barWidth, maxHeight, canvas, 0xFF5555FF);

        if (features.Beat)
        {
            float radius = Math.Min(_width, _height) * 0.05f;
            canvas.FillCircle(_width / 2f, _height / 2f, radius, 0xFFFFFFFF);
        }
    }

    private void DrawBar(float value, float volume, float x, float width, float maxHeight, ISkiaCanvas canvas, uint color)
    {
        float norm = volume <= 1e-6f ? 0f : Math.Clamp(value / volume, 0f, 1f);
        float height = norm * maxHeight;
        var points = new (float x, float y)[]
        {
            (x + width / 2f, _height),
            (x + width / 2f, _height - height)
        };
        canvas.DrawLines(points, width, color);
    }

    public void Dispose()
    {
        // Nothing to cleanup
    }
}

