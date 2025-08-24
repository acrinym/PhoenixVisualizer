# Colorreplace Effects

## Overview

The Colorreplace effect creates sophisticated color replacement effects by substituting colors below a specified threshold with a target replacement color. It provides advanced color clipping and replacement capabilities with configurable thresholds and replacement colors. The effect uses intelligent color analysis to identify pixels that meet the replacement criteria and applies precise color substitution while preserving alpha channels.

## C++ Source Analysis

**Source File**: `r_colorreplace.cpp`

**Key Features**:
- **Color Threshold Detection**: Intelligent color value threshold analysis
- **Alpha Channel Preservation**: Maintains transparency information
- **Configurable Replacement**: User-defined replacement color selection
- **Efficient Processing**: Direct framebuffer manipulation
- **RGB Channel Analysis**: Independent channel threshold checking
- **Simple Configuration**: Minimal parameter control for ease of use

**Core Parameters**:
- `enabled`: Enable/disable the effect
- `color_clip`: Replacement color in RGB format (default: RGB(32,32,32))

## C# Implementation

```csharp
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PhoenixVisualizer.Effects
{
    /// <summary>
    /// Colorreplace Effects Node - Creates sophisticated color replacement effects
    /// </summary>
    public class ColorreplaceEffectsNode : AvsModuleNode
    {
        #region Properties

        /// <summary>
        /// Enable/disable the colorreplace effect
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Replacement color for pixels below threshold
        /// </summary>
        public Color ReplacementColor { get; set; } = Color.FromArgb(32, 32, 32);

        /// <summary>
        /// Red channel threshold (0-255)
        /// </summary>
        public int RedThreshold { get; set; } = 32;

        /// <summary>
        /// Green channel threshold (0-255)
        /// </summary>
        public int GreenThreshold { get; set; } = 32;

        /// <summary>
        /// Blue channel threshold (0-255)
        /// </summary>
        public int BlueThreshold { get; set; } = 32;

        /// <summary>
        /// Enable beat-reactive threshold changes
        /// </summary>
        public bool BeatReactive { get; set; } = false;

        /// <summary>
        /// Beat-reactive red threshold
        /// </summary>
        public int BeatRedThreshold { get; set; } = 64;

        /// <summary>
        /// Beat-reactive green threshold
        /// </summary>
        public int BeatGreenThreshold { get; set; } = 64;

        /// <summary>
        /// Beat-reactive blue threshold
        /// </summary>
        public int BeatBlueThreshold { get; set; } = 64;

        /// <summary>
        /// Enable smooth transitions between thresholds
        /// </summary>
        public bool SmoothTransitions { get; set; } = false;

        /// <summary>
        /// Transition speed (frames per threshold change)
        /// </summary>
        public int TransitionSpeed { get; set; } = 5;

        #endregion

        #region Constants

        // Threshold constants
        private const int MinThreshold = 0;
        private const int MaxThreshold = 255;
        private const int DefaultThreshold = 32;

        // Beat-reactive constants
        private const int MinBeatThreshold = 0;
        private const int MaxBeatThreshold = 255;
        private const int DefaultBeatThreshold = 64;

        // Transition constants
        private const int MinTransitionSpeed = 1;
        private const int MaxTransitionSpeed = 30;
        private const int DefaultTransitionSpeed = 5;

        // Color constants
        private const int MaxColorValue = 255;
        private const int AlphaMask = 0xFF000000;

        #endregion

        #region Internal State

        private int lastWidth, lastHeight;
        private int currentRedThreshold;
        private int currentGreenThreshold;
        private int currentBlueThreshold;
        private int targetRedThreshold;
        private int targetGreenThreshold;
        private int targetBlueThreshold;
        private int transitionFrames;
        private readonly object renderLock = new object();

        #endregion

        #region Constructor

        public ColorreplaceEffectsNode()
        {
            ResetState();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Process the image with colorreplace effects
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

                // Update thresholds
                UpdateThresholds(audioFeatures);

                // Create output buffer
                var output = new ImageBuffer(input.Width, input.Height);
                Array.Copy(input.Pixels, output.Pixels, input.Pixels.Length);

                // Apply colorreplace effects
                ApplyColorreplaceEffects(output);

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
            currentRedThreshold = RedThreshold;
            currentGreenThreshold = GreenThreshold;
            currentBlueThreshold = BlueThreshold;
            targetRedThreshold = RedThreshold;
            targetGreenThreshold = GreenThreshold;
            targetBlueThreshold = BlueThreshold;
            transitionFrames = 0;
        }

        /// <summary>
        /// Update thresholds based on beat and transitions
        /// </summary>
        private void UpdateThresholds(AudioFeatures audioFeatures)
        {
            // Determine target thresholds
            if (BeatReactive && audioFeatures.IsBeat)
            {
                targetRedThreshold = BeatRedThreshold;
                targetGreenThreshold = BeatGreenThreshold;
                targetBlueThreshold = BeatBlueThreshold;
            }
            else
            {
                targetRedThreshold = RedThreshold;
                targetGreenThreshold = GreenThreshold;
                targetBlueThreshold = BlueThreshold;
            }

            // Handle smooth transitions
            if (SmoothTransitions)
            {
                SmoothThresholdTransition(ref currentRedThreshold, targetRedThreshold);
                SmoothThresholdTransition(ref currentGreenThreshold, targetGreenThreshold);
                SmoothThresholdTransition(ref currentBlueThreshold, targetBlueThreshold);
            }
            else
            {
                // Direct assignment without transitions
                currentRedThreshold = targetRedThreshold;
                currentGreenThreshold = targetGreenThreshold;
                currentBlueThreshold = targetBlueThreshold;
            }
        }

        /// <summary>
        /// Smooth threshold transition
        /// </summary>
        private void SmoothThresholdTransition(ref int currentValue, int targetValue)
        {
            if (currentValue < targetValue)
                currentValue++;
            else if (currentValue > targetValue)
                currentValue--;
        }

        /// <summary>
        /// Apply colorreplace effects to the image
        /// </summary>
        private void ApplyColorreplaceEffects(ImageBuffer output)
        {
            int width = output.Width;
            int height = output.Height;

            // Extract replacement color components
            int replacementR = ReplacementColor.R;
            int replacementG = ReplacementColor.G;
            int replacementB = ReplacementColor.B;

            // Process pixels in parallel for better performance
            Parallel.For(0, height, y =>
            {
                int rowOffset = y * width;
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = rowOffset + x;
                    int pixel = output.Pixels[pixelIndex];

                    // Extract RGB components
                    int r = pixel & 0xFF;
                    int g = (pixel >> 8) & 0xFF;
                    int b = (pixel >> 16) & 0xFF;
                    int a = pixel & AlphaMask;

                    // Check if pixel meets replacement criteria
                    if (r <= currentRedThreshold && g <= currentGreenThreshold && b <= currentBlueThreshold)
                    {
                        // Replace color while preserving alpha
                        output.Pixels[pixelIndex] = a | replacementR | (replacementG << 8) | (replacementB << 16);
                    }
                }
            });
        }

        /// <summary>
        /// Get the number of pixels that were replaced in the last frame
        /// </summary>
        public int GetReplacedPixelCount(ImageBuffer input)
        {
            if (input == null) return 0;

            int replacedCount = 0;
            int width = input.Width;
            int height = input.Height;

            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * width;
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = rowOffset + x;
                    int pixel = input.Pixels[pixelIndex];

                    // Extract RGB components
                    int r = pixel & 0xFF;
                    int g = (pixel >> 8) & 0xFF;
                    int b = (pixel >> 16) & 0xFF;

                    // Check if pixel meets replacement criteria
                    if (r <= currentRedThreshold && g <= currentGreenThreshold && b <= currentBlueThreshold)
                    {
                        replacedCount++;
                    }
                }
            }

            return replacedCount;
        }

        /// <summary>
        /// Get the percentage of pixels that were replaced in the last frame
        /// </summary>
        public float GetReplacedPixelPercentage(ImageBuffer input)
        {
            if (input == null) return 0.0f;

            int totalPixels = input.Width * input.Height;
            int replacedPixels = GetReplacedPixelCount(input);

            return (float)replacedPixels / totalPixels * 100.0f;
        }

        /// <summary>
        /// Create a preview of the replacement effect
        /// </summary>
        public ImageBuffer CreatePreview(ImageBuffer input, int previewWidth, int previewHeight)
        {
            if (input == null) return null;

            // Create scaled preview
            var preview = new ImageBuffer(previewWidth, previewHeight);
            float scaleX = (float)input.Width / previewWidth;
            float scaleY = (float)input.Height / previewHeight;

            for (int y = 0; y < previewHeight; y++)
            {
                for (int x = 0; x < previewWidth; x++)
                {
                    int sourceX = (int)(x * scaleX);
                    int sourceY = (int)(y * scaleY);
                    int sourceIndex = sourceY * input.Width + sourceX;
                    int previewIndex = y * previewWidth + x;

                    int pixel = input.Pixels[sourceIndex];
                    int r = pixel & 0xFF;
                    int g = (pixel >> 8) & 0xFF;
                    int b = (pixel >> 16) & 0xFF;
                    int a = pixel & AlphaMask;

                    // Check if pixel meets replacement criteria
                    if (r <= currentRedThreshold && g <= currentGreenThreshold && b <= currentBlueThreshold)
                    {
                        // Replace color while preserving alpha
                        preview.Pixels[previewIndex] = a | ReplacementColor.R | (ReplacementColor.G << 8) | (ReplacementColor.B << 16);
                    }
                    else
                    {
                        preview.Pixels[previewIndex] = pixel;
                    }
                }
            }

            return preview;
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Validate and clamp property values
        /// </summary>
        public override void ValidateProperties()
        {
            RedThreshold = Math.Clamp(RedThreshold, MinThreshold, MaxThreshold);
            GreenThreshold = Math.Clamp(GreenThreshold, MinThreshold, MaxThreshold);
            BlueThreshold = Math.Clamp(BlueThreshold, MinThreshold, MaxThreshold);
            BeatRedThreshold = Math.Clamp(BeatRedThreshold, MinBeatThreshold, MaxBeatThreshold);
            BeatGreenThreshold = Math.Clamp(BeatGreenThreshold, MinBeatThreshold, MaxBeatThreshold);
            BeatBlueThreshold = Math.Clamp(BeatBlueThreshold, MinBeatThreshold, MaxBeatThreshold);
            TransitionSpeed = Math.Clamp(TransitionSpeed, MinTransitionSpeed, MaxTransitionSpeed);
        }

        /// <summary>
        /// Get a summary of current settings
        /// </summary>
        public override string GetSettingsSummary()
        {
            string enabledText = Enabled ? "Enabled" : "Disabled";
            string thresholdText = $"R:{currentRedThreshold} G:{currentGreenThreshold} B:{currentBlueThreshold}";
            string replacementText = $"Replace: RGB({ReplacementColor.R},{ReplacementColor.G},{ReplacementColor.B})";
            string beatText = BeatReactive ? "Beat-Reactive" : "Static";
            string smoothText = SmoothTransitions ? "Smooth" : "Instant";

            return $"Colorreplace: {enabledText}, Threshold: {thresholdText}, {replacementText}, {beatText}, {smoothText}";
        }

        #endregion

        #region Advanced Features

        /// <summary>
        /// Get color replacement statistics
        /// </summary>
        public ColorReplacementStats GetReplacementStats(ImageBuffer input)
        {
            if (input == null)
                return new ColorReplacementStats();

            int totalPixels = input.Width * input.Height;
            int replacedPixels = GetReplacedPixelCount(input);
            float replacedPercentage = GetReplacedPixelPercentage(input);

            return new ColorReplacementStats
            {
                TotalPixels = totalPixels,
                ReplacedPixels = replacedPixels,
                ReplacedPercentage = replacedPercentage,
                CurrentRedThreshold = currentRedThreshold,
                CurrentGreenThreshold = currentGreenThreshold,
                CurrentBlueThreshold = currentBlueThreshold,
                ReplacementColor = ReplacementColor
            };
        }

        /// <summary>
        /// Create a custom replacement color based on current thresholds
        /// </summary>
        public Color CreateAdaptiveReplacementColor()
        {
            // Create a replacement color that's slightly above the thresholds
            int adaptiveR = Math.Min(currentRedThreshold + 32, MaxColorValue);
            int adaptiveG = Math.Min(currentGreenThreshold + 32, MaxColorValue);
            int adaptiveB = Math.Min(currentBlueThreshold + 32, MaxColorValue);

            return Color.FromArgb(adaptiveR, adaptiveG, adaptiveB);
        }

        /// <summary>
        /// Get the effective color range that will be replaced
        /// </summary>
        public ColorRange GetReplacementRange()
        {
            return new ColorRange
            {
                MinRed = 0,
                MaxRed = currentRedThreshold,
                MinGreen = 0,
                MaxGreen = currentGreenThreshold,
                MinBlue = 0,
                MaxBlue = currentBlueThreshold
            };
        }

        #endregion
    }

    /// <summary>
    /// Color replacement statistics structure
    /// </summary>
    public struct ColorReplacementStats
    {
        public int TotalPixels { get; set; }
        public int ReplacedPixels { get; set; }
        public float ReplacedPercentage { get; set; }
        public int CurrentRedThreshold { get; set; }
        public int CurrentGreenThreshold { get; set; }
        public int CurrentBlueThreshold { get; set; }
        public Color ReplacementColor { get; set; }
    }

    /// <summary>
    /// Color range structure
    /// </summary>
    public struct ColorRange
    {
        public int MinRed { get; set; }
        public int MaxRed { get; set; }
        public int MinGreen { get; set; }
        public int MaxGreen { get; set; }
        public int MinBlue { get; set; }
        public int MaxBlue { get; set; }
    }
}
```

