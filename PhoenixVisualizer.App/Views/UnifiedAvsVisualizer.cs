// File: PhoenixVisualizer.App/Views/UnifiedAvsVisualizer.cs
using PhoenixVisualizer.App.Services;
using AudioFeatures = PhoenixVisualizer.PluginHost.AudioFeatures;
using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Core;
using SkiaSharp;

namespace PhoenixVisualizer.App.Views;

public sealed class UnifiedAvsVisualizer : IVisualizerPlugin
{
    private UnifiedAvsData? _data;
    private int _width = 800;
    private int _height = 600;

    public string Id => "unified_avs";
    public string Name => "Unified AVS Visualizer";
    public string DisplayName => _data != null ? $"AVS: {(_data.Superscopes.FirstOrDefault()?.Name ?? "Unknown")}" : Name;
    public string Description => _data != null ? $"{_data.FileType} with {_data.Superscopes.Count} superscopes" : "Unified AVS visualizer";
    public string Author => "Phoenix Visualizer";
    public string Version => "2.0";

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        Console.WriteLine($"### JUSTIN DEBUG: [UnifiedAvsVisualizer] Initialized {width}x{height}");
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    public void LoadAvsData(UnifiedAvsData data)
    {
        _data = data;
        Console.WriteLine($"### JUSTIN DEBUG: [UnifiedAvsVisualizer] Loaded {data.FileType} with {data.Superscopes.Count} superscopes");
    }

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        // Clear with dark background
        canvas.Clear(0xFF0A0A0A);

        if (_data == null)
        {
            RenderNoDataState(canvas);
            return;
        }

