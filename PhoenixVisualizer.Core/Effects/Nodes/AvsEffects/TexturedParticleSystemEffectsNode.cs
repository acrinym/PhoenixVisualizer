using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Advanced Textured Particle System Effects
    /// High-performance particle engine with physics, textures, and complex behaviors
    /// </summary>
    public class TexturedParticleSystemEffectsNode : BaseEffectNode
    {
        #region Properties

        public bool Enabled { get; set; } = true;



        public int MaxParticles { get; set; } = 1000;
        public int EmissionRate { get; set; } = 50; // particles per second
        public float ParticleLifetime { get; set; } = 3.0f;
        public float EmitterX { get; set; } = 0.5f; // Normalized position
        public float EmitterY { get; set; } = 0.5f;
        public float EmitterRadius { get; set; } = 0.1f;
        public int EmitterShape { get; set; } = 0; // 0=Point, 1=Circle, 2=Line, 3=Rectangle
        
        // Physics
        public float GravityX { get; set; } = 0.0f;
        public float GravityY { get; set; } = 0.5f;
        public float AirResistance { get; set; } = 0.01f;
        public float InitialVelocityMin { get; set; } = 0.5f;
        public float InitialVelocityMax { get; set; } = 2.0f;
        public float VelocityAngleSpread { get; set; } = 360.0f; // degrees
        
        // Appearance
        public Color StartColor { get; set; } = Color.White;
        public Color EndColor { get; set; } = Color.Transparent;
        public float StartSize { get; set; } = 2.0f;
        public float EndSize { get; set; } = 8.0f;
        public bool UseTexture { get; set; } = true;
        public int TextureType { get; set; } = 0; // 0=Circle, 1=Star, 2=Square, 3=Diamond
        
        // Behavior
        public bool BeatReactive { get; set; } = false;
        public float BeatEmissionBoost { get; set; } = 3.0f;
        public bool AudioReactiveColor { get; set; } = false;
        public bool AudioReactiveSize { get; set; } = false;
        public float AudioSensitivity { get; set; } = 1.0f;
        
        // Advanced features
        public bool EnableCollisions { get; set; } = false;
        public float CollisionBounce { get; set; } = 0.8f;
        public bool EnableAttractors { get; set; } = false;
        public int AttractorCount { get; set; } = 3;
        public float AttractorStrength { get; set; } = 1.0f;
        
        // Blending
        public int BlendMode { get; set; } = 1; // 0=Normal, 1=Additive, 2=Multiply, 3=Screen
        public float GlobalOpacity { get; set; } = 1.0f;

        #endregion

        #region Private Classes

        private struct Particle
        {
            public float X, Y;
            public float VX, VY; // Velocity
            public float Life; // 0.0 to 1.0
            public float Age; // Time alive
            public float Size;
            public Color Color;
            public float Rotation;
            public float RotationSpeed;
            public bool Active;
        }

        private struct Attractor
        {
            public float X, Y;
            public float Strength;
            public float Range;
            public bool Active;
        }

        #endregion

        #region Private Fields

        private Particle[]? _particles;
        private Attractor[]? _attractors;
        private int _activeParticleCount = 0;
        private float _emissionAccumulator = 0.0f;
        private int _beatCounter = 0;
        private readonly Random _random = new Random();
        private const int BEAT_DURATION = 30;
        
        // Texture patterns (simple procedural textures)
        private readonly float[,] _circleTexture = new float[16, 16];
        private readonly float[,] _starTexture = new float[16, 16];
        private readonly float[,] _squareTexture = new float[16, 16];
        private readonly float[,] _diamondTexture = new float[16, 16];

        #endregion

        #region Constructor

        public TexturedParticleSystemEffectsNode()
        {
            Name = "Textured Particle System Effects";
            Description = "Advanced particle engine with physics, textures, and complex behaviors";
            Category = "Particle Effects";
            
            InitializeParticleSystem();
            GenerateTextures();
        }

        #endregion

        #region Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Background", typeof(ImageBuffer), false, null, "Optional background image"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Particle system output"));
        }

        private void InitializeParticleSystem()
        {
            _particles = new Particle[MaxParticles];
            _attractors = new Attractor[AttractorCount];
            
            // Initialize attractors
            for (int i = 0; i < AttractorCount; i++)
            {
                _attractors[i] = new Attractor
                {
                    X = (float)_random.NextDouble(),
                    Y = (float)_random.NextDouble(),
                    Strength = AttractorStrength,
                    Range = 0.3f,
                    Active = true
                };
            }
        }

        private void GenerateTextures()
        {
            // Generate circle texture
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    float dx = (x - 7.5f) / 8.0f;
                    float dy = (y - 7.5f) / 8.0f;
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                    _circleTexture[y, x] = Math.Max(0, 1.0f - distance);
                    
                    // Square texture
                    _squareTexture[y, x] = (Math.Abs(dx) < 0.8f && Math.Abs(dy) < 0.8f) ? 1.0f : 0.0f;
                    
                    // Diamond texture
                    _diamondTexture[y, x] = Math.Max(0, 1.0f - (Math.Abs(dx) + Math.Abs(dy)));
                    
                    // Star texture (simplified)
                    float angle = (float)Math.Atan2(dy, dx);
                    float starValue = (float)(0.5 + 0.5 * Math.Sin(angle * 5)) * (1.0f - distance);
                    _starTexture[y, x] = Math.Max(0, starValue);
                }
            }
        }

        #endregion

        #region Effect Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            // Early exit if we're not supposed to render anything 🌙
