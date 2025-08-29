using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Enhanced sanity check visualizer with spectrum analysis, audio reactivity,
/// and multiple visual parameters for testing the render pipeline. 🎧📊
/// </summary>
public sealed class SanityVisualizer : IVisualizerPlugin
{
    public string Id => "sanity";
    public string DisplayName => "Sanity Check";

    private int _w, _h;
    private float _time;
    private Random _random = new();
    private float _hueShift;

    // Spectrum visualization
    private const int SPECTRUM_BARS = 32;
    private float[] _spectrumHistory = new float[SPECTRUM_BARS];
    private float _spectrumDecay = 0.95f;

    public void Initialize(int width, int height)
    {
        (_w, _h) = (width, height);
        _spectrumHistory = new float[SPECTRUM_BARS];
    }

    public void Resize(int width, int height) => (_w, _h) = (width, height);

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        try
        {
            // Update time
            _time += 0.016f;
            _hueShift += 0.01f;

            // Dynamic background based on audio
            float bgBrightness = Math.Min(f.Volume * 0.3f, 0.1f);
            uint bgColor = (uint)(0xFF000000 | ((uint)(bgBrightness * 255) << 16) | ((uint)(bgBrightness * 255) << 8) | (uint)(bgBrightness * 255));
            canvas.Clear(bgColor);

            // 1. Enhanced bouncing line with audio reactivity
            RenderBouncingLine(canvas, f);

            // 2. Spectrum bars at the bottom
            RenderSpectrumBars(canvas, f);

            // 3. Audio level indicator (VU meter style)
            RenderAudioLevelIndicator(canvas, f);

            // 4. Corner indicators for various audio features
            RenderCornerIndicators(canvas, f);

            // 5. Center pulsing circle for beat detection
            RenderBeatPulse(canvas, f);

            // 6. Parameter display
            RenderParameterDisplay(canvas, f);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SanityVisualizer] RenderFrame failed: {ex.Message}");
            RenderFallbackDisplay(canvas);
        }
    }

    private void RenderBouncingLine(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Get time source with fallback
        double timeSeconds = f.TimeSeconds;
        if (timeSeconds <= 0 || double.IsNaN(timeSeconds) || double.IsInfinity(timeSeconds))
        {
            timeSeconds = DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond;
        }

        // Audio-reactive bouncing
        float phase = (float)(timeSeconds % 2.0);
        float t = phase <= 1f ? phase : 2f - phase;
        float x = t * _w;

        // Audio influences line thickness and color
        float thickness = 3f + f.Volume * 5f;
        float hue = (_hueShift + f.Bass * 0.5f) % 1f;
        uint color = HsvToRgb(hue, 0.8f, 0.9f);

        var line = new (float x, float y)[2]
        {
            (x, 0),
            (x, _h)
        };
        canvas.DrawLines(line, thickness, color);
    }

    private void RenderSpectrumBars(ISkiaCanvas canvas, AudioFeatures f)
    {
        if (f.Fft == null || f.Fft.Length == 0) return;

        float barWidth = (float)_w / SPECTRUM_BARS;
        float bottomY = _h - 10;

        // Update spectrum history with decay
        for (int i = 0; i < SPECTRUM_BARS; i++)
        {
            float fftIndex = (float)i / SPECTRUM_BARS * Math.Min(f.Fft.Length, 256);
            float fftValue = f.Fft.Length > fftIndex ? Math.Abs(f.Fft[(int)fftIndex]) : 0f;
            _spectrumHistory[i] = Math.Max(_spectrumHistory[i] * _spectrumDecay, fftValue);
        }

        // Render bars
        for (int i = 0; i < SPECTRUM_BARS; i++)
        {
            float barHeight = _spectrumHistory[i] * 100f;
            float x = i * barWidth;
            float hue = (float)i / SPECTRUM_BARS;

            uint color = HsvToRgb(hue, 1f, 0.8f);
            canvas.FillRect(x, bottomY - barHeight, barWidth - 1, barHeight, color);
        }
    }

    private void RenderAudioLevelIndicator(ISkiaCanvas canvas, AudioFeatures f)
    {
        // VU meter style indicator on the left
        float level = Math.Min(f.Volume * 2f, 1f);
        float meterHeight = _h * 0.6f;
        float meterWidth = 20;
        float meterX = 10;
        float meterY = _h * 0.2f;

        // Background
        canvas.FillRect(meterX, meterY, meterWidth, meterHeight, 0xFF333333);

        // Level indicator
        float levelHeight = level * meterHeight;
        uint levelColor = level > 0.8f ? 0xFFFF0000 : level > 0.6f ? 0xFFFFFF00 : 0xFF00FF00;
        canvas.FillRect(meterX, meterY + meterHeight - levelHeight, meterWidth, levelHeight, levelColor);

        // Peak indicator
        if (f.Beat)
        {
            canvas.FillRect(meterX - 2, meterY + meterHeight - levelHeight - 2, meterWidth + 4, 4, 0xFFFFFFFF);
        }
    }

    private void RenderCornerIndicators(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Top-left: Bass level
        RenderCornerMeter(canvas, 10, 10, f.Bass, "BASS", 0xFFFF4444);

        // Top-right: Mid level
        RenderCornerMeter(canvas, _w - 110, 10, f.Mid, "MID", 0xFF44FF44);

        // Bottom-right: Treble level
        RenderCornerMeter(canvas, _w - 110, _h - 60, f.Treble, "TREBLE", 0xFF4444FF);

        // Bottom-left: BPM indicator
        RenderBPMIndicator(canvas, 10, _h - 60, f);
    }

    private void RenderCornerMeter(ISkiaCanvas canvas, float x, float y, float value, string label, uint color)
    {
        // Label
        canvas.DrawText(label, x, y + 12, color, 10f);

        // Meter bar
        float barWidth = 80;
        float barHeight = 8;
        canvas.FillRect(x, y + 15, barWidth, barHeight, 0xFF333333);
        canvas.FillRect(x, y + 15, value * barWidth, barHeight, color);
    }

    private void RenderBPMIndicator(ISkiaCanvas canvas, float x, float y, AudioFeatures f)
    {
        string bpmText = $"BPM: {(f is AudioFeaturesImpl afi && afi.Bpm > 0 ? afi.Bpm.ToString("F0") : "--")}";
        uint bpmColor = f.Beat ? 0xFFFFFF00 : 0xFF888888;
        canvas.DrawText(bpmText, x, y + 12, bpmColor, 10f);

        // Beat flash
        if (f.Beat)
        {
            canvas.FillCircle(x + 40, y + 25, 5, 0xFFFFFF00);
        }
    }

    private void RenderBeatPulse(ISkiaCanvas canvas, AudioFeatures f)
    {
        float centerX = _w * 0.5f;
        float centerY = _h * 0.5f;
        float baseRadius = 20f;
        float pulseRadius = baseRadius + (f.Beat ? 15f : 0f) + f.Volume * 10f;

        uint pulseColor = f.Beat ? 0x80FFFFFF : 0x40FFFFFF;
        canvas.FillCircle(centerX, centerY, pulseRadius, pulseColor);

        // Inner circle
        canvas.FillCircle(centerX, centerY, baseRadius, 0x80FFFFFF);
    }

    private void RenderParameterDisplay(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Display key parameters in the center
        float centerX = _w * 0.5f;
        float startY = _h * 0.4f;

        string[] paramList = new[]
        {
            $"Volume: {f.Volume:F2}",
            $"Bass: {f.Bass:F2}",
            $"Mid: {f.Mid:F2}",
            $"Treble: {f.Treble:F2}",
            $"Time: {f.TimeSeconds:F1}s",
            $"Beat: {(f.Beat ? "YES" : "no")}"
        };

        for (int i = 0; i < paramList.Length; i++)
        {
            float y = startY + i * 15;
            uint color = f.Beat && i == 5 ? 0xFFFFFF00 : 0xFFCCCCCC;
            canvas.DrawText(paramList[i], centerX - 80, y, color, 9f);
        }
    }

    private void RenderFallbackDisplay(ISkiaCanvas canvas)
    {
        canvas.Clear(0xFF000000);
        canvas.DrawText("Sanity Check", _w * 0.5f - 50, _h * 0.5f, 0xFF40C4FF, 16f);
    }

    private static uint HsvToRgb(float h, float s, float v)
    {
        float c = v * s, x = c * (1f - Math.Abs((h * 6f) % 2f - 1f)), m = v - c;
        float r, g, b;
        if (h < 1f / 6f) { r = c; g = x; b = 0f; }
        else if (h < 2f / 6f) { r = x; g = c; b = 0f; }
        else if (h < 3f / 6f) { r = 0f; g = c; b = x; }
        else if (h < 4f / 6f) { r = 0f; g = x; b = c; }
        else if (h < 5f / 6f) { r = x; g = 0f; b = c; }
        else { r = c; g = 0f; b = x; }
        byte R = (byte)((r + m) * 255f); byte G = (byte)((g + m) * 255f); byte B = (byte)((b + m) * 255f);
        return (uint)(0xFF000000 | ((uint)R << 16) | ((uint)G << 8) | B);
    }

    public void Dispose() { }
}

