# Phoenix Visualizers Guide

## Overview

Phoenix Visualizer includes a comprehensive collection of built-in visualizers that leverage modern C# implementation with AVS-inspired effects and global parameter systems. All visualizers support the universal Global Parameter System for consistent control across the entire visualization suite.

## Global Parameter System

### Universal Parameters Available to All Visualizers

#### 🎛️ General Parameters
- **Enabled**: Enable/disable the visualizer globally
- **Opacity**: Overall transparency multiplier (0.0 - 1.0)
- **Brightness**: Brightness adjustment multiplier (0.0 - 3.0)
- **Saturation**: Color saturation multiplier (0.0 - 2.0)
- **Contrast**: Color contrast multiplier (0.1 - 3.0)

#### 🎵 Audio Parameters
- **Audio Sensitivity**: Overall audio responsiveness multiplier (0.1 - 5.0)
- **Bass Multiplier**: Low frequency response multiplier (0.0 - 3.0)
- **Mid Multiplier**: Mid frequency response multiplier (0.0 - 3.0)
- **Treble Multiplier**: High frequency response multiplier (0.0 - 3.0)
- **Beat Threshold**: Minimum audio level for beat detection (0.1 - 1.0)

#### 👁️ Visual Parameters
- **Scale**: Overall size scaling multiplier (0.1 - 3.0)
- **Blur Amount**: Gaussian blur intensity (0.0 - 10.0)
- **Glow Intensity**: Glow effect intensity (0.0 - 5.0)
- **Color Shift**: Hue shift in degrees (0.0 - 360.0)
- **Color Speed**: Automatic color cycling speed (-5.0 - 5.0)

#### 🏃 Motion Parameters
- **Animation Speed**: Animation speed multiplier (-2.0 - 5.0)
- **Rotation Speed**: Rotation speed in degrees/second (-5.0 - 5.0)
- **Position X**: Horizontal position offset (-1.0 - 1.0)
- **Position Y**: Vertical position offset (-1.0 - 1.0)
- **Bounce Factor**: Elasticity/bounce strength (0.0 - 1.0)

#### ✨ Effects Parameters
- **Trail Length**: Length of motion trails (0.0 - 1.0)
- **Decay Rate**: How quickly elements fade (0.8 - 0.99)
- **Particle Count**: Number of particles/elements (0 - 1000)
- **Waveform Mode**: Element arrangement pattern (Normal/Circular/Spiral/Random)
- **Mirror Mode**: Symmetry mode (None/Horizontal/Vertical/Both/Radial)

---

## Built-in Visualizers

### 🎵 Bars Visualizer
**ID**: `BarsVisualizer`  
**Category**: Classic AVS  
**Description**: Traditional frequency spectrum bars with logarithmic scaling and customizable appearance.

