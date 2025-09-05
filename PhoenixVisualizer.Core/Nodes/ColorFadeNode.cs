using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.Core.Interfaces;

namespace PhoenixVisualizer.Core.Nodes
{
    public class ColorFadeNode : IEffectNode
    {
        public string Name => "ColorFade";
        public Dictionary<string, EffectParam> Params { get; } = new()
        {
            ["mode"] = new EffectParam{ Label="Mode", Type="dropdown", StringValue="HSV", Options=new List<string>{"HSV", "RGB", "Rainbow"} },
            ["speed"] = new EffectParam{ Label="Speed", Type="slider", Min=0f, Max=2f, FloatValue=0.3f },
            ["offset"] = new EffectParam{ Label="Offset", Type="slider", Min=0f, Max=1f, FloatValue=0f }
        };

        public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
        {
            var canvas = ctx.Canvas;
            if (canvas == null) return;

            string mode = Params["mode"].StringValue;
            float speed = Params["speed"].FloatValue;
            float offset = Params["offset"].FloatValue;
            
            // This is a color effect - it would typically modify the color of other elements
            // For now, we'll draw a color gradient overlay to demonstrate the effect
            int w = ctx.Width, h = ctx.Height;
            
            for (int x = 0; x < w; x += 4)
            {
                float t = (ctx.Time * speed + offset + (float)x / w) % 1f;
                uint color = mode switch
                {
                    "HSV" => HsvToRgba(t, 1f, 1f),
                    "RGB" => RgbCycle(t),
                    "Rainbow" => RainbowColor(t),
                    _ => 0xFFFFFFFF
                };
                
                canvas.FillRect(x, 0, 4, h, color);
            }
        }

        private static uint HsvToRgba(float h, float s, float v)
        {
            float c = v * s;
            float x = c * (1 - Math.Abs((h * 6) % 2 - 1));
            float m = v - c;
            
            float r, g, b;
            if (h < 1f/6f) { r = c; g = x; b = 0; }
            else if (h < 2f/6f) { r = x; g = c; b = 0; }
            else if (h < 3f/6f) { r = 0; g = c; b = x; }
            else if (h < 4f/6f) { r = 0; g = x; b = c; }
            else if (h < 5f/6f) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }
            
            return ((uint)((r + m) * 255) << 16) | ((uint)((g + m) * 255) << 8) | (uint)((b + m) * 255) | 0xFF000000;
        }

        private static uint RgbCycle(float t)
        {
            float r = (float)((Math.Sin(t * Math.PI * 2) + 1) * 0.5);
            float g = (float)((Math.Sin(t * Math.PI * 2 + Math.PI * 2/3) + 1) * 0.5);
            float b = (float)((Math.Sin(t * Math.PI * 2 + Math.PI * 4/3) + 1) * 0.5);
            
            return ((uint)(r * 255) << 16) | ((uint)(g * 255) << 8) | (uint)(b * 255) | 0xFF000000;
        }

        private static uint RainbowColor(float t)
        {
            return HsvToRgba(t, 1f, 1f);
        }
    }
}
