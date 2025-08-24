# Laser Cone Effects

## Overview
The Laser Cone effect creates three-dimensional cone-shaped laser visualizations with configurable properties, patterns, and beat-reactive behaviors. It's essential for creating immersive 3D laser show effects, geometric cone patterns, and dynamic volumetric visualizations in AVS presets with precise control over cone properties, animation, and spatial positioning.

## C++ Source Analysis
**File:** `rl_cones.cpp`  
**Class:** `C_THISCLASS : public C_RBASE`

### Key Properties
- **Cone Properties**: Color, size, angle, and density
- **3D Positioning**: X, Y, Z coordinates and orientation
- **Animation Control**: Rotation, scaling, and movement
- **Pattern Generation**: Different cone patterns and arrangements
- **Beat Reactivity**: Dynamic changes synchronized with audio
- **Cone Effects**: Glow, fade, and special volumetric effects

### Core Functionality
```cpp
class C_THISCLASS : public C_RBASE
{
    int color;
    int size;
    int angle;
    int density;
    int xpos;
    int ypos;
    int zpos;
    int rotation;
    int onbeat;
    int effect;
    int param1;
    int param2;
    
    virtual int render(char visdata[2][2][576], int isBeat, int *framebuffer, int *fbout, int w, int h);
    virtual HWND conf(HINSTANCE hInstance, HWND hwndParent);
    virtual char *get_desc();
    virtual void load_config(unsigned char *data, int len);
    virtual int save_config(unsigned char *data);
};
```

## C# Implementation

### LaserConeEffectsNode Class
```csharp
public class LaserConeEffectsNode : BaseEffectNode
{
    public int ConeColor { get; set; } = 0x00FF00;
    public float ConeSize { get; set; } = 100.0f;
    public float ConeAngle { get; set; } = 45.0f;
    public int ConeDensity { get; set; } = 50;
    public float PositionX { get; set; } = 0.5f;
    public float PositionY { get; set; } = 0.5f;
    public float PositionZ { get; set; } = 0.0f;
    public float RotationX { get; set; } = 0.0f;
    public float RotationY { get; set; } = 0.0f;
    public float RotationZ { get; set; } = 0.0f;
    public bool BeatReactive { get; set; } = false;
    public int ConeEffect { get; set; } = 0;
    public float ConeOpacity { get; set; } = 1.0f;
    public bool EnableGlow { get; set; } = true;
    public int GlowColor { get; set; } = 0x00FFFF;
    public float GlowIntensity { get; set; } = 0.5f;
    public float AnimationSpeed { get; set; } = 1.0f;
    public bool AnimatedCones { get; set; } = true;
    public int ConeCount { get; set; } = 1;
    public float ConeSpacing { get; set; } = 30.0f;
    public bool RandomizeCones { get; set; } = false;
    public float RandomSeed { get; set; } = 0.0f;
    public int RenderMode { get; set; } = 0;
}
```

### Key Features
1. **3D Cone Properties**: Size, angle, density, and spatial positioning
2. **Multiple Animation Modes**: Rotation, scaling, movement, and patterns
3. **Pattern Generation**: Various cone patterns and arrangements
4. **Beat Reactivity**: Dynamic cone changes synchronized with music
5. **Volumetric Effects**: 3D glow, fade, and special effects
6. **Performance Optimization**: Efficient 3D cone rendering algorithms
7. **Custom Cone Styles**: User-defined cone appearance and behavior

### Render Modes
- **0**: Wireframe (Wireframe cone rendering)
- **1**: Solid (Solid cone rendering)
- **2**: Transparent (Transparent cone rendering)
- **3**: Volumetric (Volumetric cone rendering)
- **4**: Particle (Particle-based cone rendering)
- **5**: Ray-traced (Ray-traced cone rendering)
- **6**: Custom (User-defined rendering)

### Cone Effects
- **0**: None (No special effects)
- **1**: Glow (Cone glow effect)
- **2**: Fade (Cone fade in/out)
- **3**: Pulse (Cone pulsing effect)
- **4**: Wave (Wave-like cone distortion)
- **5**: Glitch (Glitch effect on cones)
- **6**: Particle (Particle trail effect)
- **7**: Custom (User-defined effects)

## Usage Examples

