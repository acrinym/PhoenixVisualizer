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
            float red=v, green=v, blue=v;
            if (s > 0f)
            {
                float i = (float)Math.Floor(h * 6f);
                float f = h * 6f - i;
                float p = v * (1f - s);
                float q = v * (1f - s * f);
                float t = v * (1f - s * (1f - f));
                switch (((int)i) % 6)
                {
                    case 0: red=v; green=t; blue=p; break;
                    case 1: red=q; green=v; blue=p; break;
                    case 2: red=p; green=v; blue=t; break;
                    case 3: red=p; green=q; blue=v; break;
                    case 4: red=t; green=p; blue=v; break;
                    default: red=v; green=p; blue=q; break;
                }
            }
            return Rgba((byte)(red*255),(byte)(green*255),(byte)(blue*255),(byte)(a*255));
        }
    }
}
