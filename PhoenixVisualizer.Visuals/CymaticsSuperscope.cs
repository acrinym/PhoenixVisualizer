using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Cymatics Frequency superscope visualization based on AVS superscope code
/// </summary>
public sealed class CymaticsSuperscope : IVisualizerPlugin
{
    public string Id => "cymatics_superscope";
    public string DisplayName => "Cymatics Frequency";

    private int _width;
    private int _height;
    private float _time;
    private int _numPoints = 360;
    private float _frequency = 174.0f; // Start with 174Hz (Solfeggio frequency)

    // User-adjustable parameters
    private float _amplitude = 1.0f;
    private float _waveSpeed = 1.0f;
    private int _waveType = 0; // 0=sine, 1=square, 2=triangle, 3=sawtooth
    private float _colorShift = 0.0f;
    private bool _autoFrequency = true;

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

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        // Clear with dark background
        canvas.Clear(0xFF000000);
        
        // Update time
        _time += 0.02f;
        
        // Get audio data
        float volume = features.Volume;
        bool beat = features.Beat;
        
        // Handle beat events and audio-reactive frequency changes
        if (_autoFrequency && beat)
        {
            // Cycle through Solfeggio frequencies
            float[] frequencies = { 174.0f, 285.0f, 396.0f, 417.0f, 528.0f, 639.0f, 741.0f, 852.0f, 963.0f };
            _frequency = frequencies[Random.Shared.Next(frequencies.Length)];
        }

        // Audio-reactive parameter adjustments
        _amplitude = 0.5f + volume * 1.5f;
        _colorShift += (features.Bass - features.Treble) * 0.01f;
        
        // Create points array for the cymatics pattern
        var points = new System.Collections.Generic.List<(float x, float y)>();
        
        for (int i = 0; i < _numPoints; i++)
        {
            float t = i / (float)_numPoints;
            
            // Enhanced cymatics with different wave types
            float r = t * (float)Math.PI * 2;

            // Generate wave based on selected type
            float waveValue = GenerateWave(_frequency * _time * _waveSpeed + r * _frequency * 0.1f, _waveType);
            float d = 0.35f + 0.05f * waveValue * _amplitude;

            float x = (float)Math.Cos(r) * d;
            float y = (float)Math.Sin(r) * d;
            
            // Scale and center
            x = x * _width * 0.4f + _width * 0.5f;
            y = y * _height * 0.4f + _height * 0.5f;
            
            points.Add((x, y));
        }
        
        // Draw the cymatics pattern with rainbow colors
        canvas.SetLineWidth(1.0f);
        
        // Draw each point with different colors based on frequency
        for (int i = 0; i < points.Count - 1; i++)
        {
            float phi = i * 6.283f * 2;
            uint red = (uint)((0.5f + 0.5f * Math.Sin(phi * _frequency / 100.0f)) * 255);
            uint green = (uint)((0.5f + 0.5f * Math.Sin(phi * _frequency / 100.0f + 2.094f)) * 255);
            uint blue = (uint)((0.5f + 0.5f * Math.Sin(phi * _frequency / 100.0f + 4.188f)) * 255);
            
            uint color = (uint)((0xFF << 24) | (red << 16) | (green << 8) | blue);
            
            canvas.DrawLine(points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, color, 1.0f);
        }
        
        // Draw frequency indicator
        uint textColor = beat ? 0xFFFFFF00 : 0xFF00FFFF;
        canvas.DrawText($"{_frequency:F0}Hz", 10, 30, textColor, 16.0f);
    }

    private float GenerateWave(float phase, int waveType)
    {
        switch (waveType)
        {
            case 0: // Sine wave
                return (float)Math.Sin(phase);
            case 1: // Square wave
                return Math.Sign(Math.Sin(phase));
            case 2: // Triangle wave
                return (float)(2f / Math.PI * Math.Asin(Math.Sin(phase)));
            case 3: // Sawtooth wave
                return (float)(2f / Math.PI * Math.Atan(Math.Tan(phase * 0.5f)));
            default:
                return (float)Math.Sin(phase);
        }
    }

    public void Dispose()
    {
        // Nothing to clean up
    }
}
