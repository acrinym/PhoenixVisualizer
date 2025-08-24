# AVS Mosaic Effect

## Overview
The Mosaic effect is an AVS visualization effect that creates mosaic/pixelated patterns by downsampling images. It reduces image quality by sampling pixels at larger intervals, creating blocky, pixelated effects that can be synchronized with music beats. The effect supports multiple blending modes and beat-reactive quality changes.

## C++ Source Analysis
Based on the AVS source code in `r_mosaic.cpp`, the Mosaic effect inherits from `C_RBASE` and implements sophisticated image downsampling:

### Key Properties
- **Quality Control**: Adjustable quality from 1-100 (1=most pixelated, 100=original)
- **Beat Reactivity**: Automatic quality changes on audio beats
- **Multiple Blending Modes**: Replace, additive, and 50/50 blending options
- **Duration Control**: Configurable beat effect duration
- **High Performance**: Optimized pixel sampling algorithms
- **Configurable Parameters**: Quality levels, blending modes, and timing

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
    int quality;           // Normal quality level (1-100)
    int quality2;          // Beat quality level (1-100)
    int blend;             // Additive blending flag
    int blendavg;          // 50/50 blending flag
    int onbeat;            // Beat-reactive mode flag
    int durFrames;         // Beat effect duration in frames
    int nF;                // Current frame counter
    int thisQuality;       // Current active quality level
};
```

### Mosaic Algorithm
The effect creates mosaic patterns by:
1. **Quality Calculation**: Determines current quality level (normal or beat-reactive)
2. **Pixel Sampling**: Samples source image at calculated intervals
3. **Blending Application**: Applies selected blending mode to output
4. **Beat Synchronization**: Smoothly transitions between quality levels on beats
5. **Duration Management**: Controls how long beat effects last

## C# Implementation

### Class Definition
```csharp
public class MosaicEffectsNode : BaseEffectNode
{
    public bool Enabled { get; set; } = true;
    public int Quality { get; set; } = 50; // 1 to 100
    public int BeatQuality { get; set; } = 25; // 1 to 100
    public int BlendMode { get; set; } = 0; // 0=Replace, 1=Additive, 2=50/50
    public bool BeatReactive { get; set; } = false;
    public int BeatDuration { get; set; } = 15; // 1 to 100 frames
    public bool EnableQualityAnimation { get; set; } = false;
    public float QualityAnimationSpeed { get; set; } = 1.0f;
    public int QualityAnimationMode { get; set; } = 0;
    public bool EnableMosaicMasking { get; set; } = false;
    public ImageBuffer MosaicMask { get; set; } = null;
    public float MaskInfluence { get; set; } = 1.0f;
    public bool EnableMosaicBlending { get; set; } = false;
    public float MosaicBlendStrength { get; set; } = 0.5f;
    public int MosaicAlgorithm { get; set; } = 0; // 0=Standard, 1=Enhanced, 2=Realistic
    public float MosaicCurve { get; set; } = 1.0f; // Power curve for mosaic effects
    public bool EnableMosaicClamping { get; set; } = true;
    public int ClampMode { get; set; } = 0; // 0=Standard, 1=Soft, 2=Hard
    public bool EnableMosaicInversion { get; set; } = false;
    public float InversionThreshold { get; set; } = 0.5f;
    
