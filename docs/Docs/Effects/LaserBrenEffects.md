# Laser Bren Effects

## Overview
The Laser Bren effect creates specialized laser visualizations with advanced bren (brightness) control and dynamic intensity modulation. It's essential for creating high-intensity laser effects, brightness-controlled visualizations, and dynamic laser patterns in AVS presets with precise control over laser intensity, brightness curves, and beat-reactive behaviors.

## C++ Source Analysis
**File:** `rl_bren.cpp`  
**Class:** `C_THISCLASS : public C_RBASE`

### Key Properties
- **Laser Properties**: Color, intensity, brightness, and bren control
- **Brightness Modulation**: Dynamic brightness curves and patterns
- **Animation Control**: Movement, rotation, and scaling
- **Pattern Generation**: Different laser patterns and arrangements
- **Beat Reactivity**: Dynamic changes synchronized with audio
- **Bren Effects**: Special brightness-based visual effects

### Core Functionality
```cpp
class C_THISCLASS : public C_RBASE
{
    int color;
    int intensity;
    int brightness;
    int bren;
    int animation;
    int onbeat;
    int effect;
    int param1;
    int param2;
    int param3;
    
    virtual int render(char visdata[2][2][576], int isBeat, int *framebuffer, int *fbout, int w, int h);
    virtual HWND conf(HINSTANCE hInstance, HWND hwndParent);
    virtual char *get_desc();
    virtual void load_config(unsigned char *data, int len);
    virtual int save_config(unsigned char *data);
};
```

## C# Implementation

### LaserBrenEffectsNode Class
```csharp
public class LaserBrenEffectsNode : BaseEffectNode
{
    public int LaserColor { get; set; } = 0x00FF00;
    public float LaserIntensity { get; set; } = 1.0f;
    public float Brightness { get; set; } = 1.0f;
    public float BrenControl { get; set; } = 0.5f;
    public int AnimationMode { get; set; } = 0;
    public bool BeatReactive { get; set; } = false;
    public int BrenEffect { get; set; } = 0;
    public float LaserOpacity { get; set; } = 1.0f;
    public bool EnableGlow { get; set; } = true;
    public int GlowColor { get; set; } = 0x00FFFF;
    public float GlowIntensity { get; set; } = 0.5f;
    public float AnimationSpeed { get; set; } = 1.0f;
    public bool AnimatedLasers { get; set; } = true;
    public int LaserCount { get; set; } = 1;
    public float LaserSpacing { get; set; } = 25.0f;
    public bool RandomizeLasers { get; set; } = false;
    public float RandomSeed { get; set; } = 0.0f;
    public int BrenMode { get; set; } = 0;
    public float BrenCurve { get; set; } = 1.0f;
    public bool EnableBrenModulation { get; set; } = true;
}
```

### Key Features
1. **Advanced Laser Properties**: Color, intensity, brightness, and bren control
2. **Brightness Modulation**: Dynamic brightness curves and patterns
3. **Multiple Animation Modes**: Movement, rotation, scaling, and patterns
4. **Pattern Generation**: Various laser patterns and arrangements
5. **Beat Reactivity**: Dynamic laser changes synchronized with music
6. **Bren Effects**: Special brightness-based visual effects
7. **Performance Optimization**: Efficient laser rendering algorithms

### Bren Modes
- **0**: Linear (Linear brightness control)
- **1**: Exponential (Exponential brightness curve)
- **2**: Logarithmic (Logarithmic brightness curve)
- **3**: Sine Wave (Sine wave brightness modulation)
- **4**: Pulse (Pulse-based brightness control)
- **5**: Beat-Synced (Beat-synchronized brightness)
- **6**: Random (Random brightness variation)
- **7**: Custom (User-defined brightness curve)

### Animation Modes
- **0**: Static (No animation)
- **1**: Movement (Laser movement patterns)
- **2**: Rotation (Laser rotation)
- **3**: Scaling (Laser size changes)
- **4**: Pattern (Pattern-based animation)
- **5**: Beat-Synced (Beat-synchronized animation)
- **6**: Random (Random laser changes)
- **7**: Custom (User-defined animation)

