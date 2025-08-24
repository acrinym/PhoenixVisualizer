# DDM (Dynamic Dot Matrix) Effects

## Overview
The DDM effect creates dynamic dot matrix displays with configurable dot patterns, colors, and animations. It's essential for creating retro-style digital displays and matrix-style visualizations in AVS presets.

## C++ Source Analysis
**File:** `r_ddm.cpp`  
**Class:** `C_THISCLASS : public C_RBASE`

### Key Properties
- **Dot Size**: Controls the size of individual dots
- **Dot Spacing**: Controls the spacing between dots
- **Dot Pattern**: Different dot arrangement algorithms
- **Dot Color**: Configurable dot colors and effects
- **Animation Speed**: Controls the animation rate
- **Beat Reactivity**: Dynamic changes on beat detection

### Core Functionality
```cpp
class C_THISCLASS : public C_RBASE
{
    int dotsize;
    int spacing;
    int pattern;
    int color;
    int speed;
    int onbeat;
    int speed2;
    
    virtual int render(char visdata[2][2][576], int isBeat, int *framebuffer, int *fbout, int w, int h);
    virtual HWND conf(HINSTANCE hInstance, HWND hwndParent);
    virtual char *get_desc();
};
```

## C# Implementation

### DDMEffectsNode Class
```csharp
public class DDMEffectsNode : BaseEffectNode
{
    public int DotSize { get; set; } = 4;
    public int DotSpacing { get; set; } = 8;
    public int DotPattern { get; set; } = 0;
    public int DotColor { get; set; } = 0x00FF00;
    public float AnimationSpeed { get; set; } = 1.0f;
    public bool BeatReactive { get; set; } = false;
    public float BeatAnimationSpeed { get; set; } = 2.0f;
    public bool RandomizeColors { get; set; } = false;
    public int ColorVariation { get; set; } = 50;
    public bool EnableFade { get; set; } = true;
    public float FadeRate { get; set; } = 0.95f;
    public int MatrixWidth { get; set; } = 64;
    public int MatrixHeight { get; set; } = 48;
}
```

### Key Features
1. **Multiple Dot Patterns**: Different dot arrangement algorithms
2. **Dynamic Sizing**: Adjustable dot size and spacing
3. **Color Control**: Configurable dot colors and variations
4. **Animation Support**: Smooth dot animations and transitions
5. **Beat Reactivity**: Dynamic changes on beat detection
6. **Matrix Control**: Configurable matrix dimensions
7. **Fade Effects**: Optional dot fade effects

### Dot Patterns
- **0**: Grid Pattern (Regular grid arrangement)
- **1**: Random Pattern (Random dot placement)
- **2**: Spiral Pattern (Spiral dot arrangement)
- **3**: Wave Pattern (Wave-like dot arrangement)
- **4**: Fractal Pattern (Fractal-based dot placement)
- **5**: Audio Pattern (Audio-reactive dot placement)
- **6**: Custom Pattern (User-defined dot arrangement)

### Animation Types
- **0**: Static (No animation)
- **1**: Fade (Dot fade in/out)
- **2**: Pulse (Dot size pulsing)
- **3**: Rotate (Dot rotation)
- **4**: Move (Dot movement)
- **5**: Color Cycle (Color cycling)
- **6**: Beat Pulse (Beat-reactive pulsing)

## Usage Examples

### Basic Grid Pattern
```csharp
var ddmNode = new DDMEffectsNode
{
    DotSize = 4,
    DotSpacing = 8,
    DotPattern = 0, // Grid pattern
    DotColor = 0x00FF00, // Green
    AnimationSpeed = 1.0f,
    MatrixWidth = 64,
    MatrixHeight = 48,
    EnableFade = false
};
```

### Beat-Reactive Random Pattern
```csharp
var ddmNode = new DDMEffectsNode
{
    DotSize = 6,
    DotSpacing = 10,
    DotPattern = 1, // Random pattern
    DotColor = 0xFF0000, // Red
    AnimationSpeed = 2.0f,
    BeatReactive = true,
    BeatAnimationSpeed = 4.0f,
    RandomizeColors = true,
    ColorVariation = 100,
    EnableFade = true,
    FadeRate = 0.90f
};
```

