# NF Clear Effects

## Overview
The NF Clear effect provides advanced frame clearing capabilities with multiple clearing modes, patterns, and beat-reactive behaviors. It's essential for creating clean visual transitions, background effects, and dynamic clearing patterns in AVS presets with precise control over clearing behavior and timing.

## C++ Source Analysis
**File:** `r_nfclr.cpp`  
**Class:** `C_THISCLASS : public C_RBASE`

### Key Properties
- **Clear Mode**: Different clearing algorithms and behaviors
- **Clear Pattern**: Visual pattern used for clearing
- **Clear Color**: Color used for clearing operations
- **Beat Reactivity**: Dynamic clearing on beat detection
- **Clear Timing**: When and how often clearing occurs
- **Pattern Parameters**: Customizable pattern properties

### Core Functionality
```cpp
class C_THISCLASS : public C_RBASE
{
    int mode;
    int color;
    int pattern;
    int onbeat;
    int timing;
    int param1;
    int param2;
    int param3;
    
    virtual int render(char visdata[2][2][576], int isBeat, int *framebuffer, int *fbout, int w, int h);
    virtual HWND conf(HINSTANCE hInstance, HWND hwndParent);
    virtual char *get_desc();
};
```

## C# Implementation

### NFClearEffectsNode Class
```csharp
public class NFClearEffectsNode : BaseEffectNode
{
    public int ClearMode { get; set; } = 0;
    public int ClearColor { get; set; } = 0x000000;
    public int ClearPattern { get; set; } = 0;
    public bool BeatReactive { get; set; } = false;
    public int ClearTiming { get; set; } = 0;
    public float ClearOpacity { get; set; } = 1.0f;
    public bool EnablePatterns { get; set; } = true;
    public int PatternDensity { get; set; } = 50;
    public float PatternScale { get; set; } = 1.0f;
    public bool AnimatedPattern { get; set; } = false;
    public float AnimationSpeed { get; set; } = 1.0f;
    public int ClearBlendMode { get; set; } = 0;
}
```

### Key Features
1. **Multiple Clear Modes**: Different clearing algorithms and behaviors
2. **Custom Clear Patterns**: Visual patterns for clearing operations
3. **Color Control**: Configurable clearing colors and opacity
4. **Beat Reactivity**: Dynamic clearing synchronized with music
5. **Timing Control**: Precise control over when clearing occurs
6. **Pattern Animation**: Animated clearing patterns
7. **Blend Mode Support**: Various blending modes for clearing

### Clear Modes
- **0**: Full Clear (Complete frame clearing)
- **1**: Pattern Clear (Pattern-based clearing)
- **2**: Gradient Clear (Gradient-based clearing)
- **3**: Radial Clear (Radial clearing from center)
- **4**: Wave Clear (Wave-pattern clearing)
- **5**: Noise Clear (Noise-pattern clearing)
- **6**: Custom Clear (User-defined clearing)

### Clear Patterns
- **0**: Solid (Solid color clearing)
- **1**: Checkerboard (Checkerboard pattern)
- **2**: Stripes (Horizontal/vertical stripes)
- **3**: Dots (Dot pattern)
- **4**: Lines (Line pattern)
- **5**: Waves (Wave pattern)
- **6**: Noise (Noise pattern)
- **7**: Custom (User-defined pattern)

### Blend Modes
- **0**: Replace (Replace existing pixels)
- **1**: Add (Add to existing pixels)
- **2**: Multiply (Multiply with existing pixels)
- **3**: Screen (Screen with existing pixels)
- **4**: Overlay (Overlay with existing pixels)
- **5**: Alpha Blend (Alpha-based blending)

## Usage Examples

### Basic Full Clear
```csharp
var nfClearNode = new NFClearEffectsNode
{
    ClearMode = 0, // Full clear
    ClearColor = 0x000000, // Black
    ClearPattern = 0, // Solid
    BeatReactive = false,
    ClearTiming = 0, // Every frame
    ClearOpacity = 1.0f,
    ClearBlendMode = 0 // Replace
};
```

### Beat-Reactive Pattern Clear
```csharp
var nfClearNode = new NFClearEffectsNode
{
    ClearMode = 1, // Pattern clear
    ClearColor = 0x0000FF, // Blue
    ClearPattern = 2, // Stripes
    BeatReactive = true,
    ClearTiming = 1, // On beat only
    ClearOpacity = 0.8f,
    EnablePatterns = true,
    PatternDensity = 75,
    PatternScale = 1.5f,
    AnimatedPattern = true,
    AnimationSpeed = 2.0f,
    ClearBlendMode = 1 // Add
};
```

