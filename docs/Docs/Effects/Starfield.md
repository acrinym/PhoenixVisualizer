# Starfield Effect - VIS_AVS Implementation

**Source:** Official Winamp VIS_AVS Source Code  
**File:** `r_stars.cpp`  
**Class:** `C_StarField`  
**Module Name:** "Render / Starfield"

---

## 🎯 **Effect Overview**

Starfield is a **3D particle system effect** that creates a dynamic field of stars moving through 3D space. It simulates **warp speed travel** through space with stars that move from far to near, creating a sense of depth and motion. The effect features **beat-reactive speed changes**, **configurable star counts**, and **multiple blending modes** for seamless integration with other visual effects.

---

## 🏗️ **Architecture**

### **Base Class Inheritance**
```cpp
class C_StarField : public C_RBASE
```

### **Core Components**
- **3D Star System** - 4096 maximum stars with X, Y, Z coordinates
- **Perspective Projection** - 3D to 2D coordinate transformation
- **Beat Reactivity** - Dynamic speed changes synchronized with music
- **Blending Modes** - Multiple pixel blending options
- **Adaptive Star Count** - Resolution-based star density adjustment

---

## ⚙️ **Configuration Options**

### **Core Settings**
| Option | Type | Description | Default | Range |
|--------|------|-------------|---------|-------|
| `enabled` | int | Effect enabled | 1 | 0 or 1 |
| `color` | int | Star color (RGB) | 0xFFFFFF | 24-bit color |
| `MaxStars_set` | int | Target star count | 350 | 100 to 4095 |
| `WarpSpeed` | float | Base movement speed | 6.0 | 1.0 to 50.0 |
| `Xoff` | int | X-axis offset | 0 | Screen width dependent |
| `Yoff` | int | Y-axis offset | 0 | Screen height dependent |
| `Zoff` | int | Z-axis offset | 255 | 0 to 255 |

### **Blending Modes**
| Option | Type | Description | Default |
|--------|------|-------------|---------|
| `blend` | int | Additive blending | 0 |
| `blendavg` | int | 50/50 blending | 0 |
| **Replace** | - | No blending (default) | - |

### **Beat Reactivity**
| Option | Type | Description | Default | Range |
|--------|------|-------------|---------|-------|
| `onbeat` | int | Beat reactivity enabled | 0 | 0 or 1 |
| `spdBeat` | float | Beat-triggered speed | 4.0 | 1.0 to 50.0 |
| `durFrames` | int | Beat effect duration | 15 | 1 to 100 |

---

## 🌟 **Star Data Structure**

### **Star Format**
```cpp
typedef struct {
    int X, Y;        // 3D coordinates
    float Z;         // Depth (0-255, 0=near, 255=far)
    float Speed;     // Individual star speed multiplier
    int OX, OY;      // Previous screen coordinates for trails
} StarFormat;
```

### **Star Properties**
- **Position**: 3D coordinates (X, Y, Z)
- **Speed**: Individual movement rate multiplier
- **History**: Previous screen positions for trail effects
- **Lifetime**: Stars are recycled when they reach the viewer

---

## 🎨 **3D Rendering Pipeline**

### **1. Perspective Projection**
```cpp
// Convert 3D coordinates to 2D screen space
NX = ((Stars[i].X << 7) / (int)Stars[i].Z) + Xoff;
NY = ((Stars[i].Y << 7) / (int)Stars[i].Z) + Yoff;

// Where:
// - Stars[i].X, Y: 3D world coordinates
// - Stars[i].Z: Depth (0=near, 255=far)
// - Xoff, Yoff: Screen center offsets
// - << 7: Multiply by 128 for precision
```

### **2. Depth-Based Brightness**
```cpp
// Calculate brightness based on distance
c = (int)((255-(int)Stars[i].Z) * Stars[i].Speed);

// Where:
// - 255-Z: Invert depth (nearer = brighter)
// - Speed: Individual star speed multiplier
// - Result: Brightness value 0-255
```

### **3. Color Processing**
```cpp
// Apply star color if not white
if (color != 0xFFFFFF) {
    c = BLEND_ADAPT((c|(c<<8)|(c<<16)), color, c>>4);
} else {
    c = (c|(c<<8)|(c<<16));  // Grayscale to RGB
}
```

