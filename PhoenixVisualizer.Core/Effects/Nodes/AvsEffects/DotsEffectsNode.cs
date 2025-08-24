using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class DotsEffectsNode : BaseEffectNode
    {
        private readonly Random rand = new();
        public bool Enabled { get; set; } = true;
        public int DotCount { get; set; } = 100;

        public DotsEffectsNode()
        {
            Name = "Dots Effects";
            Description = "Generates random dots on the image";
            Category = "Particle Effects";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for dot overlay"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Image with dots effect"));
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

            // Add random dots
            for (int i = 0; i < DotCount; i++)
            {
                int x = rand.Next(imageBuffer.Width);
                int y = rand.Next(imageBuffer.Height);
                output.SetPixel(x, y, 0xFFFFFF); // White dots
            }

            return output;
        }
    }
}
