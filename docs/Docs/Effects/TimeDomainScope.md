# Time Domain Scope Effect - VIS_AVS Implementation

**Source:** Official Winamp VIS_AVS Source Code  
**File:** `r_timescope.cpp`  
**Class:** `C_TimescopeClass`  
**Module Name:** "Render / Timescope"

---

## 🎯 **Effect Overview**

Time Domain Scope is a **vertical oscilloscope visualization effect** that creates **real-time audio waveform displays** synchronized with music. The effect generates **vertical bars** that **move horizontally across the screen** while **displaying audio intensity** from different frequency bands. Each vertical column represents a **specific frequency range** from the audio spectrum, creating a **dynamic waterfall-like visualization** that shows how different frequencies change over time. The effect supports **multiple audio channels** (left, right, center), **configurable frequency bands**, and **various blending modes** for smooth visual integration.

---

## 🏗️ **Architecture**

### **Base Class Inheritance**
```cpp
class C_TimescopeClass : public C_RBASE
```

### **Core Components**
- **Vertical Bar Generation** - Real-time audio intensity to vertical bar height mapping
- **Horizontal Scrolling** - Continuous left-to-right movement across the screen
- **Frequency Band Processing** - Configurable number of frequency bands (16-576)
- **Channel Selection** - Left, right, or center channel audio processing
- **Blending System** - Multiple pixel blending algorithms for smooth integration

---

## ⚙️ **Configuration Options**

### **Core Settings**
| Option | Type | Description | Default | Range |
|--------|------|-------------|---------|-------|
| `enabled` | int | Effect enable flag | 1 | 0 or 1 |
| `color` | int | Base color for bars | 0xFFFFFF | 32-bit RGB values |
| `blend` | int | Blending mode | 2 | 0=replace, 1=additive, 2=default |
| `blendavg` | int | 50/50 blending flag | 0 | 0 or 1 |
| `which_ch` | int | Audio channel selection | 2 | 0=left, 1=right, 2=center |
| `nbands` | int | Number of frequency bands | 576 | 16 to 576 |

### **Channel Selection**
- **Left Channel (0)**: Process left channel audio data only
- **Right Channel (1)**: Process right channel audio data only
- **Center Channel (2)**: Mix left and right channels (L+R)/2

### **Blending Modes**
- **Replace (0)**: Direct pixel replacement
- **Additive (1)**: Add new values to existing pixels
- **Default (2)**: Use default blending algorithm
- **50/50 (blendavg)**: Average new and existing pixel values

---

## 🎵 **Audio Data Integration**

### **Data Structure**
```cpp
char visdata[2][2][576]
// [spectrum:0,wave:1][channel][frequency band]
```

### **Audio Processing**
```cpp
// Channel selection and data preparation
if (which_ch >= 2) {
    // Center channel: mix left and right
    for (j = 0; j < 576; j++) {
        center_channel[j] = visdata[1][0][j]/2 + visdata[1][1][j]/2;
    }
    fa_data = (unsigned char *)center_channel;
} else {
    // Left or right channel
    fa_data = (unsigned char *)&visdata[1][which_ch][0];
}

// Frequency band mapping to screen height
for (i = 0; i < h; i++) {
    c = visdata[0][0][(i * nbands) / h] & 0xFF;
    // Process frequency data for each vertical position
}
```

### **Audio Data Format**
- **Source**: Spectrum data (`visdata[0]`) for frequency analysis
- **Resolution**: Up to 576 frequency bands per channel
- **Format**: 8-bit unsigned char (0-255 range)
- **Mapping**: Frequency bands mapped to screen height positions

---

## 🔧 **Rendering Pipeline**

### **1. Horizontal Scrolling**
```cpp
// Increment horizontal position
x++;
x %= w;  // Wrap around screen width

// Position framebuffer pointer
framebuffer += x;
```

### **2. Vertical Bar Generation**
```cpp
// Process each vertical position
for (i = 0; i < h; i++) {
    // Map screen position to frequency band
    c = visdata[0][0][(i * nbands) / h] & 0xFF;
    
    // Apply color scaling
    c = (r * c) / 256 + (((g * c) / 256) << 8) + (((b * c) / 256) << 16);
    
    // Apply blending mode
    if (blend == 2)
        BLEND_LINE(framebuffer, c);
    else if (blend == 1)
        framebuffer[0] = BLEND(framebuffer[0], c);
    else if (blendavg)
        framebuffer[0] = BLEND_AVG(framebuffer[0], c);
    else
        framebuffer[0] = c;
    
    // Move to next vertical position
    framebuffer += w;
}
```

