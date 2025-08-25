using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Dot Grid effect draws a grid of dots with configurable spacing,
    /// jitter and fading between frames.
    /// </summary>
    public class DotGridEffectsNode : BaseEffectNode
    {
        private readonly Random _rand = new();
        private ImageBuffer? _buffer;

        /// <summary>
        /// Enable or disable the effect.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Distance between grid dots in pixels.
        /// </summary>
        public int GridSpacing { get; set; } = 8;

        /// <summary>
        /// Maximum random offset applied to each dot.
        /// </summary>
        public int Jitter { get; set; } = 0;

        /// <summary>
        /// Fading factor applied to previous frame (0-1).
        /// Values closer to 1 clear faster.
        /// </summary>
        public float Fade { get; set; } = 0.8f;

        public DotGridEffectsNode()
        {
            Name = "Dot Grid Effects";
            Description = "Renders a grid of dots with optional jitter and fading";
            Category = "Particle Effects";
        }

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for dot grid overlay"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Image with dot grid effect"));
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

            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            if (_buffer == null || _buffer.Width != width || _buffer.Height != height)
            {
                _buffer = new ImageBuffer(width, height);
            }

            // Fade existing buffer content
            float fade = Math.Clamp(Fade, 0f, 1f);
            for (int i = 0; i < _buffer.Pixels.Length; i++)
            {
                int c = _buffer.Pixels[i];
                int r = (int)((c & 0xFF) * fade);
                int g = (int)(((c >> 8) & 0xFF) * fade);
                int b = (int)(((c >> 16) & 0xFF) * fade);
                _buffer.Pixels[i] = (b << 16) | (g << 8) | r;
            }

            // Draw grid of dots with jitter
            int spacing = Math.Max(1, GridSpacing);
            for (int y = 0; y < height; y += spacing)
            {
                for (int x = 0; x < width; x += spacing)
                {
                    int dx = x + _rand.Next(-Jitter, Jitter + 1);
                    int dy = y + _rand.Next(-Jitter, Jitter + 1);
                    if (dx >= 0 && dx < width && dy >= 0 && dy < height)
                    {
                        _buffer.SetPixel(dx, dy, 0xFFFFFF);
                    }
                }
            }

            // Combine input with dot buffer
            var output = new ImageBuffer(width, height);
            for (int i = 0; i < imageBuffer.Pixels.Length; i++)
            {
                int dotColor = _buffer.Pixels[i];
                output.Pixels[i] = dotColor != 0 ? dotColor : imageBuffer.Pixels[i];
            }

            return output;
        }

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }
    }
}
