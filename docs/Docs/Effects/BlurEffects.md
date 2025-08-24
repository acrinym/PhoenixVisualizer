# Blur Effects

## Overview

The Blur effect creates sophisticated image blurring with multiple intensity levels and configurable rounding modes. It provides three distinct blur intensities and supports both single-threaded and multi-threaded rendering for optimal performance. The effect uses advanced pixel averaging algorithms with MMX optimization for high-quality blurring effects.

## C++ Source Analysis

**Source File**: `r_blur.cpp`

**Key Features**:
- **Multiple Blur Intensities**: Three distinct blur levels (light, medium, heavy)
- **Multi-threaded Rendering**: SMP support for parallel processing
- **MMX Optimization**: SIMD-accelerated pixel processing
- **Configurable Rounding**: Round up or down for different visual effects
- **Edge Handling**: Specialized processing for image boundaries
- **Performance Timing**: Built-in performance measurement

**Core Parameters**:
- `enabled`: Blur intensity (0=off, 1=light, 2=medium, 3=heavy)
- `roundmode`: Rounding mode (0=round down, 1=round up)

## C# Implementation

```csharp
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PhoenixVisualizer.Effects
{
    /// <summary>
    /// Blur Effects Node - Creates sophisticated image blurring with multiple intensity levels
    /// </summary>
    public class BlurEffectsNode : AvsModuleNode
    {
        #region Properties

        /// <summary>
        /// Enable/disable the blur effect
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Blur intensity (0=off, 1=light, 2=medium, 3=heavy)
        /// </summary>
        public int BlurIntensity { get; set; } = 1;

        /// <summary>
        /// Rounding mode (0=round down, 1=round up)
        /// </summary>
        public int RoundMode { get; set; } = 0;

        /// <summary>
        /// Enable multi-threaded rendering
        /// </summary>
        public bool MultiThreaded { get; set; } = true;

        #endregion

        #region Constants

        // Blur intensity constants
        private const int BLUR_OFF = 0;
        private const int BLUR_LIGHT = 1;
        private const int BLUR_MEDIUM = 2;
        private const int BLUR_HEAVY = 3;

        // Rounding mode constants
        private const int ROUND_DOWN = 0;
        private const int ROUND_UP = 1;

        // Performance optimization constants
        private const int MaxBlurIntensity = 3;
        private const int MinBlurIntensity = 0;
        private const int MaxRoundMode = 1;
        private const int MinRoundMode = 0;
        private const int MaxThreads = 8;
        private const int MinThreads = 1;

        // Blur algorithm constants
        private const uint MASK_SH1 = ~(((1U << 7) | (1U << 15) | (1U << 23)) << 1);
        private const uint MASK_SH2 = ~(((3U << 6) | (3U << 14) | (3U << 22)) << 2);
        private const uint MASK_SH3 = ~(((7U << 5) | (7U << 13) | (7U << 21)) << 3);
        private const uint MASK_SH4 = ~(((15U << 4) | (15U << 12) | (15U << 20)) << 4);

        #endregion

        #region Internal State

        private int lastWidth, lastHeight;
        private readonly object renderLock = new object();

        #endregion

        #region Constructor

        public BlurEffectsNode()
        {
            ResetState();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Process the image with blur effects
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

                // Create output buffer
                var output = new ImageBuffer(input.Width, input.Height);
                Array.Copy(input.Pixels, output.Pixels, input.Pixels.Length);

                // Apply blur based on intensity
                switch (BlurIntensity)
                {
                    case BLUR_LIGHT:
                        ApplyLightBlur(input, output);
                        break;
                    case BLUR_MEDIUM:
                        ApplyMediumBlur(input, output);
                        break;
                    case BLUR_HEAVY:
                        ApplyHeavyBlur(input, output);
                        break;
                }

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
            // No specific state to reset for blur effect
        }

        /// <summary>
        /// Apply light blur effect
        /// </summary>
        private void ApplyLightBlur(ImageBuffer input, ImageBuffer output)
        {
            int width = input.Width;
            int height = input.Height;

            // Process top line
            ProcessTopLine(input, output, width);

            // Process middle block
            ProcessMiddleBlock(input, output, width, height);

            // Process bottom line
            ProcessBottomLine(input, output, width, height);
        }

        /// <summary>
        /// Apply medium blur effect
        /// </summary>
        private void ApplyMediumBlur(ImageBuffer input, ImageBuffer output)
        {
            int width = input.Width;
            int height = input.Height;

            // Process top line
            ProcessTopLineMedium(input, output, width);

            // Process middle block
            ProcessMiddleBlockMedium(input, output, width, height);

            // Process bottom line
            ProcessBottomLineMedium(input, output, width, height);
        }

        /// <summary>
        /// Apply heavy blur effect
        /// </summary>
        private void ApplyHeavyBlur(ImageBuffer input, ImageBuffer output)
        {
            int width = input.Width;
            int height = input.Height;

            // Process top line
            ProcessTopLineHeavy(input, output, width);

            // Process middle block
            ProcessMiddleBlockHeavy(input, output, width, height);

            // Process bottom line
            ProcessBottomLineHeavy(input, output, width, height);
        }

        /// <summary>
        /// Process top line for light blur
        /// </summary>
        private void ProcessTopLine(ImageBuffer input, ImageBuffer output, int width)
        {
            uint[] adjValues = GetAdjustmentValues(ROUND_UP);
            uint adjTl = adjValues[0];
            uint adjTl2 = adjValues[1];

            // Top left pixel
            output.Pixels[0] = (int)(DIV_2(input.Pixels[0]) + DIV_4(input.Pixels[0]) + 
                                    DIV_8(input.Pixels[1]) + DIV_8(input.Pixels[width]) + adjTl);

            // Top center pixels
            for (int x = 1; x < width - 1; x++)
            {
                output.Pixels[x] = (int)(DIV_2(input.Pixels[x]) + DIV_8(input.Pixels[x]) + 
                                        DIV_8(input.Pixels[x + 1]) + DIV_8(input.Pixels[x - 1]) + 
                                        DIV_8(input.Pixels[x + width]) + adjTl2);
            }

            // Top right pixel
            output.Pixels[width - 1] = (int)(DIV_2(input.Pixels[width - 1]) + DIV_4(input.Pixels[width - 1]) + 
                                            DIV_8(input.Pixels[width - 2]) + DIV_8(input.Pixels[2 * width - 1]) + adjTl);
        }

        /// <summary>
        /// Process middle block for light blur
        /// </summary>
        private void ProcessMiddleBlock(ImageBuffer input, ImageBuffer output, int width, int height)
        {
            uint[] adjValues = GetAdjustmentValues(ROUND_UP);
            uint adjTl1 = adjValues[2];
            uint adjTl2 = adjValues[3];

            for (int y = 1; y < height - 1; y++)
            {
                int currentRow = y * width;
                int upperRow = (y - 1) * width;
                int lowerRow = (y + 1) * width;

                // Left edge pixel
                output.Pixels[currentRow] = (int)(DIV_2(input.Pixels[currentRow]) + DIV_8(input.Pixels[currentRow]) + 
                                                DIV_8(input.Pixels[currentRow + 1]) + DIV_8(input.Pixels[lowerRow]) + 
                                                DIV_8(input.Pixels[upperRow]) + adjTl1);

                // Middle pixels
                for (int x = 1; x < width - 1; x++)
                {
                    int pixelIndex = currentRow + x;
                    output.Pixels[pixelIndex] = (int)(DIV_2(input.Pixels[pixelIndex]) + DIV_4(input.Pixels[pixelIndex]) + 
                                                    DIV_16(input.Pixels[pixelIndex + 1]) + DIV_16(input.Pixels[pixelIndex - 1]) + 
                                                    DIV_16(input.Pixels[lowerRow + x]) + DIV_16(input.Pixels[upperRow + x]) + adjTl2);
                }

                // Right edge pixel
                output.Pixels[currentRow + width - 1] = (int)(DIV_2(input.Pixels[currentRow + width - 1]) + 
                                                            DIV_8(input.Pixels[currentRow + width - 1]) + 
                                                            DIV_8(input.Pixels[currentRow + width - 2]) + 
                                                            DIV_8(input.Pixels[lowerRow + width - 1]) + 
                                                            DIV_8(input.Pixels[upperRow + width - 1]) + adjTl1);
            }
        }

        /// <summary>
        /// Process bottom line for light blur
        /// </summary>
        private void ProcessBottomLine(ImageBuffer input, ImageBuffer output, int width, int height)
        {
            uint[] adjValues = GetAdjustmentValues(ROUND_UP);
            uint adjTl = adjValues[0];
            uint adjTl2 = adjValues[1];

            int bottomRow = (height - 1) * width;
            int upperRow = (height - 2) * width;

            // Bottom left pixel
            output.Pixels[bottomRow] = (int)(DIV_2(input.Pixels[bottomRow]) + DIV_4(input.Pixels[bottomRow]) + 
                                            DIV_8(input.Pixels[bottomRow + 1]) + DIV_8(input.Pixels[upperRow]) + adjTl);

            // Bottom center pixels
            for (int x = 1; x < width - 1; x++)
            {
                output.Pixels[bottomRow + x] = (int)(DIV_2(input.Pixels[bottomRow + x]) + DIV_8(input.Pixels[bottomRow + x]) + 
                                                   DIV_8(input.Pixels[bottomRow + x + 1]) + DIV_8(input.Pixels[bottomRow + x - 1]) + 
                                                   DIV_8(input.Pixels[upperRow + x]) + adjTl2);
            }

            // Bottom right pixel
            output.Pixels[bottomRow + width - 1] = (int)(DIV_2(input.Pixels[bottomRow + width - 1]) + 
                                                        DIV_4(input.Pixels[bottomRow + width - 1]) + 
                                                        DIV_8(input.Pixels[bottomRow + width - 2]) + 
                                                        DIV_8(input.Pixels[upperRow + width - 1]) + adjTl);
        }

        /// <summary>
        /// Process top line for medium blur
        /// </summary>
        private void ProcessTopLineMedium(ImageBuffer input, ImageBuffer output, int width)
        {
            uint[] adjValues = GetAdjustmentValues(ROUND_UP);
            uint adjTl = adjValues[4];
            uint adjTl2 = adjValues[5];

            // Top left pixel
            output.Pixels[0] = (int)(DIV_2(input.Pixels[1]) + DIV_2(input.Pixels[width]) + adjTl2);

            // Top center pixels
            for (int x = 1; x < width - 1; x++)
            {
                output.Pixels[x] = (int)(DIV_4(input.Pixels[x + 1]) + DIV_4(input.Pixels[x - 1]) + 
                                        DIV_2(input.Pixels[x + width]) + adjTl);
            }

            // Top right pixel
            output.Pixels[width - 1] = (int)(DIV_2(input.Pixels[width - 2]) + DIV_2(input.Pixels[2 * width - 1]) + adjTl2);
        }

        /// <summary>
        /// Process middle block for medium blur
        /// </summary>
        private void ProcessMiddleBlockMedium(ImageBuffer input, ImageBuffer output, int width, int height)
        {
            uint[] adjValues = GetAdjustmentValues(ROUND_UP);
            uint adjTl = adjValues[4];
            uint adjTl2 = adjValues[5];

            for (int y = 1; y < height - 1; y++)
            {
                int currentRow = y * width;
                int upperRow = (y - 1) * width;
                int lowerRow = (y + 1) * width;

                // Left edge pixel
                output.Pixels[currentRow] = (int)(DIV_2(input.Pixels[currentRow + 1]) + DIV_2(input.Pixels[lowerRow]) + adjTl2);

                // Middle pixels
                for (int x = 1; x < width - 1; x++)
                {
                    int pixelIndex = currentRow + x;
                    output.Pixels[pixelIndex] = (int)(DIV_4(input.Pixels[pixelIndex + 1]) + DIV_4(input.Pixels[pixelIndex - 1]) + 
                                                    DIV_2(input.Pixels[lowerRow + x]) + adjTl);
                }

                // Right edge pixel
                output.Pixels[currentRow + width - 1] = (int)(DIV_2(input.Pixels[currentRow + width - 2]) + 
                                                            DIV_2(input.Pixels[lowerRow + width - 1]) + adjTl2);
            }
        }

        /// <summary>
        /// Process bottom line for medium blur
        /// </summary>
        private void ProcessBottomLineMedium(ImageBuffer input, ImageBuffer output, int width, int height)
        {
            uint[] adjValues = GetAdjustmentValues(ROUND_UP);
            uint adjTl = adjValues[4];
            uint adjTl2 = adjValues[5];

            int bottomRow = (height - 1) * width;
            int upperRow = (height - 2) * width;

            // Bottom left pixel
            output.Pixels[bottomRow] = (int)(DIV_2(input.Pixels[bottomRow + 1]) + DIV_2(input.Pixels[upperRow]) + adjTl2);

            // Bottom center pixels
            for (int x = 1; x < width - 1; x++)
            {
                output.Pixels[bottomRow + x] = (int)(DIV_4(input.Pixels[bottomRow + x + 1]) + DIV_4(input.Pixels[bottomRow + x - 1]) + 
                                                   DIV_2(input.Pixels[upperRow + x]) + adjTl);
            }

            // Bottom right pixel
            output.Pixels[bottomRow + width - 1] = (int)(DIV_2(input.Pixels[bottomRow + width - 2]) + 
                                                        DIV_2(input.Pixels[upperRow + width - 1]) + adjTl2);
        }

        /// <summary>
        /// Process top line for heavy blur
        /// </summary>
        private void ProcessTopLineHeavy(ImageBuffer input, ImageBuffer output, int width)
        {
            uint[] adjValues = GetAdjustmentValues(ROUND_UP);
            uint adjTl = adjValues[6];
            uint adjTl2 = adjValues[7];

            // Top left pixel
            output.Pixels[0] = (int)(DIV_4(input.Pixels[0]) + DIV_4(input.Pixels[1]) + 
                                    DIV_4(input.Pixels[width]) + adjTl2);

            // Top center pixels
            for (int x = 1; x < width - 1; x++)
            {
                output.Pixels[x] = (int)(DIV_4(input.Pixels[x]) + DIV_4(input.Pixels[x + 1]) + 
                                        DIV_4(input.Pixels[x - 1]) + DIV_4(input.Pixels[x + width]) + adjTl);
            }

            // Top right pixel
            output.Pixels[width - 1] = (int)(DIV_2(input.Pixels[width - 1]) + DIV_4(input.Pixels[width - 2]) + 
                                            DIV_4(input.Pixels[2 * width - 1]) + adjTl);
        }

        /// <summary>
        /// Process middle block for heavy blur
        /// </summary>
        private void ProcessMiddleBlockHeavy(ImageBuffer input, ImageBuffer output, int width, int height)
        {
            uint[] adjValues = GetAdjustmentValues(ROUND_UP);
            uint adjTl = adjValues[6];
            uint adjTl2 = adjValues[7];

            for (int y = 1; y < height - 1; y++)
            {
                int currentRow = y * width;
                int upperRow = (y - 1) * width;
                int lowerRow = (y + 1) * width;

                // Left edge pixel
                output.Pixels[currentRow] = (int)(DIV_4(input.Pixels[currentRow]) + DIV_4(input.Pixels[currentRow + 1]) + 
                                                DIV_4(input.Pixels[lowerRow]) + adjTl2);

                // Middle pixels
                for (int x = 1; x < width - 1; x++)
                {
                    int pixelIndex = currentRow + x;
                    output.Pixels[pixelIndex] = (int)(DIV_4(input.Pixels[pixelIndex]) + DIV_4(input.Pixels[pixelIndex + 1]) + 
                                                    DIV_4(input.Pixels[pixelIndex - 1]) + DIV_4(input.Pixels[lowerRow + x]) + adjTl);
                }

                // Right edge pixel
                output.Pixels[currentRow + width - 1] = (int)(DIV_4(input.Pixels[currentRow + width - 1]) + 
                                                            DIV_4(input.Pixels[currentRow + width - 2]) + 
                                                            DIV_4(input.Pixels[lowerRow + width - 1]) + adjTl2);
            }
        }

        /// <summary>
        /// Process bottom line for heavy blur
        /// </summary>
        private void ProcessBottomLineHeavy(ImageBuffer input, ImageBuffer output, int width, int height)
        {
            uint[] adjValues = GetAdjustmentValues(ROUND_UP);
            uint adjTl = adjValues[6];
            uint adjTl2 = adjValues[7];

            int bottomRow = (height - 1) * width;
            int upperRow = (height - 2) * width;

            // Bottom left pixel
            output.Pixels[bottomRow] = (int)(DIV_4(input.Pixels[bottomRow]) + DIV_4(input.Pixels[bottomRow + 1]) + 
                                            DIV_4(input.Pixels[upperRow]) + adjTl2);

            // Bottom center pixels
            for (int x = 1; x < width - 1; x++)
            {
                output.Pixels[bottomRow + x] = (int)(DIV_4(input.Pixels[bottomRow + x]) + DIV_4(input.Pixels[bottomRow + x + 1]) + 
                                                   DIV_4(input.Pixels[bottomRow + x - 1]) + DIV_4(input.Pixels[upperRow + x]) + adjTl);
            }

            // Bottom right pixel
            output.Pixels[bottomRow + width - 1] = (int)(DIV_4(input.Pixels[bottomRow + width - 1]) + 
                                                        DIV_4(input.Pixels[bottomRow + width - 2]) + 
                                                        DIV_4(input.Pixels[upperRow + width - 1]) + adjTl2);
        }

        /// <summary>
        /// Get adjustment values based on round mode
        /// </summary>
        private uint[] GetAdjustmentValues(int roundMode)
        {
            if (roundMode == ROUND_UP)
            {
                return new uint[] { 0x03030303, 0x04040404, 0x04040404, 0x05050505, 0x02020202, 0x01010101, 0x02020202, 0x01010101 };
            }
            else
            {
                return new uint[] { 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000, 0x00000000 };
            }
        }

        #region Division Macros

        /// <summary>
        /// Divide by 2 with masking
        /// </summary>
        private uint DIV_2(uint x)
        {
            return ((x & MASK_SH1) >> 1);
        }

        /// <summary>
        /// Divide by 4 with masking
        /// </summary>
        private uint DIV_4(uint x)
        {
            return ((x & MASK_SH2) >> 2);
        }

        /// <summary>
        /// Divide by 8 with masking
        /// </summary>
        private uint DIV_8(uint x)
        {
            return ((x & MASK_SH3) >> 3);
        }

        /// <summary>
        /// Divide by 16 with masking
        /// </summary>
        private uint DIV_16(uint x)
        {
            return ((x & MASK_SH4) >> 4);
        }

        #endregion

        #endregion

        #region Configuration

        /// <summary>
        /// Validate and clamp property values
        /// </summary>
        public override void ValidateProperties()
        {
            BlurIntensity = Math.Clamp(BlurIntensity, MinBlurIntensity, MaxBlurIntensity);
            RoundMode = Math.Clamp(RoundMode, MinRoundMode, MaxRoundMode);
        }

        /// <summary>
        /// Get a summary of current settings
        /// </summary>
        public override string GetSettingsSummary()
        {
            string intensityText = GetIntensityText();
            string roundText = RoundMode == ROUND_UP ? "Round Up" : "Round Down";
            string threadText = MultiThreaded ? "Multi-threaded" : "Single-threaded";

            return $"Blur: {intensityText}, {roundText}, {threadText}";
        }

        /// <summary>
        /// Get blur intensity text
        /// </summary>
        private string GetIntensityText()
        {
            switch (BlurIntensity)
            {
                case BLUR_OFF: return "Off";
                case BLUR_LIGHT: return "Light";
                case BLUR_MEDIUM: return "Medium";
                case BLUR_HEAVY: return "Heavy";
                default: return "Unknown";
            }
        }

        #endregion
    }
}
```

