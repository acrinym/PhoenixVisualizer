# Transitions (Trans / Movement)

## Overview

The **Transitions** system is a sophisticated coordinate transformation engine that provides 24 built-in transition effects and custom scripting capabilities for creating dynamic visual transformations. It implements both radial and rectangular coordinate systems, subpixel precision, and advanced blending modes. This effect is essential for creating motion effects, coordinate warping, and complex geometric transformations in AVS presets.

## Source Analysis

### Core Architecture (`r_trans.cpp`)

The effect is implemented as a C++ class `C_TransTabClass` that inherits from `C_RBASE2`. It provides a comprehensive transformation system with built-in effects, custom scripting via EEL, and advanced rendering optimizations including SMP support and subpixel precision.

### Key Components

#### Built-in Transition Effects
24 predefined transformation types:
- **None** (0): No transformation
- **Slight Fuzzify** (1): Random pixel displacement
- **Shift Rotate Left** (2): Horizontal shifting with rotation
- **Big Swirl Out** (3): Radial outward swirling
- **Medium Swirl** (4): Moderate radial swirling
- **Sunburster** (5): Radial distortion with cosine modulation
- **Swirl to Center** (6): Inward swirling with center focus
- **Blocky Partial Out** (7): Block-based partial transformation
- **Swirling Both Ways** (8): Dual-direction swirling
- **Bubbling Outward** (9): Outward bubble effect
- **Bubbling with Swirl** (10): Combined bubble and swirl
- **5-Pointed Distro** (11): 5-pointed star distortion
- **Tunneling** (12): Tunnel-like radial effect
- **Bleedin'** (13): Radial bleeding effect
- **Shifted Big Swirl** (14): Offset radial swirling
- **Psychotic Beaming** (15): Extreme outward projection
- **Cosine Radial 3-Way** (16): 3-way cosine modulation
- **Spinny Tube** (17): Rotating tube effect
- **Radial Swirlies** (18): Complex radial patterns
- **Swill** (19): Advanced radial distortion
- **Gridley** (20): Grid-based coordinate warping
- **Grapevine** (21): Vine-like coordinate patterns
- **Quadrant** (22): Quadrant-based transformations
- **6-Way Kaleida** (23): 6-way kaleidoscope effect

#### Custom Scripting Engine
Advanced EEL integration:
- **Custom Expressions**: User-defined transformation scripts
- **Variable Access**: r, d, x, y, sw, sh variables
- **Rectangular Mode**: Cartesian coordinate system support
- **Radial Mode**: Polar coordinate system support
- **Real-time Compilation**: Dynamic script compilation and execution

#### Coordinate Systems
Dual coordinate system support:
- **Radial System**: Polar coordinates (r, d) for circular effects
- **Rectangular System**: Cartesian coordinates (x, y) for grid effects
- **Subpixel Precision**: 32-level subpixel interpolation
- **Boundary Handling**: Wrap and clamp modes for edge cases

#### Performance Optimization
Advanced rendering optimizations:
- **SMP Support**: Multi-threaded rendering with thread distribution
- **Subpixel Rendering**: High-precision coordinate interpolation
- **MMX Integration**: SIMD optimization for blending operations
- **Efficient Loops**: Optimized pixel processing algorithms

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `effect` | int | 0-23, 32767 | 1 | Transition effect selection |
| `blend` | int | 0-1 | 0 | Enable blending mode |
| `sourcemapped` | int | 0-3 | 0 | Source mapping mode |
| `rectangular` | int | 0-1 | 0 | Use rectangular coordinates |
| `subpixel` | int | 0-1 | 1 | Enable subpixel precision |
| `wrap` | int | 0-1 | 0 | Enable coordinate wrapping |
| `effect_exp` | string | Custom | "" | Custom EEL expression |

### Source Mapping Modes

| Mode | Value | Description |
|------|-------|-------------|
| **Normal** | 0 | Standard transformation |
| **Source Mapped** | 1 | Map to source buffer |
| **Beat Reactive** | 2 | Toggle on beat |
| **Combined** | 3 | Source mapped + beat reactive |

## C# Implementation