    // Internal state for mosaic processing
    private int CurrentQuality { get; set; } = 50;
    private int FrameCounter { get; set; } = 0;
    private Random Random { get; set; } = new Random();
}
```

### Key Features
- **Quality Control**: Adjustable pixelation levels from 1-100
- **Beat Reactivity**: Automatic quality changes synchronized with music
- **Multiple Blending Modes**: Replace, additive, and 50/50 blending
- **Duration Control**: Configurable beat effect duration
- **Quality Animation**: Animated quality transitions and effects
- **Mosaic Masking**: Use image masks to control mosaic areas
- **Mosaic Blending**: Blend mosaic effects with background
- **Multiple Algorithms**: Standard, enhanced, and realistic mosaic methods

### Usage Examples

#### Basic Mosaic
```csharp
var mosaicNode = new MosaicEffectsNode
{
    Enabled = true,
    Quality = 30,
    BlendMode = 0, // Replace
    BeatReactive = false
};
```

#### Beat-Reactive Mosaic
```csharp
var beatMosaicNode = new MosaicEffectsNode
{
    Enabled = true,
    Quality = 50,
    BeatQuality = 15,
    BlendMode = 1, // Additive
    BeatReactive = true,
    BeatDuration = 20
};
```

#### Animated Mosaic Effect
```csharp
var animatedMosaicNode = new MosaicEffectsNode
{
    Enabled = true,
    Quality = 40,
    BeatQuality = 10,
    BlendMode = 2, // 50/50
    BeatReactive = true,
    BeatDuration = 15,
    EnableQualityAnimation = true,
    QualityAnimationSpeed = 2.0f,
    MosaicAlgorithm = 2 // Realistic
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
    
    // Update quality based on beat reactivity
    UpdateQuality(audio);
    
    // Apply mosaic effect if quality is less than 100
    if (CurrentQuality < 100)
    {
        ApplyMosaicEffect(input, output);
    }
    else
    {
        // No mosaic, copy input to output
        output.CopyFrom(input);
    }
    
    return output;
}
```

### Quality Update
```csharp
private void UpdateQuality(AudioFeatures audio)
{
    if (BeatReactive && audio != null && audio.IsBeat)
    {
        // Beat detected, switch to beat quality
        CurrentQuality = BeatQuality;
        FrameCounter = BeatDuration;
    }
    else if (FrameCounter > 0)
    {
        // Beat effect active, gradually return to normal quality
        FrameCounter--;
        if (FrameCounter > 0)
        {
            var qualityDiff = Math.Abs(Quality - BeatQuality);
            var step = qualityDiff / BeatDuration;
            CurrentQuality += step * (BeatQuality > Quality ? -1 : 1);
        }
        else
        {
            CurrentQuality = Quality;
        }
    }
    else
    {
        // Normal operation
        CurrentQuality = Quality;
    }
}
```

### Mosaic Effect Application
```csharp
private void ApplyMosaicEffect(ImageBuffer input, ImageBuffer output)
{
    var width = input.Width;
    var height = input.Height;
    
    // Calculate sampling intervals (16-bit fixed point)
    var sampleXInc = (width * 65536) / CurrentQuality;
    var sampleYInc = (height * 65536) / CurrentQuality;
    
    var yPos = (sampleYInc >> 17);
    var dyPos = 0;
    
    for (int y = 0; y < height; y++)
    {
        var x = width;
        var frameRead = input.GetRow(yPos);
        var dPos = 0;
        var xPos = (sampleXInc >> 17);
        var sourcePixel = frameRead[xPos];
        
        for (int x = 0; x < width; x++)
        {
            // Apply selected blending mode
            var outputPixel = ApplyBlendingMode(output.GetPixel(x, y), sourcePixel);
            output.SetPixel(x, y, outputPixel);
            
            // Update sampling position
            dPos += 1 << 16;
            if (dPos >= sampleXInc)
            {
                xPos += dPos >> 16;
                if (xPos >= width) break;
                sourcePixel = frameRead[xPos];
                dPos -= sampleXInc;
            }
        }
        
        // Update vertical sampling position
        dyPos += 1 << 16;
        if (dyPos >= sampleYInc)
        {
            yPos += (dyPos >> 16);
            dyPos -= sampleYInc;
            if (yPos >= height) break;
        }
    }
}
```

### Blending Mode Application
```csharp
private int ApplyBlendingMode(int currentPixel, int sourcePixel)
{
    switch (BlendMode)
    {
        case 0: // Replace
            return sourcePixel;
            
        case 1: // Additive
            return BlendPixelsAdditive(currentPixel, sourcePixel);
            
        case 2: // 50/50
            return BlendPixels50_50(currentPixel, sourcePixel);
            
        default:
            return sourcePixel;
    }
}

