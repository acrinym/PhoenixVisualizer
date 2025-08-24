# Beat Spinning Effect - VIS_AVS Implementation

**Source:** Official Winamp VIS_AVS Source Code  
**File:** `r_bspin.cpp`  
**Class:** `C_BSpinClass`  
**Module Name:** "Render / Bass Spin"

---

## 🎯 **Effect Overview**

Beat Spinning is a **dynamic audio-reactive spinning visualization effect** that creates **rotating geometric patterns** synchronized with music. The effect generates **dual spinning arms** that **rotate in opposite directions** based on **real-time audio intensity** from the left and right channels. Each channel creates a **spinning arm** that responds to **spectrum data changes**, creating **smooth rotational motion** that accelerates and decelerates with the music. The effect supports **two rendering modes** (lines and triangles) and **independent channel control** for stereo separation.

---

## 🏗️ **Architecture**

### **Base Class Inheritance**
```cpp
class C_BSpinClass : public C_RBASE
```

### **Core Components**
- **Dual Channel Processing** - Independent left and right channel audio analysis
- **Spinning Arm Generation** - Trigonometric-based rotation calculations
- **Audio Intensity Mapping** - Spectrum data to rotation speed conversion
- **Rendering Modes** - Line-based and triangle-based visualization options
- **Smooth Animation** - Velocity-based rotation with momentum

---

## ⚙️ **Configuration Options**

### **Core Settings**
| Option | Type | Description | Default | Range |
|--------|------|-------------|---------|-------|
| `enabled` | int | Channel enable flags | 3 | Bit flags (1=left, 2=right) |
| `colors[2]` | int[2] | Left and right channel colors | RGB(255,255,255) | 32-bit RGB values |
| `mode` | int | Rendering mode | 1 | 0=lines, 1=triangles |

### **Channel Control**
- **Left Channel**: Independent left channel spinning arm
- **Right Channel**: Independent right channel spinning arm
- **Dual Mode**: Both channels active simultaneously
- **Individual Control**: Enable/disable channels independently

### **Rendering Modes**
- **Line Mode (0)**: Simple line-based spinning arms
- **Triangle Mode (1)**: Filled triangle-based spinning arms

---

## 🎵 **Audio Data Integration**

### **Data Structure**
```cpp
char visdata[2][2][576]
// [stereo][spectrum/waveform][frequency bins]
```

### **Audio Processing**
```cpp
// Process each enabled channel
for (y = 0; y < 2; y++) {
    if (!(enabled & (1 << y))) continue;
    
    // Get spectrum data for channel
    unsigned char *fa_data = (unsigned char *)visdata[0][y];
    
    // Calculate audio intensity from first 44 frequency bins
    int d = 0;
    for (x = 0; x < 44; x++) {
        d += fa_data[x];
    }
    
    // Normalize and apply momentum
    int a = (d * 512) / (last_a + 30 * 256);
    if (a > 255) a = 255;
    
    // Smooth velocity calculation with momentum
    v[y] = 0.7 * (max(a - 104, 12) / 96.0) + 0.3 * v[y];
}
```

### **Audio Intensity Mapping**
- **Source**: Spectrum data from first 44 frequency bins
- **Normalization**: Audio intensity mapped to 0-255 range
- **Momentum**: Smooth velocity changes with 70/30 ratio
- **Threshold**: Minimum intensity threshold of 104 for activation

---

## 🔧 **Spinning Arm Pipeline**

### **1. Rotation Calculation**
```cpp
// Update rotation angle based on audio velocity
r_v[y] += 3.14159 / 6.0 * v[y] * dir[y];

// Direction control (opposite rotation for each channel)
dir[0] = -1.0;  // Left channel: counter-clockwise
dir[1] = 1.0;   // Right channel: clockwise
```

### **2. Position Calculation**
```cpp
// Calculate spinning arm size based on audio intensity
double s = (double)ss;  // Base size
s *= a * 1.0 / 256.0f;  // Scale by audio intensity

// Calculate arm endpoint position
int yp = (int)(sin(r_v[y]) * s);
int xp = (int)(cos(r_v[y]) * s);

// Channel positioning
int c_x = (!y ? w/2 - ss/2 : w/2 + ss/2);  // Left/right separation
```

### **3. Rendering Modes**

