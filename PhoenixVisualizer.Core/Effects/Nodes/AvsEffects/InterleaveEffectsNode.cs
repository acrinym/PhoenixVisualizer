using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Creates alternating stripe or checkerboard patterns by interleaving
    /// pixels or lines with a configurable colour and beat responsive offsets.
    /// </summary>
    public class InterleaveEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>Whether the effect is active.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Primary X offset for the interleaving pattern.</summary>
        public int XOffset { get; set; } = 0;

        /// <summary>Primary Y offset for the interleaving pattern.</summary>
        public int YOffset { get; set; } = 0;

        /// <summary>Secondary X offset used when beat response is enabled.</summary>
        public int XOffset2 { get; set; } = 0;

        /// <summary>Secondary Y offset used when beat response is enabled.</summary>
        public int YOffset2 { get; set; } = 0;

        /// <summary>Number of beats before toggling pattern.</summary>
        public int BeatDuration { get; set; } = 1;

        /// <summary>Enables audio beat response.</summary>
        public bool BeatResponse { get; set; } = false;

        /// <summary>Use additive blending with existing pixels.</summary>
        public bool BlendEnabled { get; set; } = false;

        /// <summary>Blend using 50/50 averaging instead of additive.</summary>
        public bool BlendAverage { get; set; } = false;

        /// <summary>Colour used for interleaving (0x00BBGGRR).</summary>
        public int InterleaveColor { get; set; } = 0;

        /// <summary>Intensity multiplier applied to the interleave colour.</summary>
        public float Intensity { get; set; } = 1.0f;

        #endregion

        #region Private Fields

        private double _currentX;
        private double _currentY;
        private int _frameCounter;
        private int _beatCounter;
        private bool _useSecondary;
        private bool _isInitialized;

        #endregion

        #region Constructor

        public InterleaveEffectsNode()
        {
            Name = "Interleave Effects";
            Description = "Alternate pixels or lines to form stripes or checkerboards";
            Category = "Pattern Effects";

            _currentX = XOffset;
            _currentY = YOffset;
            _isInitialized = false;
        }

        #endregion

        #region Port Initialisation

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Interleaved output"));
        }

        #endregion

        #region Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer source)
                return GetDefaultOutput();

            if (!Enabled)
                return source;

            if (!_isInitialized)
                InitializeEffect();

            _frameCounter++;

            if (BeatResponse && audioFeatures?.IsBeat == true)
                HandleBeatResponse();

            UpdatePositions();

            var output = new ImageBuffer(source.Width, source.Height);
            Array.Copy(source.Pixels, output.Pixels, source.Pixels.Length);

            int xPattern = Math.Max(1, Math.Abs((int)_currentX));
            int yPattern = Math.Max(1, Math.Abs((int)_currentY));

            int color = ApplyIntensity(InterleaveColor);

            if (xPattern > 1)
                ApplyHorizontalInterleaving(output, xPattern, color);

            if (yPattern > 1)
                ApplyVerticalInterleaving(output, yPattern, color);

            return output;
        }

        private void ApplyHorizontalInterleaving(ImageBuffer buffer, int pattern, int color)
        {
            int w = buffer.Width;
            int h = buffer.Height;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if ((x % pattern) == 0)
                    {
                        int current = buffer.GetPixel(x, y);
                        buffer.SetPixel(x, y, BlendPixel(current, color));
                    }
                }
            }
        }

        private void ApplyVerticalInterleaving(ImageBuffer buffer, int pattern, int color)
        {
            int w = buffer.Width;
            int h = buffer.Height;

            for (int y = 0; y < h; y++)
            {
                if ((y % pattern) == 0)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int current = buffer.GetPixel(x, y);
                        buffer.SetPixel(x, y, BlendPixel(current, color));
                    }
                }
            }
        }

        private int BlendPixel(int existing, int overlay)
        {
            if (BlendEnabled)
                return BlendAdditive(existing, overlay);
            if (BlendAverage)
                return BlendAveragePixels(existing, overlay);
            return overlay;
        }

        private static int BlendAdditive(int a, int b)
        {
            int r = Math.Min(255, (a & 0xFF) + (b & 0xFF));
            int g = Math.Min(255, ((a >> 8) & 0xFF) + ((b >> 8) & 0xFF));
            int bl = Math.Min(255, ((a >> 16) & 0xFF) + ((b >> 16) & 0xFF));
            return (bl << 16) | (g << 8) | r;
        }

        private static int BlendAveragePixels(int a, int b)
        {
            int r = ((a & 0xFF) + (b & 0xFF)) / 2;
            int g = (((a >> 8) & 0xFF) + ((b >> 8) & 0xFF)) / 2;
            int bl = (((a >> 16) & 0xFF) + ((b >> 16) & 0xFF)) / 2;
            return (bl << 16) | (g << 8) | r;
        }

        private int ApplyIntensity(int color)
        {
            int r = (int)Math.Clamp((color & 0xFF) * Intensity, 0, 255);
            int g = (int)Math.Clamp(((color >> 8) & 0xFF) * Intensity, 0, 255);
            int b = (int)Math.Clamp(((color >> 16) & 0xFF) * Intensity, 0, 255);
            return (b << 16) | (g << 8) | r;
        }

        private void InitializeEffect()
        {
            _currentX = XOffset;
            _currentY = YOffset;
            _frameCounter = 0;
            _beatCounter = 0;
            _useSecondary = false;
            _isInitialized = true;
        }

        private void HandleBeatResponse()
        {
            _beatCounter++;
            if (_beatCounter >= BeatDuration)
            {
                _beatCounter = 0;
                _useSecondary = !_useSecondary;
            }
        }

        private void UpdatePositions()
        {
            if (BeatResponse && _useSecondary)
            {
                _currentX = XOffset2;
                _currentY = YOffset2;
            }
            else
            {
                _currentX = XOffset;
                _currentY = YOffset;
            }
        }

        public override bool ValidateConfiguration()
        {
            if (XOffset < 0 || XOffset > 64) return false;
            if (YOffset < 0 || YOffset > 64) return false;
            if (XOffset2 < 0 || XOffset2 > 64) return false;
            if (YOffset2 < 0 || YOffset2 > 64) return false;
            if (BeatDuration < 1 || BeatDuration > 64) return false;
            if (Intensity < 0.0f || Intensity > 10.0f) return false;
            return true;
        }

        #endregion
    }
}