### Gradient Clear with Animation
```csharp
var nfClearNode = new NFClearEffectsNode
{
    ClearMode = 2, // Gradient clear
    ClearColor = 0xFF0000, // Red
    ClearPattern = 0, // Solid
    BeatReactive = false,
    ClearTiming = 2, // Every few frames
    ClearOpacity = 0.6f,
    EnablePatterns = true,
    PatternDensity = 100,
    PatternScale = 2.0f,
    AnimatedPattern = true,
    AnimationSpeed = 1.5f,
    ClearBlendMode = 4 // Overlay
};
```

## Technical Implementation

### Core Clear Algorithm
```csharp
protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
{
    if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
        return GetDefaultOutput();

    // Determine if clearing should occur
    if (!ShouldClear(audioFeatures))
    {
        return imageBuffer; // No clearing, return input unchanged
    }

    // Create output buffer
    var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
    
    // Apply clearing based on mode
    switch (ClearMode)
    {
        case 0: // Full Clear
            ApplyFullClear(output);
            break;
        case 1: // Pattern Clear
            ApplyPatternClear(output);
            break;
        case 2: // Gradient Clear
            ApplyGradientClear(output);
            break;
        case 3: // Radial Clear
            ApplyRadialClear(output);
            break;
        case 4: // Wave Clear
            ApplyWaveClear(output);
            break;
        case 5: // Noise Clear
            ApplyNoiseClear(output);
            break;
        case 6: // Custom Clear
            ApplyCustomClear(output);
            break;
        default:
            ApplyFullClear(output);
            break;
    }

    // Apply blend mode if not replacing
    if (ClearBlendMode != 0)
    {
        ApplyBlendMode(imageBuffer, output);
    }

    return output;
}
```

### Clear Decision Logic
```csharp
private bool ShouldClear(AudioFeatures audioFeatures)
{
    switch (ClearTiming)
    {
        case 0: // Every frame
            return true;
        case 1: // On beat only
            return BeatReactive && audioFeatures?.IsBeat == true;
        case 2: // Every few frames
            return (FrameCount % 3) == 0;
        case 3: // Random intervals
            return Random.Next(100) < 10; // 10% chance
        case 4: // Custom timing
            return EvaluateCustomTiming(audioFeatures);
        default:
            return true;
    }
}
```

### Full Clear Implementation
```csharp
private void ApplyFullClear(ImageBuffer output)
{
    int clearColor = ClearColor;
    
    // Apply opacity if not fully opaque
    if (ClearOpacity < 1.0f)
    {
        clearColor = ApplyOpacity(clearColor, ClearOpacity);
    }
    
    // Fill entire buffer
    for (int i = 0; i < output.Pixels.Length; i++)
    {
        output.Pixels[i] = clearColor;
    }
}
```

### Pattern Clear Implementation
```csharp
private void ApplyPatternClear(ImageBuffer output)
{
    if (!EnablePatterns)
    {
        ApplyFullClear(output);
        return;
    }

    float currentTime = GetCurrentTime();
    
    for (int y = 0; y < output.Height; y++)
    {
        for (int x = 0; x < output.Width; x++)
        {
            int patternValue = CalculatePatternValue(x, y, currentTime);
            int pixelColor = ApplyPatternToColor(ClearColor, patternValue);
            
            if (ClearOpacity < 1.0f)
            {
                pixelColor = ApplyOpacity(pixelColor, ClearOpacity);
            }
            
            output.SetPixel(x, y, pixelColor);
        }
    }
}
```

## Pattern Generation

### Checkerboard Pattern
```csharp
private int CalculateCheckerboardPattern(int x, int y, float time)
{
    int tileSize = Math.Max(1, 100 / PatternDensity);
    bool isEvenTile = ((x / tileSize) + (y / tileSize)) % 2 == 0;
    
    if (AnimatedPattern)
    {
        // Animate pattern over time
        float animation = (float)Math.Sin(time * AnimationSpeed) * 0.5f + 0.5f;
        isEvenTile = ((x / tileSize) + (y / tileSize) + (int)(animation * 10)) % 2 == 0;
    }
    
    return isEvenTile ? 255 : 0;
}
```

### Stripe Pattern
```csharp
private int CalculateStripePattern(int x, int y, float time)
{
    int stripeWidth = Math.Max(1, 200 / PatternDensity);
    int stripeType = (x / stripeWidth) % 2;
    
    if (AnimatedPattern)
    {
        // Animate stripe movement
        float offset = (time * AnimationSpeed * 50) % stripeWidth;
        stripeType = ((int)(x + offset) / stripeWidth) % 2;
    }
    
    return stripeType == 0 ? 255 : 0;
}
```

