using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Effects.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Adjusts image contrast by scaling pixel intensity relative to midpoint (128).
    /// </summary>
    public class ContrastEffectsNode : BaseEffectNode
    {
        public float Contrast { get; set; } = 1.2f; // >1 increases, <1 decreases

        public ContrastEffectsNode()
        {
            Name = "Contrast";
            Description = "Adjusts image contrast";
            Category = "AVS Effects";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Input", typeof(ImageBuffer), true, null, "Input image"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Contrast adjusted output"));
        }

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Input", out var input) || input is not ImageBuffer buffer)
                return GetDefaultOutput();

            var output = new ImageBuffer(buffer.Width, buffer.Height);
            Array.Copy(buffer.Pixels, output.Pixels, buffer.Pixels.Length);

            for (int i = 0; i < output.Pixels.Length; i++)
            {
                int pixel = output.Pixels[i];
                int r = (pixel & 0xFF);
                int g = (pixel >> 8) & 0xFF;
                int b = (pixel >> 16) & 0xFF;

                r = Math.Clamp((int)(((r - 128) * Contrast) + 128), 0, 255);
                g = Math.Clamp((int)(((g - 128) * Contrast) + 128), 0, 255);
                b = Math.Clamp((int)(((b - 128) * Contrast) + 128), 0, 255);

                output.Pixels[i] = r | (g << 8) | (b << 16);
            }

            return output;
        }

        public override object GetDefaultOutput() => new ImageBuffer(800, 600);
    }
}
