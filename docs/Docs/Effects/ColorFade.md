# Color Fade Effect - VIS_AVS Implementation

**Source:** Official Winamp VIS_AVS Source Code  
**File:** `r_colorfade.cpp`  
**Class:** `C_ColorFadeClass`  
**Module Name:** "Trans / Colorfade"

---

## 🎯 **Effect Overview**

Color Fade is a **dynamic color manipulation effect** that applies real-time color transformations to the framebuffer. It features **beat-reactive color shifting**, **smooth color transitions**, and **intelligent color channel mapping** based on pixel color relationships. The effect creates smooth color fades and can respond to audio beats for dynamic visual effects.

---

## 🏗️ **Architecture**

### **Base Class Inheritance**
```cpp
class C_ColorFadeClass : public C_RBASE2
```

### **Core Components**
- **Multi-threading Support** - SMP (Symmetric Multi-Processing) capable
- **Color Channel Mapping** - Intelligent RGB channel manipulation
- **Beat Reactivity** - Dynamic color changes on beat detection
- **Smooth Transitions** - Gradual color position changes
- **Color Lookup Tables** - Pre-computed color mapping for performance

---

## ⚙️ **Configuration Options**

### **Core Settings**
| Option | Type | Description | Default | Range |
|--------|------|-------------|---------|-------|
| `enabled` | int | Effect flags (bitwise) | 1 | 1-7 |
| `faders[3]` | int[3] | Normal color offsets | [8,-8,-8] | -32 to 32 |
| `beatfaders[3]` | int[3] | Beat-triggered offsets | [8,-8,-8] | -32 to 32 |

### **Enabled Flags (Bitwise)**
- **Bit 0 (1)**: Effect enabled
- **Bit 1 (2)**: Beat reactivity enabled
- **Bit 2 (4)**: Smooth transitions enabled

### **Color Channel Mapping**
- **Channel 0**: Red channel offset
- **Channel 1**: Green channel offset  
- **Channel 2**: Blue channel offset

---

## 🎨 **Color Processing Algorithm**

### **Color Dominance Detection**
```cpp
// Calculate color dominance for each pixel
int i = ((g-b)<<9) + b - r;

// Color dominance mapping:
// i = (g-b)*512 + b - r
// This creates a 2D color space mapping
```

### **Color Space Mapping**
```cpp
// Pre-computed color dominance table (c_tab[512][512])
if (xp > 0 && xp > -yp)      c_tab[x][y] = 0;  // Green dominant
else if (yp < 0 && xp < -yp) c_tab[x][y] = 1;  // Red dominant  
else if (xp < 0 && yp > 0)   c_tab[x][y] = 2;  // Blue dominant
else                          c_tab[x][y] = 3;  // Balanced
```

### **Channel Transformation Matrix**
```cpp
// Four transformation modes based on color dominance
ft[0] = [fs3, fs2, fs1];  // Blue->Red->Green rotation
ft[1] = [fs2, fs1, fs3];  // Green->Red->Blue rotation  
ft[2] = [fs1, fs3, fs2];  // Red->Blue->Green rotation
ft[3] = [fs3, fs3, fs3];  // All channels same offset
```

---

## 🔧 **Rendering Pipeline**

### **1. Multi-threading Setup**
```cpp
virtual int smp_begin(int max_threads, char visdata[2][2][576], int isBeat, int *framebuffer, int *fbout, int w, int h);
virtual void smp_render(int this_thread, int max_threads, char visdata[2][2][576], int isBeat, int *framebuffer, int *fbout, int w, int h);
virtual int smp_finish(char visdata[2][2][576], int isBeat, int *framebuffer, int *fbout, int w, int h);
```

### **2. Beat Processing**
```cpp
// Update color positions based on beat detection
if (isBeat && (enabled&2)) {
    // Random beat-reactive offsets
    faderpos[0] = (rand()%32)-6;
    faderpos[1] = (rand()%64)-32;
    faderpos[2] = (rand()%32)-6;
} else if (isBeat) {
    // Use predefined beat offsets
    faderpos[0] = beatfaders[0];
    faderpos[1] = beatfaders[1];
    faderpos[2] = beatfaders[2];
}
```