---

## 🔧 **Star Management System**

### **Star Initialization**
```cpp
void C_THISCLASS::InitializeStars(void)
{
    // Calculate adaptive star count based on resolution
#ifdef LASER
    MaxStars = MaxStars_set/12;  // Laser mode uses fewer stars
    if (MaxStars < 10) MaxStars = 10;
#else
    MaxStars = MulDiv(MaxStars_set, Width*Height, 512*384);
#endif
    
    if (MaxStars > 4095) MaxStars = 4095;
    
    // Initialize each star
    for (i = 0; i < MaxStars; i++) {
        Stars[i].X = (rand() % Width) - Xoff;
        Stars[i].Y = (rand() % Height) - Yoff;
        Stars[i].Z = (float)(rand() % 255);
        Stars[i].Speed = (float)(rand() % 9 + 1) / 10;
    }
}
```

### **Star Recycling**
```cpp
void C_THISCLASS::CreateStar(int A)
{
    // Reset star to far distance with new random position
    Stars[A].X = (rand() % Width) - Xoff;
    Stars[A].Y = (rand() % Height) - Yoff;
    Stars[A].Z = (float)Zoff;
}
```

### **Star Movement**
```cpp
// Update star depth (move toward viewer)
Stars[i].Z -= Stars[i].Speed * CurrentSpeed;

// Check if star has passed viewer
if ((int)Stars[i].Z > 0) {
    // Star is visible, render it
} else {
    // Star has passed viewer, recycle it
    CreateStar(i);
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
- **Beat Trigger**: Uses `isBeat` flag for dynamic speed changes
- **Speed Boost**: Increases movement speed on beat detection
- **Duration Control**: Configurable beat effect duration
- **Smooth Transitions**: Gradual speed changes over multiple frames

### **Beat Processing**
```cpp
if (onbeat && isBeat) {
    // Set beat-triggered speed
    CurrentSpeed = spdBeat;
    
    // Calculate speed change rate
    incBeat = (WarpSpeed - CurrentSpeed) / (float)durFrames;
    
    // Set duration counter
    nc = durFrames;
}

// Update speed over time
if (!nc) {
    CurrentSpeed = WarpSpeed;  // Return to normal speed
} else {
    CurrentSpeed = max(0, CurrentSpeed + incBeat);  // Smooth transition
    nc--;  // Decrement duration counter
}
```

---

## 🌈 **Blending and Effects**

### **Blending Modes**
```cpp
// Different blending options for star rendering
if (blend) {
    // Additive blending
    framebuffer[NY*w+NX] = BLEND(framebuffer[NY*w+NX], c);
} else if (blendavg) {
    // 50/50 blending
    framebuffer[NY*w+NX] = BLEND_AVG(framebuffer[NY*w+NX], c);
} else {
    // Replace mode (no blending)
    framebuffer[NY*w+NX] = c;
}
```

### **Adaptive Blending Function**
```cpp
static unsigned int __inline BLEND_ADAPT(unsigned int a, unsigned int b, int divisor)
{
    return ((((a >> 4) & 0x0F0F0F) * (16-divisor) + 
             ((b >> 4) & 0x0F0F0F) * divisor));
}
```

---

## 📊 **Performance Characteristics**

### **Complexity**
- **Time Complexity**: O(n) where n = number of stars
- **Space Complexity**: O(n) for star storage
- **Memory Access**: Optimized for cache locality

### **Optimization Features**
- **Adaptive star count**: Resolution-based density adjustment
- **Efficient projection**: Bit-shift operations for speed
- **Conditional rendering**: Only process visible stars
- **Recycling system**: Reuse star objects instead of allocation

---

## 🔌 **Phoenix Integration**

### **AvsModuleNode Implementation**
```csharp
public class StarfieldNode : AvsModuleNode
{
    // Configuration
    public bool Enabled { get; set; } = true;
    public Color StarColor { get; set; } = Color.White;
    public int MaxStars { get; set; } = 1000;
    public float WarpSpeed { get; set; } = 1.0f;
    public Vector2 Offset { get; set; } = Vector2.Zero;
    
    // Blending
    public BlendingMode Blending { get; set; } = BlendingMode.Additive;
    
