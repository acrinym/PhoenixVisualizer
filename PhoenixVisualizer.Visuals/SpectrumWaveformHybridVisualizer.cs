using System;
using PhoenixVisualizer.Audio;
using PhoenixVisualizer.Core;
using PhoenixVisualizer.Core.Models;
using SkiaSharp;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// AVS-Inspired Spectrum Waveform Hybrid Visualizer
/// Combines frequency spectrum bars with time-domain waveform
/// Inspired by classic AVS presets that layered multiple data sources
/// </summary>
public class SpectrumWaveformHybridVisualizer : BaseVisualizer
{
    // Audio data buffers
    private float[] _spectrumData = Array.Empty<float>();
    private float[] _waveformData = Array.Empty<float>();

    // Visual parameters with global parameter support
    private int _barCount = 64;
    private float _sensitivity = 1.0f;
    private float _waveformAmplitude = 0.3f;
    private uint _spectrumColor = 0xFF00FFFF; // Cyan
    private uint _waveformColor = 0xFFFF00FF; // Magenta
    private float _barWidth = 0.8f;
    private float _waveformThickness = 2.0f;
    private bool _showSpectrum = true;
    private bool _showWaveform = true;
    private int _width = 800;
    private int _height = 600;

    public override string Id => "SpectrumWaveformHybrid";
    public override string Name => "Spectrum Waveform Hybrid";
    public override string Description => "AVS-inspired hybrid combining spectrum bars with waveform data";

    public override void Render(SKCanvas canvas, int width, int height, AudioFeatures audioFeatures, float deltaTime)
    {
        // For now, provide a basic implementation
        // This would need to be fully implemented to render spectrum bars and waveform
        if (canvas == null) return;

        // Clear canvas with black background
        canvas.Clear(SKColors.Black);

        // Basic implementation - would need to add actual spectrum/waveform rendering
        // This is a placeholder until the full rendering logic is implemented
    }

    public void Initialize(int width, int height)
    {
        Resize(width, height);

        // Register global parameters
        this.RegisterGlobalParameters(Id, new[]
        {
            GlobalParameterSystem.GlobalCategory.General,
            GlobalParameterSystem.GlobalCategory.Audio,
            GlobalParameterSystem.GlobalCategory.Visual
        });

        // Register specific parameters
        RegisterParameters();
    }

    private void RegisterParameters()
    {
        var parameters = new System.Collections.Generic.List<ParameterSystem.ParameterDefinition>
        {
            new ParameterSystem.ParameterDefinition
            {
                Key = "barCount",
                Label = "Spectrum Bars",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 64,
                MinValue = 16,
                MaxValue = 128,
                Description = "Number of spectrum bars to display",
                Category = "Spectrum"
            },
            new ParameterSystem.ParameterDefinition
            {
                Key = "spectrumColor",
                Label = "Spectrum Color",
                Type = ParameterSystem.ParameterType.Color,
                DefaultValue = "#00FFFF",
                Description = "Color of the spectrum bars",
                Category = "Spectrum"
            },
            new ParameterSystem.ParameterDefinition
            {
                Key = "waveformColor",
                Label = "Waveform Color",
                Type = ParameterSystem.ParameterType.Color,
                DefaultValue = "#FF00FF",
                Description = "Color of the waveform line",
                Category = "Waveform"
            },
            new ParameterSystem.ParameterDefinition
            {
                Key = "waveformAmplitude",
                Label = "Waveform Amplitude",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.3f,
                MinValue = 0.1f,
                MaxValue = 1.0f,
                Description = "Height of the waveform relative to screen",
                Category = "Waveform"
            },
            new ParameterSystem.ParameterDefinition
            {
                Key = "waveformThickness",
                Label = "Waveform Thickness",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 2.0f,
                MinValue = 0.5f,
                MaxValue = 5.0f,
                Description = "Thickness of the waveform line",
                Category = "Waveform"
            },
            new ParameterSystem.ParameterDefinition
            {
                Key = "barWidth",
                Label = "Bar Width",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.8f,
                MinValue = 0.1f,
                MaxValue = 1.0f,
                Description = "Width of spectrum bars as fraction of available space",
                Category = "Spectrum"
            },
            new ParameterSystem.ParameterDefinition
            {
                Key = "showSpectrum",
                Label = "Show Spectrum",
                Type = ParameterSystem.ParameterType.Checkbox,
                DefaultValue = true,
                Description = "Toggle spectrum bars visibility",
                Category = "Display"
            },
            new ParameterSystem.ParameterDefinition
            {
                Key = "showWaveform",
                Label = "Show Waveform",
                Type = ParameterSystem.ParameterType.Checkbox,
                DefaultValue = true,
                Description = "Toggle waveform visibility",
                Category = "Display"
            }
        };

        ParameterSystem.RegisterVisualizerParameters(Id, parameters);
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;

        // Initialize data buffers
        _spectrumData = new float[Math.Max(1, width / 4)]; // Adaptive spectrum buffer
        _waveformData = new float[Math.Max(1, width / 2)]; // Waveform buffer
    }

