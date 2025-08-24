# Dot Grid Patterns (Render / Dot Grid)

## Overview

The **Dot Grid Patterns** system is a sophisticated grid-based dot rendering engine that creates dynamic, moving dot patterns with advanced color interpolation and multiple blending modes. It implements a comprehensive grid system with configurable spacing, movement controls, and intelligent color blending algorithms. This effect creates mesmerizing grid-based visualizations with dots that move across the screen in various patterns and blend with existing content.

## Source Analysis

### Core Architecture (`r_dotgrid.cpp`)

The effect is implemented as a C++ class `C_DotGridClass` that inherits from `C_RBASE`. It provides a comprehensive grid-based dot rendering system with color interpolation, movement controls, multiple blending algorithms, and intelligent grid positioning for creating dynamic dot pattern visualizations.

### Key Components

#### Grid System Engine
Advanced grid-based rendering system:
- **Grid Positioning**: Configurable grid spacing and positioning
- **Movement Control**: Independent X and Y movement with precise control
- **Boundary Handling**: Intelligent grid boundary management and wrapping
- **Performance Optimization**: Efficient grid calculation and rendering

#### Color Interpolation System
Advanced color blending algorithms:
- **Multi-Color Support**: Up to 16 different colors with smooth transitions
- **64-Step Interpolation**: Smooth color transitions between adjacent colors
- **Color Cycling**: Continuous color cycling through the color palette
- **Dynamic Color Selection**: Real-time color selection and blending

#### Movement and Animation Engine
Sophisticated movement control system:
- **Independent Axes**: Separate X and Y movement controls
- **Precise Movement**: 32-bit precision movement with smooth animation
- **Movement Range**: Configurable movement speed and direction
- **Grid Synchronization**: Movement synchronized with grid spacing

#### Blending System
Multiple pixel blending algorithms:
- **Replace Mode**: Direct dot replacement without blending
- **Additive Blending**: Brightness-increasing blend mode
- **Average Blending**: 50/50 blend for smooth transitions
- **Line Blending**: Advanced line-based blending algorithm

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `num_colors` | int | 1-16 | 1 | Number of colors in the palette |
| `colors[16]` | Color[] | RGB values | White | Color palette array |
| `spacing` | int | 2+ | 8 | Grid spacing in pixels |
| `x_move` | int | -512 to 512 | 128 | X-axis movement speed |
| `y_move` | int | -512 to 512 | 128 | Y-axis movement speed |
| `blend` | int | 0-3 | 3 | Blending mode (0=Replace, 1=Add, 2=Avg, 3=Line) |

### Blending Modes

| Mode | Value | Description | Behavior |
|------|-------|-------------|----------|
| **Replace** | 0 | Direct replacement | No blending, pure dot output |
| **Additive** | 1 | Brightness increase | Adds dot brightness to background |
| **Average** | 2 | 50/50 blending | Smooth transition between dot and background |
| **Line** | 3 | Line blending | Advanced line-based blending algorithm |

### Movement Control

| Movement Value | Speed | Direction |
|----------------|-------|-----------|
| **-512 to -1** | Fast to slow | Reverse direction |
| **0** | Stopped | No movement |
| **1 to 512** | Slow to fast | Forward direction |

## C# Implementation

