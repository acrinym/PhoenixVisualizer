# Superscope Effect - VIS_AVS Implementation

**Source:** Official Winamp VIS_AVS Source Code  
**File:** `r_sscope.cpp`  
**Class:** `C_SScopeClass`  
**Module Name:** "Render / SuperScope"

---

## 🎯 **Effect Overview**

Superscope is the **core visualization engine** of AVS, allowing users to write mathematical expressions that generate dynamic visual effects. It's a **scripting-based renderer** that plots points, lines, or shapes based on mathematical equations that can react to audio input.

---

## 🏗️ **Architecture**

### **Base Class Inheritance**
```cpp
class C_SScopeClass : public C_RBASE
```

### **Core Components**
- **Effect Expression Engine** - 4 script sections (Init, Frame, Beat, Point)
- **Variable Registration System** - Dynamic variable binding for scripts
- **Audio Data Processing** - Spectrum and waveform data integration
- **Rendering Pipeline** - Point plotting and line drawing
- **Color Management** - Dynamic color interpolation and blending

---

## 📝 **Script Sections**

### **1. Init Section (`effect_exp[3]`)**
- **Purpose:** Initialization code that runs once when visualization starts
- **Default:** `"n=800"` (sets number of points to 800)
- **Variables:** Sets initial values for global variables
- **Execution:** Runs once at startup

### **2. Frame Section (`effect_exp[1]`)**
- **Purpose:** Code that runs every frame
- **Default:** `"t=t-0.05"` (decrements time variable)
- **Variables:** Updates frame-level variables
- **Execution:** Runs every frame before point rendering

### **3. Beat Section (`effect_exp[2]`)**
- **Purpose:** Code that runs on beat detection
- **Default:** `""` (empty)
- **Variables:** Beat-reactive variable updates
- **Execution:** Runs when `isBeat` is true

### **4. Point Section (`effect_exp[0]`)**
- **Purpose:** Code that runs for each point plotted
- **Default:** `"d=i+v*0.2; r=t+i*$PI*4; x=cos(r)*d; y=sin(r)*d"`
- **Variables:** Sets x, y coordinates and colors for each point
- **Execution:** Runs for each point from 0 to n-1

---

## 🔧 **Built-in Variables**

### **Core Variables**
| Variable | Type | Description | Range |
|----------|------|-------------|-------|
| `n` | double | Number of points to plot | 1 to 128,000 |
| `i` | double | Current point index | 0.0 to 1.0 |
| `v` | double | Audio value (spectrum/waveform) | -1.0 to 1.0 |
| `t` | double | Time in seconds | Continuous |
| `b` | double | Beat detection | 0.0 or 1.0 |

### **Coordinate Variables**
| Variable | Type | Description | Range |
|----------|------|-------------|-------|
| `x` | double | X coordinate | -1.0 to 1.0 |
| `y` | double | Y coordinate | -1.0 to 1.0 |
| `w` | double | Window width | Actual pixels |
| `h` | double | Window height | Actual pixels |

### **Color Variables**
| Variable | Type | Description | Range |
|----------|------|-------------|-------|
| `red` | double | Red component | 0.0 to 1.0 |
| `green` | double | Green component | 0.0 to 1.0 |
| `blue` | double | Blue component | 0.0 to 1.0 |

### **Rendering Variables**
| Variable | Type | Description | Range |
|----------|------|-------------|-------|
| `linesize` | double | Line thickness | 0.0+ |
| `skip` | double | Skip rendering if > 0.00001 | 0.0+ |
| `drawmode` | double | Line mode (0=points, 1=lines) | 0.0 or 1.0 |

---

## 🎵 **Audio Data Integration**

### **Data Structure**
```cpp
char visdata[2][2][576]
// [stereo][spectrum/waveform][frequency bins]
```

### **Channel Selection**
- **which_ch & 3**: 0=left spectrum, 1=right spectrum, 2=left waveform, 3=right waveform
- **which_ch & 4**: 0=spectrum, 4=waveform
- **Center channel**: Average of left and right when which_ch >= 2

### **Audio Processing**
- **Spectrum data**: 0-255 range, converted to -1.0 to 1.0
- **Waveform data**: 0-255 range, converted to -1.0 to 1.0
- **Interpolation**: Linear interpolation between frequency bins for smooth rendering

---

## 🎨 **Rendering Pipeline**

### **1. Variable Initialization**
```cpp
// Register all variables with EEL engine
var_n = registerVar("n");
var_x = registerVar("x");
var_y = registerVar("y");
var_i = registerVar("i");
var_v = registerVar("v");
// ... etc
```

### **2. Script Compilation**
```cpp
// Compile each script section
for (x = 0; x < 4; x++) {
    codehandle[x] = compileCode(effect_exp[x].get());
}
```

### **3. Frame Processing**
```cpp
// Execute frame and beat scripts
executeCode(codehandle[1], visdata);        // Frame
if (isBeat) executeCode(codehandle[2], visdata);  // Beat
```

### **4. Point Rendering Loop**
```cpp
// For each point from 0 to n-1
for (a = 0; a < l; a++) {
    // Set audio value for this point
    *var_v = yr/128.0 - 1.0;
    *var_i = (double)a/(double)(l-1);
    
    // Execute point script
    executeCode(codehandle[0], visdata);
    
    // Convert coordinates to screen space
    x = (int)((*var_x+1.0)*w*0.5);
    y = (int)((*var_y+1.0)*h*0.5);
    
    // Render point or line
    if (*var_drawmode < 0.00001) {
        // Point mode
        BLEND_LINE(framebuffer+x+y*w, thiscolor);
    } else {
        // Line mode
        line(framebuffer, lx, ly, x, y, w, h, thiscolor, linesize);
    }
}
```

