using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

public sealed class PhoenixParticleFountainPlugin : IVisualizerPlugin
{
    public string Id => "phoenix_particle_fountain";
    public string DisplayName => "🔥 Phoenix Particle Fountain";
    public string Description => "GPU-like particle system with energy-driven emission and Phoenix fire colors";
    public bool IsEnabled { get; set; } = true;

    private int _w, _h;
    private float _time = 0f;
    private readonly int _maxParticles = 500;
    private readonly Particle[] _particles;
    private int _activeParticles = 0;

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

    private struct Particle
    {
        public float x, y, z;        // Position
        public float vx, vy, vz;     // Velocity
        public float life;            // Life remaining (0-1)
        public float maxLife;         // Maximum life
        public uint color;            // Particle color
        public float size;            // Particle size
        public bool active;           // Is particle active
    }

    public PhoenixParticleFountainPlugin()
    {
        _particles = new Particle[_maxParticles];
        for (int i = 0; i < _maxParticles; i++)
        {
            _particles[i].active = false;
        }
    }

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

        // Update time
        _time += 0.02f;

        // Get audio data
        var energy = features.Energy;
        var bass = features.Bass;
        var mid = features.Mid;
        var treble = features.Treble;
        var beat = features.Beat;

        // Continuous emission with energy boost - ALWAYS emit particles
        var baseEmissionRate = 12f; // Increased base emission
        var energyEmissionRate = energy * 20f; // Increased energy-driven emission
        var totalEmissionRate = baseEmissionRate + energyEmissionRate;

        if (beat) totalEmissionRate *= 2.0f; // Increased beat boost

        // Emit particles based on accumulated emission
        var particlesToEmit = (int)totalEmissionRate;
        var fractionalEmission = totalEmissionRate - particlesToEmit;

        // Handle fractional emission probabilistically
        if (Random.Shared.NextSingle() < fractionalEmission)
            particlesToEmit++;

        // Always emit at least some particles, even if we're at max
        particlesToEmit = Math.Max(3, particlesToEmit);

        for (int i = 0; i < particlesToEmit; i++)
        {
            EmitParticle(features);
        }

        // Update and render particles
        UpdateParticles(features);
        RenderParticles(canvas, features);

        // Draw fountain base
        var baseX = _w / 2f;
        var baseY = _h * 0.8f;
        var baseRadius = 30f + bass * 20f;
        var baseColor = GetFireColor(bass);
        canvas.FillCircle(baseX, baseY, baseRadius, baseColor);

        // Draw energy rings around base
        for (int ring = 1; ring <= 3; ring++)
        {
            var ringRadius = baseRadius + ring * 15f;
            var ringAlpha = (byte)(100 - ring * 30);
            var ringColor = (uint)(ringAlpha << 24 | 0xFFFF4400);
            canvas.DrawCircle(baseX, baseY, ringRadius, ringColor, false);
        }

