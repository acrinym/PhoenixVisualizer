# Multiplier Effects (Trans / Multiplier)

## Overview

The **Multiplier Effects** system is a high-performance color multiplication engine that provides ultra-fast color scaling with optimized processing algorithms. It implements comprehensive color multiplication with configurable scaling factors, MMX-optimized operations, and intelligent color manipulation for creating rapid color scaling transformations. This effect provides the foundation for high-performance color multiplication, real-time color scaling, and advanced image processing in AVS presets.

## Source Analysis

### Core Architecture (`r_multiplier.cpp`)

The effect is implemented as a C++ class `C_MultiplierClass` that inherits from `C_RBASE`. It provides a high-performance color multiplication system with configurable scaling factors, MMX-optimized operations, and intelligent color manipulation for creating rapid color scaling transformations.

### Key Components

#### Color Multiplication Processing Engine
High-performance multiplication control system:
- **Scaling Factors**: Configurable scaling factors with precise control
- **MMX Optimization**: MMX-optimized processing for ultra-fast operations
- **Bitwise Operations**: Efficient bitwise color manipulation
- **Performance Optimization**: Ultra-fast processing for real-time operations

#### Scaling Factor System
Sophisticated scaling processing:
- **Multiplication Modes**: x8, x4, x2, x0.5, x0.25, x0.125 scaling
- **Inversion Modes**: Color inversion and saturation control
- **Mode Selection**: Dynamic mode switching and control
- **Precision Control**: High-precision scaling operations

#### MMX Processing System
Advanced processing capabilities:
- **MMX Instructions**: MMX-optimized color multiplication algorithms
- **Batch Processing**: 8-pixel batch processing for performance
- **Memory Alignment**: Optimized memory access patterns
- **Efficient Loops**: Optimized loop structures for speed

#### Visual Enhancement System
Advanced enhancement capabilities:
- **Color Scaling**: High-quality color scaling algorithms
- **Color Processing**: Advanced color processing and manipulation
- **Visual Integration**: Seamless integration with existing visual content
- **Quality Control**: High-quality enhancement and processing

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `enabled` | bool | true/false | true | Enable/disable multiplier effect |
| `multiplierMode` | int | 0-7 | 3 | Multiplication mode (see modes below) |

### Multiplication Modes

| Mode | Value | Description | Behavior |
|------|-------|-------------|----------|
| **Invert** | 0 | Color inversion | Non-zero colors become white, zero becomes black |
| **x8** | 1 | Multiply by 8 | Colors are multiplied by 8 (with saturation) |
| **x4** | 2 | Multiply by 4 | Colors are multiplied by 4 (with saturation) |
| **x2** | 3 | Multiply by 2 | Colors are multiplied by 2 (with saturation) |
| **x0.5** | 4 | Multiply by 0.5 | Colors are divided by 2 |
| **x0.25** | 5 | Multiply by 0.25 | Colors are divided by 4 |
| **x0.125** | 6 | Multiply by 0.125 | Colors are divided by 8 |
| **Saturation** | 7 | Saturation control | White becomes black, non-white becomes white |

### Color Scaling Behavior

| Mode | Input | Output | Description |
|------|-------|--------|-------------|
| **x8** | 0x20 | 0xFF | Colors are multiplied by 8 with saturation |
| **x4** | 0x40 | 0xFF | Colors are multiplied by 4 with saturation |
| **x2** | 0x80 | 0xFF | Colors are multiplied by 2 with saturation |
| **x0.5** | 0xFF | 0x7F | Colors are divided by 2 |
| **x0.25** | 0xFF | 0x3F | Colors are divided by 4 |
| **x0.125** | 0xFF | 0x1F | Colors are divided by 8 |

## C# Implementation

