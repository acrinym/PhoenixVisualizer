using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

public sealed class ChickenVisualizer : IVisualizerPlugin
{
    public string Id => "fun.chicken.peck";
    public string DisplayName => "🐔 Chicken Field (Wireframe)";
    public string Description => "Wireframe chickens on a grass plane with pecking behavior driven by audio";
    public bool IsEnabled { get; set; } = true;

    private int _w, _h;
    private readonly System.Collections.Generic.List<Chicken> _chickens = new();
    private readonly int _chickenCount = 7;
    
    // Ground plane mapping constants
    private readonly float _horizonY = 0.62f;   // where the grid meets the sky
    private readonly float _bottomY = 0.98f;    // bottom margin
    private readonly float _minScale = 0.25f;   // grid "width" at horizon
    private readonly float _maxScale = 1.60f;   // grid width at camera
    
    // Grass grid
    private readonly int _rows = 6, _cols = 24;
    
    // Pecking dynamics
    private readonly float _peckThreshold = 0.35f;  // Lower threshold for more pecking
    private readonly int _peckCooldownMs = 200;     // Shorter cooldown for more frequent pecking
    
    // Random for chicken behavior
    private readonly Random _rng = new Random();

    public ChickenVisualizer()
    {
        // Initialize chickens
        for (int i = 0; i < _chickenCount; i++)
        {
            _chickens.Add(new Chicken
            {
                U = (i + 1f) / (_chickenCount + 1f),
                V = 0.25f + 0.10f * (i % 3),     // rows across the plane (0=near horizon .. 1=near camera)
                Dir = (i % 2 == 0) ? 1f : -1f,
                Speed = 0.05f + 0.06f * (float)_rng.NextDouble(), // uu/sec
                Step = (float)_rng.NextDouble() * MathF.PI * 2,
                HeadAngle = -0.10f,
                State = ChickenState.Idle,
                CooldownUntil = 0
            });
        }
    }

    public void Initialize() { }
    public void Initialize(int width, int height) { _w = width; _h = height; }
    public void Resize(int width, int height) { _w = width; _h = height; }
    public void Shutdown() { }
    public void ProcessFrame(AudioFeatures features, ISkiaCanvas canvas) { RenderFrame(features, canvas); }
    public void Configure() { }
    public void Dispose() { }

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        // Clear with black background
        canvas.Clear(0xFF000000);

            // Create viewport rect
    var viewport = new Viewport { Left = 0f, Top = 0f, Width = _w, Height = _h };

        // Draw sun first
        DrawSun(canvas, viewport, features);

        // Draw grass grid (lightweight)
        DrawGrass(canvas, viewport);

        // Update chickens and draw
        long nowMs = (long)(features.TimeSeconds * 1000.0);
        double dt = 1.0 / 60.0; // assume 60fps
        
