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

            // Create more cat-like face outline with proper ears
            float x, y;

            // Create a more cat-like face shape
            if (t < 0.2f || t > 0.8f) // Sides of face (more rounded)
            {
                float radius = 0.4f;
                x = (float)Math.Cos(angle) * radius;
                y = (float)Math.Sin(angle) * radius * 0.9f; // Slightly oval
            }
            else if (t < 0.4f) // Left cheek area (slightly indented)
            {
                float radius = 0.38f;
                x = (float)Math.Cos(angle) * radius;
                y = (float)Math.Sin(angle) * radius;
            }
            else if (t < 0.6f) // Chin and mouth area (flatter)
            {
                float radius = 0.35f + 0.05f * (float)Math.Sin(angle * 2); // Slight curve for mouth
                x = (float)Math.Cos(angle) * radius;
                y = (float)Math.Sin(angle) * radius;
            }
            else // Right cheek area (slightly indented)
            {
                float radius = 0.38f;
                x = (float)Math.Cos(angle) * radius;
                y = (float)Math.Sin(angle) * radius;
            }

            // Add proper triangular cat ears
            if (t > 0.85f || t < 0.15f) // Ear areas
            {
                // Calculate ear base position
                float earAngle = angle;
                float earRadius = 0.45f;

                // Ear base
                float baseX = (float)Math.Cos(earAngle) * earRadius;
                float baseY = (float)Math.Sin(earAngle) * earRadius;

                // Ear tip (pointed upward)
                float tipOffset = 0.12f;
                float earTipX = baseX + (float)Math.Cos(earAngle) * tipOffset * 0.5f;
                float earTipY = baseY - tipOffset;

                // Ear width
                float earWidth = tipOffset * 0.6f;
                float sideX = baseX + (float)Math.Cos(earAngle + Math.PI/2) * earWidth;

                // Interpolate along ear shape
                float earT = (t < 0.15f) ? (t / 0.15f) : ((t - 0.85f) / 0.15f);
                if (t < 0.15f) earT = 1 - earT; // Mirror for left ear

                // Create ear outline
                x = baseX + (earTipX - baseX) * earT;
                y = baseY + (earTipY - baseY) * earT;

                // Add slight curve to ear
                if (earT > 0.3f && earT < 0.7f)
                {
                    y += 0.02f * (float)Math.Sin(earT * Math.PI * 2);
                }

                // Add ear animation (twitching)
                y += 0.03f * (float)Math.Sin(_time * 4 + earAngle * 3);
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
        
        // Draw cat eyes with pupils
        float eyeSize = 6.0f;
        uint eyeColor = beat ? 0xFFFF6600 : 0xFFFFAA00; // Orange on beat, gold otherwise
        uint pupilColor = 0xFF000000; // Black pupils

        // Left eye (almond-shaped)
        canvas.FillCircle(_width * 0.42f, _height * 0.48f, eyeSize, eyeColor);
        canvas.FillCircle(_width * 0.42f, _height * 0.48f, eyeSize * 0.6f, pupilColor);

        // Right eye (almond-shaped)
        canvas.FillCircle(_width * 0.58f, _height * 0.48f, eyeSize, eyeColor);
        canvas.FillCircle(_width * 0.58f, _height * 0.48f, eyeSize * 0.6f, pupilColor);

        // Add eye shine effect
        uint shineColor = 0xFFFFFFFF;
        canvas.FillCircle(_width * 0.41f, _height * 0.47f, 1.5f, shineColor);
        canvas.FillCircle(_width * 0.57f, _height * 0.47f, 1.5f, shineColor);

        // Draw nose (inverted heart shape)
        canvas.FillCircle(_width * 0.5f, _height * 0.55f, 3.0f, 0xFFFF69B4);
        canvas.FillCircle(_width * 0.495f, _height * 0.56f, 1.5f, 0xFFFF69B4);
        canvas.FillCircle(_width * 0.505f, _height * 0.56f, 1.5f, 0xFFFF69B4);

        // Draw whiskers
        uint whiskerColor = 0xFFCCCCCC;
        float whiskerLength = 25.0f;

        // Left whiskers
        for (int i = 0; i < 3; i++)
        {
            float yOffset = (i - 1) * 8.0f;
            canvas.DrawLine(_width * 0.35f, _height * 0.5f + yOffset,
                          _width * 0.35f - whiskerLength, _height * 0.5f + yOffset, whiskerColor, 1.0f);
        }

        // Right whiskers
        for (int i = 0; i < 3; i++)
        {
            float yOffset = (i - 1) * 8.0f;
            canvas.DrawLine(_width * 0.65f, _height * 0.5f + yOffset,
                          _width * 0.65f + whiskerLength, _height * 0.5f + yOffset, whiskerColor, 1.0f);
        }

        // Draw mouth (simple curved line)
        float mouthY = _height * 0.58f;
        canvas.DrawLine(_width * 0.48f, mouthY, _width * 0.52f, mouthY, 0xFF333333, 2.0f);
    }

    public void Dispose()
    {
        // Nothing to clean up
    }
}
