# AVS Invert Effect

## Overview
The Invert effect is an AVS visualization effect that inverts the colors of an image by performing a bitwise XOR operation with white (0xFFFFFF). This creates a photographic negative effect where dark colors become light and light colors become dark. The effect is simple but powerful, providing instant visual transformation with minimal computational overhead.

## C++ Source Analysis
Based on the AVS source code in `r_invert.cpp`, the Invert effect inherits from `C_RBASE` and implements efficient color inversion:

### Key Properties
- **Enabled State**: Simple on/off toggle for the effect
- **MMX Optimization**: Uses MMX assembly instructions for high-performance processing
- **Fallback Support**: Non-MMX fallback for compatibility
- **Alpha Channel Safe**: Preserves alpha channel during inversion
- **Minimal Configuration**: Simple boolean configuration

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

    int enabled;           // Effect enabled/disabled flag
};
```

### Inversion Algorithm
The effect performs a bitwise XOR operation with 0xFFFFFF (white) on each pixel:
- **MMX Version**: Processes 8 pixels at once using MMX instructions
- **Fallback Version**: Processes pixels individually using XOR operation
- **Result**: Each color channel is inverted (255 - original_value)

## C# Implementation

### Class Definition
```csharp
public class InvertEffectsNode : BaseEffectNode
{
    public bool Enabled { get; set; } = true;
    public bool BeatReactive { get; set; } = false;
    public float BeatIntensity { get; set; } = 1.0f;
    public bool EnablePartialInversion { get; set; } = false;
    public float InversionStrength { get; set; } = 1.0f;
    public int InversionMode { get; set; } = 0;
    public bool EnableChannelSelectiveInversion { get; set; } = false;
    public bool InvertRedChannel { get; set; } = true;
    public bool InvertGreenChannel { get; set; } = true;
    public bool InvertBlueChannel { get; set; } = true;
    public bool EnableThresholdInversion { get; set; } = false;
    public float InversionThreshold { get; set; } = 0.5f;
    public bool EnableSmoothInversion { get; set; } = false;
    public float SmoothInversionSpeed { get; set; } = 1.0f;
    public bool EnableInversionAnimation { get; set; } = false;
    public float AnimationSpeed { get; set; } = 1.0f;
    public int AnimationMode { get; set; } = 0;
    public bool EnableInversionMasking { get; set; } = false;
    public ImageBuffer InversionMask { get; set; } = null;
    public float MaskInfluence { get; set; } = 1.0f;
    public bool EnableInversionBlending { get; set; } = false;
    public float BlendMode { get; set; } = 0.5f;
}
```

### Key Features
- **Simple Inversion**: Basic color inversion with minimal overhead
- **Beat Reactivity**: Optional beat-synchronized inversion intensity
- **Partial Inversion**: Configurable inversion strength (0.0 to 1.0)
- **Channel Selective**: Invert individual RGB channels independently
- **Threshold Inversion**: Invert only colors above/below threshold
- **Smooth Inversion**: Gradual inversion transitions
- **Inversion Animation**: Animated inversion effects
- **Inversion Masking**: Use image masks to control inversion areas
- **Inversion Blending**: Blend inverted and original images
- **Performance Optimization**: Optimized pixel processing algorithms

### Usage Examples

#### Basic Color Inversion
```csharp
var invertNode = new InvertEffectsNode
{
    Enabled = true,
    BeatReactive = false,
    InversionStrength = 1.0f,
    InvertRedChannel = true,
    InvertGreenChannel = true,
    InvertBlueChannel = true
};
```

#### Beat-Reactive Partial Inversion
```csharp
var beatInvertNode = new InvertEffectsNode
{
    Enabled = true,
    BeatReactive = true,
    BeatIntensity = 2.0f,
    EnablePartialInversion = true,
    InversionStrength = 0.7f,
    EnableSmoothInversion = true,
    SmoothInversionSpeed = 1.5f
};
```

#### Channel-Selective Inversion
```csharp
var selectiveInvertNode = new InvertEffectsNode
{
    Enabled = true,
    BeatReactive = false,
    EnableChannelSelectiveInversion = true,
    InvertRedChannel = true,
    InvertGreenChannel = false,
    InvertBlueChannel = true,
    EnableThresholdInversion = true,
    InversionThreshold = 0.6f
};
```

## Technical Implementation

### Core Processing Algorithm
```csharp
protected override ImageBuffer ProcessEffect(ImageBuffer input, AudioFeatures audio)
{
    if (!Enabled)
        return input;
    
    var output = new ImageBuffer(input.Width, input.Height);
    var currentInversionStrength = GetCurrentInversionStrength(audio);
    
    // Process each pixel
    for (int i = 0; i < input.Pixels.Length; i++)
    {
        var originalColor = input.Pixels[i];
        var invertedColor = InvertPixel(originalColor, currentInversionStrength);
        
        // Apply channel selective inversion if enabled
        if (EnableChannelSelectiveInversion)
        {
            invertedColor = ApplyChannelSelectiveInversion(originalColor, invertedColor);
        }
        
        // Apply threshold inversion if enabled
        if (EnableThresholdInversion)
        {
            invertedColor = ApplyThresholdInversion(originalColor, invertedColor);
        }
        
        // Apply inversion masking if enabled
        if (EnableInversionMasking && InversionMask != null)
        {
            invertedColor = ApplyInversionMasking(originalColor, invertedColor, i);
        }
        
        // Apply inversion blending if enabled
        if (EnableInversionBlending)
        {
            invertedColor = BlendInversion(originalColor, invertedColor);
        }
        
        output.Pixels[i] = invertedColor;
    }
    
    return output;
}
```

### Pixel Inversion Implementation
```csharp
private int InvertPixel(int color, float strength)
{
    if (strength <= 0.0f)
        return color;
    
    if (strength >= 1.0f)
        return InvertPixelFull(color);
    
    // Partial inversion
    var r = color & 0xFF;
    var g = (color >> 8) & 0xFF;
    var b = (color >> 16) & 0xFF;
    var a = (color >> 24) & 0xFF;
    
    var invertedR = (int)(r + (255 - r) * strength);
    var invertedG = (int)(g + (255 - g) * strength);
    var invertedB = (int)(b + (255 - b) * strength);
    
    return (a << 24) | (invertedB << 16) | (invertedG << 8) | invertedR;
}

