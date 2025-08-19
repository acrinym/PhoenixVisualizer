using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Vertical Bouncing Scope superscope visualization based on AVS superscope code
/// </summary>
public sealed class BouncingScopeSuperscope : IVisualizerPlugin
{
    public string Id => "bouncing_scope_superscope";
    public string DisplayName => "Vertical Bouncing Scope";

    private int _width;
    private int _height;
    private float _time;
    private float _targetVelocity;
    private float _direction = 1;
    private int _numPoints = 100;

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;
        _targetVelocity = 0.1f;
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
        
        // Update time with damping
        _time = _time * 0.9f + _targetVelocity * 0.1f;
        
        // Get audio data
        float volume = features.Volume;
        bool beat = features.Beat;
        
        // Handle beat events
        if (beat)
        {
            _targetVelocity = ((float)Random.Shared.NextDouble() * 50.0f / 50.0f) * _direction;
            _direction = -_direction;
        }
        
        // Create points array for the bouncing scope
        var points = new System.Collections.Generic.List<(float x, float y)>();
        
        for (int i = 0; i < _numPoints; i++)
        {
            float t = i / (float)_numPoints;
            
            // Bouncing scope formula from AVS: x=t+v*pow(sin(i*$PI),2); y=i*2-1.0;
            float x = _time + volume * (float)Math.Pow(Math.Sin(t * (float)Math.PI), 2);
            float y = t * 2 - 1.0f;
            
            // Scale and center
            x = x * _width * 0.4f + _width * 0.5f;
            y = y * _height * 0.4f + _height * 0.5f;
            
            points.Add((x, y));
        }
        
        // Draw the bouncing scope
        uint color = beat ? 0xFFFF0080 : 0xFF8000FF; // Pink on beat, purple otherwise
        canvas.SetLineWidth(1.0f);
        canvas.DrawLines(points.ToArray(), 1.0f, color);
    }

    public void Dispose()
    {
        // Nothing to clean up
    }
}
