# Line Drawing Modes (Misc / Set render mode)

## Overview

The **Line Drawing Modes** system is a sophisticated rendering mode control engine that manages global line blending modes and alpha controls for the entire AVS visualization system. It implements a comprehensive blend mode management system with 10 different blending algorithms, configurable alpha controls, and global rendering state management. This effect serves as a central control point for all line-based rendering operations, allowing dynamic switching between different visual blending algorithms.

## Source Analysis

### Core Architecture (`r_linemode.cpp`)

The effect is implemented as a C++ class `C_LineModeClass` that inherits from `C_RBASE`. It provides a comprehensive rendering mode management system with global blend mode control, alpha parameter management, and centralized rendering state configuration for all line-based visualization operations.

### Key Components

#### Global Blend Mode Management
Advanced blend mode control system:
- **Global State Control**: Centralized blend mode management for all line operations
- **Mode Persistence**: Persistent blend mode settings across rendering cycles
- **Dynamic Switching**: Real-time blend mode switching and configuration
- **System Integration**: Deep integration with the AVS rendering pipeline

#### Blend Mode Library
Comprehensive blending algorithm collection:
- **10 Blend Modes**: Complete set of blending algorithms for different visual effects
- **Mode Descriptions**: Clear naming and categorization of blend behaviors
- **Algorithm Selection**: Intelligent blend mode selection and application
- **Performance Optimization**: Optimized blend mode implementations

#### Alpha Control System
Advanced alpha parameter management:
- **Alpha Slider**: Configurable alpha value control (0-255)
- **Dynamic Alpha**: Real-time alpha value adjustment
- **Mode-Specific Alpha**: Alpha controls specific to certain blend modes
- **Alpha Persistence**: Persistent alpha settings across rendering cycles

#### Configuration Management
Sophisticated parameter control:
- **Parameter Encoding**: Compact parameter encoding for efficient storage
- **Configuration Persistence**: Persistent configuration across sessions
- **Dynamic Updates**: Real-time parameter updates and validation
- **State Synchronization**: Synchronized state across all rendering components

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `newmode` | int | Complex encoding | 0x80010000 | Encoded blend mode and settings |
| `enabled` | bool | 0-1 | true | Enable line mode control |
| `blendMode` | int | 0-9 | 0 | Selected blend mode index |
| `alpha` | int | 0-255 | 0 | Alpha blending value |
| `outputLevel` | int | 0-255 | 0 | Output level for adjustable blend |

### Blend Mode Encoding

The `newmode` parameter uses a complex bit-encoded format:
- **Bits 0-7**: Blend mode index (0-9)
- **Bits 8-15**: Alpha value (0-255)
- **Bits 16-23**: Output level (0-255)
- **Bit 31**: Enable flag (1 = enabled, 0 = disabled)

### Blend Modes

| Mode | Index | Description | Behavior |
|------|-------|-------------|----------|
| **Replace** | 0 | Direct replacement | No blending, pure line output |
| **Additive** | 1 | Brightness increase | Adds line brightness to background |
| **Maximum Blend** | 2 | Maximum value selection | Selects maximum of line and background |
| **50/50 Blend** | 3 | Average blending | Smooth transition between line and background |
| **Subtractive Blend 1** | 4 | First subtractive algorithm | Subtracts line from background |
| **Subtractive Blend 2** | 5 | Second subtractive algorithm | Alternative subtractive blending |
| **Multiply Blend** | 6 | Multiplicative blending | Multiplies line and background values |
| **Adjustable Blend** | 7 | Configurable blending | User-adjustable blend with alpha control |
| **XOR** | 8 | Exclusive OR blending | XOR operation between line and background |
| **Minimum Blend** | 9 | Minimum value selection | Selects minimum of line and background |

## C# Implementation

