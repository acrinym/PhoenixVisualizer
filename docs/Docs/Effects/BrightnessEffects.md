# Brightness Effects (Trans / Brightness)

## Overview

The **Brightness Effects** system is a sophisticated color and brightness manipulation engine that provides advanced control over image brightness, color channels, and blending operations. It implements comprehensive brightness adjustment with multi-threaded processing, color dissociation, and intelligent blending modes for creating complex color transformations. This effect provides the foundation for sophisticated brightness control, color manipulation, and advanced image processing in AVS presets.

## Source Analysis

### Core Architecture (`r_bright.cpp`)

The effect is implemented as a C++ class `C_BrightnessClass` that inherits from `C_RBASE2`. It provides a comprehensive brightness and color manipulation system with multi-threaded processing, color dissociation, and intelligent blending modes for creating complex color transformations.

### Key Components

#### Brightness Processing Engine
Advanced brightness control system:
- **Channel Control**: Independent control of red, green, and blue channels
- **Brightness Adjustment**: Precise brightness control with configurable ranges
- **Color Dissociation**: Advanced color dissociation and manipulation
- **Performance Optimization**: Multi-threaded processing for real-time operations

#### Color Management System
Sophisticated color processing:
- **Color Tables**: Pre-calculated color transformation tables
- **Channel Separation**: Independent processing of RGB channels
- **Color Exclusion**: Configurable color exclusion and filtering
- **Distance Control**: Intelligent color distance and similarity control

#### Blending System
Advanced blending capabilities:
- **Multiple Blend Modes**: Replace, additive, and average blending
- **Blend Control**: Configurable blend intensity and behavior
- **Visual Integration**: Seamless integration with existing visual content
- **Quality Control**: High-quality blending algorithms

#### Multi-Threading System
Performance optimization:
- **SMP Support**: Symmetric Multi-Processing support for performance
- **Thread Management**: Intelligent thread distribution and management
- **Performance Scaling**: Dynamic performance scaling based on CPU cores
- **Synchronization**: Advanced thread synchronization and data sharing

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `enabled` | bool | true/false | true | Enable/disable brightness effect |
| `redp` | int | -255-255 | 0 | Red channel brightness adjustment |
| `greenp` | int | -255-255 | 0 | Green channel brightness adjustment |
| `bluep` | int | -255-255 | 0 | Blue channel brightness adjustment |
| `blend` | int | 0-1 | 0 | Blend mode (0=Replace, 1=Additive) |
| `blendavg` | int | 0-1 | 1 | Average blending mode |
| `dissoc` | int | 0-1 | 0 | Color dissociation mode |
| `color` | int | 0x000000-0xFFFFFF | 0 | Color exclusion value |
| `exclude` | int | 0-1 | 0 | Enable color exclusion |
| `distance` | int | 0-255 | 16 | Color distance threshold |

### Blending Modes

| Mode | Value | Description | Behavior |
|------|-------|-------------|----------|
| **Replace** | 0 | Replace mode | Brightness effect replaces underlying content |
| **Additive** | 1 | Additive blending | Brightness effect adds to underlying content |
| **Average** | 1 | Average blending | Brightness effect averages with underlying content |

### Color Dissociation Modes

| Mode | Value | Description | Behavior |
|------|-------|-------------|----------|
| **Normal** | 0 | Standard processing | Standard brightness and color processing |
| **Dissociated** | 1 | Color dissociation | Advanced color dissociation and manipulation |

## C# Implementation

