using System.Numerics;
using PhoenixVisualizer.Core.Avs;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Advanced AVS plugin that uses the new AVS effects system
/// </summary>
public class AdvancedAvsPlugin : IVisualizerPlugin
{
    public string Id => "advanced_avs";
    public string DisplayName => "Advanced AVS";

    private AvsEffects.SuperScope.ScopeContext _scopeContext = new();
    private Random _random = new();
    private List<Vector3> _stars = new();
    private float _time = 0f;
    private int _effectIndex = 0;
    private float _effectTimer = 0f;
    private int _width = 800;
    private int _height = 600;

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        
        // Initialize starfield
        for (int i = 0; i < 100; i++)
        {
            _stars.Add(new Vector3(
                (float)_random.NextDouble() * 2f - 1f,
                (float)_random.NextDouble() * 2f - 1f,
                (float)_random.NextDouble() * 10f + 1f
            ));
        }
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        // Update audio context
        _scopeContext.Time = _time;
        _scopeContext.AudioData = features.Waveform ?? Array.Empty<float>();
        _scopeContext.SpectrumData = features.Fft ?? Array.Empty<float>();
        _scopeContext.IsBeat = features.Beat;
        _scopeContext.BeatIntensity = features.Volume;

        _time += 0.016f; // Roughly 60 FPS
        _effectTimer += 0.016f;

        // Switch effects every 10 seconds
        if (_effectTimer > 10f)
        {
            _effectIndex = (_effectIndex + 1) % 7;
            _effectTimer = 0f;
        }

        // Clear canvas
        canvas.Clear(0xFF000000); // Black

