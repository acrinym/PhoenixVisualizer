# PhoenixVisualizer TODO - Current State Audit

## 🎯 **CURRENT STATUS: PHASE 1-3 COMPLETE, PHASE 4 IN PROGRESS**

The PhoenixVisualizer is in a much more advanced state than previously documented. Most core functionality is working.

## ✅ **COMPLETED FEATURES (Phases 1-3)**

### Phase 1 – Core AVS and Host Wiring ✅ **COMPLETE**
- ✅ **Engine**: Superscope subset (per-frame/per-point vars, math, conditionals)
- ✅ **Audio feed**: FFT (1024/2048), BPM, energy, beat detection
- ✅ **Renderer**: Skia lines/points, clear/fade operations
- ✅ **vis_AVS plugin**: Wraps engine and drives frames from host
- ✅ **App**: Basic transport UI + spectrum panel + settings

### Phase 2 – Editor + Plots ✅ **COMPLETE**
- ✅ **Editor UI**: Preset browser, canvas viewport, properties panel
- ✅ **Plots library**: LineSeries, Polar/Wheel, Bar/Stem
- ✅ **Colormaps**: viridis/plasma/magma/inferno + genre palettes
- ✅ **Designer nodes**: Sources (FFT/BPM), transforms, styles, compose

### Phase 3 – Phoenix Plugin ✅ **COMPLETE**
- ✅ **Phoenix plugin scaffold**: Reads AudioFeatures, minimal draw stub
- ✅ **Color/vibe mapping**: Genre primary, spectrum fallback
- ✅ **States**: idle/active/cocoon/burst (hooks: beat/quiet/drop)

## 🚧 **IN PROGRESS (Phase 4)**

### Phase 4 – Compatibility & Effects 🚧 **MOSTLY COMPLETE**
- ✅ **AVS**: Real Winamp superscope preset parsing (init:, per_frame:, per_point:, beat:)
- ✅ **Real Winamp superscope preset parsing**: Full format support
- ✅ **Preset import**: Loader for common text-based presets
- ✅ **Enhanced audio processing**: Gain, smoothing, noise gate, AGC
- ✅ **Random preset scheduler**: Musical structure-aware preset switching
- ✅ **Multiple visualization plugins**: 7 different visualizers working
- ✅ **Settings system**: Comprehensive audio/visualizer configuration
- 🔄 **APE host**: Managed APE interface (partially implemented)
- 🔄 **NS-EEL expression evaluator**: For superscope math (planned)

## 🆕 **NEW FEATURES NOT IN ORIGINAL TODO**

### Advanced Audio Processing
- ✅ **Input Gain Control**: -24dB to +24dB adjustment
- ✅ **Auto Gain Control (AGC)**: Keeps levels steady
- ✅ **Smoothing**: Configurable EMA over FFT magnitude
- ✅ **Noise Gate**: Configurable threshold for low-level noise
- ✅ **Beat Sensitivity**: Configurable energy multiple for beat detection
- ✅ **Frame Blending**: Visual frame interpolation

### Random Preset System
- ✅ **OnBeat Mode**: Switch presets on detected beats
- ✅ **Interval Mode**: Time-based preset switching
- ✅ **Stanza Mode**: Musical structure-aware switching (beats per bar, bars per stanza)
- ✅ **Cooldown System**: Prevents rapid preset switching
- ✅ **Silence Detection**: Optional preset switching during quiet periods

### Multiple Visualization Plugins
- ✅ **Simple Bars**: Basic spectrum bars
- ✅ **Spectrum Bars**: Enhanced frequency visualization
- ✅ **Waveform**: Time-domain audio display
- ✅ **Pulse Circle**: Beat-reactive circular visualization
- ✅ **Energy Ring**: Energy-based ring visualization
- ✅ **Sanity Check**: Bouncing line test visualizer
- ✅ **AVS Runtime**: Winamp-compatible preset system

### Settings & Configuration
- ✅ **Plugin Selection**: Choose between AVS and Phoenix
- ✅ **Audio Settings**: Sample rate, buffer size configuration
- ✅ **Visualizer Sensitivity**: Fine-tune all audio processing parameters
- ✅ **Hotkey Support**: Y/U/Space/R/Enter controls
- ✅ **Preset Management**: Import, load, save presets

## 🔧 **KNOWN ISSUES TO FIX**

### Critical Issues
- ❌ **Play Button Not Working**: Audio controls not responding (regression from recent changes)
- ❌ **Sanity Check Visualizer Failing**: Crashes or doesn't render properly

### Minor Issues
- ⚠️ **AudioService._ringIndex Warning**: Field assigned but never used (CS0414)
- ⚠️ **Stop/Pause Behavior**: Both controls currently pause (NAudio limitation)

## 🎯 **NEXT PRIORITIES (Phase 5+)**

### Phase 5 – Advanced AVS Features
- [ ] **NS-EEL Expression Evaluator**: Full Winamp superscope math support
- [ ] **More AVS Effects**: Blur, color operations, advanced transforms
- [ ] **Preset Browser**: Built-in preset management and categorization
- [ ] **Effect Chains**: Multiple effects in sequence

### Phase 6 – Performance & Optimization
- [ ] **GPU Acceleration**: Skia GPU rendering optimization
- [ ] **Memory Management**: Optimize FFT buffers and rendering
- [ ] **Cross-platform Testing**: Linux/macOS compatibility
- [ ] **Performance Profiling**: Identify bottlenecks

### Phase 7 – Advanced Features
- [ ] **Screensaver Mode**: Full-screen visualization
- [ ] **Video Export**: Record visualizations to video files
- [ ] **MIDI Integration**: External MIDI control
- [ ] **Network Streaming**: Remote visualization control

## 🧪 **TESTING STATUS**

### Working Features
- ✅ Audio playback and analysis
- ✅ Multiple visualization plugins
- ✅ Settings and configuration
- ✅ Preset loading and management
- ✅ Random preset scheduling
- ✅ AVS preset parsing

### Needs Testing
- 🔄 Phoenix plugin with different audio types
- 🔄 Settings persistence across app restarts
- 🔄 Hotkey functionality
- 🔄 Preset import from various formats

## 📝 **DEVELOPMENT NOTES**

- **Windows Development**: .NET 8 SDK confirmed working
- **Dependencies**: NAudio, SkiaSharp, Avalonia 11
- **Architecture**: Plugin-based with shared AudioFeatures interface
- **Performance**: 60+ FPS rendering with 2048-point FFT
- **Audio Formats**: MP3, WAV, FLAC, OGG supported

## 🚀 **IMMEDIATE ACTIONS NEEDED**

1. **Fix Play Button**: Restore audio control functionality
2. **Fix Sanity Check**: Resolve visualizer crash/rendering issues
3. **Clean Up Warnings**: Remove unused _ringIndex field
4. **Test Core Features**: Verify all working features still function
5. **Update Documentation**: Reflect current working state

---

*Last Updated: 2025-08-16 - Based on comprehensive codebase audit*
