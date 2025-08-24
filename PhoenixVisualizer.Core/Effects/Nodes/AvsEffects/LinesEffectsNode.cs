using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class LinesEffectsNode : BaseEffectNode
    {
        private readonly Random rand = new();
        public bool Enabled { get; set; } = true;
        public int LineCount { get; set; } = 50;

        public LinesEffectsNode()
        {
            Name = "Lines Effects";
            Description = "Generates random lines on the image";
            Category = "Particle Effects";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for line overlay"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Image with lines effect"));
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
            
            // Copy input to output first
            for (int y = 0; y < imageBuffer.Height; y++)
            {
                for (int x = 0; x < imageBuffer.Width; x++)
                {
                    output.SetPixel(x, y, imageBuffer.GetPixel(x, y));
                }
            }

            // Add random lines
            for (int i = 0; i < LineCount; i++)
            {
                int x1 = rand.Next(imageBuffer.Width);
                int y1 = rand.Next(imageBuffer.Height);
                int x2 = rand.Next(imageBuffer.Width);
                int y2 = rand.Next(imageBuffer.Height);
                DrawLine(output, x1, y1, x2, y2, 0xFFFFFF);
            }

            return output;
        }

        private void DrawLine(ImageBuffer buffer, int x1, int y1, int x2, int y2, int color)
        {
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (x1 >= 0 && x1 < buffer.Width && y1 >= 0 && y1 < buffer.Height)
                {
                    buffer.SetPixel(x1, y1, color);
                }
                
                if (x1 == x2 && y1 == y2) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x1 += sx; }
                if (e2 < dx) { err += dx; y1 += sy; }
            }
        }
    }
}
