using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

public sealed class PhoenixCircularBarsPlugin : IVisualizerPlugin
{
    public string Id => "phoenix_circular_bars";
    public string DisplayName => "🎨 Phoenix Circular Bars";
    public string Description => "Fun animated circular bar chart that dances to the music!";
    public bool IsEnabled { get; set; } = true;

    private int _w, _h;
    private readonly Random _rng = new Random();
    
    // Animation parameters
    private float _rotation = 0f;
    private float _pulsePhase = 0f;
    private float _colorShift = 0f;
    private float _bouncePhase = 0f;
    
    // Bar configuration
    private const int BAR_COUNT = 32;
    private const float INNER_RADIUS = 0.15f;  // Center hole
    private const float OUTER_RADIUS = 0.85f;  // Max bar length
    private const float BAR_WIDTH = 0.08f;     // Angular width of each bar

    public void Initialize() { }
    public void Initialize(int width, int height) { _w = width; _h = height; }
    public void Resize(int width, int height) { _w = width; _h = height; }
    public void Shutdown() { }
    public void ProcessFrame(AudioFeatures features, ISkiaCanvas canvas) { RenderFrame(features, canvas); }
    public void Configure() { }
    public void Dispose() { }

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        // Clear with dark background
        canvas.Clear(0xFF0A0A0A);

        // Update animation phases
        float dt = 1f / 60f; // assume 60fps
        _rotation += dt * (2f + features.Mid * 3f);  // Rotation speed based on mid frequencies
        _pulsePhase += dt * (3f + features.Treble * 4f);  // Pulse speed based on treble
        _colorShift += dt * (1f + features.Rms * 2f);  // Color cycling based on overall energy
        _bouncePhase += dt * (4f + features.Bass * 6f);  // Bounce based on bass

        // Center point
        float centerX = _w * 0.5f;
        float centerY = _h * 0.5f;
        float maxRadius = Math.Min(_w, _h) * 0.4f;

        // Draw background glow rings
        DrawGlowRings(canvas, centerX, centerY, maxRadius, features);

        // Draw each bar
        for (int i = 0; i < BAR_COUNT; i++)
        {
            float angle = (i / (float)BAR_COUNT) * MathF.PI * 2f + _rotation;
            
            // Get FFT data for this bar (map bar index to FFT bins)
            int fftIndex = (i * features.Fft.Length) / BAR_COUNT;
            float fftValue = fftIndex < features.Fft.Length ? features.Fft[fftIndex] : 0f;
            
            // Calculate bar properties
            float barLength = INNER_RADIUS + (OUTER_RADIUS - INNER_RADIUS) * fftValue;
            float barHeight = barLength * maxRadius;
            
            // Add some fun variations
            float bounce = MathF.Sin(_bouncePhase + i * 0.3f) * 0.1f;
            float pulse = MathF.Sin(_pulsePhase + i * 0.2f) * 0.15f;
            barHeight *= (1f + bounce + pulse);
            
            // Ensure minimum height for visibility
            barHeight = Math.Max(barHeight, maxRadius * 0.02f);
            
            // Calculate bar endpoints
            float startRadius = INNER_RADIUS * maxRadius;
            float endRadius = startRadius + barHeight;
            
            float startX = centerX + startRadius * MathF.Cos(angle);
            float startY = centerY + startRadius * MathF.Sin(angle);
            float endX = centerX + endRadius * MathF.Cos(angle);
            float endY = centerY + endRadius * MathF.Sin(angle);
            
            // Calculate bar width (angular)
            float halfWidth = BAR_WIDTH * 0.5f;
            float width1 = startRadius * halfWidth;
            float width2 = endRadius * halfWidth;
            
            // Calculate corner points for the bar
            var corners = new[]
            {
                (startX + width1 * MathF.Cos(angle + MathF.PI/2), startY + width1 * MathF.Sin(angle + MathF.PI/2)),
                (startX + width1 * MathF.Cos(angle - MathF.PI/2), startY + width1 * MathF.Sin(angle - MathF.PI/2)),
                (endX + width2 * MathF.Cos(angle - MathF.PI/2), endY + width2 * MathF.Sin(angle - MathF.PI/2)),
                (endX + width2 * MathF.Cos(angle + MathF.PI/2), endY + width2 * MathF.Sin(angle + MathF.PI/2))
            };
            
            // Generate fun colors based on audio and position
            uint barColor = GenerateFunColor(i, fftValue, features);
            
            // Draw the bar
            DrawBar(canvas, corners, barColor, features);
        }

        // Draw center sparkle on beat
        if (features.Beat)
        {
            DrawCenterSparkle(canvas, centerX, centerY, maxRadius * 0.1f);
        }

