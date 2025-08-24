using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class BlurEffectsNode : BaseEffectNode
    {
        public bool Enabled { get; set; } = true;
        public int Radius { get; set; } = 2;

        public BlurEffectsNode()
        {
            Name = "Blur Effects";
            Description = "Applies gaussian blur with configurable radius";
            Category = "Filter Effects";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for blurring"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Blurred output image"));
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
            int w = imageBuffer.Width;
            int h = imageBuffer.Height;

            // Copy input to output first
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    output.SetPixel(x, y, imageBuffer.GetPixel(x, y));
                }
            }

            // Apply blur
            for (int y = Radius; y < h - Radius; y++)
            {
                for (int x = Radius; x < w - Radius; x++)
                {
                    int r = 0, g = 0, b = 0;
                    int count = 0;
                    for (int ky = -Radius; ky <= Radius; ky++)
                    {
                        for (int kx = -Radius; kx <= Radius; kx++)
                        {
                            var c = imageBuffer.GetPixel(x + kx, y + ky);
                            r += c & 0xFF;
                            g += (c >> 8) & 0xFF;
                            b += (c >> 16) & 0xFF;
                            count++;
                        }
                    }
                    int newR = r / count;
                    int newG = g / count;
                    int newB = b / count;
                    output.SetPixel(x, y, (newB << 16) | (newG << 8) | newR);
                }
            }

            return output;
        }
    }
}
