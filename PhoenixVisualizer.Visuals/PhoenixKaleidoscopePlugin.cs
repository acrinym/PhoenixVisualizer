using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

public sealed class PhoenixKaleidoscopePlugin : IVisualizerPlugin
{
    public string Id => "phoenix_kaleidoscope";
    public string DisplayName => "🔥 Phoenix Kaleidoscope";
    public string Description => "4-8 segment mirroring with slow rotation and Phoenix fire color wheel";
    public bool IsEnabled { get; set; } = true;

    private int _w, _h;
    private float _rotation = 0f;
    private float _time = 0f;
    private readonly int _numSegments = 6; // 6-fold symmetry
    private readonly int _numParticles = 200;

    // Phoenix color palette (NO GREEN!)
    private readonly uint[] _fireColors = new uint[]
    {
        0xFFFF4400, // Hot red-orange
        0xFFFF6600, // Bright orange
        0xFFFF8800, // Warm orange
        0xFFFFAA00, // Golden orange
        0xFFFFCC00, // Bright yellow
        0xFFFFEE00, // Light yellow
        0xFFFFFFFF  // White
    };

    public void Initialize() { }
    public void Initialize(int width, int height) { _w = width; _h = height; }
    public void Resize(int width, int height) { _w = width; _h = height; }
    public void Shutdown() { }
    public void ProcessFrame(AudioFeatures features, ISkiaCanvas canvas) { RenderFrame(features, canvas); }
    public void Configure() { }
    public void Dispose() { }

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        // Clear with dark background
        canvas.Clear(0xFF000000);

        // Update time and rotation
        _time += 0.02f;
        _rotation += 0.01f; // Slow rotation
        if (features.Beat) _rotation += 0.05f; // Extra rotation on beat

        var centerX = _w / 2f;
        var centerY = _h / 2f;
        var maxRadius = Math.Min(_w, _h) * 0.45f;

        // Get audio data for color wheel
        var midEnergy = features.Mid;
        var trebleEnergy = features.Treble;
        var bass = features.Bass;

        // Draw kaleidoscope segments
        for (int segment = 0; segment < _numSegments; segment++)
        {
            var segmentAngle = (segment / (float)_numSegments) * Math.PI * 2f + _rotation;
            
            // Draw segment boundary lines
            var boundaryColor = (uint)(0x44FFFFFF);
            canvas.SetLineWidth(1f);
            
            var endX = centerX + (float)Math.Cos(segmentAngle) * maxRadius;
            var endY = centerY + (float)Math.Sin(segmentAngle) * maxRadius;
            canvas.DrawLine(centerX, centerY, endX, endY, boundaryColor, 1f);
        }

        // Draw particles in each segment
        for (int i = 0; i < _numParticles; i++)
        {
            var t = i / (float)_numParticles;
            
            // Create base particle position
            var angle = t * Math.PI * 4f + _time * 0.5f;
            var radius = t * maxRadius * 0.8f;
            
            // Add some spiral motion
            var spiral = (float)Math.Sin(t * 10f + _time * 2f) * 0.1f;
            radius += spiral * maxRadius * 0.2f;
            
            var baseX = centerX + (float)Math.Cos(angle) * radius;
            var baseY = centerY + (float)Math.Sin(angle) * radius;
            
            // Mirror the particle across all segments
            for (int segment = 0; segment < _numSegments; segment++)
            {
                var segmentAngle = (segment / (float)_numSegments) * Math.PI * 2f + _rotation;
                
                // Rotate particle to segment
                var dx = baseX - centerX;
                var dy = baseY - centerY;
                
                var cos = (float)Math.Cos(segmentAngle);
                var sin = (float)Math.Sin(segmentAngle);
                
                var rotatedX = centerX + dx * cos - dy * sin;
                var rotatedY = centerY + dx * sin + dy * cos;
                
                // Get color based on position and audio
                var color = GetKaleidoscopeColor(t, radius, maxRadius, midEnergy, trebleEnergy);
                
                // Add alpha based on distance from center
                var alpha = (byte)((1f - t) * 255);
                color = (color & 0x00FFFFFF) | ((uint)alpha << 24);
                
                // Draw particle
                var particleSize = 2f + (1f - t) * 4f;
                canvas.FillCircle(rotatedX, rotatedY, particleSize, color);
            }
        }

        // Draw center mandala
        var mandalaRadius = 30f + bass * 40f;
        var mandalaColor = GetFireColor(bass);
        canvas.FillCircle(centerX, centerY, mandalaRadius, mandalaColor);

        // Draw inner rings
        for (int ring = 1; ring <= 3; ring++)
        {
            var ringRadius = mandalaRadius * ring / 3f;
            var ringAlpha = (byte)(100 - ring * 30);
            var ringColor = (uint)(ringAlpha << 24 | 0xFFFF4400);
            canvas.DrawCircle(centerX, centerY, ringRadius, ringColor, false);
        }

        // Draw outer boundary
        canvas.DrawCircle(centerX, centerY, maxRadius, 0x22FFFFFF, false);

        // Add sparkle effects on strong treble
        if (trebleEnergy > 0.6f)
        {
            for (int i = 0; i < 12; i++)
            {
                var angle = i * Math.PI * 2f / 12f + _time * 3f;
                var sparkleRadius = maxRadius + 20f;
                var sparkleX = centerX + (float)Math.Cos(angle) * sparkleRadius;
                var sparkleY = centerY + (float)Math.Sin(angle) * sparkleRadius;
                var sparkleColor = GetFireColor(trebleEnergy);
                canvas.FillCircle(sparkleX, sparkleY, 4f, sparkleColor);
            }
        }
    }

    private uint GetKaleidoscopeColor(float t, float radius, float maxRadius, float midEnergy, float trebleEnergy)
    {
        // Color wheel based on position and audio energy
        var positionRatio = radius / maxRadius;
        var energyMix = (midEnergy + trebleEnergy) * 0.5f;
        
        // Create color wheel that rotates with time
        var hue = (t + _time * 0.3f + energyMix * 0.5f) * 6.283f;
        
        // Map to Phoenix fire colors
        var colorIndex = (int)((hue / 6.283f) * _fireColors.Length) % _fireColors.Length;
        var nextColorIndex = (colorIndex + 1) % _fireColors.Length;
        
        var t2 = (hue / 6.283f) * _fireColors.Length - colorIndex;
        return InterpolateColor(_fireColors[colorIndex], _fireColors[nextColorIndex], t2);
    }

    private uint GetFireColor(float intensity)
    {
        var index = (int)(intensity * (_fireColors.Length - 1));
        var t = intensity * (_fireColors.Length - 1) - index;
        
        if (index >= _fireColors.Length - 1)
            return _fireColors[_fireColors.Length - 1];
            
        return InterpolateColor(_fireColors[index], _fireColors[index + 1], t);
    }

    private uint InterpolateColor(uint color1, uint color2, float t)
    {
        var r1 = (color1 >> 16) & 0xFF;
        var g1 = (color1 >> 8) & 0xFF;
        var b1 = color1 & 0xFF;
        
        var r2 = (color2 >> 16) & 0xFF;
        var g2 = (color2 >> 8) & 0xFF;
        var b2 = color2 & 0xFF;

        var r = (byte)(r1 + (r2 - r1) * t);
        var g = (byte)(g1 + (g2 - g1) * t);
        var b = (byte)(b1 + (b2 - b1) * t);

        return (uint)((r << 16) | (g << 8) | b);
    }
}
