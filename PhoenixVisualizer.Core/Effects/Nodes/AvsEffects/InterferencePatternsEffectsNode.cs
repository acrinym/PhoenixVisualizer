using Avalonia.Media;
using PhoenixVisualizer.Core.VFX;
using PhoenixVisualizer.Core.Models;
using System;
using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

public class InterferencePatternsEffectsNode : BaseEffectNode
{
    [VFXParameter("Frequency")] public double Frequency { get; set; } = 0.05;
    [VFXParameter("Amplitude")] public double Amplitude { get; set; } = 10.0;

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
        for (int y = 0; y < src.Height; y++)
        {
            for (int x = 0; x < src.Width; x++)
            {
                int dx = (int)(Amplitude * Math.Sin(Frequency * y + audio.Bass * 5));
                int dy = (int)(Amplitude * Math.Sin(Frequency * x + audio.Treble * 5));
                int sx = (x + dx) % src.Width;
                int sy = (y + dy) % src.Height;
                if (sx < 0) sx += src.Width;
                if (sy < 0) sy += src.Height;
                dst[x, y] = src[sx, sy];
            }
        }
        
        return dst;
    }
}