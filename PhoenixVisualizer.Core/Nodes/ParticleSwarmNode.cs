using System;
using System.Collections.Generic;
using System.Numerics;

namespace PhoenixVisualizer.Core.Nodes;

public class ParticleSwarmNode : IEffectNode
{
    public string Name => "Particle Swarm";
    public Dictionary<string, EffectParam> Params { get; } = new()
    {
        ["count"] = new EffectParam{ Label="Particle Count", Type="slider", Min=100, Max=5000, FloatValue=500 },
        ["speed"] = new EffectParam{ Label="Speed", Type="slider", Min=0.1f, Max=5f, FloatValue=1f },
        ["color"] = new EffectParam{ Label="Color", Type="color", ColorValue="#00FFCC" },
        ["swarmBehavior"] = new EffectParam{ Label="Swarm Behavior", Type="dropdown", StringValue="attraction", Options=new(){"attraction", "repulsion", "alignment", "cohesion", "mixed"} },
        ["fftReactivity"] = new EffectParam{ Label="FFT Reactivity", Type="slider", Min=0, Max=3f, FloatValue=1.5f },
        ["waveformReactivity"] = new EffectParam{ Label="Waveform Reactivity", Type="slider", Min=0, Max=3f, FloatValue=1f },
        ["particleSize"] = new EffectParam{ Label="Particle Size", Type="slider", Min=0.5f, Max=5f, FloatValue=2f },
        ["trailLength"] = new EffectParam{ Label="Trail Length", Type="slider", Min=0, Max=20, FloatValue=8 },
        ["fieldStrength"] = new EffectParam{ Label="Field Strength", Type="slider", Min=0.1f, Max=5f, FloatValue=2f },
        ["noiseScale"] = new EffectParam{ Label="Noise Scale", Type="slider", Min=0.1f, Max=3f, FloatValue=1f }
    };

    private float _time = 0f;
    private List<Particle> _particles = new();
    private bool _initialized = false;

    public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
    {
        if (ctx.Canvas == null) return;

        _time += 0.016f * Params["speed"].FloatValue;

        // Initialize particles if needed
        if (!_initialized)
        {
            InitializeParticles(ctx);
            _initialized = true;
        }

        // Clear canvas
        ctx.Canvas.Clear(0xFF000000);

        // Get parameters
        string swarmBehavior = Params["swarmBehavior"].StringValue;
        float fftReactivity = Params["fftReactivity"].FloatValue;
        float waveformReactivity = Params["waveformReactivity"].FloatValue;
        float particleSize = Params["particleSize"].FloatValue;
        int trailLength = (int)Params["trailLength"].FloatValue;
        float fieldStrength = Params["fieldStrength"].FloatValue;
        float noiseScale = Params["noiseScale"].FloatValue;

        // Parse color
        uint baseColor = ParseColor(Params["color"].ColorValue);

        // Update and render particles
        UpdateParticles(ctx, waveform, spectrum, swarmBehavior, fftReactivity, waveformReactivity, fieldStrength, noiseScale);
        RenderParticles(ctx, baseColor, particleSize, trailLength);
    }

    private void InitializeParticles(RenderContext ctx)
    {
        int count = (int)Params["count"].FloatValue;
        _particles.Clear();

        for (int i = 0; i < count; i++)
        {
            _particles.Add(new Particle
            {
                Position = new Vector2(
                    Random.Shared.NextSingle() * ctx.Width,
                    Random.Shared.NextSingle() * ctx.Height
                ),
                Velocity = new Vector2(
                    (Random.Shared.NextSingle() - 0.5f) * 2f,
                    (Random.Shared.NextSingle() - 0.5f) * 2f
                ),
                Life = Random.Shared.NextSingle(),
                Size = Random.Shared.NextSingle() * 2f + 1f
            });
        }
    }

