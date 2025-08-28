using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class ContrastEffectsNode : BaseEffectNode
    {
        public float Contrast { get; set; } = 1.0f;

        public ContrastEffectsNode()
        {
            Name = "Contrast";
            Description = "Adjusts the contrast of an image";
            Category = "Image Processing";
        }

        protected override void InitializePorts()
        {
            // Input ports
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Input image to process"));
            _inputPorts.Add(new EffectPort("Contrast", typeof(float), false, 1.0f, "Contrast multiplier (0.0 to 3.0)"));
            
            // Output ports
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), true, null, "Contrast-adjusted output image"));
        }

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer image)
                return GetDefaultOutput();

            // Get contrast value from input or use property
            float contrast = Contrast;
            if (inputs.TryGetValue("Contrast", out var contrastInput) && contrastInput is float contrastValue)
            {
                contrast = contrastValue;
            }

            var output = new ImageBuffer(image.Width, image.Height);
            Array.Copy(image.Pixels, output.Pixels, image.Pixels.Length);

            for (int i = 0; i < output.Pixels.Length; i++)
            {
                int c = output.Pixels[i];
                int r = ((c >> 16) & 0xFF) - 128;
                int g = ((c >> 8) & 0xFF) - 128;
                int b = (c & 0xFF) - 128;
                r = Math.Clamp((int)(r * contrast + 128), 0, 255);
                g = Math.Clamp((int)(g * contrast + 128), 0, 255);
                b = Math.Clamp((int)(b * contrast + 128), 0, 255);
                output.Pixels[i] = (c & unchecked((int)0xFF000000)) | (r << 16) | (g << 8) | b;
            }
            return output;
        }
    }
}
