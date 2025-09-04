using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.Core.Interfaces;
using PhoenixVisualizer.Core.Nodes.XSS;

namespace PhoenixVisualizer.Core.Nodes.XSS
{
    // Classic plasma effect modulated by audio spectrum for wave-like distortions
    public class XsPlasmaNode : IEffectNode
    {
        public string Name => "XS: Plasma";
        public Dictionary<string, EffectParam> Params { get; } = new()
        {
            ["scale"] = new EffectParam{ Label="Scale", Type="slider", Min=0.1f, Max=2f, FloatValue=1f },
            ["speed"] = new EffectParam{ Label="Speed", Type="slider", Min=0.1f, Max=2f, FloatValue=0.5f },
            ["reactivity"] = new EffectParam{ Label="Reactivity", Type="slider", Min=0f, Max=2f, FloatValue=1f },
            ["alpha"] = new EffectParam{ Label="Alpha", Type="slider", Min=0.1f, Max=1f, FloatValue=0.3f }
        };

        public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
        {
            var c = ctx.Canvas; if (c == null) return;
            int w = ctx.Width, h = ctx.Height;
            float scale = Params["scale"].FloatValue * 0.02f;
            float speed = Params["speed"].FloatValue;
            float react = Params["reactivity"].FloatValue;
            float alpha = Params["alpha"].FloatValue;

            float mid = 0f;
            for (int i = 8; i < Math.Min(32, spectrum.Length); i++) mid += spectrum[i];
            mid /= MathF.Max(1, Math.Min(24, spectrum.Length - 8));

            for (int y = 0; y < h; y += 4) // Skip pixels for FPS
            {
                for (int x = 0; x < w; x += 4)
                {
                    float v = MathF.Sin((x + ctx.Time * speed * 10) * scale) +
                              MathF.Sin((y + ctx.Time * speed * 5) * scale) +
                              MathF.Sin((x + y + ctx.Time * speed * 15) * scale * 0.5f);
                    v = (v + 3f) / 6f; // Normalize to 0-1
                    v += mid * react; // Audio modulation
                    v = (v % 1f + 1f) % 1f;

                    uint col = XsCommon.HsvToRgba(v, 0.8f, 1f, alpha);
                    c.FillRect(x, y, 4, 4, col);
                }
            }
        }
    }
}
