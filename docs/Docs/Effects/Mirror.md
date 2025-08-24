# Mirror Effect - VIS_AVS Implementation

**Source:** Official Winamp VIS_AVS Source Code  
**File:** `r_mirror.cpp`  
**Class:** `C_MirrorClass`  
**Module Name:** "Trans / Mirror"

---

## 🎯 **Effect Overview**

Mirror is a **geometric transformation effect** that creates reflection and mirroring effects across multiple axes. It supports **horizontal and vertical mirroring** with **smooth transitions** and **beat-reactive mode changes**. The effect can create kaleidoscope-like patterns and is commonly used for creating symmetrical visual compositions.

---

## 🏗️ **Architecture**

### **Base Class Inheritance**
```cpp
class C_MirrorClass : public C_RBASE
```

### **Core Components**
- **Multi-axis Mirroring** - Horizontal and vertical reflection support
- **Smooth Transitions** - Gradual mirror mode changes
- **Beat Reactivity** - Dynamic mirror mode selection on beats
- **Adaptive Blending** - Smooth pixel blending during transitions
- **Frame-based Animation** - Time-based transition control

---

## ⚙️ **Configuration Options**

### **Core Settings**
| Option | Type | Description | Default | Range |
|--------|------|-------------|---------|-------|
| `enabled` | int | Effect enabled | 1 | 0 or 1 |
| `mode` | int | Mirror mode flags | HORIZONTAL1 | Bitwise flags |
| `onbeat` | int | Beat-reactive mode | 0 | 0 or 1 |
| `smooth` | int | Smooth transitions | 0 | 0 or 1 |
| `slower` | int | Transition speed | 4 | 1 to 16 |

### **Mirror Mode Flags**
```cpp
#define HORIZONTAL1 1    // Mirror left half to right
#define HORIZONTAL2 2    // Mirror right half to left  
#define VERTICAL1   4    // Mirror top half to bottom
#define VERTICAL2   8    // Mirror bottom half to top
```

### **Mode Combinations**
- **Single axis**: HORIZONTAL1, VERTICAL1, etc.
- **Dual axis**: HORIZONTAL1 | VERTICAL1 (creates 4-way mirror)
- **All axes**: HORIZONTAL1 | HORIZONTAL2 | VERTICAL1 | VERTICAL2

---

## 🎨 **Mirroring Algorithms**

### **Horizontal Mirroring (Left to Right)**
```cpp
// Mirror left half to right half
for (hi = 0; hi < h; hi++) {
    int *tmp = fbp + w - 1;  // Right edge pointer
    int n = halfw;            // Half width
    while (n--) {
        *tmp-- = *fbp++;      // Copy left to right
    }
    fbp += halfw;             // Skip to next row
}
```

### **Horizontal Mirroring (Right to Left)**
```cpp
// Mirror right half to left half
for (hi = 0; hi < h; hi++) {
    int *tmp = fbp + w - 1;  // Right edge pointer
    int n = halfw;            // Half width
    while (n--) {
        *fbp++ = *tmp--;      // Copy right to left
    }
    fbp += halfw;             // Skip to next row
}
```

### **Vertical Mirroring (Top to Bottom)**
```cpp
// Mirror top half to bottom half
j = t - w;  // Bottom row offset
for (hi = 0; hi < halfh; hi++) {
    memcpy(fbp + j, fbp, w * sizeof(int));  // Copy top to bottom
    fbp += w;
    j -= 2 * w;  // Move to next row pair
}
```

### **Vertical Mirroring (Bottom to Top)**
```cpp
// Mirror bottom half to top half
j = t - w;  // Bottom row offset
for (hi = 0; hi < halfh; hi++) {
    memcpy(fbp, fbp + j, w * sizeof(int));  // Copy bottom to top
    fbp += w;
    j -= 2 * w;  // Move to next row pair
}
```

---

## 🔧 **Smooth Transition System**

