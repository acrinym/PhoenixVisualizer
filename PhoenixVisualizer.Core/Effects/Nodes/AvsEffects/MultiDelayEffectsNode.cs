using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Multi Delay effect for creating echo style frame delays with up to six taps.
    /// Supports per-buffer delay times, mix levels and optional per-channel delays.
    /// </summary>
    public class MultiDelayEffectsNode : BaseEffectNode
    {
        private const int MaxBuffers = 6;

        #region Public Properties

        /// <summary>Whether the effect is enabled.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Delay mode (0=Off, 1=Input, 2=Output).</summary>
        public DelayMode Mode { get; set; } = DelayMode.Off;

        /// <summary>Index of buffer for UI purposes (0-5).</summary>
        public int ActiveBufferIndex { get; set; } = 0;

        /// <summary>Use beat synchronisation for delay time per buffer.</summary>
        public bool[] UseBeatSync { get; set; } = new bool[MaxBuffers];

        /// <summary>Frame delays for each buffer.</summary>
        public int[] FrameDelay { get; set; } = new int[MaxBuffers];

        /// <summary>Mix level for each buffer (0.0-1.0).</summary>
        public float[] MixLevels { get; set; } = new float[MaxBuffers];

        /// <summary>Global intensity multiplier for output mix.</summary>
        public float Intensity { get; set; } = 1.0f;

        /// <summary>Enable different delay times for R,G,B channels.</summary>
        public bool EnablePerChannelDelay { get; set; } = false;

        /// <summary>Per channel delay values [buffer][channel].</summary>
        public int[][] ChannelFrameDelay { get; set; }
            = new int[MaxBuffers][];

        #endregion

        #region Private Fields

        private readonly List<ImageBuffer>[] _delayBuffers;
        private readonly Random _random = new Random();
        private int _renderId;
        private static int _instanceCount;
        private bool _isInitialized;
        private int _framesSinceBeat;

        #endregion

        #region Constructor

        public MultiDelayEffectsNode()
        {
            Name = "Multi Delay Effects";
            Description = "Stores frames in multiple buffers and replays them with delay.";
            Category = "AVS Effects";

            _delayBuffers = new List<ImageBuffer>[MaxBuffers];
            for (int i = 0; i < MaxBuffers; i++)
            {
                _delayBuffers[i] = new List<ImageBuffer>();
                ChannelFrameDelay[i] = new int[3];
                MixLevels[i] = 1.0f;
            }

            _renderId = _random.Next();
            _instanceCount++;
        }

        #endregion

        #region Port Initialisation

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Input", typeof(ImageBuffer), true, null,
                "Input image buffer"));
            _inputPorts.Add(new EffectPort("Enabled", typeof(bool), false, true,
                "Enable/disable effect"));
            _inputPorts.Add(new EffectPort("Mode", typeof(DelayMode), false, DelayMode.Off,
                "Delay mode (0=Off,1=Input,2=Output)"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), true, null,
                "Processed output image"));
        }

        #endregion

        #region Processing

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Input", out var inputObj) || inputObj is not ImageBuffer input)
                return GetDefaultOutput();

            if (inputs.TryGetValue("Enabled", out var en))
                Enabled = (bool)en;
            if (inputs.TryGetValue("Mode", out var mode))
                Mode = (DelayMode)mode;

            if (!Enabled || Mode == DelayMode.Off)
                return input;

            // Update beat tracking
            UpdateBeatSync(audioFeatures);

            // Store current frame in all buffers
            for (int i = 0; i < MaxBuffers; i++)
            {
                var buffer = _delayBuffers[i];
                var maxDelay = GetMaxDelay(i);
                if (buffer.Count >= maxDelay)
                    buffer.RemoveAt(0);
                buffer.Add(CloneBuffer(input));
            }

            if (Mode == DelayMode.Input)
                return input;

            var output = CloneBuffer(input);

            for (int b = 0; b < MaxBuffers; b++)
            {
                float mix = MixLevels[b] * Intensity;
                if (mix <= 0.0f) continue;

                var buffer = _delayBuffers[b];
                if (buffer.Count == 0) continue;

                int delay = GetDelayForBuffer(b);
                int delayR = EnablePerChannelDelay ? ChannelFrameDelay[b][0] : delay;
                int delayG = EnablePerChannelDelay ? ChannelFrameDelay[b][1] : delay;
                int delayB = EnablePerChannelDelay ? ChannelFrameDelay[b][2] : delay;

                ImageBuffer frameR = buffer.Count > delayR ? buffer[buffer.Count - delayR - 1] : null;
                ImageBuffer frameG = buffer.Count > delayG ? buffer[buffer.Count - delayG - 1] : null;
                ImageBuffer frameB = buffer.Count > delayB ? buffer[buffer.Count - delayB - 1] : null;

                for (int i = 0; i < output.Pixels.Length; i++)
                {
                    int color = output.Pixels[i];
                    int r = color & 0xFF;
                    int g = (color >> 8) & 0xFF;
                    int bcol = (color >> 16) & 0xFF;

                    if (frameR != null)
                    {
                        int dr = frameR.Pixels[i] & 0xFF;
                        r = (int)(r * (1 - mix) + dr * mix);
                    }
                    if (frameG != null)
                    {
                        int dg = (frameG.Pixels[i] >> 8) & 0xFF;
                        g = (int)(g * (1 - mix) + dg * mix);
                    }
                    if (frameB != null)
                    {
                        int db = (frameB.Pixels[i] >> 16) & 0xFF;
                        bcol = (int)(bcol * (1 - mix) + db * mix);
                    }

                    output.Pixels[i] = (bcol << 16) | (g << 8) | r;
                }
            }

            return output;
        }

        #endregion

        #region Helper Methods

        private void UpdateBeatSync(AudioFeatures audioFeatures)
        {
            if (audioFeatures == null) return;
            _lastBpm = audioFeatures.BPM;

            if (audioFeatures.IsBeat)
            {
                _framesSinceBeat = 0;
            }
            else
            {
                _framesSinceBeat++;
            }
        }

        private int GetDelayForBuffer(int bufferIndex)
        {
            if (UseBeatSync[bufferIndex])
            {
                // Approximate frames per beat assuming 60 FPS
                return (int)(audioFrameRate * 60.0 / Math.Max(1.0, _lastBpm));
            }
            return FrameDelay[bufferIndex];
        }

        private int GetMaxDelay(int bufferIndex)
        {
            int delay = GetDelayForBuffer(bufferIndex);
            if (EnablePerChannelDelay)
            {
                delay = Math.Max(delay, ChannelFrameDelay[bufferIndex][0]);
                delay = Math.Max(delay, ChannelFrameDelay[bufferIndex][1]);
                delay = Math.Max(delay, ChannelFrameDelay[bufferIndex][2]);
            }
            return Math.Max(1, delay + 1);
        }

        private static ImageBuffer CloneBuffer(ImageBuffer source)
        {
            return new ImageBuffer(source.Width, source.Height, (int[])source.Pixels.Clone());
        }

        #endregion

        #region Public Utility Methods

        public bool IsBufferReady()
        {
            return Enabled && Mode != DelayMode.Off &&
                   _delayBuffers[ActiveBufferIndex].Count > GetDelayForBuffer(ActiveBufferIndex);
        }

        public int GetCurrentDelay()
        {
            if (ActiveBufferIndex < 0 || ActiveBufferIndex >= MaxBuffers)
                return 0;
            return GetDelayForBuffer(ActiveBufferIndex);
        }

        public string GetBufferInfo(int bufferIndex)
        {
            if (bufferIndex < 0 || bufferIndex >= MaxBuffers)
                return "Invalid buffer index";
            int delay = GetDelayForBuffer(bufferIndex);
            return $"Buffer {bufferIndex}: Delay {delay}, Stored {_delayBuffers[bufferIndex].Count}";
        }

        public override void Reset()
        {
            base.Reset();
            for (int i = 0; i < MaxBuffers; i++)
            {
                _delayBuffers[i].Clear();
                FrameDelay[i] = 0;
                UseBeatSync[i] = false;
                ChannelFrameDelay[i][0] = ChannelFrameDelay[i][1] = ChannelFrameDelay[i][2] = 0;
            }
            _framesSinceBeat = 0;
            _isInitialized = false;
        }

        public string GetExecutionStats()
        {
            return $"Initialized: {_isInitialized}, Instance: {_renderId}, Total Instances: {_instanceCount}, Active Buffer: {ActiveBufferIndex}";
        }

        #endregion

        #region Internal State for Beat Sync
        // Stored BPM and assumed frame rate for beat calculations
        private double _lastBpm;
        private const double audioFrameRate = 60.0; // assume 60 FPS
        #endregion

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }
    }

    /// <summary>
    /// Available delay modes
    /// </summary>
    public enum DelayMode
    {
        /// <summary>Effect is disabled.</summary>
        Off = 0,
        /// <summary>Store input frames to buffer.</summary>
        Input = 1,
        /// <summary>Output delayed frames from buffer.</summary>
        Output = 2
    }
}