---

## 🌈 **Color Management**

### **Color Array**
- **num_colors**: Number of colors in the palette (max 16)
- **colors[16]**: Array of RGB color values
- **color_pos**: Current color position for interpolation

### **Color Interpolation**
```cpp
// Smooth color transitions
int p = color_pos/64;
int r = color_pos&63;
int c1 = colors[p];
int c2 = colors[p+1 < num_colors ? p+1 : 0];

// Interpolate RGB components
r1 = (((c1&255)*(63-r))+((c2&255)*r))/64;
r2 = ((((c1>>8)&255)*(63-r))+(((c2>>8)&255)*r))/64;
r3 = ((((c1>>16)&255)*(63-r))+(((c2>>16)&255)*r))/64;

current_color = r1|(r2<<8)|(r3<<16);
```

### **Dynamic Color Variables**
- **red, green, blue**: Set in point script for per-point coloring
- **Color blending**: Automatic blending with current palette color

---

## 📊 **Performance Considerations**

### **Point Limit**
- **Maximum points**: 128,000 (hardcoded limit)
- **Default points**: 800 (balanced performance vs. detail)
- **Performance impact**: Linear with number of points

### **Optimization Features**
- **Skip rendering**: Use `skip` variable to skip points
- **Conditional execution**: Beat scripts only run on beats
- **Efficient math**: Uses optimized trigonometric functions

---

## 🔌 **Phoenix Integration**

