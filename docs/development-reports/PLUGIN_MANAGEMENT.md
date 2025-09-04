# 🔌 Plugin Management Guide

## Overview

PhoenixVisualizer features a comprehensive plugin management system that allows you to discover, configure, and manage all types of visualizer plugins. The Plugin Manager is integrated into the Settings window and provides a user-friendly interface for plugin administration.

## 🎯 Accessing Plugin Manager

1. **Open Settings**: From the main application, go to **Settings**
2. **Navigate to Plugin Manager**: Find the **Plugin Manager** section
3. **Manage Plugins**: Use the interface to control all aspects of your plugins

## 📋 Plugin Manager Interface

### Main Components

#### 🔄 Plugin List
- **Available Plugins**: Shows all discovered plugins
- **Enable/Disable**: Checkboxes to control plugin status
- **Plugin Information**: Displays plugin ID and display name
- **Selection**: Click to select a plugin for detailed management

#### ⚙️ Plugin Details Panel
- **Plugin Name**: Full display name of the selected plugin
- **Description**: Detailed description of plugin functionality
- **Status**: Current enabled/disabled state
- **Action Buttons**: Configure, Test, and Info options

#### 🔧 Action Buttons
- **⚙️ Configure**: Open plugin-specific configuration dialog
- **▶️ Test**: Test plugin with sample audio data
- **ℹ️ Info**: Display detailed plugin information

#### 📦 Plugin Installation
- **📁 Browse**: Select plugin files from your system
- **📦 Install**: Install selected plugins to the system
- **Drop Zone**: Drag and drop .dll files directly

## 🔌 Supported Plugin Types

### Winamp Visualizers
- **Format**: `.dll` files
- **Location**: `plugins/vis/` directory
- **Compatibility**: Full Winamp visualizer plugin support
- **Features**: Direct loading, real-time rendering, configuration

### AVS Presets
- **Format**: `.avs` files
- **Location**: `presets/avs/` directory
- **Compatibility**: Advanced Visualization Studio preset format
- **Features**: Script-based visualizations, real-time editing

### APE Effects
- **Format**: `.ape` files
- **Location**: `plugins/ape/` directory
- **Compatibility**: Advanced Plugin Extension effects
- **Features**: Audio processing effects, real-time modification

### Managed Plugins
- **Format**: .NET assemblies
- **Location**: `plugins/managed/` directory
- **Compatibility**: Full .NET plugin architecture
- **Features**: High performance, native .NET integration

## 📁 Directory Structure

```
PhoenixVisualizer/
├── plugins/
│   ├── vis/           # Winamp visualizer DLLs
│   ├── ape/           # APE effect files
│   └── managed/       # .NET plugin assemblies
├── presets/
│   ├── avs/           # AVS preset files
│   │   ├── *.avs      # Preset scripts
│   │   └── *.bmp      # Associated bitmaps
│   └── milkdrop/      # MilkDrop preset files
└── libs/              # Required libraries
    └── bass.dll       # BASS audio library
```

## 🚀 Getting Started

### 1. Install Your First Plugin

1. **Download a Plugin**: Get a Winamp visualizer DLL or AVS preset
2. **Place in Directory**: Copy to the appropriate `plugins/` or `presets/` directory
3. **Refresh List**: Click the **🔄 Refresh** button in Plugin Manager
4. **Enable Plugin**: Check the enable checkbox for your new plugin

### 2. Configure a Plugin

1. **Select Plugin**: Click on a plugin in the list
2. **Open Configuration**: Click the **⚙️ Configure** button
3. **Adjust Settings**: Modify plugin-specific parameters
4. **Save Changes**: Apply your configuration

### 3. Test a Plugin

1. **Select Plugin**: Choose a plugin from the list
2. **Start Test**: Click the **▶️ Test** button
3. **Observe Output**: Watch the plugin render with sample data
4. **Verify Functionality**: Ensure the plugin works as expected

## 🔧 Advanced Configuration

### Plugin Settings

#### Audio Processing
- **Sample Rate**: Configure plugin audio input format
- **Buffer Size**: Adjust processing buffer for performance
- **FFT Size**: Set frequency analysis resolution

#### Rendering Options
- **Frame Rate**: Control visualization update frequency
- **Quality Settings**: Balance performance vs. visual quality
- **Color Schemes**: Customize plugin appearance

#### Performance Tuning
- **Threading**: Configure multi-threaded processing
- **Memory Management**: Control plugin memory usage
- **Caching**: Enable/disable plugin result caching

### Plugin Registry

The Plugin Registry automatically discovers and manages all available plugins:

