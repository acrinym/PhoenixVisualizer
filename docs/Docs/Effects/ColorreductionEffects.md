# Color Reduction Effects

## Overview
The Color Reduction effect reduces the number of colors in an image by quantizing color values to a limited palette. It's essential for creating retro-style visualizations and reducing memory usage in AVS presets.

## C++ Source Analysis
**File:** `r_colorreduction.cpp`  
**Class:** `C_THISCLASS : public C_RBASE`

### Key Properties
- **Reduction Level**: Controls how many colors are reduced to
- **Reduction Method**: Different color reduction algorithms
- **Dithering**: Optional dithering for smoother color transitions
- **Palette Type**: Predefined or custom color palettes
- **Beat Reactivity**: Dynamic reduction level changes on beat detection

### Core Functionality
```cpp
class C_THISCLASS : public C_RBASE
{
    int level;
    int method;
    int dither;
    int palette;
    int onbeat;
    int level2;
    
    virtual int render(char visdata[2][2][576], int isBeat, int *framebuffer, int *fbout, int w, int h);
    virtual HWND conf(HINSTANCE hInstance, HWND hwndParent);
    virtual char *get_desc();
};
```

## C# Implementation

### ColorReductionEffectsNode Class
```csharp
public class ColorReductionEffectsNode : BaseEffectNode
{
    public int ReductionLevel { get; set; } = 256; // Number of colors to reduce to
    public int ReductionMethod { get; set; } = 0;
    public bool EnableDithering { get; set; } = false;
    public int DitheringType { get; set; } = 0;
    public int PaletteType { get; set; } = 0;
    public bool BeatReactive { get; set; } = false;
    public int BeatReductionLevel { get; set; } = 64;
    public float DitheringStrength { get; set; } = 1.0f;
    public int[] CustomPalette { get; set; } = null;
    public bool PreserveBrightness { get; set; } = true;
}
```

### Key Features
1. **Multiple Reduction Methods**: Different color reduction algorithms
2. **Dithering Support**: Smooth color transitions with dithering
3. **Palette Control**: Predefined and custom color palettes
4. **Level Control**: Adjustable color reduction intensity
5. **Beat Reactivity**: Dynamic reduction level changes on beat detection
6. **Brightness Preservation**: Maintain image brightness during reduction
7. **Performance Optimization**: Efficient color quantization algorithms

### Reduction Methods
- **0**: Uniform Quantization (Equal color space division)
- **1**: Median Cut (Color space optimization)
- **2**: Octree Quantization (Tree-based color reduction)
- **3**: K-Means Clustering (Statistical color clustering)
- **4**: Popularity Algorithm (Most common colors)
- **5**: Adaptive Quantization (Content-aware reduction)

### Dithering Types
- **0**: None (No dithering)
- **1**: Floyd-Steinberg (Error diffusion dithering)
- **2**: Ordered Dithering (Pattern-based dithering)
- **3**: Random Dithering (Stochastic dithering)
- **4**: Bayer Dithering (Bayer matrix dithering)

### Palette Types
- **0**: Grayscale (Black to white)
- **1**: RGB (Primary colors)
- **2**: CMY (Subtractive colors)
- **3**: Custom (User-defined palette)
- **4**: Adaptive (Image-specific palette)
- **5**: Retro (Classic color schemes)

## Usage Examples

### Basic Color Reduction
```csharp
var colorReductionNode = new ColorReductionEffectsNode
{
    ReductionLevel = 64,
    ReductionMethod = 0, // Uniform quantization
    EnableDithering = false,
    PaletteType = 0, // Grayscale
    PreserveBrightness = true
};
```

### Beat-Reactive Color Reduction
```csharp
var colorReductionNode = new ColorReductionEffectsNode
{
    ReductionLevel = 128,
    ReductionMethod = 1, // Median cut
    EnableDithering = true,
    DitheringType = 1, // Floyd-Steinberg
    BeatReactive = true,
    BeatReductionLevel = 32,
    PaletteType = 1, // RGB
    DitheringStrength = 0.8f
};
```

### Custom Palette Reduction
```csharp
var customPalette = new int[] 
{
    0x000000, // Black
    0xFF0000, // Red
    0x00FF00, // Green
    0x0000FF, // Blue
    0xFFFF00, // Yellow
    0xFF00FF, // Magenta
    0x00FFFF, // Cyan
    0xFFFFFF  // White
};

var colorReductionNode = new ColorReductionEffectsNode
{
    ReductionLevel = customPalette.Length,
    ReductionMethod = 3, // K-means clustering
    EnableDithering = true,
    DitheringType = 2, // Ordered dithering
    CustomPalette = customPalette,
    PaletteType = 3, // Custom
    PreserveBrightness = false
};
```

## Technical Implementation