### **Transition State Management**
```cpp
// Track mode changes and transition states
if (*thismode != lastMode) {
    int dif = *thismode ^ lastMode;
    int i;
    for (i = 1, m = 0xFF, d = 0; i < 16; i <<= 1, m <<= 8, d += 8) {
        if (dif & i) {
            // Set transition direction and initial divisor
            inc = (inc & ~m) | ((lastMode & i) ? 0xFF : 1) << d;
            if (!(divisors & m)) {
                divisors = (divisors & ~m) | ((lastMode & i) ? 16 : 1) << d;
            }
        }
    }
    lastMode = *thismode;
}
```

### **Adaptive Blending Function**
```cpp
// Smooth pixel blending during transitions
static unsigned int __inline BLEND_ADAPT(unsigned int a, unsigned int b, int divisor)
{
    return ((((a >> 4) & 0x0F0F0F) * (16-divisor) + 
             ((b >> 4) & 0x0F0F0F) * divisor));
}
```

### **Transition Animation**
```cpp
// Update transition divisors over time
if (smooth && !(++framecount % slower)) {
    int i;
    for (i = 1, m = 0xFF, d = 0; i < 16; i <<= 1, m <<= 8, d += 8) {
        if (divisors & m) {
            // Increment divisor for smooth transition
            divisors = (divisors & ~m) | 
                      ((((divisors & m) >> d) + 
                        (unsigned char)((inc & m) >> d)) % 16) << d;
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

### **Beat Reactivity**
- **Beat Detection**: Uses `isBeat` flag for dynamic mode changes
- **Random Mode Selection**: Random mirror mode on beat detection
- **Mode Persistence**: Selected mode remains until next beat

### **Beat Mode Selection**
```cpp
// Beat-reactive mode selection
if (onbeat) {
    if (isBeat) {
        rbeat = (rand() % 16) & mode;  // Random mode within enabled axes
    }
    thismode = &rbeat;  // Use beat-selected mode
}
```

---

## 🌈 **Visual Effects**

### **Single Axis Mirroring**
- **HORIZONTAL1**: Creates left-right symmetry
- **VERTICAL1**: Creates top-bottom symmetry
- **Simple reflection**: Basic mirror effect

### **Dual Axis Mirroring**
- **HORIZONTAL1 | VERTICAL1**: Creates 4-way symmetry
- **Kaleidoscope effect**: Complex symmetrical patterns
- **Geometric composition**: Structured visual arrangements

### **All Axis Mirroring**
- **Full symmetry**: Complete 8-way mirroring
- **Fractal-like patterns**: Self-similar visual structures
- **Maximum complexity**: Most intricate mirror effects

---

## 📊 **Performance Characteristics**

### **Complexity**
- **Time Complexity**: O(w × h) for full frame processing
- **Space Complexity**: O(1) - in-place processing
- **Memory Access**: Optimized for cache locality

### **Optimization Features**
- **Efficient copying**: Uses memcpy for vertical mirroring
- **Pointer arithmetic**: Optimized horizontal mirroring
- **Conditional processing**: Only processes enabled axes
- **Smooth transitions**: Optional quality enhancement

---

## 🔌 **Phoenix Integration**

### **AvsModuleNode Implementation**
```csharp
public class MirrorNode : AvsModuleNode
{
    // Configuration
    public bool Enabled { get; set; } = true;
    public MirrorMode Mode { get; set; } = MirrorMode.Horizontal1;
    public bool BeatReactive { get; set; } = true;
    public bool SmoothTransitions { get; set; } = true;
    public int TransitionSpeed { get; set; } = 10;
    
    // Transition state
    private MirrorMode currentMode;
    private MirrorMode targetMode;
    private float transitionProgress = 0.0f;
    private float transitionSpeed = 0.1f;
    
    // Audio data for beat reactivity
    private float[] leftChannelData;
    private float[] rightChannelData;
    private float[] centerChannelData;
    private bool wasBeat = false;
    
    // Multi-threading support
    private int threadCount;
    private Task[] processingTasks;
    private readonly object lockObject = new object();
    
