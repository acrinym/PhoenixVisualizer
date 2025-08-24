# Water Bump Mapping (Trans / Water Bump)

## Overview

The **Water Bump Mapping** system is a sophisticated fluid simulation engine that creates realistic water ripple effects and bump mapping distortions. It implements advanced physics-based water simulation with dual-buffer height maps, multiple blob generation methods, and intelligent water calculation algorithms. This effect is essential for creating realistic water effects, ripple animations, and dynamic surface distortions in AVS presets.

## Source Analysis

### Core Architecture (`r_waterbump.cpp`)

The effect is implemented as a C++ class `C_WaterBumpClass` that inherits from `C_RBASE`. It provides a comprehensive water simulation system with height map buffers, multiple blob generation algorithms, and advanced water physics calculations for realistic fluid effects.

### Key Components

#### Height Map System
Dual-buffer height map architecture:
- **Dual Buffers**: Alternating buffers for current and previous water states
- **Height Values**: Integer-based height representation for water displacement
- **Buffer Management**: Automatic buffer allocation and resizing
- **Memory Optimization**: Efficient height map storage and access

#### Blob Generation Methods
Multiple blob creation algorithms:
- **Sine Blob**: Cosine-based height distribution for smooth ripples
- **Height Blob**: Direct height addition for sharp disturbances
- **Random Placement**: Dynamic blob positioning for natural effects
- **Edge Clipping**: Intelligent boundary handling for seamless effects

#### Water Physics Engine
Advanced fluid simulation algorithms:
- **8-Pixel Method**: Sophisticated water height calculation using 8 surrounding pixels
- **Damping System**: Configurable water damping for realistic fluid behavior
- **Height Propagation**: Natural water height distribution and wave propagation
- **Physics Integration**: Realistic fluid dynamics and wave behavior

#### Bump Mapping System
Advanced displacement mapping:
- **Height Displacement**: Height map-based pixel displacement
- **Normal Calculation**: Surface normal calculation from height differences
- **Displacement Mapping**: Realistic surface distortion based on water height
- **Boundary Protection**: Safe displacement with boundary checking

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `enabled` | int | 0-1 | 1 | Enable water bump effect |
| `density` | int | 2-10 | 6 | Water damping factor |
| `depth` | int | 100-2000 | 600 | Blob depth/intensity |
| `random_drop` | int | 0-1 | 0 | Enable random blob placement |
| `drop_position_x` | int | 0-2 | 1 | X position for fixed drops (0=left, 1=center, 2=right) |
| `drop_position_y` | int | 0-2 | 1 | Y position for fixed drops (0=top, 1=middle, 2=bottom) |
| `drop_radius` | int | 10-100 | 40 | Blob radius in pixels |
| `method` | int | 0-1 | 0 | Water calculation method |

### Blob Positioning

| Position | X Value | Y Value | Description |
|----------|---------|---------|-------------|
| **Left** | 0 | - | Left quarter of screen |
| **Center** | 1 | - | Center of screen |
| **Right** | 2 | - | Right quarter of screen |
| **Top** | - | 0 | Top quarter of screen |
| **Middle** | - | 1 | Middle of screen |
| **Bottom** | - | 2 | Bottom quarter of screen |

## C# Implementation

