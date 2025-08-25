using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Applies configurable multiplication or division to pixel values with optional audio reactivity.
    /// Includes safeguards to prevent channel overflow.
    /// </summary>
    public class MultiplierEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the effect is active.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Current multiplication mode.
        /// </summary>
        public MultiplierMode Mode { get; set; } = MultiplierMode.X2;

        /// <summary>
        /// Additional intensity multiplier applied to the selected mode.
        /// </summary>
        public float Intensity { get; set; } = 1.0f;

        /// <summary>
        /// Enables modulation of the multiplier using audio RMS values.
        /// </summary>
        public bool AudioReactive { get; set; } = false;

        /// <summary>
        /// Scales the audio contribution when <see cref="AudioReactive"/> is enabled.
        /// </summary>
        public float AudioScale { get; set; } = 1.0f;

        #endregion

        #region Private Fields

        /// <summary>
        /// Indicates whether the effect has been initialized.
        /// </summary>
        private bool _isInitialized = false;

        /// <summary>
        /// Pre-calculated masks for potential bit operations (reserved for future optimizations).
        /// </summary>
        private static readonly ulong[] _bitMasks =
        {
            0x7F7F7F7F7F7F7F7FUL, // For X05 (divide by 2)
            0x3F3F3F3F3F3F3F3FUL, // For X025 (divide by 4)
            0x1F1F1F1F1F1F1F1FUL  // For X0125 (divide by 8)
        };

        #endregion

        #region Constructor

        public MultiplierEffectsNode()
        {
            Name = "Multiplier Effects";
            Description = "Applies multiplication or division to pixel channels with optional audio modulation";
            Category = "Color Effects";
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for multiplier operations"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Processed output image"));
        }

        #endregion

        #region Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
                return GetDefaultOutput();

            if (!Enabled)
                return imageBuffer;

            if (!_isInitialized)
                InitializeEffect();

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);

            float baseMultiplier = GetCurrentMultiplier();
            float audioMultiplier = 1.0f;
            if (AudioReactive && audioFeatures != null)
            {
                // Normalize RMS (0-255) to 0-1 and scale
                audioMultiplier += (audioFeatures.Rms / 255f) * AudioScale;
            }

            float finalMultiplier = baseMultiplier * Intensity * audioMultiplier;

            for (int y = 0; y < imageBuffer.Height; y++)
            {
                for (int x = 0; x < imageBuffer.Width; x++)
                {
                    int color = imageBuffer.GetPixel(x, y);
                    int r = color & 0xFF;
                    int g = (color >> 8) & 0xFF;
                    int b = (color >> 16) & 0xFF;

                    switch (Mode)
                    {
                        case MultiplierMode.Invert:
                            if (r != 0 || g != 0 || b != 0)
                                r = g = b = 255;
                            else
                                r = g = b = 0;
                            break;
                        case MultiplierMode.XS:
                            if (r != 255 || g != 255 || b != 255)
                                r = g = b = 0;
                            else
                                r = g = b = 255;
                            break;
                        default:
                            r = ClampToByte(r * finalMultiplier);
                            g = ClampToByte(g * finalMultiplier);
                            b = ClampToByte(b * finalMultiplier);
                            break;
                    }

                    output.SetPixel(x, y, (b << 16) | (g << 8) | r);
                }
            }

            return output;
        }

        #endregion

        #region Utility Methods

        private void InitializeEffect()
        {
            _isInitialized = true;
        }

        private float GetCurrentMultiplier()
        {
            switch (Mode)
            {
                case MultiplierMode.X8: return 8.0f;
                case MultiplierMode.X4: return 4.0f;
                case MultiplierMode.X2: return 2.0f;
                case MultiplierMode.X05: return 0.5f;
                case MultiplierMode.X025: return 0.25f;
                case MultiplierMode.X0125: return 0.125f;
                case MultiplierMode.Invert:
                case MultiplierMode.XS:
                default: return 1.0f;
            }
        }

        private static int ClampToByte(float value)
        {
            return (int)Math.Clamp(value, 0f, 255f);
        }

        /// <summary>
        /// Check if current mode multiplies pixel values.
        /// </summary>
        public bool IsMultiplying()
        {
            return Enabled && (Mode == MultiplierMode.X8 || Mode == MultiplierMode.X4 || Mode == MultiplierMode.X2);
        }

        /// <summary>
        /// Check if current mode divides pixel values.
        /// </summary>
        public bool IsDividing()
        {
            return Enabled && (Mode == MultiplierMode.X05 || Mode == MultiplierMode.X025 || Mode == MultiplierMode.X0125);
        }

        /// <summary>
        /// Check if current mode performs a special operation.
        /// </summary>
        public bool IsSpecialMode()
        {
            return Enabled && (Mode == MultiplierMode.Invert || Mode == MultiplierMode.XS);
        }

        /// <summary>
        /// Provides human-readable description for current mode.
        /// </summary>
        public string GetEffectDescription()
        {
            return Mode switch
            {
                MultiplierMode.X8 => "Multiply by 8 (Triple Brightness)",
                MultiplierMode.X4 => "Multiply by 4 (Double Brightness)",
                MultiplierMode.X2 => "Multiply by 2 (Increase Brightness)",
                MultiplierMode.X05 => "Divide by 2 (Decrease Brightness)",
                MultiplierMode.X025 => "Divide by 4 (Quarter Brightness)",
                MultiplierMode.X0125 => "Divide by 8 (Eighth Brightness)",
                MultiplierMode.Invert => "Invert Non-Zero Pixels to White",
                MultiplierMode.XS => "Set Non-White Pixels to Black",
                _ => "Unknown Mode",
            };
        }

        /// <summary>
        /// Reset the effect to its initial state.
        /// </summary>
        public void Reset()
        {
            _isInitialized = false;
        }

        /// <summary>
        /// Retrieve simple execution statistics.
        /// </summary>
        public string GetExecutionStats()
        {
            return $"Initialized: {_isInitialized}, Mode: {Mode}, Multiplier: {GetCurrentMultiplier()}";
        }

        /// <summary>
        /// Load a preset with high brightness settings.
        /// </summary>
        public void LoadHighBrightnessPreset()
        {
            Mode = MultiplierMode.X8;
            Intensity = 1.0f;
        }

        /// <summary>
        /// Load a preset for darkening the image.
        /// </summary>
        public void LoadDarknessPreset()
        {
            Mode = MultiplierMode.X0125;
            Intensity = 1.0f;
        }

        /// <summary>
        /// Load a preset for the special invert mode.
        /// </summary>
        public void LoadInvertPreset()
        {
            Mode = MultiplierMode.Invert;
            Intensity = 1.0f;
        }

        #endregion
    }

    /// <summary>
    /// Available multiplier modes.
    /// </summary>
    public enum MultiplierMode
    {
        /// <summary>
        /// Invert non-zero pixels to white.
        /// </summary>
        Invert = 0,

        /// <summary>
        /// Multiply by 8 (triple brightness).
        /// </summary>
        X8 = 1,

        /// <summary>
        /// Multiply by 4 (double brightness).
        /// </summary>
        X4 = 2,

        /// <summary>
        /// Multiply by 2 (increase brightness).
        /// </summary>
        X2 = 3,

        /// <summary>
        /// Divide by 2 (decrease brightness).
        /// </summary>
        X05 = 4,

        /// <summary>
        /// Divide by 4 (quarter brightness).
        /// </summary>
        X025 = 5,

        /// <summary>
        /// Divide by 8 (eighth brightness).
        /// </summary>
        X0125 = 6,

        /// <summary>
        /// Set non-white pixels to black.
        /// </summary>
        XS = 7
    }
}

