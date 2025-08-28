using Avalonia.Media;
using PhoenixVisualizer.Core.VFX;
using PhoenixVisualizer.Core.Models;
using System;
using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

public class DDMEffectsNode : BaseEffectNode
{
    [VFXParameter("Frequency")] public float Frequency { get; set; } = 0.1f;
    [VFXParameter("Amplitude")] public float Amplitude { get; set; } = 15f;

    protected override void InitializePorts()
    {
        AddInput("Source");
        AddOutput("Result");
    }

    protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audio)
    {
        var src = GetInput<ImageBuffer>("Source");
        var target = GetOutput<ImageBuffer>("Result");
        if (src == null || target == null) return null!;

        target.Clear();
        for (int y = 0; y < src.Height; y++)
        {
            for (int x = 0; x < src.Width; x++)
            {
                int dx = (int)(Math.Sin(y * Frequency + audio.Time) * Amplitude);
                int dy = (int)(Math.Cos(x * Frequency + audio.Time) * Amplitude);

                int tx = x + dx;
                int ty = y + dy;

                if (tx >= 0 && tx < target.Width && ty >= 0 && ty < target.Height)
                {
                    target[tx, ty] = src[x, y];
                }
            }
        }
        
        return target;
    }
}