### Core Color Reduction Algorithm
```csharp
protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
{
    if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
        return GetDefaultOutput();

    var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
    
    int currentLevel = ReductionLevel;
    if (BeatReactive && audioFeatures?.IsBeat == true)
    {
        currentLevel = BeatReductionLevel;
    }

    // Generate or select color palette
    int[] palette = GeneratePalette(currentLevel);
    
    // Apply color reduction
    switch (ReductionMethod)
    {
        case 0: // Uniform Quantization
            ApplyUniformQuantization(imageBuffer, output, palette);
            break;
        case 1: // Median Cut
            ApplyMedianCutQuantization(imageBuffer, output, palette);
            break;
        case 2: // Octree Quantization
            ApplyOctreeQuantization(imageBuffer, output, palette);
            break;
        case 3: // K-Means Clustering
            ApplyKMeansQuantization(imageBuffer, output, palette);
            break;
        case 4: // Popularity Algorithm
            ApplyPopularityQuantization(imageBuffer, output, palette);
            break;
        case 5: // Adaptive Quantization
            ApplyAdaptiveQuantization(imageBuffer, output, palette);
            break;
    }

    return output;
}
```

### Uniform Quantization
```csharp
private void ApplyUniformQuantization(ImageBuffer source, ImageBuffer output, int[] palette)
{
    int paletteSize = palette.Length;
    int step = 256 / paletteSize;
    
    for (int y = 0; y < output.Height; y++)
    {
        for (int x = 0; x < output.Width; x++)
        {
            int pixel = source.GetPixel(x, y);
            int reducedPixel = QuantizePixel(pixel, palette, step);
            
            if (EnableDithering)
            {
                reducedPixel = ApplyDithering(source, output, x, y, pixel, reducedPixel);
            }
            
            output.SetPixel(x, y, reducedPixel);
        }
    }
}
```

### Pixel Quantization
```csharp
private int QuantizePixel(int pixel, int[] palette, int step)
{
    int r = pixel & 0xFF;
    int g = (pixel >> 8) & 0xFF;
    int b = (pixel >> 16) & 0xFF;
    
    // Quantize each channel
    int quantizedR = (r / step) * step;
    int quantizedG = (g / step) * step;
    int quantizedB = (b / step) * step;
    
    // Find closest palette color
    int closestColor = FindClosestPaletteColor(quantizedR, quantizedG, quantizedB, palette);
    
    return closestColor;
}
```

### Find Closest Palette Color
```csharp
private int FindClosestPaletteColor(int r, int g, int b, int[] palette)
{
    int closestColor = palette[0];
    int minDistance = int.MaxValue;
    
    foreach (int paletteColor in palette)
    {
        int pr = paletteColor & 0xFF;
        int pg = (paletteColor >> 8) & 0xFF;
        int pb = (paletteColor >> 16) & 0xFF;
        
        // Calculate Euclidean distance
        int distance = (r - pr) * (r - pr) + (g - pg) * (g - pg) + (b - pb) * (b - pb);
        
        if (distance < minDistance)
        {
            minDistance = distance;
            closestColor = paletteColor;
        }
    }
    
    return closestColor;
}
```

## Advanced Quantization Techniques

### Median Cut Quantization
```csharp
private void ApplyMedianCutQuantization(ImageBuffer source, ImageBuffer output, int[] palette)
{
    // Collect all unique colors
    var uniqueColors = new HashSet<int>();
    for (int y = 0; y < source.Height; y++)
    {
        for (int x = 0; x < source.Width; x++)
        {
            uniqueColors.Add(source.GetPixel(x, y));
        }
    }
    
    // Apply median cut algorithm
    var optimizedPalette = MedianCut(uniqueColors.ToArray(), palette.Length);
    
    // Apply quantization with optimized palette
    for (int y = 0; y < output.Height; y++)
    {
        for (int x = 0; x < output.Width; x++)
        {
            int pixel = source.GetPixel(x, y);
            int reducedPixel = FindClosestPaletteColor(pixel, optimizedPalette);
            
            if (EnableDithering)
            {
                reducedPixel = ApplyDithering(source, output, x, y, pixel, reducedPixel);
            }
            
            output.SetPixel(x, y, reducedPixel);
        }
    }
}
```

### Floyd-Steinberg Dithering
```csharp
private int ApplyDithering(ImageBuffer source, ImageBuffer output, int x, int y, int originalPixel, int quantizedPixel)
{
    if (!EnableDithering)
        return quantizedPixel;
    
    // Calculate quantization error
    int errorR = (originalPixel & 0xFF) - (quantizedPixel & 0xFF);
    int errorG = ((originalPixel >> 8) & 0xFF) - ((quantizedPixel >> 8) & 0xFF);
    int errorB = ((originalPixel >> 16) & 0xFF) - ((quantizedPixel >> 16) & 0xFF);
    
    // Distribute error to neighboring pixels (Floyd-Steinberg)
    if (x + 1 < source.Width)
    {
        DistributeError(output, x + 1, y, errorR, errorG, errorB, 7.0f / 16.0f);
    }
    
    if (x - 1 >= 0 && y + 1 < source.Height)
    {
        DistributeError(output, x - 1, y + 1, errorR, errorG, errorB, 3.0f / 16.0f);
    }
    
    if (y + 1 < source.Height)
    {
        DistributeError(output, x, y + 1, errorR, errorG, errorB, 5.0f / 16.0f);
    }
    
    if (x + 1 < source.Width && y + 1 < source.Height)
    {
        DistributeError(output, x + 1, y + 1, errorR, errorG, errorB, 1.0f / 16.0f);
    }
    
    return quantizedPixel;
}
```

