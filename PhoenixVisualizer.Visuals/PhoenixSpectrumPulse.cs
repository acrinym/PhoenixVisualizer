using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Phoenix Spectrum Pulse - Enhanced spectrum analyzer with pulsing effects and audio-reactive colors
/// Inspired by Windows Media Player visualizers but with advanced Phoenix features
/// FIXED: Eliminated flickering interjection of primary/secondary colors during pause/visual
/// </summary>
public sealed class PhoenixSpectrumPulse : IVisualizerPlugin
{
    public string Id => "phoenix_spectrum_pulse";
    public string DisplayName => "🔥 Phoenix Spectrum Pulse";

    private int _width, _height;
    private float _time;
    private readonly float[] _previousMagnitudes;
    private readonly float[] _pulsePhases;
    private readonly Random _random = new();

    // Enhanced spectrum constants
    private const int MAX_BARS = 128;
    private const float PULSE_SPEED = 0.05f;
    private const float DECAY_FACTOR = 0.95f;
    private const float SENSITIVITY_BOOST = 2.0f;

    public PhoenixSpectrumPulse()
    {
        _previousMagnitudes = new float[MAX_BARS];
        _pulsePhases = new float[MAX_BARS];

        // Initialize pulse phases with random offsets for organic feel
        for (int i = 0; i < MAX_BARS; i++)
        {
            _pulsePhases[i] = (float)(_random.NextDouble() * Math.PI * 2);
        }
    }

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Dispose() { }

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        // FIXED: Audio-reactive time and animation updates
        var energy = f.Energy;
        var bass = f.Bass;
        var mid = f.Mid;
        var treble = f.Treble;
        var beat = f.Beat;
        var volume = f.Volume;
        
        // Audio-reactive animation speed
        var baseSpeed = 0.016f;
        var energySpeed = energy * 0.02f;
        var trebleSpeed = treble * 0.015f;
        var beatSpeed = beat ? 0.03f : 0f;
        _time += baseSpeed + energySpeed + trebleSpeed + beatSpeed;

        // FIXED: Enhanced audio-reactive background
        uint bgColor = 0xFF0A0A0F; // Very dark blue-black
        
        // Audio-reactive background color
        if (beat)
            bgColor = 0xFF0A0A1F; // Slightly purple on beat
        else if (bass > 0.5f)
            bgColor = 0xFF1A0A0A; // Slightly red for bass
        else if (treble > 0.4f)
            bgColor = 0xFF0A1A1A; // Slightly cyan for treble
        else if (energy > 0.6f)
            bgColor = 0xFF1A1A0A; // Slightly yellow for energy
            
        canvas.Clear(bgColor);

        if (f.Fft == null || f.Fft.Length == 0) return;

        // Calculate spectrum parameters
        int numBars = Math.Min(MAX_BARS, f.Fft.Length / 2); // Use first half of FFT (positive frequencies)
        float barWidth = (float)_width / numBars;
        float centerY = _height * 0.5f;

