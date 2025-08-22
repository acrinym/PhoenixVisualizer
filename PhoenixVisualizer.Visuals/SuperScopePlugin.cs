using System.Numerics;
using PhoenixVisualizer.Core.Avs;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// SuperScope visualization plugin inspired by Winamp's SuperScope
/// </summary>
public class SuperScopePlugin : IVisualizerPlugin
{
    public string Id => "superscope_pro";
    public string DisplayName => "SuperScope Pro";

    private readonly AvsEffects.SuperScope.ScopeContext _scopeContext = new();
    private readonly Random _random = new();
    private float _time = 0f;
    private int _renderMode = 0;
    private float _modeTimer = 0f;
    private int _width = 800;
    private int _height = 600;

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
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

        _time += 0.016f;

        // Change render mode on beat
        if (_scopeContext.IsBeat && _scopeContext.BeatIntensity > 0.7f)
        {
            _modeTimer += 1f;
            if (_modeTimer > 3f) // Change mode every 3 beats
            {
                _renderMode = (_renderMode + 1) % 6;
                _modeTimer = 0f;
            }
        }

        // Fade previous frame
        canvas.Fade(0xFF000000, 0.15f);

        // Render based on current mode
        switch (_renderMode)
        {
            case 0:
                RenderOscilloscope(canvas);
                break;
            case 1:
                RenderSpectrum(canvas);
                break;
            case 2:
                RenderCircularScope(canvas);
                break;
            case 3:
                RenderTunnel(canvas);
                break;
            case 4:
                RenderSpirograph(canvas);
                break;
            case 5:
                RenderLissajous(canvas);
                break;
        }
    }

    private void RenderOscilloscope(ISkiaCanvas canvas)
    {
        // Create oscilloscope
        var points = AvsEffects.SuperScope.CreateOscilloscope(_scopeContext, 512);
        
        // Draw with color based on amplitude
        var avgAmplitude = _scopeContext.AudioData.Length > 0 ? _scopeContext.AudioData.Select(Math.Abs).Average() : 0f;
        var hue = avgAmplitude * 2f % 1f;
        var color = HsvToRgb(hue, 1f, 1f);
        
        DrawConnectedLines(canvas, points, color);
    }

    private void RenderSpectrum(ISkiaCanvas canvas)
    {
        // Create spectrum analyzer
        var points = AvsEffects.SuperScope.CreateSpectrum(_scopeContext, 128);
        
        // Draw spectrum bars with rainbow colors
        for (int i = 0; i < points.Length; i++)
        {
            var point = points[i];
            var x = (point.X + 1f) * canvas.Width * 0.5f;
            var barHeight = Math.Abs(point.Y) * canvas.Height * 0.4f;
            
            var hue = (float)i / points.Length;
            var color = HsvToRgb(hue, 1f, 1f);
            
            // Draw bar from bottom up
            var startY = canvas.Height - 1;
            canvas.FillRect(x - 2, startY - barHeight, 4, barHeight, color);
        }
    }

    private void RenderCircularScope(ISkiaCanvas canvas)
    {
        // Create circular scope
        var radius = 0.3f + _scopeContext.BeatIntensity * 0.2f;
        var points = AvsEffects.SuperScope.CreateCircularScope(_scopeContext, 256, radius);
        
        // Color based on beat
        var color = _scopeContext.IsBeat ? 
            0xFFFF5555 : // Red on beat
            0xFF55FFCC;  // Cyan normally
        
        DrawConnectedLines(canvas, points, color);
        
        // Add center dot
        var centerX = canvas.Width / 2f;
        var centerY = canvas.Height / 2f;
        canvas.FillCircle(centerX, centerY, 4f, 0xFFFFFFFF);
    }

    private void RenderTunnel(ISkiaCanvas canvas)
    {
        // Create tunnel
        var rings = 15;
        var pointsPerRing = 32;
        var points = AvsEffects.SuperScope.CreateTunnel(_scopeContext, rings, pointsPerRing);
        
        // Color gradient from center to edge
        for (int i = 0; i < points.Length; i++)
        {
            var ringIndex = i / pointsPerRing;
            var t = (float)ringIndex / (rings - 1);
            var hue = t * 0.6f + _time * 0.1f;
            var color = HsvToRgb(hue % 1f, 1f, 1f - t * 0.5f);
            
            var point = points[i];
            var screenX = (point.X + 1f) * canvas.Width * 0.5f;
            var screenY = (point.Y + 1f) * canvas.Height * 0.5f;
            canvas.DrawPoint(screenX, screenY, color, 3f);
        }
    }

    private void RenderSpirograph(ISkiaCanvas canvas)
    {
        // Create spirograph
        var points = AvsEffects.SuperScope.CreateSpirograph(_scopeContext, 512, 0.7f, 0.3f, 0.5f);
        
        // Color based on time
        var hue = _time * 0.1f % 1f;
        var color = HsvToRgb(hue, 0.8f, 1f);
        
        DrawConnectedLines(canvas, points, color);
    }

    private void RenderLissajous(ISkiaCanvas canvas)
    {
        // Create Lissajous curves
        var freqX = 3f + _scopeContext.BeatIntensity * 2f;
        var freqY = 2f + _scopeContext.BeatIntensity * 1.5f;
        var points = AvsEffects.SuperScope.CreateLissajous(_scopeContext, 256, freqX, freqY);
        
        // Color based on frequency ratio
        var hue = (freqX / freqY) % 1f;
        var color = HsvToRgb(hue, 1f, 1f);
        
        DrawConnectedLines(canvas, points, color);
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
        // Nothing to dispose
    }
}