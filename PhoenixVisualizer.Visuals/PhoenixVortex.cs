using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Phoenix Vortex - Audio-reactive volumetric vortex with dynamic tendrils and energy bursts
/// Inspired by Windows Media Player's volume-reactive circles but with advanced 3D effects
/// </summary>
public sealed class PhoenixVortex : IVisualizerPlugin
{
    public string Id => "phoenix_vortex";
    public string DisplayName => "🌪️ Phoenix Vortex";

    private int _width, _height;
    private float _time;
    private readonly Random _random = new();

    // Vortex system constants
    private const int MAX_TENDRILS = 12;
    private const int TENDRIL_SEGMENTS = 32;
    private const float VORTEX_SPEED = 2.0f;
    private const float ENERGY_THRESHOLD = 0.7f;

    // Vortex state
    private float _coreRotation;
    private float _tendrilRotation;
    private float _energyLevel;
    private float _flashIntensity;
    private readonly float[] _tendrilPhases;
    private readonly float[] _tendrilEnergies;

    // Color system
    private readonly uint[] _vortexColors = new uint[]
    {
        0xFF1E3A8A, // Deep blue
        0xFF3B82F6, // Bright blue
        0xFF06B6D4, // Cyan
        0xFF10B981, // Emerald
        0xFFF59E0B, // Amber
        0xFFEF4444, // Red
        0xFFEC4899, // Pink
        0xFF8B5CF6, // Purple
        0xFFF97316, // Orange
        0xFF84CC16, // Lime
        0xFF6366F1, // Indigo
        0xFFF43F5E  // Rose
    };

    public PhoenixVortex()
    {
        _tendrilPhases = new float[MAX_TENDRILS];
        _tendrilEnergies = new float[MAX_TENDRILS];

        // Initialize tendrils with random phases
        for (int i = 0; i < MAX_TENDRILS; i++)
        {
            _tendrilPhases[i] = (float)(_random.NextDouble() * Math.PI * 2);
            _tendrilEnergies[i] = 0.5f;
        }
    }

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;
        _coreRotation = 0;
        _tendrilRotation = 0;
        _energyLevel = 0;
        _flashIntensity = 0;
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

        // Update vortex state
        UpdateVortex(f);

        // Create dynamic background based on energy
        uint bgColor = CalculateBackgroundColor(_energyLevel);
        canvas.Clear(bgColor);