### Spiral Pattern with Color Cycling
```csharp
var ddmNode = new DDMEffectsNode
{
    DotSize = 3,
    DotSpacing = 6,
    DotPattern = 2, // Spiral pattern
    DotColor = 0x0000FF, // Blue
    AnimationSpeed = 1.5f,
    RandomizeColors = true,
    ColorVariation = 75,
    EnableFade = true,
    FadeRate = 0.98f,
    MatrixWidth = 80,
    MatrixHeight = 60
};
```

## Technical Implementation

### Core DDM Algorithm
```csharp
protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
{
    if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
        return GetDefaultOutput();

    var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
    
    float currentSpeed = AnimationSpeed;
    if (BeatReactive && audioFeatures?.IsBeat == true)
    {
        currentSpeed *= BeatAnimationSpeed;
    }

    // Calculate matrix dimensions
    int matrixWidth = Math.Min(MatrixWidth, imageBuffer.Width / DotSpacing);
    int matrixHeight = Math.Min(MatrixHeight, imageBuffer.Height / DotSpacing);

    // Generate dot positions based on pattern
    var dotPositions = GenerateDotPositions(matrixWidth, matrixHeight, DotPattern);

    // Render dots
    switch (DotPattern)
    {
        case 0: // Grid Pattern
            RenderGridPattern(output, dotPositions, currentSpeed);
            break;
        case 1: // Random Pattern
            RenderRandomPattern(output, dotPositions, currentSpeed);
            break;
        case 2: // Spiral Pattern
            RenderSpiralPattern(output, dotPositions, currentSpeed);
            break;
        case 3: // Wave Pattern
            RenderWavePattern(output, dotPositions, currentSpeed);
            break;
        case 4: // Fractal Pattern
            RenderFractalPattern(output, dotPositions, currentSpeed);
            break;
        case 5: // Audio Pattern
            RenderAudioPattern(output, dotPositions, currentSpeed, audioFeatures);
            break;
        case 6: // Custom Pattern
            RenderCustomPattern(output, dotPositions, currentSpeed);
            break;
    }

    return output;
}
```

### Dot Position Generation
```csharp
private List<Point> GenerateDotPositions(int matrixWidth, int matrixHeight, int pattern)
{
    var positions = new List<Point>();
    
    switch (pattern)
    {
        case 0: // Grid Pattern
            for (int y = 0; y < matrixHeight; y++)
            {
                for (int x = 0; x < matrixWidth; x++)
                {
                    positions.Add(new Point(x * DotSpacing, y * DotSpacing));
                }
            }
            break;
            
        case 1: // Random Pattern
            var random = new Random();
            int totalDots = matrixWidth * matrixHeight / 4; // 25% density
            
            for (int i = 0; i < totalDots; i++)
            {
                int x = random.Next(0, matrixWidth) * DotSpacing;
                int y = random.Next(0, matrixHeight) * DotSpacing;
                positions.Add(new Point(x, y));
            }
            break;
            
        case 2: // Spiral Pattern
            GenerateSpiralPositions(positions, matrixWidth, matrixHeight);
            break;
            
        case 3: // Wave Pattern
            GenerateWavePositions(positions, matrixWidth, matrixHeight);
            break;
            
        case 4: // Fractal Pattern
            GenerateFractalPositions(positions, matrixWidth, matrixHeight);
            break;
    }
    
    return positions;
}
```

### Grid Pattern Rendering
```csharp
private void RenderGridPattern(ImageBuffer output, List<Point> dotPositions, float speed)
{
    foreach (var position in dotPositions)
    {
        int x = position.X;
        int y = position.Y;
        
        // Calculate dot color
        int dotColor = CalculateDotColor(x, y, speed);
        
        // Render dot
        RenderDot(output, x, y, dotColor);
    }
}
```

