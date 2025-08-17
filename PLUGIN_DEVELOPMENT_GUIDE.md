# PhoenixVisualizer Plugin Development Guide

## 🎯 **Overview**

PhoenixVisualizer is a powerful audio visualization platform that supports multiple plugin types:
- **Visualizer Plugins** - Real-time audio visualization
- **APE Effects** - Audio processing effects
- **AVS Plugins** - Winamp AVS-style visualizers
- **Winamp Plugins** - Direct Winamp visualizer DLL support

This guide will walk you through creating each type of plugin.

---

## 🚀 **Quick Start**

### **1. Visualizer Plugin (Simplest)**

```csharp
using PhoenixVisualizer.PluginHost;

public class MyVisualizer : IVisualizerPlugin
{
    public string Id => "my_visualizer";
    public string DisplayName => "My Awesome Visualizer";
    
    private int _width, _height;
    
    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
    }
    
    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }
    
    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        // Clear the canvas
        canvas.Clear(0xFF000000);
        
        // Draw something based on audio data
        var energy = features.Energy;
        var color = (uint)(0xFF0000FF + (int)(energy * 255));
        
        canvas.FillCircle(_width / 2, _height / 2, energy * 100, color);
    }
    
    public void Dispose()
    {
        // Clean up resources
    }
}
```

### **2. Register Your Plugin**

```csharp
// In your main application startup
PluginRegistry.Register(
    "my_visualizer",
    "My Awesome Visualizer",
    () => new MyVisualizer(),
    "A simple circle visualizer that responds to audio energy",
    "1.0",
    "Your Name"
);
```

---

## 🎨 **Visualizer Plugin Development**

### **Core Interface: `IVisualizerPlugin`**

```csharp
public interface IVisualizerPlugin
{
    string Id { get; }                    // Unique identifier
    string DisplayName { get; }           // Human-readable name
    
    void Initialize(int width, int height);  // Called when plugin is loaded
    void Resize(int width, int height);      // Called when window is resized
    void RenderFrame(AudioFeatures features, ISkiaCanvas canvas);  // Called each frame
    void Dispose();                          // Called when plugin is unloaded
}
```

### **Audio Data: `AudioFeatures`**

```csharp
public interface AudioFeatures
{
    float[] Fft { get; }           // Frequency domain data (2048 samples)
    float[] Waveform { get; }      // Time domain data (2048 samples)
    float Rms { get; }             // Root mean square energy
    double Bpm { get; }            // Beats per minute
    bool Beat { get; }             // Beat detection
    float Bass { get; }            // Bass frequency energy
    float Mid { get; }             // Mid frequency energy
    float Treble { get; }          // Treble frequency energy
    float Energy { get; }          // Overall energy
    float Volume { get; }          // Audio volume
    float Peak { get; }            // Peak amplitude
    double TimeSeconds { get; }    // Playback time
}
```

### **Drawing Canvas: `ISkiaCanvas`**

```csharp
public interface ISkiaCanvas
{
    int Width { get; }             // Canvas width
    int Height { get; }            // Canvas height
    
    // Basic drawing methods
    void Clear(uint color);        // Clear with color (ARGB format)
    void DrawLine(float x1, float y1, float x2, float y2, uint color, float thickness = 1.0f);
    void DrawLines(Span<(float x, float y)> points, float thickness, uint color);
    void DrawRect(float x, float y, float width, float height, uint color, bool filled = false);
    void FillRect(float x, float y, float width, float height, uint color);
    void DrawCircle(float x, float y, float radius, uint color, bool filled = false);
    void FillCircle(float x, float y, float radius, uint color);
    void DrawText(string text, float x, float y, uint color, float size = 12.0f);
    void DrawPoint(float x, float y, uint color, float size = 1.0f);
    void Fade(uint color, float alpha);  // Apply fade effect
    
    // Advanced drawing methods for superscopes
    void DrawPolygon(Span<(float x, float y)> points, uint color, bool filled = false);
    void DrawArc(float x, float y, float radius, float startAngle, float sweepAngle, uint color, float thickness = 1.0f);
    void SetLineWidth(float width);  // Set line thickness for subsequent operations
    float GetLineWidth();            // Get current line thickness
}
```

### **Color Format**

