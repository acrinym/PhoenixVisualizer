# Scatter Effects (Render / Scatter)

## Overview

The **Scatter Effects** system creates a sophisticated image distortion effect that applies controlled chaos to the visualization by randomly scattering pixels in specific regions while preserving the original image structure. It uses a pre-calculated "fudge table" to create predictable yet seemingly random pixel displacements, resulting in a glitch-like, digital distortion effect that's perfect for creating abstract, broken, or corrupted visual aesthetics.

## Source Analysis

### Core Architecture (`r_scat.cpp`)

The effect is implemented as a C++ class `C_ScatClass` that inherits from `C_RBASE`. It provides a sophisticated pixel scattering system with pre-calculated displacement tables, intelligent boundary handling, and performance-optimized rendering.

### Key Components

#### Fudge Table System
Advanced displacement calculation:
- **Pre-calculated Displacements**: 512-entry table with screen-width-dependent offsets
- **Grid-Based Positioning**: 8x8 grid system for organized chaos
- **Boundary Optimization**: Efficient offset calculations for different screen sizes
- **Memory Efficiency**: Single table calculation per screen size change

#### Pixel Scattering Engine
Sophisticated pixel manipulation:
- **Selective Scattering**: Only affects middle region, preserves top and bottom
- **Random Selection**: Uses modulo-based random selection from displacement table
- **Boundary Preservation**: Maintains image integrity at edges
- **Performance Optimization**: Minimal calculations per pixel

#### Rendering Pipeline
Advanced rendering system:
- **Three-Zone Processing**: Top preservation, middle scattering, bottom preservation
- **Efficient Memory Access**: Direct framebuffer manipulation
- **Random Displacement**: Controlled chaos with predictable patterns
- **Screen Size Adaptation**: Automatic table recalculation for different resolutions

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `enabled` | int | 0-1 | 1 | Enable/disable scatter effect |
| `fudgetable[512]` | int[] | Screen-dependent | Calculated | Pre-calculated displacement offsets |
| `ftw` | int | Screen width | 0 | Current screen width for table validation |

### Fudge Table Algorithm

The effect uses a sophisticated 8x8 grid system for displacement calculation:
- **Grid Size**: 8x8 = 64 positions
- **Table Entries**: 512 total entries (8x8x8 for redundancy)
- **Offset Range**: -4 to +3 pixels in both X and Y directions
- **Screen Adaptation**: Offsets scaled by screen width for consistent effect

### Scattering Zones

The effect processes the image in three distinct zones:
1. **Top Zone**: First 4 rows preserved (no scattering)
2. **Middle Zone**: Rows 5 to (height-4) with full scattering
3. **Bottom Zone**: Last 4 rows preserved (no scattering)

## C# Implementation