```csharp
public class WaterBumpMappingNode : AvsModuleNode
{
    public bool Enabled { get; set; } = true;
    public int Density { get; set; } = 6;
    public int Depth { get; set; } = 600;
    public bool RandomDrop { get; set; } = false;
    public int DropPositionX { get; set; } = 1;
    public int DropPositionY { get; set; } = 1;
    public int DropRadius { get; set; } = 40;
    public int Method { get; set; } = 0;
    
    // Internal state
    private int[] heightBuffers;
    private int bufferWidth, bufferHeight;
    private int currentPage;
    private readonly object bufferLock = new object();
    private readonly Random random;
    
    // Performance optimization
    private const int MinDensity = 2;
    private const int MaxDensity = 10;
    private const int MinDepth = 100;
    private const int MaxDepth = 2000;
    private const int MinRadius = 10;
    private const int MaxRadius = 100;
    private const int MaxBlobRadius = 100;
    
    public WaterBumpMappingNode()
    {
        random = new Random();
        heightBuffers = null;
        bufferWidth = bufferHeight = 0;
        currentPage = 0;
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled || ctx.Width <= 0 || ctx.Height <= 0) return;
        
        // Update buffers if dimensions changed
        UpdateBuffers(ctx);
        
        // Handle beat events for blob generation
        if (ctx.IsBeat)
        {
            GenerateBlob(ctx);
        }
        
        // Apply bump mapping effect
        ApplyBumpMapping(ctx, input, output);
        
        // Calculate water physics for next frame
        CalculateWaterPhysics(ctx);
        
        // Switch buffer pages
        currentPage = 1 - currentPage;
    }
    
    private void UpdateBuffers(FrameContext ctx)
    {
        lock (bufferLock)
        {
            if (heightBuffers == null || bufferWidth != ctx.Width || bufferHeight != ctx.Height)
            {
                // Allocate new height buffers
                int bufferSize = ctx.Width * ctx.Height;
                heightBuffers = new int[bufferSize * 2]; // Two buffers
                bufferWidth = ctx.Width;
                bufferHeight = ctx.Height;
                currentPage = 0;
                
                // Initialize buffers to zero
                Array.Clear(heightBuffers, 0, heightBuffers.Length);
            }
        }
    }
    
    private void GenerateBlob(FrameContext ctx)
    {
        if (RandomDrop)
        {
            // Generate random blob position
            int maxDimension = Math.Max(ctx.Width, ctx.Height);
            int radius = (DropRadius * maxDimension) / 100;
            GenerateSineBlob(-1, -1, radius, -Depth, currentPage);
        }
        else
        {
            // Generate blob at fixed position
            int x, y;
            
            switch (DropPositionX)
            {
                case 0: x = ctx.Width / 4; break;
                case 1: x = ctx.Width / 2; break;
                case 2: x = (ctx.Width * 3) / 4; break;
                default: x = ctx.Width / 2; break;
            }
            
            switch (DropPositionY)
            {
                case 0: y = ctx.Height / 4; break;
                case 1: y = ctx.Height / 2; break;
                case 2: y = (ctx.Height * 3) / 4; break;
                default: y = ctx.Height / 2; break;
            }
            
            GenerateSineBlob(x, y, DropRadius, -Depth, currentPage);
        }
    }
    
    private void GenerateSineBlob(int x, int y, int radius, int height, int page)
    {
        // Generate random position if coordinates are negative
        if (x < 0) x = 1 + radius + random.Next(bufferWidth - 2 * radius - 1);
        if (y < 0) y = 1 + radius + random.Next(bufferHeight - 2 * radius - 1);
        
        int radiusSquared = radius * radius;
        int left = -radius, right = radius;
        int top = -radius, bottom = radius;
        
        // Perform edge clipping
        if (x - radius < 1) left -= (x - radius - 1);
        if (y - radius < 1) top -= (y - radius - 1);
        if (x + radius > bufferWidth - 1) right -= (x + radius - bufferWidth + 1);
        if (y + radius > bufferHeight - 1) bottom -= (y + radius - bufferHeight + 1);
        
        double length = (1024.0 / radius) * (1024.0 / radius);
        
        for (int cy = top; cy < bottom; cy++)
        {
            for (int cx = left; cx < right; cx++)
            {
                int square = cy * cy + cx * cx;
                if (square < radiusSquared)
                {
                    double distance = Math.Sqrt(square * length);
                    int bufferIndex = bufferWidth * (cy + y) + (cx + x);
                    
                    if (bufferIndex >= 0 && bufferIndex < heightBuffers.Length)
                    {
                        int heightOffset = (int)((Math.Cos(distance) + 0xFFFF) * height) >> 19;
                        heightBuffers[bufferIndex + page * bufferWidth * bufferHeight] += heightOffset;
                    }
                }
            }
        }
    }
    
    private void GenerateHeightBlob(int x, int y, int radius, int height, int page)
    {
        int radiusSquared = radius * radius;
        int left = -radius, right = radius;
        int top = -radius, bottom = radius;
        
        // Generate random position if coordinates are negative
        if (x < 0) x = 1 + radius + random.Next(bufferWidth - 2 * radius - 1);
        if (y < 0) y = 1 + radius + random.Next(bufferHeight - 2 * radius - 1);
        
        // Perform edge clipping
        if (x - radius < 1) left -= (x - radius - 1);
        if (y - radius < 1) top -= (y - radius - 1);
        if (x + radius > bufferWidth - 1) right -= (x + radius - bufferWidth + 1);
        if (y + radius > bufferHeight - 1) bottom -= (y + radius - bufferHeight + 1);
        
        for (int cy = top; cy < bottom; cy++)
        {
            int cySquared = cy * cy;
            for (int cx = left; cx < right; cx++)
            {
                if (cx * cx + cySquared < radiusSquared)
                {
                    int bufferIndex = bufferWidth * (cy + y) + (cx + x);
                    
                    if (bufferIndex >= 0 && bufferIndex < heightBuffers.Length)
                    {
                        heightBuffers[bufferIndex + page * bufferWidth * bufferHeight] += height;
                    }
                }
            }
        }
    }
    
    private void ApplyBumpMapping(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (heightBuffers == null) return;
        
        int width = ctx.Width;
        int height = ctx.Height;
        int bufferSize = width * height;
        int offset = width + 1;
        
        int[] currentBuffer = GetCurrentBuffer();
        
        for (int y = (height - 1) * width; offset < y; offset += 2)
        {
            for (int x = offset + width - 2; offset < x; offset++)
            {
                // Calculate displacement for first pixel
                int dx = currentBuffer[offset] - currentBuffer[offset + 1];
                int dy = currentBuffer[offset] - currentBuffer[offset + width];
                int displacementOffset = offset + width * (dy >> 3) + (dx >> 3);
                
                if (displacementOffset >= 0 && displacementOffset < bufferSize)
                {
                    Color pixel = input.GetPixel(displacementOffset % width, displacementOffset / width);
                    output.SetPixel(offset % width, offset / width, pixel);
                }
                else
                {
                    Color pixel = input.GetPixel(offset % width, offset / width);
                    output.SetPixel(offset % width, offset / width, pixel);
                }
                
                offset++;
                
                // Calculate displacement for second pixel
                dx = currentBuffer[offset] - currentBuffer[offset + 1];
                dy = currentBuffer[offset] - currentBuffer[offset + width];
                displacementOffset = offset + width * (dy >> 3) + (dx >> 3);
                
                if (displacementOffset >= 0 && displacementOffset < bufferSize)
                {
                    Color pixel = input.GetPixel(displacementOffset % width, displacementOffset / width);
                    output.SetPixel(offset % width, offset / width, pixel);
                }
                else
                {
                    Color pixel = input.GetPixel(offset % width, offset / width);
                    output.SetPixel(offset % width, offset / width, pixel);
                }
            }
        }
    }
    
    private void CalculateWaterPhysics(FrameContext ctx)
    {
        if (heightBuffers == null) return;
        
        int width = ctx.Width;
        int height = ctx.Height;
        int count = width + 1;
        
        int[] newBuffer = GetCurrentBuffer();
        int[] oldBuffer = GetPreviousBuffer();
        
        for (int y = (height - 1) * width; count < y; count += 2)
        {
            for (int x = count + width - 2; count < x; count++)
            {
                // 8-pixel water calculation method
                int newHeight = ((oldBuffer[count + width] +
                                oldBuffer[count - width] +
                                oldBuffer[count + 1] +
                                oldBuffer[count - 1] +
                                oldBuffer[count - width - 1] +
                                oldBuffer[count - width + 1] +
                                oldBuffer[count + width - 1] +
                                oldBuffer[count + width + 1]) >> 2) - newBuffer[count];
                
                newBuffer[count] = newHeight - (newHeight >> Density);
            }
        }
    }
    
    private int[] GetCurrentBuffer()
    {
        int startIndex = currentPage * bufferWidth * bufferHeight;
        int[] buffer = new int[bufferWidth * bufferHeight];
        Array.Copy(heightBuffers, startIndex, buffer, 0, buffer.Length);
        return buffer;
    }
    
    private int[] GetPreviousBuffer()
    {
        int startIndex = (1 - currentPage) * bufferWidth * bufferHeight;
        int[] buffer = new int[bufferWidth * bufferHeight];
        Array.Copy(heightBuffers, startIndex, buffer, 0, buffer.Length);
        return buffer;
    }
    
    // Public interface for parameter adjustment
    public void SetEnabled(bool enable) { Enabled = enable; }
    
    public void SetDensity(int density) 
    { 
        Density = Math.Clamp(density, MinDensity, MaxDensity); 
    }
    
    public void SetDepth(int depth) 
    { 
        Depth = Math.Clamp(depth, MinDepth, MaxDepth); 
    }
    
    public void SetRandomDrop(bool random) { RandomDrop = random; }
    
    public void SetDropPositionX(int position) 
    { 
        DropPositionX = Math.Clamp(position, 0, 2); 
    }
    
    public void SetDropPositionY(int position) 
    { 
        DropPositionY = Math.Clamp(position, 0, 2); 
    }
    
    public void SetDropRadius(int radius) 
    { 
        DropRadius = Math.Clamp(radius, MinRadius, MaxRadius); 
    }
    
    public void SetMethod(int method) { Method = method; }
    
    // Status queries
    public bool IsEnabled() => Enabled;
    public int GetDensity() => Density;
    public int GetDepth() => Depth;
    public bool IsRandomDropEnabled() => RandomDrop;
    public int GetDropPositionX() => DropPositionX;
    public int GetDropPositionY() => DropPositionY;
    public int GetDropRadius() => DropRadius;
    public int GetMethod() => Method;
    public int GetBufferWidth() => bufferWidth;
    public int GetBufferHeight() => bufferHeight;
    public bool IsBufferValid() => heightBuffers != null;
    
    // Advanced blob generation
    public void GenerateSineBlobAt(int x, int y, int radius, int height)
    {
        GenerateSineBlob(x, y, radius, height, currentPage);
    }
    
    public void GenerateHeightBlobAt(int x, int y, int radius, int height)
    {
        GenerateHeightBlob(x, y, radius, height, currentPage);
    }
    
    public void GenerateRandomBlob(int radius, int height)
    {
        int x = 1 + radius + random.Next(bufferWidth - 2 * radius - 1);
        int y = 1 + radius + random.Next(bufferHeight - 2 * radius - 1);
        GenerateSineBlob(x, y, radius, height, currentPage);
    }
    
    // Buffer management
    public void ClearBuffers()
    {
        lock (bufferLock)
        {
            if (heightBuffers != null)
            {
                Array.Clear(heightBuffers, 0, heightBuffers.Length);
            }
        }
    }
    
    public void ResetBuffers()
    {
        lock (bufferLock)
        {
            heightBuffers = null;
            bufferWidth = bufferHeight = 0;
            currentPage = 0;
        }
    }
    
    public override void Dispose()
    {
        lock (bufferLock)
        {
            heightBuffers = null;
        }
    }
}
```

