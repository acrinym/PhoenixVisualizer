# AVS Channel Shift Effect

## Overview
The Channel Shift effect is an AVS visualization effect that manipulates the RGB color channels of an image by swapping, rotating, or reordering the red, green, and blue components. This creates various color distortion effects that can be synchronized with audio beats for dynamic visual experiences.

## C++ Source Analysis
Based on the AVS source code in `r_chanshift.cpp`, the Channel Shift effect inherits from `C_RBASE` and implements six different RGB channel permutation modes:

### Key Properties
- **Mode**: Six different RGB channel arrangements (RGB, RBG, BRG, BGR, GBR, GRB)
- **Beat Reactivity**: Optional automatic mode switching on audio beats
- **Assembly Optimization**: Uses x86 assembly for high-performance channel manipulation

### Core Functionality
```cpp
class C_THISCLASS : public C_RBASE 
{
    typedef struct {
        int mode;      // Channel arrangement mode
        int onbeat;    // Beat synchronization flag
    } apeconfig;
    
    virtual int render(char visdata[2][2][576], int isBeat, int *framebuffer, int *fbout, int w, int h);
    virtual HWND conf(HINSTANCE hInstance, HWND hwndParent);
    virtual char *get_desc();
    virtual void load_config(unsigned char *data, int len);
    virtual int save_config(unsigned char *data);
    
    apeconfig config;
    HWND hwndDlg;
};
```

### Channel Modes
1. **RGB (0)**: No change - original colors
2. **RBG (1)**: Red-Blue-Green (swaps blue and green)
3. **BRG (2)**: Blue-Red-Green (rotates channels left)
4. **BGR (3)**: Blue-Green-Red (swaps red and blue)
5. **GBR (4)**: Green-Blue-Red (rotates channels right)
6. **GRB (5)**: Green-Red-Blue (swaps red and green)

## C# Implementation

### Class Definition
```csharp
public class ChannelShiftEffectsNode : BaseEffectNode
{
    public int ChannelMode { get; set; } = 0;
    public bool BeatReactive { get; set; } = true;
    public int BeatRandomMode { get; set; } = 1;
    public float ChannelIntensity { get; set; } = 1.0f;
    public bool EnableSmoothTransition { get; set; } = false;
    public float TransitionSpeed { get; set; } = 1.0f;
    public int CustomChannelOrder { get; set; } = 0;
    public bool EnableChannelMasking { get; set; } = false;
    public int RedChannelMask { get; set; } = 0xFF;
    public int GreenChannelMask { get; set; } = 0xFF;
    public int BlueChannelMask { get; set; } = 0xFF;
    public float ChannelBlend { get; set; } = 1.0f;
    public bool EnableChannelAnimation { get; set; } = false;
    public float AnimationSpeed { get; set; } = 1.0f;
    public int AnimationMode { get; set; } = 0;
    public bool EnableChannelInversion { get; set; } = false;
    public float InversionStrength { get; set; } = 0.5f;
    public bool EnableChannelClamping { get; set; } = true;
    public int ClampMode { get; set; } = 0;
    public bool EnableChannelQuantization { get; set; } = false;
    public int QuantizationLevels { get; set; } = 256;
}
```

### Key Features
- **Six Channel Modes**: Complete RGB permutation support
- **Beat Reactivity**: Automatic mode switching synchronized with audio
- **Smooth Transitions**: Optional smooth channel mode transitions
- **Channel Masking**: Selective channel manipulation
- **Animation Support**: Animated channel mode changes
- **Custom Channel Orders**: User-defined channel arrangements
- **Performance Optimization**: Optimized pixel processing algorithms

### Usage Examples

#### Basic Channel Swapping
```csharp
var channelShiftNode = new ChannelShiftEffectsNode
{
    ChannelMode = 1, // RBG mode (swap blue and green)
    BeatReactive = false,
    ChannelIntensity = 1.0f,
    EnableSmoothTransition = false
};
```

#### Beat-Reactive Channel Rotation
```csharp
var beatChannelShift = new ChannelShiftEffectsNode
{
    ChannelMode = 0, // Start with RGB
    BeatReactive = true,
    BeatRandomMode = 1, // Random mode on beat
    EnableSmoothTransition = true,
    TransitionSpeed = 2.0f,
    ChannelIntensity = 1.0f
};
```