```csharp
public class BrightnessEffectsNode : AvsModuleNode
{
    public bool Enabled { get; set; } = true;
    public int RedAdjustment { get; set; } = 0;
    public int GreenAdjustment { get; set; } = 0;
    public int BlueAdjustment { get; set; } = 0;
    public int Blend { get; set; } = 0;
    public int BlendAvg { get; set; } = 1;
    public int Dissoc { get; set; } = 0;
    public int Color { get; set; } = 0;
    public int Exclude { get; set; } = 0;
    public int Distance { get; set; } = 16;
    
    // Internal state
    private int[] redTable;
    private int[] greenTable;
    private int[] blueTable;
    private bool tablesNeedInit;
    private int lastWidth, lastHeight;
    private readonly object renderLock = new object();
    
    // Performance optimization
    private const int MaxAdjustment = 255;
    private const int MinAdjustment = -255;
    private const int MaxDistance = 255;
    private const int MinDistance = 0;
    private const int MaxBlend = 1;
    private const int MinBlend = 0;
    private const int TableSize = 256;
    
    public BrightnessEffectsNode()
    {
        redTable = new int[TableSize];
        greenTable = new int[TableSize];
        blueTable = new int[TableSize];
        tablesNeedInit = true;
        lastWidth = lastHeight = 0;
        
        InitializeColorTables();
    }
    
    private void InitializeColorTables()
    {
        // Initialize color transformation tables
        for (int i = 0; i < TableSize; i++)
        {
            redTable[i] = i;
            greenTable[i] = i;
            blueTable[i] = i;
        }
        
        tablesNeedInit = false;
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled || ctx.Width <= 0 || ctx.Height <= 0) return;
        
        lock (renderLock)
        {
            // Update buffers if dimensions changed
            UpdateBuffers(ctx);
            
            // Update color tables if needed
            UpdateColorTables();
            
            // Apply brightness effect
            ApplyBrightnessEffect(ctx, input, output);
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
    
    private void UpdateColorTables()
    {
        if (tablesNeedInit)
        {
            InitializeColorTables();
        }
        
        // Update red channel table
        for (int i = 0; i < TableSize; i++)
        {
            int adjusted = i + RedAdjustment;
            redTable[i] = Math.Clamp(adjusted, 0, 255);
        }
        
        // Update green channel table
        for (int i = 0; i < TableSize; i++)
        {
            int adjusted = i + GreenAdjustment;
            greenTable[i] = Math.Clamp(adjusted, 0, 255);
        }
        
        // Update blue channel table
        for (int i = 0; i < TableSize; i++)
        {
            int adjusted = i + BlueAdjustment;
            blueTable[i] = Math.Clamp(adjusted, 0, 255);
        }
    }
    
    private void ApplyBrightnessEffect(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        // Use multi-threading for performance
        int threadCount = Environment.ProcessorCount;
        
        if (threadCount > 1)
        {
            ApplyBrightnessEffectMultiThreaded(ctx, input, output, threadCount);
        }
        else
        {
            ApplyBrightnessEffectSingleThreaded(ctx, input, output);
        }
    }
    
    private void ApplyBrightnessEffectMultiThreaded(FrameContext ctx, ImageBuffer input, ImageBuffer output, int threadCount)
    {
        var tasks = new Task[threadCount];
        
        for (int t = 0; t < threadCount; t++)
        {
            int threadId = t;
            tasks[t] = Task.Run(() => ApplyBrightnessEffectThread(ctx, input, output, threadId, threadCount));
        }
        
        Task.WaitAll(tasks);
    }
    
    private void ApplyBrightnessEffectThread(FrameContext ctx, ImageBuffer input, ImageBuffer output, int threadId, int threadCount)
    {
        // Calculate thread boundaries
        int startY = (threadId * ctx.Height) / threadCount;
        int endY = ((threadId + 1) * ctx.Height) / threadCount;
        
        for (int y = startY; y < endY; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                ProcessPixel(x, y, input, output);
            }
        }
    }
    
    private void ApplyBrightnessEffectSingleThreaded(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                ProcessPixel(x, y, input, output);
            }
        }
    }
    
    private void ProcessPixel(int x, int y, ImageBuffer input, ImageBuffer output)
    {
        Color inputPixel = input.GetPixel(x, y);
        
        // Check color exclusion
        if (Exclude == 1 && ShouldExcludeColor(inputPixel))
        {
            output.SetPixel(x, y, inputPixel);
            return;
        }
        
        // Apply brightness adjustments
        Color adjustedPixel = ApplyBrightnessAdjustments(inputPixel);
        
        // Apply blending
        Color finalPixel = ApplyBlendingMode(inputPixel, adjustedPixel);
        
        output.SetPixel(x, y, finalPixel);
    }
    
    private bool ShouldExcludeColor(Color pixel)
    {
        if (Exclude == 0) return false;
        
        // Calculate color distance
        int targetR = (Color >> 16) & 0xFF;
        int targetG = (Color >> 8) & 0xFF;
        int targetB = Color & 0xFF;
        
        int distance = CalculateColorDistance(
            pixel.R, pixel.G, pixel.B,
            targetR, targetG, targetB
        );
        
        return distance <= Distance;
    }
    
    private int CalculateColorDistance(int r1, int g1, int b1, int r2, int g2, int b2)
    {
        // Calculate Euclidean distance in RGB space
        int dr = r1 - r2;
        int dg = g1 - g2;
        int db = b1 - b2;
        
        return (int)Math.Sqrt(dr * dr + dg * dg + db * db);
    }
    
    private Color ApplyBrightnessAdjustments(Color pixel)
    {
        if (Dissoc == 1)
        {
            // Apply color dissociation
            return ApplyColorDissociation(pixel);
        }
        else
        {
            // Apply standard brightness adjustments
            return ApplyStandardBrightnessAdjustments(pixel);
        }
    }
    
    private Color ApplyStandardBrightnessAdjustments(Color pixel)
    {
        int r = redTable[pixel.R];
        int g = greenTable[pixel.G];
        int b = blueTable[pixel.B];
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private Color ApplyColorDissociation(Color pixel)
    {
        // Advanced color dissociation algorithm
        float luminance = (pixel.R * 0.299f + pixel.G * 0.587f + pixel.B * 0.114f);
        
        int r = (int)(luminance + RedAdjustment);
        int g = (int)(luminance + GreenAdjustment);
        int b = (int)(luminance + BlueAdjustment);
        
        // Clamp values
        r = Math.Clamp(r, 0, 255);
        g = Math.Clamp(g, 0, 255);
        b = Math.Clamp(b, 0, 255);
        
        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
    
    private Color ApplyBlendingMode(Color originalColor, Color adjustedColor)
    {
        switch (Blend)
        {
            case 0: // Replace
                return adjustedColor;
                
            case 1: // Additive
                return Color.FromRgb(
                    (byte)Math.Min(255, originalColor.R + adjustedColor.R),
                    (byte)Math.Min(255, originalColor.G + adjustedColor.G),
                    (byte)Math.Min(255, originalColor.B + adjustedColor.B)
                );
                
            default:
                return adjustedColor;
        }
    }
    
    // Public interface for parameter adjustment
    public void SetEnabled(bool enable) { Enabled = enable; }
    
    public void SetRedAdjustment(int adjustment) 
    { 
        RedAdjustment = Math.Clamp(adjustment, MinAdjustment, MaxAdjustment); 
        tablesNeedInit = true;
    }
    
    public void SetGreenAdjustment(int adjustment) 
    { 
        GreenAdjustment = Math.Clamp(adjustment, MinAdjustment, MaxAdjustment); 
        tablesNeedInit = true;
    }
    
    public void SetBlueAdjustment(int adjustment) 
    { 
        BlueAdjustment = Math.Clamp(adjustment, MinAdjustment, MaxAdjustment); 
        tablesNeedInit = true;
    }
    
    public void SetBlend(int blend) 
    { 
        Blend = Math.Clamp(blend, MinBlend, MaxBlend); 
    }
    
    public void SetBlendAvg(int blendAvg) 
    { 
        BlendAvg = Math.Clamp(blendAvg, MinBlend, MaxBlend); 
    }
    
    public void SetDissoc(int dissoc) 
    { 
        Dissoc = Math.Clamp(dissoc, 0, 1); 
    }
    
    public void SetColor(int color) { Color = color; }
    
    public void SetExclude(int exclude) 
    { 
        Exclude = Math.Clamp(exclude, 0, 1); 
    }
    
    public void SetDistance(int distance) 
    { 
        Distance = Math.Clamp(distance, MinDistance, MaxDistance); 
    }
    
    // Status queries
    public bool IsEnabled() => Enabled;
    public int GetRedAdjustment() => RedAdjustment;
    public int GetGreenAdjustment() => GreenAdjustment;
    public int GetBlueAdjustment() => BlueAdjustment;
    public int GetBlend() => Blend;
    public int GetBlendAvg() => BlendAvg;
    public int GetDissoc() => Dissoc;
    public int GetColor() => Color;
    public int GetExclude() => Exclude;
    public int GetDistance() => Distance;
    public bool AreTablesInitialized() => !tablesNeedInit;
    
    // Advanced brightness control
    public void SetAllChannelAdjustments(int adjustment)
    {
        SetRedAdjustment(adjustment);
        SetGreenAdjustment(adjustment);
        SetBlueAdjustment(adjustment);
    }
    
    public void SetRGBAdjustments(int red, int green, int blue)
    {
        SetRedAdjustment(red);
        SetGreenAdjustment(green);
        SetBlueAdjustment(blue);
    }
    
    public void SetBrightness(int brightness)
    {
        // Convert brightness to channel adjustments
        int adjustment = brightness - 128;
        SetAllChannelAdjustments(adjustment);
    }
    
    public void SetContrast(float contrast)
    {
        // Apply contrast adjustment to all channels
        for (int i = 0; i < TableSize; i++)
        {
            int adjusted = (int)((i - 128) * contrast + 128);
            redTable[i] = Math.Clamp(adjusted, 0, 255);
            greenTable[i] = Math.Clamp(adjusted, 0, 255);
            blueTable[i] = Math.Clamp(adjusted, 0, 255);
        }
    }
    
    // Color exclusion presets
    public void SetExcludeRed()
    {
        SetColor(0xFF0000);
        SetExclude(1);
        SetDistance(50);
    }
    
    public void SetExcludeGreen()
    {
        SetColor(0x00FF00);
        SetExclude(1);
        SetDistance(50);
    }
    
    public void SetExcludeBlue()
    {
        SetColor(0x0000FF);
        SetExclude(1);
        SetDistance(50);
    }
    
    public void SetExcludeWhite()
    {
        SetColor(0xFFFFFF);
        SetExclude(1);
        SetDistance(100);
    }
    
    public void SetExcludeBlack()
    {
        SetColor(0x000000);
        SetExclude(1);
        SetDistance(100);
    }
    
    // Brightness presets
    public void SetBright()
    {
        SetAllChannelAdjustments(50);
        SetBlend(0);
    }
    
    public void SetDark()
    {
        SetAllChannelAdjustments(-50);
        SetBlend(0);
    }
    
    public void SetHighContrast()
    {
        SetContrast(1.5f);
        SetBlend(0);
    }
    
    public void SetLowContrast()
    {
        SetContrast(0.7f);
        SetBlend(0);
    }
    
    public void SetSepia()
    {
        SetRedAdjustment(30);
        SetGreenAdjustment(-20);
        SetBlueAdjustment(-50);
        SetBlend(0);
    }
    
    public void SetGrayscale()
    {
        SetDissoc(1);
        SetAllChannelAdjustments(0);
        SetBlend(0);
    }
    
    // Performance optimization
    public void SetRenderQuality(int quality)
    {
        // Quality could affect processing detail or optimization level
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
            redTable = null;
            greenTable = null;
            blueTable = null;
        }
    }
}
```

