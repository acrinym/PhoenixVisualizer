using System;
using System.Collections.Generic;
using System.Numerics;

namespace PhoenixVisualizer.Core.Nodes;

/// <summary>
/// Sacred Geometry Visualizer - Creates patterns based on sacred geometric principles
/// </summary>
public class SacredGeometryNode : IEffectNode
{
    public string Name => "Sacred Geometry";

    public Dictionary<string, EffectParam> Params { get; } = new()
    {
        ["pattern"] = new EffectParam { Label = "Pattern", Type = "dropdown", StringValue = "flower_of_life", Options = new() { "flower_of_life", "metatrons_cube", "vesica_piscis", "golden_ratio", "fibonacci_spiral", "plato_solids" } },
        ["symmetry"] = new EffectParam { Label = "Symmetry", Type = "slider", FloatValue = 6f, Min = 3f, Max = 12f },
        ["scale"] = new EffectParam { Label = "Scale", Type = "slider", FloatValue = 1.0f, Min = 0.1f, Max = 3.0f },
        ["rotation"] = new EffectParam { Label = "Rotation", Type = "slider", FloatValue = 0f, Min = 0f, Max = 360f },
        ["complexity"] = new EffectParam { Label = "Complexity", Type = "slider", FloatValue = 0.5f, Min = 0.1f, Max = 1.0f },
        ["animation"] = new EffectParam { Label = "Animation", Type = "slider", FloatValue = 0.5f, Min = 0f, Max = 2.0f },
        ["color_scheme"] = new EffectParam { Label = "Color Scheme", Type = "dropdown", StringValue = "golden", Options = new() { "golden", "rainbow", "monochrome", "complementary" } }
    };

    private float _time = 0f;
    private readonly Random _random = new();

    public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
    {
        _time += 0.016f * Params["animation"].FloatValue;

        if (ctx.Canvas == null) return;

        // Clear canvas
        ctx.Canvas.Clear(0xFF000000);

        // Get parameters
        string pattern = Params["pattern"].StringValue;
        float symmetry = Params["symmetry"].FloatValue;
        float scale = Params["scale"].FloatValue;
        float rotation = Params["rotation"].FloatValue;
        float complexity = Params["complexity"].FloatValue;
        string colorScheme = Params["color_scheme"].StringValue;

        float centerX = ctx.Width * 0.5f;
        float centerY = ctx.Height * 0.5f;
        float maxRadius = MathF.Min(ctx.Width, ctx.Height) * 0.4f * scale;

        // Apply rotation
        float rotationRad = (rotation + _time * 30f) * MathF.PI / 180f;

        // Render based on pattern type
        switch (pattern)
        {
            case "flower_of_life":
                RenderFlowerOfLife(ctx, centerX, centerY, maxRadius, symmetry, complexity, rotationRad, colorScheme);
                break;
            case "metatrons_cube":
                RenderMetatronsCube(ctx, centerX, centerY, maxRadius, complexity, rotationRad, colorScheme);
                break;
            case "vesica_piscis":
                RenderVesicaPiscis(ctx, centerX, centerY, maxRadius, complexity, rotationRad, colorScheme);
                break;
            case "golden_ratio":
                RenderGoldenRatio(ctx, centerX, centerY, maxRadius, complexity, rotationRad, colorScheme);
                break;
            case "fibonacci_spiral":
                RenderFibonacciSpiral(ctx, centerX, centerY, maxRadius, complexity, rotationRad, colorScheme);
                break;
            case "plato_solids":
                RenderPlatoSolids(ctx, centerX, centerY, maxRadius, complexity, rotationRad, colorScheme);
                break;
        }
    }

