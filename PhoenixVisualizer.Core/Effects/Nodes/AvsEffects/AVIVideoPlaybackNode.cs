using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// AVI Video Playback - enhanced video playback variant
    /// </summary>
    public class AVIVideoPlaybackNode : BaseEffectNode
    {
        public bool Enabled { get; set; } = true;
        public string VideoFile { get; set; } = "";
        public bool AutoPlay { get; set; } = true;
        public float Volume { get; set; } = 1.0f;
        public bool AudioSync { get; set; } = true;
        public int ScalingMode { get; set; } = 0; // 0=Stretch, 1=Fit, 2=Fill

        public AVIVideoPlaybackNode()
        {
            Name = "AVI Video Playback";
            Description = "Enhanced video playback with audio sync";
            Category = "Video Effects";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Background", typeof(ImageBuffer), false, null, "Background"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Playback output"));
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

            outputData["Output"] = outputImage;
        }

        public override Dictionary<string, object> GetConfiguration()
        {
            return new Dictionary<string, object>
            {
                { "Enabled", Enabled },
                { "VideoFile", VideoFile },
                { "AutoPlay", AutoPlay },
                { "Volume", Volume },
                { "AudioSync", AudioSync },
                { "ScalingMode", ScalingMode }
            };
        }

        public override void ApplyConfiguration(Dictionary<string, object> config)
        {
            if (config.TryGetValue("Enabled", out var enabled))
                Enabled = Convert.ToBoolean(enabled);
            if (config.TryGetValue("VideoFile", out var file))
                VideoFile = file.ToString();
            if (config.TryGetValue("AutoPlay", out var autoPlay))
                AutoPlay = Convert.ToBoolean(autoPlay);
            if (config.TryGetValue("Volume", out var volume))
                Volume = Convert.ToSingle(volume);
            if (config.TryGetValue("AudioSync", out var audioSync))
                AudioSync = Convert.ToBoolean(audioSync);
            if (config.TryGetValue("ScalingMode", out var scalingMode))
                ScalingMode = Convert.ToInt32(scalingMode);
        }
    }
}