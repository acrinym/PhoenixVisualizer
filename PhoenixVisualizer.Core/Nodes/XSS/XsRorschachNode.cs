using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.Core.Interfaces;
using PhoenixVisualizer.Core.Nodes.XSS;

namespace PhoenixVisualizer.Core.Nodes.XSS
{
    // XScreensaver 'rorschach' derivative — mirrored ink-blot; reacts to bass
    public class XsRorschachNode : IEffectNode
    {
        public string Name => "XS: Rorschach";
        public Dictionary<string, EffectParam> Params { get; } = new()
        {
            ["symX"] = new EffectParam{ Label="X Symmetry", Type="checkbox", BoolValue=true },
            ["symY"] = new EffectParam{ Label="Y Symmetry", Type="checkbox", BoolValue=false },
            ["blobCount"] = new EffectParam{ Label="Blob Count", Type="slider", Min=5, Max=200, FloatValue=60 },
            ["s"] = new EffectParam{ Label="Saturation", Type="slider", Min=0f, Max=1f, FloatValue=0.9f },
            ["v"] = new EffectParam{ Label="Brightness", Type="slider", Min=0.2f, Max=1f, FloatValue=0.95f },
        };

        public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
        {
            var c = ctx.Canvas; if (c==null) return;
            int w=ctx.Width, h=ctx.Height;
            float cx=w*0.5f, cy=h*0.5f;
            int blobs = (int)Params["blobCount"].FloatValue;
            bool sx = Params["symX"].BoolValue, sy = Params["symY"].BoolValue;
            float bass=0f; for(int i=0;i<Math.Min(8,spectrum.Length);i++) bass+=spectrum[i]; bass/=MathF.Max(1,Math.Min(8,spectrum.Length));
            float jitter = 1f + bass*0.8f;

            for (int i=0;i<blobs;i++)
            {
                float t = i/(float)blobs + ctx.Time*0.11f;
                float rx = (MathF.Sin(t*3.1f)*0.35f + 0.5f) * w*0.45f;
                float ry = (MathF.Cos(t*2.7f)*0.35f + 0.5f) * h*0.45f;
                float r  = (20 + (i%7)*6) * jitter;
                uint col = XsCommon.HsvToRgba((t*0.23f)%1f, Params["s"].FloatValue, Params["v"].FloatValue);
                // draw circle and its mirrors
                c.FillCircle(cx + rx, cy + ry, r, col);
                if (sx) c.FillCircle(cx - rx, cy + ry, r, col);
                if (sy) c.FillCircle(cx + rx, cy - ry, r, col);
                if (sx && sy) c.FillCircle(cx - rx, cy - ry, r, col);
            }
        }
    }
}
