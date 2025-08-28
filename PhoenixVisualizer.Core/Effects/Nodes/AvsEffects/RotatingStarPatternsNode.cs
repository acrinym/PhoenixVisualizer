using Avalonia.Media;
using PhoenixVisualizer.Core.VFX;
using PhoenixVisualizer.Core.Models;
using System;
using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

public class RotatingStarPatternsNode : BaseEffectNode
{
    [VFXParameter("Count")] public int Count { get; set; } = 100;
    [VFXParameter("Speed")] public double Speed { get; set; } = 1.0;

    private readonly Random _rng = new();

    protected override void InitializePorts()
    {
        AddOutput("Result");
    }

    protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audio)
    {
        var dst = GetOutput<ImageBuffer>("Result");
        if (dst == null) return null!;

        dst.Clear();
        int cx = dst.Width / 2, cy = dst.Height / 2;
        for (int i = 0; i < Count; i++)
        {
            double angle = i * (2 * Math.PI / Count) + audio.Time * Speed;
            int r = (int)(audio.Bass * 100 + i % 50);
            int x = cx + (int)(Math.Cos(angle) * r);
            int y = cy + (int)(Math.Sin(angle) * r);
            DrawingUtils.DrawCircle(dst, x, y, 1, Colors.White);
        }
        
        return dst;
    }
}