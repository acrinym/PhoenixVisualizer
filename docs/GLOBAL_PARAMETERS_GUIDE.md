# Global Parameters Guide

## Overview

Phoenix Visualizer features a comprehensive Global Parameter System that provides universal controls across all visualizers and effect nodes. This system allows you to apply consistent effects, adjustments, and behaviors to your entire visualization setup with just a few parameter changes.

## Architecture

### Parameter Categories
The global parameter system is organized into 5 main categories, each providing different types of universal controls:

1. **🎛️ General Parameters** - Basic on/off and opacity controls
2. **🎵 Audio Parameters** - Universal audio sensitivity and processing
3. **👁️ Visual Parameters** - Global visual effects and transformations
4. **🏃 Motion Parameters** - Animation and movement controls
5. **✨ Effects Parameters** - Advanced effect layering controls

### Implementation
- **Universal Application**: Parameters apply to all active visualizers
- **Real-time Updates**: Changes take effect immediately
- **Hierarchical Control**: Global parameters can be overridden by specific visualizer settings
- **Preservation of Uniqueness**: Each visualizer maintains its individual character while benefiting from global controls

---

## 🎛️ General Parameters

### Enabled
**Type**: Boolean (true/false)  
**Default**: true  
**Range**: N/A  
**Description**: Master enable/disable switch for the visualizer

**Effects:**
- When disabled, visualizer produces no output
- Useful for A/B testing or temporarily disabling effects
- Preserves all parameter values when re-enabled

### Opacity
**Type**: Float  
**Default**: 1.0  
**Range**: 0.0 - 1.0  
**Description**: Global transparency multiplier

**Effects:**
- 1.0 = Fully opaque (normal)
- 0.5 = 50% transparent
- 0.0 = Fully transparent (invisible)
- Multiplies with any existing opacity values

### Brightness
**Type**: Float  
**Default**: 1.0  
**Range**: 0.0 - 3.0  
**Description**: Global brightness adjustment

**Effects:**
- 1.0 = Normal brightness
- 2.0 = 2x brightness (very bright)
- 0.5 = Half brightness (darker)
- 0.0 = Complete darkness
- Applied to all color channels

### Saturation
**Type**: Float  
**Default**: 1.0  
**Range**: 0.0 - 2.0  
**Description**: Global color saturation multiplier

**Effects:**
- 1.0 = Normal color saturation
- 2.0 = Highly saturated colors
- 0.5 = Muted colors
- 0.0 = Grayscale (no color)
- Affects all color channels equally

### Contrast
**Type**: Float  
**Default**: 1.0  
**Range**: 0.1 - 3.0  
**Description**: Global contrast adjustment

**Effects:**
- 1.0 = Normal contrast
- 2.0 = High contrast (bold blacks and whites)
- 0.5 = Low contrast (washed out)
- 0.1 = Minimal contrast (very flat)
- Enhances or reduces tonal range

---

## 🎵 Audio Parameters

### Audio Sensitivity
**Type**: Float  
**Default**: 1.0  
**Range**: 0.1 - 5.0  
**Description**: Global multiplier for all audio-reactive elements

**Effects:**
- 1.0 = Normal audio responsiveness
- 2.0 = Highly reactive to audio
- 0.5 = Less responsive to audio
- 0.1 = Minimal audio influence
- Scales all audio-based animations and effects

### Bass Multiplier
**Type**: Float  
**Default**: 1.0  
**Range**: 0.0 - 3.0  
**Description**: Low frequency response multiplier

**Effects:**
- 1.0 = Normal bass response
- 2.0 = Enhanced bass effects
- 0.5 = Reduced bass influence
- 0.0 = No bass response
- Affects visual elements reacting to low frequencies (20-250Hz)

### Mid Multiplier
**Type**: Float  
**Default**: 1.0  
**Range**: 0.0 - 3.0  
**Description**: Mid frequency response multiplier

