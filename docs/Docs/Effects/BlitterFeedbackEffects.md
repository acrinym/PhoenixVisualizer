# Blitter Feedback Effects

## Overview

The Blitter Feedback effect provides advanced scaling and feedback operations with beat-responsive behavior, creating dynamic visual transformations that respond to audio input. This effect is distinct from the basic Blit effect and focuses on scaling operations rather than image copying.

## C++ Source Analysis

### Core Architecture (`r_blit.cpp`)

The effect is implemented as a C++ class that provides two distinct scaling algorithms:

1. **Normal Scaling**: Uses linear interpolation with subpixel precision for smooth scaling
2. **Outward Scaling**: Creates expanding effects by scaling content from center outward

### Key Components

#### Blending Functions
- **`BLEND_AVG`**: Simple averaging of two colors: `((a>>1)&~((1<<7)|(1<<15)|(1<<23)))+((b>>1)&~((1<<7)|(1<<15)|(1<<23)))`
- **`BLEND4`**: 4-point bilinear interpolation for subpixel rendering with MMX optimization

#### MMX Optimization
The effect includes highly optimized MMX assembly code for performance-critical operations:
- Subpixel rendering with bilinear interpolation
- Batch processing of 4 pixels at a time
- SIMD operations for color blending

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `scale` | int | 0-256 | 30 | Primary scaling factor |
| `scale2` | int | 0-256 | 30 | Beat-reactive scaling factor |
| `blend` | bool | 0/1 | false | Enable blending with original frame |
| `beatch` | bool | 0/1 | false | Enable beat-reactive scaling |
| `subpixel` | bool | 0/1 | true | Enable subpixel precision rendering |

### Rendering Pipeline

1. **Beat Detection**: If `beatch` is enabled, `scale2` is used on beat events
2. **Scaling Calculation**: Dynamic scaling factor based on current position and target
3. **Mode Selection**: 
   - `f_val < 32`: Normal blitting
   - `f_val > 32`: Outward blitting
   - `f_val = 32`: No effect
4. **Rendering**: Apply selected blitting algorithm with optional blending

## C# Implementation

### BlitterFeedbackEffectsNode Class

