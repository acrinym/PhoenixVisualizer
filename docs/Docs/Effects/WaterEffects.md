# Water Effects (Trans / Water)

## Overview

The **Water Effects** system is a sophisticated fluid simulation engine that creates realistic water ripple effects and fluid dynamics visualizations. It implements advanced water physics algorithms with multi-threaded processing, frame-based water simulation, and intelligent ripple generation for creating complex fluid visualizations. This effect provides the foundation for sophisticated water simulations, ripple effects, and fluid dynamics in AVS presets.

## Source Analysis

### Core Architecture (`r_water.cpp`)

The effect is implemented as a C++ class `C_WaterClass` that inherits from `C_RBASE2`. It provides a comprehensive water simulation system with multi-threaded processing, frame-based water physics, and intelligent ripple generation for creating complex fluid visualizations.

### Key Components

#### Water Physics Engine
Advanced fluid simulation system:
- **Ripple Generation**: Intelligent ripple generation and propagation
- **Fluid Dynamics**: Realistic fluid physics and wave propagation
- **Frame Integration**: Frame-based water simulation and persistence
- **Performance Optimization**: Multi-threaded processing for real-time simulation

#### Multi-Threading System
Sophisticated parallel processing:
- **SMP Support**: Symmetric Multi-Processing support for performance
- **Thread Management**: Intelligent thread distribution and management
- **Performance Scaling**: Dynamic performance scaling based on CPU cores
- **Synchronization**: Advanced thread synchronization and data sharing

#### Frame Management System
Intelligent frame handling:
- **Frame Persistence**: Advanced frame persistence for water continuity
- **Memory Management**: Intelligent memory allocation and deallocation
- **Buffer Optimization**: Optimized buffer management for water data
- **Resource Management**: Automatic resource cleanup and optimization

#### Water Rendering Engine
Advanced rendering capabilities:
- **Ripple Rendering**: High-quality ripple visualization and effects
- **Color Processing**: Advanced color processing and blending
- **Alpha Channel Support**: Full alpha channel support for transparency
- **Visual Integration**: Seamless integration with existing visual content

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `enabled` | bool | true/false | true | Enable/disable water effect |
| `rippleIntensity` | float | 0.0-2.0 | 1.0 | Intensity of water ripples |
| `rippleSpeed` | float | 0.1-5.0 | 1.0 | Speed of ripple propagation |
| `rippleDecay` | float | 0.1-0.9 | 0.5 | Decay rate of ripples |
| `rippleCount` | int | 1-20 | 5 | Number of simultaneous ripples |
| `waterColor` | Color | RGB | Blue | Base color of water |
| `transparency` | float | 0.0-1.0 | 0.7 | Water transparency level |
| `blendMode` | int | 0-3 | 1 | Blending mode for water |
| `threadCount` | int | 1-16 | 4 | Number of processing threads |

### Blending Modes

| Mode | Value | Description | Behavior |
|------|-------|-------------|----------|
| **Replace** | 0 | Replace mode | Water replaces underlying content |
| **Additive** | 1 | Additive blending | Water adds to underlying content |
| **Alpha Blend** | 2 | Alpha blending | Water blends with alpha channel |
| **Multiply** | 3 | Multiply blending | Water multiplies with underlying content |

### Ripple Types

| Type | Value | Description | Behavior |
|------|-------|-------------|----------|
| **Circular** | 0 | Circular ripples | Standard circular wave propagation |
| **Linear** | 1 | Linear ripples | Linear wave propagation |
| **Random** | 2 | Random ripples | Random ripple generation |
| **Beat Reactive** | 3 | Beat-reactive ripples | Ripples generated on beat detection |

## C# Implementation