        // Render different effects based on current index
        switch (_effectIndex)
        {
            case 0:
                RenderPlasma(canvas);
                break;
            case 1:
                RenderSuperScope(canvas);
                break;
            case 2:
                RenderStarfield(canvas);
                break;
            case 3:
                RenderMandelbrot(canvas);
                break;
            case 4:
                RenderMatrixRain(canvas);
                break;
            case 5:
                RenderCircularScope(canvas);
                break;
            case 6:
                RenderTunnel(canvas);
                break;
        }
    }

    private void RenderPlasma(ISkiaCanvas canvas)
    {
        // Simple plasma effect using canvas drawing
        for (int y = 0; y < canvas.Height; y += 4)
        {
            for (int x = 0; x < canvas.Width; x += 4)
            {
                var nx = (float)x / canvas.Width;
                var ny = (float)y / canvas.Height;
                
                var plasma = MathF.Sin(nx * 10f + _time) +
                            MathF.Sin(ny * 10f + _time * 1.3f) +
                            MathF.Sin((nx + ny) * 8f + _time * 0.7f) +
                            MathF.Sin(MathF.Sqrt(nx * nx + ny * ny) * 12f + _time * 2f);
                
                plasma = (plasma + 4f) / 8f; // Normalize to 0-1
                
                var hue = plasma % 1f;
                var color = HsvToRgb(hue, 1f, 1f);
                
                canvas.FillRect(x, y, 4, 4, color);
            }
        }
    }

    private void RenderSuperScope(ISkiaCanvas canvas)
    {
        // Create oscilloscope
        var points = AvsEffects.SuperScope.CreateOscilloscope(_scopeContext, 256);
        DrawConnectedLines(canvas, points, 0xFF00FF00); // Green
        
        // Add spectrum analyzer
        var spectrum = AvsEffects.SuperScope.CreateSpectrum(_scopeContext, 64);
        DrawSpectrumBars(canvas, spectrum, 0xFFFF8000); // Orange
    }

    private void RenderStarfield(ISkiaCanvas canvas)
    {
        // Update and draw stars
        for (int i = 0; i < _stars.Count; i++)
        {
            var star = _stars[i];
            star.Z -= 0.1f + _scopeContext.BeatIntensity * 0.5f;
            
            if (star.Z <= 0)
            {
                // Reset star
                star = new Vector3(
                    (float)_random.NextDouble() * 2f - 1f,
                    (float)_random.NextDouble() * 2f - 1f,
                    10f
                );
            }
            
            // Project to 2D
            var screenX = (star.X / star.Z + 1f) * canvas.Width * 0.5f;
            var screenY = (star.Y / star.Z + 1f) * canvas.Height * 0.5f;
            
            if (screenX >= 0 && screenX < canvas.Width && screenY >= 0 && screenY < canvas.Height)
            {
                var brightness = (byte)Math.Clamp(255f / star.Z, 0f, 255f);
                var color = (uint)(0xFF000000 | (brightness << 16) | (brightness << 8) | brightness);
                canvas.DrawPoint(screenX, screenY, color, 2f);
            }
            
            _stars[i] = star;
        }
    }

    private void RenderMandelbrot(ISkiaCanvas canvas)
    {
        var zoom = 1f + _time * 0.1f + _scopeContext.BeatIntensity * 2f;
        var centerX = -0.5f;
        var centerY = 0f;
        var maxIterations = 60;
        
        for (int py = 0; py < canvas.Height; py += 2)
        {
            for (int px = 0; px < canvas.Width; px += 2)
            {
                var x0 = (px - canvas.Width * 0.5f) / (canvas.Width * 0.25f * zoom) + centerX;
                var y0 = (py - canvas.Height * 0.5f) / (canvas.Height * 0.25f * zoom) + centerY;
                
                var x = 0f;
                var y = 0f;
                var iteration = 0;
                
                while (x * x + y * y <= 4f && iteration < maxIterations)
                {
                    var xtemp = x * x - y * y + x0;
                    y = 2f * x * y + y0;
                    x = xtemp;
                    iteration++;
                }
                
                if (iteration < maxIterations)
                {
                    var hue = (float)iteration / maxIterations;
                    var color = HsvToRgb(hue, 1f, 1f);
                    canvas.FillRect(px, py, 2, 2, color);
                }
            }
        }
    }

    private void RenderMatrixRain(ISkiaCanvas canvas)
    {
        // Simple matrix rain effect
        for (int x = 0; x < canvas.Width; x += 16)
        {
            if (_random.NextDouble() < 0.1 * (1f + _scopeContext.BeatIntensity))
            {
                var y = _random.Next(canvas.Height);
                var green = (byte)(128 + _random.Next(128));
                var color = (uint)(0xFF000000 | (green << 8));
                canvas.DrawText("0", x, y, color, 12f);
            }
        }
    }

    private void RenderCircularScope(ISkiaCanvas canvas)
    {
        // Create circular scope
        var points = AvsEffects.SuperScope.CreateCircularScope(_scopeContext, 128, 0.3f);
        DrawConnectedLines(canvas, points, 0xFF00CCFF); // Cyan
        
        // Add spirograph overlay
        var spirograph = AvsEffects.SuperScope.CreateSpirograph(_scopeContext, 64);
        DrawConnectedLines(canvas, spirograph, 0xFFFF33CC); // Pink
    }

    private void RenderTunnel(ISkiaCanvas canvas)
    {
        // Create tunnel
        var points = AvsEffects.SuperScope.CreateTunnel(_scopeContext, 12, 24);
        
        for (int i = 0; i < points.Length; i++)
        {
            var point = points[i];
            var ringIndex = i / 24;
            var t = (float)ringIndex / 11f;
            var hue = t * 0.6f + _time * 0.1f;
            var color = HsvToRgb(hue % 1f, 1f, 1f - t * 0.5f);
            
            var screenX = (point.X + 1f) * canvas.Width * 0.5f;
            var screenY = (point.Y + 1f) * canvas.Height * 0.5f;
            canvas.DrawPoint(screenX, screenY, color, 3f);
        }
    }

    private void DrawConnectedLines(ISkiaCanvas canvas, Vector2[] points, uint color)
    {
        if (points.Length == 0) return;
        
        var screenPoints = new (float x, float y)[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            screenPoints[i] = (
                (points[i].X + 1f) * canvas.Width * 0.5f,
                (points[i].Y + 1f) * canvas.Height * 0.5f
            );
        }
        
        canvas.DrawLines(screenPoints, 2f, color);
    }

    private void DrawSpectrumBars(ISkiaCanvas canvas, Vector2[] points, uint color)
    {
        for (int i = 0; i < points.Length; i++)
        {
            var point = points[i];
            var x = (point.X + 1f) * canvas.Width * 0.5f;
            var barHeight = Math.Abs(point.Y) * canvas.Height * 0.4f;
            
            // Draw bar from bottom up
            var startY = canvas.Height - 1;
            canvas.FillRect(x - 2, startY - barHeight, 4, barHeight, color);
        }
    }

    private static uint HsvToRgb(float h, float s, float v)
    {
        var c = v * s;
        var x = c * (1f - Math.Abs((h * 6f) % 2f - 1f));
        var m = v - c;

        Vector3 rgb;
        if (h < 1f / 6f)
            rgb = new Vector3(c, x, 0f);
        else if (h < 2f / 6f)
            rgb = new Vector3(x, c, 0f);
        else if (h < 3f / 6f)
            rgb = new Vector3(0f, c, x);
        else if (h < 4f / 6f)
            rgb = new Vector3(0f, x, c);
        else if (h < 5f / 6f)
            rgb = new Vector3(x, 0f, c);
        else
            rgb = new Vector3(c, 0f, x);

        var r = (byte)Math.Clamp((rgb.X + m) * 255f, 0f, 255f);
        var g = (byte)Math.Clamp((rgb.Y + m) * 255f, 0f, 255f);
        var b = (byte)Math.Clamp((rgb.Z + m) * 255f, 0f, 255f);
        
        return (uint)(0xFF000000 | (r << 16) | (g << 8) | b);
    }

    public void Dispose()
    {
        _stars.Clear();
    }
}