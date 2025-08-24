using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Colorreplace Effects Node - Creates sophisticated color replacement effects
    /// by substituting colors below specified thresholds with target replacement colors
    /// </summary>
    public class ColorreplaceEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Enable/disable the colorreplace effect
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Replacement color for pixels below threshold
        /// </summary>
        public int ReplacementColor { get; set; } = unchecked((int)0xFF202020); // RGB(32,32,32)

        /// <summary>
        /// Red channel threshold (0-255)
        /// </summary>
        public int RedThreshold { get; set; } = 32;

        /// <summary>
        /// Green channel threshold (0-255)
        /// </summary>
        public int GreenThreshold { get; set; } = 32;

        /// <summary>
        /// Blue channel threshold (0-255)
        /// </summary>
        public int BlueThreshold { get; set; } = 32;

        /// <summary>
        /// Enable beat-reactive threshold changes
        /// </summary>
        public bool BeatReactive { get; set; } = false;

        /// <summary>
        /// Beat-reactive red threshold
        /// </summary>
        public int BeatRedThreshold { get; set; } = 64;

        /// <summary>
        /// Beat-reactive green threshold
        /// </summary>
        public int BeatGreenThreshold { get; set; } = 64;

        /// <summary>
        /// Beat-reactive blue threshold
        /// </summary>
        public int BeatBlueThreshold { get; set; } = 64;

        /// <summary>
        /// Enable smooth transitions between thresholds
        /// </summary>
        public bool SmoothTransitions { get; set; } = false;

        /// <summary>
        /// Transition speed (frames per threshold change)
        /// </summary>
        public int TransitionSpeed { get; set; } = 5;

        /// <summary>
        /// Enable alpha channel preservation
        /// </summary>
        public bool PreserveAlpha { get; set; } = true;

        /// <summary>
        /// Enable channel-selective replacement
        /// </summary>
        public bool ChannelSelective { get; set; } = false;

        /// <summary>
        /// Enable replacement color animation
        /// </summary>
        public bool AnimateReplacementColor { get; set; } = false;

        /// <summary>
        /// Animation speed for replacement color
        /// </summary>
        public float AnimationSpeed { get; set; } = 1.0f;

        /// <summary>
        /// Enable threshold animation
        /// </summary>
        public bool AnimateThresholds { get; set; } = false;

        /// <summary>
        /// Threshold animation speed
        /// </summary>
        public float ThresholdAnimationSpeed { get; set; } = 0.5f;

        #endregion

        #region Private Fields

        private int _currentRedThreshold;
        private int _currentGreenThreshold;
        private int _currentBlueThreshold;
        private int _currentReplacementColor;
        private int _transitionFrame = 0;
        private float _animationTime = 0.0f;
        private readonly Random _random = new Random();

        #endregion

        #region Constructor

        public ColorreplaceEffectsNode()
        {
            Name = "Color Replace Effects";
            Description = "Sophisticated color replacement effects with beat reactivity and smooth transitions";
            Category = "AVS Effects";
            
            // Initialize current thresholds
            _currentRedThreshold = RedThreshold;
            _currentGreenThreshold = GreenThreshold;
            _currentBlueThreshold = BlueThreshold;
            _currentReplacementColor = ReplacementColor;
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Input", typeof(ImageBuffer), true, null, "Input image buffer"));
            _inputPorts.Add(new EffectPort("Enabled", typeof(bool), false, true, "Enable/disable effect"));
            _inputPorts.Add(new EffectPort("ReplacementColor", typeof(int), false, 0xFF202020, "Replacement color (ARGB)"));
            _inputPorts.Add(new EffectPort("RedThreshold", typeof(int), false, 32, "Red channel threshold (0-255)"));
            _inputPorts.Add(new EffectPort("GreenThreshold", typeof(int), false, 32, "Green channel threshold (0-255)"));
            _inputPorts.Add(new EffectPort("BlueThreshold", typeof(int), false, 32, "Blue channel threshold (0-255)"));
            _inputPorts.Add(new EffectPort("BeatReactive", typeof(bool), false, false, "Enable beat-reactive thresholds"));
            _inputPorts.Add(new EffectPort("BeatRedThreshold", typeof(int), false, 64, "Beat red threshold (0-255)"));
            _inputPorts.Add(new EffectPort("BeatGreenThreshold", typeof(int), false, 64, "Beat green threshold (0-255)"));
            _inputPorts.Add(new EffectPort("BeatBlueThreshold", typeof(int), false, 64, "Beat blue threshold (0-255)"));
            _inputPorts.Add(new EffectPort("SmoothTransitions", typeof(bool), false, false, "Enable smooth transitions"));
            _inputPorts.Add(new EffectPort("TransitionSpeed", typeof(int), false, 5, "Transition speed (frames)"));
            _inputPorts.Add(new EffectPort("PreserveAlpha", typeof(bool), false, true, "Preserve alpha channel"));
            _inputPorts.Add(new EffectPort("ChannelSelective", typeof(bool), false, false, "Enable channel-selective replacement"));
            _inputPorts.Add(new EffectPort("AnimateReplacementColor", typeof(bool), false, false, "Animate replacement color"));
            _inputPorts.Add(new EffectPort("AnimationSpeed", typeof(float), false, 1.0f, "Animation speed"));
            _inputPorts.Add(new EffectPort("AnimateThresholds", typeof(bool), false, false, "Animate thresholds"));
            _inputPorts.Add(new EffectPort("ThresholdAnimationSpeed", typeof(float), false, 0.5f, "Threshold animation speed"));
            
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), true, null, "Processed image buffer"));
        }

        #endregion

        #region Process Method

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!Enabled) return inputs["Input"];

            var input = inputs["Input"] as ImageBuffer;
            if (input == null) return inputs["Input"];

            // Update current thresholds and replacement color
            UpdateThresholdsAndColor(audioFeatures);

            // Create output buffer
            var output = new ImageBuffer(input.Width, input.Height);

            // Process each pixel
            for (int i = 0; i < input.Pixels.Length; i++)
            {
                int originalColor = input.Pixels[i];
                int processedColor = ProcessPixel(originalColor);
                output.Pixels[i] = processedColor;
            }

            return output;
        }

        #endregion

        #region Private Methods

        private void UpdateThresholdsAndColor(AudioFeatures audioFeatures)
        {
            // Update animation time
            if (AnimateReplacementColor || AnimateThresholds)
            {
                _animationTime += 0.016f; // Assuming 60 FPS
            }

            // Handle beat-reactive thresholds
            if (BeatReactive && audioFeatures?.IsBeat == true)
            {
                if (SmoothTransitions)
                {
                    // Smooth transition to beat thresholds
                    _transitionFrame = Math.Min(_transitionFrame + 1, TransitionSpeed);
                    float transitionFactor = (float)_transitionFrame / TransitionSpeed;
                    
                    _currentRedThreshold = (int)(RedThreshold + (BeatRedThreshold - RedThreshold) * transitionFactor);
                    _currentGreenThreshold = (int)(GreenThreshold + (BeatGreenThreshold - GreenThreshold) * transitionFactor);
                    _currentBlueThreshold = (int)(BlueThreshold + (BeatBlueThreshold - BlueThreshold) * transitionFactor);
                }
                else
                {
                    // Immediate switch to beat thresholds
                    _currentRedThreshold = BeatRedThreshold;
                    _currentGreenThreshold = BeatGreenThreshold;
                    _currentBlueThreshold = BeatBlueThreshold;
                    _transitionFrame = 0;
                }
            }
            else
            {
                if (SmoothTransitions && _transitionFrame > 0)
                {
                    // Smooth transition back to normal thresholds
                    _transitionFrame = Math.Max(_transitionFrame - 1, 0);
                    float transitionFactor = (float)_transitionFrame / TransitionSpeed;
                    
                    _currentRedThreshold = (int)(BeatRedThreshold + (RedThreshold - BeatRedThreshold) * transitionFactor);
                    _currentGreenThreshold = (int)(BeatGreenThreshold + (GreenThreshold - BeatGreenThreshold) * transitionFactor);
                    _currentBlueThreshold = (int)(BeatBlueThreshold + (BlueThreshold - BeatBlueThreshold) * transitionFactor);
                }
                else
                {
                    // Normal thresholds
                    _currentRedThreshold = RedThreshold;
                    _currentGreenThreshold = GreenThreshold;
                    _currentBlueThreshold = BlueThreshold;
                }
            }

            // Animate thresholds if enabled
            if (AnimateThresholds)
            {
                float thresholdOffset = (float)Math.Sin(_animationTime * ThresholdAnimationSpeed) * 16;
                _currentRedThreshold = Math.Max(0, Math.Min(255, _currentRedThreshold + (int)thresholdOffset));
                _currentGreenThreshold = Math.Max(0, Math.Min(255, _currentGreenThreshold + (int)thresholdOffset));
                _currentBlueThreshold = Math.Max(0, Math.Min(255, _currentBlueThreshold + (int)thresholdOffset));
            }

            // Animate replacement color if enabled
            if (AnimateReplacementColor)
            {
                _currentReplacementColor = GenerateAnimatedReplacementColor();
            }
            else
            {
                _currentReplacementColor = ReplacementColor;
            }
        }

        private int ProcessPixel(int originalColor)
        {
            // Extract color channels
            int alpha = (originalColor >> 24) & 0xFF;
            int red = (originalColor >> 16) & 0xFF;
            int green = (originalColor >> 8) & 0xFF;
            int blue = originalColor & 0xFF;

            // Check if pixel meets replacement criteria
            bool shouldReplace = false;

            if (ChannelSelective)
            {
                // Channel-selective replacement
                bool redReplace = red <= _currentRedThreshold;
                bool greenReplace = green <= _currentGreenThreshold;
                bool blueReplace = blue <= _currentBlueThreshold;

                // Replace if any channel meets threshold
                shouldReplace = redReplace || greenReplace || blueReplace;
            }
            else
            {
                // Combined threshold check
                shouldReplace = red <= _currentRedThreshold && 
                              green <= _currentGreenThreshold && 
                              blue <= _currentBlueThreshold;
            }

            if (shouldReplace)
            {
                // Extract replacement color channels
                int replacementRed = (_currentReplacementColor >> 16) & 0xFF;
                int replacementGreen = (_currentReplacementColor >> 8) & 0xFF;
                int replacementBlue = _currentReplacementColor & 0xFF;

                // Preserve alpha if enabled
                int finalAlpha = PreserveAlpha ? alpha : 0xFF;

                // Return replacement color with preserved alpha
                return (finalAlpha << 24) | (replacementRed << 16) | (replacementGreen << 8) | replacementBlue;
            }

            // Return original color unchanged
            return originalColor;
        }

        private int GenerateAnimatedReplacementColor()
        {
            // Create animated replacement color using sine waves
            float redPhase = _animationTime * AnimationSpeed;
            float greenPhase = _animationTime * AnimationSpeed * 1.3f;
            float bluePhase = _animationTime * AnimationSpeed * 0.7f;

            int red = (int)(128 + 127 * Math.Sin(redPhase));
            int green = (int)(128 + 127 * Math.Sin(greenPhase));
            int blue = (int)(128 + 127 * Math.Sin(bluePhase));

            // Clamp values
            red = Math.Max(0, Math.Min(255, red));
            green = Math.Max(0, Math.Min(255, green));
            blue = Math.Max(0, Math.Min(255, blue));

            return (0xFF << 24) | (red << 16) | (green << 8) | blue;
        }

        #endregion

        #region Configuration

        public override bool ValidateConfiguration()
        {
            RedThreshold = Math.Max(0, Math.Min(255, RedThreshold));
            GreenThreshold = Math.Max(0, Math.Min(255, GreenThreshold));
            BlueThreshold = Math.Max(0, Math.Min(255, BlueThreshold));
            
            BeatRedThreshold = Math.Max(0, Math.Min(255, BeatRedThreshold));
            BeatGreenThreshold = Math.Max(0, Math.Min(255, BeatGreenThreshold));
            BeatBlueThreshold = Math.Max(0, Math.Min(255, BeatBlueThreshold));
            
            TransitionSpeed = Math.Max(1, Math.Max(30, TransitionSpeed));
            AnimationSpeed = Math.Max(0.1f, Math.Min(10.0f, AnimationSpeed));
            ThresholdAnimationSpeed = Math.Max(0.1f, Math.Min(5.0f, ThresholdAnimationSpeed));

            return true;
        }

        public override string GetSettingsSummary()
        {
            string thresholdInfo = $"R:{_currentRedThreshold} G:{_currentGreenThreshold} B:{_currentBlueThreshold}";
            string beatInfo = BeatReactive ? $"Beat: R:{BeatRedThreshold} G:{BeatGreenThreshold} B:{BeatBlueThreshold}" : "Beat: Off";
            string transitionInfo = SmoothTransitions ? $"Transitions: {TransitionSpeed} frames" : "Transitions: Off";
            string animationInfo = AnimateReplacementColor ? $"Color Anim: {AnimationSpeed:F1}x" : "Color Anim: Off";

            return $"Color Replace: {thresholdInfo}, {beatInfo}, {transitionInfo}, {animationInfo}";
        }

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }

        #endregion
    }
}
