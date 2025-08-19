using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Spiral Graph Fun superscope visualization based on AVS superscope code
/// </summary>
public sealed class SpiralGraphSuperscope : IVisualizerPlugin
{
    public string Id => "spiral_graph_superscope";
    public string DisplayName => "Spiral Graph Fun";

    private int _width;
    private int _height;
    private float _time;
    private int _numPoints = 100;

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
        _time += 0.01f;
        
        // Get audio data
        float volume = features.Volume;
        bool beat = features.Beat;
        
        // Handle beat events - change number of points
        if (beat)
        {
            _numPoints = Random.Shared.Next(80, 120);
        }
        
        // Create points array for the spiral graph
        var points = new System.Collections.Generic.List<(float x, float y)>();
        
        for (int i = 0; i < _numPoints; i++)
        {
            float t = i / (float)_numPoints;
            
            // Spiral graph formula from AVS: r=i*$PI*128+t; x=cos(r/64)*0.7+sin(r)*0.3; y=sin(r/64)*0.7+cos(r)*0.3
            float r = t * (float)Math.PI * 128 + _time;
            float x = (float)Math.Cos(r / 64) * 0.7f + (float)Math.Sin(r) * 0.3f;
            float y = (float)Math.Sin(r / 64) * 0.7f + (float)Math.Cos(r) * 0.3f;
            
            // Scale and center
            x = x * _width * 0.4f + _width * 0.5f;
            y = y * _height * 0.4f + _height * 0.5f;
            
            points.Add((x, y));
        }
        
        // Draw the spiral graph
        uint color = beat ? 0xFFFF4000 : 0xFF40FF00; // Orange on beat, lime otherwise
        canvas.SetLineWidth(1.0f);
        canvas.DrawLines(points.ToArray(), 1.0f, color);
    }

    public void Dispose()
    {
        // Nothing to clean up
    }
}
