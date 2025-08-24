using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class ConvolutionEffectsNode : BaseEffectNode
    {
        public bool Enabled { get; set; } = true;
        public float[,] Kernel { get; set; } =
        {
            {0, -1, 0},
            {-1, 5, -1},
            {0, -1, 0}
        };

        public ConvolutionEffectsNode()
        {
            Name = "Convolution Effects";
            Description = "Applies convolution kernel for edge detection and sharpening";
            Category = "Filter Effects";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for convolution"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Convolved output image"));
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
            int kSize = Kernel.GetLength(0);
            int kHalf = kSize / 2;

            // Copy input to output first
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    output.SetPixel(x, y, imageBuffer.GetPixel(x, y));
                }
            }

            // Apply convolution
            for (int y = kHalf; y < h - kHalf; y++)
            {
                for (int x = kHalf; x < w - kHalf; x++)
                {
                    float r = 0, g = 0, b = 0;
                    for (int ky = -kHalf; ky <= kHalf; ky++)
                    {
                        for (int kx = -kHalf; kx <= kHalf; kx++)
                        {
                            var c = imageBuffer.GetPixel(x + kx, y + ky);
                            float k = Kernel[ky + kHalf, kx + kHalf];
                            r += (c & 0xFF) * k;
                            g += ((c >> 8) & 0xFF) * k;
                            b += ((c >> 16) & 0xFF) * k;
                        }
                    }
                    int ri = (int)Math.Clamp(r, 0, 255);
                    int gi = (int)Math.Clamp(g, 0, 255);
                    int bi = (int)Math.Clamp(b, 0, 255);
                    output.SetPixel(x, y, (bi << 16) | (gi << 8) | ri);
                }
            }

            return output;
        }
    }
}
