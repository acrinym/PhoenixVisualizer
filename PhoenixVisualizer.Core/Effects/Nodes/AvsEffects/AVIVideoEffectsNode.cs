using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// AVI Video Effects - video playback and processing
    /// </summary>
    public class AVIVideoEffectsNode : BaseEffectNode
    {
        public bool Enabled { get; set; } = true;
        public string VideoPath { get; set; } = "";
        public float PlaybackSpeed { get; set; } = 1.0f;
        public bool Loop { get; set; } = true;
        public bool BeatSync { get; set; } = false;
        public int BlendMode { get; set; } = 0;
        public float Opacity { get; set; } = 1.0f;

        private float _currentFrame = 0;
        private ImageBuffer _currentVideoFrame;

        public AVIVideoEffectsNode()
        {
            Name = "AVI Video Effects";
            Description = "Video playback and processing effects";
            Category = "Video Effects";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Background", typeof(ImageBuffer), false, null, "Background"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Video output"));
        }

        public override void ProcessFrame(Dictionary<string, object> inputData, Dictionary<string, object> outputData, AudioFeatures audioFeatures)
        {
            if (!Enabled) return;
            
            var backgroundImage = GetInputValue<ImageBuffer>("Background", inputData);
            var outputImage = backgroundImage != null ? 
                new ImageBuffer(backgroundImage.Width, backgroundImage.Height) : 
                new ImageBuffer(640, 480);

            if (backgroundImage != null)
                Array.Copy(backgroundImage.Data, outputImage.Data, backgroundImage.Data.Length);

            // Simulate video playback (placeholder implementation)
            _currentFrame += PlaybackSpeed;
            
            outputData["Output"] = outputImage;
        }

        public override Dictionary<string, object> GetConfiguration()
        {
            return new Dictionary<string, object>
            {
                { "Enabled", Enabled },
                { "VideoPath", VideoPath },
                { "PlaybackSpeed", PlaybackSpeed },
                { "Loop", Loop },
                { "BeatSync", BeatSync },
                { "BlendMode", BlendMode },
                { "Opacity", Opacity }
            };
        }

        public override void ApplyConfiguration(Dictionary<string, object> config)
        {
            if (config.TryGetValue("Enabled", out var enabled))
                Enabled = Convert.ToBoolean(enabled);
            if (config.TryGetValue("VideoPath", out var path))
                VideoPath = path.ToString();
            if (config.TryGetValue("PlaybackSpeed", out var speed))
                PlaybackSpeed = Convert.ToSingle(speed);
            if (config.TryGetValue("Loop", out var loop))
                Loop = Convert.ToBoolean(loop);
            if (config.TryGetValue("BeatSync", out var beatSync))
                BeatSync = Convert.ToBoolean(beatSync);
            if (config.TryGetValue("BlendMode", out var blendMode))
                BlendMode = Convert.ToInt32(blendMode);
            if (config.TryGetValue("Opacity", out var opacity))
                Opacity = Convert.ToSingle(opacity);
        }
    }
}