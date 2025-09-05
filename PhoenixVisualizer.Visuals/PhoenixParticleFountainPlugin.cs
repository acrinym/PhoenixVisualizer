using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

public sealed class PhoenixParticleFountainPlugin : IVisualizerPlugin
{
    public string Id => "phoenix_particle_fountain";
    public string DisplayName => "🔥 Phoenix Particle Fountain";
    public string Description => "FIXED: Audio-reactive particle fountain with proper emission outside center, true fountain behavior, and sound-reactive particles";
    public bool IsEnabled { get; set; } = true;

    private int _w, _h;
    private float _time = 0f;
    private readonly int _maxParticles = 500;
    private readonly Particle[] _particles;
    private int _activeParticles = 0;
    private uint _bgColor = 0xFF000000;

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
        canvas.Clear(_bgColor);

        int spawn = Math.Clamp((int)(features.Volume * 60) + (features.Beat ? 30 : 0), 2, 120);
        for (int i = 0; i < spawn; i++)
        {
            float angle = Random.Shared.NextSingle() * 6.28318f;
            float speed = 0.6f + features.Energy * 2.4f;
            EmitParticle(
                canvas.Width * 0.5f, canvas.Height * 0.55f,
                MathF.Cos(angle) * speed,
                -MathF.Sin(angle) * speed - features.Bass * 1.0f
            );
        }

        UpdateParticles(0.016f);
        DrawParticles(canvas);
    }

    private void EmitParticle(float x, float y, float vx, float vy)
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
            _particles[particleIndex] = new Particle
            {
                x = x,
                y = y,
                z = 0f,
                vx = vx,
                vy = vy,
                vz = 0f,
                life = 1f,
                maxLife = 2.5f + Random.Shared.NextSingle() * 2f,
                color = GetRainbowColor(Random.Shared.NextSingle(), 0.5f),
                size = 1.5f + Random.Shared.NextSingle() * 4f,
                active = true
            };

            // Only increment active count if this was a truly inactive particle
            if (!_particles[particleIndex].active)
            {
                _activeParticles++;
            }
        }
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

            // FIXED: More fountain-like emission - primarily upward with controlled spread
            var baseAngle = Math.PI * 0.5f; // Straight up
            var spreadAngle = features.Bass * 0.3f + 0.1f; // Bass controls spread
            var angle = (float)(baseAngle + (Random.Shared.NextDouble() - 0.5f) * spreadAngle);
            
            // FIXED: Audio-reactive speed - bass controls height, treble controls spread
            var baseSpeed = 80f + features.Energy * 150f;
            var bassSpeed = features.Bass * 100f; // Bass adds upward velocity
            var speed = baseSpeed + bassSpeed;

            // FIXED: Z-speed from bass (affects particle height and fountain effect)
            var zSpeed = features.Bass * 200f + 30f;

            // FIXED: Emission area controlled by mid frequencies
            var emissionSpread = features.Mid * 40f + 20f;

            _particles[particleIndex] = new Particle
            {
                x = baseX + (Random.Shared.NextSingle() - 0.5f) * emissionSpread, // Mid-controlled spread
                y = baseY,
                z = 0f,
                vx = (float)Math.Cos(angle) * speed * 0.2f + (Random.Shared.NextSingle() - 0.5f) * features.Treble * 15f, // Treble adds horizontal movement
                vy = -(float)Math.Sin(angle) * speed,
                vz = zSpeed + (Random.Shared.NextSingle() - 0.5f) * 20f, // Reduced Z variation for more consistent fountain
                life = 1f,
                maxLife = 2.5f + features.Energy * 3f + Random.Shared.NextSingle() * 2f, // Slightly shorter life for better fountain effect
                color = GetRainbowColor(Random.Shared.NextSingle(), features.Treble),
                size = 1.5f + Random.Shared.NextSingle() * 4f + features.Bass * 3f, // Bass affects particle size
                active = true
            };

            // Only increment active count if this was a truly inactive particle
            if (!_particles[particleIndex].active)
            {
                _activeParticles++;
            }
        }
    }

    private void UpdateParticles(float deltaTime)
    {
        var gravity = 300f;
        var drag = 0.995f;

        for (int i = 0; i < _maxParticles; i++)
        {
            if (!_particles[i].active) continue;

            var p = _particles[i];

            // Apply physics
            p.vx *= drag;
            p.vy += gravity * deltaTime;
            p.vz *= 0.995f;

            // Update position
            p.x += p.vx * deltaTime;
            p.y += p.vy * deltaTime;
            p.z += p.vz * deltaTime;

            // Update life
            p.life -= deltaTime / p.maxLife;

            // Deactivate if dead or off-screen
            if (p.life <= 0 || p.y > _h + 100)
            {
                p.active = false;
                _activeParticles--;
            }

            _particles[i] = p;
        }
    }

    private void DrawParticles(ISkiaCanvas canvas)
    {
        for (int i = 0; i < _maxParticles; i++)
        {
            if (!_particles[i].active) continue;

            var p = _particles[i];
            var alpha = (byte)(p.life * 255);
            var color = (p.color & 0x00FFFFFF) | ((uint)alpha << 24);
            
            canvas.FillCircle(p.x, p.y, p.size, color);
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