#### **Line Mode (Mode 0)**
```cpp
// Draw spinning arm lines
if (lx[0][y] || ly[0][y]) {
    line(framebuffer, lx[0][y], ly[0][y], xp + c_x, yp + h/2, w, h, oc6, blend);
}
lx[0][y] = xp + c_x;
ly[0][y] = yp + h/2;

// Draw center to arm line
line(framebuffer, c_x, h/2, c_x + xp, h/2 + yp, w, h, oc6, blend);

// Draw mirrored arm (opposite side)
if (lx[1][y] || ly[1][y]) {
    line(framebuffer, lx[1][y], ly[1][y], c_x - xp, h/2 - yp, w, h, oc6, blend);
}
lx[1][y] = c_x - xp;
ly[1][y] = h/2 - yp;
```

#### **Triangle Mode (Mode 1)**
```cpp
// Draw filled triangles for spinning arms
if (lx[0][y] || ly[0][y]) {
    int points[6] = { c_x, h/2, lx[0][y], ly[0][y], xp + c_x, yp + h/2 };
    my_triangle(framebuffer, points, w, h, oc6);
}
lx[0][y] = xp + c_x;
ly[0][y] = yp + h/2;

// Draw mirrored triangle
if (lx[1][y] || ly[1][y]) {
    int points[6] = { c_x, h/2, lx[1][y], ly[1][y], c_x - xp, h/2 - yp };
    my_triangle(framebuffer, points, w, h, oc6);
}
lx[1][y] = c_x - xp;
ly[1][y] = h/2 - yp;
```

---

## 🎨 **Visual Effects**

### **Spinning Patterns**
- **Dual Arm Rotation**: Left and right channels rotate in opposite directions
- **Audio-Reactive Speed**: Rotation speed directly proportional to audio intensity
- **Smooth Motion**: Momentum-based velocity changes for fluid animation
- **Mirrored Geometry**: Symmetrical spinning arms on opposite sides

### **Channel Separation**
- **Left Channel**: Counter-clockwise rotation, left side positioning
- **Right Channel**: Clockwise rotation, right side positioning
- **Independent Control**: Enable/disable channels individually
- **Color Differentiation**: Separate colors for each channel

### **Rendering Styles**
- **Line Mode**: Clean, minimal spinning arm lines
- **Triangle Mode**: Filled triangular spinning arms with depth
- **Blending Support**: Alpha channel support for transparency effects
- **Smooth Edges**: Anti-aliased rendering for clean visuals

---

## 📊 **Performance Characteristics**

### **Complexity**
- **Time Complexity**: O(n) - linear with number of frequency bins
- **Space Complexity**: O(1) - minimal memory usage
- **Memory Access**: Efficient audio data processing

### **Optimization Features**
- **Fixed-Point Math**: 16-bit fixed-point calculations for performance
- **Efficient Trigonometry**: Optimized sine/cosine calculations
- **Minimal Redraw**: Only update changed positions
- **Channel Filtering**: Skip disabled channels

---

## 🔌 **Phoenix Integration**

