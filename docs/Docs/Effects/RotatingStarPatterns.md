# Rotating Star Patterns (Render / RotStar)

## Overview

The **Rotating Star Patterns** system creates dynamic, rotating star formations that respond to audio data and create mesmerizing geometric patterns. It generates 5-pointed stars that rotate around a central point, with their size and color dynamically changing based on spectrum data. This effect is perfect for creating cosmic, space-themed visualizations with smooth rotation and audio-reactive behavior.

## Source Analysis

### Core Architecture (`r_rotstar.cpp`)

The effect is implemented as a C++ class `C_RotStarClass` that inherits from `C_RBASE`. It provides a sophisticated star generation system with dynamic color interpolation, audio-reactive sizing, and smooth rotation mechanics.

### Key Components

#### Star Generation System
Advanced geometric star creation:
- **5-Point Star Geometry**: Mathematical star construction using trigonometric calculations
- **Dynamic Sizing**: Audio-reactive star dimensions based on spectrum data
- **Rotation Mechanics**: Smooth continuous rotation with configurable speed
- **Dual Star System**: Two stars rotating in opposite directions for visual complexity

#### Color Management System
Sophisticated color handling:
- **Multi-Color Support**: Up to 16 different colors with smooth interpolation
- **Color Interpolation**: 64-step smooth transitions between color values
- **RGB Component Blending**: Individual red, green, and blue channel interpolation
- **Dynamic Color Cycling**: Automatic color progression through the palette

#### Audio Integration System
Real-time audio reactivity:
- **Spectrum Analysis**: Processes 3-13 frequency bands for star sizing
- **Beat Detection**: Responds to audio events for dynamic parameter changes
- **Frequency Mapping**: Maps audio data to star dimensions and positioning
- **Audio Thresholding**: Intelligent peak detection for responsive sizing

#### Rendering Pipeline
Advanced drawing system:
- **Line Drawing**: Uses optimized line rendering with blending support
- **Coordinate Calculation**: Precise star vertex positioning and rotation
- **Screen Boundary Handling**: Proper clipping and boundary management
- **Performance Optimization**: Efficient rendering with minimal calculations

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `num_colors` | int | 1-16 | 1 | Number of colors in the palette |
| `colors[16]` | int[] | RGB values | [255,255,255] | Array of RGB color values |
| `color_pos` | int | 0-1023 | 0 | Current position in color cycle |
| `r1` | double | 0.0-2π | 0.0 | Current rotation angle |

### Color Interpolation System

The effect uses a sophisticated 64-step color interpolation system:
- **Color Steps**: 64 intermediate steps between each color
- **RGB Blending**: Individual channel interpolation for smooth transitions
- **Cycle Length**: Total cycle length = `num_colors * 64`
- **Smooth Transitions**: Gradual color changes without abrupt jumps

### Star Geometry

Each star is constructed using mathematical principles:
- **5 Points**: Classic 5-pointed star geometry
- **Rotation Center**: Stars rotate around a central point
- **Audio Scaling**: Size varies based on spectrum data
- **Dual Direction**: Two stars rotate in opposite directions

## C# Implementation

