using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Laser beam and light effects system that creates dynamic laser beams,
    /// light rays, and optical effects with audio reactivity.
    /// </summary>
    public class LaserEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>Whether the laser effect is enabled.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Number of laser beams to render.</summary>
        public int BeamCount { get; set; } = 8;

        /// <summary>Laser beam intensity (0.1 to 5.0).</summary>
        public float Intensity { get; set; } = 1.0f;

        /// <summary>Beam width in pixels.</summary>
        public float BeamWidth { get; set; } = 3.0f;

        /// <summary>Beam length as percentage of screen.</summary>
        public float BeamLength { get; set; } = 0.8f;

        /// <summary>Beam color.</summary>
        public Color BeamColor { get; set; } = Color.Red;

        /// <summary>Whether to enable audio reactivity.</summary>
        public bool AudioReactive { get; set; } = true;

        /// <summary>Audio sensitivity multiplier.</summary>
        public float AudioSensitivity { get; set; } = 1.0f;

        /// <summary>Beam rotation speed in degrees per second.</summary>
        public float RotationSpeed { get; set; } = 45.0f;

        /// <summary>Beam pulse frequency.</summary>
        public float PulseFrequency { get; set; } = 2.0f;

        /// <summary>Whether to enable beam trails.</summary>
        public bool EnableTrails { get; set; } = false;

        /// <summary>Trail length in frames.</summary>
        public int TrailLength { get; set; } = 10;

        /// <summary>Beam pattern type.</summary>
        public LaserPattern Pattern { get; set; } = LaserPattern.Radial;

        /// <summary>Beam origin position (0.0 to 1.0).</summary>
        public PointF Origin { get; set; } = new PointF(0.5f, 0.5f);

        /// <summary>Whether to enable beam intersection effects.</summary>
        public bool EnableIntersections { get; set; } = true;

        /// <summary>Intersection glow intensity.</summary>
        public float IntersectionGlow { get; set; } = 0.5f;

        #endregion

        #region Private Fields

        private readonly List<LaserBeam> _beams = new List<LaserBeam>();
        private readonly List<BeamTrail> _trails = new List<BeamTrail>();
        private readonly Random _random = new Random();
        private float _rotationAngle;
        private int _frameCounter;
        private float _time;

        #endregion

        #region Constructor

        public LaserEffectsNode()
        {
            Name = "Laser Effects";
            Description = "Dynamic laser beam and light effects with audio reactivity";
            Category = "Light Effects";

            InitializeBeams();
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Input image for laser overlay"));
            _inputPorts.Add(new EffectPort("Audio", typeof(AudioFeatures), false, null, "Audio input for beam modulation"));
            _inputPorts.Add(new EffectPort("Enabled", typeof(bool), false, true, "Enable/disable laser effect"));
            _inputPorts.Add(new EffectPort("Intensity", typeof(float), false, 1.0f, "Beam intensity"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Image with laser effects"));
        }

        #endregion

        #region Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var imageObj) || imageObj is not ImageBuffer imageBuffer)
                return GetDefaultOutput();

            if (inputs.TryGetValue("Enabled", out var en))
                Enabled = (bool)en;
            if (inputs.TryGetValue("Intensity", out var intensity) && intensity is float intensityValue)
                Intensity = Math.Clamp(intensityValue, 0.1f, 5.0f);

            if (!Enabled)
                return imageBuffer;

            _frameCounter++;
            _time += 1.0f / 60.0f; // Assume 60 FPS

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height, (int[])imageBuffer.Pixels.Clone());

            UpdateBeams(audioFeatures);
            RenderLasers(output, imageBuffer);

            return output;
        }

        #endregion

        #region Beam Management

        private void InitializeBeams()
        {
            _beams.Clear();
            for (int i = 0; i < BeamCount; i++)
            {
                float angle = (float)i / BeamCount * 360.0f;
                var beam = new LaserBeam
                {
                    Angle = angle,
                    Length = BeamLength,
                    Width = BeamWidth,
                    Color = BeamColor,
                    PulsePhase = (float)i / BeamCount
                };
                _beams.Add(beam);
            }
        }

        private void UpdateBeams(AudioFeatures audioFeatures)
        {
            // Update rotation
            _rotationAngle += RotationSpeed * (1.0f / 60.0f);
            if (_rotationAngle >= 360.0f)
                _rotationAngle -= 360.0f;

            // Update each beam
            for (int i = 0; i < _beams.Count; i++)
            {
                var beam = _beams[i];
                
                // Update pulse
                beam.PulsePhase += PulseFrequency * (1.0f / 60.0f);
                if (beam.PulsePhase >= 1.0f)
                    beam.PulsePhase -= 1.0f;

                // Apply audio modulation
                if (AudioReactive && audioFeatures != null)
                {
                    ApplyAudioModulation(beam, audioFeatures, i);
                }

                // Update beam properties
                beam.CurrentIntensity = Intensity * (0.5f + 0.5f * (float)Math.Sin(beam.PulsePhase * Math.PI * 2));
            }

            // Update trails
            UpdateTrails();
        }

        private void ApplyAudioModulation(LaserBeam beam, AudioFeatures audioFeatures, int beamIndex)
        {
            if (audioFeatures.SpectrumData == null || audioFeatures.SpectrumData.Length == 0)
                return;

            // Map beam index to frequency band
            int spectrumIndex = (beamIndex * audioFeatures.SpectrumData.Length) / _beams.Count;
            spectrumIndex = Math.Min(spectrumIndex, audioFeatures.SpectrumData.Length - 1);

            float audioValue = audioFeatures.SpectrumData[spectrumIndex];
            
            // Modulate beam intensity
            beam.CurrentIntensity *= (1.0f + audioValue * AudioSensitivity * 0.5f);
            
            // Modulate beam width
            beam.CurrentWidth = BeamWidth * (1.0f + audioValue * AudioSensitivity * 0.3f);
            
            // Beat-reactive effects
            if (audioFeatures.IsBeat)
            {
                beam.CurrentIntensity *= 1.5f;
                beam.CurrentWidth *= 1.2f;
                
                // Create trail on beat
                if (EnableTrails)
                {
                    CreateBeamTrail(beam);
                }
            }
        }

        #endregion

        #region Trail System

        private void CreateBeamTrail(LaserBeam beam)
        {
            if (_trails.Count >= BeamCount * 2)
                return;

            var trail = new BeamTrail
            {
                Angle = beam.Angle + _rotationAngle,
                Length = beam.Length,
                Width = beam.CurrentWidth,
                Color = beam.Color,
                Intensity = beam.CurrentIntensity,
                Life = TrailLength
            };

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

        private void RenderLasers(ImageBuffer output, ImageBuffer input)
        {
            int width = output.Width;
            int height = output.Height;
            int centerX = (int)(Origin.X * width);
            int centerY = (int)(Origin.Y * height);

            // Render trails first (behind beams)
            if (EnableTrails)
            {
                RenderBeamTrails(output, centerX, centerY);
            }

            // Render main beams
            foreach (var beam in _beams)
            {
                RenderBeam(output, beam, centerX, centerY);
            }

            // Render intersections if enabled
            if (EnableIntersections)
            {
                RenderIntersections(output, centerX, centerY);
            }
        }

        private void RenderBeam(ImageBuffer output, LaserBeam beam, int centerX, int centerY)
        {
            int width = output.Width;
            int height = output.Height;
            
            float angle = (beam.Angle + _rotationAngle) * (float)Math.PI / 180.0f;
            float length = beam.Length * Math.Min(width, height) * 0.5f;
            float width_px = Math.Max(1, beam.CurrentWidth);

            // Calculate end point
            int endX = centerX + (int)(Math.Cos(angle) * length);
            int endY = centerY + (int)(Math.Sin(angle) * length);

            // Clamp to screen bounds
            endX = Math.Clamp(endX, 0, width - 1);
            endY = Math.Clamp(endY, 0, height - 1);

            // Render beam line
            DrawThickLine(output, centerX, centerY, endX, endY, (int)width_px, beam.Color, beam.CurrentIntensity);
        }

        private void RenderBeamTrails(ImageBuffer output, int centerX, int centerY)
        {
            foreach (var trail in _trails)
            {
                float alpha = (float)trail.Life / TrailLength;
                Color trailColor = Color.FromArgb(
                    (int)(trail.Color.A * alpha * 0.5f),
                    trail.Color.R,
                    trail.Color.G,
                    trail.Color.B
                );

                float angle = trail.Angle * (float)Math.PI / 180.0f;
                float length = trail.Length * Math.Min(output.Width, output.Height) * 0.5f;
                float width_px = Math.Max(1, trail.Width);

                int endX = centerX + (int)(Math.Cos(angle) * length);
                int endY = centerY + (int)(Math.Sin(angle) * length);

                endX = Math.Clamp(endX, 0, output.Width - 1);
                endY = Math.Clamp(endY, 0, output.Height - 1);

                DrawThickLine(output, centerX, centerY, endX, endY, (int)width_px, trailColor, trail.Intensity * alpha);
            }
        }

        private void RenderIntersections(ImageBuffer output, int centerX, int centerY)
        {
            // Find intersection points between beams
            var intersections = new List<Point>();
            
            for (int i = 0; i < _beams.Count; i++)
            {
                for (int j = i + 1; j < _beams.Count; j++)
                {
                    var intersection = FindBeamIntersection(_beams[i], _beams[j], centerX, centerY);
                    if (intersection.HasValue)
                    {
                        intersections.Add(intersection.Value);
                    }
                }
            }

            // Render intersection glows
            foreach (var intersection in intersections)
            {
                RenderIntersectionGlow(output, intersection.X, intersection.Y);
            }
        }

        private Point? FindBeamIntersection(LaserBeam beam1, LaserBeam beam2, int centerX, int centerY)
        {
            // Simple intersection calculation for radial beams
            // In a real implementation, this would be more sophisticated
            float angle1 = (beam1.Angle + _rotationAngle) * (float)Math.PI / 180.0f;
            float angle2 = (beam2.Angle + _rotationAngle) * (float)Math.PI / 180.0f;

            // For radial beams, intersections occur at the center
            if (Math.Abs(angle1 - angle2) > 0.1f)
            {
                return new Point(centerX, centerY);
            }

            return null;
        }

        private void RenderIntersectionGlow(ImageBuffer output, int x, int y)
        {
            int glowRadius = 15;
            Color glowColor = Color.FromArgb(255, 255, 255, 255);

            for (int dy = -glowRadius; dy <= glowRadius; dy++)
            {
                for (int dx = -glowRadius; dx <= glowRadius; dx++)
                {
                    int px = x + dx;
                    int py = y + dy;
                    
                    if (px >= 0 && px < output.Width && py >= 0 && py < output.Height)
                    {
                        float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (distance <= glowRadius)
                        {
                            float alpha = (1.0f - distance / glowRadius) * IntersectionGlow;
                            int currentColor = output.GetPixel(px, py);
                            int blendedColor = BlendColors(currentColor, glowColor.ToArgb(), alpha);
                            output.SetPixel(px, py, blendedColor);
                        }
                    }
                }
            }
        }

        private void DrawThickLine(ImageBuffer output, int x1, int y1, int x2, int y2, int thickness, Color color, float intensity)
        {
            // Bresenham's line algorithm with thickness
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;

            int x = x1, y = y1;
            while (true)
            {
                // Draw thick line by filling a circle at each point
                DrawThickPoint(output, x, y, thickness, color, intensity);

                if (x == x2 && y == y2) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }

        private void DrawThickPoint(ImageBuffer output, int x, int y, int thickness, Color color, float intensity)
        {
            int radius = thickness / 2;
            int alpha = (int)(color.A * intensity);

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        int px = x + dx;
                        int py = y + dy;
                        
                        if (px >= 0 && px < output.Width && py >= 0 && py < output.Height)
                        {
                            int currentColor = output.GetPixel(px, py);
                            Color pointColor = Color.FromArgb(alpha, color.R, color.G, color.B);
                            int blendedColor = BlendColors(currentColor, pointColor.ToArgb(), intensity);
                            output.SetPixel(px, py, blendedColor);
                        }
                    }
                }
            }
        }

        private int BlendColors(int baseColor, int overlayColor, float alpha)
        {
            int r1 = baseColor & 0xFF;
            int g1 = (baseColor >> 8) & 0xFF;
            int b1 = (baseColor >> 16) & 0xFF;

            int r2 = overlayColor & 0xFF;
            int g2 = (overlayColor >> 8) & 0xFF;
            int b2 = (overlayColor >> 16) & 0xFF;

            int r = (int)(r1 * (1.0f - alpha) + r2 * alpha);
            int g = (int)(g1 * (1.0f - alpha) + g2 * alpha);
            int b = (int)(b1 * (1.0f - alpha) + b2 * alpha);

            r = Math.Clamp(r, 0, 255);
            g = Math.Clamp(g, 0, 255);
            b = Math.Clamp(b, 0, 255);

            return (b << 16) | (g << 8) | r;
        }

        #endregion

        #region Public Methods

        public override void Reset()
        {
            base.Reset();
            InitializeBeams();
            _trails.Clear();
            _rotationAngle = 0;
            _frameCounter = 0;
            _time = 0;
        }

        public string GetLaserStats()
        {
            return $"Beams: {_beams.Count}, Trails: {_trails.Count}, Frame: {_frameCounter}, Angle: {_rotationAngle:F1}°";
        }

        public void SetBeamCount(int count)
        {
            BeamCount = Math.Max(1, Math.Min(32, count));
            InitializeBeams();
        }

        public void SetBeamColor(Color color)
        {
            BeamColor = color;
            foreach (var beam in _beams)
            {
                beam.Color = color;
            }
        }

        #endregion

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }
    }

    #region Supporting Classes

    /// <summary>
    /// Individual laser beam
    /// </summary>
    public class LaserBeam
    {
        public float Angle { get; set; }
        public float Length { get; set; }
        public float Width { get; set; }
        public Color Color { get; set; }
        public float PulsePhase { get; set; }
        public float CurrentIntensity { get; set; }
        public float CurrentWidth { get; set; }
    }

    /// <summary>
    /// Laser beam trail
    /// </summary>
    public class BeamTrail
    {
        public float Angle { get; set; }
        public float Length { get; set; }
        public float Width { get; set; }
        public Color Color { get; set; }
        public float Intensity { get; set; }
        public int Life { get; set; }
    }

    /// <summary>
    /// Available laser patterns
    /// </summary>
    public enum LaserPattern
    {
        Radial,
        Spiral,
        Grid,
        Random
    }

    #endregion
}