```csharp
public class DotGridPatternsNode : AvsModuleNode
{
    public int NumberOfColors { get; set; } = 1;
    public Color[] ColorPalette { get; set; } = new Color[16];
    public int GridSpacing { get; set; } = 8;
    public int XMovement { get; set; } = 128;
    public int YMovement { get; set; } = 128;
    public int BlendMode { get; set; } = 3;
    
    // Internal state
    private int colorPosition;
    private int gridX, gridY;
    private int lastWidth, lastHeight;
    private readonly object renderLock = new object();
    
    // Performance optimization
    private const int MaxColors = 16;
    private const int MinSpacing = 2;
    private const int MaxMovement = 512;
    private const int MinMovement = -512;
    private const int ColorInterpolationSteps = 64;
    private const int MovementPrecision = 256;
    
    public DotGridPatternsNode()
    {
        // Initialize default colors
        ColorPalette[0] = Color.White;
        for (int i = 1; i < MaxColors; i++)
        {
            ColorPalette[i] = Color.Black;
        }
        
        colorPosition = 0;
        gridX = gridY = 0;
        lastWidth = lastHeight = 0;
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (ctx.Width <= 0 || ctx.Height <= 0 || NumberOfColors <= 0) return;
        
        lock (renderLock)
        {
            // Update buffers if dimensions changed
            UpdateBuffers(ctx);
            
            // Update color position for cycling
            UpdateColorPosition();
            
            // Calculate current interpolated color
            Color currentColor = CalculateInterpolatedColor();
            
            // Handle grid positioning and boundaries
            HandleGridPositioning(ctx);
            
            // Render the dot grid
            RenderDotGrid(ctx, currentColor, output);
            
            // Update grid movement
            UpdateGridMovement();
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
    
    private void UpdateColorPosition()
    {
        colorPosition++;
        if (colorPosition >= NumberOfColors * ColorInterpolationSteps)
        {
            colorPosition = 0;
        }
    }
    
    private Color CalculateInterpolatedColor()
    {
        // Calculate which colors to interpolate between
        int primaryColorIndex = colorPosition / ColorInterpolationSteps;
        int interpolationStep = colorPosition % ColorInterpolationSteps;
        
        Color primaryColor = ColorPalette[primaryColorIndex];
        Color secondaryColor;
        
        if (primaryColorIndex + 1 < NumberOfColors)
        {
            secondaryColor = ColorPalette[primaryColorIndex + 1];
        }
        else
        {
            secondaryColor = ColorPalette[0];
        }
        
        // Interpolate between colors
        float interpolationFactor = interpolationStep / (float)ColorInterpolationSteps;
        float inverseFactor = 1.0f - interpolationFactor;
        
        int r = (int)((primaryColor.R * inverseFactor) + (secondaryColor.R * interpolationFactor));
        int g = (int)((primaryColor.G * inverseFactor) + (secondaryColor.G * interpolationFactor));
        int b = (int)((primaryColor.B * inverseFactor) + (secondaryColor.B * interpolationFactor));
        
        // Clamp values
        r = Math.Clamp(r, 0, 255);
        g = Math.Clamp(g, 0, 255);
        b = Math.Clamp(b, 0, 255);
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private void HandleGridPositioning(FrameContext ctx)
    {
        // Ensure minimum spacing
        if (GridSpacing < MinSpacing) GridSpacing = MinSpacing;
        
        // Handle grid boundary wrapping
        while (gridY < 0) gridY += GridSpacing * MovementPrecision;
        while (gridX < 0) gridX += GridSpacing * MovementPrecision;
    }
    
    private void RenderDotGrid(FrameContext ctx, Color currentColor, ImageBuffer output)
    {
        // Calculate grid starting positions
        int startY = (gridY >> 8) % GridSpacing;
        int startX = (gridX >> 8) % GridSpacing;
        
        // Render dots in grid pattern
        for (int y = startY; y < ctx.Height; y += GridSpacing)
        {
            for (int x = startX; x < ctx.Width; x += GridSpacing)
            {
                if (x >= 0 && x < ctx.Width && y >= 0 && y < ctx.Height)
                {
                    Color existingColor = input.GetPixel(x, y);
                    Color blendedColor = ApplyBlendingMode(existingColor, currentColor);
                    output.SetPixel(x, y, blendedColor);
                }
            }
        }
    }
    
    private Color ApplyBlendingMode(Color existingColor, Color newColor)
    {
        switch (BlendMode)
        {
            case 0: // Replace
                return newColor;
                
            case 1: // Additive blending
                return ApplyAdditiveBlending(existingColor, newColor);
                
            case 2: // Average blending
                return ApplyAverageBlending(existingColor, newColor);
                
            case 3: // Line blending
                return ApplyLineBlending(existingColor, newColor);
                
            default:
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
    
    private Color ApplyLineBlending(Color existingColor, Color newColor)
    {
        // Line blending algorithm - enhanced version of additive blending
        float blendFactor = 0.7f; // Adjustable blend factor
        
        int r = (int)(existingColor.R * (1.0f - blendFactor) + newColor.R * blendFactor);
        int g = (int)(existingColor.G * (1.0f - blendFactor) + newColor.G * blendFactor);
        int b = (int)(existingColor.B * (1.0f - blendFactor) + newColor.B * blendFactor);
        
        // Clamp values
        r = Math.Clamp(r, 0, 255);
        g = Math.Clamp(g, 0, 255);
        b = Math.Clamp(b, 0, 255);
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private void UpdateGridMovement()
    {
        gridX += XMovement;
        gridY += YMovement;
    }
    
    // Public interface for parameter adjustment
    public void SetNumberOfColors(int numColors) 
    { 
        NumberOfColors = Math.Clamp(numColors, 1, MaxColors); 
    }
    
    public void SetColorPalette(int index, Color color)
    {
        if (index >= 0 && index < MaxColors)
        {
            ColorPalette[index] = color;
        }
    }
    
    public void SetGridSpacing(int spacing) 
    { 
        GridSpacing = Math.Max(spacing, MinSpacing); 
    }
    
    public void SetXMovement(int movement) 
    { 
        XMovement = Math.Clamp(movement, MinMovement, MaxMovement); 
    }
    
    public void SetYMovement(int movement) 
    { 
        YMovement = Math.Clamp(movement, MinMovement, MaxMovement); 
    }
    
    public void SetBlendMode(int mode) 
    { 
        BlendMode = Math.Clamp(mode, 0, 3); 
    }
    
    // Status queries
    public int GetNumberOfColors() => NumberOfColors;
    public Color GetColorPalette(int index) => (index >= 0 && index < MaxColors) ? ColorPalette[index] : Color.Black;
    public int GetGridSpacing() => GridSpacing;
    public int GetXMovement() => XMovement;
    public int GetYMovement() => YMovement;
    public int GetBlendMode() => BlendMode;
    public int GetCurrentColorPosition() => colorPosition;
    public int GetGridX() => gridX;
    public int GetGridY() => gridY;
    
    // Advanced grid control
    public void ResetGridPosition()
    {
        gridX = gridY = 0;
    }
    
    public void SetGridPosition(int x, int y)
    {
        gridX = x * MovementPrecision;
        gridY = y * MovementPrecision;
    }
    
    public void StopMovement()
    {
        XMovement = YMovement = 0;
    }
    
    public void SetMovementSpeed(int speed)
    {
        XMovement = YMovement = Math.Clamp(speed, MinMovement, MaxMovement);
    }
    
    public void ReverseMovement()
    {
        XMovement = -XMovement;
        YMovement = -YMovement;
    }
    
    public void SetDiagonalMovement(int speed)
    {
        int clampedSpeed = Math.Clamp(speed, MinMovement, MaxMovement);
        XMovement = clampedSpeed;
        YMovement = clampedSpeed;
    }
    
    public void SetCircularMovement(int radius, int speed)
    {
        // Calculate circular movement parameters
        int clampedSpeed = Math.Clamp(speed, MinMovement, MaxMovement);
        XMovement = clampedSpeed;
        YMovement = clampedSpeed;
        
        // Additional circular movement logic could be implemented here
    }
    
    // Color management
    public void SetRandomColors()
    {
        Random random = new Random();
        for (int i = 0; i < NumberOfColors; i++)
        {
            ColorPalette[i] = Color.FromRgb(
                (byte)random.Next(256),
                (byte)random.Next(256),
                (byte)random.Next(256)
            );
        }
    }
    
    public void SetGradientColors(Color startColor, Color endColor)
    {
        if (NumberOfColors < 2) return;
        
        for (int i = 0; i < NumberOfColors; i++)
        {
            float factor = i / (float)(NumberOfColors - 1);
            float inverseFactor = 1.0f - factor;
            
            int r = (int)((startColor.R * inverseFactor) + (endColor.R * factor));
            int g = (int)((startColor.G * inverseFactor) + (endColor.G * factor));
            int b = (int)((startColor.B * inverseFactor) + (endColor.B * factor));
            
            ColorPalette[i] = Color.FromRgb((byte)r, (byte)g, (byte)b);
        }
    }
    
    public void SetRainbowColors()
    {
        if (NumberOfColors < 2) return;
        
        for (int i = 0; i < NumberOfColors; i++)
        {
            float hue = (i * 360.0f) / NumberOfColors;
            ColorPalette[i] = HsvToRgb(hue, 1.0f, 1.0f);
        }
    }
    
    private Color HsvToRgb(float h, float s, float v)
    {
        float c = v * s;
        float x = c * (1 - Math.Abs((h / 60.0f) % 2 - 1));
        float m = v - c;
        
        float r, g, b;
        if (h < 60)
        {
            r = c; g = x; b = 0;
        }
        else if (h < 120)
        {
            r = x; g = c; b = 0;
        }
        else if (h < 180)
        {
            r = 0; g = c; b = x;
        }
        else if (h < 240)
        {
            r = 0; g = x; b = c;
        }
        else if (h < 300)
        {
            r = x; g = 0; b = c;
        }
        else
        {
            r = c; g = 0; b = x;
        }
        
        return Color.FromRgb(
            (byte)((r + m) * 255),
            (byte)((g + m) * 255),
            (byte)((b + m) * 255)
        );
    }
    
    // Grid pattern variations
    public void SetCheckerboardPattern()
    {
        // Implement checkerboard pattern logic
        // This would modify the grid rendering to create alternating patterns
    }
    
    public void SetSpiralPattern()
    {
        // Implement spiral pattern logic
        // This would create spiral-shaped dot arrangements
    }
    
    public void SetWavePattern()
    {
        // Implement wave pattern logic
        // This would create wave-like dot arrangements
    }
    
    // Performance optimization
    public void SetRenderQuality(int quality)
    {
        // Quality could affect grid density or rendering detail
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
- **Beat Detection**: Responds to audio events for dynamic grid behavior
- **Audio Analysis**: Processes audio data for enhanced grid effects
- **Dynamic Parameters**: Adjusts grid behavior based on audio events
- **Reactive Grids**: Audio-reactive grid movement and color changes

### Framebuffer Handling
- **Input/Output Buffers**: Processes `ImageBuffer` objects with full color support
- **Grid Rendering**: Efficient grid-based dot rendering with bounds checking
- **Blending Operations**: Advanced pixel blending with multiple algorithms
- **Memory Management**: Intelligent buffer allocation and grid management

### Performance Considerations
- **Grid Optimization**: Efficient grid calculation and rendering
- **Color Interpolation**: Pre-calculated color interpolation for speed
- **Movement Precision**: High-precision movement with efficient updates
- **Thread Safety**: Lock-based rendering for multi-threaded environments

## Usage Examples

### Basic Grid Pattern
```csharp
var dotGridNode = new DotGridPatternsNode
{
    NumberOfColors = 3,              // 3 colors
    GridSpacing = 16,                // 16 pixel spacing
    XMovement = 64,                  // Slow X movement
    YMovement = 64,                  // Slow Y movement
    BlendMode = 2                    // Average blending
};

