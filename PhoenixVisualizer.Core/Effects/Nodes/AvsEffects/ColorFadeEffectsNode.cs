using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class ColorFadeEffectsNode : BaseEffectNode
    {
        #region Properties

        // Core AVS-style properties
        public bool Enabled { get; set; } = true;
        public int[] StaticFaders { get; set; } = { 8, -8, -8 }; // Red, Green, Blue offsets (-32 to 32)
        public int[] BeatFaders { get; set; } = { 8, -8, -8 }; // Beat-responsive offsets
        public int[] CurrentFadePositions { get; set; } = { 8, -8, -8 }; // Current positions
        public bool BeatResponseEnabled { get; set; } = false;
        public bool RandomBeatResponseEnabled { get; set; } = false;
        public bool SmoothAnimationEnabled { get; set; } = false;
        public float Intensity { get; set; } = 1.0f;

        // Modern fade properties
        public int FadeType { get; set; } = 0; // 0=Linear, 1=Sine, 2=Exponential, 3=Logarithmic, 4=Pulse, 5=Rainbow
        public int StartColor { get; set; } = 0x000000;
        public int EndColor { get; set; } = 0xFFFFFF;
        public float FadeSpeed { get; set; } = 1.0f;
        public int FadeMode { get; set; } = 0; // 0=Replace, 1=Add, 2=Multiply, 3=Screen, 4=Overlay, 5=Alpha
        public bool BeatReactive { get; set; } = false;
        public float BeatFadeSpeed { get; set; } = 2.0f;
        public float CurrentFadeProgress { get; private set; } = 0.0f;
        public bool LoopFade { get; set; } = true;
        public int FadeDirection { get; set; } = 1; // 1=forward, -1=reverse
        public float FadeEasing { get; set; } = 1.0f; // Easing function power

        // Animation state
        private float _currentTime = 0.0f;
        private readonly Random _random = new Random();

        // AVS-style lookup tables
        private byte[,]? _colorTable;
        private byte[]? _clipTable;
        private int[,] _transformMatrix;

        #endregion

        public ColorFadeEffectsNode()
        {
            Name = "Color Fade Effects";
            Description = "Comprehensive color manipulation with AVS-style channel offsets and modern fade effects";
            Category = "Color Effects";
            
            InitializeColorTable();
            InitializeClipTable();
            _transformMatrix = new int[4, 3];
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
            
            if (!Enabled)
            {
                // Copy input to output without modification
                for (int y = 0; y < output.Height; y++)
                {
                    for (int x = 0; x < output.Width; x++)
                    {
                        output.SetPixel(x, y, imageBuffer.GetPixel(x, y));
                    }
                }
                return output;
            }

            // Update AVS-style fade positions
            UpdateFadePositions(audioFeatures);
            UpdateTransformMatrix();

            // Update modern fade progress
            float currentSpeed = FadeSpeed;
            if (BeatReactive && audioFeatures?.IsBeat == true)
            {
                currentSpeed *= BeatFadeSpeed;
            }
            UpdateFadeProgress(currentSpeed);

            // Apply combined effects
            ApplyCombinedEffects(imageBuffer, output);

            return output;
        }

        private void UpdateFadePositions(AudioFeatures audioFeatures)
        {
            if (SmoothAnimationEnabled)
            {
                // Smoothly animate towards target fade values
                for (int i = 0; i < 3; i++)
                {
                    if (CurrentFadePositions[i] < StaticFaders[i])
                        CurrentFadePositions[i]++;
                    else if (CurrentFadePositions[i] > StaticFaders[i])
                        CurrentFadePositions[i]--;
                }
            }
            else
            {
                // Snap to target values
                Array.Copy(StaticFaders, CurrentFadePositions, 3);
            }

            // Handle beat response
            if (audioFeatures?.IsBeat == true)
            {
                if (RandomBeatResponseEnabled)
                {
                    // Random beat response
                    CurrentFadePositions[0] = _random.Next(-6, 26);
                    CurrentFadePositions[1] = _random.Next(-32, 32);
                    CurrentFadePositions[2] = _random.Next(-6, 26);

                    // Ensure green channel has sufficient contrast
                    if (CurrentFadePositions[1] < 0 && CurrentFadePositions[1] > -16)
                        CurrentFadePositions[1] = -32;
                    if (CurrentFadePositions[1] >= 0 && CurrentFadePositions[1] < 16)
                        CurrentFadePositions[1] = 32;
                }
                else if (BeatResponseEnabled)
                {
                    // Use beat fade values
                    Array.Copy(BeatFaders, CurrentFadePositions, 3);
                }
            }
        }

        private void UpdateTransformMatrix()
        {
            int fs1 = CurrentFadePositions[0]; // Red
            int fs2 = CurrentFadePositions[1]; // Green
            int fs3 = CurrentFadePositions[2]; // Blue

            // Mode 0: Blue, Green, Red
            _transformMatrix[0, 0] = fs3;
            _transformMatrix[0, 1] = fs2;
            _transformMatrix[0, 2] = fs1;

            // Mode 1: Green, Red, Blue
            _transformMatrix[1, 0] = fs2;
            _transformMatrix[1, 1] = fs1;
            _transformMatrix[1, 2] = fs3;

            // Mode 2: Red, Blue, Green
            _transformMatrix[2, 0] = fs1;
            _transformMatrix[2, 1] = fs3;
            _transformMatrix[2, 2] = fs2;

            // Mode 3: Blue, Blue, Blue (monochrome)
            _transformMatrix[3, 0] = fs3;
            _transformMatrix[3, 1] = fs3;
            _transformMatrix[3, 2] = fs3;
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

        private void ApplyCombinedEffects(ImageBuffer source, ImageBuffer output)
        {
            // Apply AVS-style color channel manipulation
            ApplyAvsColorFade(source, output);

            // Apply modern fade effects on top
            ApplyModernFadeEffects(output);
        }

        private void ApplyAvsColorFade(ImageBuffer source, ImageBuffer output)
        {
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    int sourcePixel = source.GetPixel(x, y);
                    int transformedPixel = TransformPixelAvs(sourcePixel);
                    output.SetPixel(x, y, transformedPixel);
                }
            }
        }

        private int TransformPixelAvs(int pixel)
        {
            int r = pixel & 0xFF;
            int g = (pixel >> 8) & 0xFF;
            int b = (pixel >> 16) & 0xFF;

            // Calculate color relationship index
            int index = ((g - b) << 9) + b - r;

            // Clamp index to valid range
            index = Math.Max(0, Math.Min(511, index + 255));

            // Get transformation type
            byte transformType = _colorTable![index, 0];

            // Apply transformation using current fade positions
            int newR = _clipTable![r + _transformMatrix![transformType, 0] + 40];
            int newG = _clipTable![g + _transformMatrix![transformType, 1] + 40];
            int newB = _clipTable![b + _transformMatrix![transformType, 2] + 40];

            return newR | (newG << 8) | (newB << 16);
        }

        private void ApplyModernFadeEffects(ImageBuffer output)
        {
            // Apply fade effect based on type
            switch (FadeType)
            {
                case 0: // Linear Fade
                    ApplyLinearFade(output);
                    break;
                case 1: // Sine Fade
                    ApplySineFade(output);
                    break;
                case 2: // Exponential Fade
                    ApplyExponentialFade(output);
                    break;
                case 3: // Logarithmic Fade
                    ApplyLogarithmicFade(output);
                    break;
                case 4: // Pulse Fade
                    ApplyPulseFade(output);
                    break;
                case 5: // Rainbow Fade
                    ApplyRainbowFade(output);
                    break;
                default:
                    ApplyLinearFade(output);
                    break;
            }
        }

        private void ApplyLinearFade(ImageBuffer output)
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
                    int sourcePixel = output.GetPixel(x, y);
                    int fadedPixel = ApplyFadeMode(sourcePixel, currentColor, progress);
                    output.SetPixel(x, y, fadedPixel);
                }
            }
        }

        private void ApplySineFade(ImageBuffer output)
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
                    int sourcePixel = output.GetPixel(x, y);
                    int fadedPixel = ApplyFadeMode(sourcePixel, currentColor, sineProgress);
                    output.SetPixel(x, y, fadedPixel);
                }
            }
        }

        private void ApplyExponentialFade(ImageBuffer output)
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
                    int sourcePixel = output.GetPixel(x, y);
                    int fadedPixel = ApplyFadeMode(sourcePixel, currentColor, expProgress);
                    output.SetPixel(x, y, fadedPixel);
                }
            }
        }

        private void ApplyLogarithmicFade(ImageBuffer output)
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
                    int sourcePixel = output.GetPixel(x, y);
                    int fadedPixel = ApplyFadeMode(sourcePixel, currentColor, logProgress);
                    output.SetPixel(x, y, fadedPixel);
                }
            }
        }

        private void ApplyPulseFade(ImageBuffer output)
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
                    int sourcePixel = output.GetPixel(x, y);
                    int fadedPixel = ApplyFadeMode(sourcePixel, currentColor, pulseProgress);
                    output.SetPixel(x, y, fadedPixel);
                }
            }
        }

        private void ApplyRainbowFade(ImageBuffer output)
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
                    int sourcePixel = output.GetPixel(x, y);
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

        private void InitializeColorTable()
        {
            _colorTable = new byte[512, 512];

            for (int x = 0; x < 512; x++)
            {
                for (int y = 0; y < 512; y++)
                {
                    int xp = x - 255;
                    int yp = y - 255;

                    // Determine color transformation type based on RGB relationships
                    if (xp > 0 && xp > -yp) // Green > Blue and Green > Red
                        _colorTable[x, y] = 0;
                    else if (yp < 0 && xp < -yp) // Red > Blue and Red > Green
                        _colorTable[x, y] = 1;
                    else if (xp < 0 && yp > 0) // Blue > Green and Blue > Red
                        _colorTable[x, y] = 2;
                    else // Default case
                        _colorTable[x, y] = 3;
                }
            }
        }

        private void InitializeClipTable()
        {
            _clipTable = new byte[336]; // 256 + 40 + 40

            for (int x = 0; x < 336; x++)
            {
                _clipTable[x] = (byte)Math.Max(0, Math.Min(255, x - 40));
            }
        }

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }

        #region Utility Methods

        /// <summary>
        /// Reset fade positions to static values
        /// </summary>
        public void ResetFadePositions()
        {
            Array.Copy(StaticFaders, CurrentFadePositions, 3);
        }

        /// <summary>
        /// Set all fade values to zero (no effect)
        /// </summary>
        public void ClearFadeValues()
        {
            for (int i = 0; i < 3; i++)
            {
                StaticFaders[i] = 0;
                BeatFaders[i] = 0;
                CurrentFadePositions[i] = 0;
            }
        }

        /// <summary>
        /// Create a warm color fade (emphasize red/orange)
        /// </summary>
        public void SetWarmFade()
        {
            StaticFaders[0] = 16;  // Red boost
            StaticFaders[1] = 8;   // Green slight boost
            StaticFaders[2] = -8;  // Blue reduction
        }

        /// <summary>
        /// Create a cool color fade (emphasize blue/cyan)
        /// </summary>
        public void SetCoolFade()
        {
            StaticFaders[0] = -8;  // Red reduction
            StaticFaders[1] = 8;   // Green slight boost
            StaticFaders[2] = 16;  // Blue boost
        }

        /// <summary>
        /// Create a high contrast fade
        /// </summary>
        public void SetHighContrastFade()
        {
            StaticFaders[0] = 24;  // Red boost
            StaticFaders[1] = 0;   // Green neutral
            StaticFaders[2] = -24; // Blue reduction
        }

        #endregion
    }
}
