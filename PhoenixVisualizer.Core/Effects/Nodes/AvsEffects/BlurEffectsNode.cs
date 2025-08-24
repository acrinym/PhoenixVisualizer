using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Blur Effects Node - Creates sophisticated image blurring with multiple intensity levels
    /// </summary>
    public class BlurEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Enable/disable the blur effect
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Blur intensity (0=off, 1=light, 2=medium, 3=heavy)
        /// </summary>
        public int BlurIntensity { get; set; } = 1;

        /// <summary>
        /// Rounding mode (0=round down, 1=round up)
        /// </summary>
        public int RoundMode { get; set; } = 0;

        /// <summary>
        /// Enable multi-threaded rendering
        /// </summary>
        public bool MultiThreaded { get; set; } = true;

        #endregion

        #region Constants

        // Blur intensity constants
        private const int BLUR_OFF = 0;
        private const int BLUR_LIGHT = 1;
        private const int BLUR_MEDIUM = 2;
        private const int BLUR_HEAVY = 3;

        // Rounding mode constants
        private const int ROUND_DOWN = 0;
        private const int ROUND_UP = 1;

        #endregion

        #region Constructor

        public BlurEffectsNode()
        {
            Name = "Blur Effects";
            Description = "Creates sophisticated image blurring with multiple intensity levels";
            Category = "AVS Effects";
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Input image to blur"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Blurred output image"));
        }

        #endregion

        #region Process Method

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!Enabled || BlurIntensity == BLUR_OFF)
            {
                return GetDefaultOutput();
            }

            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
            {
                return GetDefaultOutput();
            }

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);

            switch (BlurIntensity)
            {
                case BLUR_LIGHT:
                    ApplyLightBlur(imageBuffer, output);
                    break;
                case BLUR_MEDIUM:
                    ApplyMediumBlur(imageBuffer, output);
                    break;
                case BLUR_HEAVY:
                    ApplyHeavyBlur(imageBuffer, output);
                    break;
            }

            return output;
        }

        #endregion

        #region Blur Algorithms

        private void ApplyLightBlur(ImageBuffer input, ImageBuffer output)
        {
            int width = input.Width;
            int height = input.Height;

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int pixelIndex = y * width + x;
                    int currentPixel = input.Pixels[pixelIndex];
                    
                    // Sample neighboring pixels
                    int leftPixel = input.Pixels[pixelIndex - 1];
                    int rightPixel = input.Pixels[pixelIndex + 1];
                    int topPixel = input.Pixels[pixelIndex - width];
                    int bottomPixel = input.Pixels[pixelIndex + width];

                    // Apply light blur: current pixel gets more weight
                    int blurredPixel = (currentPixel >> 1) + (currentPixel >> 2) + 
                                     (leftPixel >> 3) + (rightPixel >> 3) + 
                                     (topPixel >> 3) + (bottomPixel >> 3);

                    output.Pixels[pixelIndex] = blurredPixel;
                }
            }
        }

        private void ApplyMediumBlur(ImageBuffer input, ImageBuffer output)
        {
            int width = input.Width;
            int height = input.Height;

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int pixelIndex = y * width + x;
                    
                    // Sample neighboring pixels
                    int leftPixel = input.Pixels[pixelIndex - 1];
                    int rightPixel = input.Pixels[pixelIndex + 1];
                    int bottomPixel = input.Pixels[pixelIndex + width];

                    // Apply medium blur: equal weight distribution
                    int blurredPixel = (leftPixel >> 2) + (rightPixel >> 2) + (bottomPixel >> 1);

                    output.Pixels[pixelIndex] = blurredPixel;
                }
            }
        }

        private void ApplyHeavyBlur(ImageBuffer input, ImageBuffer output)
        {
            int width = input.Width;
            int height = input.Height;

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int pixelIndex = y * width + x;
                    int currentPixel = input.Pixels[pixelIndex];
                    
                    // Sample neighboring pixels
                    int leftPixel = input.Pixels[pixelIndex - 1];
                    int rightPixel = input.Pixels[pixelIndex + 1];
                    int bottomPixel = input.Pixels[pixelIndex + width];

                    // Apply heavy blur: equal weight for all pixels
                    int blurredPixel = (currentPixel >> 2) + (leftPixel >> 2) + 
                                     (rightPixel >> 2) + (bottomPixel >> 2);

                    output.Pixels[pixelIndex] = blurredPixel;
                }
            }
        }

        #endregion

        #region Default Output

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(1, 1);
        }

        #endregion
    }
}
