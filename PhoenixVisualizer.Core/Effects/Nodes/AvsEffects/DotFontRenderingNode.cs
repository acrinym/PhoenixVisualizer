using Avalonia.Media;
using Avalonia.Media.Imaging;
using PhoenixVisualizer.Core.VFX;
using PhoenixVisualizer.Core.Models;
using System;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

public class DotFontRenderingNode : BaseEffectNode
{
    [VFXParameter("Text")] public string Text { get; set; } = "Phoenix";
    [VFXParameter("Size")] public int Size { get; set; } = 24;
    [VFXParameter("Color")] public Color TextColor { get; set; } = Colors.Orange;

    protected override void InitializePorts()
    {
        AddOutput("Result");
    }

    protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audio)
    {
        var target = GetOutput<ImageBuffer>("Result");
        if (target == null) return null!;

        target.Clear();
        if (string.IsNullOrEmpty(Text)) return target;

        var helper = new DrawingContextHelper();
        int x = target.Width / 2 - (Text.Length * Size / 4);
        int y = target.Height / 2 + (int)(Math.Sin(audio.Time) * 20);
        helper.DrawText(target, Text, Size, TextColor, x, y);
        
        return target;
    }
}