using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.Core.Interfaces;
using PhoenixVisualizer.Core.Nodes.XSS;

namespace PhoenixVisualizer.Core.Nodes.XSS
{
    // XScreensaver 'lisa' derivative — full-loop lissajous, audio-colored
    public class XsLisaNode : IEffectNode
    {
        public string Name => "XS: Lisa (Lissajous)";
        public Dictionary<string, EffectParam> Params { get; } = new()
        {
            ["a"] = new EffectParam{ Label="A", Type="slider", Min=1, Max=12, FloatValue=3 },
            ["b"] = new EffectParam{ Label="B", Type="slider", Min=1, Max=12, FloatValue=4 },
            ["delta"] = new EffectParam{ Label="Phase", Type="slider", Min=0f, Max=MathF.PI*2, FloatValue=0.0f },
            ["thickness"] = new EffectParam{ Label="Thickness", Type="slider", Min=1f, Max=8f, FloatValue=2f },
            ["colorDrift"] = new EffectParam{ Label="Color Drift", Type="slider", Min=0f, Max=1f, FloatValue=0.25f }
        };

        public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
        {
            var c = ctx.Canvas; if (c == null) return;
            int w=ctx.Width, h=ctx.Height;
            float cx=w*0.5f, cy=h*0.5f;
            float A = (MathF.Min(w,h)*0.42f);
            float a = MathF.Max(1f, Params["a"].FloatValue);
            float b = MathF.Max(1f, Params["b"].FloatValue);
            float d = Params["delta"].FloatValue + ctx.Time*0.12f;
            c.SetLineWidth(Params["thickness"].FloatValue);

            int segs=800;
            Span<(float x,float y)> pts = stackalloc (float, float)[segs];
            for (int i=0;i<segs;i++)
            {
                float t = (i/(float)(segs-1))*MathF.PI*2;
                float x = cx + A * MathF.Sin(a*t + d);
                float y = cy + A * MathF.Sin(b*t);
                pts[i]=(x,y);
            }
            float e=0f; for(int i=0;i<Math.Min(64,spectrum.Length);i++) e+=spectrum[i]; e/=MathF.Max(1,Math.Min(64,spectrum.Length));
            float hue = (ctx.Time*0.05f + e*Params["colorDrift"].FloatValue)%1f;
            c.DrawPolyline(pts, XsCommon.HsvToRgba(hue, 0.8f, 1f));
        }
    }
}