### **3. Color Processing**
```cpp
// Extract RGB components from color
r = color & 0xff;
g = (color >> 8) & 0xff;
b = (color >> 16) & 0xff;

// Scale frequency data by color components
c = (r * c) / 256 + (((g * c) / 256) << 8) + (((b * c) / 256) << 16);
```

---

## 🎨 **Visual Effects**

### **Oscilloscope Display**
- **Vertical Bars**: Each bar represents a frequency band intensity
- **Horizontal Scrolling**: Continuous left-to-right movement
- **Real-time Updates**: Immediate response to audio changes
- **Frequency Mapping**: Screen height mapped to frequency range

### **Channel Visualization**
- **Left Channel**: Independent left channel frequency analysis
- **Right Channel**: Independent right channel frequency analysis
- **Center Channel**: Mixed stereo channel visualization
- **Channel Separation**: Clear distinction between audio sources

### **Blending Effects**
- **Replace Mode**: Clean, direct frequency visualization
- **Additive Mode**: Accumulating intensity over time
- **Default Blending**: Smooth integration with existing content
- **50/50 Blending**: Balanced mixing of old and new data

---

## 📊 **Performance Characteristics**

### **Complexity**
- **Time Complexity**: O(h) - linear with screen height
- **Space Complexity**: O(1) - minimal memory usage
- **Memory Access**: Efficient sequential framebuffer access

### **Optimization Features**
- **Horizontal Scrolling**: Single column update per frame
- **Frequency Mapping**: Direct index calculation for band mapping
- **Blending Selection**: Conditional blending based on mode
- **Channel Processing**: Efficient channel mixing for center mode

---

## 🔌 **Phoenix Integration**

