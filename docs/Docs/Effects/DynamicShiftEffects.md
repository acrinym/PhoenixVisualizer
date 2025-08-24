# AVS Dynamic Shift Effect

## Overview
The Dynamic Shift effect is an AVS visualization effect that applies dynamic spatial transformations to the image buffer. It creates shifting, warping, and morphing effects by manipulating pixel positions based on mathematical functions and audio input.

## C++ Source Analysis
Based on the AVS source code structure, the Dynamic Shift effect inherits from `C_RBASE` and implements dynamic spatial transformations. It uses mathematical functions to calculate pixel displacement and applies various shift modes including:
- Wave-based shifting
- Spiral shifting
- Radial shifting
- Audio-reactive shifting
- Custom mathematical shifting

## C# Implementation

### Class Definition
```csharp
public class DynamicShiftEffectsNode : BaseEffectNode
{
    public int ShiftMode { get; set; } = 0;
    public float ShiftIntensity { get; set; } = 1.0f;
    public float ShiftSpeed { get; set; } = 1.0f;
    public int ShiftDirection { get; set; } = 0;
    public bool BeatReactive { get; set; } = false;
    public float BeatShiftIntensity { get; set; } = 2.0f;
    public float ShiftFrequency { get; set; } = 1.0f;
    public float ShiftPhase { get; set; } = 0.0f;
    public int ShiftAlgorithm { get; set; } = 0;
    public float ShiftRange { get; set; } = 100.0f;
    public bool EnableInterpolation { get; set; } = true;
    public int InterpolationMode { get; set; } = 1;
    public float ShiftEasing { get; set; } = 1.0f;
    public bool EnableBoundaryHandling { get; set; } = true;
    public int BoundaryMode { get; set; } = 0;
    public float ShiftScale { get; set; } = 1.0f;
    public bool EnableMultiPass { get; set; } = false;
    public int PassCount { get; set; } = 2;
    public float[] PassWeights { get; set; } = new float[0];
    public bool EnableCustomFunction { get; set; } = false;
    public string CustomFunction { get; set; } = "";
}
```

### Key Features
- **Multiple Shift Modes**: Wave, spiral, radial, and custom mathematical shifting
- **Audio Reactivity**: Beat-synchronized shifting with configurable intensity
- **Interpolation**: Multiple interpolation modes for smooth shifting
- **Boundary Handling**: Configurable behavior at image boundaries
- **Multi-pass Processing**: Optional multi-pass shifting for complex effects
- **Custom Functions**: Support for user-defined mathematical functions

### Usage Examples

#### Basic Wave Shifting
```csharp
var shiftNode = new DynamicShiftEffectsNode
{
    ShiftMode = 0, // Wave mode
    ShiftIntensity = 50.0f,
    ShiftSpeed = 2.0f,
    ShiftFrequency = 0.5f,
    BeatReactive = true,
    BeatShiftIntensity = 3.0f
};
```

#### Spiral Shifting
```csharp
var spiralShift = new DynamicShiftEffectsNode
{
    ShiftMode = 1, // Spiral mode
    ShiftIntensity = 75.0f,
    ShiftSpeed = 1.5f,
    ShiftDirection = 1, // Clockwise
    ShiftRange = 150.0f,
    EnableInterpolation = true,
    InterpolationMode = 2 // Bicubic
};
```

#### Custom Mathematical Shifting
```csharp
var customShift = new DynamicShiftEffectsNode
{
    ShiftMode = 3, // Custom mode
    ShiftIntensity = 100.0f,
    ShiftSpeed = 1.0f,
    EnableCustomFunction = true,
    CustomFunction = "sin(x*0.1 + time) * cos(y*0.1 + time) * intensity",
    ShiftRange = 200.0f,
    EnableMultiPass = true,
    PassCount = 3
};
```

## Technical Implementation

### Core Processing Algorithm
```csharp
protected override ImageBuffer ProcessEffect(ImageBuffer input, AudioFeatures audio)
{
    var output = new ImageBuffer(input.Width, input.Height);
    var time = GetCurrentTime();
    
    for (int y = 0; y < input.Height; y++)
    {
        for (int x = 0; x < input.Width; x++)
        {
            var shiftX = CalculateShiftX(x, y, time, audio);
            var shiftY = CalculateShiftY(x, y, time, audio);
            
            var sourceX = x + shiftX;
            var sourceY = y + shiftY;
            
            if (IsValidCoordinate(sourceX, sourceY, input.Width, input.Height))
            {
                var color = GetInterpolatedColor(input, sourceX, sourceY);
                output.SetPixel(x, y, color);
            }
        }
    }
    
    return output;
}
```