```csharp
public class LineDrawingModesNode : AvsModuleNode
{
    public bool Enabled { get; set; } = true;
    public int BlendMode { get; set; } = 0;
    public int Alpha { get; set; } = 0;
    public int OutputLevel { get; set; } = 0;
    
    // Internal state
    private int encodedMode;
    private int lastWidth, lastHeight;
    private readonly object renderLock = new object();
    
    // Performance optimization
    private const int MaxBlendMode = 9;
    private const int MinBlendMode = 0;
    private const int MaxAlpha = 255;
    private const int MinAlpha = 0;
    private const int MaxOutputLevel = 255;
    private const int MinOutputLevel = 0;
    private const int EnableFlag = 0x80000000;
    private const int BlendModeMask = 0x000000FF;
    private const int AlphaMask = 0x0000FF00;
    private const int OutputLevelMask = 0x00FF0000;
    
    // Blend mode names
    private static readonly string[] BlendModeNames = new string[]
    {
        "Replace",
        "Additive",
        "Maximum Blend",
        "50/50 Blend",
        "Subtractive Blend 1",
        "Subtractive Blend 2",
        "Multiply Blend",
        "Adjustable Blend",
        "XOR",
        "Minimum Blend"
    };
    
    public LineDrawingModesNode()
    {
        encodedMode = EnableFlag | (BlendMode & BlendModeMask) | 
                     ((Alpha & 0xFF) << 8) | ((OutputLevel & 0xFF) << 16);
        lastWidth = lastHeight = 0;
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled || ctx.Width <= 0 || ctx.Height <= 0) return;
        
        lock (renderLock)
        {
            // Update buffers if dimensions changed
            UpdateBuffers(ctx);
            
            // Update global line blend mode
            UpdateGlobalLineBlendMode();
            
            // Copy input to output (this effect only controls global state)
            CopyInputToOutput(ctx, input, output);
        }
    }
    
    private void UpdateBuffers(FrameContext ctx)
    {
        if (lastWidth != ctx.Width || lastHeight != ctx.Height)
        {
            lastWidth = ctx.Width;
            lastHeight = ctx.Height;
        }
    }
    
    private void UpdateGlobalLineBlendMode()
    {
        if (Enabled)
        {
            // Update the global line blend mode for all line operations
            GlobalLineBlendMode.SetCurrentMode(BlendMode, Alpha, OutputLevel);
        }
    }
    
    private void CopyInputToOutput(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        // This effect only controls global state, so we copy input to output
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                output.SetPixel(x, y, input.GetPixel(x, y));
            }
        }
    }
    
    // Public interface for parameter adjustment
    public void SetEnabled(bool enable) 
    { 
        Enabled = enable; 
        UpdateEncodedMode();
    }
    
    public void SetBlendMode(int mode) 
    { 
        BlendMode = Math.Clamp(mode, MinBlendMode, MaxBlendMode); 
        UpdateEncodedMode();
    }
    
    public void SetAlpha(int alpha) 
    { 
        Alpha = Math.Clamp(alpha, MinAlpha, MaxAlpha); 
        UpdateEncodedMode();
    }
    
    public void SetOutputLevel(int level) 
    { 
        OutputLevel = Math.Clamp(level, MinOutputLevel, MaxOutputLevel); 
        UpdateEncodedMode();
    }
    
    // Status queries
    public bool IsEnabled() => Enabled;
    public int GetBlendMode() => BlendMode;
    public int GetAlpha() => Alpha;
    public int GetOutputLevel() => OutputLevel;
    public int GetEncodedMode() => encodedMode;
    public string GetBlendModeName() => BlendModeNames[BlendMode];
    
    // Advanced blend mode control
    public void SetReplaceMode()
    {
        SetBlendMode(0);
    }
    
    public void SetAdditiveMode()
    {
        SetBlendMode(1);
    }
    
    public void SetMaximumBlendMode()
    {
        SetBlendMode(2);
    }
    
    public void SetFiftyFiftyBlendMode()
    {
        SetBlendMode(3);
    }
    
    public void SetSubtractiveBlend1Mode()
    {
        SetBlendMode(4);
    }
    
    public void SetSubtractiveBlend2Mode()
    {
        SetBlendMode(5);
    }
    
    public void SetMultiplyBlendMode()
    {
        SetBlendMode(6);
    }
    
    public void SetAdjustableBlendMode()
    {
        SetBlendMode(7);
    }
    
    public void SetXORMode()
    {
        SetBlendMode(8);
    }
    
    public void SetMinimumBlendMode()
    {
        SetBlendMode(9);
    }
    
    // Blend mode variations
    public void SetTransparentMode(int alpha)
    {
        SetBlendMode(7); // Adjustable Blend
        SetAlpha(alpha);
    }
    
    public void SetBrightMode(int alpha)
    {
        SetBlendMode(1); // Additive
        SetAlpha(alpha);
    }
    
    public void SetDarkMode(int alpha)
    {
        SetBlendMode(4); // Subtractive Blend 1
        SetAlpha(alpha);
    }
    
    public void SetContrastMode(int alpha)
    {
        SetBlendMode(2); // Maximum Blend
        SetAlpha(alpha);
    }
    
    // Configuration management
    public void LoadConfiguration(int encodedMode)
    {
        this.encodedMode = encodedMode;
        
        // Extract parameters from encoded mode
        Enabled = (encodedMode & EnableFlag) != 0;
        BlendMode = encodedMode & BlendModeMask;
        Alpha = (encodedMode & AlphaMask) >> 8;
        OutputLevel = (encodedMode & OutputLevelMask) >> 16;
        
        // Validate parameters
        BlendMode = Math.Clamp(BlendMode, MinBlendMode, MaxBlendMode);
        Alpha = Math.Clamp(Alpha, MinAlpha, MaxAlpha);
        OutputLevel = Math.Clamp(OutputLevel, MinOutputLevel, MaxOutputLevel);
    }
    
    public int SaveConfiguration()
    {
        UpdateEncodedMode();
        return encodedMode;
    }
    
    private void UpdateEncodedMode()
    {
        encodedMode = (Enabled ? EnableFlag : 0) | 
                     (BlendMode & BlendModeMask) | 
                     ((Alpha & 0xFF) << 8) | 
                     ((OutputLevel & 0xFF) << 16);
    }
    
    // Blend mode information
    public string[] GetAvailableBlendModes()
    {
        return (string[])BlendModeNames.Clone();
    }
    
    public string GetBlendModeDescription(int mode)
    {
        if (mode >= 0 && mode < BlendModeNames.Length)
        {
            return BlendModeNames[mode];
        }
        return "Unknown";
    }
    
    public bool IsBlendModeAvailable(int mode)
    {
        return mode >= MinBlendMode && mode <= MaxBlendMode;
    }
    
    // Performance optimization
    public void SetRenderQuality(int quality)
    {
        // Quality could affect blend mode optimization level
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
        }
    }
}

// Global line blend mode manager
public static class GlobalLineBlendMode
{
    private static int currentBlendMode = 0;
    private static int currentAlpha = 0;
    private static int currentOutputLevel = 0;
    private static readonly object modeLock = new object();
    
    public static void SetCurrentMode(int blendMode, int alpha, int outputLevel)
    {
        lock (modeLock)
        {
            currentBlendMode = blendMode;
            currentAlpha = alpha;
            currentOutputLevel = outputLevel;
        }
    }
    
    public static int GetCurrentBlendMode()
    {
        lock (modeLock)
        {
            return currentBlendMode;
        }
    }
    
    public static int GetCurrentAlpha()
    {
        lock (modeLock)
        {
            return currentAlpha;
        }
    }
    
    public static int GetCurrentOutputLevel()
    {
        lock (modeLock)
        {
            return currentOutputLevel;
        }
    }
    
    public static Color ApplyBlendMode(Color existingColor, Color newColor)
    {
        lock (modeLock)
        {
            switch (currentBlendMode)
            {
                case 0: // Replace
                    return newColor;
                    
                case 1: // Additive
                    return ApplyAdditiveBlending(existingColor, newColor, currentAlpha);
                    
                case 2: // Maximum Blend
                    return ApplyMaximumBlending(existingColor, newColor, currentAlpha);
                    
                case 3: // 50/50 Blend
                    return ApplyFiftyFiftyBlending(existingColor, newColor, currentAlpha);
                    
                case 4: // Subtractive Blend 1
                    return ApplySubtractiveBlending1(existingColor, newColor, currentAlpha);
                    
                case 5: // Subtractive Blend 2
                    return ApplySubtractiveBlending2(existingColor, newColor, currentAlpha);
                    
                case 6: // Multiply Blend
                    return ApplyMultiplyBlending(existingColor, newColor, currentAlpha);
                    
                case 7: // Adjustable Blend
                    return ApplyAdjustableBlending(existingColor, newColor, currentAlpha, currentOutputLevel);
                    
                case 8: // XOR
                    return ApplyXORBlending(existingColor, newColor, currentAlpha);
                    
                case 9: // Minimum Blend
                    return ApplyMinimumBlending(existingColor, newColor, currentAlpha);
                    
                default:
                    return newColor;
            }
        }
    }
    
    private static Color ApplyAdditiveBlending(Color existingColor, Color newColor, int alpha)
    {
        float alphaFactor = alpha / 255.0f;
        int r = Math.Min(255, existingColor.R + (int)(newColor.R * alphaFactor));
        int g = Math.Min(255, existingColor.G + (int)(newColor.G * alphaFactor));
        int b = Math.Min(255, existingColor.B + (int)(newColor.B * alphaFactor));
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private static Color ApplyMaximumBlending(Color existingColor, Color newColor, int alpha)
    {
        float alphaFactor = alpha / 255.0f;
        int r = Math.Max(existingColor.R, (int)(newColor.R * alphaFactor));
        int g = Math.Max(existingColor.G, (int)(newColor.G * alphaFactor));
        int b = Math.Max(existingColor.B, (int)(newColor.B * alphaFactor));
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private static Color ApplyFiftyFiftyBlending(Color existingColor, Color newColor, int alpha)
    {
        float alphaFactor = alpha / 255.0f;
        int r = (int)((existingColor.R * (1.0f - alphaFactor)) + (newColor.R * alphaFactor));
        int g = (int)((existingColor.G * (1.0f - alphaFactor)) + (newColor.G * alphaFactor));
        int b = (int)((existingColor.B * (1.0f - alphaFactor)) + (newColor.B * alphaFactor));
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private static Color ApplySubtractiveBlending1(Color existingColor, Color newColor, int alpha)
    {
        float alphaFactor = alpha / 255.0f;
        int r = Math.Max(0, existingColor.R - (int)(newColor.R * alphaFactor));
        int g = Math.Max(0, existingColor.G - (int)(newColor.G * alphaFactor));
        int b = Math.Max(0, existingColor.B - (int)(newColor.B * alphaFactor));
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private static Color ApplySubtractiveBlending2(Color existingColor, Color newColor, int alpha)
    {
        float alphaFactor = alpha / 255.0f;
        int r = Math.Max(0, (int)(existingColor.R * (1.0f - alphaFactor)) - newColor.R);
        int g = Math.Max(0, (int)(existingColor.G * (1.0f - alphaFactor)) - newColor.G);
        int b = Math.Max(0, (int)(existingColor.B * (1.0f - alphaFactor)) - newColor.B);
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private static Color ApplyMultiplyBlending(Color existingColor, Color newColor, int alpha)
    {
        float alphaFactor = alpha / 255.0f;
        int r = (int)((existingColor.R * newColor.R * alphaFactor) / 255.0f);
        int g = (int)((existingColor.G * newColor.G * alphaFactor) / 255.0f);
        int b = (int)((existingColor.B * newColor.B * alphaFactor) / 255.0f);
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private static Color ApplyAdjustableBlending(Color existingColor, Color newColor, int alpha, int outputLevel)
    {
        float alphaFactor = alpha / 255.0f;
        float outputFactor = outputLevel / 255.0f;
        
        int r = (int)((existingColor.R * (1.0f - alphaFactor)) + (newColor.R * alphaFactor * outputFactor));
        int g = (int)((existingColor.G * (1.0f - alphaFactor)) + (newColor.G * alphaFactor * outputFactor));
        int b = (int)((existingColor.B * (1.0f - alphaFactor)) + (newColor.B * alphaFactor * outputFactor));
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private static Color ApplyXORBlending(Color existingColor, Color newColor, int alpha)
    {
        float alphaFactor = alpha / 255.0f;
        int r = (int)(existingColor.R ^ (newColor.R * alphaFactor));
        int g = (int)(existingColor.G ^ (newColor.G * alphaFactor));
        int b = (int)(existingColor.B ^ (newColor.B * alphaFactor));
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private static Color ApplyMinimumBlending(Color existingColor, Color newColor, int alpha)
    {
        float alphaFactor = alpha / 255.0f;
        int r = Math.Min(existingColor.R, (int)(newColor.R * alphaFactor));
        int g = Math.Min(existingColor.G, (int)(newColor.G * alphaFactor));
        int b = Math.Min(existingColor.B, (int)(newColor.B * alphaFactor));
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
}
```