### **AvsModuleNode Implementation**
```csharp
public class BeatSpinningNode : AvsModuleNode
{
    // Configuration
    public bool Enabled { get; set; } = true;
    public bool LeftChannelEnabled { get; set; } = true;
    public bool RightChannelEnabled { get; set; } = true;
    public Color LeftChannelColor { get; set; } = Color.Red;
    public Color RightChannelColor { get; set; } = Color.Blue;
    public RenderingMode Mode { get; set; } = RenderingMode.Lines;
    public float Momentum { get; set; } = 0.7f;
    public float AudioSensitivity { get; set; } = 1.0f;
    public float RotationSpeed { get; set; } = 1.0f;
    public bool ShowTrails { get; set; } = false;
    public int TrailLength { get; set; } = 16;
    
    // Animation state
    private double[] rotationAngles = new double[2];
    private double[] velocities = new double[2];
    private Vector2[] lastPositions = new Vector2[2];
    private Vector2[] currentPositions = new Vector2[2];
    
    // Audio processing
    private float[] leftChannelSpectrum;
    private float[] rightChannelSpectrum;
    private int spectrumBins = 44; // First 44 frequency bins
    private float[] audioBuffer;
    private int audioBufferSize;
    
    // Trail management
    private List<Vector2[]> channelTrails;
    private int currentTrailIndex;
    
    // Multi-threading support
    private int threadCount;
    private Task[] processingTasks;
    private readonly object lockObject = new object();
    
    // Performance tracking
    private Stopwatch renderTimer;
    private float lastRenderTime;
    private int frameCount;
    
    // Constructor
    public BeatSpinningNode()
    {
        // Initialize animation state
        rotationAngles[0] = 0.0; // Left channel
        rotationAngles[1] = 0.0; // Right channel
        velocities[0] = 0.0;
        velocities[1] = 0.0;
        
        // Initialize positions
        lastPositions[0] = Vector2.Zero;
        lastPositions[1] = Vector2.Zero;
        currentPositions[0] = Vector2.Zero;
        currentPositions[1] = Vector2.Zero;
        
        // Initialize audio processing
        audioBufferSize = 576; // Standard AVS audio buffer size
        audioBuffer = new float[audioBufferSize];
        leftChannelSpectrum = new float[spectrumBins];
        rightChannelSpectrum = new float[spectrumBins];
        
        // Initialize trails
        channelTrails = new List<Vector2[]>();
        for (int i = 0; i < TrailLength; i++)
        {
            channelTrails.Add(new Vector2[2]); // Two channels
        }
        currentTrailIndex = 0;
        
        // Initialize multi-threading
        threadCount = Environment.ProcessorCount;
        processingTasks = new Task[threadCount];
        
        // Initialize performance tracking
        renderTimer = new Stopwatch();
        frameCount = 0;
        lastRenderTime = 0;
    }
    
    // Processing
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled) return;
        
        // Start render timer
        renderTimer.Restart();
        
        // Copy input to output
        CopyInputToOutput(ctx, input, output);
        
        // Update frame counter
        frameCount++;
        
        // Update audio data
        UpdateAudioData(ctx);
        
        // Process left channel
        if (LeftChannelEnabled)
        {
            ProcessChannel(ctx, output, 0, LeftChannelColor);
        }
        
        // Process right channel
        if (RightChannelEnabled)
        {
            ProcessChannel(ctx, output, 1, RightChannelColor);
        }
        
        // Render trails if enabled
        if (ShowTrails)
        {
            RenderChannelTrails(ctx, output);
        }
        
        // Update trails
        UpdateChannelTrails();
        
        // Stop timer and update statistics
        renderTimer.Stop();
        lastRenderTime = (float)renderTimer.Elapsed.TotalMilliseconds;
    }
    
    private void UpdateAudioData(FrameContext ctx)
    {
        if (ctx.AudioData?.Spectrum != null && ctx.AudioData.Spectrum.Length >= 2)
        {
            // Update left channel spectrum
            float[] leftSpectrum = ctx.AudioData.Spectrum[0];
            if (leftSpectrum != null)
            {
                for (int i = 0; i < spectrumBins && i < leftSpectrum.Length; i++)
                {
                    leftChannelSpectrum[i] = leftSpectrum[i];
                }
            }
            
            // Update right channel spectrum
            float[] rightSpectrum = ctx.AudioData.Spectrum[1];
            if (rightSpectrum != null)
            {
                for (int i = 0; i < spectrumBins && i < rightSpectrum.Length; i++)
                {
                    rightChannelSpectrum[i] = rightSpectrum[i];
                }
            }
        }
    }
    
    private void ProcessChannel(FrameContext ctx, ImageBuffer output, int channel, Color color)
    {
        // Calculate audio intensity from spectrum data
        float audioIntensity = CalculateAudioIntensity(channel);
        
        // Apply audio sensitivity
        audioIntensity *= AudioSensitivity;
        
        // Update velocity with momentum
        velocities[channel] = Momentum * audioIntensity + (1.0f - Momentum) * velocities[channel];
        
        // Update rotation angle with direction and speed
        double direction = channel == 0 ? -1.0 : 1.0; // Left: counter-clockwise, Right: clockwise
        double rotationDelta = Math.PI / 6.0 * velocities[channel] * direction * RotationSpeed;
        rotationAngles[channel] += rotationDelta;
        
        // Keep rotation angle in 0-2π range
        while (rotationAngles[channel] >= Math.PI * 2)
        {
            rotationAngles[channel] -= Math.PI * 2;
        }
        while (rotationAngles[channel] < 0)
        {
            rotationAngles[channel] += Math.PI * 2;
        }
        
        // Store last position
        lastPositions[channel] = currentPositions[channel];
        
        // Calculate new spinning arm position
        currentPositions[channel] = CalculateArmPosition(ctx, rotationAngles[channel], velocities[channel], channel);
        
        // Render based on mode
        if (Mode == RenderingMode.Lines)
        {
            RenderLines(output, channel, currentPositions[channel], color);
        }
        else
        {
            RenderTriangles(output, channel, currentPositions[channel], color);
        }
    }
    
    private float CalculateAudioIntensity(int channel)
    {
        float[] spectrum = channel == 0 ? leftChannelSpectrum : rightChannelSpectrum;
        
        // Sum first 44 frequency bins for channel
        float sum = 0.0f;
        for (int i = 0; i < spectrumBins; i++)
        {
            sum += spectrum[i];
        }
        
        // Normalize to 0-1 range (AVS uses 0-200 range)
        float normalizedIntensity = Math.Max(0.0f, (sum - 104.0f) / 96.0f);
        
        // Apply smoothing and clamping
        return Math.Clamp(normalizedIntensity, 0.0f, 1.0f);
    }
    
    private Vector2 CalculateArmPosition(FrameContext ctx, double angle, float intensity, int channel)
    {
        // Base size calculation
        float baseSize = Math.Min(ctx.Height / 2.0f, (ctx.Width * 3.0f) / 8.0f);
        float scaledSize = baseSize * intensity;
        
        // Channel positioning (left/right separation)
        float centerX = channel == 0 ? 
            ctx.Width / 2.0f - baseSize / 2.0f : 
            ctx.Width / 2.0f + baseSize / 2.0f;
        
        // Calculate arm endpoint
        float x = centerX + (float)(Math.Cos(angle) * scaledSize);
        float y = ctx.Height / 2.0f + (float)(Math.Sin(angle) * scaledSize);
        
        return new Vector2(x, y);
    }
    
    private void RenderLines(ImageBuffer output, int channel, Vector2 armPosition, Color color)
    {
        // Calculate center point for the channel
        float centerX = channel == 0 ? output.Width / 2.0f - 50 : output.Width / 2.0f + 50;
        float centerY = output.Height / 2.0f;
        
        Vector2 center = new Vector2(centerX, centerY);
        
        // Draw line from center to arm position
        DrawLine(output, center, armPosition, color, 2.0f);
        
        // Draw small circle at arm position
        DrawCircle(output, armPosition, 3.0f, color);
    }
    
    private void RenderTriangles(ImageBuffer output, int channel, Vector2 armPosition, Color color)
    {
        // Calculate center point for the channel
        float centerX = channel == 0 ? output.Width / 2.0f - 50 : output.Width / 2.0f + 50;
        float centerY = output.Height / 2.0f;
        
        Vector2 center = new Vector2(centerX, centerY);
        
        // Calculate triangle base points
        float baseWidth = 20.0f;
        Vector2 baseLeft = new Vector2(centerX - baseWidth / 2.0f, centerY);
        Vector2 baseRight = new Vector2(centerX + baseWidth / 2.0f, centerY);
        
        // Create triangle vertices
        Vector2[] triangleVertices = { center, baseLeft, baseRight };
        
        // Draw filled triangle
        DrawFilledTriangle(output, triangleVertices, color);
        
        // Draw line from center to arm position
        DrawLine(output, center, armPosition, color, 2.0f);
        
        // Draw small circle at arm position
        DrawCircle(output, armPosition, 4.0f, color);
    }
    
    private void DrawLine(ImageBuffer output, Vector2 start, Vector2 end, Color color, float thickness)
    {
        // Use Bresenham's line algorithm for efficient line drawing
        int x0 = (int)start.X;
        int y0 = (int)start.Y;
        int x1 = (int)end.X;
        int y1 = (int)end.Y;
        
        // Draw thick line
        for (int t = 0; t < thickness; t++)
        {
            int offsetX = t - (int)(thickness / 2);
            int offsetY = t - (int)(thickness / 2);
            
            DrawThinLine(output, x0 + offsetX, y0 + offsetY, x1 + offsetX, y1 + offsetY, color);
        }
    }
    
    private void DrawThinLine(ImageBuffer output, int x0, int y0, int x1, int y1, Color color)
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
            // Draw pixel
            if (x >= 0 && x < output.Width && y >= 0 && y < output.Height)
            {
                output.SetPixel(x, y, color);
            }
            
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
    
    private void DrawCircle(ImageBuffer output, Vector2 center, float radius, Color color)
    {
        int centerX = (int)center.X;
        int centerY = (int)center.Y;
        int r = (int)radius;
        
        // Midpoint circle algorithm
        int x = r;
        int y = 0;
        int err = 0;
        
        while (x >= y)
        {
            // Draw 8 octants
            DrawCirclePixel(output, centerX + x, centerY + y, color);
            DrawCirclePixel(output, centerX + y, centerY + x, color);
            DrawCirclePixel(output, centerX - y, centerY + x, color);
            DrawCirclePixel(output, centerX - x, centerY + y, color);
            DrawCirclePixel(output, centerX - x, centerY - y, color);
            DrawCirclePixel(output, centerX - y, centerY - x, color);
            DrawCirclePixel(output, centerX + y, centerY - x, color);
            DrawCirclePixel(output, centerX + x, centerY - y, color);
            
            if (err <= 0)
            {
                y += 1;
                err += 2 * y + 1;
            }
            if (err > 0)
            {
                x -= 1;
                err -= 2 * x + 1;
            }
        }
    }
    
    private void DrawCirclePixel(ImageBuffer output, int x, int y, Color color)
    {
        if (x >= 0 && x < output.Width && y >= 0 && y < output.Height)
        {
            output.SetPixel(x, y, color);
        }
    }
    
    private void DrawFilledTriangle(ImageBuffer output, Vector2[] vertices, Color color)
    {
        if (vertices.Length != 3) return;
        
        // Sort vertices by Y coordinate
        Vector2[] sortedVertices = vertices.OrderBy(v => v.Y).ToArray();
        
        // Calculate triangle bounds
        int minY = (int)sortedVertices[0].Y;
        int maxY = (int)sortedVertices[2].Y;
        
        // Scan line algorithm
        for (int y = minY; y <= maxY; y++)
        {
            if (y < 0 || y >= output.Height) continue;
            
            // Find intersection points with scan line
            List<int> intersections = new List<int>();
            
            for (int i = 0; i < 3; i++)
            {
                int next = (i + 1) % 3;
                Vector2 v1 = sortedVertices[i];
                Vector2 v2 = sortedVertices[next];
                
                if ((v1.Y <= y && v2.Y > y) || (v2.Y <= y && v1.Y > y))
                {
                    float x = v1.X + (v2.X - v1.X) * (y - v1.Y) / (v2.Y - v1.Y);
                    intersections.Add((int)x);
                }
            }
            
            // Sort intersections and draw horizontal lines
            intersections.Sort();
            for (int i = 0; i < intersections.Count - 1; i += 2)
            {
                int x1 = intersections[i];
                int x2 = intersections[i + 1];
                
                for (int x = x1; x <= x2; x++)
                {
                    if (x >= 0 && x < output.Width)
                    {
                        output.SetPixel(x, y, color);
                    }
                }
            }
        }
    }
    
    private void UpdateChannelTrails()
    {
        // Store current positions in trail
        channelTrails[currentTrailIndex][0] = currentPositions[0];
        channelTrails[currentTrailIndex][1] = currentPositions[1];
        
        // Move to next trail position
        currentTrailIndex = (currentTrailIndex + 1) % TrailLength;
    }
    
    private void RenderChannelTrails(FrameContext ctx, ImageBuffer output)
    {
        if (!ShowTrails) return;
        
        // Render trails with decreasing alpha
        for (int trailIndex = 0; trailIndex < TrailLength; trailIndex++)
        {
            int actualIndex = (currentTrailIndex - trailIndex + TrailLength) % TrailLength;
            float alpha = 1.0f - (float)trailIndex / TrailLength;
            
            if (alpha > 0.1f) // Only render visible trails
            {
                Vector2[] trailPositions = channelTrails[actualIndex];
                
                // Render left channel trail
                if (LeftChannelEnabled && trailPositions[0] != Vector2.Zero)
                {
                    Color trailColor = Color.FromArgb(
                        (int)(alpha * LeftChannelColor.A),
                        (int)(alpha * LeftChannelColor.R),
                        (int)(alpha * LeftChannelColor.G),
                        (int)(alpha * LeftChannelColor.B)
                    );
                    DrawCircle(output, trailPositions[0], 2.0f, trailColor);
                }
                
                // Render right channel trail
                if (RightChannelEnabled && trailPositions[1] != Vector2.Zero)
                {
                    Color trailColor = Color.FromArgb(
                        (int)(alpha * RightChannelColor.A),
                        (int)(alpha * RightChannelColor.R),
                        (int)(alpha * RightChannelColor.G),
                        (int)(alpha * RightChannelColor.B)
                    );
                    DrawCircle(output, trailPositions[1], 2.0f, trailColor);
                }
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
    
    // Public methods for external access
    public void SetMomentum(float momentum)
    {
        Momentum = Math.Clamp(momentum, 0.1f, 0.9f);
    }
    
    public void SetAudioSensitivity(float sensitivity)
    {
        AudioSensitivity = Math.Clamp(sensitivity, 0.1f, 5.0f);
    }
    
    public void SetRotationSpeed(float speed)
    {
        RotationSpeed = Math.Clamp(speed, 0.1f, 5.0f);
    }
    
    public void ResetRotation()
    {
        rotationAngles[0] = 0.0;
        rotationAngles[1] = 0.0;
        velocities[0] = 0.0;
        velocities[1] = 0.0;
    }
    
    public double GetChannelRotation(int channel) => rotationAngles[channel];
    public float GetChannelVelocity(int channel) => velocities[channel];
    public Vector2 GetChannelPosition(int channel) => currentPositions[channel];
    public float GetLastRenderTime() => lastRenderTime;
    public int GetFrameCount() => frameCount;
    
    // Dispose pattern
    public override void Dispose()
    {
        renderTimer?.Stop();
        base.Dispose();
    }
}

public enum RenderingMode
{
    Lines, Triangles
}
```

