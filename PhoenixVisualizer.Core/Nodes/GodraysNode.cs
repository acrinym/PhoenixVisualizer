using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Nodes;

public class GodraysNode : IEffectNode
{
    public string Name => "Godrays";
    public Dictionary<string, EffectParam> Params { get; } = new()
    {
        ["intensity"] = new EffectParam{ Label="Intensity", Type="slider", Min=0, Max=5, FloatValue=1f },
        ["color"] = new EffectParam{ Label="Color", Type="color", ColorValue="#FFD700" }
    };

    public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
    {
        // TODO: implement GLSL radial blur / scattering
    }
}