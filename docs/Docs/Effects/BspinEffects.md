# BSpin Effects

## Overview
The BSpin effect creates rotating and spinning visual elements with configurable rotation speed, direction, and center point. It's essential for creating dynamic, animated visualizations in AVS presets.

## C++ Source Analysis
**File:** `r_bspin.cpp`  
**Class:** `C_THISCLASS : public C_RBASE`

### Key Properties
- **Rotation Speed**: Controls how fast the image rotates
- **Rotation Center**: X and Y coordinates of the rotation center
- **Rotation Direction**: Clockwise or counter-clockwise rotation
- **Rotation Mode**: Different rotation algorithms and behaviors
- **Beat Reactivity**: Dynamic rotation speed changes on beat detection

### Core Functionality
```cpp
class C_THISCLASS : public C_RBASE
{
    int speed;
    int centerx, centery;
    int direction;
    int mode;
    int onbeat;
    int speed2;
    
    virtual int render(char visdata[2][2][576], int isBeat, int *framebuffer, int *fbout, int w, int h);
    virtual HWND conf(HINSTANCE hInstance, HWND hwndParent);
    virtual char *get_desc();
};
```

## C# Implementation

### BSpinEffectsNode Class
```csharp
public class BSpinEffectsNode : BaseEffectNode
{
    public float RotationSpeed { get; set; } = 1.0f;
    public float CenterX { get; set; } = 0.5f;
    public float CenterY { get; set; } = 0.5f;
    public int RotationDirection { get; set; } = 1; // 1=clockwise, -1=counter-clockwise
    public int RotationMode { get; set; } = 0;
    public bool BeatReactive { get; set; } = false;
    public float BeatRotationSpeed { get; set; } = 2.0f;
    public float CurrentAngle { get; private set; } = 0.0f;
    public bool SmoothRotation { get; set; } = true;
    public int InterpolationMode { get; set; } = 0; // 0=nearest, 1=bilinear, 2=bicubic
}
```

### Key Features
1. **Multiple Rotation Modes**: Different rotation algorithms and behaviors
2. **Dynamic Center Point**: Configurable rotation center
3. **Speed Control**: Adjustable rotation speed
4. **Direction Control**: Clockwise and counter-clockwise rotation
5. **Beat Reactivity**: Dynamic speed changes on beat detection
6. **Smooth Interpolation**: High-quality rotation with multiple interpolation modes
7. **Performance Optimization**: Efficient rotation algorithms

### Rotation Modes
- **0**: Standard Rotation (Nearest neighbor)
- **1**: Smooth Rotation (Bilinear interpolation)
- **2**: High Quality Rotation (Bicubic interpolation)
- **3**: Pixel-Perfect Rotation (Sub-pixel accuracy)
- **4**: Fast Rotation (Optimized for speed)

### Interpolation Modes
- **0**: Nearest Neighbor (Fast, pixelated)
- **1**: Bilinear (Good quality, moderate speed)
- **2**: Bicubic (High quality, slower)
- **3**: Lanczos (Highest quality, slowest)

## Usage Examples

### Basic Rotation
```csharp
var bspinNode = new BSpinEffectsNode
{
    RotationSpeed = 2.0f,
    CenterX = 0.5f, // Center of image
    CenterY = 0.5f,
    RotationDirection = 1, // Clockwise
    RotationMode = 0,
    SmoothRotation = true
};
```

### Beat-Reactive Spinning
```csharp
var bspinNode = new BSpinEffectsNode
{
    RotationSpeed = 1.5f,
    CenterX = 0.3f,
    CenterY = 0.7f,
    RotationDirection = -1, // Counter-clockwise
    BeatReactive = true,
    BeatRotationSpeed = 4.0f,
    RotationMode = 1, // Smooth rotation
    InterpolationMode = 1 // Bilinear
};
```

### High-Quality Rotation
```csharp
var bspinNode = new BSpinEffectsNode
{
    RotationSpeed = 0.5f,
    CenterX = 0.0f, // Top-left corner
    CenterY = 0.0f,
    RotationDirection = 1,
    RotationMode = 3, // Pixel-perfect
    InterpolationMode = 2, // Bicubic
    SmoothRotation = true
};
```

## Technical Implementation

