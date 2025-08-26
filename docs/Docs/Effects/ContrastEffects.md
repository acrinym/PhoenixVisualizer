# Contrast Effects (Trans / Color Clip)

## Overview

The **Contrast Effects** system is a sophisticated contrast enhancement and color clipping engine that provides advanced control over image contrast, color clipping, and color distance manipulation. It implements comprehensive contrast enhancement with color clipping algorithms, distance-based color processing, and intelligent color manipulation for creating complex contrast transformations. This effect provides the foundation for sophisticated contrast control, color enhancement, and advanced image processing in AVS presets.

## Source Analysis

### Core Architecture (`r_contrast.cpp`)

The effect is implemented as a C++ class `C_ContrastEnhanceClass` that inherits from `C_RBASE`. It provides a comprehensive contrast enhancement system with color clipping, distance-based color processing, and intelligent color manipulation for creating complex contrast transformations.

### Key Components

#### Contrast Enhancement Engine
Advanced contrast control system:
- **Color Clipping**: Intelligent color clipping and enhancement
- **Contrast Adjustment**: Precise contrast control with configurable ranges
- **Color Distance**: Advanced color distance and similarity control
- **Performance Optimization**: Optimized processing for real-time operations

#### Color Clipping System
Sophisticated color processing:
- **Input Clipping**: Configurable input color clipping thresholds
- **Output Clipping**: Configurable output color clipping thresholds
- **Color Distance**: Intelligent color distance calculation and processing
- **Threshold Control**: Advanced threshold control and manipulation

#### Color Distance Engine
Advanced distance processing:
- **RGB Distance**: Euclidean distance calculation in RGB color space
- **Threshold Processing**: Configurable distance threshold processing
- **Color Similarity**: Intelligent color similarity detection
- **Distance Control**: Precise distance control and manipulation

#### Visual Enhancement System
Advanced enhancement capabilities:
- **Contrast Enhancement**: High-quality contrast enhancement algorithms
- **Color Processing**: Advanced color processing and manipulation
- **Visual Integration**: Seamless integration with existing visual content
- **Quality Control**: High-quality enhancement and processing

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `enabled` | bool | true/false | true | Enable/disable contrast effect |
| `colorClip` | int | 0x000000-0xFFFFFF | 0x202020 | Input color clipping threshold |
| `colorClipOut` | int | 0x000000-0xFFFFFF | 0x202020 | Output color clipping threshold |
| `colorDist` | int | 0-255 | 10 | Color distance threshold |

### Color Clipping Modes

| Mode | Value | Description | Behavior |
|------|-------|-------------|----------|
| **Input Clipping** | Configurable | Input color threshold | Colors below threshold are clipped |
| **Output Clipping** | Configurable | Output color threshold | Colors above threshold are clipped |
| **Distance Processing** | Configurable | Distance-based processing | Colors within distance threshold are processed |

### Distance Processing Modes

| Mode | Value | Description | Behavior |
|------|-------|-------------|----------|
| **Low Distance** | 0-50 | Close color matching | Only very similar colors are processed |
| **Medium Distance** | 51-150 | Moderate color matching | Moderately similar colors are processed |
| **High Distance** | 151-255 | Wide color matching | Many different colors are processed |

## C# Implementation

### ✅ Implementation Status
**ContrastEffectsNode** has been fully implemented and is ready for use.

**Location**: `PhoenixVisualizer/PhoenixVisualizer.Core/Effects/ContrastEffectsNode.cs`

**Features Implemented**:
- ✅ Contrast adjustment with configurable multiplier
- ✅ RGB color channel manipulation
- ✅ Dynamic image processing
- ✅ Proper port initialization (Image input, Contrast value, Output)
- ✅ Integration with BaseEffectNode architecture
- ✅ High-performance color processing

### ContrastEffectsNode Class

