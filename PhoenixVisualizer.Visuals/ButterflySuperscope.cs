using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Butterfly superscope visualization based on AVS superscope code
/// </summary>
public sealed class ButterflySuperscope : IVisualizerPlugin
{
    public string Id => "butterfly_superscope";
    public string DisplayName => "Butterfly";

    private int _width;
    private int _height;
    private float _time;
    private int _numPoints = 300;

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
        
        // Create points array for the butterfly
        var points = new System.Collections.Generic.List<(float x, float y)>();
        
        for (int i = 0; i < _numPoints; i++)
        {
            float t = i / (float)_numPoints;

            // Improved butterfly formula - more defined wings
            int segment = (int)(t * 6); // 6 segments for better definition
            float segmentT = t * 6 - segment;

            float x, y;

            switch (segment)
            {
                case 0: // Left wing lower
                    x = -0.4f * (float)Math.Sin(segmentT * (float)Math.PI * 2);
                    y = -0.1f + 0.3f * (float)Math.Cos(segmentT * (float)Math.PI);
                    break;
                case 1: // Left wing upper
                    x = -0.25f * (float)Math.Sin(segmentT * (float)Math.PI * 2);
                    y = 0.1f + 0.4f * (float)Math.Cos(segmentT * (float)Math.PI);
                    break;
                case 2: // Right wing upper
                    x = 0.25f * (float)Math.Sin(segmentT * (float)Math.PI * 2);
                    y = 0.1f + 0.4f * (float)Math.Cos(segmentT * (float)Math.PI);
                    break;
                case 3: // Right wing lower
                    x = 0.4f * (float)Math.Sin(segmentT * (float)Math.PI * 2);
                    y = -0.1f + 0.3f * (float)Math.Cos(segmentT * (float)Math.PI);
                    break;
                case 4: // Body left
                    x = -0.05f * (1 - segmentT);
                    y = 0.2f - 0.6f * segmentT;
                    break;
                default: // Body right
                    x = 0.05f * segmentT;
                    y = 0.2f - 0.6f * (1 - segmentT);
                    break;
            }

            // Add wing flapping animation with audio reactivity
            if (segment < 4) // Wings only
            {
                float baseFlap = (float)Math.Sin(_time * 3 + segmentT * Math.PI * 2) * 0.08f;
                float audioFlap = volume * (float)Math.Sin(_time * 6) * 0.05f;
                float beatFlap = beat ? (float)Math.Sin(_time * 10) * 0.03f : 0;

                y += baseFlap + audioFlap + beatFlap;

                // Add slight wing curvature based on audio
                if (segment == 0 || segment == 3) // Lower wings
                    y -= volume * 0.02f;
                else // Upper wings
                    y += volume * 0.02f;
            }

            // Convert from AVS coordinate system (-1 to 1) to screen coordinates
            x = (x + 1.0f) * _width * 0.5f;
            y = (y + 1.0f) * _height * 0.5f;

            // Clamp to screen bounds to prevent drawing outside
            x = Math.Max(0, Math.Min(_width - 1, x));
            y = Math.Max(0, Math.Min(_height - 1, y));

            points.Add((x, y));
        }
        
        // Draw the butterfly with enhanced visuals
        canvas.SetLineWidth(2.0f);

        // Draw wing outlines with gradient colors
        for (int i = 0; i < points.Count - 1; i++)
        {
            int segment = (int)((i / (float)points.Count) * 6);

            // Different colors for different parts
            uint color;
            if (segment < 4) // Wings
            {
                float hue = ((i / (float)points.Count) * 0.8f + _time * 0.2f) % 1.0f;
                color = GetRainbowColor(hue);
                // Make wings brighter and more vibrant
                color = AdjustBrightness(color, 1.2f);
            }
            else // Body
            {
                // Body is more subdued - warm brown/tan
                color = beat ? 0xFFFFAA44 : 0xFFAA7744;
            }

            canvas.DrawLine(points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, color, 2.0f);
        }

        // Draw wing fills for more solid appearance
        DrawWingFills(canvas, points, volume, beat);
        