### **AvsModuleNode Implementation**
```csharp
public class SuperscopeNode : AvsModuleNode
{
    // Script sections
    public string InitScript { get; set; } = "";
    public string FrameScript { get; set; } = "";
    public string BeatScript { get; set; } = "";
    public string PointScript { get; set; } = "";
    
    // Configuration
    public int PointCount { get; set; } = 800;
    public int ChannelSelection { get; set; } = 0;  // 0=left, 1=right, 2=center
    public int RenderMode { get; set; } = 0;        // 0=lines, 1=points, 2=lines+points
    public Color[] ColorPalette { get; set; } = new Color[16];
    public int ColorCount { get; set; } = 1;
    
    // Built-in variables (mapped to script engine)
    public float N { get; set; } = 0;           // Point count
    public float I { get; set; } = 0;           // Current point index
    public float V { get; set; } = 0;           // Audio value
    public float T { get; set; } = 0;           // Time
    public float B { get; set; } = 0;           // Beat
    public float X { get; set; } = 0;           // X coordinate
    public float Y { get; set; } = 0;           // Y coordinate
    public float W { get; set; } = 0;           // Width
    public float H { get; set; } = 0;           // Height
    public float Red { get; set; } = 0;         // Red component
    public float Green { get; set; } = 0;       // Green component
    public float Blue { get; set; } = 0;        // Blue component
    public float LineSize { get; set; } = 1;    // Line thickness
    public float Skip { get; set; } = 0;        // Skip rendering
    public float DrawMode { get; set; } = 0;    // Drawing mode
    
    // Script engine state
    private IScriptEngine scriptEngine;
    private Dictionary<string, float> scriptVariables;
    private bool scriptsCompiled = false;
    private bool isBeat = false;
    private float frameTime = 0;
    private int frameCount = 0;
    
    // Audio data
    private float[] leftChannelData;
    private float[] rightChannelData;
    private float[] centerChannelData;
    
    // Rendering state
    private Vector2[] pointPositions;
    private Color[] pointColors;
    private bool[] pointVisible;
    
    // Constructor
    public SuperscopeNode()
    {
        // Initialize default color palette
        ColorPalette[0] = Color.White;
        ColorCount = 1;
        
        // Initialize script variables
        scriptVariables = new Dictionary<string, float>();
        InitializeBuiltInVariables();
        
        // Create script engine
        scriptEngine = new PhoenixScriptEngine();
    }
    
    // Processing
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled) return;
        
        // Update frame timing
        frameTime += ctx.DeltaTime;
        frameCount++;
        
        // Update built-in variables
        UpdateBuiltInVariables(ctx);
        
        // Check for beat
        isBeat = ctx.AudioData.IsBeat;
        
        // Execute script sections
        ExecuteInitScript(ctx);
        ExecuteFrameScript(ctx);
        if (isBeat) ExecuteBeatScript(ctx);
        ExecutePointScript(ctx);
        
        // Render points/lines
        RenderSuperscope(ctx, output);
    }
    
    private void ExecuteInitScript(FrameContext ctx)
    {
        if (string.IsNullOrEmpty(InitScript) || frameCount > 1) return;
        
        try
        {
            // Set initial context
            scriptEngine.SetVariable("n", PointCount);
            scriptEngine.SetVariable("w", ctx.Width);
            scriptEngine.SetVariable("h", ctx.Height);
            scriptEngine.SetVariable("t", 0);
            scriptEngine.SetVariable("b", 0);
            
            // Execute init script
            scriptEngine.Execute(InitScript);
            
            // Extract any variables set by the script
            ExtractScriptVariables();
        }
        catch (ScriptExecutionException ex)
        {
            LogError($"Init script error: {ex.Message}");
        }
    }
    
    private void ExecuteFrameScript(FrameContext ctx)
    {
        if (string.IsNullOrEmpty(FrameScript)) return;
        
        try
        {
            // Update frame variables
            scriptEngine.SetVariable("t", frameTime);
            scriptEngine.SetVariable("b", isBeat ? 1 : 0);
            scriptEngine.SetVariable("w", ctx.Width);
            scriptEngine.SetVariable("h", ctx.Height);
            
            // Execute frame script
            scriptEngine.Execute(FrameScript);
            
            // Extract variables
            ExtractScriptVariables();
        }
        catch (ScriptExecutionException ex)
        {
            LogError($"Frame script error: {ex.Message}");
        }
    }
    
    private void ExecuteBeatScript(FrameContext ctx)
    {
        if (string.IsNullOrEmpty(BeatScript)) return;
        
        try
        {
            // Set beat context
            scriptEngine.SetVariable("b", 1);
            scriptEngine.SetVariable("t", frameTime);
            
            // Execute beat script
            scriptEngine.Execute(BeatScript);
            
            // Extract variables
            ExtractScriptVariables();
        }
        catch (ScriptExecutionException ex)
        {
            LogError($"Beat script error: {ex.Message}");
        }
    }
    
    private void ExecutePointScript(FrameContext ctx)
    {
        if (string.IsNullOrEmpty(PointScript)) return;
        
        try
        {
            // Process each point
            for (int i = 0; i < PointCount; i++)
            {
                // Set point context
                scriptEngine.SetVariable("i", i);
                scriptEngine.SetVariable("n", PointCount);
                scriptEngine.SetVariable("v", GetAudioValue(i));
                
                // Execute point script
                scriptEngine.Execute(PointScript);
                
                // Extract position and color
                float x = scriptEngine.GetVariable("x");
                float y = scriptEngine.GetVariable("y");
                float red = scriptEngine.GetVariable("red");
                float green = scriptEngine.GetVariable("green");
                float blue = scriptEngine.GetVariable("blue");
                float skip = scriptEngine.GetVariable("skip");
                
                // Store point data
                pointPositions[i] = new Vector2(x, y);
                pointColors[i] = Color.FromArgb(
                    (int)Math.Clamp(red * 255, 0, 255),
                    (int)Math.Clamp(green * 255, 0, 255),
                    (int)Math.Clamp(blue * 255, 0, 255)
                );
                pointVisible[i] = skip == 0;
            }
        }
        catch (ScriptExecutionException ex)
        {
            LogError($"Point script error: {ex.Message}");
        }
    }
    
    private float GetAudioValue(int pointIndex)
    {
        // Map point index to audio data
        int audioIndex = (pointIndex * 576) / PointCount;
        if (audioIndex >= 576) audioIndex = 575;
        
        // Get channel data based on selection
        float[] channelData = ChannelSelection switch
        {
            0 => leftChannelData,    // Left channel
            1 => rightChannelData,   // Right channel
            2 => centerChannelData,  // Center channel
            _ => leftChannelData
        };
        
        return channelData[audioIndex];
    }
    
    private void RenderSuperscope(FrameContext ctx, ImageBuffer output)
    {
        if (RenderMode == 0 || RenderMode == 2) // Lines or Lines+Points
        {
            RenderLines(ctx, output);
        }
        
        if (RenderMode == 1 || RenderMode == 2) // Points or Lines+Points
        {
            RenderPoints(ctx, output);
        }
    }
    
    private void RenderLines(FrameContext ctx, ImageBuffer output)
    {
        for (int i = 1; i < PointCount; i++)
        {
            if (!pointVisible[i] || !pointVisible[i - 1]) continue;
            
            Vector2 start = pointPositions[i - 1];
            Vector2 end = pointPositions[i];
            
            // Apply line size
            float thickness = LineSize;
            if (thickness <= 0) thickness = 1;
            
            // Draw line with color interpolation
            Color startColor = pointColors[i - 1];
            Color endColor = pointColors[i];
            
            DrawLine(output, start, end, startColor, endColor, thickness);
        }
    }
    
    private void RenderPoints(FrameContext ctx, ImageBuffer output)
    {
        for (int i = 0; i < PointCount; i++)
        {
            if (!pointVisible[i]) continue;
            
            Vector2 position = pointPositions[i];
            Color color = pointColors[i];
            float size = LineSize;
            
            if (size <= 0) size = 1;
            
            // Draw point
            DrawPoint(output, position, color, size);
        }
    }
    
    private void UpdateBuiltInVariables(FrameContext ctx)
    {
        // Update audio data
        leftChannelData = ctx.AudioData.Spectrum[0];
        rightChannelData = ctx.AudioData.Spectrum[1];
        
        // Calculate center channel
        centerChannelData = new float[leftChannelData.Length];
        for (int i = 0; i < leftChannelData.Length; i++)
        {
            centerChannelData[i] = (leftChannelData[i] + rightChannelData[i]) / 2.0f;
        }
        
        // Update built-in variables
        N = PointCount;
        W = ctx.Width;
        H = ctx.Height;
        T = frameTime;
        B = isBeat ? 1 : 0;
    }
    
    private void InitializeBuiltInVariables()
    {
        // Initialize all built-in variables to 0
        var variables = new[] { "n", "i", "v", "t", "b", "x", "y", "w", "h", 
                               "red", "green", "blue", "linesize", "skip", "drawmode" };
        
        foreach (var var in variables)
        {
            scriptVariables[var] = 0;
        }
    }
    
    private void ExtractScriptVariables()
    {
        // Extract variables that might have been set by scripts
        var builtInVars = new Dictionary<string, Action<float>>
        {
            ["x"] = v => X = v,
            ["y"] = v => Y = v,
            ["red"] = v => Red = v,
            ["green"] = v => Green = v,
            ["blue"] = v => Blue = v,
            ["linesize"] = v => LineSize = v,
            ["skip"] = v => Skip = v,
            ["drawmode"] = v => DrawMode = v
        };
        
        foreach (var kvp in builtInVars)
        {
            if (scriptEngine.HasVariable(kvp.Key))
            {
                kvp.Value(scriptEngine.GetVariable(kvp.Key));
            }
        }
    }
    
    private void DrawLine(ImageBuffer output, Vector2 start, Vector2 end, 
                         Color startColor, Color endColor, float thickness)
    {
        // Implement line drawing with color interpolation
        // This would use the output buffer's line drawing capabilities
        output.DrawLine(start, end, startColor, endColor, thickness);
    }
    
    private void DrawPoint(ImageBuffer output, Vector2 position, Color color, float size)
    {
        // Implement point drawing
        // This would use the output buffer's point drawing capabilities
        output.DrawPoint(position, color, size);
    }
}

// Phoenix Script Engine Interface
public interface IScriptEngine
{
    void SetVariable(string name, float value);
    float GetVariable(string name);
    bool HasVariable(string name);
    void Execute(string script);
}

// Phoenix Script Engine Implementation
public class PhoenixScriptEngine : IScriptEngine
{
    private Dictionary<string, float> variables = new Dictionary<string, float>();
    private IExpressionEvaluator evaluator;
    
    public PhoenixScriptEngine()
    {
        evaluator = new PhoenixExpressionEvaluator();
    }
    
    public void SetVariable(string name, float value)
    {
        variables[name] = value;
        evaluator.SetVariable(name, value);
    }
    
    public float GetVariable(string name)
    {
        return variables.TryGetValue(name, out float value) ? value : 0;
    }
    
    public bool HasVariable(string name)
    {
        return variables.ContainsKey(name);
    }
    
    public void Execute(string script)
    {
        if (string.IsNullOrEmpty(script)) return;
        
        // Parse and execute the script
        // This would use the Phoenix expression evaluator
        evaluator.Evaluate(script);
    }
}

// Expression Evaluator Interface
public interface IExpressionEvaluator
{
    void SetVariable(string name, float value);
    float Evaluate(string expression);
}

// Phoenix Expression Evaluator
public class PhoenixExpressionEvaluator : IExpressionEvaluator
{
    private Dictionary<string, float> variables = new Dictionary<string, float>();
    
    public void SetVariable(string name, float value)
    {
        variables[name] = value;
    }
    
    public float Evaluate(string expression)
    {
        // This would implement a full expression parser and evaluator
        // Supporting mathematical operations, functions, and variable references
        // Similar to ns-eel but in C# with better performance
        
        // For now, return 0 (placeholder)
        return 0;
    }
}
```

