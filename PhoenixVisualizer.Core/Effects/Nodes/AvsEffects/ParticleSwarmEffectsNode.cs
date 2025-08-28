using Avalonia.Media;
using PhoenixVisualizer.Core.VFX;
using PhoenixVisualizer.Core.Models;
using System;
using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

public class ParticleSwarmEffectsNode : BaseEffectNode
{
    [VFXParameter("Count")] public int Count { get; set; } = 200;
    [VFXParameter("Speed")] public double Speed { get; set; } = 2.0;

    private readonly List<(double x, double y, double dx, double dy)> _particles = new();
    private readonly Random _rng = new();

    protected override void InitializePorts()
    {
        AddOutput("Result");
        if (_particles.Count == 0)
        {
            for (int i = 0; i < Count; i++)
                _particles.Add((_rng.NextDouble(), _rng.NextDouble(), _rng.NextDouble() - 0.5, _rng.NextDouble() - 0.5));
        }
    }

    protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audio)
    {
        var dst = GetOutput<ImageBuffer>("Result");
        if (dst == null) return null!;
        
        dst.Clear();

        for (int i = 0; i < _particles.Count; i++)
        {
            var (x, y, dx, dy) = _particles[i];
            x += dx * Speed * (1 + audio.Mid);
            y += dy * Speed * (1 + audio.Treble);
            if (x < 0) x += 1; if (y < 0) y += 1;
            if (x > 1) x -= 1; if (y > 1) y -= 1;
            int px = (int)(x * dst.Width);
            int py = (int)(y * dst.Height);
            DrawingUtils.DrawCircle(dst, px, py, 2, Colors.Cyan);
            _particles[i] = (x, y, dx, dy);
        }
        
        return dst;
    }
}
