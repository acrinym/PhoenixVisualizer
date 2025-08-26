using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Enhanced Channel Shift effect with RGB permutation modes
    /// Based on r_chanshift.cpp from original AVS
    /// Manipulates RGB color channels by swapping, rotating, or reordering components
    /// </summary>
    public class ChannelShiftEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Channel Shift effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Channel arrangement mode
        /// 0 = RGB (no change), 1 = RBG (swap B/G), 2 = BRG (rotate left), 
        /// 3 = BGR (swap R/B), 4 = GBR (rotate right), 5 = GRB (swap R/G)
        /// </summary>
        public int ChannelMode { get; set; } = 0;

        /// <summary>
        /// Beat reactivity enabled - changes mode on beat
        /// </summary>
        public bool BeatReactive { get; set; } = false;

        /// <summary>
        /// Beat mode selection
        /// 0 = Cycle through modes, 1 = Random mode selection
        /// </summary>
        public int BeatModeSelection { get; set; } = 1;

        /// <summary>
        /// Manual mode cycling speed (frames between changes)
        /// </summary>
        public int CycleSpeed { get; set; } = 60;

        /// <summary>
        /// Enable smooth transitions between modes
        /// </summary>
        public bool SmoothTransitions { get; set; } = false;

        /// <summary>
        /// Transition duration in frames
        /// </summary>
        public int TransitionDuration { get; set; } = 15;

        /// <summary>
        /// Intensity of the channel shift effect (0.0 to 1.0)
        /// </summary>
        public float Intensity { get; set; } = 1.0f;

        #endregion

        #region Private Fields

        private int _currentMode = 0;
        private int _frameCounter = 0;
        private int _transitionFrame = 0;
        private int _previousMode = 0;
        private bool _inTransition = false;
        private readonly Random _random = new Random();

        // Channel permutation lookup table
        private static readonly int[,] ChannelPermutations = new int[6, 3]
        {
            { 0, 1, 2 }, // RGB - no change
            { 0, 2, 1 }, // RBG - swap blue and green
            { 2, 0, 1 }, // BRG - rotate left
            { 2, 1, 0 }, // BGR - swap red and blue
            { 1, 2, 0 }, // GBR - rotate right
            { 1, 0, 2 }  // GRB - swap red and green
        };

        #endregion

        #region Constructor

        public ChannelShiftEffectsNode()
        {
            Name = "Channel Shift Effects";
            Description = "Enhanced RGB channel manipulation with permutation modes and beat reactivity";
            Category = "Color Effects";
            _currentMode = ChannelMode;
        }

        #endregion

        #region Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for channel shifting"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Channel shifted output image"));
        }

        #endregion

        #region Effect Processing

        public override void ProcessFrame(Dictionary<string, object> inputData, Dictionary<string, object> outputData, AudioFeatures audioFeatures)
        {
            if (!Enabled) return;

            try
            {
                var sourceImage = GetInputValue<ImageBuffer>("Image", inputData);
                if (sourceImage?.Data == null) return;

                var outputImage = new ImageBuffer(sourceImage.Width, sourceImage.Height);

                // Update current mode based on settings
                UpdateCurrentMode(audioFeatures);

                // Apply channel shifting
                if (SmoothTransitions && _inTransition)
                {
                    ApplyChannelShiftWithTransition(sourceImage, outputImage);
                }
                else
                {
                    ApplyChannelShift(sourceImage, outputImage, _currentMode);
                }

                outputData["Output"] = outputImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Channel Shift Effects] Error processing frame: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        private void UpdateCurrentMode(AudioFeatures audioFeatures)
        {
            _frameCounter++;

            bool shouldChangeMode = false;
            int newMode = _currentMode;

            if (BeatReactive && audioFeatures.Beat)
            {
                // Beat-triggered mode change
                shouldChangeMode = true;

                if (BeatModeSelection == 0) // Cycle through modes
                {
                    newMode = (_currentMode + 1) % 6;
                }
                else // Random mode selection
                {
                    do
                    {
                        newMode = _random.Next(6);
                    } while (newMode == _currentMode && _random.NextDouble() > 0.3); // 70% chance to pick different mode
                }
            }
            else if (!BeatReactive && _frameCounter >= CycleSpeed)
            {
                // Manual cycling
                shouldChangeMode = true;
                newMode = (ChannelMode + (_frameCounter / CycleSpeed)) % 6;
            }
            else if (!BeatReactive)
            {
                // Use fixed mode
                newMode = ChannelMode;
                if (newMode != _currentMode)
                {
                    shouldChangeMode = true;
                }
            }

            if (shouldChangeMode && newMode != _currentMode)
            {
                if (SmoothTransitions)
                {
                    StartTransition(newMode);
                }
                else
                {
                    _currentMode = newMode;
                }

                if (BeatReactive)
                {
                    _frameCounter = 0;
                }
            }

            // Update transition state
            if (_inTransition)
            {
                _transitionFrame++;
                if (_transitionFrame >= TransitionDuration)
                {
                    _inTransition = false;
                    _transitionFrame = 0;
                    _currentMode = _previousMode; // _previousMode holds the target mode during transition
                }
            }
        }

        private void StartTransition(int newMode)
        {
            _previousMode = _currentMode;
            _currentMode = newMode; // Store target mode
            _inTransition = true;
            _transitionFrame = 0;
        }

        private void ApplyChannelShift(ImageBuffer source, ImageBuffer output, int mode)
        {
            // Clamp mode to valid range
            mode = Math.Max(0, Math.Min(5, mode));

            // Get channel permutation for this mode
            int rChannel = ChannelPermutations[mode, 0];
            int gChannel = ChannelPermutations[mode, 1];
            int bChannel = ChannelPermutations[mode, 2];

            // Apply channel shifting with intensity
            for (int i = 0; i < source.Data.Length; i++)
            {
                uint pixel = source.Data[i];

                // Extract original channels
                uint alpha = (pixel >> 24) & 0xFF;
                uint red = (pixel >> 16) & 0xFF;
                uint green = (pixel >> 8) & 0xFF;
                uint blue = pixel & 0xFF;

                uint[] channels = { red, green, blue };

                // Apply permutation
                uint newRed = channels[rChannel];
                uint newGreen = channels[gChannel];
                uint newBlue = channels[bChannel];

                // Apply intensity blending
                if (Intensity < 1.0f)
                {
                    float invIntensity = 1.0f - Intensity;
                    newRed = (uint)(red * invIntensity + newRed * Intensity);
                    newGreen = (uint)(green * invIntensity + newGreen * Intensity);
                    newBlue = (uint)(blue * invIntensity + newBlue * Intensity);
                }

                // Combine channels
                output.Data[i] = (alpha << 24) | (newRed << 16) | (newGreen << 8) | newBlue;
            }
        }

        private void ApplyChannelShiftWithTransition(ImageBuffer source, ImageBuffer output)
        {
            // Calculate transition progress
            float progress = (float)_transitionFrame / TransitionDuration;
            progress = Math.Max(0.0f, Math.Min(1.0f, progress));

            // Create intermediate buffers
            var buffer1 = new ImageBuffer(source.Width, source.Height);
            var buffer2 = new ImageBuffer(source.Width, source.Height);

            // Apply both modes
            ApplyChannelShift(source, buffer1, _previousMode);
            ApplyChannelShift(source, buffer2, _currentMode);

            // Blend between the two results
            for (int i = 0; i < source.Data.Length; i++)
            {
                uint pixel1 = buffer1.Data[i];
                uint pixel2 = buffer2.Data[i];

                // Extract channels from both pixels
                uint a1 = (pixel1 >> 24) & 0xFF, r1 = (pixel1 >> 16) & 0xFF, g1 = (pixel1 >> 8) & 0xFF, b1 = pixel1 & 0xFF;
                uint a2 = (pixel2 >> 24) & 0xFF, r2 = (pixel2 >> 16) & 0xFF, g2 = (pixel2 >> 8) & 0xFF, b2 = pixel2 & 0xFF;

                // Interpolate
                uint finalA = (uint)(a1 * (1.0f - progress) + a2 * progress);
                uint finalR = (uint)(r1 * (1.0f - progress) + r2 * progress);
                uint finalG = (uint)(g1 * (1.0f - progress) + g2 * progress);
                uint finalB = (uint)(b1 * (1.0f - progress) + b2 * progress);

                output.Data[i] = (finalA << 24) | (finalR << 16) | (finalG << 8) | finalB;
            }
        }

        #endregion

        #region Configuration

        public override Dictionary<string, object> GetConfiguration()
        {
            return new Dictionary<string, object>
            {
                { "Enabled", Enabled },
                { "ChannelMode", ChannelMode },
                { "BeatReactive", BeatReactive },
                { "BeatModeSelection", BeatModeSelection },
                { "CycleSpeed", CycleSpeed },
                { "SmoothTransitions", SmoothTransitions },
                { "TransitionDuration", TransitionDuration },
                { "Intensity", Intensity }
            };
        }

        public override void ApplyConfiguration(Dictionary<string, object> config)
        {
            if (config.TryGetValue("Enabled", out var enabled))
                Enabled = Convert.ToBoolean(enabled);
            
            if (config.TryGetValue("ChannelMode", out var channelMode))
            {
                ChannelMode = Convert.ToInt32(channelMode);
                if (!BeatReactive) _currentMode = ChannelMode;
            }
            
            if (config.TryGetValue("BeatReactive", out var beatReactive))
                BeatReactive = Convert.ToBoolean(beatReactive);
            
            if (config.TryGetValue("BeatModeSelection", out var beatModeSelection))
                BeatModeSelection = Convert.ToInt32(beatModeSelection);
            
            if (config.TryGetValue("CycleSpeed", out var cycleSpeed))
                CycleSpeed = Convert.ToInt32(cycleSpeed);
            
            if (config.TryGetValue("SmoothTransitions", out var smoothTransitions))
                SmoothTransitions = Convert.ToBoolean(smoothTransitions);
            
            if (config.TryGetValue("TransitionDuration", out var transitionDuration))
                TransitionDuration = Convert.ToInt32(transitionDuration);
            
            if (config.TryGetValue("Intensity", out var intensity))
                Intensity = Convert.ToSingle(intensity);
        }

        /// <summary>
        /// Get the current active channel mode
        /// </summary>
        public int GetCurrentMode() => _currentMode;

        /// <summary>
        /// Get the name of a channel mode
        /// </summary>
        public static string GetModeName(int mode)
        {
            return mode switch
            {
                0 => "RGB (Normal)",
                1 => "RBG (Swap B/G)",
                2 => "BRG (Rotate Left)",
                3 => "BGR (Swap R/B)",
                4 => "GBR (Rotate Right)",
                5 => "GRB (Swap R/G)",
                _ => "Unknown"
            };
        }

        #endregion
    }
}