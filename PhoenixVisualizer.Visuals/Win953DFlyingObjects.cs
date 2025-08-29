using System;
using System.Collections.Generic;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Classic Windows 95 3D Flying Objects screensaver - faithfully recreated for Phoenix Visualizer
/// Features evolving geometric shapes (spheres, ribbons, cones, boxes) that morph and fly in 3D space
/// </summary>
public sealed class Win953DFlyingObjects : IVisualizerPlugin
{
    public string Id => "win95_3d_flying_objects";
    public string DisplayName => "🪟 Win95 3D Flying Objects";

    private int _width, _height;
    private float _time;
    private Random _random = new();

    // Shape system constants (based on original Win95 3DFO implementation)
    private const int MAX_SHAPES = 8;
    private const int MAX_VERTICES_PER_SHAPE = 64;
    private const float SHAPE_SIZE = 1.5f;
    private const float MORPH_SPEED = 0.02f;
    private const float ROTATION_SPEED = 0.5f;

    // Shape types from original implementation
    private enum ShapeType
    {
        Sphere = 0,
        Ribbon = 1,
        Cone = 2,
        Box = 3,
        Torus = 4,
        Helix = 5,
        SpikeBall = 6,
        MorphingBlob = 7
    }

    // Individual flying object
    private struct FlyingObject
    {
        public ShapeType Type;
        public float X, Y, Z;
        public float RotX, RotY, RotZ;
        public float VelX, VelY, VelZ;
        public float Scale;
        public uint Color;
        public float MorphPhase;
        public float AudioInfluence;

        // Shape geometry
        public List<(float x, float y, float z)> Vertices;
        public List<(float x, float y, float z)> Normals;
        public List<(int a, int b, int c)> Triangles;
    }

    // Active flying objects
    private List<FlyingObject> _flyingObjects = new();

    // Colors inspired by the original Windows 95 palette
    private readonly uint[] _shapeColors = new uint[]
    {
        0xFFFF0000, // Red
        0xFF00FF00, // Green
        0xFF0000FF, // Blue
        0xFFFFFF00, // Yellow
        0xFFFF00FF, // Magenta
        0xFF00FFFF, // Cyan
        0xFFFF8000, // Orange
        0xFF800080, // Purple
        0xFF80FF80, // Light green
        0xFF8080FF, // Light blue
        0xFFFFFF80, // Light yellow
        0xFFFF80FF  // Pink
    };

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;

        // Initialize with several flying objects
        for (int i = 0; i < MAX_SHAPES; i++)
        {
            CreateFlyingObject();
        }
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Dispose()
    {
        _flyingObjects.Clear();
    }

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        _time += 0.016f;

        // Clear with dark space-like background
        canvas.Clear(0xFF0A0A1A);

        // Update all flying objects
        UpdateFlyingObjects(f);

        // Render all objects in 3D
        RenderFlyingObjects3D(canvas, f);

        // Occasionally create new objects or remove old ones
        if (_random.NextDouble() < 0.01f && _flyingObjects.Count < MAX_SHAPES)
        {
            CreateFlyingObject();
        }

