using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Dot Font Rendering effect
    /// Renders text using dot-based fonts with various effects
    /// </summary>
    public class DotFontRenderingNode : BaseEffectNode
    {
        public bool Enabled { get; set; } = true;
        public string Text { get; set; } = "PHOENIX";
        public Color FontColor { get; set; } = Color.White;
        public int FontSize { get; set; } = 16;
        public int DotSize { get; set; } = 2;
        public int DotSpacing { get; set; } = 1;
        public float PositionX { get; set; } = 0.5f; // Normalized position
        public float PositionY { get; set; } = 0.5f;
        public bool BeatReactive { get; set; } = false;
        public float BeatScale { get; set; } = 1.5f;
        public int AnimationType { get; set; } = 0; // 0=Static, 1=Scroll, 2=Fade, 3=Pulse
        public float AnimationSpeed { get; set; } = 1.0f;
        public bool ShadowEffect { get; set; } = false;
        public Color ShadowColor { get; set; } = Color.Black;
        public int ShadowOffset { get; set; } = 2;

        private float _animationTime = 0.0f;
        private int _beatCounter = 0;
        private const int BEAT_DURATION = 20;

        // Simple 5x7 dot font matrix for basic characters
        private static readonly Dictionary<char, bool[,]> DotFont = new Dictionary<char, bool[,]>
        {
            ['A'] = new bool[,] {
                {false,true,true,true,false},
                {true,false,false,false,true},
                {true,false,false,false,true},
                {true,true,true,true,true},
                {true,false,false,false,true},
                {true,false,false,false,true},
                {false,false,false,false,false}
            },
            ['P'] = new bool[,] {
                {true,true,true,true,false},
                {true,false,false,false,true},
                {true,false,false,false,true},
                {true,true,true,true,false},
                {true,false,false,false,false},
                {true,false,false,false,false},
                {false,false,false,false,false}
            },
            ['H'] = new bool[,] {
                {true,false,false,false,true},
                {true,false,false,false,true},
                {true,false,false,false,true},
                {true,true,true,true,true},
                {true,false,false,false,true},
                {true,false,false,false,true},
                {false,false,false,false,false}
            },
            ['O'] = new bool[,] {
                {false,true,true,true,false},
                {true,false,false,false,true},
                {true,false,false,false,true},
                {true,false,false,false,true},
                {true,false,false,false,true},
                {false,true,true,true,false},
                {false,false,false,false,false}
            },
            ['E'] = new bool[,] {
                {true,true,true,true,true},
                {true,false,false,false,false},
                {true,false,false,false,false},
                {true,true,true,true,false},
                {true,false,false,false,false},
                {true,true,true,true,true},
                {false,false,false,false,false}
            },
            ['N'] = new bool[,] {
                {true,false,false,false,true},
                {true,true,false,false,true},
                {true,false,true,false,true},
                {true,false,false,true,true},
                {true,false,false,false,true},
                {true,false,false,false,true},
                {false,false,false,false,false}
            },
            ['I'] = new bool[,] {
                {false,true,true,true,false},
                {false,false,true,false,false},
                {false,false,true,false,false},
                {false,false,true,false,false},
                {false,false,true,false,false},
                {false,true,true,true,false},
                {false,false,false,false,false}
            },
            ['X'] = new bool[,] {
                {true,false,false,false,true},
                {false,true,false,true,false},
                {false,false,true,false,false},
                {false,true,false,true,false},
                {true,false,false,false,true},
                {true,false,false,false,true},
                {false,false,false,false,false}
            },
            [' '] = new bool[,] {
                {false,false,false,false,false},
                {false,false,false,false,false},
                {false,false,false,false,false},
                {false,false,false,false,false},
                {false,false,false,false,false},
                {false,false,false,false,false},
                {false,false,false,false,false}
            }
        };

        public DotFontRenderingNode()
        {
            Name = "Dot Font Rendering";
            Description = "Renders text using dot-based fonts with effects";
            Category = "Text Effects";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), false, null, "Optional background image"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Text rendered output"));
        }

        public override void ProcessFrame(Dictionary<string, object> inputData, Dictionary<string, object> outputData, AudioFeatures audioFeatures)
        {
            if (!Enabled) return;

            try
            {
                var sourceImage = GetInputValue<ImageBuffer>("Image", inputData);
                var outputImage = sourceImage != null ? 
                    new ImageBuffer(sourceImage.Width, sourceImage.Height) : 
                    new ImageBuffer(640, 480); // Default size

                // Copy background if provided
                if (sourceImage != null)
                {
                    Array.Copy(sourceImage.Data, outputImage.Data, sourceImage.Data.Length);
                }

                if (BeatReactive && audioFeatures.Beat)
                {
                    _beatCounter = BEAT_DURATION;
                }
                else if (_beatCounter > 0)
                {
                    _beatCounter--;
                }

                UpdateAnimation();
                RenderDotText(outputImage);

                outputData["Output"] = outputImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Dot Font Rendering] Error: {ex.Message}");
            }
        }

        private void UpdateAnimation()
        {
            _animationTime += AnimationSpeed * 0.016f;
        }

        private void RenderDotText(ImageBuffer output)
        {
            if (string.IsNullOrEmpty(Text)) return;

            int width = output.Width;
            int height = output.Height;

            // Calculate text dimensions
            int charWidth = 5 + DotSpacing;
            int charHeight = 7 + DotSpacing;
            int textWidth = Text.Length * charWidth * DotSize;
            int textHeight = charHeight * DotSize;

            // Calculate position
            int startX = (int)(PositionX * width - textWidth / 2);
            int startY = (int)(PositionY * height - textHeight / 2);

            // Apply animation offset
            switch (AnimationType)
            {
                case 1: // Scroll
                    startX = (int)((_animationTime * 50) % (width + textWidth)) - textWidth;
                    break;
            }

            // Calculate effective colors and sizes
            Color effectiveColor = CalculateEffectiveColor();
            int effectiveDotSize = CalculateEffectiveDotSize();

            for (int charIndex = 0; charIndex < Text.Length; charIndex++)
            {
                char c = char.ToUpper(Text[charIndex]);
                if (!DotFont.ContainsKey(c)) c = ' ';

                var charMatrix = DotFont[c];
                int charX = startX + charIndex * charWidth * DotSize;

                for (int row = 0; row < 7; row++)
                {
                    for (int col = 0; col < 5; col++)
                    {
                        if (charMatrix[row, col])
                        {
                            int dotX = charX + col * DotSize;
                            int dotY = startY + row * DotSize;

                            // Apply animation effects
                            Color dotColor = ApplyAnimationEffects(effectiveColor, charIndex, row, col);

                            // Render shadow if enabled
                            if (ShadowEffect)
                            {
                                RenderDot(output, dotX + ShadowOffset, dotY + ShadowOffset, effectiveDotSize, ShadowColor);
                            }

                            // Render main dot
                            RenderDot(output, dotX, dotY, effectiveDotSize, dotColor);
                        }
                    }
                }
            }
        }

        private Color CalculateEffectiveColor()
        {
            Color color = FontColor;

            // Apply beat reactivity to color
            if (BeatReactive && _beatCounter > 0)
            {
                float beatFactor = (_beatCounter / (float)BEAT_DURATION);
                int r = Math.Min(255, (int)(color.R * (1.0f + beatFactor * 0.5f)));
                int g = Math.Min(255, (int)(color.G * (1.0f + beatFactor * 0.5f)));
                int b = Math.Min(255, (int)(color.B * (1.0f + beatFactor * 0.5f)));
                color = Color.FromArgb(color.A, r, g, b);
            }

            return color;
        }

        private int CalculateEffectiveDotSize()
        {
            int size = DotSize;

            // Apply beat reactivity to size
            if (BeatReactive && _beatCounter > 0)
            {
                float beatFactor = (_beatCounter / (float)BEAT_DURATION);
                size = (int)(size * (1.0f + (BeatScale - 1.0f) * beatFactor));
            }

            return Math.Max(1, size);
        }

        private Color ApplyAnimationEffects(Color baseColor, int charIndex, int row, int col)
        {
            switch (AnimationType)
            {
                case 2: // Fade
                    float fadeValue = (float)(0.5 + 0.5 * Math.Sin(_animationTime + charIndex * 0.5));
                    return Color.FromArgb(
                        (int)(baseColor.A * fadeValue),
                        (int)(baseColor.R * fadeValue),
                        (int)(baseColor.G * fadeValue),
                        (int)(baseColor.B * fadeValue)
                    );

                case 3: // Pulse
                    float pulseDelay = (charIndex * 0.2f + row * 0.1f + col * 0.05f);
                    float pulseValue = (float)(0.5 + 0.5 * Math.Sin(_animationTime * 3 + pulseDelay));
                    return Color.FromArgb(
                        baseColor.A,
                        (int)(baseColor.R * pulseValue),
                        (int)(baseColor.G * pulseValue),
                        (int)(baseColor.B * pulseValue)
                    );

                default:
                    return baseColor;
            }
        }

        private void RenderDot(ImageBuffer output, int x, int y, int size, Color color)
        {
            uint colorValue = (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | color.B);

            for (int dy = 0; dy < size; dy++)
            {
                for (int dx = 0; dx < size; dx++)
                {
                    int pixelX = x + dx;
                    int pixelY = y + dy;

                    if (pixelX >= 0 && pixelX < output.Width && pixelY >= 0 && pixelY < output.Height)
                    {
                        int index = pixelY * output.Width + pixelX;
                        
                        // Alpha blend with existing pixel
                        uint existingPixel = output.Data[index];
                        output.Data[index] = BlendPixels(existingPixel, colorValue);
                    }
                }
            }
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
            if (fgA == 255) return foreground;

            float alpha = fgA / 255.0f;
            float invAlpha = 1.0f - alpha;

            uint resultA = Math.Max(bgA, fgA);
            uint resultR = (uint)(bgR * invAlpha + fgR * alpha);
            uint resultG = (uint)(bgG * invAlpha + fgG * alpha);
            uint resultB = (uint)(bgB * invAlpha + fgB * alpha);

            return (resultA << 24) | (resultR << 16) | (resultG << 8) | resultB;
        }

        public override Dictionary<string, object> GetConfiguration()
        {
            return new Dictionary<string, object>
            {
                { "Enabled", Enabled },
                { "Text", Text },
                { "FontColor", FontColor },
                { "FontSize", FontSize },
                { "DotSize", DotSize },
                { "DotSpacing", DotSpacing },
                { "PositionX", PositionX },
                { "PositionY", PositionY },
                { "BeatReactive", BeatReactive },
                { "BeatScale", BeatScale },
                { "AnimationType", AnimationType },
                { "AnimationSpeed", AnimationSpeed },
                { "ShadowEffect", ShadowEffect },
                { "ShadowColor", ShadowColor },
                { "ShadowOffset", ShadowOffset }
            };
        }

        public override void ApplyConfiguration(Dictionary<string, object> config)
        {
            if (config.TryGetValue("Enabled", out var enabled))
                Enabled = Convert.ToBoolean(enabled);
            if (config.TryGetValue("Text", out var text))
                Text = text.ToString();
            if (config.TryGetValue("FontColor", out var fontColor))
                FontColor = (Color)fontColor;
            if (config.TryGetValue("FontSize", out var fontSize))
                FontSize = Convert.ToInt32(fontSize);
            if (config.TryGetValue("DotSize", out var dotSize))
                DotSize = Convert.ToInt32(dotSize);
            if (config.TryGetValue("DotSpacing", out var spacing))
                DotSpacing = Convert.ToInt32(spacing);
            if (config.TryGetValue("PositionX", out var posX))
                PositionX = Convert.ToSingle(posX);
            if (config.TryGetValue("PositionY", out var posY))
                PositionY = Convert.ToSingle(posY);
            if (config.TryGetValue("BeatReactive", out var beatReactive))
                BeatReactive = Convert.ToBoolean(beatReactive);
            if (config.TryGetValue("BeatScale", out var beatScale))
                BeatScale = Convert.ToSingle(beatScale);
            if (config.TryGetValue("AnimationType", out var animType))
                AnimationType = Convert.ToInt32(animType);
            if (config.TryGetValue("AnimationSpeed", out var animSpeed))
                AnimationSpeed = Convert.ToSingle(animSpeed);
            if (config.TryGetValue("ShadowEffect", out var shadow))
                ShadowEffect = Convert.ToBoolean(shadow);
            if (config.TryGetValue("ShadowColor", out var shadowColor))
                ShadowColor = (Color)shadowColor;
            if (config.TryGetValue("ShadowOffset", out var shadowOffset))
                ShadowOffset = Convert.ToInt32(shadowOffset);
        }
    }
}