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

            // Butterfly formula from AVS: s=floor(i*5); p=i*5-s; s==0?(x=-0.35*cos(p*$PI); y=0.2*sin(p*$PI)):s==1?(x=-0.2*cos(p*$PI); y=0.4*sin(p*$PI)):s==2?(x=0.2*cos(p*$PI); y=0.4*sin(p*$PI)):s==3?(x=0.35*cos(p*$PI); y=0.2*sin(p*$PI)):(x=0; y=0.25-0.5*p);
            int segment = (int)(t * 5);
            float segmentT = t * 5 - segment;

            float x, y;

            switch (segment)
            {
                case 0: // Left wing tip
                    x = -0.35f * (float)Math.Cos(segmentT * (float)Math.PI);
                    y = 0.2f * (float)Math.Sin(segmentT * (float)Math.PI);
                    break;
                case 1: // Left wing
                    x = -0.2f * (float)Math.Cos(segmentT * (float)Math.PI);
                    y = 0.4f * (float)Math.Sin(segmentT * (float)Math.PI);
                    break;
                case 2: // Right wing
                    x = 0.2f * (float)Math.Cos(segmentT * (float)Math.PI);
                    y = 0.4f * (float)Math.Sin(segmentT * (float)Math.PI);
                    break;
                case 3: // Right wing tip
                    x = 0.35f * (float)Math.Cos(segmentT * (float)Math.PI);
                    y = 0.2f * (float)Math.Sin(segmentT * (float)Math.PI);
                    break;
                default: // Body
                    x = 0;
                    y = 0.25f - 0.5f * segmentT;
                    break;
            }

            // Add wing flapping animation
            if (segment < 4) // Wings only
            {
                float flap = (float)Math.Sin(_time * 2) * 0.1f;
                y += flap;
            }

            // Convert from AVS coordinate system (-1 to 1) to screen coordinates
            x = (x + 1.0f) * _width * 0.5f;
            y = (y + 1.0f) * _height * 0.5f;

            // Clamp to screen bounds to prevent drawing outside
            x = Math.Max(0, Math.Min(_width - 1, x));
            y = Math.Max(0, Math.Min(_height - 1, y));

            points.Add((x, y));
        }
        
        // Draw the butterfly with rainbow colors
        canvas.SetLineWidth(1.0f);
        
        // Draw each segment with rainbow colors
        for (int i = 0; i < points.Count - 1; i++)
        {
            // Simple rainbow based on position
            float hue = (i / (float)points.Count + _time * 0.1f) % 1.0f;
            uint color = GetRainbowColor(hue);
            canvas.DrawLine(points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, color, 1.0f);
        }

        // Close the shape for better continuity
        if (points.Count > 2)
        {
            float hue = ((points.Count - 1) / (float)points.Count + _time * 0.1f) % 1.0f;
            uint color = GetRainbowColor(hue);
            canvas.DrawLine(points[^1].x, points[^1].y, points[0].x, points[0].y, color, 1.0f);
        }
        
        // Draw antennae
        // Avoid green; use warm phoenix tones
        uint antennaColor = beat ? 0xFFFFDD00 : 0xFFFF8800;
        canvas.DrawLine(_width * 0.5f, _height * 0.3f, _width * 0.4f, _height * 0.2f, antennaColor, 2.0f);
        canvas.DrawLine(_width * 0.5f, _height * 0.3f, _width * 0.6f, _height * 0.2f, antennaColor, 2.0f);
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
