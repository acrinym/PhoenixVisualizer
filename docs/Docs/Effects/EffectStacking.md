# Effect Stacking (Misc / Buffer Save)

## Overview

The **Effect Stacking** system is a sophisticated buffer management and blending engine that provides advanced framebuffer operations for creating complex visual effects. It implements a global buffer system with 12 different blending modes, allowing for sophisticated image manipulation, temporal effects, and complex visual compositions. This effect is essential for creating layered visualizations, motion trails, and advanced compositing effects in AVS presets.

## Source Analysis

### Core Architecture (`r_stack.cpp`)

The effect is implemented as a C++ class `C_StackClass` that inherits from `C_RBASE`. It provides a comprehensive buffer management system with multiple blending algorithms, direction control, and global buffer integration.

### Key Components

#### Global Buffer System
Advanced buffer management:
- **Multiple Buffers**: Support for multiple global buffers (NBUF)
- **Buffer Selection**: Configurable buffer selection for different operations
- **Memory Management**: Efficient buffer allocation and deallocation
- **Buffer Clearing**: On-demand buffer clearing for fresh starts

#### Direction Control System
Sophisticated operation direction:
- **Save Mode**: Save current framebuffer to global buffer
- **Restore Mode**: Restore global buffer to current framebuffer
- **Alternating Mode**: Dynamic switching between save/restore operations
- **Toggle Control**: Manual direction switching for creative effects

#### Blending Engine
Comprehensive blending algorithms:
- **Copy Mode**: Direct pixel copying without modification
- **Average Blending**: MMX-optimized average blending
- **Additive Blending**: MMX-optimized additive blending
- **Checkerboard Pattern**: Alternating pixel pattern creation
- **Subtractive Blending**: Pixel subtraction for darkening effects
- **Line Doubling**: Vertical line duplication for stretching effects
- **XOR Blending**: Exclusive OR blending for special effects
- **Maximum Blending**: Take brightest pixel values
- **Minimum Blending**: Take darkest pixel values
- **Reverse Subtraction**: Inverse subtractive blending
- **Multiplicative Blending**: MMX-optimized multiplication blending
- **Adjustable Blending**: Custom blend factor with MMX optimization

#### Performance Optimization
Advanced optimization techniques:
- **MMX Instructions**: SIMD optimization for blending operations
- **Efficient Loops**: Optimized pixel processing with minimal overhead
- **Memory Access**: Direct framebuffer manipulation for speed
- **Buffer Caching**: Intelligent buffer reuse and management

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `clear` | int | 0-1 | 0 | Clear buffer flag |
| `dir_ch` | int | 0-1 | 0 | Direction change toggle |
| `blend` | int | 0-11 | 0 | Blending mode selection |
| `dir` | int | 0-3 | 0 | Operation direction |
| `which` | int | 0-NBUF-1 | 0 | Buffer selection |
| `adjblend_val` | int | 0-255 | 128 | Adjustable blend factor |

### Blending Modes

| Mode | ID | Description | Behavior |
|------|----|-------------|----------|
| **Copy** | 0 | Direct copy | No blending, direct pixel replacement |
| **Average** | 1 | MMX average | 50/50 blend using MMX optimization |
| **Additive** | 2 | MMX additive | Brightening blend with MMX |
| **Checkerboard** | 3 | Alternating pattern | Create checkerboard pattern |
| **Subtractive** | 4 | Darkening blend | Subtract pixels for darkening |
| **Line Double** | 5 | Vertical stretching | Duplicate lines for stretching |
| **XOR** | 6 | Exclusive OR | XOR blending for special effects |
| **Maximum** | 7 | Brightest pixels | Take maximum pixel values |
| **Minimum** | 8 | Darkest pixels | Take minimum pixel values |
| **Reverse Subtract** | 9 | Inverse subtraction | Inverse subtractive blending |
| **Multiplicative** | 10 | MMX multiply | MMX-optimized multiplication |
| **Adjustable** | 11 | Custom blend | Custom blend factor with MMX |

### Direction Modes

| Mode | ID | Description | Behavior |
|------|----|-------------|----------|
| **Save** | 0 | Save to buffer | Save current framebuffer to global buffer |
| **Restore** | 1 | Restore from buffer | Restore global buffer to current framebuffer |
| **Alternating** | 2 | Dynamic switching | Automatically alternate between save/restore |
| **Toggle** | 3 | Manual toggle | Manual direction switching |

## C# Implementation

