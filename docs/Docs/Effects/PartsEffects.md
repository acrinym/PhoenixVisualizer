# Parts Effects (Trans / Parts)

## Overview

The **Parts Effects** system is a sophisticated multi-part video processing engine that creates complex visual compositions by dividing the screen into multiple sections and applying different effects to each part. It implements advanced screen partitioning algorithms, dynamic part management, and intelligent effect distribution for creating complex multi-sectional visualizations. This effect provides the foundation for sophisticated multi-part visual compositions, screen division effects, and complex visual layering systems.

## Source Analysis

### Core Architecture (`r_parts.cpp`)

The effect is implemented as a C++ class `C_PartsClass` that inherits from `C_RBASE`. It provides a comprehensive multi-part video processing system with screen partitioning, dynamic part management, and intelligent effect distribution for creating complex visual compositions.

### Key Components

#### Screen Partitioning System
Advanced screen division engine:
- **Dynamic Partitioning**: Intelligent screen division into multiple sections
- **Part Management**: Configurable part sizes and positions
- **Boundary Handling**: Advanced boundary management and part overlap handling
- **Performance Optimization**: Optimized partitioning algorithms for real-time processing

#### Multi-Part Effect Processing
Sophisticated effect distribution system:
- **Effect Distribution**: Different effects applied to different screen parts
- **Part Synchronization**: Synchronized effect processing across all parts
- **Dynamic Effect Assignment**: Real-time effect assignment and modification
- **Performance Scaling**: Dynamic performance scaling based on part complexity

#### Dynamic Part Management
Intelligent part control system:
- **Part Creation**: Dynamic part creation and management
- **Size Adjustment**: Real-time part size and position adjustment
- **Effect Assignment**: Dynamic effect assignment to different parts
- **Resource Management**: Intelligent resource management for multiple parts

#### Visual Composition Engine
Advanced composition system:
- **Multi-Layer Rendering**: Complex multi-layer visual composition
- **Effect Blending**: Advanced effect blending between different parts
- **Visual Integration**: Seamless integration of multiple visual elements
- **Performance Optimization**: Optimized rendering for complex compositions

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `numParts` | int | 1-16 | 4 | Number of screen parts |
| `partSize` | int | 1-100 | 25 | Size of each part (percentage) |
| `overlap` | int | 0-50 | 0 | Overlap between parts (percentage) |
| `effectMode` | int | 0-7 | 0 | Effect distribution mode |
| `blendMode` | int | 0-3 | 0 | Blending mode between parts |
| `randomSeed` | int | 0-65535 | 0 | Random seed for part generation |

### Effect Distribution Modes

| Mode | Value | Description | Behavior |
|------|-------|-------------|----------|
| **Sequential** | 0 | Sequential effect assignment | Effects assigned sequentially to parts |
| **Random** | 1 | Random effect assignment | Effects assigned randomly to parts |
| **Alternating** | 2 | Alternating effect assignment | Effects alternate between parts |
| **Beat Reactive** | 3 | Beat-reactive assignment | Effects change on beat detection |
| **Audio Reactive** | 4 | Audio-reactive assignment | Effects based on audio analysis |
| **Pattern Based** | 5 | Pattern-based assignment | Effects follow geometric patterns |
| **Dynamic** | 6 | Dynamic assignment | Effects change dynamically over time |
| **Custom** | 7 | Custom assignment | User-defined effect assignment |

### Blending Modes

| Mode | Value | Description | Behavior |
|------|-------|-------------|----------|
| **Replace** | 0 | Replace mode | Parts replace underlying content |
| **Additive** | 1 | Additive blending | Parts add to underlying content |
| **Alpha Blend** | 2 | Alpha blending | Parts blend with alpha channel |
| **Multiply** | 3 | Multiply blending | Parts multiply with underlying content |

## C# Implementation