## Integration Points

### Global Rendering Control
- **System-Wide Control**: Centralized blend mode management for all line operations
- **State Persistence**: Persistent blend mode settings across rendering cycles
- **Dynamic Switching**: Real-time blend mode switching and configuration
- **Deep Integration**: Integration with the entire AVS rendering pipeline

### Line Rendering Integration
- **Blend Mode Application**: Automatic application of selected blend modes to all line operations
- **Alpha Control**: Global alpha parameter management for line rendering
- **Performance Optimization**: Optimized blend mode implementations for line operations
- **State Synchronization**: Synchronized blend mode state across all line rendering components

### Configuration Management
- **Parameter Encoding**: Compact parameter encoding for efficient storage and transmission
- **Configuration Persistence**: Persistent configuration across sessions and rendering cycles
- **Dynamic Updates**: Real-time parameter updates with validation and bounds checking
- **State Management**: Comprehensive state management and synchronization

## Usage Examples

### Basic Line Mode Control
```csharp
var lineModeNode = new LineDrawingModesNode
{
    Enabled = true,
    BlendMode = 1,                    // Additive blending
    Alpha = 128,                      // Medium alpha
    OutputLevel = 255                 // Full output level
};
```

### Dynamic Blend Mode Switching
```csharp
var lineModeNode = new LineDrawingModesNode
{
    Enabled = true,
    BlendMode = 7,                    // Adjustable blend
    Alpha = 200,                      // High alpha
    OutputLevel = 180                 // Medium output level
};

// Switch to different modes
lineModeNode.SetAdditiveMode();       // Switch to additive
lineModeNode.SetAlpha(100);           // Reduce alpha
```