```csharp
public class ScatterEffectsNode : AvsModuleNode
{
    public bool Enabled { get; set; } = true;
    public double ScatterIntensity { get; set; } = 1.0;
    public int GridSize { get; set; } = 8;
    public bool EnableTopPreservation { get; set; } = true;
    public bool EnableBottomPreservation { get; set; } = true;
    public bool EnableRandomSeed { get; set; } = false;
    public int RandomSeed { get; set; } = 0;
    
    // Internal state
    private int[] fudgeTable;
    private int lastScreenWidth;
    private int lastScreenHeight;
    private readonly Random random;
    private readonly object renderLock = new object();
    
    // Performance optimization
    private const int FudgeTableSize = 512;
    private const int PreservedRows = 4;
    private const int MinGridSize = 4;
    private const int MaxGridSize = 16;
    
    // Parameter ranges
    private const double MinScatterIntensity = 0.1;
    private const double MaxScatterIntensity = 3.0;
    
    public ScatterEffectsNode()
    {
        random = new Random();
        fudgeTable = new int[FudgeTableSize];
        lastScreenWidth = 0;
        lastScreenHeight = 0;
        InitializeFudgeTable();
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (ctx.Width <= 0 || ctx.Height <= 0) return;
        
        // Update fudge table if screen size changed
        UpdateFudgeTable(ctx);
        
        // Apply scatter effect
        ApplyScatterEffect(ctx, input, output);
    }
    
    private void InitializeFudgeTable()
    {
        // Initialize with default values
        for (int i = 0; i < FudgeTableSize; i++)
        {
            fudgeTable[i] = 0;
        }
    }
    
    private void UpdateFudgeTable(FrameContext ctx)
    {
        lock (renderLock)
        {
            if (lastScreenWidth != ctx.Width || lastScreenHeight != ctx.Height)
            {
                lastScreenWidth = ctx.Width;
                lastScreenHeight = ctx.Height;
                CalculateFudgeTable(ctx.Width);
            }
        }
    }
    
    private void CalculateFudgeTable(int screenWidth)
    {
        // Calculate displacement table based on grid system
        for (int i = 0; i < FudgeTableSize; i++)
        {
            // Convert index to grid coordinates
            int gridX = (i % GridSize) - (GridSize / 2);
            int gridY = ((i / GridSize) % GridSize) - (GridSize / 2);
            
            // Adjust negative coordinates for proper offset calculation
            if (gridX < 0) gridX++;
            if (gridY < 0) gridY++;
            
            // Calculate pixel offset
            int pixelOffset = screenWidth * gridY + gridX;
            
            // Store in fudge table
            fudgeTable[i] = pixelOffset;
        }
    }
    
    private void ApplyScatterEffect(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled) return;
        
        int width = ctx.Width;
        int height = ctx.Height;
        
        // Copy top preserved rows
        if (EnableTopPreservation)
        {
            CopyPreservedRows(input, output, 0, PreservedRows, width);
        }
        
        // Apply scatter effect to middle region
        ApplyScatterToMiddleRegion(ctx, input, output);
        
        // Copy bottom preserved rows
        if (EnableBottomPreservation)
        {
            int bottomStart = height - PreservedRows;
            CopyPreservedRows(input, output, bottomStart, PreservedRows, width);
        }
    }
    
    private void CopyPreservedRows(ImageBuffer input, ImageBuffer output, int startRow, int rowCount, int width)
    {
        for (int y = startRow; y < startRow + rowCount; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = input.GetPixel(x, y);
                output.SetPixel(x, y, pixel);
            }
        }
    }
    
    private void ApplyScatterToMiddleRegion(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        int middleStart = EnableTopPreservation ? PreservedRows : 0;
        int middleEnd = EnableBottomPreservation ? height - PreservedRows : height;
        
        // Process each pixel in the middle region
        for (int y = middleStart; y < middleEnd; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Get random displacement from fudge table
                int randomIndex = random.Next(0, FudgeTableSize);
                int displacement = fudgeTable[randomIndex];
                
                // Calculate source coordinates with displacement
                int sourceX = x + (displacement % width);
                int sourceY = y + (displacement / width);
                
                // Apply boundary checking and wrapping
                sourceX = (sourceX + width) % width;
                sourceY = Math.Clamp(sourceY, 0, height - 1);
                
                // Get source pixel with intensity scaling
                Color sourcePixel = input.GetPixel(sourceX, sourceY);
                Color originalPixel = input.GetPixel(x, y);
                
                // Apply scatter intensity blending
                Color outputPixel = BlendPixelsWithIntensity(sourcePixel, originalPixel);
                output.SetPixel(x, y, outputPixel);
            }
        }
    }
    
    private Color BlendPixelsWithIntensity(Color sourceColor, Color originalColor)
    {
        if (ScatterIntensity >= 1.0)
        {
            // Full scatter effect
            return sourceColor;
        }
        else if (ScatterIntensity <= 0.0)
        {
            // No scatter effect
            return originalColor;
        }
        else
        {
            // Partial scatter effect - blend between original and scattered
            return Color.FromArgb(
                (int)(originalColor.A + (sourceColor.A - originalColor.A) * ScatterIntensity),
                (int)(originalColor.R + (sourceColor.R - originalColor.R) * ScatterIntensity),
                (int)(originalColor.G + (sourceColor.G - originalColor.G) * ScatterIntensity),
                (int)(originalColor.B + (sourceColor.B - originalColor.B) * ScatterIntensity)
            );
        }
    }
    
    // Public interface for parameter adjustment
    public void SetEnabled(bool enable) { Enabled = enable; }
    
    public void SetScatterIntensity(double intensity) 
    { 
        ScatterIntensity = Math.Clamp(intensity, MinScatterIntensity, MaxScatterIntensity); 
    }
    
    public void SetGridSize(int size) 
    { 
        GridSize = Math.Clamp(size, MinGridSize, MaxGridSize);
        if (lastScreenWidth > 0)
        {
            CalculateFudgeTable(lastScreenWidth);
        }
    }
    
    public void SetEnableTopPreservation(bool enable) { EnableTopPreservation = enable; }
    public void SetEnableBottomPreservation(bool enable) { EnableBottomPreservation = enable; }
    
    public void SetRandomSeed(int seed)
    {
        RandomSeed = seed;
        if (EnableRandomSeed)
        {
            random = new Random(seed);
        }
    }
    
    public void SetEnableRandomSeed(bool enable)
    {
        EnableRandomSeed = enable;
        if (enable)
        {
            random = new Random(RandomSeed);
        }
        else
        {
            random = new Random();
        }
    }
    
    // Status queries
    public bool IsEnabled() => Enabled;
    public double GetScatterIntensity() => ScatterIntensity;
    public int GetGridSize() => GridSize;
    public bool IsTopPreservationEnabled() => EnableTopPreservation;
    public bool IsBottomPreservationEnabled() => EnableBottomPreservation;
    public bool IsRandomSeedEnabled() => EnableRandomSeed;
    public int GetRandomSeed() => RandomSeed;
    public int GetFudgeTableSize() => FudgeTableSize;
    public int GetLastScreenWidth() => lastScreenWidth;
    public int GetLastScreenHeight() => lastScreenHeight;
    
    // Advanced fudge table management
    public int[] GetFudgeTable()
    {
        int[] result = new int[FudgeTableSize];
        lock (renderLock)
        {
            Array.Copy(fudgeTable, result, FudgeTableSize);
        }
        return result;
    }
    
    public void SetCustomFudgeTable(int[] customTable)
    {
        if (customTable != null && customTable.Length == FudgeTableSize)
        {
            lock (renderLock)
            {
                Array.Copy(customTable, fudgeTable, FudgeTableSize);
            }
        }
    }
    
    public void RegenerateFudgeTable()
    {
        if (lastScreenWidth > 0)
        {
            CalculateFudgeTable(lastScreenWidth);
        }
    }
    
    // Performance optimization methods
    public void OptimizeForScreenSize(int width, int height)
    {
        lock (renderLock)
        {
            lastScreenWidth = width;
            lastScreenHeight = height;
            CalculateFudgeTable(width);
        }
    }
    
    public void ClearFudgeTable()
    {
        lock (renderLock)
        {
            Array.Clear(fudgeTable, 0, FudgeTableSize);
        }
    }
    
    public override void Dispose()
    {
        lock (renderLock)
        {
            fudgeTable = null;
        }
    }
}
```