#pragma warning disable CS8603 // Possible null reference return - acceptable for effect nodes
            if (!Enabled) return null;
#pragma warning restore CS8603

            try
            {
                var backgroundImage = GetInputValue<ImageBuffer>("Background", inputs);
                var outputImage = backgroundImage != null ?
                    new ImageBuffer(backgroundImage.Width, backgroundImage.Height) :
                    new ImageBuffer(640, 480);

                // Copy background
                if (backgroundImage != null)
                {
                    Array.Copy(backgroundImage.Data, outputImage.Data, backgroundImage.Data.Length);
                }

                // Handle beat reactivity
                if (BeatReactive && audioFeatures.Beat)
                {
                    _beatCounter = BEAT_DURATION;
                }
                else if (_beatCounter > 0)
                {
                    _beatCounter--;
                }

                // Update particle system
                UpdateParticleSystem(outputImage.Width, outputImage.Height, audioFeatures);

                // Render particles
                RenderParticles(outputImage, audioFeatures);

                return outputImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Textured Particle System] Error: {ex.Message}");
            }

            // In case of errors, return null so callers can handle gracefully 🛠️
#pragma warning disable CS8603 // Possible null reference return - acceptable for effect nodes
            return null;
#pragma warning restore CS8603
        }

        #endregion

        #region Private Methods

        private void UpdateParticleSystem(int width, int height, AudioFeatures audioFeatures)
        {
            float deltaTime = 0.016f; // 60 FPS
            
            // Update existing particles
            UpdateParticles(deltaTime, width, height, audioFeatures);
            
            // Emit new particles
            EmitParticles(deltaTime, width, height, audioFeatures);
            
            // Update attractors
            if (EnableAttractors)
            {
                UpdateAttractors(deltaTime, audioFeatures);
            }
        }

        private void UpdateParticles(float deltaTime, int width, int height, AudioFeatures audioFeatures)
        {
            for (int i = 0; i < _particles!.Length; i++)
            {
                if (!_particles![i].Active) continue;

                ref var particle = ref _particles![i];
                
                // Update age and life
                particle.Age += deltaTime;
                particle.Life = 1.0f - (particle.Age / ParticleLifetime);
                
                // Deactivate dead particles
                if (particle.Life <= 0)
                {
                    particle.Active = false;
                    _activeParticleCount--;
                    continue;
                }
                
                // Apply physics
                ApplyPhysics(ref particle, deltaTime, width, height);
                
                // Update appearance
                UpdateParticleAppearance(ref particle, audioFeatures);
                
                // Handle collisions
                if (EnableCollisions)
                {
                    HandleCollisions(ref particle, width, height);
                }
                
                // Apply attractors
                if (EnableAttractors)
                {
                    ApplyAttractors(ref particle, width, height);
                }
            }
        }

        private void ApplyPhysics(ref Particle particle, float deltaTime, int width, int height)
        {
            // Apply gravity
            particle.VX += GravityX * deltaTime;
            particle.VY += GravityY * deltaTime;
            
            // Apply air resistance
            particle.VX *= (1.0f - AirResistance * deltaTime);
            particle.VY *= (1.0f - AirResistance * deltaTime);
            
            // Update position
            particle.X += particle.VX * deltaTime;
            particle.Y += particle.VY * deltaTime;
            
            // Update rotation
            particle.Rotation += particle.RotationSpeed * deltaTime;
        }

        private void HandleCollisions(ref Particle particle, int width, int height)
        {
            // Boundary collisions
            if (particle.X < 0)
            {
                particle.X = 0;
                particle.VX = -particle.VX * CollisionBounce;
            }
            else if (particle.X >= 1.0f)
            {
                particle.X = 1.0f;
                particle.VX = -particle.VX * CollisionBounce;
            }
            
            if (particle.Y < 0)
            {
                particle.Y = 0;
                particle.VY = -particle.VY * CollisionBounce;
            }
            else if (particle.Y >= 1.0f)
            {
                particle.Y = 1.0f;
                particle.VY = -particle.VY * CollisionBounce;
            }
        }

        private void ApplyAttractors(ref Particle particle, int width, int height)
        {
            for (int i = 0; i < _attractors!.Length; i++)
            {
                if (!_attractors![i].Active) continue;

                var attractor = _attractors![i];
                float dx = attractor.X - particle.X;
                float dy = attractor.Y - particle.Y;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                
                if (distance < attractor.Range && distance > 0.001f)
                {
                    float force = attractor.Strength / (distance * distance + 0.001f);
                    particle.VX += (dx / distance) * force * 0.016f;
                    particle.VY += (dy / distance) * force * 0.016f;
                }
            }
        }

        private void UpdateParticleAppearance(ref Particle particle, AudioFeatures audioFeatures)
        {
            // Interpolate color
            float life = particle.Life;
            int startR = StartColor.R, startG = StartColor.G, startB = StartColor.B, startA = StartColor.A;
            int endR = EndColor.R, endG = EndColor.G, endB = EndColor.B, endA = EndColor.A;
            
            int r = (int)(startR * life + endR * (1 - life));
            int g = (int)(startG * life + endG * (1 - life));
            int b = (int)(startB * life + endB * (1 - life));
            int a = (int)(startA * life + endA * (1 - life));
            
            // Apply audio reactivity to color
            if (AudioReactiveColor && audioFeatures != null)
            {
                float audioFactor = audioFeatures.RMS * AudioSensitivity;
                r = (int)Math.Min(255, r * (1.0f + audioFactor));
                g = (int)Math.Min(255, g * (1.0f + audioFactor * 0.5f));
                b = (int)Math.Min(255, b * (1.0f + audioFactor * 0.3f));
            }
            
            particle.Color = Color.FromArgb(
                Math.Max(0, Math.Min(255, a)),
                Math.Max(0, Math.Min(255, r)),
                Math.Max(0, Math.Min(255, g)),
                Math.Max(0, Math.Min(255, b))
            );
            
            // Interpolate size
            particle.Size = StartSize * life + EndSize * (1 - life);
            
            // Apply audio reactivity to size
            if (AudioReactiveSize && audioFeatures != null)
            {
                particle.Size *= (1.0f + audioFeatures.RMS * AudioSensitivity * 0.5f);
            }
        }

        private void EmitParticles(float deltaTime, int width, int height, AudioFeatures audioFeatures)
        {
            // Calculate effective emission rate
            float effectiveEmissionRate = EmissionRate;
            if (BeatReactive && _beatCounter > 0)
            {
                float beatFactor = (_beatCounter / (float)BEAT_DURATION);
                effectiveEmissionRate *= (1.0f + (BeatEmissionBoost - 1.0f) * beatFactor);
            }
            
            // Accumulate particles to emit
            _emissionAccumulator += effectiveEmissionRate * deltaTime;
            int particlesToEmit = (int)_emissionAccumulator;
            _emissionAccumulator -= particlesToEmit;
            
            // Emit particles
            for (int i = 0; i < particlesToEmit && _activeParticleCount < MaxParticles; i++)
            {
                EmitParticle(width, height);
            }
        }

        private void EmitParticle(int width, int height)
        {
            // Find inactive particle slot
            for (int i = 0; i < _particles!.Length; i++)
            {
                if (!_particles![i].Active)
                {
                    ref var particle = ref _particles![i];
                    
                    // Set initial position based on emitter shape
                    SetEmitterPosition(ref particle);
                    
                    // Set initial velocity
                    // Generate a launch angle in radians 🎯
                    float angle = (float)(_random.NextDouble() * VelocityAngleSpread * Math.PI / 180.0);
                    float speed = InitialVelocityMin + (float)_random.NextDouble() * (InitialVelocityMax - InitialVelocityMin);
                    particle.VX = (float)Math.Cos(angle) * speed;
                    particle.VY = (float)Math.Sin(angle) * speed;
                    
                    // Set initial properties
                    particle.Life = 1.0f;
                    particle.Age = 0.0f;
                    particle.Size = StartSize;
                    particle.Color = StartColor;
                    particle.Rotation = 0.0f;
                    particle.RotationSpeed = ((float)_random.NextDouble() - 0.5f) * 10.0f;
                    particle.Active = true;
                    
                    _activeParticleCount++;
                    break;
                }
            }
        }

        private void SetEmitterPosition(ref Particle particle)
        {
            switch (EmitterShape)
            {
                case 0: // Point
                    particle.X = EmitterX;
                    particle.Y = EmitterY;
                    break;
                    
                case 1: // Circle
                    float angle = (float)(_random.NextDouble() * 2 * Math.PI);
                    float radius = (float)_random.NextDouble() * EmitterRadius;
                    particle.X = EmitterX + (float)Math.Cos(angle) * radius;
                    particle.Y = EmitterY + (float)Math.Sin(angle) * radius;
                    break;
                    
                case 2: // Line
                    particle.X = EmitterX + ((float)_random.NextDouble() - 0.5f) * EmitterRadius * 2;
                    particle.Y = EmitterY;
                    break;
                    
                case 3: // Rectangle
                    particle.X = EmitterX + ((float)_random.NextDouble() - 0.5f) * EmitterRadius * 2;
                    particle.Y = EmitterY + ((float)_random.NextDouble() - 0.5f) * EmitterRadius * 2;
                    break;
            }
        }

        private void UpdateAttractors(float deltaTime, AudioFeatures audioFeatures)
        {
            // Simple attractor movement
            for (int i = 0; i < _attractors!.Length; i++)
            {
                ref var attractor = ref _attractors![i];
                
                // Circular movement
                float angle = (float)(DateTime.Now.Ticks / 10000000.0) * (i + 1) * 0.5f;
                attractor.X = 0.5f + (float)Math.Cos(angle) * 0.3f;
                attractor.Y = 0.5f + (float)Math.Sin(angle) * 0.3f;
                
                // Audio reactive strength
                if (audioFeatures != null)
                {
                    attractor.Strength = AttractorStrength * (1.0f + audioFeatures.RMS * AudioSensitivity);
                }
            }
        }

        private void RenderParticles(ImageBuffer output, AudioFeatures audioFeatures)
        {
            int width = output.Width;
            int height = output.Height;
            
            for (int i = 0; i < _particles!.Length; i++)
            {
                if (!_particles![i].Active) continue;

                var particle = _particles![i];
                
                // Convert to screen coordinates
                int screenX = (int)(particle.X * width);
                int screenY = (int)(particle.Y * height);
                
                // Render particle
                if (UseTexture)
                {
                    RenderTexturedParticle(output, screenX, screenY, particle);
                }
                else
                {
                    RenderSimpleParticle(output, screenX, screenY, particle);
                }
            }
        }

        private void RenderTexturedParticle(ImageBuffer output, int centerX, int centerY, Particle particle)
        {
            int width = output.Width;
            int height = output.Height;
            int size = (int)Math.Ceiling(particle.Size);
            
            // Get texture
            float[,] texture = GetTexture(TextureType);
            
            for (int dy = -size; dy <= size; dy++)
            {
                for (int dx = -size; dx <= size; dx++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;
                    
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        // Sample texture
                        float texU = (dx + size) / (float)(size * 2) * 15.99f;
                        float texV = (dy + size) / (float)(size * 2) * 15.99f;
                        
                        if (texU >= 0 && texU < 16 && texV >= 0 && texV < 16)
                        {
                            float texValue = texture[(int)texV, (int)texU];
                            
                            if (texValue > 0.01f)
                            {
                                uint particleColor = (uint)((particle.Color.A << 24) | (particle.Color.R << 16) | 
                                                          (particle.Color.G << 8) | particle.Color.B);
                                
                                // Apply texture alpha
                                uint alpha = (uint)((particleColor >> 24) * texValue * GlobalOpacity);
                                particleColor = (alpha << 24) | (particleColor & 0x00FFFFFF);
                                
                                // Blend with background
                                uint existingPixel = output.Data[y * width + x];
                                output.Data[y * width + x] = BlendPixel(existingPixel, particleColor);
                            }
                        }
                    }
                }
            }
        }

        private void RenderSimpleParticle(ImageBuffer output, int centerX, int centerY, Particle particle)
        {
            int width = output.Width;
            int height = output.Height;
            int radius = (int)Math.Ceiling(particle.Size);
            
            uint particleColor = (uint)((particle.Color.A << 24) | (particle.Color.R << 16) | 
                                      (particle.Color.G << 8) | particle.Color.B);
            
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (distance <= particle.Size)
                    {
                        int x = centerX + dx;
                        int y = centerY + dy;
                        
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            float alpha = 1.0f - (distance / particle.Size);
                            uint finalColor = ApplyAlpha(particleColor, alpha * GlobalOpacity);
                            
                            uint existingPixel = output.Data[y * width + x];
                            output.Data[y * width + x] = BlendPixel(existingPixel, finalColor);
                        }
                    }
                }
            }
        }

        private float[,] GetTexture(int textureType)
        {
            return textureType switch
            {
                0 => _circleTexture,
                1 => _starTexture,
                2 => _squareTexture,
                3 => _diamondTexture,
                _ => _circleTexture
            };
        }

        private uint ApplyAlpha(uint color, float alpha)
        {
            uint a = (uint)((color >> 24) * alpha);
            return (a << 24) | (color & 0x00FFFFFF);
        }

        private uint BlendPixel(uint background, uint foreground)
        {
            uint fgA = (foreground >> 24) & 0xFF;
            if (fgA == 0) return background;
            
            uint bgR = (background >> 16) & 0xFF;
            uint bgG = (background >> 8) & 0xFF;
            uint bgB = background & 0xFF;
            uint fgR = (foreground >> 16) & 0xFF;
            uint fgG = (foreground >> 8) & 0xFF;
            uint fgB = foreground & 0xFF;
            
            switch (BlendMode)
            {
                case 0: // Normal
                    float alpha = fgA / 255.0f;
                    return (255u << 24) | 
                           ((uint)(bgR * (1 - alpha) + fgR * alpha) << 16) |
                           ((uint)(bgG * (1 - alpha) + fgG * alpha) << 8) |
                           (uint)(bgB * (1 - alpha) + fgB * alpha);
                    
                case 1: // Additive
                    return (255u << 24) |
                           (Math.Min(255u, bgR + fgR) << 16) |
                           (Math.Min(255u, bgG + fgG) << 8) |
                           Math.Min(255u, bgB + fgB);
                    
                case 2: // Multiply
                    return (255u << 24) |
                           ((bgR * fgR / 255) << 16) |
                           ((bgG * fgG / 255) << 8) |
                           (bgB * fgB / 255);
                    
                case 3: // Screen
                    return (255u << 24) |
                           ((255 - (255 - bgR) * (255 - fgR) / 255) << 16) |
                           ((255 - (255 - bgG) * (255 - fgG) / 255) << 8) |
                           (255 - (255 - bgB) * (255 - fgB) / 255);
                    
                default:
                    return background;
            }
        }

        #endregion

        #region Configuration

        #endregion
    }
}