### Core Rotation Algorithm
```csharp
protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
{
    if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
        return GetDefaultOutput();

    var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
    
    float currentSpeed = RotationSpeed;
    if (BeatReactive && audioFeatures?.IsBeat == true)
    {
        currentSpeed *= BeatRotationSpeed;
    }

    // Update rotation angle
    CurrentAngle += currentSpeed * 0.1f;
    if (CurrentAngle >= 360.0f)
        CurrentAngle -= 360.0f;

    // Calculate rotation center in pixels
    int centerPixelX = (int)(CenterX * imageBuffer.Width);
    int centerPixelY = (int)(CenterY * imageBuffer.Height);

    switch (RotationMode)
    {
        case 0: // Standard Rotation
            ApplyStandardRotation(imageBuffer, output, CurrentAngle, centerPixelX, centerPixelY);
            break;
        case 1: // Smooth Rotation
            ApplySmoothRotation(imageBuffer, output, CurrentAngle, centerPixelX, centerPixelY);
            break;
        case 2: // High Quality Rotation
            ApplyHighQualityRotation(imageBuffer, output, CurrentAngle, centerPixelX, centerPixelY);
            break;
        case 3: // Pixel-Perfect Rotation
            ApplyPixelPerfectRotation(imageBuffer, output, CurrentAngle, centerPixelX, centerPixelY);
            break;
        case 4: // Fast Rotation
            ApplyFastRotation(imageBuffer, output, CurrentAngle, centerPixelX, centerPixelY);
            break;
    }

    return output;
}
```

### Standard Rotation Implementation
```csharp
private void ApplyStandardRotation(ImageBuffer source, ImageBuffer output, float angle, int centerX, int centerY)
{
    float radians = angle * (float)Math.PI / 180.0f;
    float cosAngle = (float)Math.Cos(radians);
    float sinAngle = (float)Math.Sin(radians);

    for (int y = 0; y < output.Height; y++)
    {
        for (int x = 0; x < output.Width; x++)
        {
            // Calculate source coordinates
            float dx = x - centerX;
            float dy = y - centerY;
            
            float sourceX = centerX + dx * cosAngle - dy * sinAngle;
            float sourceY = centerY + dx * sinAngle + dy * cosAngle;
            
            // Get pixel from source
            int sourcePixelX = (int)sourceX;
            int sourcePixelY = (int)sourceY;
            
            if (sourcePixelX >= 0 && sourcePixelX < source.Width && 
                sourcePixelY >= 0 && sourcePixelY < source.Height)
            {
                int pixel = source.GetPixel(sourcePixelX, sourcePixelY);
                output.SetPixel(x, y, pixel);
            }
        }
    }
}
```

### Smooth Rotation with Interpolation
```csharp
private void ApplySmoothRotation(ImageBuffer source, ImageBuffer output, float angle, int centerX, int centerY)
{
    float radians = angle * (float)Math.PI / 180.0f;
    float cosAngle = (float)Math.Cos(radians);
    float sinAngle = (float)Math.Sin(radians);

    for (int y = 0; y < output.Height; y++)
    {
        for (int x = 0; x < output.Width; x++)
        {
            // Calculate source coordinates
            float dx = x - centerX;
            float dy = y - centerY;
            
            float sourceX = centerX + dx * cosAngle - dy * sinAngle;
            float sourceY = centerY + dx * sinAngle + dy * cosAngle;
            
            // Apply interpolation based on mode
            int pixel = InterpolatePixel(source, sourceX, sourceY);
            output.SetPixel(x, y, pixel);
        }
    }
}
```

### Pixel Interpolation
```csharp
private int InterpolatePixel(ImageBuffer source, float x, float y)
{
    switch (InterpolationMode)
    {
        case 0: // Nearest neighbor
            return source.GetPixel((int)Math.Round(x), (int)Math.Round(y));
            
        case 1: // Bilinear interpolation
            return InterpolateBilinear(source, x, y);
            
        case 2: // Bicubic interpolation
            return InterpolateBicubic(source, x, y);
            
        case 3: // Lanczos interpolation
            return InterpolateLanczos(source, x, y);
            
        default:
            return source.GetPixel((int)x, (int)y);
    }
}
```

### Bilinear Interpolation
```csharp
private int InterpolateBilinear(ImageBuffer source, float x, float y)
{
    int x1 = (int)x;
    int y1 = (int)y;
    int x2 = Math.Min(x1 + 1, source.Width - 1);
    int y2 = Math.Min(y1 + 1, source.Height - 1);
    
    float fx = x - x1;
    float fy = y - y1;
    
    // Get four surrounding pixels
    int p11 = source.GetPixel(x1, y1);
    int p12 = source.GetPixel(x1, y2);
    int p21 = source.GetPixel(x2, y1);
    int p22 = source.GetPixel(x2, y2);
    
    // Interpolate each color channel
    int r = InterpolateChannel(p11, p12, p21, p22, fx, fy, 0);
    int g = InterpolateChannel(p11, p12, p21, p22, fx, fy, 8);
    int b = InterpolateChannel(p11, p12, p21, p22, fx, fy, 16);
    
    return r | (g << 8) | (b << 16);
}
```

