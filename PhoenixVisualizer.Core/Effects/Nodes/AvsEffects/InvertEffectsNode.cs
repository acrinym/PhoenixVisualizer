using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class InvertEffectsNode : BaseEffectNode
    {
        // Core properties
        public bool Enabled { get; set; } = true;
        public bool BeatReactive { get; set; } = false;
        public float BeatIntensity { get; set; } = 1.0f;
        public bool EnablePartialInversion { get; set; } = false;
        public float InversionStrength { get; set; } = 1.0f;
        public int InversionMode { get; set; } = 0;
        public bool EnableChannelSelectiveInversion { get; set; } = false;
        public bool InvertRedChannel { get; set; } = true;
        public bool InvertGreenChannel { get; set; } = true;
        public bool InvertBlueChannel { get; set; } = true;
        public bool EnableThresholdInversion { get; set; } = false;
        public float InversionThreshold { get; set; } = 0.5f;
        public bool EnableSmoothInversion { get; set; } = false;
        public float SmoothInversionSpeed { get; set; } = 1.0f;
        public bool EnableInversionAnimation { get; set; } = false;
        public float AnimationSpeed { get; set; } = 1.0f;
        public int AnimationMode { get; set; } = 0;
        public bool EnableInversionMasking { get; set; } = false;
        public ImageBuffer InversionMask { get; set; } = null;
        public float MaskInfluence { get; set; } = 1.0f;
        public bool EnableInversionBlending { get; set; } = false;
        public float BlendMode { get; set; } = 0.5f;

        // Animation state
        private float _currentTime = 0.0f;
        private readonly Random _random = new Random();

        public InvertEffectsNode()
        {
            Name = "Invert Effects";
            Description = "Inverts image colors with configurable strength and channel selection";
            Category = "Color Transformation";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for inversion"));
            _inputPorts.Add(new EffectPort("Mask", typeof(ImageBuffer), false, null, "Optional inversion mask image"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Inverted output image"));
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
                InversionMask = maskBuffer;
            }

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
            var currentInversionStrength = GetCurrentInversionStrength(audioFeatures);

            // Update animation if enabled
            if (EnableInversionAnimation)
            {
                UpdateInversionAnimation(0.016f); // Assuming 60 FPS
            }

            // Process each pixel
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    var originalColor = imageBuffer.GetPixel(x, y);
                    var invertedColor = InvertPixel(originalColor, currentInversionStrength);

                    // Apply channel selective inversion if enabled
                    if (EnableChannelSelectiveInversion)
                    {
                        invertedColor = ApplyChannelSelectiveInversion(originalColor, invertedColor);
                    }

                    // Apply threshold inversion if enabled
                    if (EnableThresholdInversion)
                    {
                        invertedColor = ApplyThresholdInversion(originalColor, invertedColor);
                    }

                    // Apply inversion masking if enabled
                    if (EnableInversionMasking && InversionMask != null)
                    {
                        var pixelIndex = y * output.Width + x;
                        invertedColor = ApplyInversionMasking(originalColor, invertedColor, x, y);
                    }

                    // Apply inversion blending if enabled
                    if (EnableInversionBlending)
                    {
                        invertedColor = BlendInversion(originalColor, invertedColor);
                    }

                    output.SetPixel(x, y, invertedColor);
                }
            }

            return output;
        }

        private float GetCurrentInversionStrength(AudioFeatures audio)
        {
            if (!BeatReactive || audio == null)
                return InversionStrength;

            var beatMultiplier = 1.0f;

            if (audio.IsBeat)
            {
                beatMultiplier = BeatIntensity;
            }
            else
            {
                // Gradual return to normal
                beatMultiplier = 1.0f + (BeatIntensity - 1.0f) * (audio.Rms / 255.0f);
            }

            return Math.Max(0.0f, Math.Min(1.0f, InversionStrength * beatMultiplier));
        }

        private int InvertPixel(int color, float strength)
        {
            if (strength <= 0.0f)
                return color;

            if (strength >= 1.0f)
                return InvertPixelFull(color);

            // Partial inversion
            var r = color & 0xFF;
            var g = (color >> 8) & 0xFF;
            var b = (color >> 16) & 0xFF;
            var a = (color >> 24) & 0xFF;

            var invertedR = (int)(r + (255 - r) * strength);
            var invertedG = (int)(g + (255 - g) * strength);
            var invertedB = (int)(b + (255 - b) * strength);

            return (a << 24) | (invertedB << 16) | (invertedG << 8) | invertedR;
        }

        private int InvertPixelFull(int color)
        {
            // Full inversion using XOR (equivalent to 255 - value for each channel)
            return color ^ 0x00FFFFFF; // Preserve alpha channel
        }

        private int ApplyChannelSelectiveInversion(int originalColor, int invertedColor)
        {
            if (!EnableChannelSelectiveInversion)
                return invertedColor;

            var r = originalColor & 0xFF;
            var g = (originalColor >> 8) & 0xFF;
            var b = (originalColor >> 16) & 0xFF;
            var a = (originalColor >> 24) & 0xFF;

            var invertedR = invertedColor & 0xFF;
            var invertedG = (invertedColor >> 8) & 0xFF;
            var invertedB = (invertedColor >> 16) & 0xFF;

            var finalR = InvertRedChannel ? invertedR : r;
            var finalG = InvertGreenChannel ? invertedG : g;
            var finalB = InvertBlueChannel ? invertedB : b;

            return (a << 24) | (finalB << 16) | (finalG << 8) | finalR;
        }

        private int ApplyThresholdInversion(int originalColor, int invertedColor)
        {
            if (!EnableThresholdInversion)
                return invertedColor;

            var r = originalColor & 0xFF;
            var g = (originalColor >> 8) & 0xFF;
            var b = (originalColor >> 16) & 0xFF;

            // Calculate normalized brightness
            var brightness = (r + g + b) / (3.0f * 255.0f);

            if (brightness > InversionThreshold)
            {
                // Only invert bright pixels
                return invertedColor;
            }

            return originalColor;
        }

        private int ApplyInversionMasking(int originalColor, int invertedColor, int x, int y)
        {
            if (!EnableInversionMasking || InversionMask == null)
                return invertedColor;

            // Ensure mask coordinates are within bounds
            if (x >= InversionMask.Width || y >= InversionMask.Height)
                return invertedColor;

            var maskPixel = InversionMask.GetPixel(x, y);
            var maskIntensity = (maskPixel & 0xFF) / 255.0f; // Use red channel as mask

            // Blend original and inverted based on mask
            var blendFactor = maskIntensity * MaskInfluence;
            var finalColor = BlendColors(originalColor, invertedColor, blendFactor);

            return finalColor;
        }

        private int BlendColors(int color1, int color2, float blendFactor)
        {
            var r1 = color1 & 0xFF;
            var g1 = (color1 >> 8) & 0xFF;
            var b1 = (color1 >> 16) & 0xFF;
            var a1 = (color1 >> 24) & 0xFF;

            var r2 = color2 & 0xFF;
            var g2 = (color2 >> 8) & 0xFF;
            var b2 = (color2 >> 16) & 0xFF;

            var r = (int)(r1 + (r2 - r1) * blendFactor);
            var g = (int)(g1 + (g2 - g1) * blendFactor);
            var b = (int)(b1 + (b2 - b1) * blendFactor);

            return (a1 << 24) | (b << 16) | (g << 8) | r;
        }

        private float GetSmoothInversionProgress()
        {
            if (!EnableSmoothInversion)
                return 1.0f;

            _currentTime += 0.016f; // Assuming 60 FPS
            var progress = (_currentTime * SmoothInversionSpeed) % (Math.PI * 2);

            // Smooth sine wave transition
            return (float)((Math.Sin(progress) + 1.0) * 0.5);
        }

        private void UpdateInversionAnimation(float deltaTime)
        {
            if (!EnableInversionAnimation)
                return;

            _currentTime += deltaTime;
            var animationProgress = (_currentTime * AnimationSpeed) % (Math.PI * 2);

            switch (AnimationMode)
            {
                case 0: // Pulsing
                    var pulse = (float)((Math.Sin(animationProgress) + 1.0) * 0.5);
                    InversionStrength = 0.3f + pulse * 0.7f;
                    break;

                case 1: // Wave pattern
                    var wave = (float)Math.Sin(animationProgress * 3);
                    InversionStrength = 0.5f + wave * 0.5f;
                    break;

                case 2: // Random flicker
                    if (_random.NextDouble() < 0.02f) // 2% chance per frame
                    {
                        InversionStrength = _random.Next(0, 100) / 100.0f;
                    }
                    break;

                case 3: // Rotating channels
                    var channelRotation = (animationProgress / (Math.PI * 2)) * 3;
                    var channelIndex = (int)channelRotation;
                    var channelProgress = channelRotation - channelIndex;

                    InvertRedChannel = (channelIndex == 0);
                    InvertGreenChannel = (channelIndex == 1);
                    InvertBlueChannel = (channelIndex == 2);
                    break;
            }
        }

        private int BlendInversion(int originalColor, int invertedColor)
        {
            return BlendColors(originalColor, invertedColor, BlendMode);
        }

        protected override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }
    }
}