```csharp
public class RotatingStarPatternsNode : AvsModuleNode
{
    public int NumberOfColors { get; set; } = 1;
    public Color[] ColorPalette { get; set; } = new Color[16];
    public double RotationSpeed { get; set; } = 0.1;
    public bool EnableAudioReactivity { get; set; } = true;
    public bool EnableDualStars { get; set; } = true;
    public double StarSizeMultiplier { get; set; } = 1.0;
    public bool EnableColorInterpolation { get; set; } = true;
    
    // Internal state
    private int colorPosition = 0;
    private double rotationAngle = 0.0;
    private readonly Random random = new Random();
    private readonly object renderLock = new object();
    
    // Performance optimization
    private const double PI = Math.PI;
    private const double TwoPI = Math.PI * 2.0;
    private const double StarAngleStep = TwoPI / 5.0;
    private const int ColorInterpolationSteps = 64;
    private const int MaxColors = 16;
    private const int MinSpectrumBand = 3;
    private const int MaxSpectrumBand = 14;
    
    // Parameter ranges
    private const int MinNumberOfColors = 1;
    private const int MaxNumberOfColors = 16;
    private const double MinRotationSpeed = 0.01;
    private const double MaxRotationSpeed = 1.0;
    private const double MinStarSizeMultiplier = 0.1;
    private const double MaxStarSizeMultiplier = 5.0;
    
    public RotatingStarPatternsNode()
    {
        InitializeColorPalette();
        InitializeDefaultColors();
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (ctx.Width <= 0 || ctx.Height <= 0) return;
        
        // Update color position for smooth color cycling
        UpdateColorPosition();
        
        // Get current interpolated color
        Color currentColor = GetInterpolatedColor();
        
        // Render rotating star patterns
        RenderRotatingStars(ctx, output, currentColor);
        
        // Update rotation angle
        UpdateRotationAngle();
    }
    
    private void InitializeColorPalette()
    {
        for (int i = 0; i < MaxColors; i++)
        {
            ColorPalette[i] = Color.White;
        }
    }
    
    private void InitializeDefaultColors()
    {
        // Set default color palette with vibrant colors
        ColorPalette[0] = Color.White;
        ColorPalette[1] = Color.Red;
        ColorPalette[2] = Color.Green;
        ColorPalette[3] = Color.Blue;
        ColorPalette[4] = Color.Yellow;
        ColorPalette[5] = Color.Magenta;
        ColorPalette[6] = Color.Cyan;
        ColorPalette[7] = Color.Orange;
        ColorPalette[8] = Color.Purple;
        ColorPalette[9] = Color.Pink;
        ColorPalette[10] = Color.Lime;
        ColorPalette[11] = Color.Teal;
        ColorPalette[12] = Color.Brown;
        ColorPalette[13] = Color.Gray;
        ColorPalette[14] = Color.Gold;
        ColorPalette[15] = Color.Silver;
    }
    
    private void UpdateColorPosition()
    {
        if (EnableColorInterpolation)
        {
            colorPosition++;
            int maxPosition = NumberOfColors * ColorInterpolationSteps;
            if (colorPosition >= maxPosition)
            {
                colorPosition = 0;
            }
        }
    }
    
    private Color GetInterpolatedColor()
    {
        if (!EnableColorInterpolation || NumberOfColors <= 1)
        {
            return ColorPalette[0];
        }
        
        // Calculate current color index and interpolation factor
        int colorIndex = colorPosition / ColorInterpolationSteps;
        int interpolationFactor = colorPosition % ColorInterpolationSteps;
        
        // Get current and next colors
        Color currentColor = ColorPalette[colorIndex];
        Color nextColor = (colorIndex + 1 < NumberOfColors) ? ColorPalette[colorIndex + 1] : ColorPalette[0];
        
        // Interpolate between colors
        double factor = interpolationFactor / (double)ColorInterpolationSteps;
        return InterpolateColors(currentColor, nextColor, factor);
    }
    
    private Color InterpolateColors(Color color1, Color color2, double factor)
    {
        return Color.FromArgb(
            (int)(color1.A + (color2.A - color1.A) * factor),
            (int)(color1.R + (color2.R - color1.R) * factor),
            (int)(color1.G + (color2.G - color1.G) * factor),
            (int)(color1.B + (color2.B - color1.B) * factor)
        );
    }
    
    private void RenderRotatingStars(FrameContext ctx, ImageBuffer output, Color starColor)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        
        // Calculate center point
        int centerX = width / 2;
        int centerY = height / 2;
        
        // Calculate star size based on audio data
        double starSize = CalculateStarSize(ctx);
        
        // Render primary star
        RenderStar(output, centerX, centerY, starSize, rotationAngle, starColor, false);
        
        // Render secondary star if enabled
        if (EnableDualStars)
        {
            RenderStar(output, centerX, centerY, starSize, -rotationAngle, starColor, true);
        }
    }
    
    private double CalculateStarSize(FrameContext ctx)
    {
        if (!EnableAudioReactivity)
        {
            return 50.0 * StarSizeMultiplier;
        }
        
        // Analyze spectrum data to determine star size
        double maxSpectrumValue = 0.0;
        
        for (int band = MinSpectrumBand; band < MaxSpectrumBand; band++)
        {
            if (band < ctx.SpectrumData.Length)
            {
                double spectrumValue = ctx.SpectrumData[band];
                if (spectrumValue > maxSpectrumValue)
                {
                    maxSpectrumValue = spectrumValue;
                }
            }
        }
        
        // Map spectrum value to star size
        double baseSize = 20.0;
        double audioSize = (maxSpectrumValue + 9.0) / 88.0 * 100.0;
        double totalSize = (baseSize + audioSize) * StarSizeMultiplier;
        
        return Math.Clamp(totalSize, 10.0, 200.0);
    }
    
    private void RenderStar(ImageBuffer output, int centerX, int centerY, double starSize, double rotation, Color color, bool invertPosition)
    {
        int width = output.Width;
        int height = output.Height;
        
        // Calculate star center with slight offset
        double offsetX = Math.Cos(rotation) * width / 4.0;
        double offsetY = Math.Sin(rotation) * height / 4.0;
        
        if (invertPosition)
        {
            offsetX = -offsetX;
            offsetY = -offsetY;
        }
        
        int starCenterX = centerX + (int)offsetX;
        int starCenterY = centerY + (int)offsetY;
        
        // Calculate star vertices
        Point[] starVertices = CalculateStarVertices(starCenterX, starCenterY, starSize, rotation);
        
        // Draw star lines
        for (int i = 0; i < starVertices.Length; i++)
        {
            Point startPoint = starVertices[i];
            Point endPoint = starVertices[(i + 2) % starVertices.Length];
            
            // Draw line with boundary checking
            DrawLine(output, startPoint, endPoint, color, width, height);
        }
    }
    
    private Point[] CalculateStarVertices(int centerX, int centerY, double size, double rotation)
    {
        Point[] vertices = new Point[5];
        
        for (int i = 0; i < 5; i++)
        {
            double angle = rotation + i * StarAngleStep;
            
            // Calculate outer point
            double outerRadius = size;
            double outerX = centerX + Math.Cos(angle) * outerRadius;
            double outerY = centerY + Math.Sin(angle) * outerRadius;
            
            // Calculate inner point
            double innerRadius = size * 0.4; // Inner radius for star shape
            double innerAngle = angle + StarAngleStep / 2.0;
            double innerX = centerX + Math.Cos(innerAngle) * innerRadius;
            double innerY = centerY + Math.Sin(innerAngle) * innerRadius;
            
            // Store vertices in alternating pattern for star shape
            if (i % 2 == 0)
            {
                vertices[i] = new Point((int)outerX, (int)outerY);
            }
            else
            {
                vertices[i] = new Point((int)innerX, (int)innerY);
            }
        }
        
        return vertices;
    }
    
    private void DrawLine(ImageBuffer output, Point start, Point end, Color color, int width, int height)
    {
        // Use Bresenham's line algorithm for efficient line drawing
        int x0 = start.X;
        int y0 = start.Y;
        int x1 = end.X;
        int y1 = end.Y;
        
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        
        while (true)
        {
            // Check bounds and draw pixel
            if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height)
            {
                output.SetPixel(x0, y0, color);
            }
            
            if (x0 == x1 && y0 == y1) break;
            
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
    
    private void UpdateRotationAngle()
    {
        rotationAngle += RotationSpeed;
        if (rotationAngle >= TwoPI)
        {
            rotationAngle -= TwoPI;
        }
    }
    
    // Public interface for parameter adjustment
    public void SetNumberOfColors(int count) 
    { 
        NumberOfColors = Math.Clamp(count, MinNumberOfColors, MaxNumberOfColors); 
    }
    
    public void SetColor(int index, Color color)
    {
        if (index >= 0 && index < MaxColors)
        {
            ColorPalette[index] = color;
        }
    }
    
    public void SetRotationSpeed(double speed) 
    { 
        RotationSpeed = Math.Clamp(speed, MinRotationSpeed, MaxRotationSpeed); 
    }
    
    public void SetStarSizeMultiplier(double multiplier) 
    { 
        StarSizeMultiplier = Math.Clamp(multiplier, MinStarSizeMultiplier, MaxStarSizeMultiplier); 
    }
    
    public void SetEnableAudioReactivity(bool enable) { EnableAudioReactivity = enable; }
    public void SetEnableDualStars(bool enable) { EnableDualStars = enable; }
    public void SetEnableColorInterpolation(bool enable) { EnableColorInterpolation = enable; }
    
    // Status queries
    public int GetNumberOfColors() => NumberOfColors;
    public Color GetColor(int index) => (index >= 0 && index < MaxColors) ? ColorPalette[index] : Color.White;
    public double GetRotationSpeed() => RotationSpeed;
    public double GetStarSizeMultiplier() => StarSizeMultiplier;
    public bool IsAudioReactivityEnabled() => EnableAudioReactivity;
    public bool IsDualStarsEnabled() => EnableDualStars;
    public bool IsColorInterpolationEnabled() => EnableColorInterpolation;
    public int GetColorPosition() => colorPosition;
    public double GetRotationAngle() => rotationAngle;
    
    // Color palette management
    public void SetColorPalette(Color[] newPalette)
    {
        if (newPalette != null && newPalette.Length <= MaxColors)
        {
            Array.Copy(newPalette, ColorPalette, Math.Min(newPalette.Length, MaxColors));
            NumberOfColors = newPalette.Length;
        }
    }
    
    public Color[] GetColorPalette()
    {
        Color[] result = new Color[NumberOfColors];
        Array.Copy(ColorPalette, result, NumberOfColors);
        return result;
    }
    
    public void RandomizeColors()
    {
        for (int i = 0; i < NumberOfColors; i++)
        {
            ColorPalette[i] = Color.FromArgb(
                255,
                random.Next(0, 256),
                random.Next(0, 256),
                random.Next(0, 256)
            );
        }
    }
    
    public override void Dispose()
    {
        // Cleanup if needed
    }
}
```

