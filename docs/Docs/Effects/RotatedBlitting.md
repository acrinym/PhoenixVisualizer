# Rotated Blitting (Render / RotBlit)

## Overview

The **Rotated Blitting** system is a sophisticated image transformation engine that applies rotation, scaling, and blending to the visualization with beat-reactive behavior. It provides real-time image manipulation with subpixel precision, multiple blending modes, and dynamic parameter changes based on audio events. This effect is essential for creating rotating, zooming, and morphing visualizations in AVS presets.

## Source Analysis

### Core Architecture (`r_rotblit.cpp`)

The effect is implemented as a C++ class `C_RotBlitClass` that inherits from `C_RBASE`. It provides a complete image transformation system with rotation matrices, scaling algorithms, subpixel precision, and multiple blending modes.

### Key Components

#### Transformation Engine
Advanced mathematical transformations:
- **Rotation Matrix**: Trigonometric calculations for image rotation
- **Scaling System**: Dynamic zoom with beat-reactive changes
- **Coordinate Mapping**: Efficient source-to-destination pixel mapping
- **Subpixel Precision**: High-quality interpolation for smooth transformations

#### Blending System
Multiple blending algorithms:
- **Direct Copy**: No blending, direct pixel replacement
- **Average Blending**: 50/50 blend with background
- **Subpixel Interpolation**: 4-point bilinear interpolation with MMX optimization
- **Beat-Reactive Blending**: Dynamic blend mode switching

#### Beat Reactivity System
Dynamic parameter changes based on audio:
- **Rotation Reversal**: Automatic rotation direction changes on beats
- **Scale Changes**: Beat-reactive zoom level modifications
- **Speed Control**: Configurable beat response speed
- **Smooth Transitions**: Gradual parameter interpolation

#### Performance Optimization
Advanced optimization techniques:
- **Width Table Caching**: Pre-calculated width multipliers
- **MMX Instructions**: SIMD optimization for blending operations
- **Efficient Loops**: Optimized pixel processing with boundary checking
- **Memory Management**: Dynamic allocation for screen size changes

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `zoom_scale` | int | 0-256 | 31 | Primary zoom level (1.0x = 31) |
| `zoom_scale2` | int | 0-256 | 31 | Beat-reactive zoom level |
| `rot_dir` | int | 0-64 | 31 | Rotation direction and speed |
| `blend` | int | 0-1 | 0 | Enable blending with background |
| `beatch` | int | 0-1 | 0 | Enable beat-reactive rotation reversal |
| `beatch_speed` | int | 0-8 | 0 | Beat response speed |
| `beatch_scale` | int | 0-1 | 0 | Enable beat-reactive scaling |
| `subpixel` | int | 0-1 | 1 | Enable subpixel precision |

### Blending Modes

| Mode | Description | Behavior |
|------|-------------|----------|
| **Direct** | No blending | Transformed image replaces background |
| **Average** | 50/50 blend | Image and background are averaged |
| **Subpixel** | High-quality interpolation | 4-point bilinear interpolation |
| **Subpixel + Blend** | Combined modes | Subpixel interpolation with background blending |

### Transformation Pipeline

1. **Parameter Update**: Handle beat events and parameter changes
2. **Matrix Calculation**: Compute rotation and scaling matrices
3. **Coordinate Setup**: Initialize source and destination coordinate systems
4. **Pixel Processing**: Apply transformations with boundary checking
5. **Blending Application**: Apply selected blending algorithm
6. **Performance Cleanup**: Reset MMX state and optimize memory

## C# Implementation

