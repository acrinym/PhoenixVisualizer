using System;
using System.Collections.Generic;
using System.Numerics;

namespace PhoenixVisualizer.Core.Nodes;

/// <summary>
/// Shader Visualizer - Advanced GLSL shader-based visualizer with ray marching
/// </summary>
public class ShaderVisualizerNode : IEffectNode
{
    public string Name => "Shader Visualizer";

    public Dictionary<string, EffectParam> Params { get; } = new()
    {
        ["speed"] = new EffectParam { Label = "Speed", Type = "slider", FloatValue = 1.0f, Min = 0.1f, Max = 5.0f },
        ["complexity"] = new EffectParam { Label = "Complexity", Type = "slider", FloatValue = 0.5f, Min = 0f, Max = 1f },
        ["colorShift"] = new EffectParam { Label = "Color Shift", Type = "slider", FloatValue = 0f, Min = 0f, Max = 360f },
        ["scene"] = new EffectParam { Label = "Scene", Type = "dropdown", StringValue = "mandelbulb", Options = new() { "mandelbulb", "menger_sponge", "sierpinski", "torus", "sphere_field", "fractal_trees" } },
        ["iterations"] = new EffectParam { Label = "Iterations", Type = "slider", FloatValue = 8f, Min = 4f, Max = 16f },
        ["zoom"] = new EffectParam { Label = "Zoom", Type = "slider", FloatValue = 1.0f, Min = 0.1f, Max = 5.0f },
        ["rotationX"] = new EffectParam { Label = "Rotation X", Type = "slider", FloatValue = 0f, Min = 0f, Max = 360f },
        ["rotationY"] = new EffectParam { Label = "Rotation Y", Type = "slider", FloatValue = 0f, Min = 0f, Max = 360f },
        ["lightIntensity"] = new EffectParam { Label = "Light Intensity", Type = "slider", FloatValue = 1.0f, Min = 0.1f, Max = 3.0f },
        ["ambientOcclusion"] = new EffectParam { Label = "Ambient Occlusion", Type = "slider", FloatValue = 0.5f, Min = 0f, Max = 1f }
    };

    private float _time = 0f;
    private readonly Random _random = new();

    public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
    {
        _time += 0.016f * Params["speed"].FloatValue;

        if (ctx.Canvas == null) return;

        // Clear canvas
        ctx.Canvas.Clear(0xFF000000);

        // Get parameters
        string scene = Params["scene"].StringValue;
        float complexity = Params["complexity"].FloatValue;
        float colorShift = Params["colorShift"].FloatValue;
        float iterations = Params["iterations"].FloatValue;
        float zoom = Params["zoom"].FloatValue;
        float rotationX = Params["rotationX"].FloatValue;
        float rotationY = Params["rotationY"].FloatValue;
        float lightIntensity = Params["lightIntensity"].FloatValue;
        float ambientOcclusion = Params["ambientOcclusion"].FloatValue;

        // Convert rotations to radians
        float rotX = (rotationX + _time * 20f) * MathF.PI / 180f;
        float rotY = (rotationY + _time * 15f) * MathF.PI / 180f;

        // Create rotation matrices
        var rotMatrixX = Matrix4x4.CreateRotationX(rotX);
        var rotMatrixY = Matrix4x4.CreateRotationY(rotY);
        var rotationMatrix = rotMatrixX * rotMatrixY;

        // Ray march through the scene
        for (int x = 0; x < ctx.Width; x += 2) // Skip pixels for performance
        {
            for (int y = 0; y < ctx.Height; y += 2)
            {
                // Normalize coordinates
                float u = (x / (float)ctx.Width) * 2f - 1f;
                float v = (y / (float)ctx.Height) * 2f - 1f;

                // Create ray direction
                var rayDir = Vector3.Normalize(new Vector3(u, v, 1f));
                
                // Apply rotation
                rayDir = Vector3.Transform(rayDir, rotationMatrix);

                // Ray march
                float distance = RayMarch(new Vector3(0, 0, -3f * zoom), rayDir, scene, iterations, complexity);
                
                if (distance < 100f) // Hit something
                {
                    // Calculate lighting
                    var hitPoint = new Vector3(0, 0, -3f * zoom) + rayDir * distance;
                    var normal = CalculateNormal(hitPoint, scene, iterations, complexity);
                    
                    // Lighting calculation
                    var lightDir = Vector3.Normalize(new Vector3(MathF.Sin(_time), MathF.Cos(_time), 1f));
                    float diffuse = MathF.Max(0, Vector3.Dot(normal, lightDir)) * lightIntensity;
                    float ambient = 0.2f * ambientOcclusion;
                    float lighting = MathF.Min(1f, ambient + diffuse);

                    // Calculate color based on scene and position
                    uint color = CalculateColor(hitPoint, normal, scene, complexity, colorShift, lighting);
                    
                    // Apply lighting
                    color = ApplyLighting(color, lighting);
                    
                    // Draw pixel
                    ctx.Canvas.FillCircle(x, y, 2f, color);
                }
            }
        }
    }