### **AvsModuleNode Implementation**
```csharp
public class TimeDomainScopeNode : AvsModuleNode
{
    // Configuration
    public bool Enabled { get; set; } = true;
    public Color BaseColor { get; set; } = Color.Cyan;
    public BlendingMode BlendingMode { get; set; } = BlendingMode.Default;
    public bool UseFiftyFiftyBlending { get; set; } = false;
    public AudioChannel AudioChannel { get; set; } = AudioChannel.Center;
    public int FrequencyBands { get; set; } = 576;  // 16 to 576
    public float AudioSensitivity { get; set; } = 1.0f;
    public bool ShowGrid { get; set; } = false;
    public int GridSpacing { get; set; } = 32;
    public Color GridColor { get; set; } = Color.FromArgb(64, 64, 64, 64);
    
    // Animation state
    private int horizontalPosition;
    private float frameTime;
    private int frameCount;
    
    // Audio processing
    private float[] leftChannelSpectrum;
    private float[] rightChannelSpectrum;
    private float[] mixedChannelSpectrum;
    private float[] previousSpectrum;
    private int audioBufferSize;
    
    // Rendering state
    private Color[] colorPalette;
    private int colorPaletteSize;
    private bool colorPaletteInitialized;
    
    // Multi-threading support
    private int threadCount;
    private Task[] processingTasks;
    private readonly object lockObject = new object();
    
    // Performance tracking
    private Stopwatch renderTimer;
    private float lastRenderTime;
    
    // Constructor
    public TimeDomainScopeNode()
    {
        // Initialize animation state
        horizontalPosition = 0;
        frameTime = 0;
        frameCount = 0;
        
        // Initialize audio processing
        audioBufferSize = 576; // Standard AVS audio buffer size
        leftChannelSpectrum = new float[audioBufferSize];
        rightChannelSpectrum = new float[audioBufferSize];
        mixedChannelSpectrum = new float[audioBufferSize];
        previousSpectrum = new float[audioBufferSize];
        
        // Initialize color palette
        colorPaletteSize = 256;
        colorPalette = new Color[colorPaletteSize];
        colorPaletteInitialized = false;
        
        // Initialize multi-threading
        threadCount = Environment.ProcessorCount;
        processingTasks = new Task[threadCount];
        
        // Initialize performance tracking
        renderTimer = new Stopwatch();
        lastRenderTime = 0;
        
        // Initialize color palette
        InitializeColorPalette();
    }
    
    // Processing
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled) return;
        
        // Start render timer
        renderTimer.Restart();
        
        // Update frame timing
        frameTime += ctx.DeltaTime;
        frameCount++;
        
        // Copy input to output
        CopyInputToOutput(ctx, input, output);
        
        // Update audio data
        UpdateAudioData(ctx);
        
        // Update horizontal position
        horizontalPosition = (horizontalPosition + 1) % ctx.Width;
        
        // Process audio data for current column
        ProcessAudioColumn(ctx, output, horizontalPosition);
        
        // Render grid if enabled
        if (ShowGrid)
        {
            RenderGrid(ctx, output);
        }
        
        // Stop timer and update statistics
        renderTimer.Stop();
        lastRenderTime = (float)renderTimer.Elapsed.TotalMilliseconds;
    }
    
    private void UpdateAudioData(FrameContext ctx)
    {
        if (ctx.AudioData?.Spectrum != null && ctx.AudioData.Spectrum.Length >= 2)
        {
            // Store previous spectrum
            Array.Copy(mixedChannelSpectrum, previousSpectrum, audioBufferSize);
            
            // Update left channel spectrum
            float[] leftSpectrum = ctx.AudioData.Spectrum[0];
            if (leftSpectrum != null)
            {
                Array.Copy(leftSpectrum, leftChannelSpectrum, Math.Min(leftSpectrum.Length, audioBufferSize));
            }
            
            // Update right channel spectrum
            float[] rightSpectrum = ctx.AudioData.Spectrum[1];
            if (rightSpectrum != null)
            {
                Array.Copy(rightSpectrum, rightChannelSpectrum, Math.Min(rightSpectrum.Length, audioBufferSize));
            }
            
            // Mix channels for center mode
            MixChannels();
        }
    }
    
    private void MixChannels()
    {
        for (int i = 0; i < audioBufferSize; i++)
        {
            mixedChannelSpectrum[i] = (leftChannelSpectrum[i] + rightChannelSpectrum[i]) * 0.5f;
        }
    }
    
    private void ProcessAudioColumn(FrameContext ctx, ImageBuffer output, int x)
    {
        // Get audio data for selected channel
        float[] channelData = GetChannelData(AudioChannel);
        
        // Multi-threaded column processing
        int rowsPerThread = ctx.Height / threadCount;
        
        for (int threadIndex = 0; threadIndex < threadCount; threadIndex++)
        {
            int startRow = threadIndex * rowsPerThread;
            int endRow = (threadIndex == threadCount - 1) ? ctx.Height : startRow + rowsPerThread;
            
            processingTasks[threadIndex] = Task.Run(() =>
            {
                ProcessColumnRange(startRow, endRow, ctx, output, x, channelData);
            });
        }
        
        Task.WaitAll(processingTasks);
    }
    
    private void ProcessColumnRange(int startRow, int endRow, FrameContext ctx, ImageBuffer output, int x, float[] channelData)
    {
        for (int y = startRow; y < endRow; y++)
        {
            // Map screen position to frequency band
            int bandIndex = MapScreenPositionToFrequencyBand(y, ctx.Height);
            if (bandIndex >= channelData.Length) continue;
            
            // Get frequency intensity with smoothing
            float intensity = GetFrequencyIntensity(bandIndex, channelData);
            
            // Apply audio sensitivity
            intensity *= AudioSensitivity;
            
            // Apply color scaling
            Color scaledColor = ScaleColor(BaseColor, intensity);
            
            // Apply blending at current position
            ApplyBlending(output, x, y, scaledColor);
        }
    }
    
    private int MapScreenPositionToFrequencyBand(int screenY, int screenHeight)
    {
        // Map screen Y position to frequency band index
        // Invert Y so that low frequencies are at the bottom
        int invertedY = screenHeight - 1 - screenY;
        
        // Map to frequency band range
        return (invertedY * FrequencyBands) / screenHeight;
    }
    
    private float GetFrequencyIntensity(int bandIndex, float[] channelData)
    {
        if (bandIndex < 0 || bandIndex >= channelData.Length)
            return 0.0f;
        
        // Get current intensity
        float currentIntensity = channelData[bandIndex];
        
        // Apply smoothing with previous frame
        if (bandIndex < previousSpectrum.Length)
        {
            float previousIntensity = previousSpectrum[bandIndex];
            currentIntensity = currentIntensity * 0.7f + previousIntensity * 0.3f;
        }
        
        // Normalize to 0-1 range (AVS uses 0-200 range)
        float normalizedIntensity = Math.Clamp(currentIntensity / 200.0f, 0.0f, 1.0f);
        
        return normalizedIntensity;
    }
    
    private float[] GetChannelData(AudioChannel channel)
    {
        return channel switch
        {
            AudioChannel.Left => leftChannelSpectrum,
            AudioChannel.Right => rightChannelSpectrum,
            AudioChannel.Center => mixedChannelSpectrum,
            _ => mixedChannelSpectrum
        };
    }
    
    private Color ScaleColor(Color baseColor, float intensity)
    {
        // Use color palette for better performance
        int paletteIndex = (int)(intensity * (colorPaletteSize - 1));
        paletteIndex = Math.Clamp(paletteIndex, 0, colorPaletteSize - 1);
        
        return colorPalette[paletteIndex];
    }
    
    private void ApplyBlending(ImageBuffer output, int x, int y, Color newColor)
    {
        if (x < 0 || x >= output.Width || y < 0 || y >= output.Height)
            return;
        
        Color existingColor = output.GetPixel(x, y);
        
        Color finalColor = BlendingMode switch
        {
            BlendingMode.Replace => newColor,
            BlendingMode.Additive => AddColors(existingColor, newColor),
            BlendingMode.Default => BlendColors(existingColor, newColor),
            _ => newColor
        };
        
        if (UseFiftyFiftyBlending)
        {
            finalColor = BlendFiftyFifty(existingColor, newColor);
        }
        
        output.SetPixel(x, y, finalColor);
    }
    
    private Color AddColors(Color existing, Color newColor)
    {
        // Additive blending for light effects
        int red = Math.Min(255, existing.R + newColor.R);
        int green = Math.Min(255, existing.G + newColor.G);
        int blue = Math.Min(255, existing.B + newColor.B);
        int alpha = Math.Min(255, existing.A + newColor.A);
        
        return Color.FromArgb(alpha, red, green, blue);
    }
    
    private Color BlendColors(Color existing, Color newColor)
    {
        // Alpha blending
        float alpha = newColor.A / 255.0f;
        float invAlpha = 1.0f - alpha;
        
        int red = (int)(existing.R * invAlpha + newColor.R * alpha);
        int green = (int)(existing.G * invAlpha + newColor.G * alpha);
        int blue = (int)(existing.B * invAlpha + newColor.B * alpha);
        int finalAlpha = Math.Max(existing.A, newColor.A);
        
        return Color.FromArgb(finalAlpha, red, green, blue);
    }
    
    private Color BlendFiftyFifty(Color existing, Color newColor)
    {
        // 50/50 blending
        int red = (existing.R + newColor.R) / 2;
        int green = (existing.G + newColor.G) / 2;
        int blue = (existing.B + newColor.B) / 2;
        int alpha = (existing.A + newColor.A) / 2;
        
        return Color.FromArgb(alpha, red, green, blue);
    }
    
    private void RenderGrid(FrameContext ctx, ImageBuffer output)
    {
        // Render horizontal grid lines
        for (int y = GridSpacing; y < ctx.Height; y += GridSpacing)
        {
            DrawHorizontalLine(output, 0, ctx.Width - 1, y, GridColor);
        }
        
        // Render vertical grid lines
        for (int x = GridSpacing; x < ctx.Width; x += GridSpacing)
        {
            DrawVerticalLine(output, x, 0, ctx.Height - 1, GridColor);
        }
    }
    
    private void DrawHorizontalLine(ImageBuffer output, int x1, int x2, int y, Color color)
    {
        for (int x = x1; x <= x2; x++)
        {
            if (x >= 0 && x < output.Width && y >= 0 && y < output.Height)
            {
                output.SetPixel(x, y, color);
            }
        }
    }
    
    private void DrawVerticalLine(ImageBuffer output, int x, int y1, int y2, Color color)
    {
        for (int y = y1; y <= y2; y++)
        {
            if (x >= 0 && x < output.Width && y >= 0 && y < output.Height)
            {
                output.SetPixel(x, y, color);
            }
        }
    }
    
    private void CopyInputToOutput(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        
        // Multi-threaded copying
        int rowsPerThread = height / threadCount;
        
        var tasks = new Task[threadCount];
        
        for (int threadIndex = 0; threadIndex < threadCount; threadIndex++)
        {
            int startRow = threadIndex * rowsPerThread;
            int endRow = (threadIndex == threadCount - 1) ? height : startRow + rowsPerThread;
            
            tasks[threadIndex] = Task.Run(() =>
            {
                CopyRowRange(startRow, endRow, width, input, output);
            });
        }
        
        Task.WaitAll(tasks);
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
    
    private void InitializeColorPalette()
    {
        if (colorPaletteInitialized) return;
        
        // Create color palette with intensity variations
        for (int i = 0; i < colorPaletteSize; i++)
        {
            float intensity = (float)i / (colorPaletteSize - 1);
            
            // Create color variations based on base color
            int red = (int)(BaseColor.R * intensity);
            int green = (int)(BaseColor.G * intensity);
            int blue = (int)(BaseColor.B * intensity);
            int alpha = (int)(BaseColor.A * intensity);
            
            colorPalette[i] = Color.FromArgb(alpha, red, green, blue);
        }
        
        colorPaletteInitialized = true;
    }
    
    // Public methods for external access
    public void SetAudioSensitivity(float sensitivity)
    {
        AudioSensitivity = Math.Clamp(sensitivity, 0.1f, 5.0f);
    }
    
    public void SetFrequencyBands(int bands)
    {
        FrequencyBands = Math.Clamp(bands, 16, 576);
    }
    
    public void SetGridSpacing(int spacing)
    {
        GridSpacing = Math.Clamp(spacing, 8, 128);
    }
    
    public void UpdateColorPalette()
    {
        colorPaletteInitialized = false;
        InitializeColorPalette();
    }
    
    public void ResetPosition()
    {
        horizontalPosition = 0;
    }
    
    public int GetHorizontalPosition() => horizontalPosition;
    public float GetLastRenderTime() => lastRenderTime;
    public int GetFrameCount() => frameCount;
    
    // Dispose pattern
    public override void Dispose()
    {
        renderTimer?.Stop();
        base.Dispose();
    }
}

public enum BlendingMode
{
    Replace, Additive, Default
}

public enum AudioChannel
{
    Left, Right, Center
}
```