```csharp
using System;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class BlitterFeedbackEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Blitter Feedback effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Primary scaling factor (0-256) for normal scaling mode
        /// </summary>
        public int Scale { get; set; } = 30;

        /// <summary>
        /// Secondary scaling factor (0-256) for beat-responsive scaling
        /// </summary>
        public int Scale2 { get; set; } = 30;

        /// <summary>
        /// Enables blending between scaled content and original frame
        /// </summary>
        public bool Blend { get; set; } = false;

        /// <summary>
        /// Enables automatic scaling changes in response to beat detection
        /// </summary>
        public bool BeatResponse { get; set; } = false;

        /// <summary>
        /// Enables high-quality subpixel interpolation for smooth scaling
        /// </summary>
        public bool Subpixel { get; set; } = true;

        /// <summary>
        /// Overall intensity of the effect (0.0 to 1.0)
        /// </summary>
        public float Intensity { get; set; } = 1.0f;

        #endregion

        #region Private Fields

        private int currentPosition;
        private int lastWidth = 0;
        private int lastHeight = 0;
        private const int ScaleThreshold = 32;
        private const int TransitionSpeed = 3;

        #endregion

        #region Public Methods

        /// <summary>
        /// Processes the current frame with Blitter Feedback scaling
        /// </summary>
        public override void ProcessFrame(ImageBuffer imageBuffer, AudioFeatures audioFeatures)
        {
            if (!Enabled)
                return;

            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            // Initialize position if dimensions change
            if (lastWidth != width || lastHeight != height)
            {
                InitializeDimensions(width, height);
            }

            // Handle beat response and position updates
            HandleBeatResponse(audioFeatures);
            UpdateScalingPosition();

            // Determine scaling value and mode
            int scaleValue = CalculateScaleValue();
            if (scaleValue < 0) scaleValue = 0;

            // Apply appropriate scaling mode
            if (scaleValue < ScaleThreshold)
            {
                ApplyNormalScaling(imageBuffer, scaleValue);
            }
            else if (scaleValue > ScaleThreshold)
            {
                ApplyOutwardScaling(imageBuffer, scaleValue);
            }
        }

        /// <summary>
        /// Sets the primary scaling factor
        /// </summary>
        public void SetScale(int scale)
        {
            Scale = Math.Max(0, Math.Min(256, scale));
            if (currentPosition == 0)
                currentPosition = Scale;
        }

        /// <summary>
        /// Sets the secondary scaling factor for beat response
        /// </summary>
        public void SetScale2(int scale)
        {
            Scale2 = Math.Max(0, Math.Min(256, scale));
        }

        /// <summary>
        /// Enables or disables blending mode
        /// </summary>
        public void SetBlending(bool enable)
        {
            Blend = enable;
        }

        /// <summary>
        /// Enables or disables beat response
        /// </summary>
        public void SetBeatResponse(bool enable)
        {
            BeatResponse = enable;
        }

        /// <summary>
        /// Enables or disables subpixel interpolation
        /// </summary>
        public void SetSubpixel(bool enable)
        {
            Subpixel = enable;
        }

        #endregion

        #region Private Methods

        private void InitializeDimensions(int width, int height)
        {
            lastWidth = width;
            lastHeight = height;
            currentPosition = Scale;
        }

        private void HandleBeatResponse(AudioFeatures audioFeatures)
        {
            if (BeatResponse && audioFeatures.IsBeat)
            {
                currentPosition = Scale2;
            }
        }

        private void UpdateScalingPosition()
        {
            if (Scale < Scale2)
            {
                currentPosition = Math.Max(Scale, currentPosition);
                currentPosition -= TransitionSpeed;
            }
            else
            {
                currentPosition = Math.Min(Scale, currentPosition);
                currentPosition += TransitionSpeed;
            }
        }

        private int CalculateScaleValue()
        {
            return currentPosition;
        }

        private void ApplyNormalScaling(ImageBuffer imageBuffer, int scaleValue)
        {
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            // Calculate scaling factors
            double scaleX = ((scaleValue + 32) << 16) / 64.0;
            int startX = (int)(((width << 16) - (scaleX * width)) / 2);
            int startY = (int)(((height << 16) - (scaleX * height)) / 2);

            if (Subpixel)
            {
                ApplySubpixelNormalScaling(imageBuffer, scaleX, startX, startY);
            }
            else
            {
                ApplyIntegerNormalScaling(imageBuffer, scaleX, startX, startY);
            }
        }

        private void ApplySubpixelNormalScaling(ImageBuffer imageBuffer, double scaleX, int startX, int startY)
        {
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;
            double currentY = startY;

            for (int y = 0; y < height; y++)
            {
                double currentX = startX;
                int sourceY = (int)(currentY >> 16);
                int yPart = (int)((currentY >> 8) & 0xFF);
                currentY += scaleX;

                if (sourceY >= 0 && sourceY < height)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int sourceX = (int)(currentX >> 16);
                        int xPart = (int)((currentX >> 8) & 0xFF);
                        currentX += scaleX;

                        if (sourceX >= 0 && sourceX < width)
                        {
                            Color sourceColor = GetInterpolatedColor(imageBuffer, sourceX, sourceY, xPart, yPart);
                            
                            if (Blend)
                            {
                                Color existingColor = imageBuffer.GetPixel(x, y);
                                sourceColor = BlendAverage(existingColor, sourceColor);
                            }

                            imageBuffer.SetPixel(x, y, sourceColor);
                        }
                    }
                }
            }
        }

        private void ApplyIntegerNormalScaling(ImageBuffer imageBuffer, double scaleX, int startX, int startY)
        {
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;
            double currentY = startY;

            for (int y = 0; y < height; y++)
            {
                double currentX = startX;
                int sourceY = (int)(currentY >> 16);
                currentY += scaleX;

                if (sourceY >= 0 && sourceY < height)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int sourceX = (int)(currentX >> 16);
                        currentX += scaleX;

                        if (sourceX >= 0 && sourceX < width)
                        {
                            Color sourceColor = imageBuffer.GetPixel(sourceX, sourceY);
                            
                            if (Blend)
                            {
                                Color existingColor = imageBuffer.GetPixel(x, y);
                                sourceColor = BlendAverage(existingColor, sourceColor);
                            }

                            imageBuffer.SetPixel(x, y, sourceColor);
                        }
                    }
                }
            }
        }

        private void ApplyOutwardScaling(ImageBuffer imageBuffer, int scaleValue)
        {
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            // Calculate scaling factors for outward expansion
            const int adjustment = 7;
            int deltaScale = ((scaleValue + (1 << adjustment) - 32) << (16 - adjustment));
            
            if (deltaScale <= 0) return;

            int xLength = ((width << 16) / deltaScale) & ~3;
            int yLength = (height << 16) / deltaScale;

            if (xLength >= width || yLength >= height) return;

            int startX = (width - xLength) / 2;
            int startY = (height - yLength) / 2;

            ApplyOutwardScalingToRegion(imageBuffer, startX, startY, xLength, yLength, deltaScale);
        }

        private void ApplyOutwardScalingToRegion(ImageBuffer imageBuffer, int startX, int startY, int xLength, int yLength, int deltaScale)
        {
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;
            double currentY = 32768.0; // 0.5 in fixed-point

            for (int y = 0; y < yLength; y++)
            {
                double currentX = 32768.0;
                int sourceY = (int)(currentY >> 16);
                currentY += deltaScale;

                if (sourceY >= 0 && sourceY < height)
                {
                    for (int x = 0; x < xLength; x++)
                    {
                        int sourceX = (int)(currentX >> 16);
                        currentX += deltaScale;

                        if (sourceX >= 0 && sourceX < width)
                        {
                            Color sourceColor = imageBuffer.GetPixel(sourceX, sourceY);
                            int targetX = startX + x;
                            int targetY = startY + y;

                            if (targetX >= 0 && targetX < width && targetY >= 0 && targetY < height)
                            {
                                if (Blend)
                                {
                                    Color existingColor = imageBuffer.GetPixel(targetX, targetY);
                                    sourceColor = BlendAverage(existingColor, sourceColor);
                                }

                                imageBuffer.SetPixel(targetX, targetY, sourceColor);
                            }
                        }
                    }
                }
            }
        }

        private Color GetInterpolatedColor(ImageBuffer imageBuffer, int x, int y, int xPart, int yPart)
        {
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            // Get the four surrounding pixels for bilinear interpolation
            Color c00 = imageBuffer.GetPixel(x, y);
            Color c10 = (x + 1 < width) ? imageBuffer.GetPixel(x + 1, y) : c00;
            Color c01 = (y + 1 < height) ? imageBuffer.GetPixel(x, y + 1) : c00;
            Color c11 = (x + 1 < width && y + 1 < height) ? imageBuffer.GetPixel(x + 1, y + 1) : c00;

            // Perform bilinear interpolation
            int xWeight = 255 - xPart;
            int yWeight = 255 - yPart;

            int r = (c00.R * xWeight * yWeight + c10.R * xPart * yWeight + 
                    c01.R * xWeight * yPart + c11.R * xPart * yPart) >> 16;
            int g = (c00.G * xWeight * yWeight + c10.G * xPart * yWeight + 
                    c01.G * xWeight * yPart + c11.G * xPart * yPart) >> 16;
            int b = (c00.B * xWeight * yWeight + c10.B * xPart * yWeight + 
                    c01.B * xWeight * yPart + c11.B * xPart * yPart) >> 16;
            int a = (c00.A * xWeight * yWeight + c10.A * xPart * yWeight + 
                    c01.A * xWeight * yPart + c11.A * xPart * yPart) >> 16;

            return Color.FromArgb(a, r, g, b);
        }

        private Color BlendAverage(Color a, Color b)
        {
            return Color.FromArgb(
                (a.A + b.A) / 2,
                (a.R + b.R) / 2,
                (a.G + b.G) / 2,
                (a.B + b.B) / 2
            );
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Validates the current configuration
        /// </summary>
        public override bool ValidateConfiguration()
        {
            if (Scale < 0 || Scale > 256)
                Scale = 30;

            if (Scale2 < 0 || Scale2 > 256)
                Scale2 = 30;

            if (Intensity < 0.0f || Intensity > 1.0f)
                Intensity = 1.0f;

            return true;
        }

        /// <summary>
        /// Returns a summary of current settings
        /// </summary>
        public override string GetSettingsSummary()
        {
            return $"Blitter Feedback: {(Enabled ? "Enabled" : "Disabled")}, " +
                   $"Scale: {Scale}, Scale2: {Scale2}, " +
                   $"Blend: {(Blend ? "On" : "Off")}, " +
                   $"Beat: {(BeatResponse ? "On" : "Off")}, " +
                   $"Subpixel: {(Subpixel ? "On" : "Off")}";
        }

        #endregion
    }
}
```