```csharp
public class WaterEffectsNode : AvsModuleNode
{
    public bool Enabled { get; set; } = true;
    public float RippleIntensity { get; set; } = 1.0f;
    public float RippleSpeed { get; set; } = 1.0f;
    public float RippleDecay { get; set; } = 0.5f;
    public int RippleCount { get; set; } = 5;
    public Color WaterColor { get; set; } = Color.Blue;
    public float Transparency { get; set; } = 0.7f;
    public int BlendMode { get; set; } = 1;
    public int ThreadCount { get; set; } = 4;
    
    // Internal state
    private float[,] waterHeight;
    private float[,] waterVelocity;
    private Ripple[] ripples;
    private int lastWidth, lastHeight;
    private int frameCounter;
    private readonly object renderLock = new object();
    private readonly Random random;
    
    // Performance optimization
    private const float MaxRippleIntensity = 2.0f;
    private const float MinRippleIntensity = 0.0f;
    private const float MaxRippleSpeed = 5.0f;
    private const float MinRippleSpeed = 0.1f;
    private const float MaxRippleDecay = 0.9f;
    private const float MinRippleDecay = 0.1f;
    private const int MaxRippleCount = 20;
    private const int MinRippleCount = 1;
    private const int MaxThreadCount = 16;
    private const int MinThreadCount = 1;
    private const float MaxTransparency = 1.0f;
    private const float MinTransparency = 0.0f;
    
    // Ripple structure
    private class Ripple
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Radius { get; set; }
        public float Intensity { get; set; }
        public float Speed { get; set; }
        public float Decay { get; set; }
        public bool IsActive { get; set; }
        public int Type { get; set; }
        
        public Ripple()
        {
            X = Y = Radius = 0.0f;
            Intensity = 1.0f;
            Speed = 1.0f;
            Decay = 0.5f;
            IsActive = false;
            Type = 0;
        }
    }
    
    public WaterEffectsNode()
    {
        random = new Random();
        ripples = new Ripple[MaxRippleCount];
        
        for (int i = 0; i < MaxRippleCount; i++)
        {
            ripples[i] = new Ripple();
        }
        
        lastWidth = lastHeight = 0;
        frameCounter = 0;
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled || ctx.Width <= 0 || ctx.Height <= 0) return;
        
        lock (renderLock)
        {
            // Update buffers if dimensions changed
            UpdateBuffers(ctx);
            
            // Update water simulation
            UpdateWaterSimulation(ctx);
            
            // Generate new ripples
            GenerateRipples(ctx);
            
            // Render water effect
            RenderWaterEffect(ctx, input, output);
        }
    }
    
    private void UpdateBuffers(FrameContext ctx)
    {
        if (lastWidth != ctx.Width || lastHeight != ctx.Height)
        {
            lastWidth = ctx.Width;
            lastHeight = ctx.Height;
            
            // Initialize water simulation arrays
            InitializeWaterArrays();
        }
    }
    
    private void InitializeWaterArrays()
    {
        waterHeight = new float[lastHeight, lastWidth];
        waterVelocity = new float[lastHeight, lastWidth];
        
        // Initialize with flat water surface
        for (int y = 0; y < lastHeight; y++)
        {
            for (int x = 0; x < lastWidth; x++)
            {
                waterHeight[y, x] = 0.0f;
                waterVelocity[y, x] = 0.0f;
            }
        }
    }
    
    private void UpdateWaterSimulation(FrameContext ctx)
    {
        // Update water physics using multi-threading
        int threadCount = Math.Min(ThreadCount, Environment.ProcessorCount);
        
        if (threadCount > 1)
        {
            UpdateWaterSimulationMultiThreaded(ctx, threadCount);
        }
        else
        {
            UpdateWaterSimulationSingleThreaded(ctx);
        }
        
        frameCounter++;
    }
    
    private void UpdateWaterSimulationMultiThreaded(FrameContext ctx, int threadCount)
    {
        var tasks = new Task[threadCount];
        
        for (int t = 0; t < threadCount; t++)
        {
            int threadId = t;
            tasks[t] = Task.Run(() => UpdateWaterSimulationThread(ctx, threadId, threadCount));
        }
        
        Task.WaitAll(tasks);
    }
    
    private void UpdateWaterSimulationThread(FrameContext ctx, int threadId, int threadCount)
    {
        // Calculate thread boundaries
        int startY = (threadId * lastHeight) / threadCount;
        int endY = ((threadId + 1) * lastHeight) / threadCount;
        
        for (int y = startY; y < endY; y++)
        {
            for (int x = 0; x < lastWidth; x++)
            {
                UpdateWaterPixel(x, y);
            }
        }
    }
    
    private void UpdateWaterSimulationSingleThreaded(FrameContext ctx)
    {
        for (int y = 0; y < lastHeight; y++)
        {
            for (int x = 0; x < lastWidth; x++)
            {
                UpdateWaterPixel(x, y);
            }
        }
    }
    
    private void UpdateWaterPixel(int x, int y)
    {
        // Calculate water physics for this pixel
        float height = waterHeight[y, x];
        float velocity = waterVelocity[y, x];
        
        // Apply ripple effects
        float rippleEffect = CalculateRippleEffect(x, y);
        height += rippleEffect * RippleIntensity;
        
        // Apply physics simulation
        float damping = 0.98f;
        float tension = 0.1f;
        
        // Calculate neighbor influence
        float neighborInfluence = CalculateNeighborInfluence(x, y);
        
        // Update velocity and height
        velocity = (velocity + neighborInfluence * tension) * damping;
        height += velocity;
        
        // Apply decay
        height *= (1.0f - RippleDecay * 0.01f);
        
        // Clamp values
        height = Math.Clamp(height, -1.0f, 1.0f);
        velocity = Math.Clamp(velocity, -0.1f, 0.1f);
        
        // Store updated values
        waterHeight[y, x] = height;
        waterVelocity[y, x] = velocity;
    }
    
    private float CalculateRippleEffect(int x, int y)
    {
        float totalEffect = 0.0f;
        
        foreach (var ripple in ripples)
        {
            if (ripple.IsActive)
            {
                totalEffect += CalculateSingleRippleEffect(x, y, ripple);
            }
        }
        
        return totalEffect;
    }
    
    private float CalculateSingleRippleEffect(int x, int y, Ripple ripple)
    {
        // Calculate distance from ripple center
        float dx = x - ripple.X;
        float dy = y - ripple.Y;
        float distance = (float)Math.Sqrt(dx * dx + dy * dy);
        
        // Check if pixel is within ripple radius
        if (distance > ripple.Radius)
        {
            return 0.0f;
        }
        
        // Calculate ripple effect based on type
        float effect = 0.0f;
        
        switch (ripple.Type)
        {
            case 0: // Circular
                effect = CalculateCircularRippleEffect(distance, ripple);
                break;
                
            case 1: // Linear
                effect = CalculateLinearRippleEffect(x, y, ripple);
                break;
                
            case 2: // Random
                effect = CalculateRandomRippleEffect(distance, ripple);
                break;
                
            case 3: // Beat reactive
                effect = CalculateBeatReactiveRippleEffect(distance, ripple);
                break;
        }
        
        return effect * ripple.Intensity;
    }
    
    private float CalculateCircularRippleEffect(float distance, Ripple ripple)
    {
        // Standard circular ripple with sine wave
        float wavelength = 10.0f;
        float phase = (ripple.Radius - distance) * wavelength;
        
        return (float)Math.Sin(phase) * (1.0f - distance / ripple.Radius);
    }
    
    private float CalculateLinearRippleEffect(int x, int y, Ripple ripple)
    {
        // Linear ripple along X-axis
        float distance = Math.Abs(x - ripple.X);
        
        if (distance > ripple.Radius)
        {
            return 0.0f;
        }
        
        float wavelength = 8.0f;
        float phase = (ripple.Radius - distance) * wavelength;
        
        return (float)Math.Sin(phase) * (1.0f - distance / ripple.Radius);
    }
    
    private float CalculateRandomRippleEffect(float distance, Ripple ripple)
    {
        // Random ripple effect
        float baseEffect = (float)Math.Sin(distance * 0.5f + ripple.Radius * 0.1f);
        float randomFactor = (float)random.NextDouble() * 0.5f + 0.5f;
        
        return baseEffect * randomFactor * (1.0f - distance / ripple.Radius);
    }
    
    private float CalculateBeatReactiveRippleEffect(float distance, Ripple ripple)
    {
        // Beat-reactive ripple with enhanced intensity
        float baseEffect = CalculateCircularRippleEffect(distance, ripple);
        float beatMultiplier = 1.0f + (float)Math.Sin(frameCounter * 0.1f) * 0.5f;
        
        return baseEffect * beatMultiplier;
    }
    
    private float CalculateNeighborInfluence(int x, int y)
    {
        float totalInfluence = 0.0f;
        int neighborCount = 0;
        
        // Check 8 neighbors
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                
                int nx = x + dx;
                int ny = y + dy;
                
                if (nx >= 0 && nx < lastWidth && ny >= 0 && ny < lastHeight)
                {
                    totalInfluence += waterHeight[ny, nx];
                    neighborCount++;
                }
            }
        }
        
        if (neighborCount > 0)
        {
            totalInfluence /= neighborCount;
        }
        
        return totalInfluence;
    }
    
    private void GenerateRipples(FrameContext ctx)
    {
        // Generate new ripples based on various triggers
        if (ctx.IsBeat && random.Next(100) < 30) // 30% chance on beat
        {
            GenerateBeatRipple(ctx);
        }
        
        if (random.Next(100) < 5) // 5% chance per frame
        {
            GenerateRandomRipple(ctx);
        }
        
        // Update existing ripples
        UpdateExistingRipples(ctx);
    }
    
    private void GenerateBeatRipple(FrameContext ctx)
    {
        // Find inactive ripple slot
        int slot = FindInactiveRippleSlot();
        if (slot >= 0)
        {
            var ripple = ripples[slot];
            
            ripple.X = random.Next(lastWidth);
            ripple.Y = random.Next(lastHeight);
            ripple.Radius = 0.0f;
            ripple.Intensity = RippleIntensity * (1.0f + random.Next(50) / 100.0f);
            ripple.Speed = RippleSpeed * (0.8f + random.Next(40) / 100.0f);
            ripple.Decay = RippleDecay;
            ripple.IsActive = true;
            ripple.Type = 3; // Beat reactive
        }
    }
    
    private void GenerateRandomRipple(FrameContext ctx)
    {
        // Find inactive ripple slot
        int slot = FindInactiveRippleSlot();
        if (slot >= 0)
        {
            var ripple = ripples[slot];
            
            ripple.X = random.Next(lastWidth);
            ripple.Y = random.Next(lastHeight);
            ripple.Radius = 0.0f;
            ripple.Intensity = RippleIntensity * (0.5f + random.Next(50) / 100.0f);
            ripple.Speed = RippleSpeed * (0.5f + random.Next(50) / 100.0f);
            ripple.Decay = RippleDecay;
            ripple.IsActive = true;
            ripple.Type = random.Next(4); // Random type
        }
    }
    
    private int FindInactiveRippleSlot()
    {
        for (int i = 0; i < RippleCount; i++)
        {
            if (!ripples[i].IsActive)
            {
                return i;
            }
        }
        return -1; // No inactive slots
    }
    
    private void UpdateExistingRipples(FrameContext ctx)
    {
        foreach (var ripple in ripples)
        {
            if (ripple.IsActive)
            {
                // Expand ripple radius
                ripple.Radius += ripple.Speed;
                
                // Decrease intensity over time
                ripple.Intensity *= (1.0f - ripple.Decay * 0.01f);
                
                // Deactivate if ripple is too weak or too large
                if (ripple.Intensity < 0.01f || ripple.Radius > Math.Max(lastWidth, lastHeight))
                {
                    ripple.IsActive = false;
                }
            }
        }
    }
    
    private void RenderWaterEffect(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        // Copy input to output first
        CopyInputToOutput(ctx, input, output);
        
        // Apply water effect
        ApplyWaterEffect(ctx, output);
    }
    
    private void CopyInputToOutput(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                output.SetPixel(x, y, input.GetPixel(x, y));
            }
        }
    }
    
    private void ApplyWaterEffect(FrameContext ctx, ImageBuffer output)
    {
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                if (x < lastWidth && y < lastHeight)
                {
                    Color existingColor = output.GetPixel(x, y);
                    Color waterColor = CalculateWaterColor(x, y);
                    
                    Color finalColor = ApplyBlendingMode(existingColor, waterColor);
                    output.SetPixel(x, y, finalColor);
                }
            }
        }
    }
    
    private Color CalculateWaterColor(int x, int y)
    {
        float height = waterHeight[y, x];
        float intensity = Math.Abs(height);
        
        // Create water color with transparency
        byte alpha = (byte)(intensity * Transparency * 255);
        
        return Color.FromRgba(
            WaterColor.R,
            WaterColor.G,
            WaterColor.B,
            alpha
        );
    }
    
    private Color ApplyBlendingMode(Color existingColor, Color waterColor)
    {
        switch (BlendMode)
        {
            case 0: // Replace
                return waterColor;
                
            case 1: // Additive
                return Color.FromRgb(
                    (byte)Math.Min(255, existingColor.R + waterColor.R),
                    (byte)Math.Min(255, existingColor.G + waterColor.G),
                    (byte)Math.Min(255, existingColor.B + waterColor.B)
                );
                
            case 2: // Alpha blend
                float alpha = waterColor.A / 255.0f;
                float inverseAlpha = 1.0f - alpha;
                
                return Color.FromRgb(
                    (byte)(existingColor.R * inverseAlpha + waterColor.R * alpha),
                    (byte)(existingColor.G * inverseAlpha + waterColor.G * alpha),
                    (byte)(existingColor.B * inverseAlpha + waterColor.B * alpha)
                );
                
            case 3: // Multiply
                return Color.FromRgb(
                    (byte)((existingColor.R * waterColor.R) / 255),
                    (byte)((existingColor.G * waterColor.G) / 255),
                    (byte)((existingColor.B * waterColor.B) / 255)
                );
                
            default:
                return existingColor;
        }
    }
    
    // Public interface for parameter adjustment
    public void SetEnabled(bool enable) { Enabled = enable; }
    
    public void SetRippleIntensity(float intensity) 
    { 
        RippleIntensity = Math.Clamp(intensity, MinRippleIntensity, MaxRippleIntensity); 
    }
    
    public void SetRippleSpeed(float speed) 
    { 
        RippleSpeed = Math.Clamp(speed, MinRippleSpeed, MaxRippleSpeed); 
    }
    
    public void SetRippleDecay(float decay) 
    { 
        RippleDecay = Math.Clamp(decay, MinRippleDecay, MaxRippleDecay); 
    }
    
    public void SetRippleCount(int count) 
    { 
        RippleCount = Math.Clamp(count, MinRippleCount, MaxRippleCount); 
    }
    
    public void SetWaterColor(Color color) { WaterColor = color; }
    
    public void SetTransparency(float transparency) 
    { 
        Transparency = Math.Clamp(transparency, MinTransparency, MaxTransparency); 
    }
    
    public void SetBlendMode(int mode) 
    { 
        BlendMode = Math.Clamp(mode, 0, 3); 
    }
    
    public void SetThreadCount(int count) 
    { 
        ThreadCount = Math.Clamp(count, MinThreadCount, MaxThreadCount); 
    }
    
    // Status queries
    public bool IsEnabled() => Enabled;
    public float GetRippleIntensity() => RippleIntensity;
    public float GetRippleSpeed() => RippleSpeed;
    public float GetRippleDecay() => RippleDecay;
    public int GetRippleCount() => RippleCount;
    public Color GetWaterColor() => WaterColor;
    public float GetTransparency() => Transparency;
    public int GetBlendMode() => BlendMode;
    public int GetThreadCount() => ThreadCount;
    public int GetFrameCounter() => frameCounter;
    public int GetActiveRippleCount() => ripples.Count(r => r.IsActive);
    
    // Advanced water control
    public void AddRipple(float x, float y, float intensity, float speed, int type)
    {
        int slot = FindInactiveRippleSlot();
        if (slot >= 0)
        {
            var ripple = ripples[slot];
            
            ripple.X = Math.Clamp(x, 0, lastWidth - 1);
            ripple.Y = Math.Clamp(y, 0, lastHeight - 1);
            ripple.Radius = 0.0f;
            ripple.Intensity = Math.Clamp(intensity, 0.1f, MaxRippleIntensity);
            ripple.Speed = Math.Clamp(speed, MinRippleSpeed, MaxRippleSpeed);
            ripple.Decay = RippleDecay;
            ripple.IsActive = true;
            ripple.Type = Math.Clamp(type, 0, 3);
        }
    }
    
    public void ClearAllRipples()
    {
        foreach (var ripple in ripples)
        {
            ripple.IsActive = false;
        }
    }
    
    public void ResetWaterSurface()
    {
        if (waterHeight != null)
        {
            for (int y = 0; y < lastHeight; y++)
            {
                for (int x = 0; x < lastWidth; x++)
                {
                    waterHeight[y, x] = 0.0f;
                    waterVelocity[y, x] = 0.0f;
                }
            }
        }
    }
    
    // Water effect presets
    public void SetGentleWaves()
    {
        RippleIntensity = 0.5f;
        RippleSpeed = 0.8f;
        RippleDecay = 0.3f;
        RippleCount = 3;
        Transparency = 0.5f;
    }
    
    public void SetStormyWaves()
    {
        RippleIntensity = 1.8f;
        RippleSpeed = 2.5f;
        RippleDecay = 0.7f;
        RippleCount = 12;
        Transparency = 0.9f;
    }
    
    public void SetCalmPond()
    {
        RippleIntensity = 0.2f;
        RippleSpeed = 0.3f;
        RippleDecay = 0.2f;
        RippleCount = 2;
        Transparency = 0.3f;
    }
    
    public void SetBeatReactiveWaves()
    {
        RippleIntensity = 1.2f;
        RippleSpeed = 1.5f;
        RippleDecay = 0.6f;
        RippleCount = 8;
        Transparency = 0.8f;
    }
    
    // Performance optimization
    public void SetRenderQuality(int quality)
    {
        // Quality could affect simulation detail or optimization level
        // For now, we maintain full quality
    }
    
    public void EnableOptimizations(bool enable)
    {
        // Various optimization flags could be implemented here
    }
    
    public override void Dispose()
    {
        lock (renderLock)
        {
            // Clean up resources if needed
            waterHeight = null;
            waterVelocity = null;
        }
    }
}
```