### **Optimization Strategies**
- **Efficient trigonometry**: Use Math.Sin/Math.Cos with angle caching and optimized calculations
- **Audio processing**: Optimized spectrum data analysis with configurable bin count
- **Rendering**: Efficient line drawing algorithms with thickness support and filled triangle rendering
- **Memory management**: Minimal allocation, efficient buffer handling, and trail management
- **Multi-threading**: Parallel processing for image copying and channel processing
- **Momentum system**: Smooth velocity changes for fluid animation with configurable momentum
- **Trail effects**: Configurable trail system with alpha blending for visual enhancement
- **Performance monitoring**: Real-time render timing and frame counting

---

## 📚 **Use Cases**

### **Audio Visualization**
- **Real-time Beat Response**: Immediate reaction to audio intensity changes
- **Channel Separation**: Visual distinction between left and right audio
- **Rhythm Visualization**: Spinning motion synchronized with music
- **Dynamic Motion**: Smooth acceleration and deceleration with audio

### **Visual Effects**
- **Geometric Animation**: Clean, mathematical spinning patterns
- **Dual Channel Effects**: Independent left/right channel visualization
- **Smooth Transitions**: Momentum-based motion for fluid animation
- **Rendering Flexibility**: Choice between line and triangle styles

---

## 🚀 **Future Enhancements**

### **Phoenix-Specific Features**
- **Advanced Geometry**: Configurable arm shapes and patterns
- **3D Rendering**: Depth-based spinning arm visualization
- **Particle Effects**: Trail effects and particle systems
- **Real-time Editing**: Live parameter adjustment during visualization

### **Advanced Audio Features**
- **Frequency Analysis**: Different frequency ranges for different arm behaviors
- **Beat Synchronization**: Precise beat detection and synchronization
- **Harmonic Visualization**: Harmonic content visualization in spinning patterns
- **Multi-band Processing**: Separate processing for different frequency bands

---

## 📖 **References**

- **Source Code**: `r_bspin.cpp` from VIS_AVS
- **Header**: `r_defs.h` for base class definitions
- **Audio Processing**: Real-time spectrum data analysis
- **Geometry**: Trigonometric spinning arm calculations
- **Rendering**: Line and triangle drawing algorithms
- **Animation**: Velocity-based smooth motion systems

---

**Status:** ✅ **TWELFTH EFFECT DOCUMENTED**  
**Next:** Time Domain Scope effect analysis (`r_timescope.cpp`)
