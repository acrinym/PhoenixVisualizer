using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class MosaicEffectsNode : BaseEffectNode
    {
        // Core properties
        public bool Enabled { get; set; } = true;
        public int Quality { get; set; } = 50; // 1 to 100
        public int BeatQuality { get; set; } = 25; // 1 to 100
        public int BlendMode { get; set; } = 0; // 0=Replace, 1=Additive, 2=50/50
        public bool BeatReactive { get; set; } = false;
        public int BeatDuration { get; set; } = 15; // 1 to 100 frames
        public bool EnableQualityAnimation { get; set; } = false;
        public float QualityAnimationSpeed { get; set; } = 1.0f;
        public int QualityAnimationMode { get; set; } = 0;
        public bool EnableMosaicMasking { get; set; } = false;
        public ImageBuffer MosaicMask { get; set; } = null;
        public float MaskInfluence { get; set; } = 1.0f;
        public bool EnableMosaicBlending { get; set; } = false;
        public float MosaicBlendStrength { get; set; } = 0.5f;
        public int MosaicAlgorithm { get; set; } = 0; // 0=Standard, 1=Enhanced, 2=Realistic
        public float MosaicCurve { get; set; } = 1.0f; // Power curve for mosaic effects
        public bool EnableMosaicClamping { get; set; } = true;
        public int ClampMode { get; set; } = 0; // 0=Standard, 1=Soft, 2=Hard
        public bool EnableMosaicInversion { get; set; } = false;
        public float InversionThreshold { get; set; } = 0.5f;

        // Internal state for mosaic processing
        private int CurrentQuality { get; set; } = 50;
        private int FrameCounter { get; set; } = 0;
        private readonly Random _random = new Random();
        private float _currentTime = 0.0f;

        public MosaicEffectsNode()
        {
            Name = "Mosaic Effects";
            Description = "Creates mosaic/pixelated effects with beat synchronization";
            Category = "Transform Effects";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for mosaic processing"));
            _inputPorts.Add(new EffectPort("Mask", typeof(ImageBuffer), false, null, "Optional mosaic mask image"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Mosaic output image"));
        }

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
                return GetDefaultOutput();

            if (!Enabled)
                return imageBuffer;

            // Check for mask input
            if (inputs.TryGetValue("Mask", out var maskInput) && maskInput is ImageBuffer maskBuffer)
            {
                MosaicMask = maskBuffer;
            }

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);

            // Update quality based on beat reactivity
            UpdateQuality(audioFeatures);

            // Update quality animation if enabled
            if (EnableQualityAnimation)
            {
                UpdateQualityAnimation();
            }

            // Apply mosaic effect if quality is less than 100
            if (CurrentQuality < 100)
            {
                ApplyMosaicEffect(imageBuffer, output);
            }
            else
            {
                // No mosaic, copy input to output
                CopyImage(imageBuffer, output);
            }

            return output;
        }

        private void UpdateQuality(AudioFeatures audio)
        {
            if (BeatReactive && audio != null && audio.IsBeat)
            {
                // Beat detected, switch to beat quality
                CurrentQuality = BeatQuality;
                FrameCounter = BeatDuration;
            }
            else if (FrameCounter > 0)
            {
                // Beat effect active, gradually return to normal quality
                FrameCounter--;
                if (FrameCounter > 0)
                {
                    var qualityDiff = Math.Abs(Quality - BeatQuality);
                    var step = qualityDiff / BeatDuration;
                    CurrentQuality += step * (BeatQuality > Quality ? -1 : 1);
                }
                else
                {
                    CurrentQuality = Quality;
                }
            }
            else
            {
                // Normal operation
                CurrentQuality = Quality;
            }

            // Apply clamping if enabled
            if (EnableMosaicClamping)
            {
                CurrentQuality = ApplyClamping(CurrentQuality);
            }
        }

        private int ApplyClamping(int quality)
        {
            switch (ClampMode)
            {
                case 0: // Standard
                    return Math.Clamp(quality, 1, 100);
                case 1: // Soft
                    return Math.Clamp(quality, 5, 95);
                case 2: // Hard
                    return Math.Clamp(quality, 10, 90);
                default:
                    return Math.Clamp(quality, 1, 100);
            }
        }

        private void UpdateQualityAnimation()
        {
            if (!EnableQualityAnimation)
                return;

            _currentTime += 0.016f; // Assuming 60 FPS
            var animationProgress = (_currentTime * QualityAnimationSpeed) % (Math.PI * 2);

            switch (QualityAnimationMode)
            {
                case 0: // Pulsing quality
                    var pulse = (float)((Math.Sin(animationProgress) + 1.0) * 0.5);
                    CurrentQuality = (int)(20 + pulse * 60); // 20-80 quality range
                    break;

                case 1: // Oscillating quality
                    var oscillation = (float)Math.Sin(animationProgress * 2);
                    CurrentQuality = (int)(30 + oscillation * 40); // 30-70 quality range
                    break;

                case 2: // Random quality
                    if (_random.NextDouble() < 0.01f) // 1% chance per frame
                    {
                        CurrentQuality = _random.Next(10, 91); // 10-90 quality range
                    }
                    break;

                case 3: // Wave pattern quality
                    var wave = (float)Math.Sin(animationProgress * 3);
                    CurrentQuality = (int)(25 + wave * 50); // 25-75 quality range
                    break;
            }
        }

        private void ApplyMosaicEffect(ImageBuffer input, ImageBuffer output)
        {
            switch (MosaicAlgorithm)
            {
                case 1: // Enhanced
                    ApplyEnhancedMosaic(input, output);
                    break;
                case 2: // Realistic
                    ApplyRealisticMosaic(input, output);
                    break;
                default: // Standard
                    ApplyStandardMosaic(input, output);
                    break;
            }
        }

        private void ApplyStandardMosaic(ImageBuffer input, ImageBuffer output)
        {
            var width = input.Width;
            var height = input.Height;

            // Calculate sampling intervals (16-bit fixed point)
            var sampleXInc = (width * 65536) / CurrentQuality;
            var sampleYInc = (height * 65536) / CurrentQuality;

            var yPos = (sampleYInc >> 17);
            var dyPos = 0;

            for (int y = 0; y < height; y++)
            {
                var dPos = 0;
                var xPos = (sampleXInc >> 17);
                var sourcePixel = input.GetPixel(xPos, yPos);

                for (int x = 0; x < width; x++)
                {
                    // Apply selected blending mode
                    var outputPixel = ApplyBlendingMode(output.GetPixel(x, y), sourcePixel);
                    output.SetPixel(x, y, outputPixel);

                    // Update sampling position
                    dPos += 1 << 16;
                    if (dPos >= sampleXInc)
                    {
                        xPos += dPos >> 16;
                        if (xPos >= width) break;
                        sourcePixel = input.GetPixel(xPos, yPos);
                        dPos -= sampleXInc;
                    }
                }

                // Update vertical sampling position
                dyPos += 1 << 16;
                if (dyPos >= sampleYInc)
                {
                    yPos += (dyPos >> 16);
                    dyPos -= sampleYInc;
                    if (yPos >= height) break;
                }
            }
        }

        private void ApplyEnhancedMosaic(ImageBuffer input, ImageBuffer output)
        {
            // Multi-pass mosaic with different quality levels
            var passCount = 3;
            var passQualities = new int[] { CurrentQuality, CurrentQuality * 2, CurrentQuality * 3 };

            for (int pass = 0; pass < passCount; pass++)
            {
                var passQuality = Math.Min(100, passQualities[pass]);
                ApplyMosaicPass(input, output, passQuality, pass);
            }
        }

        private void ApplyMosaicPass(ImageBuffer input, ImageBuffer output, int quality, int pass)
        {
            // Apply mosaic with specific quality level
            var tempOutput = new ImageBuffer(output.Width, output.Height);
            ApplyStandardMosaic(input, tempOutput);

            // Blend with previous passes
            var blendFactor = 1.0f / (pass + 1);
            BlendImages(output, tempOutput, blendFactor);
        }

        private void ApplyRealisticMosaic(ImageBuffer input, ImageBuffer output)
        {
            // Realistic mosaic with edge preservation
            var width = input.Width;
            var height = input.Height;

            // Calculate sampling intervals with edge preservation
            var sampleXInc = (width * 65536) / CurrentQuality;
            var sampleYInc = (height * 65536) / CurrentQuality;

            var yPos = (sampleYInc >> 17);
            var dyPos = 0;

            for (int y = 0; y < height; y++)
            {
                var dPos = 0;
                var xPos = (sampleXInc >> 17);
                var sourcePixel = input.GetPixel(xPos, yPos);

                for (int x = 0; x < width; x++)
                {
                    // Apply edge-preserving mosaic
                    var outputPixel = ApplyEdgePreservingMosaic(input, x, y, sourcePixel);
                    output.SetPixel(x, y, outputPixel);

                    // Update sampling position
                    dPos += 1 << 16;
                    if (dPos >= sampleXInc)
                    {
                        xPos += dPos >> 16;
                        if (xPos >= width) break;
                        sourcePixel = input.GetPixel(xPos, yPos);
                        dPos -= sampleXInc;
                    }
                }

                // Update vertical sampling position
                dyPos += 1 << 16;
                if (dyPos >= sampleYInc)
                {
                    yPos += (dyPos >> 16);
                    dyPos -= sampleYInc;
                    if (yPos >= height) break;
                }
            }
        }

        private int ApplyEdgePreservingMosaic(ImageBuffer input, int x, int y, int basePixel)
        {
            // Simple edge detection and preservation
            if (x > 0 && x < input.Width - 1 && y > 0 && y < input.Height - 1)
            {
                var leftPixel = input.GetPixel(x - 1, y);
                var rightPixel = input.GetPixel(x + 1, y);
                var topPixel = input.GetPixel(x, y - 1);
                var bottomPixel = input.GetPixel(x, y + 1);

                // Check for significant color differences (edges)
                var edgeThreshold = 30;
                var hasEdge = Math.Abs((basePixel & 0xFF) - (leftPixel & 0xFF)) > edgeThreshold ||
                             Math.Abs((basePixel & 0xFF) - (rightPixel & 0xFF)) > edgeThreshold ||
                             Math.Abs((basePixel & 0xFF) - (topPixel & 0xFF)) > edgeThreshold ||
                             Math.Abs((basePixel & 0xFF) - (bottomPixel & 0xFF)) > edgeThreshold;

                if (hasEdge)
                {
                    // Preserve edge by using original pixel
                    return input.GetPixel(x, y);
                }
            }

            return basePixel;
        }

        private int ApplyBlendingMode(int currentPixel, int sourcePixel)
        {
            switch (BlendMode)
            {
                case 0: // Replace
                    return sourcePixel;

                case 1: // Additive
                    return BlendPixelsAdditive(currentPixel, sourcePixel);

                case 2: // 50/50
                    return BlendPixels50_50(currentPixel, sourcePixel);

                default:
                    return sourcePixel;
            }
        }

        private int BlendPixelsAdditive(int pixel1, int pixel2)
        {
            var r1 = pixel1 & 0xFF;
            var g1 = (pixel1 >> 8) & 0xFF;
            var b1 = (pixel1 >> 16) & 0xFF;

            var r2 = pixel2 & 0xFF;
            var g2 = (pixel2 >> 8) & 0xFF;
            var b2 = (pixel2 >> 16) & 0xFF;

            var r = Math.Min(255, r1 + r2);
            var g = Math.Min(255, g1 + g2);
            var b = Math.Min(255, b1 + b2);

            return (b << 16) | (g << 8) | r;
        }

        private int BlendPixels50_50(int pixel1, int pixel2)
        {
            var r1 = pixel1 & 0xFF;
            var g1 = (pixel1 >> 8) & 0xFF;
            var b1 = (pixel1 >> 16) & 0xFF;

            var r2 = pixel2 & 0xFF;
            var g2 = (pixel2 >> 8) & 0xFF;
            var b2 = (pixel2 >> 16) & 0xFF;

            var r = (r1 + r2) / 2;
            var g = (g1 + g2) / 2;
            var b = (b1 + b2) / 2;

            return (b << 16) | (g << 8) | r;
        }

        private void BlendImages(ImageBuffer destination, ImageBuffer source, float blendFactor)
        {
            for (int y = 0; y < destination.Height; y++)
            {
                for (int x = 0; x < destination.Width; x++)
                {
                    var destPixel = destination.GetPixel(x, y);
                    var srcPixel = source.GetPixel(x, y);
                    var blendedPixel = BlendPixels50_50(destPixel, srcPixel);
                    destination.SetPixel(x, y, blendedPixel);
                }
            }
        }

        private void CopyImage(ImageBuffer source, ImageBuffer destination)
        {
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    destination.SetPixel(x, y, source.GetPixel(x, y));
                }
            }
        }

        protected override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }
    }
}
