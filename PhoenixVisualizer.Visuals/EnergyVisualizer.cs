using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Energy Ring Visualizer - Fixed version that properly displays beats, peaks, and frequencies
/// FIXED: Now properly sized to not overshadow controls
/// FIXED: Actually visualizes beats/peaks/frequencies instead of random circles
/// FIXED: Properly sized and positioned
/// </summary>
public sealed class EnergyVisualizer : IVisualizerPlugin
{
    public string Id => "energy";
    public string DisplayName => "Energy Ring";

    private int _width;
    private int _height;
    private float _time = 0f;
    private float _lastLevel = 0f;
    private float _beatPulsePhase = 0f;

    // User parameters
    private float _sensitivity = 1.0f;
    private float _minSize = 0.1f;
    private float _maxSize = 0.4f; // FIXED: Reduced from 0.8f to prevent covering controls
    private float _smoothing = 0.95f;
    private bool _beatReactive = true;
    private bool _showGlow = true;
    private float _glowIntensity = 0.5f;
    private uint _baseColor = 0xFF00FFFF; // Cyan
    private uint _beatColor = 0xFFFFFF00; // Yellow
    private uint _frequencyColor = 0xFFFF00FF; // Magenta
    private float _marginFactor = 0.6f; // Leave 40% margin for controls

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
        float bass = features.Bass;
        float mid = features.Mid;
        float treble = features.Treble;

        // Use the best available audio data - FIXED: Now uses actual audio input
        float audioLevel = Math.Max(energy, rms);
        if (audioLevel < 0.001f) audioLevel = volume; // Fallback to volume

        // Apply sensitivity and proper scaling
        float scaledLevel = Math.Max(0f, Math.Min(1f, audioLevel * _sensitivity * 5f));
        
        // Apply smoothing to prevent "flash" behavior
        _lastLevel = _lastLevel * _smoothing + scaledLevel * (1f - _smoothing);
        float smoothedLevel = _lastLevel;

        // Calculate ring size with proper bounds and margins - FIXED: Now properly sized
        float availableSpace = Math.Min(_width, _height) * _marginFactor;
        float maxSize = availableSpace * _maxSize;
        float minSize = availableSpace * _minSize;
        float ringSize = minSize + (maxSize - minSize) * smoothedLevel;

        // Additional safety check
        float maxAllowedSize = Math.Min(_width, _height) * 0.35f; // Never exceed 35% of screen
        ringSize = Math.Min(ringSize, maxAllowedSize);

        // Choose color based on beat detection and frequency content
        uint ringColor = _baseColor;
        if (beat && _beatReactive)
        {
            ringColor = _beatColor;
            _beatPulsePhase = 1f; // Trigger beat pulse
        }
        else if (treble > 0.6f)
        {
            ringColor = _frequencyColor; // High frequencies
        }
        
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

        // Draw beat pulse effect - FIXED: Now properly responds to beats
        if (beat && _beatReactive)
        {
            float pulseSize = ringSize * 1.8f;
            uint pulseColor = (_beatColor & 0x00FFFFFF) | 0x80u << 24; // Semi-transparent
            canvas.FillCircle(_width / 2f, _height / 2f, pulseSize, pulseColor);
            
            // Additional beat ring
            float ringSize2 = ringSize * 1.3f;
            canvas.DrawCircle(_width / 2f, _height / 2f, ringSize2, _beatColor, false);
        }

        // Draw frequency-reactive elements - FIXED: Now shows frequency content
        DrawFrequencyElements(canvas, bass, mid, treble, ringSize);

        // Draw energy waves (subtle background effect)
        if (smoothedLevel > 0.3f)
        {
            DrawEnergyWaves(canvas, smoothedLevel);
        }

        // Update beat pulse phase
        if (_beatPulsePhase > 0f)
        {
            _beatPulsePhase *= 0.85f; // Decay beat pulse
        }
    }

    private void DrawFrequencyElements(ISkiaCanvas canvas, float bass, float mid, float treble, float baseSize)
    {
        float centerX = _width / 2f;
        float centerY = _height / 2f;

        // Bass rings (larger, slower)
        if (bass > 0.3f)
        {
            float bassSize = baseSize * (1.2f + bass * 0.3f);
            uint bassColor = 0xFF0000FF; // Blue for bass
            byte bassAlpha = (byte)(bass * 120);
            bassColor = (bassColor & 0x00FFFFFF) | ((uint)bassAlpha << 24);
            canvas.DrawCircle(centerX, centerY, bassSize, bassColor, false);
        }

        // Mid frequency rings
        if (mid > 0.3f)
        {
            float midSize = baseSize * (0.9f + mid * 0.2f);
            uint midColor = 0xFF00FF00; // Green for mid
            byte midAlpha = (byte)(mid * 100);
            midColor = (midColor & 0x00FFFFFF) | ((uint)midAlpha << 24);
            canvas.DrawCircle(centerX, centerY, midSize, midColor, false);
        }

        // Treble elements (smaller, faster)
        if (treble > 0.3f)
        {
            for (int i = 0; i < 12; i++)
            {
                float angle = (_time * 4f + i * MathF.PI / 6f);
                float radius = baseSize * (0.7f + treble * 0.3f);
                float x = centerX + MathF.Cos(angle) * radius;
                float y = centerY + MathF.Sin(angle) * radius;
                float dotSize = 2f + treble * 4f;
                
                uint trebleColor = 0xFFFF00FF; // Magenta for treble
                byte trebleAlpha = (byte)(treble * 180);
                trebleColor = (trebleColor & 0x00FFFFFF) | ((uint)trebleAlpha << 24);
                canvas.FillCircle(x, y, dotSize, trebleColor);
            }
        }
    }

    private void DrawEnergyWaves(ISkiaCanvas canvas, float energyLevel)
    {
        float centerX = _width / 2f;
        float centerY = _height / 2f;
        float maxRadius = Math.Min(_width, _height) * _marginFactor * 0.8f;

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
    public void SetMaxSize(float maxSize) => _maxSize = Math.Max(0.2f, Math.Min(0.5f, maxSize));
    public void SetSmoothing(float smoothing) => _smoothing = Math.Max(0.5f, Math.Min(0.99f, smoothing));
    public void SetBeatReactive(bool beatReactive) => _beatReactive = beatReactive;
    public void SetShowGlow(bool showGlow) => _showGlow = showGlow;
    public void SetGlowIntensity(float glowIntensity) => _glowIntensity = Math.Max(0f, Math.Min(2f, glowIntensity));
    public void SetBaseColor(uint color) => _baseColor = color;
    public void SetBeatColor(uint color) => _beatColor = color;
    public void SetFrequencyColor(uint color) => _frequencyColor = color;
    public void SetMarginFactor(float marginFactor) => _marginFactor = Math.Max(0.4f, Math.Min(0.8f, marginFactor));
}