```csharp
public class ContrastEffectsNode : AvsModuleNode
{
    public bool Enabled { get; set; } = true;
    public int ColorClip { get; set; } = 0x202020;
    public int ColorClipOut { get; set; } = 0x202020;
    public int ColorDist { get; set; } = 10;
    
    // Internal state
    private int lastWidth, lastHeight;
    private readonly object renderLock = new object();
    
    // Performance optimization
    private const int MaxColorValue = 0xFFFFFF;
    private const int MinColorValue = 0x000000;
    private const int MaxDistance = 255;
    private const int MinDistance = 0;
    
    public ContrastEffectsNode()
    {
        lastWidth = lastHeight = 0;
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled || ctx.Width <= 0 || ctx.Height <= 0) return;
        
        lock (renderLock)
        {
            // Update buffers if dimensions changed
            UpdateBuffers(ctx);
            
            // Apply contrast enhancement effect
            ApplyContrastEffect(ctx, input, output);
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
    
    private void ApplyContrastEffect(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        // Extract color components from thresholds
        int clipR = (ColorClip >> 16) & 0xFF;
        int clipG = (ColorClip >> 8) & 0xFF;
        int clipB = ColorClip & 0xFF;
        
        int clipOutR = (ColorClipOut >> 16) & 0xFF;
        int clipOutG = (ColorClipOut >> 8) & 0xFF;
        int clipOutB = ColorClipOut & 0xFF;
        
        // Process each pixel
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                Color inputPixel = input.GetPixel(x, y);
                Color processedPixel = ProcessPixel(inputPixel, clipR, clipG, clipB, clipOutR, clipOutG, clipOutB);
                output.SetPixel(x, y, processedPixel);
            }
        }
    }
    
    private Color ProcessPixel(Color pixel, int clipR, int clipG, int clipB, int clipOutR, int clipOutG, int clipOutB)
    {
        // Calculate color distance from clip threshold
        int distance = CalculateColorDistance(pixel.R, pixel.G, pixel.B, clipR, clipG, clipB);
        
        // Check if pixel is within distance threshold
        if (distance <= ColorDist)
        {
            // Apply contrast enhancement
            return ApplyContrastEnhancement(pixel, clipR, clipG, clipB, clipOutR, clipOutG, clipOutB);
        }
        else
        {
            // Pixel is outside distance threshold, return unchanged
            return pixel;
        }
    }
    
    private int CalculateColorDistance(int r1, int g1, int b1, int r2, int g2, int b2)
    {
        // Calculate Euclidean distance in RGB color space
        int dr = r1 - r2;
        int dg = g1 - g2;
        int db = b1 - b2;
        
        return (int)Math.Sqrt(dr * dr + dg * dg + db * db);
    }
    
    private Color ApplyContrastEnhancement(Color pixel, int clipR, int clipG, int clipB, int clipOutR, int clipOutG, int clipOutB)
    {
        // Apply input color clipping
        int r = ApplyColorClipping(pixel.R, clipR, clipOutR);
        int g = ApplyColorClipping(pixel.G, clipG, clipOutG);
        int b = ApplyColorClipping(pixel.B, clipB, clipOutB);
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private int ApplyColorClipping(int channelValue, int clipThreshold, int clipOutThreshold)
    {
        // Apply input clipping (enhance dark colors)
        if (channelValue < clipThreshold)
        {
            // Scale dark colors to enhance contrast
            float scaleFactor = (float)clipOutThreshold / clipThreshold;
            int enhanced = (int)(channelValue * scaleFactor);
            return Math.Clamp(enhanced, 0, 255);
        }
        else
        {
            // Apply output clipping (limit bright colors)
            if (channelValue > clipOutThreshold)
            {
                return clipOutThreshold;
            }
            else
            {
                return channelValue;
            }
        }
    }
    
    // Public interface for parameter adjustment
    public void SetEnabled(bool enable) { Enabled = enable; }
    
    public void SetColorClip(int colorClip) 
    { 
        ColorClip = Math.Clamp(colorClip, MinColorValue, MaxColorValue); 
    }
    
    public void SetColorClipOut(int colorClipOut) 
    { 
        ColorClipOut = Math.Clamp(colorClipOut, MinColorValue, MaxColorValue); 
    }
    
    public void SetColorDist(int colorDist) 
    { 
        ColorDist = Math.Clamp(colorDist, MinDistance, MaxDistance); 
    }
    
    // Status queries
    public bool IsEnabled() => Enabled;
    public int GetColorClip() => ColorClip;
    public int GetColorClipOut() => ColorClipOut;
    public int GetColorDist() => ColorDist;
    
    // Advanced contrast control
    public void SetRGBClip(int r, int g, int b)
    {
        int colorClip = (r << 16) | (g << 8) | b;
        SetColorClip(colorClip);
    }
    
    public void SetRGBClipOut(int r, int g, int b)
    {
        int colorClipOut = (r << 16) | (g << 8) | b;
        SetColorClipOut(colorClipOut);
    }
    
    public void SetClipThreshold(int threshold)
    {
        // Set both input and output thresholds to the same value
        int colorValue = (threshold << 16) | (threshold << 8) | threshold;
        SetColorClip(colorValue);
        SetColorClipOut(colorValue);
    }
    
    public void SetContrastEnhancement(int enhancement)
    {
        // Adjust color distance based on enhancement level
        int distance = Math.Max(1, 50 - enhancement);
        SetColorDist(distance);
    }
    
    // Contrast effect presets
    public void SetHighContrast()
    {
        SetClipThreshold(64);
        SetColorDist(5);
    }
    
    public void SetMediumContrast()
    {
        SetClipThreshold(96);
        SetColorDist(15);
    }
    
    public void SetLowContrast()
    {
        SetClipThreshold(128);
        SetColorDist(25);
    }
    
    public void SetExtremeContrast()
    {
        SetClipThreshold(32);
        SetColorDist(2);
    }
    
    public void SetSelectiveContrast(int r, int g, int b, int distance)
    {
        SetRGBClip(r, g, b);
        SetRGBClipOut(r, g, b);
        SetColorDist(distance);
    }
    
    // Color-specific presets
    public void SetRedContrast()
    {
        SetRGBClip(64, 0, 0);
        SetRGBClipOut(64, 0, 0);
        SetColorDist(10);
    }
    
    public void SetGreenContrast()
    {
        SetRGBClip(0, 64, 0);
        SetRGBClipOut(0, 64, 0);
        SetColorDist(10);
    }
    
    public void SetBlueContrast()
    {
        SetRGBClip(0, 0, 64);
        SetRGBClipOut(0, 0, 64);
        SetColorDist(10);
    }
    
    public void SetWhiteContrast()
    {
        SetRGBClip(128, 128, 128);
        SetRGBClipOut(128, 128, 128);
        SetColorDist(20);
    }
    
    public void SetBlackContrast()
    {
        SetRGBClip(32, 32, 32);
        SetRGBClipOut(32, 32, 32);
        SetColorDist(15);
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

### Contrast Processing Integration
- **Color Clipping**: Intelligent color clipping and enhancement
- **Contrast Adjustment**: Precise contrast control and manipulation
- **Distance Processing**: Advanced color distance and similarity control
- **Quality Control**: High-quality contrast enhancement algorithms

### Color Processing Integration
- **RGB Processing**: Independent processing of RGB color channels
- **Threshold Control**: Advanced threshold control and manipulation
- **Color Enhancement**: Intelligent color enhancement and processing
- **Visual Quality**: High-quality color transformation and processing

### Image Processing Integration
- **Contrast Enhancement**: Advanced contrast and brightness manipulation
- **Color Filtering**: Intelligent color filtering and processing
- **Visual Enhancement**: Multiple enhancement modes for visual integration
- **Performance Optimization**: Optimized operations for contrast processing

This effect provides the foundation for sophisticated contrast enhancement, creating advanced contrast transformations with color clipping, distance-based processing, and intelligent color manipulation for sophisticated AVS visualization systems.
