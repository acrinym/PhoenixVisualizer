using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Rotating Star Patterns effect
    /// Creates animated star patterns with rotation and various configurations
    /// </summary>
    public class RotatingStarPatternsNode : BaseEffectNode
    {
        public bool Enabled { get; set; } = true;
        public int NumberOfStars { get; set; } = 50;
        public float StarSize { get; set; } = 3.0f;
        public Color StarColor { get; set; } = Color.White;
        public float RotationSpeed { get; set; } = 1.0f;
        public int PatternType { get; set; } = 0; // 0=Spiral, 1=Circle, 2=Random, 3=Galaxy
        public float PatternRadius { get; set; } = 0.4f;
        public bool BeatReactive { get; set; } = false;
        public float BeatRotationBoost { get; set; } = 3.0f;
        public bool StarTwinkle { get; set; } = true;
        public float TwinkleSpeed { get; set; } = 2.0f;
        public bool ColorCycling { get; set; } = false;
        public float ColorCycleSpeed { get; set; } = 1.0f;
        public bool TrailEffect { get; set; } = false;
        public float TrailDecay { get; set; } = 0.95f;

        private float _currentRotation = 0.0f;
        private float _colorCycleTime = 0.0f;
        private int _beatCounter = 0;
        private readonly Random _random = new Random();
        private ImageBuffer _trailBuffer = null;
        private const int BEAT_DURATION = 30;

        // Star positions for different patterns
        private struct Star
        {
            public float X, Y;
            public float Brightness;
            public float Size;
            public float TwinklePhase;
        }

        private Star[] _stars;

        public RotatingStarPatternsNode()
        {
            Name = "Rotating Star Patterns";
            Description = "Animated star patterns with rotation and various configurations";
            Category = "Pattern Effects";
            InitializeStars();
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Background", typeof(ImageBuffer), false, null, "Optional background"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Star pattern output"));
        }

        public override void ProcessFrame(Dictionary<string, object> inputData, Dictionary<string, object> outputData, AudioFeatures audioFeatures)
        {
            if (!Enabled) return;

            try
            {
                var backgroundImage = GetInputValue<ImageBuffer>("Background", inputData);
                var outputImage = backgroundImage != null ? 
                    new ImageBuffer(backgroundImage.Width, backgroundImage.Height) : 
                    new ImageBuffer(640, 480);

                // Initialize trail buffer if needed
                if (TrailEffect && (_trailBuffer == null || _trailBuffer.Width != outputImage.Width || _trailBuffer.Height != outputImage.Height))
                {
                    _trailBuffer = new ImageBuffer(outputImage.Width, outputImage.Height);
                }

                // Copy background
                if (backgroundImage != null)
                {
                    Array.Copy(backgroundImage.Data, outputImage.Data, backgroundImage.Data.Length);
                }

                if (BeatReactive && audioFeatures.Beat)
                {
                    _beatCounter = BEAT_DURATION;
                }
                else if (_beatCounter > 0)
                {
                    _beatCounter--;
                }

                UpdateStarPattern();
                RenderStars(outputImage);

                if (TrailEffect)
                {
                    ApplyTrailEffect(outputImage);
                }

                outputData["Output"] = outputImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Rotating Star Patterns] Error: {ex.Message}");
            }
        }

        private void InitializeStars()
        {
            _stars = new Star[NumberOfStars];
            
            for (int i = 0; i < NumberOfStars; i++)
            {
                _stars[i] = new Star
                {
                    X = (float)_random.NextDouble(),
                    Y = (float)_random.NextDouble(),
                    Brightness = 0.5f + (float)_random.NextDouble() * 0.5f,
                    Size = StarSize * (0.5f + (float)_random.NextDouble() * 0.5f),
                    TwinklePhase = (float)_random.NextDouble() * (float)Math.PI * 2
                };
            }
            
            UpdateStarPositions();
        }

        private void UpdateStarPattern()
        {
            // Update rotation
            float effectiveSpeed = RotationSpeed;
            if (BeatReactive && _beatCounter > 0)
            {
                float beatFactor = (_beatCounter / (float)BEAT_DURATION);
                effectiveSpeed *= (1.0f + (BeatRotationBoost - 1.0f) * beatFactor);
            }

            _currentRotation += effectiveSpeed * 0.016f;
            _colorCycleTime += ColorCycleSpeed * 0.016f;

            // Update star positions based on pattern
            UpdateStarPositions();

            // Update twinkle phases
            if (StarTwinkle)
            {
                for (int i = 0; i < _stars.Length; i++)
                {
                    _stars[i].TwinklePhase += TwinkleSpeed * 0.016f;
                    _stars[i].Brightness = 0.3f + 0.7f * (0.5f + 0.5f * (float)Math.Sin(_stars[i].TwinklePhase));
                }
            }
        }

        private void UpdateStarPositions()
        {
            float centerX = 0.5f;
            float centerY = 0.5f;

            for (int i = 0; i < _stars.Length; i++)
            {
                float starIndex = i / (float)(_stars.Length - 1);
                
                switch (PatternType)
                {
                    case 0: // Spiral
                        float spiralAngle = _currentRotation + starIndex * (float)Math.PI * 4;
                        float spiralRadius = PatternRadius * starIndex;
                        _stars[i].X = centerX + spiralRadius * (float)Math.Cos(spiralAngle);
                        _stars[i].Y = centerY + spiralRadius * (float)Math.Sin(spiralAngle);
                        break;

                    case 1: // Circle
                        float circleAngle = _currentRotation + starIndex * (float)Math.PI * 2;
                        _stars[i].X = centerX + PatternRadius * (float)Math.Cos(circleAngle);
                        _stars[i].Y = centerY + PatternRadius * (float)Math.Sin(circleAngle);
                        break;

                    case 2: // Random (with rotation)
                        float randomAngle = _currentRotation + _stars[i].TwinklePhase;
                        float randomRadius = PatternRadius * _stars[i].Brightness;
                        _stars[i].X = centerX + randomRadius * (float)Math.Cos(randomAngle);
                        _stars[i].Y = centerY + randomRadius * (float)Math.Sin(randomAngle);
                        break;

                    case 3: // Galaxy
                        float galaxyAngle = _currentRotation * (1.0f + starIndex * 0.5f) + starIndex * (float)Math.PI * 2;
                        float galaxyRadius = PatternRadius * (0.2f + starIndex * 0.8f);
                        _stars[i].X = centerX + galaxyRadius * (float)Math.Cos(galaxyAngle);
                        _stars[i].Y = centerY + galaxyRadius * (float)Math.Sin(galaxyAngle);
                        break;
                }

                // Keep stars within bounds
                _stars[i].X = Math.Max(0.05f, Math.Min(0.95f, _stars[i].X));
                _stars[i].Y = Math.Max(0.05f, Math.Min(0.95f, _stars[i].Y));
            }
        }

        private void RenderStars(ImageBuffer output)
        {
            int width = output.Width;
            int height = output.Height;

            for (int i = 0; i < _stars.Length; i++)
            {
                var star = _stars[i];
                
                // Calculate star position in pixels
                int starX = (int)(star.X * width);
                int starY = (int)(star.Y * height);
                
                // Calculate effective color
                Color effectiveColor = CalculateStarColor(i, star.Brightness);
                
                // Calculate effective size
                float effectiveSize = star.Size;
                if (BeatReactive && _beatCounter > 0)
                {
                    float beatFactor = (_beatCounter / (float)BEAT_DURATION);
                    effectiveSize *= (1.0f + beatFactor * 0.5f);
                }

                // Render star
                RenderStar(output, starX, starY, effectiveSize, effectiveColor);
            }
        }

        private Color CalculateStarColor(int starIndex, float brightness)
        {
            Color baseColor = StarColor;

            if (ColorCycling)
            {
                // Cycle through hues
                float hue = (_colorCycleTime + starIndex * 0.1f) % 1.0f;
                baseColor = HSVToRGB(hue * 360, 1.0f, 1.0f);
            }

            // Apply brightness
            int r = (int)(baseColor.R * brightness);
            int g = (int)(baseColor.G * brightness);
            int b = (int)(baseColor.B * brightness);

            return Color.FromArgb(baseColor.A, 
                Math.Max(0, Math.Min(255, r)),
                Math.Max(0, Math.Min(255, g)),
                Math.Max(0, Math.Min(255, b)));
        }

        private Color HSVToRGB(float h, float s, float v)
        {
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

        private void RenderStar(ImageBuffer output, int centerX, int centerY, float size, Color color)
        {
            int width = output.Width;
            int height = output.Height;
            uint colorValue = (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | color.B);

            int radius = (int)Math.Ceiling(size);
            float radiusSquared = size * size;

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;

                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        float distance = dx * dx + dy * dy;
                        if (distance <= radiusSquared)
                        {
                            // Anti-aliasing
                            float alpha = 1.0f;
                            if (distance > radiusSquared * 0.5f)
                            {
                                alpha = 1.0f - (distance - radiusSquared * 0.5f) / (radiusSquared * 0.5f);
                            }

                            uint finalColor = ApplyAlpha(colorValue, alpha);
                            uint existingPixel = output.Data[y * width + x];
                            output.Data[y * width + x] = BlendPixels(existingPixel, finalColor);
                        }
                    }
                }
            }
        }

        private uint ApplyAlpha(uint color, float alpha)
        {
            uint a = (uint)((color >> 24) * alpha);
            return (a << 24) | (color & 0x00FFFFFF);
        }

        private uint BlendPixels(uint background, uint foreground)
        {
            uint bgA = (background >> 24) & 0xFF;
            uint bgR = (background >> 16) & 0xFF;
            uint bgG = (background >> 8) & 0xFF;
            uint bgB = background & 0xFF;

            uint fgA = (foreground >> 24) & 0xFF;
            uint fgR = (foreground >> 16) & 0xFF;
            uint fgG = (foreground >> 8) & 0xFF;
            uint fgB = foreground & 0xFF;

            if (fgA == 0) return background;

            float alpha = fgA / 255.0f;
            uint resultA = Math.Max(bgA, fgA);
            uint resultR = (uint)(bgR * (1 - alpha) + fgR * alpha);
            uint resultG = (uint)(bgG * (1 - alpha) + fgG * alpha);
            uint resultB = (uint)(bgB * (1 - alpha) + fgB * alpha);

            return (resultA << 24) | (resultR << 16) | (resultG << 8) | resultB;
        }

        private void ApplyTrailEffect(ImageBuffer output)
        {
            if (_trailBuffer == null) return;

            for (int i = 0; i < _trailBuffer.Data.Length; i++)
            {
                uint trailPixel = _trailBuffer.Data[i];
                uint a = (uint)((trailPixel >> 24) * TrailDecay);
                uint r = (uint)((trailPixel >> 16) & 0xFF * TrailDecay);
                uint g = (uint)((trailPixel >> 8) & 0xFF * TrailDecay);
                uint b = (uint)((trailPixel & 0xFF) * TrailDecay);

                _trailBuffer.Data[i] = (a << 24) | (r << 16) | (g << 8) | b;

                uint currentPixel = output.Data[i];
                output.Data[i] = BlendPixels(_trailBuffer.Data[i], currentPixel);
            }

            Array.Copy(output.Data, _trailBuffer.Data, output.Data.Length);
        }

        public override Dictionary<string, object> GetConfiguration()
        {
            return new Dictionary<string, object>
            {
                { "Enabled", Enabled },
                { "NumberOfStars", NumberOfStars },
                { "StarSize", StarSize },
                { "StarColor", StarColor },
                { "RotationSpeed", RotationSpeed },
                { "PatternType", PatternType },
                { "PatternRadius", PatternRadius },
                { "BeatReactive", BeatReactive },
                { "BeatRotationBoost", BeatRotationBoost },
                { "StarTwinkle", StarTwinkle },
                { "TwinkleSpeed", TwinkleSpeed },
                { "ColorCycling", ColorCycling },
                { "ColorCycleSpeed", ColorCycleSpeed },
                { "TrailEffect", TrailEffect },
                { "TrailDecay", TrailDecay }
            };
        }

        public override void ApplyConfiguration(Dictionary<string, object> config)
        {
            if (config.TryGetValue("Enabled", out var enabled))
                Enabled = Convert.ToBoolean(enabled);
            if (config.TryGetValue("NumberOfStars", out var numberOfStars))
            {
                NumberOfStars = Convert.ToInt32(numberOfStars);
                InitializeStars();
            }
            if (config.TryGetValue("StarSize", out var starSize))
                StarSize = Convert.ToSingle(starSize);
            if (config.TryGetValue("StarColor", out var starColor))
                StarColor = (Color)starColor;
            if (config.TryGetValue("RotationSpeed", out var rotationSpeed))
                RotationSpeed = Convert.ToSingle(rotationSpeed);
            if (config.TryGetValue("PatternType", out var patternType))
                PatternType = Convert.ToInt32(patternType);
            if (config.TryGetValue("PatternRadius", out var patternRadius))
                PatternRadius = Convert.ToSingle(patternRadius);
            if (config.TryGetValue("BeatReactive", out var beatReactive))
                BeatReactive = Convert.ToBoolean(beatReactive);
            if (config.TryGetValue("BeatRotationBoost", out var beatBoost))
                BeatRotationBoost = Convert.ToSingle(beatBoost);
            if (config.TryGetValue("StarTwinkle", out var twinkle))
                StarTwinkle = Convert.ToBoolean(twinkle);
            if (config.TryGetValue("TwinkleSpeed", out var twinkleSpeed))
                TwinkleSpeed = Convert.ToSingle(twinkleSpeed);
            if (config.TryGetValue("ColorCycling", out var colorCycling))
                ColorCycling = Convert.ToBoolean(colorCycling);
            if (config.TryGetValue("ColorCycleSpeed", out var colorCycleSpeed))
                ColorCycleSpeed = Convert.ToSingle(colorCycleSpeed);
            if (config.TryGetValue("TrailEffect", out var trailEffect))
                TrailEffect = Convert.ToBoolean(trailEffect);
            if (config.TryGetValue("TrailDecay", out var trailDecay))
                TrailDecay = Convert.ToSingle(trailDecay);
        }
    }
}