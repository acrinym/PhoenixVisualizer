using System;
using System.Collections.Generic;
using System.Numerics;

namespace PhoenixVisualizer.Core.Nodes;

/// <summary>
/// Cymatics Visualizer - Creates patterns based on frequency vibrations in different materials
/// Based on scientific cymatics research and sacred geometry principles
/// </summary>
public class CymaticsNode : IEffectNode
{
    public string Name => "Cymatics Visualizer";

    public Dictionary<string, EffectParam> Params { get; } = new()
    {
        ["material"] = new EffectParam { Label = "Material", Type = "dropdown", StringValue = "water", Options = new() { "water", "sand", "salt", "metal", "air", "plasma" } },
        ["frequency"] = new EffectParam { Label = "Frequency (Hz)", Type = "slider", FloatValue = 432f, Min = 20f, Max = 2000f },
        ["intensity"] = new EffectParam { Label = "Intensity", Type = "slider", FloatValue = 0.8f, Min = 0f, Max = 1f },
        ["complexity"] = new EffectParam { Label = "Pattern Complexity", Type = "slider", FloatValue = 1.0f, Min = 0.1f, Max = 3.0f },
        ["temperature"] = new EffectParam { Label = "Temperature", Type = "slider", FloatValue = 20f, Min = -50f, Max = 100f },
        ["pressure"] = new EffectParam { Label = "Pressure", Type = "slider", FloatValue = 1.0f, Min = 0.1f, Max = 10f },
        ["density"] = new EffectParam { Label = "Density", Type = "slider", FloatValue = 1.0f, Min = 0.1f, Max = 5f },
        ["animationSpeed"] = new EffectParam { Label = "Animation Speed", Type = "slider", FloatValue = 1.0f, Min = 0.1f, Max = 5.0f },
        ["showNodalPoints"] = new EffectParam { Label = "Show Nodal Points", Type = "checkbox", BoolValue = true },
        ["showHarmonics"] = new EffectParam { Label = "Show Harmonics", Type = "checkbox", BoolValue = false },
        ["harmonicDepth"] = new EffectParam { Label = "Harmonic Depth", Type = "slider", FloatValue = 3f, Min = 1f, Max = 8f }
    };

    // Cymatic pattern state
    private float _time = 0f;
    private readonly Random _random = new();
    private readonly List<Vector2> _nodalPoints = new();
    private readonly List<Harmonic> _harmonics = new();

    // Material properties
    private readonly Dictionary<string, MaterialProperties> _materials = new()
    {
        ["water"] = new MaterialProperties { SpeedOfSound = 1482f, Density = 1000f, Viscosity = 1.0f },
        ["sand"] = new MaterialProperties { SpeedOfSound = 300f, Density = 1600f, Viscosity = 0.1f },
        ["salt"] = new MaterialProperties { SpeedOfSound = 4500f, Density = 2160f, Viscosity = 0.01f },
        ["metal"] = new MaterialProperties { SpeedOfSound = 5000f, Density = 7800f, Viscosity = 0.001f },
        ["air"] = new MaterialProperties { SpeedOfSound = 343f, Density = 1.225f, Viscosity = 0.000018f },
        ["plasma"] = new MaterialProperties { SpeedOfSound = 1000f, Density = 0.1f, Viscosity = 0.0001f }
    };

    public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
    {
        _time += 0.016f * Params["animationSpeed"].FloatValue;

        // Get parameters
        float frequency = Params["frequency"].FloatValue;
        float intensity = Params["intensity"].FloatValue;
        float complexity = Params["complexity"].FloatValue;
        string material = Params["material"].StringValue;
        float temperature = Params["temperature"].FloatValue;
        float pressure = Params["pressure"].FloatValue;
        float density = Params["density"].FloatValue;
        bool showNodalPoints = Params["showNodalPoints"].BoolValue;
        bool showHarmonics = Params["showHarmonics"].BoolValue;
        float harmonicDepth = Params["harmonicDepth"].FloatValue;

        // Calculate wavelength based on material properties
        float wavelength = CalculateWavelength(frequency, material, density, temperature, pressure);
        
        // Generate nodal points
        GenerateNodalPoints(ctx, wavelength, complexity);
        
        // Generate harmonics if enabled
        if (showHarmonics)
        {
            GenerateHarmonicSeries(frequency, harmonicDepth);
        }

        // Render cymatic pattern
        RenderCymaticPattern(ctx, wavelength, intensity, material);
        
        // Render nodal points if enabled
        if (showNodalPoints)
        {
            RenderNodalPoints(ctx, intensity);
        }
        
        // Render harmonics if enabled
        if (showHarmonics)
        {
            RenderHarmonics(ctx, intensity);
        }
    }

