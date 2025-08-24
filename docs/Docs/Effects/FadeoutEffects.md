# AVS Fadeout Effect

## Overview
The Fadeout effect is an AVS visualization effect that gradually fades image colors toward a target color. It creates smooth color transitions by adjusting each RGB channel toward the specified fade color based on a configurable fade length. This effect is essential for creating smooth color washes, atmospheric effects, and gradual color transformations in AVS presets.

## C++ Source Analysis
Based on the AVS source code in `r_fadeout.cpp`, the Fadeout effect inherits from `C_RBASE` and implements sophisticated color fading with lookup table optimization:

### Key Properties
- **Fade Length**: Controls the intensity of the fade effect (0-92)
- **Target Color**: The color toward which all pixels fade
- **Lookup Tables**: Pre-computed fade values for optimal performance
- **MMX Optimization**: Uses MMX assembly for high-performance processing
- **Alpha Channel Safe**: Preserves alpha channel during processing

### Core Functionality
```cpp
class C_THISCLASS : public C_RBASE 
{
    public:
        virtual int render(char visdata[2][2][576], int isBeat, int *framebuffer, int *fbout, int w, int h);
        virtual char *get_desc();
        virtual HWND conf(HINSTANCE hInstance, HWND hwndParent);
        virtual void load_config(unsigned char *data, int len);
        virtual int save_config(unsigned char *data);
        void maketab(void);

    unsigned char fadtab[3][256]; // Lookup tables for R, G, B channels
    int fadelen;                   // Fade length (0-92)
    int color;                     // Target fade color
};
```

### Fade Algorithm
The effect uses lookup tables (`fadtab`) to pre-compute fade values:
- **Channel Processing**: Each RGB channel is processed independently
- **Fade Calculation**: Colors within fade length range move toward target
- **Lookup Optimization**: Pre-computed values eliminate runtime calculations
- **MMX Support**: Vectorized processing for enhanced performance

## C# Implementation

### Class Definition
```csharp
public class FadeoutEffectsNode : BaseEffectNode
{
    public float FadeLength { get; set; } = 16.0f; // 0.0 to 92.0
    public int TargetColor { get; set; } = 0x000000; // Target fade color
    public bool BeatReactive { get; set; } = false;
    public float BeatFadeMultiplier { get; set; } = 1.5f;
    public bool EnableSmoothFade { get; set; } = false;
    public float SmoothFadeSpeed { get; set; } = 1.0f;
    public int FadeMode { get; set; } = 0; // 0=Toward, 1=Away, 2=Oscillate
    public bool EnableChannelSelectiveFade { get; set; } = false;
    public bool FadeRedChannel { get; set; } = true;
    public bool FadeGreenChannel { get; set; } = true;
    public bool FadeBlueChannel { get; set; } = true;
    public bool EnableFadeAnimation { get; set; } = false;
    public float AnimationSpeed { get; set; } = 1.0f;
    public int AnimationMode { get; set; } = 0;
    public bool EnableFadeMasking { get; set; } = false;
    public ImageBuffer FadeMask { get; set; } = null;
    public float MaskInfluence { get; set; } = 1.0f;
    public bool EnableFadeBlending { get; set; } = false;
    public float FadeBlendStrength { get; set; } = 0.5f;
    public int FadeCurve { get; set; } = 0; // 0=Linear, 1=Exponential, 2=Sigmoid
    public float FadeCurveStrength { get; set; } = 1.0f;
    public bool EnableFadeInversion { get; set; } = false;
    public float InversionThreshold { get; set; } = 0.5f;
}
```

### Key Features
- **Configurable Fade Length**: Precise control over fade intensity
- **Target Color Selection**: Any RGB color as fade target
- **Beat Reactivity**: Dynamic fade length synchronized with audio
- **Smooth Fading**: Optional smooth fade transitions
- **Channel Selective**: Fade individual RGB channels independently
- **Fade Animation**: Animated fade effects and patterns
- **Fade Masking**: Use image masks to control fade areas
- **Fade Blending**: Blend faded and original images
- **Fade Curves**: Multiple fade curve algorithms
- **Performance Optimization**: Optimized lookup table processing

### Usage Examples

#### Basic Color Fade
```csharp
var fadeoutNode = new FadeoutEffectsNode
{
    FadeLength = 25.0f,
    TargetColor = 0x000080, // Dark blue
    BeatReactive = false,
    EnableSmoothFade = false
};
```

#### Beat-Reactive Fade
```csharp
var beatFadeNode = new FadeoutEffectsNode
{
    FadeLength = 30.0f,
    TargetColor = 0x800000, // Dark red
    BeatReactive = true,
    BeatFadeMultiplier = 2.0f,
    EnableSmoothFade = true,
    SmoothFadeSpeed = 1.5f
};
```

