# AVS Fast Brightness Effect

## Overview
The Fast Brightness effect is an AVS visualization effect that provides high-performance brightness adjustment for images. It offers three modes: brighten (multiply by 2), darken (divide by 2), and off. The effect is optimized for speed using MMX assembly instructions and lookup tables, making it ideal for real-time brightness adjustments in performance-critical applications.

## C++ Source Analysis
Based on the AVS source code in `r_fastbright.cpp`, the Fast Brightness effect inherits from `C_RBASE` and implements optimized brightness operations:

### Key Properties
- **Direction Mode**: Three operation modes (0=brighten, 1=darken, 2=off)
- **MMX Optimization**: Uses MMX assembly for high-performance processing
- **Lookup Tables**: Pre-computed brightness values for non-MMX fallback
- **Vector Processing**: Processes 8 pixels simultaneously using MMX
- **Performance Focus**: Designed for maximum speed over quality

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

#ifdef NO_MMX 
    int tab[3][256]; // Lookup tables for R, G, B channels
#endif
    
    int dir; // Direction mode (0=brighten, 1=darken, 2=off)
};
```

### Operation Modes
1. **Mode 0 (Brighten)**: Multiplies each RGB channel by 2 (saturates at 255)
2. **Mode 1 (Darken)**: Divides each RGB channel by 2 (preserves alpha)
3. **Mode 2 (Off)**: No processing, original image unchanged

### MMX Implementation
- **Brighten Mode**: Uses `paddusb` (packed unsigned byte addition) to double values
- **Darken Mode**: Uses `psrl` (packed shift right logical) and `pand` (packed AND) for division
- **Vector Processing**: Processes 8 pixels per MMX instruction cycle

## C# Implementation

### Class Definition
```csharp
public class FastBrightnessEffectsNode : BaseEffectNode
{
    public int BrightnessMode { get; set; } = 0; // 0=Brighten, 1=Darken, 2=Off
    public bool BeatReactive { get; set; } = false;
    public float BeatBrightnessMultiplier { get; set; } = 1.5f;
    public bool EnableSmoothTransition { get; set; } = false;
    public float TransitionSpeed { get; set; } = 1.0f;
    public bool EnableChannelSelectiveBrightness { get; set; } = false;
    public bool BrightenRedChannel { get; set; } = true;
    public bool BrightenGreenChannel { get; set; } = true;
    public bool BrightenBlueChannel { get; set; } = true;
    public bool EnableBrightnessAnimation { get; set; } = false;
    public float AnimationSpeed { get; set; } = 1.0f;
    public int AnimationMode { get; set; } = 0;
    public bool EnableBrightnessMasking { get; set; } = false;
    public ImageBuffer BrightnessMask { get; set; } = null;
    public float MaskInfluence { get; set; } = 1.0f;
    public bool EnableBrightnessBlending { get; set; } = false;
    public float BrightnessBlendStrength { get; set; } = 0.5f;
    public int BrightnessAlgorithm { get; set; } = 0; // 0=Fast, 1=Quality, 2=Adaptive
    public float BrightnessCurve { get; set; } = 1.0f; // Power curve for brightness
    public bool EnableBrightnessClamping { get; set; } = true;
    public int ClampMode { get; set; } = 0; // 0=Standard, 1=Soft, 2=Hard
    public bool EnableBrightnessInversion { get; set; } = false;
    public float InversionThreshold { get; set; } = 0.5f;
}
```

### Key Features
- **Three Brightness Modes**: Brighten, darken, and off with instant switching
- **Beat Reactivity**: Dynamic brightness mode switching synchronized with audio
- **Smooth Transitions**: Optional smooth brightness transitions
- **Channel Selective**: Adjust individual RGB channels independently
- **Brightness Animation**: Animated brightness effects and patterns
- **Brightness Masking**: Use image masks to control brightness areas
- **Brightness Blending**: Blend brightened and original images
- **Multiple Algorithms**: Fast, quality, and adaptive brightness methods
- **Performance Optimization**: Optimized for maximum speed

### Usage Examples

#### Basic Brightness Increase
```csharp
var brightenNode = new FastBrightnessEffectsNode
{
    BrightnessMode = 0, // Brighten mode
    BeatReactive = false,
    EnableSmoothTransition = false
};
```

#### Beat-Reactive Brightness
```csharp
var beatBrightnessNode = new FastBrightnessEffectsNode
{
    BrightnessMode = 0, // Start with brighten
    BeatReactive = true,
    BeatBrightnessMultiplier = 2.0f,
    EnableSmoothTransition = true,
    TransitionSpeed = 2.0f
};
```

#### Artistic Channel Brightness
```csharp
var artisticBrightnessNode = new FastBrightnessEffectsNode
{
    BrightnessMode = 0, // Brighten mode
    BeatReactive = false,
    EnableChannelSelectiveBrightness = true,
    BrightenRedChannel = true,
    BrightenGreenChannel = false,
    BrightenBlueChannel = true,
    EnableBrightnessAnimation = true,
    AnimationSpeed = 1.5f,
    AnimationMode = 1 // Oscillating
};
```

## Technical Implementation

### Core Processing Algorithm
```csharp
protected override ImageBuffer ProcessEffect(ImageBuffer input, AudioFeatures audio)
{
    if (BrightnessMode == 2) // Off mode
        return input;
    
    var output = new ImageBuffer(input.Width, input.Height);
    var currentMode = GetCurrentBrightnessMode(audio);
    
    // Process each pixel
    for (int i = 0; i < input.Pixels.Length; i++)
    {
        var originalColor = input.Pixels[i];
        var processedColor = ApplyBrightness(originalColor, currentMode);
        
        // Apply channel selective brightness if enabled
        if (EnableChannelSelectiveBrightness)
        {
            processedColor = ApplyChannelSelectiveBrightness(originalColor, processedColor, currentMode);
        }
        
        // Apply brightness masking if enabled
        if (EnableBrightnessMasking && BrightnessMask != null)
        {
            processedColor = ApplyBrightnessMasking(originalColor, processedColor, i);
        }
        
        // Apply brightness blending if enabled
        if (EnableBrightnessBlending)
        {
            processedColor = BlendBrightness(originalColor, processedColor);
        }
        
        output.Pixels[i] = processedColor;
    }
    
    return output;
}
```

### Brightness Application
```csharp
private int ApplyBrightness(int color, int mode)
{
    var r = color & 0xFF;
    var g = (color >> 8) & 0xFF;
    var b = (color >> 16) & 0xFF;
    var a = (color >> 24) & 0xFF;
    
    switch (mode)
    {
        case 0: // Brighten
            return ApplyBrightenMode(r, g, b, a);
        case 1: // Darken
            return ApplyDarkenMode(r, g, b, a);
        default:
            return color;
    }
}

