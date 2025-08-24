# Laser Beat Hold Effects

## Overview
The Laser Beat Hold effect creates laser visualizations that hold and sustain their state based on beat detection, creating rhythmic laser patterns that persist and evolve over time. It's essential for creating beat-synchronized laser shows, rhythmic visual patterns, and dynamic laser sequences in AVS presets with precise control over beat detection, hold duration, and laser evolution.

## C++ Source Analysis
**File:** `rl_beathold.cpp`  
**Class:** `C_THISCLASS : public C_RBASE`

### Key Properties
- **Beat Detection**: Audio beat detection and analysis
- **Hold Duration**: How long laser states are maintained
- **Laser Evolution**: How lasers change over time
- **Pattern Generation**: Different beat-synchronized patterns
- **Beat Reactivity**: Dynamic laser changes on beat detection
- **Hold Effects**: Special effects for held laser states

### Core Functionality
```cpp
class C_THISCLASS : public C_RBASE
{
    int sensitivity;
    int holdtime;
    int evolution;
    int pattern;
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

### LaserBeatHoldEffectsNode Class
```csharp
public class LaserBeatHoldEffectsNode : BaseEffectNode
{
    public float BeatSensitivity { get; set; } = 0.5f;
    public float HoldDuration { get; set; } = 1.0f;
    public int EvolutionMode { get; set; } = 0;
    public int PatternType { get; set; } = 0;
    public bool BeatReactive { get; set; } = true;
    public int HoldEffect { get; set; } = 0;
    public int LaserColor { get; set; } = 0x00FF00;
    public float LaserIntensity { get; set; } = 1.0f;
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
    public int BeatPattern { get; set; } = 0;
    public float BeatThreshold { get; set; } = 0.3f;
    public bool EnableBeatHistory { get; set; } = true;
    public int MaxBeatHistory { get; set; } = 10;
}
```

### Key Features
1. **Advanced Beat Detection**: Configurable beat sensitivity and threshold
2. **Beat Hold Duration**: Control over how long laser states persist
3. **Laser Evolution**: Multiple evolution modes for held lasers
4. **Pattern Generation**: Various beat-synchronized laser patterns
5. **Beat Reactivity**: Dynamic laser changes synchronized with music
6. **Hold Effects**: Special effects for sustained laser states
7. **Beat History**: Track and analyze beat patterns over time

### Evolution Modes
- **0**: Static (Lasers remain unchanged while held)
- **1**: Fade (Lasers gradually fade out over time)
- **2**: Pulse (Lasers pulse while held)
- **3**: Morph (Lasers change shape over time)
- **4**: Color Shift (Lasers change color over time)
- **5**: Size Change (Lasers change size over time)
- **6**: Rotation (Lasers rotate while held)
- **7**: Custom (User-defined evolution)

### Pattern Types
- **0**: Single (Single laser on each beat)
- **1**: Multiple (Multiple lasers on each beat)
- **2**: Alternating (Alternating laser patterns)
- **3**: Spiral (Spiral laser arrangement)
- **4**: Grid (Grid-based laser pattern)
- **5**: Random (Random laser placement)
- **6**: Wave (Wave-like laser pattern)
- **7**: Custom (User-defined pattern)

### Beat Patterns
- **0**: Every Beat (Trigger on every detected beat)
- **1**: Strong Beats (Trigger only on strong beats)
- **2**: Beat Groups (Trigger on beat group boundaries)
- **3**: Custom Rhythm (User-defined beat pattern)
- **4**: Frequency Based (Trigger based on frequency analysis)
- **5**: Energy Based (Trigger based on audio energy)
- **6**: Pattern Match (Trigger on specific beat patterns)
- **7**: Adaptive (Adaptive beat detection)

## Usage Examples

### Basic Beat Hold
```csharp
var laserBeatHoldNode = new LaserBeatHoldEffectsNode
{
    BeatSensitivity = 0.6f,
    HoldDuration = 1.5f,
    EvolutionMode = 0, // Static
    PatternType = 0, // Single
    BeatReactive = true,
    HoldEffect = 0, // None
    LaserColor = 0x00FF00, // Green
    LaserIntensity = 1.0f,
    LaserOpacity = 1.0f,
    EnableGlow = true,
    GlowColor = 0x00FFFF,
    GlowIntensity = 0.6f,
    AnimationSpeed = 1.0f,
    BeatPattern = 0, // Every Beat
    BeatThreshold = 0.4f,
    EnableBeatHistory = true,
    MaxBeatHistory = 8
};
```

### Evolving Beat Hold
```csharp
var laserBeatHoldNode = new LaserBeatHoldEffectsNode
{
    BeatSensitivity = 0.8f,
    HoldDuration = 2.0f,
    EvolutionMode = 3, // Morph
    PatternType = 2, // Alternating
    BeatReactive = true,
    HoldEffect = 1, // Glow
    LaserColor = 0xFF0000, // Red
    LaserIntensity = 1.2f,
    LaserOpacity = 0.9f,
    EnableGlow = true,
    GlowColor = 0xFF00FF,
    GlowIntensity = 0.8f,
    AnimationSpeed = 1.5f,
    AnimatedLasers = true,
    LaserCount = 4,
    LaserSpacing = 35.0f,
    BeatPattern = 1, // Strong Beats
    BeatThreshold = 0.5f,
    EnableBeatHistory = true,
    MaxBeatHistory = 12
};
```

### Complex Beat Pattern
```csharp
var laserBeatHoldNode = new LaserBeatHoldEffectsNode
{
    BeatSensitivity = 0.7f,
    HoldDuration = 1.8f,
    EvolutionMode = 4, // Color Shift
    PatternType = 3, // Spiral
    BeatReactive = true,
    HoldEffect = 2, // Pulse
    LaserColor = 0x0000FF, // Blue
    LaserIntensity = 0.9f,
    LaserOpacity = 0.8f,
    EnableGlow = true,
    GlowColor = 0x0080FF,
    GlowIntensity = 0.4f,
    AnimationSpeed = 2.0f,
    AnimatedLasers = true,
    LaserCount = 6,
    LaserSpacing = 30.0f,
    RandomizeLasers = true,
    RandomSeed = 42.0f,
    BeatPattern = 4, // Frequency Based
    BeatThreshold = 0.6f,
    EnableBeatHistory = true,
    MaxBeatHistory = 15
};
```

## Technical Implementation

### Core Beat Hold Algorithm
```csharp
protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
{
    if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
        return GetDefaultOutput();

    // Create output buffer
    var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
    
    // Copy input image to output
    Array.Copy(imageBuffer.Pixels, output.Pixels, imageBuffer.Pixels.Length);
    
    // Update beat detection and analysis
    UpdateBeatDetection(audioFeatures);
    
    // Update held laser states
    UpdateHeldLasers();
    
    // Generate and render beat-hold lasers
    RenderBeatHoldLasers(output);
    
    return output;
}
```

### Beat Detection Update
```csharp
private void UpdateBeatDetection(AudioFeatures audioFeatures)
{
    if (!BeatReactive || audioFeatures == null)
        return;

    // Analyze audio features for beat detection
    bool isBeat = DetectBeat(audioFeatures);
    
    if (isBeat)
    {
        // Record beat event
        RecordBeatEvent(audioFeatures);
        
        // Create new laser hold state
        CreateLaserHoldState();
    }
    
    // Update beat history
    UpdateBeatHistory();
}
```

### Beat Detection Algorithm
```csharp
private bool DetectBeat(AudioFeatures audioFeatures)
{
    // Multiple beat detection methods
    switch (BeatPattern)
    {
        case 0: // Every Beat
            return audioFeatures.IsBeat;
            
        case 1: // Strong Beats
            return audioFeatures.IsBeat && audioFeatures.BeatStrength > BeatThreshold;
            
        case 2: // Beat Groups
            return DetectBeatGroupBoundary(audioFeatures);
            
        case 3: // Custom Rhythm
            return DetectCustomRhythm(audioFeatures);
            
        case 4: // Frequency Based
            return DetectFrequencyBasedBeat(audioFeatures);
            
        case 5: // Energy Based
            return DetectEnergyBasedBeat(audioFeatures);
            
        case 6: // Pattern Match
            return DetectPatternMatch(audioFeatures);
            
        case 7: // Adaptive
            return DetectAdaptiveBeat(audioFeatures);
            
        default:
            return audioFeatures.IsBeat;
    }
}
```

### Frequency-Based Beat Detection
```csharp
private bool DetectFrequencyBasedBeat(AudioFeatures audioFeatures)
{
    // Analyze frequency spectrum for beat patterns
    float[] spectrum = audioFeatures.Spectrum;
    
    // Focus on bass frequencies (typically 60-250 Hz)
    int bassStart = 0;
    int bassEnd = Math.Min(spectrum.Length / 4, spectrum.Length);
    
    float bassEnergy = 0.0f;
    for (int i = bassStart; i < bassEnd; i++)
    {
        bassEnergy += spectrum[i];
    }
    bassEnergy /= (bassEnd - bassStart);
    
    // Compare with threshold
    return bassEnergy > BeatThreshold;
}
```

### Energy-Based Beat Detection
```csharp
private bool DetectEnergyBasedBeat(AudioFeatures audioFeatures)
{
    // Analyze overall audio energy
    float currentEnergy = audioFeatures.Volume;
    
    // Compare with historical energy levels
    if (BeatHistory.Count > 0)
    {
        float averageEnergy = BeatHistory.Average(b => b.Energy);
        float energyThreshold = averageEnergy * BeatThreshold;
        
        return currentEnergy > energyThreshold;
    }
    
    return currentEnergy > BeatThreshold;
}
```

## Laser Hold State Management

### Hold State Creation
```csharp
private void CreateLaserHoldState()
{
    var holdState = new LaserHoldState
    {
        CreationTime = GetCurrentTime(),
        Duration = HoldDuration,
        EvolutionMode = EvolutionMode,
        PatternType = PatternType,
        LaserCount = LaserCount,
        LaserSpacing = LaserSpacing,
        RandomizeLasers = RandomizeLasers,
        RandomSeed = RandomSeed
    };
    
    // Calculate laser positions based on pattern
    holdState.LaserPositions = CalculateLaserPositions(holdState);
    
    // Add to active hold states
    ActiveHoldStates.Add(holdState);
}
```

### Hold State Update
```csharp
private void UpdateHeldLasers()
{
    float currentTime = GetCurrentTime();
    
    // Update and evolve existing hold states
    for (int i = ActiveHoldStates.Count - 1; i >= 0; i--)
    {
        var holdState = ActiveHoldStates[i];
        
        // Check if hold state has expired
        if (currentTime - holdState.CreationTime > holdState.Duration)
        {
            ActiveHoldStates.RemoveAt(i);
            continue;
        }
        
        // Update laser evolution
        UpdateLaserEvolution(holdState, currentTime);
    }
}
```

### Laser Evolution
```csharp
private void UpdateLaserEvolution(LaserHoldState holdState, float currentTime)
{
    float holdProgress = (currentTime - holdState.CreationTime) / holdState.Duration;
    
    switch (holdState.EvolutionMode)
    {
        case 0: // Static
            // No evolution needed
            break;
            
        case 1: // Fade
            holdState.CurrentOpacity = 1.0f - holdProgress;
            break;
            
        case 2: // Pulse
            holdState.PulsePhase = (currentTime * AnimationSpeed) % (2.0f * (float)Math.PI);
            break;
            
        case 3: // Morph
            holdState.MorphProgress = holdProgress;
            break;
            
        case 4: // Color Shift
            holdState.ColorShift = holdProgress * 360.0f;
            break;
            
        case 5: // Size Change
            holdState.SizeMultiplier = 1.0f + holdProgress * 0.5f;
            break;
            
        case 6: // Rotation
            holdState.RotationAngle = holdProgress * 360.0f;
            break;
            
        case 7: // Custom
            UpdateCustomEvolution(holdState, holdProgress);
            break;
    }
}
```

## Laser Rendering

### Main Beat Hold Rendering
```csharp
private void RenderBeatHoldLasers(ImageBuffer output)
{
    foreach (var holdState in ActiveHoldStates)
    {
        // Render each laser in the hold state
        for (int i = 0; i < holdState.LaserPositions.Count; i++)
        {
            var position = holdState.LaserPositions[i];
            var laserProperties = CalculateLaserProperties(holdState, i);
            
            // Render individual laser
            RenderSingleLaser(output, laserProperties);
        }
    }
}
```

### Laser Property Calculation
```csharp
private LaserProperties CalculateLaserProperties(LaserHoldState holdState, int laserIndex)
{
    var properties = new LaserProperties();
    
    // Get base position from hold state
    var position = holdState.LaserPositions[laserIndex];
    properties.StartX = position.StartX;
    properties.StartY = position.StartY;
    properties.EndX = position.EndX;
    properties.EndY = position.EndY;
    
    // Apply evolution effects
    properties.Color = ApplyColorEvolution(LaserColor, holdState.ColorShift);
    properties.Intensity = LaserIntensity * holdState.SizeMultiplier;
    properties.Opacity = LaserOpacity * holdState.CurrentOpacity;
    properties.Effect = HoldEffect;
    
    // Apply pattern-specific modifications
    ApplyPatternModifications(properties, holdState, laserIndex);
    
    return properties;
}
```

### Pattern Modifications
```csharp
private void ApplyPatternModifications(LaserProperties properties, LaserHoldState holdState, int laserIndex)
{
    switch (holdState.PatternType)
    {
        case 0: // Single
            // No modifications needed
            break;
            
        case 1: // Multiple
            // Vary laser properties based on index
            properties.Intensity *= 1.0f - (laserIndex * 0.1f);
            break;
            
        case 2: // Alternating
            // Alternate laser properties
            if (laserIndex % 2 == 0)
            {
                properties.Color = BlendColors(properties.Color, 0xFFFFFF, 0.3f);
            }
            break;
            
        case 3: // Spiral
            // Apply spiral positioning
            float spiralAngle = laserIndex * (360.0f / holdState.LaserCount) + holdState.RotationAngle;
            ApplySpiralPositioning(properties, spiralAngle);
            break;
            
        case 4: // Grid
            // Apply grid positioning
            ApplyGridPositioning(properties, laserIndex, holdState.LaserCount);
            break;
            
        case 5: // Random
            // Apply randomization
            ApplyRandomModifications(properties, laserIndex);
            break;
            
        case 6: // Wave
            // Apply wave distortion
            ApplyWaveDistortion(properties, laserIndex);
            break;
            
        case 7: // Custom
            ApplyCustomPatternModifications(properties, holdState, laserIndex);
            break;
    }
}
```

## Beat History Analysis

### Beat History Update
```csharp
private void UpdateBeatHistory()
{
    if (!EnableBeatHistory)
        return;

    // Add current beat state to history
    var beatEvent = new BeatEvent
    {
        Time = GetCurrentTime(),
        Energy = CurrentAudioEnergy,
        BeatStrength = CurrentBeatStrength,
        Frequency = CurrentFrequency
    };
    
    BeatHistory.Add(beatEvent);
    
    // Maintain maximum history size
    while (BeatHistory.Count > MaxBeatHistory)
    {
        BeatHistory.RemoveAt(0);
    }
    
    // Analyze beat patterns
    AnalyzeBeatPatterns();
}
```

### Beat Pattern Analysis
```csharp
private void AnalyzeBeatPatterns()
{
    if (BeatHistory.Count < 3)
        return;

    // Calculate beat intervals
    var intervals = new List<float>();
    for (int i = 1; i < BeatHistory.Count; i++)
    {
        float interval = BeatHistory[i].Time - BeatHistory[i - 1].Time;
        intervals.Add(interval);
    }
    
    // Calculate average beat interval
    float averageInterval = intervals.Average();
    
    // Detect tempo changes
    float tempoVariation = intervals.StandardDeviation() / averageInterval;
    
    // Adjust beat sensitivity based on tempo stability
    if (tempoVariation < 0.1f)
    {
        BeatSensitivity = Math.Min(1.0f, BeatSensitivity * 1.1f);
    }
    else
    {
        BeatSensitivity = Math.Max(0.1f, BeatSensitivity * 0.9f);
    }
}
```

## Performance Optimization

### Optimization Techniques
1. **Beat History Management**: Efficient beat event storage and cleanup
2. **Laser State Caching**: Cache calculated laser properties
3. **Early Exit**: Skip processing for expired hold states
4. **Batch Processing**: Process multiple lasers together
5. **Threading**: Multi-threaded beat analysis and rendering

### Memory Management
- Efficient beat history storage
- Minimize temporary allocations
- Use value types for calculations
- Optimize laser rendering operations

## Integration with EffectGraph

### Port Configuration
```csharp
protected override void InitializePorts()
{
    _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Background image"));
    _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Beat-hold laser output"));
}
```

### Metadata Support
```csharp
public override EffectMetadata GetMetadata()
{
    var metadata = new EffectMetadata();
    metadata.Add("BeatSensitivity", BeatSensitivity);
    metadata.Add("HoldDuration", HoldDuration);
    metadata.Add("EvolutionMode", EvolutionMode);
    metadata.Add("PatternType", PatternType);
    metadata.Add("BeatReactive", BeatReactive);
    metadata.Add("HoldEffect", HoldEffect);
    metadata.Add("LaserColor", LaserColor);
    metadata.Add("LaserIntensity", LaserIntensity);
    metadata.Add("BeatPattern", BeatPattern);
    metadata.Add("BeatThreshold", BeatThreshold);
    metadata.Add("ActiveHoldStates", ActiveHoldStates.Count);
    metadata.Add("BeatHistoryCount", BeatHistory.Count);
    return metadata;
}
```

## Testing and Validation

### Test Cases
1. **Basic Beat Detection**: Verify beat detection accuracy
2. **Hold Duration**: Test laser hold timing
3. **Evolution Modes**: Test all evolution behaviors
4. **Pattern Types**: Test all pattern types
5. **Performance**: Measure beat analysis speed
6. **Edge Cases**: Handle boundary conditions

### Validation Methods
- Visual comparison with reference images
- Performance benchmarking
- Memory usage analysis
- Beat detection accuracy testing

## Future Enhancements

### Planned Features
1. **Advanced Beat Analysis**: More sophisticated beat detection algorithms
2. **Machine Learning**: ML-based beat pattern recognition
3. **Real-time Effects**: Dynamic beat pattern generation
4. **Hardware Acceleration**: GPU-accelerated beat analysis
5. **Custom Shaders**: User-defined beat visualization effects

### Compatibility
- Full AVS preset compatibility
- Support for legacy beat hold modes
- Performance parity with original
- Extended functionality

## Conclusion

The Laser Beat Hold effect provides essential beat-synchronized laser visualization capabilities for rhythmic AVS presets. This C# implementation maintains full compatibility with the original while adding modern features like advanced beat detection and enhanced laser evolution. Complete documentation ensures reliable operation in production environments with optimal performance and visual quality.
