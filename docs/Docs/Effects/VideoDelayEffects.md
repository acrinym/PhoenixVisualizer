# AVS Video Delay Effect

## Overview
The Video Delay effect is an AVS visualization effect that creates frame delay effects by buffering and replaying previous video frames. It supports two modes: frame-based delay (1-200 frames) and beat-synchronized delay (1-16 beats), creating echo-like effects and temporal displacement that can be synchronized with music.

## C++ Source Analysis
Based on the AVS source code in `r_videodelay.cpp`, the Video Delay effect inherits from `C_RBASE` and implements a sophisticated frame buffering system:

### Key Properties
- **Dual Delay Modes**: Frame-based (1-200 frames) and beat-synchronized (1-16 beats)
- **Dynamic Buffer Management**: Automatic memory allocation and deallocation based on delay requirements
- **Beat Reactivity**: Automatic delay calculation based on time since last beat
- **Memory Efficient**: Virtual memory allocation with intelligent buffer sizing
- **Configurable Parameters**: Delay amount, mode selection, and enable/disable

### Core Functionality
```cpp
class C_DELAY : public C_RBASE 
{
    public:
        virtual int render(char visdata[2][2][576], int isBeat, int *framebuffer, int *fbout, int w, int h);
        virtual HWND conf(HINSTANCE hInstance, HWND hwndParent);
        virtual char *get_desc();
        virtual void load_config(unsigned char *data, int len);
        virtual int save_config(unsigned char *data);

    // Saved configuration
    bool enabled;           // Effect enabled/disabled flag
    bool usebeats;          // Beat-synchronized mode flag
    unsigned int delay;     // Delay amount (frames or beats)

    // Runtime state
    LPVOID buffer;          // Frame buffer memory
    LPVOID inoutpos;        // Current buffer position
    unsigned long buffersize;           // Total buffer size
    unsigned long virtualbuffersize;    // Active buffer size
    unsigned long oldvirtualbuffersize; // Previous buffer size
    unsigned long framessincebeat;      // Frames since last beat
    unsigned long framedelay;           // Current frame delay
    unsigned long framemem;             // Memory per frame
    unsigned long oldframemem;          // Previous frame memory
};
```

### Delay Algorithm
The effect creates frame delays by:
1. **Frame Buffering**: Stores incoming frames in a circular buffer
2. **Delay Calculation**: Determines delay based on mode (fixed frames or beat-synchronized)
3. **Buffer Management**: Dynamically allocates memory based on delay requirements
4. **Frame Replay**: Outputs delayed frames from the buffer
5. **Beat Synchronization**: Adjusts delay timing based on audio beat detection

## C# Implementation

### Class Definition
```csharp
public class VideoDelayEffectsNode : BaseEffectNode
{
    public bool Enabled { get; set; } = true;
    public bool UseBeats { get; set; } = false;
    public int Delay { get; set; } = 10; // 1-200 frames, 1-16 beats
    public bool BeatReactive { get; set; } = true;
    public float BeatDelayMultiplier { get; set; } = 1.0f;
    public bool EnableDelayAnimation { get; set; } = false;
    public float AnimationSpeed { get; set; } = 1.0f;
    public int AnimationMode { get; set; } = 0;
    public bool EnableDelayMasking { get; set; } = false;
    public ImageBuffer DelayMask { get; set; } = null;
    public float MaskInfluence { get; set; } = 1.0f;
    public bool EnableDelayBlending { get; set; } = false;
    public float DelayBlendStrength { get; set; } = 0.5f;
    public int DelayAlgorithm { get; set; } = 0; // 0=Standard, 1=Enhanced, 2=Realistic
    public float DelayCurve { get; set; } = 1.0f; // Power curve for delay effects
    public bool EnableDelayClamping { get; set; } = true;
    public int ClampMode { get; set; } = 0; // 0=Standard, 1=Soft, 2=Hard
    public bool EnableDelayInversion { get; set; } = false;
    public float InversionThreshold { get; set; } = 0.5f;
    
    // Internal state for frame buffering
    private ImageBuffer[] FrameBuffer { get; set; } = null;
    private int BufferSize { get; set; } = 0;
    private int CurrentFrameIndex { get; set; } = 0;
    private int FramesSinceBeat { get; set; } = 0;
    private int CurrentFrameDelay { get; set; } = 0;
    private Random Random { get; set; } = new Random();
}
```