#### Artistic Channel Fade
```csharp
var artisticFadeNode = new FadeoutEffectsNode
{
    FadeLength = 40.0f,
    TargetColor = 0x008000, // Green
    BeatReactive = false,
    EnableChannelSelectiveFade = true,
    FadeRedChannel = false,
    FadeGreenChannel = true,
    FadeBlueChannel = true,
    EnableFadeAnimation = true,
    AnimationSpeed = 2.0f,
    AnimationMode = 1 // Oscillating
};
```

## Technical Implementation

### Core Processing Algorithm
```csharp
protected override ImageBuffer ProcessEffect(ImageBuffer input, AudioFeatures audio)
{
    var output = new ImageBuffer(input.Width, input.Height);
    var currentFadeLength = GetCurrentFadeLength(audio);
    var currentTargetColor = GetCurrentTargetColor(audio);
    
    // Generate or update fade lookup tables
    UpdateFadeTables(currentFadeLength, currentTargetColor);
    
    // Process each pixel
    for (int i = 0; i < input.Pixels.Length; i++)
    {
        var originalColor = input.Pixels[i];
        var fadedColor = ApplyFadeout(originalColor);
        
        // Apply channel selective fade if enabled
        if (EnableChannelSelectiveFade)
        {
            fadedColor = ApplyChannelSelectiveFade(originalColor, fadedColor);
        }
        
        // Apply fade masking if enabled
        if (EnableFadeMasking && FadeMask != null)
        {
            fadedColor = ApplyFadeMasking(originalColor, fadedColor, i);
        }
        
        // Apply fade blending if enabled
        if (EnableFadeBlending)
        {
            fadedColor = BlendFade(originalColor, fadedColor);
        }
        
        output.Pixels[i] = fadedColor;
    }
    
    return output;
}
```

### Fade Table Generation
```csharp
private void UpdateFadeTables(float fadeLength, int targetColor)
{
    var targetR = targetColor & 0xFF;
    var targetG = (targetColor >> 8) & 0xFF;
    var targetB = (targetColor >> 16) & 0xFF;
    
    for (int x = 0; x < 256; x++)
    {
        // Red channel fade table
        var r = x;
        if (r <= targetR - fadeLength)
            r += (int)fadeLength;
        else if (r >= targetR + fadeLength)
            r -= (int)fadeLength;
        else
            r = targetR;
        
        // Green channel fade table
        var g = x;
        if (g <= targetG - fadeLength)
            g += (int)fadeLength;
        else if (g >= targetG + fadeLength)
            g -= (int)fadeLength;
        else
            g = targetG;
        
        // Blue channel fade table
        var b = x;
        if (b <= targetB - fadeLength)
            b += (int)fadeLength;
        else if (b >= targetB + fadeLength)
            b -= (int)fadeLength;
        else
            b = targetB;
        
        // Clamp values and store in tables
        FadeTableR[x] = (byte)Math.Max(0, Math.Min(255, r));
        FadeTableG[x] = (byte)Math.Max(0, Math.Min(255, g));
        FadeTableB[x] = (byte)Math.Max(0, Math.Min(255, b));
    }
}
```

### Pixel Fade Application
```csharp
private int ApplyFadeout(int color)
{
    var r = color & 0xFF;
    var g = (color >> 8) & 0xFF;
    var b = (color >> 16) & 0xFF;
    var a = (color >> 24) & 0xFF;
    
    // Apply fade using lookup tables
    var fadedR = FadeTableR[r];
    var fadedG = FadeTableG[g];
    var fadedB = FadeTableB[b];
    
    // Apply fade curve if enabled
    if (FadeCurve != 0)
    {
        fadedR = ApplyFadeCurve(r, fadedR);
        fadedG = ApplyFadeCurve(g, fadedG);
        fadedB = ApplyFadeCurve(b, fadedB);
    }
    
    return (a << 24) | (fadedB << 16) | (fadedG << 8) | fadedR;
}

private byte ApplyFadeCurve(byte original, byte faded)
{
    var progress = (float)(faded - original) / 255.0f;
    
    switch (FadeCurve)
    {
        case 1: // Exponential
            progress = (float)Math.Pow(progress, FadeCurveStrength);
            break;
            
        case 2: // Sigmoid
            progress = 1.0f / (1.0f + (float)Math.Exp(-FadeCurveStrength * (progress - 0.5f)));
            break;
    }
    
        return (byte)(original + (faded - original) * progress);
}
```

