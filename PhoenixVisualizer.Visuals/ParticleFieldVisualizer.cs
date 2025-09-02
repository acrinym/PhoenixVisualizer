using System;
using PhoenixVisualizer.Audio;
using PhoenixVisualizer.Core;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// AVS-Inspired Particle Field Visualizer
/// Advanced particle system with physics simulation and audio reactivity
/// Inspired by classic AVS particle presets with complex movement patterns
/// </summary>
public class ParticleFieldVisualizer : BaseVisualizer
{
    // Particle system
    private Particle[] _particles;
    private int _particleCount = 1000;
    private float _time = 0.0f;

    // Visual parameters with global parameter support
    private uint _particleColor = 0xFFFFFFFF; // White
    private uint _trailColor = 0x80FFFFFF; // Semi-transparent white
    private float _particleSize = 2.0f;
    private float _trailLength = 0.5f;
    private float _attractionStrength = 0.1f;
    private float _repulsionStrength = 0.05f;
    private float _damping = 0.98f;
    private float _maxVelocity = 5.0f;
    private bool _enableTrails = true;
    private bool _enableGravity = false;
    private float _gravityStrength = 0.1f;
    private string _movementMode = "Attract"; // Attract, Repulse, Orbit, Random, Audio

    // Audio reactivity parameters
    private float _bassMultiplier = 2.0f;
    private float _midMultiplier = 1.5f;
    private float _trebleMultiplier = 1.0f;

    public override string Id => "ParticleField";
    public override string Name => "Particle Field";
    public override string Description => "Advanced particle system with physics simulation and audio reactivity";

    private class Particle
    {
        public float X, Y;
        public float VX, VY;
        public float AX, AY;
        public float Life;
        public float MaxLife;
        public uint Color;
        public float Size;
        public float Mass = 1.0f;

        public Particle(float x, float y)
        {
            X = x;
            Y = y;
            VX = VY = AX = AY = 0;
            Life = MaxLife = 1.0f;
            Color = 0xFFFFFFFF;
            Size = 2.0f;
        }

        public void Update(float deltaTime, float damping, float maxVelocity)
        {
            // Apply acceleration
            VX += AX * deltaTime;
            VY += AY * deltaTime;

            // Apply damping
            VX *= damping;
            VY *= damping;

            // Limit velocity
            float speed = (float)Math.Sqrt(VX * VX + VY * VY);
            if (speed > maxVelocity)
            {
                VX = (VX / speed) * maxVelocity;
                VY = (VY / speed) * maxVelocity;
            }

            // Update position
            X += VX * deltaTime;
            Y += VY * deltaTime;

            // Reset acceleration for next frame
            AX = AY = 0;

            // Update life
            Life -= deltaTime * 0.1f;
            if (Life <= 0)
            {
                Life = MaxLife;
            }
        }

        public void ApplyForce(float fx, float fy)
        {
            AX += fx / Mass;
            AY += fy / Mass;
        }

        public void ResetAcceleration()
        {
            AX = AY = 0;
        }
    }

    public void Initialize(int width, int height)
    {
        Resize(width, height);

        // Register global parameters
        this.RegisterGlobalParameters(Id, new[]
        {
            GlobalParameterSystem.GlobalCategory.General,
            GlobalParameterSystem.GlobalCategory.Audio,
            GlobalParameterSystem.GlobalCategory.Visual,
            GlobalParameterSystem.GlobalCategory.Motion,
            GlobalParameterSystem.GlobalCategory.Effects
        });

        // Register specific parameters
        RegisterParameters();

        // Initialize particles
        InitializeParticles(width, height);
    }

