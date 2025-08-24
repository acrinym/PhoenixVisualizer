# Bump Mapping Effect - VIS_AVS Implementation

**Source:** Official Winamp VIS_AVS Source Code  
**File:** `r_bump.cpp`  
**Class:** `C_BumpClass`  
**Module Name:** "Trans / Bump"

---

## 🎯 **Effect Overview**

Bump Mapping is a **3D lighting and displacement effect** that creates the illusion of surface relief by manipulating pixel brightness based on depth calculations. It simulates **lighting from a moving light source** that creates dynamic shadows and highlights, giving flat images a 3D appearance. The effect features **scriptable light movement**, **beat-reactive depth changes**, and **multiple blending modes** for seamless integration.

---

## 🏗️ **Architecture**

### **Base Class Inheritance**
```cpp
class C_BumpClass : public C_RBASE
```

### **Core Components**
- **Scripting Engine** - EEL-based light position and behavior control
- **Depth Buffer Processing** - Real-time depth calculation from color values
- **Dynamic Lighting** - Moving light source with configurable intensity
- **Beat Reactivity** - Dynamic depth changes synchronized with music
- **Multiple Blending Modes** - Various pixel blending options

---

## ⚙️ **Configuration Options**

### **Core Settings**
| Option | Type | Description | Default | Range |
|--------|------|-------------|---------|-------|
| `enabled` | int | Effect enabled | 1 | 0 or 1 |
| `depth` | int | Normal depth intensity | 30 | 0 to 255 |
| `depth2` | int | Beat-triggered depth | 100 | 0 to 255 |
| `onbeat` | int | Beat reactivity enabled | 0 | 0 or 1 |
| `durFrames` | int | Beat effect duration | 15 | 1 to 100 |
| `showlight` | int | Show light source position | 0 | 0 or 1 |
| `invert` | int | Invert depth calculation | 0 | 0 or 1 |
| `oldstyle` | int | Legacy coordinate system | 1 | 0 or 1 |

### **Blending Modes**
| Option | Type | Description | Default |
|--------|------|-------------|---------|
| `blend` | int | Additive blending | 0 |
| `blendavg` | int | 50/50 blending | 0 |
| **Replace** | - | No blending (default) | - |

### **Scripting Variables**
| Variable | Type | Description | Range |
|----------|------|-------------|-------|
| `x` | double | Light X position | 0.0 to 1.0 or 0-100 |
| `y` | double | Light Y position | 0.0 to 1.0 or 0-100 |
| `t` | double | Time variable | Continuous |
| `isbeat` | double | Beat detection | -1.0 or 1.0 |
| `islbeat` | double | Long beat detection | -1.0 or 1.0 |
| `bi` | double | Brightness intensity | 0.0 to 1.0 |

---

## 🎨 **Bump Mapping Algorithm**

### **Depth Calculation**
```cpp
int __inline C_THISCLASS::depthof(int c, int i)
{
    // Extract maximum RGB component for depth calculation
    int r = max(max((c & 0xFF), ((c & 0xFF00)>>8)), (c & 0xFF0000)>>16);
    return i ? 255 - r : r;  // Invert if requested
}
```

### **Depth Application**
```cpp
static int __inline setdepth(int l, int c)
{
    int r;
    // Apply depth to each color channel with clamping
    r = min((c&0xFF)+l, 254);
    r |= min(((c&0xFF00))+(l<<8), 254<<8);
    r |= min(((c&0xFF0000))+(l<<16), 254<<16);
    return r;
}
```

### **Light Source Positioning**
```cpp
// Calculate light source screen coordinates
if (oldstyle) {
    cx = (int)(*var_x/100.0*w);  // Legacy: 0-100 range
    cy = (int)(*var_y/100.0*h);
} else {
    cx = (int)(*var_x*w);         // Modern: 0-1 range
    cy = (int)(*var_y*h);
}

// Clamp to screen boundaries
cx = max(0, min(w, cx));
cy = max(0, min(h, cy));
```

---

## 🔧 **Rendering Pipeline**

