using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Comment effect node – stores metadata without changing the image.
    /// </summary>
    public class CommentEffectsNode : BaseEffectNode
    {
        /// <summary>
        /// Comment text associated with this node.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        public CommentEffectsNode()
        {
            Name = "Comment";
            Description = "Stores a comment for documentation; no rendering";
            Category = "Utility";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Input image"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Unchanged output image"));
        }

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
            {
                return GetDefaultOutput();
            }

            // Pass-through; comment does not affect rendering
            return imageBuffer;
        }
    }
}