### Bren Effects
- **0**: None (No special effects)
- **1**: Glow (Laser glow effect)
- **2**: Fade (Laser fade in/out)
- **3**: Pulse (Laser pulsing effect)
- **4**: Wave (Wave-like laser distortion)
- **5**: Glitch (Glitch effect on lasers)
- **6**: Particle (Particle trail effect)
- **7**: Custom (User-defined effects)

## Usage Examples

### Basic Laser Bren
```csharp
var laserBrenNode = new LaserBrenEffectsNode
{
    LaserColor = 0x00FF00, // Green
    LaserIntensity = 1.0f,
    Brightness = 1.0f,
    BrenControl = 0.5f,
    AnimationMode = 0, // Static
    BeatReactive = false,
    BrenEffect = 0, // None
    LaserOpacity = 1.0f,
    EnableGlow = true,
    GlowColor = 0x00FFFF,
    GlowIntensity = 0.6f,
    AnimationSpeed = 1.0f,
    BrenMode = 0, // Linear
    BrenCurve = 1.0f,
    EnableBrenModulation = true
};
```

### Beat-Reactive Bren Modulation
```csharp
var laserBrenNode = new LaserBrenEffectsNode
{
    LaserColor = 0xFF0000, // Red
    LaserIntensity = 1.5f,
    Brightness = 1.2f,
    BrenControl = 0.8f,
    AnimationMode = 5, // Beat-synced
    BeatReactive = true,
    BrenEffect = 1, // Glow
    LaserOpacity = 0.9f,
    EnableGlow = true,
    GlowColor = 0xFF00FF,
    GlowIntensity = 0.8f,
    AnimationSpeed = 2.0f,
    AnimatedLasers = true,
    LaserCount = 4,
    LaserSpacing = 35.0f,
    BrenMode = 4, // Pulse
    BrenCurve = 1.5f,
    EnableBrenModulation = true
};
```

### Exponential Bren Control
```csharp
var laserBrenNode = new LaserBrenEffectsNode
{
    LaserColor = 0x0000FF, // Blue
    LaserIntensity = 0.8f,
    Brightness = 0.9f,
    BrenControl = 0.6f,
    AnimationMode = 4, // Pattern
    BeatReactive = false,
    BrenEffect = 3, // Pulse
    LaserOpacity = 0.8f,
    EnableGlow = true,
    GlowColor = 0x0080FF,
    GlowIntensity = 0.4f,
    AnimationSpeed = 1.5f,
    AnimatedLasers = true,
    LaserCount = 6,
    LaserSpacing = 30.0f,
    RandomizeLasers = true,
    RandomSeed = 42.0f,
    BrenMode = 1, // Exponential
    BrenCurve = 2.0f,
    EnableBrenModulation = true
};
```

## Technical Implementation

### Core Laser Bren Algorithm
```csharp
protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
{
    if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
        return GetDefaultOutput();

    // Create output buffer
    var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
    
    // Copy input image to output
    Array.Copy(imageBuffer.Pixels, output.Pixels, imageBuffer.Pixels.Length);
    
    // Update laser animation state
    UpdateLaserAnimation(audioFeatures);
    
    // Calculate bren modulation
    float brenModulation = CalculateBrenModulation(audioFeatures);
    
    // Generate and render laser bren effects
    RenderLaserBren(output, brenModulation);
    
    return output;
}
```

### Laser Animation Update
```csharp
private void UpdateLaserAnimation(AudioFeatures audioFeatures)
{
    if (!AnimatedLasers)
        return;

    float currentTime = GetCurrentTime();
    
    // Update animation based on mode
    switch (AnimationMode)
    {
        case 1: // Movement
            UpdateMovementAnimation(currentTime);
            break;
        case 2: // Rotation
            UpdateRotationAnimation(currentTime);
            break;
        case 3: // Scaling
            UpdateScalingAnimation(currentTime);
            break;
        case 4: // Pattern
            UpdatePatternAnimation(currentTime);
            break;
        case 5: // Beat-synced
            UpdateBeatSyncedAnimation(audioFeatures);
            break;
        case 6: // Random
            UpdateRandomAnimation(currentTime);
            break;
        case 7: // Custom
            UpdateCustomAnimation(currentTime);
            break;
    }
}
```