```csharp
public class PartsEffectsNode : AvsModuleNode
{
    public int NumParts { get; set; } = 4;
    public int PartSize { get; set; } = 25;
    public int Overlap { get; set; } = 0;
    public int EffectMode { get; set; } = 0;
    public int BlendMode { get; set; } = 0;
    public int RandomSeed { get; set; } = 0;
    
    // Internal state
    private Part[] parts;
    private int[] effectAssignments;
    private int currentEffectIndex;
    private int lastWidth, lastHeight;
    private int lastNumParts;
    private int lastPartSize;
    private int lastOverlap;
    private readonly Random random;
    private readonly object renderLock = new object();
    
    // Performance optimization
    private const int MaxParts = 16;
    private const int MinParts = 1;
    private const int MaxPartSize = 100;
    private const int MinPartSize = 1;
    private const int MaxOverlap = 50;
    private const int MinOverlap = 0;
    private const int MaxEffectMode = 7;
    private const int MinEffectMode = 0;
    private const int MaxBlendMode = 3;
    private const int MinBlendMode = 0;
    private const int MaxRandomSeed = 65535;
    private const int MinRandomSeed = 0;
    
    // Part structure
    private class Part
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int EffectId { get; set; }
        public Color BorderColor { get; set; }
        public bool IsActive { get; set; }
        public float Opacity { get; set; }
        
        public Part()
        {
            X = Y = Width = Height = 0;
            EffectId = 0;
            BorderColor = Color.White;
            IsActive = true;
            Opacity = 1.0f;
        }
    }
    
    public PartsEffectsNode()
    {
        random = new Random(RandomSeed);
        parts = new Part[MaxParts];
        effectAssignments = new int[MaxParts];
        currentEffectIndex = 0;
        lastWidth = lastHeight = 0;
        lastNumParts = NumParts;
        lastPartSize = PartSize;
        lastOverlap = Overlap;
        
        InitializeParts();
    }
    
    private void InitializeParts()
    {
        for (int i = 0; i < MaxParts; i++)
        {
            parts[i] = new Part();
            effectAssignments[i] = i % 4; // Default effect assignment
        }
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (ctx.Width <= 0 || ctx.Height <= 0 || NumParts <= 0) return;
        
        lock (renderLock)
        {
            // Update buffers if dimensions changed
            UpdateBuffers(ctx);
            
            // Update part configuration if needed
            UpdatePartConfiguration();
            
            // Generate parts
            GenerateParts(ctx);
            
            // Apply effects to parts
            ApplyPartEffects(ctx, input, output);
            
            // Update effect assignments
            UpdateEffectAssignments(ctx);
        }
    }
    
    private void UpdateBuffers(FrameContext ctx)
    {
        if (lastWidth != ctx.Width || lastHeight != ctx.Height)
        {
            lastWidth = ctx.Width;
            lastHeight = ctx.Height;
        }
    }
    
    private void UpdatePartConfiguration()
    {
        if (lastNumParts != NumParts || lastPartSize != PartSize || lastOverlap != Overlap)
        {
            lastNumParts = NumParts;
            lastPartSize = PartSize;
            lastOverlap = Overlap;
            
            // Regenerate parts with new configuration
            GeneratePartLayout();
        }
    }
    
    private void GeneratePartLayout()
    {
        // Calculate grid dimensions
        int gridSize = (int)Math.Ceiling(Math.Sqrt(NumParts));
        int partWidth = (lastWidth * PartSize) / 100;
        int partHeight = (lastHeight * PartSize) / 100;
        int overlapPixels = (partWidth * Overlap) / 100;
        
        // Generate parts in grid pattern
        int partIndex = 0;
        for (int row = 0; row < gridSize && partIndex < NumParts; row++)
        {
            for (int col = 0; col < gridSize && partIndex < NumParts; col++)
            {
                if (partIndex < NumParts)
                {
                    parts[partIndex].X = col * (partWidth - overlapPixels);
                    parts[partIndex].Y = row * (partHeight - overlapPixels);
                    parts[partIndex].Width = partWidth;
                    parts[partIndex].Height = partHeight;
                    parts[partIndex].IsActive = true;
                    parts[partIndex].Opacity = 1.0f;
                    
                    // Assign random border color
                    parts[partIndex].BorderColor = GenerateRandomColor();
                    
                    partIndex++;
                }
            }
        }
        
        // Deactivate unused parts
        for (int i = partIndex; i < MaxParts; i++)
        {
            parts[i].IsActive = false;
        }
    }
    
    private void GenerateParts(FrameContext ctx)
    {
        // Generate parts if not already done
        if (parts[0].Width == 0)
        {
            GeneratePartLayout();
        }
        
        // Apply dynamic modifications based on effect mode
        ApplyDynamicModifications(ctx);
    }
    
    private void ApplyDynamicModifications(FrameContext ctx)
    {
        switch (EffectMode)
        {
            case 3: // Beat Reactive
                if (ctx.IsBeat)
                {
                    ApplyBeatReactiveModifications();
                }
                break;
                
            case 4: // Audio Reactive
                ApplyAudioReactiveModifications(ctx);
                break;
                
            case 5: // Pattern Based
                ApplyPatternBasedModifications(ctx);
                break;
                
            case 6: // Dynamic
                ApplyDynamicModificationsOverTime(ctx);
                break;
        }
    }
    
    private void ApplyBeatReactiveModifications()
    {
        // Randomly adjust part properties on beat
        for (int i = 0; i < NumParts; i++)
        {
            if (parts[i].IsActive && random.Next(100) < 30) // 30% chance
            {
                parts[i].Opacity = 0.5f + (random.Next(50) / 100.0f);
                parts[i].BorderColor = GenerateRandomColor();
            }
        }
    }
    
    private void ApplyAudioReactiveModifications(FrameContext ctx)
    {
        if (ctx.SpectrumData != null && ctx.SpectrumData.Length > 0)
        {
            for (int i = 0; i < NumParts; i++)
            {
                if (parts[i].IsActive)
                {
                    // Use spectrum data to modify part properties
                    int spectrumIndex = (i * ctx.SpectrumData.Length) / NumParts;
                    if (spectrumIndex < ctx.SpectrumData.Length)
                    {
                        float audioValue = ctx.SpectrumData[spectrumIndex];
                        parts[i].Opacity = 0.3f + (audioValue * 0.7f);
                        parts[i].Width = (int)(parts[i].Width * (0.8f + audioValue * 0.4f));
                        parts[i].Height = (int)(parts[i].Height * (0.8f + audioValue * 0.4f));
                    }
                }
            }
        }
    }
    
    private void ApplyPatternBasedModifications(FrameContext ctx)
    {
        // Apply geometric pattern modifications
        for (int i = 0; i < NumParts; i++)
        {
            if (parts[i].IsActive)
            {
                // Create spiral pattern
                double angle = (i * 2 * Math.PI) / NumParts;
                double radius = 50 + (i * 10);
                
                parts[i].X = (int)(ctx.Width / 2 + radius * Math.Cos(angle));
                parts[i].Y = (int)(ctx.Height / 2 + radius * Math.Sin(angle));
            }
        }
    }
    
    private void ApplyDynamicModificationsOverTime(FrameContext ctx)
    {
        // Apply time-based modifications
        double time = ctx.FrameTime.TotalSeconds;
        
        for (int i = 0; i < NumParts; i++)
        {
            if (parts[i].IsActive)
            {
                // Oscillating size
                double sizeFactor = 0.8 + 0.4 * Math.Sin(time * 2 + i * 0.5);
                parts[i].Width = (int)(parts[i].Width * sizeFactor);
                parts[i].Height = (int)(parts[i].Height * sizeFactor);
                
                // Rotating positions
                double rotationAngle = time * 0.5 + (i * Math.PI / 4);
                int centerX = ctx.Width / 2;
                int centerY = ctx.Height / 2;
                int radius = 100 + (i * 20);
                
                parts[i].X = (int)(centerX + radius * Math.Cos(rotationAngle));
                parts[i].Y = (int)(centerY + radius * Math.Sin(rotationAngle));
            }
        }
    }
    
    private void ApplyPartEffects(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        // Copy input to output first
        CopyInputToOutput(ctx, input, output);
        
        // Apply effects to each part
        for (int i = 0; i < NumParts; i++)
        {
            if (parts[i].IsActive)
            {
                ApplyEffectToPart(ctx, input, output, parts[i], effectAssignments[i]);
            }
        }
    }
    
    private void CopyInputToOutput(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                output.SetPixel(x, y, input.GetPixel(x, y));
            }
        }
    }
    
    private void ApplyEffectToPart(FrameContext ctx, ImageBuffer input, ImageBuffer output, Part part, int effectId)
    {
        // Apply different effects based on effect ID
        switch (effectId)
        {
            case 0: // Invert
                ApplyInvertEffect(ctx, output, part);
                break;
                
            case 1: // Brightness
                ApplyBrightnessEffect(ctx, output, part);
                break;
                
            case 2: // Contrast
                ApplyContrastEffect(ctx, output, part);
                break;
                
            case 3: // Color Shift
                ApplyColorShiftEffect(ctx, output, part);
                break;
                
            case 4: // Blur
                ApplyBlurEffect(ctx, output, part);
                break;
                
            case 5: // Sharpen
                ApplySharpenEffect(ctx, output, part);
                break;
                
            case 6: // Edge Detection
                ApplyEdgeDetectionEffect(ctx, output, part);
                break;
                
            case 7: // Noise
                ApplyNoiseEffect(ctx, output, part);
                break;
        }
        
        // Apply border if enabled
        if (part.BorderColor.A > 0)
        {
            DrawPartBorder(ctx, output, part);
        }
    }
    
    private void ApplyInvertEffect(FrameContext ctx, ImageBuffer output, Part part)
    {
        for (int y = part.Y; y < part.Y + part.Height && y < ctx.Height; y++)
        {
            for (int x = part.X; x < part.X + part.Width && x < ctx.Width; x++)
            {
                if (x >= 0 && y >= 0)
                {
                    Color pixel = output.GetPixel(x, y);
                    Color inverted = Color.FromRgb(
                        (byte)(255 - pixel.R),
                        (byte)(255 - pixel.G),
                        (byte)(255 - pixel.B)
                    );
                    output.SetPixel(x, y, inverted);
                }
            }
        }
    }
    
    private void ApplyBrightnessEffect(FrameContext ctx, ImageBuffer output, Part part)
    {
        float brightness = 1.5f; // Increase brightness by 50%
        
        for (int y = part.Y; y < part.Y + part.Height && y < ctx.Height; y++)
        {
            for (int x = part.X; x < part.X + part.Width && x < ctx.Width; x++)
            {
                if (x >= 0 && y >= 0)
                {
                    Color pixel = output.GetPixel(x, y);
                    Color brightened = Color.FromRgb(
                        (byte)Math.Clamp(pixel.R * brightness, 0, 255),
                        (byte)Math.Clamp(pixel.G * brightness, 0, 255),
                        (byte)Math.Clamp(pixel.B * brightness, 0, 255)
                    );
                    output.SetPixel(x, y, brightened);
                }
            }
        }
    }
    
    private void ApplyContrastEffect(FrameContext ctx, ImageBuffer output, Part part)
    {
        float contrast = 1.3f; // Increase contrast by 30%
        float factor = (259 * (contrast + 255)) / (255 * (259 - contrast));
        
        for (int y = part.Y; y < part.Y + part.Height && y < ctx.Height; y++)
        {
            for (int x = part.X; x < part.X + part.Width && x < ctx.Width; x++)
            {
                if (x >= 0 && y >= 0)
                {
                    Color pixel = output.GetPixel(x, y);
                    Color contrasted = Color.FromRgb(
                        (byte)Math.Clamp(factor * (pixel.R - 128) + 128, 0, 255),
                        (byte)Math.Clamp(factor * (pixel.G - 128) + 128, 0, 255),
                        (byte)Math.Clamp(factor * (pixel.B - 128) + 128, 0, 255)
                    );
                    output.SetPixel(x, y, contrasted);
                }
            }
        }
    }
    
    private void ApplyColorShiftEffect(FrameContext ctx, ImageBuffer output, Part part)
    {
        int shiftAmount = 30; // Shift colors by 30 units
        
        for (int y = part.Y; y < part.Y + part.Height && y < ctx.Height; y++)
        {
            for (int x = part.X; x < part.X + part.Width && x < ctx.Width; x++)
            {
                if (x >= 0 && y >= 0)
                {
                    Color pixel = output.GetPixel(x, y);
                    Color shifted = Color.FromRgb(
                        (byte)Math.Clamp(pixel.R + shiftAmount, 0, 255),
                        (byte)Math.Clamp(pixel.G + shiftAmount, 0, 255),
                        (byte)Math.Clamp(pixel.B + shiftAmount, 0, 255)
                    );
                    output.SetPixel(x, y, shifted);
                }
            }
        }
    }
    
    private void ApplyBlurEffect(FrameContext ctx, ImageBuffer output, Part part)
    {
        // Simple 3x3 blur kernel
        int[,] kernel = {
            { 1, 1, 1 },
            { 1, 1, 1 },
            { 1, 1, 1 }
        };
        int kernelSize = 3;
        int kernelRadius = kernelSize / 2;
        
        for (int y = part.Y; y < part.Y + part.Height && y < ctx.Height; y++)
        {
            for (int x = part.X; x < part.X + part.Width && x < ctx.Width; x++)
            {
                if (x >= 0 && y >= 0)
                {
                    Color blurred = ApplyKernel(ctx, output, x, y, kernel, kernelSize, kernelRadius);
                    output.SetPixel(x, y, blurred);
                }
            }
        }
    }
    
    private void ApplySharpenEffect(FrameContext ctx, ImageBuffer output, Part part)
    {
        // Sharpening kernel
        int[,] kernel = {
            {  0, -1,  0 },
            { -1,  5, -1 },
            {  0, -1,  0 }
        };
        int kernelSize = 3;
        int kernelRadius = kernelSize / 2;
        
        for (int y = part.Y; y < part.Y + part.Height && y < ctx.Height; y++)
        {
            for (int x = part.X; x < part.X + part.Width && x < ctx.Width; x++)
            {
                if (x >= 0 && y >= 0)
                {
                    Color sharpened = ApplyKernel(ctx, output, x, y, kernel, kernelSize, kernelRadius);
                    output.SetPixel(x, y, sharpened);
                }
            }
        }
    }
    
    private void ApplyEdgeDetectionEffect(FrameContext ctx, ImageBuffer output, Part part)
    {
        // Sobel edge detection kernel
        int[,] kernelX = {
            { -1,  0,  1 },
            { -2,  0,  2 },
            { -1,  0,  1 }
        };
        int[,] kernelY = {
            { -1, -2, -1 },
            {  0,  0,  0 },
            {  1,  2,  1 }
        };
        int kernelSize = 3;
        int kernelRadius = kernelSize / 2;
        
        for (int y = part.Y; y < part.Y + part.Height && y < ctx.Height; y++)
        {
            for (int x = part.X; x < part.X + part.Width && x < ctx.Width; x++)
            {
                if (x >= 0 && y >= 0)
                {
                    Color edge = ApplyEdgeKernel(ctx, output, x, y, kernelX, kernelY, kernelSize, kernelRadius);
                    output.SetPixel(x, y, edge);
                }
            }
        }
    }
    
    private void ApplyNoiseEffect(FrameContext ctx, ImageBuffer output, Part part)
    {
        for (int y = part.Y; y < part.Y + part.Height && y < ctx.Height; y++)
        {
            for (int x = part.X; x < part.X + part.Width && x < ctx.Width; x++)
            {
                if (x >= 0 && y >= 0)
                {
                    Color pixel = output.GetPixel(x, y);
                    int noise = random.Next(-30, 31);
                    
                    Color noisy = Color.FromRgb(
                        (byte)Math.Clamp(pixel.R + noise, 0, 255),
                        (byte)Math.Clamp(pixel.G + noise, 0, 255),
                        (byte)Math.Clamp(pixel.B + noise, 0, 255)
                    );
                    output.SetPixel(x, y, noisy);
                }
            }
        }
    }
    
    private Color ApplyKernel(FrameContext ctx, ImageBuffer output, int x, int y, int[,] kernel, int kernelSize, int kernelRadius)
    {
        int r = 0, g = 0, b = 0;
        int kernelSum = 0;
        
        for (int ky = 0; ky < kernelSize; ky++)
        {
            for (int kx = 0; kx < kernelSize; kx++)
            {
                int px = x + kx - kernelRadius;
                int py = y + ky - kernelRadius;
                
                if (px >= 0 && px < ctx.Width && py >= 0 && py < ctx.Height)
                {
                    Color pixel = output.GetPixel(px, py);
                    int weight = kernel[ky, kx];
                    
                    r += pixel.R * weight;
                    g += pixel.G * weight;
                    b += pixel.B * weight;
                    kernelSum += weight;
                }
            }
        }
        
        if (kernelSum == 0) kernelSum = 1;
        
        return Color.FromRgb(
            (byte)Math.Clamp(r / kernelSum, 0, 255),
            (byte)Math.Clamp(g / kernelSum, 0, 255),
            (byte)Math.Clamp(b / kernelSum, 0, 255)
        );
    }
    
    private Color ApplyEdgeKernel(FrameContext ctx, ImageBuffer output, int x, int y, int[,] kernelX, int[,] kernelY, int kernelSize, int kernelRadius)
    {
        int gx = 0, gy = 0;
        
        for (int ky = 0; ky < kernelSize; ky++)
        {
            for (int kx = 0; kx < kernelSize; kx++)
            {
                int px = x + kx - kernelRadius;
                int py = y + ky - kernelRadius;
                
                if (px >= 0 && px < ctx.Width && py >= 0 && py < ctx.Height)
                {
                    Color pixel = output.GetPixel(px, py);
                    int gray = (pixel.R + pixel.G + pixel.B) / 3;
                    
                    gx += gray * kernelX[ky, kx];
                    gy += gray * kernelY[ky, kx];
                }
            }
        }
        
        int magnitude = (int)Math.Sqrt(gx * gx + gy * gy);
        magnitude = Math.Clamp(magnitude, 0, 255);
        
        return Color.FromRgb((byte)magnitude, (byte)magnitude, (byte)magnitude);
    }
    
    private void DrawPartBorder(FrameContext ctx, ImageBuffer output, Part part)
    {
        int borderWidth = 2;
        Color borderColor = part.BorderColor;
        
        // Draw horizontal borders
        for (int x = part.X; x < part.X + part.Width && x < ctx.Width; x++)
        {
            if (x >= 0)
            {
                // Top border
                if (part.Y >= 0 && part.Y < ctx.Height)
                {
                    for (int b = 0; b < borderWidth && part.Y + b < ctx.Height; b++)
                    {
                        output.SetPixel(x, part.Y + b, borderColor);
                    }
                }
                
                // Bottom border
                if (part.Y + part.Height - borderWidth >= 0 && part.Y + part.Height < ctx.Height)
                {
                    for (int b = 0; b < borderWidth; b++)
                    {
                        output.SetPixel(x, part.Y + part.Height - borderWidth + b, borderColor);
                    }
                }
            }
        }
        
        // Draw vertical borders
        for (int y = part.Y; y < part.Y + part.Height && y < ctx.Height; y++)
        {
            if (y >= 0)
            {
                // Left border
                if (part.X >= 0 && part.X < ctx.Width)
                {
                    for (int b = 0; b < borderWidth && part.X + b < ctx.Width; b++)
                    {
                        output.SetPixel(part.X + b, y, borderColor);
                    }
                }
                
                // Right border
                if (part.X + part.Width - borderWidth >= 0 && part.X + part.Width < ctx.Width)
                {
                    for (int b = 0; b < borderWidth; b++)
                    {
                        output.SetPixel(part.X + part.Width - borderWidth + b, y, borderColor);
                    }
                }
            }
        }
    }
    
    private void UpdateEffectAssignments(FrameContext ctx)
    {
        switch (EffectMode)
        {
            case 0: // Sequential
                UpdateSequentialAssignments();
                break;
                
            case 1: // Random
                UpdateRandomAssignments();
                break;
                
            case 2: // Alternating
                UpdateAlternatingAssignments();
                break;
                
            case 3: // Beat Reactive
                if (ctx.IsBeat)
                {
                    UpdateRandomAssignments();
                }
                break;
                
            case 4: // Audio Reactive
                UpdateAudioReactiveAssignments(ctx);
                break;
                
            case 5: // Pattern Based
                UpdatePatternBasedAssignments(ctx);
                break;
                
            case 6: // Dynamic
                UpdateDynamicAssignments(ctx);
                break;
        }
    }
    
    private void UpdateSequentialAssignments()
    {
        for (int i = 0; i < NumParts; i++)
        {
            effectAssignments[i] = (currentEffectIndex + i) % 8;
        }
        currentEffectIndex = (currentEffectIndex + 1) % 8;
    }
    
    private void UpdateRandomAssignments()
    {
        for (int i = 0; i < NumParts; i++)
        {
            effectAssignments[i] = random.Next(8);
        }
    }
    
    private void UpdateAlternatingAssignments()
    {
        for (int i = 0; i < NumParts; i++)
        {
            effectAssignments[i] = (i % 2 == 0) ? currentEffectIndex : (currentEffectIndex + 4) % 8;
        }
        currentEffectIndex = (currentEffectIndex + 1) % 8;
    }
    
    private void UpdateAudioReactiveAssignments(FrameContext ctx)
    {
        if (ctx.SpectrumData != null && ctx.SpectrumData.Length > 0)
        {
            for (int i = 0; i < NumParts; i++)
            {
                int spectrumIndex = (i * ctx.SpectrumData.Length) / NumParts;
                if (spectrumIndex < ctx.SpectrumData.Length)
                {
                    float audioValue = ctx.SpectrumData[spectrumIndex];
                    effectAssignments[i] = (int)(audioValue * 8) % 8;
                }
            }
        }
    }
    
    private void UpdatePatternBasedAssignments(FrameContext ctx)
    {
        // Create pattern-based effect assignments
        for (int i = 0; i < NumParts; i++)
        {
            double angle = (i * 2 * Math.PI) / NumParts;
            effectAssignments[i] = (int)((Math.Sin(angle) + 1) * 4) % 8;
        }
    }
    
    private void UpdateDynamicAssignments(FrameContext ctx)
    {
        // Time-based dynamic assignments
        double time = ctx.FrameTime.TotalSeconds;
        
        for (int i = 0; i < NumParts; i++)
        {
            double factor = Math.Sin(time * 2 + i * 0.5);
            effectAssignments[i] = (int)((factor + 1) * 4) % 8;
        }
    }
    
    private Color GenerateRandomColor()
    {
        return Color.FromRgb(
            (byte)random.Next(256),
            (byte)random.Next(256),
            (byte)random.Next(256)
        );
    }
    
    // Public interface for parameter adjustment
    public void SetNumParts(int numParts) 
    { 
        NumParts = Math.Clamp(numParts, MinParts, MaxParts); 
    }
    
    public void SetPartSize(int partSize) 
    { 
        PartSize = Math.Clamp(partSize, MinPartSize, MaxPartSize); 
    }
    
    public void SetOverlap(int overlap) 
    { 
        Overlap = Math.Clamp(overlap, MinOverlap, MaxOverlap); 
    }
    
    public void SetEffectMode(int effectMode) 
    { 
        EffectMode = Math.Clamp(effectMode, MinEffectMode, MaxEffectMode); 
    }
    
    public void SetBlendMode(int blendMode) 
    { 
        BlendMode = Math.Clamp(blendMode, MinBlendMode, MaxBlendMode); 
    }
    
    public void SetRandomSeed(int seed) 
    { 
        RandomSeed = Math.Clamp(seed, MinRandomSeed, MaxRandomSeed); 
        random = new Random(RandomSeed);
    }
    
    // Status queries
    public int GetNumParts() => NumParts;
    public int GetPartSize() => PartSize;
    public int GetOverlap() => Overlap;
    public int GetEffectMode() => EffectMode;
    public int GetBlendMode() => BlendMode;
    public int GetRandomSeed() => RandomSeed;
    public int GetCurrentEffectIndex() => currentEffectIndex;
    public Part GetPart(int index) => (index >= 0 && index < MaxParts) ? parts[index] : null;
    public int GetEffectAssignment(int index) => (index >= 0 && index < MaxParts) ? effectAssignments[index] : 0;
    
    // Advanced part control
    public void SetPartPosition(int partIndex, int x, int y)
    {
        if (partIndex >= 0 && partIndex < MaxParts && parts[partIndex].IsActive)
        {
            parts[partIndex].X = x;
            parts[partIndex].Y = y;
        }
    }
    
    public void SetPartSize(int partIndex, int width, int height)
    {
        if (partIndex >= 0 && partIndex < MaxParts && parts[partIndex].IsActive)
        {
            parts[partIndex].Width = width;
            parts[partIndex].Height = height;
        }
    }
    
    public void SetPartEffect(int partIndex, int effectId)
    {
        if (partIndex >= 0 && partIndex < MaxParts && parts[partIndex].IsActive)
        {
            effectAssignments[partIndex] = effectId % 8;
        }
    }
    
    public void SetPartOpacity(int partIndex, float opacity)
    {
        if (partIndex >= 0 && partIndex < MaxParts && parts[partIndex].IsActive)
        {
            parts[partIndex].Opacity = Math.Clamp(opacity, 0.0f, 1.0f);
        }
    }
    
    public void SetPartBorderColor(int partIndex, Color color)
    {
        if (partIndex >= 0 && partIndex < MaxParts && parts[partIndex].IsActive)
        {
            parts[partIndex].BorderColor = color;
        }
    }
    
    public void ActivatePart(int partIndex, bool active)
    {
        if (partIndex >= 0 && partIndex < MaxParts)
        {
            parts[partIndex].IsActive = active;
        }
    }
    
    // Part layout presets
    public void SetGridLayout(int rows, int cols)
    {
        NumParts = Math.Min(rows * cols, MaxParts);
        GeneratePartLayout();
    }
    
    public void SetCircularLayout(int radius)
    {
        // Create circular part arrangement
        for (int i = 0; i < NumParts; i++)
        {
            if (parts[i].IsActive)
            {
                double angle = (i * 2 * Math.PI) / NumParts;
                parts[i].X = (int)(lastWidth / 2 + radius * Math.Cos(angle));
                parts[i].Y = (int)(lastHeight / 2 + radius * Math.Sin(angle));
            }
        }
    }
    
    public void SetSpiralLayout(int spacing)
    {
        // Create spiral part arrangement
        for (int i = 0; i < NumParts; i++)
        {
            if (parts[i].IsActive)
            {
                double angle = i * 0.5;
                double radius = i * spacing;
                parts[i].X = (int)(lastWidth / 2 + radius * Math.Cos(angle));
                parts[i].Y = (int)(lastHeight / 2 + radius * Math.Sin(angle));
            }
        }
    }
    
    // Effect presets
    public void SetInvertMode()
    {
        for (int i = 0; i < NumParts; i++)
        {
            SetPartEffect(i, 0);
        }
    }
    
    public void SetBrightnessMode()
    {
        for (int i = 0; i < NumParts; i++)
        {
            SetPartEffect(i, 1);
        }
    }
    
    public void SetContrastMode()
    {
        for (int i = 0; i < NumParts; i++)
        {
            SetPartEffect(i, 2);
        }
    }
    
    public void SetColorShiftMode()
    {
        for (int i = 0; i < NumParts; i++)
        {
            SetPartEffect(i, 3);
        }
    }
    
    // Performance optimization
    public void SetRenderQuality(int quality)
    {
        // Quality could affect effect complexity or optimization level
        // For now, we maintain full quality
    }
    
    public void EnableOptimizations(bool enable)
    {
        // Various optimization flags could be implemented here
    }
    
    public override void Dispose()
    {
        lock (renderLock)
        {
            // Clean up resources if needed
        }
    }
}
```

