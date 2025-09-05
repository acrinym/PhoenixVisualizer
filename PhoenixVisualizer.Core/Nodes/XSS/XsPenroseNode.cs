using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.Core.Interfaces;
using PhoenixVisualizer.Core.Nodes.XSS;

namespace PhoenixVisualizer.Core.Nodes.XSS
{
    // XScreensaver 'penrose' inspired — quasiperiodic kite/dart tiling approximation
    public class XsPenroseNode : IEffectNode
    {
        public string Name => "XS: Penrose Tiling (approx)";
        public Dictionary<string, EffectParam> Params { get; } = new()
        {
            ["density"] = new EffectParam{ Label="Density", Type="slider", Min=50, Max=2000, FloatValue=400 },
            ["outline"] = new EffectParam{ Label="Outline", Type="checkbox", BoolValue=true },
            ["sat"] = new EffectParam{ Label="Saturation", Type="slider", Min=0f, Max=1f, FloatValue=0.8f },
        };

        private static (float x,float y) Rot((float x,float y) p, float a)
            => (p.x*MathF.Cos(a)-p.y*MathF.Sin(a), p.x*MathF.Sin(a)+p.y*MathF.Cos(a));

        public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
        {
            var c = ctx.Canvas; if (c==null) return;
            int w=ctx.Width, h=ctx.Height;
            float cx=w*0.5f, cy=h*0.5f;
            int n = (int)Params["density"].FloatValue;
            float phi = (1f+MathF.Sqrt(5f))*0.5f;
            float baseR = MathF.Min(w,h)*0.45f;
            float time = ctx.Time*0.07f;
            bool outline = Params["outline"].BoolValue;
            float s = Params["sat"].FloatValue;

            for (int i=0;i<n;i++)
            {
                float t = (i/(float)n);
                float a = t * MathF.PI*2 * phi + time;
                float r = baseR * (0.1f + 0.9f*t);
                var p = (cx + r*MathF.Cos(a), cy + r*MathF.Sin(a));
                // build a thin kite-ish quad
                var v0 = (p.Item1, p.Item2);
                var v1 = (p.Item1 + 20*MathF.Cos(a+0.4f), p.Item2 + 20*MathF.Sin(a+0.4f));
                var v2 = (p.Item1 + 60*MathF.Cos(a), p.Item2 + 60*MathF.Sin(a));
                var v3 = (p.Item1 + 20*MathF.Cos(a-0.4f), p.Item2 + 20*MathF.Sin(a-0.4f));

                Span<(float x,float y)> poly = stackalloc (float, float)[4]{v0,v1,v2,v3};
                uint col = XsCommon.HsvToRgba((t+time*0.1f)%1f, s, 0.95f);
                c.DrawPolygon(poly, col, filled:true);
                if (outline)
                    c.DrawPolyline(poly, 0xFFFFFFFF);
            }
        }
    }
}