    private float RayMarch(Vector3 ro, Vector3 rd, string scene, float iterations, float complexity)
    {
        float totalDistance = 0f;
        const int maxSteps = 64;
        const float maxDistance = 100f;
        const float surfaceDistance = 0.01f;

        for (int i = 0; i < maxSteps; i++)
        {
            Vector3 p = ro + rd * totalDistance;
            float distance = GetDistance(p, scene, iterations, complexity);
            
            if (distance < surfaceDistance)
                return totalDistance;
                
            totalDistance += distance;
            
            if (totalDistance > maxDistance)
                break;
        }
        
        return maxDistance;
    }

    private float GetDistance(Vector3 p, string scene, float iterations, float complexity)
    {
        return scene switch
        {
            "mandelbulb" => MandelbulbDistance(p, iterations, complexity),
            "menger_sponge" => MengerSpongeDistance(p, iterations, complexity),
            "sierpinski" => SierpinskiDistance(p, iterations, complexity),
            "torus" => TorusDistance(p, complexity),
            "sphere_field" => SphereFieldDistance(p, complexity),
            "fractal_trees" => FractalTreesDistance(p, iterations, complexity),
            _ => MandelbulbDistance(p, iterations, complexity)
        };
    }

    private float MandelbulbDistance(Vector3 p, float iterations, float complexity)
    {
        Vector3 z = p;
        float dr = 1f;
        float r = 0f;
        
        for (int i = 0; i < iterations; i++)
        {
            r = z.Length();
            if (r > 2f) break;
            
            float theta = MathF.Acos(z.Z / r);
            float phi = MathF.Atan2(z.Y, z.X);
            
            float zr = MathF.Pow(r, 8f - 1f);
            theta = theta * 8f;
            phi = phi * 8f;
            
            z = zr * new Vector3(
                MathF.Sin(theta) * MathF.Cos(phi),
                MathF.Sin(theta) * MathF.Sin(phi),
                MathF.Cos(theta)
            ) + p;
            
            dr = MathF.Pow(r, 8f - 1f) * 8f * dr + 1f;
        }
        
        return 0.5f * MathF.Log(r) * r / dr;
    }

    private float MengerSpongeDistance(Vector3 p, float iterations, float complexity)
    {
        Vector3 z = p;
        float m = 1f;
        
        for (int i = 0; i < iterations; i++)
        {
            z = Vector3.Abs(z);
            if (z.X < z.Y) z = new Vector3(z.Y, z.X, z.Z);
            if (z.X < z.Z) z = new Vector3(z.Z, z.Y, z.X);
            if (z.Y < z.Z) z = new Vector3(z.X, z.Z, z.Y);
            
            z = z * 3f - Vector3.One * 2f;
            m *= 3f;
        }
        
        return (z.Length() - 1f) / m;
    }

    private float SierpinskiDistance(Vector3 p, float iterations, float complexity)
    {
        Vector3 z = p;
        float m = 1f;
        
        for (int i = 0; i < iterations; i++)
        {
            z = Vector3.Abs(z);
            if (z.X < z.Y) z = new Vector3(z.Y, z.X, z.Z);
            if (z.X < z.Z) z = new Vector3(z.Z, z.Y, z.X);
            if (z.Y < z.Z) z = new Vector3(z.X, z.Z, z.Y);
            
            z = z * 2f - Vector3.One;
            m *= 2f;
        }
        
        return (z.Length() - 1f) / m;
    }

