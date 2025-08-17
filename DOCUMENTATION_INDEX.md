# 📚 PhoenixVisualizer Documentation Index

## 🎯 Overview

Welcome to the complete PhoenixVisualizer documentation! This index provides quick access to all available guides, tutorials, and reference materials.

## 🚀 Getting Started

### First Time Users
1. **[RUNNING.md](RUNNING.md)** - Complete guide to running PhoenixVisualizer
2. **[README.md](README.md)** - Project overview and quick start guide
3. **[LAUNCHER_SYSTEM.md](LAUNCHER_SYSTEM.md)** - Understanding the launcher system

### Quick Commands
```bash
# Double-click this file:
run.bat

# Or use PowerShell aliases:
. .\run-phoenix.ps1
phoenix
```

## 🔌 Plugin System

### Plugin Management
- **[PLUGIN_MANAGEMENT.md](PLUGIN_MANAGEMENT.md)** - Complete plugin management guide
- **[WINAMP_PLUGIN_SETUP.md](WINAMP_PLUGIN_SETUP.md)** - Winamp plugin integration

### Plugin Types Supported
- **Winamp Visualizers** - Direct .dll loading
- **AVS Presets** - Advanced Visualization Studio
- **APE Effects** - Advanced Plugin Extension
- **Managed Plugins** - .NET-based visualizers

## 🎨 Features & Capabilities

### Audio & Analysis
- **Music Playback** - MP3, WAV, FLAC support
- **Real-time Analysis** - FFT, BPM, energy detection
- **Advanced Processing** - Gain, smoothing, noise gate
- **Audio Recovery** - Automatic corruption detection

### Visualizations
- **Waveform** - Time-domain display
- **FFT Spectrum** - Frequency-domain analysis
- **Bars Visualizer** - Dynamic spectrum bars
- **Energy Visualizer** - RMS-based energy display

### Plugin Management UI
- **Settings Integration** - Complete plugin manager
- **Plugin Registry** - Runtime discovery and management
- **Configuration** - Plugin-specific settings
- **Testing Tools** - Built-in validation

## 🏗️ Development

### Project Structure
```
PhoenixVisualizer/
├── PhoenixVisualizer.App/      # Main executable
├── PhoenixVisualizer.Core/     # Core library
├── PhoenixVisualizer.Audio/    # Audio processing
├── PhoenixVisualizer.Visuals/  # Visualization system
├── PhoenixVisualizer.PluginHost/ # Plugin infrastructure
├── PhoenixVisualizer.ApeHost/  # APE effects
├── PhoenixVisualizer.AvsEngine/ # AVS runtime
├── PhoenixVisualizer.Plugins.*/ # Plugin implementations
└── PhoenixVisualizer.Editor/   # Visualization editor
```

### Build & Run
```bash
# Build the solution
dotnet build PhoenixVisualizer.sln

# Run with launcher
.\run.bat

# Run with PowerShell alias
phoenix

# Run directly
dotnet run --project PhoenixVisualizer.App
```

## 📋 Development Status

### ✅ Completed (Phase 1-6)
- **Audio System** - Complete with corruption recovery
- **Visualizations** - All core visualizers implemented
- **Plugin Infrastructure** - Comprehensive plugin system
- **Winamp Integration** - Direct plugin loading
- **Plugin Management UI** - Complete settings integration
- **Launcher System** - Easy-to-use launchers

### 🔄 In Progress (Phase 7-8)
- **Plugin Management UI** - Enhanced configuration dialogs
- **Performance Optimization** - GPU acceleration and caching
- **Documentation** - Complete API reference

### 🚧 Planned Features
- **Advanced NS-EEL** - Custom functions and debugging
- **Plugin Marketplace** - Centralized distribution
- **Mobile Support** - Cross-platform mobile app

## 🔧 Troubleshooting

### Common Issues

#### Launcher Problems
- **Check RUNNING.md** for launcher issues
- **Verify .NET installation** with `dotnet --version`
- **Check file permissions** for batch files

