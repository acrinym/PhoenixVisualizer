# Winamp Visualization Plugins

This directory is where you place Winamp visualization plugin DLLs for PhoenixVisualizer to load.

## 🎯 **How to Use**

1. **Drop Winamp Plugin DLLs** into this folder:
   - `vis_avs.dll` - Advanced Visualization Studio
   - `vis_milk2.dll` - MilkDrop 2
   - `vis_nsfs.dll` - NSFS
   - Any other `vis_*.dll` files

2. **Open PhoenixVisualizer** and click the 🎵 **Winamp** button in the toolbar

3. **Select a Plugin** from the list and click "Select Plugin"

4. **The plugin will be activated** and integrated with the main visualization system

## 🔧 **Supported Plugin Types**

- **Standard Winamp Visualizers** (vis_*.dll)
- **Advanced Visualization Studio** (AVS) plugins
- **MilkDrop 2** visualizers
- **Custom Winamp visualization plugins**

## 📁 **Directory Structure**

```
plugins/
├── vis/           # Winamp visualization plugins (.dll files)
├── ape/           # Winamp APE effects (future)
└── presets/       # Plugin presets and configurations
    ├── avs/       # AVS presets
    └── milkdrop/  # MilkDrop presets
```

## ⚠️ **Important Notes**

- **Windows Only**: Winamp plugins are Windows-specific and won't work on Linux/macOS
- **Plugin Compatibility**: Not all Winamp plugins may work perfectly - test each one
- **Performance**: Some plugins may impact performance depending on complexity
- **Dependencies**: Ensure any required DLLs are also present

## 🚀 **Getting Started**

1. **Download Winamp plugins** from:
   - [Winamp Plugin Database](https://www.winamp.com/plugins)
   - [AVS Preset Repository](https://www.avspresets.com/)
   - [MilkDrop Presets](https://www.milkdrop.co.uk/)

2. **Extract DLL files** to this directory

3. **Restart PhoenixVisualizer** or click the refresh button

4. **Select and test** your plugins!

## 🐛 **Troubleshooting**

- **Plugin not showing**: Check that the DLL file starts with `vis_`
- **Plugin crashes**: Some plugins may not be compatible - try different ones
- **No visualization**: Ensure audio is playing and the plugin is selected
- **Performance issues**: Try simpler plugins or adjust quality settings

---

**Happy visualizing! 🎵✨**