## Integration Points

### Audio Data Integration
- **Beat Detection**: Responds to audio events for dynamic blob generation
- **Audio Analysis**: Processes audio data for enhanced water effects
- **Dynamic Parameters**: Adjusts water behavior based on audio events
- **Reactive Water**: Beat-reactive ripple generation and water disturbances

### Framebuffer Handling
- **Input/Output Buffers**: Processes `ImageBuffer` objects with full color support
- **Height Map Buffers**: Dual-buffer system for water height calculations
- **Displacement Mapping**: Advanced bump mapping based on height values
- **Performance Optimization**: Efficient height map management and calculations

### Performance Considerations
- **Dual Buffering**: Efficient buffer switching for water calculations
- **Height Maps**: Integer-based height representation for speed
- **Optimized Loops**: Efficient water physics calculations
- **Memory Management**: Intelligent buffer allocation and reuse

## Usage Examples

### Basic Water Effect
```csharp
var waterNode = new WaterBumpMappingNode
{
    Enabled = true,
    Density = 6,                   // Medium damping
    Depth = 600,                   // Medium blob intensity
    DropRadius = 40,               // 40 pixel radius
    RandomDrop = false,            // Fixed position drops
    DropPositionX = 1,             // Center X
    DropPositionY = 1              // Center Y
};
```

