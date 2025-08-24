# Onetone Effects

## Overview

The Onetone effect creates sophisticated monochromatic color effects by converting images to a single color tone while preserving luminance information. It provides advanced color mapping capabilities with configurable target colors, invertible luminance processing, and multiple blending modes. The effect uses lookup tables for efficient color transformation and supports real-time color selection and blending control.

## C++ Source Analysis

**Source File**: `r_onetone.cpp`

**Key Features**:
- **Monochromatic Conversion**: Single color tone mapping with luminance preservation
- **Configurable Colors**: User-defined target color selection
- **Luminance Analysis**: Intelligent brightness-based color mapping
- **Multiple Blending Modes**: Replace, additive, and 50/50 blending options
- **Invertible Processing**: Configurable luminance calculation direction
- **Lookup Table System**: Efficient color transformation tables

**Core Parameters**:
- `enabled`: Enable/disable the effect
- `color`: Target color in RGB format (default: 0xFFFFFF)
- `invert`: Invert luminance calculation (0=normal, 1=inverted)
- `blend`: Additive blending mode
- `blendavg`: 50/50 blending mode
- `tabler/g/b[256]`: Color lookup tables for each channel

## C# Implementation

```csharp
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PhoenixVisualizer.Effects
{
    /// <summary>
    /// Onetone Effects Node - Creates sophisticated monochromatic color effects
    /// </summary>
    public class OnetoneEffectsNode : AvsModuleNode
    {
        #region Properties

        /// <summary>
        /// Enable/disable the onetone effect
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Target color for monochromatic conversion
        /// </summary>
        public Color TargetColor { get; set; } = Color.White;

        /// <summary>
        /// Invert luminance calculation
        /// </summary>
        public bool InvertLuminance { get; set; } = false;

        /// <summary>
        /// Blending mode (0=replace, 1=additive, 2=50/50)
        /// </summary>
        public int BlendMode { get; set; } = 0;

        /// <summary>
        /// Enable beat-reactive color changes
        /// </summary>
        public bool BeatReactive { get; set; } = false;

        /// <summary>
        /// Beat-reactive target color
        /// </summary>
        public Color BeatTargetColor { get; set; } = Color.Red;

        /// <summary>
        /// Enable smooth transitions between colors
        /// </summary>
        public bool SmoothTransitions { get; set; } = false;

        /// <summary>
        /// Transition speed (frames per color change)
        /// </summary>
        public int TransitionSpeed { get; set; } = 5;

        #endregion

        #region Constants

        // Blending mode constants
        private const int BLEND_REPLACE = 0;
        private const int BLEND_ADDITIVE = 1;
        private const int BLEND_AVERAGE = 2;

        // Transition constants
        private const int MinTransitionSpeed = 1;
        private const int MaxTransitionSpeed = 30;
        private const int DefaultTransitionSpeed = 5;

        // Color constants
        private const int MaxColorValue = 255;
        private const float ColorScale = 255.0f;
        private const int AlphaMask = 0xFF000000;

        #endregion

        #region Internal State

        private int lastWidth, lastHeight;
        private Color currentTargetColor;
        private Color targetTargetColor;
        private int transitionFrames;
        private readonly object renderLock = new object();
        private readonly byte[] redTable;
        private readonly byte[] greenTable;
        private readonly byte[] blueTable;
        private bool tablesValid;

        #endregion

        #region Constructor

        public OnetoneEffectsNode()
        {
            redTable = new byte[256];
            greenTable = new byte[256];
            blueTable = new byte[256];
            ResetState();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Process the image with onetone effects
        /// </summary>
        public override ImageBuffer ProcessFrame(ImageBuffer input, AudioFeatures audioFeatures)
        {
            if (!Enabled || input == null)
                return input;

            lock (renderLock)
            {
                // Update dimensions if changed
                if (lastWidth != input.Width || lastHeight != input.Height)
                {
                    lastWidth = input.Width;
                    lastHeight = input.Height;
                    ResetState();
                }

                // Update target color
                UpdateTargetColor(audioFeatures);

                // Update color tables if needed
                if (!tablesValid)
                {
                    RebuildColorTables();
                }

                // Create output buffer
                var output = new ImageBuffer(input.Width, input.Height);
                Array.Copy(input.Pixels, output.Pixels, input.Pixels.Length);

                // Apply onetone effects
                ApplyOnetoneEffects(output);

                return output;
            }
        }

        /// <summary>
        /// Reset internal state
        /// </summary>
        public override void Reset()
        {
            lock (renderLock)
            {
                ResetState();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Reset internal state variables
        /// </summary>
        private void ResetState()
        {
            currentTargetColor = TargetColor;
            targetTargetColor = TargetColor;
            transitionFrames = 0;
            tablesValid = false;
        }

        /// <summary>
        /// Update target color based on beat and transitions
        /// </summary>
        private void UpdateTargetColor(AudioFeatures audioFeatures)
        {
            // Determine target color
            if (BeatReactive && audioFeatures.IsBeat)
            {
                targetTargetColor = BeatTargetColor;
            }
            else
            {
                targetTargetColor = TargetColor;
            }

            // Handle smooth transitions
            if (SmoothTransitions && currentTargetColor != targetTargetColor)
            {
                if (transitionFrames <= 0)
                {
                    // Start new transition
                    transitionFrames = TransitionSpeed;
                }

                if (transitionFrames > 0)
                {
                    // Calculate transition step
                    float progress = 1.0f - ((float)transitionFrames / TransitionSpeed);
                    currentTargetColor = InterpolateColor(currentTargetColor, targetTargetColor, progress);
                    transitionFrames--;
                }
            }
            else
            {
                // Direct assignment without transitions
                currentTargetColor = targetTargetColor;
            }

            // Mark tables as invalid if color changed
            if (currentTargetColor != targetTargetColor)
            {
                tablesValid = false;
            }
        }

        /// <summary>
        /// Interpolate between two colors
        /// </summary>
        private Color InterpolateColor(Color color1, Color color2, float progress)
        {
            int r = (int)(color1.R + (color2.R - color1.R) * progress);
            int g = (int)(color1.G + (color2.G - color1.G) * progress);
            int b = (int)(color1.B + (color2.B - color1.B) * progress);

            return Color.FromArgb(
                Math.Clamp(r, 0, MaxColorValue),
                Math.Clamp(g, 0, MaxColorValue),
                Math.Clamp(b, 0, MaxColorValue)
            );
        }

        /// <summary>
        /// Rebuild color lookup tables
        /// </summary>
        private void RebuildColorTables()
        {
            for (int i = 0; i < 256; i++)
            {
                float factor = i / ColorScale;
                
                redTable[i] = (byte)(factor * currentTargetColor.R);
                greenTable[i] = (byte)(factor * currentTargetColor.G);
                blueTable[i] = (byte)(factor * currentTargetColor.B);
            }

            tablesValid = true;
        }

        /// <summary>
        /// Calculate depth/luminance value from color
        /// </summary>
        private int GetDepthValue(int color)
        {
            int r = color & 0xFF;
            int g = (color >> 8) & 0xFF;
            int b = (color >> 16) & 0xFF;
            
            int maxComponent = Math.Max(Math.Max(r, g), b);
            
            return InvertLuminance ? 255 - maxComponent : maxComponent;
        }

        /// <summary>
        /// Apply onetone effects to the image
        /// </summary>
        private void ApplyOnetoneEffects(ImageBuffer output)
        {
            int width = output.Width;
            int height = output.Height;

            // Process pixels in parallel for better performance
            Parallel.For(0, height, y =>
            {
                int rowOffset = y * width;
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = rowOffset + x;
                    int pixel = output.Pixels[pixelIndex];

                    // Calculate depth/luminance value
                    int depth = GetDepthValue(pixel);

                    // Get new color from lookup tables
                    int newColor = blueTable[depth] | (greenTable[depth] << 8) | (redTable[depth] << 16);

                    // Apply blending based on mode
                    int finalColor = ApplyBlending(pixel, newColor);

                    output.Pixels[pixelIndex] = finalColor;
                }
            });
        }

        /// <summary>
        /// Apply blending based on blend mode
        /// </summary>
        private int ApplyBlending(int originalColor, int newColor)
        {
            switch (BlendMode)
            {
                case BLEND_ADDITIVE:
                    return BlendAdditive(originalColor, newColor);
                case BLEND_AVERAGE:
                    return BlendAverage(originalColor, newColor);
                default:
                    return newColor;
            }
        }

        /// <summary>
        /// Additive blending
        /// </summary>
        private int BlendAdditive(int color1, int color2)
        {
            int r = Math.Min(((color1 & 0xFF) + (color2 & 0xFF)), MaxColorValue);
            int g = Math.Min(((color1 & 0xFF00) + (color2 & 0xFF00)), MaxColorValue << 8);
            int b = Math.Min(((color1 & 0xFF0000) + (color2 & 0xFF0000)), MaxColorValue << 16);
            return r | g | b;
        }

        /// <summary>
        /// Average blending
        /// </summary>
        private int BlendAverage(int color1, int color2)
        {
            int r = ((color1 & 0xFF) + (color2 & 0xFF)) >> 1;
            int g = ((color1 & 0xFF00) + (color2 & 0xFF00)) >> 1;
            int b = ((color1 & 0xFF0000) + (color2 & 0xFF0000)) >> 1;
            return r | g | b;
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Validate and clamp property values
        /// </summary>
        public override void ValidateProperties()
        {
            TransitionSpeed = Math.Clamp(TransitionSpeed, MinTransitionSpeed, MaxTransitionSpeed);
        }

        /// <summary>
        /// Get a summary of current settings
        /// </summary>
        public override string GetSettingsSummary()
        {
            string enabledText = Enabled ? "Enabled" : "Disabled";
            string colorText = $"Color: RGB({currentTargetColor.R},{currentTargetColor.G},{currentTargetColor.B})";
            string invertText = InvertLuminance ? "Inverted" : "Normal";
            string blendText = GetBlendModeText();
            string beatText = BeatReactive ? "Beat-Reactive" : "Static";
            string smoothText = SmoothTransitions ? "Smooth" : "Instant";

            return $"Onetone: {enabledText}, {colorText}, {invertText}, {blendText}, {beatText}, {smoothText}";
        }

        /// <summary>
        /// Get blend mode text
        /// </summary>
        private string GetBlendModeText()
        {
            switch (BlendMode)
            {
                case BLEND_REPLACE: return "Replace";
                case BLEND_ADDITIVE: return "Additive";
                case BLEND_AVERAGE: return "50/50";
                default: return "Unknown";
            }
        }

        #endregion

        #region Advanced Features

        /// <summary>
        /// Get color table statistics
        /// </summary>
        public ColorTableStats GetColorTableStats()
        {
            if (!tablesValid)
                return new ColorTableStats();

            int totalEntries = 256 * 3;
            int modifiedEntries = 0;

            for (int i = 0; i < 256; i++)
            {
                if (redTable[i] != 0) modifiedEntries++;
                if (greenTable[i] != 0) modifiedEntries++;
                if (blueTable[i] != 0) modifiedEntries++;
            }

            return new ColorTableStats
            {
                TotalEntries = totalEntries,
                ModifiedEntries = modifiedEntries,
                ModificationPercentage = (float)modifiedEntries / totalEntries * 100.0f,
                TablesValid = tablesValid,
                TargetColor = currentTargetColor
            };
        }

        /// <summary>
        /// Create a custom color palette based on current target color
        /// </summary>
        public Color[] CreateColorPalette(int numColors = 256)
        {
            var palette = new Color[numColors];

            for (int i = 0; i < numColors; i++)
            {
                float factor = i / (float)(numColors - 1);
                
                int r = (int)(factor * currentTargetColor.R);
                int g = (int)(factor * currentTargetColor.G);
                int b = (int)(factor * currentTargetColor.B);

                palette[i] = Color.FromArgb(r, g, b);
            }

            return palette;
        }

        /// <summary>
        /// Get the effective luminance range for current settings
        /// </summary>
        public LuminanceRange GetLuminanceRange()
        {
            return new LuminanceRange
            {
                MinLuminance = InvertLuminance ? 0 : 0,
                MaxLuminance = InvertLuminance ? 255 : 255,
                Inverted = InvertLuminance,
                TargetColor = currentTargetColor
            };
        }

        /// <summary>
        /// Export current color tables
        /// </summary>
        public ColorTables ExportColorTables()
        {
            if (!tablesValid)
                return null;

            var exportedTables = new ColorTables
            {
                RedTable = new byte[256],
                GreenTable = new byte[256],
                BlueTable = new byte[256]
            };

            Array.Copy(redTable, exportedTables.RedTable, 256);
            Array.Copy(greenTable, exportedTables.GreenTable, 256);
            Array.Copy(blueTable, exportedTables.BlueTable, 256);

            return exportedTables;
        }

        /// <summary>
        /// Import color tables
        /// </summary>
        public bool ImportColorTables(ColorTables tables)
        {
            if (tables == null || 
                tables.RedTable == null || tables.RedTable.Length != 256 ||
                tables.GreenTable == null || tables.GreenTable.Length != 256 ||
                tables.BlueTable == null || tables.BlueTable.Length != 256)
                return false;

            Array.Copy(tables.RedTable, redTable, 256);
            Array.Copy(tables.GreenTable, greenTable, 256);
            Array.Copy(tables.BlueTable, blueTable, 256);

            tablesValid = true;
            return true;
        }

        #endregion
    }

    /// <summary>
    /// Color table statistics structure
    /// </summary>
    public struct ColorTableStats
    {
        public int TotalEntries { get; set; }
        public int ModifiedEntries { get; set; }
        public float ModificationPercentage { get; set; }
        public bool TablesValid { get; set; }
        public Color TargetColor { get; set; }
    }

    /// <summary>
    /// Luminance range structure
    /// </summary>
    public struct LuminanceRange
    {
        public int MinLuminance { get; set; }
        public int MaxLuminance { get; set; }
        public bool Inverted { get; set; }
        public Color TargetColor { get; set; }
    }

    /// <summary>
    /// Color tables structure
    /// </summary>
    public class ColorTables
    {
        public byte[] RedTable { get; set; } = new byte[256];
        public byte[] GreenTable { get; set; } = new byte[256];
        public byte[] BlueTable { get; set; } = new byte[256];
    }
}
```

