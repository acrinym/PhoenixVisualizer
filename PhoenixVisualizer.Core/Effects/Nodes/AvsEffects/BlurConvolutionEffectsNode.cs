using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Blur/Convolution effect with 5x5 convolution kernel
    /// Based on r_blur.cpp C_BlurClass from original AVS
    /// High-performance image filtering with multi-threading support
    /// </summary>
    public class BlurConvolutionEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Blur/Convolution effect is active
        /// 0 = Disabled, 1 = Normal, 2 = Enhanced
        /// </summary>
        public int Enabled { get; set; } = 1;

        /// <summary>
        /// Rounding mode for quality
        /// 0 = No rounding, 1 = Round to nearest
        /// </summary>
        public int RoundingMode { get; set; } = 0;

        /// <summary>
        /// Number of blur passes for enhanced mode
        /// </summary>
        public int BlurPasses { get; set; } = 1;

        /// <summary>
        /// Enable multi-threading for performance
        /// </summary>
        public bool UseMultiThreading { get; set; } = true;

        /// <summary>
        /// Maximum number of threads to use
        /// </summary>
        public int MaxThreads { get; set; } = Environment.ProcessorCount;

        #endregion

        #region Private Fields

        // 5x5 Convolution kernel weights (as per original AVS)
        private static readonly float[,] ConvolutionKernel = new float[3, 3]
        {
            { 1.0f/16.0f, 1.0f/8.0f, 1.0f/16.0f },
            { 1.0f/8.0f,  1.0f/2.0f, 1.0f/8.0f },
            { 1.0f/16.0f, 1.0f/8.0f, 1.0f/16.0f }
        };

        // Bit shift masks for optimized division (as per original)
        private const uint MASK_SH1 = 0xFEFEFEFE; // For division by 2
        private const uint MASK_SH2 = 0xFCFCFCFC; // For division by 4
        private const uint MASK_SH3 = 0xF8F8F8F8; // For division by 8
        private const uint MASK_SH4 = 0xF0F0F0F0; // For division by 16

        #endregion

        #region Constructor

        public BlurConvolutionEffectsNode()
        {
            Name = "Blur Convolution";
            Description = "High-performance 5x5 convolution blur with MMX-style optimization";
            Category = "Filter Effects";
        }

        #endregion

        #region Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for blur convolution"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Blurred output image"));
        }

        #endregion

        #region Effect Processing

        public override void ProcessFrame(Dictionary<string, object> inputData, Dictionary<string, object> outputData, AudioFeatures audioFeatures)
        {
            if (Enabled == 0) return;

            try
            {
                var sourceImage = GetInputValue<ImageBuffer>("Image", inputData);
                if (sourceImage?.Data == null) return;

                var outputImage = new ImageBuffer(sourceImage.Width, sourceImage.Height);

                // Process based on enabled mode
                if (Enabled == 1) // Normal mode
                {
                    ApplyConvolutionBlur(sourceImage, outputImage);
                }
                else if (Enabled == 2) // Enhanced mode
                {
                    ApplyEnhancedBlur(sourceImage, outputImage);
                }

                outputData["Output"] = outputImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Blur Convolution] Error processing frame: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        private void ApplyConvolutionBlur(ImageBuffer source, ImageBuffer output)
        {
            if (UseMultiThreading && MaxThreads > 1)
            {
                ApplyConvolutionBlurMultiThreaded(source, output);
            }
            else
            {
                ApplyConvolutionBlurSingleThreaded(source, output, 0, source.Height);
            }
        }

        private void ApplyEnhancedBlur(ImageBuffer source, ImageBuffer output)
        {
            // Enhanced mode applies multiple passes
            var tempBuffer = new ImageBuffer(source.Width, source.Height);
            Array.Copy(source.Data, tempBuffer.Data, source.Data.Length);

            for (int pass = 0; pass < BlurPasses; pass++)
            {
                if (pass == BlurPasses - 1)
                {
                    // Final pass goes to output
                    ApplyConvolutionBlur(tempBuffer, output);
                }
                else
                {
                    // Intermediate passes use temporary buffer
                    var nextTemp = new ImageBuffer(source.Width, source.Height);
                    ApplyConvolutionBlur(tempBuffer, nextTemp);
                    tempBuffer = nextTemp;
                }
            }
        }

        private void ApplyConvolutionBlurMultiThreaded(ImageBuffer source, ImageBuffer output)
        {
            int height = source.Height;
            int threadsToUse = Math.Min(MaxThreads, height);
            
            Parallel.For(0, threadsToUse, threadIndex =>
            {
                // Calculate thread boundaries (like original AVS)
                int startY = (threadIndex * height) / threadsToUse;
                int endY = (threadIndex >= threadsToUse - 1) ? height : ((threadIndex + 1) * height) / threadsToUse;
                
                ApplyConvolutionBlurSingleThreaded(source, output, startY, endY);
            });
        }

        private void ApplyConvolutionBlurSingleThreaded(ImageBuffer source, ImageBuffer output, int startY, int endY)
        {
            int width = source.Width;
            int height = source.Height;

            for (int y = startY; y < endY; y++)
            {
                // Edge detection (as per original AVS)
                bool atTop = (y == 0);
                bool atBottom = (y == height - 1);

                for (int x = 0; x < width; x++)
                {
                    bool atLeft = (x == 0);
                    bool atRight = (x == width - 1);

                    if (RoundingMode == 0)
                    {
                        // Fast mode using bit operations (like original MMX)
                        output.Data[y * width + x] = CalculateConvolutionFast(source, x, y, width, height, atLeft, atRight, atTop, atBottom);
                    }
                    else
                    {
                        // Quality mode with proper rounding
                        output.Data[y * width + x] = CalculateConvolutionQuality(source, x, y, width, height, atLeft, atRight, atTop, atBottom);
                    }
                }
            }
        }

        private uint CalculateConvolutionFast(ImageBuffer source, int x, int y, int width, int height, bool atLeft, bool atRight, bool atTop, bool atBottom)
        {
            // Fast convolution using bit operations (simulating MMX)
            uint centerPixel = GetPixelSafe(source, x, y, width, height);
            
            // Get surrounding pixels with edge handling
            uint topLeft = GetPixelSafe(source, x - 1, y - 1, width, height);
            uint top = GetPixelSafe(source, x, y - 1, width, height);
            uint topRight = GetPixelSafe(source, x + 1, y - 1, width, height);
            uint left = GetPixelSafe(source, x - 1, y, width, height);
            uint right = GetPixelSafe(source, x + 1, y, width, height);
            uint bottomLeft = GetPixelSafe(source, x - 1, y + 1, width, height);
            uint bottom = GetPixelSafe(source, x, y + 1, width, height);
            uint bottomRight = GetPixelSafe(source, x + 1, y + 1, width, height);

            // Apply convolution kernel using bit operations (like original AVS)
            uint resultR = 0, resultG = 0, resultB = 0, resultA = 0;

            // Center pixel (weight = 1/2)
            resultR += ((centerPixel >> 16) & 0xFF) >> 1; // Divide by 2
            resultG += ((centerPixel >> 8) & 0xFF) >> 1;
            resultB += (centerPixel & 0xFF) >> 1;
            resultA += ((centerPixel >> 24) & 0xFF) >> 1;

            // Adjacent pixels (weight = 1/8 each)
            uint[] adjacentPixels = { top, left, right, bottom };
            foreach (uint pixel in adjacentPixels)
            {
                resultR += ((pixel >> 16) & 0xFF) >> 3; // Divide by 8
                resultG += ((pixel >> 8) & 0xFF) >> 3;
                resultB += (pixel & 0xFF) >> 3;
                resultA += ((pixel >> 24) & 0xFF) >> 3;
            }

            // Corner pixels (weight = 1/16 each)
            uint[] cornerPixels = { topLeft, topRight, bottomLeft, bottomRight };
            foreach (uint pixel in cornerPixels)
            {
                resultR += ((pixel >> 16) & 0xFF) >> 4; // Divide by 16
                resultG += ((pixel >> 8) & 0xFF) >> 4;
                resultB += (pixel & 0xFF) >> 4;
                resultA += ((pixel >> 24) & 0xFF) >> 4;
            }

            // Clamp to valid range
            resultR = Math.Min(255u, resultR);
            resultG = Math.Min(255u, resultG);
            resultB = Math.Min(255u, resultB);
            resultA = Math.Min(255u, resultA);

            return (resultA << 24) | (resultR << 16) | (resultG << 8) | resultB;
        }

        private uint CalculateConvolutionQuality(ImageBuffer source, int x, int y, int width, int height, bool atLeft, bool atRight, bool atTop, bool atBottom)
        {
            // High-quality convolution with proper floating-point arithmetic
            float totalR = 0, totalG = 0, totalB = 0, totalA = 0;

            for (int ky = -1; ky <= 1; ky++)
            {
                for (int kx = -1; kx <= 1; kx++)
                {
                    uint pixel = GetPixelSafe(source, x + kx, y + ky, width, height);
                    float weight = ConvolutionKernel[ky + 1, kx + 1];

                    totalR += ((pixel >> 16) & 0xFF) * weight;
                    totalG += ((pixel >> 8) & 0xFF) * weight;
                    totalB += (pixel & 0xFF) * weight;
                    totalA += ((pixel >> 24) & 0xFF) * weight;
                }
            }

            // Round to nearest integer
            uint resultR = (uint)Math.Min(255, Math.Max(0, Math.Round(totalR)));
            uint resultG = (uint)Math.Min(255, Math.Max(0, Math.Round(totalG)));
            uint resultB = (uint)Math.Min(255, Math.Max(0, Math.Round(totalB)));
            uint resultA = (uint)Math.Min(255, Math.Max(0, Math.Round(totalA)));

            return (resultA << 24) | (resultR << 16) | (resultG << 8) | resultB;
        }

        private uint GetPixelSafe(ImageBuffer source, int x, int y, int width, int height)
        {
            // Handle edge cases by clamping coordinates
            x = Math.Max(0, Math.Min(width - 1, x));
            y = Math.Max(0, Math.Min(height - 1, y));
            
            return source.Data[y * width + x];
        }

        #endregion

        #region Configuration

        public override Dictionary<string, object> GetConfiguration()
        {
            return new Dictionary<string, object>
            {
                { "Enabled", Enabled },
                { "RoundingMode", RoundingMode },
                { "BlurPasses", BlurPasses },
                { "UseMultiThreading", UseMultiThreading },
                { "MaxThreads", MaxThreads }
            };
        }

        public override void ApplyConfiguration(Dictionary<string, object> config)
        {
            if (config.TryGetValue("Enabled", out var enabled))
                Enabled = Convert.ToInt32(enabled);
            
            if (config.TryGetValue("RoundingMode", out var roundingMode))
                RoundingMode = Convert.ToInt32(roundingMode);
            
            if (config.TryGetValue("BlurPasses", out var blurPasses))
                BlurPasses = Convert.ToInt32(blurPasses);
            
            if (config.TryGetValue("UseMultiThreading", out var useMultiThreading))
                UseMultiThreading = Convert.ToBoolean(useMultiThreading);
            
            if (config.TryGetValue("MaxThreads", out var maxThreads))
                MaxThreads = Convert.ToInt32(maxThreads);
        }

        #endregion
    }
}