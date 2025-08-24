using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Clear Frame Effects Node - Multiple clearing modes with patterns and beat reactivity
    /// Provides frame clearing capabilities for clean visual transitions and background effects
    /// </summary>
    public class ClearFrameEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Clear mode (0: Full, 1: Pattern, 2: Gradient, 3: Radial, 4: Wave, 5: Noise, 6: Custom)
        /// </summary>
        public int ClearMode { get; set; } = 0;

        /// <summary>
        /// Color used for clearing operations (RGB format)
        /// </summary>
        public int ClearColor { get; set; } = 0x000000;

        /// <summary>
        /// Clear pattern (0: Solid, 1: Checkerboard, 2: Stripes, 3: Dots, 4: Lines, 5: Waves, 6: Noise, 7: Custom)
        /// </summary>
        public int ClearPattern { get; set; } = 0;

        /// <summary>
        /// Enable dynamic clearing on beat detection
        /// </summary>
        public bool BeatReactive { get; set; } = false;

        /// <summary>
        /// Clear timing (0: Every frame, 1: On beat, 2: Every few frames, 3: Random, 4: Custom)
        /// </summary>
        public int ClearTiming { get; set; } = 0;

        /// <summary>
        /// Opacity of clearing operations (0.0 to 1.0)
        /// </summary>
        public float ClearOpacity { get; set; } = 1.0f;

        /// <summary>
        /// Enable pattern-based clearing
        /// </summary>
        public bool EnablePatterns { get; set; } = true;

        /// <summary>
        /// Density of patterns (1-100)
        /// </summary>
        public int PatternDensity { get; set; } = 50;

        /// <summary>
        /// Scale factor for patterns (0.1 to 10.0)
        /// </summary>
        public float PatternScale { get; set; } = 1.0f;

        /// <summary>
        /// Enable animated patterns
        /// </summary>
        public bool AnimatedPattern { get; set; } = false;

        /// <summary>
        /// Speed of pattern animation (0.1 to 10.0)
        /// </summary>
        public float AnimationSpeed { get; set; } = 1.0f;

        /// <summary>
        /// Blend mode for clearing (0: Replace, 1: Add, 2: Multiply, 3: Screen, 4: Overlay, 5: Alpha)
        /// </summary>
        public int ClearBlendMode { get; set; } = 0;

        /// <summary>
        /// Enable fade effects
        /// </summary>
        public bool EnableFade { get; set; } = true;

        /// <summary>
        /// Fade rate (0.0 to 1.0, higher = faster fade)
        /// </summary>
        public float FadeRate { get; set; } = 0.95f;

        /// <summary>
        /// Enable transition effects
        /// </summary>
        public bool EnableTransition { get; set; } = false;

        /// <summary>
        /// Speed of transitions (0.1 to 10.0)
        /// </summary>
        public float TransitionSpeed { get; set; } = 1.0f;

        #endregion

        #region Internal State

        private readonly Random _random;
        private int _frameCount;
        private double _currentTime;
        private double _lastBeatTime;
        private float _fadeProgress;

        #endregion

        #region Constants

        private const int MAX_CLEAR_MODES = 7;
        private const int MAX_PATTERNS = 8;
        private const int MAX_BLEND_MODES = 6;
        private const float MIN_PATTERN_SCALE = 0.1f;
        private const float MAX_PATTERN_SCALE = 10.0f;
        private const float MIN_ANIMATION_SPEED = 0.1f;
        private const float MAX_ANIMATION_SPEED = 10.0f;

        #endregion

        #region Constructor

        public ClearFrameEffectsNode()
        {
            Name = "Clear Frame";
            Description = "Multiple clearing modes with patterns and beat reactivity for clean visual transitions";
            Category = "AVS Effects";
            
            _random = new Random();
            _frameCount = 0;
            _currentTime = 0.0;
            _lastBeatTime = 0.0;
            _fadeProgress = 0.0f;
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Input image for clearing operations"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Cleared output image"));
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
            
            // Update internal state
            UpdateState(audioFeatures);
            
            // Check if we should clear this frame
            if (ShouldClear(audioFeatures))
            {
                // Apply clearing based on mode
                ApplyClearing(output);
                
                // Apply blend mode if not replacing
                if (ClearBlendMode != 0)
                {
                    ApplyBlendMode(imageBuffer, output);
                }
                
                // Apply fade effect if enabled
                if (EnableFade)
                {
                    ApplyFadeEffect(output);
                }
                
                // Apply transition effect if enabled
                if (EnableTransition)
                {
                    ApplyTransitionEffect(output, imageBuffer);
                }
            }
            else
            {
                // Copy input to output without clearing
                CopyImage(imageBuffer, output);
            }
            
            return output;
        }

        #endregion

        #region State Management

        private void UpdateState(AudioFeatures audioFeatures)
        {
            _frameCount++;
            _currentTime += 0.016; // Assuming 60 FPS
            
            if (audioFeatures?.IsBeat == true)
            {
                _lastBeatTime = audioFeatures.Timestamp;
            }
            
            // Update fade progress
            if (EnableFade)
            {
                _fadeProgress += FadeRate * 0.016f;
                if (_fadeProgress > 1.0f) _fadeProgress = 1.0f;
            }
        }

        private bool ShouldClear(AudioFeatures audioFeatures)
        {
            switch (ClearTiming)
            {
                case 0: // Every frame
                    return true;
                case 1: // On beat only
                    return BeatReactive && audioFeatures?.IsBeat == true;
                case 2: // Every few frames
                    return (_frameCount % 3) == 0;
                case 3: // Random intervals
                    return _random.Next(100) < 10; // 10% chance
                case 4: // Custom timing
                    return EvaluateCustomTiming(audioFeatures);
                default:
                    return true;
            }
        }

        private bool EvaluateCustomTiming(AudioFeatures audioFeatures)
        {
            // Custom timing based on audio intensity
            if (audioFeatures?.Rms > 0)
            {
                float intensity = audioFeatures.Rms / 255.0f;
                return intensity > 0.7f; // Clear when audio is loud
            }
            return false;
        }

        #endregion

        #region Clearing Operations

        private void ApplyClearing(ImageBuffer output)
        {
            switch (ClearMode)
            {
                case 0: // Full Clear
                    ApplyFullClear(output);
                    break;
                case 1: // Pattern Clear
                    ApplyPatternClear(output);
                    break;
                case 2: // Gradient Clear
                    ApplyGradientClear(output);
                    break;
                case 3: // Radial Clear
                    ApplyRadialClear(output);
                    break;
                case 4: // Wave Clear
                    ApplyWaveClear(output);
                    break;
                case 5: // Noise Clear
                    ApplyNoiseClear(output);
                    break;
                case 6: // Custom Clear
                    ApplyCustomClear(output);
                    break;
                default:
                    ApplyFullClear(output);
                    break;
            }
        }

        private void ApplyFullClear(ImageBuffer output)
        {
            int clearColor = ClearColor;
            
            // Apply opacity if not fully opaque
            if (ClearOpacity < 1.0f)
            {
                clearColor = ApplyOpacity(clearColor, ClearOpacity);
            }
            
            // Fill entire buffer
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    output.SetPixel(x, y, clearColor);
                }
            }
        }

        private void ApplyPatternClear(ImageBuffer output)
        {
            if (!EnablePatterns)
            {
                ApplyFullClear(output);
                return;
            }

            float currentTime = (float)_currentTime;
            
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    int patternValue = CalculatePatternValue(x, y, currentTime);
                    int pixelColor = ApplyPatternToColor(ClearColor, patternValue);
                    
                    if (ClearOpacity < 1.0f)
                    {
                        pixelColor = ApplyOpacity(pixelColor, ClearOpacity);
                    }
                    
                    output.SetPixel(x, y, pixelColor);
                }
            }
        }

        private int CalculatePatternValue(int x, int y, float time)
        {
            switch (ClearPattern)
            {
                case 1: // Checkerboard
                    return CalculateCheckerboardPattern(x, y, time);
                case 2: // Stripes
                    return CalculateStripePattern(x, y, time);
                case 3: // Dots
                    return CalculateDotPattern(x, y, time);
                case 4: // Lines
                    return CalculateLinePattern(x, y, time);
                case 5: // Waves
                    return CalculateWavePattern(x, y, time);
                case 6: // Noise
                    return CalculateNoisePattern(x, y, time);
                default: // Solid
                    return 255;
            }
        }

        private int CalculateCheckerboardPattern(int x, int y, float time)
        {
            int tileSize = Math.Max(1, 100 / PatternDensity);
            bool isEvenTile = ((x / tileSize) + (y / tileSize)) % 2 == 0;
            
            if (AnimatedPattern)
            {
                float animation = (float)(Math.Sin(time * AnimationSpeed) * 0.5 + 0.5);
                isEvenTile = ((x / tileSize) + (y / tileSize) + (int)(animation * 10)) % 2 == 0;
            }
            
            return isEvenTile ? 255 : 0;
        }

        private int CalculateStripePattern(int x, int y, float time)
        {
            int stripeWidth = Math.Max(1, 200 / PatternDensity);
            int stripeType = (x / stripeWidth) % 2;
            
            if (AnimatedPattern)
            {
                float offset = (time * AnimationSpeed * 50) % stripeWidth;
                stripeType = ((int)(x + offset) / stripeWidth) % 2;
            }
            
            return stripeType == 0 ? 255 : 0;
        }

        private int CalculateDotPattern(int x, int y, float time)
        {
            int dotSize = Math.Max(1, 300 / PatternDensity);
            int dotX = x % dotSize;
            int dotY = y % dotSize;
            
            bool isDot = (dotX < dotSize / 2) && (dotY < dotSize / 2);
            
            if (AnimatedPattern)
            {
                float pulse = (float)(Math.Sin(time * AnimationSpeed) * 0.5 + 0.5);
                dotSize = (int)(dotSize * (0.5f + pulse * 0.5f));
                isDot = (dotX < dotSize / 2) && (dotY < dotSize / 2);
            }
            
            return isDot ? 255 : 0;
        }

        private int CalculateLinePattern(int x, int y, float time)
        {
            int lineSpacing = Math.Max(1, 400 / PatternDensity);
            int lineType = (x + y) % lineSpacing;
            
            if (AnimatedPattern)
            {
                float rotation = time * AnimationSpeed;
                float rotatedX = (float)(x * Math.Cos(rotation) - y * Math.Sin(rotation));
                lineType = ((int)rotatedX) % lineSpacing;
            }
            
            return lineType < lineSpacing / 4 ? 255 : 0;
        }

        private int CalculateWavePattern(int x, int y, float time)
        {
            // Use default dimensions for pattern calculation
            float normalizedX = (float)x / 800.0f; // Default width
            float normalizedY = (float)y / 600.0f; // Default height
            
            float wave1 = (float)(Math.Sin(normalizedX * Math.PI * 4 + time * AnimationSpeed));
            float wave2 = (float)(Math.Sin(normalizedY * Math.PI * 3 + time * AnimationSpeed * 0.7));
            
            float combinedWave = (wave1 + wave2) * 0.5f;
            
            // Convert to 0-255 range
            int patternValue = (int)((combinedWave + 1.0f) * 127.5f);
            
            return patternValue;
        }

        private int CalculateNoisePattern(int x, int y, float time)
        {
            if (AnimatedPattern)
            {
                // Animated noise using time
                int seed = (int)((x * 73856093) ^ (y * 19349663) ^ ((int)(time * 1000)));
                return new Random(seed).Next(256);
            }
            
            return _random.Next(256);
        }

        private int ApplyPatternToColor(int baseColor, int patternValue)
        {
            if (patternValue == 0) return 0x000000;
            if (patternValue == 255) return baseColor;
            
            // Interpolate between black and base color
            float factor = patternValue / 255.0f;
            return InterpolateColor(0x000000, baseColor, factor);
        }

        private void ApplyGradientClear(ImageBuffer output)
        {
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    float normalizedX = (float)x / output.Width;
                    float normalizedY = (float)y / output.Height;
                    
                    // Create gradient from top-left to bottom-right
                    float gradientValue = (normalizedX + normalizedY) * 0.5f;
                    
                    int pixelColor = InterpolateColor(ClearColor, 0xFFFFFF, gradientValue);
                    
                    if (ClearOpacity < 1.0f)
                    {
                        pixelColor = ApplyOpacity(pixelColor, ClearOpacity);
                    }
                    
                    output.SetPixel(x, y, pixelColor);
                }
            }
        }

        private void ApplyRadialClear(ImageBuffer output)
        {
            float centerX = output.Width / 2.0f;
            float centerY = output.Height / 2.0f;
            float maxRadius = (float)Math.Sqrt(centerX * centerX + centerY * centerY);
            
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    float dx = x - centerX;
                    float dy = y - centerY;
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                    float normalizedDistance = distance / maxRadius;
                    
                    int pixelColor = InterpolateColor(ClearColor, 0x000000, normalizedDistance);
                    
                    if (ClearOpacity < 1.0f)
                    {
                        pixelColor = ApplyOpacity(pixelColor, ClearOpacity);
                    }
                    
                    output.SetPixel(x, y, pixelColor);
                }
            }
        }

        private void ApplyWaveClear(ImageBuffer output)
        {
            float currentTime = (float)_currentTime;
            
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    float normalizedX = (float)x / output.Width;
                    float normalizedY = (float)y / output.Height;
                    
                    float wave = (float)(Math.Sin(normalizedX * Math.PI * 4 + currentTime * AnimationSpeed) * 
                                       Math.Sin(normalizedY * Math.PI * 3 + currentTime * AnimationSpeed * 0.7));
                    
                    float waveValue = (wave + 1.0f) * 0.5f;
                    int pixelColor = InterpolateColor(0x000000, ClearColor, waveValue);
                    
                    if (ClearOpacity < 1.0f)
                    {
                        pixelColor = ApplyOpacity(pixelColor, ClearOpacity);
                    }
                    
                    output.SetPixel(x, y, pixelColor);
                }
            }
        }

        private void ApplyNoiseClear(ImageBuffer output)
        {
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    int noiseValue = _random.Next(256);
                    int pixelColor = InterpolateColor(0x000000, ClearColor, noiseValue / 255.0f);
                    
                    if (ClearOpacity < 1.0f)
                    {
                        pixelColor = ApplyOpacity(pixelColor, ClearOpacity);
                    }
                    
                    output.SetPixel(x, y, pixelColor);
                }
            }
        }

        private void ApplyCustomClear(ImageBuffer output)
        {
            // Custom clearing logic - could be extended with user-defined patterns
            ApplyPatternClear(output);
        }

        #endregion

        #region Blending and Effects

        private void ApplyBlendMode(ImageBuffer source, ImageBuffer destination)
        {
            for (int y = 0; y < destination.Height; y++)
            {
                for (int x = 0; x < destination.Width; x++)
                {
                    int sourceColor = source.GetPixel(x, y);
                    int destColor = destination.GetPixel(x, y);
                    int blendedColor = BlendColors(sourceColor, destColor, ClearBlendMode);
                    destination.SetPixel(x, y, blendedColor);
                }
            }
        }

        private int BlendColors(int color1, int color2, int blendMode)
        {
            switch (blendMode)
            {
                case 1: // Add
                    return AddColors(color1, color2);
                case 2: // Multiply
                    return MultiplyColors(color1, color2);
                case 3: // Screen
                    return ScreenColors(color1, color2);
                case 4: // Overlay
                    return OverlayColors(color1, color2);
                case 5: // Alpha
                    return AlphaBlendColors(color1, color2, ClearOpacity);
                default:
                    return color1;
            }
        }

        private int AddColors(int color1, int color2)
        {
            int r1 = (color1 >> 16) & 0xFF;
            int g1 = (color1 >> 8) & 0xFF;
            int b1 = color1 & 0xFF;
            
            int r2 = (color2 >> 16) & 0xFF;
            int g2 = (color2 >> 8) & 0xFF;
            int b2 = (color2 >> 8) & 0xFF;
            
            int r = Math.Min(255, r1 + r2);
            int g = Math.Min(255, g1 + g2);
            int b = Math.Min(255, b1 + b2);
            
            return (r << 16) | (g << 8) | b;
        }

        private int MultiplyColors(int color1, int color2)
        {
            int r1 = (color1 >> 16) & 0xFF;
            int g1 = (color1 >> 8) & 0xFF;
            int b1 = color1 & 0xFF;
            
            int r2 = (color2 >> 16) & 0xFF;
            int g2 = (color2 >> 8) & 0xFF;
            int b2 = (color2 >> 8) & 0xFF;
            
            int r = (r1 * r2) / 255;
            int g = (g1 * g2) / 255;
            int b = (b1 * b2) / 255;
            
            return (r << 16) | (g << 8) | b;
        }

        private int ScreenColors(int color1, int color2)
        {
            int r1 = (color1 >> 16) & 0xFF;
            int g1 = (color1 >> 8) & 0xFF;
            int b1 = color1 & 0xFF;
            
            int r2 = (color2 >> 16) & 0xFF;
            int g2 = (color2 >> 8) & 0xFF;
            int b2 = (color2 >> 8) & 0xFF;
            
            int r = 255 - ((255 - r1) * (255 - r2)) / 255;
            int g = 255 - ((255 - g1) * (255 - g2)) / 255;
            int b = 255 - ((255 - b1) * (255 - b2)) / 255;
            
            return (r << 16) | (g << 8) | b;
        }

        private int OverlayColors(int color1, int color2)
        {
            int r1 = (color1 >> 16) & 0xFF;
            int g1 = (color1 >> 8) & 0xFF;
            int b1 = color1 & 0xFF;
            
            int r2 = (color2 >> 16) & 0xFF;
            int g2 = (color2 >> 8) & 0xFF;
            int b2 = (color2 >> 8) & 0xFF;
            
            int r = r2 < 128 ? (2 * r1 * r2) / 255 : 255 - (2 * (255 - r1) * (255 - r2)) / 255;
            int g = g2 < 128 ? (2 * g1 * g2) / 255 : 255 - (2 * (255 - g1) * (255 - g2)) / 255;
            int b = b2 < 128 ? (2 * b1 * b2) / 255 : 255 - (2 * (255 - b1) * (255 - b2)) / 255;
            
            return (r << 16) | (g << 8) | b;
        }

        private int AlphaBlendColors(int color1, int color2, float alpha)
        {
            int r1 = (color1 >> 16) & 0xFF;
            int g1 = (color1 >> 8) & 0xFF;
            int b1 = color1 & 0xFF;
            
            int r2 = (color2 >> 16) & 0xFF;
            int g2 = (color2 >> 8) & 0xFF;
            int b2 = (color2 >> 8) & 0xFF;
            
            int r = (int)(r1 * alpha + r2 * (1 - alpha));
            int g = (int)(g1 * alpha + g2 * (1 - alpha));
            int b = (int)(b1 * alpha + b2 * (1 - alpha));
            
            return (r << 16) | (g << 8) | b;
        }

        private void ApplyFadeEffect(ImageBuffer output)
        {
            if (_fadeProgress >= 1.0f) return;
            
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    int pixelColor = output.GetPixel(x, y);
                    int fadedColor = ApplyOpacity(pixelColor, 1.0f - _fadeProgress);
                    output.SetPixel(x, y, fadedColor);
                }
            }
        }

        private void ApplyTransitionEffect(ImageBuffer output, ImageBuffer source)
        {
            float transitionProgress = (float)((_currentTime * TransitionSpeed) % 1.0);
            
            for (int y = 0; y < output.Height; y++)
            {
                for (int x = 0; x < output.Width; x++)
                {
                    int sourceColor = source.GetPixel(x, y);
                    int destColor = output.GetPixel(x, y);
                    int transitionColor = InterpolateColor(sourceColor, destColor, transitionProgress);
                    output.SetPixel(x, y, transitionColor);
                }
            }
        }

        #endregion

        #region Utility Methods

        private int ApplyOpacity(int color, float opacity)
        {
            if (opacity >= 1.0f) return color;
            if (opacity <= 0.0f) return 0x000000;
            
            int r = (color >> 16) & 0xFF;
            int g = (color >> 8) & 0xFF;
            int b = color & 0xFF;
            
            r = (int)(r * opacity);
            g = (int)(g * opacity);
            b = (int)(b * opacity);
            
            return (r << 16) | (g << 8) | b;
        }

        private int InterpolateColor(int color1, int color2, float factor)
        {
            int r1 = (color1 >> 16) & 0xFF;
            int g1 = (color1 >> 8) & 0xFF;
            int b1 = color1 & 0xFF;
            
            int r2 = (color2 >> 16) & 0xFF;
            int g2 = (color2 >> 8) & 0xFF;
            int b2 = (color2 >> 8) & 0xFF;
            
            int r = (int)(r1 + (r2 - r1) * factor);
            int g = (int)(g1 + (g2 - g1) * factor);
            int b = (int)(b1 + (b2 - b1) * factor);
            
            r = Math.Max(0, Math.Min(255, r));
            g = Math.Max(0, Math.Min(255, g));
            b = Math.Max(0, Math.Min(255, b));
            
            return (r << 16) | (g << 8) | b;
        }

        private void CopyImage(ImageBuffer source, ImageBuffer destination)
        {
            for (int y = 0; y < destination.Height; y++)
            {
                for (int x = 0; x < destination.Width; x++)
                {
                    destination.SetPixel(x, y, source.GetPixel(x, y));
                }
            }
        }

        #endregion

        #region Default Output

        protected override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }

        #endregion
    }
}