        // Render enhanced spectrum bars
        for (int i = 0; i < numBars; i++)
        {
            // Enhanced frequency mapping with logarithmic scaling
            float frequencyRatio = (float)i / (numBars - 1);
            int fftIndex = (int)(frequencyRatio * frequencyRatio * f.Fft.Length * 0.5f); // Exponential mapping
            if (fftIndex >= f.Fft.Length) fftIndex = f.Fft.Length - 1;

            // Calculate magnitude with enhanced processing
            float rawMagnitude = MathF.Abs(f.Fft[fftIndex]);
            float magnitude = ProcessMagnitude(rawMagnitude, frequencyRatio, f);

            // Smooth magnitude changes
            _previousMagnitudes[i] = _previousMagnitudes[i] * DECAY_FACTOR + magnitude * (1 - DECAY_FACTOR);

            // Calculate bar dimensions with better uniformity
            float barHeight = _previousMagnitudes[i] * _height * 0.75f; // Use 75% of screen height for better centering
            float barX = i * barWidth + barWidth * 0.1f; // Add small gap between bars
            float barY = _height * 0.9f - barHeight; // Align to bottom with consistent baseline

            // FIXED: Enhanced color calculation with audio features
            uint barColor = CalculateBarColor(frequencyRatio, _previousMagnitudes[i], f.Volume, i, f);

            // Add pulse effect
            float pulseFactor = CalculatePulseEffect(i, _previousMagnitudes[i], f.Beat);
            float effectiveWidth = (barWidth * 0.8f) * (0.8f + pulseFactor * 0.4f); // Account for gaps, narrower bars
            float effectiveHeight = barHeight * (1f + pulseFactor * 0.3f);

            // Draw the main bar with better positioning
            canvas.FillRect(
                barX,
                barY,
                effectiveWidth,
                effectiveHeight,
                barColor
            );

            // Add glow effect for strong frequencies
            if (_previousMagnitudes[i] > 0.3f)
            {
                DrawBarGlow(canvas, barX, barY, effectiveWidth, effectiveHeight, barColor, _previousMagnitudes[i]);
            }

            // Add reflection effect
            DrawBarReflection(canvas, barX, centerY, effectiveWidth, barHeight, barColor, _previousMagnitudes[i]);
        }