    private float CalculateWavelength(float frequency, string material, float density, float temperature, float pressure)
    {
        if (!_materials.ContainsKey(material))
            material = "water";

        var mat = _materials[material];
        
        // Adjust speed of sound for temperature and pressure
        float speedOfSound = mat.SpeedOfSound;
        speedOfSound *= MathF.Sqrt((temperature + 273.15f) / 293.15f); // Temperature adjustment
        speedOfSound *= MathF.Sqrt(pressure); // Pressure adjustment
        
        // Adjust for density effects
        speedOfSound *= MathF.Sqrt(pressure / density);
        
        // Wavelength = speed / frequency
        return speedOfSound / frequency;
    }

    private void GenerateNodalPoints(RenderContext ctx, float wavelength, float complexity)
    {
        _nodalPoints.Clear();
        
        float gridSpacing = wavelength * complexity;
        if (gridSpacing < 10f) gridSpacing = 10f; // Minimum spacing
        
        // Generate grid-based nodal points
        for (float x = 0; x < ctx.Width; x += gridSpacing)
        {
            for (float y = 0; y < ctx.Height; y += gridSpacing)
            {
                // Add complexity-based randomness
                float offsetX = MathF.Sin(x * 0.01f) * complexity * 10f;
                float offsetY = MathF.Cos(y * 0.01f) * complexity * 10f;
                
                _nodalPoints.Add(new Vector2(x + offsetX, y + offsetY));
            }
        }
    }

    private void GenerateHarmonicSeries(float fundamental, float depth)
    {
        _harmonics.Clear();
        
        for (int i = 1; i <= depth; i++)
        {
            float frequency = fundamental * i;
            float wavelength = 343f / frequency; // Speed of sound in air
            
            _harmonics.Add(new Harmonic
            {
                Order = i,
                Frequency = frequency,
                Wavelength = wavelength,
                MusicalInterval = GetMusicalInterval(i),
                Color = GetHarmonicColor(i)
            });
        }
    }

    private void RenderCymaticPattern(RenderContext ctx, float wavelength, float intensity, string material)
    {
        // Clear the canvas if available
        if (ctx.Canvas != null)
        {
            ctx.Canvas.Clear(0xFF000000); // Black background
        }

        float centerX = ctx.Width * 0.5f;
        float centerY = ctx.Height * 0.5f;
        float maxRadius = MathF.Min(ctx.Width, ctx.Height) * 0.4f;

        // Render material-specific patterns
        switch (material)
        {
            case "water":
                RenderWaterPattern(ctx, wavelength, intensity, centerX, centerY, maxRadius);
                break;
            case "sand":
                RenderSandPattern(ctx, wavelength, intensity, centerX, centerY, maxRadius);
                break;
            case "salt":
                RenderSaltPattern(ctx, wavelength, intensity, centerX, centerY, maxRadius);
                break;
            case "metal":
                RenderMetalPattern(ctx, wavelength, intensity, centerX, centerY, maxRadius);
                break;
            case "air":
                RenderAirPattern(ctx, wavelength, intensity, centerX, centerY, maxRadius);
                break;
            case "plasma":
                RenderPlasmaPattern(ctx, wavelength, intensity, centerX, centerY, maxRadius);
                break;
        }
    }

    private void RenderWaterPattern(RenderContext ctx, float wavelength, float intensity, float centerX, float centerY, float maxRadius)
    {
        if (ctx.Canvas == null) return;
        
        // Water patterns: concentric ripples and fluid dynamics
        for (int ring = 1; ring <= 8; ring++)
        {
            float ringRadius = ring * wavelength * 0.5f;
            if (ringRadius > maxRadius) break;
            
            byte alpha = (byte)(intensity * 255 * (1f - ring * 0.1f));
            uint color = (uint)((alpha << 24) | 0x0088FF); // Blue water color
            
            // Draw the main ring
            ctx.Canvas.DrawCircle(centerX, centerY, ringRadius, color, false);
            
            // Add ripple effect
            float rippleOffset = MathF.Sin(_time * 2f + ring * 0.5f) * 5f;
            float rippleRadius = ringRadius + rippleOffset;
            ctx.Canvas.DrawCircle(centerX, centerY, rippleRadius, color, false);
        }
    }