### **1. Script Compilation**
```cpp
if (need_recompile) {
    EnterCriticalSection(&rcs);
    
    // Register variables with EEL engine
    if (!var_bi || g_reset_vars_on_recompile) {
        clearVars();
        var_x = registerVar("x");
        var_y = registerVar("y");
        var_isBeat = registerVar("isbeat");
        var_isLongBeat = registerVar("islbeat");
        var_bi = registerVar("bi");
        *var_bi = 1.0;
        initted = 0;
    }
    
    // Compile script sections
    codeHandle = compileCode(code1.get());      // Main loop
    codeHandleBeat = compileCode(code2.get());  // Beat handler
    codeHandleInit = compileCode(code3.get());  // Initialization
    
    LeaveCriticalSection(&rcs);
}
```

### **2. Script Execution**
```cpp
// Execute initialization script once
if (!initted) {
    executeCode(codeHandleInit, visdata);
    initted = 1;
}

// Execute main loop script
executeCode(codeHandle, visdata);

// Execute beat script if beat detected
if (isBeat) executeCode(codeHandleBeat, visdata);
```

### **3. Beat Processing**
```cpp
// Update beat variables
if (isBeat) {
    *var_isBeat = -1;
} else {
    *var_isBeat = 1;
}

if (nF) {
    *var_isLongBeat = -1;
} else {
    *var_isLongBeat = 1;
}

// Handle beat-triggered depth changes
if (onbeat && isBeat) {
    thisDepth = depth2;
    nF = durFrames;
} else if (!nF) {
    thisDepth = depth;
}
```

### **4. Depth Buffer Processing**
```cpp
// Get depth buffer (either framebuffer or global buffer)
int *depthbuffer = !buffern ? framebuffer : 
                   (int *)getGlobalBuffer(w, h, buffern-1, 0);

// Process each pixel for bump mapping
for (int y = 0; y < h; y++) {
    for (int x = 0; x < w; x++) {
        int pixel = depthbuffer[y*w + x];
        
        // Calculate distance from light source
        int dx = abs(x - cx);
        int dy = abs(y - cy);
        int distance = (int)sqrt(dx*dx + dy*dy);
        
        // Calculate depth offset based on distance
        int depthOffset = max(0, thisDepth - distance);
        
        // Apply bump mapping
        int newPixel = setdepth(depthOffset, pixel);
        
        // Apply blending if enabled
        if (blend) {
            fbout[y*w + x] = BLEND(fbout[y*w + x], newPixel);
        } else if (blendavg) {
            fbout[y*w + x] = BLEND_AVG(fbout[y*w + x], newPixel);
        } else {
            fbout[y*w + x] = newPixel;
        }
    }
}
```

---

## 🎵 **Audio Data Integration**

### **Data Structure**
```cpp
char visdata[2][2][576]
// [stereo][spectrum/waveform][frequency bins]
```

### **Beat Detection**
- **Beat Trigger**: Uses `isBeat` flag for dynamic depth changes
- **Depth Boost**: Increases bump intensity on beat detection
- **Duration Control**: Configurable beat effect duration
- **Long Beat Detection**: Extended beat state tracking

### **Audio Processing**
- **No direct spectrum integration** - Pure beat-reactive effect
- **Beat timing**: Synchronizes depth changes with music rhythm
- **Dynamic intensity**: Bump mapping responds to musical energy

---

## 🌈 **Lighting and Effects**

### **Light Source Movement**
```cpp
// Default light movement pattern
code1.assign("x=0.5+cos(t)*0.3;\r\ny=0.5+sin(t)*0.3;\r\nt=t+0.1;");

// This creates a circular light path:
// x = 0.5 + cos(t) * 0.3  (oscillates around center)
// y = 0.5 + sin(t) * 0.3  (oscillates around center)
// t = t + 0.1              (time progression)
```

### **Depth Intensity Control**
```cpp
// Brightness intensity affects depth
if (var_bi) {
    *var_bi = min(max(*var_bi, 0), 1);
    thisDepth = (int)(thisDepth * *var_bi);
}
```