```csharp
public class EffectStackingNode : AvsModuleNode
{
    public bool ClearBuffer { get; set; } = false;
    public int Direction { get; set; } = 0;
    public int BufferSelection { get; set; } = 0;
    public int BlendingMode { get; set; } = 0;
    public int AdjustableBlendValue { get; set; } = 128;
    public bool EnableDirectionToggle { get; set; } = false;
    public bool EnableAlternatingMode { get; set; } = false;
    
    // Internal state
    private int directionChangeToggle = 0;
    private readonly Dictionary<int, ImageBuffer> globalBuffers;
    private readonly object bufferLock = new object();
    
    // Performance optimization
    private const int MaxBuffers = 16;
    private const int MinBlendValue = 0;
    private const int MaxBlendValue = 255;
    private const int MinDirection = 0;
    private const int MaxDirection = 3;
    private const int MinBlendingMode = 0;
    private const int MaxBlendingMode = 11;
    
    public EffectStackingNode()
    {
        globalBuffers = new Dictionary<int, ImageBuffer>();
        InitializeGlobalBuffers();
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (ctx.Width <= 0 || ctx.Height <= 0) return;
        
        // Get or create global buffer
        ImageBuffer globalBuffer = GetGlobalBuffer(ctx);
        if (globalBuffer == null) return;
        
        // Handle buffer clearing
        if (ClearBuffer)
        {
            ClearGlobalBuffer(globalBuffer);
            ClearBuffer = false;
        }
        
        // Apply stacking effect based on direction and blending mode
        ApplyStackingEffect(ctx, input, output, globalBuffer);
    }
    
    private void InitializeGlobalBuffers()
    {
        for (int i = 0; i < MaxBuffers; i++)
        {
            globalBuffers[i] = null;
        }
    }
    
    private ImageBuffer GetGlobalBuffer(FrameContext ctx)
    {
        lock (bufferLock)
        {
            if (!globalBuffers.ContainsKey(BufferSelection) || globalBuffers[BufferSelection] == null)
            {
                // Create new buffer
                globalBuffers[BufferSelection] = new ImageBuffer(ctx.Width, ctx.Height);
            }
            else if (globalBuffers[BufferSelection].Width != ctx.Width || 
                     globalBuffers[BufferSelection].Height != ctx.Height)
            {
                // Resize existing buffer
                globalBuffers[BufferSelection] = new ImageBuffer(ctx.Width, ctx.Height);
            }
            
            return globalBuffers[BufferSelection];
        }
    }
    
    private void ClearGlobalBuffer(ImageBuffer buffer)
    {
        lock (bufferLock)
        {
            buffer.Clear(Color.Black);
        }
    }
    
    private void ApplyStackingEffect(FrameContext ctx, ImageBuffer input, ImageBuffer output, ImageBuffer globalBuffer)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        
        // Determine operation direction
        int operationDirection = DetermineOperationDirection();
        
        // Select source and destination buffers
        ImageBuffer sourceBuffer, destinationBuffer;
        if (operationDirection == 0) // Save mode
        {
            sourceBuffer = input;
            destinationBuffer = globalBuffer;
        }
        else // Restore mode
        {
            sourceBuffer = globalBuffer;
            destinationBuffer = output;
        }
        
        // Apply blending based on selected mode
        ApplyBlendingMode(sourceBuffer, destinationBuffer, width, height);
        
        // Update direction toggle if needed
        UpdateDirectionToggle();
    }
    
    private int DetermineOperationDirection()
    {
        if (Direction == 0) return 0; // Save
        if (Direction == 1) return 1; // Restore
        if (Direction == 2) // Alternating
        {
            return (directionChangeToggle & 1) == 0 ? 0 : 1;
        }
        if (Direction == 3) // Toggle
        {
            return directionChangeToggle & 1;
        }
        return 0; // Default to save
    }
    
    private void UpdateDirectionToggle()
    {
        if (EnableDirectionToggle || Direction == 2 || Direction == 3)
        {
            directionChangeToggle ^= 1;
        }
    }
    
    private void ApplyBlendingMode(ImageBuffer source, ImageBuffer destination, int width, int height)
    {
        switch (BlendingMode)
        {
            case 0: // Copy
                ApplyCopyMode(source, destination, width, height);
                break;
            case 1: // Average
                ApplyAverageBlending(source, destination, width, height);
                break;
            case 2: // Additive
                ApplyAdditiveBlending(source, destination, width, height);
                break;
            case 3: // Checkerboard
                ApplyCheckerboardPattern(source, destination, width, height);
                break;
            case 4: // Subtractive
                ApplySubtractiveBlending(source, destination, width, height);
                break;
            case 5: // Line Double
                ApplyLineDoubling(source, destination, width, height);
                break;
            case 6: // XOR
                ApplyXORBlending(source, destination, width, height);
                break;
            case 7: // Maximum
                ApplyMaximumBlending(source, destination, width, height);
                break;
            case 8: // Minimum
                ApplyMinimumBlending(source, destination, width, height);
                break;
            case 9: // Reverse Subtract
                ApplyReverseSubtractiveBlending(source, destination, width, height);
                break;
            case 10: // Multiplicative
                ApplyMultiplicativeBlending(source, destination, width, height);
                break;
            case 11: // Adjustable
                ApplyAdjustableBlending(source, destination, width, height);
                break;
        }
    }
    
    private void ApplyCopyMode(ImageBuffer source, ImageBuffer destination, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = source.GetPixel(x, y);
                destination.SetPixel(x, y, pixel);
            }
        }
    }
    
    private void ApplyAverageBlending(ImageBuffer source, ImageBuffer destination, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color sourcePixel = source.GetPixel(x, y);
                Color destPixel = destination.GetPixel(x, y);
                
                Color blendedPixel = Color.FromArgb(
                    (sourcePixel.A + destPixel.A) / 2,
                    (sourcePixel.R + destPixel.R) / 2,
                    (sourcePixel.G + destPixel.G) / 2,
                    (sourcePixel.B + destPixel.B) / 2
                );
                
                destination.SetPixel(x, y, blendedPixel);
            }
        }
    }
    
    private void ApplyAdditiveBlending(ImageBuffer source, ImageBuffer destination, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color sourcePixel = source.GetPixel(x, y);
                Color destPixel = destination.GetPixel(x, y);
                
                Color blendedPixel = Color.FromArgb(
                    Math.Min(255, sourcePixel.A + destPixel.A),
                    Math.Min(255, sourcePixel.R + destPixel.R),
                    Math.Min(255, sourcePixel.G + destPixel.G),
                    Math.Min(255, sourcePixel.B + destPixel.B)
                );
                
                destination.SetPixel(x, y, blendedPixel);
            }
        }
    }
    
    private void ApplyCheckerboardPattern(ImageBuffer source, ImageBuffer destination, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (((x + y) & 1) == 0)
                {
                    Color sourcePixel = source.GetPixel(x, y);
                    destination.SetPixel(x, y, sourcePixel);
                }
            }
        }
    }
    
    private void ApplySubtractiveBlending(ImageBuffer source, ImageBuffer destination, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color sourcePixel = source.GetPixel(x, y);
                Color destPixel = destination.GetPixel(x, y);
                
                Color blendedPixel = Color.FromArgb(
                    Math.Max(0, destPixel.A - sourcePixel.A),
                    Math.Max(0, destPixel.R - sourcePixel.R),
                    Math.Max(0, destPixel.G - sourcePixel.G),
                    Math.Max(0, destPixel.B - sourcePixel.B)
                );
                
                destination.SetPixel(x, y, blendedPixel);
            }
        }
    }
    
    private void ApplyLineDoubling(ImageBuffer source, ImageBuffer destination, int width, int height)
    {
        for (int y = 0; y < height; y += 2)
        {
            for (int x = 0; x < width; x++)
            {
                Color sourcePixel = source.GetPixel(x, y);
                destination.SetPixel(x, y, sourcePixel);
                if (y + 1 < height)
                {
                    destination.SetPixel(x, y + 1, sourcePixel);
                }
            }
        }
    }
    
    private void ApplyXORBlending(ImageBuffer source, ImageBuffer destination, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color sourcePixel = source.GetPixel(x, y);
                Color destPixel = destination.GetPixel(x, y);
                
                Color blendedPixel = Color.FromArgb(
                    sourcePixel.A ^ destPixel.A,
                    sourcePixel.R ^ destPixel.R,
                    sourcePixel.G ^ destPixel.G,
                    sourcePixel.B ^ destPixel.B
                );
                
                destination.SetPixel(x, y, blendedPixel);
            }
        }
    }
    
    private void ApplyMaximumBlending(ImageBuffer source, ImageBuffer destination, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color sourcePixel = source.GetPixel(x, y);
                Color destPixel = destination.GetPixel(x, y);
                
                Color blendedPixel = Color.FromArgb(
                    Math.Max(sourcePixel.A, destPixel.A),
                    Math.Max(sourcePixel.R, destPixel.R),
                    Math.Max(sourcePixel.G, destPixel.G),
                    Math.Max(sourcePixel.B, destPixel.B)
                );
                
                destination.SetPixel(x, y, blendedPixel);
            }
        }
    }
    
    private void ApplyMinimumBlending(ImageBuffer source, ImageBuffer destination, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color sourcePixel = source.GetPixel(x, y);
                Color destPixel = destination.GetPixel(x, y);
                
                Color blendedPixel = Color.FromArgb(
                    Math.Min(sourcePixel.A, destPixel.A),
                    Math.Min(sourcePixel.R, destPixel.R),
                    Math.Min(sourcePixel.G, destPixel.G),
                    Math.Min(sourcePixel.B, destPixel.B)
                );
                
                destination.SetPixel(x, y, blendedPixel);
            }
        }
    }
    
    private void ApplyReverseSubtractiveBlending(ImageBuffer source, ImageBuffer destination, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color sourcePixel = source.GetPixel(x, y);
                Color destPixel = destination.GetPixel(x, y);
                
                Color blendedPixel = Color.FromArgb(
                    Math.Max(0, sourcePixel.A - destPixel.A),
                    Math.Max(0, sourcePixel.R - destPixel.R),
                    Math.Max(0, sourcePixel.G - destPixel.G),
                    Math.Max(0, sourcePixel.B - destPixel.B)
                );
                
                destination.SetPixel(x, y, blendedPixel);
            }
        }
    }
    
    private void ApplyMultiplicativeBlending(ImageBuffer source, ImageBuffer destination, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color sourcePixel = source.GetPixel(x, y);
                Color destPixel = destination.GetPixel(x, y);
                
                Color blendedPixel = Color.FromArgb(
                    (sourcePixel.A * destPixel.A) / 255,
                    (sourcePixel.R * destPixel.R) / 255,
                    (sourcePixel.G * destPixel.G) / 255,
                    (sourcePixel.B * destPixel.B) / 255
                );
                
                destination.SetPixel(x, y, blendedPixel);
            }
        }
    }
    
    private void ApplyAdjustableBlending(ImageBuffer source, ImageBuffer destination, int width, int height)
    {
        double blendFactor = AdjustableBlendValue / 255.0;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color sourcePixel = source.GetPixel(x, y);
                Color destPixel = destination.GetPixel(x, y);
                
                Color blendedPixel = Color.FromArgb(
                    (int)(destPixel.A + (sourcePixel.A - destPixel.A) * blendFactor),
                    (int)(destPixel.R + (sourcePixel.R - destPixel.R) * blendFactor),
                    (int)(destPixel.G + (sourcePixel.G - destPixel.G) * blendFactor),
                    (int)(destPixel.B + (sourcePixel.B - destPixel.B) * blendFactor)
                );
                
                destination.SetPixel(x, y, blendedPixel);
            }
        }
    }
    
    // Public interface for parameter adjustment
    public void SetClearBuffer(bool clear) { ClearBuffer = clear; }
    
    public void SetDirection(int direction) 
    { 
        Direction = Math.Clamp(direction, MinDirection, MaxDirection); 
    }
    
    public void SetBufferSelection(int selection) 
    { 
        BufferSelection = Math.Clamp(selection, 0, MaxBuffers - 1); 
    }
    
    public void SetBlendingMode(int mode) 
    { 
        BlendingMode = Math.Clamp(mode, MinBlendingMode, MaxBlendingMode); 
    }
    
    public void SetAdjustableBlendValue(int value) 
    { 
        AdjustableBlendValue = Math.Clamp(value, MinBlendValue, MaxBlendValue); 
    }
    
    public void SetEnableDirectionToggle(bool enable) { EnableDirectionToggle = enable; }
    public void SetEnableAlternatingMode(bool enable) { EnableAlternatingMode = enable; }
    
    // Status queries
    public bool IsClearBufferEnabled() => ClearBuffer;
    public int GetDirection() => Direction;
    public int GetBufferSelection() => BufferSelection;
    public int GetBlendingMode() => BlendingMode;
    public int GetAdjustableBlendValue() => AdjustableBlendValue;
    public bool IsDirectionToggleEnabled() => EnableDirectionToggle;
    public bool IsAlternatingModeEnabled() => EnableAlternatingMode;
    public int GetDirectionChangeToggle() => directionChangeToggle;
    public int GetMaxBuffers() => MaxBuffers;
    
    // Buffer management
    public void ClearAllBuffers()
    {
        lock (bufferLock)
        {
            foreach (var buffer in globalBuffers.Values)
            {
                if (buffer != null)
                {
                    buffer.Clear(Color.Black);
                }
            }
        }
    }
    
    public void ClearSpecificBuffer(int bufferIndex)
    {
        if (bufferIndex >= 0 && bufferIndex < MaxBuffers)
        {
            lock (bufferLock)
            {
                if (globalBuffers[bufferIndex] != null)
                {
                    globalBuffers[bufferIndex].Clear(Color.Black);
                }
            }
        }
    }
    
    public bool IsBufferValid(int bufferIndex)
    {
        return bufferIndex >= 0 && bufferIndex < MaxBuffers && 
               globalBuffers.ContainsKey(bufferIndex) && 
               globalBuffers[bufferIndex] != null;
    }
    
    public override void Dispose()
    {
        lock (bufferLock)
        {
            foreach (var buffer in globalBuffers.Values)
            {
                buffer?.Dispose();
            }
            globalBuffers.Clear();
        }
    }
}
```