        // Add overall energy indicator
        DrawEnergyIndicator(canvas, f.Volume, f.Bass, f.Mid, f.Treble);
    }

    private float ProcessMagnitude(float rawMagnitude, float frequencyRatio, AudioFeatures f)
    {
        // Enhanced magnitude processing
        float magnitude = rawMagnitude * SENSITIVITY_BOOST;

        // Apply frequency-dependent boost
        float bassBoost = frequencyRatio < 0.2f ? 1.5f : 1.0f;
        float trebleBoost = frequencyRatio > 0.8f ? 1.3f : 1.0f;

        magnitude *= bassBoost * trebleBoost;

        // Apply volume normalization
        magnitude *= (1f + f.Volume * 0.5f);

        // Apply logarithmic scaling for better dynamic range
        magnitude = MathF.Min(1f, MathF.Log(1 + magnitude * 8) / MathF.Log(9));

        return magnitude;
    }

    private uint CalculateBarColor(float frequencyRatio, float magnitude, float volume, int barIndex, AudioFeatures f)
    {
        // FIXED: Stable color calculation to prevent flickering
        var energy = f.Energy;
        var bass = f.Bass;
        var treble = f.Treble;
        var beat = f.Beat;
        
        // Create stable rainbow spectrum mapping
        float hue = frequencyRatio * 360f;

        // FIXED: Ensure uniform distribution across the spectrum without flickering
        if (frequencyRatio < 0.5f)
        {
            // Lower frequencies (red to green): expand range
            hue = frequencyRatio * 2f * 120f; // 0-120 degrees (red to green)
        }
        else
        {
            // Higher frequencies (green to purple): expand range
            hue = 120f + (frequencyRatio - 0.5f) * 2f * 240f; // 120-360 degrees (green to purple)
        }

        // FIXED: Audio-reactive color selection without flickering
        if (beat)
            hue = 60f; // Yellow on beat
        else if (bass > 0.5f)
            hue = 0f; // Red for bass
        else if (treble > 0.4f)
            hue = 180f; // Cyan for treble
        else if (energy > 0.6f)
            hue = 300f; // Magenta for energy

        // FIXED: Stable saturation and brightness calculation
        float saturation = 0.9f + magnitude * 0.1f;
        float brightness = 0.3f + magnitude * 0.7f + volume * 0.3f;

        // FIXED: Reduced time variation to prevent flickering
        float timeVariation = (float)Math.Sin(_time * 0.1f + barIndex * 0.02f) * 5f; // Reduced variation
        hue = (hue + timeVariation) % 360f;

        // Ensure hue stays in valid range
        if (hue < 0) hue += 360f;

        // Convert HSV to RGB
        return HsvToRgb(hue, saturation, brightness);
    }

    private float CalculatePulseEffect(int barIndex, float magnitude, bool beat)
    {
        // Calculate pulse effect
        _pulsePhases[barIndex] += PULSE_SPEED;
        float basePulse = (float)Math.Sin(_pulsePhases[barIndex]) * 0.5f + 0.5f;

        // Enhance pulse on beat
        float beatBoost = beat ? 1.5f : 1.0f;

        // Magnitude-based pulse intensity
        return basePulse * magnitude * beatBoost;
    }

    private void DrawBarGlow(ISkiaCanvas canvas, float x, float y, float width, float height,
                           uint color, float intensity)
    {
        // Create glow effect with multiple layers
        for (int layer = 1; layer <= 3; layer++)
        {
            float glowSize = layer * 4f;
            float alpha = (int)(intensity * 100 / layer);
            uint glowColor = (color & 0x00FFFFFF) | ((uint)alpha << 24);

            canvas.FillRect(
                x - glowSize,
                y - glowSize,
                width + glowSize * 2,
                height + glowSize * 2,
                glowColor
            );
        }
    }

    private void DrawBarReflection(ISkiaCanvas canvas, float x, float centerY, float width,
                                 float height, uint color, float intensity)
    {
        // Create reflection effect below the center line
        float reflectionY = centerY + height * 0.5f;
        float reflectionHeight = height * 0.3f * intensity;

        // Fade the reflection color
        uint reflectionColor = (color & 0x00FFFFFF) | ((uint)(intensity * 80) << 24);

        canvas.FillRect(
            x,
            reflectionY,
            width,
            reflectionHeight,
            reflectionColor
        );
    }

    private void DrawEnergyIndicator(ISkiaCanvas canvas, float volume, float bass, float mid, float treble)
    {
        // Draw energy indicator at the bottom
        float indicatorY = _height - 20;
        float indicatorWidth = _width - 40;
        float indicatorHeight = 8;

        // Background bar
        canvas.FillRect(20, indicatorY, indicatorWidth, indicatorHeight, 0xFF202020);

        // Energy bars for different frequency ranges
        float bassWidth = indicatorWidth * 0.3f * bass;
        float midWidth = indicatorWidth * 0.4f * mid;
        float trebleWidth = indicatorWidth * 0.3f * treble;

        // Bass (red)
        canvas.FillRect(20, indicatorY, bassWidth, indicatorHeight, 0xFFFF4444);

        // Mid (green)
        canvas.FillRect(20 + bassWidth, indicatorY, midWidth, indicatorHeight, 0xFF44FF44);

        // Treble (blue)
        canvas.FillRect(20 + bassWidth + midWidth, indicatorY, trebleWidth, indicatorHeight, 0xFF4444FF);

        // Overall volume indicator
        float volumeBarWidth = indicatorWidth * volume;
        canvas.FillRect(20, indicatorY - 15, volumeBarWidth, 3, 0xFFFFFF00);
    }

    private uint HsvToRgb(float hue, float saturation, float brightness)
    {
        // HSV to RGB conversion
        float c = brightness * saturation;
        float x = c * (1 - MathF.Abs((hue / 60f % 2) - 1));
        float m = brightness - c;

        float r, g, b;

        if (hue < 60)
        {
            r = c; g = x; b = 0;
        }
        else if (hue < 120)
        {
            r = x; g = c; b = 0;
        }
        else if (hue < 180)
        {
            r = 0; g = c; b = x;
        }
        else if (hue < 240)
        {
            r = 0; g = x; b = c;
        }
        else if (hue < 300)
        {
            r = x; g = 0; b = c;
        }
        else
        {
            r = c; g = 0; b = x;
        }

        byte red = (byte)((r + m) * 255);
        byte green = (byte)((g + m) * 255);
        byte blue = (byte)((b + m) * 255);

        return 0xFF000000 | ((uint)red << 16) | ((uint)green << 8) | blue;
    }
}