    private void RegisterParameters()
    {
        var parameters = new System.Collections.Generic.List<ParameterSystem.ParameterDefinition>
        {
            new ParameterSystem.ParameterDefinition
            {
                Key = "particleCount",
                Label = "Particle Count",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 1000,
                MinValue = 100,
                MaxValue = 5000,
                Description = "Number of particles in the system",
                Category = "Particles"
            },
            new ParameterSystem.ParameterDefinition
            {
                Key = "particleColor",
                Label = "Particle Color",
                Type = ParameterSystem.ParameterType.Color,
                DefaultValue = "#FFFFFF",
                Description = "Color of the particles",
                Category = "Particles"
            },
            new ParameterSystem.ParameterDefinition
            {
                Key = "trailColor",
                Label = "Trail Color",
                Type = ParameterSystem.ParameterType.Color,
                DefaultValue = "#80FFFFFF",
                Description = "Color of particle trails",
                Category = "Particles"
            },
            new ParameterSystem.ParameterDefinition
            {
                Key = "particleSize",
                Label = "Particle Size",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 2.0f,
                MinValue = 0.5f,
                MaxValue = 10.0f,
                Description = "Size of individual particles",
                Category = "Particles"
            },
            new ParameterSystem.ParameterDefinition
            {
                Key = "trailLength",
                Label = "Trail Length",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.5f,
                MinValue = 0.0f,
                MaxValue = 1.0f,
                Description = "Length of particle trails (0 = no trails)",
                Category = "Particles"
            },
            new ParameterSystem.ParameterDefinition
            {
                Key = "attractionStrength",
                Label = "Attraction Strength",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.1f,
                MinValue = -0.5f,
                MaxValue = 0.5f,
                Description = "Strength of particle attraction (negative = repulsion)",
                Category = "Physics"
            },
            new ParameterSystem.ParameterDefinition
            {
                Key = "repulsionStrength",
                Label = "Repulsion Strength",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.05f,
                MinValue = 0.0f,
                MaxValue = 0.2f,
                Description = "Strength of particle repulsion from mouse/audio",
                Category = "Physics"
            },
            new ParameterSystem.ParameterDefinition
            {
                Key = "damping",
                Label = "Damping",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.98f,
                MinValue = 0.9f,
                MaxValue = 0.999f,
                Description = "How quickly particles slow down",
                Category = "Physics"
            },
            new ParameterSystem.ParameterDefinition
            {
                Key = "maxVelocity",
                Label = "Max Velocity",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 5.0f,
                MinValue = 1.0f,
                MaxValue = 20.0f,
                Description = "Maximum speed of particles",
                Category = "Physics"
            },
            new ParameterSystem.ParameterDefinition
            {
                Key = "enableTrails",
                Label = "Enable Trails",
                Type = ParameterSystem.ParameterType.Checkbox,
                DefaultValue = true,
                Description = "Show particle trails",
                Category = "Display"
            },
            new ParameterSystem.ParameterDefinition
            {
                Key = "enableGravity",
                Label = "Enable Gravity",
                Type = ParameterSystem.ParameterType.Checkbox,
                DefaultValue = false,
                Description = "Apply gravitational force",
                Category = "Physics"
            },
            new ParameterSystem.ParameterDefinition
            {
                Key = "gravityStrength",
                Label = "Gravity Strength",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.1f,
                MinValue = -0.5f,
                MaxValue = 0.5f,
                Description = "Strength of gravitational pull",
                Category = "Physics"
            },
            new ParameterSystem.ParameterDefinition
            {
                Key = "movementMode",
                Label = "Movement Mode",
                Type = ParameterSystem.ParameterType.Dropdown,
                DefaultValue = "Attract",
                Options = new System.Collections.Generic.List<string> { "Attract", "Repulse", "Orbit", "Random", "Audio" },
                Description = "How particles move and interact",
                Category = "Behavior"
            },
            new ParameterSystem.ParameterDefinition
            {
                Key = "bassMultiplier",
                Label = "Bass Multiplier",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 2.0f,
                MinValue = 0.0f,
                MaxValue = 5.0f,
                Description = "How much bass affects particle movement",
                Category = "Audio"
            },
            new ParameterSystem.ParameterDefinition
            {
                Key = "midMultiplier",
                Label = "Mid Multiplier",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 1.5f,
                MinValue = 0.0f,
                MaxValue = 5.0f,
                Description = "How much mid frequencies affect particle movement",
                Category = "Audio"
            },
            new ParameterSystem.ParameterDefinition
            {
                Key = "trebleMultiplier",
                Label = "Treble Multiplier",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 1.0f,
                MinValue = 0.0f,
                MaxValue = 5.0f,
                Description = "How much treble affects particle movement",
                Category = "Audio"
            }
        };

        ParameterSystem.RegisterVisualizerParameters(Id, parameters);
    }