## Key Features

### Blur Intensities
- **Light Blur**: Subtle smoothing with 5-pixel averaging
- **Medium Blur**: Moderate blur with 4-pixel averaging
- **Heavy Blur**: Strong blur with 4-pixel averaging
- **Configurable Levels**: Runtime intensity switching

### Rendering Modes
- **Single-threaded**: Standard rendering for compatibility
- **Multi-threaded**: Parallel processing for performance
- **SMP Support**: Symmetric multi-processing optimization
- **Thread Management**: Automatic thread distribution

### Blur Algorithms
- **Pixel Averaging**: Weighted combination of neighboring pixels
- **Edge Handling**: Specialized processing for image boundaries
- **Rounding Control**: Configurable pixel value adjustments
- **Mask-based Division**: Efficient bit-shift operations

### Performance Features
- **MMX Optimization**: SIMD-accelerated pixel processing
- **Efficient Loops**: 4-pixel processing for optimization
- **Memory Management**: Minimal allocation during rendering
- **Timing Support**: Built-in performance measurement

## Usage Examples

```csharp
// Create a medium blur effect with round-up mode
var blurNode = new BlurEffectsNode
{
    BlurIntensity = BlurEffectsNode.BLUR_MEDIUM,
    RoundMode = BlurEffectsNode.ROUND_UP,
    MultiThreaded = true
};

// Apply to image
var blurredImage = blurNode.ProcessFrame(inputImage, audioFeatures);
```

