using System;

namespace PhoenixVisualizer.Core.Nodes.XSS
{
    internal static class XsCommon
    {
        public static uint Rgba(byte r, byte g, byte b, byte a=255)
            => (uint)(a << 24 | r << 16 | g << 8 | b);

        public static uint HsvToRgba(float h, float s, float v, float a = 1f)
        {
            h = (h % 1f + 1f) % 1f;
            s = Math.Clamp(s, 0f, 1f);
            v = Math.Clamp(v, 0f, 1f);
            float r=v, g=v, b=v;
            if (s > 0f)
            {
                float i = (float)Math.Floor(h * 6f);
                float f = h * 6f - i;
                float p = v * (1f - s);
                float q = v * (1f - s * f);
                float t = v * (1f - s * (1f - f));
                switch (((int)i) % 6)
                {
                    case 0: r=v; g=t; b=p; break;
                    case 1: r=q; g=v; b=p; break;
                    case 2: r=p; g=v; b=t; break;
                    case 3: r=p; g=q; b=v; break;
                    case 4: r=t; g=p; b=v; break;
                    default: r=v; g=p; b=q; break;
                }
            }
            return Rgba((byte)(r*255),(byte)(g*255),(byte)(b*255),(byte)(a*255));
        }
    }
}