using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Video Delay Effects Node - Advanced frame buffering with beat-reactive delay
    /// Based on Winamp AVS C_DELAY class with sophisticated frame management
    /// </summary>
    public class VideoDelayEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Video Delay effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Use beat-synchronized delay mode
        /// </summary>
        public bool UseBeats { get; set; } = false;

        /// <summary>
        /// Delay amount: frames (1-200) or beats (1-16)
        /// </summary>
        public int Delay { get; set; } = 10;

        /// <summary>
        /// Enable beat-reactive behavior
        /// </summary>
        public bool BeatReactive { get; set; } = true;

        /// <summary>
        /// Multiplier for beat-reactive delay adjustment
        /// </summary>
        public float BeatDelayMultiplier { get; set; } = 1.0f;

        /// <summary>
        /// Enable delay animation
        /// </summary>
        public bool EnableDelayAnimation { get; set; } = false;

        /// <summary>
        /// Animation speed for delay effects
        /// </summary>
        public float AnimationSpeed { get; set; } = 1.0f;

        /// <summary>
        /// Delay animation mode
        /// </summary>
        public int AnimationMode { get; set; } = 0;

        /// <summary>
        /// Enable delay masking
        /// </summary>
        public bool EnableDelayMasking { get; set; } = false;

        /// <summary>
        /// Mask image for delay control
        /// </summary>
        public ImageBuffer? DelayMask { get; set; }

        /// <summary>
        /// Influence of delay mask (0.0-1.0)
        /// </summary>
        public float MaskInfluence { get; set; } = 1.0f;

        /// <summary>
        /// Enable delay blending
        /// </summary>
        public bool EnableDelayBlending { get; set; } = false;

        /// <summary>
        /// Delay blend strength (0.0-1.0)
        /// </summary>
        public float DelayBlendStrength { get; set; } = 0.5f;

        /// <summary>
        /// Delay algorithm type
        /// </summary>
        public int DelayAlgorithm { get; set; } = 0;

        /// <summary>
        /// Delay curve power (for non-linear delay effects)
        /// </summary>
        public float DelayCurve { get; set; } = 1.0f;

        /// <summary>
        /// Enable delay clamping
        /// </summary>
        public bool EnableDelayClamping { get; set; } = true;

        /// <summary>
        /// Delay clamping mode
        /// </summary>
        public int ClampMode { get; set; } = 0;

        /// <summary>
        /// Enable delay inversion
        /// </summary>
        public bool EnableDelayInversion { get; set; } = false;

        /// <summary>
        /// Inversion threshold (0.0-1.0)
        /// </summary>
        public float InversionThreshold { get; set; } = 0.5f;

        #endregion

        #region Private Fields

        private ImageBuffer[]? _frameBuffer;
        private int _bufferSize = 0;
        private int _currentFrameIndex = 0;
        private int _framesSinceBeat = 0;
        private int _currentFrameDelay = 0;
        private readonly Random _random = new Random();

        #endregion

        #region Constructor

        public VideoDelayEffectsNode()
        {
            Name = "Video Delay Effects";
            Description = "Advanced frame delay with beat synchronization and masking";
            Category = "Temporal Effects";
        }

        #endregion

        #region Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for delay processing"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Delayed output image"));
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

            // Initialize frame buffer if needed
            InitializeFrameBuffer(imageBuffer.Width, imageBuffer.Height);

            // Calculate current frame delay
            var currentDelay = CalculateCurrentDelay(audioFeatures);

            if (currentDelay > 0 && currentDelay < _frameBuffer.Length)
            {
                // Get delayed frame from buffer
                var delayedFrame = GetDelayedFrame(currentDelay);

                // Apply delay masking if enabled
                if (EnableDelayMasking && DelayMask != null)
                {
                    delayedFrame = ApplyDelayMasking(imageBuffer, delayedFrame);
                }

                // Apply delay blending if enabled
                if (EnableDelayBlending)
                {
                    delayedFrame = BlendDelay(imageBuffer, delayedFrame);
                }

                output = delayedFrame;
            }
            else
            {
                // No delay, output original frame
                output = imageBuffer;
            }

            // Store current frame in buffer
            StoreFrameInBuffer(imageBuffer);

            // Update delay state
            UpdateDelayState(audioFeatures);

            return output;
        }

        #endregion

        #region Frame Buffer Management

        private void InitializeFrameBuffer(int width, int height)
        {
            var requiredBufferSize = UseBeats ? Delay * 2 : Delay + 1;

            if (_frameBuffer == null || _bufferSize != requiredBufferSize)
            {
                _bufferSize = requiredBufferSize;
                _frameBuffer = new ImageBuffer[_bufferSize];

                // Initialize buffer with empty frames
                for (int i = 0; i < _bufferSize; i++)
                {
                    _frameBuffer[i] = new ImageBuffer(width, height);
                }

                _currentFrameIndex = 0;
            }
        }

        private void StoreFrameInBuffer(ImageBuffer frame)
        {
            // Store frame at current index
            _frameBuffer[_currentFrameIndex] = frame;

            // Advance buffer index
            _currentFrameIndex = (_currentFrameIndex + 1) % _bufferSize;
        }

        private ImageBuffer GetDelayedFrame(int delay)
        {
            if (DelayAlgorithm == 1) // Enhanced
            {
                return GetEnhancedDelayedFrame(delay);
            }
            else if (DelayAlgorithm == 2) // Realistic
            {
                return GetRealisticDelayedFrame(delay);
            }

            return GetStandardDelayedFrame(delay);
        }

        private ImageBuffer GetStandardDelayedFrame(int delay)
        {
            // Calculate index for delayed frame
            var delayedIndex = (_currentFrameIndex - delay + _bufferSize) % _bufferSize;

            // Return delayed frame
            return _frameBuffer[delayedIndex];
        }

        private ImageBuffer GetEnhancedDelayedFrame(int delay)
        {
            // Multi-frame interpolation for smoother delays
            var frame1 = GetStandardDelayedFrame(delay);
            var frame2 = GetStandardDelayedFrame(delay + 1);

            var interpolationFactor = (delay % 1.0f);
            return InterpolateFrames(frame1, frame2, interpolationFactor);
        }

        private ImageBuffer GetRealisticDelayedFrame(int delay)
        {
            // Realistic delay with motion blur simulation
            var baseFrame = GetStandardDelayedFrame(delay);
            var motionBlur = CalculateMotionBlur(delay);
            return ApplyMotionBlur(baseFrame, motionBlur);
        }

        #endregion

        #region Delay Calculation

        private int CalculateCurrentDelay(AudioFeatures audio)
        {
            if (UseBeats)
            {
                // Beat-synchronized delay mode
                if (BeatReactive && audio != null && audio.IsBeat)
                {
                    // Calculate delay based on frames since last beat
                    _currentFrameDelay = _framesSinceBeat * Delay;
                    _currentFrameDelay = Math.Min(_currentFrameDelay, 400); // Cap at 400 frames
                    _framesSinceBeat = 0;
                }

                _framesSinceBeat++;
                return _currentFrameDelay;
            }
            else
            {
                // Fixed frame delay mode
                return Delay;
            }
        }

        #endregion

        #region Delay Effects

        private ImageBuffer ApplyDelayMasking(ImageBuffer originalFrame, ImageBuffer delayedFrame)
        {
            if (!EnableDelayMasking || DelayMask == null)
                return delayedFrame;

            var maskedFrame = new ImageBuffer(originalFrame.Width, originalFrame.Height);

            for (int y = 0; y < originalFrame.Height; y++)
            {
                for (int x = 0; x < originalFrame.Width; x++)
                {
                    var maskPixel = DelayMask.GetPixel(x, y);
                    var maskIntensity = (maskPixel & 0xFF) / 255.0f; // Use red channel as mask

                    // Blend original and delayed based on mask
                    var blendFactor = maskIntensity * MaskInfluence;
                    var originalPixel = originalFrame.GetPixel(x, y);
                    var delayedPixel = delayedFrame.GetPixel(x, y);

                    var finalPixel = BlendPixels(originalPixel, delayedPixel, blendFactor);
                    maskedFrame.SetPixel(x, y, finalPixel);
                }
            }

            return maskedFrame;
        }

        private ImageBuffer BlendDelay(ImageBuffer originalFrame, ImageBuffer delayedFrame)
        {
            if (!EnableDelayBlending)
                return delayedFrame;

            var blendedFrame = new ImageBuffer(originalFrame.Width, originalFrame.Height);

            for (int y = 0; y < originalFrame.Height; y++)
            {
                for (int x = 0; x < originalFrame.Width; x++)
                {
                    var originalPixel = originalFrame.GetPixel(x, y);
                    var delayedPixel = delayedFrame.GetPixel(x, y);

                    var blendFactor = DelayBlendStrength;
                    var finalPixel = BlendPixels(originalPixel, delayedPixel, blendFactor);
                    blendedFrame.SetPixel(x, y, finalPixel);
                }
            }

            return blendedFrame;
        }

        private int BlendPixels(int pixel1, int pixel2, float blendFactor)
        {
            var r1 = pixel1 & 0xFF;
            var g1 = (pixel1 >> 8) & 0xFF;
            var b1 = (pixel1 >> 16) & 0xFF;

            var r2 = pixel2 & 0xFF;
            var g2 = (pixel2 >> 8) & 0xFF;
            var b2 = (pixel2 >> 16) & 0xFF;

            var r = (int)(r1 + (r2 - r1) * blendFactor);
            var g = (int)(g1 + (g2 - g1) * blendFactor);
            var b = (int)(b1 + (b2 - b1) * blendFactor);

            // Clamp values
            r = Math.Max(0, Math.Min(255, r));
            g = Math.Max(0, Math.Min(255, g));
            b = Math.Max(0, Math.Min(255, b));

            return (b << 16) | (g << 8) | r;
        }

        private ImageBuffer InterpolateFrames(ImageBuffer frame1, ImageBuffer frame2, float factor)
        {
            var interpolatedFrame = new ImageBuffer(frame1.Width, frame1.Height);

            for (int y = 0; y < frame1.Height; y++)
            {
                for (int x = 0; x < frame1.Width; x++)
                {
                    var pixel1 = frame1.GetPixel(x, y);
                    var pixel2 = frame2.GetPixel(x, y);

                    var interpolatedPixel = BlendPixels(pixel1, pixel2, factor);
                    interpolatedFrame.SetPixel(x, y, interpolatedPixel);
                }
            }

            return interpolatedFrame;
        }

        private ImageBuffer CalculateMotionBlur(int delay)
        {
            // Simple motion blur calculation - could be enhanced
            return GetStandardDelayedFrame(delay);
        }

        private ImageBuffer ApplyMotionBlur(ImageBuffer frame, ImageBuffer motionBlur)
        {
            // Simple motion blur application
            return BlendDelay(frame, motionBlur);
        }

        #endregion

        #region Delay Animation

        private void UpdateDelayState(AudioFeatures audio)
        {
            if (!EnableDelayAnimation)
                return;

            var animationProgress = (GetCurrentTime() * AnimationSpeed) % (Math.PI * 2);

            switch (AnimationMode)
            {
                case 0: // Pulsing delay
                    var pulse = (Math.Sin(animationProgress) + 1.0f) * 0.5f;
                    Delay = (int)(5 + pulse * 25); // 5-30 frame range
                    break;

                case 1: // Oscillating delay
                    var oscillation = Math.Sin(animationProgress * 2);
                    if (UseBeats)
                    {
                        Delay = (int)(8 + oscillation * 8); // 0-16 beat range
                    }
                    else
                    {
                        Delay = (int)(50 + oscillation * 150); // 50-200 frame range
                    }
                    break;

                case 2: // Random delay
                    if (_random.NextDouble() < 0.01f) // 1% chance per frame
                    {
                        if (UseBeats)
                        {
                            Delay = _random.Next(1, 17); // 1-16 beats
                        }
                        else
                        {
                            Delay = _random.Next(10, 201); // 10-200 frames
                        }
                    }
                    break;

                case 3: // Wave pattern delay
                    var wave = Math.Sin(animationProgress * 3);
                    var baseDelay = UseBeats ? 8 : 100;
                    var waveRange = UseBeats ? 8 : 100;
                    Delay = (int)(baseDelay + wave * waveRange);
                    break;
            }
        }

        private double GetCurrentTime()
        {
            return DateTime.Now.Ticks / (double)TimeSpan.TicksPerSecond;
        }

        #endregion
    }
}

