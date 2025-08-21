using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Plugins.Avs;

public sealed class AvsVisualizerPlugin : IAvsHostPlugin, IVisualizerPlugin
{
    public string Id => "vis_avs";
    public string DisplayName => "AVS Runtime";
    public string Description => "Advanced Visualization Studio runtime for Winamp-style presets";
    public bool IsEnabled { get; set; } = true;

    private int _w, _h;

    // Mini-preset state
    private int _points = 512;
    private Mode _mode = Mode.Spectrum; // Changed default to spectrum
    private Source _source = Source.Fft;

    public void Initialize() { }
    public void Initialize(int width, int height) { _w = width; _h = height; }
    public void Resize(int width, int height)     { _w = width; _h = height; }
    public void Shutdown() { }
    public void ProcessFrame(AudioFeatures features, ISkiaCanvas canvas) { RenderFrame(features, canvas); }
    public void Configure() { LoadPreset(""); }
    public void Dispose() { }

    public void LoadPreset(string presetText)
    {
        // default values
        _points = 512; _mode = Mode.Spectrum; _source = Source.Fft;
        if (string.IsNullOrWhiteSpace(presetText)) return;
        var parts = presetText.Split(new[] { ';', '\n', '\r', ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var raw in parts)
        {
            var kv = raw.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kv.Length != 2) continue;
            var key = kv[0].Trim().ToLowerInvariant();
            var val = kv[1].Trim().ToLowerInvariant();
            switch (key)
            {
                case "points":
                    if (int.TryParse(val, out var p) && p > 1) _points = Math.Clamp(p, 2, 4096);
                    break;
                case "mode":
                    _mode = val switch { "bars" => Mode.Bars, "line" => Mode.Line, "spectrum" => Mode.Spectrum, _ => _mode };
                    break;
                case "source":
                    _source = val switch { "fft" => Source.Fft, "wave" => Source.Wave, "sin" => Source.Sin, _ => _source };
                    break;
            }
        }
        System.Diagnostics.Debug.WriteLine($"[vis_avs] Loaded mini preset: points={_points} mode={_mode} source={_source}");
    }

    // IVisualizerPlugin implementation
    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        // solid background so we actually see something
        canvas.Clear(0xFF101010);