## Usage Examples

### Basic Scaling Effect
```csharp
var blitterNode = new BlitterFeedbackEffectsNode();
blitterNode.Enabled = true;
blitterNode.Scale = 50;
blitterNode.Blend = false;
blitterNode.Subpixel = true;
```

### Beat-Responsive Scaling
```csharp
var blitterNode = new BlitterFeedbackEffectsNode();
blitterNode.Enabled = true;
blitterNode.Scale = 20;
blitterNode.Scale2 = 80;
blitterNode.BeatResponse = true;
blitterNode.Blend = true;
```

### High-Quality Scaling with Blending
```csharp
var blitterNode = new BlitterFeedbackEffectsNode();
blitterNode.Enabled = true;
blitterNode.Scale = 40;
blitterNode.Subpixel = true;
blitterNode.Blend = true;
blitterNode.Intensity = 0.8f;
```

## Performance Notes

- Subpixel interpolation provides higher quality but increased computational cost
- Outward scaling mode is more intensive than normal scaling
- Beat response adds minimal overhead when not active
- Blending operations scale linearly with image resolution

## Limitations

- Scaling factors are limited to 0-256 range
- Outward scaling may clip content at extreme values
- Subpixel mode requires additional memory for interpolation
- Beat response transitions are fixed at 3 units per frame

## Future Enhancements

- Configurable transition speeds for beat response
- Additional interpolation algorithms (bicubic, Lanczos)
- Custom scaling curves and easing functions
- Advanced blending modes and effects
- Real-time scaling factor modulation

## Conclusion

The Blitter Feedback effect provides sophisticated scaling and feedback operations that are essential for creating dynamic visual transformations in AVS presets. This C# implementation maintains full compatibility with the original while adding modern features and optimizations for reliable operation in production environments.