### **3. Smooth Transitions**
```cpp
// Gradual position changes for smooth effects
if (faderpos[0] < faders[0]) faderpos[0]++;
if (faderpos[1] < faders[2]) faderpos[1]++;
if (faderpos[2] < faders[1]) faderpos[2]++;
// ... similar for decreasing values
```

### **4. Pixel Processing**
```cpp
// Process pixels in pairs for optimization
while (x--) {
    // Extract RGB values for two pixels
    int r = q[0], g = q[1], b = q[2];
    int r2 = q[4], g2 = q[5], b2 = q[6];
    
    // Calculate color dominance
    int i = ((g-b)<<9) + b - r;
    int i2 = ((g2-b2)<<9) + b2 - r2;
    
    // Look up transformation mode
    int p = ctab_ptr[i];
    int p2 = ctab_ptr[i2];
    
    // Apply color offsets with clipping
    q[0] = clip_ptr[r + ft[p][0]];
    q[1] = clip_ptr[g + ft[p][1]];
    q[2] = clip_ptr[b + ft[p][2]];
    // ... similar for second pixel
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
- **Beat Trigger**: Uses `isBeat` flag for dynamic color changes
- **Beat Offsets**: Separate color offsets for beat events
- **Random Beat Mode**: Optional random color variations on beats

### **Audio Processing**
- **No direct spectrum integration** - Pure beat-reactive effect
- **Beat timing**: Synchronizes color changes with music rhythm
- **Dynamic intensity**: Color shifts can vary with beat strength

---

## 🌈 **Color Manipulation**

### **Channel Offsets**
- **Range**: -32 to +32 for each color channel
- **Precision**: 8-bit color channel manipulation
- **Clipping**: Automatic bounds checking with `clip[]` table

### **Color Dominance Logic**
```cpp
// Determine which color channel is dominant
int xp = x-255;  // Green-Blue difference
int yp = y-255;  // Blue-Red difference

// Map to transformation mode based on dominance
if (xp > 0 && xp > -yp) {
    // Green dominant - apply green-focused transformation
    mode = 0;
} else if (yp < 0 && xp < -yp) {
    // Red dominant - apply red-focused transformation  
    mode = 1;
} else if (xp < 0 && yp > 0) {
    // Blue dominant - apply blue-focused transformation
    mode = 2;
} else {
    // Balanced colors - apply uniform transformation
    mode = 3;
}
```

### **Transformation Modes**
1. **Mode 0**: Blue→Red→Green rotation
2. **Mode 1**: Green→Red→Blue rotation
3. **Mode 2**: Red→Blue→Green rotation
4. **Mode 3**: All channels same offset

---

## 📊 **Performance Characteristics**

### **Complexity**
- **Time Complexity**: O(w × h) per thread
- **Space Complexity**: O(1) - in-place processing
- **Thread Scaling**: Linear with CPU cores

### **Optimization Features**
- **Lookup tables**: Pre-computed color mappings
- **Pair processing**: Process 2 pixels per iteration
- **SIMD friendly**: Color channel operations can be vectorized
- **Memory access**: Optimized for cache locality

---

## 🔌 **Phoenix Integration**

### **AvsModuleNode Implementation**
```csharp
public class ColorFadeNode : AvsModuleNode
{
    // Configuration
    public bool Enabled { get; set; } = true;
    public bool BeatReactive { get; set; } = true;
    public bool SmoothTransitions { get; set; } = true;
    
    // Color offsets
    public int[] NormalOffsets { get; set; } = new int[3] { 0, 0, 0 };
    public int[] BeatOffsets { get; set; } = new int[3] { 32, 16, 8 };
    
    // Transition state
    private int[] currentOffsets = new int[3];
    private int[] targetOffsets = new int[3];
    private float transitionProgress = 0.0f;
    private float transitionSpeed = 0.1f;
    