    private void InitializeParticles(int width, int height)
    {
        _particles = new Particle[_particleCount];

        for (int i = 0; i < _particleCount; i++)
        {
            float x = Random.Shared.Next(0, width);
            float y = Random.Shared.Next(0, height);
            _particles[i] = new Particle(x, y);

            // Random initial velocity
            _particles[i].VX = (float)(Random.Shared.NextDouble() - 0.5) * 2.0f;
            _particles[i].VY = (float)(Random.Shared.NextDouble() - 0.5) * 2.0f;
        }
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;

        // Reinitialize particles if count changed
        if (_particles == null || _particles.Length != _particleCount)
        {
            InitializeParticles(width, height);
        }
    }

    public unsafe void RenderFrame(AudioFeatures features, IntPtr frameBuffer, int width, int height)
    {
        // Check global enable/disable
        var globalEnabled = GlobalParameterSystem.GetGlobalParameter<bool>(Id, GlobalParameterSystem.CommonParameters.Enabled, true);
        if (!globalEnabled) return;

        // Update global parameters
        var globalOpacity = GlobalParameterSystem.GetGlobalParameter<float>(Id, GlobalParameterSystem.CommonParameters.Opacity, 1.0f);
        var globalBrightness = GlobalParameterSystem.GetGlobalParameter<float>(Id, GlobalParameterSystem.CommonParameters.Brightness, 1.0f);
        var globalScale = GlobalParameterSystem.GetGlobalParameter<float>(Id, GlobalParameterSystem.CommonParameters.Scale, 1.0f);
        var globalSpeed = GlobalParameterSystem.GetGlobalParameter<float>(Id, GlobalParameterSystem.CommonParameters.Speed, 1.0f);

        // Update specific parameters
        var newParticleCount = ParameterSystem.GetParameterValue<int>(Id, "particleCount", 1000);
        if (newParticleCount != _particleCount)
        {
            _particleCount = newParticleCount;
            InitializeParticles(width, height);
        }

        _particleSize = ParameterSystem.GetParameterValue<float>(Id, "particleSize", 2.0f) ?? 2.0f;
        _trailLength = ParameterSystem.GetParameterValue<float>(Id, "trailLength", 0.5f) ?? 0.5f;
        _attractionStrength = ParameterSystem.GetParameterValue<float>(Id, "attractionStrength", 0.1f) ?? 0.1f;
        _repulsionStrength = ParameterSystem.GetParameterValue<float>(Id, "repulsionStrength", 0.05f) ?? 0.05f;
        _damping = ParameterSystem.GetParameterValue<float>(Id, "damping", 0.98f) ?? 0.98f;
        _maxVelocity = ParameterSystem.GetParameterValue<float>(Id, "maxVelocity", 5.0f) ?? 5.0f;
        _enableTrails = ParameterSystem.GetParameterValue<bool>(Id, "enableTrails", true);
        _enableGravity = ParameterSystem.GetParameterValue<bool>(Id, "enableGravity", false);
        _gravityStrength = ParameterSystem.GetParameterValue<float>(Id, "gravityStrength", 0.1f) ?? 0.1f;
        _movementMode = ParameterSystem.GetParameterValue<string>(Id, "movementMode", "Attract") ?? "Attract";
        _bassMultiplier = ParameterSystem.GetParameterValue<float>(Id, "bassMultiplier", 2.0f) ?? 2.0f;
        _midMultiplier = ParameterSystem.GetParameterValue<float>(Id, "midMultiplier", 1.5f) ?? 1.5f;
        _trebleMultiplier = ParameterSystem.GetParameterValue<float>(Id, "trebleMultiplier", 1.0f) ?? 1.0f;

        // Apply global parameters
        _particleSize *= globalScale;
        _maxVelocity *= globalSpeed;

        // Parse colors and apply global effects
        var particleColorHex = ParameterSystem.GetParameterValue<string>(Id, "particleColor", "#FFFFFF") ?? "#FFFFFF";
        var trailColorHex = ParameterSystem.GetParameterValue<string>(Id, "trailColor", "#80FFFFFF") ?? "#80FFFFFF";

        _particleColor = ApplyGlobalEffects(ColorFromHex(particleColorHex), globalBrightness, globalOpacity);
        _trailColor = ApplyGlobalEffects(ColorFromHex(trailColorHex), globalBrightness, globalOpacity);

        // Update time
        _time += 0.016f; // Assume ~60fps

        // Clear frame buffer with fade effect if trails are enabled
        if (_enableTrails && _trailLength > 0)
        {
            FadeFrameBuffer(frameBuffer, width, height, 1.0f - _trailLength);
        }
        else
        {
            ClearFrameBuffer(frameBuffer, width, height);
        }

        // Update and render particles
        UpdateParticles(width, height, features);
        RenderParticles(frameBuffer, width, height);
    }