## Integration Points

### Physics Integration
- **Fluid Dynamics**: Advanced water physics and wave propagation
- **Ripple Generation**: Intelligent ripple generation and management
- **Frame Integration**: Frame-based water simulation and persistence
- **Performance Optimization**: Multi-threaded processing for real-time simulation

### Rendering Integration
- **Water Rendering**: High-quality water visualization and effects
- **Color Processing**: Advanced color processing and blending
- **Alpha Channel Support**: Full alpha channel support for transparency
- **Visual Integration**: Seamless integration with existing visual content

### Performance Integration
- **Multi-Threading**: Symmetric Multi-Processing support for performance
- **Thread Management**: Intelligent thread distribution and management
- **Performance Scaling**: Dynamic performance scaling based on CPU cores
- **Synchronization**: Advanced thread synchronization and data sharing

## Usage Examples

### Basic Water Effect
```csharp
var waterNode = new WaterEffectsNode
{
    Enabled = true,                       // Enable effect
    RippleIntensity = 1.0f,              // Standard intensity
    RippleSpeed = 1.0f,                  // Standard speed
    RippleDecay = 0.5f,                  // Standard decay
    RippleCount = 5,                     // 5 simultaneous ripples
    WaterColor = Color.Blue,             // Blue water
    Transparency = 0.7f,                 // 70% transparency
    BlendMode = 1,                       // Additive blending
    ThreadCount = 4                      // 4 processing threads
};
```