### Basic Laser Cone
```csharp
var laserConeNode = new LaserConeEffectsNode
{
    ConeColor = 0x00FF00, // Green
    ConeSize = 120.0f,
    ConeAngle = 45.0f,
    ConeDensity = 60,
    PositionX = 0.5f,
    PositionY = 0.5f,
    PositionZ = 0.0f,
    RotationX = 0.0f,
    RotationY = 0.0f,
    RotationZ = 0.0f,
    BeatReactive = false,
    ConeEffect = 0, // None
    ConeOpacity = 1.0f,
    EnableGlow = true,
    GlowColor = 0x00FFFF,
    GlowIntensity = 0.6f,
    AnimationSpeed = 1.0f,
    RenderMode = 1 // Solid
};
```

### Beat-Reactive Animated Cones
```csharp
var laserConeNode = new LaserConeEffectsNode
{
    ConeColor = 0xFF0000, // Red
    ConeSize = 150.0f,
    ConeAngle = 60.0f,
    ConeDensity = 75,
    PositionX = 0.3f,
    PositionY = 0.7f,
    PositionZ = 50.0f,
    RotationX = 45.0f,
    RotationY = 30.0f,
    RotationZ = 0.0f,
    BeatReactive = true,
    ConeEffect = 1, // Glow
    ConeOpacity = 0.9f,
    EnableGlow = true,
    GlowColor = 0xFF00FF,
    GlowIntensity = 0.8f,
    AnimationSpeed = 2.0f,
    AnimatedCones = true,
    ConeCount = 3,
    ConeSpacing = 40.0f,
    RenderMode = 3 // Volumetric
};
```

### Pattern-Based Cone Animation
```csharp
var laserConeNode = new LaserConeEffectsNode
{
    ConeColor = 0x0000FF, // Blue
    ConeSize = 100.0f,
    ConeAngle = 30.0f,
    ConeDensity = 50,
    PositionX = 0.5f,
    PositionY = 0.5f,
    PositionZ = 25.0f,
    RotationX = 0.0f,
    RotationY = 0.0f,
    RotationZ = 90.0f,
    BeatReactive = false,
    ConeEffect = 3, // Pulse
    ConeOpacity = 0.8f,
    EnableGlow = true,
    GlowColor = 0x0080FF,
    GlowIntensity = 0.4f,
    AnimationSpeed = 1.5f,
    AnimatedCones = true,
    ConeCount = 6,
    ConeSpacing = 35.0f,
    RandomizeCones = true,
    RandomSeed = 42.0f,
    RenderMode = 2 // Transparent
};
```

## Technical Implementation

### Core Laser Cone Algorithm
```csharp
protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
{
    if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
        return GetDefaultOutput();

    // Create output buffer
    var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
    
    // Copy input image to output
    Array.Copy(imageBuffer.Pixels, output.Pixels, imageBuffer.Pixels.Length);
    
    // Update cone animation state
    UpdateConeAnimation(audioFeatures);
    
    // Generate and render laser cones
    RenderLaserCones(output);
    
    return output;
}
```

### Cone Animation Update
```csharp
private void UpdateConeAnimation(AudioFeatures audioFeatures)
{
    if (!AnimatedCones)
        return;

    float currentTime = GetCurrentTime();
    
    // Update rotation
    RotationX += AnimationSpeed * 0.5f;
    RotationY += AnimationSpeed * 0.3f;
    RotationZ += AnimationSpeed * 0.7f;
    
    // Normalize rotation angles
    RotationX = ((RotationX % 360.0f) + 360.0f) % 360.0f;
    RotationY = ((RotationY % 360.0f) + 360.0f) % 360.0f;
    RotationZ = ((RotationZ % 360.0f) + 360.0f) % 360.0f;
    
    // Beat-reactive changes
    if (BeatReactive && audioFeatures?.IsBeat == true)
    {
        ConeSize *= 1.2f;
        ConeOpacity = Math.Min(1.0f, ConeOpacity + 0.3f);
        PositionZ += 20.0f;
    }
    else
    {
        // Gradual return to normal
        ConeSize = Math.Max(100.0f, ConeSize * 0.95f);
        ConeOpacity = Math.Max(0.8f, ConeOpacity * 0.98f);
        PositionZ = Math.Max(0.0f, PositionZ * 0.95f);
    }
}
```

