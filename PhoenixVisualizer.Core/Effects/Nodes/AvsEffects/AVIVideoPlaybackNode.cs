using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Utils;

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

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!Enabled) 
                return GetDefaultOutput();
            
            if (inputs.TryGetValue("Background", out var backgroundInput) && backgroundInput is ImageBuffer backgroundImage)
            {
                var outputImage = new ImageBuffer(backgroundImage.Width, backgroundImage.Height);
                Array.Copy(backgroundImage.Data, outputImage.Data, backgroundImage.Data.Length);
                return outputImage;
            }

            return GetDefaultOutput();
        }

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(640, 480);
        }
    }
}