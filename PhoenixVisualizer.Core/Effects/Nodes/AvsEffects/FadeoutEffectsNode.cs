using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Fadeout Effects Node - Creates smooth color transitions by fading image colors toward a target color
    /// with beat reactivity, smooth transitions, and advanced fade algorithms
    /// </summary>
    public class FadeoutEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Fadeout effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Fade length controlling the intensity of the fade effect (0.0 to 92.0)
        /// </summary>
        public float FadeLength { get; set; } = 16.0f;

        /// <summary>
        /// Target color toward which all pixels fade (ARGB format)
        /// </summary>
        public int TargetColor { get; set; } = 0x000000; // Black

        /// <summary>
        /// Enable beat-reactive fade length changes
        /// </summary>
        public bool BeatReactive { get; set; } = false;

        /// <summary>
        /// Beat fade multiplier for reactive mode
        /// </summary>
        public float BeatFadeMultiplier { get; set; } = 1.5f;

        /// <summary>
        /// Enable smooth fade transitions
        /// </summary>
        public bool EnableSmoothFade { get; set; } = false;

        /// <summary>
        /// Speed of smooth fade transitions
        /// </summary>
        public float SmoothFadeSpeed { get; set; } = 1.0f;

        /// <summary>
        /// Fade mode: 0=Toward target, 1=Away from target, 2=Oscillate
        /// </summary>
        public int FadeMode { get; set; } = 0;

        /// <summary>
        /// Enable selective fading for individual RGB channels
        /// </summary>
        public bool EnableChannelSelectiveFade { get; set; } = false;

        /// <summary>
        /// Apply fade to red channel
        /// </summary>
        public bool FadeRedChannel { get; set; } = true;

        /// <summary>
        /// Apply fade to green channel
        /// </summary>
        public bool FadeGreenChannel { get; set; } = true;

        /// <summary>
        /// Apply fade to blue channel
        /// </summary>
        public bool FadeBlueChannel { get; set; } = true;

        /// <summary>
        /// Enable fade animation effects
        /// </summary>
        public bool EnableFadeAnimation { get; set; } = false;

        /// <summary>
        /// Speed of fade animation
        /// </summary>
        public float AnimationSpeed { get; set; } = 1.0f;

        /// <summary>
        /// Animation mode: 0=Pulsing, 1=Oscillating, 2=Wave
        /// </summary>
        public int AnimationMode { get; set; } = 0;

        /// <summary>
        /// Enable fade masking using image masks
        /// </summary>
        public bool EnableFadeMasking { get; set; } = false;

        /// <summary>
        /// Fade mask image buffer
        /// </summary>
        public ImageBuffer? FadeMask { get; set; } = null;

        /// <summary>
        /// Influence of the fade mask (0.0 to 1.0)
        /// </summary>
        public float MaskInfluence { get; set; } = 1.0f;

        /// <summary>
        /// Enable blending between faded and original images
        /// </summary>
        public bool EnableFadeBlending { get; set; } = false;

        /// <summary>
        /// Strength of fade blending (0.0 to 1.0)
        /// </summary>
        public float FadeBlendStrength { get; set; } = 0.5f;

        /// <summary>
        /// Fade curve algorithm: 0=Linear, 1=Exponential, 2=Sigmoid
        /// </summary>
        public int FadeCurve { get; set; } = 0;

        /// <summary>
        /// Strength of the fade curve effect
        /// </summary>
        public float FadeCurveStrength { get; set; } = 1.0f;

        /// <summary>
        /// Enable fade inversion above threshold
        /// </summary>
        public bool EnableFadeInversion { get; set; } = false;

        /// <summary>
        /// Threshold for fade inversion (0.0 to 1.0)
        /// </summary>
        public float InversionThreshold { get; set; } = 0.5f;

        #endregion

        #region Private Fields

        private float _currentFadeLength;
        private float _animationTime = 0.0f;
        private readonly byte[,] _fadeLookupTable; // [channel][value] lookup table for performance
        private readonly Random _random = new Random();

        #endregion

        #region Constructor

        public FadeoutEffectsNode()
        {
            Name = "Fadeout Effects";
            Description = "Smooth color transitions with beat reactivity and advanced fade algorithms";
            Category = "AVS Effects";
            
            _currentFadeLength = FadeLength;
            
            // Initialize lookup table for performance optimization
            _fadeLookupTable = new byte[3, 256];
            GenerateFadeLookupTable();
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Input", typeof(ImageBuffer), true, null, "Input image buffer"));
            _inputPorts.Add(new EffectPort("Enabled", typeof(bool), false, true, "Enable/disable effect"));
            _inputPorts.Add(new EffectPort("FadeLength", typeof(float), false, 16.0f, "Fade length (0.0-92.0)"));
            _inputPorts.Add(new EffectPort("TargetColor", typeof(int), false, 0x000000, "Target fade color (ARGB)"));
            _inputPorts.Add(new EffectPort("BeatReactive", typeof(bool), false, false, "Enable beat-reactive fade"));
            _inputPorts.Add(new EffectPort("BeatFadeMultiplier", typeof(float), false, 1.5f, "Beat fade multiplier"));
            _inputPorts.Add(new EffectPort("EnableSmoothFade", typeof(bool), false, false, "Enable smooth transitions"));
            _inputPorts.Add(new EffectPort("SmoothFadeSpeed", typeof(float), false, 1.0f, "Smooth fade speed"));
            _inputPorts.Add(new EffectPort("FadeMode", typeof(int), false, 0, "Fade mode (0=Toward, 1=Away, 2=Oscillate)"));
            _inputPorts.Add(new EffectPort("EnableChannelSelectiveFade", typeof(bool), false, false, "Enable channel selective fade"));
            _inputPorts.Add(new EffectPort("FadeRedChannel", typeof(bool), false, true, "Apply fade to red channel"));
            _inputPorts.Add(new EffectPort("FadeGreenChannel", typeof(bool), false, true, "Apply fade to green channel"));
            _inputPorts.Add(new EffectPort("FadeBlueChannel", typeof(bool), false, true, "Apply fade to blue channel"));
            _inputPorts.Add(new EffectPort("EnableFadeAnimation", typeof(bool), false, false, "Enable fade animation"));
            _inputPorts.Add(new EffectPort("AnimationSpeed", typeof(float), false, 1.0f, "Animation speed"));
            _inputPorts.Add(new EffectPort("AnimationMode", typeof(int), false, 0, "Animation mode (0=Pulsing, 1=Oscillating, 2=Wave)"));
            _inputPorts.Add(new EffectPort("EnableFadeMasking", typeof(bool), false, false, "Enable fade masking"));
            _inputPorts.Add(new EffectPort("FadeMask", typeof(ImageBuffer), false, null, "Fade mask image"));
            _inputPorts.Add(new EffectPort("MaskInfluence", typeof(float), false, 1.0f, "Mask influence (0.0-1.0)"));
            _inputPorts.Add(new EffectPort("EnableFadeBlending", typeof(bool), false, false, "Enable fade blending"));
            _inputPorts.Add(new EffectPort("FadeBlendStrength", typeof(float), false, 0.5f, "Fade blend strength (0.0-1.0)"));
            _inputPorts.Add(new EffectPort("FadeCurve", typeof(int), false, 0, "Fade curve (0=Linear, 1=Exponential, 2=Sigmoid)"));
            _inputPorts.Add(new EffectPort("FadeCurveStrength", typeof(float), false, 1.0f, "Fade curve strength"));
            _inputPorts.Add(new EffectPort("EnableFadeInversion", typeof(bool), false, false, "Enable fade inversion"));
            _inputPorts.Add(new EffectPort("InversionThreshold", typeof(float), false, 0.5f, "Inversion threshold (0.0-1.0)"));
            
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), true, null, "Processed image buffer"));
        }

        #endregion

        #region Process Method

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!Enabled) return inputs["Input"];

            var input = inputs["Input"] as ImageBuffer;
            if (input == null) return inputs["Input"];

            // Update current fade length and animation
            UpdateFadeParameters(audioFeatures);

            // Regenerate lookup table if fade length changed significantly
            if (Math.Abs(_currentFadeLength - FadeLength) > 1.0f)
            {
                GenerateFadeLookupTable();
            }

            // Create output buffer
            var output = new ImageBuffer(input.Width, input.Height);

            // Process each pixel
            for (int i = 0; i < input.Pixels.Length; i++)
            {
                int originalColor = input.Pixels[i];
                int processedColor = ProcessPixel(originalColor, i, input.Width);
                output.Pixels[i] = processedColor;
            }

            return output;
        }

        #endregion

        #region Private Methods

        private void UpdateFadeParameters(AudioFeatures audioFeatures)
        {
            // Update animation time
            if (EnableFadeAnimation)
            {
                _animationTime += 0.016f; // Assuming 60 FPS
            }

            // Calculate base fade length
            float baseFadeLength = FadeLength;

            // Apply beat reactivity
            if (BeatReactive && audioFeatures?.IsBeat == true)
            {
                baseFadeLength *= BeatFadeMultiplier;
            }

            // Apply animation if enabled
            if (EnableFadeAnimation)
            {
                float animationOffset = CalculateAnimationOffset();
                baseFadeLength += animationOffset;
            }

            // Apply smooth fade if enabled
            if (EnableSmoothFade)
            {
                float targetFadeLength = Math.Max(0.0f, Math.Min(92.0f, baseFadeLength));
                float diff = targetFadeLength - _currentFadeLength;
                _currentFadeLength += diff * SmoothFadeSpeed * 0.016f;
            }
            else
            {
                _currentFadeLength = baseFadeLength;
            }

            // Clamp fade length
            _currentFadeLength = Math.Max(0.0f, Math.Min(92.0f, _currentFadeLength));
        }

        private float CalculateAnimationOffset()
        {
            switch (AnimationMode)
            {
                case 0: // Pulsing
                    return 20.0f * (float)Math.Sin(_animationTime * AnimationSpeed * 2.0f);
                case 1: // Oscillating
                    return 15.0f * (float)Math.Sin(_animationTime * AnimationSpeed * 3.0f);
                case 2: // Wave
                    return 25.0f * (float)Math.Sin(_animationTime * AnimationSpeed * 1.5f) * 
                           (float)Math.Cos(_animationTime * AnimationSpeed * 0.8f);
                default:
                    return 0.0f;
            }
        }

        private void GenerateFadeLookupTable()
        {
            // Extract target color channels
            int targetRed = (TargetColor >> 16) & 0xFF;
            int targetGreen = (TargetColor >> 8) & 0xFF;
            int targetBlue = TargetColor & 0xFF;

            for (int value = 0; value < 256; value++)
            {
                // Calculate fade factor based on current fade length
                float fadeFactor = Math.Min(1.0f, _currentFadeLength / 92.0f);
                
                // Apply fade curve
                fadeFactor = ApplyFadeCurve(fadeFactor);

                // Calculate faded values for each channel
                if (FadeRedChannel)
                {
                    _fadeLookupTable[0, value] = (byte)CalculateFadedValue(value, targetRed, fadeFactor);
                }
                else
                {
                    _fadeLookupTable[0, value] = (byte)value;
                }

                if (FadeGreenChannel)
                {
                    _fadeLookupTable[1, value] = (byte)CalculateFadedValue(value, targetGreen, fadeFactor);
                }
                else
                {
                    _fadeLookupTable[1, value] = (byte)value;
                }

                if (FadeBlueChannel)
                {
                    _fadeLookupTable[2, value] = (byte)CalculateFadedValue(value, targetBlue, fadeFactor);
                }
                else
                {
                    _fadeLookupTable[2, value] = (byte)value;
                }
            }
        }

        private float ApplyFadeCurve(float fadeFactor)
        {
            switch (FadeCurve)
            {
                case 1: // Exponential
                    return (float)Math.Pow(fadeFactor, FadeCurveStrength);
                case 2: // Sigmoid
                    return (float)(1.0 / (1.0 + Math.Exp(-10.0 * (fadeFactor - 0.5) * FadeCurveStrength)));
                default: // Linear
                    return fadeFactor;
            }
        }

        private int CalculateFadedValue(int currentValue, int targetValue, float fadeFactor)
        {
            switch (FadeMode)
            {
                case 0: // Toward target
                    return (int)(currentValue + (targetValue - currentValue) * fadeFactor);
                case 1: // Away from target
                    return (int)(currentValue + (currentValue - targetValue) * fadeFactor);
                case 2: // Oscillate
                    float oscillation = (float)Math.Sin(_animationTime * 2.0f) * 0.5f + 0.5f;
                    return (int)(currentValue + (targetValue - currentValue) * fadeFactor * oscillation);
                default:
                    return (int)(currentValue + (targetValue - currentValue) * fadeFactor);
            }
        }

        private int ProcessPixel(int originalColor, int pixelIndex, int width)
        {
            // Extract color channels
            int alpha = (originalColor >> 24) & 0xFF;
            int red = (originalColor >> 16) & 0xFF;
            int green = (originalColor >> 8) & 0xFF;
            int blue = originalColor & 0xFF;

            // Apply fade using lookup table
            int fadedRed = _fadeLookupTable[0, red];
            int fadedGreen = _fadeLookupTable[1, green];
            int fadedBlue = _fadeLookupTable[2, blue];

            // Apply fade masking if enabled
            if (EnableFadeMasking && FadeMask != null && pixelIndex < FadeMask.Pixels.Length)
            {
                int maskColor = FadeMask.Pixels[pixelIndex];
                int maskBrightness = (maskColor & 0xFF) + ((maskColor >> 8) & 0xFF) + ((maskColor >> 16) & 0xFF);
                float maskFactor = maskBrightness / (3.0f * 255.0f) * MaskInfluence;

                // Blend between original and faded based on mask
                fadedRed = (int)(red * (1.0f - maskFactor) + fadedRed * maskFactor);
                fadedGreen = (int)(green * (1.0f - maskFactor) + fadedGreen * maskFactor);
                fadedBlue = (int)(blue * (1.0f - maskFactor) + fadedBlue * maskFactor);
            }

            // Apply fade blending if enabled
            if (EnableFadeBlending)
            {
                fadedRed = (int)(red * (1.0f - FadeBlendStrength) + fadedRed * FadeBlendStrength);
                fadedGreen = (int)(green * (1.0f - FadeBlendStrength) + fadedGreen * FadeBlendStrength);
                fadedBlue = (int)(blue * (1.0f - FadeBlendStrength) + fadedBlue * FadeBlendStrength);
            }

            // Apply fade inversion if enabled
            if (EnableFadeInversion)
            {
                float normalizedFade = _currentFadeLength / 92.0f;
                if (normalizedFade > InversionThreshold)
                {
                    fadedRed = 255 - fadedRed;
                    fadedGreen = 255 - fadedGreen;
                    fadedBlue = 255 - fadedBlue;
                }
            }

            // Clamp values
            fadedRed = Math.Max(0, Math.Min(255, fadedRed));
            fadedGreen = Math.Max(0, Math.Min(255, fadedGreen));
            fadedBlue = Math.Max(0, Math.Min(255, fadedBlue));

            // Return processed color with preserved alpha
            return (alpha << 24) | (fadedBlue << 16) | (fadedGreen << 8) | fadedRed;
        }

        #endregion

        #region Configuration

        public override bool ValidateConfiguration()
        {
            FadeLength = Math.Max(0.0f, Math.Min(92.0f, FadeLength));
            BeatFadeMultiplier = Math.Max(0.1f, Math.Min(5.0f, BeatFadeMultiplier));
            SmoothFadeSpeed = Math.Max(0.1f, Math.Min(10.0f, SmoothFadeSpeed));
            FadeMode = Math.Max(0, Math.Min(2, FadeMode));
            AnimationSpeed = Math.Max(0.1f, Math.Min(10.0f, AnimationSpeed));
            AnimationMode = Math.Max(0, Math.Min(2, AnimationMode));
            MaskInfluence = Math.Max(0.0f, Math.Min(1.0f, MaskInfluence));
            FadeBlendStrength = Math.Max(0.0f, Math.Min(1.0f, FadeBlendStrength));
            FadeCurve = Math.Max(0, Math.Min(2, FadeCurve));
            FadeCurveStrength = Math.Max(0.1f, Math.Min(5.0f, FadeCurveStrength));
            InversionThreshold = Math.Max(0.0f, Math.Min(1.0f, InversionThreshold));

            return true;
        }

        public override string GetSettingsSummary()
        {
            string modeText = FadeMode switch
            {
                0 => "Toward",
                1 => "Away",
                2 => "Oscillate",
                _ => "Unknown"
            };

            string curveText = FadeCurve switch
            {
                0 => "Linear",
                1 => "Exponential",
                2 => "Sigmoid",
                _ => "Unknown"
            };

            string animationText = AnimationMode switch
            {
                0 => "Pulsing",
                1 => "Oscillating",
                2 => "Wave",
                _ => "Unknown"
            };

            return $"Fadeout: {_currentFadeLength:F1}/92.0, Mode: {modeText}, Curve: {curveText}, " +
                   $"Beat: {(BeatReactive ? "On" : "Off")}, Animation: {(EnableFadeAnimation ? animationText : "Off")}, " +
                   $"Channels: R{(FadeRedChannel ? "+" : "-")}G{(FadeGreenChannel ? "+" : "-")}B{(FadeBlueChannel ? "+" : "-")}";
        }

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }

        #endregion
    }
}
