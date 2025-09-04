using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.Core.Interfaces;
using PhoenixVisualizer.Core.Nodes.XSS;

namespace PhoenixVisualizer.Core.Nodes.XSS
{
    // XScreensaver 'lcdscrub' derivative — patterns cycling; useful as layer
    public class XsLcdScrubNode : IEffectNode
    {
        public string Name => "XS: LCD Scrub";
        public Dictionary<string, EffectParam> Params { get; } = new()
        {
            ["mode"] = new EffectParam{ Label="Mode", Type="dropdown", StringValue="RGB", Options=new(){ "HORIZ_W","HORIZ_B","VERT_W","VERT_B","DIAG_W","DIAG_B","WHITE","BLACK","RGB","RANDOM" }},
            ["delay"] = new EffectParam{ Label="Delay", Type="slider", Min=0f, Max=2f, FloatValue=0.15f },
            ["spread"] = new EffectParam{ Label="Spread", Type="slider", Min=2, Max=64, FloatValue=8 },
            ["alpha"] = new EffectParam{ Label="Alpha", Type="slider", Min=0f, Max=1f, FloatValue=0.15f }
        };
        private float _t=0;

        public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
        {
            var c=ctx.Canvas; if (c==null) return;
            _t += MathF.Max(0.001f, Params["delay"].FloatValue) * 0.5f;
            int w=ctx.Width, h=ctx.Height, step=Math.Max(2,(int)Params["spread"].FloatValue);
            string mode = Params["mode"].StringValue;
            float alpha = Params["alpha"].FloatValue;
            if (mode=="RANDOM" && ((int)(_t*10))%14==0)
                mode = new[]{ "HORIZ_W","VERT_W","DIAG_W","RGB" }[Random.Shared.Next(0,4)];
            if (mode=="WHITE"||mode=="BLACK")
            {
                uint col = mode=="WHITE" ? 0xFFFFFFFF : 0xFF000000;
                c.FillRect(0,0,w,h, col);
                return;
            }
            for (int y=0;y<h;y+=step)
            {
                for (int x=0;x<w;x+=step)
                {
                    uint col = 0xFFFFFFFF;
                    switch (mode)
                    {
                        case "HORIZ_W": col = XsCommon.Rgba(255,255,255,(byte)(alpha*255)); break;
                        case "HORIZ_B": col = XsCommon.Rgba(0,0,0,(byte)(alpha*255)); break;
                        case "VERT_W":  col = XsCommon.Rgba(255,255,255,(byte)(alpha*255)); break;
                        case "VERT_B":  col = XsCommon.Rgba(0,0,0,(byte)(alpha*255)); break;
                        case "DIAG_W":  col = XsCommon.Rgba(255,255,255,(byte)(alpha*255)); break;
                        case "DIAG_B":  col = XsCommon.Rgba(0,0,0,(byte)(alpha*255)); break;
                        case "RGB":
                            int sel = ((x/step)+(y/step)+(int)(_t*10))%3;
                            col = sel==0 ? XsCommon.Rgba(255,0,0,(byte)(alpha*255)) : sel==1 ? XsCommon.Rgba(0,255,0,(byte)(alpha*255)) : XsCommon.Rgba(0,0,255,(byte)(alpha*255));
                            break;
                    }
                    c.FillRect(x,y, step,step, col);
                }
            }
        }
    }
}