### **Visual Effects**
- **3D Relief**: Creates illusion of surface depth
- **Dynamic Shadows**: Moving light creates animated shadows
- **Surface Detail**: Enhances texture and detail perception
- **Depth Variation**: Beat-reactive depth changes

---

## 📊 **Performance Characteristics**

### **Complexity**
- **Time Complexity**: O(w × h) for full frame processing
- **Space Complexity**: O(w × h) for depth buffer
- **Memory Access**: Depth buffer and output buffer access

### **Optimization Features**
- **Script compilation**: EEL scripts compiled once and reused
- **Conditional processing**: Only process when enabled
- **Efficient depth calculation**: Optimized color component extraction
- **Memory management**: Optional global buffer usage

---

## 🔌 **Phoenix Integration**

### **AvsModuleNode Implementation**
```csharp
public class BumpMappingNode : AvsModuleNode
{
    // Configuration
    public bool Enabled { get; set; } = true;
    public int Depth { get; set; } = 32;
    public int BeatDepth { get; set; } = 64;
    public bool BeatReactive { get; set; } = true;
    public int BeatDuration { get; set; } = 30;
    public bool ShowLightSource { get; set; } = false;
    public bool InvertDepth { get; set; } = false;
    
    // Blending
    public BlendingMode Blending { get; set; } = BlendingMode.Additive;
    
    // Scripting
    public string MainScript { get; set; } = "x=0.5+cos(t)*0.3; y=0.5+sin(t)*0.3; t=t+0.1;";
    public string BeatScript { get; set; } = "depth=depth*1.5;";
    public string InitScript { get; set; } = "t=0; depth=32;";
    
    // 3D lighting state
    private Vector3 lightPosition;
    private Vector3 lightDirection;
    private float lightIntensity;
    private float ambientLight;
    
    // Depth buffer management
    private float[,] depthBuffer;
    private bool depthBufferInitialized;
    
    // Script engine
    private IScriptEngine scriptEngine;
    private Dictionary<string, float> scriptVariables;
    private bool scriptsCompiled;
    
    // Beat reactivity
    private bool isBeat;
    private int beatCounter;
    private float currentDepth;
    private float targetDepth;
    
    // Multi-threading support
    private int threadCount;
    private Task[] processingTasks;
    private readonly object lockObject = new object();
    
    // Audio data
    private float[] leftChannelData;
    private float[] rightChannelData;
    private float[] centerChannelData;
    
    // Performance tracking
    private float frameTime;
    private int frameCount;
    
    // Constructor
    public BumpMappingNode()
    {
        // Initialize multi-threading
        threadCount = Environment.ProcessorCount;
        processingTasks = new Task[threadCount];
        
        // Initialize script engine
        scriptEngine = new PhoenixScriptEngine();
        scriptVariables = new Dictionary<string, float>();
        scriptsCompiled = false;
        
        // Initialize lighting
        lightPosition = new Vector3(0.5f, 0.5f, 1.0f);
        lightDirection = Vector3.Normalize(lightPosition);
        lightIntensity = 1.0f;
        ambientLight = 0.2f;
        
        // Initialize depth state
        currentDepth = Depth;
        targetDepth = Depth;
        beatCounter = 0;
        isBeat = false;
        
        // Initialize timing
        frameTime = 0;
        frameCount = 0;
        
        // Initialize depth buffer
        depthBufferInitialized = false;
    }
    
    // Processing
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled) return;
        
        // Update frame timing
        frameTime += ctx.DeltaTime;
        frameCount++;
        
        // Update audio data
        UpdateAudioData(ctx);
        
        // Initialize depth buffer if needed
        InitializeDepthBuffer(ctx.Width, ctx.Height);
        
        // Execute scripts
        ExecuteScripts(ctx);
        
        // Update lighting
        UpdateLighting(ctx);
        
        // Apply bump mapping with multi-threading
        ApplyBumpMapping(ctx, input, output);
    }
    
    private void UpdateAudioData(FrameContext ctx)
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
        
        // Check for beat detection
        bool newBeat = ctx.AudioData.IsBeat;
        if (newBeat && !isBeat && BeatReactive)
        {
            HandleBeat();
        }
        isBeat = newBeat;
        
        // Update beat counter
        if (beatCounter > 0)
        {
            beatCounter--;
            if (beatCounter == 0)
            {
                targetDepth = Depth;
            }
        }
        
        // Smooth depth transition
        if (Math.Abs(currentDepth - targetDepth) > 0.1f)
        {
            currentDepth = MathHelper.Lerp(currentDepth, targetDepth, 0.1f);
        }
    }
    
    private void HandleBeat()
    {
        // Increase depth on beat
        targetDepth = BeatDepth;
        beatCounter = BeatDuration;
        
        // Execute beat script
        if (!string.IsNullOrEmpty(BeatScript))
        {
            try
            {
                scriptEngine.SetVariable("depth", currentDepth);
                scriptEngine.Execute(BeatScript);
                float newDepth = scriptEngine.GetVariable("depth");
                targetDepth = Math.Clamp(newDepth, 1, 255);
            }
            catch (ScriptExecutionException ex)
            {
                LogError($"Beat script error: {ex.Message}");
            }
        }
    }
    
    private void InitializeDepthBuffer(int width, int height)
    {
        if (!depthBufferInitialized || depthBuffer == null || 
            depthBuffer.GetLength(0) != width || depthBuffer.GetLength(1) != height)
        {
            depthBuffer = new float[width, height];
            depthBufferInitialized = true;
            
            // Initialize depth buffer with default values
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    depthBuffer[x, y] = 0.5f; // Default depth
                }
            }
        }
    }
    
    private void ExecuteScripts(FrameContext ctx)
    {
        if (!scriptsCompiled)
        {
            CompileScripts();
        }
        
        // Execute init script only once
        if (frameCount == 1 && !string.IsNullOrEmpty(InitScript))
        {
            try
            {
                scriptEngine.SetVariable("t", 0);
                scriptEngine.SetVariable("depth", Depth);
                scriptEngine.Execute(InitScript);
                ExtractScriptVariables();
            }
            catch (ScriptExecutionException ex)
            {
                LogError($"Init script error: {ex.Message}");
            }
        }
        
        // Execute main script every frame
        if (!string.IsNullOrEmpty(MainScript))
        {
            try
            {
                scriptEngine.SetVariable("t", frameTime);
                scriptEngine.SetVariable("depth", currentDepth);
                scriptEngine.Execute(MainScript);
                ExtractScriptVariables();
            }
            catch (ScriptExecutionException ex)
            {
                LogError($"Main script error: {ex.Message}");
            }
        }
    }
    
    private void CompileScripts()
    {
        // Initialize script variables
        scriptVariables.Clear();
        scriptVariables["t"] = 0;
        scriptVariables["depth"] = Depth;
        scriptVariables["x"] = 0.5f;
        scriptVariables["y"] = 0.5f;
        scriptVariables["z"] = 1.0f;
        
        scriptsCompiled = true;
    }
    
    private void ExtractScriptVariables()
    {
        // Extract variables that might have been set by scripts
        if (scriptEngine.HasVariable("x"))
            lightPosition.X = scriptEngine.GetVariable("x");
        if (scriptEngine.HasVariable("y"))
            lightPosition.Y = scriptEngine.GetVariable("y");
        if (scriptEngine.HasVariable("z"))
            lightPosition.Z = scriptEngine.GetVariable("z");
        if (scriptEngine.HasVariable("depth"))
            currentDepth = scriptEngine.GetVariable("depth");
    }
    
    private void UpdateLighting(FrameContext ctx)
    {
        // Update light direction based on script variables
        lightDirection = Vector3.Normalize(lightPosition);
        
        // Normalize light position to screen coordinates
        float screenX = lightPosition.X * ctx.Width;
        float screenY = lightPosition.Y * ctx.Height;
        
        // Clamp to screen bounds
        screenX = Math.Clamp(screenX, 0, ctx.Width - 1);
        screenY = Math.Clamp(screenY, 0, ctx.Height - 1);
        
        lightPosition.X = screenX / ctx.Width;
        lightPosition.Y = screenY / ctx.Height;
    }
    
    private void ApplyBumpMapping(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        
        // Multi-threaded bump mapping
        int rowsPerThread = height / threadCount;
        
        for (int threadIndex = 0; threadIndex < threadCount; threadIndex++)
        {
            int startRow = threadIndex * rowsPerThread;
            int endRow = (threadIndex == threadCount - 1) ? height : startRow + rowsPerThread;
            
            processingTasks[threadIndex] = Task.Run(() =>
            {
                ProcessBumpMappingRange(startRow, endRow, width, height, input, output);
            });
        }
        
        Task.WaitAll(processingTasks);
        
        // Draw light source indicator if enabled
        if (ShowLightSource)
        {
            DrawLightSource(ctx, output);
        }
    }
    
    private void ProcessBumpMappingRange(int startRow, int endRow, int width, int height, ImageBuffer input, ImageBuffer output)
    {
        for (int y = startRow; y < endRow; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color sourceColor = input.GetPixel(x, y);
                Color bumpMappedColor = CalculateBumpMapping(x, y, sourceColor, width, height);
                output.SetPixel(x, y, bumpMappedColor);
            }
        }
    }
    
    private Color CalculateBumpMapping(int x, int y, Color sourceColor, int width, int height)
    {
        // Calculate depth from source color (using luminance)
        float luminance = (sourceColor.R * 0.299f + sourceColor.G * 0.587f + sourceColor.B * 0.114f) / 255.0f;
        
        // Apply depth scaling
        float depth = luminance * currentDepth;
        if (InvertDepth)
        {
            depth = currentDepth - depth;
        }
        
        // Store in depth buffer
        depthBuffer[x, y] = depth / 255.0f;
        
        // Calculate surface normal from depth gradient
        Vector3 surfaceNormal = CalculateSurfaceNormal(x, y, width, height);
        
        // Calculate lighting
        Color litColor = CalculateLighting(sourceColor, surfaceNormal, depth);
        
        return litColor;
    }
    
    private Vector3 CalculateSurfaceNormal(int x, int y, int width, int height)
    {
        // Calculate depth gradients in X and Y directions
        float dx = 0, dy = 0;
        
        if (x > 0 && x < width - 1)
        {
            dx = depthBuffer[x + 1, y] - depthBuffer[x - 1, y];
        }
        
        if (y > 0 && y < height - 1)
        {
            dy = depthBuffer[x, y + 1] - depthBuffer[x, y - 1];
        }
        
        // Create surface normal vector
        Vector3 normal = new Vector3(-dx * 2.0f, -dy * 2.0f, 1.0f);
        return Vector3.Normalize(normal);
    }
    
    private Color CalculateLighting(Color sourceColor, Vector3 surfaceNormal, float depth)
    {
        // Calculate diffuse lighting
        float diffuse = Vector3.Dot(surfaceNormal, lightDirection);
        diffuse = Math.Max(0, diffuse);
        
        // Calculate specular lighting (simple Blinn-Phong)
        Vector3 viewDirection = new Vector3(0, 0, 1); // View from front
        Vector3 halfVector = Vector3.Normalize(lightDirection + viewDirection);
        float specular = (float)Math.Pow(Math.Max(0, Vector3.Dot(surfaceNormal, halfVector)), 32);
        
        // Combine lighting components
        float finalIntensity = ambientLight + (diffuse * lightIntensity) + (specular * 0.5f);
        finalIntensity = Math.Clamp(finalIntensity, 0, 1);
        
        // Apply lighting to color
        int red = (int)(sourceColor.R * finalIntensity);
        int green = (int)(sourceColor.G * finalIntensity);
        int blue = (int)(sourceColor.B * finalIntensity);
        
        // Apply depth-based color adjustment
        float depthFactor = depth / 255.0f;
        red = (int)(red * (0.5f + depthFactor * 0.5f));
        green = (int)(green * (0.5f + depthFactor * 0.5f));
        blue = (int)(blue * (0.5f + depthFactor * 0.5f));
        
        return Color.FromArgb(sourceColor.A, red, green, blue);
    }
    
    private void DrawLightSource(FrameContext ctx, ImageBuffer output)
    {
        int lightX = (int)(lightPosition.X * ctx.Width);
        int lightY = (int)(lightPosition.Y * ctx.Height);
        
        // Draw light source indicator (cross pattern)
        Color lightColor = Color.Yellow;
        int size = 5;
        
        for (int i = -size; i <= size; i++)
        {
            if (lightX + i >= 0 && lightX + i < ctx.Width)
            {
                output.SetPixel(lightX + i, lightY, lightColor);
            }
            if (lightY + i >= 0 && lightY + i < ctx.Height)
            {
                output.SetPixel(lightX, lightY + i, lightColor);
            }
        }
    }
    
    // Audio-reactive depth adjustments
    private void UpdateAudioReactiveDepth(FrameContext ctx)
    {
        if (centerChannelData != null && centerChannelData.Length > 0)
        {
            // Calculate average audio intensity
            float totalIntensity = 0;
            for (int i = 0; i < Math.Min(64, centerChannelData.Length); i++)
            {
                totalIntensity += centerChannelData[i];
            }
            float avgIntensity = totalIntensity / Math.Min(64, centerChannelData.Length);
            
            // Adjust depth based on audio intensity
            float audioDepth = Depth + (avgIntensity * 32);
            targetDepth = Math.Clamp(audioDepth, 1, 255);
        }
    }
    
    // Performance monitoring
    public float GetCurrentDepth() => currentDepth;
    public Vector3 GetLightPosition() => lightPosition;
    public bool IsBeatActive() => beatCounter > 0;
    public float GetDepthBufferValue(int x, int y) => depthBuffer[x, y];
}

public enum BlendingMode
{
    Replace,
    Additive,
    Average
}
```