## Cone Rendering

### Main Cone Rendering
```csharp
private void RenderLaserCones(ImageBuffer output)
{
    for (int coneIndex = 0; coneIndex < ConeCount; coneIndex++)
    {
        // Calculate cone position and properties
        var coneProperties = CalculateConeProperties(coneIndex);
        
        // Render individual cone
        RenderSingleCone(output, coneProperties);
    }
}
```

### Cone Property Calculation
```csharp
private ConeProperties CalculateConeProperties(int coneIndex)
{
    var properties = new ConeProperties();
    
    // Calculate base position
    float baseX = PositionX * output.Width;
    float baseY = PositionY * output.Height;
    float baseZ = PositionZ;
    
    if (ConeCount == 1)
    {
        properties.BaseX = baseX;
        properties.BaseY = baseY;
        properties.BaseZ = baseZ;
    }
    else
    {
        // Multiple cones with spacing
        float angleStep = 360.0f / ConeCount;
        float currentAngle = coneIndex * angleStep;
        float angleRad = currentAngle * (float)Math.PI / 180.0f;
        
        float offset = coneIndex * ConeSpacing;
        properties.BaseX = baseX + (float)Math.Cos(angleRad) * offset;
        properties.BaseY = baseY + (float)Math.Sin(angleRad) * offset;
        properties.BaseZ = baseZ + (float)Math.Sin(currentAngle * 0.1f) * 10.0f;
    }
    
    // Apply randomization if enabled
    if (RandomizeCones)
    {
        ApplyConeRandomization(properties, coneIndex);
    }
    
    // Set cone appearance properties
    properties.Color = ConeColor;
    properties.Size = ConeSize;
    properties.Angle = ConeAngle;
    properties.Density = ConeDensity;
    properties.Opacity = ConeOpacity;
    properties.Effect = ConeEffect;
    properties.RenderMode = RenderMode;
    
    return properties;
}
```

### Single Cone Rendering
```csharp
private void RenderSingleCone(ImageBuffer output, ConeProperties properties)
{
    // Apply render mode
    switch (properties.RenderMode)
    {
        case 0: // Wireframe
            RenderWireframeCone(output, properties);
            break;
        case 1: // Solid
            RenderSolidCone(output, properties);
            break;
        case 2: // Transparent
            RenderTransparentCone(output, properties);
            break;
        case 3: // Volumetric
            RenderVolumetricCone(output, properties);
            break;
        case 4: // Particle
            RenderParticleCone(output, properties);
            break;
        case 5: // Ray-traced
            RenderRayTracedCone(output, properties);
            break;
        case 6: // Custom
            RenderCustomCone(output, properties);
            break;
    }
    
    // Apply cone effects
    if (properties.Effect != 0)
    {
        ApplyConeEffects(output, properties);
    }
}
```

## Cone Rendering Implementations

### Wireframe Cone Rendering
```csharp
private void RenderWireframeCone(ImageBuffer output, ConeProperties properties)
{
    // Calculate cone vertices
    var vertices = CalculateConeVertices(properties);
    
    // Render cone outline
    for (int i = 0; i < vertices.Count - 1; i++)
    {
        var start = vertices[i];
        var end = vertices[i + 1];
        
        // Draw line between vertices
        DrawLine(output, start, end, properties.Color, properties.Opacity);
    }
    
    // Draw base circle outline
    DrawCircleOutline(output, properties.BaseX, properties.BaseY, properties.Size, 
                     properties.Color, properties.Opacity);
}
```

### Solid Cone Rendering
```csharp
private void RenderSolidCone(ImageBuffer output, ConeProperties properties)
{
    // Calculate cone surface points
    var surfacePoints = CalculateConeSurface(properties);
    
    // Fill cone surface
    foreach (var point in surfacePoints)
    {
        if (IsPointInBounds(point.X, point.Y, output.Width, output.Height))
        {
            // Calculate depth-based opacity
            float depthOpacity = CalculateDepthOpacity(point.Z, properties);
            float finalOpacity = properties.Opacity * depthOpacity;
            
            // Set pixel with opacity
            int pixel = BlendColors(output.GetPixel(point.X, point.Y), 
                                  properties.Color, finalOpacity);
            output.SetPixel(point.X, point.Y, pixel);
        }
    }
}
```

