using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Nodes;

public class AuroraRibbonsNode : IEffectNode
{
    public string Name => "Aurora Ribbons";
    public Dictionary<string, EffectParam> Params { get; } = new()
    {
        ["amplitude"] = new EffectParam{ Label="Amplitude", Type="slider", Min=0.1f, Max=2f, FloatValue=1f },
        ["speed"] = new EffectParam{ Label="Speed", Type="slider", Min=0.1f, Max=5f, FloatValue=1f },
        ["color"] = new EffectParam{ Label="Color", Type="color", ColorValue="#00FFAA" }
    };

    public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
    {
        // TODO: draw sine-driven colored ribbons with spectrum amplitude
    }
}