using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.PluginHost;

/// <summary>
/// Advanced spectrum analyzer plugin showcasing enhanced FFT capabilities
/// </summary>
public sealed class SpectrumAnalyzerPlugin : IVisualizerPlugin
{
    public string Id => "spectrum_analyzer";
    public string DisplayName => "Spectrum Analyzer";
    
    private int _width, _height;
    private VisualizationMode _mode = VisualizationMode.Bars;
    private bool _showLabels = true;
    private bool _showBeatIndicator = true;
    private float _smoothingFactor = 0.8f;
    
    // Smoothing buffers for better visualization
    private readonly float[] _smoothedBass = new float[60];
    private readonly float[] _smoothedMid = new float[60];
    private readonly float[] _smoothedTreble = new float[60];
    private int _smoothingIndex = 0;
    
    // Beat detection
    private float _lastBassEnergy = 0;
    private float _lastMidEnergy = 0;
    private float _lastTrebleEnergy = 0;
    private DateTime _lastBassBeat = DateTime.MinValue;
    private DateTime _lastMidBeat = DateTime.MinValue;
    private DateTime _lastTrebleBeat = DateTime.MinValue;
    
    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        ResetSmoothingBuffers();
    }
    
    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }
    
    public void Dispose()
    {
        // Cleanup if needed
    }
    
    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        // Clear with dark background
        canvas.Clear(0xFF0A0A0A);
        
        // Update smoothing buffers
        UpdateSmoothingBuffers(features);
        
        // Render based on selected mode
        switch (_mode)
        {
            case VisualizationMode.Bars:
                RenderFrequencyBars(features, canvas);
                break;
            case VisualizationMode.Waterfall:
                RenderWaterfall(features, canvas);
                break;
            case VisualizationMode.Circular:
                RenderCircularSpectrum(features, canvas);
                break;
            case VisualizationMode.ThreeD:
                Render3DSpectrum(features, canvas);
                break;
        }
        
        // Render beat indicators
        if (_showBeatIndicator)
        {
            RenderBeatIndicators(canvas);
        }
        
        // Render labels and info
        if (_showLabels)
        {
            RenderLabels(features, canvas);
        }
    }
    
    private void UpdateSmoothingBuffers(AudioFeatures features)
    {
        // Update smoothing buffers with new energy values
        _smoothedBass[_smoothingIndex] = features.Bass;
        _smoothedMid[_smoothingIndex] = features.Mid;
        _smoothedTreble[_smoothingIndex] = features.Treble;
        
        _smoothingIndex = (_smoothingIndex + 1) % 60;
        
        // Detect beats in each frequency band
        DetectBeats(features);
    }
    
    private void DetectBeats(AudioFeatures features)
    {
        var now = DateTime.UtcNow;
        const float beatThreshold = 1.5f; // Sensitivity multiplier
        const int cooldownMs = 100; // Minimum time between beats
        
        // Bass beat detection
        if (features.Bass > _lastBassEnergy * beatThreshold && 
            (now - _lastBassBeat).TotalMilliseconds > cooldownMs)
        {
            _lastBassBeat = now;
        }
        
        // Mid beat detection
        if (features.Mid > _lastMidEnergy * beatThreshold && 
            (now - _lastMidBeat).TotalMilliseconds > cooldownMs)
        {
            _lastMidBeat = now;
        }
        
        // Treble beat detection
        if (features.Treble > _lastTrebleEnergy * beatThreshold && 
            (now - _lastTrebleBeat).TotalMilliseconds > cooldownMs)
        {
            _lastTrebleBeat = now;
        }
        
        _lastBassEnergy = features.Bass;
        _lastMidEnergy = features.Mid;
        _lastTrebleEnergy = features.Treble;
    }
    
    private void RenderFrequencyBars(AudioFeatures features, ISkiaCanvas canvas)
    {
        // Use enhanced frequency bands if available
        var bands = features.FrequencyBands;
        if (bands.Length == 0)
        {
            // Fallback to basic FFT
            bands = features.Fft ?? Array.Empty<float>();
        }
        
        if (bands.Length == 0) return;
        
        var barWidth = Math.Max(2f, (float)_width / bands.Length);
        var maxHeight = _height - 40;
        
        for (int i = 0; i < bands.Length; i++)
        {
            var amplitude = MathF.Min(1f, bands[i]);
            var height = amplitude * maxHeight;
            
            // Color based on frequency and amplitude
            var color = GetFrequencyColor(i, bands.Length, amplitude);
            
            var x = i * barWidth;
            var y = _height - 20 - height;
            
            // Main bar
            canvas.FillRect(x, y, barWidth - 1, height, color);
            
            // Glow effect for active bars
            if (amplitude > 0.1f)
            {
                var glowColor = (color & 0x00FFFFFF) | 0x30000000;
                canvas.FillRect(x - 1, y - 1, barWidth + 1, height + 2, glowColor);
            }
        }
    }
    
    private void RenderWaterfall(AudioFeatures features, ISkiaCanvas canvas)
    {
        // Waterfall effect - frequency on X, time on Y
        var bands = features.FrequencyBands;
        if (bands.Length == 0) bands = features.Fft ?? Array.Empty<float>();
        if (bands.Length == 0) return;
        
        var bandWidth = Math.Max(1f, (float)_width / bands.Length);
        var timeHeight = 2; // Height per time slice
        
        // Shift existing waterfall down
        // (This is a simplified version - in a real implementation you'd maintain a buffer)
        
        // Draw current time slice at the top
        for (int i = 0; i < bands.Length; i++)
        {
            var amplitude = MathF.Min(1f, bands[i]);
            var color = GetFrequencyColor(i, bands.Length, amplitude);
            
            var x = i * bandWidth;
            canvas.FillRect(x, 0, bandWidth - 1, timeHeight, color);
        }
    }
    
    private void RenderCircularSpectrum(AudioFeatures features, ISkiaCanvas canvas)
    {
        var centerX = _width / 2f;
        var centerY = _height / 2f;
        var maxRadius = Math.Min(_width, _height) / 2f - 20;
        
        var bands = features.FrequencyBands;
        if (bands.Length == 0) bands = features.Fft ?? Array.Empty<float>();
        if (bands.Length == 0) return;
        
        var angleStep = 2f * MathF.PI / bands.Length;
        
        for (int i = 0; i < bands.Length; i++)
        {
            var amplitude = MathF.Min(1f, bands[i]);
            var radius = maxRadius * (0.3f + 0.7f * amplitude);
            var angle = i * angleStep;
            
            var x = centerX + radius * MathF.Cos(angle);
            var y = centerY + radius * MathF.Sin(angle);
            
            var color = GetFrequencyColor(i, bands.Length, amplitude);
            var size = Math.Max(2f, 4f * amplitude);
            
            canvas.FillCircle(x, y, size, color);
        }
    }
    
    private void Render3DSpectrum(AudioFeatures features, ISkiaCanvas canvas)
    {
        // Simplified 3D effect using perspective
        var bands = features.FrequencyBands;
        if (bands.Length == 0) bands = features.Fft ?? Array.Empty<float>();
        if (bands.Length == 0) return;
        
        var bandWidth = Math.Max(2f, (float)_width / bands.Length);
        var maxHeight = _height - 40;
        
        for (int i = 0; i < bands.Length; i++)
        {
            var amplitude = MathF.Min(1f, bands[i]);
            var height = amplitude * maxHeight;
            
            // 3D effect: offset based on position
            var depth = (float)i / bands.Length;
            var offset = depth * 20; // 3D offset
            
            var x = i * bandWidth + offset;
            var y = _height - 20 - height;
            
            var color = GetFrequencyColor(i, bands.Length, amplitude);
            
            // Main bar with 3D effect
            canvas.FillRect(x, y, bandWidth - 1, height, color);
            
            // Side face (simulated 3D)
            var sideColor = (color & 0x00FFFFFF) | 0x80000000;
            canvas.FillRect(x + bandWidth - 1, y, 5, height, sideColor);
        }
    }
    
    private void RenderBeatIndicators(ISkiaCanvas canvas)
    {
        var now = DateTime.UtcNow;
        var indicatorSize = 15f;
        var margin = 20f;
        
        // Bass beat indicator (left)
        var bassActive = (now - _lastBassBeat).TotalMilliseconds < 200;
        var bassColor = bassActive ? 0xFFFF0000 : 0x80400000;
        canvas.FillCircle(margin, margin, indicatorSize, bassColor);
        
        // Mid beat indicator (center)
        var midActive = (now - _lastMidBeat).TotalMilliseconds < 200;
        var midColor = midActive ? 0xFF00FF00 : 0x80400000;
        canvas.FillCircle(_width / 2f, margin, indicatorSize, midColor);
        
        // Treble beat indicator (right)
        var trebleActive = (now - _lastTrebleBeat).TotalMilliseconds < 200;
        var trebleColor = trebleActive ? 0xFF0000FF : 0x80400000;
        canvas.FillCircle(_width - margin, margin, indicatorSize, trebleColor);
    }
    
    private void RenderLabels(AudioFeatures features, ISkiaCanvas canvas)
    {
        // Render frequency labels
        var labels = new[] { "60Hz", "250Hz", "500Hz", "1kHz", "2kHz", "4kHz", "8kHz", "16kHz" };
        var bandWidth = (float)_width / labels.Length;
        
        for (int i = 0; i < labels.Length; i++)
        {
            var x = i * bandWidth + bandWidth / 2;
            canvas.DrawText(labels[i], x, _height - 5, 0xFFFFFFFF, 10);
        }
        
        // Render energy levels
        var energyText = $"Bass: {features.Bass:F2} | Mid: {features.Mid:F2} | Treble: {features.Treble:F2}";
        canvas.DrawText(energyText, 10, 30, 0xFFFFFFFF, 12);
        
        // Render BPM if available
        if (features.Bpm > 0)
        {
            var bpmText = $"BPM: {features.Bpm:F1}";
            canvas.DrawText(bpmText, 10, 50, 0xFFFFFF00, 12);
        }
        
        // Render current mode
        var modeText = $"Mode: {_mode}";
        canvas.DrawText(modeText, 10, 70, 0xFF00FFFF, 12);
    }
    
    private uint GetFrequencyColor(int bandIndex, int totalBands, float amplitude)
    {
        // Enhanced color scheme with amplitude influence
        var ratio = (float)bandIndex / Math.Max(1, totalBands - 1);
        var intensity = MathF.Min(1f, amplitude * 2f); // Boost intensity
        
        if (ratio < 0.33f)
        {
            // Red to yellow (low frequencies)
            var r = 255;
            var g = (int)(255 * (ratio * 3));
            var b = 0;
            return (uint)((r << 16) | (g << 8) | b) | ((uint)(intensity * 255) << 24);
        }
        else if (ratio < 0.66f)
        {
            // Yellow to green (mid frequencies)
            var r = (int)(255 * (1 - (ratio - 0.33f) * 3));
            var g = 255;
            var b = 0;
            return (uint)((r << 16) | (g << 8) | b) | ((uint)(intensity * 255) << 24);
        }
        else
        {
            // Green to blue (high frequencies)
            var r = 0;
            var g = (int)(255 * (1 - (ratio - 0.66f) * 3));
            var b = (int)(255 * (ratio - 0.66f) * 3);
            return (uint)((r << 16) | (g << 8) | b) | ((uint)(intensity * 255) << 24);
        }
    }
    
    private void ResetSmoothingBuffers()
    {
        Array.Clear(_smoothedBass, 0, _smoothedBass.Length);
        Array.Clear(_smoothedMid, 0, _smoothedMid.Length);
        Array.Clear(_smoothedTreble, 0, _smoothedTreble.Length);
        _smoothingIndex = 0;
    }
    
    // Public methods for external control
    public void SetMode(VisualizationMode mode) => _mode = mode;
    public void ToggleLabels() => _showLabels = !_showLabels;
    public void ToggleBeatIndicator() => _showBeatIndicator = !_showBeatIndicator;
    public void SetSmoothingFactor(float factor) => _smoothingFactor = Math.Clamp(factor, 0f, 1f);
    
    public enum VisualizationMode
    {
        Bars,
        Waterfall,
        Circular,
        ThreeD
    }
}
