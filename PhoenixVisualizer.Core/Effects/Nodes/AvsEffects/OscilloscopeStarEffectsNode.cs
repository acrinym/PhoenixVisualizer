using Avalonia.Media;
using PhoenixVisualizer.Core.VFX;
using PhoenixVisualizer.Core.Models;
using System;
using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

public class OscilloscopeStarEffectsNode : BaseEffectNode
{
    private readonly Random _rng = new();
    private readonly List<(int x, int y)> _stars = new();

    [VFXParameter("Star Count")] public int StarCount { get; set; } = 150;
    [VFXParameter("Color")] public Color StarColor { get; set; } = Colors.White;

    protected override void InitializePorts()
    {
        AddOutput("Result");
    }

    protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audio)
    {
        var target = GetOutput<ImageBuffer>("Result");
        if (target == null) return target;

        if (_stars.Count != StarCount)
        {
            _stars.Clear();
            for (int i = 0; i < StarCount; i++)
            {
                _stars.Add((_rng.Next(target.Width), _rng.Next(target.Height)));
            }
        }

        target.Clear();
        for (int i = 0; i < _stars.Count; i++)
        {
            var (x, y) = _stars[i];
            int offset = (int)(Math.Sin(audio.Time + i) * 20);
            int nx = (x + offset) % target.Width;
            int ny = (y + offset) % target.Height;
            if (nx >= 0 && nx < target.Width && ny >= 0 && ny < target.Height)
                target[nx, ny] = StarColor;
        }
        
        return target;
    }
}