    private void UpdateParticles(int width, int height, AudioFeatures features)
    {
        float deltaTime = 0.016f; // Assume ~60fps

        // Calculate audio reactivity
        float bassLevel = features.Bass * _bassMultiplier * GlobalParameterSystem.GetGlobalParameter<float>(Id, GlobalParameterSystem.CommonParameters.AudioSensitivity, 1.0f);
        float midLevel = features.Mid * _midMultiplier * GlobalParameterSystem.GetGlobalParameter<float>(Id, GlobalParameterSystem.CommonParameters.AudioSensitivity, 1.0f);
        float trebleLevel = features.Treble * _trebleMultiplier * GlobalParameterSystem.GetGlobalParameter<float>(Id, GlobalParameterSystem.CommonParameters.AudioSensitivity, 1.0f);

        float centerX = width * 0.5f;
        float centerY = height * 0.5f;

        for (int i = 0; i < _particles.Length; i++)
        {
            var particle = _particles[i];

            // Reset acceleration
            particle.ResetAcceleration();

            // Apply forces based on movement mode
            switch (_movementMode)
            {
                case "Attract":
                    ApplyAttractionForces(particle, centerX, centerY, bassLevel);
                    break;
                case "Repulse":
                    ApplyRepulsionForces(particle, centerX, centerY, bassLevel);
                    break;
                case "Orbit":
                    ApplyOrbitalForces(particle, centerX, centerY, midLevel);
                    break;
                case "Random":
                    ApplyRandomForces(particle, trebleLevel);
                    break;
                case "Audio":
                    ApplyAudioForces(particle, features, bassLevel, midLevel, trebleLevel);
                    break;
            }

            // Apply gravity if enabled
            if (_enableGravity)
            {
                particle.ApplyForce(0, _gravityStrength);
            }

            // Update particle physics
            particle.Update(deltaTime, _damping, _maxVelocity);

            // Wrap around screen edges
            if (particle.X < 0) particle.X += width;
            if (particle.X >= width) particle.X -= width;
            if (particle.Y < 0) particle.Y += height;
            if (particle.Y >= height) particle.Y -= height;
        }
    }

    private void ApplyAttractionForces(Particle particle, float centerX, float centerY, float strength)
    {
        float dx = centerX - particle.X;
        float dy = centerY - particle.Y;
        float distance = (float)Math.Sqrt(dx * dx + dy * dy);

        if (distance > 0)
        {
            float force = _attractionStrength * strength / (distance * distance + 1);
            particle.ApplyForce(dx * force, dy * force);
        }
    }

    private void ApplyRepulsionForces(Particle particle, float centerX, float centerY, float strength)
    {
        float dx = particle.X - centerX;
        float dy = particle.Y - centerY;
        float distance = (float)Math.Sqrt(dx * dx + dy * dy);

        if (distance > 0)
        {
            float force = _repulsionStrength * strength / (distance + 1);
            particle.ApplyForce(dx * force, dy * force);
        }
    }

    private void ApplyOrbitalForces(Particle particle, float centerX, float centerY, float strength)
    {
        float dx = particle.X - centerX;
        float dy = particle.Y - centerY;
        float distance = (float)Math.Sqrt(dx * dx + dy * dy);

        if (distance > 0)
        {
            // Perpendicular force for orbital motion
            float force = _attractionStrength * strength / (distance + 10);
            particle.ApplyForce(-dy * force, dx * force);

            // Radial force
            float radialForce = _attractionStrength * strength * 0.1f;
            particle.ApplyForce(-dx * radialForce / distance, -dy * radialForce / distance);
        }
    }

    private void ApplyRandomForces(Particle particle, float strength)
    {
        float angle = (float)(_time * 2 + particle.X * 0.01f + particle.Y * 0.01f);
        float force = _attractionStrength * strength * 0.5f;
        particle.ApplyForce((float)Math.Cos(angle) * force, (float)Math.Sin(angle) * force);
    }

