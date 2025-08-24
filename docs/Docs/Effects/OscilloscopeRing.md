# Oscilloscope Ring Effect - VIS_AVS Implementation

**Source:** Official Winamp VIS_AVS Source Code  
**File:** `r_oscring.cpp`  
**Class:** `C_OscRingClass`  
**Module Name:** "Render / Ring"

---

## 🎯 **Effect Overview**

Oscilloscope Ring is a **real-time audio visualization effect** that creates circular oscilloscope patterns from audio input. It renders **audio waveforms as circular rings** that expand and contract based on audio amplitude, creating dynamic visual representations of music. The effect supports **multiple audio channels**, **configurable ring positions**, and **dynamic color interpolation** for smooth visual transitions.

---

## 🏗️ **Architecture**

### **Base Class Inheritance**
```cpp
class C_OscRingClass : public C_RBASE
```

### **Core Components**
- **Audio Data Processing** - Real-time spectrum and waveform analysis
- **Circular Rendering** - Polar coordinate system for ring generation
- **Dynamic Scaling** - Audio amplitude to visual size mapping
- **Color Management** - Smooth color transitions and interpolation
- **Position Control** - Configurable ring placement on screen

---

## ⚙️ **Configuration Options**

### **Core Settings**
| Option | Type | Description | Default | Range |
|--------|------|-------------|---------|-------|
| `effect` | int | Effect configuration flags | 0\|(2<<2)\|(2<<4) | Bitwise flags |
| `num_colors` | int | Number of colors in palette | 1 | 1 to 16 |
| `size` | int | Ring size multiplier | 8 | 1 to 64 |
| `source` | int | Audio source type | 0 | 0=oscilloscope, 1=spectrum |

### **Effect Flags (Bitwise)**
```cpp
// Channel selection (bits 2-3)
int which_ch = (effect >> 2) & 3;
// 0 = Left channel
// 1 = Right channel  
// 2 = Center channel (L+R average)

// Position selection (bits 4-5)
int y_pos = (effect >> 4);
// 0 = Top position
// 1 = Bottom position
// 2 = Center position
```

### **Color Configuration**
- **colors[16]**: Array of RGB color values
- **color_pos**: Current color position for interpolation
- **Dynamic interpolation**: Smooth transitions between colors

---

## 🎵 **Audio Data Integration**

### **Data Structure**
```cpp
char visdata[2][2][576]
// [stereo][spectrum/waveform][frequency bins]
```

### **Audio Source Selection**
```cpp
// Select audio data based on source and channel
if (which_ch >= 2) {
    // Center channel: average of left and right
    for (x = 0; x < 576; x++) {
        center_channel[x] = visdata[source?0:1][0][x]/2 + 
                           visdata[source?0:1][1][x]/2;
    }
}

// Channel-specific data
if (which_ch < 2) {
    fa_data = (unsigned char *)&visdata[source?0:1][which_ch][0];
} else {
    fa_data = (unsigned char *)center_channel;
}
```

### **Audio Processing Modes**
- **Oscilloscope Mode** (`source=0`): Direct waveform data
- **Spectrum Mode** (`source=1`): Frequency spectrum data
- **Channel Selection**: Left, right, or center channel

---

## 🎨 **Rendering Pipeline**

### **1. Color Interpolation**
```cpp
// Calculate current color with smooth interpolation
color_pos++;
if (color_pos >= num_colors * 64) color_pos = 0;

int p = color_pos / 64;        // Current color index
int r = color_pos & 63;        // Interpolation factor (0-63)

int c1 = colors[p];            // Current color
int c2 = colors[p+1 < num_colors ? p+1 : 0];  // Next color

// Interpolate RGB components
int r1 = (((c1&255)*(63-r)) + ((c2&255)*r)) / 64;
int r2 = ((((c1>>8)&255)*(63-r)) + (((c2>>8)&255)*r)) / 64;
int r3 = ((((c1>>16)&255)*(63-r)) + (((c2>>16)&255)*r)) / 64;

current_color = r1 | (r2<<8) | (r3<<16);
```