## Integration Points

### Color Processing Integration
- **Channel Control**: Independent control of RGB color channels
- **Brightness Adjustment**: Precise brightness control and manipulation
- **Color Dissociation**: Advanced color dissociation and processing
- **Quality Control**: High-quality color transformation algorithms

### Performance Integration
- **Multi-Threading**: Symmetric Multi-Processing support for performance
- **Thread Management**: Intelligent thread distribution and management
- **Performance Scaling**: Dynamic performance scaling based on CPU cores
- **Synchronization**: Advanced thread synchronization and data sharing

### Image Processing Integration
- **Brightness Control**: Advanced brightness and contrast manipulation
- **Color Filtering**: Intelligent color exclusion and filtering
- **Blending Modes**: Multiple blending modes for visual integration
- **Visual Quality**: High-quality image processing and transformation

## Usage Examples

### Basic Brightness Adjustment
```csharp
var brightnessNode = new BrightnessEffectsNode
{
    Enabled = true,                       // Enable effect
    RedAdjustment = 20,                   // Increase red channel
    GreenAdjustment = 10,                 // Slight green increase
    BlueAdjustment = -5,                  // Decrease blue channel
    Blend = 0,                            // Replace mode
    BlendAvg = 1,                         // Enable average blending
    Dissoc = 0,                           // Standard processing
    Exclude = 0,                          // No color exclusion
    Distance = 16                         // Default distance threshold
};
```