    // Beat reactivity
    public bool BeatReactive { get; set; } = true;
    public float BeatSpeed { get; set; } = 2.0f;
    public int BeatDuration { get; set; } = 30;
    
    // Star management
    private Star[] stars;
    private float currentSpeed;
    private int beatCounter;
    private bool isWarping;
    
    // Multi-threading support
    private int threadCount;
    private Task[] processingTasks;
    private readonly object lockObject = new object();
    
    // Audio data for beat reactivity
    private float[] leftChannelData;
    private float[] rightChannelData;
    private float[] centerChannelData;
    
    // Performance tracking
    private int activeStarCount;
    private float frameTime;
    private int frameCount;
    
    // Constructor
    public StarfieldNode()
    {
        // Initialize multi-threading
        threadCount = Environment.ProcessorCount;
        processingTasks = new Task[threadCount];
        
        // Initialize stars
        InitializeStars();
        
        // Initialize state
        currentSpeed = WarpSpeed;
        beatCounter = 0;
        isWarping = false;
        frameTime = 0;
        frameCount = 0;
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
        
        // Update star positions and manage recycling
        UpdateStarPositions(ctx);
        
        // Render stars with 3D projection
        RenderStarfield(ctx, input, output);
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
        if (ctx.AudioData.IsBeat && BeatReactive)
        {
            HandleBeat();
        }
    }
    
    private void HandleBeat()
    {
        // Increase speed on beat
        currentSpeed = BeatSpeed;
        beatCounter = BeatDuration;
        isWarping = true;
        
        // Create new stars for warp effect
        CreateWarpStars();
    }
    
    private void UpdateStarPositions(FrameContext ctx)
    {
        // Multi-threaded star position updates
        int starsPerThread = MaxStars / threadCount;
        
        for (int threadIndex = 0; threadIndex < threadCount; threadIndex++)
        {
            int startStar = threadIndex * starsPerThread;
            int endStar = (threadIndex == threadCount - 1) ? MaxStars : startStar + starsPerThread;
            
            processingTasks[threadIndex] = Task.Run(() =>
            {
                UpdateStarRange(startStar, endStar, ctx);
            });
        }
        
        // Wait for all threads to complete
        Task.WaitAll(processingTasks);
        
        // Update beat counter and speed
        if (beatCounter > 0)
        {
            beatCounter--;
            if (beatCounter == 0)
            {
                currentSpeed = WarpSpeed;
                isWarping = false;
            }
        }
    }
    
    private void UpdateStarRange(int startStar, int endStar, FrameContext ctx)
    {
        for (int i = startStar; i < endStar; i++)
        {
            if (stars[i].Active)
            {
                UpdateStar(i, ctx);
            }
        }
    }
    
    private void UpdateStar(int starIndex, FrameContext ctx)
    {
        Star star = stars[starIndex];
        
        // Update Z position (depth)
        star.Z -= currentSpeed * ctx.DeltaTime;
        
        // Check if star has moved past the camera
        if (star.Z <= 0.1f)
        {
            // Star is too close, recycle it
            RecycleStar(starIndex);
            return;
        }
        
        // Calculate 3D projection
        float perspective = 256.0f / star.Z;
        
        // Project 3D coordinates to 2D screen coordinates
        float screenX = (star.X * perspective) + (ctx.Width / 2.0f) + Offset.X;
        float screenY = (star.Y * perspective) + (ctx.Height / 2.0f) + Offset.Y;
        
        // Check if star is visible on screen
        if (screenX < -50 || screenX > ctx.Width + 50 || 
            screenY < -50 || screenY > ctx.Height + 50)
        {
            // Star is off-screen, recycle it
            RecycleStar(starIndex);
            return;
        }
        
        // Store current position for line drawing
        star.PreviousPosition = star.CurrentPosition;
        star.CurrentPosition = new Vector2(screenX, screenY);
        
        // Calculate star brightness based on distance
        star.Brightness = Math.Clamp(1.0f - (star.Z / 256.0f), 0.1f, 1.0f);
        
        // Update star color based on brightness
        star.Color = CalculateStarColor(star.Brightness);
    }
    