    public unsafe void RenderFrame(AudioFeatures features, IntPtr frameBuffer, int width, int height)
    {
        // Check global enable/disable
        var globalEnabled = GlobalParameterSystem.GetGlobalParameter<bool>(Id, GlobalParameterSystem.CommonParameters.Enabled, true);
        if (!globalEnabled) return;

        // Update global parameters
        var globalOpacity = GlobalParameterSystem.GetGlobalParameter<float>(Id, GlobalParameterSystem.CommonParameters.Opacity, 1.0f);
        var globalBrightness = GlobalParameterSystem.GetGlobalParameter<float>(Id, GlobalParameterSystem.CommonParameters.Brightness, 1.0f);
        var globalScale = GlobalParameterSystem.GetGlobalParameter<float>(Id, GlobalParameterSystem.CommonParameters.Scale, 1.0f);

        // Update specific parameters
        _barCount = ParameterSystem.GetParameterValue<int>(Id, "barCount", 64);
        _sensitivity = ParameterSystem.GetParameterValue<float>(Id, "sensitivity", 1.0f);
        _waveformAmplitude = ParameterSystem.GetParameterValue<float>(Id, "waveformAmplitude", 0.3f);
        _waveformThickness = ParameterSystem.GetParameterValue<float>(Id, "waveformThickness", 2.0f);
        _barWidth = ParameterSystem.GetParameterValue<float>(Id, "barWidth", 0.8f);
        _showSpectrum = ParameterSystem.GetParameterValue<bool>(Id, "showSpectrum", true);
        _showWaveform = ParameterSystem.GetParameterValue<bool>(Id, "showWaveform", true);

        // Apply global audio sensitivity
        _sensitivity *= GlobalParameterSystem.GetGlobalParameter<float>(Id, GlobalParameterSystem.CommonParameters.AudioSensitivity, 1.0f);

        // Parse colors and apply global effects
        var spectrumColorHex = ParameterSystem.GetParameterValue<string>(Id, "spectrumColor", "#00FFFF") ?? "#00FFFF";
        var waveformColorHex = ParameterSystem.GetParameterValue<string>(Id, "waveformColor", "#FF00FF") ?? "#FF00FF";

        _spectrumColor = ApplyGlobalEffects(ColorFromHex(spectrumColorHex), globalBrightness, globalOpacity);
        _waveformColor = ApplyGlobalEffects(ColorFromHex(waveformColorHex), globalBrightness, globalOpacity);

        // Apply global scaling
        _waveformAmplitude *= globalScale;
        _waveformThickness *= globalScale;

        // Get audio data
        var spectrum = features.Spectrum;
        var waveform = features.Waveform;

        // Clear frame buffer to black
        ClearFrameBuffer(frameBuffer, width, height);

        // Render spectrum bars if enabled
        if (_showSpectrum && spectrum != null && spectrum.Length > 0)
        {
            RenderSpectrumBars(frameBuffer, width, height, spectrum);
        }

        // Render waveform if enabled
        if (_showWaveform && waveform != null && waveform.Length > 0)
        {
            RenderWaveform(frameBuffer, width, height, waveform);
        }
    }

    private unsafe void RenderSpectrumBars(IntPtr frameBuffer, int width, int height, float[] spectrum)
    {
        int effectiveBarCount = Math.Min(_barCount, spectrum.Length);
        float barSpacing = width / (float)effectiveBarCount;
        float barWidth = barSpacing * _barWidth;

        for (int i = 0; i < effectiveBarCount; i++)
        {
            // Apply logarithmic scaling for better frequency response
            float magnitude = spectrum[i] * _sensitivity;
            magnitude = (float)(Math.Log(1 + magnitude * 9) / Math.Log(10)); // Log scale
            magnitude = Math.Clamp(magnitude, 0, 1);

            float barHeight = magnitude * height * 0.8f; // Use 80% of screen height
            float barX = i * barSpacing + (barSpacing - barWidth) * 0.5f;
            float barY = height - barHeight;

            // Draw the bar
            DrawRectangle(frameBuffer, width, height, barX, barY, barWidth, barHeight, _spectrumColor);
        }
    }

    private unsafe void RenderWaveform(IntPtr frameBuffer, int width, int height, float[] waveform)
    {
        if (waveform.Length < 2) return;

        // Sample waveform at regular intervals
        int sampleCount = Math.Min(width, waveform.Length);
        float xStep = width / (float)(sampleCount - 1);

        // Create waveform points
        var points = new (float x, float y)[sampleCount];
        float centerY = height * 0.5f;

        for (int i = 0; i < sampleCount; i++)
        {
            int sampleIndex = (int)(i * waveform.Length / (float)sampleCount);
            sampleIndex = Math.Clamp(sampleIndex, 0, waveform.Length - 1);

            float sample = waveform[sampleIndex] * _sensitivity;
            float y = centerY - sample * height * _waveformAmplitude * 0.5f;

            points[i] = (i * xStep, y);
        }

        // Draw waveform line
        DrawWaveformLine(frameBuffer, width, height, points, _waveformThickness, _waveformColor);
    }