```csharp
public class TransitionsNode : AvsModuleNode
{
    public int EffectType { get; set; } = 1;
    public bool EnableBlending { get; set; } = false;
    public int SourceMappingMode { get; set; } = 0;
    public bool UseRectangularCoordinates { get; set; } = false;
    public bool EnableSubpixelPrecision { get; set; } = true;
    public bool EnableCoordinateWrapping { get; set; } = false;
    public string CustomExpression { get; set; } = "";
    
    // Internal state
    private int[] transformationTable;
    private int tableWidth, tableHeight;
    private int lastEffectType;
    private bool expressionChanged;
    private readonly object renderLock = new object();
    private readonly IScriptEngine scriptEngine;
    
    // Performance optimization
    private const int MaxEffects = 24;
    private const int CustomEffectId = 32767;
    private const int SubpixelShift = 22;
    private const int SubpixelMask = 0x1F;
    private const int MaxSubpixelValue = 31;
    
    // Built-in effect descriptions
    private static readonly TransitionEffect[] BuiltInEffects = new TransitionEffect[]
    {
        new TransitionEffect(0, "none", "", false, false),
        new TransitionEffect(1, "slight fuzzify", "", false, false),
        new TransitionEffect(2, "shift rotate left", "x=x+1/32; // use wrap for this one", false, true),
        new TransitionEffect(3, "big swirl out", "r = r + (0.1 - (0.2 * d));\r\nd = d * 0.96;", false, false),
        new TransitionEffect(4, "medium swirl", "d = d * (0.99 * (1.0 - sin(r-$PI*0.5) / 32.0));\r\nr = r + (0.03 * sin(d * $PI * 4));", false, false),
        new TransitionEffect(5, "sunburster", "d = d * (0.94 + (cos((r-$PI*0.5) * 32.0) * 0.06));", false, false),
        new TransitionEffect(6, "swirl to center", "d = d * (1.01 + (cos((r-$PI*0.5) * 4) * 0.04));\r\nr = r + (0.03 * sin(d * $PI * 4));", false, false),
        new TransitionEffect(7, "blocky partial out", "", false, false),
        new TransitionEffect(8, "swirling around both ways at once", "r = r + (0.1 * sin(d * $PI * 5));", false, false),
        new TransitionEffect(9, "bubbling outward", "t = sin(d * $PI);\r\nd = d - (8*t*t*t*t*t)/sqrt((sw*sw+sh*sh)/4);", false, false),
        new TransitionEffect(10, "bubbling outward with swirl", "t = sin(d * $PI);\r\nd = d - (8*t*t*t*t*t)/sqrt((sw*sw+sh*sh)/4);\r\nt=cos(d*$PI/2.0);\r\nr= r + 0.1*t*t*t;", false, false),
        new TransitionEffect(11, "5 pointed distro", "d = d * (0.95 + (cos(((r-$PI*0.5) * 5.0) - ($PI / 2.50)) * 0.03));", false, false),
        new TransitionEffect(12, "tunneling", "r = r + 0.04;\r\nd = d * (0.96 + cos(d * $PI) * 0.05);", false, false),
        new TransitionEffect(13, "bleedin'", "t = cos(d * $PI);\r\nr = r + (0.07 * t);\r\nd = d * (0.98 + t * 0.10);", false, false),
        new TransitionEffect(14, "shifted big swirl out", "// this is a very bad approximation in script. fixme.\r\nd=sqrt(x*x+y*y); r=atan2(y,x);\r\nr=r+0.1-0.2*d; d=d*0.96;\r\nx=cos(r)*d + 8/128; y=sin(r)*d;", false, true),
        new TransitionEffect(15, "psychotic beaming outward", "d = 0.15", false, false),
        new TransitionEffect(16, "cosine radial 3-way", "r = cos(r * 3)", false, false),
        new TransitionEffect(17, "spinny tube", "d = d * (1 - ((d - .35) * .5));\r\nr = r + .1;", false, false),
        new TransitionEffect(18, "radial swirlies", "d = d * (1 - (sin((r-$PI*0.5) * 7) * .03));\r\nr = r + (cos(d * 12) * .03);", true, false),
        new TransitionEffect(19, "swill", "d = d * (1 - (sin((r - $PI*0.5) * 12) * .05));\r\nr = r + (cos(d * 18) * .05);\r\nd = d * (1-((d - .4) * .03));\r\nr = r + ((d - .4) * .13)", true, false),
        new TransitionEffect(20, "gridley", "x = x + (cos(y * 18) * .02);\r\ny = y + (sin(x * 14) * .03);", true, true),
        new TransitionEffect(21, "grapevine", "x = x + (cos(abs(y-.5) * 8) * .02);\r\ny = y + (sin(abs(x-.5) * 8) * .05);\r\nx = x * .95;\r\ny = y * .95;", true, true),
        new TransitionEffect(22, "quadrant", "y = y * ( 1 + (sin(r + $PI/2) * .3) );\r\nx = x * ( 1 + (cos(r + $PI/2) * .3) );\r\nx = x * .995;\r\ny = y * .995;", true, true),
        new TransitionEffect(23, "6-way kaleida (use wrap!)", "y = (r*6)/($PI); x = d;", true, true)
    };
    
    public TransitionsNode()
    {
        scriptEngine = new PhoenixScriptEngine();
        transformationTable = null;
        tableWidth = tableHeight = 0;
        lastEffectType = -1;
        expressionChanged = true;
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (ctx.Width <= 0 || ctx.Height <= 0) return;
        
        // Update transformation table if needed
        UpdateTransformationTable(ctx);
        
        // Apply transformation effect
        ApplyTransformationEffect(ctx, input, output);
    }
    
    private void UpdateTransformationTable(FrameContext ctx)
    {
        lock (renderLock)
        {
            if (transformationTable == null || 
                tableWidth != ctx.Width || 
                tableHeight != ctx.Height || 
                lastEffectType != EffectType || 
                expressionChanged)
            {
                GenerateTransformationTable(ctx);
                tableWidth = ctx.Width;
                tableHeight = ctx.Height;
                lastEffectType = EffectType;
                expressionChanged = false;
            }
        }
    }
    
    private void GenerateTransformationTable(FrameContext ctx)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        int tableSize = width * height;
        
        if (transformationTable == null || transformationTable.Length != tableSize)
        {
            transformationTable = new int[tableSize];
        }
        
        if (EffectType == 0) // None
        {
            for (int i = 0; i < tableSize; i++)
            {
                transformationTable[i] = i;
            }
        }
        else if (EffectType == 1) // Slight Fuzzify
        {
            Random random = new Random();
            for (int i = 0; i < tableSize; i++)
            {
                int x = i % width;
                int y = i / width;
                int offsetX = (random.Next(3) - 1) + (random.Next(3) - 1) * width;
                int newIndex = i + offsetX;
                transformationTable[i] = Math.Clamp(newIndex, 0, tableSize - 1);
            }
        }
        else if (EffectType == 2) // Shift Rotate Left
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int newX = (x + width / 64) % width;
                    transformationTable[y * width + x] = y * width + newX;
                }
            }
        }
        else if (EffectType == 7) // Blocky Partial Out
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if ((x & 2) != 0 || (y & 2) != 0)
                    {
                        transformationTable[y * width + x] = y * width + x;
                    }
                    else
                    {
                        int centerX = width / 2 + (((x & ~1) - width / 2) * 7) / 8;
                        int centerY = height / 2 + (((y & ~1) - height / 2) * 7) / 8;
                        transformationTable[y * width + x] = centerY * width + centerX;
                    }
                }
            }
        }
        else if (EffectType >= 3 && EffectType <= 23 && EffectType != 18 && EffectType != 19)
        {
            // Built-in radial effects
            GenerateRadialEffectTable(ctx, EffectType);
        }
        else if (EffectType == CustomEffectId || IsEffectUsingEval(EffectType))
        {
            // Custom expression or eval-based effect
            GenerateCustomEffectTable(ctx);
        }
    }
    
    private void GenerateRadialEffectTable(FrameContext ctx, int effectType)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        double maxDistance = Math.Sqrt((width * width + height * height) / 4.0);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double xd = x - (width / 2.0);
                double yd = y - (height / 2.0);
                double distance = Math.Sqrt(xd * xd + yd * yd);
                double angle = Math.Atan2(yd, xd);
                
                // Apply effect-specific transformation
                ApplyRadialEffect(effectType, ref angle, ref distance, maxDistance);
                
                // Convert back to coordinates
                double newX = (width / 2.0) + Math.Cos(angle) * distance;
                double newY = (height / 2.0) + Math.Sin(angle) * distance;
                
                int outputX, outputY;
                if (EnableSubpixelPrecision)
                {
                    outputX = (int)newX;
                    outputY = (int)newY;
                    int xPartial = (int)(32.0 * (newX - outputX));
                    int yPartial = (int)(32.0 * (newY - outputY));
                    
                    if (EnableCoordinateWrapping)
                    {
                        outputX = (outputX % (width - 1) + width - 1) % (width - 1);
                        outputY = (outputY % (height - 1) + height - 1) % (height - 1);
                    }
                    else
                    {
                        outputX = Math.Clamp(outputX, 0, width - 2);
                        outputY = Math.Clamp(outputY, 0, height - 2);
                        xPartial = Math.Clamp(xPartial, 0, MaxSubpixelValue);
                        yPartial = Math.Clamp(yPartial, 0, MaxSubpixelValue);
                    }
                    
                    transformationTable[y * width + x] = outputY * width + outputX | 
                                                     (yPartial << SubpixelShift) | 
                                                     (xPartial << (SubpixelShift + 5));
                }
                else
                {
                    outputX = (int)(newX + 0.5);
                    outputY = (int)(newY + 0.5);
                    
                    if (EnableCoordinateWrapping)
                    {
                        outputX = (outputX % width + width) % width;
                        outputY = (outputY % height + height) % height;
                    }
                    else
                    {
                        outputX = Math.Clamp(outputX, 0, width - 1);
                        outputY = Math.Clamp(outputY, 0, height - 1);
                    }
                    
                    transformationTable[y * width + x] = outputY * width + outputX;
                }
            }
        }
    }
    
    private void ApplyRadialEffect(int effectType, ref double angle, ref double distance, double maxDistance)
    {
        double normalizedDistance = distance / maxDistance;
        
        switch (effectType)
        {
            case 3: // Big Swirl Out
                angle += 0.1 - 0.2 * normalizedDistance;
                distance *= 0.96;
                break;
            case 4: // Medium Swirl
                distance *= 0.99 * (1.0 - Math.Sin(angle) / 32.0);
                angle += 0.03 * Math.Sin(normalizedDistance * Math.PI * 4);
                break;
            case 5: // Sunburster
                distance *= 0.94 + (Math.Cos(angle * 32.0) * 0.06);
                break;
            case 6: // Swirl to Center
                distance *= 1.01 + (Math.Cos(angle * 4.0) * 0.04);
                angle += 0.03 * Math.Sin(normalizedDistance * Math.PI * 4);
                break;
            case 8: // Swirling Both Ways
                angle += 0.1 * Math.Sin(normalizedDistance * Math.PI * 5);
                break;
            case 9: // Bubbling Outward
                double t = Math.Sin(normalizedDistance * Math.PI);
                distance -= 8 * t * t * t * t * t;
                break;
            case 10: // Bubbling with Swirl
                t = Math.Sin(normalizedDistance * Math.PI);
                distance -= 8 * t * t * t * t * t;
                t = Math.Cos(normalizedDistance * Math.PI / 2.0);
                angle += 0.1 * t * t * t;
                break;
            case 11: // 5-Pointed Distro
                distance *= 0.95 + (Math.Cos(angle * 5.0 - Math.PI / 2.50) * 0.03);
                break;
            case 12: // Tunneling
                angle += 0.04;
                distance *= 0.96 + Math.Cos(normalizedDistance * Math.PI) * 0.05;
                break;
            case 13: // Bleedin'
                t = Math.Cos(normalizedDistance * Math.PI);
                angle += 0.07 * t;
                distance *= 0.98 + t * 0.10;
                break;
            case 14: // Shifted Big Swirl
                angle += 0.1 - 0.2 * normalizedDistance;
                distance *= 0.96;
                break;
            case 15: // Psychotic Beaming
                distance = maxDistance * 0.15;
                break;
            case 16: // Cosine Radial 3-Way
                angle = Math.Cos(angle * 3);
                break;
            case 17: // Spinny Tube
                distance *= (1 - ((normalizedDistance - 0.35) * 0.5));
                angle += 0.1;
                break;
        }
    }
    
    private void GenerateCustomEffectTable(FrameContext ctx)
    {
        if (string.IsNullOrEmpty(CustomExpression))
        {
            // Fallback to identity transformation
            for (int i = 0; i < transformationTable.Length; i++)
            {
                transformationTable[i] = i;
            }
            return;
        }
        
        int width = ctx.Width;
        int height = ctx.Height;
        double maxDistance = Math.Sqrt((width * width + height * height) / 4.0);
        
        try
        {
            // Compile custom expression
            var compiledScript = scriptEngine.Compile(CustomExpression);
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double xd = x - (width / 2.0);
                    double yd = y - (height / 2.0);
                    double distance = Math.Sqrt(xd * xd + yd * yd);
                    double angle = Math.Atan2(yd, xd) + Math.PI * 0.5;
                    
                    // Set script variables
                    var variables = new Dictionary<string, double>
                    {
                        ["x"] = xd / (width / 2.0),
                        ["y"] = yd / (height / 2.0),
                        ["d"] = distance / maxDistance,
                        ["r"] = angle,
                        ["sw"] = width,
                        ["sh"] = height
                    };
                    
                    // Execute script
                    scriptEngine.Execute(compiledScript, variables);
                    
                    // Get transformed coordinates
                    double newX, newY;
                    if (UseRectangularCoordinates)
                    {
                        newX = (variables["x"] + 1.0) * (width / 2.0);
                        newY = (variables["y"] + 1.0) * (height / 2.0);
                    }
                    else
                    {
                        distance = variables["d"] * maxDistance;
                        angle = variables["r"] - Math.PI / 2.0;
                        newX = (height / 2.0) + Math.Sin(angle) * distance;
                        newY = (width / 2.0) + Math.Cos(angle) * distance;
                    }
                    
                    // Apply coordinate transformation with subpixel precision
                    ApplyCoordinateTransformation(x, y, newX, newY, width, height);
                }
            }
        }
        catch (Exception)
        {
            // Fallback to identity transformation on error
            for (int i = 0; i < transformationTable.Length; i++)
            {
                transformationTable[i] = i;
            }
        }
    }
    
    private void ApplyCoordinateTransformation(int x, int y, double newX, double newY, int width, int height)
    {
        int outputX, outputY;
        
        if (EnableSubpixelPrecision)
        {
            outputX = (int)newX;
            outputY = (int)newY;
            int xPartial = (int)(32.0 * (newX - outputX));
            int yPartial = (int)(32.0 * (newY - outputY));
            
            if (EnableCoordinateWrapping)
            {
                outputX = (outputX % (width - 1) + width - 1) % (width - 1);
                outputY = (outputY % (height - 1) + height - 1) % (height - 1);
            }
            else
            {
                outputX = Math.Clamp(outputX, 0, width - 2);
                outputY = Math.Clamp(outputY, 0, height - 2);
                xPartial = Math.Clamp(xPartial, 0, MaxSubpixelValue);
                yPartial = Math.Clamp(yPartial, 0, MaxSubpixelValue);
            }
            
            transformationTable[y * width + x] = outputY * width + outputX | 
                                               (yPartial << SubpixelShift) | 
                                               (xPartial << (SubpixelShift + 5));
        }
        else
        {
            outputX = (int)(newX + 0.5);
            outputY = (int)(newY + 0.5);
            
            if (EnableCoordinateWrapping)
            {
                outputX = (outputX % width + width) % width;
                outputY = (outputY % height + height) % height;
            }
            else
            {
                outputX = Math.Clamp(outputX, 0, width - 1);
                outputY = Math.Clamp(outputY, 0, height - 1);
            }
            
            transformationTable[y * width + x] = outputY * width + outputX;
        }
    }
    
    private void ApplyTransformationEffect(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (transformationTable == null) return;
        
        int width = ctx.Width;
        int height = ctx.Height;
        
        // Handle source mapping modes
        if ((SourceMappingMode & 2) != 0 && ctx.IsBeat)
        {
            SourceMappingMode ^= 1;
        }
        
        if ((SourceMappingMode & 1) != 0)
        {
            if (!EnableBlending)
            {
                output.Clear(Color.Black);
            }
            else
            {
                output.CopyFrom(input);
            }
        }
        
        // Apply transformation
        if (EnableSubpixelPrecision)
        {
            ApplySubpixelTransformation(ctx, input, output);
        }
        else
        {
            ApplyStandardTransformation(ctx, input, output);
        }
        
        // Apply blending if enabled
        if (EnableBlending)
        {
            ApplyBlendingMode(ctx, input, output);
        }
    }
    
    private void ApplySubpixelTransformation(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int tableIndex = y * width + x;
                int transformValue = transformationTable[tableIndex];
                
                int offset = transformValue & ((1 << SubpixelShift) - 1);
                int xPartial = (transformValue >> (SubpixelShift + 5)) & SubpixelMask;
                int yPartial = (transformValue >> SubpixelShift) & SubpixelMask;
                
                int sourceX = offset % width;
                int sourceY = offset / width;
                
                // Apply subpixel interpolation
                Color pixel = InterpolateSubpixel(input, sourceX, sourceY, xPartial, yPartial, width);
                output.SetPixel(x, y, pixel);
            }
        }
    }
    
    private void ApplyStandardTransformation(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int tableIndex = y * width + x;
                int sourceIndex = transformationTable[tableIndex];
                
                int sourceX = sourceIndex % width;
                int sourceY = sourceIndex / width;
                
                Color pixel = input.GetPixel(sourceX, sourceY);
                output.SetPixel(x, y, pixel);
            }
        }
    }
    
    private Color InterpolateSubpixel(ImageBuffer input, int x, int y, int xPartial, int yPartial, int width)
    {
        // Get surrounding pixels for interpolation
        Color c00 = input.GetPixel(x, y);
        Color c01 = (y + 1 < input.Height) ? input.GetPixel(x, y + 1) : c00;
        Color c10 = (x + 1 < width) ? input.GetPixel(x + 1, y) : c00;
        Color c11 = (x + 1 < width && y + 1 < input.Height) ? input.GetPixel(x + 1, y + 1) : c00;
        
        // Normalize partial values
        double xFactor = xPartial / (double)MaxSubpixelValue;
        double yFactor = yPartial / (double)MaxSubpixelValue;
        
        // Bilinear interpolation
        Color interpolated = Color.FromArgb(
            (int)(c00.A * (1 - xFactor) * (1 - yFactor) + 
                  c01.A * (1 - xFactor) * yFactor + 
                  c10.A * xFactor * (1 - yFactor) + 
                  c11.A * xFactor * yFactor),
            (int)(c00.R * (1 - xFactor) * (1 - yFactor) + 
                  c01.R * (1 - xFactor) * yFactor + 
                  c10.R * xFactor * (1 - yFactor) + 
                  c11.R * xFactor * yFactor),
            (int)(c00.G * (1 - xFactor) * (1 - yFactor) + 
                  c01.G * (1 - xFactor) * yFactor + 
                  c10.G * xFactor * (1 - yFactor) + 
                  c11.G * xFactor * yFactor),
            (int)(c00.B * (1 - xFactor) * (1 - yFactor) + 
                  c01.B * (1 - xFactor) * yFactor + 
                  c10.B * xFactor * (1 - yFactor) + 
                  c11.B * xFactor * yFactor)
        );
        
        return interpolated;
    }
    
    private void ApplyBlendingMode(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color inputPixel = input.GetPixel(x, y);
                Color outputPixel = output.GetPixel(x, y);
                
                Color blendedPixel = Color.FromArgb(
                    (inputPixel.A + outputPixel.A) / 2,
                    (inputPixel.R + outputPixel.R) / 2,
                    (inputPixel.G + outputPixel.G) / 2,
                    (inputPixel.B + outputPixel.B) / 2
                );
                
                output.SetPixel(x, y, blendedPixel);
            }
        }
    }
    
    private bool IsEffectUsingEval(int effectType)
    {
        if (effectType >= 0 && effectType < BuiltInEffects.Length)
        {
            return BuiltInEffects[effectType].UsesEval;
        }
        return false;
    }
    
    // Public interface for parameter adjustment
    public void SetEffectType(int effectType) 
    { 
        EffectType = Math.Clamp(effectType, 0, MaxEffects - 1); 
        if (effectType == CustomEffectId) EffectType = CustomEffectId;
    }
    
    public void SetEnableBlending(bool enable) { EnableBlending = enable; }
    
    public void SetSourceMappingMode(int mode) 
    { 
        SourceMappingMode = Math.Clamp(mode, 0, 3); 
    }
    
    public void SetUseRectangularCoordinates(bool useRect) { UseRectangularCoordinates = useRect; }
    
    public void SetEnableSubpixelPrecision(bool enable) 
    { 
        EnableSubpixelPrecision = enable; 
        expressionChanged = true;
    }
    
    public void SetEnableCoordinateWrapping(bool enable) 
    { 
        EnableCoordinateWrapping = enable; 
        expressionChanged = true;
    }
    
    public void SetCustomExpression(string expression) 
    { 
        CustomExpression = expression ?? ""; 
        expressionChanged = true;
    }
    
    // Status queries
    public int GetEffectType() => EffectType;
    public bool IsBlendingEnabled() => EnableBlending;
    public int GetSourceMappingMode() => SourceMappingMode;
    public bool IsRectangularCoordinatesEnabled() => UseRectangularCoordinates;
    public bool IsSubpixelPrecisionEnabled() => EnableSubpixelPrecision;
    public bool IsCoordinateWrappingEnabled() => EnableCoordinateWrapping;
    public string GetCustomExpression() => CustomExpression;
    public int GetTableWidth() => tableWidth;
    public int GetTableHeight() => tableHeight;
    public bool IsTableValid() => transformationTable != null;
    
    // Built-in effect information
    public TransitionEffect[] GetBuiltInEffects() => BuiltInEffects;
    public TransitionEffect GetEffectInfo(int effectType)
    {
        if (effectType >= 0 && effectType < BuiltInEffects.Length)
        {
            return BuiltInEffects[effectType];
        }
        return null;
    }
    
    public override void Dispose()
    {
        lock (renderLock)
        {
            transformationTable = null;
        }
        scriptEngine?.Dispose();
    }
}

// Supporting classes
public class TransitionEffect
{
    public int Id { get; }
    public string ListDescription { get; }
    public string EvalDescription { get; }
    public bool UsesEval { get; }
    public bool UsesRect { get; }
    
    public TransitionEffect(int id, string listDesc, string evalDesc, bool usesEval, bool usesRect)
    {
        Id = id;
        ListDescription = listDesc;
        EvalDescription = evalDesc;
        UsesEval = usesEval;
        UsesRect = usesRect;
    }
}
```

