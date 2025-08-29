using System;
using System.Collections.Generic;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Classic Windows 95 Beziers screensaver - Smooth curved lines flowing across the screen
/// Features Bézier curves that create flowing, organic patterns with audio reactivity
/// </summary>
public sealed class Win95Beziers : IVisualizerPlugin
{
    public string Id => "win95_beziers";
    public string DisplayName => "✨ Win95 Béziers";

    private int _width, _height;
    private float _time;
    private Random _random = new();

    // Bézier system constants (based on original Win95 implementation)
    private const int MAX_CURVES = 8;
    private const int CURVE_SEGMENTS = 50;
    private const float CURVE_SPEED = 1.5f;
    private const float CONTROL_POINT_VARIANCE = 100f;

    // Individual Bézier curve
    private struct BezierCurve
    {
        public float X, Y; // Current position
        public float VelX, VelY; // Movement velocity
        public (float x, float y)[] ControlPoints; // 4 control points for cubic Bézier
        public uint Color;
        public float Thickness;
        public float Phase; // For animation
        public float Length; // Curve length factor
        public List<(float x, float y, float alpha)> Trail; // Trail points
    }

    // Active curves
    private List<BezierCurve> _curves = new();

    // Colors inspired by Windows 95 Bézier screensaver
    private readonly uint[] _bezierColors = new uint[]
    {
        0xFFFF0000, // Red
        0xFF00FF00, // Green
        0xFF0000FF, // Blue
        0xFFFFFF00, // Yellow
        0xFFFF00FF, // Magenta
        0xFF00FFFF, // Cyan
        0xFF00FF80, // Spring green
        0xFF8000FF, // Violet
        0xFFFF8000, // Orange
        0xFF80FF00, // Lime
        0xFF0080FF, // Azure
        0xFFFF0080  // Pink
    };

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;

        // Initialize with several curves
        for (int i = 0; i < MAX_CURVES; i++)
        {
            CreateBezierCurve();
        }
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Dispose()
    {
        _curves.Clear();
    }

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        _time += 0.016f;

        // Clear with dark background
        canvas.Clear(0xFF000000);

        // Update all curves
        UpdateCurves(f);

        // Render all curves
        RenderCurves(canvas, f);

        // Occasionally add new curves
        if (_random.NextDouble() < 0.01f && _curves.Count < MAX_CURVES)
        {
            CreateBezierCurve();
        }

