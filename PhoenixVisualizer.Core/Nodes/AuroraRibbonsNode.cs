using System;
using System.Collections.Generic;
using System.Numerics;

namespace PhoenixVisualizer.Core.Nodes;

public class AuroraRibbonsNode : IEffectNode
{
    public string Name => "Aurora Ribbons";
    public Dictionary<string, EffectParam> Params { get; } = new()
    {
        ["amplitude"] = new EffectParam{ Label="Amplitude", Type="slider", Min=0.1f, Max=2f, FloatValue=1f },
        ["speed"] = new EffectParam{ Label="Speed", Type="slider", Min=0.1f, Max=5f, FloatValue=1f },
        ["color"] = new EffectParam{ Label="Color", Type="color", ColorValue="#00FFAA" },
        ["numRibbons"] = new EffectParam{ Label="Number of Ribbons", Type="slider", Min=3, Max=12, FloatValue=6 },
        ["waveFrequency"] = new EffectParam{ Label="Wave Frequency", Type="slider", Min=0.5f, Max=3f, FloatValue=1f },
        ["ribbonThickness"] = new EffectParam{ Label="Ribbon Thickness", Type="slider", Min=2f, Max=20f, FloatValue=8f },
        ["colorVariation"] = new EffectParam{ Label="Color Variation", Type="slider", Min=0, Max=1f, FloatValue=0.5f },
        ["spectrumReactivity"] = new EffectParam{ Label="Spectrum Reactivity", Type="slider", Min=0, Max=2f, FloatValue=1f }
    };

    private float _time = 0f;

    public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
    {
        if (ctx.Canvas == null) return;

        _time += 0.016f;

        // Get parameters
        float amplitude = Params["amplitude"].FloatValue;
        float speed = Params["speed"].FloatValue;
        int numRibbons = (int)Params["numRibbons"].FloatValue;
        float waveFrequency = Params["waveFrequency"].FloatValue;
        float ribbonThickness = Params["ribbonThickness"].FloatValue;
        float colorVariation = Params["colorVariation"].FloatValue;
        float spectrumReactivity = Params["spectrumReactivity"].FloatValue;

        // Parse base color
        uint baseColor = ParseColor(Params["color"].ColorValue);

        // Calculate audio reactivity
        float audioEnergy = GetAudioEnergy(waveform, spectrum);
        float dynamicAmplitude = amplitude * (1f + audioEnergy * spectrumReactivity);

        // Render aurora ribbons
        RenderAuroraRibbons(ctx, numRibbons, dynamicAmplitude, speed, waveFrequency, ribbonThickness, baseColor, colorVariation, audioEnergy);
    }

    private void RenderAuroraRibbons(RenderContext ctx, int numRibbons, float amplitude, float speed, float waveFrequency, float thickness, uint baseColor, float colorVariation, float audioEnergy)
    {
        float ribbonSpacing = (float)ctx.Height / numRibbons;

        for (int ribbonIndex = 0; ribbonIndex < numRibbons; ribbonIndex++)
        {
            float baseY = ribbonSpacing * ribbonIndex + ribbonSpacing * 0.5f;
            float ribbonOffset = ribbonIndex * 0.5f; // Phase offset for each ribbon

            // Generate ribbon color variation
            uint ribbonColor = GenerateRibbonColor(baseColor, ribbonIndex, numRibbons, colorVariation, audioEnergy);

            // Draw the ribbon as a series of connected segments
            RenderRibbonSegments(ctx, baseY, ribbonOffset, amplitude, speed, waveFrequency, thickness, ribbonColor, audioEnergy);
        }
    }

    private void RenderRibbonSegments(RenderContext ctx, float baseY, float ribbonOffset, float amplitude, float speed, float waveFrequency, float thickness, uint color, float audioEnergy)
    {
        const int segments = 32;
        float segmentWidth = (float)ctx.Width / segments;

        Vector2[] ribbonPoints = new Vector2[segments + 1];

        // Generate ribbon path points
        for (int i = 0; i <= segments; i++)
        {
            float x = i * segmentWidth;
            float timeOffset = _time * speed + ribbonOffset;

            // Create multiple sine waves for organic movement
            float wave1 = MathF.Sin(x * 0.01f * waveFrequency + timeOffset) * amplitude * 20f;
            float wave2 = MathF.Sin(x * 0.005f * waveFrequency + timeOffset * 0.7f) * amplitude * 15f;
            float wave3 = MathF.Sin(x * 0.02f * waveFrequency + timeOffset * 1.3f) * amplitude * 10f;

            // Combine waves for complex ribbon shape
            float y = baseY + wave1 + wave2 + wave3;

            // Add audio reactivity modulation
            y += audioEnergy * MathF.Sin(x * 0.015f + timeOffset * 2f) * 30f;

            ribbonPoints[i] = new Vector2(x, y);
        }

        // Draw the ribbon as connected circles/rectangles
        for (int i = 0; i < segments; i++)
        {
            Vector2 startPoint = ribbonPoints[i];
            Vector2 endPoint = ribbonPoints[i + 1];

            // Draw ribbon segment
            DrawRibbonSegment(ctx, startPoint, endPoint, thickness, color, audioEnergy);
        }
    }

