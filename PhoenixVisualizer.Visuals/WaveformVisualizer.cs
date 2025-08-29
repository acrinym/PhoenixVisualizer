using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

// Time-domain waveform visualizer 🩵
public sealed class WaveformVisualizer : IVisualizerPlugin
{
    public string Id => "waveform";
    public string DisplayName => "Waveform";

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
        var wave = features.Waveform;
        if (wave.Length < 2) return;
        int len = wave.Length;
        Span<(float x, float y)> pts = stackalloc (float x, float y)[len];
        for (int i = 0; i < len; i++)
        {
            // Proper normalization from 0 to 1
            float nx = len > 1 ? (float)i / (len - 1) : 0f;

            // Convert to screen coordinates
            float x = nx * (_width - 1);

            // Proper waveform scaling with center baseline
            float centerY = _height * 0.5f;
            float amplitude = _height * 0.4f; // Use 40% of screen height for waveform
            float y = centerY - wave[i] * amplitude;

            // Clamp to prevent drawing outside screen bounds
            y = MathF.Max(0, MathF.Min(_height - 1, y));

            pts[i] = (x, y);
        }
        canvas.DrawLines(pts, 1.5f, 0xFF00FF00);
    }

    public void Dispose() { }
}
