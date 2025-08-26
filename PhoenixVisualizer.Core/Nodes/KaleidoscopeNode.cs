using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Nodes;

public class KaleidoscopeNode : IEffectNode
{
    public string Name => "Kaleidoscope++";
    public Dictionary<string, EffectParam> Params { get; } = new()
    {
        ["segments"] = new EffectParam{ Label="Segments", Type="slider", Min=2, Max=32, FloatValue=6 },
        ["rotate"] = new EffectParam{ Label="Rotation Speed", Type="slider", Min=-5f, Max=5f, FloatValue=0.5f },
        ["mirror"] = new EffectParam{ Label="Mirror", Type="checkbox", BoolValue=true }
    };

    public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
    {
        // TODO: symmetry-based kaleidoscope rendering
    }
}