    private void UpdateParticles(RenderContext ctx, float[] waveform, float[] spectrum, string behavior, float fftReactivity, float waveformReactivity, float fieldStrength, float noiseScale)
    {
        float centerX = ctx.Width / 2f;
        float centerY = ctx.Height / 2f;
        var center = new Vector2(centerX, centerY);

        // Calculate audio forces
        var audioForces = CalculateAudioForces(waveform, spectrum, fftReactivity, waveformReactivity);

        foreach (var particle in _particles)
        {
            // Update particle velocity based on swarm behavior
            var force = Vector2.Zero;

            switch (behavior)
            {
                case "attraction":
                    force = CalculateAttractionForce(particle, center, fieldStrength);
                    break;
                case "repulsion":
                    force = CalculateRepulsionForce(particle, center, fieldStrength);
                    break;
                case "alignment":
                    force = CalculateAlignmentForce(particle, _particles, fieldStrength);
                    break;
                case "cohesion":
                    force = CalculateCohesionForce(particle, _particles, center, fieldStrength);
                    break;
                case "mixed":
                    force = CalculateMixedForces(particle, _particles, center, fieldStrength);
                    break;
            }

            // Add audio-driven forces
            force += audioForces * fftReactivity;

            // Add noise for organic movement
            var noise = new Vector2(
                (float)(Math.Sin(_time * 2f + particle.Position.X * 0.01f) * noiseScale),
                (float)(Math.Cos(_time * 3f + particle.Position.Y * 0.01f) * noiseScale)
            );
            force += noise;

            // Apply forces
            particle.Velocity += force * 0.01f;
            
            // Limit velocity
            if (particle.Velocity.Length() > 5f)
            {
                particle.Velocity = Vector2.Normalize(particle.Velocity) * 5f;
            }

            // Update position
            var newPosition = particle.Position + particle.Velocity;

            // Wrap around screen edges
            var wrappedPosition = new Vector2(
                (newPosition.X + ctx.Width) % ctx.Width,
                (newPosition.Y + ctx.Height) % ctx.Height
            );
            particle.Position = wrappedPosition;

            // Update life cycle
            particle.Life += 0.01f;
            if (particle.Life > 1f) particle.Life = 0f;

            // Update trail
            particle.UpdateTrail();
        }
    }

    private Vector2 CalculateAudioForces(float[] waveform, float[] spectrum, float fftReactivity, float waveformReactivity)
    {
        var force = Vector2.Zero;

        // FFT-based force (spectrum)
        if (spectrum.Length > 0)
        {
            float lowFreq = 0f, midFreq = 0f, highFreq = 0f;
            
            // Low frequencies (bass)
            for (int i = 0; i < Math.Min(spectrum.Length / 4, 20); i++)
            {
                lowFreq += spectrum[i];
            }
            lowFreq /= Math.Min(spectrum.Length / 4, 20);

            // Mid frequencies
            for (int i = spectrum.Length / 4; i < spectrum.Length / 2; i++)
            {
                midFreq += spectrum[i];
            }
            midFreq /= (spectrum.Length / 2 - spectrum.Length / 4);

            // High frequencies
            for (int i = spectrum.Length / 2; i < spectrum.Length; i++)
            {
                highFreq += spectrum[i];
            }
            highFreq /= (spectrum.Length - spectrum.Length / 2);

            // Create directional force based on frequency distribution
            force.X = (midFreq - lowFreq) * fftReactivity;
            force.Y = (highFreq - midFreq) * fftReactivity;
        }

        // Waveform-based force
        if (waveform.Length > 0)
        {
            float waveEnergy = 0f;
            for (int i = 0; i < Math.Min(waveform.Length, 100); i++)
            {
                waveEnergy += Math.Abs(waveform[i]);
            }
            waveEnergy /= Math.Min(waveform.Length, 100);

            // Add circular force based on waveform energy
            force += new Vector2(
                MathF.Cos(_time * 2f) * waveEnergy * waveformReactivity,
                MathF.Sin(_time * 2f) * waveEnergy * waveformReactivity
            );
        }

        return force;
    }

    private Vector2 CalculateAttractionForce(Particle particle, Vector2 center, float strength)
    {
        var toCenter = center - particle.Position;
        float distance = toCenter.Length();
        if (distance < 0.1f) return Vector2.Zero;
        
        return Vector2.Normalize(toCenter) * strength * (1f / (1f + distance * 0.01f));
    }

    private Vector2 CalculateRepulsionForce(Particle particle, Vector2 center, float strength)
    {
        var fromCenter = particle.Position - center;
        float distance = fromCenter.Length();
        if (distance < 0.1f) return Vector2.Zero;
        
        return Vector2.Normalize(fromCenter) * strength * (1f / (1f + distance * 0.01f));
    }

