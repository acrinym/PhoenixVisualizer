using Avalonia.Media;
using PhoenixVisualizer.Core.VFX;
using PhoenixVisualizer.Core.Models;
using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

public class DynamicShiftEffectsNode : BaseEffectNode
{
    [VFXParameter("Amount")] public int Amount { get; set; } = 10;

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

        dst.Clear();
        int shift = (int)(Amount * (1 + audio.Bass));
        for (int y = 0; y < src.Height; y++)
        {
            for (int x = 0; x < src.Width; x++)
            {
                int sx = (x + shift) % src.Width;
                dst[x, y] = src[sx, y];
            }
        }
        
        return dst;
    }
}