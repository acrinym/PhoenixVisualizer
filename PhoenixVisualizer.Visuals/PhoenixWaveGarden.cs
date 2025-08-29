using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Phoenix Wave Garden - Circular wave patterns with particle systems and fluid animations
/// Inspired by Windows Media Player's Visualizer5 but with enhanced wave mechanics
/// </summary>
public sealed class PhoenixWaveGarden : IVisualizerPlugin
{
    public string Id => "phoenix_wave_garden";
    public string DisplayName => "🌊 Phoenix Wave Garden";

    private int _width, _height;
    private float _time;
    private readonly Random _random = new();

    // Wave system constants
    private const int WAVE_COUNT = 16;
    private const int PARTICLE_COUNT = 200;
    private const float WAVE_SPEED = 0.05f;
    private const float PARTICLE_SPEED = 1.5f;

    // Wave state
    private readonly WaveData[] _waves;
    private readonly ParticleData[] _particles;
    private float _globalPhase;
    private float _gardenEnergy;

    // Color system
    private readonly uint[] _waveColors = new uint[]
    {
        0xFF0066CC, // Ocean Blue
        0xFF0099FF, // Sky Blue
        0xFF33CCFF, // Light Blue
        0xFF66FFFF, // Cyan
        0xFF99FFFF, // Pale Cyan
        0xFFCCFFFF, // Very Light Cyan
        0xFFFF99CC, // Pale Pink
        0xFFFF66AA, // Light Pink
        0xFFFF3399, // Hot Pink
        0xFFCC0099, // Magenta
        0xFF990066, // Dark Magenta
        0xFF660033, // Deep Magenta
        0xFFCC99FF, // Light Purple
        0xFF9966CC, // Purple
        0xFF663399, // Deep Purple
        0xFF330066, // Dark Purple
    };

    public PhoenixWaveGarden()
    {
        _waves = new WaveData[WAVE_COUNT];
        _particles = new ParticleData[PARTICLE_COUNT];

        InitializeWaves();
        InitializeParticles();
    }

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;
        _globalPhase = 0;
        _gardenEnergy = 0;
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

        // Update garden state
        UpdateGardenState(f);

        // Create dynamic ocean background
        uint bgColor = CalculateOceanBackground(_gardenEnergy);
        canvas.Clear(bgColor);

        // Render wave system
        RenderWaves(canvas, f);

        // Render particle system
        RenderParticles(canvas, f);

