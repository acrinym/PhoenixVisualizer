# Custom BPM Effects

## Overview

The Custom BPM (Beats Per Minute) Effects system provides advanced beat detection and tempo-based visual effects. This system goes beyond basic beat detection to offer customizable BPM analysis, tempo synchronization, and beat-reactive visual elements that can be precisely tuned to different musical styles and preferences.

## Implementation

**Class**: `CustomBPMEffectsNode`  
**Namespace**: `PhoenixVisualizer.Core.Effects.Nodes.AvsEffects`  
**Inherits**: `BaseEffectNode`

## Features

- **Advanced Beat Detection**: Sophisticated algorithms for accurate BPM detection
- **Customizable Sensitivity**: Adjustable parameters for different music genres
- **Tempo Synchronization**: Effects that sync perfectly with detected BPM
- **Beat Prediction**: Anticipates upcoming beats for smooth visual transitions
- **Multi-Band Analysis**: Separate analysis for different frequency ranges
- **Historical Tracking**: Maintains BPM history for stability and accuracy

## Properties

### Core Properties
- **Enabled** (bool): Enable or disable the BPM detection system
- **BPM** (float): Current detected BPM value
- **Confidence** (float): Confidence level of BPM detection (0.0 to 1.0)
- **IsBeat** (bool): Whether a beat was detected in the current frame
- **BeatPhase** (float): Current position within the beat cycle (0.0 to 1.0)

### Detection Parameters
- **Sensitivity** (float): Beat detection sensitivity (0.1 to 5.0)
- **Threshold** (float): Minimum amplitude for beat detection (0.0 to 1.0)
- **DecayRate** (float): How quickly beat signals decay (0.0 to 1.0)
- **MinBPM** (float): Minimum BPM to detect (60 to 200)
- **MaxBPM** (float): Maximum BPM to detect (60 to 200)

### Frequency Analysis
- **LowBandWeight** (float): Weight for low frequency analysis (0.0 to 1.0)
- **MidBandWeight** (float): Weight for mid frequency analysis (0.0 to 1.0)
- **HighBandWeight** (float): Weight for high frequency analysis (0.0 to 1.0)
- **SubBassWeight** (float): Weight for sub-bass frequencies (0.0 to 1.0)

### Advanced Settings
- **TempoLock** (bool): Lock to detected tempo for stability
- **TempoTolerance** (float): Allowed variation in BPM detection (0.0 to 0.5)
- **HistoryLength** (int): Number of BPM values to remember (10 to 100)
- **SmoothingFactor** (float): Smoothing applied to BPM changes (0.0 to 1.0)

## Technical Details

### Beat Detection Algorithm
1. **Spectral Analysis**: Analyze audio spectrum across multiple frequency bands
2. **Peak Detection**: Identify amplitude peaks that exceed threshold
3. **Temporal Analysis**: Measure time intervals between detected peaks
4. **Pattern Recognition**: Identify repeating patterns in peak intervals
5. **BPM Calculation**: Convert intervals to BPM using statistical analysis
6. **Validation**: Verify BPM consistency across multiple beat cycles

### Frequency Band Processing
- **Sub-Bass (20-60 Hz)**: Provides fundamental rhythm information
- **Low Bass (60-250 Hz)**: Contains kick drum and bass guitar fundamentals
- **Mid Bass (250-500 Hz)**: Includes snare and tom-tom frequencies
- **High Bass (500-2000 Hz)**: Contains hi-hat and cymbal information
- **High Frequencies (2000+ Hz)**: Provides detail and articulation

### Beat Phase Calculation
- **Beat Phase**: Position within the current beat (0.0 = beat start, 1.0 = next beat)
- **Sub-Beat Division**: Supports 1/4, 1/8, 1/16 note subdivisions
- **Sync Points**: Precisely timed visual events synchronized with beats

## Usage Examples

### Basic BPM Detection
```csharp
var bpmEffect = new CustomBPMEffectsNode();
bpmEffect.Enabled = true;
bpmEffect.Sensitivity = 1.0f;
bpmEffect.Threshold = 0.3f;
```

### High-Sensitivity Beat Detection
```csharp
var bpmEffect = new CustomBPMEffectsNode();
bpmEffect.Sensitivity = 2.5f;
bpmEffect.Threshold = 0.1f;
bpmEffect.DecayRate = 0.8f;
bpmEffect.TempoLock = true;
```

