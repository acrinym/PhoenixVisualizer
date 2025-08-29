using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Enhanced audio-reactive raver hoop with multiple concentric rings, particle effects, and dynamic visuals.
/// Features bass-driven pulsing, midrange color cycling, and treble sparkles.
/// </summary>
public sealed class RaverHoop : IVisualizerPlugin
{
    public string Id => "raver_hoop";
    public string DisplayName => "Raver Hoop";

    private int _w, _h;
    private float _time;
    private readonly Random _random = new();

    // Multiple hoop system
    private const int MAX_HOOPS = 5;
    private readonly float[] _hoopAngles = new float[MAX_HOOPS];
    private readonly float[] _hoopRadii = new float[MAX_HOOPS];
    private readonly float[] _hoopSpeeds = new float[MAX_HOOPS];

    // Particle system for sparkles
    private const int MAX_PARTICLES = 50;
    private readonly float[] _particleX = new float[MAX_PARTICLES];
    private readonly float[] _particleY = new float[MAX_PARTICLES];
    private readonly float[] _particleLife = new float[MAX_PARTICLES];
    private int _nextParticle;

    public void Initialize(int width, int height)
    {
        _w = width;
        _h = height;

        // Initialize multiple hoops with different properties
        for (int i = 0; i < MAX_HOOPS; i++)
        {
            _hoopAngles[i] = _random.NextSingle() * MathF.PI * 2;
            _hoopRadii[i] = (0.2f + i * 0.15f) * Math.Min(_w, _h) * 0.4f;
            _hoopSpeeds[i] = (0.5f + _random.NextSingle() * 0.5f) * (i % 2 == 0 ? 1 : -1);
        }

        // Initialize particles
        for (int i = 0; i < MAX_PARTICLES; i++)
        {
            _particleLife[i] = 0;
        }
    }

    public void Resize(int width, int height)
    {
        _w = width;
        _h = height;
    }

    public void Dispose() { }

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        _time += 0.016f;

        // Dynamic background based on bass
        float bgIntensity = Math.Clamp(f.Bass * 0.3f, 0, 0.1f);
        uint bgColor = HsvToRgb((_time * 30f) % 360f, 0.3f, bgIntensity);
        canvas.Clear(bgColor);

        float cx = _w * 0.5f, cy = _h * 0.5f;

        // Update hoop positions with audio reactivity
        for (int i = 0; i < MAX_HOOPS; i++)
        {
            float baseSpeed = _hoopSpeeds[i] * (0.02f + f.Mid * 0.1f);
            float beatBoost = f.Beat ? 0.15f : 0f;
            _hoopAngles[i] += baseSpeed + beatBoost;
        }

        // Render multiple concentric hoops
        for (int h = 0; h < MAX_HOOPS; h++)
        {
            RenderHoop(canvas, cx, cy, h, f);
        }

        // Update and render particles
        UpdateParticles(f);
        RenderParticles(canvas);

        // Spawn particles based on treble
        if (f.Treble > 0.3f && _random.NextSingle() < f.Treble * 0.5f)
        {
            SpawnParticle(cx, cy, f);
        }

        // Center pulsing core
        float coreRadius = 15f + f.Volume * 20f;
        float coreHue = (_time * 60f + f.Bass * 120f) % 360f;
        uint coreColor = HsvToRgb(coreHue, 0.8f, 0.9f);
        canvas.FillCircle(cx, cy, coreRadius, coreColor);

