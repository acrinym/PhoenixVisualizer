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
            if (src == null || dst == null) return null!;

            dst.Clear();
            for (int y = 0; y < src.Height; y += TileSize)
            {
                for (int x = 0; x < src.Width; x += TileSize)
                {
                    int nx = (x + (int)(audio.Bass * 10)) % dst.Width;
                    int ny = (y + (int)(audio.Mid * 10)) % dst.Height;

                    for (int ty = 0; ty < TileSize; ty++)
                    for (int tx = 0; tx < TileSize; tx++)
                    {
                        int sx = x + tx;
                        int sy = y + ty;
                        int dx = nx + tx;
                        int dy = ny + ty;
                        if (sx < 0 || sx >= src.Width || sy < 0 || sy >= src.Height) continue;
                        if (dx < 0 || dx >= dst.Width || dy < 0 || dy >= dst.Height) continue;
                        dst[dx, dy] = src[sx, sy];
                    }
                }
            }

            return dst;
        }
}