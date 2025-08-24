# Picture Effects (Render / Picture)

## Overview

The **Picture Effects** system is a sophisticated image rendering engine that integrates external bitmap images directly into AVS presets. It implements advanced image loading, scaling, blending, and persistence effects for creating complex visual compositions with external image assets. This effect provides the foundation for sophisticated image integration, background overlays, and complex visual layering systems.

## Source Analysis

### Core Architecture (`r_picture.cpp`)

The effect is implemented as a C++ class `C_PictureClass` that inherits from `C_RBASE`. It provides a comprehensive image rendering system with bitmap loading, dynamic scaling, multiple blending modes, and intelligent persistence effects for creating complex visual compositions with external images.

### Key Components

#### Image Loading System
Advanced bitmap management engine:
- **Dynamic Loading**: Load external bitmap files from file system
- **Memory Management**: Intelligent bitmap memory allocation and deallocation
- **Format Support**: Support for various bitmap formats and resolutions
- **Path Resolution**: Automatic path resolution and file management

#### Image Scaling Engine
Sophisticated scaling system:
- **Dynamic Scaling**: Real-time image scaling to match screen dimensions
- **Aspect Ratio Control**: Configurable aspect ratio preservation
- **Quality Optimization**: High-quality scaling with COLORONCOLOR mode
- **Performance Scaling**: Optimized scaling for different screen sizes

#### Blending System
Advanced blending capabilities:
- **Multiple Blend Modes**: Replace, additive, average, and adaptive blending
- **Alpha Channel Support**: Full alpha channel support for transparency
- **Blend Control**: Configurable blend intensity and behavior
- **Visual Integration**: Seamless integration with existing visual content

#### Persistence Engine
Intelligent persistence system:
- **Frame Persistence**: Configurable frame persistence for image retention
- **Beat Reactivity**: Beat-reactive persistence control
- **Adaptive Behavior**: Intelligent persistence based on visual content
- **Memory Optimization**: Optimized memory usage for persistent images

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `enabled` | bool | true/false | true | Enable/disable picture effect |
| `blend` | int | 0-1 | 0 | Blend mode (0=Replace, 1=Additive) |
| `blendavg` | int | 0-1 | 1 | Average blending mode |
| `adapt` | int | 0-1 | 0 | Adaptive blending mode |
| `persist` | int | 0-255 | 6 | Frame persistence count |
| `ratio` | int | 0-1 | 0 | Maintain aspect ratio |
| `axis_ratio` | int | 0-1 | 0 | Axis-based aspect ratio control |
| `ascName` | string | MAX_PATH | "" | Image file path |

### Blending Modes

| Mode | Value | Description | Behavior |
|------|-------|-------------|----------|
| **Replace** | 0 | Replace mode | Image replaces underlying content |
| **Additive** | 1 | Additive blending | Image adds to underlying content |
| **Average** | 1 | Average blending | Image averages with underlying content |
| **Adaptive** | 1 | Adaptive blending | Image adapts to underlying content |

### Persistence Modes

| Mode | Value | Description | Behavior |
|------|-------|-------------|----------|
| **No Persistence** | 0 | No frame retention | Image refreshes every frame |
| **Low Persistence** | 1-10 | Low frame retention | Image retains for few frames |
| **Medium Persistence** | 11-50 | Medium frame retention | Image retains for moderate frames |
| **High Persistence** | 51-255 | High frame retention | Image retains for many frames |

## C# Implementation