    private Vector2 CalculateAlignmentForce(Particle particle, List<Particle> allParticles, float strength)
    {
        var alignment = Vector2.Zero;
        int neighbors = 0;

        foreach (var other in allParticles)
        {
            if (other == particle) continue;
            
            float distance = Vector2.Distance(particle.Position, other.Position);
            if (distance < 50f)
            {
                alignment += other.Velocity;
                neighbors++;
            }
        }

        if (neighbors > 0)
        {
            alignment /= neighbors;
            return Vector2.Normalize(alignment) * strength;
        }

        return Vector2.Zero;
    }

    private Vector2 CalculateCohesionForce(Particle particle, List<Particle> allParticles, Vector2 center, float strength)
    {
        var cohesion = Vector2.Zero;
        int neighbors = 0;

        foreach (var other in allParticles)
        {
            if (other == particle) continue;
            
            float distance = Vector2.Distance(particle.Position, other.Position);
            if (distance < 50f)
            {
                cohesion += other.Position;
                neighbors++;
            }
        }

        if (neighbors > 0)
        {
            cohesion /= neighbors;
            var toCohesion = cohesion - particle.Position;
            return Vector2.Normalize(toCohesion) * strength * 0.1f;
        }

        return Vector2.Zero;
    }

    private Vector2 CalculateMixedForces(Particle particle, List<Particle> allParticles, Vector2 center, float strength)
    {
        var attraction = CalculateAttractionForce(particle, center, strength * 0.5f);
        var alignment = CalculateAlignmentForce(particle, allParticles, strength * 0.3f);
        var cohesion = CalculateCohesionForce(particle, allParticles, center, strength * 0.2f);
        
        return attraction + alignment + cohesion;
    }

    private void RenderParticles(RenderContext ctx, uint baseColor, float particleSize, int trailLength)
    {
        foreach (var particle in _particles)
        {
            // Render trail
            if (trailLength > 0)
            {
                RenderParticleTrail(ctx, particle, baseColor, trailLength);
            }

            // Render particle
            uint particleColor = CalculateParticleColor(baseColor, particle.Life);
            float size = particle.Size * particleSize;
            
            ctx.Canvas!.FillCircle(particle.Position.X, particle.Position.Y, size, particleColor);
            
            // Add glow effect
            uint glowColor = ApplyAlpha(particleColor, 0.3f);
            ctx.Canvas.DrawCircle(particle.Position.X, particle.Position.Y, size * 2f, glowColor, false);
        }
    }

    private void RenderParticleTrail(RenderContext ctx, Particle particle, uint baseColor, int trailLength)
    {
        for (int i = 0; i < Math.Min(particle.Trail.Count, trailLength); i++)
        {
            var trailPos = particle.Trail[i];
            float alpha = 1f - (i / (float)trailLength);
            uint trailColor = ApplyAlpha(baseColor, alpha * 0.6f);
            
            float trailSize = particle.Size * 0.5f * alpha;
            ctx.Canvas!.FillCircle(trailPos.X, trailPos.Y, trailSize, trailColor);
        }
    }

    private uint CalculateParticleColor(uint baseColor, float life)
    {
        // Create color variation based on particle life
        byte r = (byte)(baseColor >> 16);
        byte g = (byte)(baseColor >> 8);
        byte b = (byte)baseColor;

        // Add life-based variation
        float lifeFactor = 0.5f + 0.5f * MathF.Sin(life * MathF.PI * 2f);
        r = (byte)(r * (0.7f + lifeFactor * 0.3f));
        g = (byte)(g * (0.7f + lifeFactor * 0.3f));
        b = (byte)(b * (0.7f + lifeFactor * 0.3f));

        return (uint)((255 << 24) | (r << 16) | (g << 8) | b);
    }

    private uint ApplyAlpha(uint color, float alpha)
    {
        byte r = (byte)(color >> 16);
        byte g = (byte)(color >> 8);
        byte b = (byte)color;
        byte a = (byte)(alpha * 255);
        
        return (uint)((a << 24) | (r << 16) | (g << 8) | b);
    }

    private uint ParseColor(string colorString)
    {
        // Simple hex color parser
        if (colorString.StartsWith("#") && colorString.Length == 7)
        {
            string hex = colorString.Substring(1);
            if (uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out uint color))
            {
                return 0xFF000000 | color; // Add full alpha
            }
        }
        return 0xFF00FFCC; // Default cyan
    }

    private class Particle
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Life { get; set; }
        public float Size { get; set; }
        public List<Vector2> Trail { get; } = new();

        public void UpdateTrail()
        {
            Trail.Insert(0, Position);
            if (Trail.Count > 20) Trail.RemoveAt(Trail.Count - 1);
        }
    }
}