# AVS Contrast Enhancement Effect

## Overview
The Contrast Enhancement effect is an AVS visualization effect that enhances image contrast by selectively replacing colors based on their relationship to a threshold color. It provides four different modes: OFF, BELOW (replace colors below threshold), ABOVE (replace colors above threshold), and NEAR (replace colors within a distance threshold). This effect is essential for creating dramatic contrast changes and color isolation in AVS presets.

## C++ Source Analysis
Based on the AVS source code in `r_contrast.cpp`, the Contrast Enhancement effect inherits from `C_RBASE` and implements sophisticated color thresholding and replacement:

### Key Properties
- **Enabled State**: Four different operation modes (OFF, BELOW, ABOVE, NEAR)
- **Color Clip**: Threshold color for comparison operations
- **Color Clip Out**: Replacement color for matched pixels
- **Color Distance**: Distance threshold for NEAR mode operations
- **Alpha Channel Safe**: Preserves alpha channel during processing

### Core Functionality
```cpp
class C_THISCLASS : public C_RBASE 
{
    public:
        virtual int render(char visdata[2][2][576], int isBeat, int *framebuffer, int *fbout, int w, int h);
        virtual char *get_desc();
        virtual HWND conf(HINSTANCE hInstance, HWND hwndParent);
        virtual void load_config(unsigned char *data, int len);
        virtual int save_config(unsigned char *data);

    int enabled;           // Operation mode (0=OFF, 1=BELOW, 2=ABOVE, 3=NEAR)
    int color_clip;        // Threshold color for comparison
    int color_clip_out;    // Replacement color for matched pixels
    int color_dist;        // Distance threshold for NEAR mode
};
```

### Operation Modes
1. **OFF (0)**: No processing - original image unchanged
2. **BELOW (1)**: Replace colors below threshold with replacement color
3. **ABOVE (2)**: Replace colors above threshold with replacement color
4. **NEAR (3)**: Replace colors within distance threshold with replacement color

## C# Implementation

### Class Definition
```csharp
public class ContrastEnhancementEffectsNode : BaseEffectNode
{
    public int EnhancementMode { get; set; } = 1;
    public int ThresholdColor { get; set; } = 0x202020; // RGB(32,32,32)
    public int ReplacementColor { get; set; } = 0x202020;
    public float DistanceThreshold { get; set; } = 10.0f;
    public bool BeatReactive { get; set; } = false;
    public float BeatThresholdMultiplier { get; set; } = 1.5f;
    public bool EnableSmoothTransition { get; set; } = false;
    public float TransitionSpeed { get; set; } = 1.0f;
    public int ThresholdAlgorithm { get; set; } = 0;
    public float ThresholdSensitivity { get; set; } = 1.0f;
    public bool EnableColorPreservation { get; set; } = false;
    public float ColorPreservationStrength { get; set; } = 0.5f;
    public bool EnableAdaptiveThreshold { get; set; } = false;
    public float AdaptiveSensitivity { get; set; } = 0.1f;
    public int AdaptiveWindowSize { get; set; } = 16;
    public bool EnableMultiThreshold { get; set; } = false;
    public int[] MultiThresholdColors { get; set; } = new int[0];
    public int[] MultiReplacementColors { get; set; } = new int[0];
    public float[] MultiThresholdDistances { get; set; } = new float[0];
    public bool EnableThresholdAnimation { get; set; } = false;
    public float AnimationSpeed { get; set; } = 1.0f;
    public int AnimationMode { get; set; } = 0;
}
```

### Key Features
- **Four Enhancement Modes**: OFF, BELOW, ABOVE, NEAR with full control
- **Beat Reactivity**: Dynamic threshold adjustment synchronized with audio
- **Smooth Transitions**: Optional smooth threshold and color transitions
- **Adaptive Thresholding**: Automatic threshold calculation based on image content
- **Multi-Threshold Support**: Multiple threshold/replacement color pairs
- **Color Preservation**: Optional preservation of original color characteristics
- **Animation Support**: Animated threshold and color changes
- **Performance Optimization**: Optimized pixel processing algorithms