        switch (_mode)
        {
            case Mode.Line:
                RenderLine(features, canvas);
                break;
            case Mode.Bars:
                RenderBars(features, canvas);
                break;
            case Mode.Spectrum:
                RenderSpectrum(features, canvas);
                break;
        }
    }

    private void RenderLine(AudioFeatures f, ISkiaCanvas canvas)
    {
        if (_points < 2) return;
        Span<(float x, float y)> pts = _points <= 8192
            ? stackalloc (float x, float y)[_points]
            : new (float x, float y)[_points];

        for (int i = 0; i < _points; i++)
        {
            float t = (float)i / (_points - 1);
            float x = t * _w;
            float y = (float)(_h * 0.5);

            float v = SampleSource(f, t, i);
            // scale: center at mid-height, +/- 40% height
            y -= v * (float)(_h * 0.4);

            pts[i] = (x, y);
        }

        canvas.DrawLines(pts, 2f, 0xFF40C4FF);
    }

    private void RenderBars(AudioFeatures f, ISkiaCanvas canvas)
    {
        // If FFT isn't present yet, fall back to sine so we always see something
        int n = Math.Min(_points, Math.Max(2, f.Fft?.Length ?? 0));
        if (n < 2 && _source != Source.Sin) { _source = Source.Sin; n = _points; }

        float barW = Math.Max(1f, (float)_w / n);
        Span<(float x, float y)> seg = stackalloc (float, float)[2];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Math.Max(1, n - 1);
            float v = SampleSource(f, t, i);
            v = MathF.Min(1f, MathF.Max(0f, v));

            float h = v * (_h - 10);
            float x = i * barW;

            seg[0] = (x + barW * 0.5f, _h - 5);
            seg[1] = (x + barW * 0.5f, _h - 5 - h);
            canvas.DrawLines(seg, Math.Max(1f, barW * 0.6f), 0xFFFFA000);
        }
    }

    private void RenderSpectrum(AudioFeatures f, ISkiaCanvas canvas)
    {
        // Use enhanced frequency bands if available
        var bands = f.FrequencyBands;
        if (bands.Length == 0)
        {
            // Fallback to basic FFT if frequency bands aren't available
            bands = f.Fft ?? Array.Empty<float>();
        }
        
        if (bands.Length == 0) return;
        
        var barWidth = Math.Max(1f, (float)_w / bands.Length);
        var maxHeight = _h - 20;
        
        for (int i = 0; i < bands.Length; i++)
        {
            var amplitude = MathF.Min(1f, bands[i]);
            var height = amplitude * maxHeight;
            
            // Color based on frequency band
            var color = GetFrequencyColor(i, bands.Length);
            
            var x = i * barWidth;
            var y = _h - 10 - height;
            
            canvas.FillRect(x, y, barWidth - 1, height, color);
            
            // Add a subtle glow effect for active bars
            if (amplitude > 0.1f)
            {
                var glowColor = (color & 0x00FFFFFF) | 0x40000000; // Semi-transparent glow
                canvas.FillRect(x - 1, y - 1, barWidth + 1, height + 2, glowColor);
            }
        }
        
        // Draw frequency labels
        if (bands.Length >= 8)
        {
            var labels = new[] { "60Hz", "250Hz", "500Hz", "1kHz", "2kHz", "4kHz", "8kHz", "16kHz" };
            for (int i = 0; i < Math.Min(labels.Length, bands.Length); i++)
            {
                var x = i * barWidth + barWidth / 2;
                canvas.DrawText(labels[i], x, _h - 5, 0xFFFFFFFF, 10);
            }
        }
    }

    private uint GetFrequencyColor(int bandIndex, int totalBands)
    {
        // Color gradient from red (low) to blue (high)
        var ratio = (float)bandIndex / Math.Max(1, totalBands - 1);
        
        if (ratio < 0.33f)
        {
            // Red to yellow (low frequencies)
            var r = 255;
            var g = (int)(255 * (ratio * 3));
            var b = 0;
            return (uint)((r << 16) | (g << 8) | b);
        }
        else if (ratio < 0.66f)
        {
            // Yellow to green (mid frequencies)
            var r = (int)(255 * (1 - (ratio - 0.33f) * 3));
            var g = 255;
            var b = 0;
            return (uint)((r << 16) | (g << 8) | b);
        }
        else
        {
            // Green to blue (high frequencies)
            var r = 0;
            var g = (int)(255 * (1 - (ratio - 0.66f) * 3));
            var b = (int)(255 * (ratio - 0.66f) * 3);
            return (uint)((r << 16) | (g << 8) | b);
        }
    }

    private float SampleSource(AudioFeatures f, float t, int i)
    {
        switch (_source)
        {
            case Source.Fft:
                if (f.Fft is { Length: > 0 })
                {
                    int idx = (int)(t * (f.Fft.Length - 1));
                    float mag = MathF.Abs(f.Fft[idx]);
                    // soft log scale
                    return MathF.Min(1f, (float)Math.Log(1 + 6 * mag));
                }
                break;
            case Source.Wave:
                if (f.Waveform is { Length: > 0 })
                {
                    int idx = (int)(t * (f.Waveform.Length - 1));
                    return 0.5f + 0.5f * f.Waveform[idx];
                }
                break;
            case Source.Sin:
                // Time-based sine so you see motion even with no audio
                float phase = (float)(f.TimeSeconds * 2.0 * Math.PI * 0.5); // 0.5 Hz
                return 0.5f + 0.5f * MathF.Sin(phase + t * MathF.Tau);
        }
        return 0f;
    }

    private enum Mode { Line, Bars, Spectrum }
    private enum Source { Fft, Wave, Sin }
}