**Effects:**
- 1.0 = Normal midrange response
- 2.0 = Enhanced midrange effects
- 0.5 = Reduced midrange influence
- 0.0 = No midrange response
- Affects visual elements reacting to mid frequencies (250-4000Hz)

### Treble Multiplier
**Type**: Float  
**Default**: 1.0  
**Range**: 0.0 - 3.0  
**Description**: High frequency response multiplier

**Effects:**
- 1.0 = Normal treble response
- 2.0 = Enhanced treble effects
- 0.5 = Reduced treble influence
- 0.0 = No treble response
- Affects visual elements reacting to high frequencies (4000Hz+)

### Beat Threshold
**Type**: Float  
**Default**: 0.3  
**Range**: 0.1 - 1.0  
**Description**: Minimum audio level to trigger beat detection

**Effects:**
- 0.3 = Moderate beat sensitivity
- 0.1 = Very sensitive to beats
- 0.5 = Less sensitive to beats
- 0.8 = Only responds to very loud beats
- Lower values detect more beats but may include noise

---

## 👁️ Visual Parameters

### Scale
**Type**: Float  
**Default**: 1.0  
**Range**: 0.1 - 3.0  
**Description**: Global size scaling multiplier

**Effects:**
- 1.0 = Normal size
- 2.0 = 2x larger
- 0.5 = Half size
- 0.1 = Very small
- Affects all geometric elements and particle sizes

### Blur Amount
**Type**: Float  
**Default**: 0.0  
**Range**: 0.0 - 10.0  
**Description**: Global Gaussian blur intensity

**Effects:**
- 0.0 = No blur (sharp)
- 5.0 = Moderate blur
- 10.0 = Heavy blur
- Applied as post-processing effect
- Can create dreamy or ethereal effects

### Glow Intensity
**Type**: Float  
**Default**: 0.0  
**Range**: 0.0 - 5.0  
**Description**: Global glow effect strength

**Effects:**
- 0.0 = No glow
- 1.0 = Subtle glow
- 3.0 = Strong glow
- 5.0 = Intense glow
- Adds luminous halo around bright elements

### Color Shift
**Type**: Float  
**Default**: 0.0  
**Range**: 0.0 - 360.0  
**Description**: Global hue shift in degrees

**Effects:**
- 0.0 = No color shift
- 60.0 = Shift toward yellow
- 120.0 = Shift toward green
- 180.0 = Shift toward cyan
- 240.0 = Shift toward blue
- 300.0 = Shift toward magenta

### Color Speed
**Type**: Float  
**Default**: 0.0  
**Range**: -5.0 - 5.0  
**Description**: Automatic color cycling speed

**Effects:**
- 0.0 = Static colors
- 1.0 = Slow color cycling
- 5.0 = Fast color cycling
- -2.0 = Reverse color cycling
- Creates rainbow or psychedelic color effects

---

## 🏃 Motion Parameters

### Animation Speed
**Type**: Float  
**Default**: 1.0  
**Range**: -2.0 - 5.0  
**Description**: Global animation speed multiplier

**Effects:**
- 1.0 = Normal animation speed
- 2.0 = 2x faster animation
- 0.5 = Half speed animation
- -1.0 = Reverse animation
- -2.0 = Fast reverse animation
- Affects all time-based animations

### Rotation Speed
**Type**: Float  
**Default**: 0.0  
**Range**: -5.0 - 5.0  
**Description**: Global rotation speed in degrees/second

**Effects:**
- 0.0 = No rotation
- 1.0 = Slow clockwise rotation
- -2.0 = Fast counter-clockwise rotation
- Applied to entire visualization
- Can create spinning or tumbling effects

### Position X
**Type**: Float  
**Default**: 0.0  
**Range**: -1.0 - 1.0  
**Description**: Global horizontal position offset

**Effects:**
- 0.0 = Center position
- 0.5 = Shifted right by half screen
- -0.3 = Shifted left by 30% of screen
- Affects entire visualization position