### Bren Modulation Calculation
```csharp
private float CalculateBrenModulation(AudioFeatures audioFeatures)
{
    if (!EnableBrenModulation)
        return 1.0f;

    float baseModulation = BrenControl;
    float currentTime = GetCurrentTime();
    
    switch (BrenMode)
    {
        case 0: // Linear
            return baseModulation;
            
        case 1: // Exponential
            return (float)Math.Pow(baseModulation, BrenCurve);
            
        case 2: // Logarithmic
            return (float)Math.Log(baseModulation * BrenCurve + 1.0f) / (float)Math.Log(BrenCurve + 1.0f);
            
        case 3: // Sine Wave
            return baseModulation * (0.5f + 0.5f * (float)Math.Sin(currentTime * AnimationSpeed));
            
        case 4: // Pulse
            float pulse = (float)Math.Sin(currentTime * AnimationSpeed * 2.0f);
            return baseModulation * (0.5f + 0.5f * pulse);
            
        case 5: // Beat-Synced
            if (BeatReactive && audioFeatures?.IsBeat == true)
            {
                return baseModulation * 2.0f;
            }
            return baseModulation;
            
        case 6: // Random
            return baseModulation * (0.5f + 0.5f * (float)Random.NextDouble());
            
        case 7: // Custom
            return CalculateCustomBrenModulation(currentTime);
            
        default:
            return baseModulation;
    }
}
```

## Laser Rendering

### Main Laser Rendering
```csharp
private void RenderLaserBren(ImageBuffer output, float brenModulation)
{
    for (int laserIndex = 0; laserIndex < LaserCount; laserIndex++)
    {
        // Calculate laser position and properties
        var laserProperties = CalculateLaserProperties(laserIndex, brenModulation);
        
        // Render individual laser
        RenderSingleLaser(output, laserProperties);
    }
}
```

### Laser Property Calculation
```csharp
private LaserProperties CalculateLaserProperties(int laserIndex, float brenModulation)
{
    var properties = new LaserProperties();
    
    // Calculate laser position
    float centerX = output.Width / 2.0f;
    float centerY = output.Height / 2.0f;
    
    if (LaserCount == 1)
    {
        properties.StartX = centerX - 100.0f;
        properties.StartY = centerY;
        properties.EndX = centerX + 100.0f;
        properties.EndY = centerY;
    }
    else
    {
        // Multiple lasers with spacing
        float angleStep = 360.0f / LaserCount;
        float currentAngle = laserIndex * angleStep;
        float angleRad = currentAngle * (float)Math.PI / 180.0f;
        
        float offset = laserIndex * LaserSpacing;
        properties.StartX = centerX + (float)Math.Cos(angleRad) * offset;
        properties.StartY = centerY + (float)Math.Sin(angleRad) * offset;
        properties.EndX = properties.StartX + (float)Math.Cos(angleRad) * 200.0f;
        properties.EndY = properties.StartY + (float)Math.Sin(angleRad) * 200.0f;
    }
    
    // Apply randomization if enabled
    if (RandomizeLasers)
    {
        ApplyLaserRandomization(properties, laserIndex);
    }
    
    // Set laser appearance properties with bren modulation
    properties.Color = LaserColor;
    properties.Intensity = LaserIntensity * brenModulation;
    properties.Brightness = Brightness * brenModulation;
    properties.Opacity = LaserOpacity * brenModulation;
    properties.Effect = BrenEffect;
    
    return properties;
}
```