    private float TorusDistance(Vector3 p, float complexity)
    {
        Vector2 q = new Vector2(new Vector2(p.X, p.Z).Length() - 0.5f, p.Y);
        return q.Length() - 0.2f;
    }

    private float SphereFieldDistance(Vector3 p, float complexity)
    {
        float d = 1000f;
        for (int i = 0; i < 8; i++)
        {
            float angle = i * MathF.PI * 2f / 8f;
            Vector3 center = new Vector3(
                MathF.Cos(angle) * 2f,
                MathF.Sin(angle) * 2f,
                0f
            );
            d = MathF.Min(d, Vector3.Distance(p, center) - 0.5f);
        }
        return d;
    }

    private float FractalTreesDistance(Vector3 p, float iterations, float complexity)
    {
        float d = 1000f;
        Vector3 z = p;
        
        for (int i = 0; i < iterations; i++)
        {
            z = Vector3.Abs(z);
            if (z.X < z.Y) z = new Vector3(z.Y, z.X, z.Z);
            if (z.X < z.Z) z = new Vector3(z.Z, z.Y, z.X);
            
            z = z * 2f - Vector3.One;
            d = MathF.Min(d, z.Length() - 0.5f);
        }
        
        return d;
    }

    private Vector3 CalculateNormal(Vector3 p, string scene, float iterations, float complexity)
    {
        const float eps = 0.01f;
        var d = GetDistance(p, scene, iterations, complexity);
        
        return Vector3.Normalize(new Vector3(
            GetDistance(p + new Vector3(eps, 0, 0), scene, iterations, complexity) - d,
            GetDistance(p + new Vector3(0, eps, 0), scene, iterations, complexity) - d,
            GetDistance(p + new Vector3(0, 0, eps), scene, iterations, complexity) - d
        ));
    }

    private uint CalculateColor(Vector3 p, Vector3 normal, string scene, float complexity, float colorShift, float lighting)
    {
        // Base color based on scene
        uint baseColor = scene switch
        {
            "mandelbulb" => 0x00FF6B6B, // Red
            "menger_sponge" => 0x006BFF6B, // Green
            "sierpinski" => 0x006B6BFF, // Blue
            "torus" => 0x00FFFF6B, // Yellow
            "sphere_field" => 0x00FF6BFF, // Magenta
            "fractal_trees" => 0x006BFFFF, // Cyan
            _ => 0x00FFFFFF // White
        };

        // Add complexity-based color variation
        float hue = (p.X + p.Y + p.Z) * 0.1f + colorShift;
        uint complexColor = HsvToRgb(hue, 0.8f, 0.9f);
        
        // Blend base and complex colors
        uint finalColor = BlendColors(baseColor, complexColor, complexity);
        
        // Add normal-based shading
        float normalShading = (normal.X + normal.Y + normal.Z) * 0.5f + 0.5f;
        finalColor = ApplyNormalShading(finalColor, normalShading);
        
        return finalColor;
    }

    private uint BlendColors(uint color1, uint color2, float blend)
    {
        byte r1 = (byte)(color1 >> 16), g1 = (byte)(color1 >> 8), b1 = (byte)color1;
        byte r2 = (byte)(color2 >> 16), g2 = (byte)(color2 >> 8), b2 = (byte)color2;
        
        byte r = (byte)(r1 * (1f - blend) + r2 * blend);
        byte g = (byte)(g1 * (1f - blend) + g2 * blend);
        byte b = (byte)(b1 * (1f - blend) + b2 * blend);
        
        return (uint)((255 << 24) | (r << 16) | (g << 8) | b);
    }

    private uint ApplyNormalShading(uint color, float shading)
    {
        byte r = (byte)(color >> 16), g = (byte)(color >> 8), b = (byte)color;
        
        r = (byte)(r * shading);
        g = (byte)(g * shading);
        b = (byte)(b * shading);
        
        return (uint)((255 << 24) | (r << 16) | (g << 8) | b);
    }

    private uint ApplyLighting(uint color, float lighting)
    {
        byte r = (byte)(color >> 16), g = (byte)(color >> 8), b = (byte)color;
        
        r = (byte)(r * lighting);
        g = (byte)(g * lighting);
        b = (byte)(b * lighting);
        
        return (uint)((255 << 24) | (r << 16) | (g << 8) | b);
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