Colors are in ARGB format (32-bit):
- `0xFF000000` = Black
- `0xFFFF0000` = Red
- `0xFF00FF00` = Green
- `0xFF0000FF` = Blue
- `0xFFFFFFFF` = White

Example: `0x80FF0000` = Semi-transparent red (alpha = 0x80 = 128)

---

## 🎵 **APE Effect Plugin Development**

### **Core Interface: `IApeEffect`**

```csharp
public interface IApeEffect
{
    string Id { get; }                    // Unique identifier
    string DisplayName { get; }           // Human-readable name
    string Description { get; }           // Effect description
    bool IsEnabled { get; set; }          // Effect enabled state
    
    void Initialize();                    // Called when effect is loaded
    void Shutdown();                      // Called when effect is unloaded
    void ProcessFrame(AudioFeatures features, ISkiaCanvas canvas);  // Process audio frame
    void Configure();                     // Show configuration dialog
}
```

### **Example APE Effect**

```csharp
public class EchoEffect : IApeEffect
{
    public string Id => "echo_effect";
    public string DisplayName => "Echo Effect";
    public string Description => "Adds echo/reverb to audio visualization";
    public bool IsEnabled { get; set; } = true;
    
    private readonly Queue<float[]> _frameHistory = new();
    private readonly int _echoFrames = 5;
    
    public void Initialize()
    {
        // Initialize effect
    }
    
    public void Shutdown()
    {
        _frameHistory.Clear();
    }
    
    public void ProcessFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        // Store current frame
        var currentFrame = features.Fft.ToArray();
        _frameHistory.Enqueue(currentFrame);
        
        // Keep only last N frames
        while (_frameHistory.Count > _echoFrames)
        {
            _frameHistory.Dequeue();
        }
        
        // Apply echo effect
        var echoCanvas = new EchoCanvas(canvas, _frameHistory);
        // ... echo processing logic
    }
    
    public void Configure()
    {
        // Show configuration dialog
    }
}
```

---

## 🌟 **AVS Plugin Development**

### **Core Interface: `IAvsHostPlugin`**

```csharp
public interface IAvsHostPlugin : IVisualizerPlugin
{
    string Description { get; }           // Plugin description
    bool IsEnabled { get; set; }          // Plugin enabled state
    
    void LoadPreset(string presetText);   // Load AVS preset
}
```

### **Example AVS Plugin**

```csharp
public class MyAvsPlugin : IAvsHostPlugin
{
    public string Id => "my_avs";
    public string DisplayName => "My AVS Plugin";
    public string Description => "Custom AVS-style visualizer";
    public bool IsEnabled { get; set; } = true;
    
    private string _presetCode = "";
    private readonly NsEelEvaluator _evaluator = new();
    
    public void LoadPreset(string presetText)
    {
        _presetCode = presetText;
        // Parse and compile preset
    }
    
    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        // Execute AVS preset code
        if (!string.IsNullOrEmpty(_presetCode))
        {
            ExecutePreset(features, canvas);
        }
    }
    
    private void ExecutePreset(AudioFeatures features, ISkiaCanvas canvas)
    {
        // Set AVS variables
        _evaluator.SetVariable("time", features.TimeSeconds);
        _evaluator.SetVariable("beat", features.Beat ? 1.0 : 0.0);
        _evaluator.SetVariable("energy", features.Energy);
        
        // Execute preset code
        // ... AVS execution logic
    }
    
    // ... other interface methods
}
```

---

## 🎮 **Winamp Plugin Integration**

### **Direct DLL Loading**

PhoenixVisualizer can load Winamp visualizer DLLs directly:

1. **Place your DLL** in the `plugins/vis/` directory
2. **Ensure compatibility** with Winamp SDK structures
3. **Implement required exports**:
   - `winampVisGetHeader`
   - `winampVisGetModule`

### **Winamp SDK Structures**

```c
typedef struct {
    char *description;        // Plugin description
    char *filename;          // DLL filename
    int version;             // Version number
    int type;                // Plugin type
} winampVisHeader;

typedef struct {
    char *description;        // Module description
    HWND hwndParent;         // Parent window
    HINSTANCE hDllInstance;  // DLL instance
    int sRate;               // Sample rate
    int nCh;                 // Number of channels
    int latencyMS;           // Latency in milliseconds
    int delayMS;             // Delay in milliseconds
    int spectrumNch;         // Spectrum channels
    int waveformNch;         // Waveform channels
    int (*init)(struct winampVisModule *this_mod);
    int (*render)(struct winampVisModule *this_mod);
    void (*quit)(struct winampVisModule *this_mod);
    void *userData;          // User data
} winampVisModule;
```

