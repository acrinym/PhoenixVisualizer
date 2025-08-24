# AVS Grain Effect

## Overview
The Grain effect is an AVS visualization effect that adds film grain or noise to an image. It creates a textured, noisy appearance that can simulate film grain, add artistic texture, or create atmospheric effects. The effect supports both static and dynamic grain patterns with various blending modes for seamless integration with the original image.

## C++ Source Analysis
Based on the AVS source code in `r_grain.cpp`, the Grain effect inherits from `C_RBASE` and implements sophisticated noise generation and blending:

### Key Properties
- **Enabled State**: Simple on/off toggle for the effect
- **Blend Modes**: Three blending modes (Additive, 50/50, Replace)
- **Static Grain**: Option for consistent grain pattern across frames
- **Grain Intensity**: Configurable grain strength (0-100%)
- **Depth Buffer**: Internal depth buffer for grain pattern storage
- **Random Table**: Optimized random number generation using lookup tables

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
        void reinit(int w, int h);
        unsigned char __inline fastrandbyte(void);

    int enabled;           // Effect enabled/disabled flag
    int blend;             // Additive blending mode
    int blendavg;          // 50/50 blending mode
    int smax;              // Grain intensity (0-100)
    int staticgrain;       // Static grain pattern flag
    unsigned char *depthBuffer; // Depth buffer for grain storage
    unsigned char randtab[491]; // Random number lookup table
    int randtab_pos;       // Current position in random table
    int oldx, oldy;        // Previous dimensions for reinitialization
};
```

### Blending Modes
1. **Additive (blend=1)**: Adds grain to original image using BLEND macro
2. **50/50 (blendavg=1)**: Blends grain with original using BLEND_AVG macro
3. **Replace (both=0)**: Replaces pixels with grain when threshold is met

## C# Implementation

### Class Definition
```csharp
public class GrainEffectsNode : BaseEffectNode
{
    public bool Enabled { get; set; } = true;
    public int BlendMode { get; set; } = 0; // 0=Replace, 1=Additive, 2=50/50
    public float GrainIntensity { get; set; } = 50.0f; // 0.0 to 100.0
    public bool StaticGrain { get; set; } = false;
    public bool BeatReactive { get; set; } = false;
    public float BeatIntensityMultiplier { get; set; } = 1.5f;
    public int GrainSeed { get; set; } = 0;
    public bool EnableGrainAnimation { get; set; } = false;
    public float GrainAnimationSpeed { get; set; } = 1.0f;
    public int GrainAnimationMode { get; set; } = 0;
    public bool EnableGrainMasking { get; set; } = false;
    public ImageBuffer GrainMask { get; set; } = null;
    public float MaskInfluence { get; set; } = 1.0f;
    public bool EnableGrainBlending { get; set; } = false;
    public float GrainBlendStrength { get; set; } = 0.5f;
    public int GrainPatternType { get; set; } = 0;
    public float GrainScale { get; set; } = 1.0f;
    public bool EnableGrainColorization { get; set; } = false;
    public int GrainColor { get; set; } = 0xFFFFFF;
    public float ColorIntensity { get; set; } = 0.3f;
    public bool EnableGrainDirectional { get; set; } = false;
    public float GrainDirectionX { get; set; } = 0.0f;
    public float GrainDirectionY { get; set; } = 0.0f;
    public bool EnableGrainTemporal { get; set; } = false;
    public float TemporalSpeed { get; set; } = 1.0f;
    public int TemporalMode { get; set; } = 0;
}
```

### Key Features
- **Three Blending Modes**: Replace, Additive, and 50/50 blending
- **Static and Dynamic Grain**: Consistent or changing grain patterns
- **Beat Reactivity**: Dynamic grain intensity synchronized with audio
- **Grain Animation**: Animated grain patterns and movement
- **Grain Masking**: Use image masks to control grain application areas
- **Grain Colorization**: Apply colored grain effects
- **Directional Grain**: Grain patterns that follow specific directions
- **Temporal Grain**: Time-based grain evolution
- **Performance Optimization**: Optimized noise generation algorithms

### Usage Examples

#### Basic Film Grain
```csharp
var grainNode = new GrainEffectsNode
{
    Enabled = true,
    BlendMode = 1, // Additive blending
    GrainIntensity = 30.0f,
    StaticGrain = false,
    BeatReactive = false
};
```

#### Beat-Reactive Dynamic Grain
```csharp
var beatGrainNode = new GrainEffectsNode
{
    Enabled = true,
    BlendMode = 2, // 50/50 blending
    GrainIntensity = 40.0f,
    StaticGrain = false,
    BeatReactive = true,
    BeatIntensityMultiplier = 2.0f,
    EnableGrainAnimation = true,
    GrainAnimationSpeed = 1.5f
};
```

#### Artistic Colored Grain
```csharp
var artisticGrainNode = new GrainEffectsNode
{
    Enabled = true,
    BlendMode = 1, // Additive blending
    GrainIntensity = 60.0f,
    StaticGrain = true,
    EnableGrainColorization = true,
    GrainColor = 0xFF0080, // Magenta
    ColorIntensity = 0.5f,
    EnableGrainDirectional = true,
    GrainDirectionX = 0.5f,
    GrainDirectionY = -0.3f
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
    var currentIntensity = GetCurrentGrainIntensity(audio);
    
    // Initialize grain buffer if needed
    if (GrainBuffer == null || GrainBuffer.Width != input.Width || GrainBuffer.Height != input.Height)
    {
        InitializeGrainBuffer(input.Width, input.Height);
    }
    
    // Update grain pattern
    UpdateGrainPattern(audio);
    
    // Process each pixel
    for (int i = 0; i < input.Pixels.Length; i++)
    {
        var originalColor = input.Pixels[i];
        var grainColor = GetGrainColor(i, currentIntensity);
        var processedColor = ApplyGrainBlending(originalColor, grainColor);
        
        // Apply grain masking if enabled
        if (EnableGrainMasking && GrainMask != null)
        {
            processedColor = ApplyGrainMasking(originalColor, processedColor, i);
        }
        
        output.Pixels[i] = processedColor;
    }
    
    return output;
}
```

### Grain Buffer Initialization
```csharp
private void InitializeGrainBuffer(int width, int height)
{
    GrainBuffer = new ImageBuffer(width, height);
    var random = new Random(GrainSeed);
    
    for (int i = 0; i < GrainBuffer.Pixels.Length; i++)
    {
        var intensity = random.Next(0, 256);
        var threshold = random.Next(0, 101);
        GrainBuffer.Pixels[i] = (threshold << 8) | intensity;
    }
}
```

### Grain Pattern Update
```csharp
private void UpdateGrainPattern(AudioFeatures audio)
{
    if (StaticGrain)
        return;
    
    var random = new Random(GrainSeed + (int)(GetCurrentTime() * 1000));
    
    for (int i = 0; i < GrainBuffer.Pixels.Length; i++)
    {
        if (random.Next(0, 100) < 10) // 10% chance to update each pixel
        {
            var intensity = random.Next(0, 256);
            var threshold = random.Next(0, 101);
            GrainBuffer.Pixels[i] = (threshold << 8) | intensity;
        }
    }
}
```

### Grain Color Generation
```csharp
private int GetGrainColor(int pixelIndex, float intensity)
{
    var grainData = GrainBuffer.Pixels[pixelIndex];
    var grainIntensity = grainData & 0xFF;
    var grainThreshold = (grainData >> 8) & 0xFF;
    
    var intensityThreshold = (intensity * 255) / 100.0f;
    
    if (grainThreshold > intensityThreshold)
        return 0; // No grain for this pixel
    
    if (EnableGrainColorization)
    {
        return GenerateColoredGrain(grainIntensity);
    }
    
    return (grainIntensity << 16) | (grainIntensity << 8) | grainIntensity;
}

