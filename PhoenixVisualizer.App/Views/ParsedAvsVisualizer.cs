using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Core.Diagnostics;
using PhoenixVisualizer.App.Services;
using PhoenixVisualizer.Audio;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoenixVisualizer.Views;

/// <summary>
/// A proper AVS visualizer that renders parsed AVS content using our own rendering system
/// instead of relying on the old fake-loading approach
/// </summary>
public class ParsedAvsVisualizer : IVisualizerPlugin
{
    private readonly AvsImportService.AvsFileInfo _avsFile;
    private int _width, _height;
    private float _time;

    public string Id => $"parsed_avs_{_avsFile.FileName}";
    public string DisplayName => $"AVS: {_avsFile.PresetName}";
    public string Description => $"Parsed AVS preset: {_avsFile.PresetDescription}";
    public bool IsEnabled { get; set; } = true;

    public ParsedAvsVisualizer(AvsImportService.AvsFileInfo avsFile)
    {
        _avsFile = avsFile ?? throw new ArgumentNullException(nameof(avsFile));
        Log.Info($"Created ParsedAvsVisualizer for: {_avsFile.FileName}");
    }

    // FIX: Method to update the visualizer from a new AVS file (called by MainWindow.OnLoadPreset)
    public void UpdateFromAvsFile(AvsImportService.AvsFileInfo newAvsFile)
    {
        _avsFile.FilePath = newAvsFile.FilePath;
        _avsFile.FileName = newAvsFile.FileName;
        _avsFile.RawBinaryData = newAvsFile.RawBinaryData;
        _avsFile.LastModified = newAvsFile.LastModified;
        _avsFile.FileSize = newAvsFile.FileSize;
        _avsFile.IsBinaryFormat = newAvsFile.IsBinaryFormat;
        _avsFile.RawContent = newAvsFile.RawContent;
        _avsFile.PresetName = newAvsFile.PresetName;
        _avsFile.PresetDescription = newAvsFile.PresetDescription;
        _avsFile.Author = newAvsFile.Author;
        _avsFile.Superscopes.Clear();
        _avsFile.Superscopes.AddRange(newAvsFile.Superscopes);
        _avsFile.Effects.Clear();
        _avsFile.Effects.AddRange(newAvsFile.Effects);
        Log.Info($"[TestTrace] [Renderer] ParsedAvsVisualizer updated with new file: {newAvsFile.FileName}");
    }

    public void Initialize() { }

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;
        Log.Info($"ParsedAvsVisualizer initialized: {_width}x{_height}");
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Shutdown() { }

