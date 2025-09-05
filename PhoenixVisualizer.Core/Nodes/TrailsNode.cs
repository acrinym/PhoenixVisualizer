using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.Core.Interfaces;

namespace PhoenixVisualizer.Core.Nodes
{
    public class TrailsNode : IEffectNode
    {
        public string Name => "Trails";
        public Dictionary<string, EffectParam> Params { get; } = new()
        {
            ["decay"] = new EffectParam{ Label="Decay", Type="slider", Min=0f, Max=1f, FloatValue=0.9f },
            ["color"] = new EffectParam{ Label="Trail Color", Type="color", StringValue="#000000" }
        };

        public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
        {
            var canvas = ctx.Canvas;
            if (canvas == null) return;

            float decay = Params["decay"].FloatValue;
            
            // Parse trail color
            var colorStr = Params["color"].StringValue;
            uint trailColor = 0xFF000000; // Default black
            
            if (colorStr.StartsWith("#") && colorStr.Length >= 7)
            {
                try
                {
                    trailColor = Convert.ToUInt32(colorStr.Substring(1), 16);
                    if (colorStr.Length == 7) trailColor |= 0xFF000000;
                }
                catch { /* use default */ }
            }

            // Apply trail effect by fading the canvas
            canvas.Fade(trailColor, 1f - decay);
        }
    }
}