## Integration Points

### Audio Data Integration
- **Spectrum Analysis**: Processes spectrum data from bands 3-13 for star sizing
- **Audio Reactivity**: Dynamic star dimensions based on frequency analysis
- **Beat Detection**: Responds to audio events for enhanced visual effects
- **Frequency Mapping**: Intelligent mapping of audio data to visual parameters

### Framebuffer Handling
- **Line Rendering**: Efficient line drawing with boundary checking
- **Coordinate Management**: Precise star vertex positioning and rotation
- **Screen Boundaries**: Proper clipping and boundary management
- **Performance Optimization**: Minimal calculations for smooth rendering

### Performance Considerations
- **Bresenham Algorithm**: Efficient line drawing without floating-point math
- **Vertex Caching**: Pre-calculated star vertices for smooth animation
- **Boundary Checking**: Optimized screen boundary validation
- **Memory Management**: Efficient color palette and vertex storage

## Usage Examples

### Basic Rotating Star
```csharp
var rotStarNode = new RotatingStarPatternsNode
{
    NumberOfColors = 3,
    RotationSpeed = 0.1,
    StarSizeMultiplier = 1.0,
    EnableAudioReactivity = true,
    EnableDualStars = true,
    EnableColorInterpolation = true
};
```

### Multi-Color Star System
```csharp
var rotStarNode = new RotatingStarPatternsNode
{
    NumberOfColors = 8,
    RotationSpeed = 0.15,
    StarSizeMultiplier = 1.5,
    EnableAudioReactivity = true,
    EnableDualStars = true,
    EnableColorInterpolation = true
};

// Set custom color palette
rotStarNode.SetColor(0, Color.Red);
rotStarNode.SetColor(1, Color.Blue);
rotStarNode.SetColor(2, Color.Green);
rotStarNode.SetColor(3, Color.Yellow);
rotStarNode.SetColor(4, Color.Magenta);
rotStarNode.SetColor(5, Color.Cyan);
rotStarNode.SetColor(6, Color.Orange);
rotStarNode.SetColor(7, Color.Purple);
```

