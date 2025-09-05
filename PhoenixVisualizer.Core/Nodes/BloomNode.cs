using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.Core.Interfaces;

namespace PhoenixVisualizer.Core.Nodes
{
    public class BloomNode : IEffectNode
    {
        public string Name => "Bloom";
        public Dictionary<string, EffectParam> Params { get; } = new()
        {
            ["radius"] = new EffectParam{ Label="Radius", Type="slider", Min=1f, Max=20f, FloatValue=5f },
            ["intensity"] = new EffectParam{ Label="Intensity", Type="slider", Min=0f, Max=2f, FloatValue=0.8f },
            ["threshold"] = new EffectParam{ Label="Threshold", Type="slider", Min=0f, Max=1f, FloatValue=0.5f }
        };

        public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
        {
            var canvas = ctx.Canvas;
            if (canvas == null) return;

            float radius = Params["radius"].FloatValue;
            float intensity = Params["intensity"].FloatValue;
            float threshold = Params["threshold"].FloatValue;
            
            int w = ctx.Width, h = ctx.Height;
            
            // Simple bloom effect - draw bright spots with glow
            if (spectrum != null && spectrum.Length > 0)
            {
                float maxEnergy = 0f;
                for (int i = 0; i < spectrum.Length; i++)
                {
                    maxEnergy = Math.Max(maxEnergy, spectrum[i]);
                }
                
                if (maxEnergy > threshold)
                {
                    // Draw bloom at center
                    float centerX = w * 0.5f;
                    float centerY = h * 0.5f;
                    float bloomSize = radius * maxEnergy * intensity;
                    
                    // Draw multiple circles for bloom effect
                    for (int i = 0; i < 5; i++)
                    {
                        float r = bloomSize * (i + 1) / 5f;
                        uint alphaValue = (uint)(255 * intensity * (1f - i / 5f));
                        uint color = 0xFFFFFF | (alphaValue << 24);
                        
                        canvas.DrawCircle(centerX, centerY, r, color, true);
                    }
                }
            }
        }
    }
}