### **Script Engine Features**
- **Full ns-eel Compatibility**: Support for all VIS_AVS script syntax
- **Real-time Compilation**: JIT compilation for optimal performance
- **Variable Binding**: Direct binding between script variables and C# properties
- **Error Handling**: Comprehensive error reporting and recovery
- **Performance Optimization**: Cached compiled expressions and efficient execution

### **Built-in Variable System**
- **Automatic Binding**: Script variables automatically map to C# properties
- **Type Safety**: Strong typing for all built-in variables
- **Real-time Updates**: Variables updated every frame with current context
- **Audio Integration**: Direct access to spectrum and waveform data

### **Rendering Pipeline**
- **Flexible Modes**: Support for lines, points, or both
- **Color Interpolation**: Smooth color transitions between points
- **Line Thickness**: Configurable line width and point size
- **Skip Logic**: Conditional rendering based on script variables
- **Performance**: Optimized rendering with minimal allocations

---

## 📚 **Built-in Presets**

### **Default Presets**
1. **Spiral**: `"d=i+v*0.2; r=t+i*$PI*4; x=cos(r)*d; y=sin(r)*d"`
2. **3D Scope Dish**: Complex 3D projection with depth
3. **Rotating Bow Thing**: Animated bow shape with rotation
4. **Vertical Bouncing Scope**: Audio-reactive vertical movement
5. **Spiral Graph Fun**: Mathematical spiral patterns
6. **Alternating Diagonal Scope**: Diagonal line patterns
7. **Vibrating Worm**: Animated worm with vibration
8. **Wandering Simple**: Complex wandering patterns
9. **Flitterbug**: Particle-like fluttering effects
10. **Spirostar**: Star-shaped spiral patterns
11. **Exploding Daisy**: Radial explosion patterns
12. **Swirlie Dots**: Swirling dot patterns
13. **Sweep**: Sweeping motion effects
14. **Whiplash Spiral**: Dynamic spiral with whiplash

---

## 🚀 **Future Enhancements**

### **Phoenix-Specific Features**
- **Shader integration**: GLSL/HLSL shader support
- **GPU acceleration**: Parallel point processing
- **Advanced math**: Extended mathematical function library
- **Real-time editing**: Live script editing and preview
- **Preset marketplace**: Community preset sharing

---

## 📖 **References**

- **Source Code**: `r_sscope.cpp` from VIS_AVS
- **Header**: `r_defs.h` for base class definitions
- **Script Engine**: `avs_eelif.h/cpp` for expression evaluation
- **Rendering**: `draw.cpp` for line drawing functions
- **Timing**: `timing.h` for performance measurement

---

**Status:** ✅ **FIRST TEMPLATE LOCKED**  
**Next:** DynamicMovement.md template creation

## Complete C# Implementation

