# Dynamic Movement Effects

## Overview
The Dynamic Movement effect creates animated movement patterns with configurable properties, paths, and beat-reactive behaviors. It's essential for creating dynamic visual motion, animated transitions, and moving visual elements in AVS presets with precise control over movement patterns, speed, and synchronization.

## C++ Source Analysis
**File:** `r_dynamicmovement.cpp`  
**Class:** `C_THISCLASS : public C_RBASE`

### Key Properties
- **Movement Type**: Different movement algorithms and patterns
- **Movement Speed**: Control over animation speed and timing
- **Movement Path**: Configurable movement paths and trajectories
- **Beat Reactivity**: Dynamic movement synchronized with audio
- **Movement Parameters**: Customizable movement properties
- **Path Generation**: Algorithmic path creation and modification

### Core Functionality
```cpp
class C_THISCLASS : public C_RBASE
{
    int movetype;
    int speed;
    int path;
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

### DynamicMovementEffectsNode Class
```csharp
public class DynamicMovementEffectsNode : BaseEffectNode
{
    public int MovementType { get; set; } = 0;
    public float MovementSpeed { get; set; } = 1.0f;
    public int MovementPath { get; set; } = 0;
    public bool BeatReactive { get; set; } = false;
    public int MovementEffect { get; set; } = 0;
    public float MovementOpacity { get; set; } = 1.0f;
    public bool EnableGlow { get; set; } = true;
    public int GlowColor { get; set; } = 0x00FFFF;
    public float GlowIntensity { get; set; } = 0.5f;
    public float AnimationSpeed { get; set; } = 1.0f;
    public bool AnimatedMovement { get; set; } = true;
    public int MovementCount { get; set; } = 1;
    public float MovementSpacing { get; set; } = 25.0f;
    public bool RandomizeMovement { get; set; } = false;
    public float RandomSeed { get; set; } = 0.0f;
    public int PathMode { get; set; } = 0;
    public float PathScale { get; set; } = 1.0f;
    public bool EnablePathAnimation { get; set; } = true;
    public float PathAnimationSpeed { get; set; } = 1.0f;
    public bool EnableTrail { get; set; } = false;
    public float TrailLength { get; set; } = 10.0f;
    public float TrailOpacity { get; set; } = 0.5f;
}
```

### Key Features
1. **Multiple Movement Types**: Different movement algorithms and behaviors
2. **Configurable Paths**: Various movement paths and trajectories
3. **Speed Control**: Precise control over movement speed and timing
4. **Beat Reactivity**: Dynamic movement synchronized with music
5. **Path Animation**: Animated movement paths
6. **Trail Effects**: Movement trail visualization
7. **Performance Optimization**: Efficient movement algorithms

### Movement Types
- **0**: Linear (Straight-line movement)
- **1**: Circular (Circular path movement)
- **2**: Spiral (Spiral path movement)
- **3**: Wave (Wave-pattern movement)
- **4**: Random (Random movement patterns)
- **5**: Follow (Follow-the-leader movement)
- **6**: Bounce (Bouncing movement)
- **7**: Custom (User-defined movement)

### Movement Paths
- **0**: Straight (Straight-line paths)
- **1**: Curved (Curved paths)
- **2**: Zigzag (Zigzag patterns)
- **3**: Figure-8 (Figure-8 patterns)
- **4**: Lissajous (Lissajous curves)
- **5**: Fractal (Fractal-based paths)
- **6**: Noise (Noise-based paths)
- **7**: Custom (User-defined paths)

### Path Modes
- **0**: Static (Fixed path)
- **1**: Animated (Animated path)
- **2**: Beat-Synced (Beat-synchronized path)
- **3**: Interactive (Interactive path)
- **4**: Adaptive (Adaptive path)
- **5**: Reactive (Audio-reactive path)
- **6**: Random (Random path changes)
- **7**: Custom (User-defined path behavior)

## Usage Examples

### Basic Linear Movement
```csharp
var dynamicMovementNode = new DynamicMovementEffectsNode
{
    MovementType = 0, // Linear
    MovementSpeed = 1.5f,
    MovementPath = 0, // Straight
    BeatReactive = false,
    MovementEffect = 0, // None
    MovementOpacity = 1.0f,
    EnableGlow = true,
    GlowColor = 0x00FFFF,
    GlowIntensity = 0.6f,
    AnimationSpeed = 1.0f,
    PathMode = 0, // Static
    PathScale = 1.0f,
    EnablePathAnimation = false,
    EnableTrail = false
};
```

### Beat-Reactive Circular Movement
```csharp
var dynamicMovementNode = new DynamicMovementEffectsNode
{
    MovementType = 1, // Circular
    MovementSpeed = 2.0f,
    MovementPath = 1, // Curved
    BeatReactive = true,
    MovementEffect = 1, // Glow
    MovementOpacity = 0.9f,
    EnableGlow = true,
    GlowColor = 0xFF00FF,
    GlowIntensity = 0.8f,
    AnimationSpeed = 2.0f,
    AnimatedMovement = true,
    MovementCount = 3,
    MovementSpacing = 35.0f,
    PathMode = 2, // Beat-Synced
    PathScale = 1.5f,
    EnablePathAnimation = true,
    PathAnimationSpeed = 1.5f,
    EnableTrail = true,
    TrailLength = 15.0f,
    TrailOpacity = 0.6f
};
```

### Complex Spiral Movement
```csharp
var dynamicMovementNode = new DynamicMovementEffectsNode
{
    MovementType = 2, // Spiral
    MovementSpeed = 1.8f,
    MovementPath = 2, // Zigzag
    BeatReactive = false,
    MovementEffect = 2, // Pulse
    MovementOpacity = 0.8f,
    EnableGlow = true,
    GlowColor = 0x0080FF,
    GlowIntensity = 0.4f,
    AnimationSpeed = 1.5f,
    AnimatedMovement = true,
    MovementCount = 5,
    MovementSpacing = 30.0f,
    RandomizeMovement = true,
    RandomSeed = 42.0f,
    PathMode = 1, // Animated
    PathScale = 2.0f,
    EnablePathAnimation = true,
    PathAnimationSpeed = 2.0f,
    EnableTrail = true,
    TrailLength = 20.0f,
    TrailOpacity = 0.7f
};
```

## Technical Implementation

### Core Movement Algorithm
```csharp
protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
{
    if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
        return GetDefaultOutput();

    // Create output buffer
    var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
    
    // Copy input image to output
    Array.Copy(imageBuffer.Pixels, output.Pixels, imageBuffer.Pixels.Length);
    
    // Update movement state
    UpdateMovementState(audioFeatures);
    
    // Generate and render dynamic movement
    RenderDynamicMovement(output, audioFeatures);
    
    return output;
}
```

### Movement State Update
```csharp
private void UpdateMovementState(AudioFeatures audioFeatures)
{
    if (!AnimatedMovement)
        return;

    float currentTime = GetCurrentTime();
    
    // Update movement based on type
    switch (MovementType)
    {
        case 0: // Linear
            UpdateLinearMovement(currentTime);
            break;
        case 1: // Circular
            UpdateCircularMovement(currentTime);
            break;
        case 2: // Spiral
            UpdateSpiralMovement(currentTime);
            break;
        case 3: // Wave
            UpdateWaveMovement(currentTime);
            break;
        case 4: // Random
            UpdateRandomMovement(currentTime);
            break;
        case 5: // Follow
            UpdateFollowMovement(currentTime);
            break;
        case 6: // Bounce
            UpdateBounceMovement(currentTime);
            break;
        case 7: // Custom
            UpdateCustomMovement(currentTime);
            break;
    }
    
    // Update path animation
    if (EnablePathAnimation)
    {
        UpdatePathAnimation(currentTime, audioFeatures);
    }
}
```

## Movement Type Implementations

### Linear Movement
```csharp
private void UpdateLinearMovement(float currentTime)
{
    float movementDistance = currentTime * MovementSpeed;
    
    for (int i = 0; i < MovementCount; i++)
    {
        var movement = MovementStates[i];
        
        // Calculate linear position
        float offset = i * MovementSpacing;
        movement.CurrentX = (movementDistance + offset) % (output.Width + 100) - 50;
        movement.CurrentY = output.Height / 2.0f + (float)Math.Sin(currentTime * 0.5f) * 20;
        
        // Update movement state
        movement.LastX = movement.CurrentX;
        movement.LastY = movement.CurrentY;
    }
}
```

### Circular Movement
```csharp
private void UpdateCircularMovement(float currentTime)
{
    float centerX = output.Width / 2.0f;
    float centerY = output.Height / 2.0f;
    float radius = 100.0f;
    
    for (int i = 0; i < MovementCount; i++)
    {
        var movement = MovementStates[i];
        
        // Calculate circular position
        float angle = currentTime * MovementSpeed + (i * 2.0f * (float)Math.PI / MovementCount);
        float offset = i * MovementSpacing;
        
        movement.CurrentX = centerX + (float)Math.Cos(angle) * (radius + offset);
        movement.CurrentY = centerY + (float)Math.Sin(angle) * (radius + offset);
        
        // Update movement state
        movement.LastX = movement.CurrentX;
        movement.LastY = movement.CurrentY;
    }
}
```

### Spiral Movement
```csharp
private void UpdateSpiralMovement(float currentTime)
{
    float centerX = output.Width / 2.0f;
    float centerY = output.Height / 2.0f;
    
    for (int i = 0; i < MovementCount; i++)
    {
        var movement = MovementStates[i];
        
        // Calculate spiral position
        float angle = currentTime * MovementSpeed + (i * 0.5f);
        float radius = angle * 2.0f + (i * MovementSpacing);
        
        movement.CurrentX = centerX + (float)Math.Cos(angle) * radius;
        movement.CurrentY = centerY + (float)Math.Sin(angle) * radius;
        
        // Update movement state
        movement.LastX = movement.CurrentX;
        movement.LastY = movement.CurrentY;
    }
}
```

## Path Generation

### Path Calculation
```csharp
private MovementPath CalculateMovementPath(int pathType, float currentTime)
{
    switch (pathType)
    {
        case 0: // Straight
            return CalculateStraightPath(currentTime);
        case 1: // Curved
            return CalculateCurvedPath(currentTime);
        case 2: // Zigzag
            return CalculateZigzagPath(currentTime);
        case 3: // Figure-8
            return CalculateFigure8Path(currentTime);
        case 4: // Lissajous
            return CalculateLissajousPath(currentTime);
        case 5: // Fractal
            return CalculateFractalPath(currentTime);
        case 6: // Noise
            return CalculateNoisePath(currentTime);
        case 7: // Custom
            return CalculateCustomPath(currentTime);
        default:
            return CalculateStraightPath(currentTime);
    }
}
```

### Figure-8 Path
```csharp
private MovementPath CalculateFigure8Path(float currentTime)
{
    var path = new MovementPath();
    
    // Generate Figure-8 curve points
    int numPoints = 100;
    path.Points = new PointF[numPoints];
    
    for (int i = 0; i < numPoints; i++)
    {
        float t = (float)i / (numPoints - 1) * 2.0f * (float)Math.PI;
        
        // Figure-8 parametric equations
        float x = (float)Math.Sin(t) * PathScale * 100.0f;
        float y = (float)Math.Sin(t) * (float)Math.Cos(t) * PathScale * 100.0f;
        
        // Center the path
        x += output.Width / 2.0f;
        y += output.Height / 2.0f;
        
        path.Points[i] = new PointF(x, y);
    }
    
    return path;
}
```

### Lissajous Path
```csharp
private MovementPath CalculateLissajousPath(float currentTime)
{
    var path = new MovementPath();
    
    // Generate Lissajous curve points
    int numPoints = 120;
    path.Points = new PointF[numPoints];
    
    float a = 3.0f; // X frequency
    float b = 2.0f; // Y frequency
    float delta = (float)Math.PI / 2.0f; // Phase difference
    
    for (int i = 0; i < numPoints; i++)
    {
        float t = (float)i / (numPoints - 1) * 2.0f * (float)Math.PI;
        
        // Lissajous parametric equations
        float x = (float)Math.Sin(a * t) * PathScale * 80.0f;
        float y = (float)Math.Sin(b * t + delta) * PathScale * 80.0f;
        
        // Center the path
        x += output.Width / 2.0f;
        y += output.Height / 2.0f;
        
        path.Points[i] = new PointF(x, y);
    }
    
    return path;
}
```

## Movement Rendering

### Main Movement Rendering
```csharp
private void RenderDynamicMovement(ImageBuffer output, AudioFeatures audioFeatures)
{
    // Render movement trails if enabled
    if (EnableTrail)
    {
        RenderMovementTrails(output);
    }
    
    // Render current movement positions
    for (int i = 0; i < MovementCount; i++)
    {
        var movement = MovementStates[i];
        
        // Calculate movement properties
        var properties = CalculateMovementProperties(movement, i);
        
        // Render individual movement element
        RenderMovementElement(output, properties);
    }
}
```

### Movement Trail Rendering
```csharp
private void RenderMovementTrails(ImageBuffer output)
{
    for (int i = 0; i < MovementCount; i++)
    {
        var movement = MovementStates[i];
        
        if (movement.TrailPoints.Count < 2)
            continue;
        
        // Render trail segments
        for (int j = 0; j < movement.TrailPoints.Count - 1; j++)
        {
            var point1 = movement.TrailPoints[j];
            var point2 = movement.TrailPoints[j + 1];
            
            // Calculate trail opacity based on position
            float trailOpacity = TrailOpacity * (1.0f - (float)j / movement.TrailPoints.Count);
            
            // Draw trail line
            DrawLine(output, point1.X, point1.Y, point2.X, point2.Y, 
                    GlowColor, trailOpacity);
        }
    }
}
```

### Movement Element Rendering
```csharp
private void RenderMovementElement(ImageBuffer output, MovementProperties properties)
{
    // Apply movement effects
    switch (properties.Effect)
    {
        case 0: // None
            RenderBasicMovement(output, properties);
            break;
        case 1: // Glow
            RenderGlowingMovement(output, properties);
            break;
        case 2: // Pulse
            RenderPulsingMovement(output, properties);
            break;
        case 3: // Wave
            RenderWaveMovement(output, properties);
            break;
        case 4: // Glitch
            RenderGlitchMovement(output, properties);
            break;
        case 5: // Particle
            RenderParticleMovement(output, properties);
            break;
        case 6: // Custom
            RenderCustomMovement(output, properties);
            break;
    }
}
```

## Beat Reactivity

### Beat-Synchronized Movement
```csharp
private void UpdateBeatSynchronizedMovement(AudioFeatures audioFeatures)
{
    if (!BeatReactive || audioFeatures == null)
        return;

    if (audioFeatures.IsBeat)
    {
        // Beat-triggered movement changes
        MovementSpeed *= 1.5f;
        PathScale *= 1.2f;
        
        // Add random movement variation
        if (RandomizeMovement)
        {
            for (int i = 0; i < MovementCount; i++)
            {
                var movement = MovementStates[i];
                movement.RandomOffsetX = (float)(Random.NextDouble() - 0.5) * 50.0f;
                movement.RandomOffsetY = (float)(Random.NextDouble() - 0.5) * 50.0f;
            }
        }
    }
    else
    {
        // Gradual return to normal
        MovementSpeed = Math.Max(1.0f, MovementSpeed * 0.98f);
        PathScale = Math.Max(1.0f, PathScale * 0.99f);
    }
}
```

## Performance Optimization

### Optimization Techniques
1. **Path Caching**: Cache generated movement paths
2. **State Management**: Efficient movement state storage
3. **Early Exit**: Skip processing for invisible movements
4. **Batch Processing**: Process multiple movements together
5. **Threading**: Multi-threaded movement calculations

### Memory Management
- Efficient movement state storage
- Minimize temporary allocations
- Use value types for calculations
- Optimize movement rendering operations

## Integration with EffectGraph

### Port Configuration
```csharp
protected override void InitializePorts()
{
    _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Background image"));
    _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Dynamic movement output"));
}
```

### Metadata Support
```csharp
public override EffectMetadata GetMetadata()
{
    var metadata = new EffectMetadata();
    metadata.Add("MovementType", MovementType);
    metadata.Add("MovementSpeed", MovementSpeed);
    metadata.Add("MovementPath", MovementPath);
    metadata.Add("BeatReactive", BeatReactive);
    metadata.Add("MovementEffect", MovementEffect);
    metadata.Add("MovementOpacity", MovementOpacity);
    metadata.Add("PathMode", PathMode);
    metadata.Add("PathScale", PathScale);
    metadata.Add("EnableTrail", EnableTrail);
    metadata.Add("TrailLength", TrailLength);
    metadata.Add("MovementCount", MovementCount);
    return metadata;
}
```

## Testing and Validation

### Test Cases
1. **Basic Movement**: Verify movement accuracy
2. **Path Generation**: Test all path types
3. **Movement Types**: Test all movement algorithms
4. **Performance**: Measure movement calculation speed
5. **Edge Cases**: Handle boundary conditions
6. **Beat Reactivity**: Validate beat-synchronized movement

### Validation Methods
- Visual comparison with reference animations
- Performance benchmarking
- Memory usage analysis
- Movement accuracy testing

## Future Enhancements

### Planned Features
1. **Advanced Paths**: More sophisticated path generation
2. **3D Movement**: Three-dimensional movement support
3. **Physics Simulation**: Physics-based movement
4. **Hardware Acceleration**: GPU-accelerated movement
5. **Custom Shaders**: User-defined movement effects

### Compatibility
- Full AVS preset compatibility
- Support for legacy movement modes
- Performance parity with original
- Extended functionality

## Conclusion

The Dynamic Movement effect provides essential animated movement capabilities for dynamic AVS visualizations. This C# implementation maintains full compatibility with the original while adding modern features like enhanced path generation and improved beat reactivity. Complete documentation ensures reliable operation in production environments with optimal performance and visual quality.
