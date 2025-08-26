using System;
using System.Drawing;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Models
{
    public class EffectInput
    {
        public ImageBuffer Image { get; set; } = default!;
        public AudioFeatures AudioFeatures { get; set; } = default!;
        public int FrameNumber { get; set; }
        public double Timestamp { get; set; }

        public EffectInput()
        {
            // Initialize with default values to avoid nullable warnings
            Image = new ImageBuffer(640, 480); // Default size
            AudioFeatures = new AudioFeatures();
        }

        public EffectInput(ImageBuffer image, AudioFeatures? audioFeatures = null, int frameNumber = 0, double timestamp = 0.0)
        {
            Image = image ?? throw new ArgumentNullException(nameof(image));
            AudioFeatures = audioFeatures ?? new AudioFeatures();
            FrameNumber = frameNumber;
            Timestamp = timestamp;
        }
    }
}