### Single Laser Rendering
```csharp
private void RenderSingleLaser(ImageBuffer output, LaserProperties properties)
{
    // Apply laser effects
    switch (properties.Effect)
    {
        case 0: // None
            RenderBasicLaser(output, properties);
            break;
        case 1: // Glow
            RenderGlowingLaser(output, properties);
            break;
        case 2: // Fade
            RenderFadingLaser(output, properties);
            break;
        case 3: // Pulse
            RenderPulsingLaser(output, properties);
            break;
        case 4: // Wave
            RenderWaveLaser(output, properties);
            break;
        case 5: // Glitch
            RenderGlitchLaser(output, properties);
            break;
        case 6: // Particle
            RenderParticleLaser(output, properties);
            break;
        case 7: // Custom
            RenderCustomLaser(output, properties);
            break;
    }
}
```

## Laser Effect Implementations

### Basic Laser Rendering
```csharp
private void RenderBasicLaser(ImageBuffer output, LaserProperties properties)
{
    // Use Bresenham's line algorithm for efficient rendering
    int x0 = (int)properties.StartX;
    int y0 = (int)properties.StartY;
    int x1 = (int)properties.EndX;
    int y1 = (int)properties.EndY;
    
    int dx = Math.Abs(x1 - x0);
    int dy = Math.Abs(y1 - y0);
    int sx = x0 < x1 ? 1 : -1;
    int sy = y0 < y1 ? 1 : -1;
    int err = dx - dy;
    
    while (true)
    {
        // Draw pixel with bren-modulated intensity
        DrawBrenPixel(output, x0, y0, properties);
        
        if (x0 == x1 && y0 == y1) break;
        
        int e2 = 2 * err;
        if (e2 > -dy)
        {
            err -= dy;
            x0 += sx;
        }
        if (e2 < dx)
        {
            err += dx;
            y0 += sy;
        }
    }
}
```

### Glowing Laser Rendering
```csharp
private void RenderGlowingLaser(ImageBuffer output, LaserProperties properties)
{
    if (!EnableGlow)
    {
        RenderBasicLaser(output, properties);
        return;
    }

    // Render multiple layers for glow effect with bren modulation
    for (int layer = 0; layer < 5; layer++)
    {
        var glowProperties = properties.Clone();
        glowProperties.Intensity = properties.Intensity * (1.0f - layer * 0.2f) * BrenControl;
        glowProperties.Opacity = properties.Opacity * (1.0f - layer * 0.2f);
        glowProperties.Color = BlendColors(properties.Color, GlowColor, layer * 0.2f);
        
        // Increase thickness for glow layers
        int thickness = 2 + layer * 2;
        RenderThickLaser(output, glowProperties, thickness);
    }
}
```

### Pulsing Laser Rendering
```csharp
private void RenderPulsingLaser(ImageBuffer output, LaserProperties properties)
{
    float pulseTime = GetCurrentTime() * AnimationSpeed;
    float pulseStrength = (float)Math.Sin(pulseTime) * 0.5f + 0.5f;
    
    // Apply pulsing to laser properties
    var pulsingProperties = properties.Clone();
    pulsingProperties.Intensity *= pulseStrength;
    pulsingProperties.Brightness *= pulseStrength;
    pulsingProperties.Opacity *= pulseStrength;
    
    // Re-render laser with new properties
    RenderBasicLaser(output, pulsingProperties);
}
```

## Bren-Specific Effects

### Bren Glow Effect
```csharp
private void ApplyBrenGlow(ImageBuffer output, LaserProperties properties)
{
    if (!EnableGlow)
        return;

    // Create bren-modulated glow around the laser
    int glowRadius = (int)(GlowIntensity * 25.0f * BrenControl);
    
    for (int y = Math.Max(0, (int)properties.StartY - glowRadius); 
         y < Math.Min(output.Height, (int)properties.EndY + glowRadius); y++)
    {
        for (int x = Math.Max(0, (int)properties.StartX - glowRadius); 
             x < Math.Min(output.Width, (int)properties.EndX + glowRadius); x++)
        {
            float distance = CalculateDistanceToLaser(x, y, properties);
            
            if (distance <= glowRadius)
            {
                float glowStrength = 1.0f - (distance / glowRadius);
                float brenGlowStrength = glowStrength * GlowIntensity * BrenControl;
                
                int glowPixel = BlendColors(output.GetPixel(x, y), 
                                          GlowColor, brenGlowStrength);
                output.SetPixel(x, y, glowPixel);
            }
        }
    }
}
```

