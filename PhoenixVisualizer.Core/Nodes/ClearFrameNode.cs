using System.Collections.Generic;
using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.Core.Interfaces;

namespace PhoenixVisualizer.Core.Nodes
{
    public class ClearFrameNode : IEffectNode
    {
        public string Name => "ClearFrame";
        public Dictionary<string, EffectParam> Params { get; } = new()
        {
            ["color"] = new EffectParam{ Label="Color", Type="color", StringValue="#000000" }
        };

        public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
        {
            var canvas = ctx.Canvas;
            if (canvas == null) return;

            // Parse color (simple hex for now)
            var colorStr = Params["color"].StringValue;
            uint color = 0xFF000000; // Default black
            
            if (colorStr.StartsWith("#") && colorStr.Length >= 7)
            {
                try
                {
                    color = Convert.ToUInt32(colorStr.Substring(1), 16);
                    if (colorStr.Length == 7) color |= 0xFF000000; // Add alpha if missing
                }
                catch { /* use default */ }
            }

            canvas.Clear(color);
        }
    }
}