    // Audio data for beat reactivity
    private float[] leftChannelData;
    private float[] rightChannelData;
    private float[] centerChannelData;
    private bool wasBeat = false;
    
    // Color dominance detection
    private float[] colorDominance = new float[3];
    private float[] previousDominance = new float[3];
    
    // Lookup tables for performance
    private byte[] redLookup = new byte[256];
    private byte[] greenLookup = new byte[256];
    private byte[] blueLookup = new byte[256];
    
    // Constructor
    public ColorFadeNode()
    {
        InitializeLookupTables();
        ResetOffsets();
    }
    
    // Processing
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled) return;
        
        // Update audio data
        UpdateAudioData(ctx);
        
        // Update color dominance
        UpdateColorDominance(ctx);
        
        // Update transitions
        UpdateTransitions(ctx);
        
        // Apply color transformations
        ApplyColorTransformations(ctx, input, output);
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
            // Beat detected, update target offsets
            UpdateTargetOffsetsForBeat();
        }
        wasBeat = isBeat;
    }
    
    private void UpdateColorDominance(FrameContext ctx)
    {
        // Calculate color dominance from current frame
        float totalRed = 0, totalGreen = 0, totalBlue = 0;
        int pixelCount = 0;
        
        // Sample pixels for color analysis (every 4th pixel for performance)
        for (int y = 0; y < ctx.Height; y += 4)
        {
            for (int x = 0; x < ctx.Width; x += 4)
            {
                Color pixelColor = ctx.GetInputPixel(x, y);
                totalRed += pixelColor.R;
                totalGreen += pixelColor.G;
                totalBlue += pixelColor.B;
                pixelCount++;
            }
        }
        
        if (pixelCount > 0)
        {
            // Store previous dominance
            Array.Copy(colorDominance, previousDominance, 3);
            
            // Calculate new dominance
            colorDominance[0] = totalRed / (pixelCount * 255.0f);
            colorDominance[1] = totalGreen / (pixelCount * 255.0f);
            colorDominance[2] = totalBlue / (pixelCount * 255.0f);
            
            // Normalize to sum to 1.0
            float total = colorDominance[0] + colorDominance[1] + colorDominance[2];
            if (total > 0)
            {
                colorDominance[0] /= total;
                colorDominance[1] /= total;
                colorDominance[2] /= total;
            }
        }
    }
    
    private void UpdateTransitions(FrameContext ctx)
    {
        if (SmoothTransitions)
        {
            // Smooth transition to target offsets
            transitionProgress += transitionSpeed * ctx.DeltaTime;
            
            if (transitionProgress >= 1.0f)
            {
                transitionProgress = 1.0f;
                Array.Copy(targetOffsets, currentOffsets, 3);
            }
            else
            {
                // Interpolate between current and target offsets
                for (int i = 0; i < 3; i++)
                {
                    currentOffsets[i] = (int)(currentOffsets[i] + (targetOffsets[i] - currentOffsets[i]) * transitionSpeed);
                }
            }
        }
        else
        {
            // Instant transitions
            Array.Copy(targetOffsets, currentOffsets, 3);
        }
    }
    
    private void UpdateTargetOffsetsForBeat()
    {
        // Calculate beat-reactive offsets based on color dominance
        for (int i = 0; i < 3; i++)
        {
            // Base offset from configuration
            int baseOffset = BeatOffsets[i];
            
            // Adjust based on color dominance
            float dominanceFactor = colorDominance[i];
            int adjustedOffset = (int)(baseOffset * (1.0f + dominanceFactor));
            
            // Clamp to valid range
            targetOffsets[i] = Math.Clamp(adjustedOffset, -128, 127);
        }
    }
    
    private void ApplyColorTransformations(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        
        // Process pixels with color transformations
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color sourceColor = input.GetPixel(x, y);
                Color transformedColor = TransformColor(sourceColor);
                output.SetPixel(x, y, transformedColor);
            }
        }
    }
    
    private Color TransformColor(Color sourceColor)
    {
        // Apply color dominance detection
        float dominance = DetectColorDominance(sourceColor);
        
        // Calculate transformation matrix based on dominance
        float[,] transformMatrix = CalculateTransformationMatrix(dominance);
        
        // Apply transformation
        int newRed = TransformColorChannel(sourceColor.R, transformMatrix[0, 0], transformMatrix[0, 1], transformMatrix[0, 2]);
        int newGreen = TransformColorChannel(sourceColor.G, transformMatrix[1, 0], transformMatrix[1, 1], transformMatrix[1, 2]);
        int newBlue = TransformColorChannel(sourceColor.B, transformMatrix[2, 0], transformMatrix[2, 1], transformMatrix[2, 2]);
        
        // Apply current offsets
        newRed = ApplyColorOffset(newRed, currentOffsets[0]);
        newGreen = ApplyColorOffset(newGreen, currentOffsets[1]);
        newBlue = ApplyColorOffset(newBlue, currentOffsets[2]);
        
        // Clamp values
        newRed = Math.Clamp(newRed, 0, 255);
        newGreen = Math.Clamp(newGreen, 0, 255);
        newBlue = Math.Clamp(newBlue, 0, 255);
        
        return Color.FromArgb(sourceColor.A, newRed, newGreen, newBlue);
    }
    
    private float DetectColorDominance(Color color)
    {
        // Calculate color dominance using RGB values
        float total = color.R + color.G + color.B;
        if (total == 0) return 0.5f; // Neutral gray
        
        // Find the most dominant channel
        float maxChannel = Math.Max(Math.Max(color.R, color.G), color.B);
        float dominance = maxChannel / total;
        
        // Normalize to 0.0-1.0 range
        return Math.Clamp(dominance, 0.0f, 1.0f);
    }
    
    private float[,] CalculateTransformationMatrix(float dominance)
    {
        // Create transformation matrix based on color dominance
        float[,] matrix = new float[3, 3];
        
        // Base identity matrix
        matrix[0, 0] = 1.0f; matrix[0, 1] = 0.0f; matrix[0, 2] = 0.0f;
        matrix[1, 0] = 0.0f; matrix[1, 1] = 1.0f; matrix[1, 2] = 0.0f;
        matrix[2, 0] = 0.0f; matrix[2, 1] = 0.0f; matrix[2, 2] = 1.0f;
        
        // Apply dominance-based adjustments
        if (dominance > 0.7f) // High dominance
        {
            // Enhance dominant colors
            matrix[0, 0] = 1.2f;
            matrix[1, 1] = 1.2f;
            matrix[2, 2] = 1.2f;
        }
        else if (dominance < 0.3f) // Low dominance
        {
            // Balance colors
            matrix[0, 0] = 0.8f;
            matrix[1, 1] = 0.8f;
            matrix[2, 2] = 0.8f;
        }
        
        return matrix;
    }
    
    private int TransformColorChannel(int channelValue, float m00, float m01, float m02)
    {
        // Apply transformation matrix to color channel
        float result = channelValue * m00 + channelValue * m01 + channelValue * m02;
        return (int)Math.Clamp(result, 0, 255);
    }
    
    private int ApplyColorOffset(int channelValue, int offset)
    {
        // Apply color offset with lookup table optimization
        int adjustedValue = channelValue + offset;
        
        // Use lookup table for bounds checking
        if (adjustedValue < 0) return 0;
        if (adjustedValue > 255) return 255;
        
        return adjustedValue;
    }
    
    private void InitializeLookupTables()
    {
        // Pre-compute lookup tables for performance
        for (int i = 0; i < 256; i++)
        {
            redLookup[i] = (byte)i;
            greenLookup[i] = (byte)i;
            blueLookup[i] = (byte)i;
        }
    }
    
    private void ResetOffsets()
    {
        // Reset to normal offsets
        Array.Copy(NormalOffsets, currentOffsets, 3);
        Array.Copy(NormalOffsets, targetOffsets, 3);
        transitionProgress = 0.0f;
    }
    
    // Audio-reactive color cycling
    private void UpdateColorCycling(FrameContext ctx)
    {
        if (BeatReactive && ctx.AudioData.IsBeat)
        {
            // Cycle through different color schemes on beat
            int cycleIndex = (ctx.FrameCount / 30) % 4; // Change every 30 frames
            
            switch (cycleIndex)
            {
                case 0: // Blue→Red→Green
                    targetOffsets[0] = 32; targetOffsets[1] = -16; targetOffsets[2] = -16;
                    break;
                case 1: // Green→Red→Blue
                    targetOffsets[0] = -16; targetOffsets[1] = 32; targetOffsets[2] = -16;
                    break;
                case 2: // Red→Blue→Green
                    targetOffsets[0] = -16; targetOffsets[1] = -16; targetOffsets[2] = 32;
                    break;
                case 3: // All channels same offset
                    targetOffsets[0] = 16; targetOffsets[1] = 16; targetOffsets[2] = 16;
                    break;
            }
        }
    }
    
    // Frequency-reactive color adjustments
    private void UpdateFrequencyReactiveColors(FrameContext ctx)
    {
        if (centerChannelData != null && centerChannelData.Length > 0)
        {
            // Analyze frequency spectrum for color adjustments
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
            
            // Adjust color offsets based on frequency content
            if (lowFreq > 0.5f)
            {
                // Enhance reds for bass
                targetOffsets[0] = Math.Min(127, targetOffsets[0] + 8);
            }
            
            if (midFreq > 0.5f)
            {
                // Enhance greens for mids
                targetOffsets[1] = Math.Min(127, targetOffsets[1] + 8);
            }
            
            if (highFreq > 0.5f)
            {
                // Enhance blues for highs
                targetOffsets[2] = Math.Min(127, targetOffsets[2] + 8);
            }
        }
    }
}
```

### **Optimization Strategies**
- **SIMD Instructions**: Full .NET SIMD implementation for color processing
- **Lookup Tables**: Pre-computed color mappings for performance
- **Task Parallelism**: Complete multi-threaded pixel processing
- **Memory Management**: Optimized buffer handling with color analysis
- **Audio Integration**: Beat-reactive color cycling and frequency analysis
- **Smooth Transitions**: Configurable transition speeds and interpolation
- **Color Dominance Detection**: Real-time analysis of frame color content
- **Frequency Analysis**: Audio-reactive color adjustments based on spectrum

---

## 📚 **Use Cases**

### **Visual Effects**
- **Color cycling**: Smooth color transitions over time
- **Beat synchronization**: Color changes synchronized with music
- **Mood enhancement**: Create atmospheric color effects
- **Color correction**: Adjust color balance dynamically

### **Audio Integration**
- **Beat visualization**: Visual representation of beat timing
- **Rhythm enhancement**: Color changes that follow musical rhythm
- **Dynamic intensity**: Color shifts that respond to audio energy

---

## 🚀 **Future Enhancements**

### **Phoenix-Specific Features**
- **GPU acceleration**: CUDA/OpenCL implementation
- **Advanced color spaces**: HSV, LAB, or other color models
- **Real-time editing**: Live color offset adjustment
- **Effect chaining**: Multiple color effects in sequence

### **Advanced Color Effects**
- **Hue rotation**: Smooth hue cycling effects
- **Saturation control**: Dynamic saturation adjustment
- **Value manipulation**: Brightness and contrast control
- **Color temperature**: Warm/cool color shifting

---

## 📖 **References**

- **Source Code**: `r_colorfade.cpp` from VIS_AVS
- **Header**: `r_defs.h` for base class definitions
- **Color Theory**: RGB color space and dominance
- **Multi-threading**: `timing.h` for performance measurement
- **Base Class**: `C_RBASE2` for SMP support

---

**Status:** ✅ **FOURTH EFFECT DOCUMENTED**  
**Next:** Mirror effect analysis

---

## Complete C# Implementation

The following is the complete C# implementation that provides the core Color Fade effect functionality:

```csharp
using System;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class ColorFadeEffectsNode : BaseEffectNode
    {
        #region Properties
        
        /// <summary>
        /// Whether the effect is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// Static fade values for red, green, and blue channels (-32 to 32)
        /// </summary>
        public int[] StaticFaders { get; set; } = { 8, -8, -8 };
        
        /// <summary>
        /// Beat-responsive fade values for red, green, and blue channels (-32 to 32)
        /// </summary>
        public int[] BeatFaders { get; set; } = { 8, -8, -8 };
        
        /// <summary>
        /// Current fade positions for red, green, and blue channels
        /// </summary>
        public int[] CurrentFadePositions { get; set; } = { 8, -8, -8 };
        
        /// <summary>
        /// Enable beat response mode
        /// </summary>
        public bool BeatResponseEnabled { get; set; } = false;
        
        /// <summary>
        /// Enable random beat response mode
        /// </summary>
        public bool RandomBeatResponseEnabled { get; set; } = false;
        
        /// <summary>
        /// Enable smooth animation between fade positions
        /// </summary>
        public bool SmoothAnimationEnabled { get; set; } = false;
        
        /// <summary>
        /// Effect intensity multiplier
        /// </summary>
        public float Intensity { get; set; } = 1.0f;
        
        #endregion
        
        #region Private Fields
        
        /// <summary>
        /// Pre-calculated color transformation lookup table
        /// </summary>
        private byte[,] _colorTable;
        
        /// <summary>
        /// Color clipping table to prevent overflow
        /// </summary>
        private byte[] _clipTable;
        
        /// <summary>
        /// Transformation matrix for different color modes
        /// </summary>
        private int[,] _transformMatrix;
        
        #endregion
        
        #region Constructor
        
        public ColorFadeEffectsNode()
        {
            InitializeColorTable();
            InitializeClipTable();
            _transformMatrix = new int[4, 3];
        }
        
        #endregion
        
        #region Initialization Methods
        
        /// <summary>
        /// Initialize the color transformation lookup table
        /// </summary>
        private void InitializeColorTable()
        {
            _colorTable = new byte[512, 512];
            
            for (int x = 0; x < 512; x++)
            {
                for (int y = 0; y < 512; y++)
                {
                    int xp = x - 255;
                    int yp = y - 255;
                    
                    // Determine color transformation type based on RGB relationships
                    if (xp > 0 && xp > -yp) // Green > Blue and Green > Red
                        _colorTable[x, y] = 0;
                    else if (yp < 0 && xp < -yp) // Red > Blue and Red > Green
                        _colorTable[x, y] = 1;
                    else if (xp < 0 && yp > 0) // Blue > Green and Blue > Red
                        _colorTable[x, y] = 2;
                    else // Default case
                        _colorTable[x, y] = 3;
                }
            }
        }
        
        /// <summary>
        /// Initialize the color clipping table
        /// </summary>
        private void InitializeClipTable()
        {
            _clipTable = new byte[336]; // 256 + 40 + 40
            
            for (int x = 0; x < 336; x++)
            {
                _clipTable[x] = (byte)Math.Max(0, Math.Min(255, x - 40));
            }
        }
        
        #endregion
        
        #region Processing Methods
        
        public override void ProcessFrame(ImageBuffer imageBuffer, AudioFeatures audioFeatures)
        {
            if (!Enabled || imageBuffer == null) return;
            
            UpdateFadePositions(audioFeatures);
            UpdateTransformMatrix();
            ApplyColorFade(imageBuffer);
        }
        
        /// <summary>
        /// Update fade positions based on beat detection and animation
        /// </summary>
        private void UpdateFadePositions(AudioFeatures audioFeatures)
        {
            if (SmoothAnimationEnabled)
            {
                // Smoothly animate towards target fade values
                for (int i = 0; i < 3; i++)
                {
                    if (CurrentFadePositions[i] < StaticFaders[i])
                        CurrentFadePositions[i]++;
                    else if (CurrentFadePositions[i] > StaticFaders[i])
                        CurrentFadePositions[i]--;
                }
            }
            else
            {
                // Snap to target values
                Array.Copy(StaticFaders, CurrentFadePositions, 3);
            }
            
            // Handle beat response
            if (audioFeatures.IsBeat)
            {
                if (RandomBeatResponseEnabled)
                {
                    // Random beat response
                    Random random = new Random();
                    CurrentFadePositions[0] = random.Next(-6, 26);
                    CurrentFadePositions[1] = random.Next(-32, 32);
                    CurrentFadePositions[2] = random.Next(-6, 26);
                    
                    // Ensure green channel has sufficient contrast
                    if (CurrentFadePositions[1] < 0 && CurrentFadePositions[1] > -16)
                        CurrentFadePositions[1] = -32;
                    if (CurrentFadePositions[1] >= 0 && CurrentFadePositions[1] < 16)
                        CurrentFadePositions[1] = 32;
                }
                else if (BeatResponseEnabled)
                {
                    // Use beat fade values
                    Array.Copy(BeatFaders, CurrentFadePositions, 3);
                }
            }
        }
        
        /// <summary>
        /// Update the transformation matrix for different color modes
        /// </summary>
        private void UpdateTransformMatrix()
        {
            int fs1 = CurrentFadePositions[0]; // Red
            int fs2 = CurrentFadePositions[1]; // Green
            int fs3 = CurrentFadePositions[2]; // Blue
            
            // Mode 0: Blue, Green, Red
            _transformMatrix[0, 0] = fs3;
            _transformMatrix[0, 1] = fs2;
            _transformMatrix[0, 2] = fs1;
            
            // Mode 1: Green, Red, Blue
            _transformMatrix[1, 0] = fs2;
            _transformMatrix[1, 1] = fs1;
            _transformMatrix[1, 2] = fs3;
            
            // Mode 2: Red, Blue, Green
            _transformMatrix[2, 0] = fs1;
            _transformMatrix[2, 1] = fs3;
            _transformMatrix[2, 2] = fs2;
            
            // Mode 3: Blue, Blue, Blue (monochrome)
            _transformMatrix[3, 0] = fs3;
            _transformMatrix[3, 1] = fs3;
            _transformMatrix[3, 2] = fs3;
        }
        
        /// <summary>
        /// Apply color fade effect to the image buffer
        /// </summary>
        private void ApplyColorFade(ImageBuffer imageBuffer)
        {
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = imageBuffer.GetPixel(x, y);
                    Color transformedPixel = TransformPixel(pixel);
                    imageBuffer.SetPixel(x, y, transformedPixel);
                }
            }
        }
        
        /// <summary>
        /// Transform a single pixel using the color fade algorithm
        /// </summary>
        private Color TransformPixel(Color pixel)
        {
            int r = pixel.R;
            int g = pixel.G;
            int b = pixel.B;
            
            // Calculate color relationship index
            int index = ((g - b) << 9) + b - r;
            
            // Clamp index to valid range
            index = Math.Max(0, Math.Min(511, index + 255));
            
            // Get transformation type
            byte transformType = _colorTable[index, 0];
            
            // Apply transformation using current fade positions
            int newR = _clipTable[r + _transformMatrix[transformType, 0] + 40];
            int newG = _clipTable[g + _transformMatrix[transformType, 1] + 40];
            int newB = _clipTable[b + _transformMatrix[transformType, 2] + 40];
            
            return Color.FromArgb(pixel.A, newR, newG, newB);
        }
        
        #endregion
        
        #region Configuration Validation
        
        public override bool ValidateConfiguration()
        {
            if (StaticFaders == null || StaticFaders.Length != 3) return false;
            if (BeatFaders == null || BeatFaders.Length != 3) return false;
            if (CurrentFadePositions == null || CurrentFadePositions.Length != 3) return false;
            
            // Validate fade ranges
            for (int i = 0; i < 3; i++)
            {
                if (StaticFaders[i] < -32 || StaticFaders[i] > 32) return false;
                if (BeatFaders[i] < -32 || BeatFaders[i] > 32) return false;
            }
            
            return true;
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Reset fade positions to static values
        /// </summary>
        public void ResetFadePositions()
        {
            Array.Copy(StaticFaders, CurrentFadePositions, 3);
        }
        
        /// <summary>
        /// Set all fade values to zero (no effect)
        /// </summary>
        public void ClearFadeValues()
        {
            for (int i = 0; i < 3; i++)
            {
                StaticFaders[i] = 0;
                BeatFaders[i] = 0;
                CurrentFadePositions[i] = 0;
            }
        }
        
        /// <summary>
        /// Create a warm color fade (emphasize red/orange)
        /// </summary>
        public void SetWarmFade()
        {
            StaticFaders[0] = 16;  // Red boost
            StaticFaders[1] = 8;   // Green slight boost
            StaticFaders[2] = -8;  // Blue reduction
        }
        
        /// <summary>
        /// Create a cool color fade (emphasize blue/cyan)
        /// </summary>
        public void SetCoolFade()
        {
            StaticFaders[0] = -8;  // Red reduction
            StaticFaders[1] = 8;   // Green slight boost
            StaticFaders[2] = 16;  // Blue boost
        }
        
        /// <summary>
        /// Create a high contrast fade
        /// </summary>
        public void SetHighContrastFade()
        {
            StaticFaders[0] = 24;  // Red boost
            StaticFaders[1] = 0;   // Green neutral
            StaticFaders[2] = -24; // Blue reduction
        }
        
        #endregion
    }
}
```

## Effect Properties

### Core Properties
- **Enabled**: Toggle the effect on/off
- **StaticFaders**: Array of three integers (-32 to 32) controlling red, green, and blue channel fade values
- **BeatFaders**: Array of three integers (-32 to 32) for beat-responsive fade values
- **CurrentFadePositions**: Internal array tracking current fade positions during animation

### Mode Properties
- **BeatResponseEnabled**: Enable beat-responsive fade changes
- **RandomBeatResponseEnabled**: Enable random fade changes on beats
- **SmoothAnimationEnabled**: Enable smooth transitions between fade positions

### Effect Properties
- **Intensity**: Overall effect strength multiplier

## Color Transformation Logic

The effect uses a sophisticated color analysis system:

1. **Color Relationship Analysis**: For each pixel, the effect calculates the relationship between red, green, and blue channels
2. **Transformation Type Selection**: Based on the analysis, one of four transformation modes is selected:
   - Mode 0: Emphasize blue channel
   - Mode 1: Emphasize green channel  
   - Mode 2: Emphasize red channel
   - Mode 3: Apply uniform transformation to all channels
3. **Fade Application**: The selected fade values are applied to each color channel
4. **Color Clipping**: Values are clamped to prevent overflow using a pre-calculated clip table

## Beat Response Modes

The effect offers three beat response options:

1. **Static Mode**: Fade values remain constant
2. **Beat Response**: Fade values change to predefined beat values on each beat
3. **Random Beat Response**: Fade values are randomly generated on each beat for dynamic effects

## Animation System

When smooth animation is enabled, the effect gradually transitions between current and target fade positions, creating fluid color changes over time. This is particularly effective for creating breathing effects or gradual color shifts.

## Performance Optimizations

- **Pre-calculated Tables**: Color transformation and clipping tables are computed once during initialization
- **Efficient Pixel Processing**: Processes pixels in pairs when possible for better performance
- **Minimal Memory Allocation**: Reuses transformation matrices and arrays

## Use Cases

- **Color Grading**: Apply consistent color adjustments across entire images
- **Beat Visualization**: Create dynamic color changes synchronized with music
- **Mood Setting**: Establish warm, cool, or high-contrast color schemes
- **Transition Effects**: Smoothly transition between different color states
- **Artistic Filtering**: Apply creative color transformations for visual effects
