using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Brightness Effects Node - Adjusts brightness, contrast, and gamma of images
    /// </summary>
    public class BrightnessEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Brightness multiplier (0.0 = black, 1.0 = normal, 2.0 = twice as bright)
        /// </summary>
        public float Brightness { get; set; } = 1.0f;

        /// <summary>
        /// Contrast multiplier (0.5 = low contrast, 1.0 = normal, 2.0 = high contrast)
        /// </summary>
        public float Contrast { get; set; } = 1.0f;

        /// <summary>
        /// Gamma correction (0.5 = lighter midtones, 1.0 = normal, 2.0 = darker midtones)
        /// </summary>
        public float Gamma { get; set; } = 1.0f;

        /// <summary>
        /// Enable beat-reactive brightness changes
        /// </summary>
        public bool BeatReactive { get; set; } = false;

        /// <summary>
        /// Beat brightness multiplier
        /// </summary>
        public float BeatBrightness { get; set; } = 1.5f;

        #endregion

        #region Constructor

        public BrightnessEffectsNode()
        {
            Name = "Brightness Effects";
            Description = "Adjusts brightness, contrast, and gamma of the image";
            Category = "AVS Effects";
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Input image for brightness adjustment"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Brightness adjusted output image"));
        }

        #endregion

        #region Process Method

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
            {
                return GetDefaultOutput();
            }

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
            float currentBrightness = Brightness;

            if (BeatReactive && audioFeatures?.IsBeat == true)
            {
                currentBrightness *= BeatBrightness;
            }

            for (int y = 0; y < imageBuffer.Height; y++)
            {
                for (int x = 0; x < imageBuffer.Width; x++)
                {
                    int pixel = imageBuffer.GetPixel(x, y);
                    
                    // Extract RGB components
                    int r = pixel & 0xFF;
                    int g = (pixel >> 8) & 0xFF;
                    int b = (pixel >> 16) & 0xFF;

                    // Convert to normalized values
                    float rNorm = r / 255.0f;
                    float gNorm = g / 255.0f;
                    float bNorm = b / 255.0f;

                    // Apply brightness, contrast, and gamma
                    rNorm = ApplyBrightnessContrastGamma(rNorm, currentBrightness, Contrast, Gamma);
                    gNorm = ApplyBrightnessContrastGamma(gNorm, currentBrightness, Contrast, Gamma);
                    bNorm = ApplyBrightnessContrastGamma(bNorm, currentBrightness, Contrast, Gamma);

                    // Convert back to 8-bit values
                    int newR = Math.Clamp((int)(rNorm * 255), 0, 255);
                    int newG = Math.Clamp((int)(gNorm * 255), 0, 255);
                    int newB = Math.Clamp((int)(bNorm * 255), 0, 255);

                    // Combine into new pixel
                    int newPixel = newR | (newG << 8) | (newB << 16);
                    output.SetPixel(x, y, newPixel);
                }
            }

            return output;
        }

        #endregion

        #region Helper Methods

        private float ApplyBrightnessContrastGamma(float value, float brightness, float contrast, float gamma)
        {
            // Apply brightness
            value = value * brightness;
            
            // Apply contrast
            value = (value - 0.5f) * contrast + 0.5f;
            
            // Apply gamma
            value = (float)Math.Pow(value, gamma);
            
            // Clamp to valid range
            return Math.Clamp(value, 0.0f, 1.0f);
        }

        #endregion

        #region Default Output

        protected override object GetDefaultOutput()
        {
            return new ImageBuffer(1, 1);
        }

        #endregion
    }
}