```csharp
public class MultiplierEffectsNode : AvsModuleNode
{
    public bool Enabled { get; set; } = true;
    public int MultiplierMode { get; set; } = 3;
    
    // Internal state
    private int lastWidth, lastHeight;
    private int lastMultiplierMode;
    private readonly object renderLock = new object();
    
    // Performance optimization
    private const int MaxMultiplierMode = 7;
    private const int MinMultiplierMode = 0;
    private const int InvertMode = 0;
    private const int X8Mode = 1;
    private const int X4Mode = 2;
    private const int X2Mode = 3;
    private const int X05Mode = 4;
    private const int X025Mode = 5;
    private const int X0125Mode = 6;
    private const int SaturationMode = 7;
    
    public MultiplierEffectsNode()
    {
        lastWidth = lastHeight = 0;
        lastMultiplierMode = MultiplierMode;
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled || ctx.Width <= 0 || ctx.Height <= 0) 
        {
            // Pass through if disabled
            if (input != output)
            {
                input.CopyTo(output);
            }
            return;
        }
        
        lock (renderLock)
        {
            // Update buffers if dimensions changed
            UpdateBuffers(ctx);
            
            // Update mode if changed
            if (lastMultiplierMode != MultiplierMode)
            {
                lastMultiplierMode = MultiplierMode;
            }
            
            // Apply multiplier effect
            ApplyMultiplierEffect(ctx, input, output);
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
    
    private void ApplyMultiplierEffect(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        switch (MultiplierMode)
        {
            case InvertMode:
                ApplyInvertMode(ctx, input, output);
                break;
            case X8Mode:
                ApplyX8Mode(ctx, input, output);
                break;
            case X4Mode:
                ApplyX4Mode(ctx, input, output);
                break;
            case X2Mode:
                ApplyX2Mode(ctx, input, output);
                break;
            case X05Mode:
                ApplyX05Mode(ctx, input, output);
                break;
            case X025Mode:
                ApplyX025Mode(ctx, input, output);
                break;
            case X0125Mode:
                ApplyX0125Mode(ctx, input, output);
                break;
            case SaturationMode:
                ApplySaturationMode(ctx, input, output);
                break;
            default:
                ApplyX2Mode(ctx, input, output);
                break;
        }
    }
    
    private void ApplyInvertMode(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        // Invert mode: non-zero colors become white, zero becomes black
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                Color pixel = input.GetPixel(x, y);
                Color processedPixel = ProcessInvertPixel(pixel);
                output.SetPixel(x, y, processedPixel);
            }
        }
    }
    
    private void ApplyX8Mode(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        // x8 mode: multiply colors by 8 with saturation
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                Color pixel = input.GetPixel(x, y);
                Color processedPixel = ProcessX8Pixel(pixel);
                output.SetPixel(x, y, processedPixel);
            }
        }
    }
    
    private void ApplyX4Mode(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        // x4 mode: multiply colors by 4 with saturation
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                Color pixel = input.GetPixel(x, y);
                Color processedPixel = ProcessX4Pixel(pixel);
                output.SetPixel(x, y, processedPixel);
            }
        }
    }
    
    private void ApplyX2Mode(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        // x2 mode: multiply colors by 2 with saturation
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                Color pixel = input.GetPixel(x, y);
                Color processedPixel = ProcessX2Pixel(pixel);
                output.SetPixel(x, y, processedPixel);
            }
        }
    }
    
    private void ApplyX05Mode(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        // x0.5 mode: divide colors by 2
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                Color pixel = input.GetPixel(x, y);
                Color processedPixel = ProcessX05Pixel(pixel);
                output.SetPixel(x, y, processedPixel);
            }
        }
    }
    
    private void ApplyX025Mode(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        // x0.25 mode: divide colors by 4
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                Color pixel = input.GetPixel(x, y);
                Color processedPixel = ProcessX025Pixel(pixel);
                output.SetPixel(x, y, processedPixel);
            }
        }
    }
    
    private void ApplyX0125Mode(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        // x0.125 mode: divide colors by 8
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                Color pixel = input.GetPixel(x, y);
                Color processedPixel = ProcessX0125Pixel(pixel);
                output.SetPixel(x, y, processedPixel);
            }
        }
    }
    
    private void ApplySaturationMode(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        // Saturation mode: white becomes black, non-white becomes white
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                Color pixel = input.GetPixel(x, y);
                Color processedPixel = ProcessSaturationPixel(pixel);
                output.SetPixel(x, y, processedPixel);
            }
        }
    }
    
    private Color ProcessInvertPixel(Color pixel)
    {
        // Invert mode: non-zero colors become white, zero becomes black
        if (pixel.R == 0 && pixel.G == 0 && pixel.B == 0)
        {
            return Color.Black;
        }
        else
        {
            return Color.White;
        }
    }
    
    private Color ProcessX8Pixel(Color pixel)
    {
        // x8 mode: multiply by 8 with saturation
        int r = Math.Min(255, pixel.R * 8);
        int g = Math.Min(255, pixel.G * 8);
        int b = Math.Min(255, pixel.B * 8);
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private Color ProcessX4Pixel(Color pixel)
    {
        // x4 mode: multiply by 4 with saturation
        int r = Math.Min(255, pixel.R * 4);
        int g = Math.Min(255, pixel.G * 4);
        int b = Math.Min(255, pixel.B * 4);
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private Color ProcessX2Pixel(Color pixel)
    {
        // x2 mode: multiply by 2 with saturation
        int r = Math.Min(255, pixel.R * 2);
        int g = Math.Min(255, pixel.G * 2);
        int b = Math.Min(255, pixel.B * 2);
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private Color ProcessX05Pixel(Color pixel)
    {
        // x0.5 mode: divide by 2
        int r = pixel.R / 2;
        int g = pixel.G / 2;
        int b = pixel.B / 2;
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private Color ProcessX025Pixel(Color pixel)
    {
        // x0.25 mode: divide by 4
        int r = pixel.R / 4;
        int g = pixel.G / 4;
        int b = pixel.B / 4;
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private Color ProcessX0125Pixel(Color pixel)
    {
        // x0.125 mode: divide by 8
        int r = pixel.R / 8;
        int g = pixel.G / 8;
        int b = pixel.B / 8;
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private Color ProcessSaturationPixel(Color pixel)
    {
        // Saturation mode: white becomes black, non-white becomes white
        if (pixel.R == 255 && pixel.G == 255 && pixel.B == 255)
        {
            return Color.Black;
        }
        else
        {
            return Color.White;
        }
    }
    
    // Optimized processing for large images
    private void ApplyMultiplierEffectOptimized(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        // Use parallel processing for large images
        if (ctx.Width * ctx.Height > 10000)
        {
            ApplyMultiplierEffectParallel(ctx, input, output);
        }
        else
        {
            ApplyMultiplierEffect(ctx, input, output);
        }
    }
    
    private void ApplyMultiplierEffectParallel(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        int processorCount = Environment.ProcessorCount;
        int chunkSize = ctx.Height / processorCount;
        
        var tasks = new Task[processorCount];
        
        for (int i = 0; i < processorCount; i++)
        {
            int startY = i * chunkSize;
            int endY = (i == processorCount - 1) ? ctx.Height : (i + 1) * chunkSize;
            
            tasks[i] = Task.Run(() => ProcessImageChunk(input, output, ctx.Width, startY, endY));
        }
        
        Task.WaitAll(tasks);
    }
    
    private void ProcessImageChunk(ImageBuffer input, ImageBuffer output, int width, int startY, int endY)
    {
        for (int y = startY; y < endY; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color inputPixel = input.GetPixel(x, y);
                Color processedPixel = ProcessPixel(inputPixel);
                output.SetPixel(x, y, processedPixel);
            }
        }
    }
    
    private Color ProcessPixel(Color pixel)
    {
        switch (MultiplierMode)
        {
            case InvertMode:
                return ProcessInvertPixel(pixel);
            case X8Mode:
                return ProcessX8Pixel(pixel);
            case X4Mode:
                return ProcessX4Pixel(pixel);
            case X2Mode:
                return ProcessX2Pixel(pixel);
            case X05Mode:
                return ProcessX05Pixel(pixel);
            case X025Mode:
                return ProcessX025Pixel(pixel);
            case X0125Mode:
                return ProcessX0125Pixel(pixel);
            case SaturationMode:
                return ProcessSaturationPixel(pixel);
            default:
                return ProcessX2Pixel(pixel);
        }
    }
    
    // Public interface for parameter adjustment
    public void SetEnabled(bool enable) { Enabled = enable; }
    
    public void SetMultiplierMode(int multiplierMode) 
    { 
        MultiplierMode = Math.Clamp(multiplierMode, MinMultiplierMode, MaxMultiplierMode); 
    }
    
    // Status queries
    public bool IsEnabled() => Enabled;
    public int GetMultiplierMode() => MultiplierMode;
    
    // Advanced multiplier control
    public void SetMultiplicationMode(int mode)
    {
        switch (mode)
        {
            case 0: SetMultiplierMode(InvertMode); break;
            case 1: SetMultiplierMode(X8Mode); break;
            case 2: SetMultiplierMode(X4Mode); break;
            case 3: SetMultiplierMode(X2Mode); break;
            case 4: SetMultiplierMode(X05Mode); break;
            case 5: SetMultiplierMode(X025Mode); break;
            case 6: SetMultiplierMode(X0125Mode); break;
            case 7: SetMultiplierMode(SaturationMode); break;
            default: SetMultiplierMode(X2Mode); break;
        }
    }
    
    public void SetScalingFactor(float factor)
    {
        if (factor >= 8.0f)
        {
            SetMultiplierMode(X8Mode);
        }
        else if (factor >= 4.0f)
        {
            SetMultiplierMode(X4Mode);
        }
        else if (factor >= 2.0f)
        {
            SetMultiplierMode(X2Mode);
        }
        else if (factor >= 0.5f)
        {
            SetMultiplierMode(X05Mode);
        }
        else if (factor >= 0.25f)
        {
            SetMultiplierMode(X025Mode);
        }
        else if (factor >= 0.125f)
        {
            SetMultiplierMode(X0125Mode);
        }
        else
        {
            SetMultiplierMode(X0125Mode);
        }
    }
    
    // Multiplier effect presets
    public void SetInvertMode()
    {
        SetMultiplierMode(InvertMode);
    }
    
    public void SetX8Mode()
    {
        SetMultiplierMode(X8Mode);
    }
    
    public void SetX4Mode()
    {
        SetMultiplierMode(X4Mode);
    }
    
    public void SetX2Mode()
    {
        SetMultiplierMode(X2Mode);
    }
    
    public void SetX05Mode()
    {
        SetMultiplierMode(X05Mode);
    }
    
    public void SetX025Mode()
    {
        SetMultiplierMode(X025Mode);
    }
    
    public void SetX0125Mode()
    {
        SetMultiplierMode(X0125Mode);
    }
    
    public void SetSaturationMode()
    {
        SetMultiplierMode(SaturationMode);
    }
    
    // Custom multiplier configurations
    public void SetCustomMultiplier(int mode)
    {
        SetMultiplierMode(mode);
    }
    
    public void SetMultiplierPreset(int preset)
    {
        switch (preset)
        {
            case 0: // Invert
                SetMultiplierMode(InvertMode);
                break;
            case 1: // High brightness
                SetMultiplierMode(X8Mode);
                break;
            case 2: // Medium brightness
                SetMultiplierMode(X4Mode);
                break;
            case 3: // Light brightness
                SetMultiplierMode(X2Mode);
                break;
            case 4: // Darken
                SetMultiplierMode(X05Mode);
                break;
            case 5: // Heavy darken
                SetMultiplierMode(X025Mode);
                break;
            case 6: // Extreme darken
                SetMultiplierMode(X0125Mode);
                break;
            case 7: // Saturation
                SetMultiplierMode(SaturationMode);
                break;
            default:
                SetMultiplierMode(X2Mode);
                break;
        }
    }
    
    // Performance optimization
    public void SetRenderQuality(int quality)
    {
        // Quality could affect processing detail or optimization level
        // For now, we maintain full quality
    }
    
    public void EnableOptimizations(bool enable)
    {
        // Various optimization flags could be implemented here
    }
    
    public void SetProcessingMode(int mode)
    {
        // Mode could control processing method (standard vs parallel)
        // For now, we maintain automatic mode selection
    }
    
    // Advanced multiplier features
    public void SetBeatReactiveMultiplier(bool enable)
    {
        // Beat reactivity could be implemented here
        // For now, we maintain standard behavior
    }
    
    public void SetTemporalMultiplier(bool enable)
    {
        // Temporal multiplier effects could be implemented here
        // For now, we maintain standard behavior
    }
    
    public void SetSpatialMultiplier(bool enable)
    {
        // Spatial multiplier effects could be implemented here
        // For now, we maintain standard behavior
    }
    
    // Channel-specific control
    public void SetRedMultiplier(int mode)
    {
        // This could implement per-channel multiplier control
        // For now, we maintain full RGB processing
    }
    
    public void SetGreenMultiplier(int mode)
    {
        // This could implement per-channel multiplier control
        // For now, we maintain full RGB processing
    }
    
    public void SetBlueMultiplier(int mode)
    {
        // This could implement per-channel multiplier control
        // For now, we maintain full RGB processing
    }
    
    // Multiplier analysis
    public float GetEffectiveMultiplier()
    {
        switch (MultiplierMode)
        {
            case X8Mode: return 8.0f;
            case X4Mode: return 4.0f;
            case X2Mode: return 2.0f;
            case X05Mode: return 0.5f;
            case X025Mode: return 0.25f;
            case X0125Mode: return 0.125f;
            case InvertMode:
            case SaturationMode:
            default: return 1.0f;
        }
    }
    
    public string GetMultiplierDescription()
    {
        switch (MultiplierMode)
        {
            case InvertMode: return "Invert";
            case X8Mode: return "x8";
            case X4Mode: return "x4";
            case X2Mode: return "x2";
            case X05Mode: return "x0.5";
            case X025Mode: return "x0.25";
            case X0125Mode: return "x0.125";
            case SaturationMode: return "Saturation";
            default: return "Unknown";
        }
    }
    
    // Multiplier presets for common scenarios
    public void SetPhotographicMultiplier()
    {
        // Optimized for photographic images
        SetMultiplierMode(X2Mode);
    }
    
    public void SetArtisticMultiplier()
    {
        // Optimized for artistic effects
        SetMultiplierMode(X8Mode);
    }
    
    public void SetTechnicalMultiplier()
    {
        // Optimized for technical visualization
        SetMultiplierMode(X05Mode);
    }
    
    public void SetCinematicMultiplier()
    {
        // Optimized for cinematic effects
        SetMultiplierMode(X4Mode);
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

### Multiplier Processing Integration
- **Color Scaling**: Intelligent color scaling and multiplication processing
- **Mode Control**: Advanced mode control and selection
- **Performance Optimization**: Ultra-fast color multiplication algorithms
- **Quality Control**: High-quality color scaling and processing

### Color Processing Integration
- **RGB Processing**: Independent processing of RGB color channels
- **Color Mapping**: Advanced color mapping and transformation
- **Color Enhancement**: Intelligent color enhancement and processing
- **Visual Quality**: High-quality color transformation and processing

### Image Processing Integration
- **Color Scaling**: Advanced color scaling and manipulation
- **Color Filtering**: Intelligent color filtering and processing
- **Visual Enhancement**: Multiple enhancement modes for visual integration
- **Performance Optimization**: Optimized operations for multiplier processing

## Usage Examples

### Basic Multiplier Effect
```csharp
var multiplierNode = new MultiplierEffectsNode
{
    Enabled = true,                        // Enable effect
    MultiplierMode = 3                     // x2 mode
};
```

### High Brightness Effect
```csharp
var multiplierNode = new MultiplierEffectsNode
{
    Enabled = true,
    MultiplierMode = 1                     // x8 mode
};