        // Remove objects that are too far
        _flyingObjects.RemoveAll(obj => obj.Z > 20f);
    }

    private void CreateFlyingObject()
    {
        var obj = new FlyingObject
        {
            Type = (ShapeType)_random.Next(Enum.GetValues(typeof(ShapeType)).Length),
            X = (float)(_random.NextDouble() * 10 - 5),
            Y = (float)(_random.NextDouble() * 10 - 5),
            Z = (float)(_random.NextDouble() * 10 + 5),
            RotX = (float)(_random.NextDouble() * Math.PI * 2),
            RotY = (float)(_random.NextDouble() * Math.PI * 2),
            RotZ = (float)(_random.NextDouble() * Math.PI * 2),
            VelX = (float)(_random.NextDouble() * 0.1f - 0.05f),
            VelY = (float)(_random.NextDouble() * 0.1f - 0.05f),
            VelZ = (float)(_random.NextDouble() * 0.05 - 0.1), // Move toward camera
            Scale = 0.5f + (float)_random.NextDouble(),
            Color = _shapeColors[_random.Next(_shapeColors.Length)],
            MorphPhase = (float)(_random.NextDouble() * Math.PI * 2),
            AudioInfluence = 0f,
            Vertices = new List<(float x, float y, float z)>(),
            Normals = new List<(float x, float y, float z)>(),
            Triangles = new List<(int a, int b, int c)>()
        };

        // Generate initial geometry
        GenerateShapeGeometry(obj);
        _flyingObjects.Add(obj);
    }

    private void GenerateShapeGeometry(FlyingObject obj)
    {
        obj.Vertices.Clear();
        obj.Normals.Clear();
        obj.Triangles.Clear();

        switch (obj.Type)
        {
            case ShapeType.Sphere:
                GenerateSphereGeometry(obj);
                break;
            case ShapeType.Ribbon:
                GenerateRibbonGeometry(obj);
                break;
            case ShapeType.Cone:
                GenerateConeGeometry(obj);
                break;
            case ShapeType.Box:
                GenerateBoxGeometry(obj);
                break;
            case ShapeType.Torus:
                GenerateTorusGeometry(obj);
                break;
            case ShapeType.Helix:
                GenerateHelixGeometry(obj);
                break;
            case ShapeType.SpikeBall:
                GenerateSpikeBallGeometry(obj);
                break;
            case ShapeType.MorphingBlob:
                GenerateMorphingBlobGeometry(obj);
                break;
        }
    }

    private void GenerateSphereGeometry(FlyingObject obj)
    {
        int stacks = 8;
        int slices = 8;
        float radius = SHAPE_SIZE * obj.Scale;

        for (int i = 0; i <= stacks; i++)
        {
            float phi = (float)(i * Math.PI / stacks);
            for (int j = 0; j <= slices; j++)
            {
                float theta = (float)(j * 2 * Math.PI / slices);

                float x = radius * (float)(Math.Sin(phi) * Math.Cos(theta));
                float y = radius * (float)(Math.Sin(phi) * Math.Sin(theta));
                float z = radius * (float)Math.Cos(phi);

                obj.Vertices.Add((x, y, z));
                obj.Normals.Add((x / radius, y / radius, z / radius));
            }
        }

        // Generate triangles
        for (int i = 0; i < stacks; i++)
        {
            for (int j = 0; j < slices; j++)
            {
                int first = i * (slices + 1) + j;
                int second = first + slices + 1;

                obj.Triangles.Add((first, second, first + 1));
                obj.Triangles.Add((second, second + 1, first + 1));
            }
        }
    }

    private void GenerateRibbonGeometry(FlyingObject obj)
    {
        int segments = 20;
        float width = SHAPE_SIZE * obj.Scale;
        float length = SHAPE_SIZE * 2 * obj.Scale;

        // Create a wavy ribbon
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float wave = (float)Math.Sin(t * Math.PI * 4 + obj.MorphPhase) * 0.3f;

            // Top edge
            obj.Vertices.Add((-length/2 + t * length, width/2 + wave, 0));
            obj.Normals.Add((0, 0, 1));

            // Bottom edge
            obj.Vertices.Add((-length/2 + t * length, -width/2 + wave, 0));
            obj.Normals.Add((0, 0, 1));
        }

        // Generate triangles
        for (int i = 0; i < segments; i++)
        {
            int baseIdx = i * 2;
            obj.Triangles.Add((baseIdx, baseIdx + 1, baseIdx + 2));
            obj.Triangles.Add((baseIdx + 1, baseIdx + 3, baseIdx + 2));
        }
    }

    private void GenerateConeGeometry(FlyingObject obj)
    {
        int segments = 16;
        float radius = SHAPE_SIZE * obj.Scale;
        float height = SHAPE_SIZE * 2 * obj.Scale;

        // Apex
        obj.Vertices.Add((0, 0, height/2));
        obj.Normals.Add((0, 0, 1));

        // Base vertices
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)(i * 2 * Math.PI / segments);
            float x = radius * (float)Math.Cos(angle);
            float y = radius * (float)Math.Sin(angle);
            obj.Vertices.Add((x, y, -height/2));
            obj.Normals.Add((x / radius, y / radius, 0));
        }

        // Generate triangles
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            obj.Triangles.Add((0, i + 1, next + 1));
        }
    }

    private void GenerateBoxGeometry(FlyingObject obj)
    {
        float size = SHAPE_SIZE * obj.Scale;

        // Cube vertices
        obj.Vertices.AddRange(new (float x, float y, float z)[]
        {
            (-size, -size, -size), (size, -size, -size), (size, size, -size), (-size, size, -size), // Back
            (-size, -size, size), (size, -size, size), (size, size, size), (-size, size, size)     // Front
        });

        // Normals for each face
        obj.Normals.AddRange(new (float x, float y, float z)[]
        {
            (0, 0, -1), (0, 0, -1), (0, 0, -1), (0, 0, -1), // Back
            (0, 0, 1), (0, 0, 1), (0, 0, 1), (0, 0, 1)      // Front
        });

        // Cube triangles (simplified - just front and back faces for visibility)
        obj.Triangles.AddRange(new[]
        {
            (0, 1, 2), (0, 2, 3), // Back
            (4, 6, 5), (4, 7, 6)  // Front
        });
    }

    private void GenerateTorusGeometry(FlyingObject obj)
    {
        int majorSegments = 12;
        int minorSegments = 8;
        float majorRadius = SHAPE_SIZE * obj.Scale;
        float minorRadius = SHAPE_SIZE * 0.3f * obj.Scale;

        for (int i = 0; i < majorSegments; i++)
        {
            float u = (float)(i * 2 * Math.PI / majorSegments);
            float cosU = (float)Math.Cos(u);
            float sinU = (float)Math.Sin(u);

            for (int j = 0; j < minorSegments; j++)
            {
                float v = (float)(j * 2 * Math.PI / minorSegments);
                float cosV = (float)Math.Cos(v);
                float sinV = (float)Math.Sin(v);

                float x = (majorRadius + minorRadius * cosV) * cosU;
                float y = (majorRadius + minorRadius * cosV) * sinU;
                float z = minorRadius * sinV;

                obj.Vertices.Add((x, y, z));
                obj.Normals.Add((cosV * cosU, cosV * sinU, sinV));
            }
        }

        // Generate triangles (simplified)
        for (int i = 0; i < majorSegments; i++)
        {
            for (int j = 0; j < minorSegments; j++)
            {
                int current = i * minorSegments + j;
                int next = i * minorSegments + (j + 1) % minorSegments;
                int nextRing = ((i + 1) % majorSegments) * minorSegments + j;
                int nextRingNext = ((i + 1) % majorSegments) * minorSegments + (j + 1) % minorSegments;

                obj.Triangles.Add((current, next, nextRing));
                obj.Triangles.Add((next, nextRingNext, nextRing));
            }
        }
    }

    private void GenerateHelixGeometry(FlyingObject obj)
    {
        int segments = 32;
        float radius = SHAPE_SIZE * obj.Scale;
        float height = SHAPE_SIZE * 2 * obj.Scale;
        int turns = 3;

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);
            float angle = t * turns * 2 * (float)Math.PI;
            float x = radius * (float)Math.Cos(angle);
            float y = radius * (float)Math.Sin(angle);
            float z = -height/2 + t * height;

            obj.Vertices.Add((x, y, z));
            obj.Normals.Add((x / radius, y / radius, 0.1f));
        }

        // Generate triangles for ribbon effect
        for (int i = 0; i < segments - 1; i++)
        {
            obj.Triangles.Add((i, i + 1, (i + 2) % segments));
        }
    }

    private void GenerateSpikeBallGeometry(FlyingObject obj)
    {
        int numSpikes = 12;
        float ballRadius = SHAPE_SIZE * 0.5f * obj.Scale;
        float spikeLength = SHAPE_SIZE * obj.Scale;

        // Center point
        obj.Vertices.Add((0, 0, 0));
        obj.Normals.Add((0, 0, 1));

        // Generate spikes
        for (int i = 0; i < numSpikes; i++)
        {
            float theta = (float)(i * Math.PI * 2 / numSpikes);
            float phi = (float)(Math.PI / 4); // 45 degrees

            float x = ballRadius * (float)(Math.Sin(phi) * Math.Cos(theta));
            float y = ballRadius * (float)(Math.Sin(phi) * Math.Sin(theta));
            float z = ballRadius * (float)Math.Cos(phi);

            // Spike base
            obj.Vertices.Add((x, y, z));
            obj.Normals.Add((x / ballRadius, y / ballRadius, z / ballRadius));

            // Spike tip
            float tipX = x + spikeLength * (float)(Math.Sin(phi) * Math.Cos(theta));
            float tipY = y + spikeLength * (float)(Math.Sin(phi) * Math.Sin(theta));
            float tipZ = z + spikeLength * (float)Math.Cos(phi);

            obj.Vertices.Add((tipX, tipY, tipZ));
            obj.Normals.Add((tipX / (ballRadius + spikeLength), tipY / (ballRadius + spikeLength), tipZ / (ballRadius + spikeLength)));
        }

        // Generate triangles for spikes
        for (int i = 0; i < numSpikes; i++)
        {
            int baseIdx = i * 2 + 1;
            int tipIdx = baseIdx + 1;
            int nextBase = ((i + 1) % numSpikes) * 2 + 1;

            obj.Triangles.Add((0, baseIdx, nextBase));
            obj.Triangles.Add((baseIdx, tipIdx, nextBase));
        }
    }

    private void GenerateMorphingBlobGeometry(FlyingObject obj)
    {
        int numPoints = 16;
        float baseRadius = SHAPE_SIZE * obj.Scale;

        for (int i = 0; i < numPoints; i++)
        {
            float theta = (float)(i * 2 * Math.PI / numPoints);
            float morph1 = (float)Math.Sin(obj.MorphPhase + theta * 2) * 0.3f;
            float morph2 = (float)Math.Cos(obj.MorphPhase * 1.5f + theta * 3) * 0.2f;
            float radius = baseRadius * (1f + morph1 + morph2);

            float x = radius * (float)Math.Cos(theta);
            float y = radius * (float)Math.Sin(theta);
            float z = morph1 * baseRadius * 0.5f;

            obj.Vertices.Add((x, y, z));
            obj.Normals.Add((x / radius, y / radius, z / (baseRadius * 0.5f)));
        }

        // Generate triangles
        for (int i = 0; i < numPoints; i++)
        {
            int next = (i + 1) % numPoints;
            obj.Triangles.Add((0, i, next)); // Connect to center
            obj.Triangles.Add((i, next, (i + 2) % numPoints)); // Create web
        }
    }

    private void UpdateFlyingObjects(AudioFeatures f)
    {
        for (int i = _flyingObjects.Count - 1; i >= 0; i--)
        {
            var obj = _flyingObjects[i];

            // Update position
            obj.X += obj.VelX * (1f + f.Volume * 0.5f);
            obj.Y += obj.VelY * (1f + f.Volume * 0.5f);
            obj.Z += obj.VelZ * (1f + f.Volume * 0.5f);

            // Update rotation
            obj.RotX += ROTATION_SPEED * 0.01f * (1f + f.Bass * 2f);
            obj.RotY += ROTATION_SPEED * 0.015f * (1f + f.Mid * 1.5f);
            obj.RotZ += ROTATION_SPEED * 0.008f * (1f + f.Treble * 3f);

            // Update morphing
            obj.MorphPhase += MORPH_SPEED * (1f + f.Volume);
            obj.AudioInfluence = f.Volume;

            // Bounce off boundaries
            if (Math.Abs(obj.X) > 15f)
            {
                obj.VelX = -obj.VelX;
                obj.X = Math.Sign(obj.X) * 15f;
            }
            if (Math.Abs(obj.Y) > 15f)
            {
                obj.VelY = -obj.VelY;
                obj.Y = Math.Sign(obj.Y) * 15f;
            }

            // Update geometry for morphing shapes
            if (obj.Type == ShapeType.MorphingBlob || obj.Type == ShapeType.Ribbon)
            {
                GenerateShapeGeometry(obj);
            }

            _flyingObjects[i] = obj;
        }
    }

    private void RenderFlyingObjects3D(ISkiaCanvas canvas, AudioFeatures f)
    {
        float centerX = _width * 0.5f;
        float centerY = _height * 0.5f;

        // 3D perspective parameters
        float fov = 60f * (float)(Math.PI / 180f);
        float near = 0.1f;
        float far = 50f;

        foreach (var obj in _flyingObjects)
        {
            RenderObject3D(canvas, obj, centerX, centerY, fov, near, far, f);
        }
    }

    private void RenderObject3D(ISkiaCanvas canvas, FlyingObject obj, float centerX, float centerY,
                               float fov, float near, float far, AudioFeatures f)
    {
        // Audio-reactive color and brightness
        uint color = obj.Color;
        float brightness = 0.7f + obj.AudioInfluence * 0.5f;
        color = AdjustBrightness(color, brightness);

        foreach (var triangle in obj.Triangles)
        {
            if (triangle.a >= obj.Vertices.Count || triangle.b >= obj.Vertices.Count || triangle.c >= obj.Vertices.Count)
                continue;

            var v1 = TransformVertex(obj.Vertices[triangle.a], obj);
            var v2 = TransformVertex(obj.Vertices[triangle.b], obj);
            var v3 = TransformVertex(obj.Vertices[triangle.c], obj);

            // Project to screen coordinates
            var p1 = Project3D(v1.x, v1.y, v1.z, centerX, centerY, fov, near, far);
            var p2 = Project3D(v2.x, v2.y, v2.z, centerX, centerY, fov, near, far);
            var p3 = Project3D(v3.x, v3.y, v3.z, centerX, centerY, fov, near, far);

            // Only render if all points are visible
            if (p1.z > near && p2.z > near && p3.z > near &&
                p1.z < far && p2.z < far && p3.z < far)
            {
                // Distance-based alpha
                float avgZ = (p1.z + p2.z + p3.z) / 3f;
                float alpha = Math.Max(0.2f, 1f - avgZ / far);
                uint fadedColor = (uint)((uint)(alpha * 255) << 24 | (color & 0x00FFFFFF));

                // Draw triangle edges
                canvas.DrawLine(p1.x, p1.y, p2.x, p2.y, fadedColor, 2f);
                canvas.DrawLine(p2.x, p2.y, p3.x, p3.y, fadedColor, 2f);
                canvas.DrawLine(p3.x, p3.y, p1.x, p1.y, fadedColor, 2f);
            }
        }
    }

    private (float x, float y, float z) TransformVertex((float x, float y, float z) vertex, FlyingObject obj)
    {
        // Apply object transformation (rotation)
        float cosX = (float)Math.Cos(obj.RotX);
        float sinX = (float)Math.Sin(obj.RotX);
        float cosY = (float)Math.Cos(obj.RotY);
        float sinY = (float)Math.Sin(obj.RotY);
        float cosZ = (float)Math.Cos(obj.RotZ);
        float sinZ = (float)Math.Sin(obj.RotZ);

        // Rotate around Z axis
        float x1 = vertex.x * cosZ - vertex.y * sinZ;
        float y1 = vertex.x * sinZ + vertex.y * cosZ;
        float z1 = vertex.z;

        // Rotate around Y axis
        float x2 = x1 * cosY + z1 * sinY;
        float z2 = -x1 * sinY + z1 * cosY;
        float y2 = y1;

        // Rotate around X axis
        float y3 = y2 * cosX - z2 * sinX;
        float z3 = y2 * sinX + z2 * cosX;
        float x3 = x2;

        // Apply object position
        x3 += obj.X;
        y3 += obj.Y;
        z3 += obj.Z;

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
}