        // Remove old curves occasionally
        if (_random.NextDouble() < 0.005f && _curves.Count > 3)
        {
            _curves.RemoveAt(0);
        }
    }

    private void CreateBezierCurve()
    {
        var curve = new BezierCurve
        {
            X = _random.Next(_width),
            Y = _random.Next(_height),
            VelX = (float)(_random.NextDouble() * CURVE_SPEED * 2 - CURVE_SPEED),
            VelY = (float)(_random.NextDouble() * CURVE_SPEED * 2 - CURVE_SPEED),
            ControlPoints = new (float x, float y)[4],
            Color = _bezierColors[_random.Next(_bezierColors.Length)],
            Thickness = 2f + (float)(_random.NextDouble() * 3f),
            Phase = (float)(_random.NextDouble() * Math.PI * 2),
            Length = 0.5f + (float)(_random.NextDouble() * 0.5f),
            Trail = new List<(float x, float y, float alpha)>()
        };

        // Generate initial control points
        GenerateControlPoints(curve);

        _curves.Add(curve);
    }

    private void GenerateControlPoints(BezierCurve curve)
    {
        // Start point (relative to curve position)
        curve.ControlPoints[0] = (0, 0);

        // Control point 1 (first handle)
        float cp1x = (float)(_random.NextDouble() * CONTROL_POINT_VARIANCE - CONTROL_POINT_VARIANCE * 0.5f);
        float cp1y = (float)(_random.NextDouble() * CONTROL_POINT_VARIANCE - CONTROL_POINT_VARIANCE * 0.5f);
        curve.ControlPoints[1] = (cp1x, cp1y);

        // Control point 2 (second handle)
        float cp2x = cp1x + (float)(_random.NextDouble() * CONTROL_POINT_VARIANCE - CONTROL_POINT_VARIANCE * 0.5f);
        float cp2y = cp1y + (float)(_random.NextDouble() * CONTROL_POINT_VARIANCE - CONTROL_POINT_VARIANCE * 0.5f);
        curve.ControlPoints[2] = (cp2x, cp2y);

        // End point
        float endX = cp2x + (float)(_random.NextDouble() * CONTROL_POINT_VARIANCE * curve.Length);
        float endY = cp2y + (float)(_random.NextDouble() * CONTROL_POINT_VARIANCE * curve.Length);
        curve.ControlPoints[3] = (endX, endY);
    }

    private void UpdateCurves(AudioFeatures f)
    {
        for (int i = 0; i < _curves.Count; i++)
        {
            var curve = _curves[i];

            // Update position
            curve.X += curve.VelX * (1f + f.Volume * 0.3f);
            curve.Y += curve.VelY * (1f + f.Volume * 0.3f);

            // Update phase for animation
            curve.Phase += 0.02f * (1f + f.Mid * 0.5f);

            // Bounce off walls
            if (curve.X <= -200 || curve.X >= _width + 200)
            {
                curve.VelX = -curve.VelX;
                curve.X = Math.Max(-200, Math.Min(_width + 200, curve.X));

                // Regenerate control points for variety
                GenerateControlPoints(curve);
                curve.Color = _bezierColors[_random.Next(_bezierColors.Length)];
            }

            if (curve.Y <= -200 || curve.Y >= _height + 200)
            {
                curve.VelY = -curve.VelY;
                curve.Y = Math.Max(-200, Math.Min(_height + 200, curve.Y));

                // Regenerate control points for variety
                GenerateControlPoints(curve);
                curve.Color = _bezierColors[_random.Next(_bezierColors.Length)];
            }

            // Audio-reactive speed
            float speedMultiplier = 1f + f.Bass * 0.8f;
            curve.VelX *= speedMultiplier;
            curve.VelY *= speedMultiplier;

            // Keep velocities reasonable
            curve.VelX = Math.Max(-CURVE_SPEED * 3, Math.Min(CURVE_SPEED * 3, curve.VelX));
            curve.VelY = Math.Max(-CURVE_SPEED * 3, Math.Min(CURVE_SPEED * 3, curve.VelY));

            // Update thickness based on treble
            curve.Thickness = (2f + (float)_random.NextDouble() * 3f) * (1f + f.Treble * 0.5f);

            // Update trail
            UpdateTrail(curve, f);

            _curves[i] = curve;
        }
    }

    private void UpdateTrail(BezierCurve curve, AudioFeatures f)
    {
        // Add current start point to trail
        curve.Trail.Add((curve.X + curve.ControlPoints[0].x, curve.Y + curve.ControlPoints[0].y, 1.0f));

        // Limit trail length
        int maxTrailLength = 15 + (int)(f.Volume * 20);
        while (curve.Trail.Count > maxTrailLength)
        {
            curve.Trail.RemoveAt(0);
        }

        // Fade trail
        float fadeSpeed = 0.03f + f.Volume * 0.05f;
        for (int i = 0; i < curve.Trail.Count; i++)
        {
            var trailPoint = curve.Trail[i];
            trailPoint.alpha -= fadeSpeed;
            trailPoint.alpha = Math.Max(0, trailPoint.alpha);
            curve.Trail[i] = trailPoint;
        }

        // Remove faded points
        curve.Trail.RemoveAll(p => p.alpha <= 0);
    }

    private void RenderCurves(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Render trails first (behind curves)
        foreach (var curve in _curves)
        {
            RenderTrail(canvas, curve, f);
        }

        // Render curves on top
        foreach (var curve in _curves)
        {
            RenderCurve(canvas, curve, f);
        }
    }

    private void RenderTrail(ISkiaCanvas canvas, BezierCurve curve, AudioFeatures f)
    {
        if (curve.Trail.Count < 2) return;

        uint trailColor = AdjustBrightness(curve.Color, 0.4f);

        for (int i = 1; i < curve.Trail.Count; i++)
        {
            var p1 = curve.Trail[i - 1];
            var p2 = curve.Trail[i];

            float combinedAlpha = p1.alpha * p2.alpha * 0.6f;
            uint fadedColor = (uint)((uint)(combinedAlpha * 255) << 24 | (trailColor & 0x00FFFFFF));

            float thickness = 1f + combinedAlpha * 2f;
            canvas.DrawLine(p1.x, p1.y, p2.x, p2.y, fadedColor, thickness);
        }
    }

    private void RenderCurve(ISkiaCanvas canvas, BezierCurve curve, AudioFeatures f)
    {
        var points = CalculateBezierPoints(curve);

        if (points.Length < 2) return;

        // Audio-reactive color and thickness
        uint color = AdjustBrightness(curve.Color, 0.8f + f.Volume * 0.3f);
        float thickness = curve.Thickness * (1f + f.Bass * 0.5f);

        // Draw the curve as connected line segments
        for (int i = 1; i < points.Length; i++)
        {
            var p1 = points[i - 1];
            var p2 = points[i];

            // Add some variation to thickness along the curve
            float segmentThickness = thickness * (0.8f + (float)Math.Sin(i * 0.1f + curve.Phase) * 0.4f);

            canvas.DrawLine(p1.x, p1.y, p2.x, p2.y, color, segmentThickness);
        }

        // Add control point visualization for extra mystique
        if (f.Beat && _random.NextDouble() < 0.7f)
        {
            RenderControlPoints(canvas, curve, f);
        }

        // Add particle effects along the curve
        if (f.Volume > 0.3f)
        {
            RenderCurveParticles(canvas, points, curve, f);
        }
    }

    private (float x, float y)[] CalculateBezierPoints(BezierCurve curve)
    {
        var points = new (float x, float y)[CURVE_SEGMENTS + 1];

        for (int i = 0; i <= CURVE_SEGMENTS; i++)
        {
            float t = (float)i / CURVE_SEGMENTS;

            // Cubic Bézier formula: B(t) = (1-t)^3*P0 + 3*(1-t)^2*t*P1 + 3*(1-t)*t^2*P2 + t^3*P3
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            float x = uuu * curve.ControlPoints[0].x +
                     3 * uu * t * curve.ControlPoints[1].x +
                     3 * u * tt * curve.ControlPoints[2].x +
                     ttt * curve.ControlPoints[3].x;

            float y = uuu * curve.ControlPoints[0].y +
                     3 * uu * t * curve.ControlPoints[1].y +
                     3 * u * tt * curve.ControlPoints[2].y +
                     ttt * curve.ControlPoints[3].y;

            // Add some wave motion for organic feel
            float waveX = (float)Math.Sin(t * Math.PI * 4 + curve.Phase) * 5f;
            float waveY = (float)Math.Cos(t * Math.PI * 3 + curve.Phase * 0.7f) * 3f;

            points[i] = (curve.X + x + waveX, curve.Y + y + waveY);
        }

        return points;
    }

    private void RenderControlPoints(ISkiaCanvas canvas, BezierCurve curve, AudioFeatures f)
    {
        // Render the Bézier control points for visual interest
        uint pointColor = AdjustBrightness(curve.Color, 1.2f);
        pointColor = (uint)(pointColor & 0x00FFFFFF | 0xC0 << 24); // 75% alpha

        float pointSize = 4f + (f.Beat ? 1f : 0f) * 6f;

        foreach (var cp in curve.ControlPoints)
        {
            float px = curve.X + cp.x;
            float py = curve.Y + cp.y;

            if (px >= 0 && px < _width && py >= 0 && py < _height)
            {
                canvas.FillCircle(px, py, pointSize, pointColor);
            }
        }

        // Draw control point connections
        uint lineColor = (uint)(pointColor & 0x00FFFFFF | 0x60 << 24); // 40% alpha

        for (int i = 1; i < curve.ControlPoints.Length; i++)
        {
            var p1 = curve.ControlPoints[i - 1];
            var p2 = curve.ControlPoints[i];

            float x1 = curve.X + p1.x;
            float y1 = curve.Y + p1.y;
            float x2 = curve.X + p2.x;
            float y2 = curve.Y + p2.y;

            canvas.DrawLine(x1, y1, x2, y2, lineColor, 1f);
        }
    }

    private void RenderCurveParticles(ISkiaCanvas canvas, (float x, float y)[] curvePoints, BezierCurve curve, AudioFeatures f)
    {
        int particleCount = (int)(f.Volume * 15);

        for (int i = 0; i < particleCount; i++)
        {
            int pointIndex = _random.Next(curvePoints.Length);
            var basePoint = curvePoints[pointIndex];

            // Add some offset for particle spread
            float offsetX = (float)(_random.NextDouble() * 20 - 10);
            float offsetY = (float)(_random.NextDouble() * 20 - 10);

            float px = basePoint.x + offsetX;
            float py = basePoint.y + offsetY;

            if (px >= 0 && px < _width && py >= 0 && py < _height)
            {
                uint particleColor = AdjustBrightness(curve.Color, 0.8f + (float)_random.NextDouble() * 0.4f);
                particleColor = (uint)(particleColor & 0x00FFFFFF | 0xA0 << 24); // 60% alpha

                float particleSize = 2f + f.Treble * 3f;
                canvas.FillCircle(px, py, particleSize, particleColor);
            }
        }
    }

    private uint AdjustBrightness(uint color, float factor)
    {
        byte r = (byte)((color >> 16) & 0xFF);
        byte g = (byte)((color >> 8) & 0xFF);
        byte b = (byte)(color & 0xFF);

        r = (byte)Math.Min(255, r * factor);
        g = (byte)Math.Min(255, g * factor);
        b = (byte)Math.Min(255, b * factor);

        return (uint)(0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | (uint)b);
    }
}