### Error Distribution
```csharp
private void DistributeError(ImageBuffer output, int x, int y, int errorR, int errorG, int errorB, float factor)
{
    int pixel = output.GetPixel(x, y);
    
    int r = Math.Clamp((pixel & 0xFF) + (int)(errorR * factor), 0, 255);
    int g = Math.Clamp(((pixel >> 8) & 0xFF) + (int)(errorG * factor), 0, 255);
    int b = Math.Clamp(((pixel >> 16) & 0xFF) + (int)(errorB * factor), 0, 255);
    
    int newPixel = r | (g << 8) | (b << 16);
    output.SetPixel(x, y, newPixel);
}
```

## Palette Generation

### Grayscale Palette
```csharp
private int[] GenerateGrayscalePalette(int level)
{
    var palette = new int[level];
    int step = 256 / (level - 1);
    
    for (int i = 0; i < level; i++)
    {
        int intensity = Math.Min(i * step, 255);
        palette[i] = intensity | (intensity << 8) | (intensity << 16);
    }
    
    return palette;
}
```

### RGB Palette
```csharp
private int[] GenerateRgbPalette(int level)
{
    var palette = new int[level];
    int colorsPerChannel = (int)Math.Ceiling(Math.Pow(level, 1.0 / 3.0));
    int step = 256 / colorsPerChannel;
    
    int index = 0;
    for (int r = 0; r < colorsPerChannel && index < level; r++)
    {
        for (int g = 0; g < colorsPerChannel && index < level; g++)
        {
            for (int b = 0; b < colorsPerChannel && index < level; b++)
            {
                int red = Math.Min(r * step, 255);
                int green = Math.Min(g * step, 255);
                int blue = Math.Min(b * step, 255);
                
                palette[index] = red | (green << 8) | (blue << 16);
                index++;
            }
        }
    }
    
    return palette;
}
```

## Performance Optimization

### Optimization Techniques
1. **Color Lookup Tables**: Pre-calculated color mappings
2. **SIMD Operations**: Vectorized color operations
3. **Early Exit**: Skip processing for transparent areas
4. **Caching**: Cache frequently accessed palette values
5. **Threading**: Multi-threaded processing for large images

### Memory Management
- Efficient palette storage
- Minimize temporary allocations
- Use value types for calculations
- Optimize color distance calculations

## Integration with EffectGraph

### Port Configuration
```csharp
protected override void InitializePorts()
{
    _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for color reduction"));
    _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Color reduced output image"));
}
```

### Metadata Support
```csharp
public override EffectMetadata GetMetadata()
{
    var metadata = new EffectMetadata();
    metadata.Add("ReductionMethod", ReductionMethod);
    metadata.Add("ReductionLevel", ReductionLevel);
    metadata.Add("Dithering", EnableDithering ? DitheringType.ToString() : "Disabled");
    metadata.Add("PaletteType", PaletteType);
    metadata.Add("PaletteSize", CustomPalette?.Length ?? 0);
    return metadata;
}
```

## Testing and Validation

### Test Cases
1. **Basic Reduction**: Verify color reduction accuracy
2. **Reduction Methods**: Test all quantization algorithms
3. **Dithering**: Validate dithering effects
4. **Palette Types**: Test different palette generation
5. **Performance**: Measure rendering speed
6. **Edge Cases**: Handle boundary conditions

### Validation Methods
- Visual comparison with reference images
- Performance benchmarking
- Memory usage analysis
- Thread safety testing

## Future Enhancements

### Planned Features
1. **Advanced Algorithms**: More sophisticated quantization
2. **Hardware Acceleration**: GPU-accelerated color operations
3. **Real-time Palettes**: Dynamic palette generation
4. **Custom Shaders**: User-defined reduction algorithms
5. **Machine Learning**: AI-powered color optimization

### Compatibility
- Full AVS preset compatibility
- Support for legacy reduction modes
- Performance parity with original
- Extended functionality

## Conclusion

The Color Reduction effect provides essential color quantization capabilities for memory-efficient AVS visualizations. This C# implementation maintains full compatibility with the original while adding modern features like beat reactivity and enhanced quantization algorithms. Complete documentation ensures reliable operation in production environments.
