using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Advanced Color Fade effect with smooth transitions
    /// Based on r_colorfade.cpp from original AVS
    /// Creates smooth color transitions and fades between different color states
    /// </summary>
    public class ColorfadeEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Color Fade effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Fade type/algorithm
        /// 0 = Linear fade, 1 = Exponential fade, 2 = Sine wave fade, 3 = Bounce fade
        /// </summary>
        public int FadeType { get; set; } = 0;

        /// <summary>
        /// Start color for the fade (ARGB format)
        /// </summary>
        public Color StartColor { get; set; } = Color.Black;

        /// <summary>
        /// End color for the fade (ARGB format)
        /// </summary>
        public Color EndColor { get; set; } = Color.White;

        /// <summary>
        /// Fade speed (0.0 to 10.0)
        /// </summary>
        public float FadeSpeed { get; set; } = 1.0f;

        /// <summary>
        /// Fade mode
        /// 0 = Replace colors, 1 = Additive blend, 2 = Multiply blend, 3 = Overlay blend
        /// </summary>
        public int FadeMode { get; set; } = 0;

        /// <summary>
        /// Beat reactivity enabled
        /// </summary>
        public bool BeatReactive { get; set; } = false;

        /// <summary>
        /// Beat-triggered fade speed multiplier
        /// </summary>
        public float BeatFadeSpeed { get; set; } = 2.0f;

        /// <summary>
        /// Current fade progress (0.0 to 1.0)
        /// </summary>
        public float CurrentFadeProgress { get; private set; } = 0.0f;

        /// <summary>
        /// Enable fade looping (ping-pong effect)
        /// </summary>
        public bool LoopFade { get; set; } = true;

        /// <summary>
        /// Fade direction (1 = forward, -1 = reverse)
        /// </summary>
        public int FadeDirection { get; set; } = 1;

        /// <summary>
        /// Enable color cycling through multiple colors
        /// </summary>
        public bool ColorCycling { get; set; } = false;

        /// <summary>
        /// Colors for cycling mode
        /// </summary>
        public Color[] CyclingColors { get; set; } = { Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Magenta, Color.Cyan };

        /// <summary>
        /// Current color index for cycling
        /// </summary>
        public int CurrentColorIndex { get; private set; } = 0;

        /// <summary>
        /// Alpha blending factor (0.0 to 1.0)
        /// </summary>
        public float AlphaBlending { get; set; } = 1.0f;

        #endregion

        #region Private Fields

        private float _fadeProgress = 0.0f;
        private int _beatFadeCounter = 0;
        private const int BEAT_FADE_DURATION = 20; // frames

        #endregion

        #region Constructor

        public ColorfadeEffectsNode()
        {
            Name = "Color Fade Effects";
            Description = "Advanced color transitions and fades with multiple algorithms";
            Category = "Color Effects";
        }

        #endregion

        #region Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for color fading"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Color faded output image"));
        }

        #endregion

        #region Effect Processing

        public override void ProcessFrame(Dictionary<string, object> inputData, Dictionary<string, object> outputData, AudioFeatures audioFeatures)
        {
            if (!Enabled) return;

            try
            {
                var sourceImage = GetInputValue<ImageBuffer>("Image", inputData);
                if (sourceImage?.Data == null) return;

                var outputImage = new ImageBuffer(sourceImage.Width, sourceImage.Height);

                // Update fade progress
                UpdateFadeProgress(audioFeatures);

                // Get current colors
                Color currentStartColor, currentEndColor;
                GetCurrentColors(out currentStartColor, out currentEndColor);

                // Calculate fade color
                Color fadeColor = CalculateFadeColor(currentStartColor, currentEndColor, CurrentFadeProgress);

                // Apply color fade
                ApplyColorFade(sourceImage, outputImage, fadeColor);

                outputData["Output"] = outputImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Color Fade Effects] Error processing frame: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        private void UpdateFadeProgress(AudioFeatures audioFeatures)
        {
            // Calculate effective speed
            float effectiveSpeed = FadeSpeed;

            // Handle beat reactivity
            if (BeatReactive)
            {
                if (audioFeatures.Beat)
                {
                    _beatFadeCounter = BEAT_FADE_DURATION;
                }
                
                if (_beatFadeCounter > 0)
                {
                    float beatFactor = (_beatFadeCounter / (float)BEAT_FADE_DURATION);
                    effectiveSpeed *= (1.0f + (BeatFadeSpeed - 1.0f) * beatFactor);
                    _beatFadeCounter--;
                }
            }

            // Update progress based on direction
            _fadeProgress += (effectiveSpeed * 0.016f * FadeDirection); // Assuming 60 FPS (0.016s per frame)

            // Handle looping
            if (LoopFade)
            {
                if (_fadeProgress >= 1.0f)
                {
                    if (ColorCycling)
                    {
                        // Move to next color
                        CurrentColorIndex = (CurrentColorIndex + 1) % CyclingColors.Length;
                        _fadeProgress = 0.0f;
                    }
                    else
                    {
                        // Reverse direction
                        _fadeProgress = 1.0f;
                        FadeDirection = -1;
                    }
                }
                else if (_fadeProgress <= 0.0f)
                {
                    _fadeProgress = 0.0f;
                    FadeDirection = 1;
                }
            }
            else
            {
                _fadeProgress = Math.Max(0.0f, Math.Min(1.0f, _fadeProgress));
            }

            CurrentFadeProgress = ApplyFadeType(_fadeProgress);
        }

        private float ApplyFadeType(float progress)
        {
            return FadeType switch
            {
                0 => progress, // Linear
                1 => 1.0f - (float)Math.Exp(-5.0 * progress), // Exponential
                2 => (float)(0.5 * (1.0 - Math.Cos(Math.PI * progress))), // Sine wave
                3 => BounceFade(progress), // Bounce
                _ => progress
            };
        }

        private float BounceFade(float progress)
        {
            // Bounce easing function
            if (progress < 1.0f / 2.75f)
            {
                return 7.5625f * progress * progress;
            }
            else if (progress < 2.0f / 2.75f)
            {
                progress -= 1.5f / 2.75f;
                return 7.5625f * progress * progress + 0.75f;
            }
            else if (progress < 2.5f / 2.75f)
            {
                progress -= 2.25f / 2.75f;
                return 7.5625f * progress * progress + 0.9375f;
            }
            else
            {
                progress -= 2.625f / 2.75f;
                return 7.5625f * progress * progress + 0.984375f;
            }
        }

        private void GetCurrentColors(out Color currentStartColor, out Color currentEndColor)
        {
            if (ColorCycling && CyclingColors.Length > 1)
            {
                currentStartColor = CyclingColors[CurrentColorIndex];
                currentEndColor = CyclingColors[(CurrentColorIndex + 1) % CyclingColors.Length];
            }
            else
            {
                currentStartColor = StartColor;
                currentEndColor = EndColor;
            }
        }

        private Color CalculateFadeColor(Color startColor, Color endColor, float progress)
        {
            int r = (int)(startColor.R * (1.0f - progress) + endColor.R * progress);
            int g = (int)(startColor.G * (1.0f - progress) + endColor.G * progress);
            int b = (int)(startColor.B * (1.0f - progress) + endColor.B * progress);
            int a = (int)(startColor.A * (1.0f - progress) + endColor.A * progress);

            return Color.FromArgb(
                Math.Max(0, Math.Min(255, a)),
                Math.Max(0, Math.Min(255, r)),
                Math.Max(0, Math.Min(255, g)),
                Math.Max(0, Math.Min(255, b))
            );
        }

        private void ApplyColorFade(ImageBuffer source, ImageBuffer output, Color fadeColor)
        {
            uint fadeR = (uint)fadeColor.R;
            uint fadeG = (uint)fadeColor.G;
            uint fadeB = (uint)fadeColor.B;
            uint fadeA = (uint)fadeColor.A;

            for (int i = 0; i < source.Data.Length; i++)
            {
                uint sourcePixel = source.Data[i];
                
                // Extract source channels
                uint srcA = (sourcePixel >> 24) & 0xFF;
                uint srcR = (sourcePixel >> 16) & 0xFF;
                uint srcG = (sourcePixel >> 8) & 0xFF;
                uint srcB = sourcePixel & 0xFF;

                uint resultR, resultG, resultB, resultA;

                switch (FadeMode)
                {
                    case 0: // Replace colors
                        float alpha = AlphaBlending;
                        resultR = (uint)(srcR * (1.0f - alpha) + fadeR * alpha);
                        resultG = (uint)(srcG * (1.0f - alpha) + fadeG * alpha);
                        resultB = (uint)(srcB * (1.0f - alpha) + fadeB * alpha);
                        resultA = Math.Max(srcA, fadeA);
                        break;

                    case 1: // Additive blend
                        resultR = Math.Min(255u, srcR + (uint)(fadeR * AlphaBlending));
                        resultG = Math.Min(255u, srcG + (uint)(fadeG * AlphaBlending));
                        resultB = Math.Min(255u, srcB + (uint)(fadeB * AlphaBlending));
                        resultA = Math.Max(srcA, fadeA);
                        break;

                    case 2: // Multiply blend
                        resultR = (uint)((srcR * fadeR * AlphaBlending) / 255 + srcR * (1.0f - AlphaBlending));
                        resultG = (uint)((srcG * fadeG * AlphaBlending) / 255 + srcG * (1.0f - AlphaBlending));
                        resultB = (uint)((srcB * fadeB * AlphaBlending) / 255 + srcB * (1.0f - AlphaBlending));
                        resultA = Math.Max(srcA, fadeA);
                        break;

                    case 3: // Overlay blend
                        resultR = OverlayBlend(srcR, fadeR, AlphaBlending);
                        resultG = OverlayBlend(srcG, fadeG, AlphaBlending);
                        resultB = OverlayBlend(srcB, fadeB, AlphaBlending);
                        resultA = Math.Max(srcA, fadeA);
                        break;

                    default:
                        resultR = srcR;
                        resultG = srcG;
                        resultB = srcB;
                        resultA = srcA;
                        break;
                }

                output.Data[i] = (resultA << 24) | (resultR << 16) | (resultG << 8) | resultB;
            }
        }

        private uint OverlayBlend(uint baseColor, uint fadeColor, float alpha)
        {
            float base = baseColor / 255.0f;
            float fade = fadeColor / 255.0f;
            
            float result;
            if (base < 0.5f)
            {
                result = 2.0f * base * fade;
            }
            else
            {
                result = 1.0f - 2.0f * (1.0f - base) * (1.0f - fade);
            }

            // Blend with original based on alpha
            result = base * (1.0f - alpha) + result * alpha;
            
            return (uint)Math.Max(0, Math.Min(255, Math.Round(result * 255)));
        }

        #endregion

        #region Configuration

        public override Dictionary<string, object> GetConfiguration()
        {
            return new Dictionary<string, object>
            {
                { "Enabled", Enabled },
                { "FadeType", FadeType },
                { "StartColor", StartColor },
                { "EndColor", EndColor },
                { "FadeSpeed", FadeSpeed },
                { "FadeMode", FadeMode },
                { "BeatReactive", BeatReactive },
                { "BeatFadeSpeed", BeatFadeSpeed },
                { "LoopFade", LoopFade },
                { "FadeDirection", FadeDirection },
                { "ColorCycling", ColorCycling },
                { "CyclingColors", CyclingColors },
                { "AlphaBlending", AlphaBlending }
            };
        }

        public override void ApplyConfiguration(Dictionary<string, object> config)
        {
            if (config.TryGetValue("Enabled", out var enabled))
                Enabled = Convert.ToBoolean(enabled);
            
            if (config.TryGetValue("FadeType", out var fadeType))
                FadeType = Convert.ToInt32(fadeType);
            
            if (config.TryGetValue("StartColor", out var startColor))
                StartColor = (Color)startColor;
            
            if (config.TryGetValue("EndColor", out var endColor))
                EndColor = (Color)endColor;
            
            if (config.TryGetValue("FadeSpeed", out var fadeSpeed))
                FadeSpeed = Convert.ToSingle(fadeSpeed);
            
            if (config.TryGetValue("FadeMode", out var fadeMode))
                FadeMode = Convert.ToInt32(fadeMode);
            
            if (config.TryGetValue("BeatReactive", out var beatReactive))
                BeatReactive = Convert.ToBoolean(beatReactive);
            
            if (config.TryGetValue("BeatFadeSpeed", out var beatFadeSpeed))
                BeatFadeSpeed = Convert.ToSingle(beatFadeSpeed);
            
            if (config.TryGetValue("LoopFade", out var loopFade))
                LoopFade = Convert.ToBoolean(loopFade);
            
            if (config.TryGetValue("FadeDirection", out var fadeDirection))
                FadeDirection = Convert.ToInt32(fadeDirection);
            
            if (config.TryGetValue("ColorCycling", out var colorCycling))
                ColorCycling = Convert.ToBoolean(colorCycling);
            
            if (config.TryGetValue("CyclingColors", out var cyclingColors))
                CyclingColors = (Color[])cyclingColors;
            
            if (config.TryGetValue("AlphaBlending", out var alphaBlending))
                AlphaBlending = Convert.ToSingle(alphaBlending);
        }

        /// <summary>
        /// Reset fade progress to beginning
        /// </summary>
        public void ResetFade()
        {
            _fadeProgress = 0.0f;
            FadeDirection = 1;
            CurrentColorIndex = 0;
        }

        /// <summary>
        /// Get fade type name
        /// </summary>
        public static string GetFadeTypeName(int fadeType)
        {
            return fadeType switch
            {
                0 => "Linear",
                1 => "Exponential",
                2 => "Sine Wave",
                3 => "Bounce",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Get fade mode name
        /// </summary>
        public static string GetFadeModeName(int fadeMode)
        {
            return fadeMode switch
            {
                0 => "Replace",
                1 => "Additive",
                2 => "Multiply",
                3 => "Overlay",
                _ => "Unknown"
            };
        }

        #endregion
    }
}