    private void RenderFlowerOfLife(RenderContext ctx, float centerX, float centerY, float maxRadius, float symmetry, float complexity, float rotation, string colorScheme)
    {
        float baseRadius = maxRadius * 0.3f;
        int circles = (int)(symmetry * complexity * 2f);
        if (circles < 3) circles = 3;
        if (circles > 20) circles = 20;

        for (int i = 0; i < circles; i++)
        {
            float angle = (i / (float)circles) * MathF.PI * 2f + rotation;
            float radius = baseRadius * (1f + MathF.Sin(_time + i) * 0.2f);
            
            float x = centerX + MathF.Cos(angle) * radius;
            float y = centerY + MathF.Sin(angle) * radius;
            
            uint color = GetSacredColor(i, circles, colorScheme);
            ctx.Canvas!.DrawCircle(x, y, baseRadius * 0.8f, color, false);
        }

        // Center circle
        uint centerColor = GetSacredColor(0, 1, colorScheme);
        ctx.Canvas!.DrawCircle(centerX, centerY, baseRadius * 0.8f, centerColor, false);
    }

    private void RenderMetatronsCube(RenderContext ctx, float centerX, float centerY, float maxRadius, float complexity, float rotation, string colorScheme)
    {
        float baseRadius = maxRadius * 0.4f;
        
        // Draw the 13 circles of Metatron's Cube
        var positions = new List<Vector2>
        {
            new(centerX, centerY), // Center
            new(centerX, centerY - baseRadius), // Top
            new(centerX, centerY + baseRadius), // Bottom
            new(centerX - baseRadius, centerY), // Left
            new(centerX + baseRadius, centerY), // Right
            new(centerX - baseRadius * 0.5f, centerY - baseRadius * 0.866f), // Top-left
            new(centerX + baseRadius * 0.5f, centerY - baseRadius * 0.866f), // Top-right
            new(centerX - baseRadius * 0.5f, centerY + baseRadius * 0.866f), // Bottom-left
            new(centerX + baseRadius * 0.5f, centerY + baseRadius * 0.866f), // Bottom-right
            new(centerX - baseRadius * 0.866f, centerY - baseRadius * 0.5f), // Far top-left
            new(centerX + baseRadius * 0.866f, centerY - baseRadius * 0.5f), // Far top-right
            new(centerX - baseRadius * 0.866f, centerY + baseRadius * 0.5f), // Far bottom-left
            new(centerX + baseRadius * 0.866f, centerY + baseRadius * 0.5f)  // Far bottom-right
        };

        // Draw circles
        for (int i = 0; i < positions.Count; i++)
        {
            float radius = baseRadius * 0.3f * (1f + MathF.Sin(_time + i) * 0.1f);
            uint color = GetSacredColor(i, positions.Count, colorScheme);
            ctx.Canvas!.DrawCircle(positions[i].X, positions[i].Y, radius, color, false);
        }

        // Draw connecting lines
        uint lineColor = GetSacredColor(0, 1, colorScheme);
        for (int i = 0; i < positions.Count; i++)
        {
            for (int j = i + 1; j < positions.Count; j++)
            {
                float distance = Vector2.Distance(positions[i], positions[j]);
                if (distance < baseRadius * 1.8f) // Only connect nearby circles
                {
                    ctx.Canvas!.DrawLine(positions[i].X, positions[i].Y, positions[j].X, positions[j].Y, lineColor, 1f);
                }
            }
        }
    }

    private void RenderVesicaPiscis(RenderContext ctx, float centerX, float centerY, float maxRadius, float complexity, float rotation, string colorScheme)
    {
        float baseRadius = maxRadius * 0.4f;
        float offset = baseRadius * 0.5f;
        
        // Draw two overlapping circles
        uint circleColor = GetSacredColor(0, 2, colorScheme);
        ctx.Canvas!.DrawCircle(centerX - offset, centerY, baseRadius, circleColor, false);
        ctx.Canvas!.DrawCircle(centerX + offset, centerY, baseRadius, circleColor, false);
        
        // Draw the vesica piscis (almond shape) intersection
        uint intersectionColor = GetSacredColor(1, 2, colorScheme);
        
        // Create almond shape using multiple small circles
        for (int i = 0; i < 20; i++)
        {
            float t = i / 19f;
            float angle = t * MathF.PI * 2f + rotation;
            float radius = baseRadius * 0.3f * (1f + MathF.Sin(_time * 2f + t * MathF.PI) * 0.2f);
            
            float x = centerX + MathF.Cos(angle) * radius * 0.5f;
            float y = centerY + MathF.Sin(angle) * radius * 0.5f;
            
            ctx.Canvas!.FillCircle(x, y, 2f, intersectionColor);
        }
    }

