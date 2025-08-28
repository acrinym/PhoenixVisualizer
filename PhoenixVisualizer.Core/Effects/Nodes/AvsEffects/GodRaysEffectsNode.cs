using Avalonia.Media;
using PhoenixVisualizer.Core.VFX;
using PhoenixVisualizer.Core.Models;
using System;
using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

public class GodRaysEffectsNode : BaseEffectNode
{
    [VFXParameter("Intensity")] public double Intensity { get; set; } = 1.0;
    [VFXParameter("Decay")] public double Decay { get; set; } = 0.95;

    protected override void InitializePorts()
    {
        AddInput("Source");
        AddOutput("Result");
    }

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audio)
        {
            var src = GetInput<ImageBuffer>("Source");
            var dst = GetOutput<ImageBuffer>("Result");
            if (src == null || dst == null) return null!;

            dst.Clear();
            int cx = dst.Width / 2, cy = dst.Height / 2;
            for (int y = 0; y < dst.Height; y++)
            {
                for (int x = 0; x < dst.Width; x++)
                {
                    var c = Color.FromUInt32((uint)src[x, y]);
                    if (c.A > 10) // bright pixel
                    {
                        int dx = x - cx, dy = y - cy;
                        for (int k = 0; k < 50; k++)
                        {
                            int nx = cx + dx * k / 50;
                            int ny = cy + dy * k / 50;
                            if (nx >= 0 && nx < dst.Width && ny >= 0 && ny < dst.Height)
                            {
                                var oc = Color.FromUInt32((uint)dst[nx, ny]);
                                var nc = Color.FromArgb(255,
                                    Clamp(oc.R + (byte)(c.R * Intensity * Math.Pow(Decay, k))),
                                    Clamp(oc.G + (byte)(c.G * Intensity * Math.Pow(Decay, k))),
                                    Clamp(oc.B + (byte)(c.B * Intensity * Math.Pow(Decay, k))));
                                dst[nx, ny] = (int)(((uint)nc.A << 24) | ((uint)nc.R << 16) | ((uint)nc.G << 8) | nc.B);
                            }
                        }
                    }
                }
            }

            return dst;
        }

    private static byte Clamp(double v) => (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
}
