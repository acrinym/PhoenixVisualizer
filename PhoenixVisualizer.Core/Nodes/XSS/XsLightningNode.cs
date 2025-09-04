using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.Core.Interfaces;
using PhoenixVisualizer.Core.Nodes.XSS;

namespace PhoenixVisualizer.Core.Nodes.XSS
{
    // XScreensaver 'lightning' derivative — branching bolts, treble-driven
    public class XsLightningNode : IEffectNode
    {
        public string Name => "XS: Lightning";
        public Dictionary<string, EffectParam> Params { get; } = new()
        {
            ["branches"] = new EffectParam{ Label="Branches", Type="slider", Min=1, Max=12, FloatValue=4 },
            ["jitter"]   = new EffectParam{ Label="Jitter", Type="slider", Min=0f, Max=1f, FloatValue=0.35f },
            ["thickness"]= new EffectParam{ Label="Thickness", Type="slider", Min=1f, Max=6f, FloatValue=2f }
        };

        private static void Bolt(ISkiaCanvas c, float x1, float y1, float x2, float y2, int depth, float jitter, uint col)
        {
            if (depth==0) { c.DrawLine(x1,y1,x2,y2,col); return; }
            float mx = (x1+x2)/2f + (float)(Random.Shared.NextDouble()*2-1)*jitter* (x2-x1);
            float my = (y1+y2)/2f + (float)(Random.Shared.NextDouble()*2-1)*jitter* (y2-y1);
            Bolt(c, x1,y1,mx,my, depth-1, jitter*0.6f, col);
            Bolt(c, mx,my,x2,y2, depth-1, jitter*0.6f, col);
            if (Random.Shared.NextSingle()<0.25f)
                Bolt(c, mx,my, mx+(y2-y1)*0.2f, my-(x2-x1)*0.2f, depth-1, jitter*0.5f, col);
        }

        public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
        {
            var c=ctx.Canvas; if (c==null) return;
            int w=ctx.Width, h=ctx.Height;
            c.SetLineWidth(Params["thickness"].FloatValue);
            int b=(int)Params["branches"].FloatValue;
            float treble=0f; for(int i=32;i<Math.Min(128,spectrum.Length);i++) treble+=spectrum[i];
            treble /= MathF.Max(1, Math.Min(96, Math.Max(0,spectrum.Length-32)));
            float hue = (ctx.Time*0.12f + treble*0.4f)%1f;
            uint col = XsCommon.HsvToRgba(hue, 0.6f, 1f);
            for (int i=0;i<b;i++)
            {
                float x1 = Random.Shared.Next(0, w);
                float x2 = Random.Shared.Next(0, w);
                float y1 = 0;
                float y2 = h;
                Bolt(c, x1,y1,x2,y2, 4, Params["jitter"].FloatValue, col);
            }
        }
    }
}