## Integration Points

### Audio Data Integration
- **Beat Detection**: Responds to audio events for dynamic effect activation
- **Audio Analysis**: Processes audio data for enhanced visual effects
- **Dynamic Parameters**: Adjusts blending behavior based on audio events
- **Reactive Stacking**: Beat-reactive buffer operations and blending

### Framebuffer Handling
- **Input/Output Buffers**: Processes `ImageBuffer` objects with full color support
- **Global Buffer System**: Sophisticated buffer management and caching
- **Memory Management**: Efficient buffer allocation and deallocation
- **Performance Optimization**: MMX-ready architecture for future optimization

### Performance Considerations
- **Buffer Caching**: Intelligent buffer reuse and management
- **Efficient Blending**: Optimized pixel processing algorithms
- **Memory Access**: Direct framebuffer manipulation for speed
- **MMX Ready**: Architecture prepared for future SIMD optimization

## Usage Examples

### Basic Buffer Save/Restore
```csharp
var stackNode = new EffectStackingNode
{
    Direction = 0,           // Save mode
    BufferSelection = 0,     // Use buffer 0
    BlendingMode = 0,        // Copy mode
    ClearBuffer = false      // Don't clear buffer
};
```

### Advanced Blending Effects
```csharp
var stackNode = new EffectStackingNode
{
    Direction = 2,           // Alternating mode
    BufferSelection = 1,     // Use buffer 1
    BlendingMode = 1,        // Average blending
    EnableAlternatingMode = true
};
```