    private unsafe void DrawRectangle(IntPtr frameBuffer, int width, int height, float x, float y, float rectWidth, float rectHeight, uint color)
    {
        int startX = (int)x;
        int startY = (int)y;
        int endX = (int)(x + rectWidth);
        int endY = (int)(y + rectHeight);

        startX = Math.Clamp(startX, 0, width - 1);
        endX = Math.Clamp(endX, 0, width - 1);
        startY = Math.Clamp(startY, 0, height - 1);
        endY = Math.Clamp(endY, 0, height - 1);

        for (int py = startY; py < endY; py++)
        {
            for (int px = startX; px < endX; px++)
            {
                SetPixel(frameBuffer, width, height, px, py, color);
            }
        }
    }

    private unsafe void DrawWaveformLine(IntPtr frameBuffer, int width, int height, (float x, float y)[] points, float thickness, uint color)
    {
        if (points.Length < 2) return;

        for (int i = 1; i < points.Length; i++)
        {
            var p1 = points[i - 1];
            var p2 = points[i];

            DrawLine(frameBuffer, width, height, p1.x, p1.y, p2.x, p2.y, thickness, color);
        }
    }

    private unsafe void DrawLine(IntPtr frameBuffer, int width, int height, float x1, float y1, float x2, float y2, float thickness, uint color)
    {
        // Bresenham's line algorithm with thickness
        int ix1 = (int)x1, iy1 = (int)y1;
        int ix2 = (int)x2, iy2 = (int)y2;

        int dx = Math.Abs(ix2 - ix1);
        int dy = Math.Abs(iy2 - iy1);
        int sx = ix1 < ix2 ? 1 : -1;
        int sy = iy1 < iy2 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            // Draw a circle of pixels around the current point for thickness
            DrawCircle(frameBuffer, width, height, ix1, iy1, thickness * 0.5f, color);

            if (ix1 == ix2 && iy1 == iy2) break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; ix1 += sx; }
            if (e2 < dx) { err += dx; iy1 += sy; }
        }
    }

    private unsafe void DrawCircle(IntPtr frameBuffer, int width, int height, int centerX, int centerY, float radius, uint color)
    {
        int r = (int)radius;
        for (int y = -r; y <= r; y++)
        {
            for (int x = -r; x <= r; x++)
            {
                if (x * x + y * y <= r * r)
                {
                    int px = centerX + x;
                    int py = centerY + y;
                    if (px >= 0 && px < width && py >= 0 && py < height)
                    {
                        SetPixel(frameBuffer, width, height, px, py, color);
                    }
                }
            }
        }
    }

    private unsafe void SetPixel(IntPtr frameBuffer, int width, int height, int x, int y, uint color)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;

        uint* pixels = (uint*)frameBuffer;
        pixels[y * width + x] = color;
    }

    private unsafe void ClearFrameBuffer(IntPtr frameBuffer, int width, int height)
    {
        uint* pixels = (uint*)frameBuffer;
        for (int i = 0; i < width * height; i++)
        {
            pixels[i] = 0xFF000000; // Black background
        }
    }

    private uint ApplyGlobalEffects(uint color, float brightness, float opacity)
    {
        // Extract RGBA components
        var a = ((color >> 24) & 0xFF) / 255.0f;
        var r = ((color >> 16) & 0xFF) / 255.0f;
        var g = ((color >> 8) & 0xFF) / 255.0f;
        var b = (color & 0xFF) / 255.0f;

        // Apply brightness
        r = Math.Clamp(r * brightness, 0, 1);
        g = Math.Clamp(g * brightness, 0, 1);
        b = Math.Clamp(b * brightness, 0, 1);

        // Apply opacity
        a *= opacity;

        // Convert back to uint
        return ((uint)(a * 255) << 24) |
               ((uint)(r * 255) << 16) |
               ((uint)(g * 255) << 8) |
               (uint)(b * 255);
    }

    private uint ColorFromHex(string hexColor)
    {
        if (string.IsNullOrEmpty(hexColor) || !hexColor.StartsWith("#"))
            return 0xFF00FFFF; // Default cyan

        try
        {
            var color = System.Drawing.ColorTranslator.FromHtml(hexColor);
            return ((uint)color.A << 24) |
                   ((uint)color.R << 16) |
                   ((uint)color.G << 8) |
                   (uint)color.B;
        }
        catch
        {
            return 0xFF00FFFF; // Default cyan
        }
    }
}