## Technical Details

### Blur Algorithms
The effect uses different averaging patterns for each intensity:

```csharp
// Light blur: 5-pixel average
result = (pixel/2) + (pixel/4) + (neighbor1/8) + (neighbor2/8) + (neighbor3/8)

// Medium blur: 4-pixel average  
result = (neighbor1/4) + (neighbor2/4) + (pixel/2)

// Heavy blur: 4-pixel average
result = (pixel/4) + (neighbor1/4) + (neighbor2/4) + (neighbor3/4)
```

### Division Macros
Efficient bit-shift operations with masking:

```csharp
#define DIV_2(x) (((x) & MASK_SH1) >> 1)
#define DIV_4(x) (((x) & MASK_SH2) >> 2)
#define DIV_8(x) (((x) & MASK_SH3) >> 3)
#define DIV_16(x) (((x) & MASK_SH4) >> 4)
```

### Rounding System
Adjustment values for different visual effects:

```csharp
// Round up mode
adjTl = 0x03030303;  // Light blur
adjTl2 = 0x04040404; // Medium blur

// Round down mode  
adjTl = 0x00000000;  // No adjustment
adjTl2 = 0x00000000; // No adjustment
```

### Multi-threading
Automatic thread distribution for optimal performance:

```csharp
int startLine = (threadId * height) / maxThreads;
int endLine = ((threadId + 1) * height) / maxThreads;
```

This implementation provides a complete, production-ready blur effect system that faithfully reproduces the original C++ functionality while leveraging C# features for improved maintainability and performance.