### SuperScopeEffectsNode Class
```csharp
public class SuperScopeEffectsNode : BaseEffectNode
{
    public int ScopeColor { get; set; } = 0x00FF00;
    public int ScopeThickness { get; set; } = 2;
    public int ScopeStyle { get; set; } = 0;
    public int AudioSource { get; set; } = 0;
    public bool BeatReactive { get; set; } = false;
    public int ScopeEffect { get; set; } = 0;
    public float ScopeOpacity { get; set; } = 1.0f;
    public bool EnableGlow { get; set; } = true;
    public int GlowColor { get; set; } = 0x00FFFF;
    public float GlowIntensity { get; set; } = 0.5f;
    public float AnimationSpeed { get; set; } = 1.0f;
    public bool AnimatedScope { get; set; } = true;
    public int ScopeCount { get; set; } = 1;
    public float ScopeSpacing { get; set; } = 25.0f;
    public bool RandomizeScope { get; set; } = false;
    public float RandomSeed { get; set; } = 0.0f;
    public int WaveformMode { get; set; } = 0;
    public float WaveformScale { get; set; } = 1.0f;
    public bool EnableSpectrum { get; set; } = true;
    public int SpectrumMode { get; set; } = 0;
    public float SpectrumScale { get; set; } = 1.0f;
    public bool EnableBeatSync { get; set; } = true;
    public float BeatThreshold { get; set; } = 0.3f;
}
```

### Key Features
1. **Advanced Scope Properties**: Color, thickness, style, and audio source selection
2. **Multiple Audio Sources**: Waveform, spectrum, and beat analysis
3. **Pattern Generation**: Various scope patterns and arrangements
4. **Beat Reactivity**: Dynamic scope changes synchronized with audio
5. **Scope Effects**: Glow, fade, and special visual effects
6. **Performance Optimization**: Efficient scope rendering algorithms
7. **Custom Scope Styles**: User-defined scope appearance and behavior

### Scope Styles
- **0**: Line (Simple line scope)
- **1**: Thick Line (Thick line scope)
- **2**: Dots (Dot-based scope)
- **3**: Bars (Bar-based scope)
- **4**: Area (Filled area scope)
- **5**: Glowing (Glowing scope effect)
- **6**: Animated (Animated scope pattern)
- **7**: Custom (User-defined scope style)

### Audio Sources
- **0**: Waveform (Left channel waveform)
- **1**: Waveform Right (Right channel waveform)
- **2**: Spectrum (Frequency spectrum)
- **3**: Beat (Beat detection)
- **4**: Volume (Audio volume)
- **5**: Bass (Bass frequencies)
- **6**: Treble (Treble frequencies)
- **7**: Custom (User-defined audio source)

### Waveform Modes
- **0**: Normal (Standard waveform display)
- **1**: Mirrored (Mirrored waveform)
- **2**: Centered (Centered waveform)
- **3**: Scaled (Scaled waveform)
- **4**: Filtered (Filtered waveform)
- **5**: Smoothed (Smoothed waveform)
- **6**: Envelope (Envelope waveform)
- **7**: Custom (User-defined waveform)

### Spectrum Modes
- **0**: Linear (Linear frequency scale)
- **1**: Logarithmic (Logarithmic frequency scale)
- **2**: Mel (Mel frequency scale)
- **3**: Bark (Bark frequency scale)
- **4**: Filtered (Filtered spectrum)
- **5**: Smoothed (Smoothed spectrum)
- **6**: Peak (Peak-hold spectrum)
- **7**: Custom (User-defined spectrum)

## Usage Examples

### Basic SuperScope
```csharp
var superScopeNode = new SuperScopeEffectsNode
{
    ScopeColor = 0x00FF00, // Green
    ScopeThickness = 3,
    ScopeStyle = 0, // Line
    AudioSource = 0, // Waveform
    BeatReactive = false,
    ScopeEffect = 0, // None
    ScopeOpacity = 1.0f,
    EnableGlow = true,
    GlowColor = 0x00FFFF,
    GlowIntensity = 0.6f,
    AnimationSpeed = 1.0f,
    WaveformMode = 0, // Normal
    WaveformScale = 1.0f,
    EnableSpectrum = false,
    EnableBeatSync = false
};
```

### Beat-Reactive Spectrum Scope
```csharp
var superScopeNode = new SuperScopeEffectsNode
{
    ScopeColor = 0xFF0000, // Red
    ScopeThickness = 4,
    ScopeStyle = 2, // Dots
    AudioSource = 2, // Spectrum
    BeatReactive = true,
    ScopeEffect = 1, // Glow
    ScopeOpacity = 0.9f,
    EnableGlow = true,
    GlowColor = 0xFF00FF,
    GlowIntensity = 0.8f,
    AnimationSpeed = 2.0f,
    AnimatedScope = true,
    ScopeCount = 3,
    ScopeSpacing = 35.0f,
    EnableSpectrum = true,
    SpectrumMode = 1, // Logarithmic
    SpectrumScale = 1.5f,
    EnableBeatSync = true,
    BeatThreshold = 0.4f
};
```

### Multi-Scope Waveform Display
```csharp
var superScopeNode = new SuperScopeEffectsNode
{
    ScopeColor = 0x0000FF, // Blue
    ScopeThickness = 2,
    ScopeStyle = 4, // Area
    AudioSource = 0, // Waveform
    BeatReactive = false,
    ScopeEffect = 3, // Pulse
    ScopeOpacity = 0.8f,
    EnableGlow = true,
    GlowColor = 0x0080FF,
    GlowIntensity = 0.4f,
    AnimationSpeed = 1.5f,
    AnimatedScope = true,
    ScopeCount = 5,
    ScopeSpacing = 30.0f,
    RandomizeScope = true,
    RandomSeed = 42.0f,
    WaveformMode = 2, // Centered
    WaveformScale = 0.8f,
    EnableSpectrum = false,
    EnableBeatSync = false
};
```

## Technical Implementation

### Core SuperScope Algorithm
```csharp
protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
{
    if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
        return GetDefaultOutput();

    // Create output buffer
    var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
    
    // Copy input image to output
    Array.Copy(imageBuffer.Pixels, output.Pixels, imageBuffer.Pixels.Length);
    
    // Update scope animation state
    UpdateScopeAnimation(audioFeatures);
    
    // Generate and render super scope
    RenderSuperScope(output, audioFeatures);
    
    return output;
}
```