    public void ProcessFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        RenderFrame(features, canvas);
    }

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        _time += 0.016f; // ~60 FPS

        // Clear with dark background
        canvas.Clear(0xFF0A0A0A);

        if (_avsFile.IsBinaryFormat)
        {
            RenderBinaryAvsContent(canvas, features);
        }
        else
        {
            RenderTextAvsContent(canvas, features);
        }

        // Render metadata overlay
        RenderMetadataOverlay(canvas);
    }

    private void RenderTextAvsContent(ISkiaCanvas canvas, AudioFeatures features)
    {
        // Render superscopes from text-based AVS files
        foreach (var scope in _avsFile.Superscopes.Take(5)) // Limit to 5 for performance
        {
            RenderSuperscope(canvas, scope, features);
        }

        // Render any effects from text parsing
        foreach (var effect in _avsFile.Effects.Take(3)) // Limit effects too
        {
            RenderAvsEffect(canvas, effect, features);
        }
    }

    private void RenderBinaryAvsContent(ISkiaCanvas canvas, AudioFeatures features)
    {
        // For binary AVS files, render the effects that were parsed
        foreach (var effect in _avsFile.Effects.Take(5))
        {
            RenderAvsEffect(canvas, effect, features);
        }

        // Show that this is binary content
        canvas.DrawText("BINARY AVS CONTENT", _width / 2 - 100, _height / 2, 0xFF00FF00, 16);
    }

    private void RenderSuperscope(ISkiaCanvas canvas, AvsImportService.AvsSuperscope scope, AudioFeatures features)
    {
        if (string.IsNullOrEmpty(scope.Code)) return;

        try
        {
            // Simple superscope rendering - evaluate basic math expressions
            var points = EvaluateSuperscopeCode(scope.Code, features);

            if (points.Count > 1)
            {
                // Render as connected lines
                uint color = 0xFF00FF00; // Green for superscopes
                for (int i = 1; i < points.Count; i++)
                {
                    var p1 = points[i - 1];
                    var p2 = points[i];
                    canvas.DrawLine(p1.X, p1.Y, p2.X, p2.Y, color, 2);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to render superscope {scope.Name}: {ex.Message}");
        }
    }

    private void RenderAvsEffect(ISkiaCanvas canvas, AvsImportService.AvsEffect effect, AudioFeatures features)
    {
        // Basic effect rendering based on type
        switch (effect.Type?.ToLower())
        {
            case "blur":
                RenderBlurEffect(canvas, effect, features);
                break;
            case "motion":
            case "movement":
                RenderMovementEffect(canvas, effect, features);
                break;
            case "color":
            case "color modifier":
                RenderColorEffect(canvas, effect, features);
                break;
            default:
                // Generic effect visualization
                RenderGenericEffect(canvas, effect, features);
                break;
        }
    }

    private void RenderBlurEffect(ISkiaCanvas canvas, AvsImportService.AvsEffect effect, AudioFeatures features)
    {
        // Simple blur visualization - just draw some soft circles
        var centerX = _width / 2;
        var centerY = _height / 2;
        var radius = 50 + features.Volume * 100;

        for (int i = 0; i < 3; i++)
        {
            var alpha = (byte)(100 - i * 30);
            var color = (uint)((alpha << 24) | 0x00FFFFFF);
            canvas.DrawCircle(centerX, centerY, radius + i * 10, color, false);
        }
    }

    private void RenderMovementEffect(ISkiaCanvas canvas, AvsImportService.AvsEffect effect, AudioFeatures features)
    {
        // Movement effect - draw moving lines
        var offset = _time * 50 % _width;
        canvas.DrawLine(offset, 0, offset, _height, 0xFF0088FF, 3);
        canvas.DrawLine(_width - offset, 0, _width - offset, _height, 0xFF0088FF, 3);
    }

    private void RenderColorEffect(ISkiaCanvas canvas, AvsImportService.AvsEffect effect, AudioFeatures features)
    {
        // Color effect - pulsating colors based on audio
        var hue = (_time * 50) % 360;
        var saturation = 0.7f + features.Bass * 0.3f;
        var brightness = 0.5f + features.Volume * 0.5f;

        // Convert HSV to RGB (simplified)
        var color = HsvToRgb(hue, saturation, brightness);
        canvas.DrawCircle(_width / 2, _height / 2, 100, color, true);
    }

    private void RenderGenericEffect(ISkiaCanvas canvas, AvsImportService.AvsEffect effect, AudioFeatures features)
    {
        // Generic effect - show effect info as text
        var y = 50 + (_avsFile.Effects.IndexOf(effect) * 30);
        canvas.DrawText($"{effect.Name}: {effect.Type}", 20, y, 0xFFFFFF00, 12);
    }

    private List<(float X, float Y)> EvaluateSuperscopeCode(string code, AudioFeatures features)
    {
        var points = new List<(float X, float Y)>();
        var lines = code.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Very basic superscope evaluation - just look for simple patterns
        foreach (var line in lines.Take(10)) // Limit processing
        {
            if (line.Contains("sin(") || line.Contains("cos("))
            {
                // Generate some points based on audio
                for (int i = 0; i < 50; i++)
                {
                    var t = (float)i / 49;
                    var x = _width / 2 + (float)Math.Sin(t * Math.PI * 4 + _time) * (_width / 4);
                    var y = _height / 2 + (float)Math.Cos(t * Math.PI * 4 + _time) * (_height / 4);

                    // Modulate with audio
                    if (features.Waveform.Length > 0)
                    {
                        var waveIndex = Math.Min(i * 4, features.Waveform.Length - 1);
                        x += features.Waveform[waveIndex] * 50;
                        y += features.Waveform[Math.Min(waveIndex + 1, features.Waveform.Length - 1)] * 50;
                    }

                    points.Add(((float)x, (float)y));
                }
                break; // Just process first math line
            }
        }

        return points;
    }

    private void RenderMetadataOverlay(ISkiaCanvas canvas)
    {
        // Show file info in corner
        var infoText = $"{_avsFile.FileName}";
        if (!string.IsNullOrEmpty(_avsFile.PresetName))
        {
            infoText += $" ({_avsFile.PresetName})";
        }

        canvas.DrawText(infoText, 10, _height - 30, 0xFFCCCCCC, 10);

        // Show format
        var formatText = _avsFile.IsBinaryFormat ? "BINARY" : "TEXT";
        canvas.DrawText(formatText, 10, _height - 15, 0xFFCCCCCC, 10);
    }

    private uint HsvToRgb(float h, float s, float v)
    {
        // Simple HSV to RGB conversion
        var c = v * s;
        var x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        var m = v - c;

        float r = 0, g = 0, b = 0;

        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        var ri = (byte)((r + m) * 255);
        var gi = (byte)((g + m) * 255);
        var bi = (byte)((b + m) * 255);

        return (uint)((0xFF << 24) | (ri << 16) | (gi << 8) | bi);
    }

    public void Dispose()
    {
        Log.Info($"Disposed ParsedAvsVisualizer for: {_avsFile.FileName}");
    }
}
