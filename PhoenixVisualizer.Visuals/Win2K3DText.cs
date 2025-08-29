using System;
using System.Collections.Generic;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Classic Windows 2000 3D Text screensaver - faithfully recreated for Phoenix Visualizer
/// Features rotating 3D text with various effects and audio-reactive transformations
/// </summary>
public sealed class Win2K3DText : IVisualizerPlugin
{
    public string Id => "win2k_3d_text";
    public string DisplayName => "📝 Win2K 3D Text";

    private int _width, _height;
    private float _time;
    private Random _random = new();

    // Text and animation constants (based on original Win2K implementation)
    private const float MIN_DEPTH = 0.15f;
    private const float MAX_DEPTH = 0.6f;
    private const float MIN_VIEW_ANGLE = 90f;
    private const float MAX_VIEW_ANGLE = 130f;
    private const float MAX_ZOOM = 5.0f;
    private const int MIN_ROT_STEP = 1;
    private const int MAX_ROT_STEP = 20;

    // Text strings to rotate through
    private readonly string[] _textStrings = new[]
    {
        "PHOENIX VISUALIZER",
        "WINDOWS 2000",
        "3D TEXT",
        "CLASSIC SCREENSAVER",
        "AUDIO REACTIVE",
        "OPEN SOURCE",
        "RETRO STYLE",
        "DIGITAL ART"
    };

    // Animation state
    private int _currentTextIndex;
    private float _rotationX, _rotationY, _rotationZ;
    private float _zoomLevel;
    private float _depth;
    private float _viewAngle;
    private int _rotationStep;
    private float _cycleTime;
    private bool _useLighting;

    // Text geometry (simplified 3D text representation)
    private struct TextChar
    {
        public char Character;
        public List<(float x, float y, float z)> Vertices;
        public List<(int a, int b, int c)> Triangles;
        public uint Color;
    }

    private List<TextChar> _currentText;

    public Win2K3DText()
    {
        _currentText = new List<TextChar>();
    }

    // Colors inspired by the original
    private readonly uint[] _textColors = new uint[]
    {
        0xFFFF0000, // Red
        0xFF00FF00, // Green
        0xFF0000FF, // Blue
        0xFFFFFF00, // Yellow
        0xFFFF00FF, // Magenta
        0xFF00FFFF, // Cyan
        0xFF00FF88, // Light green
        0xFF8888FF, // Light blue
        0xFFFF8888, // Light red
        0xFFFFFF88  // Light yellow
    };

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;

        InitializeText();
        ResetAnimation();
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Dispose() { }

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        _time += 0.016f;
        _cycleTime += 0.016f;

        // Change text occasionally or on beat
        if (_cycleTime > 8f || (f.Beat && _random.NextDouble() < 0.3f))
        {
            ChangeText();
            ResetAnimation();
        }

        // Update animation based on audio
        UpdateAnimation(f);

        // Clear with gradient background
        RenderBackground(canvas, f);

