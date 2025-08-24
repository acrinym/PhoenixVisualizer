using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class StarfieldEffectsNode : BaseEffectNode
    {
        private readonly List<(float x, float y, float z)> stars = new();
        private readonly Random rand = new();
        public bool Enabled { get; set; } = true;
        public int StarCount { get; set; } = 200;
        public float Speed { get; set; } = 0.05f;

        public StarfieldEffectsNode()
        {
            Name = "Starfield Effects";
            Description = "Creates animated starfield with depth and movement";
            Category = "Particle Effects";
            
            for (int i = 0; i < StarCount; i++)
                stars.Add((RandCoord(), RandCoord(), (float)rand.NextDouble() * 1.0f + 0.1f));
        }

        private float RandCoord() => (float)(rand.NextDouble() * 2 - 1);

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for starfield overlay"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Image with starfield effect"));
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

            // Update and draw stars
            foreach (var (x, y, z) in stars.ToArray())
            {
                float newZ = z - Speed;
                if (newZ <= 0)
                {
                    stars.Remove((x, y, z));
                    stars.Add((RandCoord(), RandCoord(), 1f));
                    continue;
                }
                int sx = (int)((x / newZ) * w / 2 + w / 2);
                int sy = (int)((y / newZ) * h / 2 + h / 2);
                if (sx >= 0 && sx < w && sy >= 0 && sy < h)
                    output.SetPixel(sx, sy, 0xFFFFFF);
                stars[stars.IndexOf((x, y, z))] = (x, y, newZ);
            }

            return output;
        }
    }
}
