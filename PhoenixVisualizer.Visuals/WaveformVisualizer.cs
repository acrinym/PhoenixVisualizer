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
            float nx = len > 1 ? (float)i / (len - 1) : 0f;
            float x = nx * (_width - 1);
            float y = (float)(_height * 0.5 - wave[i] * (_height * 0.4));
            pts[i] = (x, y);
        }
        canvas.DrawLines(pts, 1.5f, 0xFF00FF00);
    }

    public void Dispose() { }
}
