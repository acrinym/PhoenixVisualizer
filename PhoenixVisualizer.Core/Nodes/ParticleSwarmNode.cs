using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Nodes;

public class ParticleSwarmNode : IEffectNode
{
    public string Name => "Particle Swarm";
    public Dictionary<string, EffectParam> Params { get; } = new()
    {
        ["count"] = new EffectParam{ Label="Particle Count", Type="slider", Min=100, Max=5000, FloatValue=500 },
        ["speed"] = new EffectParam{ Label="Speed", Type="slider", Min=0.1f, Max=5f, FloatValue=1f },
        ["color"] = new EffectParam{ Label="Color", Type="color", ColorValue="#00FFCC" }
    };

    public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
    {
        // TODO: FFT-driven swarm movement
    }
}