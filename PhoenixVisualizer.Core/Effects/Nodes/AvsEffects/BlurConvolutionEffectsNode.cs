using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Blur Convolution Effects Node - Advanced blur using 5x5 convolution kernels
    /// Based on Winamp AVS C_BlurClass with MMX optimization concepts
    /// </summary>
    public class BlurConvolutionEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Blur Convolution effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Blur strength/intensity (0-255)
        /// </summary>
        public int BlurStrength { get; set; } = 128;

        /// <summary>
        /// Blur mode: 0=Box, 1=Gaussian, 2=Motion, 3=Radial
        /// </summary>
        public int BlurMode { get; set; } = 1;

        /// <summary>
        /// Motion blur angle (0-360 degrees) - only used in Motion mode
        /// </summary>
        public float MotionAngle { get; set; } = 0.0f;

        /// <summary>
        /// Motion blur length (1-50) - only used in Motion mode
        /// </summary>
        public int MotionLength { get; set; } = 10;

        /// <summary>
        /// Enable edge preservation (reduces blur at edges)
        /// </summary>
        public bool EdgePreservation { get; set; } = false;

        /// <summary>
        /// Edge threshold for preservation (0.0-1.0)
        /// </summary>
        public float EdgeThreshold { get; set; } = 0.1f;

        /// <summary>
        /// Audio reactivity factor (0.0-1.0)
        /// </summary>
        public float AudioReactivity { get; set; } = 0.0f;

        #endregion

        #region Private Fields

        private float[] _gaussianKernel5x5;
        private float[] _boxKernel5x5;

        #endregion

        #region Constructor

        public BlurConvolutionEffectsNode()
        {
            Name = "Blur Convolution Effects";
            Description = "Advanced 5x5 convolution blur with multiple modes and edge preservation";
            Category = "Filter Effects";

            // Initialize convolution kernels with empty arrays first
            _gaussianKernel5x5 = Array.Empty<float>();
            _boxKernel5x5 = Array.Empty<float>();

            // Initialize convolution kernels
            InitializeKernels();
        }

        #endregion

        #region Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for convolution blur"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Blurred output image"));
        }

        private void InitializeKernels()
        {
            // 5x5 Gaussian kernel (normalized)
            _gaussianKernel5x5 = new float[]
            {
                1/273f,  4/273f,  7/273f,  4/273f, 1/273f,
                4/273f, 16/273f, 26/273f, 16/273f, 4/273f,
                7/273f, 26/273f, 41/273f, 26/273f, 7/273f,
                4/273f, 16/273f, 26/273f, 16/273f, 4/273f,
                1/273f,  4/273f,  7/273f,  4/273f, 1/273f
            };

            // 5x5 Box kernel (simple averaging)
            _boxKernel5x5 = new float[]
            {
                1/25f, 1/25f, 1/25f, 1/25f, 1/25f,
                1/25f, 1/25f, 1/25f, 1/25f, 1/25f,
                1/25f, 1/25f, 1/25f, 1/25f, 1/25f,
                1/25f, 1/25f, 1/25f, 1/25f, 1/25f,
                1/25f, 1/25f, 1/25f, 1/25f, 1/25f
            };
        }

        #endregion

        #region Processing

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

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);

            // Apply audio reactivity if enabled
            float reactivityFactor = 1.0f;
            if (AudioReactivity > 0.0f)
            {
                reactivityFactor = 1.0f + (audioFeatures.Bass * AudioReactivity);
            }

            // Select kernel based on mode
            float[] kernel;
            switch (BlurMode)
            {
                case 0: // Box blur
                    kernel = _boxKernel5x5;
                    break;
                case 1: // Gaussian blur
                    kernel = _gaussianKernel5x5;
                    break;
                case 2: // Motion blur
                    kernel = GenerateMotionKernel();
                    break;
                case 3: // Radial blur (simplified as Gaussian)
                    kernel = _gaussianKernel5x5;
                    break;
                default:
                    kernel = _gaussianKernel5x5;
                    break;
            }

            // Apply convolution
            ApplyConvolution(imageBuffer, output, kernel, reactivityFactor, audioFeatures);

            return output;
        }

        #endregion

        #region Convolution Methods

        private void ApplyConvolution(ImageBuffer input, ImageBuffer output, float[] kernel,
                                     float reactivityFactor, AudioFeatures audioFeatures)
        {
            int width = input.Width;
            int height = input.Height;
            int kernelSize = 5;
            int kernelRadius = kernelSize / 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Skip edge preservation check for now (can be added later)
                    uint resultColor = ConvolvePixel(input, x, y, kernel, kernelSize,
                                                   kernelRadius, reactivityFactor, audioFeatures);
                    output.SetPixel(x, y, (int)resultColor);
                }
            }
        }

        private uint ConvolvePixel(ImageBuffer input, int x, int y, float[] kernel, int kernelSize,
                                  int kernelRadius, float reactivityFactor, AudioFeatures audioFeatures)
        {
            float r = 0, g = 0, b = 0, a = 0;
            float kernelSum = 0;

            for (int ky = 0; ky < kernelSize; ky++)
            {
                for (int kx = 0; kx < kernelSize; kx++)
                {
                    int px = x + kx - kernelRadius;
                    int py = y + ky - kernelRadius;

                    // Clamp to image boundaries
                    px = Math.Max(0, Math.Min(input.Width - 1, px));
                    py = Math.Max(0, Math.Min(input.Height - 1, py));

                    uint pixelColor = (uint)input.GetPixel(px, py);
                    float kernelValue = kernel[ky * kernelSize + kx];

                    // Apply reactivity factor to kernel strength
                    kernelValue *= reactivityFactor;

                    r += ((pixelColor >> 16) & 0xFF) * kernelValue;
                    g += ((pixelColor >> 8) & 0xFF) * kernelValue;
                    b += (pixelColor & 0xFF) * kernelValue;
                    a += ((pixelColor >> 24) & 0xFF) * kernelValue;

                    kernelSum += kernelValue;
                }
            }

            // Normalize result
            if (kernelSum > 0)
            {
                r /= kernelSum;
                g /= kernelSum;
                b /= kernelSum;
                a /= kernelSum;
            }

            // Clamp values
            r = Math.Max(0, Math.Min(255, r));
            g = Math.Max(0, Math.Min(255, g));
            b = Math.Max(0, Math.Min(255, b));
            a = Math.Max(0, Math.Min(255, a));

            return ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | (uint)b;
        }

        private float[] GenerateMotionKernel()
        {
            const int kernelSize = 5;
            var kernel = new float[kernelSize * kernelSize];

            // Convert angle to radians
            float angleRad = MotionAngle * (float)Math.PI / 180.0f;

            // Calculate motion vector
            float dx = (float)Math.Cos(angleRad);
            float dy = (float)Math.Sin(angleRad);

            // Generate motion blur kernel
            float total = 0;
            for (int i = 0; i < MotionLength; i++)
            {
                float t = i / (float)(MotionLength - 1);
                int x = (int)(dx * t * 2) + kernelSize / 2;
                int y = (int)(dy * t * 2) + kernelSize / 2;

                if (x >= 0 && x < kernelSize && y >= 0 && y < kernelSize)
                {
                    kernel[y * kernelSize + x] += 1.0f;
                    total += 1.0f;
                }
            }

            // Normalize kernel
            if (total > 0)
            {
                for (int i = 0; i < kernel.Length; i++)
                {
                    kernel[i] /= total;
                }
            }

            return kernel;
        }

        #endregion
    }
}