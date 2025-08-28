using Avalonia.Media;
using PhoenixVisualizer.Core.VFX;
using PhoenixVisualizer.Core.Models;
using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

public class PartsEffectsNode : BaseEffectNode
{
    [VFXParameter("TileSize")] public int TileSize { get; set; } = 32;

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
        for (int y = 0; y < src.Height; y += TileSize)
        {
            for (int x = 0; x < src.Width; x += TileSize)
            {
                int nx = (x + (int)(audio.Bass * 10)) % dst.Width;
                int ny = (y + (int)(audio.Mid * 10)) % dst.Height;
                dst.Blit(src, x, y, TileSize, TileSize, nx, ny);
            }
        }
        
        return dst;
    }
}