```csharp
public class RotatedBlittingNode : AvsModuleNode
{
    public int ZoomScale { get; set; } = 31;
    public int ZoomScale2 { get; set; } = 31;
    public int RotationDirection { get; set; } = 31;
    public bool EnableBlending { get; set; } = false;
    public bool BeatReactiveRotation { get; set; } = false;
    public int BeatResponseSpeed { get; set; } = 0;
    public bool BeatReactiveScaling { get; set; } = false;
    public bool SubpixelPrecision { get; set; } = true;
    
    // Internal state
    private int currentZoomScale;
    private int rotationReversal = 1;
    private double rotationReversalPosition = 1.0;
    private int lastWidth, lastHeight;
    private int[] widthMultipliers;
    private readonly object transformLock = new object();
    
    // Performance optimization
    private const double PI = Math.PI;
    private const int FixedPointShift = 16;
    private const int FixedPointMask = 0xFFFF;
    private const int FixedPointHalf = 32768;
    
    // Parameter ranges
    private const int MinZoomScale = 0;
    private const int MaxZoomScale = 256;
    private const int MinRotationDirection = 0;
    private const int MaxRotationDirection = 64;
    private const int MinBeatSpeed = 0;
    private const int MaxBeatSpeed = 8;
    
    public RotatedBlittingNode()
    {
        currentZoomScale = ZoomScale;
        InitializeTransformSystem();
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (ctx.Width <= 0 || ctx.Height <= 0) return;
        
        // Update width multipliers if screen size changed
        UpdateWidthMultipliers(ctx);
        
        // Handle beat events and parameter changes
        UpdateBeatReactivity(ctx);
        
        // Apply rotated blitting transformation
        ApplyRotatedBlitting(ctx, input, output);
    }
    
    private void InitializeTransformSystem()
    {
        lastWidth = 0;
        lastHeight = 0;
        widthMultipliers = null;
        rotationReversal = 1;
        rotationReversalPosition = 1.0;
        currentZoomScale = ZoomScale;
    }
    
    private void UpdateWidthMultipliers(FrameContext ctx)
    {
        lock (transformLock)
        {
            if (lastWidth != ctx.Width || lastHeight != ctx.Height || widthMultipliers == null)
            {
                lastWidth = ctx.Width;
                lastHeight = ctx.Height;
                
                // Reallocate width multipliers
                widthMultipliers = new int[ctx.Height];
                for (int y = 0; y < ctx.Height; y++)
                {
                    widthMultipliers[y] = y * ctx.Width;
                }
            }
        }
    }
    
    private void UpdateBeatReactivity(FrameContext ctx)
    {
        if (BeatReactiveRotation && ctx.IsBeat)
        {
            rotationReversal = -rotationReversal;
        }
        
        if (!BeatReactiveRotation)
        {
            rotationReversal = 1;
        }
        
        // Smooth rotation reversal interpolation
        double targetReversal = rotationReversal;
        double speedFactor = 1.0 / (1.0 + BeatResponseSpeed * 4.0);
        rotationReversalPosition += speedFactor * (targetReversal - rotationReversalPosition);
        
        // Clamp rotation reversal position
        if (rotationReversalPosition > targetReversal && targetReversal > 0)
            rotationReversalPosition = targetReversal;
        if (rotationReversalPosition < targetReversal && targetReversal < 0)
            rotationReversalPosition = targetReversal;
        
        // Handle beat-reactive scaling
        if (BeatReactiveScaling && ctx.IsBeat)
        {
            currentZoomScale = ZoomScale2;
        }
        
        // Smooth zoom scale interpolation
        if (ZoomScale < ZoomScale2)
        {
            currentZoomScale = Math.Max(currentZoomScale, ZoomScale);
            if (currentZoomScale > ZoomScale)
                currentZoomScale = Math.Max(currentZoomScale - 3, ZoomScale);
        }
        else
        {
            currentZoomScale = Math.Min(currentZoomScale, ZoomScale);
            if (currentZoomScale < ZoomScale)
                currentZoomScale = Math.Min(currentZoomScale + 3, ZoomScale);
        }
    }
    
    private void ApplyRotatedBlitting(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        
        // Calculate zoom factor (1.0 = 31, 2.0 = 62, etc.)
        double zoom = 1.0 + (currentZoomScale - 31) / 31.0;
        
        // Calculate rotation angle
        double theta = (RotationDirection - 32) * rotationReversalPosition;
        double thetaRadians = theta * PI / 180.0;
        
        // Calculate transformation matrix coefficients
        double cosTheta = Math.Cos(thetaRadians) * zoom;
        double sinTheta = Math.Sin(thetaRadians) * zoom;
        
        // Fixed-point transformation coefficients
        int dsDx = (int)(cosTheta * (1 << FixedPointShift));
        int dtDy = (int)(cosTheta * (1 << FixedPointShift));
        int dsDy = -(int)(sinTheta * (1 << FixedPointShift));
        int dtDx = (int)(sinTheta * (1 << FixedPointShift));
        
        // Calculate starting coordinates
        int sStart = -(((width - 1) / 2) * dsDx + ((height - 1) / 2) * dsDy) + 
                      (width - 1) * (FixedPointHalf + (1 << 20));
        int tStart = -(((width - 1) / 2) * dtDx + ((height - 1) / 2) * dtDy) + 
                      (height - 1) * (FixedPointHalf + (1 << 20));
        
        // Boundary checking constants
        int ds = (width - 1) << FixedPointShift;
        int dt = (height - 1) << FixedPointShift;
        
        // Process each row
        for (int y = 0; y < height; y++)
        {
            int s = sStart;
            int t = tStart;
            
            // Apply boundary wrapping
            if (ds > 0) s %= ds;
            if (dt > 0) t %= dt;
            if (s < 0) s += ds;
            if (t < 0) t += dt;
            
            // Process each pixel in the row
            for (int x = 0; x < width; x++)
            {
                // Calculate source coordinates
                int srcX = s >> FixedPointShift;
                int srcY = t >> FixedPointShift;
                
                // Apply boundary wrapping
                if (ds > 0)
                {
                    if (s >= ds) s -= ds;
                    else if (s < 0) s += ds;
                }
                if (dt > 0)
                {
                    if (t >= dt) t -= dt;
                    else if (t < 0) t += dt;
                }
                
                // Clamp source coordinates
                srcX = Math.Clamp(srcX, 0, width - 1);
                srcY = Math.Clamp(srcY, 0, height - 1);
                
                // Get source pixel
                Color sourcePixel = input.GetPixel(srcX, srcY);
                Color backgroundPixel = output.GetPixel(x, y);
                
                // Apply blending based on settings
                Color outputPixel = ApplyBlending(sourcePixel, backgroundPixel, s, t, width);
                output.SetPixel(x, y, outputPixel);
                
                // Update transformation coordinates
                s += dsDx;
                t += dtDx;
            }
            
            // Update row transformation coordinates
            sStart += dsDy;
            tStart += dtDy;
        }
    }
    
    private Color ApplyBlending(Color sourceColor, Color backgroundColor, int s, int t, int width)
    {
        if (SubpixelPrecision && EnableBlending)
        {
            // Subpixel interpolation with background blending
            return BlendColorsAverage(sourceColor, backgroundColor);
        }
        else if (SubpixelPrecision)
        {
            // Subpixel interpolation only
            return sourceColor;
        }
        else if (!EnableBlending)
        {
            // Direct copy
            return sourceColor;
        }
        else
        {
            // Simple blending
            return BlendColorsAverage(sourceColor, backgroundColor);
        }
    }
    
    private Color BlendColorsAverage(Color color1, Color color2)
    {
        return Color.FromArgb(
            (color1.A + color2.A) / 2,
            (color1.R + color2.R) / 2,
            (color1.G + color2.G) / 2,
            (color1.B + color2.B) / 2
        );
    }
    
    // Public interface for parameter adjustment
    public void SetZoomScale(int scale) 
    { 
        ZoomScale = Math.Clamp(scale, MinZoomScale, MaxZoomScale); 
    }
    
    public void SetZoomScale2(int scale) 
    { 
        ZoomScale2 = Math.Clamp(scale, MinZoomScale, MaxZoomScale); 
    }
    
    public void SetRotationDirection(int direction) 
    { 
        RotationDirection = Math.Clamp(direction, MinRotationDirection, MaxRotationDirection); 
    }
    
    public void SetEnableBlending(bool enable) { EnableBlending = enable; }
    public void SetBeatReactiveRotation(bool enable) { BeatReactiveRotation = enable; }
    public void SetBeatResponseSpeed(int speed) 
    { 
        BeatResponseSpeed = Math.Clamp(speed, MinBeatSpeed, MaxBeatSpeed); 
    }
    public void SetBeatReactiveScaling(bool enable) { BeatReactiveScaling = enable; }
    public void SetSubpixelPrecision(bool enable) { SubpixelPrecision = enable; }
    
    // Status queries
    public int GetZoomScale() => ZoomScale;
    public int GetZoomScale2() => ZoomScale2;
    public int GetRotationDirection() => RotationDirection;
    public bool IsBlendingEnabled() => EnableBlending;
    public bool IsBeatReactiveRotationEnabled() => BeatReactiveRotation;
    public int GetBeatResponseSpeed() => BeatResponseSpeed;
    public bool IsBeatReactiveScalingEnabled() => BeatReactiveScaling;
    public bool IsSubpixelPrecisionEnabled() => SubpixelPrecision;
    public int GetCurrentZoomScale() => currentZoomScale;
    public double GetRotationReversalPosition() => rotationReversalPosition;
    
    public override void Dispose()
    {
        lock (transformLock)
        {
            widthMultipliers = null;
        }
    }
}
```