### Dot Pattern
```csharp
private int CalculateDotPattern(int x, int y, float time)
{
    int dotSpacing = Math.Max(1, 300 / PatternDensity);
    int dotSize = Math.Max(1, dotSpacing / 4);
    
    int centerX = (x / dotSpacing) * dotSpacing + dotSpacing / 2;
    int centerY = (y / dotSpacing) * dotSpacing + dotSpacing / 2;
    
    float distance = (float)Math.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
    
    if (AnimatedPattern)
    {
        // Animate dot size
        float sizeVariation = (float)Math.Sin(time * AnimationSpeed) * 0.3f + 1.0f;
        dotSize = (int)(dotSize * sizeVariation);
    }
    
    return distance <= dotSize ? 255 : 0;
}
```

### Wave Pattern
```csharp
private int CalculateWavePattern(int x, int y, float time)
{
    float normalizedX = (float)x / output.Width;
    float normalizedY = (float)y / output.Height;
    
    float wave1 = (float)Math.Sin(normalizedX * Math.PI * 4 + time * AnimationSpeed);
    float wave2 = (float)Math.Sin(normalizedY * Math.PI * 3 + time * AnimationSpeed * 0.7f);
    
    float combinedWave = (wave1 + wave2) * 0.5f;
    
    // Convert to 0-255 range
    int patternValue = (int)((combinedWave + 1.0f) * 127.5f);
    
    return patternValue;
}
```

### Noise Pattern
```csharp
private int CalculateNoisePattern(int x, int y, float time)
{
    // Use Perlin noise for smooth, coherent noise
    float noiseX = x * PatternScale * 0.01f;
    float noiseY = y * PatternScale * 0.01f;
    float noiseTime = time * AnimationSpeed * 0.1f;
    
    float noiseValue = PerlinNoise3D(noiseX, noiseY, noiseTime);
    
    // Apply density control
    float threshold = (100.0f - PatternDensity) / 100.0f;
    int patternValue = noiseValue > threshold ? 255 : 0;
    
    return patternValue;
}
```

## Advanced Clearing Techniques

### Gradient Clear
```csharp
private void ApplyGradientClear(ImageBuffer output)
{
    for (int y = 0; y < output.Height; y++)
    {
        for (int x = 0; x < output.Width; x++)
        {
            float normalizedX = (float)x / output.Width;
            float normalizedY = (float)y / output.Height;
            
            // Create gradient from top-left to bottom-right
            float gradientValue = (normalizedX + normalizedY) * 0.5f;
            
            int pixelColor = InterpolateColor(ClearColor, 0xFFFFFF, gradientValue);
            
            if (ClearOpacity < 1.0f)
            {
                pixelColor = ApplyOpacity(pixelColor, ClearOpacity);
            }
            
            output.SetPixel(x, y, pixelColor);
        }
    }
}
```

### Radial Clear
```csharp
private void ApplyRadialClear(ImageBuffer output)
{
    float centerX = output.Width / 2.0f;
    float centerY = output.Height / 2.0f;
    float maxRadius = (float)Math.Sqrt(centerX * centerX + centerY * centerY);
    
    for (int y = 0; y < output.Height; y++)
    {
        for (int x = 0; x < output.Width; x++)
        {
            float dx = x - centerX;
            float dy = y - centerY;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
            
            // Create radial gradient
            float normalizedDistance = distance / maxRadius;
            int pixelColor = InterpolateColor(ClearColor, 0xFFFFFF, normalizedDistance);
            
            if (ClearOpacity < 1.0f)
            {
                pixelColor = ApplyOpacity(pixelColor, ClearOpacity);
            }
            
            output.SetPixel(x, y, pixelColor);
        }
    }
}
```

## Color and Opacity Functions

### Color Interpolation
```csharp
private int InterpolateColor(int color1, int color2, float factor)
{
    int r1 = color1 & 0xFF, g1 = (color1 >> 8) & 0xFF, b1 = (color1 >> 16) & 0xFF;
    int r2 = color2 & 0xFF, g2 = (color2 >> 8) & 0xFF, b2 = (color2 >> 16) & 0xFF;
    
    int r = (int)(r1 * (1.0f - factor) + r2 * factor);
    int g = (int)(g1 * (1.0f - factor) + g2 * factor);
    int b = (int)(b1 * (1.0f - factor) + b2 * factor);
    
    return r | (g << 8) | (b << 16);
}
```