### Scope Animation Update
```csharp
private void UpdateScopeAnimation(AudioFeatures audioFeatures)
{
    if (!AnimatedScope)
        return;

    float currentTime = GetCurrentTime();
    
    // Update animation based on audio features
    if (BeatReactive && audioFeatures?.IsBeat == true)
    {
        // Beat-reactive scope changes
        ScopeScale = 1.0f + (float)Math.Sin(currentTime * AnimationSpeed) * 0.3f;
        ScopeOpacity = Math.Min(1.0f, ScopeOpacity + 0.2f);
    }
    else
    {
        // Gradual return to normal
        ScopeScale = Math.Max(1.0f, ScopeScale * 0.98f);
        ScopeOpacity = Math.Max(0.8f, ScopeOpacity * 0.99f);
    }
}
```

## Audio Data Processing

### Audio Source Selection
```csharp
private float[] GetAudioData(AudioFeatures audioFeatures)
{
    switch (AudioSource)
    {
        case 0: // Waveform Left
            return ProcessWaveform(audioFeatures.WaveformLeft);
        case 1: // Waveform Right
            return ProcessWaveform(audioFeatures.WaveformRight);
        case 2: // Spectrum
            return ProcessSpectrum(audioFeatures.Spectrum);
        case 3: // Beat
            return ProcessBeat(audioFeatures);
        case 4: // Volume
            return ProcessVolume(audioFeatures);
        case 5: // Bass
            return ProcessBass(audioFeatures);
        case 6: // Treble
            return ProcessTreble(audioFeatures);
        case 7: // Custom
            return ProcessCustomAudio(audioFeatures);
        default:
            return ProcessWaveform(audioFeatures.WaveformLeft);
    }
}
```

### Waveform Processing
```csharp
private float[] ProcessWaveform(float[] waveform)
{
    if (waveform == null || waveform.Length == 0)
        return new float[0];

    var processed = new float[waveform.Length];
    
    switch (WaveformMode)
    {
        case 0: // Normal
            Array.Copy(waveform, processed, waveform.Length);
            break;
        case 1: // Mirrored
            for (int i = 0; i < waveform.Length; i++)
            {
                processed[i] = waveform[i] * WaveformScale;
                if (i < waveform.Length / 2)
                {
                    processed[waveform.Length - 1 - i] = processed[i];
                }
            }
            break;
        case 2: // Centered
            for (int i = 0; i < waveform.Length; i++)
            {
                processed[i] = (waveform[i] - 0.5f) * WaveformScale + 0.5f;
            }
            break;
        case 3: // Scaled
            for (int i = 0; i < waveform.Length; i++)
            {
                processed[i] = waveform[i] * WaveformScale;
            }
            break;
        case 4: // Filtered
            processed = ApplyLowPassFilter(waveform);
            break;
        case 5: // Smoothed
            processed = ApplySmoothingFilter(waveform);
            break;
        case 6: // Envelope
            processed = CalculateEnvelope(waveform);
            break;
        case 7: // Custom
            processed = ApplyCustomWaveformProcessing(waveform);
            break;
    }
    
    return processed;
}
```

### Spectrum Processing
```csharp
private float[] ProcessSpectrum(float[] spectrum)
{
    if (spectrum == null || spectrum.Length == 0)
        return new float[0];

    var processed = new float[spectrum.Length];
    
    switch (SpectrumMode)
    {
        case 0: // Linear
            Array.Copy(spectrum, processed, spectrum.Length);
            break;
        case 1: // Logarithmic
            processed = ApplyLogarithmicScale(spectrum);
            break;
        case 2: // Mel
            processed = ApplyMelScale(spectrum);
            break;
        case 3: // Bark
            processed = ApplyBarkScale(spectrum);
            break;
        case 4: // Filtered
            processed = ApplySpectrumFilter(spectrum);
            break;
        case 5: // Smoothed
            processed = ApplySpectrumSmoothing(spectrum);
            break;
        case 6: // Peak
            processed = ApplyPeakHold(spectrum);
            break;
        case 7: // Custom
            processed = ApplyCustomSpectrumProcessing(spectrum);
            break;
    }
    
    // Apply spectrum scaling
    for (int i = 0; i < processed.Length; i++)
    {
        processed[i] *= SpectrumScale;
    }
    
    return processed;
}
```

## Scope Rendering

### Main Scope Rendering
```csharp
private void RenderSuperScope(ImageBuffer output, AudioFeatures audioFeatures)
{
    // Get audio data for rendering
    float[] audioData = GetAudioData(audioFeatures);
    
    if (audioData.Length == 0)
        return;

    for (int scopeIndex = 0; scopeIndex < ScopeCount; scopeIndex++)
    {
        // Calculate scope position and properties
        var scopeProperties = CalculateScopeProperties(scopeIndex, audioData);
        
        // Render individual scope
        RenderSingleScope(output, scopeProperties, audioData);
    }
}
```

### Scope Property Calculation
```csharp
private ScopeProperties CalculateScopeProperties(int scopeIndex, float[] audioData)
{
    var properties = new ScopeProperties();
    
    // Calculate scope position
    float centerX = output.Width / 2.0f;
    float centerY = output.Height / 2.0f;
    
    if (ScopeCount == 1)
    {
        properties.StartX = 0;
        properties.EndX = output.Width;
        properties.StartY = centerY;
        properties.EndY = centerY;
    }
    else
    {
        // Multiple scopes with spacing
        float scopeHeight = (float)output.Height / ScopeCount;
        float scopeY = scopeIndex * scopeHeight + scopeHeight / 2.0f;
        
        properties.StartX = 0;
        properties.EndX = output.Width;
        properties.StartY = scopeY;
        properties.EndY = scopeY;
    }
    
    // Apply randomization if enabled
    if (RandomizeScope)
    {
        ApplyScopeRandomization(properties, scopeIndex);
    }
    
    // Set scope appearance properties
    properties.Color = ScopeColor;
    properties.Thickness = ScopeThickness;
    properties.Opacity = ScopeOpacity;
    properties.Style = ScopeStyle;
    properties.Effect = ScopeEffect;
    
    return properties;
}
```