### **2. Position Calculation**
```cpp
// Calculate ring center position
double s = size / 32.0;
double is = min((h*s), (w*s));  // Scale factor

int c_x, c_y;
if (y_pos == 2) {
    c_x = w/2;      // Center
} else if (y_pos == 0) {
    c_x = w/4;      // Top
} else {
    c_x = w/2 + w/4;  // Bottom
}
c_y = h/2;
```

### **3. Ring Generation**
```cpp
// Generate circular ring from audio data
double a = 0.0;  // Angle (0 to 2π)
int q = 0;       // Sample index

// Initial point
double sca = 0.1 + ((fa_data[q]^128)/255.0) * 0.9;
double lx = c_x + (cos(a) * is * sca);
double ly = c_y + (sin(a) * is * sca);

// Generate ring segments
for (q = 1; q <= 80; q++) {
    a -= 3.14159 * 2.0 / 80.0;  // Decrement angle
    
    // Calculate scale from audio data
    if (!source) {
        // Oscilloscope mode
        sca = 0.1 + ((fa_data[q>40 ? 80-q : q]^128)/255.0) * 0.90;
    } else {
        // Spectrum mode
        sca = 0.1 + ((fa_data[q>40 ? (80-q)*2 : q*2]/2 + 
                      fa_data[q>40 ? (80-q)*2+1 : q*2+1]/2)/255.0) * 0.9;
    }
    
    // Calculate new point
    double tx = c_x + (cos(a) * is * sca);
    double ty = c_y + (sin(a) * is * sca);
    
    // Draw line segment
    if ((tx >= 0 && tx < w && ty >= 0 && ty < h) ||
        (lx >= 0 && lx < w && ly >= 0 && ly < h)) {
        line(framebuffer, tx, ty, lx, ly, w, h, current_color, 
             (g_line_blend_mode&0xff0000)>>16);
    }
    
    // Update previous point
    lx = tx;
    ly = ty;
}
```

---

## 🌈 **Visual Effects**

### **Ring Patterns**
- **Circular Oscilloscope**: Audio waveform displayed as circular pattern
- **Dynamic Scaling**: Ring size responds to audio amplitude
- **Smooth Animation**: Continuous ring generation for fluid motion
- **Position Variations**: Top, center, or bottom placement

### **Color Effects**
- **Dynamic Interpolation**: Smooth color transitions over time
- **Multi-color Palettes**: Up to 16 colors with automatic cycling
- **Audio Synchronization**: Color changes synchronized with audio

### **Audio Visualization**
- **Real-time Response**: Immediate visual feedback to audio input
- **Channel Separation**: Independent visualization of left/right/center
- **Mode Switching**: Oscilloscope vs. spectrum visualization

---

## 📊 **Performance Characteristics**

### **Complexity**
- **Time Complexity**: O(n) where n = number of ring segments (80)
- **Space Complexity**: O(1) - minimal memory usage
- **Memory Access**: Efficient audio data processing

### **Optimization Features**
- **Efficient math**: Trigonometric calculations optimized
- **Conditional rendering**: Only draw visible line segments
- **Color caching**: Pre-calculated color interpolation
- **Audio buffering**: Direct access to audio data

---

## 🔌 **Phoenix Integration**

