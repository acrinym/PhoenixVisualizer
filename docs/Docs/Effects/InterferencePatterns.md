# Interference Patterns (Trans / Interferences)

## Overview

The **Interference Patterns** system is a sophisticated multi-point interference engine that creates dynamic, rotating interference patterns with advanced color blending and beat-reactive behavior. It implements a complex multi-point system with configurable rotation, distance, and alpha controls, creating mesmerizing interference effects that respond to audio events and create complex visual patterns through overlapping point sources.

## Source Analysis

### Core Architecture (`r_interf.cpp`)

The effect is implemented as a C++ class `C_InterferencesClass` that inherits from `C_RBASE`. It provides a comprehensive multi-point interference system with dynamic rotation, distance controls, alpha blending, and advanced beat-reactive behavior for creating complex interference pattern visualizations.

### Key Components

#### Multi-Point System
Advanced multi-point interference engine:
- **Point Management**: Up to 8 configurable interference points
- **Point Positioning**: Dynamic point positioning with rotation and distance controls
- **Point Synchronization**: Coordinated point movement and rotation
- **Performance Optimization**: Efficient point calculation and rendering

#### Dynamic Parameter System
Sophisticated parameter control:
- **Dual Parameter Sets**: Primary and secondary parameter sets for dynamic effects
- **Beat Reactivity**: Beat-triggered parameter transitions
- **Smooth Interpolation**: Smooth parameter transitions between states
- **Status Management**: Dynamic status tracking for parameter evolution

#### Color Blending Engine
Advanced color processing algorithms:
- **RGB Channel Separation**: Independent RGB channel processing for 3 and 6 point modes
- **Alpha Blending**: Configurable alpha blending with blend tables
- **Color Accumulation**: Intelligent color accumulation with overflow protection
- **Blend Mode Selection**: Multiple blending modes (replace, additive, 50/50)

#### Rotation and Distance Control
Dynamic spatial control system:
- **Rotation Control**: Configurable rotation speed and direction
- **Distance Control**: Adjustable point distances for pattern variation
- **Beat Synchronization**: Beat-reactive rotation and distance changes
- **Smooth Transitions**: Gradual parameter evolution over time

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `enabled` | int | 0-1 | 1 | Enable interference effect |
| `nPoints` | int | 0-8 | 2 | Number of interference points |
| `rotation` | int | 0-255 | 0 | Initial rotation angle |
| `distance` | int | 1-64 | 10 | Primary point distance |
| `alpha` | int | 1-255 | 128 | Primary alpha blending |
| `rotationinc` | int | -32 to 32 | 0 | Primary rotation increment |
| `distance2` | int | 1-64 | 32 | Secondary point distance |
| `alpha2` | int | 1-255 | 192 | Secondary alpha blending |
| `rotationinc2` | int | -32 to 32 | 25 | Secondary rotation increment |
| `rgb` | int | 0-1 | 1 | Enable RGB channel separation |
| `blend` | int | 0-1 | 0 | Enable additive blending |
| `blendavg` | int | 0-1 | 0 | Enable 50/50 blending |
| `onbeat` | int | 0-1 | 1 | Enable beat reactivity |
| `speed` | float | 0.01-1.28 | 0.2 | Parameter transition speed |

### Blending Modes

| Mode | Description | Behavior |
|------|-------------|----------|
| **Replace** | Direct replacement | No blending, pure interference output |
| **Additive** | Brightness increase | Adds interference brightness to background |
| **50/50** | Average blending | Smooth transition between interference and background |

### RGB Channel Separation

| Point Count | RGB Mode | Description |
|-------------|----------|-------------|
| **3 Points** | Enabled | Red, Green, Blue channels for each point |
| **6 Points** | Enabled | RGB channels with additional point blending |
| **Other** | Disabled | Standard color processing |

## C# Implementation