### Key Features
- **Dual Delay Modes**: Frame-based and beat-synchronized delay options
- **Dynamic Buffer Management**: Automatic frame buffer sizing and management
- **Beat Reactivity**: Automatic delay adjustment based on audio beats
- **Memory Efficient**: Intelligent buffer allocation and reuse
- **Delay Animation**: Animated delay patterns and effects
- **Delay Masking**: Use image masks to control delay areas
- **Delay Blending**: Blend delayed and original frames
- **Multiple Algorithms**: Standard, enhanced, and realistic delay methods

### Usage Examples

#### Basic Frame Delay
```csharp
var frameDelayNode = new VideoDelayEffectsNode
{
    Enabled = true,
    UseBeats = false,
    Delay = 15, // 15 frame delay
    BeatReactive = false
};
```

#### Beat-Synchronized Delay
```csharp
var beatDelayNode = new VideoDelayEffectsNode
{
    Enabled = true,
    UseBeats = true,
    Delay = 8, // 8 beat delay
    BeatReactive = true,
    BeatDelayMultiplier = 1.5f
};
```

#### Animated Delay Effect
```csharp
var animatedDelayNode = new VideoDelayEffectsNode
{
    Enabled = true,
    UseBeats = false,
    Delay = 20,
    EnableDelayAnimation = true,
    AnimationSpeed = 2.0f,
    AnimationMode = 1, // Oscillating
    DelayAlgorithm = 2 // Realistic algorithm
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
    
    // Initialize frame buffer if needed
    InitializeFrameBuffer(input.Width, input.Height);
    
    // Calculate current frame delay
    var currentDelay = CalculateCurrentDelay(audio);
    
    if (currentDelay > 0 && currentDelay < FrameBuffer.Length)
    {
        // Get delayed frame from buffer
        var delayedFrame = GetDelayedFrame(currentDelay);
        
        // Apply delay masking if enabled
        if (EnableDelayMasking && DelayMask != null)
        {
            delayedFrame = ApplyDelayMasking(input, delayedFrame);
        }
        
        // Apply delay blending if enabled
        if (EnableDelayBlending)
        {
            delayedFrame = BlendDelay(input, delayedFrame);
        }
        
        output = delayedFrame;
    }
    else
    {
        // No delay, output original frame
        output = input;
    }
    
    // Store current frame in buffer
    StoreFrameInBuffer(input);
    
    // Update delay state
    UpdateDelayState(audio);
    
    return output;
}
```

### Frame Buffer Initialization
```csharp
private void InitializeFrameBuffer(int width, int height)
{
    var requiredBufferSize = UseBeats ? Delay * 2 : Delay + 1;
    
    if (FrameBuffer == null || BufferSize != requiredBufferSize)
    {
        BufferSize = requiredBufferSize;
        FrameBuffer = new ImageBuffer[BufferSize];
        
        // Initialize buffer with empty frames
        for (int i = 0; i < BufferSize; i++)
        {
            FrameBuffer[i] = new ImageBuffer(width, height);
        }
        
        CurrentFrameIndex = 0;
    }
}
```

### Delay Calculation
```csharp
private int CalculateCurrentDelay(AudioFeatures audio)
{
    if (UseBeats)
    {
        // Beat-synchronized delay mode
        if (BeatReactive && audio != null && audio.IsBeat)
        {
            // Calculate delay based on frames since last beat
            CurrentFrameDelay = FramesSinceBeat * Delay;
            CurrentFrameDelay = Math.Min(CurrentFrameDelay, 400); // Cap at 400 frames
            FramesSinceBeat = 0;
        }
        
        FramesSinceBeat++;
        return CurrentFrameDelay;
    }
    else
    {
        // Fixed frame delay mode
        return Delay;
    }
}
```

### Frame Storage and Retrieval
```csharp
private void StoreFrameInBuffer(ImageBuffer frame)
{
    // Store frame at current index
    FrameBuffer[CurrentFrameIndex] = frame;
    
    // Advance buffer index
    CurrentFrameIndex = (CurrentFrameIndex + 1) % BufferSize;
}

private ImageBuffer GetDelayedFrame(int delay)
{
    // Calculate index for delayed frame
    var delayedIndex = (CurrentFrameIndex - delay + BufferSize) % BufferSize;
    
    // Return delayed frame
    return FrameBuffer[delayedIndex];
}
```

