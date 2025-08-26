# 🚀 Phoenix Visualizer

**Cross-platform Avalonia visualizer studio** with a native C# AVS-compatible runtime at its core. Features a Phoenix visualizer plugin, comprehensive plugin management UI, and support for AVS-style presets, APE-style effects, and managed plugins. Each track gets one primary vibe (genre-driven), nuanced by BPM, energy, and frequency bands, with real-world frequency-to-visible-color fallback when genre is missing.

## ✨ Latest Features (v2.0)

- **🎵 VLC Audio Integration**: Real-time audio playback with actual FFT and waveform data ✅
- **🎭 Superscopes System**: 11 AVS-based mathematical visualizations with audio response
- **✏️ AVS Editor**: Full-featured editor for creating and editing AVS presets with real-time preview
- **🎯 Plugin Management UI**: Complete plugin manager in Settings window
- **🎨 Native AVS Engine**: Pure C# implementation of Advanced Visualization Studio
- **⚡ Enhanced Audio System**: VLC-based audio with real-time analysis and processing
- **🎨 Advanced Visualizations**: Waveform, FFT, Bars, Energy with real audio data
- **🚀 Easy Launcher System**: Double-click `run.bat` or use `phoenix` alias
- **📁 Organized Plugin Structure**: Clean directories for plugins, presets, and effects

## ✨ Features

### 🎵 Audio & Analysis
- **VLC Audio Engine**: Real-time audio playback with actual FFT and waveform data ✅
- **Music Playback**: Open file, Play/Pause, Stop, Seek, Volume (MP3, WAV, FLAC, OGG, M4A)
- **Real-time Analysis**: FFT (1024/2048), BPM detection, energy/peaks, RMS
- **Advanced Processing**: Input gain, smoothing, noise gate, beat sensitivity
- **Audio Recovery**: Automatic stream corruption detection and recovery
- **Thread-safe Processing**: Lock-free audio data reading with automatic fallbacks

### 🎨 Visualizations
- **Waveform Visualizer**: Real-time time-domain waveform display
- **FFT Spectrum**: Frequency-domain analysis with configurable scaling
- **Bars Visualizer**: Dynamic spectrum bars with fallback patterns
- **Energy Visualizer**: RMS-based energy display with smooth animations
- **Fallback Patterns**: Automatic detection and recovery from stuck data
- **🎭 Superscopes**: 11 AVS-based mathematical visualizations with audio response
  - Spiral, 3D Scope Dish, Rotating Bow, Bouncing Scope, Spiral Graph
  - Rainbow Merkaba, Cat Face, Cymatics Frequency, Pong Simulation
  - Butterfly, Rainbow Sphere Grid with rainbow color cycling
- **✏️ AVS Editor**: Full-featured editor for creating and editing AVS presets
  - Real-time preview and validation
  - Import/export functionality
  - Seamless integration with main application

### 🔌 Plugin System
- **Winamp Compatibility**: Direct loading of Winamp visualizer DLLs
- **AVS Presets**: Advanced Visualization Studio preset support
- **APE Effects**: Advanced Plugin Extension effect system
- **Managed Plugins**: .NET-based visualizer plugin architecture
- **NS-EEL Evaluator**: Winamp AVS-style expression evaluation

### 🎯 Plugin Management
- **Settings Integration**: Complete plugin manager in Settings window
- **Plugin Registry**: Runtime discovery and management of all plugins
- **Enable/Disable**: Individual plugin control with status tracking
- **Configuration**: Plugin-specific settings and options
- **Testing Tools**: Built-in plugin testing and validation

### 🚀 User Experience
- **Easy Launcher**: Double-click `run.bat` or use `phoenix` alias
- **Cross-platform**: Avalonia-based UI for Windows, macOS, and Linux
- **Responsive Design**: Modern, intuitive interface with proper spacing
- **Error Handling**: Comprehensive error reporting and recovery
- **Documentation**: Complete guides and troubleshooting information

## Color and Vibe Logic

- One primary vibe per track (keeps the experience focused and code simple)
- Genre → base palette and animation style (examples):
  - Blues/Jazz: deep blues; smooth, flowing
  - Bluegrass: sky/light blue; lively, bouncy
  - Classical: gold/yellow; elegant, graceful
  - Metal: purple/deep red; sharp, aggressive
  - Love/Trance: pink/gold; gentle, spiraling
  - Hip hop/Rap: silver/green; rippling, rhythmic
  - Pop: orange/bright yellow; peppy, energetic
  - Electronic: neon; strobing, fast
- Frequency bands influence details within the vibe:
  - Bass (20–250 Hz) → body glow/flame intensity
  - Mid (250–2000 Hz) → aura/eyes
  - Treble (2–20 kHz) → feather tips/tail sparkles

### Spectrum-to-Color Fallback (real-world mapping)

If genre is unavailable/ambiguous, compute a weighted color from the spectrum using approximate frequency→visible color mapping:

- 20–250 Hz → reds/oranges
- 250–2000 Hz → yellows/greens
- 2000–20000 Hz → blues/violets

This mapping also colors the spectrum visualizer so users can “see the music.”

## 🔌 Plugin Management

### Plugin Manager UI
Access the comprehensive plugin management system through **Settings → Plugin Manager**:

- **📋 Plugin List**: View all available plugins with enable/disable checkboxes
- **⚙️ Plugin Details**: See plugin information, status, and configuration options
- **🔧 Action Buttons**: Configure, test, and get info about each plugin
- **📦 Installation**: Browse and install new plugins (.dll files)