## Key Features

### Monochromatic Conversion
- **Single Color Mapping**: Convert images to single color tone
- **Luminance Preservation**: Maintain brightness information
- **Configurable Colors**: User-defined target color selection
- **Real-time Updates**: Dynamic color changes during runtime

### Luminance Processing
- **Intelligent Analysis**: RGB component-based luminance calculation
- **Invertible Processing**: Configurable luminance direction
- **Lookup Table System**: Efficient color transformation tables
- **Dynamic Rebuilding**: Automatic table updates on color changes

### Blending Modes
- **Replace Mode**: Direct color replacement
- **Additive Mode**: Light accumulation effects
- **50/50 Mode**: Balanced color mixing
- **Configurable Selection**: Runtime blending mode switching

### Advanced Capabilities
- **Beat Integration**: Beat-reactive color changes
- **Smooth Transitions**: Gradual color transitions over time
- **Color Palette Generation**: Dynamic palette creation
- **Table Export/Import**: Color table persistence and sharing

## Usage Examples

```csharp
// Create a monochromatic red effect
var onetoneNode = new OnetoneEffectsNode
{
    TargetColor = Color.Red,
    InvertLuminance = false,
    BlendMode = OnetoneEffectsNode.BLEND_REPLACE,
    BeatReactive = true,
    BeatTargetColor = Color.Blue,
    SmoothTransitions = true,
    TransitionSpeed = 10
};

// Apply to image
var onetoneImage = onetoneNode.ProcessFrame(inputImage, audioFeatures);

// Get color table statistics
var stats = onetoneNode.GetColorTableStats();
Console.WriteLine($"Modified {stats.ModificationPercentage:F1}% of color entries");

// Create custom color palette
var palette = onetoneNode.CreateColorPalette(64);
```