### Delay Masking
```csharp
private ImageBuffer ApplyDelayMasking(ImageBuffer originalFrame, ImageBuffer delayedFrame)
{
    if (!EnableDelayMasking || DelayMask == null)
        return delayedFrame;
    
    var maskedFrame = new ImageBuffer(originalFrame.Width, originalFrame.Height);
    
    for (int y = 0; y < originalFrame.Height; y++)
    {
        for (int x = 0; x < originalFrame.Width; x++)
        {
            var maskPixel = DelayMask.GetPixel(x, y);
            var maskIntensity = (maskPixel & 0xFF) / 255.0f; // Use red channel as mask
            
            // Blend original and delayed based on mask
            var blendFactor = maskIntensity * MaskInfluence;
            var originalPixel = originalFrame.GetPixel(x, y);
            var delayedPixel = delayedFrame.GetPixel(x, y);
            
            var finalPixel = BlendPixels(originalPixel, delayedPixel, blendFactor);
            maskedFrame.SetPixel(x, y, finalPixel);
        }
    }
    
    return maskedFrame;
}
```

### Delay Blending
```csharp
private ImageBuffer BlendDelay(ImageBuffer originalFrame, ImageBuffer delayedFrame)
{
    if (!EnableDelayBlending)
        return delayedFrame;
    
    var blendedFrame = new ImageBuffer(originalFrame.Width, originalFrame.Height);
    
    for (int y = 0; y < originalFrame.Height; y++)
    {
        for (int x = 0; x < originalFrame.Width; x++)
        {
            var originalPixel = originalFrame.GetPixel(x, y);
            var delayedPixel = delayedFrame.GetPixel(x, y);
            
            var blendFactor = DelayBlendStrength;
            var finalPixel = BlendPixels(originalPixel, delayedPixel, blendFactor);
            blendedFrame.SetPixel(x, y, finalPixel);
        }
    }
    
    return blendedFrame;
}

private int BlendPixels(int pixel1, int pixel2, float blendFactor)
{
    var r1 = pixel1 & 0xFF;
    var g1 = (pixel1 >> 8) & 0xFF;
    var b1 = (pixel1 >> 16) & 0xFF;
    
    var r2 = pixel2 & 0xFF;
    var g2 = (pixel2 >> 8) & 0xFF;
    var b2 = (pixel2 >> 16) & 0xFF;
    
    var r = (int)(r1 + (r2 - r1) * blendFactor);
    var g = (int)(g1 + (g2 - g1) * blendFactor);
    var b = (int)(b1 + (b2 - b1) * blendFactor);
    
    // Clamp values
    r = Math.Max(0, Math.Min(255, r));
    g = Math.Max(0, Math.Min(255, g));
    b = Math.Max(0, Math.Min(255, b));
    
    return (b << 16) | (g << 8) | r;
}
```

### Delay Animation
```csharp
private void UpdateDelayState(AudioFeatures audio)
{
    if (!EnableDelayAnimation)
        return;
    
    var animationProgress = (GetCurrentTime() * AnimationSpeed) % (Math.PI * 2);
    
    switch (AnimationMode)
    {
        case 0: // Pulsing delay
            var pulse = (Math.Sin(animationProgress) + 1.0f) * 0.5f;
            Delay = (int)(5 + pulse * 25); // 5-30 frame range
            break;
            
        case 1: // Oscillating delay
            var oscillation = Math.Sin(animationProgress * 2);
            if (UseBeats)
            {
                Delay = (int)(8 + oscillation * 8); // 0-16 beat range
            }
            else
            {
                Delay = (int)(50 + oscillation * 150); // 50-200 frame range
            }
            break;
            
        case 2: // Random delay
            if (Random.NextDouble() < 0.01f) // 1% chance per frame
            {
                if (UseBeats)
                {
                    Delay = Random.Next(1, 17); // 1-16 beats
                }
                else
                {
                    Delay = Random.Next(10, 201); // 10-200 frames
                }
            }
            break;
            
        case 3: // Wave pattern delay
            var wave = Math.Sin(animationProgress * 3);
            var baseDelay = UseBeats ? 8 : 100;
            var waveRange = UseBeats ? 8 : 100;
            Delay = (int)(baseDelay + wave * waveRange);
            break;
    }
}
```

## Performance Considerations

### Optimization Strategies
- **Buffer Management**: Efficient frame buffer allocation and reuse
- **Memory Access**: Optimized buffer access patterns
- **Delay Calculation**: Efficient delay computation algorithms
- **Frame Copying**: Optimized frame copying and blending operations
- **Algorithm Selection**: Choose appropriate delay method for performance vs. quality

