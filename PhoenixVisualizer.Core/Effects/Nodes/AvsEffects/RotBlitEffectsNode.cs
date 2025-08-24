using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class RotBlitEffectsNode : BaseEffectNode
    {
        public bool Enabled { get; set; } = true;
        public float Angle { get; set; } = 0.1f;

        public RotBlitEffectsNode()
        {
            Name = "Rotate Blit Effects";
            Description = "Rotates and blits images with configurable angle";
            Category = "Transform Effects";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for rotation"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Rotated output image"));
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
            output.Clear(0); // Clear to black
            int cx = imageBuffer.Width / 2;
            int cy = imageBuffer.Height / 2;
            double cos = Math.Cos(Angle);
            double sin = Math.Sin(Angle);

            for (int y = 0; y < imageBuffer.Height; y++)
            {
                for (int x = 0; x < imageBuffer.Width; x++)
                {
                    int dx = x - cx;
                    int dy = y - cy;
                    int sx = (int)(dx * cos - dy * sin + cx);
                    int sy = (int)(dx * sin + dy * cos + cy);
                    if (sx >= 0 && sx < imageBuffer.Width && sy >= 0 && sy < imageBuffer.Height)
                    {
                        var c = imageBuffer.GetPixel(sx, sy);
                        output.SetPixel(x, y, c);
                    }
                }
            }

            return output;
        }
    }
}