## Integration Points

### Audio Data Integration
- **Beat Detection**: Responds to audio events for dynamic effect activation
- **Audio Analysis**: Processes audio data for enhanced visual effects
- **Dynamic Parameters**: Adjusts transformation behavior based on audio events
- **Reactive Transitions**: Beat-reactive coordinate transformations

### Framebuffer Handling
- **Input/Output Buffers**: Processes `ImageBuffer` objects with full color support
- **Transformation Tables**: Pre-calculated coordinate mapping for performance
- **Subpixel Precision**: High-resolution coordinate interpolation
- **Performance Optimization**: Efficient table-based transformations

### Performance Considerations
- **Table Caching**: Pre-calculated transformation values for speed
- **Subpixel Rendering**: High-precision coordinate mapping
- **Efficient Loops**: Optimized pixel processing algorithms
- **Memory Management**: Intelligent table allocation and reuse

## Usage Examples

### Basic Built-in Effect
```csharp
var transNode = new TransitionsNode
{
    EffectType = 3,                    // Big Swirl Out
    EnableBlending = false,
    EnableSubpixelPrecision = true,
    EnableCoordinateWrapping = false
};
```

### Custom Scripting Effect
```csharp
var transNode = new TransitionsNode
{
    EffectType = 32767,               // Custom effect
    CustomExpression = "r = r + 0.1; d = d * 0.95;",
    UseRectangularCoordinates = false,
    EnableSubpixelPrecision = true,
    EnableCoordinateWrapping = true
};
```