### Shift Calculation Methods
```csharp
private float CalculateShiftX(int x, int y, float time, AudioFeatures audio)
{
    var normalizedX = (float)x / Width;
    var normalizedY = (float)y / Height;
    var intensity = BeatReactive ? ShiftIntensity * (1.0f + audio.BeatIntensity * BeatShiftIntensity) : ShiftIntensity;
    
    switch (ShiftMode)
    {
        case 0: // Wave
            return (float)(Math.Sin(normalizedX * ShiftFrequency * Math.PI * 2 + time * ShiftSpeed + ShiftPhase) * intensity);
        case 1: // Spiral
            var angle = Math.Atan2(normalizedY - 0.5f, normalizedX - 0.5f);
            var distance = Math.Sqrt(Math.Pow(normalizedX - 0.5f, 2) + Math.Pow(normalizedY - 0.5f, 2));
            return (float)(Math.Cos(angle + time * ShiftSpeed) * distance * intensity);
        case 2: // Radial
            var centerX = normalizedX - 0.5f;
            var centerY = normalizedY - 0.5f;
            var radius = Math.Sqrt(centerX * centerX + centerY * centerY);
            return (float)(Math.Cos(radius * ShiftFrequency + time * ShiftSpeed) * intensity);
        case 3: // Custom
            return EvaluateCustomFunction(normalizedX, normalizedY, time, intensity);
        default:
            return 0.0f;
    }
}
```

### Interpolation Methods
```csharp
private int GetInterpolatedColor(ImageBuffer input, float x, float y)
{
    if (!EnableInterpolation)
    {
        return input.GetPixel((int)x, (int)y);
    }
    
    switch (InterpolationMode)
    {
        case 0: // Nearest neighbor
            return input.GetPixel((int)Math.Round(x), (int)Math.Round(y));
        case 1: // Bilinear
            return GetBilinearInterpolatedColor(input, x, y);
        case 2: // Bicubic
            return GetBicubicInterpolatedColor(input, x, y);
        case 3: // Lanczos
            return GetLanczosInterpolatedColor(input, x, y);
        default:
            return input.GetPixel((int)x, (int)y);
    }
}
```

### Multi-pass Processing
```csharp
private ImageBuffer ProcessMultiPass(ImageBuffer input, AudioFeatures audio)
{
    if (!EnableMultiPass || PassCount <= 1)
    {
        return ProcessEffect(input, audio);
    }
    
    var current = input;
    var weights = PassWeights.Length > 0 ? PassWeights : GenerateDefaultWeights(PassCount);
    
    for (int pass = 0; pass < PassCount; pass++)
    {
        var passIntensity = ShiftIntensity * weights[pass];
        var passNode = new DynamicShiftEffectsNode
        {
            ShiftMode = ShiftMode,
            ShiftIntensity = passIntensity,
            ShiftSpeed = ShiftSpeed,
            ShiftDirection = ShiftDirection,
            BeatReactive = BeatReactive,
            BeatShiftIntensity = BeatShiftIntensity,
            ShiftFrequency = ShiftFrequency,
            ShiftPhase = ShiftPhase + pass * (Math.PI * 2 / PassCount),
            ShiftAlgorithm = ShiftAlgorithm,
            ShiftRange = ShiftRange,
            EnableInterpolation = EnableInterpolation,
            InterpolationMode = InterpolationMode,
            ShiftEasing = ShiftEasing,
            EnableBoundaryHandling = EnableBoundaryHandling,
            BoundaryMode = BoundaryMode,
            ShiftScale = ShiftScale
        };
        
        current = passNode.ProcessEffect(current, audio);
    }
    
    return current;
}
```

## Performance Considerations

### Optimization Strategies
- **Spatial Coherence**: Process pixels in cache-friendly order
- **SIMD Operations**: Use vectorized operations for shift calculations
- **LOD System**: Implement level-of-detail for distant pixels
- **Caching**: Cache shift calculations for repeated frames
- **Parallel Processing**: Process image regions in parallel

### Memory Management
- **Buffer Reuse**: Reuse ImageBuffer instances when possible
- **Temporary Buffers**: Minimize temporary buffer allocations
- **Garbage Collection**: Avoid allocations in hot paths

## Integration with EffectGraph

