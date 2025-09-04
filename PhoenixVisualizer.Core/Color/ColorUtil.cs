namespace PhoenixVisualizer.Core.ColorUtils
{
    public static class ColorUtil
    {
        public static uint HsvToRgb(float h, float s, float v)
        {
            h = (h % 360f + 360f) % 360f;
            float c = v * s;
            float x = c * (1 - MathF.Abs((h / 60f) % 2 - 1));
            float m = v - c;
            float r=0,g=0,b=0;
            if (h < 60)      { r=c; g=x; b=0; }
            else if (h <120) { r=x; g=c; b=0; }
            else if (h <180) { r=0; g=c; b=x; }
            else if (h <240) { r=0; g=x; b=c; }
            else if (h <300) { r=x; g=0; b=c; }
            else             { r=c; g=0; b=x; }
            byte R=(byte)((r+m)*255), G=(byte)((g+m)*255), B=(byte)((b+m)*255);
            return 0xFF000000u | ((uint)R<<16) | ((uint)G<<8) | (uint)B;
        }
    }
}
