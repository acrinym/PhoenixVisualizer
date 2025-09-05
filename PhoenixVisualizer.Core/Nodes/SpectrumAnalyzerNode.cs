using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.Core.Interfaces;

namespace PhoenixVisualizer.Core.Nodes
{
    public class SpectrumAnalyzerNode : IEffectNode
    {
        public string Name => "SpectrumAnalyzer";
        public Dictionary<string, EffectParam> Params { get; } = new()
        {
            ["bars"] = new EffectParam{ Label="Bars", Type="checkbox", BoolValue=true },
            ["smoothing"] = new EffectParam{ Label="Smoothing", Type="slider", Min=0f, Max=1f, FloatValue=0.3f },
            ["height"] = new EffectParam{ Label="Height", Type="slider", Min=0.1f, Max=1f, FloatValue=0.8f },
            ["color"] = new EffectParam{ Label="Color", Type="color", StringValue="#00FFFF" }
        };

        public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
        {
            var canvas = ctx.Canvas;
            if (canvas == null || spectrum == null || spectrum.Length == 0) return;

            int w = ctx.Width, h = ctx.Height;
            bool showBars = Params["bars"].BoolValue;
            float smoothing = Params["smoothing"].FloatValue;
            float height = Params["height"].FloatValue;
            
            // Parse color
            var colorStr = Params["color"].StringValue;
            uint color = 0xFF00FFFF; // Default cyan
            
            if (colorStr.StartsWith("#") && colorStr.Length >= 7)
            {
                try
                {
                    color = Convert.ToUInt32(colorStr.Substring(1), 16);
                    if (colorStr.Length == 7) color |= 0xFF000000;
                }
                catch { /* use default */ }
            }

            if (showBars)
            {
                // Draw spectrum bars
                int barCount = Math.Min(64, spectrum.Length);
                float barWidth = (float)w / barCount;
                
                for (int i = 0; i < barCount; i++)
                {
                    float value = spectrum[i] * height * h;
                    float x = i * barWidth;
                    float y = h - value;
                    
                    canvas.FillRect(x, y, barWidth - 1, value, color);
                }
            }
            else
            {
                // Draw spectrum line
                canvas.SetLineWidth(2f);
                int pointCount = Math.Min(spectrum.Length, w);
                float stepX = (float)w / pointCount;
                
                for (int i = 1; i < pointCount; i++)
                {
                    float x1 = (i - 1) * stepX;
                    float x2 = i * stepX;
                    float y1 = h - spectrum[i - 1] * height * h;
                    float y2 = h - spectrum[i] * height * h;
                    
                    canvas.DrawLine(x1, y1, x2, y2, color);
                }
            }
        }
    }
}