### Random Water Disturbances
```csharp
var waterNode = new WaterBumpMappingNode
{
    Enabled = true,
    Density = 4,                   // Low damping (more ripples)
    Depth = 800,                   // High blob intensity
    DropRadius = 60,               // Large radius
    RandomDrop = true,             // Random placement
    Method = 0                     // Standard water calculation
};
```

### Dynamic Water Control
```csharp
var waterNode = new WaterBumpMappingNode
{
    Enabled = true,
    Density = 8,                   // High damping
    Depth = 400,                   // Low blob intensity
    DropRadius = 30                // Small radius
};

// Generate custom blobs
waterNode.GenerateSineBlobAt(100, 100, 50, -500);
waterNode.GenerateRandomBlob(80, -800);
waterNode.SetDensity(3);          // Reduce damping for more ripples
```

## Technical Notes

### Water Physics Architecture
The effect implements sophisticated fluid simulation:
- **8-Pixel Method**: Advanced water height calculation using surrounding pixels
- **Damping System**: Configurable water damping for realistic behavior
- **Height Propagation**: Natural wave propagation and height distribution
- **Physics Integration**: Realistic fluid dynamics and wave behavior

### Performance Architecture
Advanced optimization techniques:
- **Dual Buffering**: Efficient buffer switching for water calculations
- **Height Maps**: Integer-based height representation for speed
- **Optimized Loops**: Efficient water physics calculations
- **Memory Management**: Intelligent buffer allocation and reuse

### Blob Generation System
Multiple blob creation algorithms:
- **Sine Blob**: Cosine-based height distribution for smooth ripples
- **Height Blob**: Direct height addition for sharp disturbances
- **Random Placement**: Dynamic blob positioning for natural effects
- **Edge Clipping**: Intelligent boundary handling for seamless effects

This effect provides the foundation for sophisticated water simulations, ripple effects, and dynamic surface distortions, making it essential for realistic fluid visualization and advanced AVS preset creation.