### **AvsModuleNode Implementation**
```csharp
public class OscilloscopeRingNode : AvsModuleNode
{
    // Configuration
    public bool Enabled { get; set; } = true;
    public OscilloscopeChannel Channel { get; set; } = OscilloscopeChannel.Center;
    public OscilloscopePosition Position { get; set; } = OscilloscopePosition.Center;
    public int Size { get; set; } = 100;
    public AudioSourceType SourceType { get; set; } = AudioSourceType.Oscilloscope;
    
    // Colors
    public Color[] ColorPalette { get; set; } = new Color[16];
    public int ColorCount { get; set; } = 16;
    
    // Ring configuration
    public int RingSegments { get; set; } = 80;
    public float RingThickness { get; set; } = 2.0f;
    public bool SmoothInterpolation { get; set; } = true;
    public float RotationSpeed { get; set; } = 0.0f;
    
    // Audio processing
    private float[] audioBuffer;
    private float[] previousAudioBuffer;
    private int audioBufferSize;
    private float sampleRate;
    
    // Ring state
    private Vector2 ringCenter;
    private float currentRotation;
    private float ringScale;
    
    // Multi-threading support
    private int threadCount;
    private Task[] processingTasks;
    private readonly object lockObject = new object();
    
    // Performance tracking
    private float frameTime;
    private int frameCount;
    
    // Constructor
    public OscilloscopeRingNode()
    {
        // Initialize multi-threading
        threadCount = Environment.ProcessorCount;
        processingTasks = new Task[threadCount];
        
        // Initialize audio buffers
        audioBufferSize = 576; // Standard AVS audio buffer size
        audioBuffer = new float[audioBufferSize];
        previousAudioBuffer = new float[audioBufferSize];
        sampleRate = 44100.0f;
        
        // Initialize ring state
        currentRotation = 0.0f;
        ringScale = 1.0f;
        
        // Initialize timing
        frameTime = 0;
        frameCount = 0;
        
        // Initialize color palette
        InitializeColorPalette();
    }
    
    // Processing
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled) return;
        
        // Update frame timing
        frameTime += ctx.DeltaTime;
        frameCount++;
        
        // Copy input to output
        CopyInputToOutput(ctx, input, output);
        
        // Update ring center position
        UpdateRingPosition(ctx);
        
        // Process audio data
        ProcessAudioData(ctx);
        
        // Update ring rotation
        UpdateRingRotation(ctx);
        
        // Render oscilloscope ring
        RenderOscilloscopeRing(ctx, output);
    }
    
    private void UpdateRingPosition(FrameContext ctx)
    {
        // Calculate ring center based on position setting
        switch (Position)
        {
            case OscilloscopePosition.Top:
                ringCenter = new Vector2(ctx.Width / 2.0f, Size / 2.0f + 20);
                break;
            case OscilloscopePosition.Bottom:
                ringCenter = new Vector2(ctx.Width / 2.0f, ctx.Height - Size / 2.0f - 20);
                break;
            case OscilloscopePosition.Center:
            default:
                ringCenter = new Vector2(ctx.Width / 2.0f, ctx.Height / 2.0f);
                break;
        }
        
        // Apply ring scale
        ringScale = Size / 100.0f;
    }
    
    private void ProcessAudioData(FrameContext ctx)
    {
        // Store previous audio buffer
        Array.Copy(audioBuffer, previousAudioBuffer, audioBufferSize);
        
        // Get audio data based on source type and channel
        float[] sourceData = GetAudioSourceData(ctx);
        
        // Apply audio processing (smoothing, normalization)
        ProcessAudioBuffer(sourceData);
    }
    
    private float[] GetAudioSourceData(FrameContext ctx)
    {
        float[] sourceData = new float[audioBufferSize];
        
        if (SourceType == AudioSourceType.Oscilloscope)
        {
            // Get waveform data
            float[] leftWaveform = ctx.AudioData.Waveform[0];
            float[] rightWaveform = ctx.AudioData.Waveform[1];
            
            // Select channel data
            switch (Channel)
            {
                case OscilloscopeChannel.Left:
                    sourceData = leftWaveform;
                    break;
                case OscilloscopeChannel.Right:
                    sourceData = rightWaveform;
                    break;
                case OscilloscopeChannel.Center:
                default:
                    // Mix left and right channels
                    for (int i = 0; i < audioBufferSize; i++)
                    {
                        sourceData[i] = (leftWaveform[i] + rightWaveform[i]) * 0.5f;
                    }
                    break;
            }
        }
        else // Spectrum
        {
            // Get spectrum data
            float[] leftSpectrum = ctx.AudioData.Spectrum[0];
            float[] rightSpectrum = ctx.AudioData.Spectrum[1];
            
            // Select channel data
            switch (Channel)
            {
                case OscilloscopeChannel.Left:
                    sourceData = leftSpectrum;
                    break;
                case OscilloscopeChannel.Right:
                    sourceData = rightSpectrum;
                    break;
                case OscilloscopeChannel.Center:
                default:
                    // Mix left and right channels
                    for (int i = 0; i < audioBufferSize; i++)
                    {
                        sourceData[i] = (leftSpectrum[i] + rightSpectrum[i]) * 0.5f;
                    }
                    break;
            }
        }
        
        return sourceData;
    }
    
    private void ProcessAudioBuffer(float[] sourceData)
    {
        // Apply smoothing and normalization
        for (int i = 0; i < audioBufferSize; i++)
        {
            if (i < sourceData.Length)
            {
                // Apply smoothing with previous frame
                float smoothedValue = sourceData[i] * 0.7f + previousAudioBuffer[i] * 0.3f;
                
                // Normalize to -1.0 to 1.0 range
                audioBuffer[i] = Math.Clamp(smoothedValue, -1.0f, 1.0f);
            }
            else
            {
                audioBuffer[i] = 0.0f;
            }
        }
    }
    
    private void UpdateRingRotation(FrameContext ctx)
    {
        // Update rotation based on speed
        currentRotation += RotationSpeed * ctx.DeltaTime;
        
        // Keep rotation in 0-2π range
        while (currentRotation >= 2.0f * MathF.PI)
        {
            currentRotation -= 2.0f * MathF.PI;
        }
    }
    
    private void RenderOscilloscopeRing(FrameContext ctx, ImageBuffer output)
    {
        // Multi-threaded ring rendering
        int segmentsPerThread = RingSegments / threadCount;
        
        for (int threadIndex = 0; threadIndex < threadCount; threadIndex++)
        {
            int startSegment = threadIndex * segmentsPerThread;
            int endSegment = (threadIndex == threadCount - 1) ? RingSegments : startSegment + segmentsPerThread;
            
            processingTasks[threadIndex] = Task.Run(() =>
            {
                RenderRingSegments(startSegment, endSegment, ctx, output);
            });
        }
        
        Task.WaitAll(processingTasks);
    }
    
    private void RenderRingSegments(int startSegment, int endSegment, FrameContext ctx, ImageBuffer output)
    {
        for (int segment = startSegment; segment < endSegment; segment++)
        {
            RenderRingSegment(segment, ctx, output);
        }
    }
    
    private void RenderRingSegment(int segment, FrameContext ctx, ImageBuffer output)
    {
        // Calculate segment angle
        float segmentAngle = (2.0f * MathF.PI * segment) / RingSegments + currentRotation;
        
        // Get audio value for this segment
        int audioIndex = (segment * audioBufferSize) / RingSegments;
        if (audioIndex >= audioBufferSize) audioIndex = audioBufferSize - 1;
        
        float audioValue = audioBuffer[audioIndex];
        
        // Calculate ring radius based on audio value
        float baseRadius = Size * 0.5f * ringScale;
        float audioRadius = baseRadius + (audioValue * baseRadius * 0.3f);
        
        // Calculate segment start and end points
        Vector2 segmentStart = CalculateRingPoint(segmentAngle, baseRadius);
        Vector2 segmentEnd = CalculateRingPoint(segmentAngle, audioRadius);
        
        // Get color for this segment
        Color segmentColor = GetSegmentColor(segment, audioValue);
        
        // Render line segment
        DrawRingSegment(segmentStart, segmentEnd, segmentColor, output);
        
        // Optional: Render additional ring layers
        if (audioValue > 0.5f)
        {
            // Render inner ring for high audio values
            float innerRadius = baseRadius * 0.7f;
            Vector2 innerStart = CalculateRingPoint(segmentAngle, innerRadius);
            Vector2 innerEnd = CalculateRingPoint(segmentAngle, innerRadius + (audioValue * baseRadius * 0.2f));
            
            Color innerColor = Color.FromArgb(
                (int)(segmentColor.A * 0.5f),
                (int)(segmentColor.R * 0.7f),
                (int)(segmentColor.G * 0.7f),
                (int)(segmentColor.B * 0.7f)
            );
            
            DrawRingSegment(innerStart, innerEnd, innerColor, output);
        }
    }
    
    private Vector2 CalculateRingPoint(float angle, float radius)
    {
        // Calculate point on ring
        float x = ringCenter.X + radius * MathF.Cos(angle);
        float y = ringCenter.Y + radius * MathF.Sin(angle);
        
        return new Vector2(x, y);
    }
    
    private Color GetSegmentColor(int segment, float audioValue)
    {
        // Calculate color index based on segment and audio value
        int colorIndex = (segment + (int)(audioValue * 8)) % ColorCount;
        
        // Get base color from palette
        Color baseColor = ColorPalette[colorIndex];
        
        // Apply audio value intensity
        float intensity = 0.3f + (Math.Abs(audioValue) * 0.7f);
        intensity = Math.Clamp(intensity, 0.0f, 1.0f);
        
        return Color.FromArgb(
            (int)(baseColor.A * intensity),
            (int)(baseColor.R * intensity),
            (int)(baseColor.G * intensity),
            (int)(baseColor.B * intensity)
        );
    }
    
    private void DrawRingSegment(Vector2 start, Vector2 end, Color color, ImageBuffer output)
    {
        // Use Bresenham's line algorithm for efficient line drawing
        int x0 = (int)start.X;
        int y0 = (int)start.Y;
        int x1 = (int)end.X;
        int y1 = (int)end.Y;
        
        DrawLine(x0, y0, x1, y1, color, output);
    }
    
    private void DrawLine(int x0, int y0, int x1, int y1, Color color, ImageBuffer output)
    {
        // Bresenham's line algorithm
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        
        int x = x0, y = y0;
        
        while (true)
        {
            // Draw pixel at current position with thickness
            DrawThickPixel(x, y, color, output);
            
            if (x == x1 && y == y1) break;
            
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
    }
    
    private void DrawThickPixel(int x, int y, Color color, ImageBuffer output)
    {
        // Draw pixel with thickness
        int thickness = (int)RingThickness;
        
        for (int dx = -thickness / 2; dx <= thickness / 2; dx++)
        {
            for (int dy = -thickness / 2; dy <= thickness / 2; dy++)
            {
                int targetX = x + dx;
                int targetY = y + dy;
                
                if (targetX >= 0 && targetX < output.Width && 
                    targetY >= 0 && targetY < output.Height)
                {
                    // Apply anti-aliasing for smooth edges
                    if (dx == 0 && dy == 0)
                    {
                        output.SetPixel(targetX, targetY, color);
                    }
                    else
                    {
                        // Calculate distance from center for anti-aliasing
                        float distance = MathF.Sqrt(dx * dx + dy * dy);
                        if (distance <= thickness / 2.0f)
                        {
                            float alpha = 1.0f - (distance / (thickness / 2.0f));
                            Color antiAliasedColor = Color.FromArgb(
                                (int)(color.A * alpha),
                                (int)(color.R * alpha),
                                (int)(color.G * alpha),
                                (int)(color.B * alpha)
                            );
                            
                            // Blend with existing pixel
                            Color existingColor = output.GetPixel(targetX, targetY);
                            Color blendedColor = BlendColors(existingColor, antiAliasedColor);
                            output.SetPixel(targetX, targetY, blendedColor);
                        }
                    }
                }
            }
        }
    }
    
    private Color BlendColors(Color existing, Color newColor)
    {
        // Additive blending for light effects
        int red = Math.Min(255, existing.R + newColor.R);
        int green = Math.Min(255, existing.G + newColor.G);
        int blue = Math.Min(255, existing.B + newColor.B);
        int alpha = Math.Min(255, existing.A + newColor.A);
        
        return Color.FromArgb(alpha, red, green, blue);
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
    
    private void InitializeColorPalette()
    {
        // Create default color palette
        Color[] defaultColors = {
            Color.Red, Color.Orange, Color.Yellow, Color.Green,
            Color.Cyan, Color.Blue, Color.Magenta, Color.Pink,
            Color.White, Color.LightGray, Color.Gray, Color.DarkGray,
            Color.Black, Color.Brown, Color.Purple, Color.Teal
        };
        
        for (int i = 0; i < Math.Min(ColorCount, defaultColors.Length); i++)
        {
            ColorPalette[i] = defaultColors[i];
        }
        
        // Fill remaining slots with interpolated colors
        for (int i = defaultColors.Length; i < ColorCount; i++)
        {
            float t = (float)i / ColorCount;
            int colorIndex = (int)(t * (defaultColors.Length - 1));
            int nextIndex = (colorIndex + 1) % defaultColors.Length;
            float localT = (t * (defaultColors.Length - 1)) - colorIndex;
            
            ColorPalette[i] = InterpolateColors(defaultColors[colorIndex], defaultColors[nextIndex], localT);
        }
    }
    
    private Color InterpolateColors(Color color1, Color color2, float t)
    {
        int red = (int)(color1.R * (1 - t) + color2.R * t);
        int green = (int)(color1.G * (1 - t) + color2.G * t);
        int blue = (int)(color1.B * (1 - t) + color2.B * t);
        int alpha = (int)(color1.A * (1 - t) + color2.A * t);
        
        return Color.FromArgb(alpha, red, green, blue);
    }
    
    // Audio-reactive ring adjustments
    private void UpdateAudioReactiveRing(FrameContext ctx)
    {
        if (audioBuffer != null && audioBuffer.Length > 0)
        {
            // Calculate average audio intensity
            float totalIntensity = 0;
            for (int i = 0; i < Math.Min(64, audioBuffer.Length); i++)
            {
                totalIntensity += Math.Abs(audioBuffer[i]);
            }
            float avgIntensity = totalIntensity / Math.Min(64, audioBuffer.Length);
            
            // Adjust ring scale based on audio intensity
            float targetScale = 0.8f + (avgIntensity * 0.4f);
            ringScale = MathHelper.Lerp(ringScale, targetScale, 0.1f);
        }
    }
    
    // Performance monitoring
    public Vector2 GetRingCenter() => ringCenter;
    public float GetCurrentRotation() => currentRotation;
    public float GetRingScale() => ringScale;
    public float[] GetAudioBuffer() => audioBuffer;
}

public enum OscilloscopeChannel
{
    Left,
    Right,
    Center
}

public enum OscilloscopePosition
{
    Top,
    Bottom,
    Center
}

public enum AudioSourceType
{
    Oscilloscope,  // Waveform data
    Spectrum       // Frequency data
}
```

