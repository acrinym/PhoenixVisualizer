using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

// Ring visualizer that swells with audio energy 🎵
public sealed class EnergyVisualizer : IVisualizerPlugin
{
    public string Id => "energy";
    public string DisplayName => "Energy Ring";

    private int _width;
    private int _height;
    private float _time = 0f;
    private float _lastLevel = 0f; // Instance variable instead of static

    // User parameters (these would be exposed in the UI)
    private float _sensitivity = 1.0f;
    private float _minSize = 0.1f;
    private float _maxSize = 0.8f;
    private float _smoothing = 0.95f;
    private bool _beatReactive = true;
    private bool _showGlow = true;
    private float _glowIntensity = 0.5f;
    private uint _baseColor = 0xFF00FFFF; // Cyan
    private uint _beatColor = 0xFFFFFF00; // Yellow

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0f;
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        _time += 0.016f;

        // Keep the background dark so the glow pops ✨
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
        float scaledLevel = Math.Max(0f, Math.Min(1f, audioLevel * _sensitivity * 5f));
        
        // Apply smoothing to prevent "flash" behavior
        _lastLevel = _lastLevel * _smoothing + scaledLevel * (1f - _smoothing);
        float smoothedLevel = _lastLevel;

        // Calculate ring size with proper bounds
        float maxSize = Math.Min(_width, _height) * _maxSize;
        float minSize = Math.Min(_width, _height) * _minSize;
        float ringSize = minSize + (maxSize - minSize) * smoothedLevel;

        // Choose color based on beat detection
        uint ringColor = beat && _beatReactive ? _beatColor : _baseColor;
        
        // Apply intensity based on audio level
        byte alpha = (byte)(smoothedLevel * 255);
        ringColor = (ringColor & 0x00FFFFFF) | ((uint)alpha << 24);

        // Draw main ring
        canvas.FillCircle(_width / 2f, _height / 2f, ringSize, ringColor);

        // Draw glow effect if enabled
        if (_showGlow && smoothedLevel > 0.1f)
        {
            float glowSize = ringSize * (1f + _glowIntensity);
            byte glowAlpha = (byte)(smoothedLevel * 100 * _glowIntensity);
            uint glowColor = (ringColor & 0x00FFFFFF) | ((uint)glowAlpha << 24);
            canvas.FillCircle(_width / 2f, _height / 2f, glowSize, glowColor);
        }

        // Draw beat pulse effect
        if (beat && _beatReactive)
        {
            float pulseSize = ringSize * 1.5f;
            uint pulseColor = (_beatColor & 0x00FFFFFF) | 0x40u << 24; // Semi-transparent
            canvas.FillCircle(_width / 2f, _height / 2f, pulseSize, pulseColor);
        }

        // Draw energy waves (subtle background effect)
        if (smoothedLevel > 0.3f)
        {
            DrawEnergyWaves(canvas, smoothedLevel);
        }
    }

    private void DrawEnergyWaves(ISkiaCanvas canvas, float energyLevel)
    {
        float centerX = _width / 2f;
        float centerY = _height / 2f;
        float maxRadius = Math.Min(_width, _height) * 0.9f;

        // Draw expanding wave rings
        for (int wave = 0; wave < 3; wave++)
        {
            float waveRadius = (_time * 50f + wave * 40f) % maxRadius;
            float waveAlpha = (1f - wave * 0.3f) * energyLevel * 0.3f;
            byte alpha = (byte)(waveAlpha * 255);
            
            uint waveColor = (_baseColor & 0x00FFFFFF) | ((uint)alpha << 24);
            canvas.DrawCircle(centerX, centerY, waveRadius, waveColor, false);
        }
    }

    public void Dispose()
    {
        // Nothing to clean up here 😊
    }

    // Parameter setters for UI binding
    public void SetSensitivity(float sensitivity) => _sensitivity = Math.Max(0.1f, Math.Min(10f, sensitivity));
    public void SetMinSize(float minSize) => _minSize = Math.Max(0.01f, Math.Min(0.5f, minSize));
    public void SetMaxSize(float maxSize) => _maxSize = Math.Max(0.5f, Math.Min(0.95f, maxSize));
    public void SetSmoothing(float smoothing) => _smoothing = Math.Max(0.5f, Math.Min(0.99f, smoothing));
    public void SetBeatReactive(bool beatReactive) => _beatReactive = beatReactive;
    public void SetShowGlow(bool showGlow) => _showGlow = showGlow;
    public void SetGlowIntensity(float glowIntensity) => _glowIntensity = Math.Max(0f, Math.Min(2f, glowIntensity));
    public void SetBaseColor(uint color) => _baseColor = color;
    public void SetBeatColor(uint color) => _beatColor = color;
}