        // Draw floating particles
        DrawFloatingParticles(canvas, features);
    }

    private void DrawGlowRings(ISkiaCanvas canvas, float centerX, float centerY, float maxRadius, AudioFeatures features)
    {
        // Inner glow ring
        uint innerGlowColor = 0x2200FFFF; // Subtle blue glow
        canvas.FillCircle(centerX, centerY, maxRadius * INNER_RADIUS * 1.2f, innerGlowColor);
        
        // Outer glow ring that pulses with bass
        float outerGlowRadius = maxRadius * OUTER_RADIUS * (1f + features.Bass * 0.3f);
        uint outerGlowColor = 0x1500FF88; // Very subtle outer glow
        canvas.FillCircle(centerX, centerY, outerGlowRadius, outerGlowColor);
    }

    private void DrawBar(ISkiaCanvas canvas, (float x, float y)[] corners, uint color, AudioFeatures features)
    {
        // Create a simple polygon by connecting the corners
        var points = new (float x, float y)[corners.Length + 1];
        for (int i = 0; i < corners.Length; i++)
        {
            points[i] = corners[i];
        }
        points[corners.Length] = corners[0]; // Close the polygon
        
        // Draw the filled bar by filling circles at each corner and connecting with lines
        // Fill the center area with multiple small circles
        float centerX = (corners[0].x + corners[1].x + corners[2].x + corners[3].x) / 4f;
        float centerY = (corners[0].y + corners[1].y + corners[2].y + corners[3].y) / 4f;
        
        // Calculate approximate radius for filling
        float maxDist = 0f;
        for (int i = 0; i < corners.Length; i++)
        {
            float dist = MathF.Sqrt((corners[i].x - centerX) * (corners[i].x - centerX) + 
                                   (corners[i].y - centerY) * (corners[i].y - centerY));
            maxDist = Math.Max(maxDist, dist);
        }
        
        // Fill with overlapping circles
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                float offsetX = (i - 2) * maxDist * 0.2f;
                float offsetY = (j - 2) * maxDist * 0.2f;
                float fillX = centerX + offsetX;
                float fillY = centerY + offsetY;
                
                // Check if this fill point is inside the bar bounds
                if (IsPointInBar(fillX, fillY, corners))
                {
                    canvas.FillCircle(fillX, fillY, maxDist * 0.15f, color);
                }
            }
        }
        
        // Add a bright edge highlight
        uint highlightColor = 0xFFFFFFFF; // White highlight
        canvas.SetLineWidth(1f);
        canvas.DrawLines(points, 1f, highlightColor);
    }
    
    private bool IsPointInBar(float x, float y, (float x, float y)[] corners)
    {
        // Simple point-in-polygon test using ray casting
        bool inside = false;
        for (int i = 0, j = corners.Length - 1; i < corners.Length; j = i++)
        {
            if (((corners[i].y > y) != (corners[j].y > y)) &&
                (x < (corners[j].x - corners[i].x) * (y - corners[i].y) / (corners[j].y - corners[i].y) + corners[i].x))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    private uint GenerateFunColor(int barIndex, float fftValue, AudioFeatures features)
    {
        // Base hue shifts over time
        float hue = (_colorShift + barIndex * 0.3f) % 360f;
        
        // Saturation based on FFT value and audio energy
        float saturation = 0.6f + fftValue * 0.4f + features.Rms * 0.3f;
        saturation = Math.Min(saturation, 1f);
        
        // Value (brightness) based on FFT and beat
        float value = 0.4f + fftValue * 0.5f + (features.Beat ? 0.3f : 0f);
        value = Math.Min(value, 1f);
        
        // Convert HSV to RGB (simplified)
        return HsvToRgb(hue, saturation, value);
    }

    private uint HsvToRgb(float h, float s, float v)
    {
        // Simplified HSV to RGB conversion
        float c = v * s;
        float x = c * (1f - Math.Abs((h / 60f) % 2f - 1f));
        float m = v - c;
        
        float r, g, b;
        if (h < 60f)
        {
            r = c; g = x; b = 0f;
        }
        else if (h < 120f)
        {
            r = x; g = c; b = 0f;
        }
        else if (h < 180f)
        {
            r = 0f; g = c; b = x;
        }
        else if (h < 240f)
        {
            r = 0f; g = x; b = c;
        }
        else if (h < 300f)
        {
            r = x; g = 0f; b = c;
        }
        else
        {
            r = c; g = 0f; b = x;
        }
        
        // Convert to 0-255 range and pack into uint
        byte red = (byte)((r + m) * 255f);
        byte green = (byte)((g + m) * 255f);
        byte blue = (byte)((b + m) * 255f);
        
        return (uint)(0xFF << 24 | red << 16 | green << 8 | blue);
    }

    private void DrawCenterSparkle(ISkiaCanvas canvas, float centerX, float centerY, float size)
    {
        // Draw a bright center sparkle
        uint sparkleColor = 0xFFFFFFFF; // Bright white
        canvas.FillCircle(centerX, centerY, size, sparkleColor);
        
        // Add some rays
        uint rayColor = 0x88FFFFFF; // Semi-transparent white
        canvas.SetLineWidth(2f);
        
        for (int i = 0; i < 8; i++)
        {
            float angle = (i / 8f) * MathF.PI * 2f;
            float endX = centerX + size * 2f * MathF.Cos(angle);
            float endY = centerY + size * 2f * MathF.Sin(angle);
            canvas.DrawLine(centerX, centerY, endX, endY, rayColor, 2f);
        }
    }

    private void DrawFloatingParticles(ISkiaCanvas canvas, AudioFeatures features)
    {
        // Draw some floating particles for extra fun
        int particleCount = 12;
        uint particleColor = 0x44FFFFFF; // Very subtle white particles
        
        for (int i = 0; i < particleCount; i++)
        {
            float time = (float)features.TimeSeconds;
            float x = _w * (0.1f + 0.8f * (0.5f + 0.5f * MathF.Sin(time * 0.5f + i * 0.7f)));
            float y = _h * (0.1f + 0.8f * (0.5f + 0.5f * MathF.Cos(time * 0.3f + i * 0.9f)));
            float size = 2f + MathF.Sin(time * 2f + i * 1.3f) * 2f;
            
            canvas.FillCircle(x, y, size, particleColor);
        }
    }
}
