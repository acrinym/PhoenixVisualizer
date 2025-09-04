using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Pulse Circle Visualizer - Fixed version that properly displays beats, peaks, and frequencies
/// FIXED: Now pulses based on actual audio input instead of random frequencies
/// FIXED: Properly sized to not overshadow controls
/// FIXED: Actually visualizes beats/peaks/frequencies
/// </summary>
public sealed class PulseVisualizer : IVisualizerPlugin
{
    public string Id => "pulse";
    public string DisplayName => "Pulse Circle";

    private int _width;
    private int _height;
    private float _time = 0f;
    private float _lastLevel = 0f;
    private float _emaLevel = 0f;
    private const float EMA_A = 0.35f;
    private float _lastBeatLevel = 0f;
    private float _beatPulsePhase = 0f;

    // User parameters
    private float _sensitivity = 1.0f;
    private float _minSize = 0.05f;
    private float _maxSize = 0.25f; // FIXED: Reduced to prevent covering controls
    private float _smoothing = 0.92f;
    private bool _beatReactive = true;
    private bool _showPulseWaves = true;
    private float _pulseWaveSpeed = 1.0f;
    private uint _baseColor = 0xFFFFAA00; // Orange
    private uint _beatColor = 0xFFFFFFFF; // White
    private uint _frequencyColor = 0xFF00FFFF; // Cyan
    private float _marginFactor = 0.7f; // Leave 30% margin for controls