private int GenerateColoredGrain(int intensity)
{
    var r = (GrainColor >> 16) & 0xFF;
    var g = (GrainColor >> 8) & 0xFF;
    var b = GrainColor & 0xFF;
    
    var grainR = (int)(r * intensity * ColorIntensity / 255.0f);
    var grainG = (int)(g * intensity * ColorIntensity / 255.0f);
    var grainB = (int)(b * intensity * ColorIntensity / 255.0f);
    
    return (grainB << 16) | (grainG << 8) | grainR;
}
```

### Grain Blending Implementation
```csharp
private int ApplyGrainBlending(int originalColor, int grainColor)
{
    switch (BlendMode)
    {
        case 0: // Replace
            return grainColor;
            
        case 1: // Additive
            return BlendAdditive(originalColor, grainColor);
            
        case 2: // 50/50
            return BlendFiftyFifty(originalColor, grainColor);
            
        default:
            return originalColor;
    }
}

private int BlendAdditive(int color1, int color2)
{
    var r1 = color1 & 0xFF;
    var g1 = (color1 >> 8) & 0xFF;
    var b1 = (color1 >> 16) & 0xFF;
    
    var r2 = color2 & 0xFF;
    var g2 = (color2 >> 8) & 0xFF;
    var b2 = (color2 >> 16) & 0xFF;
    
    var r = Math.Min(255, r1 + r2);
    var g = Math.Min(255, g1 + g2);
    var b = Math.Min(255, b1 + b2);
    
    return (b << 16) | (g << 8) | r;
}