private int InvertPixelFull(int color)
{
    // Full inversion using XOR (equivalent to 255 - value for each channel)
    return color ^ 0x00FFFFFF; // Preserve alpha channel
}
```

### Beat-Reactive Inversion
```csharp
private float GetCurrentInversionStrength(AudioFeatures audio)
{
    if (!BeatReactive || audio == null)
        return InversionStrength;
    
    var beatMultiplier = 1.0f;
    
    if (audio.IsBeat)
    {
        beatMultiplier = BeatIntensity;
    }
    else
    {
        // Gradual return to normal
        beatMultiplier = 1.0f + (BeatIntensity - 1.0f) * audio.BeatIntensity;
    }
    
    return Math.Max(0.0f, Math.Min(1.0f, InversionStrength * beatMultiplier));
}
```

### Channel Selective Inversion
```csharp
private int ApplyChannelSelectiveInversion(int originalColor, int invertedColor)
{
    if (!EnableChannelSelectiveInversion)
        return invertedColor;
    
    var r = originalColor & 0xFF;
    var g = (originalColor >> 8) & 0xFF;
    var b = (originalColor >> 16) & 0xFF;
    var a = (originalColor >> 24) & 0xFF;
    
    var invertedR = invertedColor & 0xFF;
    var invertedG = (invertedColor >> 8) & 0xFF;
    var invertedB = (invertedColor >> 16) & 0xFF;
    
    var finalR = InvertRedChannel ? invertedR : r;
    var finalG = InvertGreenChannel ? invertedG : g;
    var finalB = InvertBlueChannel ? invertedB : b;
    
    return (a << 24) | (finalB << 16) | (finalG << 8) | finalR;
}
```

### Threshold Inversion
```csharp
private int ApplyThresholdInversion(int originalColor, int invertedColor)
{
    if (!EnableThresholdInversion)
        return invertedColor;
    
    var r = originalColor & 0xFF;
    var g = (originalColor >> 8) & 0xFF;
    var b = (originalColor >> 16) & 0xFF;
    
    // Calculate normalized brightness
    var brightness = (r + g + b) / (3.0f * 255.0f);
    
    if (brightness > InversionThreshold)
    {
        // Only invert bright pixels
        return invertedColor;
    }
    
    return originalColor;
}
```

### Inversion Masking
```csharp
private int ApplyInversionMasking(int originalColor, int invertedColor, int pixelIndex)
{
    if (!EnableInversionMasking || InversionMask == null)
        return invertedColor;
    
    var maskPixel = InversionMask.Pixels[pixelIndex];
    var maskIntensity = (maskPixel & 0xFF) / 255.0f; // Use red channel as mask
    
    // Blend original and inverted based on mask
    var blendFactor = maskIntensity * MaskInfluence;
    var finalColor = BlendColors(originalColor, invertedColor, blendFactor);
    
    return finalColor;
}

