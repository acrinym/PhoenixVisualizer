using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class ContrastEffectsNode : BaseEffectNode
    {
        #region Properties

        public bool Enabled { get; set; } = true;
        public float Contrast { get; set; } = 1.0f;
        public float Brightness { get; set; } = 0.0f;
        public float Gamma { get; set; } = 1.0f;
        public bool BeatReactive { get; set; } = false;
        public float BeatContrastMultiplier { get; set; } = 1.5f;
        public bool EnableChannelSelectiveContrast { get; set; } = false;
        public float RedChannelContrast { get; set; } = 1.0f;
        public float GreenChannelContrast { get; set; } = 1.0f;
        public float BlueChannelContrast { get; set; } = 1.0f;
        public bool EnableContrastAnimation { get; set; } = false;
        public float AnimationSpeed { get; set; } = 1.0f;
        public int AnimationMode { get; set; } = 0; // 0=Sine, 1=Triangle, 2=Random
        public bool EnableContrastMasking { get; set; } = false;
        public ImageBuffer? ContrastMask { get; set; }
        public float MaskInfluence { get; set; } = 1.0f;
        public bool EnableContrastClamping { get; set; } = false;
        public int ClampMode { get; set; } = 0; // 0=Clamp, 1=Wrap, 2=Reflect
        public bool EnableContrastInversion { get; set; } = false;
        public float InversionThreshold { get; set; } = 0.5f;
        public int ContrastAlgorithm { get; set; } = 0; // 0=Linear, 1=Curve-based, 2=Adaptive
        public float ContrastCurve { get; set; } = 1.0f;

        #endregion

        #region Private Fields

        private float _currentTime = 0.0f;
        private readonly Random _random = new Random();

        #endregion

        #region Constructor

        public ContrastEffectsNode()
        {
            Name = "Contrast Effects";
            Description = "Adjusts image contrast with advanced algorithms and beat reactivity";
            Category = "Color Transformation";
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for contrast adjustment"));
            _inputPorts.Add(new EffectPort("Mask", typeof(ImageBuffer), false, null, "Optional contrast mask image"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Contrast-adjusted output image"));
        }

        #endregion

        #region Process Method

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
            {
                return GetDefaultOutput();
            }

            if (!Enabled)
            {
                return imageBuffer;
            }

            // Check for mask input
            if (inputs.TryGetValue("Mask", out var maskInput) && maskInput is ImageBuffer maskBuffer)
            {
                ContrastMask = maskBuffer;
            }

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
            var currentContrast = GetCurrentContrast(audioFeatures);

            // Update animation if enabled
            if (EnableContrastAnimation)
            {
                UpdateContrastAnimation(0.016f); // Assuming 60 FPS
            }

            // Process each pixel
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    var originalPixel = imageBuffer.GetPixel(x, y);
                    var adjustedPixel = ApplyContrast(originalPixel, currentContrast, x, y);

                    output.SetPixel(x, y, adjustedPixel);
                }
            }

            return output;
        }

        #endregion

        #region Helper Methods

        private float GetCurrentContrast(AudioFeatures audioFeatures)
        {
            float baseContrast = Contrast;

            if (BeatReactive && audioFeatures?.IsBeat == true)
            {
                baseContrast *= BeatContrastMultiplier;
            }

            return baseContrast;
        }

        private int ApplyContrast(int color, float contrast, int x, int y)
        {
            // Extract RGB components
            int r = color & 0xFF;
            int g = (color >> 8) & 0xFF;
            int b = (color >> 16) & 0xFF;
            int a = (color >> 24) & 0xFF;

            // Apply channel selective contrast if enabled
            if (EnableChannelSelectiveContrast)
            {
                r = ApplyChannelContrast(r, RedChannelContrast);
                g = ApplyChannelContrast(g, GreenChannelContrast);
                b = ApplyChannelContrast(b, BlueChannelContrast);
            }
            else
            {
                r = ApplyChannelContrast(r, contrast);
                g = ApplyChannelContrast(g, contrast);
                b = ApplyChannelContrast(b, contrast);
            }

            // Apply brightness
            r = Math.Clamp(r + (int)(Brightness * 255), 0, 255);
            g = Math.Clamp(g + (int)(Brightness * 255), 0, 255);
            b = Math.Clamp(b + (int)(Brightness * 255), 0, 255);

            // Apply gamma correction
            r = ApplyGamma(r, Gamma);
            g = ApplyGamma(g, Gamma);
            b = ApplyGamma(b, Gamma);

            // Apply contrast masking if enabled
            if (EnableContrastMasking && ContrastMask != null)
            {
                var maskColor = ContrastMask.GetPixel(x, y);
                float maskValue = ((maskColor & 0xFF) + ((maskColor >> 8) & 0xFF) + ((maskColor >> 16) & 0xFF)) / (3.0f * 255.0f);
                float maskMultiplier = 1.0f + (MaskInfluence * (maskValue - 0.5f));
                
                r = Math.Clamp((int)(r * maskMultiplier), 0, 255);
                g = Math.Clamp((int)(g * maskMultiplier), 0, 255);
                b = Math.Clamp((int)(b * maskMultiplier), 0, 255);
            }

            // Apply clamping if enabled
            if (EnableContrastClamping)
            {
                r = ApplyClamping(r);
                g = ApplyClamping(g);
                b = ApplyClamping(b);
            }

            // Apply inversion if enabled
            if (EnableContrastInversion)
            {
                r = ApplyInversion(r);
                g = ApplyInversion(g);
                b = ApplyInversion(b);
            }

            return (a << 24) | (b << 16) | (g << 8) | r;
        }

        private int ApplyChannelContrast(int value, float contrast)
        {
            switch (ContrastAlgorithm)
            {
                case 0: // Linear
                    return Math.Clamp((int)((value - 128) * contrast + 128), 0, 255);
                
                case 1: // Curve-based
                    float normalized = value / 255.0f;
                    float curved = (float)Math.Pow(normalized, ContrastCurve);
                    return Math.Clamp((int)(curved * 255), 0, 255);
                
                case 2: // Adaptive
                    float adaptiveContrast = contrast * (1.0f + (value / 255.0f) * ContrastCurve);
                    return Math.Clamp((int)((value - 128) * adaptiveContrast + 128), 0, 255);
                
                default:
                    return Math.Clamp((int)((value - 128) * contrast + 128), 0, 255);
            }
        }

        private int ApplyGamma(int value, float gamma)
        {
            if (gamma == 1.0f) return value;
            
            float normalized = value / 255.0f;
            float corrected = (float)Math.Pow(normalized, gamma);
            return Math.Clamp((int)(corrected * 255), 0, 255);
        }

        private int ApplyClamping(int value)
        {
            switch (ClampMode)
            {
                case 0: // Clamp
                    return Math.Clamp(value, 0, 255);
                
                case 1: // Wrap
                    return ((value % 256) + 256) % 256;
                
                case 2: // Reflect
                    if (value < 0) return -value;
                    if (value > 255) return 510 - value;
                    return value;
                
                default:
                    return Math.Clamp(value, 0, 255);
            }
        }

        private int ApplyInversion(int value)
        {
            if (value > InversionThreshold * 255)
            {
                return 255 - value;
            }
            return value;
        }

        private void UpdateContrastAnimation(float deltaTime)
        {
            _currentTime += deltaTime * AnimationSpeed;
            
            float animationValue = 0.0f;
            
            switch (AnimationMode)
            {
                case 0: // Sine wave
                    animationValue = (float)Math.Sin(_currentTime) * 0.5f + 0.5f;
                    break;
                
                case 1: // Triangle wave
                    animationValue = 2.0f * Math.Abs((_currentTime % (2.0f * (float)Math.PI)) / (2.0f * (float)Math.PI) - 0.5f);
                    break;
                
                case 2: // Random
                    if (_currentTime > 1.0f)
                    {
                        animationValue = (float)_random.NextDouble();
                        _currentTime = 0.0f;
                    }
                    break;
            }
            
            // Apply animation to contrast
            Contrast = 0.5f + animationValue * 2.0f;
        }

        #endregion

        #region Validation and Settings

        public override bool ValidateConfiguration()
        {
            return Contrast >= 0.0f && Contrast <= 5.0f &&
                   Brightness >= -1.0f && Brightness <= 1.0f &&
                   Gamma >= 0.1f && Gamma <= 5.0f &&
                   BeatContrastMultiplier >= 0.1f && BeatContrastMultiplier <= 5.0f &&
                   MaskInfluence >= 0.0f && MaskInfluence <= 2.0f &&
                   InversionThreshold >= 0.0f && InversionThreshold <= 1.0f &&
                   ContrastCurve >= 0.1f && ContrastCurve <= 5.0f;
        }

        public override string GetSettingsSummary()
        {
            return $"Contrast: {Contrast:F2}, Brightness: {Brightness:F2}, Gamma: {Gamma:F2}, " +
                   $"BeatReactive: {BeatReactive}, Algorithm: {ContrastAlgorithm}";
        }

        #endregion
    }
}