### Opacity Application
```csharp
private int ApplyOpacity(int color, float opacity)
{
    int r = color & 0xFF;
    int g = (color >> 8) & 0xFF;
    int b = (color >> 16) & 0xFF;
    
    r = (int)(r * opacity);
    g = (int)(g * opacity);
    b = (int)(b * opacity);
    
    return r | (g << 8) | (b << 16);
}
```

### Pattern Color Application
```csharp
private int ApplyPatternToColor(int baseColor, int patternValue)
{
    if (patternValue == 0)
    {
        return 0; // Transparent/black
    }
    else if (patternValue == 255)
    {
        return baseColor; // Full color
    }
    else
    {
        // Partial pattern - blend with base color
        float factor = patternValue / 255.0f;
        return InterpolateColor(0, baseColor, factor);
    }
}
```

## Blend Mode Implementation

### Blend Mode Application
```csharp
private void ApplyBlendMode(ImageBuffer original, ImageBuffer cleared)
{
    for (int y = 0; y < original.Height; y++)
    {
        for (int x = 0; x < original.Width; x++)
        {
            int originalPixel = original.GetPixel(x, y);
            int clearedPixel = cleared.GetPixel(x, y);
            
            int blendedPixel = BlendPixels(originalPixel, clearedPixel, ClearBlendMode);
            cleared.SetPixel(x, y, blendedPixel);
        }
    }
}
```

### Pixel Blending
```csharp
private int BlendPixels(int pixel1, int pixel2, int blendMode)
{
    switch (blendMode)
    {
        case 0: // Replace
            return pixel2;
        case 1: // Add
            return BlendAdd(pixel1, pixel2);
        case 2: // Multiply
            return BlendMultiply(pixel1, pixel2);
        case 3: // Screen
            return BlendScreen(pixel1, pixel2);
        case 4: // Overlay
            return BlendOverlay(pixel1, pixel2);
        case 5: // Alpha Blend
            return BlendAlpha(pixel1, pixel2, ClearOpacity);
        default:
            return pixel2;
    }
}
```

## Performance Optimization

### Optimization Techniques
1. **Pattern Caching**: Cache generated patterns
2. **SIMD Operations**: Vectorized pixel operations
3. **Early Exit**: Skip processing when not clearing
4. **Caching**: Cache pattern calculations
5. **Threading**: Multi-threaded clearing operations

### Memory Management
- Efficient pattern storage
- Minimize temporary allocations
- Use value types for calculations
- Optimize clearing operations

## Integration with EffectGraph

### Port Configuration
```csharp
protected override void InitializePorts()
{
    _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Input image"));
    _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Cleared output image"));
}
```

### Metadata Support
```csharp
public override EffectMetadata GetMetadata()
{
    var metadata = new EffectMetadata();
    metadata.Add("ClearMode", ClearMode);
    metadata.Add("ClearColor", ClearColor);
    metadata.Add("ClearPattern", ClearPattern);
    metadata.Add("BeatReactive", BeatReactive);
    metadata.Add("ClearTiming", ClearTiming);
    metadata.Add("ClearOpacity", ClearOpacity);
    metadata.Add("PatternDensity", PatternDensity);
    metadata.Add("AnimationSpeed", AnimationSpeed);
    return metadata;
}
```

## Testing and Validation

### Test Cases
1. **Basic Clearing**: Verify clearing accuracy
2. **Pattern Generation**: Test all pattern types
3. **Clear Modes**: Test all clearing algorithms
4. **Performance**: Measure clearing speed
5. **Edge Cases**: Handle boundary conditions
6. **Beat Reactivity**: Validate beat-synchronized clearing

### Validation Methods
- Visual comparison with reference images
- Performance benchmarking
- Memory usage analysis
- Pattern accuracy testing

## Future Enhancements

### Planned Features
1. **Advanced Patterns**: More sophisticated pattern generation
2. **3D Clearing**: Three-dimensional clearing effects
3. **Real-time Effects**: Dynamic pattern generation
4. **Hardware Acceleration**: GPU-accelerated clearing
5. **Custom Shaders**: User-defined clearing algorithms

### Compatibility
- Full AVS preset compatibility
- Support for legacy clearing modes
- Performance parity with original
- Extended functionality

## Conclusion

The NF Clear effect provides essential frame clearing capabilities for clean AVS visualizations. This C# implementation maintains full compatibility with the original while adding modern features like beat reactivity and enhanced pattern generation. Complete documentation ensures reliable operation in production environments with optimal performance and visual quality.
