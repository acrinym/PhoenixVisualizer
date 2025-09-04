using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.Core.Interfaces;
using PhoenixVisualizer.Core.Nodes.XSS;

namespace PhoenixVisualizer.Core.Nodes.XSS
{
    // XScreensaver 'rotor' derivative (swirly rotor lines) — audio-reactive
    public class XsRotorNode : IEffectNode
    {
        public string Name => "XS: Rotor";
        public Dictionary<string, EffectParam> Params { get; } = new()
        {
            ["count"] = new EffectParam{ Label="Arms", Type="slider", Min=1, Max=12, FloatValue=4 },
            ["size"]  = new EffectParam{ Label="Radius", Type="slider", Min=0.05f, Max=0.9f, FloatValue=0.45f },
            ["speed"] = new EffectParam{ Label="Spin Speed", Type="slider", Min=-4f, Max=4f, FloatValue=0.7f },
            ["trail"] = new EffectParam{ Label="Trail", Type="slider", Min=0f, Max=1f, FloatValue=0.65f },
            ["fftReactivity"] = new EffectParam{ Label="FFT Reactivity", Type="slider", Min=0f, Max=2f, FloatValue=1.0f },
        };

        public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
        {
            var c = ctx.Canvas; if (c == null) return;
            int w = ctx.Width, h = ctx.Height;
            float cx = w * 0.5f, cy = h * 0.5f;
            int n = (int)Params["count"].FloatValue;
            float R = MathF.Min(w, h) * Params["size"].FloatValue;
            float spin = Params["speed"].FloatValue * 0.6f;
            float fft = 0f;
            for (int i=0;i<Math.Min(64, spectrum.Length);i++) fft += spectrum[i];
            fft = MathF.Min(1f, fft / MathF.Max(1, Math.Min(64, spectrum.Length)));
            float amp = 0.6f + Params["fftReactivity"].FloatValue * fft;

            // subtle fade for trails
            c.Fade(0x00000000, Math.Clamp(Params["trail"].FloatValue * 0.1f, 0, 0.25f));

            c.SetLineWidth(2f);
            for (int k=0;k<n;k++)
            {
                float t = ctx.Time * spin + k * (MathF.PI * 2 / n);
                int segs = 200;
                Span<(float x, float y)> pts = stackalloc (float, float)[segs];
                for (int i=0;i<segs;i++)
                {
                    float a = t + i * 0.05f;
                    float r = R * (0.6f + 0.4f*MathF.Sin(a*1.7f + k)) * amp;
                    float x = cx + r * MathF.Cos(a);
                    float y = cy + r * MathF.Sin(a*1.1f);
                    pts[i] = (x,y);
                }
                uint col = XsCommon.HsvToRgba((k/(float)n + ctx.Time*0.03f)%1f, 0.9f, 0.95f);
                c.DrawPolyline(pts, col);
            }
        }
    }
}
