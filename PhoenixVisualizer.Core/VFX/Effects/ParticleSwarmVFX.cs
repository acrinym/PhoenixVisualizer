using System;
using System.Numerics;
using PhoenixVisualizer.Core.VFX;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.VFX.Effects
{
    /// <summary>
    /// Particle swarm VFX effect with audio reactivity
    /// </summary>
    [PhoenixVFX(
        Id = "particle_swarm",
        Name = "Particle Swarm",
        Category = "Particles",
        Version = "1.0.0",
        Author = "Phoenix Team",
        Description = "Dynamic particle swarm with audio-reactive behavior"
    )]
    public class ParticleSwarmVFX : BasePhoenixVFX
    {
        #region Parameters

        [VFXParameter(
            Id = "particle_count",
            Name = "Particle Count",
            Description = "Number of particles in the swarm",
            MinValue = 10,
            MaxValue = 10000,
            DefaultValue = 1000
        )]
        public int ParticleCount { get; set; } = 1000;

        [VFXParameter(
            Id = "swarm_speed",
            Name = "Swarm Speed",
            Description = "Base speed of particle movement",
            MinValue = 0.1f,
            MaxValue = 10.0f,
            DefaultValue = 2.0f
        )]
        public float SwarmSpeed { get; set; } = 2.0f;

        [VFXParameter(
            Id = "audio_reactivity",
            Name = "Audio Reactivity",
            Description = "How much audio affects particle behavior",
            MinValue = 0.0f,
            MaxValue = 2.0f,
            DefaultValue = 1.0f
        )]
        public float AudioReactivity { get; set; } = 1.0f;

        [VFXParameter(
            Id = "swarm_radius",
            Name = "Swarm Radius",
            Description = "Radius of the swarm area",
            MinValue = 10.0f,
            MaxValue = 500.0f,
            DefaultValue = 100.0f
        )]
        public float SwarmRadius { get; set; } = 100.0f;

        [VFXParameter(
            Id = "particle_size",
            Name = "Particle Size",
            Description = "Size of individual particles",
            MinValue = 1.0f,
            MaxValue = 20.0f,
            DefaultValue = 3.0f
        )]
        public float ParticleSize { get; set; } = 3.0f;

        [VFXParameter(
            Id = "color_hue",
            Name = "Color Hue",
            Description = "Base hue for particle colors",
            MinValue = 0.0f,
            MaxValue = 360.0f,
            DefaultValue = 200.0f
        )]
        public float ColorHue { get; set; } = 200.0f;

        #endregion

        #region Private Fields

        private Particle[] _particles = Array.Empty<Particle>();
        private Random _random = new Random();
        private Vector2 _swarmCenter;
        private float _time;

        #endregion

        #region Particle Structure

        private struct Particle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Life;
            public float MaxLife;
            public float Size;
            public float Hue;
            public float Saturation;
            public float Value;
        }

        #endregion

        #region Initialization

        protected override void OnInitialize(VFXRenderContext context, AudioFeatures audio)
        {
            base.OnInitialize(context, audio);
            
            _random = new Random();
            _swarmCenter = new Vector2(context.Width / 2.0f, context.Height / 2.0f);
            _time = 0.0f;
            
            InitializeParticles();
        }

        private void InitializeParticles()
        {
            _particles = new Particle[ParticleCount];
            
            for (int i = 0; i < ParticleCount; i++)
            {
                _particles[i] = CreateRandomParticle();
            }
        }

        private Particle CreateRandomParticle()
        {
            var angle = (float)(_random!.NextDouble() * Math.PI * 2.0);
            var radius = (float)(_random!.NextDouble() * SwarmRadius);
            
            return new Particle
            {
                Position = _swarmCenter + new Vector2(
                    (float)Math.Cos(angle) * radius,
                    (float)Math.Sin(angle) * radius
                ),
                Velocity = new Vector2(
                    (float)(_random!.NextDouble() - 0.5) * SwarmSpeed,
                    (float)(_random!.NextDouble() - 0.5) * SwarmSpeed
                ),
                Life = (float)(_random!.NextDouble() * 0.5 + 0.5),
                MaxLife = 1.0f,
                Size = ParticleSize * (float)(_random!.NextDouble() * 0.5 + 0.75),
                Hue = ColorHue + (float)(_random!.NextDouble() * 60.0 - 30.0),
                Saturation = (float)(_random!.NextDouble() * 0.3 + 0.7),
                Value = (float)(_random!.NextDouble() * 0.3 + 0.7)
            };
        }

        #endregion

        #region Frame Processing

        protected override void OnProcessFrame(VFXRenderContext context)
        {
            _time += context.DeltaTime;
            
            // Update swarm center based on audio
            UpdateSwarmCenter(context);
            
            // Update all particles
            for (int i = 0; i < _particles.Length; i++)
            {
                UpdateParticle(ref _particles[i], context);
            }
        }

        private void UpdateSwarmCenter(VFXRenderContext context)
        {
            // Make swarm center move based on audio
            var audioInfluence = AudioReactivity * 0.1f;
            var bassInfluence = (float)(_audio?.Bass ?? 0.0) * audioInfluence;
            var midInfluence = (float)(_audio?.Mid ?? 0.0) * audioInfluence;
            
            _swarmCenter.X = context.Width / 2.0f + bassInfluence * 50.0f;
            _swarmCenter.Y = context.Height / 2.0f + midInfluence * 50.0f;
        }

        private void UpdateParticle(ref Particle particle, VFXRenderContext context)
        {
            // Update life
            particle.Life -= context.DeltaTime * 0.5f;
            
            // Respawn if dead
            if (particle.Life <= 0.0f)
            {
                particle = CreateRandomParticle();
                return;
            }
            
            // Calculate audio influence
            var audioInfluence = AudioReactivity * 0.5f;
            var beatInfluence = (_audio?.BeatIntensity ?? 0.0f) * audioInfluence;
            var rmsInfluence = (float)(_audio?.RMS ?? 0.0) * audioInfluence;
            
            // Update velocity based on audio
            var beatForce = beatInfluence * 100.0f;
            var rmsForce = rmsInfluence * 50.0f;
            
            particle.Velocity += new Vector2(
                (float)(_random!.NextDouble() - 0.5) * beatForce * context.DeltaTime,
                (float)(_random!.NextDouble() - 0.5) * rmsForce * context.DeltaTime
            );
            
            // Apply swarm behavior
            var toCenter = _swarmCenter - particle.Position;
            var distance = toCenter.Length();
            
            if (distance > SwarmRadius)
            {
                var force = toCenter / distance * SwarmSpeed * 2.0f;
                particle.Velocity += force * context.DeltaTime;
            }
            
            // Limit velocity
            var maxSpeed = SwarmSpeed * (1.0f + beatInfluence);
            if (particle.Velocity.Length() > maxSpeed)
            {
                particle.Velocity = Vector2.Normalize(particle.Velocity) * maxSpeed;
            }
            
            // Update position
            particle.Position += particle.Velocity * context.DeltaTime;
            
            // Wrap around screen edges
            if (particle.Position.X < 0) particle.Position.X = context.Width;
            if (particle.Position.X > context.Width) particle.Position.X = 0;
            if (particle.Position.Y < 0) particle.Position.Y = context.Height;
            if (particle.Position.Y > context.Height) particle.Position.Y = 0;
            
            // Update color based on audio
            particle.Hue = ColorHue + beatInfluence * 30.0f;
            particle.Value = 0.7f + rmsInfluence * 0.3f;
        }

        #endregion

        #region GPU Processing Support

        protected override bool SupportsGPUProcessing() => false; // CPU-only for now

        #endregion
    }
}