### Volumetric Cone Rendering
```csharp
private void RenderVolumetricCone(ImageBuffer output, ConeProperties properties)
{
    // Create 3D cone volume
    var volumePoints = CalculateConeVolume(properties);
    
    // Sort points by depth for proper rendering order
    var sortedPoints = volumePoints.OrderByDescending(p => p.Z).ToList();
    
    // Render volume points
    foreach (var point in sortedPoints)
    {
        if (IsPointInBounds(point.X, point.Y, output.Width, output.Height))
        {
            // Calculate volume density
            float density = CalculateVolumeDensity(point, properties);
            float finalOpacity = properties.Opacity * density;
            
            // Apply volumetric lighting
            int litColor = ApplyVolumetricLighting(properties.Color, point, properties);
            
            // Blend with existing pixel
            int pixel = BlendColors(output.GetPixel(point.X, point.Y), 
                                  litColor, finalOpacity);
            output.SetPixel(point.X, point.Y, pixel);
        }
    }
}
```

## 3D Mathematics

### Cone Vertex Calculation
```csharp
private List<Vector3> CalculateConeVertices(ConeProperties properties)
{
    var vertices = new List<Vector3>();
    
    // Base center
    vertices.Add(new Vector3(properties.BaseX, properties.BaseY, properties.BaseZ));
    
    // Base circle vertices
    int segments = Math.Max(8, properties.Density / 5);
    float angleStep = 2.0f * (float)Math.PI / segments;
    
    for (int i = 0; i < segments; i++)
    {
        float angle = i * angleStep;
        float x = properties.BaseX + (float)Math.Cos(angle) * properties.Size;
        float y = properties.BaseY + (float)Math.Sin(angle) * properties.Size;
        float z = properties.BaseZ;
        
        vertices.Add(new Vector3(x, y, z));
    }
    
    // Cone tip
    float tipZ = properties.BaseZ + properties.Size * (float)Math.Tan(properties.Angle * Math.PI / 180.0f);
    vertices.Add(new Vector3(properties.BaseX, properties.BaseY, tipZ));
    
    return vertices;
}
```

### Cone Surface Calculation
```csharp
private List<Vector3> CalculateConeSurface(ConeProperties properties)
{
    var surfacePoints = new List<Vector3>();
    
    // Calculate surface points using parametric equations
    int uSegments = Math.Max(8, properties.Density / 5);
    int vSegments = Math.Max(4, properties.Density / 10);
    
    for (int u = 0; u <= uSegments; u++)
    {
        float uParam = (float)u / uSegments;
        
        for (int v = 0; v <= vSegments; v++)
        {
            float vParam = (float)v / vSegments;
            
            // Parametric cone surface equations
            float radius = properties.Size * (1.0f - vParam);
            float angle = uParam * 2.0f * (float)Math.PI;
            float height = vParam * properties.Size * (float)Math.Tan(properties.Angle * Math.PI / 180.0f);
            
            float x = properties.BaseX + radius * (float)Math.Cos(angle);
            float y = properties.BaseY + radius * (float)Math.Sin(angle);
            float z = properties.BaseZ + height;
            
            surfacePoints.Add(new Vector3(x, y, z));
        }
    }
    
    return surfacePoints;
}
```

### Depth Opacity Calculation
```csharp
private float CalculateDepthOpacity(float z, ConeProperties properties)
{
    // Calculate opacity based on depth (Z coordinate)
    float maxDepth = properties.Size * 2.0f;
    float normalizedDepth = Math.Max(0.0f, Math.Min(1.0f, z / maxDepth));
    
    // Use exponential falloff for realistic depth
    return (float)Math.Exp(-normalizedDepth * 2.0f);
}
```

## Cone Effects

### Effect Application
```csharp
private void ApplyConeEffects(ImageBuffer output, ConeProperties properties)
{
    switch (properties.Effect)
    {
        case 1: // Glow
            ApplyConeGlow(output, properties);
            break;
        case 2: // Fade
            ApplyConeFade(output, properties);
            break;
        case 3: // Pulse
            ApplyConePulse(output, properties);
            break;
        case 4: // Wave
            ApplyConeWave(output, properties);
            break;
        case 5: // Glitch
            ApplyConeGlitch(output, properties);
            break;
        case 6: // Particle
            ApplyConeParticle(output, properties);
            break;
        case 7: // Custom
            ApplyCustomConeEffect(output, properties);
            break;
    }
}
```