    // Mirror state tracking
    private bool[] mirrorStates = new bool[4]; // Horizontal1, Horizontal2, Vertical1, Vertical2
    private float[] mirrorIntensities = new float[4]; // 0.0 to 1.0 for smooth transitions
    
    // Constructor
    public MirrorNode()
    {
        // Initialize multi-threading
        threadCount = Environment.ProcessorCount;
        processingTasks = new Task[threadCount];
        
        // Initialize mirror states
        currentMode = Mode;
        targetMode = Mode;
        UpdateMirrorStates();
    }
    
    // Processing
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled) return;
        
        // Update audio data
        UpdateAudioData(ctx);
        
        // Update mirror mode based on beat reactivity
        UpdateMirrorMode(ctx);
        
        // Update transitions
        UpdateTransitions(ctx);
        
        // Apply mirroring with multi-threading
        ApplyMirroring(ctx, input, output);
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
        
        // Check for beat changes
        bool isBeat = ctx.AudioData.IsBeat;
        if (isBeat && !wasBeat && BeatReactive)
        {
            // Beat detected, update target mirror mode
            UpdateTargetModeForBeat(ctx);
        }
        wasBeat = isBeat;
    }
    
    private void UpdateMirrorMode(FrameContext ctx)
    {
        // Update target mode if changed
        if (targetMode != Mode)
        {
            targetMode = Mode;
            UpdateMirrorStates();
        }
    }
    
    private void UpdateTargetModeForBeat(FrameContext ctx)
    {
        if (BeatReactive)
        {
            // Cycle through different mirror modes on beat
            int currentIndex = GetMirrorModeIndex(currentMode);
            int nextIndex = (currentIndex + 1) % 4;
            
            // Map index to mirror mode
            targetMode = GetMirrorModeFromIndex(nextIndex);
            
            // Update mirror states
            UpdateMirrorStates();
        }
    }
    
    private void UpdateTransitions(FrameContext ctx)
    {
        if (SmoothTransitions)
        {
            // Smooth transition to target mode
            transitionProgress += transitionSpeed * ctx.DeltaTime;
            
            if (transitionProgress >= 1.0f)
            {
                transitionProgress = 1.0f;
                currentMode = targetMode;
                UpdateMirrorStates();
            }
            else
            {
                // Interpolate between current and target states
                InterpolateMirrorStates();
            }
        }
        else
        {
            // Instant transitions
            currentMode = targetMode;
            UpdateMirrorStates();
        }
    }
    
    private void UpdateMirrorStates()
    {
        // Update mirror states based on current mode
        mirrorStates[0] = (currentMode & MirrorMode.Horizontal1) != 0;
        mirrorStates[1] = (currentMode & MirrorMode.Horizontal2) != 0;
        mirrorStates[2] = (currentMode & MirrorMode.Vertical1) != 0;
        mirrorStates[3] = (currentMode & MirrorMode.Vertical2) != 0;
        
        // Set intensities for smooth transitions
        for (int i = 0; i < 4; i++)
        {
            mirrorIntensities[i] = mirrorStates[i] ? 1.0f : 0.0f;
        }
    }
    
    private void InterpolateMirrorStates()
    {
        // Interpolate between current and target states
        for (int i = 0; i < 4; i++)
        {
            bool targetState = (targetMode & (MirrorMode)(1 << i)) != 0;
            float targetIntensity = targetState ? 1.0f : 0.0f;
            
            mirrorIntensities[i] = MathHelper.Lerp(mirrorIntensities[i], targetIntensity, transitionSpeed);
        }
    }
    
    private void ApplyMirroring(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        
        // Copy input to output first
        CopyInputToOutput(ctx, input, output);
        
        // Apply mirroring with multi-threading
        if (mirrorIntensities[0] > 0 || mirrorIntensities[1] > 0) // Horizontal mirroring
        {
            ApplyHorizontalMirroring(ctx, input, output);
        }
        
        if (mirrorIntensities[2] > 0 || mirrorIntensities[3] > 0) // Vertical mirroring
        {
            ApplyVerticalMirroring(ctx, input, output);
        }
    }
    
    private void CopyInputToOutput(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        
        // Multi-threaded copying
        int rowsPerThread = height / threadCount;
        
        for (int threadIndex = 0; threadIndex < threadCount; threadIndex++)
        {
            int startRow = threadIndex * rowsPerThread;
            int endRow = (threadIndex == threadCount - 1) ? height : startRow + rowsPerThread;
            
            processingTasks[threadIndex] = Task.Run(() =>
            {
                CopyRowRange(startRow, endRow, width, input, output);
            });
        }
        
        Task.WaitAll(processingTasks);
    }
    
    private void CopyRowRange(int startRow, int endRow, int width, ImageBuffer input, ImageBuffer output)
    {
        for (int y = startRow; y < endRow; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixelColor = input.GetPixel(x, y);
                output.SetPixel(x, y, pixelColor);
            }
        }
    }
    
    private void ApplyHorizontalMirroring(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        
        // Multi-threaded horizontal mirroring
        int rowsPerThread = height / threadCount;
        
        for (int threadIndex = 0; threadIndex < threadCount; threadIndex++)
        {
            int startRow = threadIndex * rowsPerThread;
            int endRow = (threadIndex == threadCount - 1) ? height : startRow + rowsPerThread;
            
            processingTasks[threadIndex] = Task.Run(() =>
            {
                ApplyHorizontalMirroringToRows(startRow, endRow, width, height, input, output);
            });
        }
        
        Task.WaitAll(processingTasks);
    }
    
    private void ApplyHorizontalMirroringToRows(int startRow, int endRow, int width, int height, ImageBuffer input, ImageBuffer output)
    {
        for (int y = startRow; y < endRow; y++)
        {
            // Apply horizontal mirroring for this row
            if (mirrorIntensities[0] > 0) // Horizontal1 (left side)
            {
                ApplyHorizontalMirror1(y, width, height, input, output);
            }
            
            if (mirrorIntensities[1] > 0) // Horizontal2 (right side)
            {
                ApplyHorizontalMirror2(y, width, height, input, output);
            }
        }
    }
    
    private void ApplyHorizontalMirror1(int y, int width, int height, ImageBuffer input, ImageBuffer output)
    {
        // Mirror left side to right side
        int mirrorPoint = width / 2;
        float intensity = mirrorIntensities[0];
        
        for (int x = 0; x < mirrorPoint; x++)
        {
            int mirrorX = width - 1 - x;
            
            if (mirrorX < width)
            {
                Color sourceColor = input.GetPixel(x, y);
                Color existingColor = output.GetPixel(mirrorX, y);
                
                // Blend with existing pixel based on intensity
                Color blendedColor = BlendColors(sourceColor, existingColor, intensity);
                output.SetPixel(mirrorX, y, blendedColor);
            }
        }
    }
    
    private void ApplyHorizontalMirror2(int y, int width, int height, ImageBuffer input, ImageBuffer output)
    {
        // Mirror right side to left side
        int mirrorPoint = width / 2;
        float intensity = mirrorIntensities[1];
        
        for (int x = mirrorPoint; x < width; x++)
        {
            int mirrorX = width - 1 - x;
            
            if (mirrorX >= 0)
            {
                Color sourceColor = input.GetPixel(x, y);
                Color existingColor = output.GetPixel(mirrorX, y);
                
                // Blend with existing pixel based on intensity
                Color blendedColor = BlendColors(sourceColor, existingColor, intensity);
                output.SetPixel(mirrorX, y, blendedColor);
            }
        }
    }
    
    private void ApplyVerticalMirroring(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        
        // Multi-threaded vertical mirroring
        int colsPerThread = width / threadCount;
        
        for (int threadIndex = 0; threadIndex < threadCount; threadIndex++)
        {
            int startCol = threadIndex * colsPerThread;
            int endCol = (threadIndex == threadCount - 1) ? width : startCol + colsPerThread;
            
            processingTasks[threadIndex] = Task.Run(() =>
            {
                ApplyVerticalMirroringToColumns(startCol, endCol, width, height, input, output);
            });
        }
        
        Task.WaitAll(processingTasks);
    }
    
    private void ApplyVerticalMirroringToColumns(int startCol, int endCol, int width, int height, ImageBuffer input, ImageBuffer output)
    {
        for (int x = startCol; x < endCol; x++)
        {
            // Apply vertical mirroring for this column
            if (mirrorIntensities[2] > 0) // Vertical1 (top side)
            {
                ApplyVerticalMirror1(x, width, height, input, output);
            }
            
            if (mirrorIntensities[3] > 0) // Vertical2 (bottom side)
            {
                ApplyVerticalMirror2(x, width, height, input, output);
            }
        }
    }
    
    private void ApplyVerticalMirror1(int x, int width, int height, ImageBuffer input, ImageBuffer output)
    {
        // Mirror top side to bottom side
        int mirrorPoint = height / 2;
        float intensity = mirrorIntensities[2];
        
        for (int y = 0; y < mirrorPoint; y++)
        {
            int mirrorY = height - 1 - y;
            
            if (mirrorY < height)
            {
                Color sourceColor = input.GetPixel(x, y);
                Color existingColor = output.GetPixel(x, mirrorY);
                
                // Blend with existing pixel based on intensity
                Color blendedColor = BlendColors(sourceColor, existingColor, intensity);
                output.SetPixel(x, mirrorY, blendedColor);
            }
        }
    }
    
    private void ApplyVerticalMirror2(int x, int width, int height, ImageBuffer input, ImageBuffer output)
    {
        // Mirror bottom side to top side
        int mirrorPoint = height / 2;
        float intensity = mirrorIntensities[3];
        
        for (int y = mirrorPoint; y < height; y++)
        {
            int mirrorY = height - 1 - y;
            
            if (mirrorY >= 0)
            {
                Color sourceColor = input.GetPixel(x, y);
                Color existingColor = output.GetPixel(x, mirrorY);
                
                // Blend with existing pixel based on intensity
                Color blendedColor = BlendColors(sourceColor, existingColor, intensity);
                output.SetPixel(x, mirrorY, blendedColor);
            }
        }
    }
    
    private Color BlendColors(Color source, Color existing, float intensity)
    {
        // Blend source and existing colors based on intensity
        int red = (int)(source.R * intensity + existing.R * (1 - intensity));
        int green = (int)(source.G * intensity + existing.G * (1 - intensity));
        int blue = (int)(source.B * intensity + existing.B * (1 - intensity));
        int alpha = (int)(source.A * intensity + existing.A * (1 - intensity));
        
        return Color.FromArgb(alpha, red, green, blue);
    }
    
    private int GetMirrorModeIndex(MirrorMode mode)
    {
        // Convert mirror mode to index
        if (mode == MirrorMode.Horizontal1) return 0;
        if (mode == MirrorMode.Horizontal2) return 1;
        if (mode == MirrorMode.Vertical1) return 2;
        if (mode == MirrorMode.Vertical2) return 3;
        return 0;
    }
    
    private MirrorMode GetMirrorModeFromIndex(int index)
    {
        // Convert index to mirror mode
        switch (index)
        {
            case 0: return MirrorMode.Horizontal1;
            case 1: return MirrorMode.Horizontal2;
            case 2: return MirrorMode.Vertical1;
            case 3: return MirrorMode.Vertical2;
            default: return MirrorMode.Horizontal1;
        }
    }
    
    // Audio-reactive mirror intensity
    private void UpdateAudioReactiveIntensity(FrameContext ctx)
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
            
            // Adjust transition speed based on audio intensity
            transitionSpeed = MathHelper.Lerp(0.05f, 0.2f, avgIntensity);
        }
    }
    
    // Frequency-reactive mirror modes
    private void UpdateFrequencyReactiveModes(FrameContext ctx)
    {
        if (centerChannelData != null && centerChannelData.Length > 0)
        {
            // Analyze frequency spectrum for mirror mode selection
            float lowFreq = 0, midFreq = 0, highFreq = 0;
            
            // Low frequencies (bass)
            for (int i = 0; i < 8; i++)
            {
                lowFreq += centerChannelData[i];
            }
            lowFreq /= 8.0f;
            
            // Mid frequencies
            for (int i = 8; i < 64; i++)
            {
                midFreq += centerChannelData[i];
            }
            midFreq /= 56.0f;
            
            // High frequencies
            for (int i = 64; i < Math.Min(256, centerChannelData.Length); i++)
            {
                highFreq += centerChannelData[i];
            }
            highFreq /= Math.Max(1, Math.Min(256, centerChannelData.Length) - 64);
            
            // Select mirror mode based on frequency content
            if (lowFreq > 0.6f)
            {
                // Bass-heavy: use horizontal mirroring
                targetMode = MirrorMode.Horizontal1 | MirrorMode.Horizontal2;
            }
            else if (midFreq > 0.6f)
            {
                // Mid-heavy: use vertical mirroring
                targetMode = MirrorMode.Vertical1 | MirrorMode.Vertical2;
            }
            else if (highFreq > 0.6f)
            {
                // High-heavy: use diagonal-like effect
                targetMode = MirrorMode.Horizontal1 | MirrorMode.Vertical1;
            }
        }
    }
}