### Usage Examples

#### Basic Below Threshold Enhancement
```csharp
var contrastNode = new ContrastEnhancementEffectsNode
{
    EnhancementMode = 1, // BELOW mode
    ThresholdColor = 0x404040, // RGB(64,64,64)
    ReplacementColor = 0x000000, // Black
    DistanceThreshold = 5.0f,
    BeatReactive = false
};
```

#### Beat-Reactive Above Threshold Enhancement
```csharp
var beatContrastNode = new ContrastEnhancementEffectsNode
{
    EnhancementMode = 2, // ABOVE mode
    ThresholdColor = 0x808080, // RGB(128,128,128)
    ReplacementColor = 0xFFFFFF, // White
    DistanceThreshold = 15.0f,
    BeatReactive = true,
    BeatThresholdMultiplier = 2.0f,
    EnableSmoothTransition = true,
    TransitionSpeed = 2.0f
};
```

#### Advanced Near Threshold Enhancement
```csharp
var advancedContrastNode = new ContrastEnhancementEffectsNode
{
    EnhancementMode = 3, // NEAR mode
    ThresholdColor = 0x404040, // RGB(64,64,64)
    ReplacementColor = 0xFF0000, // Red
    DistanceThreshold = 20.0f,
    BeatReactive = true,
    EnableAdaptiveThreshold = true,
    AdaptiveSensitivity = 0.2f,
    AdaptiveWindowSize = 32,
    EnableColorPreservation = true,
    ColorPreservationStrength = 0.7f
};
```

## Technical Implementation

### Core Processing Algorithm
```csharp
protected override ImageBuffer ProcessEffect(ImageBuffer input, AudioFeatures audio)
{
    if (EnhancementMode == 0) // OFF mode
        return input;
    
    var output = new ImageBuffer(input.Width, input.Height);
    var currentThreshold = GetCurrentThreshold(audio);
    var currentReplacement = GetCurrentReplacementColor(audio);
    
    // Process each pixel
    for (int i = 0; i < input.Pixels.Length; i++)
    {
        var originalColor = input.Pixels[i];
        var processedColor = ProcessPixel(originalColor, currentThreshold, currentReplacement);
        
        // Apply color preservation if enabled
        if (EnableColorPreservation)
        {
            processedColor = PreserveColorCharacteristics(originalColor, processedColor);
        }
        
        output.Pixels[i] = processedColor;
    }
    
    return output;
}
```

### Pixel Processing Implementation
```csharp
private int ProcessPixel(int color, int threshold, int replacement)
{
    var r = color & 0xFF;
    var g = (color >> 8) & 0xFF;
    var b = (color >> 16) & 0xFF;
    var a = (color >> 24) & 0xFF;
    
    var thresholdR = threshold & 0xFF;
    var thresholdG = (threshold >> 8) & 0xFF;
    var thresholdB = (threshold >> 16) & 0xFF;
    
    bool shouldReplace = false;
    
    switch (EnhancementMode)
    {
        case 1: // BELOW mode
            shouldReplace = (r <= thresholdR && g <= thresholdG && b <= thresholdB);
            break;
            
        case 2: // ABOVE mode
            shouldReplace = (r >= thresholdR && g >= thresholdG && b >= thresholdB);
            break;
            
        case 3: // NEAR mode
            var distance = CalculateColorDistance(r, g, b, thresholdR, thresholdG, thresholdB);
            shouldReplace = (distance <= DistanceThreshold);
            break;
    }
    
    if (shouldReplace)
    {
        // Preserve alpha channel
        return (a << 24) | (replacement & 0x00FFFFFF);
    }
    
    return color;
}
```