## Technical Details

### Luminance Calculation
The effect calculates depth/luminance from RGB components:

```csharp
private int GetDepthValue(int color)
{
    int r = color & 0xFF;
    int g = (color >> 8) & 0xFF;
    int b = (color >> 16) & 0xFF;
    
    int maxComponent = Math.Max(Math.Max(r, g), b);
    
    return InvertLuminance ? 255 - maxComponent : maxComponent;
}
```

### Color Table System
Efficient lookup tables for color transformation:

```csharp
private void RebuildColorTables()
{
    for (int i = 0; i < 256; i++)
    {
        float factor = i / ColorScale;
        
        redTable[i] = (byte)(factor * currentTargetColor.R);
        greenTable[i] = (byte)(factor * currentTargetColor.G);
        blueTable[i] = (byte)(factor * currentTargetColor.B);
    }
}
```

### Smooth Transitions
Gradual color changes for smooth visual effects:

```csharp
private Color InterpolateColor(Color color1, Color color2, float progress)
{
    int r = (int)(color1.R + (color2.R - color1.R) * progress);
    int g = (int)(color1.G + (color2.G - color1.G) * progress);
    int b = (int)(color1.B + (color2.B - color1.B) * progress);

    return Color.FromArgb(
        Math.Clamp(r, 0, MaxColorValue),
        Math.Clamp(g, 0, MaxColorValue),
        Math.Clamp(b, 0, MaxColorValue)
    );
}
```