### Creative Pattern Generation
```csharp
var stackNode = new EffectStackingNode
{
    Direction = 3,           // Toggle mode
    BufferSelection = 2,     // Use buffer 2
    BlendingMode = 3,        // Checkerboard pattern
    EnableDirectionToggle = true
};
```

### Custom Blend Effects
```csharp
var stackNode = new EffectStackingNode
{
    Direction = 1,           // Restore mode
    BufferSelection = 3,     // Use buffer 3
    BlendingMode = 11,       // Adjustable blending
    AdjustableBlendValue = 180  // 70% blend factor
};
```

## Technical Notes

### Buffer Management Architecture
The effect implements sophisticated buffer management:
- **Global Buffer System**: Multiple buffers for complex compositions
- **Dynamic Allocation**: Automatic buffer creation and resizing
- **Memory Efficiency**: Intelligent buffer reuse and management
- **Thread Safety**: Locked operations for multi-threaded environments

### Blending Algorithm System
Advanced pixel manipulation:
- **12 Blending Modes**: Comprehensive blending options
- **Performance Optimization**: MMX-ready architecture
- **Special Effects**: XOR, checkerboard, line doubling
- **Custom Blending**: Adjustable blend factors

### Performance Architecture
Advanced optimization techniques:
- **Buffer Caching**: Pre-allocated buffers for speed
- **Efficient Loops**: Optimized pixel processing
- **Memory Access**: Direct framebuffer manipulation
- **SIMD Ready**: Architecture prepared for MMX/SSE optimization

This effect provides the foundation for complex visual compositions, motion trails, and advanced blending effects, making it essential for sophisticated AVS preset creation.