private int BlendPixelsAdditive(int pixel1, int pixel2)
{
    var r1 = pixel1 & 0xFF;
    var g1 = (pixel1 >> 8) & 0xFF;
    var b1 = (pixel1 >> 16) & 0xFF;
    
    var r2 = pixel2 & 0xFF;
    var g2 = (pixel2 >> 8) & 0xFF;
    var b2 = (pixel2 >> 16) & 0xFF;
    
    var r = Math.Min(255, r1 + r2);
    var g = Math.Min(255, g1 + g2);
    var b = Math.Min(255, b1 + b2);
    
    return (b << 16) | (g << 8) | r;
}

private int BlendPixels50_50(int pixel1, int pixel2)
{
    var r1 = pixel1 & 0xFF;
    var g1 = (pixel1 >> 8) & 0xFF;
    var b1 = (pixel1 >> 16) & 0xFF;
    
    var r2 = pixel2 & 0xFF;
    var g2 = (pixel2 >> 8) & 0xFF;
    var b2 = (pixel2 >> 16) & 0xFF;
    
    var r = (r1 + r2) / 2;
    var g = (g1 + g2) / 2;
    var b = (b1 + b2) / 2;
    
    return (b << 16) | (g << 8) | r;
}
```

### Enhanced Mosaic Algorithm
```csharp
private void ApplyEnhancedMosaic(ImageBuffer input, ImageBuffer output)
{
    if (MosaicAlgorithm == 1) // Enhanced
    {
        // Multi-pass mosaic with different quality levels
        var passCount = 3;
        var passQualities = new int[] { CurrentQuality, CurrentQuality * 2, CurrentQuality * 3 };
        
        for (int pass = 0; pass < passCount; pass++)
        {
            var passQuality = Math.Min(100, passQualities[pass]);
            ApplyMosaicPass(input, output, passQuality, pass);
        }
    }
    else if (MosaicAlgorithm == 2) // Realistic
    {
        // Realistic mosaic with edge preservation
        ApplyRealisticMosaic(input, output);
    }
    else
    {
        // Standard mosaic
        ApplyMosaicEffect(input, output);
    }
}