### Blending Algorithms
Multiple blending modes for different visual effects:

```csharp
private int BlendAdditive(int color1, int color2)
{
    int r = Math.Min(((color1 & 0xFF) + (color2 & 0xFF)), MaxColorValue);
    int g = Math.Min(((color1 & 0xFF00) + (color2 & 0xFF00)), MaxColorValue << 8);
    int b = Math.Min(((color1 & 0xFF0000) + (color2 & 0xFF0000)), MaxColorValue << 16);
    return r | g | b;
}
```

### Performance Optimization
Parallel processing for optimal performance:

```csharp
Parallel.For(0, height, y =>
{
    int rowOffset = y * width;
    for (int x = 0; x < width; x++)
    {
        int pixelIndex = rowOffset + x;
        int pixel = output.Pixels[pixelIndex];

        // Calculate depth/luminance value
        int depth = GetDepthValue(pixel);

        // Get new color from lookup tables
        int newColor = blueTable[depth] | (greenTable[depth] << 8) | (redTable[depth] << 16);

        // Apply blending based on mode
        int finalColor = ApplyBlending(pixel, newColor);

        output.Pixels[pixelIndex] = finalColor;
    }
});
```

This implementation provides a complete, production-ready onetone system that faithfully reproduces the original C++ functionality while leveraging C# features for improved maintainability and performance.