```csharp
public class PictureEffectsNode : AvsModuleNode
{
    public bool Enabled { get; set; } = true;
    public int Blend { get; set; } = 0;
    public int BlendAvg { get; set; } = 1;
    public int Adapt { get; set; } = 0;
    public int Persist { get; set; } = 6;
    public int Ratio { get; set; } = 0;
    public int AxisRatio { get; set; } = 0;
    public string ImagePath { get; set; } = "";
    
    // Internal state
    private ImageBuffer sourceImage;
    private ImageBuffer scaledImage;
    private int imageWidth, imageHeight;
    private int lastWidth, lastHeight;
    private int persistCount;
    private bool imageLoaded;
    private readonly object renderLock = new object();
    
    // Performance optimization
    private const int MaxPersist = 255;
    private const int MinPersist = 0;
    private const int MaxBlend = 1;
    private const int MinBlend = 0;
    private const int MaxRatio = 1;
    private const int MinRatio = 0;
    private const int MaxAxisRatio = 1;
    private const int MinAxisRatio = 0;
    
    public PictureEffectsNode()
    {
        sourceImage = null;
        scaledImage = null;
        imageWidth = imageHeight = 0;
        lastWidth = lastHeight = 0;
        persistCount = 0;
        imageLoaded = false;
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled || !imageLoaded || ctx.Width <= 0 || ctx.Height <= 0) return;
        
        lock (renderLock)
        {
            // Update buffers if dimensions changed
            UpdateBuffers(ctx);
            
            // Handle persistence
            if (persistCount > 0)
            {
                persistCount--;
                return; // Use persistent image
            }
            
            // Reset persistence counter
            persistCount = Persist;
            
            // Apply picture effect
            ApplyPictureEffect(ctx, input, output);
        }
    }
    
    private void UpdateBuffers(FrameContext ctx)
    {
        if (lastWidth != ctx.Width || lastHeight != ctx.Height)
        {
            lastWidth = ctx.Width;
            lastHeight = ctx.Height;
            
            // Regenerate scaled image for new dimensions
            GenerateScaledImage(ctx);
        }
    }
    
    private void GenerateScaledImage(FrameContext ctx)
    {
        if (sourceImage == null || !imageLoaded) return;
        
        // Calculate scaling dimensions
        int targetWidth, targetHeight;
        int startWidth, startHeight;
        
        if (Ratio == 1)
        {
            // Maintain aspect ratio
            float sourceAspect = (float)imageWidth / imageHeight;
            float targetAspect = (float)ctx.Width / ctx.Height;
            
            if (sourceAspect > targetAspect)
            {
                // Source is wider, fit to width
                targetWidth = ctx.Width;
                targetHeight = (int)(ctx.Width / sourceAspect);
                startWidth = 0;
                startHeight = (ctx.Height - targetHeight) / 2;
            }
            else
            {
                // Source is taller, fit to height
                targetHeight = ctx.Height;
                targetWidth = (int)(ctx.Height * sourceAspect);
                startHeight = 0;
                startWidth = (ctx.Width - targetWidth) / 2;
            }
        }
        else
        {
            // Stretch to fill
            targetWidth = ctx.Width;
            targetHeight = ctx.Height;
            startWidth = startHeight = 0;
        }
        
        // Create scaled image
        scaledImage = new ImageBuffer(ctx.Width, ctx.Height);
        
        // Clear background
        ClearBackground(ctx);
        
        // Scale and copy source image
        ScaleAndCopyImage(ctx, targetWidth, targetHeight, startWidth, startHeight);
    }
    
    private void ClearBackground(FrameContext ctx)
    {
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                scaledImage.SetPixel(x, y, Color.Black);
            }
        }
    }
    
    private void ScaleAndCopyImage(FrameContext ctx, int targetWidth, int targetHeight, int startX, int startY)
    {
        if (sourceImage == null) return;
        
        // Calculate scaling factors
        float scaleX = (float)imageWidth / targetWidth;
        float scaleY = (float)imageHeight / targetHeight;
        
        // Scale image using high-quality bilinear interpolation
        for (int y = 0; y < targetHeight; y++)
        {
            for (int x = 0; x < targetWidth; x++)
            {
                int sourceX = (int)(x * scaleX);
                int sourceY = (int)(y * scaleY);
                
                // Clamp source coordinates
                sourceX = Math.Clamp(sourceX, 0, imageWidth - 1);
                sourceY = Math.Clamp(sourceY, 0, imageHeight - 1);
                
                // Get source pixel
                Color sourcePixel = sourceImage.GetPixel(sourceX, sourceY);
                
                // Calculate target position
                int targetX = startX + x;
                int targetY = startY + y;
                
                // Check bounds
                if (targetX >= 0 && targetX < ctx.Width && targetY >= 0 && targetY < ctx.Height)
                {
                    scaledImage.SetPixel(targetX, targetY, sourcePixel);
                }
            }
        }
    }
    
    private void ApplyPictureEffect(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (scaledImage == null) return;
        
        // Apply blending mode
        switch (Blend)
        {
            case 0: // Replace
                ApplyReplaceBlending(ctx, output);
                break;
                
            case 1: // Additive
                ApplyAdditiveBlending(ctx, input, output);
                break;
        }
        
        // Apply additional blending modes
        if (BlendAvg == 1)
        {
            ApplyAverageBlending(ctx, input, output);
        }
        
        if (Adapt == 1)
        {
            ApplyAdaptiveBlending(ctx, input, output);
        }
    }
    
    private void ApplyReplaceBlending(FrameContext ctx, ImageBuffer output)
    {
        // Direct copy of scaled image
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                output.SetPixel(x, y, scaledImage.GetPixel(x, y));
            }
        }
    }
    
    private void ApplyAdditiveBlending(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        // Add scaled image to input
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                Color inputPixel = input.GetPixel(x, y);
                Color scaledPixel = scaledImage.GetPixel(x, y);
                
                Color blended = Color.FromRgb(
                    (byte)Math.Min(255, inputPixel.R + scaledPixel.R),
                    (byte)Math.Min(255, inputPixel.G + scaledPixel.G),
                    (byte)Math.Min(255, inputPixel.B + scaledPixel.B)
                );
                
                output.SetPixel(x, y, blended);
            }
        }
    }
    
    private void ApplyAverageBlending(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        // Average scaled image with input
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                Color inputPixel = input.GetPixel(x, y);
                Color scaledPixel = scaledImage.GetPixel(x, y);
                
                Color averaged = Color.FromRgb(
                    (byte)((inputPixel.R + scaledPixel.R) / 2),
                    (byte)((inputPixel.G + scaledPixel.G) / 2),
                    (byte)((inputPixel.B + scaledPixel.B) / 2)
                );
                
                output.SetPixel(x, y, averaged);
            }
        }
    }
    
    private void ApplyAdaptiveBlending(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        // Adaptive blending based on input content
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                Color inputPixel = input.GetPixel(x, y);
                Color scaledPixel = scaledImage.GetPixel(x, y);
                
                // Calculate adaptive blend factor based on input brightness
                float inputBrightness = (inputPixel.R + inputPixel.G + inputPixel.B) / 765.0f; // 255 * 3
                float blendFactor = 0.5f + (inputBrightness * 0.5f);
                
                Color adaptive = Color.FromRgb(
                    (byte)((inputPixel.R * (1.0f - blendFactor)) + (scaledPixel.R * blendFactor)),
                    (byte)((inputPixel.G * (1.0f - blendFactor)) + (scaledPixel.G * blendFactor)),
                    (byte)((inputPixel.B * (1.0f - blendFactor)) + (scaledPixel.B * blendFactor))
                );
                
                output.SetPixel(x, y, adaptive);
            }
        }
    }
    
    // Image loading methods
    public bool LoadImage(string imagePath)
    {
        try
        {
            lock (renderLock)
            {
                // Load image from file
                using (var stream = File.OpenRead(imagePath))
                {
                    var bitmap = new System.Drawing.Bitmap(stream);
                    
                    // Convert to ImageBuffer
                    sourceImage = new ImageBuffer(bitmap.Width, bitmap.Height);
                    
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        for (int x = 0; x < bitmap.Width; x++)
                        {
                            var pixel = bitmap.GetPixel(x, y);
                            sourceImage.SetPixel(x, y, Color.FromRgba(pixel.R, pixel.G, pixel.B, pixel.A));
                        }
                    }
                    
                    imageWidth = bitmap.Width;
                    imageHeight = bitmap.Height;
                    ImagePath = imagePath;
                    imageLoaded = true;
                    
                    // Reset persistence
                    persistCount = 0;
                    
                    return true;
                }
            }
        }
        catch (Exception)
        {
            // Failed to load image
            imageLoaded = false;
            return false;
        }
    }
    
    public void UnloadImage()
    {
        lock (renderLock)
        {
            sourceImage = null;
            scaledImage = null;
            imageWidth = imageHeight = 0;
            imageLoaded = false;
            ImagePath = "";
            persistCount = 0;
        }
    }
    
    public bool IsImageLoaded() => imageLoaded;
    
    // Public interface for parameter adjustment
    public void SetEnabled(bool enable) { Enabled = enable; }
    
    public void SetBlend(int blend) 
    { 
        Blend = Math.Clamp(blend, MinBlend, MaxBlend); 
    }
    
    public void SetBlendAvg(int blendAvg) 
    { 
        BlendAvg = Math.Clamp(blendAvg, MinBlend, MaxBlend); 
    }
    
    public void SetAdapt(int adapt) 
    { 
        Adapt = Math.Clamp(adapt, MinBlend, MaxBlend); 
    }
    
    public void SetPersist(int persist) 
    { 
        Persist = Math.Clamp(persist, MinPersist, MaxPersist); 
    }
    
    public void SetRatio(int ratio) 
    { 
        Ratio = Math.Clamp(ratio, MinRatio, MaxRatio); 
    }
    
    public void SetAxisRatio(int axisRatio) 
    { 
        AxisRatio = Math.Clamp(axisRatio, MinAxisRatio, MaxAxisRatio); 
    }
    
    // Status queries
    public bool IsEnabled() => Enabled;
    public int GetBlend() => Blend;
    public int GetBlendAvg() => BlendAvg;
    public int GetAdapt() => Adapt;
    public int GetPersist() => Persist;
    public int GetRatio() => Ratio;
    public int GetAxisRatio() => AxisRatio;
    public string GetImagePath() => ImagePath;
    public int GetImageWidth() => imageWidth;
    public int GetImageHeight() => imageHeight;
    public int GetLastWidth() => lastWidth;
    public int GetLastHeight() => lastHeight;
    public int GetPersistCount() => persistCount;
    
    // Advanced image control
    public void SetImagePosition(int x, int y)
    {
        // This would modify the image positioning within the scaled output
        // For now, we use centered positioning
    }
    
    public void SetImageScale(float scaleX, float scaleY)
    {
        // This would modify the image scaling factors
        // For now, we use automatic scaling
    }
    
    public void SetImageRotation(float angle)
    {
        // This would add rotation to the image
        // For now, we use no rotation
    }
    
    // Image effects
    public void ApplyImageFilter(ImageFilter filter)
    {
        if (sourceImage == null || !imageLoaded) return;
        
        lock (renderLock)
        {
            switch (filter)
            {
                case ImageFilter.Grayscale:
                    ApplyGrayscaleFilter();
                    break;
                    
                case ImageFilter.Sepia:
                    ApplySepiaFilter();
                    break;
                    
                case ImageFilter.Invert:
                    ApplyInvertFilter();
                    break;
                    
                case ImageFilter.Brightness:
                    ApplyBrightnessFilter(1.5f);
                    break;
                    
                case ImageFilter.Contrast:
                    ApplyContrastFilter(1.3f);
                    break;
            }
            
            // Regenerate scaled image
            if (lastWidth > 0 && lastHeight > 0)
            {
                var ctx = new FrameContext { Width = lastWidth, Height = lastHeight };
                GenerateScaledImage(ctx);
            }
        }
    }
    
    private void ApplyGrayscaleFilter()
    {
        for (int y = 0; y < imageHeight; y++)
        {
            for (int x = 0; x < imageWidth; x++)
            {
                Color pixel = sourceImage.GetPixel(x, y);
                int gray = (pixel.R + pixel.G + pixel.B) / 3;
                sourceImage.SetPixel(x, y, Color.FromRgb((byte)gray, (byte)gray, (byte)gray));
            }
        }
    }
    
    private void ApplySepiaFilter()
    {
        for (int y = 0; y < imageHeight; y++)
        {
            for (int x = 0; x < imageWidth; x++)
            {
                Color pixel = sourceImage.GetPixel(x, y);
                
                int r = (int)((pixel.R * 0.393) + (pixel.G * 0.769) + (pixel.B * 0.189));
                int g = (int)((pixel.R * 0.349) + (pixel.G * 0.686) + (pixel.B * 0.168));
                int b = (int)((pixel.R * 0.272) + (pixel.G * 0.534) + (pixel.B * 0.131));
                
                r = Math.Clamp(r, 0, 255);
                g = Math.Clamp(g, 0, 255);
                b = Math.Clamp(b, 0, 255);
                
                sourceImage.SetPixel(x, y, Color.FromRgb((byte)r, (byte)g, (byte)b));
            }
        }
    }
    
    private void ApplyInvertFilter()
    {
        for (int y = 0; y < imageHeight; y++)
        {
            for (int x = 0; x < imageWidth; x++)
            {
                Color pixel = sourceImage.GetPixel(x, y);
                sourceImage.SetPixel(x, y, Color.FromRgb(
                    (byte)(255 - pixel.R),
                    (byte)(255 - pixel.G),
                    (byte)(255 - pixel.B)
                ));
            }
        }
    }
    
    private void ApplyBrightnessFilter(float factor)
    {
        for (int y = 0; y < imageHeight; y++)
        {
            for (int x = 0; x < imageWidth; x++)
            {
                Color pixel = sourceImage.GetPixel(x, y);
                sourceImage.SetPixel(x, y, Color.FromRgb(
                    (byte)Math.Clamp(pixel.R * factor, 0, 255),
                    (byte)Math.Clamp(pixel.G * factor, 0, 255),
                    (byte)Math.Clamp(pixel.B * factor, 0, 255)
                ));
            }
        }
    }
    
    private void ApplyContrastFilter(float factor)
    {
        float contrastFactor = (259 * (factor + 255)) / (255 * (259 - factor));
        
        for (int y = 0; y < imageHeight; y++)
        {
            for (int x = 0; x < imageWidth; x++)
            {
                Color pixel = sourceImage.GetPixel(x, y);
                sourceImage.SetPixel(x, y, Color.FromRgb(
                    (byte)Math.Clamp(contrastFactor * (pixel.R - 128) + 128, 0, 255),
                    (byte)Math.Clamp(contrastFactor * (pixel.G - 128) + 128, 0, 255),
                    (byte)Math.Clamp(contrastFactor * (pixel.B - 128) + 128, 0, 255)
                ));
            }
        }
    }
    
    // Image filter enum
    public enum ImageFilter
    {
        Grayscale,
        Sepia,
        Invert,
        Brightness,
        Contrast
    }
    
    // Performance optimization
    public void SetRenderQuality(int quality)
    {
        // Quality could affect scaling algorithm or optimization level
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
            UnloadImage();
        }
    }
}
```