### Color Distance Calculation
```csharp
private float CalculateColorDistance(int r1, int g1, int b1, int r2, int g2, int b2)
{
    switch (ThresholdAlgorithm)
    {
        case 0: // Euclidean distance (default)
            var dr = r1 - r2;
            var dg = g1 - g2;
            var db = b1 - b2;
            return (float)Math.Sqrt(dr * dr + dg * dg + db * db);
            
        case 1: // Manhattan distance
            return Math.Abs(r1 - r2) + Math.Abs(g1 - g2) + Math.Abs(b1 - b2);
            
        case 2: // Chebyshev distance
            return Math.Max(Math.Max(Math.Abs(r1 - r2), Math.Abs(g1 - g2)), Math.Abs(b1 - b2));
            
        case 3: // Weighted Euclidean (perceptual)
            var luma1 = 0.299f * r1 + 0.587f * g1 + 0.114f * b1;
            var luma2 = 0.299f * r2 + 0.587f * g2 + 0.114f * b2;
            var lumaDiff = luma1 - luma2;
            var chromaDiff = CalculateColorDistance(r1, g1, b1, r2, g2, b2);
            return (float)Math.Sqrt(lumaDiff * lumaDiff + chromaDiff * chromaDiff);
            
        default:
            return CalculateColorDistance(r1, g1, b1, r2, g2, b2);
    }
}
```

### Beat-Reactive Threshold Adjustment
```csharp
private int GetCurrentThreshold(AudioFeatures audio)
{
    if (!BeatReactive || audio == null)
        return ThresholdColor;
    
    var beatMultiplier = 1.0f;
    
    if (audio.IsBeat)
    {
        beatMultiplier = BeatThresholdMultiplier;
    }
    else
    {
        // Gradual return to normal
        beatMultiplier = 1.0f + (BeatThresholdMultiplier - 1.0f) * audio.BeatIntensity;
    }
    
    return AdjustColorIntensity(ThresholdColor, beatMultiplier);
}

private int AdjustColorIntensity(int color, float multiplier)
{
    var r = (int)((color & 0xFF) * multiplier);
    var g = (int)(((color >> 8) & 0xFF) * multiplier);
    var b = (int)(((color >> 16) & 0xFF) * multiplier);
    
    r = Math.Max(0, Math.Min(255, r));
    g = Math.Max(0, Math.Min(255, g));
    b = Math.Max(0, Math.Min(255, b));
    
    return (color & 0xFF000000) | (b << 16) | (g << 8) | r;
}
```

### Adaptive Thresholding
```csharp
private int CalculateAdaptiveThreshold(ImageBuffer input, int x, int y)
{
    if (!EnableAdaptiveThreshold)
        return ThresholdColor;
    
    var windowSize = AdaptiveWindowSize;
    var halfWindow = windowSize / 2;
    var sumR = 0; var sumG = 0; var sumB = 0;
    var count = 0;
    
    // Sample pixels in window around current pixel
    for (int wy = Math.Max(0, y - halfWindow); wy < Math.Min(input.Height, y + halfWindow + 1); wy++)
    {
        for (int wx = Math.Max(0, x - halfWindow); wx < Math.Min(input.Width, x + halfWindow + 1); wx++)
        {
            var pixel = input.GetPixel(wx, wy);
            sumR += pixel & 0xFF;
            sumG += (pixel >> 8) & 0xFF;
            sumB += (pixel >> 16) & 0xFF;
            count++;
        }
    }
    
    if (count == 0)
        return ThresholdColor;
    
    var avgR = sumR / count;
    var avgG = sumG / count;
    var avgB = sumB / count;
    
    // Apply adaptive sensitivity
    var adaptiveR = (int)(avgR * AdaptiveSensitivity + (ThresholdColor & 0xFF) * (1.0f - AdaptiveSensitivity));
    var adaptiveG = (int)(avgG * AdaptiveSensitivity + ((ThresholdColor >> 8) & 0xFF) * (1.0f - AdaptiveSensitivity));
    var adaptiveB = (int)(avgB * AdaptiveSensitivity + ((ThresholdColor >> 16) & 0xFF) * (1.0f - AdaptiveSensitivity));
    
    return (adaptiveB << 16) | (adaptiveG << 8) | adaptiveR;
}
```