    private void RenderGoldenRatio(RenderContext ctx, float centerX, float centerY, float maxRadius, float complexity, float rotation, string colorScheme)
    {
        float phi = 1.618033988749f; // Golden ratio
        float baseRadius = maxRadius * 0.3f;
        
        // Create golden rectangle spiral
        var points = new List<Vector2>();
        float x = centerX, y = centerY;
        float width = baseRadius, height = baseRadius;
        
        for (int i = 0; i < 12; i++)
        {
            points.Add(new Vector2(x, y));
            
            // Rotate and scale according to golden ratio
            float newWidth = height / phi;
            float newHeight = width;
            
            // Move to next rectangle position
            x += MathF.Cos(rotation + i * MathF.PI / 2f) * (width - newWidth);
            y += MathF.Sin(rotation + i * MathF.PI / 2f) * (height - newHeight);
            
            width = newWidth;
            height = newHeight;
        }
        
        // Draw golden rectangles
        uint rectColor = GetSacredColor(0, 1, colorScheme);
        for (int i = 0; i < points.Count - 1; i++)
        {
            var p1 = points[i];
            var p2 = points[i + 1];
            
            float rectWidth = MathF.Abs(p2.X - p1.X);
            float rectHeight = MathF.Abs(p2.Y - p1.Y);
            
            if (rectWidth > 5f && rectHeight > 5f) // Only draw if large enough
            {
                ctx.Canvas!.DrawRect(p1.X, p1.Y, rectWidth, rectHeight, rectColor, false);
            }
        }
    }

    private void RenderFibonacciSpiral(RenderContext ctx, float centerX, float centerY, float maxRadius, float complexity, float rotation, string colorScheme)
    {
        float baseRadius = maxRadius * 0.2f;
        
        // Generate Fibonacci sequence
        var fibonacci = new List<int> { 1, 1, 2, 3, 5, 8, 13, 21, 34, 55 };
        
        float x = centerX, y = centerY;
        float angle = rotation;
        
        for (int i = 0; i < fibonacci.Count; i++)
        {
            float radius = baseRadius * fibonacci[i] * 0.1f;
            if (radius > maxRadius * 0.8f) break;
            
            // Draw Fibonacci circle
            uint circleColor = GetSacredColor(i, fibonacci.Count, colorScheme);
            ctx.Canvas!.DrawCircle(x, y, radius, circleColor, false);
            
            // Move to next position
            x += MathF.Cos(angle) * radius * 0.5f;
            y += MathF.Sin(angle) * radius * 0.5f;
            angle += MathF.PI / 2f; // 90-degree turn
        }
    }

    private void RenderPlatoSolids(RenderContext ctx, float centerX, float centerY, float maxRadius, float complexity, float rotation, string colorScheme)
    {
        float baseRadius = maxRadius * 0.3f;
        
        // Tetrahedron (4 faces)
        RenderTetrahedron(ctx, centerX, centerY, baseRadius, rotation, colorScheme);
        
        // Cube (6 faces) - offset
        RenderCube(ctx, centerX + baseRadius * 1.5f, centerY, baseRadius, rotation, colorScheme);
        
        // Octahedron (8 faces) - offset
        RenderOctahedron(ctx, centerX - baseRadius * 1.5f, centerY, baseRadius, rotation, colorScheme);
    }

    private void RenderTetrahedron(RenderContext ctx, float centerX, float centerY, float radius, float rotation, string colorScheme)
    {
        var points = new List<Vector2>
        {
            new(centerX, centerY - radius), // Top
            new(centerX - radius * 0.866f, centerY + radius * 0.5f), // Bottom-left
            new(centerX + radius * 0.866f, centerY + radius * 0.5f)  // Bottom-right
        };
        
        uint color = GetSacredColor(0, 3, colorScheme);
        
        // Draw edges
        for (int i = 0; i < points.Count; i++)
        {
            var p1 = points[i];
            var p2 = points[(i + 1) % points.Count];
            ctx.Canvas!.DrawLine(p1.X, p1.Y, p2.X, p2.Y, color, 2f);
        }
        
        // Draw vertices
        foreach (var point in points)
        {
            ctx.Canvas!.FillCircle(point.X, point.Y, 3f, color);
        }
    }

