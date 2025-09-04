using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Butterfly superscope visualization based on AVS superscope code
/// FIXED: Cleaned up and made fully beat-reactive with enhanced audio responsiveness
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
        
        // FIXED: Audio-reactive time and animation updates
        var energy = features.Energy;
        var bass = features.Bass;
        var mid = features.Mid;
        var treble = features.Treble;
        var beat = features.Beat;
        var volume = features.Volume;
        
        // Audio-reactive animation speed
        var baseSpeed = 0.02f;
        var energySpeed = energy * 0.03f;
        var trebleSpeed = treble * 0.02f;
        var beatSpeed = beat ? 0.05f : 0f;
        _time += baseSpeed + energySpeed + trebleSpeed + beatSpeed;
        
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

            // FIXED: Enhanced audio-reactive wing flapping
            if (segment < 4) // Wings only
            {
                // Base flapping rhythm
                float baseFlap = (float)Math.Sin(_time * 3 + segmentT * Math.PI * 2) * 0.08f;
                
                // Audio-reactive flapping intensity
                var bassFlap = bass * (float)Math.Sin(_time * 4 + segmentT * Math.PI) * 0.12f;
                var midFlap = mid * (float)Math.Sin(_time * 6 + segmentT * Math.PI * 1.5f) * 0.08f;
                var trebleFlap = treble * (float)Math.Sin(_time * 8 + segmentT * Math.PI * 2f) * 0.06f;
                var energyFlap = energy * (float)Math.Sin(_time * 5 + segmentT * Math.PI * 0.5f) * 0.10f;
                
                // Beat-triggered dramatic flapping
                var beatFlap = beat ? (float)Math.Sin(_time * 12 + segmentT * Math.PI * 3f) * 0.15f : 0f;
                
                // Combine all flapping effects
                y += baseFlap + bassFlap + midFlap + trebleFlap + energyFlap + beatFlap;

                // FIXED: Audio-reactive wing curvature and positioning
                if (segment == 0 || segment == 3) // Lower wings
                {
                    y -= bass * 0.04f; // Bass pushes lower wings down
                    x += mid * 0.02f; // Mid frequencies affect horizontal position
                }
                else // Upper wings
                {
                    y += treble * 0.04f; // Treble lifts upper wings
                    x += energy * 0.02f; // Energy affects horizontal position
                }
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

            // FIXED: Audio-reactive color generation
            uint color;
            if (segment < 4) // Wings
            {
                // Audio-reactive hue progression
                var bassHue = bass * 0.3f;
                var trebleHue = treble * 0.2f;
                var energyHue = energy * 0.25f;
                var beatHue = beat ? 0.1f : 0f;
                
                float hue = ((i / (float)points.Count) * 0.8f + _time * 0.2f + bassHue + trebleHue + energyHue + beatHue) % 1.0f;
                color = GetRainbowColor(hue);
                
                // Audio-reactive brightness
                var baseBrightness = 1.2f;
                var energyBrightness = energy * 0.4f;
                var beatBrightness = beat ? 0.3f : 0f;
                var totalBrightness = baseBrightness + energyBrightness + beatBrightness;
                
                color = AdjustBrightness(color, totalBrightness);
            }
            else // Body
            {
                // Audio-reactive body color
                if (beat)
                    color = 0xFFFFAA44; // Bright orange on beat
                else if (bass > 0.4f)
                    color = 0xFFFF8844; // Warm orange for bass
                else if (treble > 0.3f)
                    color = 0xFFFFCC44; // Bright yellow for treble
                else
                    color = 0xFFAA7744; // Default warm brown
            }

            canvas.DrawLine(points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, color, 2.0f);
        }

        // FIXED: Audio-reactive wing fills
        DrawWingFills(canvas, points, features);
        
        // FIXED: Audio-reactive antennae
        uint antennaColor;
        if (beat)
            antennaColor = 0xFFFFDD00; // Bright yellow on beat
        else if (treble > 0.4f)
            antennaColor = 0xFFFFFF00; // Bright yellow for treble
        else if (bass > 0.3f)
            antennaColor = 0xFFFFAA00; // Orange for bass
        else
            antennaColor = 0xFFFF8800; // Default warm orange
            
        // Audio-reactive antennae movement
        var antennaWave = (float)Math.Sin(_time * 4 + treble * 2) * treble * 10f;
        var antennaLength = 0.1f + energy * 0.05f;
        
        canvas.DrawLine(_width * 0.5f, _height * 0.3f, _width * (0.4f - antennaLength), _height * (0.2f + antennaWave), antennaColor, 2.0f);
        canvas.DrawLine(_width * 0.5f, _height * 0.3f, _width * (0.6f + antennaLength), _height * (0.2f + antennaWave), antennaColor, 2.0f);
    }

    private void DrawWingFills(ISkiaCanvas canvas, System.Collections.Generic.List<(float x, float y)> points, AudioFeatures features)
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

        // FIXED: Audio-reactive wing fill colors and transparency
        var energy = features.Energy;
        var bass = features.Bass;
        var treble = features.Treble;
        var beat = features.Beat;
        
        // Audio-reactive transparency
        var baseAlpha = 0x44;
        var energyAlpha = (byte)(energy * 0x33);
        var beatAlpha = beat ? (byte)0x22 : (byte)0x00;
        var totalAlpha = (byte)Math.Min(0xFF, baseAlpha + energyAlpha + beatAlpha);

        if (leftWingPoints.Count > 2)
        {
            // Audio-reactive left wing color
            uint fillColor;
            if (beat)
                fillColor = (uint)(totalAlpha << 24) | 0xFFFFAAAA; // Pink on beat
            else if (bass > 0.4f)
                fillColor = (uint)(totalAlpha << 24) | 0xFFFF8888; // Red-pink for bass
            else if (treble > 0.3f)
                fillColor = (uint)(totalAlpha << 24) | 0xFFFFCCCC; // Light pink for treble
            else
                fillColor = (uint)(totalAlpha << 24) | 0xFFFFAAAA; // Default pink
                
            DrawFilledShape(canvas, leftWingPoints, fillColor);
        }

        if (rightWingPoints.Count > 2)
        {
            // Audio-reactive right wing color
            uint fillColor;
            if (beat)
                fillColor = (uint)(totalAlpha << 24) | 0xFFAAAAFF; // Blue on beat
            else if (energy > 0.4f)
                fillColor = (uint)(totalAlpha << 24) | 0xFF8888FF; // Bright blue for energy
            else if (treble > 0.3f)
                fillColor = (uint)(totalAlpha << 24) | 0xFFCCCCFF; // Light blue for treble
            else
                fillColor = (uint)(totalAlpha << 24) | 0xFFAAAAFF; // Default blue
                
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

        private static uint HsvToRgb(float h, float s, float v)
        {
            h = (h % 360f + 360f) % 360f;
            float c = v * s;
            float x = c * (1 - MathF.Abs((h / 60f) % 2 - 1));
            float m = v - c;
            float r=0,g=0,b=0;
            if (h < 60)      { r=c; g=x; b=0; }
            else if (h <120) { r=x; g=c; b=0; }
            else if (h <180) { r=0; g=c; b=x; }
            else if (h <240) { r=0; g=x; b=c; }
            else if (h <300) { r=x; g=0; b=c; }
            else             { r=c; g=0; b=x; }
            byte R=(byte)((r+m)*255), G=(byte)((g+m)*255), B=(byte)((b+m)*255);
            return 0xFF000000u | ((uint)R<<16) | ((uint)G<<8) | (uint)B;
        }
    }