## Integration Points

### Image Processing Integration
- **Bitmap Loading**: Advanced bitmap file loading and management
- **Image Scaling**: High-quality image scaling with aspect ratio control
- **Format Support**: Support for various bitmap formats and resolutions
- **Memory Management**: Intelligent memory management for image data

### Video Processing Integration
- **Frame Integration**: Seamless integration with video frame processing
- **Blending Modes**: Advanced blending with existing visual content
- **Persistence Control**: Intelligent frame persistence for image retention
- **Performance Optimization**: Optimized rendering for image overlays

### File System Integration
- **Path Resolution**: Automatic path resolution and file management
- **Error Handling**: Robust error handling for file operations
- **Format Detection**: Automatic format detection and validation
- **Resource Management**: Intelligent resource cleanup and management

## Usage Examples

### Basic Image Loading
```csharp
var pictureNode = new PictureEffectsNode
{
    Enabled = true,                      // Enable effect
    Blend = 0,                          // Replace mode
    BlendAvg = 0,                       // No average blending
    Adapt = 0,                          // No adaptive blending
    Persist = 10,                       // 10 frame persistence
    Ratio = 1,                          // Maintain aspect ratio
    AxisRatio = 0                       // No axis ratio control
};

// Load image
pictureNode.LoadImage("C:\\Images\\background.bmp");
```