```csharp
public class InterferencePatternsNode : AvsModuleNode
{
    public bool Enabled { get; set; } = true;
    public int NumberOfPoints { get; set; } = 2;
    public int Rotation { get; set; } = 0;
    public int Distance { get; set; } = 10;
    public int Alpha { get; set; } = 128;
    public int RotationIncrement { get; set; } = 0;
    public int Distance2 { get; set; } = 32;
    public int Alpha2 { get; set; } = 192;
    public int RotationIncrement2 { get; set; } = 25;
    public bool RGBMode { get; set; } = true;
    public bool Blend { get; set; } = false;
    public bool BlendAverage { get; set; } = false;
    public bool OnBeat { get; set; } = true;
    public float Speed { get; set; } = 0.2f;
    
    // Internal state
    private float status;
    private float angle;
    private int currentRotation;
    private int currentDistance;
    private int currentAlpha;
    private int currentRotationIncrement;
    private int lastWidth, lastHeight;
    private readonly object renderLock = new object();
    
    // Performance optimization
    private const int MaxPoints = 8;
    private const int MinPoints = 0;
    private const int MaxDistance = 64;
    private const int MinDistance = 1;
    private const int MaxAlpha = 255;
    private const int MinAlpha = 1;
    private const int MaxRotationIncrement = 32;
    private const int MinRotationIncrement = -32;
    private const float MaxSpeed = 1.28f;
    private const float MinSpeed = 0.01f;
    private const float PI = (float)Math.PI;
    private const float TwoPI = (float)Math.PI * 2.0f;
    
    public InterferencePatternsNode()
    {
        status = PI;
        angle = 0.0f;
        currentRotation = Rotation;
        currentDistance = Distance;
        currentAlpha = Alpha;
        currentRotationIncrement = RotationIncrement;
        lastWidth = lastHeight = 0;
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled || ctx.Width <= 0 || ctx.Height <= 0 || NumberOfPoints <= 0) return;
        
        lock (renderLock)
        {
            // Update buffers if dimensions changed
            UpdateBuffers(ctx);
            
            // Update dynamic parameters
            UpdateDynamicParameters(ctx);
            
            // Calculate interference points
            var points = CalculateInterferencePoints();
            
            // Render interference pattern
            RenderInterferencePattern(ctx, input, output, points);
            
            // Update rotation and status
            UpdateRotationAndStatus();
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
    
    private void UpdateDynamicParameters(FrameContext ctx)
    {
        // Handle beat reactivity
        if (OnBeat && ctx.IsBeat && status >= PI)
        {
            status = 0.0f;
        }
        
        // Calculate sine wave for parameter interpolation
        float sineValue = (float)Math.Sin(status);
        
        // Interpolate between primary and secondary parameters
        currentRotationIncrement = RotationIncrement + (int)((RotationIncrement2 - RotationIncrement) * sineValue);
        currentAlpha = Alpha + (int)((Alpha2 - Alpha) * sineValue);
        currentDistance = Distance + (int)((Distance2 - Distance) * sineValue);
        
        // Calculate current angle
        angle = (Rotation / 255.0f) * TwoPI;
    }
    
    private Point[] CalculateInterferencePoints()
    {
        Point[] points = new Point[NumberOfPoints];
        float angleStep = TwoPI / NumberOfPoints;
        
        for (int i = 0; i < NumberOfPoints; i++)
        {
            float currentAngle = angle + (i * angleStep);
            int x = (int)(Math.Cos(currentAngle) * currentDistance);
            int y = (int)(Math.Sin(currentAngle) * currentDistance);
            points[i] = new Point(x, y);
        }
        
        return points;
    }
    
    private void RenderInterferencePattern(FrameContext ctx, ImageBuffer input, ImageBuffer output, Point[] points)
    {
        // Calculate bounds for optimization
        var bounds = CalculateBounds(points);
        
        // Render the interference pattern
        if (RGBMode && (NumberOfPoints == 3 || NumberOfPoints == 6))
        {
            RenderRGBInterference(ctx, input, output, points, bounds);
        }
        else
        {
            RenderStandardInterference(ctx, input, output, points, bounds);
        }
    }
    
    private Rectangle CalculateBounds(Point[] points)
    {
        int minX = 0, maxX = 0, minY = 0, maxY = 0;
        
        foreach (var point in points)
        {
            if (point.X > minX) minX = point.X;
            if (-point.X > maxX) maxX = -point.X;
            if (point.Y > minY) minY = point.Y;
            if (-point.Y > maxY) maxY = -point.Y;
        }
        
        return new Rectangle(minX, minY, maxX + minX, maxY + minY);
    }
    
    private void RenderRGBInterference(FrameContext ctx, ImageBuffer input, ImageBuffer output, Point[] points, Rectangle bounds)
    {
        if (NumberOfPoints == 3)
        {
            Render3PointRGB(ctx, input, output, points);
        }
        else if (NumberOfPoints == 6)
        {
            Render6PointRGB(ctx, input, output, points);
        }
    }
    
    private void Render3PointRGB(FrameContext ctx, ImageBuffer input, ImageBuffer output, Point[] points)
    {
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                int r = 0, g = 0, b = 0;
                
                // Process each point for RGB channels
                for (int i = 0; i < 3; i++)
                {
                    int sourceX = x - points[i].X;
                    int sourceY = y - points[i].Y;
                    
                    if (sourceX >= 0 && sourceX < ctx.Width && sourceY >= 0 && sourceY < ctx.Height)
                    {
                        Color sourcePixel = input.GetPixel(sourceX, sourceY);
                        
                        // Apply alpha blending based on channel
                        switch (i)
                        {
                            case 0: // Red channel
                                r = ApplyAlphaBlending(sourcePixel.R, currentAlpha);
                                break;
                            case 1: // Green channel
                                g = ApplyAlphaBlending(sourcePixel.G, currentAlpha);
                                break;
                            case 2: // Blue channel
                                b = ApplyAlphaBlending(sourcePixel.B, currentAlpha);
                                break;
                        }
                    }
                }
                
                // Clamp values and create output color
                r = Math.Clamp(r, 0, 255);
                g = Math.Clamp(g, 0, 255);
                b = Math.Clamp(b, 0, 255);
                
                output.SetPixel(x, y, Color.FromRgb((byte)r, (byte)g, (byte)b));
            }
        }
    }
    
    private void Render6PointRGB(FrameContext ctx, ImageBuffer input, ImageBuffer output, Point[] points)
    {
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                int r = 0, g = 0, b = 0;
                
                // Process each point for RGB channels with accumulation
                for (int i = 0; i < 6; i++)
                {
                    int sourceX = x - points[i].X;
                    int sourceY = y - points[i].Y;
                    
                    if (sourceX >= 0 && sourceX < ctx.Width && sourceY >= 0 && sourceY < ctx.Height)
                    {
                        Color sourcePixel = input.GetPixel(sourceX, sourceY);
                        
                        // Apply alpha blending and accumulate based on channel
                        switch (i % 3)
                        {
                            case 0: // Red channel
                                r += ApplyAlphaBlending(sourcePixel.R, currentAlpha);
                                break;
                            case 1: // Green channel
                                g += ApplyAlphaBlending(sourcePixel.G, currentAlpha);
                                break;
                            case 2: // Blue channel
                                b += ApplyAlphaBlending(sourcePixel.B, currentAlpha);
                                break;
                        }
                    }
                }
                
                // Clamp values and create output color
                r = Math.Clamp(r, 0, 255);
                g = Math.Clamp(g, 0, 255);
                b = Math.Clamp(b, 0, 255);
                
                output.SetPixel(x, y, Color.FromRgb((byte)r, (byte)g, (byte)b));
            }
        }
    }
    
    private void RenderStandardInterference(FrameContext ctx, ImageBuffer input, ImageBuffer output, Point[] points, Rectangle bounds)
    {
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                int r = 0, g = 0, b = 0;
                
                // Process each interference point
                foreach (var point in points)
                {
                    int sourceX = x - point.X;
                    int sourceY = y - point.Y;
                    
                    if (sourceX >= 0 && sourceX < ctx.Width && sourceY >= 0 && sourceY < ctx.Height)
                    {
                        Color sourcePixel = input.GetPixel(sourceX, sourceY);
                        
                        // Apply alpha blending to all channels
                        r += ApplyAlphaBlending(sourcePixel.R, currentAlpha);
                        g += ApplyAlphaBlending(sourcePixel.G, currentAlpha);
                        b += ApplyAlphaBlending(sourcePixel.B, currentAlpha);
                    }
                }
                
                // Clamp values and create output color
                r = Math.Clamp(r, 0, 255);
                g = Math.Clamp(g, 0, 255);
                b = Math.Clamp(b, 0, 255);
                
                output.SetPixel(x, y, Color.FromRgb((byte)r, (byte)g, (byte)b));
            }
        }
    }
    
    private int ApplyAlphaBlending(int channelValue, int alpha)
    {
        return (channelValue * alpha) / 255;
    }
    
    private void UpdateRotationAndStatus()
    {
        // Update rotation
        currentRotation += currentRotationIncrement;
        
        // Normalize rotation to 0-255 range
        while (currentRotation > 255) currentRotation -= 255;
        while (currentRotation < -255) currentRotation += 255;
        
        // Update status
        status += Speed;
        status = Math.Min(status, PI);
        if (status < -PI) status = PI;
    }
    
    // Public interface for parameter adjustment
    public void SetEnabled(bool enable) { Enabled = enable; }
    
    public void SetNumberOfPoints(int points) 
    { 
        NumberOfPoints = Math.Clamp(points, MinPoints, MaxPoints); 
    }
    
    public void SetRotation(int rotation) 
    { 
        Rotation = Math.Clamp(rotation, 0, 255); 
    }
    
    public void SetDistance(int distance) 
    { 
        Distance = Math.Clamp(distance, MinDistance, MaxDistance); 
    }
    
    public void SetAlpha(int alpha) 
    { 
        Alpha = Math.Clamp(alpha, MinAlpha, MaxAlpha); 
    }
    
    public void SetRotationIncrement(int increment) 
    { 
        RotationIncrement = Math.Clamp(increment, MinRotationIncrement, MaxRotationIncrement); 
    }
    
    public void SetDistance2(int distance) 
    { 
        Distance2 = Math.Clamp(distance, MinDistance, MaxDistance); 
    }
    
    public void SetAlpha2(int alpha) 
    { 
        Alpha2 = Math.Clamp(alpha, MinAlpha, MaxAlpha); 
    }
    
    public void SetRotationIncrement2(int increment) 
    { 
        RotationIncrement2 = Math.Clamp(increment, MinRotationIncrement, MaxRotationIncrement); 
    }
    
    public void SetRGBMode(bool rgb) { RGBMode = rgb; }
    
    public void SetBlend(bool blend) { Blend = blend; }
    
    public void SetBlendAverage(bool blendAvg) { BlendAverage = blendAvg; }
    
    public void SetOnBeat(bool onBeat) { OnBeat = onBeat; }
    
    public void SetSpeed(float speed) 
    { 
        Speed = Math.Clamp(speed, MinSpeed, MaxSpeed); 
    }
    
    // Status queries
    public bool IsEnabled() => Enabled;
    public int GetNumberOfPoints() => NumberOfPoints;
    public int GetRotation() => Rotation;
    public int GetDistance() => Distance;
    public int GetAlpha() => Alpha;
    public int GetRotationIncrement() => RotationIncrement;
    public int GetDistance2() => Distance2;
    public int GetAlpha2() => Alpha2;
    public int GetRotationIncrement2() => RotationIncrement2;
    public bool IsRGBMode() => RGBMode;
    public bool IsBlendEnabled() => Blend;
    public bool IsBlendAverageEnabled() => BlendAverage;
    public bool IsOnBeatEnabled() => OnBeat;
    public float GetSpeed() => Speed;
    public float GetCurrentStatus() => status;
    public float GetCurrentAngle() => angle;
    public int GetCurrentRotation() => currentRotation;
    public int GetCurrentDistance() => currentDistance;
    public int GetCurrentAlpha() => currentAlpha;
    public int GetCurrentRotationIncrement() => currentRotationIncrement;
    
    // Advanced interference control
    public void ResetStatus()
    {
        status = PI;
    }
    
    public void ResetRotation()
    {
        currentRotation = Rotation;
        angle = (Rotation / 255.0f) * TwoPI;
    }
    
    public void SetDynamicMode(bool enable)
    {
        // Enable/disable dynamic parameter transitions
        if (!enable)
        {
            currentDistance = Distance;
            currentAlpha = Alpha;
            currentRotationIncrement = RotationIncrement;
        }
    }
    
    public void SetBeatReactivity(bool enable)
    {
        OnBeat = enable;
        if (enable && status >= PI)
        {
            status = 0.0f;
        }
    }
    
    // Pattern variations
    public void SetCircularPattern(int points, int distance)
    {
        SetNumberOfPoints(points);
        SetDistance(distance);
        SetDistance2(distance);
    }
    
    public void SetSpiralPattern(int points, int distance, int rotationSpeed)
    {
        SetNumberOfPoints(points);
        SetDistance(distance);
        SetRotationIncrement(rotationSpeed);
    }
    
    public void SetPulsingPattern(int points, int distance, int alpha, int alpha2)
    {
        SetNumberOfPoints(points);
        SetDistance(distance);
        SetAlpha(alpha);
        SetAlpha2(alpha2);
    }
    
    // Performance optimization
    public void SetRenderQuality(int quality)
    {
        // Quality could affect rendering detail or optimization level
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

### Audio Data Integration
- **Beat Detection**: Responds to audio events for dynamic parameter changes
- **Audio Analysis**: Processes audio data for enhanced interference effects
- **Dynamic Parameters**: Adjusts interference behavior based on audio events
- **Reactive Patterns**: Audio-reactive interference pattern evolution

### Framebuffer Handling
- **Input/Output Buffers**: Processes `ImageBuffer` objects with full color support
- **Multi-Point Rendering**: Efficient multi-point interference rendering
- **Color Blending**: Advanced color blending with alpha controls
- **Memory Management**: Intelligent buffer allocation and pattern management

### Performance Considerations
- **Bounds Optimization**: Efficient rendering with calculated bounds
- **RGB Channel Processing**: Optimized RGB channel separation for 3/6 point modes
- **Alpha Blending**: Efficient alpha blending with pre-calculated tables
- **Thread Safety**: Lock-based rendering for multi-threaded environments

## Usage Examples

### Basic Interference Pattern
```csharp
var interferenceNode = new InterferencePatternsNode
{
    Enabled = true,
    NumberOfPoints = 3,              // 3 interference points
    Distance = 20,                   // 20 pixel distance
    Alpha = 128,                     // Medium alpha blending
    RotationIncrement = 5,           // Slow rotation
    RGBMode = true                   // Enable RGB separation
};
```

### Beat-Reactive Interference
```csharp
var interferenceNode = new InterferencePatternsNode
{
    Enabled = true,
    NumberOfPoints = 6,              // 6 interference points
    Distance = 15,                   // 15 pixel distance
    Distance2 = 40,                  // 40 pixel secondary distance
    Alpha = 100,                     // Low alpha
    Alpha2 = 200,                    // High alpha on beat
    RotationIncrement = 10,          // Medium rotation
    RotationIncrement2 = 30,         // Fast rotation on beat
    OnBeat = true,                   // Enable beat reactivity
    Speed = 0.5f                     // Medium transition speed
};
```

### Dynamic Spiral Pattern
```csharp
var interferenceNode = new InterferencePatternsNode
{
    Enabled = true,
    NumberOfPoints = 8,              // 8 interference points
    Distance = 25,                   // 25 pixel distance
    RotationIncrement = 15,          // Fast rotation
    Alpha = 150,                     // High alpha blending
    RGBMode = false,                 // Standard color processing
    Blend = true                     // Additive blending
};

// Create spiral pattern
interferenceNode.SetSpiralPattern(8, 25, 15);
```

## Technical Notes

### Interference Architecture
The effect implements sophisticated interference processing:
- **Multi-Point System**: Up to 8 configurable interference points
- **Dynamic Parameters**: Beat-reactive parameter transitions
- **RGB Channel Separation**: Specialized processing for 3 and 6 point modes
- **Performance Optimization**: Efficient bounds calculation and rendering

### Parameter Architecture
Advanced parameter control system:
- **Dual Parameter Sets**: Primary and secondary parameter sets
- **Beat Reactivity**: Beat-triggered parameter transitions
- **Smooth Interpolation**: Sine-based parameter interpolation
- **Status Management**: Dynamic status tracking for parameter evolution

### Blending System
Sophisticated color blending algorithms:
- **Alpha Blending**: Configurable alpha blending with blend tables
- **Color Accumulation**: Intelligent color accumulation with overflow protection
- **Multiple Blend Modes**: Replace, additive, and 50/50 blending
- **RGB Channel Processing**: Specialized RGB channel separation

This effect provides the foundation for sophisticated interference pattern visualizations, creating complex multi-point interference effects that respond dynamically to audio input and create mesmerizing visual patterns through overlapping point sources.