### Advanced Transformation
```csharp
var transNode = new TransitionsNode
{
    EffectType = 20,                  // Gridley effect
    EnableBlending = true,
    SourceMappingMode = 2,            // Beat reactive
    UseRectangularCoordinates = true,
    EnableSubpixelPrecision = true
};
```

## Technical Notes

### Transformation Architecture
The effect implements sophisticated coordinate transformation:
- **24 Built-in Effects**: Pre-defined transformation algorithms
- **Custom Scripting**: EEL-based custom transformation support
- **Dual Coordinate Systems**: Radial and rectangular modes
- **Subpixel Precision**: 32-level coordinate interpolation

### Performance Architecture
Advanced optimization techniques:
- **Table Caching**: Pre-calculated transformation values
- **Subpixel Rendering**: High-resolution coordinate mapping
- **Efficient Algorithms**: Optimized transformation calculations
- **Memory Management**: Intelligent table allocation

### Scripting System
Advanced EEL integration:
- **Real-time Compilation**: Dynamic script compilation
- **Variable Binding**: r, d, x, y, sw, sh variables
- **Coordinate Modes**: Support for both coordinate systems
- **Error Handling**: Graceful fallback on script errors

This effect provides the foundation for complex coordinate transformations, motion effects, and geometric warping, making it essential for sophisticated AVS preset creation.
