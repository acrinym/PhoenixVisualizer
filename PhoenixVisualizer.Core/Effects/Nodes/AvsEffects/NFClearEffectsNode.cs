using System;
using System.Collections.Generic;
using System.Numerics;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;
using CoreAvs = PhoenixVisualizer.Core.Avs.AvsEffects;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// NFClear effect - clears the screen on beat without fading, while keeping input buffers intact.
    /// </summary>
    public class NFClearEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Whether the effect is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Clear color (default white, RGB format)
        /// </summary>
        public int ClearColor { get; set; } = 0xFFFFFF;

        /// <summary>
        /// Blend with existing pixels instead of replacing
        /// </summary>
        public bool BlendEnabled { get; set; } = false;

        /// <summary>
        /// Number of beats to wait before clearing (1-100)
        /// </summary>
        public int BeatCount { get; set; } = 1;

        /// <summary>
        /// Intensity multiplier applied to clear color
        /// </summary>
        public float Intensity { get; set; } = 1.0f;

        #endregion

        #region Private Fields

        private int _currentBeatCount = 0;
        private int _delayCounter = 0;
        private bool _isInitialized = false;
        private bool _clearPending = false;

        #endregion

        #region Constructor

        public NFClearEffectsNode()
        {
            Name = "NF Clear Effects";
            Description = "Clears the screen every N beats without fading";
            Category = "AVS Effects";
        }

        #endregion

        #region Port Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Image buffer to clear"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Cleared image buffer"));
        }

        #endregion

        #region Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer image)
                return GetDefaultOutput();

            if (!Enabled)
                return image;

            if (!_isInitialized)
                InitializeEffect();

            if (audioFeatures?.IsBeat == true)
                HandleBeatDetection();

            if (_clearPending)
            {
                var output = new ImageBuffer(image.Width, image.Height);
                ApplyClear(output, image);
                _clearPending = false;
                _delayCounter = 0;
                return output;
            }

            _delayCounter++;
            return image;
        }

        #endregion

        #region Effect Logic

        private void InitializeEffect()
        {
            _currentBeatCount = 0;
            _delayCounter = 0;
            _clearPending = false;
            _isInitialized = true;
        }

        private void HandleBeatDetection()
        {
            _currentBeatCount++;
            if (_currentBeatCount >= Math.Clamp(BeatCount, 1, 100))
            {
                _clearPending = true;
                _currentBeatCount = 0;
            }
            _delayCounter = 0;
        }

        private void ApplyClear(ImageBuffer output, ImageBuffer source)
        {
            var frame = ConvertToVectorFrame(source);
            var colorVec = IntToVector4(ClearColor) * Intensity;
            colorVec = Vector4.Clamp(colorVec, Vector4.Zero, Vector4.One);

            if (BlendEnabled)
            {
                CoreAvs.ClearFrame.ClearBlend(frame, colorVec, 0.5f);
            }
            else
            {
                CoreAvs.ClearFrame.ClearSolid(frame, colorVec);
            }

            ConvertToImageBuffer(frame, output);
        }

        #endregion

        #region Helpers

        private static Vector4[,] ConvertToVectorFrame(ImageBuffer buffer)
        {
            var frame = new Vector4[buffer.Height, buffer.Width];
            for (int y = 0; y < buffer.Height; y++)
            {
                for (int x = 0; x < buffer.Width; x++)
                {
                    frame[y, x] = IntToVector4(buffer.GetPixel(x, y));
                }
            }
            return frame;
        }

        private static void ConvertToImageBuffer(Vector4[,] frame, ImageBuffer buffer)
        {
            for (int y = 0; y < buffer.Height; y++)
            {
                for (int x = 0; x < buffer.Width; x++)
                {
                    buffer.SetPixel(x, y, Vector4ToInt(frame[y, x]));
                }
            }
        }

        private static Vector4 IntToVector4(int color)
        {
            float r = (color & 0xFF) / 255f;
            float g = ((color >> 8) & 0xFF) / 255f;
            float b = ((color >> 16) & 0xFF) / 255f;
            return new Vector4(r, g, b, 1f);
        }

        private static int Vector4ToInt(Vector4 color)
        {
            int r = (int)(color.X * 255) & 0xFF;
            int g = (int)(color.Y * 255) & 0xFF;
            int b = (int)(color.Z * 255) & 0xFF;
            return r | (g << 8) | (b << 16);
        }

        #endregion

        #region Public API

        public bool IsClearPending() => _clearPending;

        public void ForceClear()
        {
            _clearPending = true;
            _currentBeatCount = 0;
            _delayCounter = 0;
        }

        public void ResetBeatCounter()
        {
            _currentBeatCount = 0;
            _delayCounter = 0;
        }

        public override void Reset()
        {
            _isInitialized = false;
            _currentBeatCount = 0;
            _delayCounter = 0;
            _clearPending = false;
        }

        public override bool ValidateConfiguration()
        {
            if (BeatCount < 1) BeatCount = 1;
            if (BeatCount > 100) BeatCount = 100;
            if (Intensity < 0f) Intensity = 0f;
            return true;
        }

        public override string GetSettingsSummary()
        {
            return $"NF Clear: Color=0x{ClearColor:X6}, Beats={BeatCount}, Blend={(BlendEnabled ? "On" : "Off")}";
        }

        #endregion

        #region Default Output

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(1, 1);
        }

        #endregion
    }
}