    private void RenderSandPattern(RenderContext ctx, float wavelength, float intensity, float centerX, float centerY, float maxRadius)
    {
        if (ctx.Canvas == null) return;
        
        // Sand patterns: geometric forms and granular dynamics
        int divisions = (int)(wavelength * 0.1f);
        if (divisions < 3) divisions = 3;
        if (divisions > 12) divisions = 12;
        
        for (int i = 0; i < divisions; i++)
        {
            float angle = (i / (float)divisions) * MathF.PI * 2f + _time * 0.5f;
            float radius = maxRadius * (0.3f + MathF.Sin(_time + i) * 0.2f);
            
            float x = centerX + MathF.Cos(angle) * radius;
            float y = centerY + MathF.Sin(angle) * radius;
            
            uint color = (uint)((int)(intensity * 255) << 24 | 0x00FFAA44); // Sand color
            
            // Draw sand particles
            ctx.Canvas.FillCircle(x, y, 3f, color);
        }
    }

    private void RenderSaltPattern(RenderContext ctx, float wavelength, float intensity, float centerX, float centerY, float maxRadius)
    {
        if (ctx.Canvas == null) return;
        
        // Salt patterns: crystalline structures with sharp angles
        int crystals = (int)(wavelength * 0.05f);
        if (crystals < 4) crystals = 4;
        if (crystals > 16) crystals = 16;
        
        for (int i = 0; i < crystals; i++)
        {
            float angle = (i / (float)crystals) * MathF.PI * 2f;
            float radius = maxRadius * 0.6f;
            
            // Create crystal points
            for (int j = 0; j < 6; j++) // Hexagonal crystals
            {
                float crystalAngle = angle + (j / 6f) * MathF.PI * 2f;
                float crystalRadius = radius * (0.8f + MathF.Sin(_time * 3f + i) * 0.2f);
                
                float x = centerX + MathF.Cos(crystalAngle) * crystalRadius;
                float y = centerY + MathF.Sin(crystalAngle) * crystalRadius;
                
                uint color = (uint)((int)(intensity * 255) << 24 | 0x00FFFFFF); // White salt color
                
                // Draw crystal points
                ctx.Canvas.FillCircle(x, y, 2f, color);
            }
        }
    }

    private void RenderMetalPattern(RenderContext ctx, float wavelength, float intensity, float centerX, float centerY, float maxRadius)
    {
        if (ctx.Canvas == null) return;
        
        // Metal patterns: electromagnetic fields and conductive patterns
        for (int field = 0; field < 4; field++)
        {
            float fieldAngle = field * MathF.PI * 0.5f + _time * 0.3f;
            float fieldRadius = maxRadius * (0.4f + MathF.Sin(_time * 2f + field) * 0.3f);
            
            // Draw field lines
            for (int line = 0; line < 8; line++)
            {
                float lineAngle = fieldAngle + (line / 8f) * MathF.PI * 2f;
                float lineLength = fieldRadius * 0.8f;
                
                float startX = centerX + MathF.Cos(lineAngle) * 10f;
                float startY = centerY + MathF.Sin(lineAngle) * 10f;
                float endX = centerX + MathF.Cos(lineAngle) * lineLength;
                float endY = centerY + MathF.Sin(lineAngle) * lineLength;
                
                uint color = (uint)((int)(intensity * 255) << 24 | 0x00C0C0C0); // Silver metal color
                
                // Draw field lines
                ctx.Canvas.DrawLine(startX, startY, endX, endY, color, 2f);
            }
        }
    }

