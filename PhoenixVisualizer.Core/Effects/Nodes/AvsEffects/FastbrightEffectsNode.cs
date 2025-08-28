using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Fast Brightness effect with optimized algorithms
    /// High-performance brightness adjustment with various optimization modes
    /// Different from standard brightness with additional processing options
    /// </summary>
    public class FastbrightEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Fast Brightness effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Brightness adjustment level (-2.0 to 2.0, 0.0 = no change)
        /// </summary>
        public float BrightnessLevel { get; set; } = 0.0f;

        /// <summary>
        /// Processing mode
        /// 0 = Linear adjustment, 1 = Logarithmic, 2 = Exponential, 3 = S-curve
        /// </summary>
        public int ProcessingMode { get; set; } = 0;

        /// <summary>
        /// Enable fast lookup table mode for performance
        /// </summary>
        public bool UseLookupTable { get; set; } = true;

        /// <summary>
        /// Beat reactivity enabled
        /// </summary>
        public bool BeatReactive { get; set; } = false;

        /// <summary>
        /// Beat brightness boost factor
        /// </summary>
        public float BeatBrightnessBoost { get; set; } = 0.3f;

        /// <summary>
        /// Auto-level adjustment based on image content
        /// </summary>
        public bool AutoLevel { get; set; } = false;

        /// <summary>
        /// Target average brightness for auto-level (0.0 to 1.0)
        /// </summary>
        public float TargetBrightness { get; set; } = 0.5f;

        /// <summary>
        /// Auto-level adaptation speed (0.0 to 1.0)
        /// </summary>
        public float AdaptationSpeed { get; set; } = 0.1f;

        /// <summary>
        /// Preserve highlights during adjustment
        /// </summary>
        public bool PreserveHighlights { get; set; } = true;

        /// <summary>
        /// Highlight preservation threshold (0.0 to 1.0)
        /// </summary>
        public float HighlightThreshold { get; set; } = 0.9f;

        /// <summary>
        /// Process RGB channels separately
        /// </summary>
        public bool ProcessChannelsSeparately { get; set; } = false;

        /// <summary>
        /// Red channel brightness multiplier
        /// </summary>
        public float RedMultiplier { get; set; } = 1.0f;

        /// <summary>
        /// Green channel brightness multiplier
        /// </summary>
        public float GreenMultiplier { get; set; } = 1.0f;

        /// <summary>
        /// Blue channel brightness multiplier
        /// </summary>
        public float BlueMultiplier { get; set; } = 1.0f;

        #endregion

        #region Private Fields

        private float[] _lookupTable = new float[256];
        private int _beatCounter = 0;
        private float _currentAutoLevel = 0.0f;
        private const int BEAT_DURATION = 15;

        #endregion

        #region Constructor

        public FastbrightEffectsNode()
        {
            Name = "Fast Brightness Effects";
            Description = "High-performance brightness adjustment with optimization modes";
            Category = "Color Effects";
        }

        #endregion

        #region Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for brightness adjustment"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Brightness adjusted output image"));
        }

        #endregion

        #region Effect Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!Enabled) 
                return GetDefaultOutput();

            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
                return GetDefaultOutput();

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
            
            // Calculate effective brightness
            float effectiveBrightness = BrightnessLevel;
            
            // Apply beat reactivity
            if (BeatReactive && audioFeatures?.IsBeat == true)
            {
                effectiveBrightness += BeatBrightnessBoost;
            }

            // Apply brightness adjustment
            for (int i = 0; i < output.Pixels.Length; i++)
            {
                int pixel = imageBuffer.Pixels[i];
                int r = (pixel >> 16) & 0xFF;
                int g = (pixel >> 8) & 0xFF;
                int b = pixel & 0xFF;

                // Apply brightness with channel multipliers
                r = (int)((r * RedMultiplier + effectiveBrightness * 255) * 0.5f);
                g = (int)((g * GreenMultiplier + effectiveBrightness * 255) * 0.5f);
                b = (int)((b * BlueMultiplier + effectiveBrightness * 255) * 0.5f);

                // Clamp values
                r = Math.Clamp(r, 0, 255);
                g = Math.Clamp(g, 0, 255);
                b = Math.Clamp(b, 0, 255);

                output.Pixels[i] = (r << 16) | (g << 8) | b;
            }

            return output;
        }

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }

        #endregion

        #region Private Methods

        private void UpdateAutoLevel(ImageBuffer source)
        {
            // Calculate current average brightness
            float totalBrightness = 0;
            int pixelCount = source.Data.Length;

            for (int i = 0; i < pixelCount; i++)
            {
                uint pixel = source.Data[i];
                uint r = (pixel >> 16) & 0xFF;
                uint g = (pixel >> 8) & 0xFF;
                uint b = pixel & 0xFF;
                
                float brightness = (r + g + b) / (3.0f * 255.0f);
                totalBrightness += brightness;
            }

            float averageBrightness = totalBrightness / pixelCount;
            
            // Calculate adjustment needed
            float targetAdjustment = TargetBrightness - averageBrightness;
            
            // Apply adaptation speed
            _currentAutoLevel += targetAdjustment * AdaptationSpeed;
            _currentAutoLevel = Math.Max(-1.0f, Math.Min(1.0f, _currentAutoLevel));
        }

        private void UpdateLookupTable()
        {
            float effectiveBrightness = CalculateEffectiveBrightness();

            for (int i = 0; i < 256; i++)
            {
                float normalized = i / 255.0f;
                float adjusted = ApplyBrightnessFunction(normalized, effectiveBrightness);
                _lookupTable[i] = Math.Max(0.0f, Math.Min(1.0f, adjusted));
            }
        }

        private float CalculateEffectiveBrightness()
        {
            float brightness = BrightnessLevel;
            
            // Add auto-level adjustment
            if (AutoLevel)
            {
                brightness += _currentAutoLevel;
            }
            
            // Add beat boost
            if (BeatReactive && _beatCounter > 0)
            {
                float beatFactor = (_beatCounter / (float)BEAT_DURATION);
                brightness += BeatBrightnessBoost * beatFactor;
            }
            
            return Math.Max(-2.0f, Math.Min(2.0f, brightness));
        }

        private float ApplyBrightnessFunction(float input, float brightness)
        {
            switch (ProcessingMode)
            {
                case 0: // Linear
                    return input + brightness;

                case 1: // Logarithmic
                    if (brightness > 0)
                    {
                        return input + brightness * (1.0f - input);
                    }
                    else
                    {
                        return input + brightness * input;
                    }

                case 2: // Exponential
                    if (brightness > 0)
                    {
                        return (float)Math.Pow(input, 1.0 - brightness * 0.5);
                    }
                    else
                    {
                        return (float)Math.Pow(input, 1.0 + Math.Abs(brightness) * 0.5);
                    }

                case 3: // S-curve
                    float midpoint = 0.5f + brightness * 0.3f;
                    float contrast = 1.0f + Math.Abs(brightness);
                    return ApplySCurve(input, midpoint, contrast);

                default:
                    return input + brightness;
            }
        }

        private float ApplySCurve(float input, float midpoint, float contrast)
        {
            // S-curve function with adjustable midpoint and contrast
            float adjusted = (input - midpoint) * contrast + midpoint;
            return 1.0f / (1.0f + (float)Math.Exp(-6.0 * (adjusted - 0.5)));
        }

        private void ApplyBrightnessWithLUT(ImageBuffer source, ImageBuffer output)
        {
            for (int i = 0; i < source.Data.Length; i++)
            {
                uint pixel = source.Data[i];
                
                uint a = (pixel >> 24) & 0xFF;
                uint r = (pixel >> 16) & 0xFF;
                uint g = (pixel >> 8) & 0xFF;
                uint b = pixel & 0xFF;

                if (ProcessChannelsSeparately)
                {
                    r = ApplyLUTWithMultiplier(r, RedMultiplier);
                    g = ApplyLUTWithMultiplier(g, GreenMultiplier);
                    b = ApplyLUTWithMultiplier(b, BlueMultiplier);
                }
                else
                {
                    r = ApplyLUTWithHighlightPreservation(r);
                    g = ApplyLUTWithHighlightPreservation(g);
                    b = ApplyLUTWithHighlightPreservation(b);
                }

                output.Data[i] = (a << 24) | (r << 16) | (g << 8) | b;
            }
        }

        private uint ApplyLUTWithMultiplier(uint value, float multiplier)
        {
            float adjusted = _lookupTable[value] * multiplier;
            return (uint)Math.Max(0, Math.Min(255, Math.Round(adjusted * 255)));
        }

        private uint ApplyLUTWithHighlightPreservation(uint value)
        {
            float normalized = value / 255.0f;
            float adjusted = _lookupTable[value];

            // Apply highlight preservation
            if (PreserveHighlights && normalized > HighlightThreshold)
            {
                float preservationFactor = (normalized - HighlightThreshold) / (1.0f - HighlightThreshold);
                adjusted = adjusted * (1.0f - preservationFactor) + normalized * preservationFactor;
            }

            return (uint)Math.Max(0, Math.Min(255, Math.Round(adjusted * 255)));
        }

        private void ApplyBrightnessDirectly(ImageBuffer source, ImageBuffer output)
        {
            float effectiveBrightness = CalculateEffectiveBrightness();

            for (int i = 0; i < source.Data.Length; i++)
            {
                uint pixel = source.Data[i];
                
                uint a = (pixel >> 24) & 0xFF;
                uint r = (pixel >> 16) & 0xFF;
                uint g = (pixel >> 8) & 0xFF;
                uint b = pixel & 0xFF;

                if (ProcessChannelsSeparately)
                {
                    r = ApplyBrightnessToChannel(r, effectiveBrightness * RedMultiplier);
                    g = ApplyBrightnessToChannel(g, effectiveBrightness * GreenMultiplier);
                    b = ApplyBrightnessToChannel(b, effectiveBrightness * BlueMultiplier);
                }
                else
                {
                    r = ApplyBrightnessToChannel(r, effectiveBrightness);
                    g = ApplyBrightnessToChannel(g, effectiveBrightness);
                    b = ApplyBrightnessToChannel(b, effectiveBrightness);
                }

                output.Data[i] = (a << 24) | (r << 16) | (g << 8) | b;
            }
        }

        private uint ApplyBrightnessToChannel(uint value, float brightness)
        {
            float normalized = value / 255.0f;
            float adjusted = ApplyBrightnessFunction(normalized, brightness);

            // Apply highlight preservation
            if (PreserveHighlights && normalized > HighlightThreshold)
            {
                float preservationFactor = (normalized - HighlightThreshold) / (1.0f - HighlightThreshold);
                adjusted = adjusted * (1.0f - preservationFactor) + normalized * preservationFactor;
            }

            return (uint)Math.Max(0, Math.Min(255, Math.Round(adjusted * 255)));
        }

        #endregion
    }
}