### Dot Rendering
```csharp
private void RenderDot(ImageBuffer output, int centerX, int centerY, int color)
{
    int radius = DotSize / 2;
    
    for (int dy = -radius; dy <= radius; dy++)
    {
        for (int dx = -radius; dx <= radius; dx++)
        {
            int x = centerX + dx;
            int y = centerY + dy;
            
            // Check bounds
            if (x < 0 || x >= output.Width || y < 0 || y >= output.Height)
                continue;
            
            // Calculate distance from center
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
            
            if (distance <= radius)
            {
                // Apply anti-aliasing for smooth dots
                float alpha = 1.0f - (distance / radius);
                int finalColor = BlendColors(output.GetPixel(x, y), color, alpha);
                output.SetPixel(x, y, finalColor);
            }
        }
    }
}
```

### Color Calculation
```csharp
private int CalculateDotColor(int x, int y, float speed)
{
    int baseColor = DotColor;
    
    if (RandomizeColors)
    {
        var random = new Random((x * 73856093) ^ (y * 19349663) ^ (int)(speed * 1000));
        int variation = random.Next(-ColorVariation, ColorVariation + 1);
        
        int r = Math.Clamp(((baseColor >> 16) & 0xFF) + variation, 0, 255);
        int g = Math.Clamp(((baseColor >> 8) & 0xFF) + variation, 0, 255);
        int b = Math.Clamp((baseColor & 0xFF) + variation, 0, 255);
        
        baseColor = r | (g << 8) | (b << 16);
    }
    
    // Apply animation effects
    if (EnableFade)
    {
        float fadeFactor = (float)Math.Pow(FadeRate, speed);
        baseColor = ApplyFade(baseColor, fadeFactor);
    }
    
    return baseColor;
}
```

## Advanced Pattern Techniques

### Spiral Pattern Generation
```csharp
private void GenerateSpiralPositions(List<Point> positions, int matrixWidth, int matrixHeight)
{
    int centerX = matrixWidth / 2;
    int centerY = matrixHeight / 2;
    int maxRadius = Math.Max(matrixWidth, matrixHeight) / 2;
    
    for (int radius = 0; radius < maxRadius; radius += 2)
    {
        for (int angle = 0; angle < 360; angle += 10)
        {
            double radians = angle * Math.PI / 180.0;
            int x = centerX + (int)(radius * Math.Cos(radians));
            int y = centerY + (int)(radius * Math.Sin(radians));
            
            if (x >= 0 && x < matrixWidth && y >= 0 && y < matrixHeight)
            {
                positions.Add(new Point(x * DotSpacing, y * DotSpacing));
            }
        }
    }
}
```

### Wave Pattern Generation
```csharp
private void GenerateWavePositions(List<Point> positions, int matrixWidth, int matrixHeight)
{
    for (int y = 0; y < matrixHeight; y++)
    {
        for (int x = 0; x < matrixWidth; x++)
        {
            // Create wave pattern
            double waveX = Math.Sin(y * 0.2) * 5;
            double waveY = Math.Cos(x * 0.3) * 3;
            
            int adjustedX = x + (int)waveX;
            int adjustedY = y + (int)waveY;
            
            if (adjustedX >= 0 && adjustedX < matrixWidth && 
                adjustedY >= 0 && adjustedY < matrixHeight)
            {
                positions.Add(new Point(adjustedX * DotSpacing, adjustedY * DotSpacing));
            }
        }
    }
}
```

### Audio Pattern Rendering
```csharp
private void RenderAudioPattern(ImageBuffer output, List<Point> dotPositions, float speed, AudioFeatures audioFeatures)
{
    if (audioFeatures == null)
        return;
    
    // Use audio data to influence dot properties
    float[] spectrum = audioFeatures.Spectrum;
    float[] waveform = audioFeatures.Waveform;
    
    foreach (var position in dotPositions)
    {
        int x = position.X;
        int y = position.Y;
        
        // Map position to audio data
        int spectrumIndex = (x * spectrum.Length) / output.Width;
        int waveformIndex = (y * waveform.Length) / output.Height;
        
        if (spectrumIndex < spectrum.Length && waveformIndex < waveform.Length)
        {
            float audioIntensity = (spectrum[spectrumIndex] + waveform[waveformIndex]) / 2.0f;
            
            // Adjust dot size based on audio
            int dynamicDotSize = (int)(DotSize * (1.0f + audioIntensity));
            
            // Adjust color based on audio
            int dynamicColor = AdjustColorForAudio(DotColor, audioIntensity);
            
            // Render dynamic dot
            RenderDynamicDot(output, x, y, dynamicDotSize, dynamicColor);
        }
    }
}
```