---

## 🔧 **Advanced Features**

### **Performance Monitoring**

```csharp
// Get performance metrics
var perfMonitor = renderSurface.GetPerformanceMonitor();
var metrics = perfMonitor.GetMetrics("my_plugin");

if (metrics != null)
{
    Console.WriteLine($"FPS: {metrics.CurrentFps:F1}");
    Console.WriteLine($"Render Time: {metrics.LastRenderTimeMs:F2}ms");
    Console.WriteLine($"Memory: {metrics.CurrentMemoryBytes / 1024 / 1024:F1}MB");
}
```

### **GPU Acceleration**

```csharp
// Use GPU acceleration
var gpuRenderer = new GpuAcceleratedRenderer();
var config = new GpuAccelerationConfig
{
    EnableGpuAcceleration = true,
    UseComputeShaders = true,
    MaxBatchSize = 2000
};

gpuRenderer.OptimizeForHardware();
```

### **NS-EEL Expression Evaluation**

```csharp
// Create expression editor
var editor = new NsEelEditor();

// Set variables
editor.SetVariable("myVar", 42.0);

// Evaluate expressions
var result = editor.EvaluateExpression("sin(time) * myVar");

// Get suggestions
var suggestions = editor.GetSuggestions("sin");
```

---

## 📁 **Plugin Organization**

### **Directory Structure**

```
PhoenixVisualizer/
├── plugins/
│   ├── vis/           # Winamp visualizer DLLs
│   ├── ape/           # APE effect files
│   └── custom/        # Custom .NET plugins
├── presets/
│   ├── avs/           # AVS preset files
│   └── milkdrop/      # MilkDrop presets
└── libs/              # Dependencies
```

### **Plugin Registration**

```csharp
// Register multiple plugins
PluginRegistry.Register("plugin1", "Plugin 1", () => new Plugin1());
PluginRegistry.Register("plugin2", "Plugin 2", () => new Plugin2());

// Get available plugins
var plugins = PluginRegistry.AvailablePlugins;
foreach (var plugin in plugins)
{
    Console.WriteLine($"{plugin.DisplayName}: {plugin.Description}");
}
```

---

## 🎭 **Superscopes Development**

### **What are Superscopes?**

Superscopes are mathematical visualizations that create complex geometric patterns by plotting points based on mathematical formulas. They respond to audio input (volume, beat detection) and create dynamic, animated visualizations.

### **Creating a Superscope**

```csharp
public class MySuperscope : IVisualizerPlugin
{
    public string Id => "my_superscope";
    public string DisplayName => "My Mathematical Visualization";
    
    private int _width, _height;
    private float _time;
    
    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;
    }
    
    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        canvas.Clear(0xFF000000);
        _time += 0.02f;
        
        var points = new List<(float x, float y)>();
        
        // Create mathematical pattern
        for (int i = 0; i < 100; i++)
        {
            float t = i / 100.0f;
            float angle = t * Math.PI * 2 + _time;
            float radius = 0.3f + features.Volume * 0.2f;
            
            float x = (float)Math.Cos(angle) * radius;
            float y = (float)Math.Sin(angle) * radius;
            
            // Scale and center
            x = x * _width * 0.4f + _width * 0.5f;
            y = y * _height * 0.4f + _height * 0.5f;
            
            points.Add((x, y));
        }
        
        // Draw with rainbow colors
        uint color = features.Beat ? 0xFFFFFF00 : 0xFF00FFFF;
        canvas.SetLineWidth(1.0f);
        canvas.DrawLines(points.ToArray(), 1.0f, color);
    }
}
```

### **Advanced Superscope Techniques**

#### **3D Transformations**
```csharp
// Apply 3D rotation and perspective
float x3d = x * Math.Cos(rotation) - y * Math.Sin(rotation);
float y3d = x * Math.Sin(rotation) + y * Math.Cos(rotation);
float z3d = z;

// Perspective projection
float pers = 1.0f / (1.0f + z3d);
float finalX = x3d * pers;
float finalY = y3d * pers;
```