#### Plugin Issues
- **Review PLUGIN_MANAGEMENT.md** for plugin problems
- **Check directory structure** for correct plugin placement
- **Verify dependencies** for Winamp plugins

#### Build Problems
- **Ensure .NET 8 SDK** is installed
- **Run `dotnet restore`** to restore packages
- **Check project references** in solution file

### Getting Help
1. **Check relevant documentation** from this index
2. **Review TODO.md** for development status
3. **Check PHOENIX_VISUALIZER_STATUS.md** for comprehensive info
4. **Use built-in error reporting** in the application

## 📚 Documentation Files

| File | Purpose | Audience |
|------|---------|----------|
| **[README.md](README.md)** | Project overview and quick start | All users |
| **[RUNNING.md](RUNNING.md)** | How to run the application | End users |
| **[LAUNCHER_SYSTEM.md](LAUNCHER_SYSTEM.md)** | Launcher system explanation | Developers |
| **[PLUGIN_MANAGEMENT.md](PLUGIN_MANAGEMENT.md)** | Plugin system guide | Plugin developers |
| **[WINAMP_PLUGIN_SETUP.md](WINAMP_PLUGIN_SETUP.md)** | Winamp integration | Winamp users |
| **[TODO.md](TODO.md)** | Development roadmap | Developers |
| **[PHOENIX_VISUALIZER_STATUS.md](PHOENIX_VISUALIZER_STATUS.md)** | Project status report | All users |

## 🎯 User Workflows

### End User Workflow
1. **Download and install** PhoenixVisualizer
2. **Double-click `run.bat`** to start the application
3. **Open audio files** and enjoy visualizations
4. **Access Settings** to customize the experience
5. **Install plugins** through the Plugin Manager

### Developer Workflow
1. **Clone the repository** and open in your IDE
2. **Load the aliases** with `. .\run-phoenix.ps1`
3. **Use `phoenix` command** for development
4. **Build and test** with `dotnet build`
5. **Create plugins** using the provided interfaces

### Plugin Developer Workflow
1. **Review PLUGIN_MANAGEMENT.md** for API reference
2. **Implement IVisualizerPlugin** interface
3. **Test your plugin** with the built-in testing tools
4. **Package and distribute** your plugin
5. **Document usage** for end users

## 🔮 Future Documentation

### Planned Guides
- **API Reference** - Complete code documentation
- **Plugin Development Tutorial** - Step-by-step plugin creation
- **Performance Tuning Guide** - Optimization techniques
- **Deployment Guide** - Distribution and installation
- **Contributing Guide** - How to contribute to the project

### Integration Guides
- **VST Plugin Support** - VST integration documentation
- **Web Standards** - Web-based visualization plugins
- **Mobile Development** - Cross-platform mobile support
- **Cloud Integration** - Cloud-based plugin storage

## 📞 Support & Community

### Getting Help
- **Documentation**: Start with this index
- **Troubleshooting**: Check relevant guides
- **Development**: Review TODO.md for roadmap
- **Status**: Check PHOENIX_VISUALIZER_STATUS.md

### Contributing
- **Code**: Follow the development workflow
- **Documentation**: Improve and expand guides
- **Testing**: Test plugins and report issues
- **Feedback**: Provide user experience feedback

---

## 🎉 Quick Reference

### Essential Commands
```bash
# Run the application
.\run.bat                    # Double-click launcher
phoenix                      # PowerShell alias
dotnet run --project PhoenixVisualizer.App  # Direct command

# Build the solution
dotnet build PhoenixVisualizer.sln

# Load PowerShell aliases
. .\run-phoenix.ps1
```

### Key Directories
```
plugins/vis/           # Winamp visualizer DLLs
plugins/ape/           # APE effect files
presets/avs/           # AVS preset files
presets/milkdrop/      # MilkDrop presets
```

### Important Files
```
run.bat                # Windows launcher
run-phoenix.ps1        # PowerShell aliases
PhoenixVisualizer.sln  # Solution file
README.md              # Project overview
```

---

**Happy visualizing! 🎵✨**