#### Custom Channel Manipulation
```csharp
var customChannelShift = new ChannelShiftEffectsNode
{
    ChannelMode = 5, // GRB mode
    BeatReactive = false,
    EnableChannelMasking = true,
    RedChannelMask = 0x80, // Reduce red intensity
    GreenChannelMask = 0xFF, // Full green
    BlueChannelMask = 0xC0, // Reduce blue intensity
    ChannelBlend = 0.8f,
    EnableChannelAnimation = true,
    AnimationSpeed = 1.5f,
    AnimationMode = 1 // Rotating animation
};
```

## Technical Implementation

### Core Processing Algorithm
```csharp
protected override ImageBuffer ProcessEffect(ImageBuffer input, AudioFeatures audio)
{
    var output = new ImageBuffer(input.Width, input.Height);
    var currentMode = GetCurrentChannelMode(audio);
    
    // Process each pixel
    for (int i = 0; i < input.Pixels.Length; i++)
    {
        var originalColor = input.Pixels[i];
        var shiftedColor = ApplyChannelShift(originalColor, currentMode);
        
        // Apply channel masking if enabled
        if (EnableChannelMasking)
        {
            shiftedColor = ApplyChannelMasking(shiftedColor);
        }
        
        // Apply channel blend
        if (ChannelBlend < 1.0f)
        {
            shiftedColor = BlendChannels(originalColor, shiftedColor, ChannelBlend);
        }
        
        output.Pixels[i] = shiftedColor;
    }
    
    return output;
}
```

### Channel Shift Implementation
```csharp
private int ApplyChannelShift(int color, int mode)
{
    var r = (color >> 16) & 0xFF;
    var g = (color >> 8) & 0xFF;
    var b = color & 0xFF;
    
    switch (mode)
    {
        case 0: // RGB - No change
            return color;
        case 1: // RBG - Swap blue and green
            return (r << 16) | (b << 8) | g;
        case 2: // BRG - Rotate left
            return (b << 16) | (r << 8) | g;
        case 3: // BGR - Swap red and blue
            return (b << 16) | (g << 8) | r;
        case 4: // GBR - Rotate right
            return (g << 16) | (b << 8) | r;
        case 5: // GRB - Swap red and green
            return (g << 16) | (r << 8) | b;
        default:
            return color;
    }
}
```

### Beat-Reactive Mode Switching
```csharp
private int GetCurrentChannelMode(AudioFeatures audio)
{
    if (!BeatReactive || audio == null)
        return ChannelMode;
    
    if (audio.IsBeat)
    {
        switch (BeatRandomMode)
        {
            case 0: // Sequential
                return (ChannelMode + 1) % 6;
            case 1: // Random
                return Random.Next(0, 6);
            case 2: // Beat-based pattern
                return (ChannelMode + (int)(audio.BeatIntensity * 3)) % 6;
            case 3: // Audio frequency based
                var freqIndex = (int)(audio.Spectrum[0] * 6);
                return Math.Max(0, Math.Min(5, freqIndex));
            default:
                return ChannelMode;
        }
    }
    
    return ChannelMode;
}
```

### Smooth Channel Transitions
```csharp
private int ApplySmoothTransition(int color1, int color2, float progress)
{
    if (!EnableSmoothTransition || progress <= 0.0f)
        return color1;
    
    if (progress >= 1.0f)
        return color2;
    
    // Extract channels
    var r1 = (color1 >> 16) & 0xFF;
    var g1 = (color1 >> 8) & 0xFF;
    var b1 = color1 & 0xFF;
    
    var r2 = (color2 >> 16) & 0xFF;
    var g2 = (color2 >> 8) & 0xFF;
    var b2 = color2 & 0xFF;
    
    // Interpolate channels
    var r = (int)(r1 + (r2 - r1) * progress);
    var g = (int)(g1 + (g2 - g1) * progress);
    var b = (int)(b1 + (b2 - b1) * progress);
    
    // Clamp values
    r = Math.Max(0, Math.Min(255, r));
    g = Math.Max(0, Math.Min(255, g));
    b = Math.Max(0, Math.Min(255, b));
    
    return (r << 16) | (g << 8) | b;
}
```