        // Add garden effects
        RenderGardenEffects(canvas, f);
    }

    private void InitializeWaves()
    {
        for (int i = 0; i < WAVE_COUNT; i++)
        {
            _waves[i] = new WaveData();
        }
        ResetWaves();
    }

    private void InitializeParticles()
    {
        for (int i = 0; i < PARTICLE_COUNT; i++)
        {
            _particles[i] = new ParticleData();
        }
        ResetParticles();
    }

    private void ResetWaves()
    {
        float centerX = _width * 0.5f;
        float centerY = _height * 0.5f;

        for (int i = 0; i < WAVE_COUNT; i++)
        {
            var wave = _waves[i];
            wave.Radius = 20f + i * 15f;
            wave.Phase = (float)(_random.NextDouble() * Math.PI * 2);
            wave.Amplitude = 10f + (float)(_random.NextDouble() * 20f);
            wave.Frequency = 2f + (float)(_random.NextDouble() * 4f);
            wave.ColorIndex = i % _waveColors.Length;
            wave.PulseOffset = (float)(_random.NextDouble() * Math.PI);
        }
    }

    private void ResetParticles()
    {
        for (int i = 0; i < PARTICLE_COUNT; i++)
        {
            var particle = _particles[i];
            particle.X = (float)(_random.NextDouble() * _width);
            particle.Y = (float)(_random.NextDouble() * _height);
            particle.VelocityX = (float)((_random.NextDouble() - 0.5) * PARTICLE_SPEED);
            particle.VelocityY = (float)((_random.NextDouble() - 0.5) * PARTICLE_SPEED);
            particle.Size = 2f + (float)(_random.NextDouble() * 4f);
            particle.Life = (float)(_random.NextDouble() * 100f);
            particle.MaxLife = 100f;
            particle.ColorIndex = i % _waveColors.Length;
        }
    }

    private void UpdateGardenState(AudioFeatures f)
    {
        // Update global phase
        _globalPhase += WAVE_SPEED * (1f + f.Volume * 0.5f);

        // Update garden energy
        float targetEnergy = (f.Bass + f.Mid + f.Treble) / 3f;
        _gardenEnergy = _gardenEnergy * 0.95f + targetEnergy * 0.05f;

        // Update waves
        for (int i = 0; i < WAVE_COUNT; i++)
        {
            var wave = _waves[i];
            wave.Phase += WAVE_SPEED * (0.8f + f.Treble * 0.4f);

            // Update amplitude based on frequency band
            float frequencyRatio = (float)i / WAVE_COUNT;
            float bandEnergy = GetFrequencyBandEnergy(f, frequencyRatio);
            wave.Amplitude = wave.Amplitude * 0.9f + (bandEnergy * 30f) * 0.1f;
        }

        // Update particles
        UpdateParticles(f);
    }

    private void UpdateParticles(AudioFeatures f)
    {
        float centerX = _width * 0.5f;
        float centerY = _height * 0.5f;

        for (int i = 0; i < PARTICLE_COUNT; i++)
        {
            var particle = _particles[i];

            // Update position
            particle.X += particle.VelocityX;
            particle.Y += particle.VelocityY;

            // Apply wave influence
            float waveInfluence = CalculateWaveInfluence(particle.X, particle.Y, f);
            particle.VelocityX += waveInfluence * 0.1f;
            particle.VelocityY += waveInfluence * 0.05f;

            // Update life
            particle.Life -= 0.5f;

            // Wrap around screen
            if (particle.X < 0) particle.X = _width;
            if (particle.X > _width) particle.X = 0;
            if (particle.Y < 0) particle.Y = _height;
            if (particle.Y > _height) particle.Y = 0;

            // Respawn dead particles
            if (particle.Life <= 0)
            {
                RespawnParticle(particle);
            }

            // Apply audio-reactive size
            particle.Size = 2f + (f.Volume * 6f);
        }
    }

    private void RespawnParticle(ParticleData particle)
    {
        // Respawn from edges
        int edge = _random.Next(4);
        switch (edge)
        {
            case 0: // Top
                particle.X = (float)(_random.NextDouble() * _width);
                particle.Y = 0;
                break;
            case 1: // Right
                particle.X = _width;
                particle.Y = (float)(_random.NextDouble() * _height);
                break;
            case 2: // Bottom
                particle.X = (float)(_random.NextDouble() * _width);
                particle.Y = _height;
                break;
            case 3: // Left
                particle.X = 0;
                particle.Y = (float)(_random.NextDouble() * _height);
                break;
        }

        particle.VelocityX = (float)((_random.NextDouble() - 0.5) * PARTICLE_SPEED);
        particle.VelocityY = (float)((_random.NextDouble() - 0.5) * PARTICLE_SPEED);
        particle.Life = particle.MaxLife;
        particle.Size = 2f + (float)(_random.NextDouble() * 4f);
    }

    private float CalculateWaveInfluence(float x, float y, AudioFeatures f)
    {
        float centerX = _width * 0.5f;
        float centerY = _height * 0.5f;

        float distance = MathF.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
        float influence = 0;

        // Calculate influence from all waves
        for (int i = 0; i < WAVE_COUNT; i++)
        {
            var wave = _waves[i];
            float waveDistance = MathF.Abs(distance - wave.Radius);
            if (waveDistance < 50f)
            {
                float waveEffect = MathF.Sin(wave.Phase + wave.Frequency * distance * 0.01f) * wave.Amplitude;
                influence += waveEffect / (waveDistance + 1f);
            }
        }

        return influence * 0.01f * f.Volume;
    }

    private void RenderWaves(ISkiaCanvas canvas, AudioFeatures f)
    {
        float centerX = _width * 0.5f;
        float centerY = _height * 0.5f;

        // Render concentric waves
        for (int i = 0; i < WAVE_COUNT; i++)
        {
            var wave = _waves[i];

            // Calculate wave properties
            float baseRadius = wave.Radius;
            float pulse = (float)Math.Sin(_globalPhase + wave.PulseOffset) * 0.5f + 0.5f;
            float currentRadius = baseRadius + pulse * 20f;

            uint waveColor = GetWaveColor(i, wave.Amplitude, f.Volume, f.Beat);

            // Draw wave as a circle (filled=false for outline)
            canvas.DrawCircle(centerX, centerY, currentRadius, waveColor, false);

            // Add ripple effect for high-amplitude waves
            if (wave.Amplitude > 15f)
            {
                uint rippleColor = (waveColor & 0x00FFFFFF) | 0x60u << 24;
                canvas.DrawCircle(centerX, centerY, currentRadius + 5f, rippleColor, false);
            }
        }
    }

    private void RenderParticles(ISkiaCanvas canvas, AudioFeatures f)
    {
        for (int i = 0; i < PARTICLE_COUNT; i++)
        {
            var particle = _particles[i];

            // Calculate particle color based on life and audio
            uint particleColor = GetParticleColor(particle.ColorIndex, particle.Life / particle.MaxLife, f.Volume);

            // Draw particle
            canvas.FillCircle(particle.X, particle.Y, particle.Size, particleColor);

            // Add trail effect for fast-moving particles
            float speed = MathF.Sqrt(particle.VelocityX * particle.VelocityX + particle.VelocityY * particle.VelocityY);
            if (speed > 1.5f)
            {
                RenderParticleTrail(canvas, particle, particleColor);
            }
        }
    }

    private void RenderParticleTrail(ISkiaCanvas canvas, ParticleData particle, uint color)
    {
        uint trailColor = (color & 0x00FFFFFF) | 0x40u << 24; // Semi-transparent

        // Draw trail as a line in opposite direction of movement
        float trailLength = particle.Size * 3f;
        float trailX = particle.X - particle.VelocityX * trailLength * 0.1f;
        float trailY = particle.Y - particle.VelocityY * trailLength * 0.1f;

        canvas.DrawLine(particle.X, particle.Y, trailX, trailY, trailColor, particle.Size * 0.5f);
    }

    private void RenderGardenEffects(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Add wave interference patterns
        RenderWaveInterference(canvas, f);

        // Add energy field visualization
        RenderEnergyField(canvas, f);

        // Add garden status indicator
        RenderGardenStatus(canvas, f);
    }

    private void RenderWaveInterference(ISkiaCanvas canvas, AudioFeatures f)
    {
        if (_gardenEnergy < 0.3f) return;

        float centerX = _width * 0.5f;
        float centerY = _height * 0.5f;

        // Create interference patterns between waves
        for (int i = 0; i < 12; i++)
        {
            float angle = (i / 12f) * MathF.PI * 2;
            float radius = 150f + (float)Math.Sin(_globalPhase * 3f + i) * 50f;

            float x = centerX + MathF.Cos(angle) * radius;
            float y = centerY + MathF.Sin(angle) * radius;

            uint interferenceColor = _waveColors[i % _waveColors.Length];
            interferenceColor = (interferenceColor & 0x00FFFFFF) | 0x60u << 24;

            canvas.FillCircle(x, y, 3f + _gardenEnergy * 5f, interferenceColor);
        }
    }

    private void RenderEnergyField(ISkiaCanvas canvas, AudioFeatures f)
    {
        float centerX = _width * 0.5f;
        float centerY = _height * 0.5f;

        // Render energy field as gradient circles
        for (int layer = 0; layer < 5; layer++)
        {
            float radius = 100f + layer * 50f;
            float alpha = (int)(30 - layer * 5);
            uint fieldColor = 0x0000FF | ((uint)alpha << 24);

            canvas.DrawCircle(centerX, centerY, radius, fieldColor, false);
        }
    }

    private void RenderGardenStatus(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Draw garden status at the bottom
        float statusY = _height - 20;
        float statusWidth = _width - 40;
        float statusHeight = 6;

        // Background
        canvas.FillRect(20, statusY, statusWidth, statusHeight, 0xFF204040);

        // Wave energy indicators
        for (int i = 0; i < WAVE_COUNT; i++)
        {
            float waveEnergy = _waves[i].Amplitude / 30f;
            float waveWidth = statusWidth / WAVE_COUNT;
            float waveHeight = statusHeight * waveEnergy;

            uint waveColor = _waveColors[i % _waveColors.Length];
            canvas.FillRect(
                20 + i * waveWidth,
                statusY + statusHeight - waveHeight,
                waveWidth,
                waveHeight,
                waveColor
            );
        }
    }

    private uint CalculateOceanBackground(float energy)
    {
        // Dynamic ocean background
        if (energy < 0.2f)
            return 0xFF001122; // Deep ocean
        else if (energy < 0.4f)
            return 0xFF002244; // Ocean blue
        else if (energy < 0.6f)
            return 0xFF004466; // Light ocean
        else
            return 0xFF006688; // Bright ocean
    }

    private uint GetWaveColor(int waveIndex, float amplitude, float volume, bool beat)
    {
        uint baseColor = _waveColors[waveIndex % _waveColors.Length];

        // Enhance based on amplitude and volume
        float brightness = 0.4f + (amplitude / 30f) * 0.6f + volume * 0.3f;

        if (beat)
            brightness += 0.2f;

        brightness = MathF.Min(1f, brightness);

        return AdjustBrightness(baseColor, brightness);
    }

    private uint GetParticleColor(int colorIndex, float lifeRatio, float volume)
    {
        uint baseColor = _waveColors[colorIndex % _waveColors.Length];

        // Fade based on life and enhance with volume
        float alpha = lifeRatio * 0.8f + volume * 0.2f;
        alpha = MathF.Min(1f, alpha);

        return (baseColor & 0x00FFFFFF) | ((uint)(alpha * 255) << 24);
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

    private class WaveData
    {
        public float Radius;
        public float Phase;
        public float Amplitude;
        public float Frequency;
        public int ColorIndex;
        public float PulseOffset;
    }

    private class ParticleData
    {
        public float X, Y;
        public float VelocityX, VelocityY;
        public float Size;
        public float Life;
        public float MaxLife;
        public int ColorIndex;
    }
}
