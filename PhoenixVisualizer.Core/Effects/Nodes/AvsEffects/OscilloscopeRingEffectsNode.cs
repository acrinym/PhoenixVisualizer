using Avalonia.Media;
using PhoenixVisualizer.Core.VFX;
using PhoenixVisualizer.Core.Models;
using System;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

public class OscilloscopeRingEffectsNode : BaseEffectNode
{
    [VFXParameter("Radius")] public int Radius { get; set; } = 100;
    [VFXParameter("Color")] public Color RingColor { get; set; } = Colors.Lime;

    protected override void InitializePorts()
    {
        AddOutput("Result");
    }

    protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audio)
    {
        var target = GetOutput<ImageBuffer>("Result");
        if (target == null) return target;

        target.Clear();
        float[] wave = audio.Waveform;
        if (wave == null || wave.Length == 0) return target;

        int cx = target.Width / 2;
        int cy = target.Height / 2;

        int prevX = cx, prevY = cy;
        for (int i = 0; i < wave.Length; i++)
        {
            double angle = i * (2 * Math.PI / wave.Length);
            int x = cx + (int)((Radius + wave[i] * 50) * Math.Cos(angle));
            int y = cy + (int)((Radius + wave[i] * 50) * Math.Sin(angle));
            DrawingUtils.DrawLine(target, prevX, prevY, x, y, RingColor);
            prevX = x; prevY = y;
        }
        DrawingUtils.DrawLine(target, prevX, prevY, cx + Radius, cy, RingColor); // close loop
        
        return target;
    }
}