private int BlendColors(int color1, int color2, float blendFactor)
{
    var r1 = color1 & 0xFF;
    var g1 = (color1 >> 8) & 0xFF;
    var b1 = (color1 >> 16) & 0xFF;
    var a1 = (color1 >> 24) & 0xFF;
    
    var r2 = color2 & 0xFF;
    var g2 = (color2 >> 8) & 0xFF;
    var b2 = (color2 >> 16) & 0xFF;
    
    var r = (int)(r1 + (r2 - r1) * blendFactor);
    var g = (int)(g1 + (g2 - g1) * blendFactor);
    var b = (int)(b1 + (b2 - b1) * blendFactor);
    
    return (a1 << 24) | (b << 16) | (g << 8) | r;
}
```

### Smooth Inversion
```csharp
private float GetSmoothInversionProgress()
{
    if (!EnableSmoothInversion)
        return 1.0f;
    
    var time = GetCurrentTime();
    var progress = (time * SmoothInversionSpeed) % (Math.PI * 2);
    
    // Smooth sine wave transition
    return (Math.Sin(progress) + 1.0f) * 0.5f;
}
```

### Inversion Animation
```csharp
private void UpdateInversionAnimation(float deltaTime)
{
    if (!EnableInversionAnimation)
        return;
    
    var animationProgress = (GetCurrentTime() * AnimationSpeed) % (Math.PI * 2);
    
    switch (AnimationMode)
    {
        case 0: // Pulsing
            var pulse = (Math.Sin(animationProgress) + 1.0f) * 0.5f;
            InversionStrength = 0.3f + pulse * 0.7f;
            break;
            
        case 1: // Wave pattern
            var wave = Math.Sin(animationProgress * 3);
            InversionStrength = 0.5f + wave * 0.5f;
            break;
            
        case 2: // Random flicker
            if (Random.NextDouble() < 0.02f) // 2% chance per frame
            {
                InversionStrength = Random.Next(0, 100) / 100.0f;
            }
            break;
            
        case 3: // Rotating channels
            var channelRotation = (animationProgress / (Math.PI * 2)) * 3;
            var channelIndex = (int)channelRotation;
            var channelProgress = channelRotation - channelIndex;
            
            InvertRedChannel = (channelIndex == 0);
            InvertGreenChannel = (channelIndex == 1);
            InvertBlueChannel = (channelIndex == 2);
            break;
    }
}
```

## Performance Considerations

### Optimization Strategies
- **SIMD Operations**: Use vectorized operations for pixel processing
- **Memory Access**: Optimize pixel buffer access patterns
- **Early Exit**: Skip processing when disabled
- **Lookup Tables**: Pre-compute inversion values where possible
- **Parallel Processing**: Process image regions in parallel

### Memory Management
- **Buffer Reuse**: Reuse ImageBuffer instances when possible
- **Temporary Buffers**: Minimize temporary buffer allocations
- **Garbage Collection**: Avoid allocations in hot paths

## Integration with EffectGraph

### Input Ports
- **Image Input**: Source image buffer for inversion
- **Audio Input**: Audio features for beat-reactive behavior
- **Mask Input**: Optional inversion mask image
- **Control Input**: External control signals for dynamic parameters

### Output Ports
- **Inverted Image**: The processed image with applied inversion
- **Inversion Data**: Inversion parameters and metadata
- **Performance Metrics**: Processing time and memory usage

### Node Configuration
```csharp
public override void ConfigureNode()
{
    AddInputPort("Image", typeof(ImageBuffer));
    AddInputPort("Audio", typeof(AudioFeatures));
    AddInputPort("Mask", typeof(ImageBuffer));
    AddInputPort("Control", typeof(float));
    
    AddOutputPort("InvertedImage", typeof(ImageBuffer));
    AddOutputPort("InversionData", typeof(InversionData));
    AddOutputPort("Performance", typeof(PerformanceMetrics));
    
    SetMetadata(new EffectMetadata
    {
        Name = "Invert",
        Category = "Color Transformation",
        Description = "Inverts image colors with configurable strength and channel selection",
        Version = "1.0.0",
        Author = "Phoenix Visualizer Team"
    });
}
```

## Advanced Features

### Multi-Pass Inversion
```csharp
private ImageBuffer ProcessMultiPassInversion(ImageBuffer input, AudioFeatures audio)
{
    var current = input;
    var passCount = 2; // Number of inversion passes
    
    for (int pass = 0; pass < passCount; pass++)
    {
        var passStrength = InversionStrength / passCount;
        var passNode = new InvertEffectsNode
        {
            Enabled = true,
            BeatReactive = BeatReactive,
            BeatIntensity = BeatIntensity,
            EnablePartialInversion = EnablePartialInversion,
            InversionStrength = passStrength,
            InversionMode = InversionMode,
            EnableChannelSelectiveInversion = EnableChannelSelectiveInversion,
            InvertRedChannel = InvertRedChannel,
            InvertGreenChannel = InvertGreenChannel,
            InvertBlueChannel = InvertBlueChannel
        };
        
        current = passNode.ProcessEffect(current, audio);
    }
    
    return current;
}
```

### Adaptive Inversion
```csharp
private float CalculateAdaptiveInversionStrength(ImageBuffer input)
{
    var totalBrightness = 0.0f;
    var pixelCount = input.Pixels.Length;
    
    // Calculate average brightness
    for (int i = 0; i < pixelCount; i++)
    {
        var color = input.Pixels[i];
        var r = color & 0xFF;
        var g = (color >> 8) & 0xFF;
        var b = (color >> 16) & 0xFF;
        totalBrightness += (r + g + b) / (3.0f * 255.0f);
    }
    
    var averageBrightness = totalBrightness / pixelCount;
    
    // Adjust inversion strength based on image brightness
    if (averageBrightness > 0.7f)
    {
        // Bright image - increase inversion strength
        return Math.Min(1.0f, InversionStrength * 1.5f);
    }
    else if (averageBrightness < 0.3f)
    {
        // Dark image - decrease inversion strength
        return Math.Max(0.0f, InversionStrength * 0.7f);
    }
    
    return InversionStrength;
}
```

## Testing and Validation

### Unit Tests
```csharp
[Test]
public void TestBasicInversion()
{
    var node = new InvertEffectsNode
    {
        Enabled = true,
        InversionStrength = 1.0f
    };
    
    var input = CreateTestImage(100, 100);
    var audio = CreateTestAudio();
    
    var output = node.ProcessEffect(input, audio);
    
    Assert.IsNotNull(output);
    Assert.AreEqual(100, output.Width);
    Assert.AreEqual(100, output.Height);
    
    // Test that colors are inverted
    var testPixel = 0x000000; // Black
    var expectedPixel = 0xFFFFFF; // White
    
    var invertedPixel = node.InvertPixel(testPixel, 1.0f);
    Assert.AreEqual(expectedPixel, invertedPixel & 0x00FFFFFF);
}
```

### Performance Tests
```csharp
[Test]
public void TestPerformance()
{
    var node = new InvertEffectsNode();
    var input = CreateTestImage(1920, 1080);
    var audio = CreateTestAudio();
    
    var stopwatch = Stopwatch.StartNew();
    var output = node.ProcessEffect(input, audio);
    stopwatch.Stop();
    
    var processingTime = stopwatch.ElapsedMilliseconds;
    Assert.Less(processingTime, 30); // Should process in under 30ms
}
```

## Future Enhancements

### Planned Features
- **HSV/HSL Inversion**: Inversion in different color spaces
- **Selective Area Inversion**: Invert specific regions of the image
- **Real-time Control**: MIDI and OSC control integration
- **Inversion Presets**: Predefined inversion patterns
- **3D Inversion**: Depth-based inversion effects

### Research Areas
- **Perceptual Inversion**: Human visual system considerations
- **Machine Learning**: AI-generated inversion patterns
- **Real-time Analysis**: Dynamic inversion parameter adjustment
- **Color Theory**: Advanced color manipulation algorithms

## Conclusion
The Invert effect provides powerful color inversion capabilities with extensive customization options. Its beat-reactive nature and channel-selective features make it suitable for creating dynamic, engaging visualizations. The implementation balances performance with flexibility, offering both real-time processing and high-quality output suitable for professional visualization applications.

The effect's ability to create instant visual transformations through color inversion makes it an essential tool for AVS preset creation, allowing artists to create dramatic visual changes, explore negative color spaces, and create visually striking effects that respond to music.