### Position Y
**Type**: Float  
**Default**: 0.0  
**Range**: -1.0 - 1.0  
**Description**: Global vertical position offset

**Effects:**
- 0.0 = Center position
- 0.4 = Shifted up by 40% of screen
- -0.6 = Shifted down by 60% of screen
- Affects entire visualization position

### Bounce Factor
**Type**: Float  
**Default**: 0.0  
**Range**: 0.0 - 1.0  
**Description**: Global elasticity/bounce strength

**Effects:**
- 0.0 = No bouncing
- 0.5 = Moderate bouncing
- 1.0 = Maximum bouncing
- Affects particle systems and animated elements
- Creates springy or elastic motion

---

## ✨ Effects Parameters

### Trail Length
**Type**: Float  
**Default**: 0.0  
**Range**: 0.0 - 1.0  
**Description**: Global motion trail length

**Effects:**
- 0.0 = No trails
- 0.5 = Medium-length trails
- 1.0 = Long, persistent trails
- Creates motion blur or comet-like effects
- Memory intensive at high values

### Decay Rate
**Type**: Float  
**Default**: 0.95  
**Range**: 0.8 - 0.99  
**Description**: Global element decay/fade speed

**Effects:**
- 0.95 = Slow decay (long-lasting elements)
- 0.99 = Very slow decay (persistent elements)
- 0.85 = Faster decay (quick fading)
- 0.80 = Rapid decay (short-lived elements)
- Affects particle lifetime and trail fading

### Particle Count
**Type**: Integer  
**Default**: 0  
**Range**: 0 - 1000  
**Description**: Global particle count override

**Effects:**
- 0 = Use visualizer's default particle count
- 100 = Limit to 100 particles maximum
- 500 = Moderate particle count
- 1000 = High particle count (may impact performance)
- Overrides individual visualizer particle settings

### Waveform Mode
**Type**: String (Dropdown)  
**Default**: "Normal"  
**Options**: "Normal", "Circular", "Spiral", "Random"  
**Description**: Global waveform/element arrangement pattern

**Effects:**
- **Normal**: Standard linear arrangement
- **Circular**: Elements arranged in circles
- **Spiral**: Spiral pattern arrangement
- **Random**: Random positioning
- Affects particle systems and geometric patterns

### Mirror Mode
**Type**: String (Dropdown)  
**Default**: "None"  
**Options**: "None", "Horizontal", "Vertical", "Both", "Radial"  
**Description**: Global symmetry/mirroring mode

**Effects:**
- **None**: No mirroring
- **Horizontal**: Mirror across horizontal axis
- **Vertical**: Mirror across vertical axis
- **Both**: Mirror across both axes (creates 4-way symmetry)
- **Radial**: Radial symmetry pattern
- Creates kaleidoscope-like effects

---

## Usage Guide

### Getting Started
1. **Enable Global Parameters**: Make sure your visualizer supports global parameters
2. **Start Conservative**: Begin with default values (1.0 for multipliers, 0.0 for offsets)
3. **Test Incrementally**: Change one parameter at a time to understand effects
4. **Save Presets**: Create presets for different global parameter combinations

### Parameter Interaction
- **Multiplicative Effects**: Global parameters multiply with visualizer-specific parameters
- **Additive Effects**: Position and rotation offsets are added to existing values
- **Override Behavior**: Some parameters (like particle count) can override visualizer settings
- **Real-time Updates**: All changes take effect immediately without restart

### Performance Considerations
- **High Values Impact Performance**:
  - Blur Amount > 5.0 may reduce frame rate
  - Trail Length > 0.7 increases memory usage
  - Particle Count > 500 may cause slowdown
  - Glow Intensity > 3.0 requires more GPU processing

- **Optimization Tips**:
  - Use lower values for better performance
  - Disable unused effects (set to 0.0 or false)
  - Monitor frame rate while adjusting parameters
  - Save performance presets for different hardware

### Best Practices

