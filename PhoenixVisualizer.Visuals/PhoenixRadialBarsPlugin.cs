using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

public sealed class PhoenixRadialBarsPlugin : IVisualizerPlugin
{
    public string Id => "phoenix_radial_bars";
    public string DisplayName => "🔥 Phoenix Radial Bars";
    public string Description => "Classic Winamp-style polar spectrum with rotating bars and Phoenix fire colors";
    public bool IsEnabled { get; set; } = true;

    private int _w, _h;
    private float _rotation = 0f;
    private readonly int _numBars = 64;
    private readonly float _barWidth = 2f;
    private readonly float _maxRadius = 0.8f;

    // Phoenix color palette (NO GREEN!)
    private readonly uint[] _fireColors = new uint[]
    {
        0xFFFF4400, // Hot red-orange (bass)
        0xFFFF6600, // Bright orange
        0xFFFF8800, // Warm orange
        0xFFFFAA00, // Golden orange
        0xFFFFCC00, // Bright yellow
        0xFFFFEE00, // Light yellow
        0xFFFFFFFF  // White (treble sparkles)
    };

    public void Initialize() { }
    public void Initialize(int width, int height) { _w = width; _h = height; }
    public void Resize(int width, int height) { _w = width; _h = height; }
    public void Shutdown() { }
    public void ProcessFrame(AudioFeatures features, ISkiaCanvas canvas) { RenderFrame(features, canvas); }
    public void Configure() { }
    public void Dispose() { }

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        if (features.Fft?.Length == 0) return;

        // Clear with dark background
        canvas.Clear(0xFF000000);

        // Update rotation based on beat and time
        _rotation += 0.02f;
        if (features.Beat) _rotation += 0.1f; // Extra rotation on beat

        var centerX = _w / 2f;
        var centerY = _h / 2f;
        var radius = Math.Min(_w, _h) * 0.4f;

        // Draw inner glow circle (bass-driven)
        var glowRadius = 20f + features.Bass * 30f;
        var glowColor = (uint)(((byte)(features.Bass * 100)) << 24 | 0xFFFF4400);
        canvas.FillCircle(centerX, centerY, glowRadius, glowColor);

        // Draw radial bars
        for (int i = 0; i < _numBars; i++)
        {
            var angle = (i / (float)_numBars) * Math.PI * 2f + _rotation;
            
            // Get FFT data for this bar (map circular to linear)
            var fftIndex = (int)((i / (float)_numBars) * features.Fft.Length);
            if (fftIndex >= features.Fft.Length) fftIndex = features.Fft.Length - 1;
            
            var magnitude = MathF.Min(1f, features.Fft[fftIndex] * 3f); // Boost sensitivity
            
            // Bass boost for lower frequencies
            var bassBoost = i < _numBars / 4 ? 1.5f : 1.0f;
            magnitude *= bassBoost;
            
            // Calculate bar length and position
            var barLength = magnitude * radius * _maxRadius;
            var startRadius = 30f; // Start from inner glow
            
            var startX = centerX + (float)Math.Cos(angle) * startRadius;
            var startY = centerY + (float)Math.Sin(angle) * startRadius;
            var endX = centerX + (float)Math.Cos(angle) * (startRadius + barLength);
            var endY = centerY + (float)Math.Sin(angle) * (startRadius + barLength);

            // Get color based on frequency and intensity
            var color = GetPhoenixColor(magnitude, i, _numBars);
            
            // Add alpha based on magnitude
            var alpha = (byte)(magnitude * 255);
            color = (color & 0x00FFFFFF) | ((uint)alpha << 24);

            // Draw the bar with thickness
            canvas.SetLineWidth(_barWidth);
            canvas.DrawLine(startX, startY, endX, endY, color, _barWidth);

            // Add sparkle effect on strong hits
            if (magnitude > 0.8f)
            {
                var sparkleRadius = 3f + magnitude * 5f;
                var sparkleColor = (color & 0x00FFFFFF) | 0xFF000000; // Full alpha
                canvas.FillCircle(endX, endY, sparkleRadius, sparkleColor);
            }
        }

        // Draw bass anchor circle (center pulse)
        if (features.Bass > 0.3f)
        {
            var anchorRadius = 15f + features.Bass * 20f;
            var anchorColor = (uint)(((byte)(features.Bass * 200)) << 24 | 0xFFFF0000);
            canvas.FillCircle(centerX, centerY, anchorRadius, anchorColor);
        }

        // Draw outer ring for visual boundary
        canvas.SetLineWidth(1f);
        canvas.DrawCircle(centerX, centerY, radius, 0x44FFFFFF, false);
    }

    private uint GetPhoenixColor(float intensity, int barIndex, int totalBars)
    {
        // Map bar position to color: inner = red/orange, outer = yellow/white
        var positionRatio = (float)barIndex / totalBars;
        
        if (positionRatio < 0.3f) // Inner bars (bass)
        {
            var t = positionRatio / 0.3f;
            return InterpolateColor(_fireColors[0], _fireColors[2], t);
        }
        else if (positionRatio < 0.7f) // Middle bars
        {
            var t = (positionRatio - 0.3f) / 0.4f;
            return InterpolateColor(_fireColors[2], _fireColors[4], t);
        }
        else // Outer bars (treble)
        {
            var t = (positionRatio - 0.7f) / 0.3f;
            return InterpolateColor(_fireColors[4], _fireColors[6], t);
        }
    }

    private uint InterpolateColor(uint color1, uint color2, float t)
    {
        var r1 = (color1 >> 16) & 0xFF;
        var g1 = (color1 >> 8) & 0xFF;
        var b1 = color1 & 0xFF;
        
        var r2 = (color2 >> 16) & 0xFF;
        var g2 = (color2 >> 8) & 0xFF;
        var b2 = color2 & 0xFF;

        var r = (byte)(r1 + (r2 - r1) * t);
        var g = (byte)(g1 + (g2 - g1) * t);
        var b = (byte)(b1 + (b2 - b1) * t);

        return (uint)((r << 16) | (g << 8) | b);
    }
}