### Bren Modulation Effects
```csharp
private void ApplyBrenModulationEffects(ImageBuffer output, LaserProperties properties)
{
    // Apply brightness modulation based on bren control
    float brightnessMod = properties.Brightness * BrenControl;
    
    // Apply intensity modulation
    float intensityMod = properties.Intensity * BrenControl;
    
    // Apply opacity modulation
    float opacityMod = properties.Opacity * BrenControl;
    
    // Create modulated laser properties
    var modulatedProperties = properties.Clone();
    modulatedProperties.Brightness = brightnessMod;
    modulatedProperties.Intensity = intensityMod;
    modulatedProperties.Opacity = opacityMod;
    
    // Re-render with modulated properties
    RenderBasicLaser(output, modulatedProperties);
}
```

## Performance Optimization

### Optimization Techniques
1. **Efficient Algorithms**: Use optimized line drawing algorithms
2. **Batch Processing**: Process multiple lasers together
3. **Early Exit**: Skip processing for invisible lasers
4. **Caching**: Cache bren calculations and effects
5. **Threading**: Multi-threaded laser rendering

### Memory Management
- Efficient laser property storage
- Minimize temporary allocations
- Use value types for calculations
- Optimize laser rendering operations

## Integration with EffectGraph

### Port Configuration
```csharp
protected override void InitializePorts()
{
    _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Background image"));
    _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Laser bren output"));
}
```

### Metadata Support
```csharp
public override EffectMetadata GetMetadata()
{
    var metadata = new EffectMetadata();
    metadata.Add("LaserColor", LaserColor);
    metadata.Add("LaserIntensity", LaserIntensity);
    metadata.Add("Brightness", Brightness);
    metadata.Add("BrenControl", BrenControl);
    metadata.Add("AnimationMode", AnimationMode);
    metadata.Add("BeatReactive", BeatReactive);
    metadata.Add("BrenEffect", BrenEffect);
    metadata.Add("LaserOpacity", LaserOpacity);
    metadata.Add("BrenMode", BrenMode);
    metadata.Add("BrenCurve", BrenCurve);
    metadata.Add("EnableBrenModulation", EnableBrenModulation);
    metadata.Add("LaserCount", LaserCount);
    return metadata;
}
```

## Testing and Validation

### Test Cases
1. **Basic Laser Rendering**: Verify laser drawing accuracy
2. **Bren Modulation**: Test all bren control modes
3. **Animation Modes**: Test all animation behaviors
4. **Laser Effects**: Test all effect types
5. **Performance**: Measure laser rendering speed
6. **Edge Cases**: Handle boundary conditions

### Validation Methods
- Visual comparison with reference images
- Performance benchmarking
- Memory usage analysis
- Bren modulation accuracy testing

## Future Enhancements

### Planned Features
1. **Advanced Bren Control**: More sophisticated brightness curves
2. **3D Lasers**: Three-dimensional laser support
3. **Real-time Effects**: Dynamic laser generation
4. **Hardware Acceleration**: GPU-accelerated laser rendering
5. **Custom Shaders**: User-defined laser effects

### Compatibility
- Full AVS preset compatibility
- Support for legacy bren modes
- Performance parity with original
- Extended functionality

## Conclusion

The Laser Bren effect provides essential brightness-controlled laser visualization capabilities for dynamic AVS presets. This C# implementation maintains full compatibility with the original while adding modern features like beat reactivity and enhanced bren modulation. Complete documentation ensures reliable operation in production environments with optimal performance and visual quality.