    private void DrawRibbonSegment(RenderContext ctx, Vector2 start, Vector2 end, float thickness, uint color, float audioEnergy)
    {
        // Calculate ribbon width based on thickness and audio energy
        float ribbonWidth = thickness * (1f + audioEnergy * 0.5f);

        // Draw ribbon as a filled rectangle between points
        Vector2 direction = end - start;
        float length = direction.Length();

        if (length < 1f) return;

        Vector2 perpendicular = new Vector2(-direction.Y, direction.X);
        perpendicular = Vector2.Normalize(perpendicular) * (ribbonWidth * 0.5f);

        // Calculate ribbon corners
        Vector2 corner1 = start + perpendicular;
        Vector2 corner2 = start - perpendicular;
        Vector2 corner3 = end - perpendicular;
        Vector2 corner4 = end + perpendicular;

        // Draw ribbon as filled polygon
        DrawFilledRectangle(ctx, corner1, corner2, corner3, corner4, color);

        // Add some inner glow for aurora effect
        uint glowColor = ApplyAlpha(color, 0.4f);
        DrawRectangleOutline(ctx, corner1, corner2, corner3, corner4, glowColor, 2f);
    }

    private void DrawFilledRectangle(RenderContext ctx, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, uint color)
    {
        // Simple filled rectangle drawing using multiple circles
        // In a full implementation, this would use proper polygon filling
        int steps = 10;
        for (int i = 0; i < steps; i++)
        {
            float t = (float)i / (steps - 1);

            // Interpolate along the two edges
            Vector2 leftEdge = Vector2.Lerp(p1, p4, t);
            Vector2 rightEdge = Vector2.Lerp(p2, p3, t);

            // Draw line between edges
            DrawLineBetweenPoints(ctx, leftEdge, rightEdge, color);
        }
    }

    private void DrawLineBetweenPoints(RenderContext ctx, Vector2 start, Vector2 end, uint color)
    {
        Vector2 delta = end - start;
        float length = delta.Length();
        Vector2 direction = Vector2.Normalize(delta);

        int steps = Math.Max(1, (int)length);
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector2 point = start + direction * length * t;

            if (point.X >= 0 && point.X < ctx.Width && point.Y >= 0 && point.Y < ctx.Height)
            {
                ctx.Canvas!.FillCircle((int)point.X, (int)point.Y, 1f, color);
            }
        }
    }

    private void DrawRectangleOutline(RenderContext ctx, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, uint color, float thickness)
    {
        DrawLineBetweenPoints(ctx, p1, p4, color);
        DrawLineBetweenPoints(ctx, p4, p3, color);
        DrawLineBetweenPoints(ctx, p3, p2, color);
        DrawLineBetweenPoints(ctx, p2, p1, color);
    }

    private uint GenerateRibbonColor(uint baseColor, int ribbonIndex, int totalRibbons, float colorVariation, float audioEnergy)
    {
        // Extract base color components
        byte r = (byte)(baseColor >> 16);
        byte g = (byte)(baseColor >> 8);
        byte b = (byte)baseColor;

        // Apply color variation based on ribbon index
        float hueShift = (float)ribbonIndex / totalRibbons * colorVariation;
        float energyShift = audioEnergy * 0.3f;

        // Modify color components based on ribbon position and audio
        float ribbonPosition = (float)ribbonIndex / totalRibbons;
        r = (byte)(r * (0.7f + ribbonPosition * 0.6f + energyShift));
        g = (byte)(g * (0.8f + hueShift + energyShift));
        b = (byte)(b * (0.9f - ribbonPosition * 0.3f + energyShift));

        // Ensure values stay within byte range
        r = (byte)Math.Clamp((int)r, 0, 255);
        g = (byte)Math.Clamp((int)g, 0, 255);
        b = (byte)Math.Clamp((int)b, 0, 255);

        return (uint)((255 << 24) | (r << 16) | (g << 8) | b);
    }

    private float GetAudioEnergy(float[] waveform, float[] spectrum)
    {
        if (waveform.Length == 0 && spectrum.Length == 0) return 0.3f;

        float energy = 0f;

        // Use spectrum for ribbon amplitude modulation
        if (spectrum.Length > 0)
        {
            for (int i = 0; i < Math.Min(spectrum.Length, 50); i++)
            {
                energy += spectrum[i];
            }
            energy /= Math.Min(spectrum.Length, 50);
        }

        // Add waveform contribution
        if (waveform.Length > 0)
        {
            float waveEnergy = 0f;
            for (int i = 0; i < Math.Min(waveform.Length, 100); i++)
            {
                waveEnergy += Math.Abs(waveform[i]);
            }
            waveEnergy /= Math.Min(waveform.Length, 100);
            energy = Math.Max(energy, waveEnergy);
        }

        return Math.Clamp(energy, 0f, 1f);
    }

    private uint ApplyAlpha(uint color, float alpha)
    {
        byte r = (byte)(color >> 16);
        byte g = (byte)(color >> 8);
        byte b = (byte)color;
        byte a = (byte)(alpha * 255);

        return (uint)((a << 24) | (r << 16) | (g << 8) | b);
    }

    private uint ParseColor(string colorString)
    {
        // Simple hex color parser
        if (colorString.StartsWith("#") && colorString.Length == 7)
        {
            string hex = colorString.Substring(1);
            if (uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out uint color))
            {
                return 0xFF000000 | color; // Add full alpha
            }
        }
        return 0xFF00FFAA; // Default cyan-green
    }
}