private void ApplyMosaicPass(ImageBuffer input, ImageBuffer output, int quality, int pass)
{
    // Apply mosaic with specific quality level
    var tempOutput = new ImageBuffer(output.Width, output.Height);
    ApplyMosaicEffect(input, tempOutput, quality);
    
    // Blend with previous passes
    var blendFactor = 1.0f / (pass + 1);
    BlendImages(output, tempOutput, blendFactor);
}
```

## Performance Considerations

### Optimization Strategies
- **Fixed-Point Math**: Use 16-bit fixed-point arithmetic for sampling
- **Row Access**: Optimize row-based pixel access
- **Blending Optimization**: Efficient pixel blending algorithms
- **Memory Access**: Minimize memory allocations and copies
- **Algorithm Selection**: Choose appropriate mosaic method for performance vs. quality

### Memory Management
- **Buffer Reuse**: Reuse image buffers when possible
- **Row Caching**: Cache frequently accessed image rows
- **Garbage Collection**: Avoid allocations in hot paths

## Integration with EffectGraph

### Input Ports
- **Image Input**: Source image buffer for mosaic processing
- **Audio Input**: Audio features for beat-reactive behavior
- **Mask Input**: Optional mosaic mask image
- **Control Input**: External control signals for dynamic parameters

### Output Ports
- **Mosaic Image**: The processed image with applied mosaic effects
- **Mosaic Data**: Mosaic parameters and metadata
- **Performance Metrics**: Processing time and memory usage

### Node Configuration
```csharp
public override void ConfigureNode()
{
    AddInputPort("Image", typeof(ImageBuffer));
    AddInputPort("Audio", typeof(AudioFeatures));
    AddInputPort("Mask", typeof(ImageBuffer));
    AddInputPort("Control", typeof(float));
    
    AddOutputPort("MosaicImage", typeof(ImageBuffer));
    AddOutputPort("MosaicData", typeof(MosaicData));
    AddOutputPort("Performance", typeof(PerformanceMetrics));
    
    SetMetadata(new EffectMetadata
    {
        Name = "Mosaic",
        Category = "Transform Effects",
        Description = "Creates mosaic/pixelated effects with beat synchronization",
        Version = "1.0.0",
        Author = "Phoenix Visualizer Team"
    });
}
```

## Advanced Features

### Quality Animation
```csharp
private void UpdateQualityAnimation()
{
    if (!EnableQualityAnimation)
        return;
    
    var animationProgress = (GetCurrentTime() * QualityAnimationSpeed) % (Math.PI * 2);
    
    switch (QualityAnimationMode)
    {
        case 0: // Pulsing quality
            var pulse = (Math.Sin(animationProgress) + 1.0f) * 0.5f;
            CurrentQuality = (int)(20 + pulse * 60); // 20-80 quality range
            break;
            
        case 1: // Oscillating quality
            var oscillation = Math.Sin(animationProgress * 2);
            CurrentQuality = (int)(30 + oscillation * 40); // 30-70 quality range
            break;
            
        case 2: // Random quality
            if (Random.NextDouble() < 0.01f) // 1% chance per frame
            {
                CurrentQuality = Random.Next(10, 91); // 10-90 quality range
            }
            break;
            
        case 3: // Wave pattern quality
            var wave = Math.Sin(animationProgress * 3);
            CurrentQuality = (int)(25 + wave * 50); // 25-75 quality range
            break;
    }
}
```

## Testing and Validation

### Unit Tests
```csharp
[Test]
public void TestBasicMosaic()
{
    var node = new MosaicEffectsNode
    {
        Enabled = true,
        Quality = 30,
        BlendMode = 0,
        BeatReactive = false
    };
    
    var input = CreateTestImage(100, 100);
    var audio = CreateTestAudio();
    
    var output = node.ProcessEffect(input, audio);
    
    Assert.IsNotNull(output);
    Assert.AreEqual(100, output.Width);
    Assert.AreEqual(100, output.Height);
    
    // Test that mosaic effect is applied
    var hasMosaicEffect = false;
    for (int i = 0; i < output.Pixels.Length; i++)
    {
        if (output.Pixels[i] != input.Pixels[i])
        {
            hasMosaicEffect = true;
            break;
        }
    }
    
    Assert.IsTrue(hasMosaicEffect, "Mosaic effect should modify pixels");
}
```

### Performance Tests
```csharp
[Test]
public void TestPerformance()
{
    var node = new MosaicEffectsNode();
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
- **Advanced Mosaic Patterns**: More sophisticated pixelation algorithms
- **Real-time Control**: MIDI and OSC control integration
- **Mosaic Presets**: Predefined mosaic patterns and styles
- **3D Mosaic**: Depth-based mosaic effects
- **GPU Acceleration**: OpenGL/OpenCL implementation for real-time processing

### Research Areas
- **Image Processing**: Advanced downsampling algorithms
- **Machine Learning**: AI-generated mosaic patterns
- **Real-time Analysis**: Dynamic quality adjustment
- **Visual Effects**: Advanced mosaic rendering techniques

## Conclusion
The Mosaic effect provides powerful image transformation capabilities with extensive customization options. Its beat-reactive nature and sophisticated downsampling algorithms make it suitable for creating dynamic, engaging visualizations. The implementation balances performance with flexibility, offering both real-time processing and high-quality output suitable for professional visualization applications.

The effect's ability to create mosaic patterns, pixelated effects, and beat-synchronized quality changes makes it an essential tool for AVS preset creation, allowing artists to create dynamic image transformations that respond to music in real-time.