### Multi-Threshold Processing
```csharp
private int ProcessMultiThreshold(int color, int x, int y)
{
    if (!EnableMultiThreshold || MultiThresholdColors.Length == 0)
        return ProcessPixel(color, ThresholdColor, ReplacementColor);
    
    var bestMatch = -1;
    var bestDistance = float.MaxValue;
    
    // Find the closest threshold color
    for (int i = 0; i < MultiThresholdColors.Length; i++)
    {
        var thresholdColor = MultiThresholdColors[i];
        var distance = CalculateColorDistance(color, thresholdColor);
        var maxDistance = MultiThresholdDistances.Length > i ? MultiThresholdDistances[i] : DistanceThreshold;
        
        if (distance <= maxDistance && distance < bestDistance)
        {
            bestDistance = distance;
            bestMatch = i;
        }
    }
    
    if (bestMatch >= 0)
    {
        var replacementColor = MultiReplacementColors.Length > bestMatch ? 
            MultiReplacementColors[bestMatch] : ReplacementColor;
        return (color & 0xFF000000) | (replacementColor & 0x00FFFFFF);
    }
    
    return color;
}
```

### Color Preservation
```csharp
private int PreserveColorCharacteristics(int originalColor, int processedColor)
{
    if (!EnableColorPreservation)
        return processedColor;
    
    var originalR = originalColor & 0xFF;
    var originalG = (originalColor >> 8) & 0xFF;
    var originalB = (originalColor >> 16) & 0xFF;
    
    var processedR = processedColor & 0xFF;
    var processedG = (processedColor >> 8) & 0xFF;
    var processedB = (processedColor >> 16) & 0xFF;
    
    // Blend original and processed colors
    var blendStrength = ColorPreservationStrength;
    var finalR = (int)(originalR * blendStrength + processedR * (1.0f - blendStrength));
    var finalG = (int)(originalG * blendStrength + processedG * (1.0f - blendStrength));
    var finalB = (int)(originalB * blendStrength + processedB * (1.0f - blendStrength));
    
    return (originalColor & 0xFF000000) | (finalB << 16) | (finalG << 8) | finalR;
}
```

## Performance Considerations

### Optimization Strategies
- **Early Exit**: Skip processing for OFF mode
- **SIMD Operations**: Use vectorized operations for pixel processing
- **Memory Access**: Optimize pixel buffer access patterns
- **Branch Prediction**: Minimize conditional branches in hot paths
- **Lookup Tables**: Pre-compute threshold calculations where possible

### Memory Management
- **Buffer Reuse**: Reuse ImageBuffer instances when possible
- **Temporary Buffers**: Minimize temporary buffer allocations
- **Garbage Collection**: Avoid allocations in hot paths

## Integration with EffectGraph

### Input Ports
- **Image Input**: Source image buffer for contrast enhancement
- **Audio Input**: Audio features for beat-reactive behavior
- **Control Input**: External control signals for dynamic parameters

### Output Ports
- **Enhanced Image**: The processed image with applied contrast enhancement
- **Threshold Data**: Threshold and replacement color metadata
- **Performance Metrics**: Processing time and memory usage

### Node Configuration
```csharp
public override void ConfigureNode()
{
    AddInputPort("Image", typeof(ImageBuffer));
    AddInputPort("Audio", typeof(AudioFeatures));
    AddInputPort("Control", typeof(float));
    
    AddOutputPort("EnhancedImage", typeof(ImageBuffer));
    AddOutputPort("ThresholdData", typeof(ThresholdData));
    AddOutputPort("Performance", typeof(PerformanceMetrics));
    
    SetMetadata(new EffectMetadata
    {
        Name = "Contrast Enhancement",
        Category = "Color Enhancement",
        Description = "Enhances image contrast through selective color replacement",
        Version = "1.0.0",
        Author = "Phoenix Visualizer Team"
    });
}
```

## Advanced Features

