using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Basic image copying and blending operations effect
    /// Based on r_blit.cpp from original AVS
    /// </summary>
    public class BlitEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Blit effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Blend mode for blitting operation
        /// 0 = Replace, 1 = Additive, 2 = Maximum, 3 = Minimum, 4 = Multiply, 5 = Average
        /// </summary>
        public int BlendMode { get; set; } = 0;

        /// <summary>
        /// X position offset for blit operation
        /// </summary>
        public int XPosition { get; set; } = 0;

        /// <summary>
        /// Y position offset for blit operation
        /// </summary>
        public int YPosition { get; set; } = 0;

        /// <summary>
        /// Source width for blit operation
        /// </summary>
        public int SourceWidth { get; set; } = 0;

        /// <summary>
        /// Source height for blit operation
        /// </summary>
        public int SourceHeight { get; set; } = 0;

        /// <summary>
        /// Destination width for blit operation
        /// </summary>
        public int DestWidth { get; set; } = 0;

        /// <summary>
        /// Destination height for blit operation
        /// </summary>
        public int DestHeight { get; set; } = 0;

        /// <summary>
        /// Rotation angle in degrees
        /// </summary>
        public float Rotation { get; set; } = 0.0f;

        /// <summary>
        /// Alpha transparency (0.0 = transparent, 1.0 = opaque)
        /// </summary>
        public float Alpha { get; set; } = 1.0f;

        /// <summary>
        /// Source buffer selection (0 = current, 1 = auxiliary)
        /// </summary>
        public int SourceBuffer { get; set; } = 0;

        /// <summary>
        /// Blit operation mode
        /// 0 = Normal, 1 = Scaled, 2 = Rotated, 3 = Scaled + Rotated
        /// </summary>
        public int OperationMode { get; set; } = 0;

        #endregion

        #region Constructor

        public BlitEffectsNode()
        {
            Name = "Blit Effects";
            Description = "Basic image copying and blending operations for image composition";
            Category = "Render Effects";
        }

        #endregion

        #region Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Source", typeof(ImageBuffer), true, null, "Source image to blit"));
            _inputPorts.Add(new EffectPort("Destination", typeof(ImageBuffer), true, null, "Destination image buffer"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Blitted output image"));
        }

        #endregion

        #region Effect Processing

        public override void ProcessFrame(Dictionary<string, object> inputData, Dictionary<string, object> outputData, AudioFeatures audioFeatures)
        {
            if (!Enabled) return;

            try
            {
                var sourceImage = GetInputValue<ImageBuffer>("Source", inputData);
                var destImage = GetInputValue<ImageBuffer>("Destination", inputData);

                if (sourceImage?.Data == null || destImage?.Data == null)
                {
                    return;
                }

                var outputImage = new ImageBuffer(destImage.Width, destImage.Height);
                
                // Copy destination as base
                Array.Copy(destImage.Data, outputImage.Data, destImage.Data.Length);

                // Perform blit operation
                PerformBlitOperation(sourceImage, outputImage);

                outputData["Output"] = outputImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Blit Effects] Error processing frame: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        private void PerformBlitOperation(ImageBuffer source, ImageBuffer destination)
        {
            int srcW = SourceWidth > 0 ? SourceWidth : source.Width;
            int srcH = SourceHeight > 0 ? SourceHeight : source.Height;
            int dstW = DestWidth > 0 ? DestWidth : srcW;
            int dstH = DestHeight > 0 ? DestHeight : srcH;

            switch (OperationMode)
            {
                case 0: // Normal blit
                    PerformNormalBlit(source, destination, srcW, srcH);
                    break;
                case 1: // Scaled blit
                    PerformScaledBlit(source, destination, srcW, srcH, dstW, dstH);
                    break;
                case 2: // Rotated blit
                    PerformRotatedBlit(source, destination, srcW, srcH);
                    break;
                case 3: // Scaled + Rotated blit
                    PerformScaledRotatedBlit(source, destination, srcW, srcH, dstW, dstH);
                    break;
            }
        }

        private void PerformNormalBlit(ImageBuffer source, ImageBuffer destination, int srcW, int srcH)
        {
            int dstX = XPosition;
            int dstY = YPosition;

            for (int y = 0; y < srcH && dstY + y < destination.Height; y++)
            {
                if (dstY + y < 0) continue;

                for (int x = 0; x < srcW && dstX + x < destination.Width; x++)
                {
                    if (dstX + x < 0) continue;

                    if (x < source.Width && y < source.Height)
                    {
                        uint srcPixel = source.Data[y * source.Width + x];
                        int dstIndex = (dstY + y) * destination.Width + (dstX + x);

                        destination.Data[dstIndex] = BlendPixels(destination.Data[dstIndex], srcPixel);
                    }
                }
            }
        }

        private void PerformScaledBlit(ImageBuffer source, ImageBuffer destination, int srcW, int srcH, int dstW, int dstH)
        {
            float xScale = (float)srcW / dstW;
            float yScale = (float)srcH / dstH;

            int dstX = XPosition;
            int dstY = YPosition;

            for (int y = 0; y < dstH && dstY + y < destination.Height; y++)
            {
                if (dstY + y < 0) continue;

                int srcY = (int)(y * yScale);
                if (srcY >= source.Height) continue;

                for (int x = 0; x < dstW && dstX + x < destination.Width; x++)
                {
                    if (dstX + x < 0) continue;

                    int srcX = (int)(x * xScale);
                    if (srcX >= source.Width) continue;

                    uint srcPixel = source.Data[srcY * source.Width + srcX];
                    int dstIndex = (dstY + y) * destination.Width + (dstX + x);

                    destination.Data[dstIndex] = BlendPixels(destination.Data[dstIndex], srcPixel);
                }
            }
        }

        private void PerformRotatedBlit(ImageBuffer source, ImageBuffer destination, int srcW, int srcH)
        {
            float radians = Rotation * (float)Math.PI / 180.0f;
            float cosTheta = (float)Math.Cos(radians);
            float sinTheta = (float)Math.Sin(radians);

            int centerX = srcW / 2;
            int centerY = srcH / 2;
            int dstCenterX = XPosition + centerX;
            int dstCenterY = YPosition + centerY;

            for (int y = 0; y < srcH; y++)
            {
                for (int x = 0; x < srcW; x++)
                {
                    // Rotate around center
                    int relX = x - centerX;
                    int relY = y - centerY;

                    int rotX = (int)(relX * cosTheta - relY * sinTheta);
                    int rotY = (int)(relX * sinTheta + relY * cosTheta);

                    int dstX = dstCenterX + rotX;
                    int dstY = dstCenterY + rotY;

                    if (dstX >= 0 && dstX < destination.Width && dstY >= 0 && dstY < destination.Height)
                    {
                        uint srcPixel = source.Data[y * source.Width + x];
                        int dstIndex = dstY * destination.Width + dstX;

                        destination.Data[dstIndex] = BlendPixels(destination.Data[dstIndex], srcPixel);
                    }
                }
            }
        }

        private void PerformScaledRotatedBlit(ImageBuffer source, ImageBuffer destination, int srcW, int srcH, int dstW, int dstH)
        {
            // Combine scaling and rotation
            float xScale = (float)srcW / dstW;
            float yScale = (float)srcH / dstH;
            float radians = Rotation * (float)Math.PI / 180.0f;
            float cosTheta = (float)Math.Cos(radians);
            float sinTheta = (float)Math.Sin(radians);

            int centerX = dstW / 2;
            int centerY = dstH / 2;
            int dstCenterX = XPosition + centerX;
            int dstCenterY = YPosition + centerY;

            for (int y = 0; y < dstH; y++)
            {
                for (int x = 0; x < dstW; x++)
                {
                    // Scale then rotate
                    int srcX = (int)(x * xScale);
                    int srcY = (int)(y * yScale);

                    if (srcX >= source.Width || srcY >= source.Height) continue;

                    // Rotate around center
                    int relX = x - centerX;
                    int relY = y - centerY;

                    int rotX = (int)(relX * cosTheta - relY * sinTheta);
                    int rotY = (int)(relX * sinTheta + relY * cosTheta);

                    int dstX = dstCenterX + rotX;
                    int dstY = dstCenterY + rotY;

                    if (dstX >= 0 && dstX < destination.Width && dstY >= 0 && dstY < destination.Height)
                    {
                        uint srcPixel = source.Data[srcY * source.Width + srcX];
                        int dstIndex = dstY * destination.Width + dstX;

                        destination.Data[dstIndex] = BlendPixels(destination.Data[dstIndex], srcPixel);
                    }
                }
            }
        }

        private uint BlendPixels(uint dest, uint src)
        {
            if (Alpha <= 0.0f) return dest;
            if (Alpha >= 1.0f && BlendMode == 0) return src;

            // Extract color components
            uint srcA = (src >> 24) & 0xFF;
            uint srcR = (src >> 16) & 0xFF;
            uint srcG = (src >> 8) & 0xFF;
            uint srcB = src & 0xFF;

            uint destA = (dest >> 24) & 0xFF;
            uint destR = (dest >> 16) & 0xFF;
            uint destG = (dest >> 8) & 0xFF;
            uint destB = dest & 0xFF;

            // Apply alpha
            float alpha = Alpha * (srcA / 255.0f);

            uint resultR, resultG, resultB;

            switch (BlendMode)
            {
                case 0: // Replace
                    resultR = (uint)(destR * (1 - alpha) + srcR * alpha);
                    resultG = (uint)(destG * (1 - alpha) + srcG * alpha);
                    resultB = (uint)(destB * (1 - alpha) + srcB * alpha);
                    break;
                case 1: // Additive
                    resultR = Math.Min(255u, (uint)(destR + srcR * alpha));
                    resultG = Math.Min(255u, (uint)(destG + srcG * alpha));
                    resultB = Math.Min(255u, (uint)(destB + srcB * alpha));
                    break;
                case 2: // Maximum
                    resultR = Math.Max(destR, (uint)(srcR * alpha));
                    resultG = Math.Max(destG, (uint)(srcG * alpha));
                    resultB = Math.Max(destB, (uint)(srcB * alpha));
                    break;
                case 3: // Minimum
                    resultR = Math.Min(destR, (uint)(destR * (1 - alpha) + srcR * alpha));
                    resultG = Math.Min(destG, (uint)(destG * (1 - alpha) + srcG * alpha));
                    resultB = Math.Min(destB, (uint)(destB * (1 - alpha) + srcB * alpha));
                    break;
                case 4: // Multiply
                    resultR = (uint)((destR * srcR * alpha) / 255);
                    resultG = (uint)((destG * srcG * alpha) / 255);
                    resultB = (uint)((destB * srcB * alpha) / 255);
                    break;
                case 5: // Average
                    resultR = (uint)((destR + srcR * alpha) / 2);
                    resultG = (uint)((destG + srcG * alpha) / 2);
                    resultB = (uint)((destB + srcB * alpha) / 2);
                    break;
                default:
                    resultR = destR;
                    resultG = destG;
                    resultB = destB;
                    break;
            }

            return (Math.Max(destA, srcA) << 24) | (resultR << 16) | (resultG << 8) | resultB;
        }

        #endregion
    }
}