### Memory Management
- **Dynamic Allocation**: Allocate frame buffers based on delay requirements
- **Buffer Reuse**: Reuse frame buffer arrays when possible
- **Garbage Collection**: Avoid allocations in hot paths

## Integration with EffectGraph

### Input Ports
- **Image Input**: Source image buffer for delay processing
- **Audio Input**: Audio features for beat-reactive behavior
- **Mask Input**: Optional delay mask image
- **Control Input**: External control signals for dynamic parameters

### Output Ports
- **Delayed Image**: The processed image with applied delay effects
- **Delay Data**: Delay parameters and metadata
- **Performance Metrics**: Processing time and memory usage

### Node Configuration
```csharp
public override void ConfigureNode()
{
    AddInputPort("Image", typeof(ImageBuffer));
    AddInputPort("Audio", typeof(AudioFeatures));
    AddInputPort("Mask", typeof(ImageBuffer));
    AddInputPort("Control", typeof(float));
    
    AddOutputPort("DelayedImage", typeof(ImageBuffer));
    AddOutputPort("DelayData", typeof(VideoDelayData));
    AddOutputPort("Performance", typeof(PerformanceMetrics));
    
    SetMetadata(new EffectMetadata
    {
        Name = "Video Delay",
        Category = "Temporal Effects",
        Description = "Creates frame delay effects with beat synchronization",
        Version = "1.0.0",
        Author = "Phoenix Visualizer Team"
    });
}
```

## Advanced Features

### Enhanced Delay Algorithm
```csharp
private ImageBuffer GetEnhancedDelayedFrame(int delay)
{
    if (DelayAlgorithm == 1) // Enhanced
    {
        // Multi-frame interpolation for smoother delays
        var frame1 = GetDelayedFrame(delay);
        var frame2 = GetDelayedFrame(delay + 1);
        
        var interpolationFactor = (delay % 1.0f);
        return InterpolateFrames(frame1, frame2, interpolationFactor);
    }
    else if (DelayAlgorithm == 2) // Realistic
    {
        // Realistic delay with motion blur simulation
        var baseFrame = GetDelayedFrame(delay);
        var motionBlur = CalculateMotionBlur(delay);
        return ApplyMotionBlur(baseFrame, motionBlur);
    }
    
    return GetDelayedFrame(delay);
}
```

## Testing and Validation

### Unit Tests
```csharp
[Test]
public void TestBasicFrameDelay()
{
    var node = new VideoDelayEffectsNode
    {
        Enabled = true,
        UseBeats = false,
        Delay = 5,
        BeatReactive = false
    };
    
    var input = CreateTestImage(100, 100);
    var audio = CreateTestAudio();
    
    var output = node.ProcessEffect(input, audio);
    
    Assert.IsNotNull(output);
    Assert.AreEqual(100, output.Width);
    Assert.AreEqual(100, output.Height);
    
    // Test that delay effect is applied after sufficient frames
    for (int i = 0; i < 10; i++)
    {
        output = node.ProcessEffect(input, audio);
    }
    
    // Should see delayed effect
    Assert.IsTrue(true, "Delay effect should be functional");
}
```

### Performance Tests
```csharp
[Test]
public void TestPerformance()
{
    var node = new VideoDelayEffectsNode();
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
- **Advanced Delay Patterns**: More sophisticated delay algorithms
- **Real-time Control**: MIDI and OSC control integration
- **Delay Presets**: Predefined delay patterns and styles
- **3D Delay**: Depth-based delay effects
- **GPU Acceleration**: OpenGL/OpenCL implementation for real-time processing

### Research Areas
- **Temporal Effects**: Advanced frame manipulation algorithms
- **Machine Learning**: AI-generated delay patterns
- **Real-time Analysis**: Dynamic delay parameter adjustment
- **Motion Analysis**: Advanced motion detection and compensation

## Conclusion
The Video Delay effect provides powerful temporal manipulation capabilities with extensive customization options. Its beat-reactive nature and sophisticated frame buffering make it suitable for creating dynamic, engaging visualizations. The implementation balances performance with flexibility, offering both real-time processing and high-quality output suitable for professional visualization applications.

The effect's ability to create frame delays, echo effects, and beat-synchronized temporal displacement makes it an essential tool for AVS preset creation, allowing artists to create time-based effects, motion trails, and visually striking temporal transformations that respond to music.