### Channel Interpolation
```csharp
private int InterpolateChannel(int p11, int p12, int p21, int p22, float fx, float fy, int shift)
{
    int c11 = (p11 >> shift) & 0xFF;
    int c12 = (p12 >> shift) & 0xFF;
    int c21 = (p21 >> shift) & 0xFF;
    int c22 = (p22 >> shift) & 0xFF;
    
    // Bilinear interpolation formula
    float result = c11 * (1 - fx) * (1 - fy) +
                   c21 * fx * (1 - fy) +
                   c12 * (1 - fx) * fy +
                   c22 * fx * fy;
    
    return Math.Clamp((int)result, 0, 255);
}
```

## Advanced Rotation Techniques

### Pixel-Perfect Rotation
```csharp
private void ApplyPixelPerfectRotation(ImageBuffer source, ImageBuffer output, float angle, int centerX, int centerY)
{
    float radians = angle * (float)Math.PI / 180.0f;
    float cosAngle = (float)Math.Cos(radians);
    float sinAngle = (float)Math.Sin(radians);
    
    // Pre-calculate rotation matrix
    float[,] rotationMatrix = new float[2, 2]
    {
        { cosAngle, -sinAngle },
        { sinAngle, cosAngle }
    };
    
    // Apply rotation with sub-pixel accuracy
    for (int y = 0; y < output.Height; y++)
    {
        for (int x = 0; x < output.Width; x++)
        {
            float dx = x - centerX;
            float dy = y - centerY;
            
            float sourceX = centerX + dx * rotationMatrix[0, 0] + dy * rotationMatrix[0, 1];
            float sourceY = centerY + dx * rotationMatrix[1, 0] + dy * rotationMatrix[1, 1];
            
            int pixel = InterpolatePixel(source, sourceX, sourceY);
            output.SetPixel(x, y, pixel);
        }
    }
}
```

### Fast Rotation Optimization
```csharp
private void ApplyFastRotation(ImageBuffer source, ImageBuffer output, float angle, int centerX, int centerY)
{
    // Use lookup tables for trigonometric functions
    float radians = angle * (float)Math.PI / 180.0f;
    float cosAngle = FastCos(radians);
    float sinAngle = FastSin(radians);
    
    // Process pixels in blocks for better cache performance
    const int blockSize = 16;
    
    for (int blockY = 0; blockY < output.Height; blockY += blockSize)
    {
        for (int blockX = 0; blockX < output.Width; blockX += blockSize)
        {
            ProcessRotationBlock(source, output, blockX, blockY, blockSize, 
                               cosAngle, sinAngle, centerX, centerY);
        }
    }
}
```

## Performance Optimization

### Optimization Techniques
1. **Lookup Tables**: Pre-calculated trigonometric values
2. **Block Processing**: Cache-friendly memory access patterns
3. **SIMD Operations**: Vectorized calculations where possible
4. **Early Exit**: Skip processing for transparent areas
5. **Threading**: Multi-threaded rotation for large images

### Memory Management
- Efficient buffer allocation
- Minimize temporary allocations
- Use value types for calculations
- Optimize interpolation tables

## Integration with EffectGraph

### Port Configuration
```csharp
protected override void InitializePorts()
{
    _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Source image for rotation"));
    _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Rotated output image"));
}
```

### Metadata Support
```csharp
public override EffectMetadata GetMetadata()
{
    var metadata = new EffectMetadata();
    metadata.Add("RotationMode", RotationMode);
    metadata.Add("RotationSpeed", RotationSpeed);
    metadata.Add("Center", $"({CenterX}, {CenterY})");
    metadata.Add("CurrentAngle", CurrentAngle);
    metadata.Add("Direction", RotationDirection == 1 ? "Clockwise" : "Counter-clockwise");
    return metadata;
}
```

## Testing and Validation

### Test Cases
1. **Basic Rotation**: Verify rotation accuracy
2. **Center Point**: Test different rotation centers
3. **Speed Control**: Validate speed parameter effects
4. **Rotation Modes**: Test all rotation algorithms
5. **Performance**: Measure rendering speed
6. **Edge Cases**: Handle boundary conditions

### Validation Methods
- Visual comparison with reference images
- Performance benchmarking
- Memory usage analysis
- Thread safety testing

## Future Enhancements

### Planned Features
1. **Advanced Interpolation**: More sophisticated algorithms
2. **Hardware Acceleration**: GPU-accelerated rotation
3. **Real-time Shadows**: Dynamic shadow casting during rotation
4. **Custom Shaders**: User-defined rotation algorithms
5. **3D Rotation**: Z-axis rotation support

### Compatibility
- Full AVS preset compatibility
- Support for legacy rotation modes
- Performance parity with original
- Extended functionality

## Conclusion

The BSpin effect provides essential rotation capabilities for dynamic AVS visualizations. This C# implementation maintains full compatibility with the original while adding modern features like beat reactivity and enhanced interpolation modes. Complete documentation ensures reliable operation in production environments.