#### Specific Parameters (8 total)
- **Max Bars**: Maximum number of frequency bars (8-128)
- **Bar Color**: Color of frequency bars (#RRGGBB)
- **Background Color**: Background color (#RRGGBB)
- **Bar Thickness Base**: Base thickness multiplier (0.1-1.0)
- **Bar Thickness Scale**: Thickness scaling with magnitude (0.0-1.0)
- **Logarithmic Sensitivity**: Logarithmic scaling sensitivity (1.0-50.0)
- **Height Scale**: Vertical scale percentage (0.3-1.0)
- **Bottom Margin**: Bottom margin percentage (0.7-1.0)

#### Audio Reactivity
- Bass frequencies drive bar thickness
- Mid frequencies control bar height
- Treble adds visual noise/detail

### 📊 Waveform Visualizer
**ID**: `WaveformVisualizer`  
**Category**: Classic AVS  
**Description**: Time-domain waveform display with smooth rendering and customizable styling.

#### Specific Parameters (5 total)
- **Waveform Color**: Color of the waveform line (#RRGGBB)
- **Background Color**: Background color (#RRGGBB)
- **Line Thickness**: Thickness of waveform line (0.5-5.0)
- **Amplitude Scale**: Vertical scale of waveform (0.1-1.0)
- **Center Position**: Vertical center position (0.1-0.9)

#### Audio Reactivity
- Direct waveform amplitude visualization
- Global audio sensitivity affects line thickness
- Color shifts based on audio intensity

### 🌈 Spectrum Visualizer
**ID**: `SpectrumVisualizer`  
**Category**: Classic AVS  
**Description**: Enhanced spectrum analyzer with peak detection, color cycling, and mirror modes.

#### Specific Parameters (7 total)
- **Bar Count**: Number of frequency bars (16-128)
- **Sensitivity**: Audio sensitivity multiplier (0.1-3.0)
- **Decay Rate**: Bar decay speed (0.8-0.99)
- **Show Peaks**: Enable peak indicators (true/false)
- **Color Shift**: Base color shift in degrees (0.0-360.0)
- **Bar Width**: Bar width as fraction of space (0.1-1.0)
- **Mirror Mode**: Enable horizontal mirroring (true/false)

#### Audio Reactivity
- Real-time frequency analysis
- Peak detection with decay
- Color shifting based on frequency content
- Mirror mode for symmetrical display

### 🐱 Nyan Cat Visualizer
**ID**: `NyanCatVisualizer`  
**Category**: Gaming/Cultural  
**Description**: Classic Nyan Cat animation with rainbow trail, sparkles, and audio-reactive elements.

#### Specific Parameters (8 total)
- **Trail Length**: Length of rainbow trail (0.1-1.0)
- **Trail Width**: Width of trail segments (1.0-20.0)
- **Sparkle Density**: Number of sparkles per frame (0.1-2.0)
- **Cat Speed**: Movement speed multiplier (0.1-3.0)
- **Movement Mode**: Cat movement pattern (Classic/Smooth/Random)
- **Max Birds**: Maximum flying birds (0-20)
- **Pipe Gap Size**: Vertical spacing for pipes (0.1-1.0)
- **Scroll Speed**: Background scroll speed (0.1-3.0)

#### Audio Reactivity
- Bass drives trail width
- Mid frequencies control sparkle density
- Treble affects cat speed
- Beat detection triggers special effects

---

## AVS-Inspired Visualizers

### 🎵 Spectrum Waveform Hybrid
**ID**: `SpectrumWaveformHybrid`  
**Category**: AVS-Inspired  
**Description**: Combines frequency spectrum bars with time-domain waveform data for layered visualization.

#### Specific Parameters (8 total)
- **Spectrum Bars**: Number of spectrum bars (16-128)
- **Spectrum Color**: Color of spectrum bars (#RRGGBB)
- **Waveform Color**: Color of waveform line (#RRGGBB)
- **Waveform Amplitude**: Vertical scale of waveform (0.1-1.0)
- **Waveform Thickness**: Line thickness (0.5-5.0)
- **Bar Width**: Spectrum bar width (0.1-1.0)
- **Show Spectrum**: Toggle spectrum display (true/false)
- **Show Waveform**: Toggle waveform display (true/false)

#### Audio Reactivity
- Spectrum bars show frequency content
- Waveform overlays time-domain data
- Independent color schemes for each layer
- Logarithmic scaling for better frequency response

### 🌊 Particle Field Visualizer
**ID**: `ParticleField`  
**Category**: AVS-Inspired  
**Description**: Advanced particle physics system with multiple movement modes and audio reactivity.

#### Specific Parameters (15 total)
- **Particle Count**: Number of particles (100-5000)
- **Particle Color**: Color of particles (#RRGGBB)
- **Trail Color**: Color of particle trails (#RRGGBB)
- **Particle Size**: Size of individual particles (0.5-10.0)
- **Trail Length**: Length of particle trails (0.0-1.0)
- **Attraction Strength**: Particle attraction force (-0.5-0.5)
- **Repulsion Strength**: Particle repulsion force (0.0-0.2)
- **Damping**: Velocity decay rate (0.9-0.999)
- **Max Velocity**: Maximum particle speed (1.0-20.0)
- **Enable Trails**: Show particle trails (true/false)
- **Enable Gravity**: Apply gravitational force (true/false)
- **Gravity Strength**: Gravity force strength (-0.5-0.5)
- **Movement Mode**: Force calculation method (Attract/Repulse/Orbit/Random/Audio)
- **Bass Multiplier**: Low frequency response (0.0-5.0)
- **Mid Multiplier**: Mid frequency response (0.0-5.0)
- **Treble Multiplier**: High frequency response (0.0-5.0)

#### Movement Modes
- **Attract**: Particles drawn to center point
- **Repulse**: Particles pushed away from center
- **Orbit**: Orbital motion around center
- **Random**: Organic, flowing movement
- **Audio**: Complex multi-band audio reactivity

#### Audio Reactivity
- Multi-band frequency analysis
- Real-time force calculation
- Particle physics simulation
- Audio-reactive color shifting

---

## Usage Guide

### Parameter Categories
Parameters are organized into logical categories for easy navigation:
- **Global**: Universal parameters affecting all visualizers
- **Specific**: Unique parameters for each visualizer
- **Audio**: Audio processing and reactivity settings
- **Visual**: Appearance and rendering options
- **Physics**: Movement and interaction parameters

### Performance Considerations
- Higher particle counts impact performance
- Trail effects require additional rendering
- Complex movement modes may reduce frame rate
- Audio reactivity adds CPU overhead

### Best Practices
1. **Start Simple**: Begin with basic visualizers and add complexity gradually
2. **Use Global Parameters**: Leverage universal controls for consistent behavior
3. **Audio Tuning**: Adjust sensitivity and multipliers for optimal reactivity
4. **Performance Balance**: Find the right balance between visual quality and performance
5. **Save Presets**: Use the preset system to save your favorite configurations

### Troubleshooting
- **Low Performance**: Reduce particle count or disable trails
- **No Audio Reactivity**: Check audio sensitivity and multipliers
- **Visual Glitches**: Verify parameter ranges and global settings
- **Memory Issues**: Reduce particle counts for lower-end systems

---

## Technical Specifications

### Rendering
- **Resolution**: Adaptive to display size
- **Frame Rate**: Target 60 FPS
- **Color Depth**: 32-bit RGBA
- **Blending**: Alpha blending for transparency effects

### Audio Processing
- **Sample Rate**: Matches system audio
- **FFT Size**: 2048-8192 samples (adaptive)
- **Frequency Bands**: Bass (20-250Hz), Mid (250-4000Hz), Treble (4000Hz+)
- **Beat Detection**: Energy-based algorithm

### Memory Usage
- **Base Memory**: ~50MB for core system
- **Per Visualizer**: 1-10MB depending on complexity
- **Particle Systems**: ~100KB per 1000 particles
- **Audio Buffers**: ~1MB for real-time processing

### Platform Support
- **Windows**: Full support with hardware acceleration
- **Linux**: Full support via .NET runtime
- **macOS**: Full support via .NET runtime
- **Cross-Platform**: Consistent behavior across all platforms