### Channel Masking
```csharp
private int ApplyChannelMasking(int color)
{
    if (!EnableChannelMasking)
        return color;
    
    var r = (color >> 16) & 0xFF;
    var g = (color >> 8) & 0xFF;
    var b = color & 0xFF;
    
    // Apply masks
    r = (r & RedChannelMask);
    g = (g & GreenChannelMask);
    b = (b & BlueChannelMask);
    
    // Handle channel inversion
    if (EnableChannelInversion)
    {
        r = (int)(r + (255 - r) * InversionStrength);
        g = (int)(g + (255 - g) * InversionStrength);
        b = (int)(b + (255 - b) * InversionStrength);
    }
    
    // Clamp values
    if (EnableChannelClamping)
    {
        r = Math.Max(0, Math.Min(255, r));
        g = Math.Max(0, Math.Min(255, g));
        b = Math.Max(0, Math.Min(255, b));
    }
    
    return (r << 16) | (g << 8) | b;
}
```

### Channel Animation
```csharp
private void UpdateChannelAnimation(float deltaTime)
{
    if (!EnableChannelAnimation)
        return;
    
    var animationProgress = (GetCurrentTime() * AnimationSpeed) % (Math.PI * 2);
    
    switch (AnimationMode)
    {
        case 0: // Rotating
            ChannelMode = (int)((animationProgress / (Math.PI * 2)) * 6) % 6;
            break;
        case 1: // Pulsing
            var pulse = (Math.Sin(animationProgress) + 1) * 0.5f;
            ChannelIntensity = 0.5f + pulse * 0.5f;
            break;
        case 2: // Wave
            var wave = Math.Sin(animationProgress * 2);
            ChannelMode = (int)((wave + 1) * 3) % 6;
            break;
        case 3: // Random walk
            if (Random.NextDouble() < 0.01f) // 1% chance per frame
            {
                ChannelMode = Random.Next(0, 6);
            }
            break;
    }
}
```

## Performance Considerations

### Optimization Strategies
- **SIMD Operations**: Use vectorized operations for channel manipulation
- **Memory Access**: Optimize pixel buffer access patterns
- **Branch Prediction**: Minimize conditional branches in hot paths
- **Lookup Tables**: Pre-compute channel permutation tables
- **Parallel Processing**: Process image regions in parallel

### Memory Management
- **Buffer Reuse**: Reuse ImageBuffer instances when possible
- **Temporary Buffers**: Minimize temporary buffer allocations
- **Garbage Collection**: Avoid allocations in hot paths

## Integration with EffectGraph

### Input Ports
- **Image Input**: Source image buffer for channel manipulation
- **Audio Input**: Audio features for beat-reactive behavior
- **Control Input**: External control signals for dynamic parameters

### Output Ports
- **Shifted Image**: The processed image with applied channel shifts
- **Channel Data**: Channel permutation metadata for debugging
- **Performance Metrics**: Processing time and memory usage

### Node Configuration
```csharp
public override void ConfigureNode()
{
    AddInputPort("Image", typeof(ImageBuffer));
    AddInputPort("Audio", typeof(AudioFeatures));
    AddInputPort("Control", typeof(float));
    
    AddOutputPort("ShiftedImage", typeof(ImageBuffer));
    AddOutputPort("ChannelData", typeof(ChannelShiftData));
    AddOutputPort("Performance", typeof(PerformanceMetrics));
    
    SetMetadata(new EffectMetadata
    {
        Name = "Channel Shift",
        Category = "Color Manipulation",
        Description = "Manipulates RGB color channels with beat-reactive mode switching",
        Version = "1.0.0",
        Author = "Phoenix Visualizer Team"
    });
}
```

## Advanced Features

