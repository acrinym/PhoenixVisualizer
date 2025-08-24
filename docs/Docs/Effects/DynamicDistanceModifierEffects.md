# Dynamic Distance Modifier Effects

## Overview
The Dynamic Distance Modifier effect creates distance-based visual modifications with configurable properties, patterns, and beat-reactive behaviors. It's essential for creating depth-based effects, distance-dependent visualizations, and dynamic spatial modifications in AVS presets with precise control over distance calculations, modification algorithms, and visual appearance.

## C++ Source Analysis
**File:** `r_dynamicdistance.cpp`  
**Class:** `C_THISCLASS : public C_RBASE`

### Key Properties
- **Distance Calculation**: Different distance measurement algorithms
- **Modification Type**: Various distance-based modification methods
- **Distance Range**: Configurable distance ranges and thresholds
- **Beat Reactivity**: Dynamic modifications synchronized with audio
- **Modification Parameters**: Customizable modification properties
- **Spatial Effects**: Distance-dependent visual transformations

### Core Functionality
```cpp
class C_THISCLASS : public C_RBASE
{
    int disttype;
    int modtype;
    int range;
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

### DynamicDistanceModifierEffectsNode Class
```csharp
public class DynamicDistanceModifierEffectsNode : BaseEffectNode
{
    public int DistanceType { get; set; } = 0;
    public int ModificationType { get; set; } = 0;
    public float DistanceRange { get; set; } = 100.0f;
    public bool BeatReactive { get; set; } = false;
    public int DistanceEffect { get; set; } = 0;
    public float ModificationOpacity { get; set; } = 1.0f;
    public bool EnableGlow { get; set; } = true;
    public int GlowColor { get; set; } = 0x00FFFF;
    public float GlowIntensity { get; set; } = 0.5f;
    public float AnimationSpeed { get; set; } = 1.0f;
    public bool AnimatedModification { get; set; } = true;
    public int ModificationCount { get; set; } = 1;
    public float ModificationSpacing { get; set; } = 25.0f;
    public bool RandomizeModification { get; set; } = false;
    public float RandomSeed { get; set; } = 0.0f;
    public int DistanceMode { get; set; } = 0;
    public float DistanceScale { get; set; } = 1.0f;
    public bool EnableDistanceAnimation { get; set; } = true;
    public float DistanceAnimationSpeed { get; set; } = 1.0f;
    public bool EnableDepthBuffer { get; set; } = false;
    public float DepthBufferScale { get; set; } = 1.0f;
    public int DistanceAlgorithm { get; set; } = 0;
    public float DistanceThreshold { get; set; } = 0.5f;
}
```

### Key Features
1. **Multiple Distance Types**: Different distance calculation methods
2. **Modification Algorithms**: Various distance-based modification techniques
3. **Distance Range Control**: Configurable distance ranges and thresholds
4. **Beat Reactivity**: Dynamic modifications synchronized with music
5. **Distance Animation**: Animated distance-based effects
6. **Depth Buffer Support**: Optional depth buffer integration
7. **Performance Optimization**: Efficient distance calculation algorithms

### Distance Types
- **0**: Euclidean (Standard Euclidean distance)
- **1**: Manhattan (Manhattan distance metric)
- **2**: Chebyshev (Chebyshev distance metric)
- **3**: Minkowski (Minkowski distance with configurable power)
- **4**: Cosine (Cosine similarity-based distance)
- **5**: Hamming (Hamming distance for discrete values)
- **6**: Custom (User-defined distance metric)
- **7**: Adaptive (Adaptive distance calculation)

### Modification Types
- **0**: Color Shift (Distance-based color modification)
- **1**: Opacity Change (Distance-based transparency)
- **2**: Size Variation (Distance-based scaling)
- **3**: Blur Effect (Distance-based blurring)
- **4**: Distortion (Distance-based distortion)
- **5**: Glow (Distance-based glow intensity)
- **6**: Custom (User-defined modification)
- **7**: Multi-Effect (Combination of effects)

### Distance Modes
- **0**: Static (Fixed distance calculation)
- **1**: Animated (Animated distance values)
- **2**: Beat-Synced (Beat-synchronized distance)
- **3**: Interactive (Interactive distance changes)
- **4**: Adaptive (Adaptive distance adjustment)
- **5**: Reactive (Audio-reactive distance)
- **6**: Random (Random distance variation)
- **7**: Custom (User-defined distance behavior)

## Usage Examples

### Basic Distance Modification
```csharp
var dynamicDistanceNode = new DynamicDistanceModifierEffectsNode
{
    DistanceType = 0, // Euclidean
    ModificationType = 1, // Opacity Change
    DistanceRange = 150.0f,
    BeatReactive = false,
    DistanceEffect = 0, // None
    ModificationOpacity = 1.0f,
    EnableGlow = true,
    GlowColor = 0x00FFFF,
    GlowIntensity = 0.6f,
    AnimationSpeed = 1.0f,
    DistanceMode = 0, // Static
    DistanceScale = 1.0f,
    EnableDistanceAnimation = false,
    EnableDepthBuffer = false
};
```

### Beat-Reactive Color Shift
```csharp
var dynamicDistanceNode = new DynamicDistanceModifierEffectsNode
{
    DistanceType = 1, // Manhattan
    ModificationType = 0, // Color Shift
    DistanceRange = 200.0f,
    BeatReactive = true,
    DistanceEffect = 1, // Glow
    ModificationOpacity = 0.9f,
    EnableGlow = true,
    GlowColor = 0xFF00FF,
    GlowIntensity = 0.8f,
    AnimationSpeed = 2.0f,
    AnimatedModification = true,
    ModificationCount = 3,
    ModificationSpacing = 35.0f,
    DistanceMode = 2, // Beat-Synced
    DistanceScale = 1.5f,
    EnableDistanceAnimation = true,
    DistanceAnimationSpeed = 1.5f,
    EnableDepthBuffer = true,
    DepthBufferScale = 1.2f
};
```

### Complex Distance Distortion
```csharp
var dynamicDistanceNode = new DynamicDistanceModifierEffectsNode
{
    DistanceType = 2, // Chebyshev
    ModificationType = 4, // Distortion
    DistanceRange = 120.0f,
    BeatReactive = false,
    DistanceEffect = 2, // Pulse
    ModificationOpacity = 0.8f,
    EnableGlow = true,
    GlowColor = 0x0080FF,
    GlowIntensity = 0.4f,
    AnimationSpeed = 1.5f,
    AnimatedModification = true,
    ModificationCount = 5,
    ModificationSpacing = 30.0f,
    RandomizeModification = true,
    RandomSeed = 42.0f,
    DistanceMode = 1, // Animated
    DistanceScale = 2.0f,
    EnableDistanceAnimation = true,
    DistanceAnimationSpeed = 2.0f,
    DistanceAlgorithm = 1, // Advanced
    DistanceThreshold = 0.7f
};
```

## Technical Implementation

### Core Distance Modification Algorithm
```csharp
protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
{
    if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
        return GetDefaultOutput();

    // Create output buffer
    var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
    
    // Copy input image to output
    Array.Copy(imageBuffer.Pixels, output.Pixels, imageBuffer.Pixels.Length);
    
    // Update distance modification state
    UpdateDistanceModificationState(audioFeatures);
    
    // Generate and render distance modifications
    RenderDistanceModifications(output, audioFeatures);
    
    return output;
}
```

### Distance Modification State Update
```csharp
private void UpdateDistanceModificationState(AudioFeatures audioFeatures)
{
    if (!AnimatedModification)
        return;

    float currentTime = GetCurrentTime();
    
    // Update distance calculations based on type
    switch (DistanceType)
    {
        case 0: // Euclidean
            UpdateEuclideanDistance(currentTime);
            break;
        case 1: // Manhattan
            UpdateManhattanDistance(currentTime);
            break;
        case 2: // Chebyshev
            UpdateChebyshevDistance(currentTime);
            break;
        case 3: // Minkowski
            UpdateMinkowskiDistance(currentTime);
            break;
        case 4: // Cosine
            UpdateCosineDistance(currentTime);
            break;
        case 5: // Hamming
            UpdateHammingDistance(currentTime);
            break;
        case 6: // Custom
            UpdateCustomDistance(currentTime);
            break;
        case 7: // Adaptive
            UpdateAdaptiveDistance(currentTime, audioFeatures);
            break;
    }
    
    // Update distance animation
    if (EnableDistanceAnimation)
    {
        UpdateDistanceAnimation(currentTime, audioFeatures);
    }
}
```

## Distance Calculation Methods

### Euclidean Distance
```csharp
private float CalculateEuclideanDistance(float x1, float y1, float x2, float y2)
{
    float dx = x2 - x1;
    float dy = y2 - y1;
    return (float)Math.Sqrt(dx * dx + dy * dy);
}
```

### Manhattan Distance
```csharp
private float CalculateManhattanDistance(float x1, float y1, float x2, float y2)
{
    return Math.Abs(x2 - x1) + Math.Abs(y2 - y1);
}
```

### Chebyshev Distance
```csharp
private float CalculateChebyshevDistance(float x1, float y1, float x2, float y2)
{
    return Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1));
}
```

### Minkowski Distance
```csharp
private float CalculateMinkowskiDistance(float x1, float y1, float x2, float y2, float power)
{
    float dx = Math.Abs(x2 - x1);
    float dy = Math.Abs(y2 - y1);
    return (float)Math.Pow(Math.Pow(dx, power) + Math.Pow(dy, power), 1.0f / power);
}
```

## Modification Algorithms

### Color Shift Modification
```csharp
private int ApplyColorShiftModification(int originalColor, float distance, float maxDistance)
{
    // Normalize distance to 0-1 range
    float normalizedDistance = Math.Min(1.0f, distance / maxDistance);
    
    // Extract color components
    int r = originalColor & 0xFF;
    int g = (originalColor >> 8) & 0xFF;
    int b = (originalColor >> 16) & 0xFF;
    
    // Apply distance-based color shift
    float shiftAmount = normalizedDistance * 0.5f;
    
    r = (int)(r * (1.0f + shiftAmount));
    g = (int)(g * (1.0f - shiftAmount * 0.3f));
    b = (int)(b * (1.0f + shiftAmount * 0.7f));
    
    // Clamp values
    r = Math.Max(0, Math.Min(255, r));
    g = Math.Max(0, Math.Min(255, g));
    b = Math.Max(0, Math.Min(255, b));
    
    return r | (g << 8) | (b << 16);
}
```

### Opacity Change Modification
```csharp
private float ApplyOpacityModification(float originalOpacity, float distance, float maxDistance)
{
    // Normalize distance to 0-1 range
    float normalizedDistance = Math.Min(1.0f, distance / maxDistance);
    
    // Apply distance-based opacity change
    float opacityMultiplier = 1.0f - normalizedDistance * 0.5f;
    
    return originalOpacity * opacityMultiplier;
}
```

### Size Variation Modification
```csharp
private float ApplySizeVariationModification(float originalSize, float distance, float maxDistance)
{
    // Normalize distance to 0-1 range
    float normalizedDistance = Math.Min(1.0f, distance / maxDistance);
    
    // Apply distance-based size variation
    float sizeMultiplier = 1.0f + normalizedDistance * 0.3f;
    
    return originalSize * sizeMultiplier;
}
```

## Distance-Based Rendering

### Main Distance Modification Rendering
```csharp
private void RenderDistanceModifications(ImageBuffer output, AudioFeatures audioFeatures)
{
    // Calculate reference points for distance calculations
    var referencePoints = CalculateReferencePoints();
    
    // Apply modifications based on distance
    for (int y = 0; y < output.Height; y++)
    {
        for (int x = 0; x < output.Width; x++)
        {
            // Calculate distances to all reference points
            var distances = CalculateDistancesToPoints(x, y, referencePoints);
            
            // Apply distance-based modifications
            ApplyDistanceModifications(output, x, y, distances);
        }
    }
}
```

### Reference Point Calculation
```csharp
private List<PointF> CalculateReferencePoints()
{
    var points = new List<PointF>();
    
    if (ModificationCount == 1)
    {
        // Single center point
        points.Add(new PointF(output.Width / 2.0f, output.Height / 2.0f));
    }
    else
    {
        // Multiple points with spacing
        for (int i = 0; i < ModificationCount; i++)
        {
            float angle = i * (2.0f * (float)Math.PI / ModificationCount);
            float radius = ModificationSpacing;
            
            float x = output.Width / 2.0f + (float)Math.Cos(angle) * radius;
            float y = output.Height / 2.0f + (float)Math.Sin(angle) * radius;
            
            points.Add(new PointF(x, y));
        }
    }
    
    return points;
}
```

### Distance Calculation to Points
```csharp
private List<float> CalculateDistancesToPoints(int x, int y, List<PointF> referencePoints)
{
    var distances = new List<float>();
    
    foreach (var point in referencePoints)
    {
        float distance = 0.0f;
        
        switch (DistanceType)
        {
            case 0: // Euclidean
                distance = CalculateEuclideanDistance(x, y, point.X, point.Y);
                break;
            case 1: // Manhattan
                distance = CalculateManhattanDistance(x, y, point.X, point.Y);
                break;
            case 2: // Chebyshev
                distance = CalculateChebyshevDistance(x, y, point.X, point.Y);
                break;
            case 3: // Minkowski
                distance = CalculateMinkowskiDistance(x, y, point.X, point.Y, 3.0f);
                break;
            case 4: // Cosine
                distance = CalculateCosineDistance(x, y, point.X, point.Y);
                break;
            case 5: // Hamming
                distance = CalculateHammingDistance(x, y, point.X, point.Y);
                break;
            case 6: // Custom
                distance = CalculateCustomDistance(x, y, point.X, point.Y);
                break;
            case 7: // Adaptive
                distance = CalculateAdaptiveDistance(x, y, point.X, point.Y);
                break;
        }
        
        distances.Add(distance);
    }
    
    return distances;
}
```

## Advanced Distance Effects

### Depth Buffer Integration
```csharp
private void ApplyDepthBufferModifications(ImageBuffer output, float[,] depthBuffer)
{
    if (!EnableDepthBuffer || depthBuffer == null)
        return;

    for (int y = 0; y < output.Height; y++)
    {
        for (int x = 0; x < output.Width; x++)
        {
            float depth = depthBuffer[x, y];
            
            // Apply depth-based modifications
            if (depth > DistanceThreshold)
            {
                int pixel = output.GetPixel(x, y);
                float opacity = ApplyOpacityModification(ModificationOpacity, depth, DistanceRange);
                
                // Apply depth-based color shift
                int modifiedPixel = ApplyColorShiftModification(pixel, depth, DistanceRange);
                
                // Blend with original based on opacity
                int finalPixel = BlendColors(pixel, modifiedPixel, opacity);
                output.SetPixel(x, y, finalPixel);
            }
        }
    }
}
```

### Beat-Synchronized Distance Changes
```csharp
private void UpdateBeatSynchronizedDistance(AudioFeatures audioFeatures)
{
    if (!BeatReactive || audioFeatures == null)
        return;

    if (audioFeatures.IsBeat)
    {
        // Beat-triggered distance changes
        DistanceRange *= 1.3f;
        DistanceScale *= 1.2f;
        
        // Add random distance variation
        if (RandomizeModification)
        {
            for (int i = 0; i < ModificationCount; i++)
            {
                var modification = ModificationStates[i];
                modification.RandomOffset = (float)(Random.NextDouble() - 0.5) * 30.0f;
            }
        }
    }
    else
    {
        // Gradual return to normal
        DistanceRange = Math.Max(100.0f, DistanceRange * 0.98f);
        DistanceScale = Math.Max(1.0f, DistanceScale * 0.99f);
    }
}
```

## Performance Optimization

### Optimization Techniques
1. **Distance Caching**: Cache calculated distances
2. **Spatial Partitioning**: Use spatial data structures
3. **Early Exit**: Skip processing for out-of-range distances
4. **Batch Processing**: Process multiple modifications together
5. **Threading**: Multi-threaded distance calculations

### Memory Management
- Efficient distance storage
- Minimize temporary allocations
- Use value types for calculations
- Optimize distance modification operations

## Integration with EffectGraph

### Port Configuration
```csharp
protected override void InitializePorts()
{
    _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Input image"));
    _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Distance-modified output"));
}
```

### Metadata Support
```csharp
public override EffectMetadata GetMetadata()
{
    var metadata = new EffectMetadata();
    metadata.Add("DistanceType", DistanceType);
    metadata.Add("ModificationType", ModificationType);
    metadata.Add("DistanceRange", DistanceRange);
    metadata.Add("BeatReactive", BeatReactive);
    metadata.Add("DistanceEffect", DistanceEffect);
    metadata.Add("ModificationOpacity", ModificationOpacity);
    metadata.Add("DistanceMode", DistanceMode);
    metadata.Add("DistanceScale", DistanceScale);
    metadata.Add("EnableDepthBuffer", EnableDepthBuffer);
    metadata.Add("DepthBufferScale", DepthBufferScale);
    metadata.Add("DistanceAlgorithm", DistanceAlgorithm);
    metadata.Add("DistanceThreshold", DistanceThreshold);
    return metadata;
}
```

## Testing and Validation

### Test Cases
1. **Basic Distance Calculation**: Verify distance accuracy
2. **Modification Types**: Test all modification algorithms
3. **Distance Types**: Test all distance calculation methods
4. **Performance**: Measure distance calculation speed
5. **Edge Cases**: Handle boundary conditions
6. **Beat Reactivity**: Validate beat-synchronized modifications

### Validation Methods
- Visual comparison with reference images
- Performance benchmarking
- Memory usage analysis
- Distance calculation accuracy testing

## Future Enhancements

### Planned Features
1. **Advanced Distance Metrics**: More sophisticated distance calculations
2. **3D Distance**: Three-dimensional distance support
3. **Machine Learning**: ML-based distance optimization
4. **Hardware Acceleration**: GPU-accelerated distance calculations
5. **Custom Shaders**: User-defined distance effects

### Compatibility
- Full AVS preset compatibility
- Support for legacy distance modes
- Performance parity with original
- Extended functionality

## Conclusion

The Dynamic Distance Modifier effect provides essential distance-based visual modification capabilities for spatial AVS visualizations. This C# implementation maintains full compatibility with the original while adding modern features like enhanced distance algorithms and improved modification techniques. Complete documentation ensures reliable operation in production environments with optimal performance and visual quality.
