using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class ChannelShiftEffectsNode : BaseEffectNode
    {
        public bool Enabled { get; set; } = true;
        public int ShiftR { get; set; } = 1;
        public int ShiftG { get; set; } = 0;
        public int ShiftB { get; set; } = -1;

        public ChannelShiftEffectsNode()
        {
            Name = "Channel Shift Effects";
            Description = "Shifts RGB channels independently for color separation effects";
            Category = "Color Effects";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for channel shifting"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Channel-shifted output image"));
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

            // Apply channel shifting
            for (int y = 1; y < h - 1; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    int r = imageBuffer.GetPixel(Math.Clamp(x + ShiftR, 0, w - 1), y) & 0xFF;
                    int g = imageBuffer.GetPixel(Math.Clamp(x + ShiftG, 0, w - 1), y) & 0xFF;
                    int b = imageBuffer.GetPixel(Math.Clamp(x + ShiftB, 0, w - 1), y) & 0xFF;
                    output.SetPixel(x, y, (b << 16) | (g << 8) | r);
                }
            }

            return output;
        }
    }
}