private int BlendFiftyFifty(int color1, int color2)
{
    var r1 = color1 & 0xFF;
    var g1 = (color1 >> 8) & 0xFF;
    var b1 = (color1 >> 16) & 0xFF;
    
    var r2 = color2 & 0xFF;
    var g2 = (color2 >> 8) & 0xFF;
    var b2 = (color2 >> 16) & 0xFF;
    
    var r = (r1 + r2) / 2;
    var g = (g1 + g2) / 2;
    var b = (b1 + b2) / 2;
    
    return (b << 16) | (g << 8) | r;
}
```

### Beat-Reactive Intensity
```csharp
private float GetCurrentGrainIntensity(AudioFeatures audio)
{
    if (!BeatReactive || audio == null)
        return GrainIntensity;
    
    var beatMultiplier = 1.0f;
    
    if (audio.IsBeat)
    {
        beatMultiplier = BeatIntensityMultiplier;
    }
    else
    {
        // Gradual return to normal
        beatMultiplier = 1.0f + (BeatIntensityMultiplier - 1.0f) * audio.BeatIntensity;
    }
    
    return Math.Max(0.0f, Math.Min(100.0f, GrainIntensity * beatMultiplier));
}
```

### Grain Animation
```csharp
private void UpdateGrainAnimation(float deltaTime)
{
    if (!EnableGrainAnimation)
        return;
    
    var animationProgress = (GetCurrentTime() * GrainAnimationSpeed) % (Math.PI * 2);
    
    switch (GrainAnimationMode)
    {
        case 0: // Pulsing
            var pulse = (Math.Sin(animationProgress) + 1.0f) * 0.5f;
            GrainIntensity = 20.0f + pulse * 60.0f;
            break;
            
        case 1: // Wave pattern
            var wave = Math.Sin(animationProgress * 3);
            GrainIntensity = 40.0f + wave * 30.0f;
            break;
            
        case 2: // Random walk
            if (Random.NextDouble() < 0.01f) // 1% chance per frame
            {
                GrainIntensity = Random.Next(20, 80);
            }
            break;
            
        case 3: // Directional movement
            var directionX = Math.Sin(animationProgress) * GrainDirectionX;
            var directionY = Math.Cos(animationProgress) * GrainDirectionY;
            UpdateDirectionalGrain(directionX, directionY);
            break;
    }
}
```

### Directional Grain
```csharp
private void UpdateDirectionalGrain(float directionX, float directionY)
{
    if (!EnableGrainDirectional)
        return;
    
    var width = GrainBuffer.Width;
    var height = GrainBuffer.Height;
    
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            var offsetX = (int)(directionX * GrainScale);
            var offsetY = (int)(directionY * GrainScale);
            
            var sourceX = (x + offsetX + width) % width;
            var sourceY = (y + offsetY + height) % height;
            
            var sourceIndex = sourceY * width + sourceX;
            var targetIndex = y * width + x;
            
            if (sourceIndex >= 0 && sourceIndex < GrainBuffer.Pixels.Length)
            {
                GrainBuffer.Pixels[targetIndex] = GrainBuffer.Pixels[sourceIndex];
            }
        }
    }
}
```

### Temporal Grain
```csharp
private void UpdateTemporalGrain(float deltaTime)
{
    if (!EnableTemporalGrain)
        return;
    
    var time = GetCurrentTime() * TemporalSpeed;
    
    switch (TemporalMode)
    {
        case 0: // Linear evolution
            var evolution = (time % 100.0f) / 100.0f;
            UpdateGrainEvolution(evolution);
            break;
            
        case 1: // Cyclic evolution
            var cycle = (Math.Sin(time * 0.1f) + 1.0f) * 0.5f;
            UpdateGrainEvolution(cycle);
            break;
            
        case 2: // Chaotic evolution
            var chaos = (Math.Sin(time * 0.05f) * Math.Cos(time * 0.03f) + 1.0f) * 0.5f;
            UpdateGrainEvolution(chaos);
            break;
    }
}

