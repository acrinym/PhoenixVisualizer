# Phoenix Visualizer 🚀

A modern, cross-platform visual effects framework designed to replace and enhance traditional AVS (Advanced Visualization Studio) systems. Built with .NET 8 and Avalonia UI, Phoenix Visualizer provides a powerful, extensible platform for real-time audio visualization.

## 🏆 **CURRENT STATUS: PHASE 4 COMPLETE - Clean Build Achieved!**

**✅ CLEAN BUILD STATUS**: 0 Errors, 0 Warnings - Production Ready!
**🎯 Latest Achievement**: Professional PHX Editor with advanced blend modes, code compilation, and preset management.

## ✨ **NEW: Phoenix Visualization Editor - Complete!**

**🎨 Professional Visual Effects Editor with Drag & Drop Interface**

- **Visual Node-Based Editing**: Compose effects using an intuitive drag & drop interface
- **Live Real-Time Preview**: See your effects immediately with performance monitoring
- **Categorized Effect Nodes**: 50+ effects organized by type (Pattern, Color, Video, Audio, Utility)
- **Interactive Graph Canvas**: Grid-based layout with automatic snapping
- **Professional UI**: Modern, cross-platform interface built with Avalonia

### 🎯 **Key Features**

- **EffectsGraph System**: Complete graph-based effect composition
- **Real-Time Processing**: 60+ FPS for complex visualizations
- **Cross-Platform**: Runs on Windows, macOS, and Linux
- **Extensible Architecture**: Easy to add new effects and capabilities
- **Performance Optimized**: Efficient rendering and processing

## 🏗️ **Architecture**

### **Core Components**
- **PhoenixVisualizer.Core**: Core framework and EffectsGraph system
- **PhoenixVisualizer.Editor**: Professional visual effects editor
- **PhoenixVisualizer.Plugins.Avs**: AVS compatibility layer
- **PhoenixVisualizer.Audio**: Audio processing and analysis

### **EffectsGraph System**
The heart of Phoenix Visualizer, providing:
- **Node-Based Composition**: Visual programming for effects
- **Topological Sorting**: Automatic processing order determination
- **Cycle Detection**: Prevents infinite loops
- **Real-Time Validation**: Live graph integrity checking
- **Performance Monitoring**: FPS tracking and optimization

## 🚀 **Getting Started**

### **Prerequisites**
- .NET 8.0 SDK
- Windows 10+, macOS 10.15+, or Linux

### **Quick Start**
```bash
# Clone the repository
git clone https://github.com/yourusername/PhoenixVisualizer.git
cd PhoenixVisualizer

# 🔒 CRITICAL: Create immediate backup of current codebase
./create_codebase_export.sh

# Build the project
dotnet build PhoenixVisualizer.sln -c Release --verbosity minimal

# Run the editor
dotnet run --project PhoenixVisualizer.App
```

### 🚨 **CRITICAL: Codebase Backup Procedures**

**MANDATORY**: Always create a backup before making changes:

```bash
# Create complete codebase export with build status
./create_codebase_export.sh

# Windows PowerShell alternative:
# .\export_with_build_status.ps1

# Output: phoenix_visualizer_source_export_YYYY-MM-DD.txt
```

**WHY BACKUP IS CRITICAL:**
- Captures current working state before modifications
- Includes complete build status and error tracking
- Provides restore point for broken builds
- Documents codebase state for debugging

**Backup includes:**
- All C# source files and XAML files
- Complete documentation and project files
- Current build status and error details
- File manifest with line counts and categories

### **Using the EffectsGraph Editor**
1. **Open the Editor**: Launch PhoenixVisualizer.Editor
2. **Select Effects**: Choose from categorized effect nodes in the left panel
3. **Drag & Drop**: Drag effects to the canvas to create your composition
4. **Connect Nodes**: Click and drag between ports to create connections
5. **Live Preview**: See your effects in real-time in the preview area
6. **Play & Test**: Use the playback controls to test your visualization

## 🎨 **Available Effects**

### **Pattern Effects**
- Starfield, Particle Swarm, Oscilloscope, Vector Fields
- Rotating Star Patterns, Interference Patterns, God Rays

### **Color Effects**
- Color Fade, Contrast, Brightness, Color Reduction
- Color Replace, One Tone, Dynamic Shift

### **Video Effects**
- AVI Video, Blur, Blit, Composite, Mirror
- Picture, Text, Dot Font Rendering

### **Audio Effects**
- Beat Detection, BPM, Custom BPM
- Oscilloscope Ring, Time Domain Scope

### **Utility Effects**
- Clear Frame, Comment, Stack, Scatter
- Advanced Transitions, DDM Effects

## 🔧 **Development**

### **Adding New Effects**
1. Implement `IEffectNode` interface
2. Inherit from `BaseEffectNode`
3. Register with `EffectsGraphManager`
4. Add to appropriate category in the editor

### **Project Structure**
```
PhoenixVisualizer/
├── PhoenixVisualizer.Core/          # Core framework
├── PhoenixVisualizer.Editor/        # Visual editor
├── PhoenixVisualizer.Plugins.Avs/   # AVS compatibility
├── PhoenixVisualizer.Audio/         # Audio processing
└── EffectsGraphTestApp/             # Testing and demos
```

## 📊 **Performance**

- **Simple Graphs**: 1000+ FPS
- **Complex Compositions**: 60-120 FPS
- **Memory Usage**: <100MB for typical compositions
- **Startup Time**: <2 seconds

## 🤝 **Contributing**

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### **Areas for Contribution**
- New effect implementations
- UI improvements
- Performance optimizations
- Documentation updates
- Bug fixes and testing

## 📄 **License**

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 **Acknowledgments**

- **AVS Community**: For inspiration and effect algorithms
- **Avalonia Team**: For the excellent cross-platform UI framework
- **Contributors**: Everyone who has helped build Phoenix Visualizer

## 📞 **Support**

- **Issues**: [GitHub Issues](https://github.com/yourusername/PhoenixVisualizer/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/PhoenixVisualizer/discussions)
- **Documentation**: [Wiki](https://github.com/yourusername/PhoenixVisualizer/wiki)

---

**🎉 Phoenix Visualizer - Transforming Visual Effects Creation**

*Built with ❤️ using .NET 8 and Avalonia UI*