- **Auto-discovery**: Scans plugin directories on startup
- **Dependency Management**: Handles plugin dependencies automatically
- **Version Control**: Manages plugin versioning and updates
- **Conflict Resolution**: Prevents plugin conflicts and crashes

## 🐛 Troubleshooting

### Common Issues

#### Plugin Not Loading
- **Check File Location**: Ensure plugin is in correct directory
- **Verify Dependencies**: Check for missing required libraries
- **File Permissions**: Ensure read access to plugin files
- **Format Support**: Verify plugin format is supported

#### Plugin Crashes
- **Memory Issues**: Check plugin memory usage
- **Thread Conflicts**: Verify threading compatibility
- **Audio Format**: Ensure audio input format matches expectations
- **Version Compatibility**: Check plugin version compatibility

#### Performance Problems
- **FFT Size**: Reduce FFT size for better performance
- **Frame Rate**: Lower frame rate for smoother operation
- **Plugin Count**: Disable unused plugins to free resources
- **System Resources**: Monitor CPU and memory usage

### Debug Information

Enable debug logging to troubleshoot plugin issues:

1. **Open Settings**: Go to application settings
2. **Enable Debug**: Turn on debug logging
3. **Check Logs**: Review console output for error messages
4. **Plugin Info**: Use Info button to get detailed plugin status

## 📚 Plugin Development

### Creating Custom Plugins

PhoenixVisualizer supports custom plugin development:

#### .NET Plugins
- **Interface**: Implement `IVisualizerPlugin` interface
- **Audio Data**: Access real-time audio features
- **Rendering**: Use `ISkiaCanvas` for drawing
- **Configuration**: Provide user-configurable options

#### Winamp Plugins
- **SDK**: Use Winamp Visualizer SDK
- **Functions**: Implement required plugin functions
- **Audio Format**: Handle Winamp audio data format
- **Configuration**: Provide configuration dialogs

### Plugin API Reference

#### Core Interfaces
```csharp
public interface IVisualizerPlugin
{
    string Id { get; }
    string DisplayName { get; }
    string Description { get; }
    bool IsEnabled { get; set; }
    
    void Initialize();
    void Shutdown();
    void ProcessFrame(AudioFeatures features, ISkiaCanvas canvas);
    void Configure();
}
```

#### Audio Features
```csharp
public interface AudioFeatures
{
    float[] Fft { get; }           // Frequency domain data
    float[] Waveform { get; }      // Time domain data
    float Rms { get; }             // Root mean square
    double Bpm { get; }            // Beats per minute
    bool Beat { get; }             // Beat detection
    float Bass { get; }            // Bass frequency energy
    float Mid { get; }             // Mid frequency energy
    float Treble { get; }          // Treble frequency energy
    float Energy { get; }          // Overall energy
    float Volume { get; }          // Current volume
    float Peak { get; }            // Peak amplitude
    double TimeSeconds { get; }    // Current playback time
}
```

## 🎯 Best Practices

### Plugin Organization
- **Categorize**: Group plugins by type and function
- **Version Control**: Keep track of plugin versions
- **Documentation**: Maintain plugin documentation
- **Testing**: Test plugins before production use

### Performance Optimization
- **Resource Management**: Monitor plugin resource usage
- **Caching**: Implement result caching where appropriate
- **Threading**: Use appropriate threading models
- **Memory**: Minimize memory allocations

### User Experience
- **Configuration**: Provide intuitive configuration options
- **Error Handling**: Gracefully handle errors and failures
- **Documentation**: Include clear usage instructions
- **Examples**: Provide sample configurations and presets

## 🔮 Future Enhancements

### Planned Features
- **Plugin Marketplace**: Centralized plugin distribution
- **Auto-updates**: Automatic plugin version management
- **Plugin Analytics**: Usage statistics and performance metrics
- **Advanced Configuration**: Visual configuration builders
- **Plugin Validation**: Automated plugin testing and validation

### Integration Opportunities
- **VST Support**: VST plugin compatibility
- **Web Standards**: Web-based visualization plugins
- **Mobile Support**: Cross-platform mobile plugins
- **Cloud Integration**: Cloud-based plugin storage and sync

---

For more information, see:
- [RUNNING.md](RUNNING.md) - How to run PhoenixVisualizer
- [WINAMP_PLUGIN_SETUP.md](WINAMP_PLUGIN_SETUP.md) - Winamp plugin setup
- [TODO.md](TODO.md) - Development roadmap
- [PHOENIX_VISUALIZER_STATUS.md](PHOENIX_VISUALIZER_STATUS.md) - Project status