    private void ApplyAudioForces(Particle particle, AudioFeatures features, float bass, float mid, float treble)
    {
        // Use different frequency bands for different forces
        float bassForce = bass * 0.1f;
        float midForce = mid * 0.05f;
        float trebleForce = treble * 0.02f;

        // Apply forces based on frequency analysis
        float angle1 = (float)(_time * bass * 0.1f + particle.X * 0.01f);
        float angle2 = (float)(_time * mid * 0.05f + particle.Y * 0.01f);

        particle.ApplyForce(
            (float)Math.Cos(angle1) * bassForce + (float)Math.Sin(angle2) * midForce,
            (float)Math.Sin(angle1) * bassForce + (float)Math.Cos(angle2) * midForce
        );

        // Add some treble-based turbulence
        float turbulence = (float)(Math.Sin(_time * treble * 0.1f + particle.X * 0.05f) * trebleForce);
        particle.ApplyForce(turbulence, turbulence);
    }

    private unsafe void RenderParticles(IntPtr frameBuffer, int width, int height)
    {
        for (int i = 0; i < _particles.Length; i++)
        {
            var particle = _particles[i];
            DrawParticle(frameBuffer, width, height, particle);
        }
    }

    private unsafe void DrawParticle(IntPtr frameBuffer, int width, int height, Particle particle)
    {
        int x = (int)particle.X;
        int y = (int)particle.Y;
        int size = (int)_particleSize;

        for (int py = y - size; py <= y + size; py++)
        {
            for (int px = x - size; px <= x + size; px++)
            {
                int dx = px - x;
                int dy = py - y;
                if (dx * dx + dy * dy <= size * size)
                {
                    if (px >= 0 && px < width && py >= 0 && py < height)
                    {
                        SetPixel(frameBuffer, width, height, px, py, _particleColor);
                    }
                }
            }
        }
    }

    private unsafe void FadeFrameBuffer(IntPtr frameBuffer, int width, int height, float fadeFactor)
    {
        uint* pixels = (uint*)frameBuffer;
        for (int i = 0; i < width * height; i++)
        {
            uint pixel = pixels[i];
            byte a = (byte)((pixel >> 24) & 0xFF);
            byte r = (byte)((pixel >> 16) & 0xFF);
            byte g = (byte)((pixel >> 8) & 0xFF);
            byte b = (byte)(pixel & 0xFF);

            a = (byte)(a * fadeFactor);
            r = (byte)(r * fadeFactor);
            g = (byte)(g * fadeFactor);
            b = (byte)(b * fadeFactor);

            pixels[i] = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
        }
    }

    private unsafe void ClearFrameBuffer(IntPtr frameBuffer, int width, int height)
    {
        uint* pixels = (uint*)frameBuffer;
        for (int i = 0; i < width * height; i++)
        {
            pixels[i] = 0x00000000; // Transparent
        }
    }

    private unsafe void SetPixel(IntPtr frameBuffer, int width, int height, int x, int y, uint color)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;

        uint* pixels = (uint*)frameBuffer;
        pixels[y * width + x] = color;
    }

    private uint ApplyGlobalEffects(uint color, float brightness, float opacity)
    {
        // Extract RGBA components
        var a = ((color >> 24) & 0xFF) / 255.0f;
        var r = ((color >> 16) & 0xFF) / 255.0f;
        var g = ((color >> 8) & 0xFF) / 255.0f;
        var b = (color & 0xFF) / 255.0f;

        // Apply brightness
        r = Math.Clamp(r * brightness, 0, 1);
        g = Math.Clamp(g * brightness, 0, 1);
        b = Math.Clamp(b * brightness, 0, 1);

        // Apply opacity
        a *= opacity;

        // Convert back to uint
        return ((uint)(a * 255) << 24) |
               ((uint)(r * 255) << 16) |
               ((uint)g << 8) |
               (uint)b;
    }

    private uint ColorFromHex(string hexColor)
    {
        if (string.IsNullOrEmpty(hexColor) || !hexColor.StartsWith("#"))
            return 0xFFFFFFFF; // Default white

        try
        {
            var color = System.Drawing.ColorTranslator.FromHtml(hexColor);
            return ((uint)color.A << 24) |
                   ((uint)color.R << 16) |
                   ((uint)color.G << 8) |
                   ((uint)color.B);
        }
        catch
        {
            return 0xFFFFFFFF; // Default white
        }
    }
}
