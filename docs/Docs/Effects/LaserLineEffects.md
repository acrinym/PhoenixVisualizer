# Laser Line Effects

## Overview
The Laser Line effect creates dynamic laser line visualizations with configurable properties, patterns, and beat-reactive behaviors. It's essential for creating laser show effects, geometric patterns, and dynamic line-based visualizations in AVS presets with precise control over line properties, animation, and synchronization.

## C++ Source Analysis
**File:** `rl_line.cpp`  
**Class:** `C_THISCLASS : public C_RBASE`

### Key Properties
- **Line Properties**: Color, thickness, length, and style
- **Animation Control**: Movement, rotation, and scaling
- **Pattern Generation**: Different line patterns and arrangements
- **Beat Reactivity**: Dynamic changes synchronized with audio
- **Line Effects**: Glow, fade, and special effects
- **Performance Optimization**: Efficient line rendering algorithms

### Core Functionality
```cpp
class C_THISCLASS : public C_RBASE
{
    int color;
    int thickness;
    int length;
    int style;
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

### LaserLineEffectsNode Class
```csharp
public class LaserLineEffectsNode : BaseEffectNode
{
    public int LineColor { get; set; } = 0x00FF00;
    public int LineThickness { get; set; } = 2;
    public float LineLength { get; set; } = 100.0f;
    public int LineStyle { get; set; } = 0;
    public int AnimationMode { get; set; } = 0;
    public bool BeatReactive { get; set; } = false;
    public int LineEffect { get; set; } = 0;
    public float LineOpacity { get; set; } = 1.0f;
    public bool EnableGlow { get; set; } = true;
    public int GlowColor { get; set; } = 0x00FFFF;
    public float GlowIntensity { get; set; } = 0.5f;
    public float LineSpeed { get; set; } = 1.0f;
    public float LineRotation { get; set; } = 0.0f;
    public float LineScale { get; set; } = 1.0f;
    public bool AnimatedLines { get; set; } = true;
    public float AnimationSpeed { get; set; } = 1.0f;
    public int LineCount { get; set; } = 1;
    public float LineSpacing { get; set; } = 20.0f;
    public bool RandomizeLines { get; set; } = false;
    public float RandomSeed { get; set; } = 0.0f;
}
```

### Key Features
1. **Configurable Line Properties**: Color, thickness, length, and style
2. **Multiple Animation Modes**: Movement, rotation, scaling, and patterns
3. **Pattern Generation**: Various line patterns and arrangements
4. **Beat Reactivity**: Dynamic line changes synchronized with music
5. **Line Effects**: Glow, fade, and special visual effects
6. **Performance Optimization**: Efficient line rendering algorithms
7. **Custom Line Styles**: User-defined line appearance and behavior

### Line Styles
- **0**: Solid (Solid color line)
- **1**: Dashed (Dashed line pattern)
- **2**: Dotted (Dotted line pattern)
- **3**: Gradient (Gradient color line)
- **4**: Rainbow (Rainbow color line)
- **5**: Glowing (Glowing line effect)
- **6**: Animated (Animated line pattern)
- **7**: Custom (User-defined line style)

### Animation Modes
- **0**: Static (No animation)
- **1**: Movement (Line movement patterns)
- **2**: Rotation (Line rotation)
- **3**: Scaling (Line size changes)
- **4**: Pattern (Pattern-based animation)
- **5**: Beat-Synced (Beat-synchronized animation)
- **6**: Random (Random line changes)
- **7**: Custom (User-defined animation)

### Line Effects
- **0**: None (No special effects)
- **1**: Glow (Line glow effect)
- **2**: Fade (Line fade in/out)
- **3**: Pulse (Line pulsing effect)
- **4**: Wave (Wave-like line distortion)
- **5**: Glitch (Glitch effect on lines)
- **6**: Particle (Particle trail effect)
- **7**: Custom (User-defined effects)

## Usage Examples

### Basic Laser Line
```csharp
var laserLineNode = new LaserLineEffectsNode
{
    LineColor = 0x00FF00, // Green
    LineThickness = 3,
    LineLength = 150.0f,
    LineStyle = 0, // Solid
    AnimationMode = 0, // Static
    BeatReactive = false,
    LineEffect = 0, // None
    LineOpacity = 1.0f,
    EnableGlow = true,
    GlowColor = 0x00FFFF,
    GlowIntensity = 0.6f,
    LineSpeed = 1.0f,
    LineRotation = 0.0f,
    LineScale = 1.0f
};
```

### Beat-Reactive Animated Lines
```csharp
var laserLineNode = new LaserLineEffectsNode
{
    LineColor = 0xFF0000, // Red
    LineThickness = 4,
    LineLength = 200.0f,
    LineStyle = 5, // Glowing
    AnimationMode = 5, // Beat-synced
    BeatReactive = true,
    LineEffect = 1, // Glow
    LineOpacity = 0.9f,
    EnableGlow = true,
    GlowColor = 0xFF00FF,
    GlowIntensity = 0.8f,
    LineSpeed = 2.0f,
    LineRotation = 45.0f,
    LineScale = 1.5f,
    AnimatedLines = true,
    AnimationSpeed = 2.0f,
    LineCount = 5,
    LineSpacing = 30.0f
};
```

### Pattern-Based Line Animation
```csharp
var laserLineNode = new LaserLineEffectsNode
{
    LineColor = 0x0000FF, // Blue
    LineThickness = 2,
    LineLength = 120.0f,
    LineStyle = 2, // Dotted
    AnimationMode = 4, // Pattern
    BeatReactive = false,
    LineEffect = 3, // Pulse
    LineOpacity = 0.8f,
    EnableGlow = true,
    GlowColor = 0x0080FF,
    GlowIntensity = 0.4f,
    LineSpeed = 1.5f,
    LineRotation = 90.0f,
    LineScale = 0.8f,
    AnimatedLines = true,
    AnimationSpeed = 1.8f,
    LineCount = 8,
    LineSpacing = 25.0f,
    RandomizeLines = true,
    RandomSeed = 42.0f
};
```

## Technical Implementation

### Core Laser Line Algorithm
```csharp
protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
{
    if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
        return GetDefaultOutput();

    // Create output buffer
    var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
    
    // Copy input image to output
    Array.Copy(imageBuffer.Pixels, output.Pixels, imageBuffer.Pixels.Length);
    
    // Update line animation state
    UpdateLineAnimation(audioFeatures);
    
    // Generate and render laser lines
    RenderLaserLines(output);
    
    return output;
}
```

### Line Animation Update
```csharp
private void UpdateLineAnimation(AudioFeatures audioFeatures)
{
    if (!AnimatedLines)
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

### Beat-Synchronized Animation
```csharp
private void UpdateBeatSyncedAnimation(AudioFeatures audioFeatures)
{
    if (!BeatReactive || audioFeatures?.IsBeat != true)
        return;

    // Beat-reactive line changes
    LineScale = 1.0f + (float)Math.Sin(GetCurrentTime() * AnimationSpeed) * 0.3f;
    LineRotation += BeatReactive ? 15.0f : 5.0f;
    LineOpacity = Math.Min(1.0f, LineOpacity + 0.2f);
    
    // Reset opacity after beat
    if (LineOpacity > 1.0f)
    {
        LineOpacity = 1.0f;
    }
}
```

## Line Rendering

### Main Line Rendering
```csharp
private void RenderLaserLines(ImageBuffer output)
{
    for (int lineIndex = 0; lineIndex < LineCount; lineIndex++)
    {
        // Calculate line position and properties
        var lineProperties = CalculateLineProperties(lineIndex);
        
        // Render individual line
        RenderSingleLine(output, lineProperties);
    }
}
```

### Line Property Calculation
```csharp
private LineProperties CalculateLineProperties(int lineIndex)
{
    var properties = new LineProperties();
    
    // Calculate line position
    float centerX = output.Width / 2.0f;
    float centerY = output.Height / 2.0f;
    
    if (LineCount == 1)
    {
        properties.StartX = centerX - LineLength / 2.0f;
        properties.StartY = centerY;
        properties.EndX = centerX + LineLength / 2.0f;
        properties.EndY = centerY;
    }
    else
    {
        // Multiple lines with spacing
        float angleStep = 360.0f / LineCount;
        float currentAngle = lineIndex * angleStep + LineRotation;
        float angleRad = currentAngle * (float)Math.PI / 180.0f;
        
        float offset = lineIndex * LineSpacing;
        properties.StartX = centerX + (float)Math.Cos(angleRad) * offset;
        properties.StartY = centerY + (float)Math.Sin(angleRad) * offset;
        properties.EndX = properties.StartX + (float)Math.Cos(angleRad) * LineLength;
        properties.EndY = properties.StartY + (float)Math.Sin(angleRad) * LineLength;
    }
    
    // Apply randomization if enabled
    if (RandomizeLines)
    {
        ApplyLineRandomization(properties, lineIndex);
    }
    
    // Set line appearance properties
    properties.Color = LineColor;
    properties.Thickness = LineThickness;
    properties.Opacity = LineOpacity;
    properties.Style = LineStyle;
    properties.Effect = LineEffect;
    
    return properties;
}
```

### Single Line Rendering
```csharp
private void RenderSingleLine(ImageBuffer output, LineProperties properties)
{
    // Apply line style
    switch (properties.Style)
    {
        case 0: // Solid
            RenderSolidLine(output, properties);
            break;
        case 1: // Dashed
            RenderDashedLine(output, properties);
            break;
        case 2: // Dotted
            RenderDottedLine(output, properties);
            break;
        case 3: // Gradient
            RenderGradientLine(output, properties);
            break;
        case 4: // Rainbow
            RenderRainbowLine(output, properties);
            break;
        case 5: // Glowing
            RenderGlowingLine(output, properties);
            break;
        case 6: // Animated
            RenderAnimatedLine(output, properties);
            break;
        case 7: // Custom
            RenderCustomLine(output, properties);
            break;
    }
    
    // Apply line effects
    if (properties.Effect != 0)
    {
        ApplyLineEffects(output, properties);
    }
}
```

## Line Style Implementations

### Solid Line Rendering
```csharp
private void RenderSolidLine(ImageBuffer output, LineProperties properties)
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
        // Draw pixel with thickness
        DrawThickPixel(output, x0, y0, properties);
        
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

### Dashed Line Rendering
```csharp
private void RenderDashedLine(ImageBuffer output, LineProperties properties)
{
    float dashLength = 10.0f;
    float gapLength = 5.0f;
    float totalLength = dashLength + gapLength;
    
    float lineLength = (float)Math.Sqrt(
        Math.Pow(properties.EndX - properties.StartX, 2) + 
        Math.Pow(properties.EndY - properties.StartY, 2)
    );
    
    int dashCount = (int)(lineLength / totalLength);
    
    for (int i = 0; i < dashCount; i++)
    {
        float dashStart = i * totalLength;
        float dashEnd = dashStart + dashLength;
        
        // Calculate dash start and end points
        var dashStartPoint = InterpolatePoint(properties.StartX, properties.StartY, 
                                            properties.EndX, properties.EndY, dashStart / lineLength);
        var dashEndPoint = InterpolatePoint(properties.StartX, properties.StartY, 
                                          properties.EndX, properties.EndY, dashEnd / lineLength);
        
        // Render dash segment
        RenderLineSegment(output, dashStartPoint, dashEndPoint, properties);
    }
}
```

### Glowing Line Rendering
```csharp
private void RenderGlowingLine(ImageBuffer output, LineProperties properties)
{
    if (!EnableGlow)
    {
        RenderSolidLine(output, properties);
        return;
    }
    
    // Render multiple layers for glow effect
    for (int layer = 0; layer < 5; layer++)
    {
        var glowProperties = properties.Clone();
        glowProperties.Thickness = properties.Thickness + layer * 2;
        glowProperties.Opacity = properties.Opacity * (1.0f - layer * 0.2f);
        glowProperties.Color = BlendColors(properties.Color, GlowColor, layer * 0.2f);
        
        RenderSolidLine(output, glowProperties);
    }
}
```

## Line Effects

### Effect Application
```csharp
private void ApplyLineEffects(ImageBuffer output, LineProperties properties)
{
    switch (properties.Effect)
    {
        case 1: // Glow
            ApplyGlowEffect(output, properties);
            break;
        case 2: // Fade
            ApplyFadeEffect(output, properties);
            break;
        case 3: // Pulse
            ApplyPulseEffect(output, properties);
            break;
        case 4: // Wave
            ApplyWaveEffect(output, properties);
            break;
        case 5: // Glitch
            ApplyGlitchEffect(output, properties);
            break;
        case 6: // Particle
            ApplyParticleEffect(output, properties);
            break;
        case 7: // Custom
            ApplyCustomEffect(output, properties);
            break;
    }
}
```

### Glow Effect
```csharp
private void ApplyGlowEffect(ImageBuffer output, LineProperties properties)
{
    if (!EnableGlow)
        return;

    // Create glow around the line
    int glowRadius = (int)(GlowIntensity * 20.0f);
    
    for (int y = Math.Max(0, (int)properties.StartY - glowRadius); 
         y < Math.Min(output.Height, (int)properties.EndY + glowRadius); y++)
    {
        for (int x = Math.Max(0, (int)properties.StartX - glowRadius); 
             x < Math.Min(output.Width, (int)properties.EndX + glowRadius); x++)
        {
            float distance = CalculateDistanceToLine(x, y, properties);
            
            if (distance <= glowRadius)
            {
                float glowStrength = 1.0f - (distance / glowRadius);
                int glowPixel = BlendColors(output.GetPixel(x, y), GlowColor, glowStrength * GlowIntensity);
                output.SetPixel(x, y, glowPixel);
            }
        }
    }
}
```

### Pulse Effect
```csharp
private void ApplyPulseEffect(ImageBuffer output, LineProperties properties)
{
    float pulseTime = GetCurrentTime() * AnimationSpeed;
    float pulseStrength = (float)Math.Sin(pulseTime) * 0.5f + 0.5f;
    
    // Apply pulsing to line opacity
    properties.Opacity *= pulseStrength;
    
    // Re-render line with new opacity
    RenderSolidLine(output, properties);
}
```

## Performance Optimization

### Optimization Techniques
1. **Efficient Algorithms**: Use optimized line drawing algorithms
2. **Batch Processing**: Process multiple lines together
3. **Early Exit**: Skip processing for invisible lines
4. **Caching**: Cache line calculations and effects
5. **Threading**: Multi-threaded line rendering

### Memory Management
- Efficient line property storage
- Minimize temporary allocations
- Use value types for calculations
- Optimize line rendering operations

## Integration with EffectGraph

### Port Configuration
```csharp
protected override void InitializePorts()
{
    _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Background image"));
    _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Laser line output"));
}
```

### Metadata Support
```csharp
public override EffectMetadata GetMetadata()
{
    var metadata = new EffectMetadata();
    metadata.Add("LineColor", LineColor);
    metadata.Add("LineThickness", LineThickness);
    metadata.Add("LineLength", LineLength);
    metadata.Add("LineStyle", LineStyle);
    metadata.Add("AnimationMode", AnimationMode);
    metadata.Add("BeatReactive", BeatReactive);
    metadata.Add("LineEffect", LineEffect);
    metadata.Add("LineOpacity", LineOpacity);
    metadata.Add("EnableGlow", EnableGlow);
    metadata.Add("GlowIntensity", GlowIntensity);
    metadata.Add("LineSpeed", LineSpeed);
    metadata.Add("LineRotation", LineRotation);
    metadata.Add("LineScale", LineScale);
    metadata.Add("LineCount", LineCount);
    return metadata;
}
```

## Testing and Validation

### Test Cases
1. **Basic Line Rendering**: Verify line drawing accuracy
2. **Line Styles**: Test all line style types
3. **Animation Modes**: Test all animation behaviors
4. **Line Effects**: Test all effect types
5. **Performance**: Measure line rendering speed
6. **Edge Cases**: Handle boundary conditions

### Validation Methods
- Visual comparison with reference images
- Performance benchmarking
- Memory usage analysis
- Line accuracy testing

## Future Enhancements

### Planned Features
1. **Advanced Patterns**: More sophisticated line patterns
2. **3D Lines**: Three-dimensional line support
3. **Real-time Effects**: Dynamic line generation
4. **Hardware Acceleration**: GPU-accelerated line rendering
5. **Custom Shaders**: User-defined line effects

### Compatibility
- Full AVS preset compatibility
- Support for legacy line modes
- Performance parity with original
- Extended functionality

## Conclusion

The Laser Line effect provides essential line-based visualization capabilities for dynamic AVS presets. This C# implementation maintains full compatibility with the original while adding modern features like beat reactivity and enhanced line effects. Complete documentation ensures reliable operation in production environments with optimal performance and visual quality.