### High Contrast Effect
```csharp
var brightnessNode = new BrightnessEffectsNode
{
    Enabled = true,
    RedAdjustment = 50,                   // High red adjustment
    GreenAdjustment = 50,                 // High green adjustment
    BlueAdjustment = 50,                  // High blue adjustment
    Blend = 0,                            // Replace mode
    Dissoc = 0,                           // Standard processing
    Exclude = 0                           // No color exclusion
};

// Apply high contrast preset
brightnessNode.SetHighContrast();
```

### Color Exclusion Effect
```csharp
var brightnessNode = new BrightnessEffectsNode
{
    Enabled = true,
    RedAdjustment = 30,                   // Moderate red increase
    GreenAdjustment = 0,                  // No green change
    BlueAdjustment = 0,                   // No blue change
    Blend = 1,                            // Additive blending
    Dissoc = 0,                           // Standard processing
    Exclude = 1,                          // Enable color exclusion
    Color = 0xFF0000,                     // Exclude red colors
    Distance = 50                         // Color distance threshold
};

// Apply red exclusion preset
brightnessNode.SetExcludeRed();
```

### Advanced Color Manipulation
```csharp
var brightnessNode = new BrightnessEffectsNode
{
    Enabled = true,
    RedAdjustment = 0,                    // No red change
    GreenAdjustment = 0,                  // No green change
    BlueAdjustment = 0,                   // No blue change
    Blend = 0,                            // Replace mode
    Dissoc = 1,                           // Enable color dissociation
    Exclude = 0,                          // No color exclusion
    Distance = 16                         // Default distance threshold
};

// Apply various presets
brightnessNode.SetSepia();                // Sepia tone effect
brightnessNode.SetGrayscale();            // Grayscale conversion
brightnessNode.SetBright();               // Brightness increase
```