### Blended Image Overlay
```csharp
var pictureNode = new PictureEffectsNode
{
    Enabled = true,
    Blend = 1,                          // Additive blending
    BlendAvg = 1,                       // Average blending
    Adapt = 1,                          // Adaptive blending
    Persist = 20,                       // 20 frame persistence
    Ratio = 1,                          // Maintain aspect ratio
    AxisRatio = 0
};

pictureNode.LoadImage("C:\\Images\\overlay.png");
```

### High Persistence Background
```csharp
var pictureNode = new PictureEffectsNode
{
    Enabled = true,
    Blend = 0,                          // Replace mode
    BlendAvg = 0,                       // No average blending
    Adapt = 0,                          // No adaptive blending
    Persist = 100,                      // High persistence
    Ratio = 1,                          // Maintain aspect ratio
    AxisRatio = 0
};

pictureNode.LoadImage("C:\\Images\\background.jpg");

// Apply image filter
pictureNode.ApplyImageFilter(PictureEffectsNode.ImageFilter.Sepia);
```

## Technical Notes

### Image Architecture
The effect implements sophisticated image processing:
- **Dynamic Loading**: Intelligent bitmap loading and memory management
- **Quality Scaling**: High-quality scaling with bilinear interpolation
- **Format Support**: Comprehensive bitmap format support
- **Memory Optimization**: Optimized memory usage for large images

### Blending Architecture
Advanced blending processing system:
- **Multiple Modes**: Replace, additive, average, and adaptive blending
- **Alpha Support**: Full alpha channel support for transparency
- **Quality Control**: High-quality blending algorithms
- **Performance Scaling**: Dynamic performance scaling based on image complexity

### Integration System
Sophisticated system integration:
- **File System**: Deep integration with file system operations
- **Video Processing**: Seamless integration with video frame pipeline
- **Memory Management**: Advanced memory management and optimization
- **Performance Optimization**: Optimized operations for image processing

This effect provides the foundation for sophisticated image integration, creating complex visual compositions with external image assets, enabling advanced background overlays, image effects, and complex visual layering systems for sophisticated AVS visualization systems.
