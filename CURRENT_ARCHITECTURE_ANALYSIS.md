# CURRENT ARCHITECTURE ANALYSIS - PhoenixVisualizer

## 🏗️ PROJECT STRUCTURE

### Core Projects
- **PhoenixVisualizer.Core** - Core interfaces, models, and base classes
- **PhoenixVisualizer.PluginHost** - Plugin interface definitions
- **PhoenixVisualizer.Audio** - Audio processing and analysis
- **PhoenixVisualizer.App** - Main application with UI
- **PhoenixVisualizer.Visuals** - All visualizer implementations
- **PhoenixVisualizer.Rendering** - Rendering infrastructure
- **PhoenixVisualizer.Editor** - Visual editor components

### Additional Projects
- **PhoenixVisualizer.Plugins.Avs** - AVS plugin support
- **PhoenixVisualizer.AvsEngine** - AVS script engine
- **PhoenixVisualizer.Plots** - Plotting utilities
- **PhoenixVisualizer.ApeHost** - APE plugin host

## 🔌 INTERFACE ARCHITECTURE

### IVisualizerPlugin Interface (PluginHost)
```csharp
public interface IVisualizerPlugin
{
    string Id { get; }
    string DisplayName { get; }
    void Initialize(int width, int height);
    void Resize(int width, int height);
    void Dispose();
    void RenderFrame(AudioFeatures f, ISkiaCanvas canvas);
}
```

### AudioFeatures Interface (PluginHost)
```csharp
public interface AudioFeatures
{
    float Volume { get; }
    float Beat { get; }
    // Missing: Spectrum, Time properties that Node files expect
}
```

### ISkiaCanvas Interface (PluginHost)
```csharp
public interface ISkiaCanvas
{
    int Width { get; }
    int Height { get; }
    void Clear(uint color);
    void DrawLine(float x1, float y1, float x2, float y2, uint color, float thickness = 1.0f);
    void FillRectangle(float x, y, float width, float height, uint color);
    // Additional methods for XSS nodes
    void Fade(uint color, float amount);
    void SetLineWidth(float width);
    void DrawPolyline(System.Span<(float x, float y)> points, uint color);
    void DrawPolygon(System.Span<(float x, float y)> points, uint color, bool filled = false);
    void FillRect(float x, float y, float width, float height, uint color);
}
```

### IEffectNode Interface (Core.Nodes)
```csharp
public interface IEffectNode
{
    void Render(float[] waveform, float[] spectrum, RenderContext ctx);
    // Missing: With() method that Node files expect
}
```

### RenderContext Structure (Core.Nodes)
```csharp
public struct RenderContext
{
    public int Width { get; set; }
    public int Height { get; set; }
    public float[] Waveform { get; set; }
    public float[] Spectrum { get; set; }
    public float Time { get; set; }
    public float Beat { get; set; }
    public float Volume { get; set; }
    public ISkiaCanvas Canvas { get; set; }
}
```

## 🎨 VISUALIZER TYPES

### 1. BaseVisualizer (Core)
- Abstract base class for traditional visualizers
- Uses `Render(SKCanvas canvas, int width, int height, AudioFeatures audioFeatures, float deltaTime)`
- Examples: SpectrumWaveformHybridVisualizer, ParticleFieldVisualizer

### 2. IVisualizerPlugin Implementations
- Direct implementations of the plugin interface
- Uses `RenderFrame(AudioFeatures f, ISkiaCanvas canvas)`
- Examples: All Node visualizers, XSS visualizers

### 3. Node-based Visualizers
- Use IEffectNode and EffectRegistry
- Expect fluent API with `.With()` method
- Currently broken due to API mismatches

## 🚨 CURRENT ISSUES

### 1. API Mismatches
- **IEffectNode.With()** - Node files expect fluent configuration API
- **AudioFeatures.Spectrum/Time** - Node files expect these properties
- **ISkiaCanvas Conversion** - Two different ISkiaCanvas interfaces

### 2. Interface Conflicts
- `PhoenixVisualizer.Core.Interfaces.ISkiaCanvas` vs `PhoenixVisualizer.PluginHost.ISkiaCanvas`
- `PhoenixVisualizer.Core.Models.AudioFeatures` vs `PhoenixVisualizer.PluginHost.AudioFeatures`

### 3. Missing Implementations
- EffectRegistry.CreateByName() works, but Node files expect .With() method
- AudioFeatures interface missing Spectrum and Time properties

## 📊 VISUALIZER REGISTRATION STATUS

### Working Visualizers (25 Node + 9 XSS = 34 total)
- All registered in App.axaml.cs
- Node visualizers: NodeBarsReactive, NodePulseTunnel, etc.
- XSS visualizers: NodeXsFireworks, NodeXsPlasma, etc.

### Build Status
- **PhoenixVisualizer.Core**: ✅ Builds successfully
- **PhoenixVisualizer.Audio**: ✅ Builds successfully  
- **PhoenixVisualizer.PluginHost**: ✅ Builds successfully
- **PhoenixVisualizer.Visuals**: ❌ 201 errors due to API mismatches

## 🔧 SYSTEMATIC FIX STRATEGY

### Phase 1: Steal Working Code from Backup
1. Examine `PhoenixVisualizer_backup` for working implementations
2. Find working IEffectNode implementations with .With() method
3. Find working AudioFeatures with Spectrum/Time properties
4. Find working ISkiaCanvas implementations

### Phase 2: Implement Missing APIs
1. Add .With() extension method to IEffectNode
2. Add Spectrum/Time properties to AudioFeatures interface
3. Create adapter for ISkiaCanvas interface conversion

### Phase 3: Fix All Node Files
1. Apply consistent pattern to all Node visualizers
2. Ensure all use correct APIs
3. Test build after each batch

## 📁 KEY FILES TO EXAMINE

### Core Interfaces
- `PhoenixVisualizer.PluginHost/IVisualizerPlugin.cs`
- `PhoenixVisualizer.PluginHost/AudioFeatures.cs`
- `PhoenixVisualizer.PluginHost/ISkiaCanvas.cs`
- `PhoenixVisualizer.Core/Nodes/IEffectNode.cs`
- `PhoenixVisualizer.Core/Nodes/EffectRegistry.cs`

### Working Visualizers
- `PhoenixVisualizer.Visuals/NodeBarsReactive.cs` (Fixed)
- `PhoenixVisualizer.Visuals/NodePulseTunnel.cs` (Fixed)
- `PhoenixVisualizer.Visuals/SpectrumWaveformHybridVisualizer.cs` (Fixed)

### Broken Visualizers
- All other Node*.cs files (25+ files)
- All NodeXs*.cs files (9 files)

## 🎯 SUCCESS CRITERIA

1. **Build Success**: All projects build without errors
2. **Runtime Success**: App runs with all 34 visualizers
3. **API Consistency**: All visualizers use consistent interfaces
4. **No Regressions**: Existing working visualizers remain functional

## 📝 NEXT STEPS

1. **Examine Backup**: Look at `PhoenixVisualizer_backup` for working code patterns
2. **Implement Missing APIs**: Add .With(), Spectrum, Time, ISkiaCanvas adapter
3. **Apply Systematic Fixes**: Fix all Node files using consistent pattern
4. **Test and Validate**: Ensure all visualizers work correctly