### Supported Plugin Types
- **Winamp Visualizers**: Direct loading of Winamp visualizer DLLs
- **AVS Presets**: Advanced Visualization Studio preset files
- **APE Effects**: Advanced Plugin Extension effects
- **Managed Plugins**: .NET-based visualizer plugins

### Plugin Directory Structure
```
plugins/
├── vis/           # Winamp visualizer DLLs
├── ape/           # APE effect files
presets/
├── avs/           # AVS preset files and bitmaps
└── milkdrop/      # MilkDrop preset files
```

## 🏗️ Project Structure

- `PhoenixVisualizer.App` — Avalonia UI host app with plugin management
- `PhoenixVisualizer.Core` — config, models, genre/vibe mapping, utilities
- `PhoenixVisualizer.Audio` — enhanced playback + analysis (ManagedBass/BPM/FFT)
- `PhoenixVisualizer.Visuals` — advanced visualizations (Waveform, FFT, Bars, Energy, Superscopes)
- `PhoenixVisualizer.PluginHost` — comprehensive plugin interfaces and `AudioFeatures`
- `PhoenixVisualizer.ApeHost` — managed APE-style host interfaces
- `PhoenixVisualizer.AvsEngine` — AVS runtime (Superscope-first), Skia renderer
- `PhoenixVisualizer.Plugins.Avs` — vis_AVS plugin that wraps the AVS engine
- `PhoenixVisualizer.Plugins.Ape.Phoenix` — Phoenix visual as an APE-style plugin
- `PhoenixVisualizer.Plots` — Matplotlib-inspired plotting primitives
- `PhoenixVisualizer.Editor` — Avalonia-based visualization editor UI
- `libs_etc/WAMPSDK` — Winamp SDK materials for plugin compatibility
- `Directory.Build.props` — sets `WinampSdkDir` relative to this folder

## Tech Stack

- .NET 8, Avalonia 11
- NAudio for playback and audio processing
- SkiaSharp for custom 2D drawing
- Newtonsoft.Json for config (Core)

## 🚀 Quick Start

### Option 1: Double-click Launcher (Easiest)
```
run.bat  ← Just double-click this!
```

### Option 2: PowerShell Aliases (Most Convenient)
```powershell
. .\run-phoenix.ps1  # Load once
phoenix              # Run main app
phoenix-editor       # Run editor
```

### Option 3: Direct Commands
```bash
# From solution root:
dotnet run --project PhoenixVisualizer.App

# From project directory:
cd PhoenixVisualizer.App
dotnet run
```

## 🔨 Build

```bash
dotnet build PhoenixVisualizer.sln
```

## 📚 Documentation

- **🚀 RUNNING.md** - Complete guide to running PhoenixVisualizer
- **📚 PROJECT_PHOENIX_PLAN.md** - Project architecture and roadmap
- **🎭 SUPERSCOPES_IMPLEMENTATION.md** - Complete superscopes guide and reference
- **📋 TODO.md** - Development roadmap and progress tracking
- **📊 PHOENIX_VISUALIZER_STATUS.md** - Comprehensive project status report

## 🔧 Prerequisites

**None required** - The app uses ManagedBass which has full .NET 8 support and no external dependencies.

## 🗺️ Development Roadmap

### ✅ Completed (Phase 1-6)
- **🎵 Audio System**: Complete audio playback and analysis with corruption recovery
- **🎨 Visualizations**: Waveform, FFT, Bars, Energy visualizers with fallback patterns
- **🎭 Superscopes**: 11 AVS-based mathematical visualizations with audio response
- **✏️ AVS Editor**: Full-featured editor with real-time preview and seamless integration
- **🔌 Plugin Infrastructure**: Comprehensive plugin interfaces and registry system
- **⚡ Phoenix Integration**: Native C# AVS engine and Phoenix scripting
- **🎯 Plugin Management UI**: Complete settings-based plugin manager
- **🚀 Launcher System**: Easy-to-use batch files and PowerShell aliases

### 🔄 In Progress (Phase 7-8)
- **🎛️ Plugin Management UI**: Enhanced configuration dialogs and testing
- **⚡ Performance Optimization**: GPU acceleration and plugin caching
- **📚 Documentation**: Complete API reference and development guides

### 🚧 Planned Features
- **🎨 Advanced NS-EEL**: Custom function definitions and debugging tools
- **🌐 Plugin Distribution**: Plugin marketplace and automatic updates
- **🎭 Preset Management**: Advanced preset organization and sharing
- **📱 Mobile Support**: Cross-platform mobile visualizer app

## 🔧 Troubleshooting

### Common Issues
- **Build errors**: Ensure you're using .NET 8 SDK
- **Plugin loading**: Check that DLLs are in the correct `plugins/` directories
- **Audio issues**: Verify BASS audio library is properly installed
- **Performance**: Adjust FFT size and smoothing settings in Settings

### Getting Help
- Check `RUNNING.md` for launcher issues
- Review `PROJECT_PHOENIX_PLAN.md` for project status
- Consult `TODO.md` for development status
- Check `PHOENIX_VISUALIZER_STATUS.md` for comprehensive project info

## 📝 Notes

- **Platform Support**: Windows development confirmed with .NET SDK 8.x
- **Project Structure**: All project assets and SDK materials live under `PhoenixVisualizer/`
- **Documentation**: Comprehensive guides available in root directory
- **Plugin Support**: Full Winamp compatibility with organized directory structure