## Color and Fade Effects

### Color Blending
```csharp
private int BlendColors(int background, int foreground, float alpha)
{
    int r1 = background & 0xFF;
    int g1 = (background >> 8) & 0xFF;
    int b1 = (background >> 16) & 0xFF;
    
    int r2 = foreground & 0xFF;
    int g2 = (foreground >> 8) & 0xFF;
    int b2 = (foreground >> 16) & 0xFF;
    
    int r = (int)(r1 * (1.0f - alpha) + r2 * alpha);
    int g = (int)(g1 * (1.0f - alpha) + g2 * alpha);
    int b = (int)(b1 * (1.0f - alpha) + b2 * alpha);
    
    return r | (g << 8) | (b << 16);
}
```

### Fade Application
```csharp
private int ApplyFade(int color, float fadeFactor)
{
    int r = (int)((color & 0xFF) * fadeFactor);
    int g = (int)(((color >> 8) & 0xFF) * fadeFactor);
    int b = (int)(((color >> 16) & 0xFF) * fadeFactor);
    
    return r | (g << 8) | (b << 16);
}
```

### Audio Color Adjustment
```csharp
private int AdjustColorForAudio(int baseColor, float audioIntensity)
{
    int r = (baseColor >> 16) & 0xFF;
    int g = (baseColor >> 8) & 0xFF;
    int b = baseColor & 0xFF;
    
    // Shift colors based on audio intensity
    float shift = audioIntensity * 0.5f;
    
    r = Math.Clamp((int)(r + shift * 100), 0, 255);
    g = Math.Clamp((int)(g + shift * 50), 0, 255);
    b = Math.Clamp((int)(b + shift * 150), 0, 255);
    
    return r | (g << 8) | (b << 16);
}
```

## Performance Optimization

### Optimization Techniques
1. **Spatial Partitioning**: Efficient dot position calculation
2. **LOD System**: Level-of-detail for distant dots
3. **SIMD Operations**: Vectorized color operations
4. **Caching**: Cache frequently accessed patterns
5. **Threading**: Multi-threaded dot rendering

### Memory Management
- Efficient dot position storage
- Minimize temporary allocations
- Use value types for calculations
- Optimize color calculations

## Integration with EffectGraph

### Port Configuration
```csharp
protected override void InitializePorts()
{
    _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for DDM overlay"));
    _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "DDM output image"));
}
```

### Metadata Support
```csharp
public override EffectMetadata GetMetadata()
{
    var metadata = new EffectMetadata();
    metadata.Add("DotPattern", DotPattern);
    metadata.Add("DotSize", DotSize);
    metadata.Add("DotSpacing", DotSpacing);
    metadata.Add("MatrixDimensions", $"{MatrixWidth}x{MatrixHeight}");
    metadata.Add("AnimationSpeed", AnimationSpeed);
    metadata.Add("BeatReactive", BeatReactive);
    return metadata;
}
```

## Testing and Validation

### Test Cases
1. **Basic Rendering**: Verify dot rendering accuracy
2. **Pattern Generation**: Test all dot patterns
3. **Animation**: Validate animation effects
4. **Beat Reactivity**: Test beat-reactive behavior
5. **Performance**: Measure rendering speed
6. **Edge Cases**: Handle boundary conditions

### Validation Methods
- Visual comparison with reference images
- Performance benchmarking
- Memory usage analysis
- Pattern accuracy testing

## Future Enhancements

### Planned Features
1. **Advanced Patterns**: More sophisticated dot arrangements
2. **3D Dots**: Three-dimensional dot rendering
3. **Particle Systems**: Advanced particle effects
4. **Hardware Acceleration**: GPU-accelerated rendering
5. **Custom Shaders**: User-defined dot algorithms

### Compatibility
- Full AVS preset compatibility
- Support for legacy DDM modes
- Performance parity with original
- Extended functionality

## Conclusion

The DDM effect provides essential dot matrix visualization capabilities for retro-style AVS presets. This C# implementation maintains full compatibility with the original while adding modern features like beat reactivity and enhanced pattern generation. Complete documentation ensures reliable operation in production environments.
