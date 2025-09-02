# AVS Effect Nodes Guide

## Overview

Phoenix Visualizer includes a comprehensive collection of AVS (Advanced Visualization Studio) effect nodes that have been natively re-implemented in C#. These nodes provide the core visual effects that power the visualization system, offering both classic AVS functionality and modern enhancements.

## Core Architecture

### Effect Node System
- **BaseEffectNode**: Abstract base class for all effects
- **Graph-based Processing**: Effects can be chained together
- **Parameter System**: Each node has its own parameters
- **Thread-Safe**: All effects are designed for concurrent processing
- **Memory Efficient**: Optimized for real-time rendering

### Processing Pipeline
```
Audio Input → Effect Node 1 → Effect Node 2 → ... → Output
```

## Available Effect Nodes

### 🎨 Render Effects

#### Simple Render Node
**Class**: `SimpleRenderNode`  
**Category**: Render  
**Description**: Basic rendering node that outputs solid colors or gradients.

**Parameters (6):**
- **Enabled**: Enable/disable the effect (true/false)
- **Render Mode**: Rendering method (Solid/Gradient/Radial)
- **Primary Color**: Main color (#RRGGBB)
- **Secondary Color**: Secondary color for gradients (#RRGGBB)
- **Blend Mode**: How to blend with existing content (Normal/Add/Multiply)
- **Opacity**: Transparency level (0.0-1.0)

#### Superscope Render Node
**Class**: `SuperscopeRenderNode`  
**Category**: Render  
**Description**: Advanced mathematical shape renderer based on AVS Superscope.

**Parameters (12):**
- **Enabled**: Enable/disable the effect (true/false)
- **Point Count**: Number of points to render (2-10000)
- **Line Width**: Thickness of connecting lines (0.1-10.0)
- **Color**: Line color (#RRGGBB)
- **Blend Mode**: Blending method (Normal/Add/Multiply/Subtract)
- **Init Code**: Initialization code (string)
- **Frame Code**: Per-frame code (string)
- **Point Code**: Per-point calculation code (string)
- **Audio Channel**: Audio channel to use (Left/Right/Both)
- **Draw Mode**: How to connect points (Lines/Dots)
- **Buffer Mode**: Enable buffer for trails (true/false)
- **Clear Every Frame**: Clear buffer each frame (true/false)

### 🎵 Audio Processing Effects

#### BPM Detection Node
**Class**: `BPMEffectsNode`  
**Category**: Audio  
**Description**: Real-time BPM detection and beat analysis with adaptive algorithms.

**Parameters (8):**
- **Enabled**: Enable/disable BPM detection (true/false)
- **Sensitivity**: Beat detection sensitivity (0.1-2.0)
- **Adaptive BPM**: Use adaptive BPM calculation (true/false)
- **BPM Smoothing**: BPM value smoothing factor (0.1-0.9)
- **Beat Threshold**: Minimum beat strength (0.0-1.0)
- **Energy Window**: Energy analysis window size (10-100)
- **BPM Range Min**: Minimum BPM to detect (60-200)
- **BPM Range Max**: Maximum BPM to detect (60-200)

#### Audio Spectrum Node
**Class**: `AudioSpectrumNode`  
**Category**: Audio  
**Description**: Real-time frequency spectrum analysis with customizable bands.

**Parameters (10):**
- **Enabled**: Enable/disable spectrum analysis (true/false)
- **FFT Size**: FFT window size (512/1024/2048/4096)
- **Frequency Bands**: Number of frequency bands (8-64)
- **Min Frequency**: Lowest frequency to analyze (20-20000)
- **Max Frequency**: Highest frequency to analyze (20-20000)
- **Smoothing**: Spectrum smoothing factor (0.0-0.99)
- **Scale**: Output scaling factor (0.1-10.0)
- **Normalize**: Normalize spectrum values (true/false)
- **Log Scale**: Use logarithmic frequency scaling (true/false)
- **Window Type**: FFT window function (Hann/Hamming/Blackman)

### 🎭 Filter Effects

#### Blur Effects Node
**Class**: `BlurEffectsNode`  
**Category**: Filter  
**Description**: Advanced blur effects with multiple algorithms and edge preservation.

**Parameters (12):**
- **Enabled**: Enable/disable blur effect (true/false)
- **Blur Type**: Blur algorithm (Gaussian/Box/Motion/Radial)
- **Blur Radius**: Blur intensity (0.5-50.0)
- **Blur Quality**: Quality vs speed tradeoff (1-10)
- **Edge Preservation**: Preserve edges during blur (0.0-1.0)
- **Motion Angle**: Motion blur direction (0-360)
- **Motion Length**: Motion blur distance (1-100)
- **Radial Center X**: Radial blur center X (0.0-1.0)
- **Radial Center Y**: Radial blur center Y (0.0-1.0)
- **Blend Mode**: How to blend blurred result (Normal/Add/Multiply)
- **Opacity**: Blur layer opacity (0.0-1.0)
- **Mask Influence**: Edge mask strength (0.0-1.0)

#### Brightness/Contrast Node
**Class**: `BrightnessContrastNode`  
**Category**: Filter  
**Description**: Advanced brightness and contrast adjustments with color correction.

**Parameters (10):**
- **Enabled**: Enable/disable effect (true/false)
- **Brightness**: Brightness adjustment (-1.0 to 1.0)
- **Contrast**: Contrast adjustment (0.0-2.0)
- **Saturation**: Color saturation (0.0-2.0)
- **Hue Shift**: Color hue shift (-180 to 180)
- **Gamma**: Gamma correction (0.1-3.0)
- **Lift**: Shadow lift adjustment (-0.5 to 0.5)
- **Gain**: Highlight gain adjustment (0.5-2.0)
- **Color Temperature**: Color temperature in Kelvin (2000-12000)
- **Vibrance**: Smart saturation adjustment (-1.0 to 1.0)

### 🎪 Distortion Effects

#### Water Ripple Node
**Class**: `WaterRippleNode`  
**Category**: Distortion  
**Description**: Realistic water ripple simulation with physics-based wave propagation.

**Parameters (15):**
- **Enabled**: Enable/disable water effect (true/false)
- **Wave Speed**: Speed of wave propagation (0.1-5.0)
- **Wave Amplitude**: Height of waves (0.1-2.0)
- **Wave Frequency**: Wave frequency multiplier (0.1-5.0)
- **Damping**: Wave energy loss over time (0.8-0.999)
- **Ripple Count**: Number of simultaneous ripples (1-20)
- **Ripple Radius**: Size of ripple effect (10-500)
- **Ripple Strength**: Initial ripple strength (0.1-2.0)
- **Refraction**: Light refraction through water (0.0-1.0)
- **Reflection**: Water surface reflection (0.0-1.0)
- **Caustics**: Underwater light patterns (true/false)
- **Foam**: Surface foam effect (true/false)
- **Depth**: Water depth simulation (0.1-10.0)
- **Color**: Water color tint (#RRGGBBAA)
- **Blend Mode**: Water blending method (Normal/Add/Multiply)

#### Kaleidoscope Node
**Class**: `KaleidoscopeNode`  
**Category**: Distortion  
**Description**: Mathematical kaleidoscope effect with rotational symmetry.

**Parameters (12):**
- **Enabled**: Enable/disable kaleidoscope (true/false)
- **Segments**: Number of symmetry segments (3-32)
- **Rotation Speed**: Automatic rotation speed (-5.0 to 5.0)
- **Rotation Angle**: Manual rotation angle (0-360)
- **Center X**: Effect center X position (0.0-1.0)
- **Center Y**: Effect center Y position (0.0-1.0)
- **Scale**: Zoom level (0.1-3.0)
- **Mirror Type**: Symmetry mirroring type (Reflect/Rotate/Both)
- **Blend Mode**: Kaleidoscope blending (Normal/Add/Multiply)
- **Edge Mode**: How to handle edges (Wrap/Clamp/Reflect)
- **Color Shift**: Color shifting per segment (0.0-360.0)
- **Depth**: 3D depth effect (0.0-1.0)

### 🎪 Advanced Transitions

#### Video Delay Node
**Class**: `VideoDelayEffectsNode`  
**Category**: Temporal  
**Description**: Frame buffering with beat-reactive delay effects and motion blur.

**Parameters (18):**
- **Enabled**: Enable/disable delay effect (true/false)
- **Use Beats**: Synchronize with beat detection (true/false)
- **Delay**: Base delay in frames (1-60)
- **Beat Reactive**: Enable beat-reactive delay (true/false)
- **Beat Delay Multiplier**: Beat influence on delay (0.0-2.0)
- **Enable Delay Animation**: Animate delay over time (true/false)
- **Animation Speed**: Delay animation speed (0.1-5.0)
- **Animation Mode**: Delay animation pattern (0-5)
- **Enable Delay Masking**: Use mask for selective delay (true/false)
- **Mask Influence**: Mask strength (0.0-1.0)
- **Enable Delay Blending**: Blend multiple delay frames (true/false)
- **Delay Blend Strength**: Blend intensity (0.0-1.0)
- **Delay Algorithm**: Frame interpolation method (0-2)
- **Delay Curve**: Delay response curve (0.1-3.0)
- **Enable Delay Clamping**: Clamp delay values (true/false)
- **Clamp Mode**: Delay clamping method (0-2)
- **Enable Delay Inversion**: Invert delay effect (true/false)
- **Inversion Threshold**: Inversion trigger level (0.0-1.0)

#### Advanced Transitions Node
**Class**: `AdvancedTransitionsEffectsNode`  
**Category**: Spatial  
**Description**: Coordinate transformation system with 24 built-in effects.

**Parameters (8):**
- **Enabled**: Enable/disable transition effect (true/false)
- **Effect Type**: Built-in effect selection (0-23)
- **Enable Blending**: Blend with original image (true/false)
- **Source Mapping Mode**: Coordinate mapping method (0-2)
- **Use Rectangular Coordinates**: Use rect vs polar coords (true/false)
- **Enable Subpixel Precision**: High-quality interpolation (true/false)
- **Enable Coordinate Wrapping**: Wrap coordinates at edges (true/false)
- **Custom Expression**: Custom transformation code (string)

### 🎨 Color Effects

#### Color Correction Node
**Class**: `ColorCorrectionNode`  
**Category**: Color  
**Description**: Professional color grading with curves, levels, and color spaces.

**Parameters (16):**
- **Enabled**: Enable/disable color correction (true/false)
- **Master Brightness**: Overall brightness (-1.0 to 1.0)
- **Master Contrast**: Overall contrast (0.0-2.0)
- **Master Saturation**: Overall saturation (0.0-2.0)
- **Shadows**: Shadow adjustment (-1.0 to 1.0)
- **Midtones**: Midtone adjustment (-1.0 to 1.0)
- **Highlights**: Highlight adjustment (-1.0 to 1.0)
- **Red Balance**: Red channel adjustment (-1.0 to 1.0)
- **Green Balance**: Green channel adjustment (-1.0 to 1.0)
- **Blue Balance**: Blue channel adjustment (-1.0 to 1.0)
- **Hue Shift**: Color hue rotation (-180 to 180)
- **Color Temperature**: White balance in Kelvin (2000-12000)
- **Vibrance**: Smart saturation (0.0-2.0)
- **Clarity**: Local contrast enhancement (0.0-1.0)
- **Vignette**: Corner darkening effect (0.0-1.0)
- **Grain**: Film grain simulation (0.0-1.0)

#### Chromatic Aberration Node
**Class**: `ChromaticAberrationNode`  
**Category**: Color  
**Description**: Lens chromatic aberration simulation with customizable dispersion.

**Parameters (10):**
- **Enabled**: Enable/disable chromatic aberration (true/false)
- **Red Shift**: Red channel displacement (0.0-10.0)
- **Green Shift**: Green channel displacement (0.0-10.0)
- **Blue Shift**: Blue channel displacement (0.0-10.0)
- **Shift Direction**: Aberration direction in degrees (0-360)
- **Center X**: Effect center X (0.0-1.0)
- **Center Y**: Effect center Y (0.0-1.0)
- **Falloff**: Aberration strength falloff (0.0-1.0)
- **Blend Mode**: Aberration blending method (Normal/Add/Multiply)
- **Intensity**: Overall effect intensity (0.0-1.0)

### 🎪 Particle Effects

#### Particle System Node
**Class**: `ParticleSystemNode`  
**Category**: Particles  
**Description**: Complete particle system with emitters, forces, and rendering.

**Parameters (20):**
- **Enabled**: Enable/disable particle system (true/false)
- **Particle Count**: Maximum particles (100-10000)
- **Emitter Count**: Number of particle emitters (1-10)
- **Emitter X**: Emitter position X (0.0-1.0)
- **Emitter Y**: Emitter position Y (0.0-1.0)
- **Emission Rate**: Particles per second (10-1000)
- **Particle Lifetime**: How long particles live (0.1-10.0)
- **Initial Speed**: Starting particle speed (0.0-100.0)
- **Initial Size**: Starting particle size (0.1-10.0)
- **Gravity X**: Horizontal gravity (-10.0 to 10.0)
- **Gravity Y**: Vertical gravity (-10.0 to 10.0)
- **Air Resistance**: Speed decay factor (0.95-0.999)
- **Color Start**: Initial particle color (#RRGGBBAA)
- **Color End**: Final particle color (#RRGGBBAA)
- **Size Over Time**: Size change over lifetime (0.0-2.0)
- **Blend Mode**: Particle blending method (Normal/Add/Multiply)
- **Trail Length**: Particle trail length (0.0-1.0)
- **Random Seed**: Randomization seed (0-999999)
- **Audio Reactivity**: Audio influence strength (0.0-2.0)

#### Particle Forces Node
**Class**: `ParticleForcesNode`  
**Category**: Particles  
**Description**: Force field system for particle manipulation with attractors and deflectors.

**Parameters (15):**
- **Enabled**: Enable/disable force fields (true/false)
- **Attractor Count**: Number of attractors (0-10)
- **Deflector Count**: Number of deflectors (0-10)
- **Vortex Count**: Number of vortex fields (0-10)
- **Force Strength**: Overall force multiplier (0.1-10.0)
- **Force Range**: Effective range of forces (10-500)
- **Attractor Strength**: Attractor pull strength (0.1-5.0)
- **Deflector Strength**: Deflector push strength (0.1-5.0)
- **Vortex Strength**: Vortex spin strength (0.1-5.0)
- **Force Falloff**: Force strength over distance (0.1-2.0)
- **Position X**: Force field center X (0.0-1.0)
- **Position Y**: Force field center Y (0.0-1.0)
- **Animation Speed**: Force field movement speed (0.0-5.0)
- **Random Forces**: Add random force variation (0.0-1.0)
- **Audio Influence**: Audio-reactive force modulation (0.0-2.0)

### 🎵 Audio Visualization Effects

#### Spectrum Analyzer Node
**Class**: `SpectrumAnalyzerNode`  
**Category**: Audio Visualization  
**Description**: Advanced spectrum analyzer with multiple visualization modes.

**Parameters (14):**
- **Enabled**: Enable/disable spectrum analyzer (true/false)
- **Bar Count**: Number of frequency bars (8-128)
- **Bar Style**: Bar rendering style (Solid/Gradient/Outline)
- **Bar Width**: Width of each bar (0.1-1.0)
- **Bar Spacing**: Space between bars (0.0-0.5)
- **Height Scale**: Vertical scaling factor (0.1-3.0)
- **Color Mode**: Bar coloring method (Solid/Gradient/Spectrum)
- **Gradient Start**: Start color for gradients (#RRGGBB)
- **Gradient End**: End color for gradients (#RRGGBB)
- **Peak Detection**: Show frequency peaks (true/false)
- **Peak Decay**: Peak fade speed (0.8-0.99)
- **Smoothing**: Spectrum smoothing factor (0.0-0.9)
- **Mirror Mode**: Horizontal mirroring (true/false)
- **Log Scale**: Logarithmic frequency scaling (true/false)

#### Waveform Renderer Node
**Class**: `WaveformRendererNode`  
**Category**: Audio Visualization  
**Description**: Time-domain waveform visualization with multiple rendering modes.

**Parameters (12):**
- **Enabled**: Enable/disable waveform renderer (true/false)
- **Sample Count**: Number of waveform samples (100-2000)
- **Line Thickness**: Waveform line width (0.5-10.0)
- **Line Style**: Waveform rendering style (Solid/Dashed/Dotted)
- **Color Mode**: Waveform coloring (Solid/Gradient/Audio)
- **Gradient Start**: Start color for gradients (#RRGGBB)
- **Gradient End**: End color for gradients (#RRGGBB)
- **Vertical Scale**: Waveform amplitude scaling (0.1-2.0)
- **Center Position**: Vertical center position (0.0-1.0)
- **Smoothing**: Waveform smoothing factor (0.0-0.9)
- **Fill Mode**: Fill area under waveform (None/Solid/Gradient)
- **Antialiasing**: Enable line smoothing (true/false)

---

## Effect Node Categories

### 🎨 Render Nodes
Basic rendering and shape generation effects.

### 🎵 Audio Nodes
Audio analysis and processing effects.

### 🎭 Filter Nodes
Image filtering and color correction effects.

### 🎪 Distortion Nodes
Geometric distortion and transformation effects.

### 🎪 Temporal Nodes
Time-based effects using frame buffering.

### 🎨 Color Nodes
Color manipulation and correction effects.

### 🎪 Particle Nodes
Particle system and force field effects.

### 🎵 Audio Visualization Nodes
Direct audio waveform and spectrum visualization.

---

## Usage Guide

### Creating Effect Chains
1. **Start with Audio**: Begin with audio analysis nodes
2. **Add Filters**: Apply color correction and filters
3. **Apply Distortion**: Add geometric transformations
4. **Layer Effects**: Combine multiple effects for complex results
5. **Final Rendering**: End with render nodes for output

### Parameter Optimization
- **Start Conservative**: Begin with moderate parameter values
- **Test Incrementally**: Change one parameter at a time
- **Monitor Performance**: Watch for frame rate drops
- **Save Configurations**: Use presets for complex setups

### Performance Considerations
- **Particle Effects**: High particle counts impact performance
- **Complex Filters**: Gaussian blur and advanced effects are expensive
- **Audio Analysis**: Real-time FFT processing adds CPU overhead
- **Temporal Effects**: Frame buffering requires additional memory

### Best Practices
1. **Layer Effects Logically**: Group related effects together
2. **Use Appropriate Blend Modes**: Choose blending that enhances your effect
3. **Balance Quality vs Speed**: Adjust quality settings for target performance
4. **Test on Target Hardware**: Performance varies by system capabilities
5. **Document Your Chains**: Save presets with descriptive names

### Troubleshooting
- **No Effect Visible**: Check if node is enabled and has valid parameters
- **Performance Issues**: Reduce particle counts or disable expensive effects
- **Audio Not Working**: Verify audio source and check node connections
- **Memory Errors**: Reduce buffer sizes or particle counts
- **Visual Artifacts**: Check parameter ranges and blend mode compatibility

---

## Technical Specifications

### Processing Architecture
- **Multi-threaded**: Effects can process in parallel
- **Memory Management**: Automatic buffer allocation and cleanup
- **Error Handling**: Graceful failure with fallback rendering
- **Parameter Validation**: Automatic range checking and clamping

### Supported Formats
- **Input**: 32-bit RGBA frame buffers
- **Audio**: 16-bit PCM, various sample rates
- **Output**: 32-bit RGBA with alpha blending

### Performance Metrics
- **Target Frame Rate**: 60 FPS on modern hardware
- **Memory Usage**: 10-100MB depending on complexity
- **CPU Usage**: 5-50% depending on effect count and complexity

### Platform Compatibility
- **Windows**: Full DirectX/OpenGL acceleration support
- **Linux**: OpenGL acceleration via Mesa
- **macOS**: Metal/OpenGL acceleration
- **Cross-platform**: Consistent behavior across all platforms
