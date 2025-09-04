using System;
using System.Collections.Generic;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Windows 95/98 inspired 3D Twister - Audio-reactive spinning tornado funnel
/// Features dynamic funnel width/circumference that responds to audio frequencies
/// FIXED: Now fluidly pulses like a sideways VU meter with more dynamic colors
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

    // FIXED: More dynamic colors for VU meter-style visualization
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
        0xFF60A0C0, // Sky blue
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

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        canvas.Clear(0xFF101010);

        int bars = 48;
        float w = _width / (float)bars;
        for (int i = 0; i < bars; i++)
        {
            float t = i / (float)(bars - 1);
            if (features.Fft == null || features.Fft.Length == 0) break;
            int bin = Math.Min((int)(t * (features.Fft.Length - 1)), features.Fft.Length - 1);
            float v = MathF.Abs(features.Fft[bin]);
            // soft compress to [0..1]
            v = v / (1f + v);
            float h = MathF.Min(_height * 0.9f, v * _height * 0.9f);
            float x = i * w;
            float y = _height * 0.5f - h * 0.5f;

            float hue = (t * 360f + _time * 25f) % 360f;
            uint color = HsvToRgb(hue, 0.85f, 0.95f);
            canvas.FillRect(x, y, w * 0.85f, h, color);
        }
    }

    private void UpdateTwister(AudioFeatures f)
    {
        var energy = f.Energy;
        var bass = f.Bass;
        var mid = f.Mid;
        var treble = f.Treble;
        var beat = f.Beat;
        var volume = f.Volume;
        
        // FIXED: VU meter-style pulsing updates
        for (int layer = 0; layer < LAYERS; layer++)
        {
            float layerRatio = (float)layer / (LAYERS - 1);

            // FIXED: VU meter-style frequency distribution
            // Bass controls lower layers (wider base) - like VU meter bass
            float bassInfluence = bass * (1f - layerRatio) * 3f;
            // Mid controls middle layers - like VU meter mid
            float midInfluence = mid * (float)(1f - Math.Abs(layerRatio - 0.5f) * 2f) * 2.5f;
            // Treble controls upper layers (narrower top) - like VU meter treble
            float trebleInfluence = treble * layerRatio * 2f;

            // FIXED: VU meter-style radius pulsing
            var baseRadius = BASE_RADIUS;
            var bassRadius = bassInfluence * MAX_RADIUS * 0.8f;
            var midRadius = midInfluence * MAX_RADIUS * 0.6f;
            var trebleRadius = trebleInfluence * MAX_RADIUS * 0.4f;
            var energyRadius = energy * MAX_RADIUS * 0.3f;
            var beatRadius = beat ? MAX_RADIUS * 0.2f : 0f;
            
            float targetRadius = baseRadius + bassRadius + midRadius + trebleRadius + energyRadius + beatRadius;

            // FIXED: Smooth VU meter-style transitions
            var smoothFactor = 0.08f + energy * 0.05f + (beat ? 0.1f : 0f);
            _layerRadii[layer] += (targetRadius - _layerRadii[layer]) * smoothFactor;
            _layerRadii[layer] = Math.Max(BASE_RADIUS * 0.1f, Math.Min(MAX_RADIUS * 1.2f, _layerRadii[layer]));

            // FIXED: Enhanced audio-reactive twist (VU meter movement)
            var baseTwist = TWIST_SPEED * 0.01f;
            var volumeTwist = volume * 3f;
            var bassTwist = bass * 2f;
            var midTwist = mid * 1.5f;
            var trebleTwist = treble * 2.5f;
            var energyTwist = energy * 2f;
            var beatTwist = beat ? 4f : 0f;
            
            _layerTwist[layer] += (baseTwist + volumeTwist + bassTwist + midTwist + trebleTwist + energyTwist + beatTwist) * 
                                (1f + layerRatio * 0.2f);

            // FIXED: VU meter-style brightness pulsing
            var baseBrightness = 0.3f;
            var bassBrightness = bass * 0.6f;
            var midBrightness = mid * 0.5f;
            var trebleBrightness = treble * 0.4f;
            var energyBrightness = energy * 0.7f;
            var beatBrightness = beat ? 0.8f : 0f;
            var layerBrightness = layerRatio * 0.3f;
            
            _layerBrightness[layer] = baseBrightness + bassBrightness + midBrightness + trebleBrightness + 
                                    energyBrightness + beatBrightness + layerBrightness;
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

        // FIXED: Dynamic audio-reactive color selection
        uint baseColor;
        var energy = f.Energy;
        var bass = f.Bass;
        var treble = f.Treble;
        var beat = f.Beat;
        
        if (beat)
            baseColor = 0xFFFFFF00; // Bright yellow on beat
        else if (bass > 0.5f)
            baseColor = 0xFFFF0000; // Red for bass
        else if (treble > 0.4f)
            baseColor = 0xFF00FFFF; // Cyan for treble
        else if (energy > 0.6f)
            baseColor = 0xFFFF00FF; // Magenta for energy
        else
            baseColor = _twisterColors[layer % _twisterColors.Length];
            
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
        var energy = f.Energy;
        var bass = f.Bass;
        var treble = f.Treble;
        var beat = f.Beat;
        var volume = f.Volume;
        
        // FIXED: VU meter-style particle effects
        int baseParticleCount = 30;
        var energyParticles = (int)(energy * 50);
        var bassParticles = (int)(bass * 40);
        var trebleParticles = (int)(treble * 30);
        var beatParticles = beat ? 20 : 0;
        int particleCount = baseParticleCount + energyParticles + bassParticles + trebleParticles + beatParticles;

        for (int i = 0; i < particleCount; i++)
        {
            float angle = _time * 3f + i * 0.3f;
            float radius = 200f + (float)Math.Sin(_time * 2f + i * 0.1f) * 100f;
            float height = (float)(i * _height / particleCount - _height * 0.5f);

            // FIXED: Audio-reactive spiral motion
            var bassMotion = bass * 0.5f;
            var trebleMotion = treble * 0.3f;
            var energyMotion = energy * 0.4f;
            
            // Spiral motion with audio influence
            float x = _width * 0.5f + (float)Math.Cos(angle + bassMotion) * radius;
            float y = _height * 0.5f + height + (float)Math.Sin(angle * 1.5f + trebleMotion) * 50f;

            // FIXED: Dynamic particle colors
            uint particleColor;
            if (beat && i < 10)
                particleColor = 0xFFFFFF00; // Bright yellow for beat particles
            else if (bass > 0.4f && i % 3 == 0)
                particleColor = 0xFFFF0000; // Red for bass particles
            else if (treble > 0.3f && i % 2 == 0)
                particleColor = 0xFF00FFFF; // Cyan for treble particles
            else if (energy > 0.5f)
                particleColor = 0xFFFF00FF; // Magenta for energy particles
            else
                particleColor = _twisterColors[i % _twisterColors.Length];
                
            float alpha = (float)_random.NextDouble() * 0.8f;
            particleColor = (uint)((uint)(alpha * 255) << 24 | (particleColor & 0x00FFFFFF));

            // FIXED: Audio-reactive particle size
            var baseSize = 2f;
            var energySize = energy * 3f;
            var beatSize = beat ? 4f : 0f;
            var particleSize = baseSize + energySize + beatSize;
            
            canvas.FillCircle(x, y, particleSize, particleColor);
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
