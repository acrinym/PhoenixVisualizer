using System;
using System.Collections.Generic;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Windows 95/98 inspired 3D Twister - Audio-reactive spinning tornado funnel
/// Features dynamic funnel width/circumference that responds to audio frequencies
/// </summary>
public sealed class Win953DTwister : IVisualizerPlugin
{
    public string Id => "win95_3d_twister";
    public string DisplayName => "🌀 Win95 3D Twister";

    private int _width, _height;
    private float _time;
    private Random _random = new();

    // Twister system constants
    private const int LAYERS = 24;
    private const int SEGMENTS = 16;
    private const float BASE_RADIUS = 0.5f;
    private const float MAX_RADIUS = 3.0f;
    private const float LAYER_HEIGHT = 0.8f;
    private const float TWIST_SPEED = 2.0f;

    // Audio-reactive parameters
    private float[] _layerRadii = new float[LAYERS];
    private float[] _layerTwist = new float[LAYERS];
    private float[] _layerBrightness = new float[LAYERS];

    // Colors inspired by Windows 95 tornado effects
    private readonly uint[] _twisterColors = new uint[]
    {
        0xFF404080, // Dark blue
        0xFF6060A0, // Medium blue
        0xFF8080C0, // Light blue
        0xFFA06060, // Dusty rose
        0xFFC08080, // Light rose
        0xFF80A060, // Sage green
        0xFFA0C080, // Light green
        0xFF8060A0, // Purple
        0xFFA080C0, // Light purple
        0xFFC0A060, // Gold
        0xFFE0C080, // Light gold
        0xFF60A0C0  // Sky blue
    };

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;

        // Initialize layer parameters
        for (int i = 0; i < LAYERS; i++)
        {
            _layerRadii[i] = BASE_RADIUS;
            _layerTwist[i] = 0;
            _layerBrightness[i] = 0.5f;
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

        // Update twister based on audio
        UpdateTwister(f);

        // Clear with dark background (stormy sky)
        canvas.Clear(0xFF0A0A15);

        // Render the 3D twister
        Render3DTwister(canvas, f);
    }

    private void UpdateTwister(AudioFeatures f)
    {
        // Audio-reactive layer updates
        for (int layer = 0; layer < LAYERS; layer++)
        {
            float layerRatio = (float)layer / (LAYERS - 1);

            // Bass controls lower layers (wider base)
            float bassInfluence = f.Bass * (1f - layerRatio) * 2f;
            // Mid controls middle layers
            float midInfluence = f.Mid * (float)(1f - Math.Abs(layerRatio - 0.5f) * 2f);
            // Treble controls upper layers (narrower top)
            float trebleInfluence = f.Treble * layerRatio * 1.5f;

            // Update radius based on audio
            float targetRadius = BASE_RADIUS + bassInfluence * MAX_RADIUS * 0.7f +
                               midInfluence * MAX_RADIUS * 0.5f +
                               trebleInfluence * MAX_RADIUS * 0.3f;

            _layerRadii[layer] += (targetRadius - _layerRadii[layer]) * 0.05f;
            _layerRadii[layer] = Math.Max(BASE_RADIUS * 0.2f, Math.Min(MAX_RADIUS, _layerRadii[layer]));

            // Update twist based on audio and layer
            _layerTwist[layer] += TWIST_SPEED * 0.01f * (1f + f.Volume * 2f) +
                                layerRatio * 0.1f * (1f + trebleInfluence);

            // Update brightness based on audio energy
            float audioEnergy = (f.Bass + f.Mid + f.Treble) / 3f;
            _layerBrightness[layer] = 0.3f + audioEnergy * 0.7f + layerRatio * 0.2f;
        }
    }

    private void Render3DTwister(ISkiaCanvas canvas, AudioFeatures f)
    {
        float centerX = _width * 0.5f;
        float centerY = _height * 0.5f;

        // 3D perspective parameters
        float fov = 65f * (float)(Math.PI / 180f);
        float near = 0.1f;
        float far = 50f;

        // Render layers from back to front for proper depth
        for (int layer = LAYERS - 1; layer >= 0; layer--)
        {
            RenderTwisterLayer(canvas, layer, centerX, centerY, fov, near, far, f);
        }

        // Add particle effects around the twister
        RenderParticleEffects(canvas, f);
    }