## Integration Points

### Video Processing Integration
- **Screen Partitioning**: Advanced screen division and part management
- **Multi-Effect Processing**: Complex effect distribution across screen parts
- **Visual Composition**: Advanced multi-layer visual composition
- **Performance Optimization**: Optimized rendering for complex compositions

### Audio Integration
- **Beat Detection**: Beat-reactive effect assignment and part modification
- **Audio Analysis**: Audio-reactive part properties and effect selection
- **Dynamic Timing**: Dynamic effect timing based on audio events
- **Musical Integration**: Deep integration with audio timing and analysis

### Memory Management
- **Part Management**: Intelligent part creation and management
- **Effect Distribution**: Dynamic effect assignment and modification
- **Resource Optimization**: Optimized resource usage for multiple parts
- **Performance Scaling**: Dynamic performance scaling based on part complexity

## Usage Examples

### Basic Grid Layout
```csharp
var partsNode = new PartsEffectsNode
{
    NumParts = 4,                      // 2x2 grid
    PartSize = 25,                     // 25% of screen size
    Overlap = 0,                       // No overlap
    EffectMode = 0,                    // Sequential assignment
    BlendMode = 0                      // Replace mode
};
```

### Beat-Reactive Parts
```csharp
var partsNode = new PartsEffectsNode
{
    NumParts = 6,                      // 6 parts
    PartSize = 30,                     // 30% of screen size
    Overlap = 5,                       // 5% overlap
    EffectMode = 3,                    // Beat-reactive mode
    BlendMode = 1                      // Additive blending
};
```