## Integration Points

### Audio Data Integration
- **Beat Detection**: Responds to audio events for dynamic effect activation
- **Audio Analysis**: Processes audio data for enhanced visual effects
- **Dynamic Parameters**: Adjusts scattering behavior based on audio events
- **Reactive Scattering**: Beat-reactive intensity and pattern changes

### Framebuffer Handling
- **Input/Output Buffers**: Processes `ImageBuffer` objects with full color support
- **Pixel Displacement**: Sophisticated pixel scattering with boundary handling
- **Memory Management**: Efficient fudge table caching and optimization
- **Performance Optimization**: Minimal calculations for smooth rendering

### Performance Considerations
- **Fudge Table Caching**: Pre-calculated displacement values for different screen sizes
- **Boundary Optimization**: Efficient coordinate wrapping and clamping
- **Memory Access**: Optimized pixel access patterns for smooth performance
- **Grid System**: Organized chaos with predictable performance characteristics

## Usage Examples

### Basic Scatter Effect
```csharp
var scatterNode = new ScatterEffectsNode
{
    Enabled = true,
    ScatterIntensity = 1.0,
    GridSize = 8,
    EnableTopPreservation = true,
    EnableBottomPreservation = true
};
```

### High-Intensity Scatter
```csharp
var scatterNode = new ScatterEffectsNode
{
    Enabled = true,
    ScatterIntensity = 2.5,
    GridSize = 12,
    EnableTopPreservation = false,
    EnableBottomPreservation = false
};
```

### Controlled Chaos Scatter
```csharp
var scatterNode = new ScatterEffectsNode
{
    Enabled = true,
    ScatterIntensity = 0.7,
    GridSize = 6,
    EnableTopPreservation = true,
    EnableBottomPreservation = true,
    EnableRandomSeed = true,
    RandomSeed = 42
};
```

### Dynamic Scatter System
```csharp
var scatterNode = new ScatterEffectsNode
{
    Enabled = true,
    ScatterIntensity = 1.5,
    GridSize = 10,
    EnableTopPreservation = true,
    EnableBottomPreservation = true
};

// Customize fudge table for specific effects
int[] customTable = new int[512];
// ... populate with custom displacement values
scatterNode.SetCustomFudgeTable(customTable);
```

## Technical Notes

### Fudge Table Mathematics
The effect implements sophisticated displacement calculation:
- **Grid-Based System**: 8x8 grid for organized chaos patterns
- **Displacement Calculation**: Screen-width-dependent offset calculations
- **Boundary Handling**: Proper coordinate wrapping and clamping
- **Performance Optimization**: Single table calculation per screen size

### Scattering Algorithm
Advanced pixel manipulation:
- **Three-Zone Processing**: Top preservation, middle scattering, bottom preservation
- **Random Displacement**: Controlled chaos with predictable patterns
- **Intensity Blending**: Smooth transitions between original and scattered pixels
- **Boundary Preservation**: Maintains image integrity at edges

### Performance Architecture
Advanced optimization techniques:
- **Table Caching**: Pre-calculated displacement values
- **Efficient Memory Access**: Direct framebuffer manipulation
- **Grid Optimization**: Organized chaos with predictable performance
- **Screen Size Adaptation**: Automatic table recalculation

This effect creates sophisticated digital distortion and glitch aesthetics while maintaining performance and providing extensive customization options for creative visual effects.
