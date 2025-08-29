using System;
using System.Collections.Generic;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Classic Windows 95 Mystify screensaver - Bouncing geometric shapes with colorful trails
/// Features lines and polygons that bounce around the screen leaving mesmerizing patterns
/// </summary>
public sealed class Win95Mystify : IVisualizerPlugin
{
    public string Id => "win95_mystify";
    public string DisplayName => "🎭 Win95 Mystify";

    private int _width, _height;
    private float _time;
    private Random _random = new();

    // Mystify system constants (based on original Win95 implementation)
    private const int MAX_SHAPES = 6;
    private const int MAX_VERTICES = 10; // Increased to accommodate Star shape (10 vertices)
    private const int TRAIL_LENGTH = 20;
    private const float BASE_SPEED = 2.0f;
    private const float MAX_SPEED = 8.0f;

    // Individual bouncing shape
    private struct MystifyShape
    {
        public ShapeType Type;
        public float X, Y;
        public float VelX, VelY;
        public float[] Vertices; // Relative to center
        public uint Color;
        public float Scale;
        public float Rotation;
        public float RotSpeed;

        // Trail data
        public List<(float x, float y, float alpha)> Trail;
    }

    // Shape types from the original Mystify
    private enum ShapeType
    {
        Line = 0,
        Triangle = 1,
        Square = 2,
        Pentagon = 3,
        Hexagon = 4,
        Star = 5
    }

    // Active shapes
    private List<MystifyShape> _shapes = new();

    // Colors inspired by Windows 95 Mystify
    private readonly uint[] _mystifyColors = new uint[]
    {
        0xFFFF0000, // Red
        0xFF00FF00, // Green
        0xFF0000FF, // Blue
        0xFFFFFF00, // Yellow
        0xFFFF00FF, // Magenta
        0xFF00FFFF, // Cyan
        0xFF808080, // Gray
        0xFFFFFF80, // Light yellow
        0xFF80FFFF, // Light cyan
        0xFFFF80FF, // Light magenta
        0xFF80FF80, // Light green
        0xFF8080FF  // Light blue
    };

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;

        // Initialize with several bouncing shapes
        for (int i = 0; i < MAX_SHAPES; i++)
        {
            CreateMystifyShape();
        }
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Dispose()
    {
        _shapes.Clear();
    }

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        _time += 0.016f;

        // Clear with black background
        canvas.Clear(0xFF000000);

        // Update all shapes
        UpdateShapes(f);

        // Render all shapes with trails
        RenderShapes(canvas, f);

        // Occasionally add new shapes
        if (_random.NextDouble() < 0.005f && _shapes.Count < MAX_SHAPES)
        {
            CreateMystifyShape();
        }