// Apply high brightness preset
multiplierNode.SetX8Mode();
```

### Darkening Effect
```csharp
var multiplierNode = new MultiplierEffectsNode
{
    Enabled = true,
    MultiplierMode = 4                     // x0.5 mode
};

// Apply darkening preset
multiplierNode.SetX05Mode();
```

### Advanced Multiplier Control
```csharp
var multiplierNode = new MultiplierEffectsNode
{
    Enabled = true,
    MultiplierMode = 2                     // x4 mode
};

// Apply various presets
multiplierNode.SetX8Mode();                // High brightness
multiplierNode.SetX025Mode();              // Heavy darkening
multiplierNode.SetCustomMultiplier(6);     // x0.125 mode
```

## Technical Notes

### Multiplier Architecture
The effect implements sophisticated multiplier processing:
- **Color Scaling**: Intelligent color scaling and multiplication processing algorithms
- **Mode Control**: Advanced mode control and selection
- **Performance Optimization**: Ultra-fast color multiplication and manipulation
- **Quality Optimization**: High-quality color scaling and processing

### Color Architecture
Advanced color processing system:
- **RGB Processing**: Independent RGB channel processing and manipulation
- **Color Mapping**: Advanced color mapping and transformation
- **Mode Management**: Intelligent mode management and optimization
- **Performance Optimization**: Optimized color processing operations

### Integration System
Sophisticated system integration:
- **Multiplier Processing**: Deep integration with multiplier enhancement system
- **Color Management**: Seamless integration with color management system
- **Effect Management**: Advanced effect management and optimization
- **Performance Optimization**: Optimized operations for multiplier processing

This effect provides the foundation for high-performance color multiplication, creating ultra-fast color scaling transformations with configurable scaling factors, MMX-optimized operations, and intelligent color manipulation for sophisticated AVS visualization systems.