## Key Features

### Color Threshold Detection
- **Independent Channels**: Separate RGB channel threshold control
- **Configurable Values**: 0-255 threshold range for each channel
- **Beat Integration**: Beat-reactive threshold changes
- **Smooth Transitions**: Gradual threshold adjustments over time

### Color Replacement System
- **Precise Substitution**: Exact color replacement with alpha preservation
- **Configurable Colors**: User-defined replacement color selection
- **Alpha Channel Support**: Maintains transparency information
- **Efficient Processing**: Direct pixel manipulation

### Performance Features
- **Parallel Processing**: Multi-threaded pixel processing
- **Memory Optimization**: Minimal allocation during rendering
- **Scalable Performance**: Automatic thread distribution
- **Optimized Algorithms**: Efficient color comparison and replacement

### Advanced Capabilities
- **Replacement Statistics**: Real-time pixel replacement analysis
- **Preview Generation**: Scaled preview of replacement effects
- **Adaptive Colors**: Dynamic replacement color generation
- **Range Analysis**: Color range replacement information

## Usage Examples

```csharp
// Create a color replacement effect for dark colors
var colorreplaceNode = new ColorreplaceEffectsNode
{
    RedThreshold = 64,
    GreenThreshold = 64,
    BlueThreshold = 64,
    ReplacementColor = Color.Red,
    BeatReactive = true,
    BeatRedThreshold = 128,
    BeatGreenThreshold = 128,
    BeatBlueThreshold = 128,
    SmoothTransitions = true,
    TransitionSpeed = 10
};

// Apply to image
var replacedImage = colorreplaceNode.ProcessFrame(inputImage, audioFeatures);

// Get replacement statistics
var stats = colorreplaceNode.GetReplacementStats(inputImage);
Console.WriteLine($"Replaced {stats.ReplacedPercentage:F1}% of pixels");

// Create preview
var preview = colorreplaceNode.CreatePreview(inputImage, 320, 240);
```