### **Optimization Strategies**
- **Efficient column processing**: Single column update per frame with multi-threading
- **Frequency mapping**: Direct index calculation for band mapping with inverted Y coordinates
- **Color scaling**: Pre-computed color palette for performance optimization
- **Blending selection**: Multiple blending modes with conditional processing
- **Audio smoothing**: Frame-to-frame smoothing for stable visualization
- **Grid rendering**: Optional grid overlay for better visual reference
- **Memory management**: Efficient buffer handling and minimal allocation
- **Performance monitoring**: Real-time render timing and frame counting

---

## 📚 **Use Cases**

### **Audio Visualization**
- **Real-time Frequency Analysis**: Live display of frequency content
- **Channel Comparison**: Visual distinction between audio channels
- **Time-domain Analysis**: How frequencies change over time
- **Audio Monitoring**: Real-time audio intensity visualization

### **Visual Effects**
- **Waterfall Display**: Continuous frequency band visualization
- **Dynamic Patterns**: Moving frequency bars synchronized with music
- **Channel Separation**: Clear visualization of stereo separation
- **Blending Integration**: Smooth integration with other effects

---

## 🚀 **Future Enhancements**

### **Phoenix-Specific Features**
- **Advanced Frequency Mapping**: Logarithmic frequency scaling
- **3D Visualization**: Depth-based frequency display
- **Real-time Editing**: Live parameter adjustment during visualization
- **Multiple Display Modes**: Different visualization styles

### **Advanced Audio Features**
- **FFT Integration**: High-resolution frequency analysis
- **Beat Synchronization**: Frequency display synchronized with beats
- **Harmonic Analysis**: Harmonic content visualization
- **Multi-band Processing**: Separate processing for different frequency ranges

---

## 📖 **References**

- **Source Code**: `r_timescope.cpp` from VIS_AVS
- **Header**: `r_defs.h` for base class definitions
- **Audio Processing**: Real-time spectrum data analysis
- **Rendering**: Vertical bar generation and horizontal scrolling
- **Blending**: Multiple pixel blending algorithms
- **Channel Processing**: Left, right, and center channel audio handling

---

**Status:** ✅ **THIRTEENTH EFFECT DOCUMENTED**  
**Next:** Blit Operations effect analysis (`r_blit.cpp`)