### Custom Channel Orders
```csharp
private int ApplyCustomChannelOrder(int color)
{
    if (CustomChannelOrder == 0)
        return ApplyChannelShift(color, ChannelMode);
    
    var r = (color >> 16) & 0xFF;
    var g = (color >> 8) & 0xFF;
    var b = color & 0xFF;
    
    // Custom channel arrangement based on CustomChannelOrder
    var order = CustomChannelOrder;
    var channels = new int[3];
    
    channels[0] = (order >> 16) & 0xFF; // First channel
    channels[1] = (order >> 8) & 0xFF;  // Second channel
    channels[2] = order & 0xFF;          // Third channel
    
    var newR = GetChannelValue(color, channels[0]);
    var newG = GetChannelValue(color, channels[1]);
    var newB = GetChannelValue(color, channels[2]);
    
    return (newR << 16) | (newG << 8) | newB;
}

private int GetChannelValue(int color, int channelIndex)
{
    switch (channelIndex)
    {
        case 0: return (color >> 16) & 0xFF; // Red
        case 1: return (color >> 8) & 0xFF;  // Green
        case 2: return color & 0xFF;          // Blue
        case 3: return ((color >> 16) & 0xFF + (color >> 8) & 0xFF) / 2; // Red+Green
        case 4: return ((color >> 8) & 0xFF + color & 0xFF) / 2;         // Green+Blue
        case 5: return ((color >> 16) & 0xFF + color & 0xFF) / 2;        // Red+Blue
        case 6: return ((color >> 16) & 0xFF + (color >> 8) & 0xFF + color & 0xFF) / 3; // All
        default: return 0;
    }
}
```

### Channel Quantization
```csharp
private int ApplyChannelQuantization(int color)
{
    if (!EnableChannelQuantization)
        return color;
    
    var r = (color >> 16) & 0xFF;
    var g = (color >> 8) & 0xFF;
    var b = color & 0xFF;
    
    var levels = Math.Max(2, Math.Min(256, QuantizationLevels));
    var step = 256.0f / levels;
    
    r = (int)(Math.Round(r / step) * step);
    g = (int)(Math.Round(g / step) * step);
    b = (int)(Math.Round(b / step) * step);
    
    return (r << 16) | (g << 8) | b;
}
```

## Testing and Validation

### Unit Tests
```csharp
[Test]
public void TestChannelShifting()
{
    var node = new ChannelShiftEffectsNode
    {
        ChannelMode = 1, // RBG mode
        BeatReactive = false
    };
    
    var input = CreateTestImage(100, 100);
    var audio = CreateTestAudio();
    
    var output = node.ProcessEffect(input, audio);
    
    Assert.IsNotNull(output);
    Assert.AreEqual(100, output.Width);
    Assert.AreEqual(100, output.Height);
    
    // Test that blue and green channels are swapped
    var inputPixel = input.GetPixel(50, 50);
    var outputPixel = output.GetPixel(50, 50);
    
    var inputBlue = inputPixel & 0xFF;
    var inputGreen = (inputPixel >> 8) & 0xFF;
    var outputBlue = outputPixel & 0xFF;
    var outputGreen = (outputPixel >> 8) & 0xFF;
    
    Assert.AreEqual(inputBlue, outputGreen);
    Assert.AreEqual(inputGreen, outputBlue);
}
```

### Performance Tests
```csharp
[Test]
public void TestPerformance()
{
    var node = new ChannelShiftEffectsNode();
    var input = CreateTestImage(1920, 1080);
    var audio = CreateTestAudio();
    
    var stopwatch = Stopwatch.StartNew();
    var output = node.ProcessEffect(input, audio);
    stopwatch.Stop();
    
    var processingTime = stopwatch.ElapsedMilliseconds;
    Assert.Less(processingTime, 50); // Should process in under 50ms
}
```

## Future Enhancements

### Planned Features
- **HSV/HSL Support**: Channel manipulation in different color spaces
- **Alpha Channel**: Support for alpha channel manipulation
- **Channel Effects**: Advanced channel effects like noise, blur, and distortion
- **Real-time Control**: MIDI and OSC control integration
- **Channel Presets**: Predefined channel manipulation presets

### Research Areas
- **Color Theory**: Advanced color manipulation algorithms
- **Perceptual Effects**: Human visual system considerations
- **Machine Learning**: AI-generated channel manipulation patterns
- **Real-time Analysis**: Dynamic channel analysis and adjustment

## Conclusion
The Channel Shift effect provides powerful RGB channel manipulation capabilities with extensive customization options. Its beat-reactive nature and smooth transition support make it suitable for creating dynamic, engaging visualizations. The implementation balances performance with flexibility, offering both real-time processing and high-quality output suitable for professional visualization applications.

The effect's ability to create various color distortions through simple channel permutations makes it an essential tool for AVS preset creation, allowing artists to explore unique color combinations and create visually striking effects that respond to music.