private void UpdateGrainEvolution(float evolution)
{
    var random = new Random((int)(evolution * 10000));
    
    for (int i = 0; i < GrainBuffer.Pixels.Length; i++)
    {
        if (random.NextDouble() < evolution * 0.1f)
        {
            var intensity = random.Next(0, 256);
            var threshold = random.Next(0, 101);
            GrainBuffer.Pixels[i] = (threshold << 8) | intensity;
        }
    }
}
```

## Performance Considerations

### Optimization Strategies
- **Lookup Tables**: Pre-computed random values for consistent performance
- **Static Grain**: Avoid recalculation for static patterns
- **SIMD Operations**: Use vectorized operations for pixel processing
- **Memory Access**: Optimize grain buffer access patterns
- **Early Exit**: Skip processing when disabled

### Memory Management
- **Buffer Reuse**: Reuse GrainBuffer instances when possible
- **Temporary Buffers**: Minimize temporary buffer allocations
- **Garbage Collection**: Avoid allocations in hot paths

## Integration with EffectGraph

### Input Ports
- **Image Input**: Source image buffer for grain application
- **Audio Input**: Audio features for beat-reactive behavior
- **Mask Input**: Optional grain mask image
- **Control Input**: External control signals for dynamic parameters

### Output Ports
- **Grained Image**: The processed image with applied grain
- **Grain Data**: Grain pattern and intensity metadata
- **Performance Metrics**: Processing time and memory usage

### Node Configuration
```csharp
public override void ConfigureNode()
{
    AddInputPort("Image", typeof(ImageBuffer));
    AddInputPort("Audio", typeof(AudioFeatures));
    AddInputPort("Mask", typeof(ImageBuffer));
    AddInputPort("Control", typeof(float));
    
    AddOutputPort("GrainedImage", typeof(ImageBuffer));
    AddOutputPort("GrainData", typeof(GrainData));
    AddOutputPort("Performance", typeof(PerformanceMetrics));
    
    SetMetadata(new EffectMetadata
    {
        Name = "Grain",
        Category = "Texture Effects",
        Description = "Adds film grain and noise effects with configurable blending modes",
        Version = "1.0.0",
        Author = "Phoenix Visualizer Team"
    });
}
```

## Advanced Features

### Grain Pattern Types
```csharp
private int GeneratePatternedGrain(int x, int y, int intensity)
{
    switch (GrainPatternType)
    {
        case 0: // Random
            return GenerateRandomGrain(intensity);
            
        case 1: // Perlin noise
            return GeneratePerlinGrain(x, y, intensity);
            
        case 2: // Simplex noise
            return GenerateSimplexGrain(x, y, intensity);
            
        case 3: // Cellular noise
            return GenerateCellularGrain(x, y, intensity);
            
        case 4: // Fractal noise
            return GenerateFractalGrain(x, y, intensity);
            
        default:
            return GenerateRandomGrain(intensity);
    }
}
```

### Grain Masking
```csharp
private int ApplyGrainMasking(int originalColor, int grainedColor, int pixelIndex)
{
    if (!EnableGrainMasking || GrainMask == null)
        return grainedColor;
    
    var maskPixel = GrainMask.Pixels[pixelIndex];
    var maskIntensity = (maskPixel & 0xFF) / 255.0f; // Use red channel as mask
    
    // Blend original and grained based on mask
    var blendFactor = maskIntensity * MaskInfluence;
    var finalColor = BlendColors(originalColor, grainedColor, blendFactor);
    
    return finalColor;
}
```

## Testing and Validation

### Unit Tests
```csharp
[Test]
public void TestBasicGrain()
{
    var node = new GrainEffectsNode
    {
        Enabled = true,
        BlendMode = 1, // Additive
        GrainIntensity = 50.0f,
        StaticGrain = false
    };
    
    var input = CreateTestImage(100, 100);
    var audio = CreateTestAudio();
    
    var output = node.ProcessEffect(input, audio);
    
    Assert.IsNotNull(output);
    Assert.AreEqual(100, output.Width);
    Assert.AreEqual(100, output.Height);
    
    // Test that grain is applied
    var hasGrain = false;
    for (int i = 0; i < output.Pixels.Length; i++)
    {
        if (output.Pixels[i] != input.Pixels[i])
        {
            hasGrain = true;
            break;
        }
    }
    
    Assert.IsTrue(hasGrain, "Grain effect should modify pixels");
}
```

### Performance Tests
```csharp
[Test]
public void TestPerformance()
{
    var node = new GrainEffectsNode();
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
- **Advanced Noise Algorithms**: More sophisticated noise generation
- **Grain Presets**: Predefined grain patterns and styles
- **Real-time Control**: MIDI and OSC control integration
- **3D Grain**: Depth-based grain effects
- **Grain Synthesis**: AI-generated grain patterns

### Research Areas
- **Film Grain Analysis**: Real film grain pattern analysis
- **Perceptual Effects**: Human visual system considerations
- **Machine Learning**: AI-generated grain optimization
- **Real-time Analysis**: Dynamic grain parameter adjustment

## Conclusion
The Grain effect provides powerful texture and noise generation capabilities with extensive customization options. Its beat-reactive nature and multiple blending modes make it suitable for creating dynamic, engaging visualizations. The implementation balances performance with flexibility, offering both real-time processing and high-quality output suitable for professional visualization applications.

The effect's ability to add realistic film grain, artistic texture, and atmospheric noise makes it an essential tool for AVS preset creation, allowing artists to create authentic film looks, add visual interest, and create mood-enhancing effects that respond to music.