    private void RenderTwisterLayer(ISkiaCanvas canvas, int layer, float centerX, float centerY,
                                  float fov, float near, float far, AudioFeatures f)
    {
        float layerRatio = (float)layer / (LAYERS - 1);
        float yPosition = -LAYER_HEIGHT * (layerRatio - 0.5f) * 8f;
        float radius = _layerRadii[layer];
        float twist = _layerTwist[layer];
        float brightness = _layerBrightness[layer];

        // Audio-reactive color
        uint baseColor = _twisterColors[layer % _twisterColors.Length];
        uint layerColor = AdjustBrightness(baseColor, brightness);

        // Create vertices for this layer
        var vertices = new (float x, float y, float z)[SEGMENTS];

        for (int seg = 0; seg < SEGMENTS; seg++)
        {
            float angle = (float)(seg * 2 * Math.PI / SEGMENTS) + twist + (float)(layerRatio * Math.PI);
            float x = (float)(Math.Cos(angle) * radius * (1f + Math.Sin(_time * 2f + layerRatio * 4f) * 0.2f));
            float z = (float)(Math.Sin(angle) * radius * (1f + Math.Cos(_time * 1.5f + layerRatio * 3f) * 0.2f));

            vertices[seg] = (x, yPosition, z);
        }

        // Render the layer as connected segments
        for (int seg = 0; seg < SEGMENTS; seg++)
        {
            var currentVertex = vertices[seg];
            var nextVertex = vertices[(seg + 1) % SEGMENTS];

            // Project 3D points to 2D screen coordinates
            var p1 = Project3D(currentVertex.x, currentVertex.y, currentVertex.z, centerX, centerY, fov, near, far);
            var p2 = Project3D(nextVertex.x, nextVertex.y, nextVertex.z, centerX, centerY, fov, near, far);

            // Also connect to next layer if not the top layer
            if (layer < LAYERS - 1)
            {
                var nextLayerVertex = vertices[seg];
                var nextLayerY = -LAYER_HEIGHT * ((float)(layer + 1) / (LAYERS - 1) - 0.5f) * 8f;
                nextLayerVertex.y = nextLayerY;

                var p3 = Project3D(nextLayerVertex.x, nextLayerVertex.y, nextLayerVertex.z, centerX, centerY, fov, near, far);

                // Draw vertical connection to next layer
                if (p1.z > near && p3.z > near && p1.z < far && p3.z < far)
                {
                    float alpha = Math.Max(0.3f, 1f - (p1.z + p3.z) / (2f * far));
                    uint fadedColor = (uint)((uint)(alpha * 255) << 24 | (layerColor & 0x00FFFFFF));
                    canvas.DrawLine(p1.x, p1.y, p3.x, p3.y, fadedColor, 2f);
                }
            }

            // Draw circumferential connections
            if (p1.z > near && p2.z > near && p1.z < far && p2.z < far)
            {
                // Distance-based alpha and thickness
                float avgZ = (p1.z + p2.z) / 2f;
                float alpha = Math.Max(0.4f, 1f - avgZ / far);
                float thickness = 2f + radius * 2f;

                uint fadedColor = (uint)((uint)(alpha * 255) << 24 | (layerColor & 0x00FFFFFF));
                canvas.DrawLine(p1.x, p1.y, p2.x, p2.y, fadedColor, thickness);
            }
        }

        // Render layer center point for extra visual interest
        var centerPoint = Project3D(0, yPosition, 0, centerX, centerY, fov, near, far);
        if (centerPoint.z > near && centerPoint.z < far)
        {
            float alpha = Math.Max(0.5f, 1f - centerPoint.z / far);
            uint centerColor = (uint)((uint)(alpha * 255) << 24 | 0x00FFFFFF);
            canvas.FillCircle(centerPoint.x, centerPoint.y, 3f + radius * 2f, centerColor);
        }
    }

    private void RenderParticleEffects(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Add swirling particles around the twister
        int particleCount = (int)(30 + f.Volume * 100);

        for (int i = 0; i < particleCount; i++)
        {
            float angle = _time * 3f + i * 0.3f;
            float radius = 200f + (float)Math.Sin(_time * 2f + i * 0.1f) * 100f;
            float height = (float)(i * _height / particleCount - _height * 0.5f);

            // Spiral motion
            float x = _width * 0.5f + (float)Math.Cos(angle) * radius;
            float y = _height * 0.5f + height + (float)Math.Sin(angle * 1.5f) * 50f;

            float alpha = (float)_random.NextDouble() * 0.8f;
            uint particleColor = _twisterColors[i % _twisterColors.Length];
            particleColor = (uint)((uint)(alpha * 255) << 24 | (particleColor & 0x00FFFFFF));

            canvas.FillCircle(x, y, 2f, particleColor);
        }

        // Add lightning-like effects when beat is detected
        if (f.Beat)
        {
            RenderLightningEffect(canvas, f);
        }
    }

    private void RenderLightningEffect(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Create lightning-like streaks emanating from the twister
        int streakCount = _random.Next(3, 8);

        for (int i = 0; i < streakCount; i++)
        {
            float startAngle = (float)(_random.NextDouble() * Math.PI * 2);
            float startRadius = 50f + (float)_random.NextDouble() * 100f;
            float endRadius = startRadius + 100f + (float)_random.NextDouble() * 200f;

            float startX = _width * 0.5f + (float)Math.Cos(startAngle) * startRadius;
            float startY = _height * 0.5f + (float)Math.Sin(startAngle) * startRadius;

            float endX = _width * 0.5f + (float)Math.Cos(startAngle + 0.5f) * endRadius;
            float endY = _height * 0.5f + (float)Math.Sin(startAngle + 0.5f) * endRadius;

            uint lightningColor = 0x80FFFFFF; // Bright white with alpha
            canvas.DrawLine(startX, startY, endX, endY, lightningColor, 3f);
        }
    }

    private (float x, float y, float z) Project3D(float worldX, float worldY, float worldZ,
                                                 float centerX, float centerY, float fov, float near, float far)
    {
        // Fixed camera position looking at the twister center
        float x = worldX;
        float y = worldY;
        float z = worldZ + 8f; // Push back from camera (reduced from 10f)

        // Perspective projection
        if (z <= near) z = near + 0.1f;

        float screenX = centerX + (x / z) * (centerX / (float)Math.Tan(fov * 0.5));
        float screenY = centerY + (y / z) * (centerY / (float)Math.Tan(fov * 0.5));

        return (screenX, screenY, z);
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
}
