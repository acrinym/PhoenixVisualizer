using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Advanced particle system effect that creates dynamic particle simulations
    /// with physics, audio reactivity, and various emission patterns.
    /// </summary>
    public class ParticleSystemsEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>Whether the particle system is enabled.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Maximum number of particles in the system.</summary>
        public int MaxParticles { get; set; } = 1000;

        /// <summary>Particle emission rate (particles per second).</summary>
        public float EmissionRate { get; set; } = 50.0f;

        /// <summary>Particle lifetime in seconds.</summary>
        public float ParticleLifetime { get; set; } = 3.0f;

        /// <summary>Initial particle velocity range.</summary>
        public float InitialVelocity { get; set; } = 100.0f;

        /// <summary>Gravity force applied to particles.</summary>
        public float Gravity { get; set; } = 98.0f;

        /// <summary>Air resistance/drag coefficient.</summary>
        public float Drag { get; set; } = 0.1f;

        /// <summary>Particle size range (min, max).</summary>
        public Vector2 ParticleSize { get; set; } = new Vector2(2.0f, 8.0f);

        /// <summary>Emission pattern type.</summary>
        public EmissionPattern Pattern { get; set; } = EmissionPattern.Point;

        /// <summary>Emission area size for area-based patterns.</summary>
        public Vector2 EmissionArea { get; set; } = new Vector2(100.0f, 100.0f);

        /// <summary>Whether particles respond to audio input.</summary>
        public bool AudioReactive { get; set; } = true;

        /// <summary>Audio sensitivity multiplier.</summary>
        public float AudioSensitivity { get; set; } = 1.0f;

        /// <summary>Whether to use beat detection for particle bursts.</summary>
        public bool BeatReactive { get; set; } = true;

        /// <summary>Color gradient for particles.</summary>
        public Color[] ParticleColors { get; set; } = new Color[5];

        /// <summary>Particle blending mode.</summary>
        public BlendMode BlendMode { get; set; } = BlendMode.Additive;

        /// <summary>Whether to enable particle trails.</summary>
        public bool EnableTrails { get; set; } = false;

        /// <summary>Trail length in frames.</summary>
        public int TrailLength { get; set; } = 10;

        #endregion

        #region Private Fields

        private readonly List<Particle> _particles = new List<Particle>();
        private readonly List<ParticleTrail> _trails = new List<ParticleTrail>();
        private readonly Random _random = new Random();
        private float _emissionAccumulator;
        private float _time;
        private int _frameCounter;
        private Vector2 _emissionCenter;

        #endregion

        #region Constructor

        public ParticleSystemsEffectsNode()
        {
            Name = "Particle Systems Effects";
            Description = "Advanced particle system with physics, audio reactivity, and multiple emission patterns";
            Category = "Particle Effects";

            InitializeParticleColors();
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Input image for overlay"));
            _inputPorts.Add(new EffectPort("Audio", typeof(AudioFeatures), false, null, "Audio input for reactivity"));
            _inputPorts.Add(new EffectPort("Enabled", typeof(bool), false, true, "Enable/disable particle system"));
            _inputPorts.Add(new EffectPort("EmissionRate", typeof(float), false, 50.0f, "Particles per second"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Image with particle effects"));
        }

        #endregion

        #region Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var imageObj) || imageObj is not ImageBuffer imageBuffer)
                return GetDefaultOutput();

            if (inputs.TryGetValue("Enabled", out var en))
                Enabled = (bool)en;
            if (inputs.TryGetValue("EmissionRate", out var rate))
                EmissionRate = Math.Max(0.1f, (float)rate);

            if (!Enabled)
                return imageBuffer;

            _frameCounter++;
            _time += 1.0f / 60.0f; // Assume 60 FPS

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height, (int[])imageBuffer.Pixels.Clone());
            
            _emissionCenter = new Vector2(output.Width / 2.0f, output.Height / 2.0f);

            UpdateParticles(audioFeatures);
            EmitParticles();
            RenderParticles(output);

            return output;
        }

        #endregion

        #region Particle Management

        private void UpdateParticles(AudioFeatures audioFeatures)
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var particle = _particles[i];
                
                // Update particle physics
                particle.Velocity.Y += Gravity * (1.0f / 60.0f);
                particle.Velocity *= (1.0f - Drag * (1.0f / 60.0f));
                particle.Position += particle.Velocity * (1.0f / 60.0f);
                particle.Life -= 1.0f / 60.0f;

                // Apply audio forces if enabled
                if (AudioReactive && audioFeatures != null)
                {
                    ApplyAudioForces(particle, audioFeatures);
                }

                // Remove dead particles
                if (particle.Life <= 0)
                {
                    if (EnableTrails)
                    {
                        CreateTrail(particle);
                    }
                    _particles.RemoveAt(i);
                }
            }

            // Update trails
            UpdateTrails();
        }

        private void EmitParticles()
        {
            if (_particles.Count >= MaxParticles)
                return;

            _emissionAccumulator += EmissionRate * (1.0f / 60.0f);
            int particlesToEmit = (int)_emissionAccumulator;
            _emissionAccumulator -= particlesToEmit;

            for (int i = 0; i < particlesToEmit && _particles.Count < MaxParticles; i++)
            {
                var particle = CreateParticle();
                _particles.Add(particle);
            }
        }

        private Particle CreateParticle()
        {
            Vector2 position = GetEmissionPosition();
            Vector2 velocity = GetEmissionVelocity();
            float size = _random.NextSingle() * (ParticleSize.Y - ParticleSize.X) + ParticleSize.X;
            Color color = GetRandomParticleColor();

            return new Particle
            {
                Position = position,
                Velocity = velocity,
                Size = size,
                Color = color,
                Life = ParticleLifetime,
                MaxLife = ParticleLifetime
            };
        }

        private Vector2 GetEmissionPosition()
        {
            switch (Pattern)
            {
                case EmissionPattern.Point:
                    return _emissionCenter;

                case EmissionPattern.Circle:
                    float angle = _random.NextSingle() * 2.0f * (float)Math.PI;
                    float radius = _random.NextSingle() * EmissionArea.X * 0.5f;
                    return _emissionCenter + new Vector2(
                        (float)Math.Cos(angle) * radius,
                        (float)Math.Sin(angle) * radius
                    );

                case EmissionPattern.Rectangle:
                    return _emissionCenter + new Vector2(
                        (_random.NextSingle() - 0.5f) * EmissionArea.X,
                        (_random.NextSingle() - 0.5f) * EmissionArea.Y
                    );

                case EmissionPattern.Line:
                    float t = _random.NextSingle();
                    return _emissionCenter + new Vector2(
                        (t - 0.5f) * EmissionArea.X,
                        0
                    );

                default:
                    return _emissionCenter;
            }
        }

        private Vector2 GetEmissionVelocity()
        {
            float speed = InitialVelocity * (0.5f + _random.NextSingle() * 0.5f);
            float angle = _random.NextSingle() * 2.0f * (float)Math.PI;
            
            return new Vector2(
                (float)Math.Cos(angle) * speed,
                (float)Math.Sin(angle) * speed
            );
        }

        private Color GetRandomParticleColor()
        {
            if (ParticleColors.Length == 0)
                return Color.White;

            int index = _random.Next(ParticleColors.Length);
            return ParticleColors[index];
        }

        #endregion

        #region Audio Reactivity

        private void ApplyAudioForces(Particle particle, AudioFeatures audioFeatures)
        {
            if (audioFeatures.SpectrumData == null || audioFeatures.SpectrumData.Length == 0)
                return;

            // Get audio energy from spectrum
            float audioEnergy = 0.0f;
            for (int i = 0; i < Math.Min(audioFeatures.SpectrumData.Length, 32); i++)
            {
                audioEnergy += audioFeatures.SpectrumData[i];
            }
            audioEnergy /= 32.0f;

                            // Apply beat-reactive forces
                if (BeatReactive && audioFeatures.IsBeat)
                {
                    float beatForce = audioEnergy * AudioSensitivity * 200.0f;
                    Vector2 beatVelocity = particle.Velocity + new Vector2(
                        (_random.NextSingle() - 0.5f) * beatForce,
                        (_random.NextSingle() - 0.5f) * beatForce
                    );
                    particle.Velocity = beatVelocity;
                }

                // Apply continuous audio forces
                float audioForce = audioEnergy * AudioSensitivity * 50.0f;
                Vector2 audioDirection = Vector2.Normalize(particle.Position - _emissionCenter);
                Vector2 newVelocity = particle.Velocity + audioDirection * audioForce * (1.0f / 60.0f);
                particle.Velocity = newVelocity;
        }

        #endregion

        #region Trail System

        private void CreateTrail(Particle particle)
        {
            if (_trails.Count >= MaxParticles)
                return;

            var trail = new ParticleTrail
            {
                Positions = new Vector2[TrailLength],
                Colors = new Color[TrailLength],
                Life = TrailLength
            };

            // Initialize trail with particle history
            for (int i = 0; i < TrailLength; i++)
            {
                trail.Positions[i] = particle.Position;
                trail.Colors[i] = particle.Color;
            }

            _trails.Add(trail);
        }

        private void UpdateTrails()
        {
            for (int i = _trails.Count - 1; i >= 0; i--)
            {
                var trail = _trails[i];
                trail.Life--;

                if (trail.Life <= 0)
                {
                    _trails.RemoveAt(i);
                }
            }
        }

        #endregion

        #region Rendering

        private void RenderParticles(ImageBuffer output)
        {
            // Render trails first (behind particles)
            if (EnableTrails)
            {
                RenderTrails(output);
            }

            // Render particles
            foreach (var particle in _particles)
            {
                RenderParticle(output, particle);
            }
        }

        private void RenderParticle(ImageBuffer output, Particle particle)
        {
            int x = (int)particle.Position.X;
            int y = (int)particle.Position.Y;
            int size = (int)particle.Size;

            if (x < -size || x >= output.Width + size || y < -size || y >= output.Height + size)
                return;

            // Calculate alpha based on life
            float alpha = particle.Life / particle.MaxLife;
            Color color = Color.FromArgb(
                (int)(particle.Color.A * alpha),
                particle.Color.R,
                particle.Color.G,
                particle.Color.B
            );

            // Render particle as a circle
            for (int dy = -size; dy <= size; dy++)
            {
                for (int dx = -size; dx <= size; dx++)
                {
                    if (dx * dx + dy * dy <= size * size)
                    {
                        int px = x + dx;
                        int py = y + dy;
                        
                        if (px >= 0 && px < output.Width && py >= 0 && py < output.Height)
                        {
                            int currentColor = output.GetPixel(px, py);
                            int blendedColor = BlendColors(currentColor, color.ToArgb());
                            output.SetPixel(px, py, blendedColor);
                        }
                    }
                }
            }
        }

        private void RenderTrails(ImageBuffer output)
        {
            foreach (var trail in _trails)
            {
                for (int i = 0; i < trail.Life; i++)
                {
                    float alpha = (float)i / TrailLength;
                    Color color = Color.FromArgb(
                        (int)(255 * alpha * 0.5f),
                        trail.Colors[i].R,
                        trail.Colors[i].G,
                        trail.Colors[i].B
                    );

                    int x = (int)trail.Positions[i].X;
                    int y = (int)trail.Positions[i].Y;

                    if (x >= 0 && x < output.Width && y >= 0 && y < output.Height)
                    {
                        int currentColor = output.GetPixel(x, y);
                        int blendedColor = BlendColors(currentColor, color.ToArgb());
                        output.SetPixel(x, y, blendedColor);
                    }
                }
            }
        }

        private int BlendColors(int baseColor, int overlayColor)
        {
            switch (BlendMode)
            {
                case BlendMode.Additive:
                    return BlendAdditive(baseColor, overlayColor);
                case BlendMode.Multiply:
                    return BlendMultiply(baseColor, overlayColor);
                case BlendMode.Screen:
                    return BlendScreen(baseColor, overlayColor);
                default:
                    return overlayColor;
            }
        }

        private int BlendAdditive(int baseColor, int overlayColor)
        {
            int r = Math.Min(255, ((baseColor & 0xFF) + (overlayColor & 0xFF)));
            int g = Math.Min(255, (((baseColor >> 8) & 0xFF) + ((overlayColor >> 8) & 0xFF)));
            int b = Math.Min(255, (((baseColor >> 16) & 0xFF) + ((overlayColor >> 16) & 0xFF)));
            return (b << 16) | (g << 8) | r;
        }

        private int BlendMultiply(int baseColor, int overlayColor)
        {
            int r = ((baseColor & 0xFF) * (overlayColor & 0xFF)) / 255;
            int g = (((baseColor >> 8) & 0xFF) * ((overlayColor >> 8) & 0xFF)) / 255;
            int b = (((baseColor >> 16) & 0xFF) * ((overlayColor >> 16) & 0xFF)) / 255;
            return (b << 16) | (g << 8) | r;
        }

        private int BlendScreen(int baseColor, int overlayColor)
        {
            int r = 255 - ((255 - (baseColor & 0xFF)) * (255 - (overlayColor & 0xFF))) / 255;
            int g = 255 - ((255 - ((baseColor >> 8) & 0xFF)) * (255 - ((overlayColor >> 8) & 0xFF))) / 255;
            int b = 255 - ((255 - ((baseColor >> 16) & 0xFF)) * (255 - ((overlayColor >> 16) & 0xFF))) / 255;
            return (b << 16) | (g << 8) | r;
        }

        #endregion

        #region Initialization

        private void InitializeParticleColors()
        {
            ParticleColors = new Color[]
            {
                Color.FromArgb(255, 255, 100, 100),   // Red
                Color.FromArgb(255, 100, 255, 100),   // Green
                Color.FromArgb(255, 100, 100, 255),   // Blue
                Color.FromArgb(255, 255, 255, 100),   // Yellow
                Color.FromArgb(255, 255, 100, 255)    // Magenta
            };
        }

        #endregion

        #region Public Methods

        public override void Reset()
        {
            base.Reset();
            _particles.Clear();
            _trails.Clear();
            _emissionAccumulator = 0;
            _time = 0;
            _frameCounter = 0;
        }

        public string GetParticleStats()
        {
            return $"Particles: {_particles.Count}/{MaxParticles}, Trails: {_trails.Count}, Frame: {_frameCounter}";
        }

        public int GetActiveParticleCount()
        {
            return _particles.Count;
        }

        public void SetEmissionCenter(Vector2 center)
        {
            _emissionCenter = center;
        }

        #endregion

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }
    }

    #region Supporting Classes

    /// <summary>
    /// Individual particle in the system
    /// </summary>
    public class Particle
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Size { get; set; }
        public Color Color { get; set; }
        public float Life { get; set; }
        public float MaxLife { get; set; }
    }

    /// <summary>
    /// Particle trail for motion blur effects
    /// </summary>
    public class ParticleTrail
    {
        public Vector2[] Positions { get; set; }
        public Color[] Colors { get; set; }
        public int Life { get; set; }
    }

    /// <summary>
    /// Available emission patterns
    /// </summary>
    public enum EmissionPattern
    {
        Point,
        Circle,
        Rectangle,
        Line
    }

    /// <summary>
    /// Available blending modes
    /// </summary>
    public enum BlendMode
    {
        Normal,
        Additive,
        Multiply,
        Screen
    }

    #endregion
}