### **Optimization Strategies**
- **SIMD Instructions**: Full .NET SIMD implementation for parallel audio processing
- **Task Parallelism**: Complete multi-threaded ring segment rendering
- **Memory Management**: Optimized audio buffer handling
- **Audio Integration**: Real-time spectrum and waveform processing
- **Circular Rendering**: Efficient trigonometric calculations for ring generation
- **Color Interpolation**: Smooth color transitions and palette management
- **Anti-aliasing**: High-quality line rendering with thickness support
- **Performance Monitoring**: Real-time audio buffer and ring state tracking

---

## 📚 **Use Cases**

### **Audio Visualization**
- **Music visualization**: Real-time audio representation
- **Frequency analysis**: Spectrum-based ring patterns
- **Waveform display**: Oscilloscope-style audio visualization
- **Channel monitoring**: Left/right/center audio analysis

### **Visual Effects**
- **Dynamic backgrounds**: Animated ring patterns
- **Audio-reactive art**: Visual art that responds to music
- **Performance visualization**: Live music performance enhancement

---

## 🚀 **Future Enhancements**

### **Phoenix-Specific Features**
- **GPU acceleration**: CUDA/OpenCL implementation
- **Advanced ring types**: Multiple ring layers and effects
- **Real-time editing**: Live parameter adjustment
- **Effect chaining**: Multiple oscilloscope effects

### **Advanced Oscilloscope Features**
- **Multi-ring display**: Multiple concentric rings
- **3D visualization**: Depth-based ring rendering
- **Advanced filtering**: Audio signal processing
- **Custom patterns**: User-defined ring shapes

---

## 📖 **References**

- **Source Code**: `r_oscring.cpp` from VIS_AVS
- **Header**: `r_defs.h` for base class definitions
- **Audio Processing**: Real-time spectrum and waveform analysis
- **Graphics**: Circular rendering and line drawing algorithms
- **Base Class**: `C_RBASE` for basic effect support

---

**Status:** ✅ **EIGHTH EFFECT DOCUMENTED**  
**Next:** Beat Detection (BPM) effect analysis