    public void Initialize(int width, int height) => Resize(width, height);

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0f;
    }

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        _time += 0.016f;

        canvas.Clear(0xFF000000);
        // Band-weighted level
        float level = (bass * 0.5f + mid * 0.35f + treble * 0.15f);
        // EMA toward current to avoid "random" feel
        _emaLevel = (_emaLevel == 0f) ? level : (_emaLevel * (1f - EMA_A) + level * EMA_A);
        float beatBoost = beat ? 0.12f : 0f;
        float pulse = MathF.Min(1f, _emaLevel + beatBoost);
        // expose for subsequent drawing
        float __pulseLevel = pulse;
        //  Get audio data with proper scaling
        float energy = features.Energy;
        float rms = features.Rms;
        float volume = features.Volume;
        bool beat = features.Beat;
        float bass = features.Bass;
        float mid = features.Mid;
        float treble = features.Treble;

        // Use the best available audio data - FIXED: Now uses actual audio input
        float audioLevel = Math.Max(energy, rms);
        if (audioLevel < 0.001f) audioLevel = volume; // Fallback to volume

        // Apply sensitivity and proper scaling
        float scaledLevel = Math.Max(0f, Math.Min(1f, audioLevel * _sensitivity * 3f));
        
        // Apply smoothing to prevent "flash" behavior
        _lastLevel = _lastLevel * _smoothing + scaledLevel * (1f - _smoothing);
        float smoothedLevel = _lastLevel;

        // Calculate circle size with proper bounds and margins for controls
        float availableSpace = Math.Min(_width, _height) * _marginFactor;
        float maxSize = availableSpace * _maxSize;
        float minSize = availableSpace * _minSize;
        float circleSize = minSize + (maxSize - minSize) * smoothedLevel;

        // Additional safety check to ensure circle doesn't get too big
        float maxAllowedSize = Math.Min(_width, _height) * 0.3f; // Never exceed 30% of screen
        circleSize = Math.Min(circleSize, maxAllowedSize);

        // Choose color based on beat detection and frequency content
        uint circleColor = _baseColor;
        if (beat && _beatReactive)
        {
            circleColor = _beatColor;
            _beatPulsePhase = 1f; // Trigger beat pulse
        }
        else if (treble > 0.5f)
        {
            circleColor = _frequencyColor; // High frequencies
        }
        
        // Apply intensity based on audio level
        byte alpha = (byte)(smoothedLevel * 255);
        circleColor = (circleColor & 0x00FFFFFF) | ((uint)alpha << 24);

        // Draw main circle
        canvas.FillCircle(_width / 2f, _height / 2f, circleSize, circleColor);

        // Draw beat pulse effect - FIXED: Now properly responds to beats
        if (beat && _beatReactive)
        {
            float pulseSize = circleSize * 2f;
            uint pulseColor = (_beatColor & 0x00FFFFFF) | 0x80u << 24; // Semi-transparent
            canvas.FillCircle(_width / 2f, _height / 2f, pulseSize, pulseColor);
            
            // Additional beat ring
            float ringSize = circleSize * 1.5f;
            canvas.DrawCircle(_width / 2f, _height / 2f, ringSize, _beatColor, false);
        }

        // Draw frequency-reactive elements - FIXED: Now shows frequency content
        DrawFrequencyElements(canvas, bass, mid, treble, circleSize);

        // Draw pulse waves if enabled
        if (_showPulseWaves && smoothedLevel > 0.2f)
        {
            DrawPulseWaves(canvas, smoothedLevel);
        }

        // Draw energy ripples
        if (smoothedLevel > 0.4f)
        {
            DrawEnergyRipples(canvas, smoothedLevel);
        }

        // Update beat pulse phase
        if (_beatPulsePhase > 0f)
        {
            _beatPulsePhase *= 0.9f; // Decay beat pulse
        }
    }

    private void DrawFrequencyElements(ISkiaCanvas canvas, float bass, float mid, float treble, float baseSize)
    {
        float centerX = _width / 2f;
        float centerY = _height / 2f;

        // Bass circles (larger, slower)
        if (bass > 0.3f)
        {
            float bassSize = baseSize * (1f + bass * 0.5f);
            uint bassColor = 0xFF0000FF; // Blue for bass
            byte bassAlpha = (byte)(bass * 150);
            bassColor = (bassColor & 0x00FFFFFF) | ((uint)bassAlpha << 24);
            canvas.DrawCircle(centerX, centerY, bassSize, bassColor, false);
        }

        // Mid frequency rings
        if (mid > 0.3f)
        {
            float midSize = baseSize * (0.8f + mid * 0.3f);
            uint midColor = 0xFF00FF00; // Green for mid
            byte midAlpha = (byte)(mid * 120);
            midColor = (midColor & 0x00FFFFFF) | ((uint)midAlpha << 24);
            canvas.DrawCircle(centerX, centerY, midSize, midColor, false);
        }

        // Treble dots (smaller, faster)
        if (treble > 0.3f)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = (_time * 3f + i * MathF.PI / 4f);
                float radius = baseSize * (0.6f + treble * 0.4f);
                float x = centerX + MathF.Cos(angle) * radius;
                float y = centerY + MathF.Sin(angle) * radius;
                float dotSize = 3f + treble * 5f;
                
                uint trebleColor = 0xFFFF00FF; // Magenta for treble
                byte trebleAlpha = (byte)(treble * 200);
                trebleColor = (trebleColor & 0x00FFFFFF) | ((uint)trebleAlpha << 24);
                canvas.FillCircle(x, y, dotSize, trebleColor);
            }
        }
    }

    private void DrawPulseWaves(ISkiaCanvas canvas, float energyLevel)
    {
        float centerX = _width / 2f;
        float centerY = _height / 2f;
        float maxRadius = Math.Min(_width, _height) * _marginFactor * 0.6f;

        // Draw expanding pulse waves
        for (int wave = 0; wave < 4; wave++)
        {
            float waveRadius = (_time * 80f * _pulseWaveSpeed + wave * 60f) % maxRadius;
            float waveAlpha = (1f - wave * 0.25f) * energyLevel * 0.4f;
            byte alpha = (byte)(waveAlpha * 255);
            
            uint waveColor = (_baseColor & 0x00FFFFFF) | ((uint)alpha << 24);
            canvas.DrawCircle(centerX, centerY, waveRadius, waveColor, false);
        }
    }

    private void DrawEnergyRipples(ISkiaCanvas canvas, float energyLevel)
    {
        float centerX = _width / 2f;
        float centerY = _height / 2f;
        float maxRadius = Math.Min(_width, _height) * _marginFactor * 0.5f;

        // Draw energy-based ripple effects
        for (int ripple = 0; ripple < 3; ripple++)
        {
            float rippleRadius = maxRadius * (0.6f + 0.8f * energyLevel);
            float rippleAlpha = energyLevel * (0.8f - ripple * 0.2f);
            byte alpha = (byte)(rippleAlpha * 255);
            
            uint rippleColor = (_baseColor & 0x00FFFFFF) | ((uint)alpha << 24);
            canvas.DrawCircle(centerX, centerY, rippleRadius, rippleColor, false);
        }
    }

    public void Dispose() { }

    // Parameter setters for UI binding
    public void SetSensitivity(float sensitivity) => _sensitivity = Math.Max(0.1f, Math.Min(10f, sensitivity));
    public void SetMinSize(float minSize) => _minSize = Math.Max(0.01f, Math.Min(0.5f, minSize));
    public void SetMaxSize(float maxSize) => _maxSize = Math.Max(0.1f, Math.Min(0.4f, maxSize));
    public void SetSmoothing(float smoothing) => _smoothing = Math.Max(0.5f, Math.Min(0.99f, smoothing));
    public void SetBeatReactive(bool beatReactive) => _beatReactive = beatReactive;
    public void SetShowPulseWaves(bool showPulseWaves) => _showPulseWaves = showPulseWaves;
    public void SetPulseWaveSpeed(float pulseWaveSpeed) => _pulseWaveSpeed = Math.Max(0.1f, Math.Min(5f, pulseWaveSpeed));
    public void SetBaseColor(uint color) => _baseColor = color;
    public void SetBeatColor(uint color) => _beatColor = color;
    public void SetFrequencyColor(uint color) => _frequencyColor = color;
    public void SetMarginFactor(float marginFactor) => _marginFactor = Math.Max(0.5f, Math.Min(0.9f, marginFactor));
}