#### Audio Tuning
- Start with Audio Sensitivity = 1.0
- Adjust Bass/Mid/Treble multipliers based on your music
- Use Beat Threshold to filter out noise
- Fine-tune individual frequency bands for optimal response

#### Visual Enhancement
- Use Brightness and Contrast for mood adjustment
- Apply subtle Color Shift for creative effects
- Use Glow sparingly to highlight important elements
- Balance Blur with detail preservation

#### Motion Control
- Animation Speed affects the "energy" of your visualization
- Use Rotation Speed for dynamic effects
- Position offsets can create asymmetric layouts
- Bounce Factor adds playfulness to motion

#### Creative Effects
- Combine Mirror Mode with Color Shift for kaleidoscope effects
- Use Trail Length to create motion blur effects
- Experiment with Waveform Mode for different arrangements
- Layer multiple global effects for complex results

### Troubleshooting

#### Common Issues
- **No Effect Visible**: Check if global parameters are supported by your visualizer
- **Performance Drop**: Reduce high-impact parameters (blur, particles, trails)
- **Audio Not Responding**: Verify Audio Sensitivity > 0.1 and check audio source
- **Visual Artifacts**: Reset parameters to defaults if experiencing glitches

#### Parameter Conflicts
- **Multiple Visualizers**: Global parameters apply to all active visualizers
- **Parameter Override**: Some visualizer-specific parameters may conflict with global settings
- **Range Limits**: Parameters are automatically clamped to valid ranges
- **Type Conversion**: Automatic conversion between parameter types when needed

---

## Technical Details

### Parameter Storage
- **Real-time Values**: Parameters stored in memory for immediate access
- **Persistent Storage**: Values saved to configuration files
- **Preset System**: Global parameter presets can be saved and loaded
- **Validation**: Automatic range checking and type validation

### Processing Order
1. **Audio Processing**: Audio parameters applied to input signal
2. **Motion Calculation**: Animation and physics parameters processed
3. **Visual Effects**: Color, brightness, and transformation parameters applied
4. **Post-processing**: Blur, glow, and trail effects added
5. **Output**: Final composite rendered to display

### Memory Usage
- **Base Overhead**: ~2KB for parameter storage
- **Per Parameter**: ~100 bytes additional
- **Preset Storage**: ~5KB per saved preset
- **Runtime Buffers**: Varies based on enabled effects

### Thread Safety
- **Atomic Updates**: Parameter changes are thread-safe
- **No Race Conditions**: Protected against concurrent access
- **Real-time Consistency**: All threads see consistent parameter values

### Platform Compatibility
- **Cross-platform**: Consistent behavior across Windows, Linux, and macOS
- **Hardware Acceleration**: Leverages GPU for performance-critical operations
- **Fallback Support**: Graceful degradation on lower-end hardware

---

## Advanced Usage

### Parameter Automation
- **MIDI Control**: Map global parameters to MIDI controllers
- **OSC Integration**: Control parameters via Open Sound Control
- **Scripting**: Programmatic parameter manipulation
- **Network Sync**: Synchronize parameters across multiple instances

### Custom Parameter Sets
- **Genre-Specific**: Different settings for different music genres
- **Mood-Based**: Parameters optimized for different emotional responses
- **Performance Profiles**: Optimized settings for different hardware capabilities
- **Creative Templates**: Pre-configured parameter combinations for specific effects

### Integration with Effect Nodes
- **Global Parameter Bridge**: Connect global parameters to effect node inputs
- **Parameter Routing**: Route global parameters to specific effect chains
- **Conditional Effects**: Enable/disable effects based on global parameter values
- **Dynamic Chains**: Modify effect chains based on global parameter states

### Performance Profiling
- **Parameter Impact Analysis**: Measure performance impact of each parameter
- **Optimization Recommendations**: Automatic suggestions for performance improvement
- **Hardware-Specific Tuning**: Parameters optimized for specific hardware configurations
- **Real-time Monitoring**: Live performance metrics and parameter impact display