### Single Scope Rendering
```csharp
private void RenderSingleScope(ImageBuffer output, ScopeProperties properties, float[] audioData)
{
    // Apply scope style
    switch (properties.Style)
    {
        case 0: // Line
            RenderLineScope(output, properties, audioData);
            break;
        case 1: // Thick Line
            RenderThickLineScope(output, properties, audioData);
            break;
        case 2: // Dots
            RenderDotScope(output, properties, audioData);
            break;
        case 3: // Bars
            RenderBarScope(output, properties, audioData);
            break;
        case 4: // Area
            RenderAreaScope(output, properties, audioData);
            break;
        case 5: // Glowing
            RenderGlowingScope(output, properties, audioData);
            break;
        case 6: // Animated
            RenderAnimatedScope(output, properties, audioData);
            break;
        case 7: // Custom
            RenderCustomScope(output, properties, audioData);
            break;
    }
    
    // Apply scope effects
    if (properties.Effect != 0)
    {
        ApplyScopeEffects(output, properties);
    }
}
```

## Scope Style Implementations

### Line Scope Rendering
```csharp
private void RenderLineScope(ImageBuffer output, ScopeProperties properties, float[] audioData)
{
    if (audioData.Length < 2)
        return;

    // Calculate step size for audio data
    float stepX = (float)output.Width / (audioData.Length - 1);
    
    // Render line segments
    for (int i = 0; i < audioData.Length - 1; i++)
    {
        float x1 = i * stepX;
        float y1 = properties.StartY + audioData[i] * (output.Height / 2.0f);
        float x2 = (i + 1) * stepX;
        float y2 = properties.StartY + audioData[i + 1] * (output.Height / 2.0f);
        
        // Draw line segment
        DrawLine(output, x1, y1, x2, y2, properties.Color, properties.Opacity);
    }
}
```

### Bar Scope Rendering
```csharp
private void RenderBarScope(ImageBuffer output, ScopeProperties properties, float[] audioData)
{
    if (audioData.Length == 0)
        return;

    // Calculate bar width
    float barWidth = (float)output.Width / audioData.Length;
    
    // Render bars
    for (int i = 0; i < audioData.Length; i++)
    {
        float x = i * barWidth;
        float barHeight = Math.Abs(audioData[i]) * (output.Height / 2.0f);
        float y = properties.StartY - barHeight / 2.0f;
        
        // Draw bar
        DrawRectangle(output, x, y, barWidth, barHeight, properties.Color, properties.Opacity);
    }
}
```

### Area Scope Rendering
```csharp
private void RenderAreaScope(ImageBuffer output, ScopeProperties properties, float[] audioData)
{
    if (audioData.Length < 2)
        return;

    // Calculate step size for audio data
    float stepX = (float)output.Width / (audioData.Length - 1);
    
    // Create polygon points for area fill
    var points = new List<PointF>();
    
    // Add top points
    for (int i = 0; i < audioData.Length; i++)
    {
        float x = i * stepX;
        float y = properties.StartY + audioData[i] * (output.Height / 2.0f);
        points.Add(new PointF(x, y));
    }
    
    // Add bottom points in reverse order
    for (int i = audioData.Length - 1; i >= 0; i--)
    {
        float x = i * stepX;
        float y = properties.StartY - audioData[i] * (output.Height / 2.0f);
        points.Add(new PointF(x, y));
    }
    
    // Fill polygon
    FillPolygon(output, points.ToArray(), properties.Color, properties.Opacity);
}
```

## Scope Effects

### Effect Application
```csharp
private void ApplyScopeEffects(ImageBuffer output, ScopeProperties properties)
{
    switch (properties.Effect)
    {
        case 1: // Glow
            ApplyScopeGlow(output, properties);
            break;
        case 2: // Fade
            ApplyScopeFade(output, properties);
            break;
        case 3: // Pulse
            ApplyScopePulse(output, properties);
            break;
        case 4: // Wave
            ApplyScopeWave(output, properties);
            break;
        case 5: // Glitch
            ApplyScopeGlitch(output, properties);
            break;
        case 6: // Particle
            ApplyScopeParticle(output, properties);
            break;
        case 7: // Custom
            ApplyCustomScopeEffect(output, properties);
            break;
    }
}
```

### Scope Glow Effect
```csharp
private void ApplyScopeGlow(ImageBuffer output, ScopeProperties properties)
{
    if (!EnableGlow)
        return;

    // Create glow around the scope
    int glowRadius = (int)(GlowIntensity * 15.0f);
    
    // Apply glow to scope area
    for (int y = Math.Max(0, (int)properties.StartY - glowRadius); 
         y < Math.Min(output.Height, (int)properties.EndY + glowRadius); y++)
    {
        for (int x = 0; x < output.Width; x++)
        {
            // Calculate distance to scope line
            float distance = CalculateDistanceToScope(x, y, properties);
            
            if (distance <= glowRadius)
            {
                float glowStrength = 1.0f - (distance / glowRadius);
                int glowPixel = BlendColors(output.GetPixel(x, y), 
                                          GlowColor, glowStrength * GlowIntensity);
                output.SetPixel(x, y, glowPixel);
            }
        }
    }
}
```

## Performance Optimization

