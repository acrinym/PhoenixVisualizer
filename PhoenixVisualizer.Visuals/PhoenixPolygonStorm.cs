using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Phoenix Polygon Storm - Audio-reactive expanding polygons with dynamic scaling and rotation
/// Inspired by Windows Media Player's Visualizer3 but with enhanced geometric patterns
/// </summary>
public sealed class PhoenixPolygonStorm : IVisualizerPlugin
{
    public string Id => "phoenix_polygon_storm";
    public string DisplayName => "⚡ Phoenix Polygon Storm";

    private int _width, _height;
    private float _time;
    private readonly Random _random = new();

    // Storm system constants
    private const int MAX_POLYGONS = 8;
    private const int MIN_SIDES = 3;
    private const int MAX_SIDES = 12;
    private const float EXPANSION_RATE = 1.5f;
    private const float ROTATION_SPEED = 0.02f;

    // Polygon storm state
    private readonly PolygonData[] _polygons;
    private readonly uint[] _stormColors = new uint[]
    {
        0xFF00FFFF, // Cyan
        0xFFFF00FF, // Magenta
        0xFFFFFF00, // Yellow
        0xFFFF0080, // Hot Pink
        0xFF80FF00, // Lime Green
        0xFF0080FF, // Electric Blue
        0xFFFF8000, // Orange
        0xFF8000FF, // Purple
        0xFF00FF80, // Spring Green
        0xFF8080FF, // Light Purple
        0xFFFF8080, // Light Coral
        0xFF80FFFF, // Light Cyan
    };

