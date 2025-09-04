using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.Core.Interfaces;
using PhoenixVisualizer.Core.Nodes.XSS;

namespace PhoenixVisualizer.Core.Nodes.XSS
{
    // Swirling vortex that pulls in spectrum-based particles, Shadertoy-inspired
    public class XsVortexNode : IEffectNode
    {
        public string Name => "XS: Vortex";
        public Dictionary<string, EffectParam> Params { get; } = new()
        {
            ["strength"] = new EffectParam{ Label="Strength", Type="slider", Min=0.1f, Max=2f, FloatValue=1f },
            ["particleCount"] = new EffectParam{ Label="Particles", Type="slider", Min=100, Max=1000, FloatValue=500 },
            ["size"] = new EffectParam{ Label="Size", Type="slider", Min=1f, Max=5f, FloatValue=2f },
            ["trail"] = new EffectParam{ Label="Trail", Type="slider", Min=0f, Max=0.2f, FloatValue=0.05f }
        };

        private class Particle
        {
            public float X, Y, Angle, Radius, Speed;
        }

        private List<Particle> _particles = new();
        private Random _rnd = new();

        public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
        {
            var c = ctx.Canvas; if (c == null) return;
            int w = ctx.Width, h = ctx.Height;
            float cx = w * 0.5f, cy = h * 0.5f;
            float strength = Params["strength"].FloatValue;
            int maxParticles = (int)Params["particleCount"].FloatValue;
            float psize = Params["size"].FloatValue;
            float trail = Params["trail"].FloatValue;

            // Fade for trails
            c.Fade(0x00000000, trail);

            // Add new particles based on spectrum
            float energy = 0f;
            for (int i = 0; i < spectrum.Length; i++) energy += spectrum[i];
            energy /= spectrum.Length;
            int newParticles = (int)(energy * 20f);
            for (int i = 0; i < newParticles; i++)
            {
                var p = new Particle
                {
                    X = (float)_rnd.NextDouble() * w,
                    Y = (float)_rnd.NextDouble() * h,
                    Angle = (float)_rnd.NextDouble() * MathF.PI * 2,
                    Radius = MathF.Min(w, h) * 0.5f,
                    Speed = (float)_rnd.NextDouble() * 0.5f + 0.5f
                };
                _particles.Add(p);
            }

            // Update and draw
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                float dx = cx - p.X, dy = cy - p.Y;
                float dist = MathF.Sqrt(dx*dx + dy*dy);
                if (dist < 10) { _particles.RemoveAt(i); continue; }

                p.Angle += strength / dist;
                p.Radius -= p.Speed;
                p.X = cx + p.Radius * MathF.Cos(p.Angle);
                p.Y = cy + p.Radius * MathF.Sin(p.Angle);

                float hue = (dist / MathF.Min(w, h)) % 1f;
                c.FillCircle(p.X, p.Y, psize, XsCommon.HsvToRgba(hue, 0.9f, 1f));
            }

            if (_particles.Count > maxParticles) _particles.RemoveRange(0, _particles.Count - maxParticles);
        }
    }
}
