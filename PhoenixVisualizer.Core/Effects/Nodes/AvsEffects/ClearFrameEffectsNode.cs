using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class ClearFrameEffectsNode : BaseEffectNode
    {
        public int ClearColor { get; set; } = unchecked((int)0xFF000000);
        public bool ClearEveryFrame { get; set; } = true;

        public ClearFrameEffectsNode()
        {
            Name = "Clear Frame";
            Description = "Clears the frame with a specified color";
            Category = "Utility Effects";
        }

        protected override void InitializePorts()
        {
            // Input ports
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), false, null, "Input image to clear"));
            
            // Output ports
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), true, null, "Cleared output image"));
        }

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            // Get dimensions from input image or use defaults
            int width = 640;
            int height = 480;
            
            if (inputs.TryGetValue("Image", out var input) && input is ImageBuffer inputImage)
            {
                width = inputImage.Width;
                height = inputImage.Height;
            }

            var output = new ImageBuffer(width, height);
            if (ClearEveryFrame)
                output.Clear(ClearColor);
            return output;
        }
    }
}
