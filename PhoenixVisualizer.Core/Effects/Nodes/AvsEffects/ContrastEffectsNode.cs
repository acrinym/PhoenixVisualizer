using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class ContrastEffectsNode : BaseEffectNode
    {
        // Core properties
        public bool Enabled { get; set; } = true;
        public int ColorClip { get; set; } = 0x202020;
        public int ColorClipOut { get; set; } = 0x202020;
        public int ColorDist { get; set; } = 10;
        public bool BeatReactive { get; set; } = false;
        public float BeatIntensity { get; set; } = 1.0f;
        public bool EnableAdvancedContrast { get; set; } = false;
        public float ContrastStrength { get; set; } = 1.0f;
        public int ContrastMode { get; set; } = 0; // 0=Standard, 1=Enhanced, 2=Extreme
        public bool EnableColorPreservation { get; set; } = false;
        public float ColorPreservationStrength { get; set; } = 0.5f;
        public bool EnableDistanceAnimation { get; set; } = false;
        public float DistanceAnimationSpeed { get; set; } = 1.0f;
        public int DistanceAnimationMode { get; set; } = 0;

        // Internal state
        private int _lastWidth, _lastHeight;
        private readonly object _renderLock = new object();
        private float _currentTime = 0.0f;
        private readonly Random _random = new Random();

        // Performance optimization constants
        private const int MaxColorValue = 0xFFFFFF;
        private const int MinColorValue = 0x000000;
        private const int MaxDistance = 255;
        private const int MinDistance = 0;

        public ContrastEffectsNode()
        {
            Name = "Contrast Effects";
            Description = "Advanced contrast enhancement with color clipping and distance-based processing";
            Category = "Color Enhancement";
            _lastWidth = _lastHeight = 0;
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for contrast enhancement"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Contrast enhanced output image"));
        }

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
                return GetDefaultOutput();

            if (!Enabled || imageBuffer.Width <= 0 || imageBuffer.Height <= 0)
                return imageBuffer;

            lock (_renderLock)
            {
                // Update buffers if dimensions changed
                UpdateBuffers(imageBuffer);

                // Update distance animation if enabled
                if (EnableDistanceAnimation)
                {
                    UpdateDistanceAnimation();
                }

                var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);

                // Apply contrast enhancement effect
                ApplyContrastEffect(imageBuffer, output, audioFeatures);

                return output;
            }
        }

        private void UpdateBuffers(ImageBuffer imageBuffer)
        {
            if (_lastWidth != imageBuffer.Width || _lastHeight != imageBuffer.Height)
            {
                _lastWidth = imageBuffer.Width;
                _lastHeight = imageBuffer.Height;
            }
        }

        private void UpdateDistanceAnimation()
        {
            if (!EnableDistanceAnimation)
                return;

            _currentTime += 0.016f; // Assuming 60 FPS
            var animationProgress = (_currentTime * DistanceAnimationSpeed) % (Math.PI * 2);

            switch (DistanceAnimationMode)
            {
                case 0: // Pulsing distance
                    var pulse = (float)((Math.Sin(animationProgress) + 1.0) * 0.5);
                    ColorDist = (int)(5 + pulse * 45); // 5-50 distance range
                    break;

                case 1: // Oscillating distance
                    var oscillation = (float)Math.Sin(animationProgress * 2);
                    ColorDist = (int)(10 + oscillation * 40); // 10-50 distance range
                    break;

                case 2: // Random distance
                    if (_random.NextDouble() < 0.01f) // 1% chance per frame
                    {
                        ColorDist = _random.Next(5, 51); // 5-50 distance range
                    }
                    break;

                case 3: // Wave pattern distance
                    var wave = (float)Math.Sin(animationProgress * 3);
                    ColorDist = (int)(8 + wave * 42); // 8-50 distance range
                    break;
            }
        }

        private void ApplyContrastEffect(ImageBuffer input, ImageBuffer output, AudioFeatures audioFeatures)
        {
            // Extract color components from thresholds
            int clipR = (ColorClip >> 16) & 0xFF;
            int clipG = (ColorClip >> 8) & 0xFF;
            int clipB = ColorClip & 0xFF;

            int clipOutR = (ColorClipOut >> 16) & 0xFF;
            int clipOutG = (ColorClipOut >> 8) & 0xFF;
            int clipOutB = ColorClipOut & 0xFF;

            // Apply beat reactivity if enabled
            if (BeatReactive && audioFeatures?.IsBeat == true)
            {
                var beatMultiplier = BeatIntensity;
                clipR = (int)Math.Clamp(clipR * beatMultiplier, 0, 255);
                clipG = (int)Math.Clamp(clipG * beatMultiplier, 0, 255);
                clipB = (int)Math.Clamp(clipB * beatMultiplier, 0, 255);
            }

            // Process each pixel
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    int inputPixel = input.GetPixel(x, y);
                    int processedPixel = ProcessPixel(inputPixel, clipR, clipG, clipB, clipOutR, clipOutG, clipOutB);
                    output.SetPixel(x, y, processedPixel);
                }
            }
        }

        private int ProcessPixel(int pixel, int clipR, int clipG, int clipB, int clipOutR, int clipOutG, int clipOutB)
        {
            // Extract RGB components
            int r = pixel & 0xFF;
            int g = (pixel >> 8) & 0xFF;
            int b = (pixel >> 16) & 0xFF;
            int a = (pixel >> 24) & 0xFF;

            // Calculate color distance from clip threshold
            int distance = CalculateColorDistance(r, g, b, clipR, clipG, clipB);

            // Check if pixel is within distance threshold
            if (distance <= ColorDist)
            {
                // Apply contrast enhancement
                return ApplyContrastEnhancement(r, g, b, a, clipR, clipG, clipB, clipOutR, clipOutG, clipOutB);
            }
            else
            {
                // Pixel is outside distance threshold, return unchanged
                return pixel;
            }
        }

        private int CalculateColorDistance(int r1, int g1, int b1, int r2, int g2, int b2)
        {
            // Calculate Euclidean distance in RGB color space
            int dr = r1 - r2;
            int dg = g1 - g2;
            int db = b1 - b2;

            return (int)Math.Sqrt(dr * dr + dg * dg + db * db);
        }

        private int ApplyContrastEnhancement(int r, int g, int b, int a, int clipR, int clipG, int clipB, int clipOutR, int clipOutG, int clipOutB)
        {
            // Apply input color clipping
            int newR = ApplyColorClipping(r, clipR, clipOutR);
            int newG = ApplyColorClipping(g, clipG, clipOutG);
            int newB = ApplyColorClipping(b, clipB, clipOutB);

            // Apply advanced contrast if enabled
            if (EnableAdvancedContrast)
            {
                newR = ApplyAdvancedContrast(newR, clipR, clipOutR);
                newG = ApplyAdvancedContrast(newG, clipG, clipOutG);
                newB = ApplyAdvancedContrast(newB, clipB, clipOutB);
            }

            // Apply color preservation if enabled
            if (EnableColorPreservation)
            {
                newR = ApplyColorPreservation(r, newR);
                newG = ApplyColorPreservation(g, newG);
                newB = ApplyColorPreservation(b, newB);
            }

            return (a << 24) | (newB << 16) | (newG << 8) | newR;
        }

        private int ApplyColorClipping(int channelValue, int clipThreshold, int clipOutThreshold)
        {
            // Apply input clipping (enhance dark colors)
            if (channelValue < clipThreshold)
            {
                // Scale dark colors to enhance contrast
                float scaleFactor = (float)clipOutThreshold / clipThreshold;
                int enhanced = (int)(channelValue * scaleFactor);
                return Math.Clamp(enhanced, 0, 255);
            }
            else
            {
                // Apply output clipping (limit bright colors)
                if (channelValue > clipOutThreshold)
                {
                    return clipOutThreshold;
                }
                else
                {
                    return channelValue;
                }
            }
        }

        private int ApplyAdvancedContrast(int channelValue, int clipThreshold, int clipOutThreshold)
        {
            switch (ContrastMode)
            {
                case 1: // Enhanced
                    return ApplyEnhancedContrast(channelValue, clipThreshold, clipOutThreshold);
                case 2: // Extreme
                    return ApplyExtremeContrast(channelValue, clipThreshold, clipOutThreshold);
                default: // Standard
                    return channelValue;
            }
        }

        private int ApplyEnhancedContrast(int channelValue, int clipThreshold, int clipOutThreshold)
        {
            // Enhanced contrast with gamma correction
            float gamma = 1.2f;
            float normalized = channelValue / 255.0f;
            float enhanced = (float)Math.Pow(normalized, gamma);
            return Math.Clamp((int)(enhanced * 255), 0, 255);
        }

        private int ApplyExtremeContrast(int channelValue, int clipThreshold, int clipOutThreshold)
        {
            // Extreme contrast with sigmoid function
            float normalized = (channelValue - 127.0f) / 127.0f;
            float extreme = (float)(1.0 / (1.0 + Math.Exp(-normalized * 3.0)));
            return Math.Clamp((int)(extreme * 255), 0, 255);
        }

        private int ApplyColorPreservation(int originalValue, int processedValue)
        {
            // Blend original and processed values based on preservation strength
            float blend = ColorPreservationStrength;
            int preserved = (int)(originalValue * blend + processedValue * (1.0f - blend));
            return Math.Clamp(preserved, 0, 255);
        }

        // Public interface for parameter adjustment
        public void SetEnabled(bool enable) { Enabled = enable; }

        public void SetColorClip(int colorClip)
        {
            ColorClip = Math.Clamp(colorClip, MinColorValue, MaxColorValue);
        }

        public void SetColorClipOut(int colorClipOut)
        {
            ColorClipOut = Math.Clamp(colorClipOut, MinColorValue, MaxColorValue);
        }

        public void SetColorDist(int colorDist)
        {
            ColorDist = Math.Clamp(colorDist, MinDistance, MaxDistance);
        }

        // Status queries
        public new bool IsEnabled() => Enabled;
        public int GetColorClip() => ColorClip;
        public int GetColorClipOut() => ColorClipOut;
        public int GetColorDist() => ColorDist;

        // Advanced contrast control
        public void SetRGBClip(int r, int g, int b)
        {
            int colorClip = (r << 16) | (g << 8) | b;
            SetColorClip(colorClip);
        }

        public void SetRGBClipOut(int r, int g, int b)
        {
            int colorClipOut = (r << 16) | (g << 8) | b;
            SetColorClipOut(colorClipOut);
        }

        public void SetClipThreshold(int threshold)
        {
            // Set both input and output thresholds to the same value
            int colorValue = (threshold << 16) | (threshold << 8) | threshold;
            SetColorClip(colorValue);
            SetColorClipOut(colorValue);
        }

        public void SetContrastEnhancement(int enhancement)
        {
            // Adjust color distance based on enhancement level
            int distance = Math.Max(1, 50 - enhancement);
            SetColorDist(distance);
        }

        // Contrast effect presets
        public void SetHighContrast()
        {
            SetClipThreshold(64);
            SetColorDist(5);
            ContrastMode = 1; // Enhanced
        }

        public void SetMediumContrast()
        {
            SetClipThreshold(96);
            SetColorDist(15);
            ContrastMode = 0; // Standard
        }

        public void SetLowContrast()
        {
            SetClipThreshold(128);
            SetColorDist(25);
            ContrastMode = 0; // Standard
        }

        public void SetExtremeContrast()
        {
            SetClipThreshold(32);
            SetColorDist(2);
            ContrastMode = 2; // Extreme
        }

        public void SetSelectiveContrast(int r, int g, int b, int distance)
        {
            SetRGBClip(r, g, b);
            SetRGBClipOut(r, g, b);
            SetColorDist(distance);
        }

        // Color-specific presets
        public void SetRedContrast()
        {
            SetRGBClip(64, 0, 0);
            SetRGBClipOut(64, 0, 0);
            SetColorDist(10);
        }

        public void SetGreenContrast()
        {
            SetRGBClip(0, 64, 0);
            SetRGBClipOut(0, 64, 0);
            SetColorDist(10);
        }

        public void SetBlueContrast()
        {
            SetRGBClip(0, 0, 64);
            SetRGBClipOut(0, 0, 64);
            SetColorDist(10);
        }

        public void SetWhiteContrast()
        {
            SetRGBClip(128, 128, 128);
            SetRGBClipOut(128, 128, 128);
            SetColorDist(20);
        }

        public void SetBlackContrast()
        {
            SetRGBClip(32, 32, 32);
            SetRGBClipOut(32, 32, 32);
            SetColorDist(15);
        }

        protected override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }
    }
}