### Threshold Animation
```csharp
private void UpdateThresholdAnimation(float deltaTime)
{
    if (!EnableThresholdAnimation)
        return;
    
    var animationProgress = (GetCurrentTime() * AnimationSpeed) % (Math.PI * 2);
    
    switch (AnimationMode)
    {
        case 0: // Pulsing
            var pulse = (Math.Sin(animationProgress) + 1) * 0.5f;
            DistanceThreshold = 5.0f + pulse * 15.0f;
            break;
            
        case 1: // Rotating colors
            var hue = (animationProgress / (Math.PI * 2)) * 360.0f;
            ThresholdColor = HsvToRgb(hue, 0.5f, 0.5f);
            break;
            
        case 2: // Wave pattern
            var wave = Math.Sin(animationProgress * 3);
            DistanceThreshold = 10.0f + wave * 10.0f;
            break;
            
        case 3: // Random walk
            if (Random.NextDouble() < 0.01f) // 1% chance per frame
            {
                ThresholdColor = Random.Next(0x000000, 0xFFFFFF);
            }
            break;
    }
}
```

### Smooth Transitions
```csharp
private int GetCurrentReplacementColor(AudioFeatures audio)
{
    if (!EnableSmoothTransition)
        return ReplacementColor;
    
    var targetColor = GetTargetReplacementColor(audio);
    var transitionProgress = GetTransitionProgress();
    
    if (transitionProgress <= 0.0f)
        return ReplacementColor;
    
    if (transitionProgress >= 1.0f)
        return targetColor;
    
    return InterpolateColors(ReplacementColor, targetColor, transitionProgress);
}

private int InterpolateColors(int color1, int color2, float progress)
{
    var r1 = color1 & 0xFF;
    var g1 = (color1 >> 8) & 0xFF;
    var b1 = (color1 >> 16) & 0xFF;
    
    var r2 = color2 & 0xFF;
    var g2 = (color2 >> 8) & 0xFF;
    var b2 = (color2 >> 16) & 0xFF;
    
    var r = (int)(r1 + (r2 - r1) * progress);
    var g = (int)(g1 + (g2 - g1) * progress);
    var b = (int)(b1 + (b2 - b1) * progress);
    
    return (b << 16) | (g << 8) | r;
}
```

## Testing and Validation

### Unit Tests
```csharp
[Test]
public void TestBelowThresholdEnhancement()
{
    var node = new ContrastEnhancementEffectsNode
    {
        EnhancementMode = 1, // BELOW mode
        ThresholdColor = 0x404040,
        ReplacementColor = 0x000000,
        DistanceThreshold = 5.0f
    };
    
    var input = CreateTestImage(100, 100);
    var audio = CreateTestAudio();
    
    var output = node.ProcessEffect(input, audio);
    
    Assert.IsNotNull(output);
    Assert.AreEqual(100, output.Width);
    Assert.AreEqual(100, output.Height);
    
    // Test that dark pixels are replaced
    var darkPixel = 0x202020; // RGB(32,32,32) - below threshold
    var expectedPixel = 0x000000; // Black replacement
    
    var processedPixel = node.ProcessPixel(darkPixel, 0x404040, 0x000000);
    Assert.AreEqual(expectedPixel, processedPixel & 0x00FFFFFF);
}
```

### Performance Tests
```csharp
[Test]
public void TestPerformance()
{
    var node = new ContrastEnhancementEffectsNode();
    var input = CreateTestImage(1920, 1080);
    var audio = CreateTestAudio();
    
    var stopwatch = Stopwatch.StartNew();
    var output = node.ProcessEffect(input, audio);
    stopwatch.Stop();
    
    var processingTime = stopwatch.ElapsedMilliseconds;
    Assert.Less(processingTime, 50); // Should process in under 50ms
}
```

## Future Enhancements

### Planned Features
- **HSV/HSL Support**: Threshold operations in different color spaces
- **Histogram Analysis**: Automatic threshold calculation from image histograms
- **Edge Detection**: Edge-aware contrast enhancement
- **Real-time Control**: MIDI and OSC control integration
- **Preset System**: Predefined enhancement presets

