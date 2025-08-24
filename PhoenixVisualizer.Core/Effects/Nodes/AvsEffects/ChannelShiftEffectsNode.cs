using System;
using System.Collections.Generic;
using System.Linq;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Channel Shift Effects Node - RGB channel manipulation with beat reactivity
    /// Implements six different RGB channel permutation modes with smooth transitions
    /// </summary>
    public class ChannelShiftEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Channel arrangement mode (0-5)
        /// 0: RGB, 1: RBG, 2: BRG, 3: BGR, 4: GBR, 5: GRB
        /// </summary>
        public int ChannelMode { get; set; } = 0;

        /// <summary>
        /// Enable automatic mode switching on audio beats
        /// </summary>
        public bool BeatReactive { get; set; } = true;

        /// <summary>
        /// Beat reaction mode (0: none, 1: random, 2: pattern, 3: frequency-based)
        /// </summary>
        public int BeatRandomMode { get; set; } = 1;

        /// <summary>
        /// Intensity of channel manipulation (0.0 to 1.0)
        /// </summary>
        public float ChannelIntensity { get; set; } = 1.0f;

        /// <summary>
        /// Enable smooth transitions between channel modes
        /// </summary>
        public bool EnableSmoothTransition { get; set; } = false;

        /// <summary>
        /// Speed of smooth transitions (1.0 = normal speed)
        /// </summary>
        public float TransitionSpeed { get; set; } = 1.0f;

        /// <summary>
        /// Custom channel order for advanced manipulation
        /// </summary>
        public int CustomChannelOrder { get; set; } = 0;

        /// <summary>
        /// Enable channel masking for selective manipulation
        /// </summary>
        public bool EnableChannelMasking { get; set; } = false;

        /// <summary>
        /// Red channel mask (0x00 to 0xFF)
        /// </summary>
        public int RedChannelMask { get; set; } = 0xFF;

        /// <summary>
        /// Green channel mask (0x00 to 0xFF)
        /// </summary>
        public int GreenChannelMask { get; set; } = 0xFF;

        /// <summary>
        /// Blue channel mask (0x00 to 0xFF)
        /// </summary>
        public int BlueChannelMask { get; set; } = 0xFF;

        /// <summary>
        /// Channel blending factor (0.0 to 1.0)
        /// </summary>
        public float ChannelBlend { get; set; } = 1.0f;

        /// <summary>
        /// Enable animated channel mode changes
        /// </summary>
        public bool EnableChannelAnimation { get; set; } = false;

        /// <summary>
        /// Speed of channel animation (1.0 = normal speed)
        /// </summary>
        public float AnimationSpeed { get; set; } = 1.0f;

        /// <summary>
        /// Animation mode (0: rotating, 1: pulsing, 2: wave, 3: random walk)
        /// </summary>
        public int AnimationMode { get; set; } = 0;

        /// <summary>
        /// Enable channel inversion effects
        /// </summary>
        public bool EnableChannelInversion { get; set; } = false;

        /// <summary>
        /// Strength of channel inversion (0.0 to 1.0)
        /// </summary>
        public float InversionStrength { get; set; } = 0.5f;

        /// <summary>
        /// Enable channel value clamping
        /// </summary>
        public bool EnableChannelClamping { get; set; } = true;

        /// <summary>
        /// Clamping mode (0: clamp to 0-255, 1: wrap around, 2: mirror)
        /// </summary>
        public int ClampMode { get; set; } = 0;

        /// <summary>
        /// Enable channel quantization
        /// </summary>
        public bool EnableChannelQuantization { get; set; } = false;

        /// <summary>
        /// Number of quantization levels (2-256)
        /// </summary>
        public int QuantizationLevels { get; set; } = 256;

        #endregion

        #region Internal State

        private readonly Random _random;
        private int _currentChannelMode;
        private int _targetChannelMode;
        private float _transitionProgress;
        private double _lastBeatTime;
        private double _animationTime;

        #endregion

        #region Constants

        private const int MAX_CHANNEL_MODES = 6;
        private const float TRANSITION_THRESHOLD = 0.99f;
        private const int DEFAULT_QUANTIZATION_LEVELS = 256;

        #endregion

        #region Constructor

        public ChannelShiftEffectsNode()
        {
            Name = "Channel Shift";
            Description = "Manipulates RGB color channels with beat-reactive mode switching";
            Category = "AVS Effects";
            
            _random = new Random();
            _currentChannelMode = ChannelMode;
            _targetChannelMode = ChannelMode;
            _transitionProgress = 0.0f;
            _lastBeatTime = 0.0;
            _animationTime = 0.0;
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Input image for channel manipulation"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Image with applied channel shifts"));
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
            
            // Update channel mode based on beat reactivity and animation
            UpdateChannelMode(audioFeatures);
            
            // Apply channel shift transformation
            ApplyChannelShift(imageBuffer, output);
            
            return output;
        }

        #endregion

        #region Channel Mode Management

        private void UpdateChannelMode(AudioFeatures audioFeatures)
        {
            // Handle beat reactivity
            if (BeatReactive && audioFeatures?.IsBeat == true)
            {
                _targetChannelMode = GetBeatReactiveMode(audioFeatures);
                _transitionProgress = 0.0f;
                _lastBeatTime = audioFeatures.Timestamp;
            }

            // Handle channel animation
            if (EnableChannelAnimation)
            {
                UpdateChannelAnimation();
            }

            // Update smooth transitions
            if (EnableSmoothTransition && _currentChannelMode != _targetChannelMode)
            {
                _transitionProgress += TransitionSpeed * 0.016f; // Assuming 60 FPS
                if (_transitionProgress >= TRANSITION_THRESHOLD)
                {
                    _currentChannelMode = _targetChannelMode;
                    _transitionProgress = 1.0f;
                }
            }
            else
            {
                _currentChannelMode = _targetChannelMode;
                _transitionProgress = 1.0f;
            }
        }

        private int GetBeatReactiveMode(AudioFeatures audioFeatures)
        {
            switch (BeatRandomMode)
            {
                case 1: // Random mode
                    return _random.Next(0, MAX_CHANNEL_MODES);
                case 2: // Beat-based pattern
                    var beatIntensity = audioFeatures.Rms / 255.0f;
                    return (_currentChannelMode + (int)(beatIntensity * 3)) % MAX_CHANNEL_MODES;
                case 3: // Audio frequency based
                    if (audioFeatures.SpectrumData != null && audioFeatures.SpectrumData.Length > 0)
                    {
                        var freqIndex = (int)(audioFeatures.SpectrumData[0] * MAX_CHANNEL_MODES);
                        return Math.Max(0, Math.Min(MAX_CHANNEL_MODES - 1, freqIndex));
                    }
                    return _currentChannelMode;
                default:
                    return ChannelMode;
            }
        }

        private void UpdateChannelAnimation()
        {
            _animationTime += AnimationSpeed * 0.016f; // Assuming 60 FPS
            
            switch (AnimationMode)
            {
                case 0: // Rotating
                    var rotationProgress = (_animationTime % (Math.PI * 2)) / (Math.PI * 2);
                    _targetChannelMode = (int)(rotationProgress * MAX_CHANNEL_MODES) % MAX_CHANNEL_MODES;
                    break;
                case 1: // Pulsing
                    var pulse = (float)((Math.Sin(_animationTime) + 1) * 0.5);
                    ChannelIntensity = 0.5f + pulse * 0.5f;
                    break;
                case 2: // Wave
                    var wave = Math.Sin(_animationTime * 2);
                    _targetChannelMode = (int)((wave + 1) * 3) % MAX_CHANNEL_MODES;
                    break;
                case 3: // Random walk
                    if (_random.NextDouble() < 0.01f) // 1% chance per frame
                    {
                        _targetChannelMode = _random.Next(0, MAX_CHANNEL_MODES);
                    }
                    break;
            }
        }

        #endregion

        #region Channel Shift Processing

        private void ApplyChannelShift(ImageBuffer input, ImageBuffer output)
        {
            int width = input.Width;
            int height = input.Height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int sourceColor = input.GetPixel(x, y);
                    int processedColor = ProcessPixel(sourceColor);
                    output.SetPixel(x, y, processedColor);
                }
            }
        }

        private int ProcessPixel(int color)
        {
            // Apply channel shift
            int shiftedColor = ApplyChannelShift(color, _currentChannelMode);
            
            // Apply smooth transition if enabled
            if (EnableSmoothTransition && _transitionProgress < 1.0f)
            {
                int targetColor = ApplyChannelShift(color, _targetChannelMode);
                shiftedColor = ApplySmoothTransition(shiftedColor, targetColor, _transitionProgress);
            }
            
            // Apply channel masking
            if (EnableChannelMasking)
            {
                shiftedColor = ApplyChannelMasking(shiftedColor);
            }
            
            // Apply channel quantization
            if (EnableChannelQuantization)
            {
                shiftedColor = ApplyChannelQuantization(shiftedColor);
            }
            
            return shiftedColor;
        }

        private int ApplyChannelShift(int color, int mode)
        {
            int r = (color >> 16) & 0xFF;
            int g = (color >> 8) & 0xFF;
            int b = color & 0xFF;

            int newR, newG, newB;

            switch (mode)
            {
                case 0: // RGB (no change)
                    newR = r; newG = g; newB = b;
                    break;
                case 1: // RBG (swap blue and green)
                    newR = r; newG = b; newB = g;
                    break;
                case 2: // BRG (rotate left)
                    newR = b; newG = r; newB = g;
                    break;
                case 3: // BGR (swap red and blue)
                    newR = b; newG = g; newB = r;
                    break;
                case 4: // GBR (rotate right)
                    newR = g; newG = b; newB = r;
                    break;
                case 5: // GRB (swap red and green)
                    newR = g; newG = r; newB = b;
                    break;
                default:
                    newR = r; newG = g; newB = b;
                    break;
            }

            // Apply channel intensity
            if (ChannelIntensity < 1.0f)
            {
                newR = (int)(r + (newR - r) * ChannelIntensity);
                newG = (int)(g + (newG - g) * ChannelIntensity);
                newB = (int)(b + (newB - b) * ChannelIntensity);
            }

            // Apply channel blend
            if (ChannelBlend < 1.0f)
            {
                newR = (int)(r + (newR - r) * ChannelBlend);
                newG = (int)(g + (newG - g) * ChannelBlend);
                newB = (int)(b + (newB - b) * ChannelBlend);
            }

            return (newR << 16) | (newG << 8) | newB;
        }

        private int ApplySmoothTransition(int color1, int color2, float progress)
        {
            if (progress <= 0.0f) return color1;
            if (progress >= 1.0f) return color2;

            // Extract channels
            int r1 = (color1 >> 16) & 0xFF;
            int g1 = (color1 >> 8) & 0xFF;
            int b1 = color1 & 0xFF;

            int r2 = (color2 >> 16) & 0xFF;
            int g2 = (color2 >> 8) & 0xFF;
            int b2 = color2 & 0xFF;

            // Interpolate channels
            int r = (int)(r1 + (r2 - r1) * progress);
            int g = (int)(g1 + (g2 - g1) * progress);
            int b = (int)(b1 + (b2 - b1) * progress);

            // Clamp values
            r = Math.Max(0, Math.Min(255, r));
            g = Math.Max(0, Math.Min(255, g));
            b = Math.Max(0, Math.Min(255, b));

            return (r << 16) | (g << 8) | b;
        }

        private int ApplyChannelMasking(int color)
        {
            int r = (color >> 16) & 0xFF;
            int g = (color >> 8) & 0xFF;
            int b = color & 0xFF;

            // Apply masks
            r = r & RedChannelMask;
            g = g & GreenChannelMask;
            b = b & BlueChannelMask;

            // Handle channel inversion
            if (EnableChannelInversion)
            {
                r = (int)(r + (255 - r) * (float)InversionStrength);
                g = (int)(g + (255 - g) * (float)InversionStrength);
                b = (int)(b + (255 - b) * (float)InversionStrength);
            }

            // Apply clamping
            if (EnableChannelClamping)
            {
                r = ApplyClamping(r);
                g = ApplyClamping(g);
                b = ApplyClamping(b);
            }

            return (r << 16) | (g << 8) | b;
        }

        private int ApplyClamping(int value)
        {
            switch (ClampMode)
            {
                case 0: // Clamp to 0-255
                    return Math.Max(0, Math.Min(255, value));
                case 1: // Wrap around
                    return ((value % 256) + 256) % 256;
                case 2: // Mirror
                    value = Math.Abs(value);
                    if (value > 255)
                    {
                        value = 510 - value;
                    }
                    return Math.Max(0, Math.Min(255, value));
                default:
                    return Math.Max(0, Math.Min(255, value));
            }
        }

        private int ApplyChannelQuantization(int color)
        {
            if (QuantizationLevels <= 1) return color;

            int levels = Math.Max(2, Math.Min(DEFAULT_QUANTIZATION_LEVELS, QuantizationLevels));
            int step = 256 / levels;

            int r = (color >> 16) & 0xFF;
            int g = (color >> 8) & 0xFF;
            int b = color & 0xFF;

            // Quantize each channel
            r = (r / step) * step;
            g = (g / step) * step;
            b = (b / step) * step;

            return (r << 16) | (g << 8) | b;
        }

        #endregion

        #region Default Output

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }

        #endregion
    }
}