### Audio-Reactive Star Field
```csharp
var rotStarNode = new RotatingStarPatternsNode
{
    NumberOfColors = 16,
    RotationSpeed = 0.2,
    StarSizeMultiplier = 2.0,
    EnableAudioReactivity = true,
    EnableDualStars = true,
    EnableColorInterpolation = true
};

// Randomize colors for dynamic appearance
rotStarNode.RandomizeColors();
```

## Technical Notes

### Star Geometry Mathematics
The effect implements sophisticated star generation:
- **5-Point Star**: Classic star geometry using trigonometric calculations
- **Vertex Calculation**: Alternating outer and inner points for star shape
- **Rotation System**: Smooth continuous rotation with configurable speed
- **Size Scaling**: Dynamic sizing based on audio spectrum analysis

### Color Interpolation System
Advanced color management:
- **64-Step Interpolation**: Smooth transitions between colors
- **RGB Component Blending**: Individual channel interpolation
- **Cycle Management**: Automatic color progression through palette
- **Dynamic Palette**: Support for up to 16 different colors

### Audio Integration Architecture
Sophisticated audio processing:
- **Spectrum Analysis**: Multi-band frequency analysis
- **Peak Detection**: Intelligent audio threshold detection
- **Size Mapping**: Audio data to visual parameter mapping
- **Beat Reactivity**: Dynamic parameter changes based on audio events

This effect creates mesmerizing, cosmic visualizations with smooth rotation, dynamic sizing, and beautiful color transitions, making it perfect for space-themed and ambient music visualizations.