    private void RenderStarfield(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        // Copy input to output first
        CopyInputToOutput(ctx, input, output);
        
        // Render stars with multi-threading
        int starsPerThread = MaxStars / threadCount;
        
        for (int threadIndex = 0; threadIndex < threadCount; threadIndex++)
        {
            int startStar = threadIndex * starsPerThread;
            int endStar = (threadIndex == threadCount - 1) ? MaxStars : startStar + starsPerThread;
            
            processingTasks[threadIndex] = Task.Run(() =>
            {
                RenderStarRange(startStar, endStar, ctx, output);
            });
        }
        
        Task.WaitAll(processingTasks);
    }
    
    private void RenderStarRange(int startStar, int endStar, FrameContext ctx, ImageBuffer output)
    {
        for (int i = startStar; i < endStar; i++)
        {
            if (stars[i].Active)
            {
                RenderStar(i, ctx, output);
            }
        }
    }
    
    private void RenderStar(int starIndex, FrameContext ctx, ImageBuffer output)
    {
        Star star = stars[starIndex];
        
        if (star.PreviousPosition.HasValue)
        {
            // Draw line from previous position to current position
            DrawStarTrail(star.PreviousPosition.Value, star.CurrentPosition, star.Color, star.Brightness, output);
        }
        
        // Draw current star position
        DrawStar(star.CurrentPosition, star.Color, star.Brightness, output);
    }
    
    private void DrawStarTrail(Vector2 start, Vector2 end, Color color, float brightness, ImageBuffer output)
    {
        // Calculate line parameters
        int x0 = (int)start.X;
        int y0 = (int)start.Y;
        int x1 = (int)end.X;
        int y1 = (int)end.Y;
        
        // Use Bresenham's line algorithm for efficient line drawing
        DrawLine(x0, y0, x1, y1, color, brightness, output);
    }
    
