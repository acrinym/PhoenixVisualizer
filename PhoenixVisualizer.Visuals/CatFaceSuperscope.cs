using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Cat Face Outline superscope visualization based on AVS superscope code
/// </summary>
public sealed class CatFaceSuperscope : IVisualizerPlugin
{
    public string Id => "cat_face_superscope";
    public string DisplayName => "Cat Face Outline";

    private int _width;
    private int _height;
    private float _time;
    private int _numPoints = 320;

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
        
        // Create points array for the cat face
        var points = new System.Collections.Generic.List<(float x, float y)>();
        
        for (int i = 0; i < _numPoints; i++)
        {
            float t = i / (float)_numPoints;
            
            // Cat face formula from AVS: r=i*$PI*2; d=0.45; x=cos(r)*d; y=sin(r)*d; y=y+(i>0.75?0.2*sin(t*4):0);
            float r = t * (float)Math.PI * 2;
            float d = 0.45f;
            float x = (float)Math.Cos(r) * d;
            float y = (float)Math.Sin(r) * d;
            
            // Add ear movement for the top part
            if (t > 0.75f)
            {
                y += 0.2f * (float)Math.Sin(_time * 4);
            }
            
            // Scale and center
            x = x * _width * 0.4f + _width * 0.5f;
            y = y * _height * 0.4f + _height * 0.5f;
            
            points.Add((x, y));
        }
        
        // Draw the cat face (avoid cyan/green)
        uint color = beat ? 0xFFFFA500 : 0xFFFF8844; // Orange on beat, warm orange otherwise
        canvas.SetLineWidth(2.0f);
        canvas.DrawLines(points.ToArray(), 2.0f, color);
        // Close the loop for a solid outline
        if (points.Count > 1)
        {
            canvas.DrawLine(points[^1].x, points[^1].y, points[0].x, points[0].y, color, 2.0f);
        }
        
        // Draw cat eyes
        float eyeSize = 8.0f;
        // Avoid green for idle
        uint eyeColor = beat ? 0xFFFF0000 : 0xFFFFCC00; // Red on beat, yellow otherwise
        
        // Left eye
        canvas.FillCircle(_width * 0.4f, _height * 0.45f, eyeSize, eyeColor);
        // Right eye
        canvas.FillCircle(_width * 0.6f, _height * 0.45f, eyeSize, eyeColor);
        
        // Draw nose
        canvas.FillCircle(_width * 0.5f, _height * 0.55f, 4.0f, 0xFFFF69B4);
    }

    public void Dispose()
    {
        // Nothing to clean up
    }
}