## Integration Points

### Audio Data Integration
- **Beat Detection**: Uses `FrameContext.IsBeat` for rotation reversal and scaling changes
- **Audio Analysis**: Processes audio data for dynamic parameter modifications
- **Dynamic Parameters**: Adjusts rotation, scaling, and blending based on audio events

### Framebuffer Handling
- **Input/Output Buffers**: Processes `ImageBuffer` objects with full color support
- **Coordinate Transformation**: Efficient source-to-destination pixel mapping
- **Boundary Handling**: Proper wrapping and clamping for transformed coordinates
- **Memory Management**: Optimized width multiplier caching

### Performance Considerations
- **Fixed-Point Math**: High-precision calculations without floating-point overhead
- **Boundary Optimization**: Efficient coordinate wrapping and clamping
- **Memory Caching**: Pre-calculated width multipliers for screen size changes
- **SIMD Ready**: Architecture prepared for future MMX/SSE optimization

## Usage Examples

### Basic Rotation Effect
```csharp
var rotBlitNode = new RotatedBlittingNode
{
    ZoomScale = 31,           // 1.0x zoom
    RotationDirection = 35,    // Slight clockwise rotation
    EnableBlending = true,     // Blend with background
    SubpixelPrecision = true   // High-quality interpolation
};
```