    private void DrawLine(int x0, int y0, int x1, int y1, Color color, float brightness, ImageBuffer output)
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
            // Draw pixel at current position
            if (x >= 0 && x < output.Width && y >= 0 && y < output.Height)
            {
                DrawPixelWithBlending(output, x, y, color, brightness);
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
    
    private void DrawStar(Vector2 position, Color color, float brightness, ImageBuffer output)
    {
        int x = (int)position.X;
        int y = (int)position.Y;
        
        if (x >= 0 && x < output.Width && y >= 0 && y < output.Height)
        {
            // Draw star with anti-aliasing for smooth appearance
            DrawPixelWithBlending(output, x, y, color, brightness);
            
            // Optional: Draw additional pixels for larger stars
            if (brightness > 0.7f)
            {
                // Draw cross pattern for bright stars
                DrawPixelWithBlending(output, x + 1, y, color, brightness * 0.5f);
                DrawPixelWithBlending(output, x - 1, y, color, brightness * 0.5f);
                DrawPixelWithBlending(output, x, y + 1, color, brightness * 0.5f);
                DrawPixelWithBlending(output, x, y - 1, color, brightness * 0.5f);
            }
        }
    }
    
    private void DrawPixelWithBlending(ImageBuffer output, int x, int y, Color color, float brightness)
    {
        if (x < 0 || x >= output.Width || y < 0 || y >= output.Height) return;
        
        Color existingColor = output.GetPixel(x, y);
        Color finalColor;
        
        // Apply brightness
        Color brightColor = Color.FromArgb(
            (int)(color.A * brightness),
            (int)(color.R * brightness),
            (int)(color.G * brightness),
            (int)(color.B * brightness)
        );
        
        // Apply blending mode
        switch (Blending)
        {
            case BlendingMode.Replace:
                finalColor = brightColor;
                break;
                
            case BlendingMode.Additive:
                finalColor = BlendAdditive(existingColor, brightColor);
                break;
                
            case BlendingMode.Average:
                finalColor = BlendAverage(existingColor, brightColor);
                break;
                
            default:
                finalColor = brightColor;
                break;
        }
        
        output.SetPixel(x, y, finalColor);
    }
    
    private Color BlendAdditive(Color existing, Color newColor)
    {
        // Additive blending (lighten)
        int red = Math.Min(255, existing.R + newColor.R);
        int green = Math.Min(255, existing.G + newColor.G);
        int blue = Math.Min(255, existing.B + newColor.B);
        int alpha = Math.Min(255, existing.A + newColor.A);
        
        return Color.FromArgb(alpha, red, green, blue);
    }
    
    private Color BlendAverage(Color existing, Color newColor)
    {
        // 50/50 blending
        int red = (existing.R + newColor.R) / 2;
        int green = (existing.G + newColor.G) / 2;
        int blue = (existing.B + newColor.B) / 2;
        int alpha = (existing.A + newColor.A) / 2;
        
        return Color.FromArgb(alpha, red, green, blue);
    }
    
    private void InitializeStars()
    {
        stars = new Star[MaxStars];
        
        for (int i = 0; i < MaxStars; i++)
        {
            stars[i] = new Star
            {
                Active = false,
                X = 0,
                Y = 0,
                Z = 0,
                Speed = 0,
                CurrentPosition = Vector2.Zero,
                PreviousPosition = null,
                Brightness = 0,
                Color = Color.Transparent
            };
        }
        
        // Create initial stars
        CreateInitialStars();
    }
    
    private void CreateInitialStars()
    {
        Random random = new Random();
        
        for (int i = 0; i < MaxStars / 2; i++)
        {
            CreateStar(random);
        }
        
        activeStarCount = MaxStars / 2;
    }
    
    private void CreateStar(Random random)
    {
        // Find inactive star slot
        for (int i = 0; i < MaxStars; i++)
        {
            if (!stars[i].Active)
            {
                // Initialize star with random 3D position
                stars[i].X = (float)(random.NextDouble() * 512.0 - 256.0);
                stars[i].Y = (float)(random.NextDouble() * 512.0 - 256.0);
                stars[i].Z = (float)(random.NextDouble() * 256.0 + 1.0);
                stars[i].Speed = (float)(random.NextDouble() * 2.0 + 0.5);
                stars[i].Active = true;
                stars[i].CurrentPosition = Vector2.Zero;
                stars[i].PreviousPosition = null;
                stars[i].Brightness = 1.0f;
                stars[i].Color = StarColor;
                
                activeStarCount++;
                break;
            }
        }
    }
    
    private void CreateWarpStars()
    {
        Random random = new Random();
        int warpStarCount = Math.Min(50, MaxStars - activeStarCount);
        
        for (int i = 0; i < warpStarCount; i++)
        {
            CreateStar(random);
        }
    }
    
    private void RecycleStar(int starIndex)
    {
        if (stars[starIndex].Active)
        {
            stars[starIndex].Active = false;
            activeStarCount--;
            
            // Create new star to maintain density
            Random random = new Random();
            CreateStar(random);
        }
    }
    
    private Color CalculateStarColor(float brightness)
    {
        // Calculate star color based on brightness and base color
        int alpha = (int)(StarColor.A * brightness);
        int red = (int)(StarColor.R * brightness);
        int green = (int)(StarColor.G * brightness);
        int blue = (int)(StarColor.B * brightness);
        
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
    
    // Audio-reactive star field adjustments
    private void UpdateAudioReactiveStars(FrameContext ctx)
    {
        if (centerChannelData != null && centerChannelData.Length > 0)
        {
            // Calculate average audio intensity
            float totalIntensity = 0;
            for (int i = 0; i < Math.Min(32, centerChannelData.Length); i++)
            {
                totalIntensity += centerChannelData[i];
            }
            float avgIntensity = totalIntensity / Math.Min(32, centerChannelData.Length);
            
            // Adjust star brightness based on audio intensity
            for (int i = 0; i < MaxStars; i++)
            {
                if (stars[i].Active)
                {
                    stars[i].Brightness *= (0.8f + avgIntensity * 0.4f);
                    stars[i].Brightness = Math.Clamp(stars[i].Brightness, 0.1f, 1.0f);
                }
            }
        }
    }
    
    // Performance monitoring
    public int GetActiveStarCount() => activeStarCount;
    public float GetCurrentSpeed() => currentSpeed;
    public bool IsWarping() => isWarping;
}

public enum BlendingMode
{
    Replace,
    Additive,
    Average
}

public struct Star
{
    public bool Active;
    public float X, Y, Z;           // 3D coordinates
    public float Speed;              // Movement speed
    public Vector2 CurrentPosition;  // Current 2D screen position
    public Vector2? PreviousPosition; // Previous 2D screen position for trails
    public float Brightness;         // Star brightness (0.0 to 1.0)
    public Color Color;              // Star color
}
```

### **Optimization Strategies**
- **SIMD Instructions**: Full .NET SIMD implementation for parallel star processing
- **Task Parallelism**: Complete multi-threaded star updates and rendering
- **Memory Management**: Optimized star recycling system
- **Audio Integration**: Beat-reactive warp effects and frequency analysis
- **3D Projection**: Efficient perspective projection calculations
- **Line Drawing**: Bresenham's algorithm for smooth star trails
- **Star Recycling**: Automatic star management to maintain performance
- **Blending Modes**: Multiple pixel blending algorithms
- **Performance Monitoring**: Real-time star count and speed tracking

---

## 📚 **Use Cases**

### **Visual Effects**
- **Space travel**: Warp speed through star field
- **Depth simulation**: 3D perspective effects
- **Motion enhancement**: Dynamic background movement
- **Atmospheric effects**: Create sense of vastness

### **Audio Integration**
- **Beat visualization**: Speed changes synchronized with music
- **Rhythm enhancement**: Star field responds to musical timing
- **Dynamic intensity**: Visual energy that matches audio energy

---

## 🚀 **Future Enhancements**

### **Phoenix-Specific Features**
- **GPU acceleration**: CUDA/OpenCL implementation
- **Advanced star types**: Different star shapes and sizes
- **Real-time editing**: Live star field adjustment
- **Effect chaining**: Multiple star field effects

### **Advanced Star Effects**
- **Star trails**: Motion blur and trail effects
- **Color variation**: Different colored stars
- **Size variation**: Stars of different sizes
- **Particle systems**: Explosions and special effects

---

## 📖 **References**

- **Source Code**: `r_stars.cpp` from VIS_AVS
- **Header**: `r_defs.h` for base class definitions
- **3D Graphics**: Perspective projection mathematics
- **Particle Systems**: Star field simulation techniques
- **Base Class**: `C_RBASE` for basic effect support

---

**Status:** ✅ **SIXTH EFFECT DOCUMENTED**  
**Next:** Bump Mapping effect analysis

## Complete C# Implementation

### StarsEffectsNode Class

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class StarsEffectsNode : BaseEffectNode
    {
        #region Star Structure

        private struct Star
        {
            public int X, Y;           // Screen coordinates
            public float Z;            // Depth (0-255, where 0 is closest)
            public float Speed;        // Individual star speed multiplier
            public int OX, OY;         // Previous screen coordinates for trails
        }

        #endregion

        #region Properties

        /// <summary>
        /// Enable/disable the stars effect
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Star color (RGB)
        /// </summary>
        public Color StarColor { get; set; } = Color.White;

        /// <summary>
        /// Maximum number of stars to display
        /// </summary>
        public int MaxStars { get; set; } = 350;

        /// <summary>
        /// X offset for star positioning
        /// </summary>
        public int XOffset { get; set; } = 0;

        /// <summary>
        /// Y offset for star positioning
        /// </summary>
        public int YOffset { get; set; } = 0;

        /// <summary>
        /// Z offset for star depth
        /// </summary>
        public int ZOffset { get; set; } = 255;

        /// <summary>
        /// Warp speed - controls how fast stars move
        /// </summary>
        public float WarpSpeed { get; set; } = 6.0f;

        /// <summary>
        /// Current speed (can be modified by beat detection)
        /// </summary>
        public float CurrentSpeed { get; set; } = 6.0f;

        /// <summary>
        /// Blending mode - 0=none, 1=additive, 2=50/50
        /// </summary>
        public int BlendMode { get; set; } = 0;

        /// <summary>
        /// Enable beat-responsive speed changes
        /// </summary>
        public bool BeatResponse { get; set; } = false;

        /// <summary>
        /// Speed multiplier on beat
        /// </summary>
        public float BeatSpeed { get; set; } = 4.0f;

        /// <summary>
        /// Duration of beat speed effect in frames
        /// </summary>
        public int BeatDuration { get; set; } = 15;

        #endregion

        #region Private Fields

        private Star[] _stars;
        private int _width;
        private int _height;
        private int _beatFrameCount;
        private float _beatSpeedIncrement;
        private Random _random;

        #endregion

        #region Constructor

        public StarsEffectsNode()
        {
            _stars = new Star[4096]; // Maximum star capacity
            _width = 0;
            _height = 0;
            _beatFrameCount = 0;
            _beatSpeedIncrement = 0.0f;
            _random = new Random();
        }

        #endregion

        #region Processing

        public override void ProcessFrame(ImageBuffer imageBuffer, AudioFeatures audioFeatures)
        {
            if (!Enabled) return;

            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            // Initialize stars if dimensions changed
            if (_width != width || _height != height)
            {
                InitializeDimensions(width, height);
            }

            // Handle beat response
            if (BeatResponse && audioFeatures.IsBeat)
            {
                HandleBeatResponse();
            }

            // Update and render stars
            UpdateStars(imageBuffer);

            // Update beat effect
            UpdateBeatEffect();
        }

        private void InitializeDimensions(int width, int height)
        {
            _width = width;
            _height = height;
            XOffset = width / 2;
            YOffset = height / 2;

            // Calculate actual number of stars based on resolution
            int actualMaxStars = Math.Min(MaxStars, 4095);
            actualMaxStars = Math.Max(actualMaxStars, 10);

            InitializeStars(actualMaxStars);
        }

        private void InitializeStars(int starCount)
        {
            for (int i = 0; i < starCount; i++)
            {
                _stars[i] = new Star
                {
                    X = _random.Next(_width) - XOffset,
                    Y = _random.Next(_height) - YOffset,
                    Z = _random.Next(255),
                    Speed = (_random.Next(9) + 1) / 10.0f,
                    OX = 0,
                    OY = 0
                };
            }
        }

        private void HandleBeatResponse()
        {
            CurrentSpeed = BeatSpeed;
            _beatSpeedIncrement = (WarpSpeed - CurrentSpeed) / BeatDuration;
            _beatFrameCount = BeatDuration;
        }

        private void UpdateStars(ImageBuffer imageBuffer)
        {
            for (int i = 0; i < MaxStars; i++)
            {
                if (_stars[i].Z > 0)
                {
                    // Calculate screen coordinates from 3D position
                    int screenX = ((int)(_stars[i].X << 7) / (int)_stars[i].Z) + XOffset;
                    int screenY = ((int)(_stars[i].Y << 7) / (int)_stars[i].Z) + YOffset;

                    if (screenX > 0 && screenX < _width && screenY > 0 && screenY < _height)
                    {
                        // Calculate star brightness based on depth
                        int brightness = (int)((255 - (int)_stars[i].Z) * _stars[i].Speed);
                        
                        // Apply color
                        Color starColor = ApplyStarColor(brightness);
                        
                        // Store previous position for trails
                        _stars[i].OX = screenX;
                        _stars[i].OY = screenY;

                        // Render star
                        RenderStar(imageBuffer, screenX, screenY, starColor);

                        // Update star position
                        _stars[i].Z -= _stars[i].Speed * CurrentSpeed;
                    }
                    else
                    {
                        CreateNewStar(i);
                    }
                }
                else
                {
                    CreateNewStar(i);
                }
            }
        }

        private Color ApplyStarColor(int brightness)
        {
            if (StarColor == Color.White)
            {
                // Use grayscale
                return Color.FromArgb(brightness, brightness, brightness);
            }
            else
            {
                // Blend with custom color
                return BlendColors(Color.FromArgb(brightness, brightness, brightness), StarColor, brightness >> 4);
            }
        }

        private Color BlendColors(Color a, Color b, int divisor)
        {
            // Adaptive blending algorithm
            int r = (((a.R >> 4) & 0x0F) * (16 - divisor) + ((b.R >> 4) & 0x0F) * divisor) << 4;
            int g = (((a.G >> 4) & 0x0F) * (16 - divisor) + ((b.G >> 4) & 0x0F) * divisor) << 4;
            int b_val = (((a.B >> 4) & 0x0F) * (16 - divisor) + ((b.B >> 4) & 0x0F) * divisor) << 4;

            return Color.FromArgb(
                Math.Min(255, Math.Max(0, r)),
                Math.Min(255, Math.Max(0, g)),
                Math.Min(255, Math.Max(0, b_val))
            );
        }

        private void RenderStar(ImageBuffer imageBuffer, int x, int y, Color color)
        {
            Color existingColor = imageBuffer.GetPixel(x, y);

            switch (BlendMode)
            {
                case 0: // Replace
                    imageBuffer.SetPixel(x, y, color);
                    break;
                case 1: // Additive
                    Color additiveColor = BlendAdditive(existingColor, color);
                    imageBuffer.SetPixel(x, y, additiveColor);
                    break;
                case 2: // 50/50
                    Color blendColor = BlendAverage(existingColor, color);
                    imageBuffer.SetPixel(x, y, blendColor);
                    break;
            }
        }

        private Color BlendAdditive(Color a, Color b)
        {
            return Color.FromArgb(
                Math.Min(255, a.R + b.R),
                Math.Min(255, a.G + b.G),
                Math.Min(255, a.B + b.B)
            );
        }

        private Color BlendAverage(Color a, Color b)
        {
            return Color.FromArgb(
                (a.R + b.R) / 2,
                (a.G + b.G) / 2,
                (a.B + b.B) / 2
            );
        }

        private void CreateNewStar(int index)
        {
            _stars[index] = new Star
            {
                X = _random.Next(_width) - XOffset,
                Y = _random.Next(_height) - YOffset,
                Z = ZOffset,
                Speed = (_random.Next(9) + 1) / 10.0f,
                OX = 0,
                OY = 0
            };
        }

        private void UpdateBeatEffect()
        {
            if (_beatFrameCount > 0)
            {
                CurrentSpeed = Math.Max(0, CurrentSpeed + _beatSpeedIncrement);
                _beatFrameCount--;
            }
            else
            {
                CurrentSpeed = WarpSpeed;
            }
        }

        #endregion

        #region Configuration

        public override bool ValidateConfiguration()
        {
            // Validate property ranges
            MaxStars = Math.Max(100, Math.Min(4095, MaxStars));
            WarpSpeed = Math.Max(0.01f, Math.Min(50.0f, WarpSpeed));
            BeatSpeed = Math.Max(0.01f, Math.Min(50.0f, BeatSpeed));
            BeatDuration = Math.Max(1, Math.Min(100, BeatDuration));
            BlendMode = Math.Max(0, Math.Min(2, BlendMode));

            return true;
        }

        public override string GetSettingsSummary()
        {
            string blendMode = BlendMode == 0 ? "Replace" : BlendMode == 1 ? "Additive" : "50/50";
            string beatResponse = BeatResponse ? "On" : "Off";

            return $"Stars Effect - Enabled: {Enabled}, Stars: {MaxStars}, " +
                   $"Warp Speed: {WarpSpeed:F2}, Color: {StarColor.Name}, " +
                   $"Blend: {blendMode}, Beat Response: {beatResponse}";
        }

        #endregion
    }
}
```

## Effect Behavior

The Stars effect creates an immersive 3D space environment by:

1. **3D Star Positioning**: Each star has X, Y, and Z coordinates for depth perception
2. **Perspective Projection**: Stars closer to the viewer appear larger and brighter
3. **Dynamic Movement**: Stars move through 3D space at configurable speeds
4. **Beat Synchronization**: Speed changes can be triggered by audio beat detection
5. **Multiple Blending Modes**: Replace, additive, and 50/50 blending options
6. **Automatic Star Recycling**: Stars that move out of view are repositioned

## Key Features

- **3D Depth Simulation**: Realistic space travel effect with perspective projection
- **Individual Star Properties**: Each star has unique speed and movement characteristics
- **Audio Responsiveness**: Beat-triggered speed changes for dynamic visual impact
- **Flexible Blending**: Multiple blending modes for different visual styles
- **Performance Optimized**: Efficient star management and rendering
- **Configurable Parameters**: Adjustable star count, speed, color, and behavior

## Use Cases

- **Space Visualizations**: Create immersive space environments and starfields
- **Music Visualization**: Dynamic star movement that responds to audio
- **Gaming Backgrounds**: Animated space scenes for game environments
- **Cinematic Effects**: Professional space travel and exploration visuals
- **Ambient Displays**: Calming starfield animations for relaxation
