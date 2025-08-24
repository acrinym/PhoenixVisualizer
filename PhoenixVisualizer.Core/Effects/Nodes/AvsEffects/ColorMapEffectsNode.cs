using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class ColorMapEffectsNode : BaseEffectNode
    {
        public bool Enabled { get; set; } = true;
        public Func<Color, Color> Map { get; set; } = c => c; // identity by default

        public ColorMapEffectsNode()
        {
            Name = "Color Map Effects";
            Description = "Applies custom color mapping functions to images";
            Category = "Color Effects";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for color mapping"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Color-mapped output image"));
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
            for (int y = 0; y < imageBuffer.Height; y++)
            {
                for (int x = 0; x < imageBuffer.Width; x++)
                {
                    var c = imageBuffer.GetPixel(x, y);
                    var mappedColor = Map(Color.FromArgb(c));
                    output.SetPixel(x, y, (mappedColor.B << 16) | (mappedColor.G << 8) | mappedColor.R);
                }
            }

            return output;
        }
    }
}