        // Render the 3D text
        Render3DText(canvas, f);
    }

    private void InitializeText()
    {
        _currentText = new List<TextChar>();
        _currentTextIndex = 0;
        GenerateTextGeometry(_textStrings[0]);
    }

    private void GenerateTextGeometry(string text)
    {
        _currentText.Clear();
        float charSpacing = 1.2f;
        float startX = -text.Length * charSpacing * 0.5f;

        for (int i = 0; i < text.Length; i++)
        {
            var textChar = new TextChar
            {
                Character = text[i],
                Vertices = new List<(float x, float y, float z)>(),
                Triangles = new List<(int a, int b, int c)>(),
                Color = _textColors[_random.Next(_textColors.Length)]
            };

            // Generate simple 3D geometry for each character
            GenerateCharGeometry(textChar, startX + i * charSpacing, 0, 0);
            _currentText.Add(textChar);
        }
    }

    private void GenerateCharGeometry(TextChar textChar, float x, float y, float z)
    {
        // Create simple 3D block geometry for characters (simplified)
        float halfWidth = 0.4f;
        float halfHeight = 0.6f;
        float depth = _depth;

        // Front face vertices
        textChar.Vertices.Add((x - halfWidth, y - halfHeight, z + depth)); // 0
        textChar.Vertices.Add((x + halfWidth, y - halfHeight, z + depth)); // 1
        textChar.Vertices.Add((x + halfWidth, y + halfHeight, z + depth)); // 2
        textChar.Vertices.Add((x - halfWidth, y + halfHeight, z + depth)); // 3

        // Back face vertices
        textChar.Vertices.Add((x - halfWidth, y - halfHeight, z)); // 4
        textChar.Vertices.Add((x + halfWidth, y - halfHeight, z)); // 5
        textChar.Vertices.Add((x + halfWidth, y + halfHeight, z)); // 6
        textChar.Vertices.Add((x - halfWidth, y + halfHeight, z)); // 7

        // Front face
        textChar.Triangles.Add((0, 1, 2));
        textChar.Triangles.Add((0, 2, 3));

        // Back face
        textChar.Triangles.Add((4, 6, 5));
        textChar.Triangles.Add((4, 7, 6));

        // Sides
        textChar.Triangles.Add((0, 3, 7));
        textChar.Triangles.Add((0, 7, 4));
        textChar.Triangles.Add((1, 5, 6));
        textChar.Triangles.Add((1, 6, 2));
        textChar.Triangles.Add((3, 2, 6));
        textChar.Triangles.Add((3, 6, 7));
        textChar.Triangles.Add((0, 4, 5));
        textChar.Triangles.Add((0, 5, 1));
    }

    private void ChangeText()
    {
        _currentTextIndex = (_currentTextIndex + 1) % _textStrings.Length;
        GenerateTextGeometry(_textStrings[_currentTextIndex]);
        _cycleTime = 0;
    }

    private void ResetAnimation()
    {
        _rotationX = _random.Next(360);
        _rotationY = _random.Next(360);
        _rotationZ = _random.Next(360);
        _zoomLevel = 1f + (float)_random.NextDouble() * 2f;
        _depth = MIN_DEPTH + (float)_random.NextDouble() * (MAX_DEPTH - MIN_DEPTH);
        _viewAngle = MIN_VIEW_ANGLE + (float)_random.NextDouble() * (MAX_VIEW_ANGLE - MIN_VIEW_ANGLE);
        _rotationStep = MIN_ROT_STEP + _random.Next(MAX_ROT_STEP - MIN_ROT_STEP);
        _useLighting = _random.Next(2) == 0;
    }

    private void UpdateAnimation(AudioFeatures f)
    {
        // Audio-reactive rotation speeds
        float baseRotSpeed = _rotationStep * 0.01f;
        float audioMultiplier = 1f + f.Volume * 2f;

        _rotationX += baseRotSpeed * audioMultiplier * (1f + f.Bass);
        _rotationY += baseRotSpeed * audioMultiplier * (1f + f.Mid);
        _rotationZ += baseRotSpeed * audioMultiplier * (1f + f.Treble);

        // Audio-reactive zoom
        float targetZoom = 1f + f.Volume * 2f;
        _zoomLevel += (targetZoom - _zoomLevel) * 0.02f;

        // Audio-reactive depth
        if (f.Beat)
        {
            _depth = MIN_DEPTH + (float)_random.NextDouble() * (MAX_DEPTH - MIN_DEPTH);
        }

        // Keep rotations in reasonable range
        _rotationX %= 360;
        _rotationY %= 360;
        _rotationZ %= 360;
    }

    private void RenderBackground(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Create gradient background that reacts to audio
        uint topColor = 0xFF000033; // Dark blue
        uint bottomColor = 0xFF000011; // Very dark blue

        // Add audio reactivity
        float brightness = 0.5f + f.Volume * 0.5f;
        topColor = AdjustBrightness(topColor, brightness);
        bottomColor = AdjustBrightness(bottomColor, brightness);

        // Simple gradient fill
        for (int y = 0; y < _height; y++)
        {
            float t = (float)y / _height;
            uint color = InterpolateColor(topColor, bottomColor, t);
            canvas.DrawLine(0, y, _width, y, color, 1f);
        }
    }

    private void Render3DText(ISkiaCanvas canvas, AudioFeatures f)
    {
        float centerX = _width * 0.5f;
        float centerY = _height * 0.5f;

        // 3D perspective parameters
        float fov = _viewAngle * (float)(Math.PI / 180f);
        float near = 0.1f;
        float far = 50f;

        foreach (var textChar in _currentText)
        {
            RenderChar3D(canvas, textChar, centerX, centerY, fov, near, far, f);
        }

        // Add some particle effects around the text
        RenderParticleEffects(canvas, f);
    }

    private void RenderChar3D(ISkiaCanvas canvas, TextChar textChar, float centerX, float centerY,
                             float fov, float near, float far, AudioFeatures f)
    {
        // Audio-reactive color
        uint color = textChar.Color;
        if (_useLighting)
        {
            float lightingFactor = 0.5f + f.Volume * 0.5f;
            color = AdjustBrightness(color, lightingFactor);
        }

        foreach (var triangle in textChar.Triangles)
        {
            var v1 = textChar.Vertices[triangle.a];
            var v2 = textChar.Vertices[triangle.b];
            var v3 = textChar.Vertices[triangle.c];

            // Transform vertices
            var tv1 = TransformVertex(v1.x, v1.y, v1.z);
            var tv2 = TransformVertex(v2.x, v2.y, v2.z);
            var tv3 = TransformVertex(v3.x, v3.y, v3.z);

            // Project to screen coordinates
            var p1 = Project3D(tv1.x, tv1.y, tv1.z, centerX, centerY, fov, near, far);
            var p2 = Project3D(tv2.x, tv2.y, tv2.z, centerX, centerY, fov, near, far);
            var p3 = Project3D(tv3.x, tv3.y, tv3.z, centerX, centerY, fov, near, far);

            // Only render if all points are visible
            if (p1.z > near && p2.z > near && p3.z > near &&
                p1.z < far && p2.z < far && p3.z < far)
            {
                // Distance-based alpha
                float avgZ = (p1.z + p2.z + p3.z) / 3f;
                float alpha = Math.Max(0.3f, 1f - avgZ / far);
                uint fadedColor = (uint)((uint)(alpha * 255) << 24 | (color & 0x00FFFFFF));

                // Draw triangle edges
                canvas.DrawLine(p1.x, p1.y, p2.x, p2.y, fadedColor, 2f);
                canvas.DrawLine(p2.x, p2.y, p3.x, p3.y, fadedColor, 2f);
                canvas.DrawLine(p3.x, p3.y, p1.x, p1.y, fadedColor, 2f);
            }
        }
    }

    private void RenderParticleEffects(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Add particle effects that react to audio
        int particleCount = (int)(10 + f.Volume * 30);

        for (int i = 0; i < particleCount; i++)
        {
            float angle = _time * 2f + i * 0.5f;
            float radius = 100f + (float)Math.Sin(_time + i * 0.1f) * 50f;
            float x = _width * 0.5f + (float)Math.Cos(angle) * radius;
            float y = _height * 0.5f + (float)Math.Sin(angle) * radius;

            float alpha = (float)_random.NextDouble() * 0.6f;
            uint particleColor = _textColors[i % _textColors.Length];
            particleColor = (uint)((uint)(alpha * 255) << 24 | (particleColor & 0x00FFFFFF));

            canvas.FillCircle(x, y, 2f, particleColor);
        }
    }

    private (float x, float y, float z) TransformVertex(float x, float y, float z)
    {
        // Apply rotations (simplified rotation matrices)
        float cosX = (float)Math.Cos(_rotationX * Math.PI / 180f);
        float sinX = (float)Math.Sin(_rotationX * Math.PI / 180f);
        float cosY = (float)Math.Cos(_rotationY * Math.PI / 180f);
        float sinY = (float)Math.Sin(_rotationY * Math.PI / 180f);
        float cosZ = (float)Math.Cos(_rotationZ * Math.PI / 180f);
        float sinZ = (float)Math.Sin(_rotationZ * Math.PI / 180f);

        // Rotate around Z axis
        float x1 = x * cosZ - y * sinZ;
        float y1 = x * sinZ + y * cosZ;
        float z1 = z;

        // Rotate around Y axis
        float x2 = x1 * cosY + z1 * sinY;
        float z2 = -x1 * sinY + z1 * cosY;
        float y2 = y1;

        // Rotate around X axis
        float y3 = y2 * cosX - z2 * sinX;
        float z3 = y2 * sinX + z2 * cosX;
        float x3 = x2;

        // Apply zoom
        x3 *= _zoomLevel;
        y3 *= _zoomLevel;
        z3 *= _zoomLevel;

        return (x3, y3, z3);
    }

    private (float x, float y, float z) Project3D(float worldX, float worldY, float worldZ,
                                                 float centerX, float centerY, float fov, float near, float far)
    {
        // Perspective projection
        if (worldZ <= near) worldZ = near + 0.1f;

        float screenX = centerX + (worldX / worldZ) * (centerX / (float)Math.Tan(fov * 0.5));
        float screenY = centerY + (worldY / worldZ) * (centerY / (float)Math.Tan(fov * 0.5));

        return (screenX, screenY, worldZ);
    }

    private uint AdjustBrightness(uint color, float factor)
    {
        byte r = (byte)((color >> 16) & 0xFF);
        byte g = (byte)((color >> 8) & 0xFF);
        byte b = (byte)(color & 0xFF);

        r = (byte)Math.Min(255, r * factor);
        g = (byte)Math.Min(255, g * factor);
        b = (byte)Math.Min(255, b * factor);

        return (uint)(0xFF000000 | r << 16 | g << 8 | b);
    }

    private uint InterpolateColor(uint color1, uint color2, float t)
    {
        byte r1 = (byte)((color1 >> 16) & 0xFF);
        byte g1 = (byte)((color1 >> 8) & 0xFF);
        byte b1 = (byte)(color1 & 0xFF);

        byte r2 = (byte)((color2 >> 16) & 0xFF);
        byte g2 = (byte)((color2 >> 8) & 0xFF);
        byte b2 = (byte)(color2 & 0xFF);

        byte r = (byte)(r1 + (r2 - r1) * t);
        byte g = (byte)(g1 + (g2 - g1) * t);
        byte b = (byte)(b1 + (b2 - b1) * t);

        return (uint)(0xFF000000 | r << 16 | g << 8 | b);
    }
}