        // Core glow effect
        uint glowColor = HsvToRgb(coreHue, 0.6f, 0.6f);
        canvas.FillCircle(cx, cy, coreRadius * 1.5f, glowColor);
    }

    private void RenderHoop(ISkiaCanvas canvas, float cx, float cy, int hoopIndex, AudioFeatures f)
    {
        float radius = _hoopRadii[hoopIndex] * (0.8f + f.Bass * 0.4f);
        float angle = _hoopAngles[hoopIndex];

        int segments = 32;
        float segmentAngle = MathF.PI * 2 / segments;

        // Render trail effect
        int trails = Math.Max(8, (int)(f.Volume * 20));
        for (int t = 0; t < trails; t++)
        {
            float trailAngle = angle - t * (0.05f + f.Treble * 0.1f);
            float alpha = MathF.Max(0.1f, 1f - t / (float)trails);
            float trailRadius = radius * (0.9f + t * 0.01f);

            for (int s = 0; s < segments; s++)
            {
                float a1 = trailAngle + s * segmentAngle;
                float a2 = trailAngle + (s + 1) * segmentAngle;

                float x1 = cx + MathF.Cos(a1) * trailRadius;
                float y1 = cy + MathF.Sin(a1) * trailRadius;
                float x2 = cx + MathF.Cos(a2) * trailRadius;
                float y2 = cy + MathF.Sin(a2) * trailRadius;

                // Color based on position and audio
                float hue = (a1 * 180f / MathF.PI + _time * 50f + hoopIndex * 60f) % 360f;
                uint color = HsvToRgb(hue, 0.9f, alpha * (0.6f + f.Mid * 0.4f));

                canvas.DrawLine(x1, y1, x2, y2, color, 3f);
            }
        }

        // Add hoop highlights
        for (int i = 0; i < 8; i++)
        {
            float highlightAngle = angle + i * MathF.PI * 2 / 8;
            float hx = cx + MathF.Cos(highlightAngle) * radius;
            float hy = cy + MathF.Sin(highlightAngle) * radius;
            uint highlightColor = HsvToRgb((_time * 100f + i * 45f) % 360f, 1f, 1f);
            canvas.FillCircle(hx, hy, 4f, highlightColor);
        }
    }

    private void SpawnParticle(float cx, float cy, AudioFeatures f)
    {
        _particleX[_nextParticle] = cx + (_random.NextSingle() - 0.5f) * 100f;
        _particleY[_nextParticle] = cy + (_random.NextSingle() - 0.5f) * 100f;
        _particleLife[_nextParticle] = 1.0f;
        _nextParticle = (_nextParticle + 1) % MAX_PARTICLES;
    }

    private void UpdateParticles(AudioFeatures f)
    {
        for (int i = 0; i < MAX_PARTICLES; i++)
        {
            if (_particleLife[i] > 0)
            {
                _particleLife[i] -= 0.02f + f.Treble * 0.05f;

                // Move particles outward from center
                float dx = _particleX[i] - _w * 0.5f;
                float dy = _particleY[i] - _h * 0.5f;
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                if (dist > 0)
                {
                    _particleX[i] += dx / dist * 2f;
                    _particleY[i] += dy / dist * 2f;
                }
            }
        }
    }

    private void RenderParticles(ISkiaCanvas canvas)
    {
        for (int i = 0; i < MAX_PARTICLES; i++)
        {
            if (_particleLife[i] > 0)
            {
                float size = _particleLife[i] * 6f;
                uint color = HsvToRgb((_time * 200f + i * 30f) % 360f, 1f, _particleLife[i]);
                canvas.FillCircle(_particleX[i], _particleY[i], size, color);
            }
        }
    }

    private static uint HsvToRgb(float h, float s, float v)
    {
        float c = v * s, x = c * (1f - Math.Abs((h / 60f) % 2f - 1f)), m = v - c;
        float r, g, b;
        if (h < 60f) { r = c; g = x; b = 0f; }
        else if (h < 120f) { r = x; g = c; b = 0f; }
        else if (h < 180f) { r = 0f; g = c; b = x; }
        else if (h < 240f) { r = 0f; g = x; b = c; }
        else if (h < 300f) { r = x; g = 0f; b = c; }
        else { r = c; g = 0f; b = x; }
        byte R = (byte)((r + m) * 255f); byte G = (byte)((g + m) * 255f); byte B = (byte)((b + m) * 255f);
        return (uint)(0xFF << 24 | R << 16 | G << 8 | B);
    }
}