private int ApplyBrightenMode(int r, int g, int b, int a)
{
    switch (BrightnessAlgorithm)
    {
        case 0: // Fast (original behavior)
            r = Math.Min(255, r * 2);
            g = Math.Min(255, g * 2);
            b = Math.Min(255, b * 2);
            break;
            
        case 1: // Quality
            r = (int)Math.Min(255, r * (1.0f + BrightnessCurve));
            g = (int)Math.Min(255, g * (1.0f + BrightnessCurve));
            b = (int)Math.Min(255, b * (1.0f + BrightnessCurve));
            break;
            
        case 2: // Adaptive
            var brightness = (r + g + b) / (3.0f * 255.0f);
            var adaptiveMultiplier = 1.0f + (BrightnessCurve * (1.0f - brightness));
            r = (int)Math.Min(255, r * adaptiveMultiplier);
            g = (int)Math.Min(255, g * adaptiveMultiplier);
            b = (int)Math.Min(255, b * adaptiveMultiplier);
            break;
    }
    
    return (a << 24) | (b << 16) | (g << 8) | r;
}

private int ApplyDarkenMode(int r, int g, int b, int a)
{
    switch (BrightnessAlgorithm)
    {
        case 0: // Fast (original behavior)
            r = r >> 1;
            g = g >> 1;
            b = b >> 1;
            break;
            
        case 1: // Quality
            var darkenFactor = 1.0f / (1.0f + BrightnessCurve);
            r = (int)(r * darkenFactor);
            g = (int)(g * darkenFactor);
            b = (int)(b * darkenFactor);
            break;
            
        case 2: // Adaptive
            var brightness = (r + g + b) / (3.0f * 255.0f);
            var adaptiveFactor = 1.0f / (1.0f + (BrightnessCurve * brightness));
            r = (int)(r * adaptiveFactor);
            g = (int)(g * adaptiveFactor);
            b = (int)(b * adaptiveFactor);
            break;
    }
    
    return (a << 24) | (b << 16) | (g << 8) | r;
}
```

### Beat-Reactive Mode Switching
```csharp
private int GetCurrentBrightnessMode(AudioFeatures audio)
{
    if (!BeatReactive || audio == null)
        return BrightnessMode;
    
    if (audio.IsBeat)
    {
        // Switch to brighten mode on beat
        return 0;
    }
    else
    {
        // Return to original mode
        return BrightnessMode;
    }
}
```

### Channel Selective Brightness
```csharp
private int ApplyChannelSelectiveBrightness(int originalColor, int processedColor, int mode)
{
    if (!EnableChannelSelectiveBrightness)
        return processedColor;
    
    var r = originalColor & 0xFF;
    var g = (originalColor >> 8) & 0xFF;
    var b = (originalColor >> 16) & 0xFF;
    var a = (originalColor >> 24) & 0xFF;
    
    var processedR = processedColor & 0xFF;
    var processedG = (processedColor >> 8) & 0xFF;
    var processedB = (processedColor >> 16) & 0xFF;
    
    var finalR = BrightenRedChannel ? processedR : r;
    var finalG = BrightenGreenChannel ? processedG : g;
    var finalB = BrightenBlueChannel ? processedB : b;
    
    return (a << 24) | (finalB << 16) | (finalG << 8) | finalR;
}
```

### Brightness Animation
```csharp
private void UpdateBrightnessAnimation(float deltaTime)
{
    if (!EnableBrightnessAnimation)
        return;
    
    var animationProgress = (GetCurrentTime() * AnimationSpeed) % (Math.PI * 2);
    
    switch (AnimationMode)
    {
        case 0: // Pulsing
            var pulse = (Math.Sin(animationProgress) + 1.0f) * 0.5f;
            BrightnessCurve = 0.5f + pulse * 1.5f;
            break;
            
        case 1: // Oscillating
            var oscillation = Math.Sin(animationProgress * 2);
            if (oscillation > 0)
            {
                BrightnessMode = 0; // Brighten
            }
            else
            {
                BrightnessMode = 1; // Darken
            }
            break;
            
        case 2: // Wave pattern
            var wave = Math.Sin(animationProgress * 3);
            BrightnessCurve = 1.0f + wave * 0.5f;
            break;
            
        case 3: // Random flicker
            if (Random.NextDouble() < 0.01f) // 1% chance per frame
            {
                BrightnessMode = Random.Next(0, 2);
            }
            break;
    }
}
```

### Brightness Masking
```csharp
private int ApplyBrightnessMasking(int originalColor, int processedColor, int pixelIndex)
{
    if (!EnableBrightnessMasking || BrightnessMask == null)
        return processedColor;
    
    var maskPixel = BrightnessMask.Pixels[pixelIndex];
    var maskIntensity = (maskPixel & 0xFF) / 255.0f; // Use red channel as mask
    
    // Blend original and processed based on mask
    var blendFactor = maskIntensity * MaskInfluence;
    var finalColor = BlendColors(originalColor, processedColor, blendFactor);
    
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
- **Lookup Tables**: Pre-computed brightness values for consistent performance
- **SIMD Operations**: Use vectorized operations for pixel processing
- **Early Exit**: Skip processing for off mode
- **Memory Access**: Optimize pixel buffer access patterns
- **Algorithm Selection**: Choose appropriate brightness method for performance vs. quality

### Memory Management
- **Buffer Reuse**: Reuse ImageBuffer instances when possible
- **Table Storage**: Efficient storage of brightness lookup tables
- **Garbage Collection**: Avoid allocations in hot paths

## Integration with EffectGraph

### Input Ports
- **Image Input**: Source image buffer for brightness adjustment
- **Audio Input**: Audio features for beat-reactive behavior
- **Mask Input**: Optional brightness mask image
- **Control Input**: External control signals for dynamic parameters

### Output Ports
- **Brightness Adjusted Image**: The processed image with applied brightness
- **Brightness Data**: Brightness parameters and metadata
- **Performance Metrics**: Processing time and memory usage

### Node Configuration
```csharp
public override void ConfigureNode()
{
    AddInputPort("Image", typeof(ImageBuffer));
    AddInputPort("Audio", typeof(AudioFeatures));
    AddInputPort("Mask", typeof(ImageBuffer));
    AddInputPort("Control", typeof(float));
    
    AddOutputPort("BrightnessAdjustedImage", typeof(ImageBuffer));
    AddOutputPort("BrightnessData", typeof(BrightnessData));
    AddOutputPort("Performance", typeof(PerformanceMetrics));
    
    SetMetadata(new EffectMetadata
    {
        Name = "Fast Brightness",
        Category = "Color Adjustment",
        Description = "High-performance brightness adjustment with MMX optimization",
        Version = "1.0.0",
        Author = "Phoenix Visualizer Team"
    });
}
```

## Advanced Features

### Adaptive Brightness
```csharp
private float CalculateAdaptiveBrightness(int color)
{
    var r = color & 0xFF;
    var g = (color >> 8) & 0xFF;
    var b = (color >> 16) & 0xFF;
    
    var brightness = (r + g + b) / (3.0f * 255.0f);
    var contrast = CalculateContrast(r, g, b);
    
    // Adjust brightness based on image characteristics
    if (brightness < 0.3f && contrast > 0.5f)
    {
        // Dark image with high contrast - increase brightness
        return 1.0f + BrightnessCurve;
    }
    else if (brightness > 0.7f && contrast < 0.3f)
    {
        // Bright image with low contrast - decrease brightness
        return 1.0f / (1.0f + BrightnessCurve);
    }
    
    return 1.0f;
}

private float CalculateContrast(int r, int g, int b)
{
    var mean = (r + g + b) / 3.0f;
    var variance = Math.Pow(r - mean, 2) + Math.Pow(g - mean, 2) + Math.Pow(b - mean, 2);
    return (float)Math.Sqrt(variance) / 255.0f;
}
```

### Brightness Clamping
```csharp
private int ApplyBrightnessClamping(int color)
{
    if (!EnableBrightnessClamping)
        return color;
    
    var r = color & 0xFF;
    var g = (color >> 8) & 0xFF;
    var b = (color >> 16) & 0xFF;
    
    switch (ClampMode)
    {
        case 0: // Standard
            r = Math.Max(0, Math.Min(255, r));
            g = Math.Max(0, Math.Min(255, g));
            b = Math.Max(0, Math.Min(255, b));
            break;
            
        case 1: // Soft
            r = (int)(255.0f / (1.0f + Math.Exp(-r / 64.0f)));
            g = (int)(255.0f / (1.0f + Math.Exp(-g / 64.0f)));
            b = (int)(255.0f / (1.0f + Math.Exp(-b / 64.0f)));
            break;
            
        case 2: // Hard
            r = r < 0 ? 0 : (r > 255 ? 255 : r);
            g = g < 0 ? 0 : (g > 255 ? 255 : g);
            b = b < 0 ? 0 : (b > 255 ? 255 : b);
            break;
    }
    
    return (color & 0xFF000000) | (b << 16) | (g << 8) | r;
}
```

## Testing and Validation

### Unit Tests
```csharp
[Test]
public void TestBrightenMode()
{
    var node = new FastBrightnessEffectsNode
    {
        BrightnessMode = 0, // Brighten
        BrightnessAlgorithm = 0 // Fast
    };
    
    var input = CreateTestImage(100, 100);
    var audio = CreateTestAudio();
    
    var output = node.ProcessEffect(input, audio);
    
    Assert.IsNotNull(output);
    Assert.AreEqual(100, output.Width);
    Assert.AreEqual(100, output.Height);
    
    // Test that colors are brightened
    var testPixel = 0x404040; // RGB(64,64,64)
    var expectedPixel = 0x808080; // RGB(128,128,128) - doubled
    
    var brightenedPixel = node.ApplyBrightness(testPixel, 0);
    Assert.AreEqual(expectedPixel, brightenedPixel & 0x00FFFFFF);
}
```

### Performance Tests
```csharp
[Test]
public void TestPerformance()
{
    var node = new FastBrightnessEffectsNode();
    var input = CreateTestImage(1920, 1080);
    var audio = CreateTestAudio();
    
    var stopwatch = Stopwatch.StartNew();
    var output = node.ProcessEffect(input, audio);
    stopwatch.Stop();
    
    var processingTime = stopwatch.ElapsedMilliseconds;
    Assert.Less(processingTime, 25); // Should process in under 25ms for fast mode
}
```

## Future Enhancements

### Planned Features
- **GPU Acceleration**: OpenGL/OpenCL implementation for real-time processing
- **Advanced Algorithms**: More sophisticated brightness adjustment methods
- **Real-time Control**: MIDI and OSC control integration
- **Brightness Presets**: Predefined brightness patterns and styles
- **3D Brightness**: Depth-based brightness effects

### Research Areas
- **Perceptual Brightness**: Human visual system considerations
- **Machine Learning**: AI-generated brightness optimization
- **Real-time Analysis**: Dynamic brightness parameter adjustment
- **Color Theory**: Advanced color manipulation algorithms

## Conclusion
The Fast Brightness effect provides high-performance brightness adjustment capabilities with extensive customization options. Its beat-reactive nature and MMX optimization make it suitable for creating dynamic, engaging visualizations. The implementation balances performance with flexibility, offering both real-time processing and high-quality output suitable for professional visualization applications.

The effect's ability to provide instant brightness changes with minimal computational overhead makes it an essential tool for AVS preset creation, allowing artists to create dramatic lighting effects, mood changes, and visually striking transformations that respond to music in real-time.