### Advanced Blend Mode Configuration
```csharp
var lineModeNode = new LineDrawingModesNode
{
    Enabled = true
};

// Configure for different visual effects
lineModeNode.SetTransparentMode(150); // Transparent effect
lineModeNode.SetBrightMode(200);      // Bright effect
lineModeNode.SetDarkMode(180);        // Dark effect
lineModeNode.SetContrastMode(220);    // High contrast
```

## Technical Notes

### Blend Mode Architecture
The effect implements sophisticated blend mode processing:
- **10 Blend Algorithms**: Complete set of blending algorithms for different visual effects
- **Global State Management**: Centralized blend mode state for all line operations
- **Dynamic Switching**: Real-time blend mode switching with parameter validation
- **Performance Optimization**: Optimized blend mode implementations for line rendering

### Parameter Architecture
Advanced parameter control system:
- **Bit-Encoded Parameters**: Compact parameter encoding for efficient storage
- **Parameter Validation**: Comprehensive parameter validation and bounds checking
- **Dynamic Updates**: Real-time parameter updates with state synchronization
- **Configuration Persistence**: Persistent configuration across rendering cycles

### Integration System
Sophisticated system integration:
- **Global State Control**: System-wide blend mode control and management
- **Deep Integration**: Integration with the entire AVS rendering pipeline
- **State Synchronization**: Synchronized state across all rendering components
- **Performance Optimization**: Optimized integration for minimal performance impact

This effect serves as a central control point for all line-based rendering operations, providing comprehensive blend mode management and global rendering state control for sophisticated AVS visualization systems.