### Genre-Specific Settings
```csharp
// Electronic/Dance Music
var bpmEffect = new CustomBPMEffectsNode();
bpmEffect.MinBPM = 120;
bpmEffect.MaxBPM = 140;
bpmEffect.SubBassWeight = 0.8f;
bpmEffect.LowBandWeight = 0.9f;

// Rock/Metal
var rockBPM = new CustomBPMEffectsNode();
rockBPM.MinBPM = 80;
rockBPM.MaxBPM = 160;
rockBPM.MidBandWeight = 0.9f;
rockBPM.HighBandWeight = 0.7f;
```

### Beat-Synchronized Effects
```csharp
var bpmEffect = new CustomBPMEffectsNode();
if (bpmEffect.IsBeat)
{
    // Trigger visual effect on beat
    TriggerBeatEffect();
}

// Use beat phase for smooth transitions
float phase = bpmEffect.BeatPhase;
ApplyPhaseBasedEffect(phase);
```

## Performance Considerations

### CPU Usage
- **Real-time Analysis**: Processes audio data every frame
- **FFT Operations**: Fast Fourier Transform for spectral analysis
- **Memory Allocation**: Minimal allocation during normal operation
- **Optimization**: Efficient algorithms for real-time performance

### Memory Usage
- **BPM History**: Stores recent BPM values for stability
- **Peak Buffers**: Maintains peak detection history
- **Frequency Buffers**: Caches frequency analysis data
- **Overall Footprint**: Typically under 1MB for complete system

## Audio Integration

### Input Requirements
- **Sample Rate**: Supports 44.1kHz, 48kHz, and 96kHz
- **Bit Depth**: 16-bit and 24-bit audio supported
- **Channels**: Mono and stereo input supported
- **Buffer Size**: Configurable for different latency requirements

### Output Data
- **BPM Value**: Current detected tempo
- **Beat Events**: Boolean beat detection signals
- **Phase Information**: Precise beat timing
- **Confidence Metrics**: Reliability indicators

## Integration with PhoenixVisualizer

### Port Configuration
- **Audio Input**: Audio stream for analysis
- **BPM Output**: Current BPM value
- **Beat Output**: Beat detection signal
- **Phase Output**: Beat phase information

### Effect Chain Integration
- **Input Node**: Receives audio from audio system
- **Processing**: Analyzes audio and detects beats
- **Output**: Provides BPM data to other effects
- **Feedback**: Can receive feedback from visual effects

## Advanced Features

### Tempo Locking
- **Stability**: Prevents BPM jumping between similar tempos
- **Tolerance**: Configurable range for tempo variations
- **Hysteresis**: Requires significant change to unlock tempo

### Beat Prediction
- **Anticipation**: Predicts upcoming beats based on current tempo
- **Smoothing**: Applies smoothing to prevent jitter
- **Adaptation**: Adjusts to tempo changes over time

### Multi-Genre Support
- **Electronic**: Optimized for consistent electronic beats
- **Rock/Metal**: Handles variable tempo and complex rhythms
- **Jazz/Classical**: Supports free-form and rubato passages
- **Hip-Hop**: Optimized for sampled and looped beats

## Troubleshooting

### Common Issues
- **Incorrect BPM**: Adjust sensitivity and threshold settings
- **Missed Beats**: Increase sensitivity or lower threshold
- **False Positives**: Increase threshold or adjust frequency weights
- **Tempo Drift**: Enable tempo lock and adjust tolerance

### Debug Information
- **Confidence Level**: Check reliability of current BPM
- **Frequency Analysis**: Monitor individual band contributions
- **Peak Detection**: Verify peak detection is working
- **History Stability**: Check BPM consistency over time

### Performance Issues
- **High CPU Usage**: Reduce analysis frequency or complexity
- **Memory Leaks**: Monitor buffer allocation and cleanup
- **Latency**: Adjust buffer sizes for real-time requirements

## Future Enhancements

### Planned Features
- **Machine Learning**: AI-powered beat detection
- **Genre Recognition**: Automatic genre-specific optimization
- **Polyrhythm Support**: Detection of complex rhythmic patterns
- **Tempo Mapping**: Support for variable tempo tracks

### API Extensions
- **Plugin System**: Third-party beat detection algorithms
- **Custom Metrics**: User-defined beat detection criteria
- **Export/Import**: Save and load detection presets
- **Real-time Tuning**: Live adjustment of detection parameters

## Related Effects

- **BeatDetection**: Basic beat detection functionality
- **BPMEffects**: Standard BPM-based effects
- **BeatSpinning**: Beat-reactive spinning effects
- **DynamicMovement**: Audio-reactive movement effects
- **SpectrumVisualization**: Audio spectrum display
