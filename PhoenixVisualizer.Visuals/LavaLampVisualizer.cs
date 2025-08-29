using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Enhanced Lava Lamp visualizer with proper containment and realistic lava lamp appearance.
/// Features audio-reactive blob movement, lamp-shaped container, and proper rendering bounds.
/// </summary>
public sealed class LavaLampVisualizer : IVisualizerPlugin
{
    public string Id => "lava_lamp";
    public string DisplayName => "Lava Lamp";

    private sealed class Blob
    {
        public float X, Y;
        public float VX, VY;
        public float R;
        public uint Color;
        public float Density;
    }

    private readonly Random _rng = new();
    private Blob[] _blobs = Array.Empty<Blob>();
    private float _amp;
    private int _width, _height;
    private float _time;

    // Lamp container properties
    private float _lampBaseY;
    private float _lampWidth;
    private float _lampHeight;

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;

        // Set up lamp container dimensions (central area, not full screen)
        _lampWidth = Math.Min(width * 0.6f, height * 0.4f);
        _lampHeight = height * 0.7f;
        _lampBaseY = height * 0.15f;

        // Init blobs within lamp bounds
        int n = 6;
        _blobs = new Blob[n];
        for (int i = 0; i < n; i++)
        {
            _blobs[i] = new Blob
            {
                X = width * 0.5f + (float)(_rng.NextDouble() - 0.5) * _lampWidth * 0.8f,
                Y = _lampBaseY + _lampHeight * 0.2f + (float)_rng.NextDouble() * _lampHeight * 0.6f,
                VX = (float)((_rng.NextDouble() - 0.5) * 20),
                VY = (float)((_rng.NextDouble() - 0.5) * 15),
                R = _lampWidth * (0.08f + (float)_rng.NextDouble() * 0.08f),
                Density = 0.8f + (float)_rng.NextDouble() * 0.4f
            };

            // Assign colors based on density (hotter = more red, cooler = more orange)
            float hue = 20f + _blobs[i].Density * 40f; // Red to orange range
            _blobs[i].Color = HsvToRgb(hue, 0.9f, 0.8f);
        }
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

        // Clear with dark background
        canvas.Clear(0xFF0A0A0A);

        // Draw lamp container first
        DrawLampContainer(canvas);

        // Update amplitude from audio
        _amp = f.Volume;

        // Update and draw blobs within lamp bounds
        foreach (var b in _blobs)
        {
            // Audio-reactive speed and movement
            float speed = 0.8f + _amp * 2f;
            float bassInfluence = f.Bass * 0.5f;

            // Update position with proper bounds checking
            b.X += b.VX * 0.033f * speed;
            b.Y += b.VY * 0.033f * speed;

            // Add some wobble based on bass
            b.X += (float)Math.Sin(_time * 2 + b.Density * 5) * bassInfluence * 2;
            b.Y += (float)Math.Cos(_time * 1.8 + b.Density * 3) * bassInfluence * 1.5f;

            // Constrain blobs to lamp container with bounce
            float lampLeft = _width * 0.5f - _lampWidth * 0.45f;
            float lampRight = _width * 0.5f + _lampWidth * 0.45f;
            float lampTop = _lampBaseY + _lampHeight * 0.15f;
            float lampBottom = _lampBaseY + _lampHeight * 0.85f;

            if (b.X - b.R < lampLeft)
            {
                b.X = lampLeft + b.R;
                b.VX = Math.Abs(b.VX) * 0.8f; // Bounce with energy loss
            }
            else if (b.X + b.R > lampRight)
            {
                b.X = lampRight - b.R;
                b.VX = -Math.Abs(b.VX) * 0.8f;
            }

            if (b.Y - b.R < lampTop)
            {
                b.Y = lampTop + b.R;
                b.VY = Math.Abs(b.VY) * 0.8f;
            }
            else if (b.Y + b.R > lampBottom)
            {
                b.Y = lampBottom - b.R;
                b.VY = -Math.Abs(b.VY) * 0.8f;
            }

            // Apply gravity and buoyancy based on density
            b.VY += (b.Density - 1.0f) * 0.1f; // Density affects floating/sinking

            // Draw blob only if it's within lamp bounds
            if (b.X >= lampLeft && b.X <= lampRight && b.Y >= lampTop && b.Y <= lampBottom)
            {
                DrawBlob(canvas, b, f);
            }
        }

        // Draw lamp cap/lighting effect
        DrawLampCap(canvas);
    }

    private void DrawLampContainer(ISkiaCanvas canvas)
    {
        float centerX = _width * 0.5f;
        float lampTop = _lampBaseY;
        float lampBottom = _lampBaseY + _lampHeight;

        // Lamp glass (subtle outline)
        uint glassColor = 0x44FFFFFF;
        canvas.DrawLine(centerX - _lampWidth * 0.5f, lampTop, centerX - _lampWidth * 0.5f, lampBottom, glassColor, 2f);
        canvas.DrawLine(centerX + _lampWidth * 0.5f, lampTop, centerX + _lampWidth * 0.5f, lampBottom, glassColor, 2f);
        canvas.DrawLine(centerX - _lampWidth * 0.5f, lampBottom, centerX + _lampWidth * 0.5f, lampBottom, glassColor, 2f);

        // Lamp base
        uint baseColor = 0xFF2A2A2A;
        float baseHeight = 20f;
        canvas.FillRect(centerX - _lampWidth * 0.6f, lampBottom, _lampWidth * 1.2f, baseHeight, baseColor);
    }

    private void DrawLampCap(ISkiaCanvas canvas)
    {
        float centerX = _width * 0.5f;
        float capY = _lampBaseY - 10f;
        float capWidth = _lampWidth * 0.7f;
        float capHeight = 15f;

        // Lamp cap with lighting effect
        uint capColor = 0xFF1A1A1A;
        canvas.FillRect(centerX - capWidth * 0.5f, capY, capWidth, capHeight, capColor);

        // Light glow from top
        uint glowColor = HsvToRgb(_time * 30f % 360f, 0.3f, 0.8f);
        canvas.FillCircle(centerX, capY + capHeight * 0.5f, capWidth * 0.3f, glowColor);
    }

    private void DrawBlob(ISkiaCanvas canvas, Blob blob, AudioFeatures f)
    {
        // Enhanced blob rendering with multiple layers and audio reactivity
        int layers = 6;
        float beatPulse = f.Beat ? 1.2f : 1.0f;

        for (int i = 0; i < layers; i++)
        {
            float radius = blob.R * (1.0f - i * 0.12f) * beatPulse;
            float alpha = (1.0f - i * 0.15f) * 0.9f;

            // Use blob's assigned color with audio-reactive brightness
            uint baseColor = blob.Color;
            float brightness = 0.7f + f.Volume * 0.3f + (f.Beat ? 0.2f : 0f);
            uint color = AdjustBrightness(baseColor, brightness * alpha);

            canvas.FillCircle(blob.X, blob.Y, radius, color);
        }

        // Add inner glow for hot blobs
        if (blob.Density > 1.0f)
        {
            uint glowColor = AdjustBrightness(blob.Color, 1.5f);
            canvas.FillCircle(blob.X, blob.Y, blob.R * 0.6f, glowColor);
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
        return (uint)(0xFF000000 | ((uint)R << 16) | ((uint)G << 8) | B);
    }
}
