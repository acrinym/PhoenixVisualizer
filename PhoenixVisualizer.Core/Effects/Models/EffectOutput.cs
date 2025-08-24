using System.Drawing;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Models
{
    public class EffectOutput
    {
        public required ImageBuffer Image { get; set; }
        public EffectMetadata Metadata { get; set; } = default!;

        public EffectOutput()
        {
            Metadata = new EffectMetadata();
        }

        public EffectOutput(ImageBuffer image)
        {
            Image = image;
            Metadata = new EffectMetadata();
        }
    }
}