#### **Rainbow Color Cycling**
```csharp
// Create rainbow effect
float phi = i * 6.283f * 2;
uint red = (uint)((0.5f + 0.5f * Math.Sin(phi)) * 255);
uint green = (uint)((0.5f + 0.5f * Math.Sin(phi + 2.094f)) * 255);
uint blue = (uint)((0.5f + 0.5f * Math.Sin(phi + 4.188f)) * 255);

uint color = (uint)((0xFF << 24) | (red << 16) | (green << 8) | blue);
```

#### **Audio Response**
```csharp
// Respond to different audio features
if (features.Beat)
{
    // Change pattern on beat
    _patternType = (_patternType + 1) % 3;
}

// Use volume for size/amplitude
float size = 0.3f + features.Volume * 0.4f;

// Use frequency bands for color
uint color = GetColorFromFrequency(features.Bass, features.Mid, features.Treble);
```

### **Built-in Superscopes**

PhoenixVisualizer includes 11 pre-built superscopes:
- **Spiral Superscope** - Classic spiral with volume response
- **3D Scope Dish** - 3D dish with perspective projection
- **Rotating Bow Thing** - Animated bow pattern
- **Vertical Bouncing Scope** - Beat-responsive bouncing line
- **Spiral Graph Fun** - Dynamic spiral with beat changes
- **Rainbow Merkaba** - Complex 3D sacred geometry
- **Cat Face Outline** - Animated cat with moving ears
- **Cymatics Frequency** - Solfeggio frequency patterns
- **Pong Simulation** - Interactive Pong game
- **Butterfly** - Animated butterfly with flapping wings
- **Rainbow Sphere Grid** - 3D sphere with grid distortion

See `SUPERSCOPES_IMPLEMENTATION.md` for complete details and examples.

---

## 🧪 **Testing Your Plugin**

### **1. Build and Test**

```bash
# Build your plugin
dotnet build MyPlugin.csproj

# Copy to plugins directory
cp bin/Debug/MyPlugin.dll plugins/custom/
```

### **2. Debug Output**

```csharp
// Add debug logging
System.Diagnostics.Debug.WriteLine($"[MyPlugin] Rendering frame: {features.Energy}");

// Check performance
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
// ... your rendering code ...
stopwatch.Stop();
System.Diagnostics.Debug.WriteLine($"[MyPlugin] Render time: {stopwatch.Elapsed.TotalMilliseconds}ms");
```

### **3. Error Handling**

```csharp
public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
{
    try
    {
        // Your rendering code
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[MyPlugin] Render error: {ex.Message}");
        
        // Fallback rendering
        canvas.Clear(0xFFFF0000); // Red error screen
        canvas.DrawText("Plugin Error", 10, 10, 0xFFFFFFFF);
    }
}
```

---

## 📚 **Best Practices**

### **Performance**

1. **Minimize allocations** in render loops
2. **Use object pooling** for frequently created objects
3. **Batch operations** when possible
4. **Profile your plugin** using the performance monitor

### **Memory Management**

1. **Implement IDisposable** properly
2. **Release unmanaged resources** in Dispose()
3. **Avoid memory leaks** in event handlers
4. **Use weak references** for cross-references

### **Error Handling**

1. **Validate inputs** before processing
2. **Provide fallback behavior** for errors
3. **Log errors** with context information
4. **Gracefully degrade** when features fail

### **User Experience**

1. **Provide meaningful names** and descriptions
2. **Include configuration options** where appropriate
3. **Handle window resizing** gracefully
4. **Support accessibility** features

---

## 🚀 **Next Steps**

1. **Start simple** with a basic visualizer plugin
2. **Add features incrementally** to avoid complexity
3. **Test thoroughly** with different audio sources
4. **Share your plugin** with the community!

### **Resources**

- **PhoenixVisualizer Source**: Check the main project for examples
- **Winamp SDK**: Reference for Winamp plugin development
- **NS-EEL Documentation**: Learn the expression language
- **Community**: Join discussions and get help

---

## 📝 **Example Projects**

### **Complete Visualizer Plugin**

See `PhoenixVisualizer.Visuals/` for built-in visualizer examples.

### **Complete APE Effect**

See `PhoenixVisualizer.Plugins.Ape.Phoenix/` for APE effect examples.

### **Complete AVS Plugin**

See `PhoenixVisualizer.Plugins.Avs/` for AVS plugin examples.

---

**Happy coding! 🎨✨**