        foreach (var c in _chickens)
        {
            UpdateChicken(c, features, dt, nowMs);
            DrawChicken(canvas, viewport, c);
        }
    }

    // ---------- Ground Mapping ----------

    // Map ground coords (u in [0..1] across, v in [0..1] from horizon->bottom) to screen.
    private (float x, float y) Ground(float u, float v, Viewport viewport)
    {
        float widthScale = _minScale + (_maxScale - _minScale) * v;       // perspective spread
        float nx = 0.5f + (u - 0.5f) * widthScale;                        // converge to center at horizon
        float ny = _horizonY + v * (_bottomY - _horizonY);                // lerp horizon->bottom
        return (viewport.Left + nx * viewport.Width, viewport.Top + ny * viewport.Height);
    }

    // Local pixel scale at a ground point (used to size birds correctly as they recede)
    private float GroundScale(float v, Viewport viewport)
    {
        // scale roughly with distance from horizon and viewport height
        return (0.035f + 0.12f * v) * viewport.Height;   // tweakable
    }

    // ---------- Rendering ----------

    private void DrawSun(ISkiaCanvas canvas, Viewport viewport, AudioFeatures features)
    {
        // position: top-left-ish
        float nx = 0.18f + 0.05f * features.Mid;         // slight drift with mids
        float ny = 0.15f;
        var centerX = viewport.Left + nx * viewport.Width;
        var centerY = viewport.Top + ny * viewport.Height;

        // size + pulse with treble
        float r = (0.08f + 0.05f * features.Treble) * Math.Min(viewport.Width, viewport.Height);

        // Sun core
        uint sunColor = 0xFF70A7FF;
        canvas.FillCircle(centerX, centerY, r, sunColor);

        // Glow layer
        uint glowColor = 0x4670A7FF; // lower alpha
        canvas.FillCircle(centerX, centerY, r * 1.6f, glowColor);

        // Minimal rays (beat pops)
        if (features.Beat)
        {
            uint rayColor = 0x8C9CC2FF;
            float R1 = r * 1.2f, R2 = r * 1.45f;
            canvas.SetLineWidth(3f);
            
            for (int i = 0; i < 12; i++)
            {
                float t = (i / 12f) * MathF.PI * 2;
                var p1X = centerX + R1 * MathF.Cos(t);
                var p1Y = centerY + R1 * MathF.Sin(t);
                var p2X = centerX + R2 * MathF.Cos(t);
                var p2Y = centerY + R2 * MathF.Sin(t);
                canvas.DrawLine(p1X, p1Y, p2X, p2Y, rayColor, 3f);
            }
        }
    }

    private void DrawGrass(ISkiaCanvas canvas, Viewport viewport)
    {
        // Grass color: neon green with transparency
        uint grassColor = 0x886FE26F; // neon green, semi-transparent
        
        // Horizontal lines
        for (int r = 0; r <= _rows; r++)
        {
            float v = r / (float)_rows;
            var (x1, y1) = Ground(0f, v, viewport);
            var (x2, y2) = Ground(1f, v, viewport);
            canvas.DrawLine(x1, y1, x2, y2, grassColor, 1f);
        }
        
        // Vertical lines that "fan" with perspective
        for (int c = 0; c <= _cols; c++)
        {
            float u = c / (float)_cols;
            var (x1, y1) = Ground(u, 0f, viewport);
            var (x2, y2) = Ground(u, 1f, viewport);
            canvas.DrawLine(x1, y1, x2, y2, grassColor, 1f);
        }
    }

    private void DrawChicken(ISkiaCanvas canvas, Viewport viewport, Chicken c)
    {
        var (pX, pY) = Ground(c.U, c.V, viewport);
        float s = GroundScale(c.V, viewport);               // pixel scale (size)
        float facing = (c.Dir >= 0) ? 1f : -1f;

        // Colors
        uint chickenColor = 0xFFFDDCA8; // warm white/amber
        uint beakColor = 0xFFFF8C00;    // orange
        uint combColor = 0xFFE5262A;    // red

        // Draw around (0,0)≈body center in "model units"
        float centerX = pX;
        float centerY = pY;

        // body ellipse (wireframe model space ~1x1)
        DrawEllipse(canvas, centerX, centerY, s * 0.7f, s * 0.50f, chickenColor);

        // tail - flip based on direction
        if (facing > 0) // walking right
        {
            canvas.DrawLine(centerX - s * 0.2f, centerY + s * 0.45f, centerX - s * 0.55f, centerY + s * 0.20f, chickenColor, 2f);
            canvas.DrawLine(centerX - s * 0.2f, centerY + s * 0.45f, centerX - s * 0.60f, centerY + s * 0.45f, chickenColor, 2f);
            canvas.DrawLine(centerX - s * 0.2f, centerY + s * 0.45f, centerX - s * 0.55f, centerY + s * 0.70f, chickenColor, 2f);
        }
        else // walking left
        {
            canvas.DrawLine(centerX + s * 0.2f, centerY + s * 0.45f, centerX + s * 0.55f, centerY + s * 0.20f, chickenColor, 2f);
            canvas.DrawLine(centerX + s * 0.2f, centerY + s * 0.45f, centerX + s * 0.60f, centerY + s * 0.45f, chickenColor, 2f);
            canvas.DrawLine(centerX + s * 0.2f, centerY + s * 0.45f, centerX + s * 0.55f, centerY + s * 0.70f, chickenColor, 2f);
        }

        // neck base & head position - flip based on direction
        float neckX, neckY;
        if (facing > 0) // walking right
        {
            neckX = centerX + s * 0.85f;
            neckY = centerY + s * 0.30f;
        }
        else // walking left
        {
            neckX = centerX - s * 0.85f;
            neckY = centerY + s * 0.30f;
        }
        
        float hx = neckX + s * 0.35f * MathF.Cos(c.HeadAngle) * facing;
        float hy = neckY + s * 0.35f * MathF.Sin(c.HeadAngle);

        // head
        DrawEllipse(canvas, hx, hy, s * 0.18f, s * 0.16f, chickenColor);

        // comb + beak - flip based on direction
        if (facing > 0) // walking right
        {
            canvas.DrawLine(hx - s * 0.09f, hy - s * 0.18f, hx - s * 0.03f, hy - s * 0.26f, combColor, 2f);
            canvas.DrawLine(hx - s * 0.03f, hy - s * 0.26f, hx + s * 0.03f, hy - s * 0.18f, combColor, 2f);
            canvas.DrawLine(hx + s * 0.16f, hy, hx + s * 0.26f, hy - s * 0.04f, beakColor, 2f);
            canvas.DrawLine(hx + s * 0.16f, hy, hx + s * 0.26f, hy + s * 0.04f, beakColor, 2f);
            canvas.DrawLine(hx + s * 0.26f, hy - s * 0.04f, hx + s * 0.26f, hy + s * 0.04f, beakColor, 2f);
        }
        else // walking left
        {
            canvas.DrawLine(hx + s * 0.09f, hy - s * 0.18f, hx + s * 0.03f, hy - s * 0.26f, combColor, 2f);
            canvas.DrawLine(hx + s * 0.03f, hy - s * 0.26f, hx - s * 0.03f, hy - s * 0.18f, combColor, 2f);
            canvas.DrawLine(hx - s * 0.16f, hy, hx - s * 0.26f, hy - s * 0.04f, beakColor, 2f);
            canvas.DrawLine(hx - s * 0.16f, hy, hx - s * 0.26f, hy + s * 0.04f, beakColor, 2f);
            canvas.DrawLine(hx - s * 0.26f, hy - s * 0.04f, hx - s * 0.26f, hy + s * 0.04f, beakColor, 2f);
        }

        // legs + little step bounce - flip based on direction
        float foot = centerY + s * 0.98f;
        float stepLift = s * 0.06f * MathF.Max(0, MathF.Sin(c.Step));
        if (facing > 0) // walking right
        {
            canvas.DrawLine(centerX + s * 0.45f, centerY + s * 0.85f - stepLift, centerX + s * 0.45f, foot, chickenColor, 2f);
            canvas.DrawLine(centerX + s * 0.25f, centerY + s * 0.83f + stepLift * 0.5f, centerX + s * 0.25f, foot, chickenColor, 2f);
        }
        else // walking left
        {
            canvas.DrawLine(centerX - s * 0.45f, centerY + s * 0.85f - stepLift, centerX - s * 0.45f, foot, chickenColor, 2f);
            canvas.DrawLine(centerX - s * 0.25f, centerY + s * 0.83f + stepLift * 0.5f, centerX - s * 0.25f, foot, chickenColor, 2f);
        }

        // ground peck marker - flip based on direction
        if (c.State == ChickenState.Pecking)
        {
            if (facing > 0) // walking right
                canvas.FillCircle(centerX + s * 0.95f, centerY + s * 0.95f, s * 0.02f, beakColor);
            else // walking left
                canvas.FillCircle(centerX - s * 0.95f, centerY + s * 0.95f, s * 0.02f, beakColor);
        }
    }

    private static void DrawEllipse(ISkiaCanvas canvas, float cx, float cy, float rx, float ry, uint color)
    {
        const int STEPS = 32; // Reduced for performance
        var points = new System.Collections.Generic.List<(float x, float y)>();
        
        for (int i = 0; i <= STEPS; i++)
        {
            float t = (float)i / STEPS * MathF.PI * 2;
            float x = cx + rx * MathF.Cos(t);
            float y = cy + ry * MathF.Sin(t);
            points.Add((x, y));
        }
        
        canvas.SetLineWidth(2f);
        canvas.DrawLines(points.ToArray(), 2f, color);
    }

    // ---------- Logic ----------

    private enum ChickenState { Idle, Pecking, Recover }

    private sealed class Chicken
    {
        public float U, V;           // ground coords
        public float Dir;            // -1..+1 (left/right)
        public float Speed;          // uu/sec along ground
        public float Step;           // walk phase
        public float HeadAngle;      // radians; -0.9 = down, +0.3 = up
        public ChickenState State;
        public long CooldownUntil;
    }

    private void UpdateChicken(Chicken c, AudioFeatures features, double dt, long nowMs)
    {
        // walk
        if (c.State == ChickenState.Idle)
        {
            c.U += c.Dir * c.Speed * (float)dt * (0.7f + features.Mid);   // features.Mid adds hustle
            if (c.U < 0f) { c.U = 1f; } else if (c.U > 1f) { c.U = 0f; }
            c.Step += (float)dt * (4.0f + 6.0f * features.Rms);           // step rate
            // idle head bob
            float bob = 0.05f * MathF.Sin(c.Step);
            c.HeadAngle = Lerp(c.HeadAngle, -0.05f + bob, 0.25f);
        }

        // peck trigger: bass spike OR beat OR more frequent random ticks, with cooldown
        bool wantPeck =
            (features.Bass >= _peckThreshold) || features.Beat ||
            (_rng.NextDouble() < 0.008 + 0.025 * features.Rms);  // Increased random peck chance

        if (c.State == ChickenState.Idle && nowMs >= c.CooldownUntil && wantPeck)
        {
            c.State = ChickenState.Pecking;
            c.CooldownUntil = nowMs + _peckCooldownMs;
        }

        // peck state machine
        const float DOWN = -0.90f, UP = +0.25f;
        switch (c.State)
        {
            case ChickenState.Pecking:
                c.HeadAngle = Lerp(c.HeadAngle, DOWN, 0.55f);
                if (c.HeadAngle < DOWN + 0.05f) c.State = ChickenState.Recover;
                break;
            case ChickenState.Recover:
                c.HeadAngle = Lerp(c.HeadAngle, UP, 0.30f);
                if (c.HeadAngle > UP - 0.05f) c.State = ChickenState.Idle;
                break;
        }
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
    private static float Clamp01(float v) => v < 0 ? 0 : (v > 1 ? 1 : v);

    // Viewport structure for ground mapping
    private struct Viewport
    {
        public float Left;
        public float Top;
        public int Width;
        public int Height;
    }
}