### Beat-Reactive Fade Length
```csharp
private float GetCurrentFadeLength(AudioFeatures audio)
{
    if (!BeatReactive || audio == null)
        return FadeLength;
    
    var beatMultiplier = 1.0f;
    
    if (audio.IsBeat)
    {
        beatMultiplier = BeatFadeMultiplier;
    }
    else
    {
        // Gradual return to normal
        beatMultiplier = 1.0f + (BeatFadeMultiplier - 1.0f) * audio.BeatIntensity;
    }
    
    return Math.Max(0.0f, Math.Min(92.0f, FadeLength * beatMultiplier));
}
```

### Channel Selective Fade
```csharp
private int ApplyChannelSelectiveFade(int originalColor, int fadedColor)
{
    if (!EnableChannelSelectiveFade)
        return fadedColor;
    
    var r = originalColor & 0xFF;
    var g = (originalColor >> 8) & 0xFF;
    var b = (originalColor >> 16) & 0xFF;
    var a = (originalColor >> 24) & 0xFF;
    
    var fadedR = fadedColor & 0xFF;
    var fadedG = (fadedColor >> 8) & 0xFF;
    var fadedB = (fadedColor >> 16) & 0xFF;
    
    var finalR = FadeRedChannel ? fadedR : r;
    var finalG = FadeGreenChannel ? fadedG : g;
    var finalB = FadeBlueChannel ? fadedB : b;
    
    return (a << 24) | (finalB << 16) | (finalG << 8) | finalR;
}
```

### Fade Animation
```csharp
private void UpdateFadeAnimation(float deltaTime)
{
    if (!EnableFadeAnimation)
        return;
    
    var animationProgress = (GetCurrentTime() * AnimationSpeed) % (Math.PI * 2);
    
    switch (AnimationMode)
    {
        case 0: // Pulsing
            var pulse = (Math.Sin(animationProgress) + 1.0f) * 0.5f;
            FadeLength = 10.0f + pulse * 50.0f;
            break;
            
        case 1: // Oscillating
            var oscillation = Math.Sin(animationProgress * 2);
            FadeLength = 30.0f + oscillation * 30.0f;
            break;
            
        case 2: // Wave pattern
            var wave = Math.Sin(animationProgress * 3);
            FadeLength = 25.0f + wave * 25.0f;
            break;
            
        case 3: // Random walk
            if (Random.NextDouble() < 0.02f) // 2% chance per frame
            {
                FadeLength = Random.Next(10, 80);
            }
            break;
    }
}
```

### Fade Masking
```csharp
private int ApplyFadeMasking(int originalColor, int fadedColor, int pixelIndex)
{
    if (!EnableFadeMasking || FadeMask == null)
        return fadedColor;
    
    var maskPixel = FadeMask.Pixels[pixelIndex];
    var maskIntensity = (maskPixel & 0xFF) / 255.0f; // Use red channel as mask
    
    // Blend original and faded based on mask
    var blendFactor = maskIntensity * MaskInfluence;
    var finalColor = BlendColors(originalColor, fadedColor, blendFactor);
    
    return finalColor;
}

private int BlendColors(int color1, int color2, float blendFactor)
{
    var r1 = color1 & 0xFF;
    var g1 = (color1 >> 8) & 0xFF;
    var b1 = (color1 >> 16) & 0xFF;
    
    var r2 = color2 & 0xFF;
    var g2 = (color2 >> 8) & 0xFF;
    var b2 = (color2 >> 16) & 0xFF;
    
    var r = (int)(r1 + (r2 - r1) * blendFactor);
    var g = (int)(g1 + (g2 - g1) * blendFactor);
    var b = (int)(b1 + (b2 - b1) * blendFactor);
    
    return (b << 16) | (g << 8) | r;
}
```

## Performance Considerations

### Optimization Strategies
- **Lookup Tables**: Pre-computed fade values eliminate runtime calculations
- **SIMD Operations**: Use vectorized operations for pixel processing
- **Memory Access**: Optimize pixel buffer access patterns
- **Early Exit**: Skip processing when fade length is zero
- **Table Caching**: Cache fade tables when parameters don't change

### Memory Management
- **Buffer Reuse**: Reuse ImageBuffer instances when possible
- **Table Storage**: Efficient storage of fade lookup tables
- **Garbage Collection**: Avoid allocations in hot paths

## Integration with EffectGraph

### Input Ports
- **Image Input**: Source image buffer for fadeout processing
- **Audio Input**: Audio features for beat-reactive behavior
- **Mask Input**: Optional fade mask image
- **Control Input**: External control signals for dynamic parameters