        // Draw antennae
        // Avoid green; use warm phoenix tones
        uint antennaColor = beat ? 0xFFFFDD00 : 0xFFFF8800;
        canvas.DrawLine(_width * 0.5f, _height * 0.3f, _width * 0.4f, _height * 0.2f, antennaColor, 2.0f);
        canvas.DrawLine(_width * 0.5f, _height * 0.3f, _width * 0.6f, _height * 0.2f, antennaColor, 2.0f);
    }

    private void DrawWingFills(ISkiaCanvas canvas, System.Collections.Generic.List<(float x, float y)> points, float volume, bool beat)
    {
        // Find wing segments and draw simple fills
        var leftWingPoints = new System.Collections.Generic.List<(float x, float y)>();
        var rightWingPoints = new System.Collections.Generic.List<(float x, float y)>();

        for (int i = 0; i < points.Count; i++)
        {
            int segment = (int)((i / (float)points.Count) * 6);
            if (segment == 0 || segment == 1) // Left wing
                leftWingPoints.Add(points[i]);
            else if (segment == 2 || segment == 3) // Right wing
                rightWingPoints.Add(points[i]);
        }

        // Draw translucent wing fills
        if (leftWingPoints.Count > 2)
        {
            uint fillColor = 0x44FFAAAA; // Light pink with transparency
            DrawFilledShape(canvas, leftWingPoints, fillColor);
        }

        if (rightWingPoints.Count > 2)
        {
            uint fillColor = 0x44AAAAFF; // Light blue with transparency
            DrawFilledShape(canvas, rightWingPoints, fillColor);
        }
    }

    private void DrawFilledShape(ISkiaCanvas canvas, System.Collections.Generic.List<(float x, float y)> shapePoints, uint color)
    {
        if (shapePoints.Count < 3) return;

        // Simple shape fill using horizontal lines
        // Find min and max Y bounds
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (var point in shapePoints)
        {
            minY = Math.Min(minY, point.y);
            maxY = Math.Max(maxY, point.y);
        }

        // For each horizontal line, find intersections with shape edges
        for (float y = minY; y <= maxY; y += 1f)
        {
            var intersections = new System.Collections.Generic.List<float>();

            // Find intersections with all edges
            for (int i = 0; i < shapePoints.Count; i++)
            {
                var p1 = shapePoints[i];
                var p2 = shapePoints[(i + 1) % shapePoints.Count];

                if ((p1.y <= y && p2.y > y) || (p2.y <= y && p1.y > y))
                {
                    if (p1.y != p2.y)
                    {
                        float t = (y - p1.y) / (p2.y - p1.y);
                        float x = p1.x + t * (p2.x - p1.x);
                        intersections.Add(x);
                    }
                }
            }

            // Sort intersections and draw lines between pairs
            intersections.Sort();
            for (int i = 0; i < intersections.Count - 1; i += 2)
            {
                if (i + 1 < intersections.Count)
                {
                    canvas.DrawLine(intersections[i], y, intersections[i + 1], y, color, 1f);
                }
            }
        }
    }

    private uint AdjustBrightness(uint color, float factor)
    {
        byte r = (byte)((color >> 16) & 0xFF);
        byte g = (byte)((color >> 8) & 0xFF);
        byte b = (byte)(color & 0xFF);

        r = (byte)Math.Min(255, r * factor);
        g = (byte)Math.Min(255, g * factor);
        b = (byte)Math.Min(255, b * factor);

        return (uint)(0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | (uint)b);
    }

    private uint GetRainbowColor(float hue)
    {
        // Convert HSV to RGB (simplified rainbow)
        float h = hue * 6.0f;
        int sector = (int)h;
        float fractional = h - sector;

        float r = 0, g = 0, b = 0;

        switch (sector)
        {
            case 0: r = 1; g = fractional; b = 0; break;        // Red to Yellow
            case 1: r = 1 - fractional; g = 1; b = 0; break;   // Yellow to Green
            case 2: r = 0; g = 1; b = fractional; break;       // Green to Cyan
            case 3: r = 0; g = 1 - fractional; b = 1; break;   // Cyan to Blue
            case 4: r = fractional; g = 0; b = 1; break;       // Blue to Magenta
            case 5: r = 1; g = 0; b = 1 - fractional; break;   // Magenta to Red
        }

        uint red = (uint)(r * 255);
        uint green = (uint)(g * 255);
        uint blue = (uint)(b * 255);

        return (uint)((0xFF << 24) | (red << 16) | (green << 8) | blue);
    }

    public void Dispose()
    {
        // Nothing to clean up
    }
}