        // Draw particle count info (debug)
        var infoColor = 0x88FFFFFF;
        canvas.DrawText($"Particles: {_activeParticles}", 10, 10, infoColor, 14f);
        canvas.DrawText($"Energy: {energy:F2}", 10, 30, infoColor, 14f);
    }

    private void EmitParticle(AudioFeatures features)
    {
        int particleIndex = -1;
        
        // First try to find an inactive particle
        for (int i = 0; i < _maxParticles; i++)
        {
            if (!_particles[i].active)
            {
                particleIndex = i;
                break;
            }
        }
        
        // If no inactive particles, recycle the oldest one (lowest life)
        if (particleIndex == -1)
        {
            float lowestLife = float.MaxValue;
            for (int i = 0; i < _maxParticles; i++)
            {
                if (_particles[i].life < lowestLife)
                {
                    lowestLife = _particles[i].life;
                    particleIndex = i;
                }
            }
        }
        
        if (particleIndex != -1)
        {
            var baseX = _w / 2f;
            var baseY = _h * 0.8f;

            // Random emission angle (mostly upward with some spread)
            var angle = (float)(Math.PI * 0.5f + (Random.Shared.NextDouble() - 0.5f) * 0.6f);
            var speed = 100f + features.Energy * 200f; // Speed from energy

            // Z-speed from bass (affects particle height)
            var zSpeed = features.Bass * 150f + 50f;

            _particles[particleIndex] = new Particle
            {
                x = baseX + (Random.Shared.NextSingle() - 0.5f) * 30f, // Wider emission area
                y = baseY,
                z = 0f,
                vx = (float)Math.Cos(angle) * speed * 0.3f + (Random.Shared.NextSingle() - 0.5f) * 20f, // Add horizontal spread
                vy = -(float)Math.Sin(angle) * speed,
                vz = zSpeed + (Random.Shared.NextSingle() - 0.5f) * 30f, // Add Z variation
                life = 1f,
                maxLife = 3f + features.Energy * 4f + Random.Shared.NextSingle() * 3f, // Longer, more varied life
                color = GetRainbowColor(Random.Shared.NextSingle(), features.Treble),
                size = 2f + Random.Shared.NextSingle() * 6f, // More size variation
                active = true
            };

            // Only increment active count if this was a truly inactive particle
            if (!_particles[particleIndex].active)
            {
                _activeParticles++;
            }
        }
    }

    private void UpdateParticles(AudioFeatures features)
    {
        var gravity = 300f; // Reduced gravity for higher arcs
        var drag = 0.995f;  // Less air resistance for longer flight
        var windStrength = features.Mid * 50f; // Wind from mid frequencies

        for (int i = 0; i < _maxParticles; i++)
        {
            if (!_particles[i].active) continue;

            var p = _particles[i];

            // Apply physics with wind
            p.vx *= drag;
            p.vx += windStrength * 0.001f; // Gentle wind effect
            p.vy += gravity * 0.016f; // 60 FPS assumed
            p.vz *= 0.995f; // Z drag

            // Update position
            p.x += p.vx * 0.016f;
            p.y += p.vy * 0.016f;
            p.z += p.vz * 0.016f;

            // Update life
            p.life -= 0.016f / p.maxLife;

            // Deactivate dead particles
            if (p.life <= 0f)
            {
                p.active = false;
                _activeParticles--;
                continue;
            }

            // Bounce off walls
            if (p.x < 0f || p.x > _w)
            {
                p.vx *= -0.5f;
                p.x = Math.Clamp(p.x, 0f, _w);
            }

            if (p.y > _h)
            {
                p.vy *= -0.3f;
                p.y = _h;
                p.vz *= 0.5f; // Reduce z velocity on bounce
            }

            // Bounce off floor (z-axis)
            if (p.z < 0f)
            {
                p.vz *= -0.5f;
                p.z = 0f;
            }

            _particles[i] = p;
        }
    }

    private void RenderParticles(ISkiaCanvas canvas, AudioFeatures features)
    {
        // Sort particles by Z for depth ordering (back to front)
        var sortedParticles = new System.Collections.Generic.List<Particle>();
        for (int i = 0; i < _maxParticles; i++)
        {
            if (_particles[i].active)
                sortedParticles.Add(_particles[i]);
        }

        sortedParticles.Sort((a, b) => a.z.CompareTo(b.z));

        // Render particles
        foreach (var p in sortedParticles)
        {
            // Calculate screen position (simple perspective)
            var screenX = p.x + p.z * 0.1f; // Z affects X position
            var screenY = p.y - p.z * 0.2f; // Z affects Y position (upward)

            // Skip off-screen particles
            if (screenX < -50f || screenX > _w + 50f || screenY < -50f || screenY > _h + 50f)
                continue;

            // Calculate alpha based on life and Z depth
            var alpha = (byte)(p.life * 255);
            var color = (p.color & 0x00FFFFFF) | ((uint)alpha << 24);

            // Adjust size based on Z depth
            var size = p.size * (1f + p.z * 0.001f);

            // Draw particle
            canvas.FillCircle(screenX, screenY, size, color);

            // Add glow effect for bright particles
            if (p.life > 0.7f)
            {
                var glowColor = (color & 0x00FFFFFF) | ((uint)((byte)(p.life * 100)) << 24);
                canvas.FillCircle(screenX, screenY, size * 1.5f, glowColor);
            }
        }
    }

    private uint GetRainbowColor(float t, float trebleEnergy)
    {
        // Create rainbow color that drifts with treble
        var hue = (t + _time * 0.5f + trebleEnergy * 0.3f) * 6.283f;
        
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
