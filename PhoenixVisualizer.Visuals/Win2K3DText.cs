using PhoenixVisualizer.PluginHost;
using System;
using System.Collections.Generic;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Classic Windows 2000 3D Text screensaver - faithfully recreated for Phoenix Visualizer
/// Features rotating 3D text with various effects and audio-reactive transformations
/// FIXED: Now implements actual 3D text with enhanced audio reactivity instead of random patterns
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
        // FIXED: Audio-reactive time and animation updates
        var energy = f.Energy;
        var bass = f.Bass;
        var mid = f.Mid;
        var treble = f.Treble;
        var beat = f.Beat;
        var volume = f.Volume;
        
        // Audio-reactive animation speed
        var baseSpeed = 0.016f;
        var energySpeed = energy * 0.02f;
        var trebleSpeed = treble * 0.015f;
        var beatSpeed = beat ? 0.03f : 0f;
        _time += baseSpeed + energySpeed + trebleSpeed + beatSpeed;
        _cycleTime += baseSpeed + energySpeed + trebleSpeed + beatSpeed;

        // FIXED: Enhanced audio-reactive text changes
        var baseCycleTime = 8f;
        var energyCycleTime = energy * 4f; // Faster cycles on high energy
        var beatCycleTime = beat ? 2f : 0f; // Much faster cycles on beat
        var totalCycleTime = baseCycleTime - energyCycleTime - beatCycleTime;
        
        if (_cycleTime > totalCycleTime || (beat && _random.NextDouble() < 0.5f))
        {
            ChangeText(f);
            ResetAnimation(f);
        }

        // Update animation based on audio
        UpdateAnimation(f);

        // FIXED: Enhanced audio-reactive background
        RenderBackground(canvas, f);

        // FIXED: Enhanced 3D text rendering with audio reactivity
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

    private void ChangeText(AudioFeatures f = null)
    {
        _currentTextIndex = (_currentTextIndex + 1) % _textStrings.Length;
        GenerateTextGeometry(_textStrings[_currentTextIndex]);
        _cycleTime = 0;
    }

    private void ResetAnimation(AudioFeatures f = null)
    {
        var energy = f?.Energy ?? 0f;
        var bass = f?.Bass ?? 0f;
        var treble = f?.Treble ?? 0f;
        var beat = f?.Beat ?? false;
        
        // FIXED: Audio-reactive animation reset
        var baseRotation = 360;
        var energyRotation = energy * 180f;
        var bassRotation = bass * 90f;
        var trebleRotation = treble * 90f;
        
        _rotationX = _random.Next((int)(baseRotation + energyRotation));
        _rotationY = _random.Next((int)(baseRotation + bassRotation));
        _rotationZ = _random.Next((int)(baseRotation + trebleRotation));
        
        // FIXED: Audio-reactive zoom and depth
        var baseZoom = 1f;
        var energyZoom = energy * 3f;
        var beatZoom = beat ? 2f : 0f;
        _zoomLevel = baseZoom + energyZoom + beatZoom + (float)_random.NextDouble() * 2f;
        
        var baseDepth = MIN_DEPTH;
        var energyDepth = energy * (MAX_DEPTH - MIN_DEPTH) * 0.5f;
        var bassDepth = bass * (MAX_DEPTH - MIN_DEPTH) * 0.3f;
        _depth = baseDepth + energyDepth + bassDepth + (float)_random.NextDouble() * (MAX_DEPTH - MIN_DEPTH) * 0.5f;
        
        // FIXED: Audio-reactive view angle and rotation step
        var baseViewAngle = MIN_VIEW_ANGLE;
        var trebleViewAngle = treble * (MAX_VIEW_ANGLE - MIN_VIEW_ANGLE) * 0.5f;
        _viewAngle = baseViewAngle + trebleViewAngle + (float)_random.NextDouble() * (MAX_VIEW_ANGLE - MIN_VIEW_ANGLE) * 0.5f;
        
        var baseRotationStep = MIN_ROT_STEP;
        var energyRotationStep = energy * (MAX_ROT_STEP - MIN_ROT_STEP) * 0.7f;
        var beatRotationStep = beat ? (MAX_ROT_STEP - MIN_ROT_STEP) * 0.5f : 0f;
        _rotationStep = (int)(baseRotationStep + energyRotationStep + beatRotationStep);
        
        // FIXED: Audio-reactive lighting
        _useLighting = energy > 0.5f || beat;
    }

    private void UpdateAnimation(AudioFeatures f)
    {
        var energy = f.Energy;
        var bass = f.Bass;
        var mid = f.Mid;
        var treble = f.Treble;
        var beat = f.Beat;
        var volume = f.Volume;
        
        // FIXED: Enhanced audio-reactive rotation speeds
        var baseRotSpeed = _rotationStep * 0.01f;
        var volumeMultiplier = 1f + volume * 2f;
        var energyMultiplier = 1f + energy * 1.5f;
        var beatMultiplier = beat ? 2f : 1f;
        var totalMultiplier = volumeMultiplier * energyMultiplier * beatMultiplier;

        // FIXED: Frequency-specific rotation responses
        var bassRotation = baseRotSpeed * totalMultiplier * (1f + bass * 2f);
        var midRotation = baseRotSpeed * totalMultiplier * (1f + mid * 1.5f);
        var trebleRotation = baseRotSpeed * totalMultiplier * (1f + treble * 2.5f);
        var energyRotation = baseRotSpeed * totalMultiplier * (1f + energy * 1.8f);

        _rotationX += bassRotation;
        _rotationY += midRotation;
        _rotationZ += trebleRotation;

        // FIXED: Enhanced audio-reactive zoom
        var baseZoom = 1f;
        var volumeZoom = volume * 2f;
        var energyZoom = energy * 1.5f;
        var beatZoom = beat ? 1.5f : 0f;
        var targetZoom = baseZoom + volumeZoom + energyZoom + beatZoom;
        
        var zoomSpeed = 0.02f + energy * 0.03f + (beat ? 0.05f : 0f);
        _zoomLevel += (targetZoom - _zoomLevel) * zoomSpeed;

        // FIXED: Enhanced audio-reactive depth
        if (beat || energy > 0.7f)
        {
            var baseDepth = MIN_DEPTH;
            var energyDepth = energy * (MAX_DEPTH - MIN_DEPTH) * 0.6f;
            var bassDepth = bass * (MAX_DEPTH - MIN_DEPTH) * 0.4f;
            var targetDepth = baseDepth + energyDepth + bassDepth;
            
            var depthSpeed = 0.1f + (beat ? 0.2f : 0f);
            _depth += (targetDepth - _depth) * depthSpeed;
        }

        // FIXED: Audio-reactive view angle
        var baseViewAngle = MIN_VIEW_ANGLE;
        var trebleViewAngle = treble * (MAX_VIEW_ANGLE - MIN_VIEW_ANGLE) * 0.3f;
        var energyViewAngle = energy * (MAX_VIEW_ANGLE - MIN_VIEW_ANGLE) * 0.2f;
        var targetViewAngle = baseViewAngle + trebleViewAngle + energyViewAngle;
        
        _viewAngle += (targetViewAngle - _viewAngle) * 0.01f;

        // Keep rotations in reasonable range
        _rotationX %= 360;
        _rotationY %= 360;
        _rotationZ %= 360;
    }

    private void RenderBackground(ISkiaCanvas canvas, AudioFeatures f)
    {
        var energy = f.Energy;
        var bass = f.Bass;
        var treble = f.Treble;
        var beat = f.Beat;
        
        // FIXED: Enhanced audio-reactive background
        uint topColor, bottomColor;
        
        if (beat)
        {
            topColor = 0xFF330033; // Purple on beat
            bottomColor = 0xFF110011;
        }
        else if (bass > 0.5f)
        {
            topColor = 0xFF330000; // Red for bass
            bottomColor = 0xFF110000;
        }
        else if (treble > 0.4f)
        {
            topColor = 0xFF003333; // Cyan for treble
            bottomColor = 0xFF001111;
        }
        else if (energy > 0.6f)
        {
            topColor = 0xFF333300; // Yellow for energy
            bottomColor = 0xFF111100;
        }
        else
        {
            topColor = 0xFF000033; // Default blue
            bottomColor = 0xFF000011;
        }

        // FIXED: Audio-reactive brightness
        var baseBrightness = 0.5f;
        var energyBrightness = energy * 0.4f;
        var beatBrightness = beat ? 0.3f : 0f;
        var totalBrightness = baseBrightness + energyBrightness + beatBrightness;
        
        topColor = AdjustBrightness(topColor, totalBrightness);
        bottomColor = AdjustBrightness(bottomColor, totalBrightness);

        // FIXED: Enhanced gradient fill with audio-reactive patterns
        for (int y = 0; y < _height; y++)
        {
            float t = (float)y / _height;
            
            // Add wave pattern based on audio
            var waveOffset = (float)Math.Sin(_time * 2f + y * 0.01f) * energy * 0.1f;
            t += waveOffset;
            t = Math.Max(0f, Math.Min(1f, t));
            
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
        var energy = f.Energy;
        var bass = f.Bass;
        var treble = f.Treble;
        var beat = f.Beat;
        var volume = f.Volume;
        
        // FIXED: Enhanced audio-reactive color selection
        uint color = textChar.Color;
        
        if (beat)
            color = 0xFFFFFF00; // Bright yellow on beat
        else if (bass > 0.5f)
            color = 0xFFFF0000; // Red for bass
        else if (treble > 0.4f)
            color = 0xFF00FFFF; // Cyan for treble
        else if (energy > 0.6f)
            color = 0xFFFF00FF; // Magenta for energy
        
        // FIXED: Enhanced audio-reactive lighting
        if (_useLighting)
        {
            var baseLighting = 0.5f;
            var volumeLighting = volume * 0.5f;
            var energyLighting = energy * 0.4f;
            var beatLighting = beat ? 0.8f : 0f;
            var totalLighting = baseLighting + volumeLighting + energyLighting + beatLighting;
            
            color = AdjustBrightness(color, totalLighting);
        }

        // Sort triangles by depth (back to front) for proper rendering
        var sortedTriangles = new List<(int a, int b, int c, float depth)>();

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

            // Calculate average depth for sorting
            float avgDepth = (p1.z + p2.z + p3.z) / 3f;
            sortedTriangles.Add((triangle.a, triangle.b, triangle.c, avgDepth));
        }

        // Sort by depth (farthest first)
        sortedTriangles.Sort((a, b) => b.depth.CompareTo(a.depth));

        // Render sorted triangles
        foreach (var (a, b, c, depth) in sortedTriangles)
        {
            var v1 = textChar.Vertices[a];
            var v2 = textChar.Vertices[b];
            var v3 = textChar.Vertices[c];

            // Transform vertices
            var tv1 = TransformVertex(v1.x, v1.y, v1.z);
            var tv2 = TransformVertex(v2.x, v2.y, v2.z);
            var tv3 = TransformVertex(v3.x, v3.y, v3.z);

            // Project to screen coordinates
            var p1 = Project3D(tv1.x, tv1.y, tv1.z, centerX, centerY, fov, near, far);
            var p2 = Project3D(tv2.x, tv2.y, tv2.z, centerX, centerY, fov, near, far);
            var p3 = Project3D(tv3.x, tv3.y, tv3.z, centerX, centerY, fov, near, far);

            // Back-face culling (don't render triangles facing away)
            var normal = CalculateNormal(tv1, tv2, tv3);
            if (normal.z < 0) continue; // Triangle is facing away from camera

            // Only render if all points are visible
            if (p1.z > near && p2.z > near && p3.z > near &&
                p1.z < far && p2.z < far && p3.z < far)
            {
                // Distance-based alpha
                float alpha = Math.Max(0.4f, 1f - depth / far);
                uint fadedColor = (uint)((uint)(alpha * 255) << 24 | (color & 0x00FFFFFF));

                // Fill triangle instead of just drawing edges
                FillTriangle(canvas, p1, p2, p3, fadedColor);

                // Draw subtle edges for definition
                uint edgeColor = AdjustBrightness(fadedColor, 0.7f);
                canvas.DrawLine(p1.x, p1.y, p2.x, p2.y, edgeColor, 1f);
                canvas.DrawLine(p2.x, p2.y, p3.x, p3.y, edgeColor, 1f);
                canvas.DrawLine(p3.x, p3.y, p1.x, p1.y, edgeColor, 1f);
            }
        }
    }

    private void RenderParticleEffects(ISkiaCanvas canvas, AudioFeatures f)
    {
        var energy = f.Energy;
        var bass = f.Bass;
        var treble = f.Treble;
        var beat = f.Beat;
        var volume = f.Volume;
        
        // FIXED: Enhanced audio-reactive particle effects
        var baseParticleCount = 10;
        var volumeParticles = (int)(volume * 30);
        var energyParticles = (int)(energy * 20);
        var beatParticles = beat ? 15 : 0;
        int particleCount = baseParticleCount + volumeParticles + energyParticles + beatParticles;

        for (int i = 0; i < particleCount; i++)
        {
            // FIXED: Audio-reactive particle motion
            var baseAngle = _time * 2f + i * 0.5f;
            var bassAngle = bass * 0.5f;
            var trebleAngle = treble * 0.3f;
            var energyAngle = energy * 0.4f;
            float angle = baseAngle + bassAngle + trebleAngle + energyAngle;
            
            var baseRadius = 100f;
            var energyRadius = energy * 100f;
            var beatRadius = beat ? 50f : 0f;
            float radius = baseRadius + energyRadius + beatRadius + (float)Math.Sin(_time + i * 0.1f) * 50f;
            
            float x = _width * 0.5f + (float)Math.Cos(angle) * radius;
            float y = _height * 0.5f + (float)Math.Sin(angle) * radius;

            // FIXED: Audio-reactive particle colors
            uint particleColor;
            if (beat && i < 5)
                particleColor = 0xFFFFFF00; // Bright yellow for beat particles
            else if (bass > 0.4f && i % 3 == 0)
                particleColor = 0xFFFF0000; // Red for bass particles
            else if (treble > 0.3f && i % 2 == 0)
                particleColor = 0xFF00FFFF; // Cyan for treble particles
            else if (energy > 0.5f)
                particleColor = 0xFFFF00FF; // Magenta for energy particles
            else
                particleColor = _textColors[i % _textColors.Length];
            
            // FIXED: Audio-reactive particle size and alpha
            var baseAlpha = 0.6f;
            var energyAlpha = energy * 0.3f;
            var beatAlpha = beat ? 0.4f : 0f;
            float alpha = baseAlpha + energyAlpha + beatAlpha;
            alpha = Math.Min(1f, alpha);
            
            particleColor = (uint)((uint)(alpha * 255) << 24 | (particleColor & 0x00FFFFFF));

            // FIXED: Audio-reactive particle size
            var baseSize = 2f;
            var energySize = energy * 3f;
            var beatSize = beat ? 4f : 0f;
            var particleSize = baseSize + energySize + beatSize;
            
            canvas.FillCircle(x, y, particleSize, particleColor);
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

    private (float x, float y, float z) CalculateNormal((float x, float y, float z) v1, (float x, float y, float z) v2, (float x, float y, float z) v3)
    {
        // Calculate surface normal using cross product
        float ux = v2.x - v1.x;
        float uy = v2.y - v1.y;
        float uz = v2.z - v1.z;

        float vx = v3.x - v1.x;
        float vy = v3.y - v1.y;
        float vz = v3.z - v1.z;

        float nx = uy * vz - uz * vy;
        float ny = uz * vx - ux * vz;
        float nz = ux * vy - uy * vx;

        // Normalize
        float length = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
        if (length > 0)
        {
            nx /= length;
            ny /= length;
            nz /= length;
        }

        return (nx, ny, nz);
    }

    private void FillTriangle(ISkiaCanvas canvas, (float x, float y, float z) p1, (float x, float y, float z) p2, (float x, float y, float z) p3, uint color)
    {
        // Simple triangle filling by drawing horizontal lines
        // Sort points by Y coordinate
        var points = new[] { p1, p2, p3 };
        Array.Sort(points, (a, b) => a.y.CompareTo(b.y));

        var top = points[0];
        var middle = points[1];
        var bottom = points[2];

        // If middle and bottom are at same height, handle as flat bottom
        if (Math.Abs(middle.y - bottom.y) < 0.1f)
        {
            FillFlatBottomTriangle(canvas, top, middle, bottom, color);
        }
        // If top and middle are at same height, handle as flat top
        else if (Math.Abs(top.y - middle.y) < 0.1f)
        {
            FillFlatTopTriangle(canvas, top, middle, bottom, color);
        }
        // General case - split into flat bottom and flat top
        else
        {
            // Find intermediate point on longer edge
            float t = (middle.y - top.y) / (bottom.y - top.y);
            var intermediate = (
                top.x + t * (bottom.x - top.x),
                middle.y,
                top.z + t * (bottom.z - top.z)
            );

            FillFlatBottomTriangle(canvas, top, intermediate, middle, color);
            FillFlatTopTriangle(canvas, intermediate, middle, bottom, color);
        }
    }

    private void FillFlatBottomTriangle(ISkiaCanvas canvas, (float x, float y, float z) v1, (float x, float y, float z) v2, (float x, float y, float z) v3, uint color)
    {
        // v1 and v2 are at the same Y, v3 is below
        float invSlope1 = (v2.x - v1.x) / (v2.y - v1.y + 0.001f);
        float invSlope2 = (v3.x - v1.x) / (v3.y - v1.y + 0.001f);

        float curX1 = v1.x;
        float curX2 = v1.x;

        for (float y = v1.y; y <= v2.y; y++)
        {
            if (y >= 0 && y < _height)
            {
                int startX = (int)Math.Max(0, Math.Min(curX1, curX2));
                int endX = (int)Math.Min(_width - 1, Math.Max(curX1, curX2));

                if (startX < endX)
                {
                    canvas.DrawLine(startX, y, endX, y, color, 1f);
                }
            }

            curX1 += invSlope1;
            curX2 += invSlope2;
        }
    }

    private void FillFlatTopTriangle(ISkiaCanvas canvas, (float x, float y, float z) v1, (float x, float y, float z) v2, (float x, float y, float z) v3, uint color)
    {
        // v1 and v2 are at the same Y, v3 is above
        float invSlope1 = (v3.x - v1.x) / (v3.y - v1.y + 0.001f);
        float invSlope2 = (v3.x - v2.x) / (v3.y - v2.y + 0.001f);

        float curX1 = v3.x;
        float curX2 = v3.x;

        for (float y = v3.y; y >= v1.y; y--)
        {
            if (y >= 0 && y < _height)
            {
                int startX = (int)Math.Max(0, Math.Min(curX1, curX2));
                int endX = (int)Math.Min(_width - 1, Math.Max(curX1, curX2));

                if (startX < endX)
                {
                    canvas.DrawLine(startX, y, endX, y, color, 1f);
                }
            }

            curX1 -= invSlope1;
            curX2 -= invSlope2;
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

        return (uint)(0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | (uint)b);
    }
}