### Research Areas
- **Perceptual Contrast**: Human visual system considerations
- **Machine Learning**: AI-generated threshold optimization
- **Real-time Analysis**: Dynamic threshold analysis and adjustment
- **Color Theory**: Advanced color manipulation algorithms

## Conclusion
The Contrast Enhancement effect provides powerful image contrast manipulation capabilities with extensive customization options. Its beat-reactive nature and adaptive thresholding make it suitable for creating dynamic, engaging visualizations. The implementation balances performance with flexibility, offering both real-time processing and high-quality output suitable for professional visualization applications.

The effect's ability to selectively replace colors based on sophisticated thresholding algorithms makes it an essential tool for AVS preset creation, allowing artists to create dramatic contrast changes, isolate specific color ranges, and create visually striking effects that respond to music.

## Complete C# Implementation

### ContrastEffectsNode Class

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class ContrastEffectsNode : BaseEffectNode
    {
        #region Operation Modes

        public enum ContrastMode
        {
            Off = 0,           // Effect disabled
            BelowThreshold = 1, // Replace colors below threshold
            AboveThreshold = 2, // Replace colors above threshold
            NearColor = 3       // Replace colors near reference color
        }

        #endregion

        #region Properties

        /// <summary>
        /// Enable/disable the contrast effect
        /// </summary>
        public ContrastMode Mode { get; set; } = ContrastMode.BelowThreshold;

        /// <summary>
        /// Color threshold for clipping operations
        /// </summary>
        public Color ColorThreshold { get; set; } = Color.FromArgb(32, 32, 32);

        /// <summary>
        /// Output color for replacement
        /// </summary>
        public Color OutputColor { get; set; } = Color.FromArgb(32, 32, 32);

        /// <summary>
        /// Color distance threshold for proximity-based replacement (0-64)
        /// </summary>
        public int ColorDistance { get; set; } = 10;

        /// <summary>
        /// Effect intensity multiplier
        /// </summary>
        public float Intensity { get; set; } = 1.0f;

        /// <summary>
        /// Enable alpha channel preservation
        /// </summary>
        public bool PreserveAlpha { get; set; } = true;

        #endregion

        #region Private Fields

        private int _thresholdRed;
        private int _thresholdGreen;
        private int _thresholdBlue;
        private int _distanceSquared;

        #endregion

        #region Constructor

        public ContrastEffectsNode()
        {
            UpdateThresholds();
        }

        #endregion

        #region Processing

        public override void ProcessFrame(ImageBuffer imageBuffer, AudioFeatures audioFeatures)
        {
            if (!Enabled || Mode == ContrastMode.Off) return;

            // Update internal thresholds
            UpdateThresholds();

            // Apply contrast effect
            ApplyContrastEffect(imageBuffer);
        }

        private void UpdateThresholds()
        {
            _thresholdRed = ColorThreshold.R;
            _thresholdGreen = ColorThreshold.G;
            _thresholdBlue = ColorThreshold.B;
            _distanceSquared = ColorDistance * ColorDistance;
        }

        private void ApplyContrastEffect(ImageBuffer imageBuffer)
        {
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color originalColor = imageBuffer.GetPixel(x, y);
                    Color processedColor = ProcessPixel(originalColor);

                    // Apply intensity if not full strength
                    if (Intensity != 1.0f)
                    {
                        processedColor = ApplyIntensity(originalColor, processedColor);
                    }

                    imageBuffer.SetPixel(x, y, processedColor);
                }
            }
        }

        private Color ProcessPixel(Color originalColor)
        {
            switch (Mode)
            {
                case ContrastMode.BelowThreshold:
                    return ProcessBelowThreshold(originalColor);

                case ContrastMode.AboveThreshold:
                    return ProcessAboveThreshold(originalColor);

                case ContrastMode.NearColor:
                    return ProcessNearColor(originalColor);

                default:
                    return originalColor;
            }
        }

        private Color ProcessBelowThreshold(Color color)
        {
            // Check if all color components are below threshold
            if (color.R <= _thresholdRed && 
                color.G <= _thresholdGreen && 
                color.B <= _thresholdBlue)
            {
                return CreateOutputColor(color.A);
            }

            return color;
        }

        private Color ProcessAboveThreshold(Color color)
        {
            // Check if all color components are above threshold
            if (color.R >= _thresholdRed && 
                color.G >= _thresholdGreen && 
                color.B >= _thresholdBlue)
            {
                return CreateOutputColor(color.A);
            }

            return color;
        }

        private Color ProcessNearColor(Color color)
        {
            // Calculate color distance using Euclidean distance
            int redDiff = color.R - _thresholdRed;
            int greenDiff = color.G - _thresholdGreen;
            int blueDiff = color.B - _thresholdBlue;

            int distanceSquared = redDiff * redDiff + greenDiff * greenDiff + blueDiff * blueDiff;

            // Replace color if within distance threshold
            if (distanceSquared <= _distanceSquared)
            {
                return CreateOutputColor(color.A);
            }

            return color;
        }

        private Color CreateOutputColor(byte alpha)
        {
            if (PreserveAlpha)
            {
                return Color.FromArgb(alpha, OutputColor.R, OutputColor.G, OutputColor.B);
            }
            else
            {
                return OutputColor;
            }
        }

        private Color ApplyIntensity(Color original, Color processed)
        {
            // Blend between original and processed color based on intensity
            float intensity = Math.Max(0.0f, Math.Min(1.0f, Intensity));

            int red = (int)(original.R * (1.0f - intensity) + processed.R * intensity);
            int green = (int)(original.G * (1.0f - intensity) + processed.G * intensity);
            int blue = (int)(original.B * (1.0f - intensity) + processed.B * intensity);

            // Clamp values
            red = Math.Max(0, Math.Min(255, red));
            green = Math.Max(0, Math.Min(255, green));
            blue = Math.Max(0, Math.Min(255, blue));

            return Color.FromArgb(original.A, red, green, blue);
        }

        #endregion

        #region Configuration

        public override bool ValidateConfiguration()
        {
            // Validate property ranges
            ColorDistance = Math.Max(0, Math.Min(64, ColorDistance));
            Intensity = Math.Max(0.0f, Math.Min(5.0f, Intensity));

            return true;
        }

        public override string GetSettingsSummary()
        {
            string mode = Mode switch
            {
                ContrastMode.Off => "Off",
                ContrastMode.BelowThreshold => "Below Threshold",
                ContrastMode.AboveThreshold => "Above Threshold",
                ContrastMode.NearColor => "Near Color",
                _ => "Unknown"
            };

            return $"Contrast Effect - Mode: {mode}, " +
                   $"Threshold: {ColorThreshold.Name}, Output: {OutputColor.Name}, " +
                   $"Distance: {ColorDistance}, Intensity: {Intensity:F2}, " +
                   $"Preserve Alpha: {PreserveAlpha}";
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set the output color to match the threshold color
        /// </summary>
        public void SyncOutputColor()
        {
            OutputColor = ColorThreshold;
        }

        /// <summary>
        /// Create a high contrast effect by setting extreme thresholds
        /// </summary>
        public void SetHighContrast()
        {
            ColorThreshold = Color.FromArgb(128, 128, 128);
            OutputColor = Color.Black;
            Mode = ContrastMode.BelowThreshold;
        }

        /// <summary>
        /// Create a low contrast effect by setting moderate thresholds
        /// </summary>
        public void SetLowContrast()
        {
            ColorThreshold = Color.FromArgb(64, 64, 64);
            OutputColor = Color.FromArgb(128, 128, 128);
            Mode = ContrastMode.BelowThreshold;
        }

        /// <summary>
        /// Set up for color isolation (replace colors near a specific color)
        /// </summary>
        /// <param name="targetColor">Color to isolate</param>
        /// <param name="tolerance">Color tolerance (0-64)</param>
        public void SetupColorIsolation(Color targetColor, int tolerance)
        {
            ColorThreshold = targetColor;
            OutputColor = Color.White;
            ColorDistance = tolerance;
            Mode = ContrastMode.NearColor;
        }

        /// <summary>
        /// Set up for shadow enhancement
        /// </summary>
        public void SetupShadowEnhancement()
        {
            ColorThreshold = Color.FromArgb(64, 64, 64);
            OutputColor = Color.Black;
            Mode = ContrastMode.BelowThreshold;
        }

        /// <summary>
        /// Set up for highlight enhancement
        /// </summary>
        public void SetupHighlightEnhancement()
        {
            ColorThreshold = Color.FromArgb(192, 192, 192);
            OutputColor = Color.White;
            Mode = ContrastMode.AboveThreshold;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Calculate color distance between two colors
        /// </summary>
        /// <param name="color1">First color</param>
        /// <param name="color2">Second color</param>
        /// <returns>Euclidean distance between colors</returns>
        public static double CalculateColorDistance(Color color1, Color color2)
        {
            int redDiff = color1.R - color2.R;
            int greenDiff = color1.G - color2.G;
            int blueDiff = color1.B - color2.B;

            return Math.Sqrt(redDiff * redDiff + greenDiff * greenDiff + blueDiff * blueDiff);
        }

        /// <summary>
        /// Check if a color is within distance threshold of another
        /// </summary>
        /// <param name="color1">First color</param>
        /// <param name="color2">Second color</param>
        /// <param name="threshold">Distance threshold</param>
        /// <returns>True if colors are within threshold</returns>
        public static bool IsColorNear(Color color1, Color color2, int threshold)
        {
            return CalculateColorDistance(color1, color2) <= threshold;
        }

        #endregion
    }
}
```

## Effect Behavior

The Contrast effect provides advanced color manipulation by:

1. **Threshold-Based Clipping**: Replace colors above or below specific brightness thresholds
2. **Proximity-Based Replacement**: Replace colors within a specified distance of a reference color
3. **Flexible Color Output**: Customizable replacement colors with alpha channel preservation
4. **Multiple Operation Modes**: Different clipping strategies for various visual effects
5. **Intensity Control**: Adjustable effect strength for subtle to dramatic results
6. **Performance Optimization**: Efficient color processing with minimal computational overhead

## Operation Modes

### Below Threshold Mode
- **Purpose**: Replace dark colors with specified output color
- **Use Case**: Shadow enhancement, dark area isolation
- **Behavior**: Colors with all components below threshold are replaced

### Above Threshold Mode
- **Purpose**: Replace bright colors with specified output color
- **Use Case**: Highlight enhancement, bright area isolation
- **Behavior**: Colors with all components above threshold are replaced

### Near Color Mode
- **Purpose**: Replace colors similar to a reference color
- **Use Case**: Color isolation, selective color replacement
- **Behavior**: Colors within distance threshold are replaced

## Key Features

- **Multiple Clipping Modes**: Three distinct operation modes for different effects
- **Color Distance Calculation**: Euclidean distance-based color similarity
- **Alpha Channel Preservation**: Maintains transparency information
- **Intensity Control**: Adjustable effect strength
- **Performance Optimized**: Efficient pixel processing algorithms
- **Flexible Configuration**: Customizable thresholds and output colors

## Configuration Options

- **Mode Selection**: Choose between different clipping strategies
- **Color Thresholds**: Set RGB values for clipping operations
- **Output Color**: Specify replacement color for clipped pixels
- **Distance Threshold**: Control color similarity tolerance (0-64)
- **Intensity**: Adjust effect strength (0.1-5.0)
- **Alpha Preservation**: Maintain or override transparency

## Use Cases

- **Color Correction**: Isolate and replace specific color ranges
- **Artistic Effects**: Create high contrast or stylized looks
- **Image Enhancement**: Improve shadow and highlight detail
- **Color Isolation**: Extract or replace specific color elements
- **Video Processing**: Real-time color manipulation for live streams
- **Photography**: Post-processing color adjustments and effects
