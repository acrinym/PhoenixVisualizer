using Avalonia.Media;
using PhoenixVisualizer.Core.VFX;
using PhoenixVisualizer.Core.Models;
using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

public class OnetoneEffectsNode : BaseEffectNode
{
    [VFXParameter("Channel")] public string Channel { get; set; } = "Gray"; // Gray, R, G, B

    protected override void InitializePorts()
    {
        AddInput("Source");
        AddOutput("Result");
    }

    protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audio)
    {
            var src = GetInput<ImageBuffer>("Source");
            var dst = GetOutput<ImageBuffer>("Result");
            if (src == null || dst == null) return dst;

            for (int y = 0; y < src.Height; y++)
            for (int x = 0; x < src.Width; x++)
            {
                var c = Color.FromUInt32((uint)src[x, y]);
                var nc = Channel switch
                {
                    "R" => Color.FromRgb(c.R, 0, 0),
                    "G" => Color.FromRgb(0, c.G, 0),
                    "B" => Color.FromRgb(0, 0, c.B),
                    _ => Gray(c)
                };
                dst[x, y] = (int)(((uint)nc.A << 24) | ((uint)nc.R << 16) | ((uint)nc.G << 8) | nc.B);
            }

            return dst;
        }

    private Color Gray(Color c)
    {
        byte g = (byte)((c.R + c.G + c.B) / 3);
        return Color.FromRgb(g, g, g);
    }
}