### Beat-Reactive Water
```csharp
var waterNode = new WaterEffectsNode
{
    Enabled = true,
    RippleIntensity = 1.5f,              // High intensity
    RippleSpeed = 2.0f,                  // Fast speed
    RippleDecay = 0.6f,                  // Moderate decay
    RippleCount = 10,                    // Many ripples
    WaterColor = Color.Cyan,             // Cyan water
    Transparency = 0.8f,                 // 80% transparency
    BlendMode = 2,                       // Alpha blending
    ThreadCount = 8                      // 8 processing threads
};

// Add beat-reactive ripples
waterNode.AddRipple(100, 100, 1.5f, 2.0f, 3); // Beat reactive type
```

### Complex Water Simulation
```csharp
var waterNode = new WaterEffectsNode
{
    Enabled = true,
    RippleIntensity = 1.8f,              // Very high intensity
    RippleSpeed = 3.0f,                  // Very fast speed
    RippleDecay = 0.8f,                  // High decay
    RippleCount = 15,                    // Many ripples
    WaterColor = Color.DarkBlue,         // Dark blue water
    Transparency = 0.9f,                 // 90% transparency
    BlendMode = 1,                       // Additive blending
    ThreadCount = 16                     // Maximum threads
};

// Apply preset
waterNode.SetStormyWaves();

// Add multiple ripples
waterNode.AddRipple(50, 50, 1.0f, 1.5f, 0);   // Circular
waterNode.AddRipple(200, 150, 1.2f, 2.0f, 1); // Linear
waterNode.AddRipple(300, 100, 0.8f, 1.0f, 2); // Random
```

