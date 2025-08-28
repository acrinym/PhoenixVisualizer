using Avalonia.Media;
using PhoenixVisualizer.Core.VFX;
using PhoenixVisualizer.Core.Models;
using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

public class BlitEffectsNode : BaseEffectNode
{
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
        
        dst.Blit(src, 0, 0, src.Width, src.Height, 0, 0);
        
        return dst;
    }
}