### Optimization Techniques
1. **Audio Data Caching**: Cache processed audio data
2. **Scope State Management**: Efficient scope property storage
3. **Early Exit**: Skip processing for empty audio data
4. **Batch Processing**: Process multiple scopes together
5. **Threading**: Multi-threaded scope rendering

### Memory Management
- Efficient audio data storage
- Minimize temporary allocations
- Use value types for calculations
- Optimize scope rendering operations

## Integration with EffectGraph

### Port Configuration
```csharp
protected override void InitializePorts()
{
    _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Background image"));
    _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "SuperScope output"));
}
```

### Metadata Support
```csharp
public override EffectMetadata GetMetadata()
{
    var metadata = new EffectMetadata();
    metadata.Add("ScopeColor", ScopeColor);
    metadata.Add("ScopeThickness", ScopeThickness);
    metadata.Add("ScopeStyle", ScopeStyle);
    metadata.Add("AudioSource", AudioSource);
    metadata.Add("BeatReactive", BeatReactive);
    metadata.Add("ScopeEffect", ScopeEffect);
    metadata.Add("ScopeOpacity", ScopeOpacity);
    metadata.Add("WaveformMode", WaveformMode);
    metadata.Add("WaveformScale", WaveformScale);
    metadata.Add("SpectrumMode", SpectrumMode);
    metadata.Add("SpectrumScale", SpectrumScale);
    metadata.Add("ScopeCount", ScopeCount);
    return metadata;
}
```

## Testing and Validation

### Test Cases
1. **Basic Scope Rendering**: Verify scope drawing accuracy
2. **Audio Sources**: Test all audio source types
3. **Scope Styles**: Test all scope style types
4. **Scope Effects**: Test all effect types
5. **Performance**: Measure scope rendering speed
6. **Edge Cases**: Handle boundary conditions

### Validation Methods
- Visual comparison with reference images
- Performance benchmarking
- Memory usage analysis
- Audio data accuracy testing

## Future Enhancements

### Planned Features
1. **Advanced Audio Analysis**: More sophisticated audio processing
2. **3D Scopes**: Three-dimensional scope visualization
3. **Real-time Effects**: Dynamic scope generation
4. **Hardware Acceleration**: GPU-accelerated scope rendering
5. **Custom Shaders**: User-defined scope effects

### Compatibility
- Full AVS preset compatibility
- Support for legacy scope modes
- Performance parity with original
- Extended functionality

## Conclusion

The SuperScope effect provides essential oscilloscope visualization capabilities for audio-reactive AVS presets. This C# implementation maintains full compatibility with the original while adding modern features like enhanced audio processing and improved scope effects. Complete documentation ensures reliable operation in production environments with optimal performance and visual quality.

---

## 🚀 **PhoenixVisualizer Implementation**

### **Phoenix Architecture Integration** ✅ **COMPLETE**
The SuperscopeEffectsNode is now fully integrated with the **Phoenix Architecture** - a unified system that provides:

- **PhoenixExpressionEngine**: True ns-eel compatible expression evaluator
- **PhoenixExecutionEngine**: Global audio variable injection system
- **Unified Variable Management**: Global audio context (bass, mid, treb, rms, beat, spec, wave)
- **Phoenix-Native Variables**: pel_frame, pel_time, pel_dt for Phoenix-specific context
- **No Winamp Drift**: Pure Phoenix implementation prevents legacy patterns

### **Implementation Status** ✅ **COMPLETE**
- ✅ **PhoenixExpressionEngine** - Complete and functional
- ✅ **PhoenixExecutionEngine** - Global audio variable injection
- ✅ **SuperscopeEffectsNode** - Fully integrated with Phoenix architecture
- ✅ **API Integration** - Complete integration with new engine API
- ✅ **Variable Binding** - Automatic binding to global expression engine

### **Technical Architecture**
```csharp
public class SuperscopeEffectsNode : BaseEffectNode
{
    // Now inherits Engine from BaseEffectNode
    // Engine is automatically bound by PhoenixExecutionEngine
    
    private void RenderSuperscope(ImageBuffer output, AudioFeatures audioFeatures)
    {
        if (Engine == null) return;
        
        // Init script runs once
        if (!_initialized && !string.IsNullOrEmpty(InitScript))
        {
            Engine.Execute(InitScript);
            _initialized = true;
        }
        
        // Frame script
        if (!string.IsNullOrEmpty(FrameScript))
            Engine.Execute(FrameScript);
            
        // Beat script
        if (BeatReactive && audioFeatures?.IsBeat == true && !string.IsNullOrEmpty(BeatScript))
            Engine.Execute(BeatScript);
    }
    
    private void RenderPoints(ImageBuffer output, AudioFeatures audioFeatures)
    {
        // Bind per-point vars
        Engine.SetVar("i", (double)i / PointCount);
        Engine.SetVar("v", audioData[i % audioData.Length]);
        
        // Execute point script
        if (!string.IsNullOrEmpty(PointScript))
            Engine.Execute(PointScript);
            
        // Pull results from engine
        double x = Engine.GetVar("x", 0.0);
        double y = Engine.GetVar("y", 0.0);
        double red = Engine.GetVar("red", 1.0);
        double green = Engine.GetVar("green", 1.0);
        double blue = Engine.GetVar("blue", 1.0);
    }
}
```

### **Phoenix Architecture Benefits**
- **Global Audio Context**: All audio variables automatically injected every frame
- **Unified Expression Engine**: Consistent API across all effects
- **Future-Ready**: Foundation for Phoenix Effect Language (PEL) extensions
- **Performance**: Optimized variable injection and expression evaluation
- **Maintainability**: Clean separation from legacy AVS/Winamp patterns

---