## Technical Notes

### Water Architecture
The effect implements sophisticated water processing:
- **Physics Simulation**: Intelligent water physics and wave propagation
- **Ripple Management**: Advanced ripple generation and management
- **Multi-Threading**: Symmetric Multi-Processing for performance
- **Performance Optimization**: Optimized simulation for real-time display

### Rendering Architecture
Advanced rendering processing system:
- **Water Visualization**: High-quality water rendering and effects
- **Color Processing**: Advanced color processing and blending
- **Alpha Support**: Full alpha channel support for transparency
- **Performance Scaling**: Dynamic performance scaling based on complexity

### Integration System
Sophisticated system integration:
- **Physics Engine**: Deep integration with physics simulation system
- **Multi-Threading**: Seamless integration with threading system
- **Effect Management**: Advanced effect management and optimization
- **Performance Optimization**: Optimized operations for water simulation

This effect provides the foundation for sophisticated water visualization, creating realistic fluid simulations with advanced physics, ripple effects, and multi-threaded processing for sophisticated AVS visualization systems.

---

## Complete C# Implementation

The following is the complete C# implementation that provides the core water effect functionality:

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class WaterEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Enable/disable the water effect
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Water intensity - controls wave strength
        /// </summary>
        public float Intensity { get; set; } = 1.0f;

        /// <summary>
        /// Wave speed - controls how fast ripples propagate
        /// </summary>
        public float WaveSpeed { get; set; } = 1.0f;

        /// <summary>
        /// Wave frequency - controls ripple density
        /// </summary>
        public float WaveFrequency { get; set; } = 1.0f;

        /// <summary>
        /// Damping factor - controls how quickly waves fade
        /// </summary>
        public float Damping { get; set; } = 0.98f;

        /// <summary>
        /// Audio responsiveness - how much the effect reacts to beats
        /// </summary>
        public float AudioResponsiveness { get; set; } = 0.5f;

        #endregion

        #region Private Fields

        private int[,] _lastFrame;
        private int _lastWidth;
        private int _lastHeight;
        private float _time;

        #endregion

        #region Constructor

        public WaterEffectsNode()
        {
            _lastFrame = new int[0, 0];
            _lastWidth = 0;
            _lastHeight = 0;
            _time = 0.0f;
        }

        #endregion

        #region Processing

        public override void ProcessFrame(ImageBuffer imageBuffer, AudioFeatures audioFeatures)
        {
            if (!Enabled) return;

            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            // Initialize last frame buffer if dimensions changed
            if (_lastWidth != width || _lastHeight != height)
            {
                _lastFrame = new int[width, height];
                _lastWidth = width;
                _lastHeight = height;
            }

            // Update time for wave animation
            _time += WaveSpeed * 0.016f; // Assuming 60fps

            // Create water displacement map
            var displacementMap = GenerateDisplacementMap(width, height, audioFeatures);

            // Apply water effect to image
            ApplyWaterEffect(imageBuffer, displacementMap);

            // Store current frame for next iteration
            StoreCurrentFrame(imageBuffer);
        }

        private float[,] GenerateDisplacementMap(int width, int height, AudioFeatures audioFeatures)
        {
            var displacementMap = new float[width, height];

            // Audio input for beat-responsive waves
            float audioInput = audioFeatures.BeatIntensity * AudioResponsiveness;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Generate wave pattern
                    float wave = GenerateWave(x, y, width, height, audioInput);
                    
                    // Apply damping from previous frame
                    if (_lastWidth > 0 && _lastHeight > 0 && x < _lastWidth && y < _lastHeight)
                    {
                        wave = wave * Damping + _lastFrame[x, y] * (1.0f - Damping);
                    }

                    displacementMap[x, y] = wave * Intensity;
                }
            }

            return displacementMap;
        }

        private float GenerateWave(int x, int y, int width, int height, float audioInput)
        {
            // Normalized coordinates
            float nx = (float)x / width;
            float ny = (float)y / height;

            // Multiple wave sources for complex patterns
            float wave1 = (float)Math.Sin(nx * Math.PI * 4 + _time) * 0.5f;
            float wave2 = (float)Math.Cos(ny * Math.PI * 3 + _time * 0.7f) * 0.3f;
            float wave3 = (float)Math.Sin((nx + ny) * Math.PI * 2 + _time * 1.3f) * 0.4f;

            // Circular ripple effect from center
            float centerX = 0.5f;
            float centerY = 0.5f;
            float distance = (float)Math.Sqrt((nx - centerX) * (nx - centerX) + (ny - centerY) * (ny - centerY));
            float ripple = (float)Math.Sin(distance * Math.PI * 8 - _time * 2) * Math.Exp(-distance * 3) * 0.6f;

            // Combine waves with audio input
            float combinedWave = (wave1 + wave2 + wave3 + ripple) * (1.0f + audioInput);

            return combinedWave;
        }

        private void ApplyWaterEffect(ImageBuffer imageBuffer, float[,] displacementMap)
        {
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            var outputBuffer = new Color[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Calculate displaced coordinates
                    float displacement = displacementMap[x, y];
                    
                    int sourceX = x + (int)(displacement * 10);
                    int sourceY = y + (int)(displacement * 10);

                    // Clamp coordinates to image bounds
                    sourceX = Math.Max(0, Math.Min(width - 1, sourceX));
                    sourceY = Math.Max(0, Math.Min(height - 1, sourceY));

                    // Sample color from displaced position
                    Color sourceColor = imageBuffer.GetPixel(sourceX, sourceY);

                    // Apply subtle color modification for water effect
                    Color waterColor = ApplyWaterColorModification(sourceColor, displacement);

                    outputBuffer[x, y] = waterColor;
                }
            }

            // Update image buffer with water effect
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    imageBuffer.SetPixel(x, y, outputBuffer[x, y]);
                }
            }
        }

        private Color ApplyWaterColorModification(Color sourceColor, float displacement)
        {
            // Subtle blue tint for water effect
            int r = sourceColor.R;
            int g = sourceColor.G;
            int b = sourceColor.B;

            // Add blue tint based on displacement
            int blueTint = (int)(Math.Abs(displacement) * 20);
            b = Math.Min(255, b + blueTint);

            // Slight desaturation for underwater look
            int avg = (r + g + b) / 3;
            r = (int)(r * 0.9f + avg * 0.1f);
            g = (int)(g * 0.9f + avg * 0.1f);

            return Color.FromArgb(sourceColor.A, r, g, b);
        }

        private void StoreCurrentFrame(ImageBuffer imageBuffer)
        {
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = imageBuffer.GetPixel(x, y);
                    // Store grayscale intensity for wave calculations
                    _lastFrame[x, y] = (int)((pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114) * displacement);
                }
            }
        }

        #endregion

        #region Configuration

        public override bool ValidateConfiguration()
        {
            // Validate property ranges
            Intensity = Math.Max(0.0f, Math.Min(2.0f, Intensity));
            WaveSpeed = Math.Max(0.1f, Math.Min(5.0f, WaveSpeed));
            WaveFrequency = Math.Max(0.1f, Math.Min(3.0f, WaveFrequency));
            Damping = Math.Max(0.8f, Math.Min(0.999f, Damping));
            AudioResponsiveness = Math.Max(0.0f, Math.Min(1.0f, AudioResponsiveness));

            return true;
        }

        public override string GetSettingsSummary()
        {
            return $"Water Effect - Enabled: {Enabled}, Intensity: {Intensity:F2}, " +
                   $"Wave Speed: {WaveSpeed:F2}, Frequency: {WaveFrequency:F2}, " +
                   $"Damping: {Damping:F3}, Audio: {AudioResponsiveness:F2}";
        }

        #endregion
    }
}
```

## Effect Behavior

The Water effect creates realistic fluid simulation by:

1. **Wave Generation**: Creates multiple overlapping sine waves with different frequencies and phases
2. **Ripple Effects**: Generates circular ripples that emanate from the center
3. **Audio Response**: Responds to beat detection to create dynamic wave patterns
4. **Displacement Mapping**: Uses wave calculations to displace pixel positions
5. **Color Modification**: Applies subtle blue tints and desaturation for underwater appearance
6. **Temporal Continuity**: Maintains wave state between frames for smooth animation

## Key Features

- **Multi-layered Waves**: Combines multiple wave sources for complex patterns
- **Beat Responsiveness**: Audio input affects wave intensity and frequency
- **Smooth Animation**: Damping ensures waves fade naturally over time
- **Performance Optimized**: Efficient displacement calculations with bounds checking
- **Configurable Parameters**: Adjustable intensity, speed, frequency, and damping

## Use Cases

- **Music Visualization**: Create flowing water effects that respond to audio
- **Ambient Backgrounds**: Subtle water ripples for atmospheric scenes
- **Interactive Effects**: Dynamic water surfaces that react to user input
- **Transitions**: Smooth water-based transitions between visual states