        // Render vortex components
        RenderVortexCore(canvas, f);
        RenderTendrils(canvas, f);
        RenderEnergyBursts(canvas, f);
        RenderParticleField(canvas, f);
    }

    private void UpdateVortex(AudioFeatures f)
    {
        // Update core rotation
        _coreRotation += VORTEX_SPEED * 0.02f * (1f + f.Volume * 2f);
        _tendrilRotation += VORTEX_SPEED * 0.015f * (1f + f.Mid * 1.5f);

        // Update energy level with smoothing
        float targetEnergy = (f.Bass + f.Mid + f.Treble) / 3f;
        _energyLevel = _energyLevel * 0.9f + targetEnergy * 0.1f;

        // Update flash intensity
        if (f.Volume > ENERGY_THRESHOLD || f.Beat)
        {
            _flashIntensity = MathF.Min(1f, _flashIntensity + 0.3f);
        }
        else
        {
            _flashIntensity = MathF.Max(0, _flashIntensity - 0.05f);
        }

        // Update tendrils
        for (int i = 0; i < MAX_TENDRILS; i++)
        {
            // Update phase
            _tendrilPhases[i] += 0.05f * (1f + f.Treble * 0.5f);

            // Update energy based on frequency band
            float frequencyRatio = (float)i / MAX_TENDRILS;
            float bandEnergy = GetFrequencyBandEnergy(f, frequencyRatio);
            _tendrilEnergies[i] = _tendrilEnergies[i] * 0.8f + bandEnergy * 0.2f;
        }
    }

    private void RenderVortexCore(ISkiaCanvas canvas, AudioFeatures f)
    {
        float centerX = _width * 0.5f;
        float centerY = _height * 0.5f;

        // Calculate core size based on energy
        float baseRadius = Math.Min(_width, _height) * 0.08f;
        float coreRadius = baseRadius * (1f + _energyLevel * 2f);

        // Render multiple core layers
        for (int layer = 0; layer < 3; layer++)
        {
            float layerRadius = coreRadius * (1f - layer * 0.2f);
            float layerRotation = _coreRotation * (layer + 1) * 0.5f;

            uint coreColor = GetVortexColor(layer, _energyLevel, f.Volume);
            float alpha = 1f - layer * 0.3f;
            uint layerColor = (coreColor & 0x00FFFFFF) | ((uint)(alpha * 255) << 24);

            // Draw rotating core segments
            int segments = 16;
            for (int i = 0; i < segments; i++)
            {
                float angle1 = (i / (float)segments) * MathF.PI * 2 + layerRotation;
                float angle2 = ((i + 1) / (float)segments) * MathF.PI * 2 + layerRotation;

                float x1 = centerX + MathF.Cos(angle1) * layerRadius;
                float y1 = centerY + MathF.Sin(angle1) * layerRadius;
                float x2 = centerX + MathF.Cos(angle2) * layerRadius;
                float y2 = centerY + MathF.Sin(angle2) * layerRadius;

                canvas.DrawLine(x1, y1, x2, y2, layerColor, 3f);
            }
        }

        // Add core glow effect
        if (_flashIntensity > 0)
        {
            uint glowColor = 0x40FFFFFF; // White glow
            canvas.FillCircle(centerX, centerY, coreRadius * 1.5f, glowColor);
        }
    }

    private void RenderTendrils(ISkiaCanvas canvas, AudioFeatures f)
    {
        float centerX = _width * 0.5f;
        float centerY = _height * 0.5f;
        float maxRadius = Math.Min(_width, _height) * 0.4f;

        for (int i = 0; i < MAX_TENDRILS; i++)
        {
            float tendrilEnergy = _tendrilEnergies[i];
            if (tendrilEnergy < 0.1f) continue; // Skip inactive tendrils

            float baseAngle = (i / (float)MAX_TENDRILS) * MathF.PI * 2 + _tendrilRotation;
            uint tendrilColor = GetTendrilColor(i, tendrilEnergy);

            // Render tendril as a curved line
            float prevX = centerX;
            float prevY = centerY;

            for (int segment = 1; segment <= TENDRIL_SEGMENTS; segment++)
            {
                float t = segment / (float)TENDRIL_SEGMENTS;
                float radius = t * maxRadius * tendrilEnergy;

                // Add spiral and wave motion
                float angle = baseAngle + t * MathF.PI * 4 + _tendrilPhases[i];
                float waveOffset = MathF.Sin(t * MathF.PI * 6 + _time * 3f) * 20f * tendrilEnergy;

                float x = centerX + MathF.Cos(angle) * radius + waveOffset;
                float y = centerY + MathF.Sin(angle) * radius + waveOffset;

                // Ensure points stay within bounds
                x = MathF.Max(0, MathF.Min(_width, x));
                y = MathF.Max(0, MathF.Min(_height, y));

                float thickness = (3f - t * 2f) * tendrilEnergy;
                canvas.DrawLine(prevX, prevY, x, y, tendrilColor, thickness);

                prevX = x;
                prevY = y;
            }
        }
    }

    private void RenderEnergyBursts(ISkiaCanvas canvas, AudioFeatures f)
    {
        if (_flashIntensity < 0.1f) return;

        float centerX = _width * 0.5f;
        float centerY = _height * 0.5f;

        // Create radial burst lines
        int burstCount = (int)(_flashIntensity * 16);
        for (int i = 0; i < burstCount; i++)
        {
            float angle = (i / (float)burstCount) * MathF.PI * 2 + _time;
            float length = 100f + _flashIntensity * 200f;

            float endX = centerX + MathF.Cos(angle) * length;
            float endY = centerY + MathF.Sin(angle) * length;

            uint burstColor = 0x80FFFFFF; // White with alpha
            float thickness = 2f + _flashIntensity * 4f;

            canvas.DrawLine(centerX, centerY, endX, endY, burstColor, thickness);
        }
    }

    private void RenderParticleField(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Create floating particles around the vortex
        int particleCount = (int)(20 + f.Volume * 50);
        float centerX = _width * 0.5f;
        float centerY = _height * 0.5f;

        for (int i = 0; i < particleCount; i++)
        {
            float angle = (float)(_random.NextDouble() * Math.PI * 2);
            float distance = (float)(_random.NextDouble() * Math.Min(_width, _height) * 0.3f);
            float size = 2f + (float)(_random.NextDouble() * 3f);

            float x = centerX + MathF.Cos(angle) * distance;
            float y = centerY + MathF.Sin(angle) * distance;

            // Add orbital motion
            float orbitAngle = angle + _time * 2f;
            x += MathF.Cos(orbitAngle) * 10f;
            y += MathF.Sin(orbitAngle) * 10f;

            uint particleColor = _vortexColors[i % _vortexColors.Length];
            particleColor = (particleColor & 0x00FFFFFF) | (0x80u << 24); // Add alpha

            canvas.FillCircle(x, y, size, particleColor);
        }
    }

    private uint CalculateBackgroundColor(float energy)
    {
        // Create dynamic background based on energy level
        if (energy < 0.3f)
        {
            return 0xFF050510; // Very dark blue
        }
        else if (energy < 0.6f)
        {
            return 0xFF0A0A20; // Dark blue
        }
        else
        {
            return 0xFF101030; // Medium dark blue
        }
    }

    private uint GetVortexColor(int layer, float energy, float volume)
    {
        int colorIndex = (int)(energy * _vortexColors.Length);
        colorIndex = Math.Clamp(colorIndex, 0, _vortexColors.Length - 1);

        uint baseColor = _vortexColors[colorIndex];

        // Adjust brightness based on volume
        float brightness = 0.6f + volume * 0.4f;
        return AdjustBrightness(baseColor, brightness);
    }

    private uint GetTendrilColor(int tendrilIndex, float energy)
    {
        uint baseColor = _vortexColors[tendrilIndex % _vortexColors.Length];
        return AdjustBrightness(baseColor, 0.4f + energy * 0.6f);
    }

    private float GetFrequencyBandEnergy(AudioFeatures f, float frequencyRatio)
    {
        // Map frequency ratio to audio bands
        if (frequencyRatio < 0.3f)
            return f.Bass;
        else if (frequencyRatio < 0.7f)
            return f.Mid;
        else
            return f.Treble;
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
}
