using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Spiral superscope visualization based on AVS superscope code
/// </summary>
public sealed class SpiralSuperscope : IVisualizerPlugin
{
    public string Id => "spiral_superscope";
    public string DisplayName => "Spiral Superscope";

    private int _width;
    private int _height;
    private float _time;
    private int _numPoints = 800;

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
        _time -= 0.05f;
        
        // Get audio data
        float volume = features.Volume;
        bool beat = features.Beat;
        
        // Create points array for the spiral
        var points = new System.Collections.Generic.List<(float x, float y)>();
        
        for (int i = 0; i < _numPoints; i++)
        {
            float t = i / (float)_numPoints;
            
            // Spiral formula from AVS: d=i+v*0.2; r=t+i*$PI*4; x=cos(r)*d; y=sin(r)*d
            float d = t + volume * 0.2f;
            float r = _time + t * (float)Math.PI * 4;
            
            float x = (float)Math.Cos(r) * d;
            float y = (float)Math.Sin(r) * d;
            
            // Scale and center
            x = x * _width * 0.3f + _width * 0.5f;
            y = y * _height * 0.3f + _height * 0.5f;
            
            points.Add((x, y));
        }
        
        // Draw the spiral
        uint color = beat ? 0xFFFFFF00 : 0xFF00FFFF; // Yellow on beat, cyan otherwise
        canvas.SetLineWidth(1.0f);
        canvas.DrawLines(points.ToArray(), 1.0f, color);
    }

    public void Dispose()
    {
        // Nothing to clean up
    }
}