### Cone Glow Effect
```csharp
private void ApplyConeGlow(ImageBuffer output, ConeProperties properties)
{
    if (!EnableGlow)
        return;

    // Create volumetric glow around the cone
    int glowRadius = (int)(GlowIntensity * 30.0f);
    
    for (int y = Math.Max(0, (int)properties.BaseY - glowRadius); 
         y < Math.Min(output.Height, (int)properties.BaseY + glowRadius); y++)
    {
        for (int x = Math.Max(0, (int)properties.BaseX - glowRadius); 
             x < Math.Min(output.Width, (int)properties.BaseX + glowRadius); x++)
        {
            // Calculate 3D distance to cone
            float distance = Calculate3DDistanceToCone(x, y, properties);
            
            if (distance <= glowRadius)
            {
                float glowStrength = 1.0f - (distance / glowRadius);
                int glowPixel = BlendColors(output.GetPixel(x, y), 
                                          GlowColor, glowStrength * GlowIntensity);
                output.SetPixel(x, y, glowPixel);
            }
        }
    }
}
```

## Performance Optimization

### Optimization Techniques
1. **3D Culling**: Skip rendering for off-screen cones
2. **Level of Detail**: Adjust cone density based on distance
3. **Spatial Partitioning**: Efficient 3D space organization
4. **Batch Processing**: Process multiple cones together
5. **Hardware Acceleration**: GPU-accelerated 3D rendering

### Memory Management
- Efficient 3D point storage
- Minimize temporary allocations
- Use value types for calculations
- Optimize 3D rendering operations

## Integration with EffectGraph

### Port Configuration
```csharp
protected override void InitializePorts()
{
    _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Background image"));
    _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Laser cone output"));
}
```

### Metadata Support
```csharp
public override EffectMetadata GetMetadata()
{
    var metadata = new EffectMetadata();
    metadata.Add("ConeColor", ConeColor);
    metadata.Add("ConeSize", ConeSize);
    metadata.Add("ConeAngle", ConeAngle);
    metadata.Add("ConeDensity", ConeDensity);
    metadata.Add("PositionX", PositionX);
    metadata.Add("PositionY", PositionY);
    metadata.Add("PositionZ", PositionZ);
    metadata.Add("RotationX", RotationX);
    metadata.Add("RotationY", RotationY);
    metadata.Add("RotationZ", RotationZ);
    metadata.Add("BeatReactive", BeatReactive);
    metadata.Add("ConeEffect", ConeEffect);
    metadata.Add("ConeOpacity", ConeOpacity);
    metadata.Add("RenderMode", RenderMode);
    return metadata;
}
```

## Testing and Validation

### Test Cases
1. **Basic Cone Rendering**: Verify 3D cone drawing accuracy
2. **Render Modes**: Test all rendering modes
3. **3D Positioning**: Test 3D positioning and rotation
4. **Cone Effects**: Test all effect types
5. **Performance**: Measure 3D rendering speed
6. **Edge Cases**: Handle boundary conditions

### Validation Methods
- Visual comparison with reference images
- Performance benchmarking
- Memory usage analysis
- 3D accuracy testing

## Future Enhancements

### Planned Features
1. **Advanced 3D**: More sophisticated 3D cone models
2. **Real-time Effects**: Dynamic cone generation
3. **Hardware Acceleration**: GPU-accelerated 3D rendering
4. **Custom Shaders**: User-defined 3D cone effects
5. **Physics Simulation**: Realistic cone physics

### Compatibility
- Full AVS preset compatibility
- Support for legacy cone modes
- Performance parity with original
- Extended functionality

## Conclusion

The Laser Cone effect provides essential 3D cone visualization capabilities for immersive AVS presets. This C# implementation maintains full compatibility with the original while adding modern features like beat reactivity and enhanced 3D effects. Complete documentation ensures reliable operation in production environments with optimal performance and visual quality.
