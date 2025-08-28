using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Enhanced Starfield Effects - different from basic Starfield
    /// Creates moving starfield with depth, speed variations, and effects
    /// </summary>
    public class StarfieldEffectsNode : BaseEffectNode
    {
        public bool Enabled { get; set; } = true;

        public int NumberOfStars { get; set; } = 200;
        public float Speed { get; set; } = 1.0f;
        public Color StarColor { get; set; } = Color.White;
        public bool BeatReactive { get; set; } = false;
        public float BeatSpeedBoost { get; set; } = 3.0f;
        public bool DepthEffect { get; set; } = true;
        public bool MotionBlur { get; set; } = true;
        public int Direction { get; set; } = 0; // 0=Center-out, 1=Linear

        private struct StarData
        {
            public float X, Y, Z;
            public float VX, VY;
            public float Brightness;
        }

        private StarData[] _stars;
        private int _beatCounter = 0;
        private readonly Random _random = new Random();

        public StarfieldEffectsNode()
        {
            Name = "Starfield Effects";
            Description = "Enhanced starfield with depth and motion effects";
            Category = "Pattern Effects";
            InitializeStars();
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Background", typeof(ImageBuffer), false, null, "Background"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Starfield output"));
        }

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            // If the effect is disabled, bail out gracefully ✨
            if (!Enabled) return null;

            try
            {
                var backgroundImage = GetInputValue<ImageBuffer>("Background", inputs);
                var outputImage = backgroundImage != null ?
                    new ImageBuffer(backgroundImage.Width, backgroundImage.Height) :
                    new ImageBuffer(640, 480);

                if (backgroundImage != null)
                    Array.Copy(backgroundImage.Data, outputImage.Data, backgroundImage.Data.Length);

                if (BeatReactive && audioFeatures.Beat)
                    _beatCounter = 25;
                else if (_beatCounter > 0)
                    _beatCounter--;

                UpdateStars();
                RenderStars(outputImage);

                return outputImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Starfield Effects] Error: {ex.Message}");
            }

            // Something went wrong, so return null 🛠️
            return null;
        }

        private void InitializeStars()
        {
            _stars = new StarData[NumberOfStars];
            for (int i = 0; i < NumberOfStars; i++)
            {
                _stars[i] = new StarData
                {
                    X = (float)_random.NextDouble(),
                    Y = (float)_random.NextDouble(),
                    Z = (float)_random.NextDouble() * 10 + 1,
                    VX = Direction == 0 ? (float)Math.Cos(_random.NextDouble() * Math.PI * 2) : 1.0f,
                    VY = Direction == 0 ? (float)Math.Sin(_random.NextDouble() * Math.PI * 2) : 0.0f,
                    Brightness = 0.3f + (float)_random.NextDouble() * 0.7f
                };
            }
        }

        private void UpdateStars()
        {
            float effectiveSpeed = Speed;
            if (BeatReactive && _beatCounter > 0)
                effectiveSpeed *= (1.0f + (BeatSpeedBoost - 1.0f) * (_beatCounter / 25.0f));

            for (int i = 0; i < _stars.Length; i++)
            {
                ref var star = ref _stars[i];
                float depthFactor = DepthEffect ? (10.0f / star.Z) : 1.0f;
                
                star.X += star.VX * effectiveSpeed * depthFactor * 0.01f;
                star.Y += star.VY * effectiveSpeed * depthFactor * 0.01f;
                
                if (DepthEffect)
                {
                    star.Z -= effectiveSpeed * 0.05f;
                    if (star.Z <= 0.1f)
                        ResetStar(ref star);
                }

                if (star.X < -0.1f || star.X > 1.1f || star.Y < -0.1f || star.Y > 1.1f)
                    ResetStar(ref star);
            }
        }

        private void ResetStar(ref StarData star)
        {
            if (Direction == 0) // Center-out
            {
                star.X = 0.5f + ((float)_random.NextDouble() - 0.5f) * 0.1f;
                star.Y = 0.5f + ((float)_random.NextDouble() - 0.5f) * 0.1f;
            }
            else // Linear
            {
                star.X = -0.05f;
                star.Y = (float)_random.NextDouble();
            }
            
            star.Z = 10.0f;
            star.Brightness = 0.3f + (float)_random.NextDouble() * 0.7f;
        }

        private void RenderStars(ImageBuffer output)
        {
            int width = output.Width;
            int height = output.Height;

            for (int i = 0; i < _stars.Length; i++)
            {
                var star = _stars[i];
                int x = (int)(star.X * width);
                int y = (int)(star.Y * height);
                
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    float size = DepthEffect ? Math.Max(0.5f, 10.0f / star.Z) : 1.0f;
                    Color color = Color.FromArgb(
                        StarColor.A,
                        (int)(StarColor.R * star.Brightness),
                        (int)(StarColor.G * star.Brightness),
                        (int)(StarColor.B * star.Brightness));
                    
                    RenderStar(output, x, y, color, size);
                }
            }
        }

        private void RenderStar(ImageBuffer output, int centerX, int centerY, Color color, float size)
        {
            int width = output.Width;
            int height = output.Height;
            uint colorValue = (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | color.B);
            int radius = (int)Math.Ceiling(size);
            
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;

                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (distance <= size)
                        {
                            float alpha = 1.0f - (distance / size);
                            uint alphaColor = (uint)((colorValue & 0x00FFFFFF) | ((uint)(((colorValue >> 24) & 0xFF) * alpha) << 24));
                            uint existingPixel = output.Data[y * width + x];
                            
                            if ((alphaColor >> 24) > 0)
                            {
                                output.Data[y * width + x] = BlendPixels(existingPixel, alphaColor);
                            }
                        }
                    }
                }
            }
        }

        private uint BlendPixels(uint background, uint foreground)
        {
            uint fgA = (foreground >> 24) & 0xFF;
            if (fgA == 0) return background;
            
            float alpha = fgA / 255.0f;
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

    }
}
