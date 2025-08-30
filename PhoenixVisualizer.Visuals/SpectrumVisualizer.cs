using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

// Enhanced spectrum analyzer with customizable parameters and smooth animation 🎵📊
public sealed class SpectrumVisualizer : IVisualizerPlugin
{
    public string Id => "spectrum";
    public string DisplayName => "Spectrum Analyzer";

    private int _width;
    private int _height;
    private float _time;

    // Parameter controls
    private int _barCount = 64;
    private float _sensitivity = 1.0f;
    private float _decayRate = 0.95f;
    private bool _showPeaks = true;
    private float _colorShift = 0.0f;
    private float _barWidth = 0.8f;
    private bool _mirrorMode = false;

    // State for smoothing and peak detection
    private float[] _previousHeights = Array.Empty<float>();
    private float[] _peakHeights = Array.Empty<float>();
    private float[] _peakTimes = Array.Empty<float>();

    public void Initialize(int width, int height) => Resize(width, height);
    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;

        // Ensure arrays are properly sized (use maximum possible size to avoid reallocation)
        int maxBars = _barCount * 2; // Support both normal and mirror modes
        if (_previousHeights == null || _previousHeights.Length < maxBars)
        {
            _previousHeights = new float[maxBars];
            _peakHeights = new float[maxBars];
            _peakTimes = new float[maxBars];
        }
    }

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        _time += 0.016f;

        // Dynamic background based on audio
        uint bgColor = HsvToArgb((_colorShift + features.Bass * 0.5f) % 360f, 0.1f, 0.05f);
        canvas.Clear(bgColor);

        var fft = features.Fft;
        int len = fft.Length;
        int step = Math.Max(1, len / _barCount);
        float barWidth = _width / (float)(_barCount * (_mirrorMode ? 2 : 1));
        float maxHeight = _height * 0.8f;
        float baseY = _height * 0.95f;

        int totalBars = _barCount * (_mirrorMode ? 2 : 1);

        for (int i = 0; i < _barCount; i++)
        {
            // Calculate FFT magnitude for this bar
            int start = i * step;
            int end = Math.Min(start + step, len);
            float sum = 0f;
            for (int j = start; j < end; j++) sum += MathF.Abs(fft[j]);
            float magnitude = sum / (end - start);

            // Apply sensitivity and smoothing
            float targetHeight = Math.Clamp(magnitude * _sensitivity * 15f, 0f, 1f) * maxHeight;
            float currentHeight = _previousHeights[i];
            float smoothedHeight = currentHeight * _decayRate + targetHeight * (1 - _decayRate);
            _previousHeights[i] = smoothedHeight;

            // Update peak detection
            if (_showPeaks)
            {
                if (smoothedHeight > _peakHeights[i])
                {
                    _peakHeights[i] = smoothedHeight;
                    _peakTimes[i] = _time;
                }
                else if (_time - _peakTimes[i] > 2.0f) // Peak decay time
                {
                    _peakHeights[i] *= 0.98f;
                }
            }

            // Render main bar
            float x = i * barWidth + barWidth / 2f;
            RenderSpectrumBar(canvas, x, baseY, smoothedHeight, barWidth * _barWidth,
                            GetBarColor(i, _barCount, smoothedHeight / maxHeight), i);

            // Render mirror if enabled
            if (_mirrorMode)
            {
                int mirrorIndex = i + _barCount;
                float mirrorX = (_barCount + i) * barWidth + barWidth / 2f;
                RenderSpectrumBar(canvas, mirrorX, baseY, smoothedHeight, barWidth * _barWidth,
                                GetBarColor(i, _barCount, smoothedHeight / maxHeight), mirrorIndex);
            }
        }

        // Render frequency labels if there's space
        if (_barCount <= 32 && _width > 800)
        {
            RenderFrequencyLabels(canvas);
        }
    }

    private void RenderSpectrumBar(ISkiaCanvas canvas, float x, float baseY, float height, float width, uint color, int barIndex)
    {
        // Main bar body
        canvas.FillRect(x - width / 2, baseY - height, width, height, color);

        // Add gradient effect
        uint highlightColor = AdjustBrightness(color, 1.3f);
        canvas.FillRect(x - width / 2, baseY - height, width, height * 0.3f, highlightColor);

        // Peak indicator
        if (_showPeaks && _peakHeights[barIndex] > height * 0.95f)
        {
            float peakY = baseY - _peakHeights[barIndex];
            canvas.FillRect(x - width / 2, peakY - 1, width, 2, 0xFFFFFFFF);
        }

        // Reflection effect
        uint reflectionColor = AdjustBrightness(color, 0.3f);
        canvas.FillRect(x - width / 2, baseY, width, height * 0.2f, reflectionColor);
    }

    private uint GetBarColor(int barIndex, int totalBars, float intensity)
    {
        // Create rainbow spectrum from bass to treble
        float hue = (_colorShift + (barIndex / (float)totalBars) * 270f) % 360f;
        float saturation = 0.8f + intensity * 0.2f;
        float brightness = 0.6f + intensity * 0.4f;

        return HsvToArgb(hue, saturation, brightness);
    }

    private void RenderFrequencyLabels(ISkiaCanvas canvas)
    {
        // Simple frequency labels for reference
        string[] labels = { "60Hz", "250Hz", "1K", "4K", "16K" };
        float[] positions = { 0.1f, 0.3f, 0.5f, 0.7f, 0.9f };

        for (int i = 0; i < labels.Length; i++)
        {
            float x = _width * positions[i];
            canvas.FillRect(x - 1, _height - 15, 2, 10, 0xFF888888);
            // Note: Text rendering would require additional font support
        }
    }

    private uint AdjustBrightness(uint color, float factor)
    {
        byte r = (byte)((color >> 16) & 0xFF);
        byte g = (byte)((color >> 8) & 0xFF);
        byte b = (byte)(color & 0xFF);

        r = (byte)Math.Min(255, r * factor);
        g = (byte)Math.Min(255, g * factor);
        b = (byte)Math.Min(255, b * factor);

        return (uint)(0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | (uint)b);
    }

    public void Dispose() { }

    // Tiny HSV→ARGB helper 🎨
    private static uint HsvToArgb(float h, float s, float v)
    {
        float c = v * s;
        float x = c * (1 - MathF.Abs((h / 60f % 2) - 1));
        float m = v - c;
        float r, g, b;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }
        byte R = (byte)Math.Clamp((r + m) * 255f, 0, 255);
        byte G = (byte)Math.Clamp((g + m) * 255f, 0, 255);
        byte B = (byte)Math.Clamp((b + m) * 255f, 0, 255);
        return 0xFF000000u | ((uint)R << 16) | ((uint)G << 8) | B;
    }
}