        // Render based on source type
        switch (_data.FileType)
        {
            case AvsFileType.PhoenixText:
                RenderPhoenixAvsContent(canvas, features);
                break;
            case AvsFileType.WinampBinary:
                RenderWinampAvsContent(canvas, features);
                break;
            default:
                RenderGenericContent(canvas, features);
                break;
        }
    }

    private void RenderNoDataState(ISkiaCanvas canvas)
    {
        var centerX = _width / 2f;
        var centerY = _height / 2f;
        
        canvas.DrawText("No AVS data loaded", centerX, centerY, 0xFFFFFFFF, 24);
    }

    private void RenderPhoenixAvsContent(ISkiaCanvas canvas, AudioFeatures features)
    {
        if (_data?.Superscopes == null || _data.Superscopes.Count == 0)
        {
            RenderDefaultPattern(canvas, features, "Phoenix");
            return;
        }

        // Render each superscope
        var time = (float)features.TimeSeconds;
        var centerX = _width / 2f;
        var centerY = _height / 2f;

        for (int scopeIndex = 0; scopeIndex < _data.Superscopes.Count; scopeIndex++)
        {
            var scope = _data.Superscopes[scopeIndex];
            
            // For now, render geometric patterns based on audio features
            // TODO: Integrate with Phoenix Expression Engine (PEL)
            RenderSuperscope(canvas, scope, features, scopeIndex);
        }

        // Render metadata overlay
        canvas.DrawText($"Phoenix AVS: {_data.Superscopes.Count} scopes", 10, 30, 0xFFAAAAAAu, 16);
    }

    private void RenderWinampAvsContent(ISkiaCanvas canvas, AudioFeatures features)
    {
        if (_data?.Superscopes == null || _data.Superscopes.Count == 0)
        {
            RenderDefaultPattern(canvas, features, "Winamp");
            return;
        }

        // Classic Winamp-style oscilloscope rendering
        var time = (float)features.TimeSeconds;
        var centerX = _width / 2f;
        var centerY = _height / 2f;

        // Render classic waveform visualization
        RenderClassicWaveform(canvas, features);

        // Render metadata overlay
        canvas.DrawText($"Winamp AVS: {_data.Effects.Count} effects, {_data.Superscopes.Count} scopes", 10, 30, 0xFFAAAAAAu, 16);
    }

    private void RenderSuperscope(ISkiaCanvas canvas, UnifiedSuperscope scope, AudioFeatures features, int scopeIndex)
    {
        // For now, render based on scope properties
        // TODO: Execute actual NS-EEL code using Phoenix Expression Engine
        
        var time = (float)features.TimeSeconds;
        var bass = features.Bass;
        var centerX = _width / 2f;
        var centerY = _height / 2f;
        
        // Create audio-reactive geometric pattern
        var points = new List<(float x, float y)>();
        var numPoints = 128;
        
        for (int i = 0; i < numPoints; i++)
        {
            var t = (float)i / numPoints;
            var angle = t * 2 * MathF.PI * (1 + scopeIndex * 0.5f);
            
            // Audio-reactive radius
            var baseRadius = 100f + scopeIndex * 50f;
            var audioRadius = baseRadius * (1 + bass * 0.5f);
            
            // Time-based modulation
            var modulatedRadius = audioRadius * (1 + MathF.Sin(time * 2 + scopeIndex) * 0.3f);
            
            var x = centerX + MathF.Cos(angle + time * 0.5f) * modulatedRadius;
            var y = centerY + MathF.Sin(angle + time * 0.5f) * modulatedRadius;
            
            points.Add((x, y));
        }
        
        // Draw connected lines
        if (points.Count > 1)
        {
            // Color based on scope index and audio
            var hue = (scopeIndex * 60f + time * 30f) % 360f;
            var saturation = 0.8f + bass * 0.2f;
            var brightness = 0.6f + features.Mid * 0.4f;
            
            var color = HSVToRGB(hue, saturation, brightness);
            
            for (int i = 0; i < points.Count - 1; i++)
            {
                canvas.DrawLine(points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, color, 2f);
            }
            
            // Close the loop
            if (points.Count > 2)
            {
                canvas.DrawLine(points[^1].x, points[^1].y, points[0].x, points[0].y, color, 2f);
            }
        }
        
        Console.WriteLine($"### JUSTIN DEBUG: [UnifiedAvsVisualizer] Rendered superscope '{scope.Name}' with {points.Count} points");
    }

    private void RenderClassicWaveform(ISkiaCanvas canvas, AudioFeatures features)
    {
        // Classic oscilloscope-style waveform
        var waveform = features.Waveform;
        if (waveform.Length == 0) return;
        
        var centerY = _height / 2f;
        var amplitude = _height * 0.3f;
        
        for (int i = 0; i < waveform.Length - 1 && i < _width - 1; i++)
        {
            var x1 = (float)i / waveform.Length * _width;
            var y1 = centerY - waveform[i] * amplitude;
            var x2 = (float)(i + 1) / waveform.Length * _width;
            var y2 = centerY - waveform[i + 1] * amplitude;
            
            canvas.DrawLine(x1, y1, x2, y2, 0xFF00FF00u, 1f); // Green waveform
        }
    }

    private void RenderDefaultPattern(ISkiaCanvas canvas, AudioFeatures features, string type)
    {
        var time = (float)features.TimeSeconds;
        var centerX = _width / 2f;
        var centerY = _height / 2f;
        
        // Simple spinning pattern to indicate the visualizer is working
        var radius = 100f * (1 + features.Bass * 0.5f);
        var numSpokes = 8;
        
        for (int i = 0; i < numSpokes; i++)
        {
            var angle = (float)i / numSpokes * 2 * MathF.PI + time;
            var x = centerX + MathF.Cos(angle) * radius;
            var y = centerY + MathF.Sin(angle) * radius;
            
            canvas.DrawLine(centerX, centerY, x, y, 0xFF808080u, 2f);
        }
        
        canvas.DrawText($"{type} AVS (No Data)", centerX, centerY + 150, 0xFFFFFFFFu, 16);
    }

    private void RenderGenericContent(ISkiaCanvas canvas, AudioFeatures features)
    {
        RenderDefaultPattern(canvas, features, "Generic");
    }

    private static uint HSVToRGB(float h, float s, float v)
    {
        var c = v * s;
        var x = c * (1 - MathF.Abs(h / 60f % 2 - 1));
        var m = v - c;
        
        float r, g, b;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }
        
        var red = (byte)((r + m) * 255);
        var green = (byte)((g + m) * 255);
        var blue = (byte)((b + m) * 255);
        
        return 0xFF000000u | ((uint)red << 16) | ((uint)green << 8) | blue;
    }
}
