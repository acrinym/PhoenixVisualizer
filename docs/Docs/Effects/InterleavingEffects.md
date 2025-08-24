# Interleaving Effects (Trans / Interleave)

## Overview

The **Interleaving Effects** system is a sophisticated pattern interleaving engine that creates dynamic, alternating pattern visualizations with advanced color blending and beat-reactive behavior. It implements a complex interleaving system with configurable X and Y pattern spacing, dynamic color application, and intelligent beat-reactive pattern transitions. This effect creates mesmerizing alternating pattern visualizations that respond to audio events and create complex visual rhythms through pattern interleaving.

## Source Analysis

### Core Architecture (`r_interleave.cpp`)

The effect is implemented as a C++ class `C_InterleaveClass` that inherits from `C_RBASE`. It provides a comprehensive interleaving system with dynamic pattern spacing, color controls, multiple blending modes, and advanced beat-reactive behavior for creating complex alternating pattern visualizations.

### Key Components

#### Pattern Interleaving Engine
Advanced pattern interleaving system:
- **X/Y Pattern Spacing**: Configurable horizontal and vertical pattern spacing
- **Pattern Alternation**: Intelligent alternating pattern rendering
- **Dynamic Spacing**: Beat-reactive pattern spacing changes
- **Performance Optimization**: Efficient pattern calculation and rendering

#### Dynamic Parameter System
Sophisticated parameter control:
- **Dual Parameter Sets**: Primary and secondary pattern spacing for dynamic effects
- **Beat Reactivity**: Beat-triggered parameter transitions
- **Smooth Interpolation**: Smooth parameter transitions between states
- **Status Management**: Dynamic status tracking for parameter evolution

#### Color Blending Engine
Advanced color processing algorithms:
- **Color Application**: Configurable color application to interleaved patterns
- **Multiple Blend Modes**: Replace, additive, and 50/50 blending options
- **Pattern Color Integration**: Intelligent color integration with pattern structure
- **Blend Mode Selection**: Flexible blending mode configuration

#### Beat Synchronization System
Dynamic audio integration:
- **Beat Detection**: Beat-reactive pattern parameter changes
- **Parameter Transitions**: Smooth transitions between beat states
- **Duration Control**: Configurable beat duration for pattern evolution
- **Dynamic Response**: Real-time adjustment of pattern behavior

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `enabled` | int | 0-1 | 1 | Enable interleaving effect |
| `x` | int | 0-64 | 1 | Primary X pattern spacing |
| `y` | int | 0-64 | 1 | Primary Y pattern spacing |
| `color` | int | RGB values | 0 | Pattern color |
| `blend` | int | 0-1 | 0 | Enable additive blending |
| `blendavg` | int | 0-1 | 0 | Enable 50/50 blending |
| `onbeat` | int | 0-1 | 0 | Enable beat reactivity |
| `x2` | int | 0-64 | 1 | Secondary X pattern spacing |
| `y2` | int | 0-64 | 1 | Secondary Y pattern spacing |
| `beatdur` | int | 1-64 | 4 | Beat duration for transitions |

### Blending Modes

| Mode | Description | Behavior |
|------|-------------|----------|
| **Replace** | Direct replacement | No blending, pure pattern color output |
| **Additive** | Brightness increase | Adds pattern color brightness to background |
| **50/50** | Average blending | Smooth transition between pattern and background |

### Pattern Spacing

| Spacing Value | Pattern Behavior |
|---------------|------------------|
| **0** | No pattern (pure color or background) |
| **1-64** | Pattern spacing in pixels |
| **Dynamic** | Beat-reactive spacing changes |

## C# Implementation