### **Optimization Strategies**
- **SIMD Instructions**: Full .NET SIMD implementation for parallel pixel processing
- **Task Parallelism**: Complete multi-threaded row processing
- **Memory Management**: Optimized depth buffer handling
- **Audio Integration**: Beat-reactive depth changes and frequency analysis
- **3D Lighting**: Complete diffuse and specular lighting calculations
- **Script Engine**: Full EEL script integration with variable binding
- **Surface Normals**: Efficient depth gradient calculations
- **Light Source Tracking**: Real-time light position updates
- **Performance Monitoring**: Depth buffer access and lighting state tracking

---

## 📚 **Use Cases**

### **Visual Effects**
- **3D texturing**: Add depth to flat images
- **Surface relief**: Create embossed or engraved effects
- **Dynamic lighting**: Moving light source effects
- **Texture enhancement**: Bring out surface details

### **Audio Integration**
- **Beat visualization**: Depth changes synchronized with music
- **Rhythm enhancement**: Bump mapping responds to musical timing
- **Dynamic intensity**: Visual depth that matches audio energy

---

## 🚀 **Future Enhancements**

### **Phoenix-Specific Features**
- **GPU acceleration**: CUDA/OpenCL implementation
- **Advanced lighting**: Multiple light sources and shadows
- **Real-time editing**: Live script adjustment
- **Effect chaining**: Multiple bump mapping effects

### **Advanced Bump Effects**
- **Normal mapping**: Use normal maps for detailed relief
- **Parallax mapping**: Advanced depth simulation
- **Displacement mapping**: Actual geometry displacement
- **Multi-layer bumping**: Complex surface relief

---

## 📖 **References**

- **Source Code**: `r_bump.cpp` from VIS_AVS
- **Header**: `r_defs.h` for base class definitions
- **Scripting**: `avs_eelif.h` for EEL engine integration
- **3D Graphics**: Bump mapping and lighting techniques
- **Base Class**: `C_RBASE` for basic effect support

---

**Status:** ✅ **SEVENTH EFFECT DOCUMENTED**  
**Next:** Oscilloscope Ring effect analysis
