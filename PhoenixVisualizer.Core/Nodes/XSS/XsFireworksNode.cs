using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.Core.Interfaces;
using PhoenixVisualizer.Core.Nodes.XSS;

namespace PhoenixVisualizer.Core.Nodes.XSS
{
    // Audio-reactive fireworks: explosions triggered by bass hits, particles colored by frequency
    public class XsFireworksNode : IEffectNode
    {
        public string Name => "XS: Fireworks";
        public Dictionary<string, EffectParam> Params { get; } = new()
        {
            ["sensitivity"] = new EffectParam{ Label="Sensitivity", Type="slider", Min=0.1f, Max=1f, FloatValue=0.5f },
            ["particleCount"] = new EffectParam{ Label="Particles", Type="slider", Min=50, Max=300, FloatValue=150 },
            ["gravity"] = new EffectParam{ Label="Gravity", Type="slider", Min=0f, Max=2f, FloatValue=0.8f },
            ["fade"] = new EffectParam{ Label="Fade", Type="slider", Min=0.01f, Max=0.1f, FloatValue=0.05f }
        };

        private class Particle
        {
            public float X, Y, VX, VY, Life;
            public uint Color;
        }

        private List<Particle> _particles = new();
        private float _bassAccumulator;
        private Random _rnd = new();

        public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
        {
            var c = ctx.Canvas; if (c == null) return;
            int w = ctx.Width, h = ctx.Height;
            float sens = Params["sensitivity"].FloatValue;
            int maxParticles = (int)Params["particleCount"].FloatValue;
            float grav = Params["gravity"].FloatValue;
            float fade = Params["fade"].FloatValue;

            // Accumulate bass energy
            float bass = 0f;
            for (int i = 0; i < Math.Min(8, spectrum.Length); i++) bass += spectrum[i];
            bass /= MathF.Max(1, Math.Min(8, spectrum.Length));
            _bassAccumulator += bass;

            // Trigger explosion if threshold reached
            if (_bassAccumulator > sens)
            {
                float explodeX = (float)_rnd.NextDouble() * w;
                float explodeY = (float)_rnd.NextDouble() * h;
                int particleCount = (int)(maxParticles * bass);
                float hue = (ctx.Time * 0.1f + bass) % 1f;

                for (int i = 0; i < particleCount; i++)
                {
                    var p = new Particle
                    {
                        X = explodeX,
                        Y = explodeY,
                        VX = (float)(_rnd.NextDouble() * 2 - 1) * 10f * bass,
                        VY = (float)(_rnd.NextDouble() * 2 - 1) * 10f * bass,
                        Life = 1f,
                        Color = XsCommon.HsvToRgba(hue + (float)_rnd.NextDouble() * 0.1f, 0.9f, 1f)
                    };
                    _particles.Add(p);
                }
                _bassAccumulator = 0f;
            }

            // Update and draw particles
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.VY += grav;
                p.X += p.VX;
                p.Y += p.VY;
                p.Life -= fade;

                if (p.Life <= 0 || p.X < 0 || p.X > w || p.Y < 0 || p.Y > h)
                {
                    _particles.RemoveAt(i);
                    continue;
                }

                uint col = (p.Color & 0x00FFFFFF) | (uint)(p.Life * 255) << 24;
                c.FillCircle(p.X, p.Y, 2f, col);
            }

            // Limit particles for FPS
            if (_particles.Count > 2000) _particles.RemoveRange(0, _particles.Count - 2000);
        }
    }
}