    private void RenderCube(RenderContext ctx, float centerX, float centerY, float radius, float rotation, string colorScheme)
    {
        float halfRadius = radius * 0.5f;
        var points = new List<Vector2>
        {
            new(centerX - halfRadius, centerY - halfRadius), // Top-left
            new(centerX + halfRadius, centerY - halfRadius), // Top-right
            new(centerX + halfRadius, centerY + halfRadius), // Bottom-right
            new(centerX - halfRadius, centerY + halfRadius)  // Bottom-left
        };
        
        uint color = GetSacredColor(1, 3, colorScheme);
        
        // Draw edges
        for (int i = 0; i < points.Count; i++)
        {
            var p1 = points[i];
            var p2 = points[(i + 1) % points.Count];
            ctx.Canvas!.DrawLine(p1.X, p1.Y, p2.X, p2.Y, color, 2f);
        }
    }

    private void RenderOctahedron(RenderContext ctx, float centerX, float centerY, float radius, float rotation, string colorScheme)
    {
        var points = new List<Vector2>
        {
            new(centerX, centerY - radius), // Top
            new(centerX, centerY + radius), // Bottom
            new(centerX - radius, centerY), // Left
            new(centerX + radius, centerY)  // Right
        };
        
        uint color = GetSacredColor(2, 3, colorScheme);
        
        // Draw edges
        for (int i = 0; i < points.Count; i += 2)
        {
            var p1 = points[i];
            var p2 = points[i + 1];
            ctx.Canvas!.DrawLine(p1.X, p1.Y, p2.X, p2.Y, color, 2f);
        }
        
        // Draw vertices
        foreach (var point in points)
        {
            ctx.Canvas!.FillCircle(point.X, point.Y, 3f, color);
        }
    }

    private uint GetSacredColor(int index, int total, string colorScheme)
    {
        return colorScheme switch
        {
            "golden" => GetGoldenColor(index, total),
            "rainbow" => GetRainbowColor(index, total),
            "monochrome" => GetMonochromeColor(index, total),
            "complementary" => GetComplementaryColor(index, total),
            _ => GetGoldenColor(index, total)
        };
    }

    private uint GetGoldenColor(int index, int total)
    {
        float hue = (index / (float)total) * 360f;
        return HsvToRgb(hue, 0.8f, 0.9f);
    }

    private uint GetRainbowColor(int index, int total)
    {
        float hue = (index / (float)total) * 360f;
        return HsvToRgb(hue, 1.0f, 1.0f);
    }

    private uint GetMonochromeColor(int index, int total)
    {
        float intensity = 0.3f + (index / (float)total) * 0.7f;
        byte value = (byte)(intensity * 255);
        return (uint)((255 << 24) | (value << 16) | (value << 8) | value);
    }

    private uint GetComplementaryColor(int index, int total)
    {
        float hue = (index / (float)total) * 360f;
        if (index % 2 == 0)
        {
            return HsvToRgb(hue, 0.8f, 0.9f);
        }
        else
        {
            return HsvToRgb((hue + 180f) % 360f, 0.8f, 0.9f);
        }
    }

    private uint HsvToRgb(float h, float s, float v)
    {
        float c = v * s;
        float x = c * (1f - MathF.Abs((h / 60f) % 2f - 1f));
        float m = v - c;
        
        float r = 0f, g = 0f, b = 0f;
        
        if (h < 60f) { r = c; g = x; b = 0f; }
        else if (h < 120f) { r = x; g = c; b = 0f; }
        else if (h < 180f) { r = 0f; g = c; b = x; }
        else if (h < 240f) { r = 0f; g = x; b = c; }
        else if (h < 300f) { r = x; g = 0f; b = c; }
        else { r = c; g = 0f; b = x; }
        
        byte red = (byte)((r + m) * 255);
        byte green = (byte)((g + m) * 255);
        byte blue = (byte)((b + m) * 255);
        
        return (uint)((255 << 24) | (red << 16) | (green << 8) | blue);
    }
}