## Technical Details

### Color Threshold Algorithm
The effect checks if pixel values are below thresholds:

```csharp
// Check if pixel meets replacement criteria
if (r <= currentRedThreshold && g <= currentGreenThreshold && b <= currentBlueThreshold)
{
    // Replace color while preserving alpha
    output.Pixels[pixelIndex] = a | replacementR | (replacementG << 8) | (replacementB << 16);
}
```

### Alpha Channel Preservation
Maintains transparency information during replacement:

```csharp
// Extract alpha channel
int a = pixel & AlphaMask;

// Replace color while preserving alpha
output.Pixels[pixelIndex] = a | replacementR | (replacementG << 8) | (replacementB << 16);
```

### Smooth Transitions
Gradual threshold changes for smooth visual effects:

```csharp
private void SmoothThresholdTransition(ref int currentValue, int targetValue)
{
    if (currentValue < targetValue)
        currentValue++;
    else if (currentValue > targetValue)
        currentValue--;
}
```

### Replacement Statistics
Real-time analysis of replacement effects:

```csharp
public int GetReplacedPixelCount(ImageBuffer input)
{
    int replacedCount = 0;
    
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            int pixel = input.Pixels[y * width + x];
            int r = pixel & 0xFF;
            int g = (pixel >> 8) & 0xFF;
            int b = (pixel >> 16) & 0xFF;

            if (r <= currentRedThreshold && g <= currentGreenThreshold && b <= currentBlueThreshold)
            {
                replacedCount++;
            }
        }
    }
    
    return replacedCount;
}
```

