using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class FastBrightnessEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Fast Brightness effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Brightness mode: 0=Brighten, 1=Darken, 2=Off
        /// </summary>
        public int BrightnessMode { get; set; } = 0;

        /// <summary>
        /// Enable beat-reactive brightness mode switching
        /// </summary>
        public bool BeatReactive { get; set; } = false;

        /// <summary>
        /// Beat brightness multiplier for reactive mode
        /// </summary>
        public float BeatBrightnessMultiplier { get; set; } = 1.5f;

        /// <summary>
        /// Enable smooth brightness transitions
        /// </summary>
        public bool EnableSmoothTransition { get; set; } = false;

        /// <summary>
        /// Speed of brightness transitions
        /// </summary>
        public float TransitionSpeed { get; set; } = 1.0f;

        /// <summary>
        /// Enable selective brightness for individual RGB channels
        /// </summary>
        public bool EnableChannelSelectiveBrightness { get; set; } = false;

        /// <summary>
        /// Apply brightness to red channel
        /// </summary>
        public bool BrightenRedChannel { get; set; } = true;

        /// <summary>
        /// Apply brightness to green channel
        /// </summary>
        public bool BrightenGreenChannel { get; set; } = true;

        /// <summary>
        /// Apply brightness to blue channel
        /// </summary>
        public bool BrightenBlueChannel { get; set; } = true;

        /// <summary>
        /// Enable brightness animation effects
        /// </summary>
        public bool EnableBrightnessAnimation { get; set; } = false;

        /// <summary>
        /// Speed of brightness animation
        /// </summary>
        public float AnimationSpeed { get; set; } = 1.0f;

        /// <summary>
        /// Animation mode: 0=Pulsing, 1=Oscillating, 2=Wave
        /// </summary>
        public int AnimationMode { get; set; } = 0;

        /// <summary>
        /// Enable brightness masking using image masks
        /// </summary>
        public bool EnableBrightnessMasking { get; set; } = false;

        /// <summary>
        /// Brightness mask image buffer
        /// </summary>
        public ImageBuffer BrightnessMask { get; set; } = null;

        /// <summary>
        /// Influence of the brightness mask (0.0 to 1.0)
        /// </summary>
        public float MaskInfluence { get; set; } = 1.0f;

        /// <summary>
        /// Enable blending between brightened and original images
        /// </summary>
        public bool EnableBrightnessBlending { get; set; } = false;

        /// <summary>
        /// Strength of brightness blending (0.0 to 1.0)
        /// </summary>
        public float BrightnessBlendStrength { get; set; } = 0.5f;

        /// <summary>
        /// Brightness algorithm: 0=Fast, 1=Quality, 2=Adaptive
        /// </summary>
        public int BrightnessAlgorithm { get; set; } = 0;

        /// <summary>
        /// Power curve for brightness adjustment
        /// </summary>
        public float BrightnessCurve { get; set; } = 1.0f;

        /// <summary>
        /// Enable brightness value clamping
        /// </summary>
        public bool EnableBrightnessClamping { get; set; } = true;

        /// <summary>
        /// Clamp mode: 0=Standard, 1=Soft, 2=Hard
        /// </summary>
        public int ClampMode { get; set; } = 0;

        /// <summary>
        /// Enable brightness inversion above threshold
        /// </summary>
        public bool EnableBrightnessInversion { get; set; } = false;

        /// <summary>
        /// Threshold for brightness inversion (0.0 to 1.0)
        /// </summary>
        public float InversionThreshold { get; set; } = 0.5f;

        #endregion

        #region Private Fields

        private float currentBrightnessMultiplier = 1.0f;
        private float animationTime = 0.0f;
        private readonly Random random = new Random();

        #endregion

        #region Constructor

        public FastBrightnessEffectsNode()
        {
            Name = "Fast Brightness Effects";
            Description = "High-performance brightness adjustment with multiple modes and optimizations";
            Category = "AVS Effects";
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Input", typeof(ImageBuffer), true, null, "Input image buffer"));
            _inputPorts.Add(new EffectPort("Enabled", typeof(bool), false, true, "Enable/disable effect"));
            _inputPorts.Add(new EffectPort("BrightnessMode", typeof(int), false, 0, "Brightness mode (0=Brighten, 1=Darken, 2=Off)"));
            _inputPorts.Add(new EffectPort("BeatReactive", typeof(bool), false, false, "Enable beat-reactive brightness"));
            _inputPorts.Add(new EffectPort("BeatBrightnessMultiplier", typeof(float), false, 1.5f, "Beat brightness multiplier"));
            _inputPorts.Add(new EffectPort("EnableSmoothTransition", typeof(bool), false, false, "Enable smooth transitions"));
            _inputPorts.Add(new EffectPort("TransitionSpeed", typeof(float), false, 1.0f, "Transition speed"));
            _inputPorts.Add(new EffectPort("EnableChannelSelectiveBrightness", typeof(bool), false, false, "Enable channel selective brightness"));
            _inputPorts.Add(new EffectPort("BrightenRedChannel", typeof(bool), false, true, "Apply brightness to red channel"));
            _inputPorts.Add(new EffectPort("BrightenGreenChannel", typeof(bool), false, true, "Apply brightness to green channel"));
            _inputPorts.Add(new EffectPort("BrightenBlueChannel", typeof(bool), false, true, "Apply brightness to blue channel"));
            _inputPorts.Add(new EffectPort("EnableBrightnessAnimation", typeof(bool), false, false, "Enable brightness animation"));
            _inputPorts.Add(new EffectPort("AnimationSpeed", typeof(float), false, 1.0f, "Animation speed"));
            _inputPorts.Add(new EffectPort("AnimationMode", typeof(int), false, 0, "Animation mode (0=Pulsing, 1=Oscillating, 2=Wave)"));
            _inputPorts.Add(new EffectPort("EnableBrightnessMasking", typeof(bool), false, false, "Enable brightness masking"));
            _inputPorts.Add(new EffectPort("BrightnessMask", typeof(ImageBuffer), false, null, "Brightness mask image"));
            _inputPorts.Add(new EffectPort("MaskInfluence", typeof(float), false, 1.0f, "Mask influence (0.0-1.0)"));
            _inputPorts.Add(new EffectPort("EnableBrightnessBlending", typeof(bool), false, false, "Enable brightness blending"));
            _inputPorts.Add(new EffectPort("BrightnessBlendStrength", typeof(float), false, 0.5f, "Blend strength (0.0-1.0)"));
            _inputPorts.Add(new EffectPort("BrightnessAlgorithm", typeof(int), false, 0, "Algorithm (0=Fast, 1=Quality, 2=Adaptive)"));
            _inputPorts.Add(new EffectPort("BrightnessCurve", typeof(float), false, 1.0f, "Brightness power curve"));
            _inputPorts.Add(new EffectPort("EnableBrightnessClamping", typeof(bool), false, true, "Enable brightness clamping"));
            _inputPorts.Add(new EffectPort("ClampMode", typeof(int), false, 0, "Clamp mode (0=Standard, 1=Soft, 2=Hard)"));
            _inputPorts.Add(new EffectPort("EnableBrightnessInversion", typeof(bool), false, false, "Enable brightness inversion"));
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

            // Update animation time
            if (EnableBrightnessAnimation)
            {
                UpdateBrightnessAnimation();
            }

            // Get current brightness mode
            int currentMode = GetCurrentBrightnessMode(audioFeatures);

            // Off mode - return original
            if (currentMode == 2)
                return input;

            // Create output buffer
            var output = new ImageBuffer(input.Width, input.Height);

            // Process each pixel
            for (int i = 0; i < input.Pixels.Length; i++)
            {
                int originalColor = input.Pixels[i];
                int processedColor = ApplyBrightness(originalColor, currentMode);

                // Apply channel selective brightness if enabled
                if (EnableChannelSelectiveBrightness)
                {
                    processedColor = ApplyChannelSelectiveBrightness(originalColor, processedColor, currentMode);
                }

                // Apply brightness masking if enabled
                if (EnableBrightnessMasking && BrightnessMask != null)
                {
                    processedColor = ApplyBrightnessMasking(originalColor, processedColor, i, input.Width);
                }

                // Apply brightness blending if enabled
                if (EnableBrightnessBlending)
                {
                    processedColor = BlendBrightness(originalColor, processedColor);
                }

                output.Pixels[i] = processedColor;
            }

            return output;
        }

        #endregion

        #region Private Methods

        private int GetCurrentBrightnessMode(AudioFeatures audioFeatures)
        {
            if (!BeatReactive || audioFeatures == null)
                return BrightnessMode;

            if (audioFeatures.IsBeat)
            {
                // Switch to brighten mode on beat
                return 0;
            }
            else
            {
                // Return to original mode
                return BrightnessMode;
            }
        }

        private int ApplyBrightness(int color, int mode)
        {
            int r = color & 0xFF;
            int g = (color >> 8) & 0xFF;
            int b = (color >> 16) & 0xFF;
            int a = (color >> 24) & 0xFF;

            switch (mode)
            {
                case 0: // Brighten
                    return ApplyBrightenMode(r, g, b, a);
                case 1: // Darken
                    return ApplyDarkenMode(r, g, b, a);
                default:
                    return color;
            }
        }

        private int ApplyBrightenMode(int r, int g, int b, int a)
        {
            switch (BrightnessAlgorithm)
            {
                case 0: // Fast (original behavior)
                    r = Math.Min(255, r * 2);
                    g = Math.Min(255, g * 2);
                    b = Math.Min(255, b * 2);
                    break;

                case 1: // Quality
                    r = (int)Math.Min(255, r * (1.0f + BrightnessCurve));
                    g = (int)Math.Min(255, g * (1.0f + BrightnessCurve));
                    b = (int)Math.Min(255, b * (1.0f + BrightnessCurve));
                    break;

                case 2: // Adaptive
                    float brightness = (r + g + b) / (3.0f * 255.0f);
                    float adaptiveMultiplier = 1.0f + (BrightnessCurve * (1.0f - brightness));
                    r = (int)Math.Min(255, r * adaptiveMultiplier);
                    g = (int)Math.Min(255, g * adaptiveMultiplier);
                    b = (int)Math.Min(255, b * adaptiveMultiplier);
                    break;
            }

            // Apply clamping if enabled
            if (EnableBrightnessClamping)
            {
                r = ApplyClamping(r);
                g = ApplyClamping(g);
                b = ApplyClamping(b);
            }

            // Apply inversion if enabled
            if (EnableBrightnessInversion)
            {
                r = ApplyInversion(r);
                g = ApplyInversion(g);
                b = ApplyInversion(b);
            }

            return (a << 24) | (b << 16) | (g << 8) | r;
        }

        private int ApplyDarkenMode(int r, int g, int b, int a)
        {
            switch (BrightnessAlgorithm)
            {
                case 0: // Fast (original behavior)
                    r = r >> 1;
                    g = g >> 1;
                    b = b >> 1;
                    break;

                case 1: // Quality
                    float darkenFactor = 1.0f / (1.0f + BrightnessCurve);
                    r = (int)(r * darkenFactor);
                    g = (int)(g * darkenFactor);
                    b = (int)(b * darkenFactor);
                    break;

                case 2: // Adaptive
                    float brightness = (r + g + b) / (3.0f * 255.0f);
                    float adaptiveFactor = 1.0f / (1.0f + (BrightnessCurve * brightness));
                    r = (int)(r * adaptiveFactor);
                    g = (int)(g * adaptiveFactor);
                    b = (int)(b * adaptiveFactor);
                    break;
            }

            // Apply clamping if enabled
            if (EnableBrightnessClamping)
            {
                r = ApplyClamping(r);
                g = ApplyClamping(g);
                b = ApplyClamping(b);
            }

            return (a << 24) | (b << 16) | (g << 8) | r;
        }

        private int ApplyChannelSelectiveBrightness(int originalColor, int processedColor, int mode)
        {
            if (!EnableChannelSelectiveBrightness)
                return processedColor;

            int r = originalColor & 0xFF;
            int g = (originalColor >> 8) & 0xFF;
            int b = (originalColor >> 16) & 0xFF;
            int a = (originalColor >> 24) & 0xFF;

            int processedR = processedColor & 0xFF;
            int processedG = (processedColor >> 8) & 0xFF;
            int processedB = (processedColor >> 16) & 0xFF;

            int finalR = BrightenRedChannel ? processedR : r;
            int finalG = BrightenGreenChannel ? processedG : g;
            int finalB = BrightenBlueChannel ? processedB : b;

            return (a << 24) | (finalB << 16) | (finalG << 8) | finalR;
        }

        private int ApplyBrightnessMasking(int originalColor, int processedColor, int pixelIndex, int width)
        {
            if (BrightnessMask == null || pixelIndex >= BrightnessMask.Pixels.Length)
                return processedColor;

            int maskColor = BrightnessMask.Pixels[pixelIndex];
            int maskBrightness = (maskColor & 0xFF) + ((maskColor >> 8) & 0xFF) + ((maskColor >> 16) & 0xFF);
            float maskFactor = maskBrightness / (3.0f * 255.0f) * MaskInfluence;

            // Blend between original and processed based on mask
            int r = (int)((originalColor & 0xFF) * (1.0f - maskFactor) + (processedColor & 0xFF) * maskFactor);
            int g = (int)(((originalColor >> 8) & 0xFF) * (1.0f - maskFactor) + ((processedColor >> 8) & 0xFF) * maskFactor);
            int b = (int)(((originalColor >> 16) & 0xFF) * (1.0f - maskFactor) + ((processedColor >> 16) & 0xFF) * maskFactor);
            int a = (originalColor >> 24) & 0xFF;

            return (a << 24) | (b << 16) | (g << 8) | r;
        }

        private int BlendBrightness(int originalColor, int processedColor)
        {
            if (!EnableBrightnessBlending)
                return processedColor;

            int r = (int)((originalColor & 0xFF) * (1.0f - BrightnessBlendStrength) + (processedColor & 0xFF) * BrightnessBlendStrength);
            int g = (int)(((originalColor >> 8) & 0xFF) * (1.0f - BrightnessBlendStrength) + ((processedColor >> 8) & 0xFF) * BrightnessBlendStrength);
            int b = (int)(((originalColor >> 16) & 0xFF) * (1.0f - BrightnessBlendStrength) + ((processedColor >> 16) & 0xFF) * BrightnessBlendStrength);
            int a = (originalColor >> 24) & 0xFF;

            return (a << 24) | (b << 16) | (g << 8) | r;
        }

        private int ApplyClamping(int value)
        {
            if (!EnableBrightnessClamping)
                return value;

            switch (ClampMode)
            {
                case 0: // Standard
                    return Math.Max(0, Math.Min(255, value));
                case 1: // Soft
                    if (value < 0) return 0;
                    if (value > 255) return 255;
                    return value;
                case 2: // Hard
                    if (value < 0) return 0;
                    if (value > 255) return 255;
                    return value;
                default:
                    return Math.Max(0, Math.Min(255, value));
            }
        }

        private int ApplyInversion(int value)
        {
            if (!EnableBrightnessInversion)
                return value;

            float normalizedValue = value / 255.0f;
            if (normalizedValue > InversionThreshold)
            {
                return 255 - value;
            }
            return value;
        }

        private void UpdateBrightnessAnimation()
        {
            if (!EnableBrightnessAnimation)
                return;

            animationTime += AnimationSpeed * 0.016f; // Assuming 60 FPS

            switch (AnimationMode)
            {
                case 0: // Pulsing
                    currentBrightnessMultiplier = 1.0f + 0.5f * (float)Math.Sin(animationTime * 2.0f);
                    break;
                case 1: // Oscillating
                    currentBrightnessMultiplier = 1.0f + 0.3f * (float)Math.Sin(animationTime * 3.0f);
                    break;
                case 2: // Wave
                    currentBrightnessMultiplier = 1.0f + 0.4f * (float)Math.Sin(animationTime * 1.5f) * (float)Math.Cos(animationTime * 0.8f);
                    break;
                default:
                    currentBrightnessMultiplier = 1.0f;
                    break;
            }
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Validates the current configuration
        /// </summary>
        public override bool ValidateConfiguration()
        {
            BrightnessMode = Math.Max(0, Math.Min(2, BrightnessMode));
            BeatBrightnessMultiplier = Math.Max(0.1f, Math.Min(5.0f, BeatBrightnessMultiplier));
            TransitionSpeed = Math.Max(0.1f, Math.Max(10.0f, TransitionSpeed));
            MaskInfluence = Math.Max(0.0f, Math.Min(1.0f, MaskInfluence));
            BrightnessBlendStrength = Math.Max(0.0f, Math.Min(1.0f, BrightnessBlendStrength));
            BrightnessAlgorithm = Math.Max(0, Math.Min(2, BrightnessAlgorithm));
            BrightnessCurve = Math.Max(0.1f, Math.Min(5.0f, BrightnessCurve));
            ClampMode = Math.Max(0, Math.Min(2, ClampMode));
            InversionThreshold = Math.Max(0.0f, Math.Min(1.0f, InversionThreshold));

            return true;
        }

        /// <summary>
        /// Returns a summary of current settings
        /// </summary>
        public override string GetSettingsSummary()
        {
            string modeText = BrightnessMode switch
            {
                0 => "Brighten",
                1 => "Darken",
                2 => "Off",
                _ => "Unknown"
            };

            string algorithmText = BrightnessAlgorithm switch
            {
                0 => "Fast",
                1 => "Quality",
                2 => "Adaptive",
                _ => "Unknown"
            };

            return $"Fast Brightness: {modeText}, Algorithm: {algorithmText}, " +
                   $"Beat: {(BeatReactive ? "On" : "Off")}, " +
                   $"Animation: {(EnableBrightnessAnimation ? "On" : "Off")}, " +
                   $"Channels: R{(BrightenRedChannel ? "+" : "-")}G{(BrightenGreenChannel ? "+" : "-")}B{(BrightenBlueChannel ? "+" : "-")}";
        }

        protected override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }

        #endregion
    }
}
