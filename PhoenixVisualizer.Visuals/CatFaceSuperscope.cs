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
            float angle = t * (float)Math.PI * 2;

            // Create proper cat face outline with ears
            float x, y;

            if (t < 0.25f) // Left side of face (more circular)
            {
                float radius = 0.45f;
                x = (float)Math.Cos(angle) * radius;
                y = (float)Math.Sin(angle) * radius;
            }
            else if (t < 0.75f) // Bottom of face (flatter)
            {
                float radius = 0.45f + 0.1f * (float)Math.Sin(angle); // Slight bulge
                x = (float)Math.Cos(angle) * radius;
                y = (float)Math.Sin(angle) * radius;
            }
            else // Right side of face (more circular)
            {
                float radius = 0.45f;
                x = (float)Math.Cos(angle) * radius;
                y = (float)Math.Sin(angle) * radius;
            }

            // Add ear shapes at the top
            if (t > 0.8f || t < 0.2f)
            {
                // Position ears slightly above the head
                float earOffset = 0.08f;
                if (t > 0.8f) // Right ear
                {
                    x += earOffset;
                    y -= earOffset;
                }
                else // Left ear
                {
                    x -= earOffset;
                    y -= earOffset;
                }

                // Add ear animation
                y += 0.05f * (float)Math.Sin(_time * 3 + angle * 2);
            }

            // Add subtle breathing animation to the whole face
            float breathingScale = 1f + 0.02f * (float)Math.Sin(_time * 2);
            x *= breathingScale;
            y *= breathingScale;

            // Convert from AVS coordinate system (-1 to 1) to screen coordinates
            x = (x + 1.0f) * _width * 0.5f;
            y = (y + 1.0f) * _height * 0.5f;

            // Clamp to screen bounds to prevent drawing outside
            x = Math.Max(0, Math.Min(_width - 1, x));
            y = Math.Max(0, Math.Min(_height - 1, y));

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
