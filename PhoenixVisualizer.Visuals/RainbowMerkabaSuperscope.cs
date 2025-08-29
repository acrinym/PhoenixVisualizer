using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Rainbow Merkaba superscope visualization based on complex AVS superscope code
/// </summary>
public sealed class RainbowMerkabaSuperscope : IVisualizerPlugin
{
    public string Id => "rainbow_merkaba_superscope";
    public string DisplayName => "Rainbow Merkaba";

    private int _width;
    private int _height;
    private float _rotation;
    private int _numPoints = 720;

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _rotation = 0;
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
        
        // Update rotation
        _rotation += 0.02f;
        
        // Get audio data
        float volume = features.Volume;
        bool beat = features.Beat;
        
        // Handle beat events
        if (beat)
        {
            _rotation += 0.2f;
        }
        
        // Create points array for the merkaba
        var points = new System.Collections.Generic.List<(float x, float y)>();
        
        for (int i = 0; i < _numPoints; i++)
        {
            float t = i / (float)_numPoints;
            
            // Merkaba edge calculation from AVS
            int edge = (int)(t * 12);
            float edgeT = t * 12 - edge;
            
            float x1, y1, z1, x2, y2, z2;
            
            // Define the 12 edges of the merkaba
            switch (edge)
            {
                case 0: x1 = 1; y1 = 1; z1 = 1; x2 = -1; y2 = -1; z2 = 1; break;
                case 1: x1 = 1; y1 = 1; z1 = 1; x2 = -1; y2 = 1; z2 = -1; break;
                case 2: x1 = 1; y1 = 1; z1 = 1; x2 = 1; y2 = -1; z2 = -1; break;
                case 3: x1 = -1; y1 = -1; z1 = 1; x2 = -1; y2 = 1; z2 = -1; break;
                case 4: x1 = -1; y1 = -1; z1 = 1; x2 = 1; y2 = -1; z2 = -1; break;
                case 5: x1 = -1; y1 = 1; z1 = -1; x2 = 1; y2 = -1; z2 = -1; break;
                case 6: x1 = 1; y1 = 1; z1 = -1; x2 = -1; y2 = -1; z2 = -1; break;
                case 7: x1 = 1; y1 = 1; z1 = -1; x2 = -1; y2 = 1; z2 = 1; break;
                case 8: x1 = 1; y1 = 1; z1 = -1; x2 = 1; y2 = -1; z2 = 1; break;
                case 9: x1 = -1; y1 = -1; z1 = -1; x2 = -1; y2 = 1; z2 = 1; break;
                case 10: x1 = -1; y1 = -1; z1 = -1; x2 = 1; y2 = -1; z2 = 1; break;
                default: x1 = -1; y1 = 1; z1 = 1; x2 = 1; y2 = -1; z2 = 1; break;
            }
            
            // Interpolate along the edge
            float x = (x2 - x1) * edgeT + x1;
            float y = (y2 - y1) * edgeT + y1;
            float z = (z2 - z1) * edgeT + z1;
            
            // Apply 3D rotations
            float cz = (float)Math.Cos(_rotation * 0.6f);
            float sz = (float)Math.Sin(_rotation * 0.6f);
            float cy = (float)Math.Cos(_rotation * 0.3f);
            float sy = (float)Math.Sin(_rotation * 0.3f);
            float cx = (float)Math.Cos(_rotation);
            float sx = (float)Math.Sin(_rotation);
            
            // Rotate around Z
            float x1_rot = x * cz - y * sz;
            float y1_rot = x * sz + y * cz;
            float z1_rot = z;
            
            // Rotate around Y
            float x2_rot = x1_rot * cy + z1_rot * sy;
            float z2_rot = -x1_rot * sy + z1_rot * cy;
            float y2_rot = y1_rot;
            
            // Rotate around X
            float y3_rot = y2_rot * cx - z2_rot * sx;
            float z3_rot = y2_rot * sx + z2_rot * cx;
            float x3_rot = x2_rot;
            
            // Apply perspective projection
            float pers = 2.0f / (2.0f + z3_rot);
            float finalX = x3_rot * pers;
            float finalY = y3_rot * pers;
            
            // Scale and center
            finalX = finalX * _width * 0.3f + _width * 0.5f;
            finalY = finalY * _height * 0.3f + _height * 0.5f;
            
            points.Add((finalX, finalY));
        }
        
        // Draw the merkaba with rainbow colors
        canvas.SetLineWidth(1.0f);
        
        // Draw each edge with proper rainbow colors
        for (int i = 0; i < points.Count - 1; i++)
        {
            // Create rainbow spectrum based on position along the merkaba
            float baseHue = (float)i / points.Count; // 0 to 1 rainbow progression

            // Make rainbow dynamic with audio and time
            float dynamicHue = baseHue + _rotation * 0.1f + volume * 0.3f;
            dynamicHue = dynamicHue % 1.0f; // Keep in 0-1 range

            // Adjust brightness and saturation based on audio
            float saturation = 0.8f + volume * 0.2f;
            float brightness = 0.7f + features.Bass * 0.3f;

            uint color = HsvToRgb(dynamicHue, saturation, brightness);

            // Thicker lines on beat
            float lineWidth = beat ? 2.5f : 1.0f;

            canvas.DrawLine(points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, color, lineWidth);
        }
    }

    public void Dispose()
    {
        // Nothing to clean up
    }

    // HSV to RGB conversion for proper rainbow colors
    private uint HsvToRgb(float h, float s, float v)
    {
        float c = v * s;
        float x = c * (1 - MathF.Abs((h * 6) % 2 - 1));
        float m = v - c;

        float r, g, b;
        if (h < 1f/6f) { r = c; g = x; b = 0; }
        else if (h < 2f/6f) { r = x; g = c; b = 0; }
        else if (h < 3f/6f) { r = 0; g = c; b = x; }
        else if (h < 4f/6f) { r = 0; g = x; b = c; }
        else if (h < 5f/6f) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        byte R = (byte)((r + m) * 255);
        byte G = (byte)((g + m) * 255);
        byte B = (byte)((b + m) * 255);

        return 0xFF000000u | ((uint)R << 16) | ((uint)G << 8) | (uint)B;
    }
}
