using System.Collections.Generic;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Effects.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Clears the frame to black.
    /// </summary>
    public class ClearFrameEffectsNode : BaseEffectNode
    {
        public ClearFrameEffectsNode()
        {
            Name = "Clear Frame";
            Description = "Clears the frame to black";
            Category = "AVS Effects";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Input", typeof(ImageBuffer), true, null, "Input image"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Cleared output"));
        }

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Input", out var input) || input is not ImageBuffer buffer)
                return GetDefaultOutput();

            var output = new ImageBuffer(buffer.Width, buffer.Height);
            for (int i = 0; i < output.Pixels.Length; i++)
                output.Pixels[i] = 0;

            return output;
        }

        public override object GetDefaultOutput() => new ImageBuffer(800, 600);
    }
}
