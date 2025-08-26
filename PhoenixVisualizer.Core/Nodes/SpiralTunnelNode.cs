using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Nodes;

public class SpiralTunnelNode : IEffectNode
{
    public string Name => "Spiral Tunnel";
    public Dictionary<string, EffectParam> Params { get; } = new()
    {
        ["twist"] = new EffectParam{ Label="Twist", Type="slider", Min=0, Max=10, FloatValue=5 },
        ["zoomSpeed"] = new EffectParam{ Label="Zoom Speed", Type="slider", Min=0.1f, Max=5f, FloatValue=1f },
        ["color"] = new EffectParam{ Label="Color", Type="color", ColorValue="#FF33FF" }
    };

    public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
    {
        // TODO: spiral warp + infinite tunnel, spectrum = twist amount
    }
}