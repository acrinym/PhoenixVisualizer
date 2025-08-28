using Avalonia.Media;
using PhoenixVisualizer.Core.VFX;
using PhoenixVisualizer.Core.Models;
using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

public class CompositeEffectsNode : BaseEffectNode
{
    [VFXParameter("BlendMode")] public string BlendMode { get; set; } = "Add"; // Add, Multiply, Screen

    protected override void InitializePorts()
    {
        AddInput("A");
        AddInput("B");
        AddOutput("Result");
    }

        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audio)
        {
            var a = GetInput<ImageBuffer>("A");
            var b = GetInput<ImageBuffer>("B");
            var result = GetOutput<ImageBuffer>("Result");
            if (a == null || b == null || result == null) return null!;

            for (int y = 0; y < result.Height; y++)
            for (int x = 0; x < result.Width; x++)
            {
                // Convert raw pixels to Avalonia colors for blending
                var ca = Color.FromUInt32((uint)a[x, y]);
                var cb = Color.FromUInt32((uint)b[x, y]);
                var blended = Blend(ca, cb);
                result[x, y] = (int)(((uint)blended.A << 24) | ((uint)blended.R << 16) | ((uint)blended.G << 8) | blended.B);
            }

            return result;
        }

    private Color Blend(Color a, Color b)
    {
        return BlendMode switch
        {
            "Multiply" => Color.FromArgb(255,
                (byte)(a.R * b.R / 255),
                (byte)(a.G * b.G / 255),
                (byte)(a.B * b.B / 255)),
            "Screen" => Color.FromArgb(255,
                (byte)(255 - (255 - a.R) * (255 - b.R) / 255),
                (byte)(255 - (255 - a.G) * (255 - b.G) / 255),
                (byte)(255 - (255 - a.B) * (255 - b.B) / 255)),
            _ => Color.FromArgb(255,
                ClampByte(a.R + b.R),
                ClampByte(a.G + b.G),
                ClampByte(a.B + b.B))
        };
    }

    private static byte ClampByte(int v) => (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
}