```csharp
public class InterleavingEffectsNode : AvsModuleNode
{
    public bool Enabled { get; set; } = true;
    public int XSpacing { get; set; } = 1;
    public int YSpacing { get; set; } = 1;
    public Color PatternColor { get; set; } = Color.Black;
    public bool Blend { get; set; } = false;
    public bool BlendAverage { get; set; } = false;
    public bool OnBeat { get; set; } = false;
    public int XSpacing2 { get; set; } = 1;
    public int YSpacing2 { get; set; } = 1;
    public int BeatDuration { get; set; } = 4;
    
    // Internal state
    private double currentX, currentY;
    private int lastWidth, lastHeight;
    private readonly object renderLock = new object();
    
    // Performance optimization
    private const int MaxSpacing = 64;
    private const int MinSpacing = 0;
    private const int MaxBeatDuration = 64;
    private const int MinBeatDuration = 1;
    private const double SmoothingFactor = 512.0;
    private const double SmoothingOffset = 64.0;
    
    public InterleavingEffectsNode()
    {
        currentX = XSpacing;
        currentY = YSpacing;
        lastWidth = lastHeight = 0;
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled || ctx.Width <= 0 || ctx.Height <= 0) return;
        
        lock (renderLock)
        {
            // Update buffers if dimensions changed
            UpdateBuffers(ctx);
            
            // Update dynamic parameters
            UpdateDynamicParameters(ctx);
            
            // Render interleaving pattern
            RenderInterleavingPattern(ctx, input, output);
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
        // Calculate smoothing factor for parameter transitions
        double smoothingFactor = (BeatDuration + SmoothingFactor - SmoothingOffset) / SmoothingFactor;
        
        // Smoothly interpolate between primary and secondary parameters
        currentX = (currentX * smoothingFactor) + (XSpacing * (1.0 - smoothingFactor));
        currentY = (currentY * smoothingFactor) + (YSpacing * (1.0 - smoothingFactor));
        
        // Handle beat reactivity
        if (ctx.IsBeat && OnBeat)
        {
            currentX = XSpacing2;
            currentY = YSpacing2;
        }
    }
    
    private void RenderInterleavingPattern(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        // Convert current spacing to integers
        int targetX = (int)currentX;
        int targetY = (int)currentY;
        
        // Calculate pattern offsets for centering
        int xOffset = 0;
        int yOffset = 0;
        
        if (targetX > 0)
        {
            xOffset = (ctx.Width % targetX) / 2;
        }
        
        if (targetY > 0)
        {
            yOffset = (ctx.Height % targetY) / 2;
        }
        
        // Render the interleaving pattern
        if (targetX >= 0 && targetY >= 0)
        {
            RenderPattern(ctx, input, output, targetX, targetY, xOffset, yOffset);
        }
    }
    
    private void RenderPattern(FrameContext ctx, ImageBuffer input, ImageBuffer output, int xSpacing, int ySpacing, int xOffset, int yOffset)
    {
        int yStatus = 0;
        int yPosition = 0;
        
        // Process each line
        for (int y = 0; y < ctx.Height; y++)
        {
            int xStatus = 0;
            
            // Update Y pattern status
            if (ySpacing > 0)
            {
                yPosition++;
                if (yPosition >= ySpacing)
                {
                    yStatus = !yStatus;
                    yPosition = 0;
                }
            }
            
            // Render this line
            if (!yStatus)
            {
                // This line is pure color
                RenderColorLine(ctx, output, y, PatternColor);
            }
            else if (xSpacing > 0)
            {
                // This line has X pattern interleaving
                RenderInterleavedLine(ctx, input, output, y, xSpacing, xOffset, PatternColor);
            }
            else
            {
                // Skip this line (no pattern)
                continue;
            }
        }
    }
    
    private void RenderColorLine(FrameContext ctx, ImageBuffer output, int y, Color color)
    {
        for (int x = 0; x < ctx.Width; x++)
        {
            Color existingColor = output.GetPixel(x, y);
            Color blendedColor = ApplyBlendingMode(existingColor, color);
            output.SetPixel(x, y, blendedColor);
        }
    }
    
    private void RenderInterleavedLine(FrameContext ctx, ImageBuffer input, ImageBuffer output, int y, int xSpacing, int xOffset, Color color)
    {
        int xPosition = xOffset;
        int remainingWidth = ctx.Width;
        
        while (remainingWidth > 0)
        {
            // Calculate segment length
            int segmentLength = Math.Min(remainingWidth, xSpacing - xPosition);
            xPosition = 0;
            remainingWidth -= segmentLength;
            
            if (xStatus)
            {
                // Skip this segment (background)
                xStatus = !xStatus;
            }
            else
            {
                // Apply color to this segment
                for (int i = 0; i < segmentLength; i++)
                {
                    int x = ctx.Width - remainingWidth - segmentLength + i;
                    if (x >= 0 && x < ctx.Width)
                    {
                        Color existingColor = output.GetPixel(x, y);
                        Color blendedColor = ApplyBlendingMode(existingColor, color);
                        output.SetPixel(x, y, blendedColor);
                    }
                }
                xStatus = !xStatus;
            }
        }
    }
    
    private Color ApplyBlendingMode(Color existingColor, Color newColor)
    {
        if (Blend)
        {
            return ApplyAdditiveBlending(existingColor, newColor);
        }
        else if (BlendAverage)
        {
            return ApplyAverageBlending(existingColor, newColor);
        }
        else
        {
            return newColor;
        }
    }
    
    private Color ApplyAdditiveBlending(Color existingColor, Color newColor)
    {
        int r = Math.Min(255, existingColor.R + newColor.R);
        int g = Math.Min(255, existingColor.G + newColor.G);
        int b = Math.Min(255, existingColor.B + newColor.B);
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private Color ApplyAverageBlending(Color existingColor, Color newColor)
    {
        int r = (existingColor.R + newColor.R) / 2;
        int g = (existingColor.G + newColor.G) / 2;
        int b = (existingColor.B + newColor.B) / 2;
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    // Public interface for parameter adjustment
    public void SetEnabled(bool enable) { Enabled = enable; }
    
    public void SetXSpacing(int spacing) 
    { 
        XSpacing = Math.Clamp(spacing, MinSpacing, MaxSpacing); 
    }
    
    public void SetYSpacing(int spacing) 
    { 
        YSpacing = Math.Clamp(spacing, MinSpacing, MaxSpacing); 
    }
    
    public void SetPatternColor(Color color) { PatternColor = color; }
    
    public void SetBlend(bool blend) { Blend = blend; }
    
    public void SetBlendAverage(bool blendAvg) { BlendAverage = blendAvg; }
    
    public void SetOnBeat(bool onBeat) { OnBeat = onBeat; }
    
    public void SetXSpacing2(int spacing) 
    { 
        XSpacing2 = Math.Clamp(spacing, MinSpacing, MaxSpacing); 
    }
    
    public void SetYSpacing2(int spacing) 
    { 
        YSpacing2 = Math.Clamp(spacing, MinSpacing, MaxSpacing); 
    }
    
    public void SetBeatDuration(int duration) 
    { 
        BeatDuration = Math.Clamp(duration, MinBeatDuration, MaxBeatDuration); 
    }
    
    // Status queries
    public bool IsEnabled() => Enabled;
    public int GetXSpacing() => XSpacing;
    public int GetYSpacing() => YSpacing;
    public Color GetPatternColor() => PatternColor;
    public bool IsBlendEnabled() => Blend;
    public bool IsBlendAverageEnabled() => BlendAverage;
    public bool IsOnBeatEnabled() => OnBeat;
    public int GetXSpacing2() => XSpacing2;
    public int GetYSpacing2() => YSpacing2;
    public int GetBeatDuration() => BeatDuration;
    public double GetCurrentX() => currentX;
    public double GetCurrentY() => currentY;
    
    // Advanced interleaving control
    public void ResetPattern()
    {
        currentX = XSpacing;
        currentY = YSpacing;
    }
    
    public void SetPatternSpacing(int x, int y)
    {
        SetXSpacing(x);
        SetYSpacing(y);
        currentX = x;
        currentY = y;
    }
    
    public void SetSecondarySpacing(int x, int y)
    {
        SetXSpacing2(x);
        SetYSpacing2(y);
    }
    
    public void SetBeatReactivity(bool enable)
    {
        OnBeat = enable;
        if (enable)
        {
            ResetPattern();
        }
    }
    
    // Pattern variations
    public void SetCheckerboardPattern(int spacing)
    {
        SetPatternSpacing(spacing, spacing);
        SetSecondarySpacing(spacing, spacing);
    }
    
    public void SetStripedPattern(int xSpacing, int ySpacing)
    {
        SetPatternSpacing(xSpacing, ySpacing);
        SetSecondarySpacing(xSpacing, ySpacing);
    }
    
    public void SetDynamicPattern(int x1, int y1, int x2, int y2)
    {
        SetPatternSpacing(x1, y1);
        SetSecondarySpacing(x2, y2);
        SetOnBeat(true);
    }
    
    public void SetPulsingPattern(int baseSpacing, int pulseSpacing, int duration)
    {
        SetPatternSpacing(baseSpacing, baseSpacing);
        SetSecondarySpacing(pulseSpacing, pulseSpacing);
        SetBeatDuration(duration);
        SetOnBeat(true);
    }
    
    // Color management
    public void SetRandomColor()
    {
        Random random = new Random();
        PatternColor = Color.FromRgb(
            (byte)random.Next(256),
            (byte)random.Next(256),
            (byte)random.Next(256)
        );
    }
    
    public void SetGradientColor(Color startColor, Color endColor, float factor)
    {
        factor = Math.Clamp(factor, 0.0f, 1.0f);
        float inverseFactor = 1.0f - factor;
        
        int r = (int)((startColor.R * inverseFactor) + (endColor.R * factor));
        int g = (int)((startColor.G * inverseFactor) + (endColor.G * factor));
        int b = (int)((startColor.B * inverseFactor) + (endColor.B * factor));
        
        PatternColor = Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    // Performance optimization
    public void SetRenderQuality(int quality)
    {
        // Quality could affect pattern detail or optimization level
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
- **Beat Detection**: Responds to audio events for dynamic pattern changes
- **Audio Analysis**: Processes audio data for enhanced interleaving effects
- **Dynamic Parameters**: Adjusts pattern behavior based on audio events
- **Reactive Patterns**: Audio-reactive pattern spacing evolution

### Framebuffer Handling
- **Input/Output Buffers**: Processes `ImageBuffer` objects with full color support
- **Pattern Rendering**: Efficient pattern interleaving with bounds checking
- **Color Blending**: Advanced color blending with multiple algorithms
- **Memory Management**: Intelligent buffer allocation and pattern management

### Performance Considerations
- **Pattern Optimization**: Efficient pattern calculation and rendering
- **Blending Operations**: Optimized color blending for different modes
- **Dynamic Parameters**: Efficient parameter interpolation and updates
- **Thread Safety**: Lock-based rendering for multi-threaded environments

## Usage Examples

### Basic Interleaving Pattern
```csharp
var interleavingNode = new InterleavingEffectsNode
{
    Enabled = true,
    XSpacing = 16,                    // 16 pixel X spacing
    YSpacing = 16,                    // 16 pixel Y spacing
    PatternColor = Color.Red,         // Red pattern color
    Blend = false,                    // No blending
    OnBeat = false                    // No beat reactivity
};
```

### Beat-Reactive Interleaving
```csharp
var interleavingNode = new InterleavingEffectsNode
{
    Enabled = true,
    XSpacing = 8,                     // 8 pixel X spacing
    YSpacing = 8,                     // 8 pixel Y spacing
    XSpacing2 = 32,                  // 32 pixel secondary X spacing
    YSpacing2 = 32,                  // 32 pixel secondary Y spacing
    PatternColor = Color.Blue,        // Blue pattern color
    Blend = true,                     // Additive blending
    OnBeat = true,                    // Enable beat reactivity
    BeatDuration = 8                  // 8 frame beat duration
};
```

### Dynamic Checkerboard Pattern
```csharp
var interleavingNode = new InterleavingEffectsNode
{
    Enabled = true,
    PatternColor = Color.Green,       // Green pattern color
    BlendAverage = true,              // 50/50 blending
    OnBeat = true                     // Enable beat reactivity
};

// Create checkerboard pattern
interleavingNode.SetCheckerboardPattern(24);
```

## Technical Notes

### Interleaving Architecture
The effect implements sophisticated pattern processing:
- **Pattern Management**: Configurable X and Y pattern spacing
- **Dynamic Parameters**: Beat-reactive parameter transitions
- **Pattern Alternation**: Intelligent alternating pattern rendering
- **Performance Optimization**: Efficient pattern calculation and rendering

### Parameter Architecture
Advanced parameter control system:
- **Dual Parameter Sets**: Primary and secondary pattern spacing
- **Beat Reactivity**: Beat-triggered parameter transitions
- **Smooth Interpolation**: Smooth parameter transitions between states
- **Status Management**: Dynamic status tracking for parameter evolution

### Blending System
Sophisticated color blending algorithms:
- **Multiple Blend Modes**: Replace, additive, and 50/50 blending
- **Pattern Integration**: Intelligent color integration with pattern structure
- **Color Management**: Flexible color application and management
- **Performance Optimization**: Efficient blending operations

This effect provides the foundation for sophisticated pattern interleaving visualizations, creating complex alternating patterns that respond dynamically to audio input and create mesmerizing visual rhythms through intelligent pattern interleaving.
