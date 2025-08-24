using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class ColorFadeEffectsNode : BaseEffectNode
    {
        // Core properties
        public int FadeType { get; set; } = 0;
        public int StartColor { get; set; } = 0x000000;
        public int EndColor { get; set; } = 0xFFFFFF;
        public float FadeSpeed { get; set; } = 1.0f;
        public int FadeMode { get; set; } = 0;
        public bool BeatReactive { get; set; } = false;
        public float BeatFadeSpeed { get; set; } = 2.0f;
        public float CurrentFadeProgress { get; private set; } = 0.0f;
        public bool LoopFade { get; set; } = true;
        public int FadeDirection { get; set; } = 1; // 1=forward, -1=reverse
        public float FadeEasing { get; set; } = 1.0f; // Easing function power

        // Animation state
        private float _currentTime = 0.0f;
        private readonly Random _random = new Random();

        public ColorFadeEffectsNode()
        {
            Name = "Color Fade Effects";
            Description = "Creates smooth color transitions and fades between different color states";
            Category = "Color Effects";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for color fading"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Color faded output image"));
        }

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
                return GetDefaultOutput();

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
            
            float currentSpeed = FadeSpeed;
            if (BeatReactive && audioFeatures?.IsBeat == true)
            {
                currentSpeed *= BeatFadeSpeed;
            }

            // Update fade progress
            UpdateFadeProgress(currentSpeed);

            // Apply fade effect based on type
            switch (FadeType)
            {
                case 0: // Linear Fade
                    ApplyLinearFade(imageBuffer, output);
                    break;
                case 1: // Sine Fade
                    ApplySineFade(imageBuffer, output);
                    break;
                case 2: // Exponential Fade
                    ApplyExponentialFade(imageBuffer, output);
                    break;
                case 3: // Logarithmic Fade
                    ApplyLogarithmicFade(imageBuffer, output);
                    break;
                case 4: // Pulse Fade
                    ApplyPulseFade(imageBuffer, output);
                    break;
                case 5: // Rainbow Fade
                    ApplyRainbowFade(imageBuffer, output);
                    break;
                default:
                    ApplyLinearFade(imageBuffer, output);
                    break;
            }

            return output;
        }

        private void UpdateFadeProgress(float speed)
        {
            _currentTime += 0.016f; // Assuming 60 FPS
            CurrentFadeProgress += speed * 0.01f;
            
            if (LoopFade)
            {
                if (CurrentFadeProgress >= 1.0f)
                {
                    CurrentFadeProgress = 0.0f;
                    FadeDirection *= -1; // Reverse direction
                }
                else if (CurrentFadeProgress <= 0.0f)
                {
                    CurrentFadeProgress = 0.0f;
                    FadeDirection *= -1; // Reverse direction
                }
            }
            else
            {
                CurrentFadeProgress = Math.Clamp(CurrentFadeProgress, 0.0f, 1.0f);
            }
        }

        private void ApplyLinearFade(ImageBuffer source, ImageBuffer output)
        {
            float progress = CurrentFadeProgress;
            if (FadeDirection < 0)
                progress = 1.0f - progress;
            
            // Apply easing
            progress = ApplyEasing(progress);
            
            // Calculate current color
            int currentColor = InterpolateColor(StartColor, EndColor, progress);
            
            // Apply fade to each pixel
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    int sourcePixel = source.GetPixel(x, y);
                    int fadedPixel = ApplyFadeMode(sourcePixel, currentColor, progress);
                    output.SetPixel(x, y, fadedPixel);
                }
            }
        }

        private void ApplySineFade(ImageBuffer source, ImageBuffer output)
        {
            float progress = CurrentFadeProgress;
            if (FadeDirection < 0)
                progress = 1.0f - progress;
            
            // Apply sine wave easing
            float sineProgress = (float)(Math.Sin(progress * Math.PI * 2) + 1) / 2;
            sineProgress = ApplyEasing(sineProgress);
            
            int currentColor = InterpolateColor(StartColor, EndColor, sineProgress);
            
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    int sourcePixel = source.GetPixel(x, y);
                    int fadedPixel = ApplyFadeMode(sourcePixel, currentColor, sineProgress);
                    output.SetPixel(x, y, fadedPixel);
                }
            }
        }

        private void ApplyExponentialFade(ImageBuffer source, ImageBuffer output)
        {
            float progress = CurrentFadeProgress;
            if (FadeDirection < 0)
                progress = 1.0f - progress;
            
            // Apply exponential easing (accelerating)
            float expProgress = (float)(Math.Pow(progress, 2));
            expProgress = ApplyEasing(expProgress);
            
            int currentColor = InterpolateColor(StartColor, EndColor, expProgress);
            
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    int sourcePixel = source.GetPixel(x, y);
                    int fadedPixel = ApplyFadeMode(sourcePixel, currentColor, expProgress);
                    output.SetPixel(x, y, fadedPixel);
                }
            }
        }

        private void ApplyLogarithmicFade(ImageBuffer source, ImageBuffer output)
        {
            float progress = CurrentFadeProgress;
            if (FadeDirection < 0)
                progress = 1.0f - progress;
            
            // Apply logarithmic easing (decelerating)
            float logProgress = (float)(1.0 - Math.Pow(1.0 - progress, 2));
            logProgress = ApplyEasing(logProgress);
            
            int currentColor = InterpolateColor(StartColor, EndColor, logProgress);
            
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    int sourcePixel = source.GetPixel(x, y);
                    int fadedPixel = ApplyFadeMode(sourcePixel, currentColor, logProgress);
                    output.SetPixel(x, y, fadedPixel);
                }
            }
        }

        private void ApplyPulseFade(ImageBuffer source, ImageBuffer output)
        {
            float progress = CurrentFadeProgress;
            if (FadeDirection < 0)
                progress = 1.0f - progress;
            
            // Apply pulsing effect
            float pulseProgress = (float)(Math.Sin(progress * Math.PI * 4) + 1) / 2;
            pulseProgress = ApplyEasing(pulseProgress);
            
            int currentColor = InterpolateColor(StartColor, EndColor, pulseProgress);
            
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    int sourcePixel = source.GetPixel(x, y);
                    int fadedPixel = ApplyFadeMode(sourcePixel, currentColor, pulseProgress);
                    output.SetPixel(x, y, fadedPixel);
                }
            }
        }

        private void ApplyRainbowFade(ImageBuffer source, ImageBuffer output)
        {
            float progress = CurrentFadeProgress;
            if (FadeDirection < 0)
                progress = 1.0f - progress;
            
            // Convert progress to hue (0-360 degrees)
            float hue = progress * 360.0f;
            
            // Convert HSV to RGB
            int currentColor = HsvToRgb(hue, 1.0f, 1.0f);
            
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    int sourcePixel = source.GetPixel(x, y);
                    int fadedPixel = ApplyFadeMode(sourcePixel, currentColor, progress);
                    output.SetPixel(x, y, fadedPixel);
                }
            }
        }

        private int InterpolateColor(int startColor, int endColor, float progress)
        {
            int r1 = startColor & 0xFF;
            int g1 = (startColor >> 8) & 0xFF;
            int b1 = (startColor >> 16) & 0xFF;
            
            int r2 = endColor & 0xFF;
            int g2 = (endColor >> 8) & 0xFF;
            int b2 = (endColor >> 16) & 0xFF;
            
            int r = (int)(r1 + (r2 - r1) * progress);
            int g = (int)(g1 + (g2 - g1) * progress);
            int b = (int)(b1 + (b2 - b1) * progress);
            
            return r | (g << 8) | (b << 16);
        }

        private float ApplyEasing(float progress)
        {
            switch (FadeEasing)
            {
                case 1.0f: // Linear
                    return progress;
                case 2.0f: // Quadratic
                    return progress * progress;
                case 3.0f: // Cubic
                    return progress * progress * progress;
                case 0.5f: // Square root
                    return (float)Math.Sqrt(progress);
                case 0.33f: // Cube root
                    return (float)Math.Pow(progress, 1.0 / 3.0);
                default:
                    return (float)Math.Pow(progress, FadeEasing);
            }
        }

        private int ApplyFadeMode(int sourcePixel, int fadeColor, float progress)
        {
            switch (FadeMode)
            {
                case 0: // Replace
                    return fadeColor;
                    
                case 1: // Add
                    return BlendAdditive(sourcePixel, fadeColor, progress);
                    
                case 2: // Multiply
                    return BlendMultiply(sourcePixel, fadeColor, progress);
                    
                case 3: // Screen
                    return BlendScreen(sourcePixel, fadeColor, progress);
                    
                case 4: // Overlay
                    return BlendOverlay(sourcePixel, fadeColor, progress);
                    
                case 5: // Alpha Blend
                    return BlendAlpha(sourcePixel, fadeColor, progress);
                    
                default:
                    return sourcePixel;
            }
        }

        private int BlendAdditive(int color1, int color2, float progress)
        {
            int r1 = color1 & 0xFF;
            int g1 = (color1 >> 8) & 0xFF;
            int b1 = (color1 >> 16) & 0xFF;
            
            int r2 = color2 & 0xFF;
            int g2 = (color2 >> 8) & 0xFF;
            int b2 = (color2 >> 16) & 0xFF;
            
            int r = Math.Min(255, r1 + (int)(r2 * progress));
            int g = Math.Min(255, g1 + (int)(g2 * progress));
            int b = Math.Min(255, b1 + (int)(b2 * progress));
            
            return r | (g << 8) | (b << 16);
        }

        private int BlendMultiply(int color1, int color2, float progress)
        {
            int r1 = color1 & 0xFF;
            int g1 = (color1 >> 8) & 0xFF;
            int b1 = (color1 >> 16) & 0xFF;
            
            int r2 = color2 & 0xFF;
            int g2 = (color2 >> 8) & 0xFF;
            int b2 = (color2 >> 16) & 0xFF;
            
            int r = (int)((r1 * r2 * progress) / 255.0f);
            int g = (int)((g1 * g2 * progress) / 255.0f);
            int b = (int)((b1 * b2 * progress) / 255.0f);
            
            return r | (g << 8) | (b << 16);
        }

        private int BlendScreen(int color1, int color2, float progress)
        {
            int r1 = color1 & 0xFF;
            int g1 = (color1 >> 8) & 0xFF;
            int b1 = (color1 >> 16) & 0xFF;
            
            int r2 = color2 & 0xFF;
            int g2 = (color2 >> 8) & 0xFF;
            int b2 = (color2 >> 16) & 0xFF;
            
            int r = (int)(255 - ((255 - r1) * (255 - r2) * progress) / 255.0f);
            int g = (int)(255 - ((255 - g1) * (255 - g2) * progress) / 255.0f);
            int b = (int)(255 - ((255 - b1) * (255 - b2) * progress) / 255.0f);
            
            return r | (g << 8) | (b << 16);
        }

        private int BlendOverlay(int color1, int color2, float progress)
        {
            int r1 = color1 & 0xFF;
            int g1 = (color1 >> 8) & 0xFF;
            int b1 = (color1 >> 16) & 0xFF;
            
            int r2 = color2 & 0xFF;
            int g2 = (color2 >> 8) & 0xFF;
            int b2 = (color2 >> 16) & 0xFF;
            
            int r = (int)(r1 < 128 ? (2 * r1 * r2 * progress) / 255.0f : 255 - (2 * (255 - r1) * (255 - r2) * progress) / 255.0f);
            int g = (int)(g1 < 128 ? (2 * g1 * g2 * progress) / 255.0f : 255 - (2 * (255 - g1) * (255 - g2) * progress) / 255.0f);
            int b = (int)(b1 < 128 ? (2 * b1 * b2 * progress) / 255.0f : 255 - (2 * (255 - b1) * (255 - b2) * progress) / 255.0f);
            
            return r | (g << 8) | (b << 16);
        }

        private int BlendAlpha(int color1, int color2, float progress)
        {
            int r1 = color1 & 0xFF;
            int g1 = (color1 >> 8) & 0xFF;
            int b1 = (color1 >> 16) & 0xFF;
            
            int r2 = color2 & 0xFF;
            int g2 = (color2 >> 8) & 0xFF;
            int b2 = (color2 >> 16) & 0xFF;
            
            int r = (int)(r1 * (1 - progress) + r2 * progress);
            int g = (int)(g1 * (1 - progress) + g2 * progress);
            int b = (int)(b1 * (1 - progress) + b2 * progress);
            
            return r | (g << 8) | (b << 16);
        }

        private int HsvToRgb(float h, float s, float v)
        {
            float c = v * s;
            float x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            float m = v - c;
            
            float r, g, b;
            
            if (h >= 0 && h < 60)
            {
                r = c; g = x; b = 0;
            }
            else if (h >= 60 && h < 120)
            {
                r = x; g = c; b = 0;
            }
            else if (h >= 120 && h < 180)
            {
                r = 0; g = c; b = x;
            }
            else if (h >= 180 && h < 240)
            {
                r = 0; g = x; b = c;
            }
            else if (h >= 240 && h < 300)
            {
                r = x; g = 0; b = c;
            }
            else
            {
                r = c; g = 0; b = x;
            }
            
            int ri = Math.Clamp((int)((r + m) * 255), 0, 255);
            int gi = Math.Clamp((int)((g + m) * 255), 0, 255);
            int bi = Math.Clamp((int)((b + m) * 255), 0, 255);
            
            return ri | (gi << 8) | (bi << 16);
        }

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }
    }
}