### Input Ports
- **Image Input**: Source image buffer for shifting
- **Audio Input**: Audio features for beat-reactive shifting
- **Control Input**: External control signals for dynamic parameters

### Output Ports
- **Shifted Image**: The processed image with applied shifting
- **Shift Data**: Shift vectors and metadata for debugging
- **Performance Metrics**: Processing time and memory usage

### Node Configuration
```csharp
public override void ConfigureNode()
{
    AddInputPort("Image", typeof(ImageBuffer));
    AddInputPort("Audio", typeof(AudioFeatures));
    AddInputPort("Control", typeof(float));
    
    AddOutputPort("ShiftedImage", typeof(ImageBuffer));
    AddOutputPort("ShiftData", typeof(ShiftData));
    AddOutputPort("Performance", typeof(PerformanceMetrics));
    
    SetMetadata(new EffectMetadata
    {
        Name = "Dynamic Shift",
        Category = "Spatial Transformation",
        Description = "Applies dynamic spatial transformations with audio reactivity",
        Version = "1.0.0",
        Author = "Phoenix Visualizer Team"
    });
}
```

## Advanced Features

### Custom Function Engine
```csharp
private float EvaluateCustomFunction(float x, float y, float time, float intensity)
{
    if (string.IsNullOrEmpty(CustomFunction))
        return 0.0f;
    
    try
    {
        // Simple expression evaluator for mathematical functions
        var expression = CustomFunction
            .Replace("x", x.ToString())
            .Replace("y", y.ToString())
            .Replace("time", time.ToString())
            .Replace("intensity", intensity.ToString());
        
        return EvaluateExpression(expression);
    }
    catch
    {
        return 0.0f;
    }
}
```

### Boundary Handling
```csharp
private int HandleBoundary(int x, int y, int width, int height)
{
    if (!EnableBoundaryHandling)
        return 0;
    
    switch (BoundaryMode)
    {
        case 0: // Clamp
            return Math.Max(0, Math.Min(width - 1, x));
        case 1: // Wrap
            return ((x % width) + width) % width;
        case 2: // Mirror
            if (x < 0) return -x;
            if (x >= width) return 2 * width - x - 1;
            return x;
        case 3: // Extend
            if (x < 0) return 0;
            if (x >= width) return width - 1;
            return x;
        default:
            return x;
    }
}
```

## Testing and Validation

### Unit Tests
```csharp
[Test]
public void TestWaveShifting()
{
    var node = new DynamicShiftEffectsNode
    {
        ShiftMode = 0,
        ShiftIntensity = 10.0f,
        ShiftSpeed = 1.0f
    };
    
    var input = CreateTestImage(100, 100);
    var audio = CreateTestAudio();
    
    var output = node.ProcessEffect(input, audio);
    
    Assert.IsNotNull(output);
    Assert.AreEqual(100, output.Width);
    Assert.AreEqual(100, output.Height);
    Assert.AreNotEqual(input.GetPixel(50, 50), output.GetPixel(50, 50));
}
```

### Performance Tests
```csharp
[Test]
public void TestPerformance()
{
    var node = new DynamicShiftEffectsNode();
    var input = CreateTestImage(1920, 1080);
    var audio = CreateTestAudio();
    
    var stopwatch = Stopwatch.StartNew();
    var output = node.ProcessEffect(input, audio);
    stopwatch.Stop();
    
    var processingTime = stopwatch.ElapsedMilliseconds;
    Assert.Less(processingTime, 100); // Should process in under 100ms
}
```

## Future Enhancements

### Planned Features
- **GPU Acceleration**: OpenGL/OpenCL implementation for real-time processing
- **Advanced Functions**: Support for complex mathematical expressions
- **Preset System**: Predefined shift patterns and animations
- **Real-time Control**: MIDI and OSC control integration
- **3D Shifting**: Depth-based shifting for stereoscopic effects

### Research Areas
- **Machine Learning**: AI-generated shift patterns
- **Procedural Generation**: Algorithmic shift pattern creation
- **Audio Analysis**: Advanced audio feature extraction for shifting
- **Temporal Coherence**: Frame-to-frame consistency optimization

## Conclusion
The Dynamic Shift effect provides powerful spatial transformation capabilities with extensive customization options. Its audio-reactive nature and mathematical foundation make it suitable for creating dynamic, engaging visualizations. The implementation balances performance with flexibility, offering both real-time processing and high-quality output suitable for professional visualization applications.