### Output Ports
- **Faded Image**: The processed image with applied fadeout
- **Fade Data**: Fade parameters and lookup table metadata
- **Performance Metrics**: Processing time and memory usage

### Node Configuration
```csharp
public override void ConfigureNode()
{
    AddInputPort("Image", typeof(ImageBuffer));
    AddInputPort("Audio", typeof(AudioFeatures));
    AddInputPort("Mask", typeof(ImageBuffer));
    AddInputPort("Control", typeof(float));
    
    AddOutputPort("FadedImage", typeof(ImageBuffer));
    AddOutputPort("FadeData", typeof(FadeoutData));
    AddOutputPort("Performance", typeof(PerformanceMetrics));
    
    SetMetadata(new EffectMetadata
    {
        Name = "Fadeout",
        Category = "Color Transformation",
        Description = "Gradually fades image colors toward a target color",
        Version = "1.0.0",
        Author = "Phoenix Visualizer Team"
    });
}
```

## Advanced Features

### Multi-Target Fade
```csharp
private int GetCurrentTargetColor(AudioFeatures audio)
{
    if (!BeatReactive || audio == null)
        return TargetColor;
    
    // Cycle through multiple target colors based on beat
    var beatCount = audio.BeatCount % 4;
    var targetColors = new int[] { 0x000000, 0x800000, 0x008000, 0x000080 };
    
    return targetColors[beatCount];
}
```

### Fade Inversion
```csharp
private int ApplyFadeInversion(int color)
{
    if (!EnableFadeInversion)
        return color;
    
    var brightness = ((color & 0xFF) + ((color >> 8) & 0xFF) + ((color >> 16) & 0xFF)) / (3.0f * 255.0f);
    
    if (brightness > InversionThreshold)
    {
        // Invert the fade for bright pixels
        return InvertFadeColor(color);
    }
    
    return color;
}

private int InvertFadeColor(int color)
{
    var r = 255 - (color & 0xFF);
    var g = 255 - ((color >> 8) & 0xFF);
    var b = 255 - ((color >> 16) & 0xFF);
    
    return (color & 0xFF000000) | (b << 16) | (g << 8) | r;
}
```

## Testing and Validation

### Unit Tests
```csharp
[Test]
public void TestBasicFadeout()
{
    var node = new FadeoutEffectsNode
    {
        FadeLength = 25.0f,
        TargetColor = 0x000000, // Black
        BeatReactive = false
    };
    
    var input = CreateTestImage(100, 100);
    var audio = CreateTestAudio();
    
    var output = node.ProcessEffect(input, audio);
    
    Assert.IsNotNull(output);
    Assert.AreEqual(100, output.Width);
    Assert.AreEqual(100, output.Height);
    
    // Test that colors are faded toward target
    var testPixel = 0xFFFFFF; // White
    var expectedPixel = 0xE6E6E6; // Faded white
    
    var fadedPixel = node.ApplyFadeout(testPixel);
    Assert.AreEqual(expectedPixel, fadedPixel & 0x00FFFFFF);
}
```

### Performance Tests
```csharp
[Test]
public void TestPerformance()
{
    var node = new FadeoutEffectsNode();
    var input = CreateTestImage(1920, 1080);
    var audio = CreateTestAudio();
    
    var stopwatch = Stopwatch.StartNew();
    var output = node.ProcessEffect(input, audio);
    stopwatch.Stop();
    
    var processingTime = stopwatch.ElapsedMilliseconds;
    Assert.Less(processingTime, 40); // Should process in under 40ms
}
```

## Future Enhancements

### Planned Features
- **HSV/HSL Fade**: Fade operations in different color spaces
- **Gradient Fade**: Directional fade effects
- **Real-time Control**: MIDI and OSC control integration
- **Fade Presets**: Predefined fade patterns and styles
- **3D Fade**: Depth-based fade effects

### Research Areas
- **Color Theory**: Advanced color manipulation algorithms
- **Perceptual Effects**: Human visual system considerations
- **Machine Learning**: AI-generated fade patterns
- **Real-time Analysis**: Dynamic fade parameter adjustment

## Conclusion
The Fadeout effect provides powerful color transformation capabilities with extensive customization options. Its beat-reactive nature and lookup table optimization make it suitable for creating dynamic, engaging visualizations. The implementation balances performance with flexibility, offering both real-time processing and high-quality output suitable for professional visualization applications.

The effect's ability to create smooth color transitions and atmospheric washes makes it an essential tool for AVS preset creation, allowing artists to create mood-enhancing effects, smooth color changes, and visually appealing transformations that respond to music.