        // Remove shapes occasionally for variety
        if (_random.NextDouble() < 0.001f && _shapes.Count > 2)
        {
            _shapes.RemoveAt(0);
        }
    }

    private void CreateMystifyShape()
    {
        var shape = new MystifyShape
        {
            Type = (ShapeType)_random.Next(Enum.GetValues(typeof(ShapeType)).Length),
            X = _random.Next(_width),
            Y = _random.Next(_height),
            VelX = (float)(_random.NextDouble() * BASE_SPEED * 2 - BASE_SPEED),
            VelY = (float)(_random.NextDouble() * BASE_SPEED * 2 - BASE_SPEED),
            Vertices = new float[MAX_VERTICES * 2], // x,y pairs
            Color = _mystifyColors[_random.Next(_mystifyColors.Length)],
            Scale = 20f + (float)_random.NextDouble() * 40f,
            Rotation = (float)(_random.NextDouble() * Math.PI * 2),
            RotSpeed = (float)(_random.NextDouble() * 0.1f - 0.05f),
            Trail = new List<(float x, float y, float alpha)>()
        };

        // Generate shape geometry
        GenerateShapeGeometry(shape);

        _shapes.Add(shape);
    }

    private void GenerateShapeGeometry(MystifyShape shape)
    {
        int vertexCount = GetVertexCount(shape.Type);

        // Ensure vertices array is large enough
        if (shape.Vertices == null || shape.Vertices.Length < vertexCount * 2)
        {
            shape.Vertices = new float[vertexCount * 2];
        }

        float angleStep = (float)(Math.PI * 2 / vertexCount);

        for (int i = 0; i < vertexCount; i++)
        {
            float angle = i * angleStep + shape.Rotation;
            float radius = shape.Scale;

            // Add some variation for organic feel
            if (shape.Type != ShapeType.Line)
            {
                radius *= (0.8f + (float)Math.Sin(angle * 3 + _time) * 0.2f);
            }

            shape.Vertices[i * 2] = (float)Math.Cos(angle) * radius;     // x
            shape.Vertices[i * 2 + 1] = (float)Math.Sin(angle) * radius; // y
        }
    }

    private int GetVertexCount(ShapeType type)
    {
        return type switch
        {
            ShapeType.Line => 2,
            ShapeType.Triangle => 3,
            ShapeType.Square => 4,
            ShapeType.Pentagon => 5,
            ShapeType.Hexagon => 6,
            ShapeType.Star => 10, // Star has 10 points (5 outer + 5 inner)
            _ => 3
        };
    }

    private void UpdateShapes(AudioFeatures f)
    {
        for (int i = 0; i < _shapes.Count; i++)
        {
            var shape = _shapes[i];

            // Update position
            shape.X += shape.VelX * (1f + f.Volume * 0.5f);
            shape.Y += shape.VelY * (1f + f.Volume * 0.5f);

            // Update rotation
            shape.Rotation += shape.RotSpeed * (1f + f.Mid * 2f);

            // Bounce off walls
            if (shape.X <= 0 || shape.X >= _width)
            {
                shape.VelX = -shape.VelX;
                shape.X = Math.Max(0, Math.Min(_width, shape.X));

                // Add some randomness to bounce
                shape.VelY += (float)(_random.NextDouble() * 0.5f - 0.25f);
                shape.RotSpeed += (float)(_random.NextDouble() * 0.02f - 0.01f);
            }

            if (shape.Y <= 0 || shape.Y >= _height)
            {
                shape.VelY = -shape.VelY;
                shape.Y = Math.Max(0, Math.Min(_height, shape.Y));

                // Add some randomness to bounce
                shape.VelX += (float)(_random.NextDouble() * 0.5f - 0.25f);
                shape.RotSpeed += (float)(_random.NextDouble() * 0.02f - 0.01f);
            }

            // Keep velocities reasonable
            shape.VelX = Math.Max(-MAX_SPEED, Math.Min(MAX_SPEED, shape.VelX));
            shape.VelY = Math.Max(-MAX_SPEED, Math.Min(MAX_SPEED, shape.VelY));

            // Audio-reactive speed changes
            float speedMultiplier = 1f + f.Bass * 1.5f;
            shape.VelX *= speedMultiplier;
            shape.VelY *= speedMultiplier;

            // Update scale based on treble
            shape.Scale = (20f + (float)_random.NextDouble() * 40f) * (1f + f.Treble * 0.5f);

            // Regenerate geometry for non-static shapes
            if (shape.Type != ShapeType.Line)
            {
                GenerateShapeGeometry(shape);
            }

            // Update trail
            UpdateTrail(shape, f);

            // Audio-reactive color changes
            if (f.Beat && _random.NextDouble() < 0.3f)
            {
                shape.Color = _mystifyColors[_random.Next(_mystifyColors.Length)];
            }

            _shapes[i] = shape;
        }
    }

    private void UpdateTrail(MystifyShape shape, AudioFeatures f)
    {
        // Add current position to trail
        shape.Trail.Add((shape.X, shape.Y, 1.0f));

        // Limit trail length
        while (shape.Trail.Count > TRAIL_LENGTH)
        {
            shape.Trail.RemoveAt(0);
        }

        // Fade trail based on audio
        float fadeSpeed = 0.05f + f.Volume * 0.1f;
        for (int i = 0; i < shape.Trail.Count; i++)
        {
            var trailPoint = shape.Trail[i];
            trailPoint.alpha -= fadeSpeed;
            trailPoint.alpha = Math.Max(0, trailPoint.alpha);
            shape.Trail[i] = trailPoint;
        }

        // Remove faded points
        shape.Trail.RemoveAll(p => p.alpha <= 0);
    }

    private void RenderShapes(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Render trails first (behind shapes)
        foreach (var shape in _shapes)
        {
            RenderTrail(canvas, shape, f);
        }

        // Render shapes on top
        foreach (var shape in _shapes)
        {
            RenderShape(canvas, shape, f);
        }
    }

    private void RenderTrail(ISkiaCanvas canvas, MystifyShape shape, AudioFeatures f)
    {
        if (shape.Trail.Count < 2) return;

        // Audio-reactive trail color
        uint trailColor = AdjustBrightness(shape.Color, 0.5f);

        for (int i = 1; i < shape.Trail.Count; i++)
        {
            var p1 = shape.Trail[i - 1];
            var p2 = shape.Trail[i];

            // Combine trail alpha with distance-based fading
            float combinedAlpha = p1.alpha * p2.alpha;
            uint fadedColor = (uint)((uint)(combinedAlpha * 255) << 24 | (trailColor & 0x00FFFFFF));

            float thickness = 1f + combinedAlpha * 3f;
            canvas.DrawLine(p1.x, p1.y, p2.x, p2.y, fadedColor, thickness);
        }
    }

    private void RenderShape(ISkiaCanvas canvas, MystifyShape shape, AudioFeatures f)
    {
        int vertexCount = GetVertexCount(shape.Type);
        if (vertexCount < 2) return;

        // Audio-reactive brightness
        uint color = AdjustBrightness(shape.Color, 0.8f + f.Volume * 0.4f);

        // Render shape outline
        for (int i = 0; i < vertexCount; i++)
        {
            int nextIndex = (i + 1) % vertexCount;

            float x1 = shape.X + shape.Vertices[i * 2];
            float y1 = shape.Y + shape.Vertices[i * 2 + 1];
            float x2 = shape.X + shape.Vertices[nextIndex * 2];
            float y2 = shape.Y + shape.Vertices[nextIndex * 2 + 1];

            float thickness = 2f + f.Bass * 3f;
            canvas.DrawLine(x1, y1, x2, y2, color, thickness);
        }

        // For filled shapes, add some interior lines for extra mystify effect
        if (vertexCount > 3)
        {
            RenderInteriorLines(canvas, shape, f);
        }

        // Add glow effect on beat
        if (f.Beat)
        {
            RenderGlowEffect(canvas, shape, f);
        }
    }

    private void RenderInteriorLines(ISkiaCanvas canvas, MystifyShape shape, AudioFeatures f)
    {
        int vertexCount = GetVertexCount(shape.Type);

        // Draw lines from center to vertices (mystify spider web effect)
        for (int i = 0; i < vertexCount; i++)
        {
            float x1 = shape.X;
            float y1 = shape.Y;
            float x2 = shape.X + shape.Vertices[i * 2];
            float y2 = shape.Y + shape.Vertices[i * 2 + 1];

            uint color = AdjustBrightness(shape.Color, 0.3f + f.Mid * 0.4f);
            float thickness = 1f + f.Mid * 2f;
            canvas.DrawLine(x1, y1, x2, y2, color, thickness);
        }

        // Draw diagonal lines for complex shapes
        if (vertexCount >= 4)
        {
            for (int i = 0; i < vertexCount; i++)
            {
                int skipIndex = (i + 2) % vertexCount;

                float x1 = shape.X + shape.Vertices[i * 2];
                float y1 = shape.Y + shape.Vertices[i * 2 + 1];
                float x2 = shape.X + shape.Vertices[skipIndex * 2];
                float y2 = shape.Y + shape.Vertices[skipIndex * 2 + 1];

                uint color = AdjustBrightness(shape.Color, 0.2f + f.Treble * 0.3f);
                float thickness = 0.5f + f.Treble * 1.5f;
                canvas.DrawLine(x1, y1, x2, y2, color, thickness);
            }
        }
    }

    private void RenderGlowEffect(ISkiaCanvas canvas, MystifyShape shape, AudioFeatures f)
    {
        // Add glowing particles around the shape
        int glowCount = 5 + (int)(f.Volume * 10);

        for (int i = 0; i < glowCount; i++)
        {
            float angle = (float)(_random.NextDouble() * Math.PI * 2);
            float radius = shape.Scale * (0.8f + (float)_random.NextDouble() * 0.4f);

            float x = shape.X + (float)Math.Cos(angle) * radius;
            float y = shape.Y + (float)Math.Sin(angle) * radius;

            uint glowColor = AdjustBrightness(shape.Color, 1.5f);
            glowColor = (uint)(glowColor & 0x00FFFFFF | 0x80 << 24); // 50% alpha

            canvas.FillCircle(x, y, 3f + (f.Beat ? 1f : 0f) * 5f, glowColor);
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

        return (uint)(0xFF000000 | r << 16 | g << 8 | b);
    }
}
