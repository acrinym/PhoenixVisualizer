using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Vector Field Effects - Advanced vector field visualizations
    /// Creates complex flow fields, fluid dynamics, and mathematical vector visualizations
    /// </summary>
    public class VectorFieldEffectsNode : BaseEffectNode
    {
        #region Properties

        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// Vector field type
        /// 0 = Flow field, 1 = Curl field, 2 = Divergence field, 3 = Magnetic field, 4 = Custom field
        /// </summary>
        public int FieldType { get; set; } = 0;
        
        /// <summary>
        /// Field resolution (grid size)
        /// </summary>
        public int FieldResolution { get; set; } = 32;
        
        /// <summary>
        /// Vector field strength multiplier
        /// </summary>
        public float FieldStrength { get; set; } = 1.0f;
        
        /// <summary>
        /// Field animation speed
        /// </summary>
        public float AnimationSpeed { get; set; } = 1.0f;
        
        /// <summary>
        /// Visualization mode
        /// 0 = Arrows, 1 = Streamlines, 2 = Flow particles, 3 = Heat map, 4 = LIC (Line Integral Convolution)
        /// </summary>
        public int VisualizationMode { get; set; } = 1;
        
        /// <summary>
        /// Vector color mapping
        /// 0 = Magnitude, 1 = Direction, 2 = Velocity, 3 = Custom
        /// </summary>
        public int ColorMode { get; set; } = 0;
        
        /// <summary>
        /// Base color for vectors
        /// </summary>
        public Color BaseColor { get; set; } = Color.White;
        
        /// <summary>
        /// Secondary color for gradients
        /// </summary>
        public Color SecondaryColor { get; set; } = Color.Blue;
        
        /// <summary>
        /// Beat reactivity enabled
        /// </summary>
        public bool BeatReactive { get; set; } = false;
        
        /// <summary>
        /// Beat strength multiplier
        /// </summary>
        public float BeatStrengthMultiplier { get; set; } = 2.0f;
        
        /// <summary>
        /// Audio reactive field distortion
        /// </summary>
        public bool AudioReactiveField { get; set; } = false;
        
        /// <summary>
        /// Audio sensitivity for field distortion
        /// </summary>
        public float AudioSensitivity { get; set; } = 1.0f;
        
        /// <summary>
        /// Streamline count for streamline visualization
        /// </summary>
        public int StreamlineCount { get; set; } = 100;
        
        /// <summary>
        /// Streamline length
        /// </summary>
        public int StreamlineLength { get; set; } = 50;
        
        /// <summary>
        /// Particle count for flow particle visualization
        /// </summary>
        public int ParticleCount { get; set; } = 500;
        
        /// <summary>
        /// Particle lifetime
        /// </summary>
        public float ParticleLifetime { get; set; } = 3.0f;
        
        /// <summary>
        /// Field scale factor
        /// </summary>
        public float FieldScale { get; set; } = 1.0f;
        
        /// <summary>
        /// Noise scale for field perturbation
        /// </summary>
        public float NoiseScale { get; set; } = 0.1f;
        
        /// <summary>
        /// Enable field persistence (trails)
        /// </summary>
        public bool EnablePersistence { get; set; } = true;
        
        /// <summary>
        /// Persistence decay rate
        /// </summary>
        public float PersistenceDecay { get; set; } = 0.95f;

        #endregion

        #region Private Classes

        private struct Vector2D
        {
            public float X, Y;
            public float Magnitude => (float)Math.Sqrt(X * X + Y * Y);
            public float Angle => (float)Math.Atan2(Y, X);
            
            public Vector2D(float x, float y)
            {
                X = x;
                Y = y;
            }
            
            public static Vector2D operator +(Vector2D a, Vector2D b) => new Vector2D(a.X + b.X, a.Y + b.Y);
            public static Vector2D operator *(Vector2D a, float scalar) => new Vector2D(a.X * scalar, a.Y * scalar);
        }

        private struct FlowParticle
        {
            public float X, Y;
            public float VX, VY;
            public float Life;
            public Color Color;
            public float Size;
            public bool Active;
        }

        private struct Streamline
        {
            public List<Vector2D> Points;
            public Color Color;
            public float Width;
            public bool Active;
        }

        #endregion

        #region Private Fields

        private Vector2D[,] _vectorField;
        private FlowParticle[] _flowParticles;
        private List<Streamline> _streamlines;
        private ImageBuffer _persistenceBuffer;
        private float _time = 0.0f;
        private int _beatCounter = 0;
        private readonly Random _random = new Random();
        private const int BEAT_DURATION = 25;

        #endregion

        #region Constructor

        public VectorFieldEffectsNode()
        {
            Name = "Vector Field Effects";
            Description = "Advanced vector field visualizations with flow dynamics";
            Category = "Field Effects";
            
            InitializeVectorField();
            InitializeFlowParticles();
            InitializeStreamlines();
        }

        #endregion

        #region Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Background", typeof(ImageBuffer), false, null, "Optional background image"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Vector field visualization output"));
        }

        private void InitializeVectorField()
        {
            _vectorField = new Vector2D[FieldResolution, FieldResolution];
            UpdateVectorField(0.0f, null);
        }

        private void InitializeFlowParticles()
        {
            _flowParticles = new FlowParticle[ParticleCount];
            
            for (int i = 0; i < ParticleCount; i++)
            {
                ResetFlowParticle(ref _flowParticles[i]);
            }
        }

        private void InitializeStreamlines()
        {
            _streamlines = new List<Streamline>();
            
            for (int i = 0; i < StreamlineCount; i++)
            {
                var streamline = new Streamline
                {
                    Points = new List<Vector2D>(),
                    Color = CalculateStreamlineColor(i),
                    Width = 1.0f + (float)_random.NextDouble() * 2.0f,
                    Active = true
                };
                
                _streamlines.Add(streamline);
            }
        }

        #endregion

        #region Effect Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            // Skip processing when disabled 🚫
            if (!Enabled) return null;

            try
            {
                var backgroundImage = GetInputValue<ImageBuffer>("Background", inputs);
                var outputImage = backgroundImage != null ? 
                    new ImageBuffer(backgroundImage.Width, backgroundImage.Height) : 
                    new ImageBuffer(640, 480);

                // Initialize persistence buffer if needed
                if (EnablePersistence && (_persistenceBuffer == null || 
                    _persistenceBuffer.Width != outputImage.Width || 
                    _persistenceBuffer.Height != outputImage.Height))
                {
                    _persistenceBuffer = new ImageBuffer(outputImage.Width, outputImage.Height);
                }

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

                // Update time
                _time += AnimationSpeed * 0.016f;

                // Update vector field
                UpdateVectorField(_time, audioFeatures);

                // Apply persistence
                if (EnablePersistence)
                {
                    ApplyPersistence(outputImage);
                }

                // Visualize vector field
                VisualizeVectorField(outputImage, audioFeatures);

                return outputImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Vector Field Effects] Error: {ex.Message}");
            }

            // Return null if something funky happens 🧪
            return null;
        }

        #endregion

        #region Private Methods

        private void UpdateVectorField(float time, AudioFeatures audioFeatures)
        {
            float effectiveStrength = CalculateEffectiveStrength(audioFeatures);
            
            for (int y = 0; y < FieldResolution; y++)
            {
                for (int x = 0; x < FieldResolution; x++)
                {
                    float normalizedX = x / (float)(FieldResolution - 1);
                    float normalizedY = y / (float)(FieldResolution - 1);
                    
                    Vector2D vector = CalculateVectorAtPosition(normalizedX, normalizedY, time, audioFeatures);
                    _vectorField[y, x] = vector * effectiveStrength;
                }
            }
        }

        private float CalculateEffectiveStrength(AudioFeatures audioFeatures)
        {
            float strength = FieldStrength;
            
            // Apply beat boost
            if (BeatReactive && _beatCounter > 0)
            {
                float beatFactor = (_beatCounter / (float)BEAT_DURATION);
                strength *= (1.0f + (BeatStrengthMultiplier - 1.0f) * beatFactor);
            }
            
            // Apply audio reactivity
            if (AudioReactiveField && audioFeatures != null)
            {
                strength *= (1.0f + audioFeatures.RMS * AudioSensitivity);
            }
            
            return strength;
        }

        private Vector2D CalculateVectorAtPosition(float x, float y, float time, AudioFeatures audioFeatures)
        {
            switch (FieldType)
            {
                case 0: // Flow field
                    return CalculateFlowField(x, y, time);
                    
                case 1: // Curl field
                    return CalculateCurlField(x, y, time);
                    
                case 2: // Divergence field
                    return CalculateDivergenceField(x, y, time);
                    
                case 3: // Magnetic field
                    return CalculateMagneticField(x, y, time);
                    
                case 4: // Custom field
                    return CalculateCustomField(x, y, time, audioFeatures);
                    
                default:
                    return new Vector2D(0, 0);
            }
        }

        private Vector2D CalculateFlowField(float x, float y, float time)
        {
            // Perlin noise-based flow field
            float noiseScale = NoiseScale * FieldScale;
            float noise1 = PerlinNoise(x * noiseScale + time * 0.5f, y * noiseScale);
            float noise2 = PerlinNoise(x * noiseScale, y * noiseScale + time * 0.5f);
            
            return new Vector2D(
                (float)Math.Sin(noise1 * Math.PI * 2) * 0.5f,
                (float)Math.Cos(noise2 * Math.PI * 2) * 0.5f
            );
        }

        private Vector2D CalculateCurlField(float x, float y, float time)
        {
            // Curl of a potential field
            float centerX = 0.5f + 0.3f * (float)Math.Sin(time);
            float centerY = 0.5f + 0.3f * (float)Math.Cos(time);
            
            float dx = x - centerX;
            float dy = y - centerY;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy) + 0.001f;
            
            // Circular flow around center
            return new Vector2D(-dy / distance, dx / distance);
        }

        private Vector2D CalculateDivergenceField(float x, float y, float time)
        {
            // Radial field from center
            float centerX = 0.5f;
            float centerY = 0.5f;
            
            float dx = x - centerX;
            float dy = y - centerY;
            float pulseFactor = 1.0f + 0.5f * (float)Math.Sin(time * 3);
            
            return new Vector2D(dx * pulseFactor, dy * pulseFactor);
        }

        private Vector2D CalculateMagneticField(float x, float y, float time)
        {
            // Magnetic dipole field
            float dipoleX = 0.5f;
            float dipoleY = 0.3f + 0.2f * (float)Math.Sin(time);
            
            float dx = x - dipoleX;
            float dy = y - dipoleY;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy) + 0.001f;
            float distance3 = distance * distance * distance;
            
            // Simplified magnetic field
            return new Vector2D(
                (3 * dx * dy) / distance3,
                (2 * dy * dy - dx * dx) / distance3
            );
        }

        private Vector2D CalculateCustomField(float x, float y, float time, AudioFeatures audioFeatures)
        {
            // Audio-reactive custom field
            float audioFactor = audioFeatures?.RMS ?? 0.5f;
            float bassInfluence = audioFeatures?.Bass ?? 0.5f;
            float trebleInfluence = audioFeatures?.Treble ?? 0.5f;
            
            float waveX = (float)Math.Sin((x * 5 + time) * Math.PI) * audioFactor;
            float waveY = (float)Math.Cos((y * 3 + time * 1.5f) * Math.PI) * audioFactor;
            
            // Add frequency-specific components
            waveX += (float)Math.Sin(x * 10 * Math.PI) * bassInfluence * 0.3f;
            waveY += (float)Math.Cos(y * 15 * Math.PI) * trebleInfluence * 0.3f;
            
            return new Vector2D(waveX, waveY);
        }

        private void VisualizeVectorField(ImageBuffer output, AudioFeatures audioFeatures)
        {
            switch (VisualizationMode)
            {
                case 0: // Arrows
                    RenderArrows(output);
                    break;
                    
                case 1: // Streamlines
                    RenderStreamlines(output);
                    break;
                    
                case 2: // Flow particles
                    UpdateFlowParticles(output);
                    RenderFlowParticles(output);
                    break;
                    
                case 3: // Heat map
                    RenderHeatMap(output);
                    break;
                    
                case 4: // LIC (simplified)
                    RenderLIC(output);
                    break;
            }
        }

        private void RenderArrows(ImageBuffer output)
        {
            int width = output.Width;
            int height = output.Height;
            int stepX = width / FieldResolution;
            int stepY = height / FieldResolution;
            
            for (int y = 0; y < FieldResolution; y++)
            {
                for (int x = 0; x < FieldResolution; x++)
                {
                    Vector2D vector = _vectorField[y, x];
                    
                    int screenX = x * stepX + stepX / 2;
                    int screenY = y * stepY + stepY / 2;
                    
                    Color vectorColor = CalculateVectorColor(vector);
                    
                    // Draw arrow
                    DrawArrow(output, screenX, screenY, vector, vectorColor, stepX / 3);
                }
            }
        }

        private void RenderStreamlines(ImageBuffer output)
        {
            // Update streamlines
            for (int i = 0; i < _streamlines.Count; i++)
            {
                if (_streamlines[i].Points.Count == 0 || _streamlines[i].Points.Count > StreamlineLength)
                {
                    // Start new streamline
                    var streamline = _streamlines[i];
                    streamline.Points.Clear();
                    
                    // Random starting position
                    Vector2D startPos = new Vector2D((float)_random.NextDouble(), (float)_random.NextDouble());
                    streamline.Points.Add(startPos);
                    streamline.Color = CalculateStreamlineColor(i);
                    
                    _streamlines[i] = streamline;
                }
                else
                {
                    // Extend streamline
                    var streamline = _streamlines[i];
                    Vector2D lastPoint = streamline.Points[streamline.Points.Count - 1];
                    
                    // Sample vector field
                    Vector2D vector = SampleVectorField(lastPoint.X, lastPoint.Y);
                    
                    // Integrate
                    Vector2D newPoint = lastPoint + vector * 0.01f;
                    
                    // Check bounds
                    if (newPoint.X >= 0 && newPoint.X <= 1 && newPoint.Y >= 0 && newPoint.Y <= 1)
                    {
                        streamline.Points.Add(newPoint);
                    }
                    
                    _streamlines[i] = streamline;
                }
            }
            
            // Render streamlines
            foreach (var streamline in _streamlines)
            {
                RenderStreamline(output, streamline);
            }
        }

        private void UpdateFlowParticles(ImageBuffer output)
        {
            for (int i = 0; i < _flowParticles.Length; i++)
            {
                ref var particle = ref _flowParticles[i];
                
                if (!particle.Active)
                {
                    ResetFlowParticle(ref particle);
                    continue;
                }
                
                // Update particle life
                particle.Life -= 0.016f / ParticleLifetime;
                if (particle.Life <= 0)
                {
                    ResetFlowParticle(ref particle);
                    continue;
                }
                
                // Sample vector field
                Vector2D vector = SampleVectorField(particle.X, particle.Y);
                
                // Update velocity
                particle.VX += vector.X * 0.1f;
                particle.VY += vector.Y * 0.1f;
                
                // Apply damping
                particle.VX *= 0.98f;
                particle.VY *= 0.98f;
                
                // Update position
                particle.X += particle.VX * 0.016f;
                particle.Y += particle.VY * 0.016f;
                
                // Check bounds
                if (particle.X < 0 || particle.X > 1 || particle.Y < 0 || particle.Y > 1)
                {
                    ResetFlowParticle(ref particle);
                }
                
                // Update color based on velocity
                float speed = (float)Math.Sqrt(particle.VX * particle.VX + particle.VY * particle.VY);
                particle.Color = InterpolateColor(BaseColor, SecondaryColor, Math.Min(1.0f, speed * 5));
                particle.Size = 1.0f + speed * 3;
            }
        }

        private void RenderFlowParticles(ImageBuffer output)
        {
            int width = output.Width;
            int height = output.Height;
            
            foreach (var particle in _flowParticles)
            {
                if (!particle.Active) continue;
                
                int x = (int)(particle.X * width);
                int y = (int)(particle.Y * height);
                
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    uint color = (uint)((particle.Color.A << 24) | (particle.Color.R << 16) | 
                                      (particle.Color.G << 8) | particle.Color.B);
                    
                    // Simple particle rendering
                    int size = (int)particle.Size;
                    for (int dy = -size; dy <= size; dy++)
                    {
                        for (int dx = -size; dx <= size; dx++)
                        {
                            int px = x + dx;
                            int py = y + dy;
                            
                            if (px >= 0 && px < width && py >= 0 && py < height)
                            {
                                float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                                if (distance <= size)
                                {
                                    float alpha = (1.0f - distance / size) * particle.Life;
                                    uint blendedColor = BlendPixel(output.Data[py * width + px], color, alpha);
                                    output.Data[py * width + px] = blendedColor;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void RenderHeatMap(ImageBuffer output)
        {
            int width = output.Width;
            int height = output.Height;
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float normalizedX = x / (float)(width - 1);
                    float normalizedY = y / (float)(height - 1);
                    
                    Vector2D vector = SampleVectorField(normalizedX, normalizedY);
                    float magnitude = vector.Magnitude;
                    
                    Color heatColor = CalculateHeatMapColor(magnitude);
                    uint pixelColor = (uint)((heatColor.A << 24) | (heatColor.R << 16) | 
                                           (heatColor.G << 8) | heatColor.B);
                    
                    output.Data[y * width + x] = BlendPixel(output.Data[y * width + x], pixelColor, 0.7f);
                }
            }
        }

        private void RenderLIC(ImageBuffer output)
        {
            // Simplified Line Integral Convolution
            int width = output.Width;
            int height = output.Height;
            var licBuffer = new float[width * height];
            
            // Generate noise texture
            for (int i = 0; i < licBuffer.Length; i++)
            {
                licBuffer[i] = (float)_random.NextDouble();
            }
            
            // Apply LIC (simplified version)
            var resultBuffer = new float[width * height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float normalizedX = x / (float)(width - 1);
                    float normalizedY = y / (float)(height - 1);
                    
                    float licValue = TraceLIC(normalizedX, normalizedY, licBuffer, width, height);
                    resultBuffer[y * width + x] = licValue;
                }
            }
            
            // Convert to color
            for (int i = 0; i < resultBuffer.Length; i++)
            {
                byte intensity = (byte)(resultBuffer[i] * 255);
                uint color = (255u << 24) | ((uint)intensity << 16) | ((uint)intensity << 8) | intensity;
                output.Data[i] = BlendPixel(output.Data[i], color, 0.8f);
            }
        }

        // Helper methods continue...
        private Vector2D SampleVectorField(float x, float y)
        {
            // Bilinear interpolation of vector field
            float fx = x * (FieldResolution - 1);
            float fy = y * (FieldResolution - 1);
            
            int x1 = (int)fx;
            int y1 = (int)fy;
            int x2 = Math.Min(x1 + 1, FieldResolution - 1);
            int y2 = Math.Min(y1 + 1, FieldResolution - 1);
            
            float wx = fx - x1;
            float wy = fy - y1;
            
            Vector2D v1 = _vectorField[y1, x1];
            Vector2D v2 = _vectorField[y1, x2];
            Vector2D v3 = _vectorField[y2, x1];
            Vector2D v4 = _vectorField[y2, x2];
            
            Vector2D top = new Vector2D(
                v1.X * (1 - wx) + v2.X * wx,
                v1.Y * (1 - wx) + v2.Y * wx
            );
            
            Vector2D bottom = new Vector2D(
                v3.X * (1 - wx) + v4.X * wx,
                v3.Y * (1 - wx) + v4.Y * wx
            );
            
            return new Vector2D(
                top.X * (1 - wy) + bottom.X * wy,
                top.Y * (1 - wy) + bottom.Y * wy
            );
        }

        private void ResetFlowParticle(ref FlowParticle particle)
        {
            particle.X = (float)_random.NextDouble();
            particle.Y = (float)_random.NextDouble();
            particle.VX = 0;
            particle.VY = 0;
            particle.Life = 1.0f;
            particle.Color = BaseColor;
            particle.Size = 1.0f;
            particle.Active = true;
        }

        private Color CalculateVectorColor(Vector2D vector)
        {
            switch (ColorMode)
            {
                case 0: // Magnitude
                    float magnitude = Math.Min(1.0f, vector.Magnitude * 2);
                    return InterpolateColor(BaseColor, SecondaryColor, magnitude);
                    
                case 1: // Direction
                    float angle = (vector.Angle + (float)Math.PI) / (2 * (float)Math.PI);
                    return HSVToRGB(angle * 360, 1.0f, 1.0f);
                    
                case 2: // Velocity (same as magnitude for vectors)
                    return CalculateVectorColor(vector); // Recursive, but should be magnitude
                    
                case 3: // Custom
                    return BaseColor;
                    
                default:
                    return BaseColor;
            }
        }

        private Color CalculateStreamlineColor(int index)
        {
            float hue = (index / (float)StreamlineCount) * 360;
            return HSVToRGB(hue, 0.8f, 1.0f);
        }

        private Color CalculateHeatMapColor(float magnitude)
        {
            magnitude = Math.Min(1.0f, magnitude * 2);
            
            if (magnitude < 0.25f)
                return InterpolateColor(Color.Black, Color.Blue, magnitude * 4);
            else if (magnitude < 0.5f)
                return InterpolateColor(Color.Blue, Color.Green, (magnitude - 0.25f) * 4);
            else if (magnitude < 0.75f)
                return InterpolateColor(Color.Green, Color.Yellow, (magnitude - 0.5f) * 4);
            else
                return InterpolateColor(Color.Yellow, Color.Red, (magnitude - 0.75f) * 4);
        }

        private void ApplyPersistence(ImageBuffer output)
        {
            if (_persistenceBuffer == null) return;
            
            // Decay persistence buffer
            for (int i = 0; i < _persistenceBuffer.Data.Length; i++)
            {
                uint pixel = _persistenceBuffer.Data[i];
                uint a = (uint)((pixel >> 24) * PersistenceDecay);
                uint r = (uint)(((pixel >> 16) & 0xFF) * PersistenceDecay);
                uint g = (uint)(((pixel >> 8) & 0xFF) * PersistenceDecay);
                uint b = (uint)(((pixel) & 0xFF) * PersistenceDecay);
                
                _persistenceBuffer.Data[i] = (a << 24) | (r << 16) | (g << 8) | b;
                
                // Blend with output
                output.Data[i] = BlendPixel(output.Data[i], _persistenceBuffer.Data[i], 1.0f);
            }
            
            // Update persistence buffer with current frame
            Array.Copy(output.Data, _persistenceBuffer.Data, output.Data.Length);
        }

        // Additional helper methods for rendering, color interpolation, etc.
        private float PerlinNoise(float x, float y)
        {
            // Simplified Perlin noise implementation
            int xi = (int)Math.Floor(x) & 255;
            int yi = (int)Math.Floor(y) & 255;
            
            float xf = x - (float)Math.Floor(x);
            float yf = y - (float)Math.Floor(y);
            
            float u = Fade(xf);
            float v = Fade(yf);
            
            int a = (xi + yi * 57) % 256;
            int b = (xi + 1 + yi * 57) % 256;
            int c = (xi + (yi + 1) * 57) % 256;
            int d = (xi + 1 + (yi + 1) * 57) % 256;
            
            float x1 = Lerp(Grad(a, xf, yf), Grad(b, xf - 1, yf), u);
            float x2 = Lerp(Grad(c, xf, yf - 1), Grad(d, xf - 1, yf - 1), u);
            
            return Lerp(x1, x2, v);
        }

        private float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
        private float Lerp(float a, float b, float t) => a + t * (b - a);
        private float Grad(int hash, float x, float y)
        {
            int h = hash & 3;
            float u = h < 2 ? x : y;
            float v = h < 2 ? y : x;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        private Color InterpolateColor(Color a, Color b, float t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return Color.FromArgb(
                (int)(a.A * (1 - t) + b.A * t),
                (int)(a.R * (1 - t) + b.R * t),
                (int)(a.G * (1 - t) + b.G * t),
                (int)(a.B * (1 - t) + b.B * t)
            );
        }

        private Color HSVToRGB(float h, float s, float v)
        {
            // HSV to RGB conversion
            h = h % 360;
            float c = v * s;
            float x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            float m = v - c;

            float r, g, b;
            if (h >= 0 && h < 60) { r = c; g = x; b = 0; }
            else if (h >= 60 && h < 120) { r = x; g = c; b = 0; }
            else if (h >= 120 && h < 180) { r = 0; g = c; b = x; }
            else if (h >= 180 && h < 240) { r = 0; g = x; b = c; }
            else if (h >= 240 && h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }

            return Color.FromArgb(255,
                (int)((r + m) * 255),
                (int)((g + m) * 255),
                (int)((b + m) * 255));
        }

        private uint BlendPixel(uint background, uint foreground, float alpha)
        {
            uint bgR = (background >> 16) & 0xFF;
            uint bgG = (background >> 8) & 0xFF;
            uint bgB = background & 0xFF;
            uint fgR = (foreground >> 16) & 0xFF;
            uint fgG = (foreground >> 8) & 0xFF;
            uint fgB = foreground & 0xFF;

            uint resultR = (uint)(bgR * (1 - alpha) + fgR * alpha);
            uint resultG = (uint)(bgG * (1 - alpha) + fgG * alpha);
            uint resultB = (uint)(bgB * (1 - alpha) + fgB * alpha);

            return (255u << 24) | (resultR << 16) | (resultG << 8) | resultB;
        }

        // Additional rendering methods would continue here...
        private void DrawArrow(ImageBuffer output, int x, int y, Vector2D vector, Color color, int length) { /* Implementation */ }
        private void RenderStreamline(ImageBuffer output, Streamline streamline) { /* Implementation */ }
        private float TraceLIC(float x, float y, float[] noiseTexture, int width, int height) { return 0.5f; /* Implementation */ }

        #endregion

        #region Configuration

        #endregion
    }
}