    private void RenderAirPattern(RenderContext ctx, float wavelength, float intensity, float centerX, float centerY, float maxRadius)
    {
        if (ctx.Canvas == null) return;
        
        // Air patterns: wave interference and standing waves
        for (int wave = 0; wave < 6; wave++)
        {
            float waveAngle = wave * MathF.PI / 3f + _time * 0.4f;
            float waveRadius = maxRadius * (0.5f + MathF.Sin(_time * 1.5f + wave) * 0.4f);
            
            // Draw wave interference pattern
            for (int point = 0; point < 32; point++)
            {
                float pointAngle = (point / 32f) * MathF.PI * 2f;
                float interference = MathF.Sin(pointAngle * 3f + _time * 2f) * MathF.Cos(waveAngle + _time);
                float radius = waveRadius * (0.7f + interference * 0.3f);
                
                float x = centerX + MathF.Cos(pointAngle) * radius;
                float y = centerY + MathF.Sin(pointAngle) * radius;
                
                uint color = (uint)((int)(intensity * 255) << 24 | 0x0088FFFF); // Light blue air color
                
                // Draw interference points
                ctx.Canvas.FillCircle(x, y, 1.5f, color);
            }
        }
    }

    private void RenderPlasmaPattern(RenderContext ctx, float wavelength, float intensity, float centerX, float centerY, float maxRadius)
    {
        if (ctx.Canvas == null) return;
        
        // Plasma patterns: ionization patterns and field interactions
        for (int ion = 0; ion < 20; ion++)
        {
            float ionAngle = ion * MathF.PI * 0.1f + _time * 0.6f;
            float ionRadius = maxRadius * (0.3f + MathF.Sin(_time * 4f + ion) * 0.5f);
            
            float x = centerX + MathF.Cos(ionAngle) * ionRadius;
            float y = centerY + MathF.Sin(ionAngle) * ionRadius;
            
            // Plasma colors: purple to blue
            uint color = (uint)((int)(intensity * 255) << 24 | 0x00FF00FF); // Purple plasma color
            
            // Draw plasma ions
            ctx.Canvas.FillCircle(x, y, 4f, color);
            
            // Add glow effect
            uint glowColor = (uint)((int)(intensity * 100) << 24 | 0x00FF00FF);
            ctx.Canvas.FillCircle(x, y, 8f, glowColor);
        }
    }

    private void RenderNodalPoints(RenderContext ctx, float intensity)
    {
        if (ctx.Canvas == null) return;
        
        uint pointColor = (uint)((int)(intensity * 255) << 24 | 0x00FFFF00); // Yellow nodal points
        
        foreach (var point in _nodalPoints)
        {
            // Draw nodal points
            ctx.Canvas.FillCircle(point.X, point.Y, 2f, pointColor);
        }
    }

    private void RenderHarmonics(RenderContext ctx, float intensity)
    {
        if (ctx.Canvas == null) return;
        
        float centerX = ctx.Width * 0.5f;
        float centerY = ctx.Height * 0.5f;
        
        foreach (var harmonic in _harmonics)
        {
            float radius = harmonic.Wavelength * 2f;
            if (radius > ctx.Width * 0.8f) radius = ctx.Width * 0.8f;
            
            uint color = harmonic.Color;
            color = (uint)((int)(intensity * 255) << 24) | (color & 0x00FFFFFF);
            
            // Draw harmonic circles
            ctx.Canvas.DrawCircle(centerX, centerY, radius, color, false);
        }
    }

    private string GetMusicalInterval(int harmonic)
    {
        return harmonic switch
        {
            1 => "Unison",
            2 => "Octave",
            3 => "Perfect Fifth",
            4 => "Octave",
            5 => "Major Third",
            6 => "Perfect Fifth",
            7 => "Minor Seventh",
            8 => "Octave",
            _ => $"Harmonic {harmonic}"
        };
    }

    private uint GetHarmonicColor(int harmonic)
    {
        return harmonic switch
        {
            1 => 0x00FFFFFF, // White
            2 => 0x00FF0000, // Red
            3 => 0x0000FF00, // Green
            4 => 0x000000FF, // Blue
            5 => 0x00FFFF00, // Yellow
            6 => 0x00FF00FF, // Magenta
            7 => 0x0000FFFF, // Cyan
            8 => 0x00FF8000, // Orange
            _ => 0x00FFFFFF  // White
        };
    }

    private struct MaterialProperties
    {
        public float SpeedOfSound;
        public float Density;
        public float Viscosity;
    }

    private struct Harmonic
    {
        public int Order;
        public float Frequency;
        public float Wavelength;
        public string MusicalInterval;
        public uint Color;
    }
}
