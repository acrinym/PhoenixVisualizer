using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class MirrorEffectsNode : BaseEffectNode
    {
        public bool Enabled { get; set; } = true;
        public bool Vertical { get; set; } = true;

        public MirrorEffectsNode()
        {
            Name = "Mirror Effects";
            Description = "Creates mirror effects horizontally or vertically";
            Category = "Transform Effects";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for mirroring"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Mirrored output image"));
        }

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
            {
                return GetDefaultOutput();
            }

            if (!Enabled)
            {
                return imageBuffer;
            }

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            // Copy input to output first
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    output.SetPixel(x, y, imageBuffer.GetPixel(x, y));
                }
            }

            if (Vertical)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width / 2; x++)
                    {
                        var c = output.GetPixel(x, y);
                        output.SetPixel(width - x - 1, y, c);
                    }
                }
            }
            else
            {
                for (int y = 0; y < height / 2; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var c = output.GetPixel(x, y);
                        output.SetPixel(x, height - y - 1, c);
                    }
                }
            }

            return output;
        }
    }
}
