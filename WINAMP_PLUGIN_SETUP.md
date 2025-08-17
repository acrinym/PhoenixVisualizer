# Winamp Plugin Setup Guide

## 🎯 **Overview**

The PhoenixVisualizer now supports **actual Winamp visualizer plugins**! This means you can use the same visualizers you use in Winamp, including:

- **vis_avs.dll** - Advanced Visualization Studio (the main one!)
- **vis_milk2.dll** - MilkDrop
- **vis_nsfs.dll** - NSFS
- **vis_otv.dll** - Old TV
- **vis_peaks.dll** - Peaks
- **vis_spectrum.dll** - Spectrum
- **vis_waveform.dll** - Waveform

## 📁 **Setup Instructions**

### 1. **Create Plugin Directory**
The system will automatically create a `plugins/vis/` directory in your app folder, or you can create it manually.

### 2. **Copy Your Winamp Plugins**
Copy your Winamp visualizer `.dll` files to the `plugins/vis/` directory. These are typically found in:
- `C:\Program Files\Winamp\Plugins\`
- `C:\Program Files (x86)\Winamp\Plugins\`

### 3. **Test Plugin Loading**
Run the test program to verify your plugins load correctly:
```bash
dotnet run TestWinampPlugins.cs
```

## 🔌 **How It Works**

The system uses **direct Winamp plugin loading** instead of the complex BASS_WA system:

1. **Loads DLLs directly** using Windows API
2. **Calls Winamp functions** like `visHeader`, `Init`, `Render`, etc.
3. **Feeds audio data** in the format Winamp plugins expect
4. **Manages plugin lifecycle** (init, render, quit)

## 🎵 **Audio Data Format**

Winamp plugins expect:
- **Spectrum Data**: 576 bytes per channel (0-255 values)
- **Waveform Data**: 576 bytes per channel (0-255 values)
- **Sample Rate**: Usually 44100 Hz
- **Channels**: Stereo (2 channels)

## 🚀 **Using Plugins in Code**

```csharp
// Create the plugin host
using var host = new SimpleWinampHost();

// Scan for plugins
host.ScanForPlugins();

// Get available plugins
var plugins = host.GetAvailablePlugins();

// Initialize a plugin module
if (host.InitializeModule(0, 0)) // Plugin 0, Module 0
{
    // Set parent window (if needed)
    host.SetParentWindow(hwnd);
    
    // Update audio data
    host.UpdateAudioData(0, 0, spectrumData, waveformData);
    
    // Render the plugin
    host.RenderModule(0, 0);
}
```

## 🧪 **Testing Your Plugins**

### **Step 1: Copy Plugins**
Copy your Winamp visualizer `.dll` files to `plugins/vis/`

### **Step 2: Run Test**
```bash
dotnet run TestWinampPlugins.cs
```

### **Step 3: Check Output**
The test program will show:
- Which plugins were found
- Plugin descriptions and capabilities
- Module information
- Initialization results

## 🔧 **Troubleshooting**

### **Plugin Not Loading**
- **Check file path**: Ensure `.dll` files are in `plugins/vis/`
- **Architecture mismatch**: 32-bit plugins won't work in 64-bit apps
- **Missing dependencies**: Some plugins need additional DLLs
- **Corrupted files**: Try copying from a fresh Winamp installation

### **Initialization Fails**
- **Parent window**: Some plugins need a valid window handle
- **Audio format**: Ensure audio data matches expected format
- **Plugin compatibility**: Not all plugins work outside Winamp

### **Rendering Issues**
- **Audio data**: Check that spectrum/waveform data is valid
- **Window handle**: Ensure parent window is set correctly
- **Plugin state**: Verify plugin is initialized before rendering

## 📋 **Common Winamp Plugins**

| Plugin | Description | Status |
|--------|-------------|---------|
| **vis_avs.dll** | Advanced Visualization Studio | ✅ Primary target |
| **vis_milk2.dll** | MilkDrop | 🔄 Needs testing |
| **vis_nsfs.dll** | NSFS | 🔄 Needs testing |
| **vis_otv.dll** | Old TV | 🔄 Needs testing |
| **vis_peaks.dll** | Peaks | 🔄 Needs testing |
| **vis_spectrum.dll** | Spectrum | 🔄 Needs testing |
| **vis_waveform.dll** | Waveform | 🔄 Needs testing |

## 🎨 **Integration with PhoenixVisualizer**

Once plugins are working, you can:

1. **Replace built-in visualizers** with Winamp plugins
2. **Use NS-EEL expressions** for custom effects
3. **Load AVS presets** directly from Winamp
4. **Access the full Winamp ecosystem** of visualizers

## 📚 **Next Steps**

1. **Test basic plugin loading** with the test program
2. **Verify audio data format** matches Winamp expectations
3. **Integrate with main app** for real-time visualization
4. **Add plugin management UI** for easy switching
5. **Support AVS preset loading** for custom effects

## 🔗 **Useful Links**

- [Winamp Plugin Development](https://www.winamp.com/plugin/visualization)
- [AVS Documentation](https://www.avs4you.com/)
- [NS-EEL Reference](https://www.avs4you.com/NS-EEL.html)

---

**Note**: This system bypasses the complex BASS_WA integration and directly loads Winamp plugins, which should be more reliable and compatible with your existing plugins.