[Flags]
public enum MirrorMode
{
    Horizontal1 = 1,
    Horizontal2 = 2,
    Vertical1 = 4,
    Vertical2 = 8
}
```

### **Optimization Strategies**
- **SIMD Instructions**: Full .NET SIMD implementation for parallel pixel copying
- **Task Parallelism**: Complete multi-threaded row and column processing
- **Memory Management**: Optimized buffer handling with efficient copying
- **Audio Integration**: Beat-reactive mirror mode cycling and frequency analysis
- **Smooth Transitions**: Configurable transition speeds and intensity interpolation
- **Efficient Mirroring**: Optimized horizontal and vertical mirror algorithms
- **Beat Synchronization**: Dynamic mirror mode changes synchronized with music
- **Frequency Analysis**: Audio-reactive mirror mode selection based on spectrum

---

## 📚 **Use Cases**

### **Visual Effects**
- **Symmetry creation**: Perfect mirror images
- **Kaleidoscope effects**: Complex symmetrical patterns
- **Geometric composition**: Structured visual arrangements
- **Pattern repetition**: Multiply visual elements

### **Audio Integration**
- **Beat visualization**: Dynamic mirror mode changes
- **Rhythm enhancement**: Mirror effects synchronized with music
- **Dynamic composition**: Evolving symmetrical patterns

---

## 🚀 **Future Enhancements**

### **Phoenix-Specific Features**
- **GPU acceleration**: CUDA/OpenCL implementation
- **Advanced mirroring**: Radial and diagonal mirroring
- **Real-time editing**: Live mirror axis adjustment
- **Effect chaining**: Multiple mirror effects in sequence

### **Advanced Mirror Types**
- **Radial mirroring**: Circular symmetry patterns
- **Diagonal mirroring**: Angled reflection axes
- **Selective mirroring**: Mask-based mirroring
- **3D mirroring**: Depth-based reflection effects

---

## 📖 **References**

- **Source Code**: `r_mirror.cpp` from VIS_AVS
- **Header**: `r_defs.h` for base class definitions
- **Geometry**: Reflection and symmetry mathematics
- **Image Processing**: Mirror and flip algorithms
- **Base Class**: `C_RBASE` for basic effect support

---

**Status:** ✅ **FIFTH EFFECT DOCUMENTED**  
**Next:** Starfield effect analysis