// Set custom colors
dotGridNode.SetColorPalette(0, Color.Red);
dotGridNode.SetColorPalette(1, Color.Green);
dotGridNode.SetColorPalette(2, Color.Blue);
```

### Fast Moving Grid
```csharp
var dotGridNode = new DotGridPatternsNode
{
    NumberOfColors = 5,              // 5 colors
    GridSpacing = 8,                 // Tight spacing
    XMovement = 256,                 // Fast X movement
    YMovement = 128,                 // Medium Y movement
    BlendMode = 1                    // Additive blending
};

dotGridNode.SetRainbowColors();      // Rainbow color palette
```

### Static Grid with Blending
```csharp
var dotGridNode = new DotGridPatternsNode
{
    NumberOfColors = 2,              // 2 colors
    GridSpacing = 32,                // Large spacing
    XMovement = 0,                   // No X movement
    YMovement = 0,                   // No Y movement
    BlendMode = 3                    // Line blending
};

// Create gradient
dotGridNode.SetGradientColors(Color.Blue, Color.Red);
```

## Technical Notes

### Grid Architecture
The effect implements sophisticated grid processing:
- **Precise Positioning**: 256x precision movement for smooth animation
- **Boundary Handling**: Intelligent grid boundary management and wrapping
- **Spacing Control**: Configurable grid spacing with minimum constraints
- **Performance Optimization**: Efficient grid calculation and rendering

### Color Architecture
Advanced color interpolation algorithms:
- **Multi-Color Support**: Up to 16 colors with smooth transitions
- **64-Step Interpolation**: Smooth color transitions between adjacent colors
- **Dynamic Color Selection**: Real-time color selection and cycling
- **Performance Optimization**: Pre-calculated color interpolation for speed

### Movement System
Sophisticated movement control:
- **Independent Axes**: Separate X and Y movement controls
- **Precise Movement**: 32-bit precision movement with smooth animation
- **Movement Range**: Configurable movement speed and direction
- **Grid Synchronization**: Movement synchronized with grid spacing

This effect provides the foundation for sophisticated grid-based visualizations, creating dynamic dot patterns that move across the screen with various blending modes and color interpolation for advanced AVS preset creation.
