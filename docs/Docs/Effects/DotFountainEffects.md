# Dot Fountain Effects

## Overview

The Dot Fountain effect creates a 3D fountain of colored dots that respond to audio input, creating dynamic particle systems that react to music. This effect simulates a fountain with physics-based particle movement, color interpolation, and audio-reactive behavior.

## Implementation

**Class**: `DotFountainEffectsNode`  
**Namespace**: `PhoenixVisualizer.Core.Effects.Nodes.AvsEffects`  
**Inherits**: `BaseEffectNode`

## Features

- **3D Particle System**: Renders particles in 3D space with perspective projection
- **Audio Reactivity**: Particles respond to audio spectrum data and beat detection
- **Physics Simulation**: Includes gravity, velocity, and particle lifetime
- **Color Interpolation**: Smooth color transitions between predefined color sets
- **Rotation Control**: Configurable rotation speed and angle
- **Performance Optimized**: Capped particle count for smooth performance

## Properties

### Core Properties
- **Enabled** (bool): Enable or disable the effect
- **RotationVelocity** (float): Speed of rotation in degrees per frame
- **Angle** (float): Tilt angle of the fountain (-90° to 90°)
- **BaseRadius** (float): Base radius of the fountain (0.1 to 10.0)
- **Intensity** (float): Brightness multiplier for particles (0.1 to 10.0)

### Audio Properties
- **BeatResponse** (bool): Enable beat detection response
- **AudioSensitivity** (float): Multiplier for audio input (0.1 to 5.0)
- **ParticleLifetime** (float): Decay factor for particle velocity (0.0 to 1.0)

### Physics Properties
- **Gravity** (float): Gravity force applied to particles (0.0 to 1.0)
- **HeightOffset** (float): Vertical offset of the fountain (-100 to 100)
- **Depth** (float): Distance from camera (100 to 1000)

### Colors
- **DefaultColors** (Color[]): Array of 5 base colors for particle coloring

## Technical Details

### Particle System
- **Maximum Particles**: 7,680 (30 rotation divisions × 256 height levels)
- **Particle Structure**: Includes position, velocity, color, and active state
- **Memory Management**: Efficient array-based storage with active/inactive flags

### Rendering Pipeline
1. **Audio Processing**: Extract spectrum data and beat information
2. **Particle Update**: Apply physics (gravity, velocity, lifetime)
3. **Matrix Transformation**: 3D rotation and perspective projection
4. **Screen Mapping**: Convert 3D coordinates to 2D screen space
5. **Color Application**: Apply intensity and color interpolation

### Audio Integration
- **Spectrum Analysis**: Uses audio spectrum data for particle generation
- **Beat Detection**: Responds to detected beats with intensity boosts
- **Frequency Mapping**: Maps audio bands to particle positions
- **Variation**: Adds subtle sine wave variation for organic movement

## Usage Examples

### Basic Fountain
```csharp
var fountain = new DotFountainEffectsNode();
fountain.Enabled = true;
fountain.RotationVelocity = 16.0f;
fountain.AudioSensitivity = 1.0f;
```

### Beat-Responsive Fountain
```csharp
var fountain = new DotFountainEffectsNode();
fountain.BeatResponse = true;
fountain.AudioSensitivity = 2.0f;
fountain.Intensity = 1.5f;
fountain.Gravity = 0.05f;
```

### Slow Rotating Fountain
```csharp
var fountain = new DotFountainEffectsNode();
fountain.RotationVelocity = 4.0f;
fountain.Angle = -15.0f;
fountain.BaseRadius = 0.8f;
fountain.ParticleLifetime = 0.95f;
```

## Performance Considerations

- **Particle Count**: Limited to 7,680 particles for smooth performance
- **Memory Usage**: Efficient array storage with minimal allocation
- **Rendering**: Only renders active particles
- **Audio Processing**: Minimal CPU overhead for spectrum analysis

## Audio Reactivity Details

### Spectrum Response
- Particles are generated based on audio spectrum data
- Each rotation division corresponds to a frequency band
- Higher audio values create more energetic particles

### Beat Detection
- When a beat is detected, all particles receive an intensity boost
- Beat response adds 128 to the base audio value
- Creates dramatic visual spikes synchronized with music

### Frequency Mapping
- Audio spectrum is mapped to particle positions
- Lower frequencies affect inner particles
- Higher frequencies affect outer particles

## Color System

### Default Color Palette
1. **Forest Green** (28, 107, 24)
2. **Bright Red** (255, 10, 35)
3. **Navy Blue** (42, 29, 116)
4. **Purple** (144, 54, 217)
5. **Light Blue** (107, 136, 255)

### Color Interpolation
- 16-step interpolation between each color pair
- Creates smooth color transitions across the spectrum
- 64 total color variations available

## Physics Simulation

### Gravity System
- Particles fall naturally under gravity influence
- Gravity affects both height and radius velocity
- Creates realistic fountain-like behavior

### Velocity Decay
- Particle velocity decreases over time
- ParticleLifetime controls decay rate
- Prevents infinite particle accumulation

### Collision Detection
- Particles are constrained to positive height values
- No complex collision physics (performance optimization)
- Natural boundary constraints

## Integration with PhoenixVisualizer

### Port Configuration
- **Input**: Image buffer for sizing and overlay
- **Output**: Processed image with fountain effect
- **Audio**: Integrated through AudioFeatures system

### Effect Graph Integration
- Can be chained with other effects
- Supports real-time parameter adjustment
- Compatible with preset system

## Future Enhancements

### Planned Features
- **Particle Textures**: Support for custom particle shapes
- **Advanced Physics**: Wind effects, turbulence, and collision
- **Color Presets**: User-defined color schemes
- **Performance Scaling**: Dynamic particle count based on system capability

### Optimization Opportunities
- **GPU Acceleration**: Move particle calculations to GPU
- **LOD System**: Level-of-detail based on distance
- **Culling**: Frustum culling for off-screen particles
- **Instancing**: Batch rendering for similar particles

## Troubleshooting

### Common Issues
- **Low Performance**: Reduce particle count or disable complex effects
- **No Particles**: Check audio input and sensitivity settings
- **Visual Glitches**: Ensure depth and perspective values are reasonable
- **Memory Issues**: Monitor particle count and lifetime settings

### Debug Information
- Use `GetExecutionStats()` for runtime information
- Monitor active particle count with `GetActiveParticleCount()`
- Check audio sensitivity and beat detection status

## Related Effects

- **DotGridEffects**: Static grid of dots
- **DotPlaneEffects**: 2D plane of audio-reactive dots
- **StarfieldEffects**: Space-based particle systems
- **ParticleSystems**: General particle effect framework