## Technical Notes

### Color Architecture
The effect implements sophisticated color processing:
- **Channel Management**: Independent RGB channel control and manipulation
- **Color Tables**: Pre-calculated color transformation tables for performance
- **Dissociation Processing**: Advanced color dissociation algorithms
- **Quality Optimization**: High-quality color transformation and processing

### Performance Architecture
Advanced performance processing system:
- **Multi-Threading**: Symmetric Multi-Processing for performance optimization
- **Thread Management**: Intelligent thread distribution and management
- **Performance Scaling**: Dynamic performance scaling based on CPU cores
- **Synchronization**: Advanced thread synchronization and data sharing

### Integration System
Sophisticated system integration:
- **Color Processing**: Deep integration with color management system
- **Multi-Threading**: Seamless integration with threading system
- **Effect Management**: Advanced effect management and optimization
- **Performance Optimization**: Optimized operations for color processing

This effect provides the foundation for sophisticated brightness and color manipulation, creating advanced color transformations with multi-threaded processing, color dissociation, and intelligent blending modes for sophisticated AVS visualization systems.

---

## Complete C# Implementation

The following is the complete C# implementation that provides the core Brightness effect functionality:

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class BrightnessEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Enable/disable the brightness effect
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Red channel adjustment (-4096 to 4096)
        /// </summary>
        public int RedAdjustment { get; set; } = 0;

        /// <summary>
        /// Green channel adjustment (-4096 to 4096)
        /// </summary>
        public int GreenAdjustment { get; set; } = 0;

        /// <summary>
        /// Blue channel adjustment (-4096 to 4096)
        /// </summary>
        public int BlueAdjustment { get; set; } = 0;

        /// <summary>
        /// Blending mode - 0=none, 1=additive, 2=50/50
        /// </summary>
        public int BlendMode { get; set; } = 0;

        /// <summary>
        /// Enable 50/50 blending mode
        /// </summary>
        public bool BlendAverage { get; set; } = true;

        /// <summary>
        /// Disassociate color channels (independent control)
        /// </summary>
        public bool Disassociate { get; set; } = false;

        /// <summary>
        /// Reference color for exclusion mode
        /// </summary>
        public Color ReferenceColor { get; set; } = Color.Black;

        /// <summary>
        /// Enable color exclusion mode
        /// </summary>
        public bool ExcludeMode { get; set; } = false;

        /// <summary>
        /// Color similarity threshold for exclusion (0-255)
        /// </summary>
        public int DistanceThreshold { get; set; } = 16;

        /// <summary>
        /// Effect intensity multiplier
        /// </summary>
        public float Intensity { get; set; } = 1.0f;

        #endregion

        #region Private Fields

        private int[] _redTable;
        private int[] _greenTable;
        private int[] _blueTable;
        private bool _tablesNeedInit;

        #endregion

        #region Constructor

        public BrightnessEffectsNode()
        {
            _redTable = new int[256];
            _greenTable = new int[256];
            _blueTable = new int[256];
            _tablesNeedInit = true;
        }

        #endregion

        #region Processing

        public override void ProcessFrame(ImageBuffer imageBuffer, AudioFeatures audioFeatures)
        {
            if (!Enabled) return;

            // Initialize lookup tables if needed
            if (_tablesNeedInit)
            {
                InitializeLookupTables();
            }

            // Apply brightness adjustments
            ApplyBrightnessEffect(imageBuffer);
        }

        private void InitializeLookupTables()
        {
            // Calculate multipliers for each channel
            int redMultiplier = CalculateChannelMultiplier(RedAdjustment);
            int greenMultiplier = CalculateChannelMultiplier(GreenAdjustment);
            int blueMultiplier = CalculateChannelMultiplier(BlueAdjustment);

            // Generate lookup tables for each channel
            for (int i = 0; i < 256; i++)
            {
                // Red channel (16-bit precision)
                int redValue = (i * redMultiplier) & 0xFFFF0000;
                _redTable[i] = Math.Max(0, Math.Min(0xFF0000, redValue));

                // Green channel (8-bit precision)
                int greenValue = ((i * greenMultiplier) >> 8) & 0xFFFF00;
                _greenTable[i] = Math.Max(0, Math.Min(0xFF00, greenValue));

                // Blue channel (16-bit precision)
                int blueValue = ((i * blueMultiplier) >> 16) & 0xFFFF;
                _blueTable[i] = Math.Max(0, Math.Min(0xFF, blueValue));
            }

            _tablesNeedInit = false;
        }

        private int CalculateChannelMultiplier(int adjustment)
        {
            // Convert adjustment to multiplier with 16-bit precision
            if (adjustment < 0)
            {
                return (int)((1.0f + adjustment / 4096.0f) * 65536.0f);
            }
            else
            {
                return (int)((1.0f + (adjustment / 4096.0f) * 16.0f) * 65536.0f);
            }
        }

        private void ApplyBrightnessEffect(ImageBuffer imageBuffer)
        {
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color originalColor = imageBuffer.GetPixel(x, y);
                    Color adjustedColor = CalculateAdjustedColor(originalColor);

                    // Apply blending if enabled
                    if (BlendMode > 0 || BlendAverage)
                    {
                        adjustedColor = ApplyBlending(originalColor, adjustedColor);
                    }

                    imageBuffer.SetPixel(x, y, adjustedColor);
                }
            }
        }

        private Color CalculateAdjustedColor(Color originalColor)
        {
            // Check if color should be excluded
            if (ExcludeMode && IsColorInRange(originalColor, ReferenceColor, DistanceThreshold))
            {
                return originalColor; // No adjustment for excluded colors
            }

            // Apply channel adjustments using lookup tables
            int red = _redTable[originalColor.R];
            int green = _greenTable[originalColor.G];
            int blue = _blueTable[originalColor.B];

            // Combine adjusted channels
            int adjustedRGB = red | green | blue;

            // Extract individual components
            int adjustedRed = (adjustedRGB >> 16) & 0xFF;
            int adjustedGreen = (adjustedRGB >> 8) & 0xFF;
            int adjustedBlue = adjustedRGB & 0xFF;

            // Apply intensity multiplier
            adjustedRed = (int)(adjustedRed * Intensity);
            adjustedGreen = (int)(adjustedGreen * Intensity);
            adjustedBlue = (int)(adjustedBlue * Intensity);

            // Clamp values to valid range
            adjustedRed = Math.Max(0, Math.Min(255, adjustedRed));
            adjustedGreen = Math.Max(0, Math.Min(255, adjustedGreen));
            adjustedBlue = Math.Max(0, Math.Min(255, adjustedBlue));

            return Color.FromArgb(originalColor.A, adjustedRed, adjustedGreen, adjustedBlue);
        }

        private bool IsColorInRange(Color color1, Color color2, int threshold)
        {
            // Check if colors are within similarity threshold
            int redDiff = Math.Abs(color1.R - color2.R);
            int greenDiff = Math.Abs(color1.G - color2.G);
            int blueDiff = Math.Abs(color1.B - color2.B);

            return redDiff <= threshold && greenDiff <= threshold && blueDiff <= threshold;
        }

        private Color ApplyBlending(Color original, Color adjusted)
        {
            if (BlendMode == 1) // Additive blending
            {
                return BlendAdditive(original, adjusted);
            }
            else if (BlendAverage) // 50/50 blending
            {
                return BlendAverage(original, adjusted);
            }

            return adjusted; // No blending
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

        #endregion

        #region Configuration

        public override bool ValidateConfiguration()
        {
            // Validate property ranges
            RedAdjustment = Math.Max(-4096, Math.Min(4096, RedAdjustment));
            GreenAdjustment = Math.Max(-4096, Math.Min(4096, GreenAdjustment));
            BlueAdjustment = Math.Max(-4096, Math.Min(4096, BlueAdjustment));
            DistanceThreshold = Math.Max(0, Math.Min(255, DistanceThreshold));
            Intensity = Math.Max(0.1f, Math.Min(5.0f, Intensity));

            // Handle channel synchronization if not disassociated
            if (!Disassociate)
            {
                // Synchronize all channels to the first changed one
                if (RedAdjustment != 0)
                {
                    GreenAdjustment = RedAdjustment;
                    BlueAdjustment = RedAdjustment;
                }
                else if (GreenAdjustment != 0)
                {
                    RedAdjustment = GreenAdjustment;
                    BlueAdjustment = GreenAdjustment;
                }
                else if (BlueAdjustment != 0)
                {
                    RedAdjustment = BlueAdjustment;
                    GreenAdjustment = BlueAdjustment;
                }
            }

            // Mark tables for reinitialization if adjustments changed
            _tablesNeedInit = true;

            return true;
        }

        public override string GetSettingsSummary()
        {
            string blendMode = BlendMode == 0 ? "None" : BlendMode == 1 ? "Additive" : "50/50";
            string channelMode = Disassociate ? "Independent" : "Synchronized";
            string exclusionMode = ExcludeMode ? $"Exclude (Threshold: {DistanceThreshold})" : "None";

            return $"Brightness Effect - Enabled: {Enabled}, " +
                   $"Adjustments: R:{RedAdjustment}, G:{GreenAdjustment}, B:{BlueAdjustment}, " +
                   $"Mode: {channelMode}, Blend: {blendMode}, " +
                   $"Exclusion: {exclusionMode}, Intensity: {Intensity:F2}";
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Reset all channel adjustments to zero
        /// </summary>
        public void ResetAdjustments()
        {
            RedAdjustment = 0;
            GreenAdjustment = 0;
            BlueAdjustment = 0;
            _tablesNeedInit = true;
        }

        /// <summary>
        /// Set all channels to the same adjustment value
        /// </summary>
        /// <param name="adjustment">Adjustment value for all channels</param>
        public void SetAllChannels(int adjustment)
        {
            RedAdjustment = adjustment;
            GreenAdjustment = adjustment;
            BlueAdjustment = adjustment;
            _tablesNeedInit = true;
        }

        /// <summary>
        /// Increase brightness by a percentage
        /// </summary>
        /// <param name="percentage">Brightness increase percentage (0-100)</param>
        public void IncreaseBrightness(float percentage)
        {
            int adjustment = (int)(percentage * 40.96f); // Convert to adjustment range
            SetAllChannels(adjustment);
        }

        /// <summary>
        /// Decrease brightness by a percentage
        /// </summary>
        /// <param name="percentage">Brightness decrease percentage (0-100)</param>
        public void DecreaseBrightness(float percentage)
        {
            int adjustment = -(int)(percentage * 40.96f); // Convert to adjustment range
            SetAllChannels(adjustment);
        }

        #endregion
    }
}
```

## Effect Behavior

The Brightness effect provides comprehensive color adjustment by:

1. **Channel-Specific Control**: Independent adjustment of red, green, and blue channels
2. **Precision Adjustment**: 16-bit precision for smooth color transitions
3. **Multiple Blending Modes**: Replace, additive, and 50/50 blending options
4. **Color Exclusion**: Selective adjustment based on color similarity
5. **Channel Synchronization**: Option to link all channels for uniform adjustments
6. **Lookup Table Optimization**: Pre-calculated adjustments for performance

## Key Features

- **High Precision**: 16-bit color channel adjustments for smooth transitions
- **Flexible Blending**: Multiple blending modes for different visual effects
- **Color Exclusion**: Selective adjustment based on reference color similarity
- **Channel Synchronization**: Option to control all channels simultaneously
- **Performance Optimized**: Pre-calculated lookup tables for efficient processing
- **Range Validation**: Automatic clamping of adjustment values

## Adjustment Ranges

- **Red Channel**: -4096 to 4096 (negative = darker, positive = brighter)
- **Green Channel**: -4096 to 4096 (negative = darker, positive = brighter)
- **Blue Channel**: -4096 to 4096 (negative = darker, positive = brighter)
- **Distance Threshold**: 0-255 (color similarity for exclusion mode)
- **Intensity**: 0.1-5.0 (effect strength multiplier)

## Use Cases

- **Color Correction**: Adjust brightness and contrast for image enhancement
- **Color Grading**: Fine-tune individual color channels for artistic effects
- **Image Enhancement**: Improve visibility in dark or overexposed images
- **Color Balancing**: Correct color casts and achieve neutral color balance
- **Creative Effects**: Create stylized looks through selective color adjustment
- **Video Processing**: Real-time color adjustment for live video streams