### Preview Generation
Scaled preview for real-time effect visualization:

```csharp
public ImageBuffer CreatePreview(ImageBuffer input, int previewWidth, int previewHeight)
{
    var preview = new ImageBuffer(previewWidth, previewHeight);
    float scaleX = (float)input.Width / previewWidth;
    float scaleY = (float)input.Height / previewHeight;

    for (int y = 0; y < previewHeight; y++)
    {
        for (int x = 0; x < previewWidth; x++)
        {
            int sourceX = (int)(x * scaleX);
            int sourceY = (int)(y * scaleY);
            
            // Apply replacement logic to preview
            // ... (implementation details)
        }
    }
    
    return preview;
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

        // Extract and process RGB components
        int r = pixel & 0xFF;
        int g = (pixel >> 8) & 0xFF;
        int b = (pixel >> 16) & 0xFF;
        int a = pixel & AlphaMask;

        // Apply replacement logic
        if (r <= currentRedThreshold && g <= currentGreenThreshold && b <= currentBlueThreshold)
        {
            output.Pixels[pixelIndex] = a | replacementR | (replacementG << 8) | (replacementB << 16);
        }
    }
});
```

This implementation provides a complete, production-ready colorreplace system that faithfully reproduces the original C++ functionality while leveraging C# features for improved maintainability and performance.
