using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

// Pulsing circle visualizer driven by energy 🚨
public sealed class PulseVisualizer : IVisualizerPlugin
{
    public string Id => "pulse";
    public string DisplayName => "Pulse Circle";

    private int _width;
    private int _height;
    private float _time = 0f;
    private float _lastLevel = 0f; // Instance variable instead of static

    // User parameters (these would be exposed in the UI)
    private float _sensitivity = 1.0f;
    private float _minSize = 0.05f;
    private float _maxSize = 0.35f; // FIXED: Reduced from 0.7f to 0.35f to prevent covering controls
    private float _smoothing = 0.92f;
    private bool _beatReactive = true;
    private bool _showPulseWaves = true;
    private float _pulseWaveSpeed = 1.0f;
    private uint _baseColor = 0xFFFFAA00; // Orange
    private uint _beatColor = 0xFFFFFFFF; // White
    private float _marginFactor = 0.8f; // Leave 20% margin for controls

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
        
        // Get audio data with proper scaling
        float energy = features.Energy;
        float rms = features.Rms;
        float volume = features.Volume;
        bool beat = features.Beat;

        // Use the best available audio data
        float audioLevel = Math.Max(energy, rms);
        if (audioLevel < 0.001f) audioLevel = volume; // Fallback to volume

        // Apply sensitivity and proper scaling
        float scaledLevel = Math.Max(0f, Math.Min(1f, audioLevel * _sensitivity * 3f));
        
        // Apply smoothing to prevent "flash" behavior
        _lastLevel = _lastLevel * _smoothing + scaledLevel * (1f - _smoothing);
        float smoothedLevel = _lastLevel;

        // Calculate circle size with proper bounds and margins for controls
        float availableSpace = Math.Min(_width, _height) * _marginFactor; // Leave margin for controls
        float maxSize = availableSpace * _maxSize; // Now 35% of available space instead of full screen
        float minSize = availableSpace * _minSize;
        float circleSize = minSize + (maxSize - minSize) * smoothedLevel;

        // Additional safety check to ensure circle doesn't get too big
        float maxAllowedSize = Math.Min(_width, _height) * 0.4f; // Never exceed 40% of screen
        circleSize = Math.Min(circleSize, maxAllowedSize);

        // Choose color based on beat detection
        uint circleColor = beat && _beatReactive ? _beatColor : _baseColor;
        
        // Apply intensity based on audio level
        byte alpha = (byte)(smoothedLevel * 255);
        circleColor = (circleColor & 0x00FFFFFF) | ((uint)alpha << 24);

        // Draw main circle
        canvas.FillCircle(_width / 2f, _height / 2f, circleSize, circleColor);

        // Draw pulse waves if enabled
        if (_showPulseWaves && smoothedLevel > 0.2f)
        {
            DrawPulseWaves(canvas, smoothedLevel);
        }

        // Draw beat pulse effect
        if (beat && _beatReactive)
        {
            float pulseSize = circleSize * 1.8f;
            uint pulseColor = (_beatColor & 0x00FFFFFF) | 0x60u << 24; // Semi-transparent
            canvas.FillCircle(_width / 2f, _height / 2f, pulseSize, pulseColor);
        }

        // Draw energy ripples
        if (smoothedLevel > 0.4f)
        {
            DrawEnergyRipples(canvas, smoothedLevel);
        }
    }

    private void DrawPulseWaves(ISkiaCanvas canvas, float energyLevel)
    {
        float centerX = _width / 2f;
        float centerY = _height / 2f;
        float maxRadius = Math.Min(_width, _height) * _marginFactor * 0.8f; // Use same margin system

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
        float maxRadius = Math.Min(_width, _height) * _marginFactor * 0.6f; // Use same margin system

        // Draw energy-based ripple effects
        for (int ripple = 0; ripple < 3; ripple++)
        {
            float rippleRadius = maxRadius * (0.3f + ripple * 0.2f);
            float rippleAlpha = energyLevel * (0.8f - ripple * 0.2f);
            byte alpha = (byte)(rippleAlpha * 255);
            
            uint rippleColor = (_baseColor & 0x00FFFFFF) | ((uint)alpha << 24);
            canvas.DrawCircle(centerX, centerY, rippleRadius, rippleColor, false);
        }
    }

    public void Dispose() { }

    // Parameter setters for UI binding
    public void SetSensitivity(float sensitivity) => _sensitivity = Math.Max(0.1f, Math.Min(10f, sensitivity));
    public void SetMinSize(float minSize) => _minSize = Math.Max(0.01f, Math.Min(0.3f, minSize));
    public void SetMaxSize(float maxSize) => _maxSize = Math.Max(0.1f, Math.Min(0.5f, maxSize)); // Reduced max from 0.9f to 0.5f for safety
    public void SetSmoothing(float smoothing) => _smoothing = Math.Max(0.5f, Math.Min(0.99f, smoothing));
    public void SetBeatReactive(bool beatReactive) => _beatReactive = beatReactive;
    public void SetShowPulseWaves(bool showPulseWaves) => _showPulseWaves = showPulseWaves;
    public void SetPulseWaveSpeed(float pulseWaveSpeed) => _pulseWaveSpeed = Math.Max(0.1f, Math.Min(5f, pulseWaveSpeed));
    public void SetBaseColor(uint color) => _baseColor = color;
    public void SetBeatColor(uint color) => _beatColor = color;
}