    public PhoenixPolygonStorm()
    {
        _polygons = new PolygonData[MAX_POLYGONS];
        InitializePolygons();
    }

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;
        ResetPolygons();
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        ResetPolygons();
    }

    public void Dispose() { }

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        _time += 0.016f;

        // Create dynamic storm background
        uint bgColor = CalculateStormBackground(f.Volume, f.Bass);
        canvas.Clear(bgColor);

        // Update and render polygons
        for (int i = 0; i < _polygons.Length; i++)
        {
            UpdatePolygon(i, f);
            RenderPolygon(canvas, _polygons[i], f.Volume, f.Beat);
        }

        // Add storm effects
        RenderStormEffects(canvas, f);
    }

    private void InitializePolygons()
    {
        for (int i = 0; i < _polygons.Length; i++)
        {
            _polygons[i] = new PolygonData();
        }
    }

    private void ResetPolygons()
    {
        float centerX = _width * 0.5f;
        float centerY = _height * 0.5f;

        for (int i = 0; i < _polygons.Length; i++)
        {
            var poly = _polygons[i];
            poly.Sides = MIN_SIDES + (i * 2) % (MAX_SIDES - MIN_SIDES + 1);
            poly.Radius = 10f + i * 5f;
            poly.Rotation = (float)(_random.NextDouble() * Math.PI * 2);
            poly.ExpansionFactor = 0.1f;
            poly.X = centerX + (float)(_random.NextDouble() - 0.5) * 100f;
            poly.Y = centerY + (float)(_random.NextDouble() - 0.5) * 100f;
            poly.ColorIndex = i % _stormColors.Length;
            poly.PulsePhase = (float)(_random.NextDouble() * Math.PI * 2);
        }
    }

    private void UpdatePolygon(int index, AudioFeatures f)
    {
        var poly = _polygons[index];

        // Update rotation based on audio
        poly.Rotation += ROTATION_SPEED * (1f + f.Mid * 2f);

        // Update expansion
        poly.ExpansionFactor *= 0.98f; // Decay
        poly.ExpansionFactor += f.Volume * 0.1f; // Grow with volume

        // Update pulse phase
        poly.PulsePhase += 0.1f + f.Treble * 0.2f;

        // Update position with audio-reactive movement
        float moveAmount = f.Bass * 20f;
        poly.X += (float)(Math.Sin(_time * 0.5f + index) * moveAmount);
        poly.Y += (float)(Math.Cos(_time * 0.7f + index) * moveAmount);

        // Keep within bounds
        poly.X = MathF.Max(poly.Radius, MathF.Min(_width - poly.Radius, poly.X));
        poly.Y = MathF.Max(poly.Radius, MathF.Min(_height - poly.Radius, poly.Y));

        // Reset if too large
        if (poly.ExpansionFactor > 3.0f)
        {
            poly.ExpansionFactor = 0.1f;
            poly.Rotation = (float)(_random.NextDouble() * Math.PI * 2);
        }
    }

    private void RenderPolygon(ISkiaCanvas canvas, PolygonData poly, float volume, bool beat)
    {
        float currentRadius = poly.Radius * poly.ExpansionFactor;
        if (currentRadius < 5f) return; // Don't render tiny polygons

        // Calculate pulse effect
        float pulseFactor = 1f + (float)Math.Sin(poly.PulsePhase) * 0.3f;
        float effectiveRadius = currentRadius * pulseFactor;

        // Get polygon vertices
        var points = CalculatePolygonPoints(poly.X, poly.Y, effectiveRadius, poly.Sides, poly.Rotation);

        // Enhanced color calculation
        uint baseColor = _stormColors[poly.ColorIndex];
        uint polyColor = EnhanceColor(baseColor, poly.ExpansionFactor, volume, beat);

        // Draw polygon outline
        for (int i = 0; i < points.Length; i++)
        {
            int nextIndex = (i + 1) % points.Length;
            canvas.DrawLine(
                points[i].x, points[i].y,
                points[nextIndex].x, points[nextIndex].y,
                polyColor, 2f + poly.ExpansionFactor
            );
        }

        // Add inner glow for large polygons
        if (poly.ExpansionFactor > 1.5f)
        {
            RenderPolygonGlow(canvas, points, polyColor, poly.ExpansionFactor);
        }

        // Add center point for emphasis
        if (poly.ExpansionFactor > 1.0f)
        {
            float centerSize = 4f + poly.ExpansionFactor * 2f;
            canvas.FillCircle(poly.X, poly.Y, centerSize, polyColor);
        }
    }

    private (float x, float y)[] CalculatePolygonPoints(float centerX, float centerY, float radius, int sides, float rotation)
    {
        var points = new (float x, float y)[sides];

        for (int i = 0; i < sides; i++)
        {
            float angle = (i / (float)sides) * MathF.PI * 2 + rotation;
            points[i] = (
                centerX + MathF.Cos(angle) * radius,
                centerY + MathF.Sin(angle) * radius
            );
        }

        return points;
    }

    private void RenderPolygonGlow(ISkiaCanvas canvas, (float x, float y)[] points, uint color, float intensity)
    {
        // Create glow effect by drawing multiple layers
        for (int layer = 1; layer <= 3; layer++)
        {
            float glowSize = layer * 6f;
            float alpha = (int)(intensity * 60 / layer);
            uint glowColor = (color & 0x00FFFFFF) | ((uint)alpha << 24);

            // Draw glow outline
            for (int i = 0; i < points.Length; i++)
            {
                int nextIndex = (i + 1) % points.Length;
                canvas.DrawLine(
                    points[i].x - glowSize * 0.5f, points[i].y - glowSize * 0.5f,
                    points[nextIndex].x - glowSize * 0.5f, points[nextIndex].y - glowSize * 0.5f,
                    glowColor, glowSize
                );
            }
        }
    }

    private void RenderStormEffects(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Add lightning-like effects on beats
        if (f.Beat || f.Volume > 0.8f)
        {
            RenderLightningBolts(canvas, f.Volume);
        }

        // Add energy particles
        RenderEnergyParticles(canvas, f);

        // Add storm intensity indicator
        RenderStormMeter(canvas, f);
    }

    private void RenderLightningBolts(ISkiaCanvas canvas, float intensity)
    {
        int boltCount = (int)(intensity * 8);
        for (int i = 0; i < boltCount; i++)
        {
            float startX = (float)(_random.NextDouble() * _width);
            float startY = 0;
            float endX = startX + (float)(_random.NextDouble() - 0.5) * 200f;
            float endY = _height;

            uint boltColor = 0x80FFFFFF; // White with alpha
            canvas.DrawLine(startX, startY, endX, endY, boltColor, 3f);
        }
    }

    private void RenderEnergyParticles(ISkiaCanvas canvas, AudioFeatures f)
    {
        int particleCount = (int)(f.Volume * 30);
        float centerX = _width * 0.5f;
        float centerY = _height * 0.5f;

        for (int i = 0; i < particleCount; i++)
        {
            float angle = (float)(_random.NextDouble() * Math.PI * 2);
            float distance = (float)(_random.NextDouble() * Math.Min(_width, _height) * 0.4f);
            float size = 2f + (float)(_random.NextDouble() * 4f);

            float x = centerX + MathF.Cos(angle) * distance;
            float y = centerY + MathF.Sin(angle) * distance;

            // Add orbital motion
            float orbitAngle = angle + _time * 3f;
            x += MathF.Cos(orbitAngle) * 15f;
            y += MathF.Sin(orbitAngle) * 15f;

            uint particleColor = _stormColors[i % _stormColors.Length];
            particleColor = (particleColor & 0x00FFFFFF) | 0xA0u << 24; // Add alpha

            canvas.FillCircle(x, y, size, particleColor);
        }
    }

    private void RenderStormMeter(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Draw storm intensity meter at the top
        float meterY = 30;
        float meterWidth = _width - 60;
        float meterHeight = 6;

        // Background
        canvas.FillRect(30, meterY, meterWidth, meterHeight, 0xFF303030);

        // Energy levels
        float bassWidth = meterWidth * 0.4f * f.Bass;
        float midWidth = meterWidth * 0.4f * f.Mid;
        float trebleWidth = meterWidth * 0.2f * f.Treble;

        // Bass (red)
        canvas.FillRect(30, meterY, bassWidth, meterHeight, 0xFFFF4444);

        // Mid (yellow)
        canvas.FillRect(30 + bassWidth, meterY, midWidth, meterHeight, 0xFFFFFF44);

        // Treble (cyan)
        canvas.FillRect(30 + bassWidth + midWidth, meterY, trebleWidth, meterHeight, 0xFF44FFFF);
    }

    private uint CalculateStormBackground(float volume, float bass)
    {
        // Dynamic background based on storm intensity
        float intensity = (volume + bass) * 0.5f;

        if (intensity < 0.3f)
            return 0xFF0A0A15; // Very dark
        else if (intensity < 0.6f)
            return 0xFF151520; // Dark blue
        else
            return 0xFF202030; // Medium dark with blue tint
    }

    private uint EnhanceColor(uint baseColor, float expansionFactor, float volume, bool beat)
    {
        // Enhance color based on polygon state
        float brightness = 0.6f + expansionFactor * 0.4f + volume * 0.2f;

        if (beat)
            brightness += 0.3f;

        brightness = MathF.Min(1f, brightness);

        return AdjustBrightness(baseColor, brightness);
    }

    private uint AdjustBrightness(uint color, float factor)
    {
        byte r = (byte)((color >> 16) & 0xFF);
        byte g = (byte)((color >> 8) & 0xFF);
        byte b = (byte)(color & 0xFF);

        r = (byte)Math.Clamp(r * factor, 0, 255);
        g = (byte)Math.Clamp(g * factor, 0, 255);
        b = (byte)Math.Clamp(b * factor, 0, 255);

        return 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | b;
    }

    private class PolygonData
    {
        public int Sides;
        public float Radius;
        public float Rotation;
        public float ExpansionFactor;
        public float X, Y;
        public int ColorIndex;
        public float PulsePhase;
    }
}