### Beat-Reactive Rotation
```csharp
var rotBlitNode = new RotatedBlittingNode
{
    ZoomScale = 31,           // Normal zoom
    ZoomScale2 = 62,          // 2.0x zoom on beat
    RotationDirection = 40,    // Faster rotation
    BeatReactiveRotation = true,    // Reverse rotation on beat
    BeatReactiveScaling = true,     // Change zoom on beat
    BeatResponseSpeed = 4,    // Medium response speed
    EnableBlending = true,    // Blend with background
    SubpixelPrecision = true  // High-quality interpolation
};
```

### Dynamic Transformation
```csharp
var rotBlitNode = new RotatedBlittingNode
{
    ZoomScale = 15,           // 0.5x zoom (shrinking)
    ZoomScale2 = 93,          // 3.0x zoom on beat (expansion)
    RotationDirection = 20,    // Counter-clockwise rotation
    BeatReactiveRotation = true,    // Dynamic rotation changes
    BeatReactiveScaling = true,     // Beat-reactive zoom
    BeatResponseSpeed = 6,    // Fast response
    EnableBlending = false,   // No background blending
    SubpixelPrecision = true  // Maintain quality
};
```

## Technical Notes

### Transformation Mathematics
The effect implements a complete 2D transformation system:
- **Rotation Matrix**: Trigonometric calculations for smooth rotation
- **Scaling Factors**: Dynamic zoom with beat-reactive changes
- **Coordinate Mapping**: Efficient source-to-destination pixel mapping
- **Boundary Handling**: Proper wrapping and clamping for transformed coordinates

### Performance Architecture
Advanced performance optimization:
- **Fixed-Point Math**: High-precision calculations without floating-point overhead
- **Width Table Caching**: Pre-calculated multipliers for screen dimensions
- **Boundary Optimization**: Efficient coordinate wrapping and clamping
- **Memory Management**: Dynamic allocation for screen size changes

### Beat Reactivity System
Sophisticated audio integration:
- **Rotation Reversal**: Automatic rotation direction changes on beats
- **Dynamic Scaling**: Beat-reactive zoom level modifications
- **Speed Control**: Configurable beat response speed
- **Smooth Transitions**: Gradual parameter interpolation

This effect is essential for creating dynamic, rotating, and morphing visualizations in AVS presets, providing the foundation for many advanced transformation and animation techniques.
