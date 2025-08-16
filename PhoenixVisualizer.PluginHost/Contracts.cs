namespace PhoenixVisualizer.PluginHost;

public record AudioFeatures(
    double TimeSeconds,
    double Bpm,
    bool Beat,
    float Volume,
    float Rms,
    float Peak,
    float Energy,
    float[] Fft,
    float[] Waveform,           // <-- new in PR
    float Bass,
    float Mid,
    float Treble,
    string? Genre,
    uint? SuggestedColorArgb
);

public interface IVisualizerPlugin
{
    string Id { get; }
    string DisplayName { get; }

    void Initialize(int width, int height);
    void Resize(int width, int height);
    void RenderFrame(AudioFeatures features, ISkiaCanvas canvas);
    void Dispose();
}

public interface IApeEffect : IVisualizerPlugin { }

public interface IAvsHostPlugin : IVisualizerPlugin
{
    void LoadPreset(string presetText);
}

public interface ISkiaCanvas
{
    void Clear(uint argb);
    void DrawLines(ReadOnlySpan<(float x, float y)> points, float thickness, uint argb);
    void FillCircle(float cx, float cy, float radius, uint argb);

    // 👀 Optional frame blending hint (0..1) for smoother visuals
    float FrameBlend { get; set; }
}
