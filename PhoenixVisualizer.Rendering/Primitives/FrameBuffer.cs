using System;
using System.Collections.Generic;
using System.Globalization;
using SkiaSharp;

namespace PhoenixVisualizer.Rendering
{
    /// <summary>
    /// Lightweight vector frame buffer: list of line segments with color & thickness.
    /// Allows affine transforms and color/thickness operations, then flush to ISkiaCanvas.
    /// </summary>
    public sealed class FrameBuffer
    {
        public sealed class Seg
        {
            public float X1, Y1, X2, Y2;
            public uint Color;   // RGBA
            public float Thick;
        }

        private readonly List<Seg> _segs = new();
        public IReadOnlyList<Seg> Segments => _segs;
        public void Clear() => _segs.Clear();

        public void AddLine(float x1, float y1, float x2, float y2, uint color, float thick)
            => _segs.Add(new Seg { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Color = color, Thick = thick });

        public void AddPolyline(ReadOnlySpan<float> xs, ReadOnlySpan<float> ys, uint color, float thick)
        {
            if (xs.Length != ys.Length || xs.Length < 2) return;
            for (int i = 1; i < xs.Length; i++)
                AddLine(xs[i - 1], ys[i - 1], xs[i], ys[i], color, thick);
        }

        // Affine transform [ x' y' 1 ] = [ x y 1 ] * M
        public void Transform(float m11, float m12, float m13, float m21, float m22, float m23)
        {
            for (int i = 0; i < _segs.Count; i++)
            {
                var s = _segs[i];
                (s.X1, s.Y1) = Apply(s.X1, s.Y1);
                (s.X2, s.Y2) = Apply(s.X2, s.Y2);
                _segs[i] = s;
            }
            (float, float) Apply(float x, float y)
                => (x * m11 + y * m21 + m13, x * m12 + y * m22 + m23);
        }

        public void Translate(float dx, float dy) => Transform(1, 0, dx, 0, 1, dy);
        public void Scale(float sx, float sy) => Transform(sx, 0, 0, 0, sy, 0);
        public void RotateDegrees(float deg, float cx, float cy)
        {
            var rad = deg * (float)Math.PI / 180f;
            var c = (float)Math.Cos(rad); var s = (float)Math.Sin(rad);
            // T(-C) * R * T(C)
            Translate(-cx, -cy);
            Transform(c, s, 0, -s, c, 0);
            Translate(cx, cy);
        }

        public void MultiplyAlpha(float mul)
        {
            mul = Math.Clamp(mul, 0f, 1f);
            for (int i = 0; i < _segs.Count; i++)
            {
                var s = _segs[i];
                var a = (byte)(s.Color & 0xFF);
                var na = (byte)Math.Clamp((int)(a * mul), 0, 255);
                s.Color = (s.Color & 0xFFFFFF00u) | na;
                _segs[i] = s;
            }
        }
        public void MultiplyThickness(float mul)
        {
            for (int i = 0; i < _segs.Count; i++) _segs[i].Thick = MathF.Max(0.1f, _segs[i].Thick * mul);
        }
        public void Tint(uint rgba)
        {
            byte tr = (byte)((rgba >> 24) & 0xFF);
            byte tg = (byte)((rgba >> 16) & 0xFF);
            byte tb = (byte)((rgba >> 8) & 0xFF);
            for (int i = 0; i < _segs.Count; i++)
            {
                var s = _segs[i];
                byte r = (byte)((s.Color >> 24) & 0xFF);
                byte g = (byte)((s.Color >> 16) & 0xFF);
                byte b = (byte)((s.Color >> 8)  & 0xFF);
                byte a = (byte)(s.Color & 0xFF);
                r = (byte)((r * tr) / 255);
                g = (byte)((g * tg) / 255);
                b = (byte)((b * tb) / 255);
                s.Color = ((uint)r << 24) | ((uint)g << 16) | ((uint)b << 8) | a;
                _segs[i] = s;
            }
        }

        public void Flush(ISkiaCanvas canvas)
        {
            foreach (var s in _segs)
                canvas.DrawLine(s.X1, s.Y1, s.X2, s.Y2, s.Color, s.Thick);
        }

        public static uint ParseColor(string s, uint fallback)
        {
            if (string.IsNullOrWhiteSpace(s)) return fallback;
            var hex = s.Trim().TrimStart('#');
            if (hex.Length == 6) hex += "FF";
            if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgba))
                return rgba;
            return fallback;
        }
    }
}