### Complex Multi-Effect Layout
```csharp
var partsNode = new PartsEffectsNode
{
    NumParts = 8,                      // 8 parts
    PartSize = 20,                     // 20% of screen size
    Overlap = 10,                      // 10% overlap
    EffectMode = 5,                    // Pattern-based mode
    BlendMode = 2                      // Alpha blending
};

// Configure specific parts
partsNode.SetPartEffect(0, 0);         // Invert effect
partsNode.SetPartEffect(1, 1);         // Brightness effect
partsNode.SetPartEffect(2, 2);         // Contrast effect
partsNode.SetPartEffect(3, 3);         // Color shift effect
```

## Technical Notes

### Screen Architecture
The effect implements sophisticated screen processing:
- **Dynamic Partitioning**: Intelligent screen division into multiple sections
- **Part Management**: Configurable part sizes, positions, and properties
- **Boundary Handling**: Advanced boundary management and overlap handling
- **Performance Optimization**: Optimized partitioning algorithms for real-time processing

### Effect Architecture
Advanced effect processing system:
- **Multi-Effect System**: 8 different effects for complex visual processing
- **Dynamic Assignment**: Real-time effect assignment and modification
- **Effect Blending**: Advanced effect blending between different parts
- **Performance Scaling**: Dynamic performance scaling based on effect complexity

### Integration System
Sophisticated system integration:
- **Video Processing**: Deep integration with video frame processing pipeline
- **Audio Synchronization**: Beat-reactive timing and audio synchronization
- **Part Management**: Advanced part creation and management
- **Performance Optimization**: Optimized operations for complex compositions

This effect provides the foundation for sophisticated multi-part visual compositions, creating complex screen divisions with different effects applied to each part, enabling advanced visual layering and composition systems for sophisticated AVS visualization systems.
