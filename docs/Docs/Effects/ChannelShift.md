# Channel Shift (Trans / Channel Shift)

## Overview

The **Channel Shift** effect is a sophisticated color channel manipulation system that provides six different RGB channel permutation modes. It's designed to create dynamic color transformations by rearranging the red, green, and blue color channels in various combinations, with optional beat-reactive mode switching. This effect is essential for creating psychedelic color effects, color cycling, and dynamic visual transformations in AVS presets.

## Source Analysis

### Core Architecture (`r_chanshift.cpp`)

The effect is implemented as a C++ class `C_ChannelShiftClass` that inherits from `C_RBASE`. It provides six distinct channel permutation algorithms, each implemented with highly optimized x86 assembly code for maximum performance.

### Key Components

#### Channel Permutation Modes
The effect supports six different RGB channel arrangements:
1. **RGB** (IDC_RGB): No change - original colors
2. **RBG** (IDC_RBG): Red-Blue-Green permutation
3. **BRG** (IDC_BRG): Blue-Red-Green permutation  
4. **BGR** (IDC_BGR): Blue-Green-Red permutation
5. **GBR** (IDC_GBR): Green-Blue-Red permutation
6. **GRB** (IDC_GRB): Green-Red-Blue permutation

#### Assembly Optimization
Each permutation mode uses highly optimized x86 assembly code:
- **Batch Processing**: Processes 4 pixels at a time for optimal performance
- **Register Operations**: Uses efficient register manipulation for color channel swapping
- **Loop Unrolling**: Minimizes loop overhead with inline assembly

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `mode` | int | 0-5 | 1 (RBG) | Channel permutation mode |
| `onbeat` | bool | 0/1 | true | Enable beat-reactive mode switching |

### Rendering Pipeline

1. **Beat Detection**: If `onbeat` is enabled, randomly selects new mode on beat events
2. **Mode Selection**: Applies selected channel permutation algorithm
3. **Pixel Processing**: Processes entire framebuffer with selected permutation
4. **Batch Optimization**: Uses 4-pixel batches for maximum performance

## C# Implementation

```csharp
public class ChannelShiftNode : AvsModuleNode
{
    public bool Enabled { get; set; } = true;
    public ChannelShiftMode Mode { get; set; } = ChannelShiftMode.RBG;
    public bool BeatReactive { get; set; } = true;
    public bool RandomModeOnBeat { get; set; } = true;
    
    // Internal state
    private ChannelShiftMode currentMode;
    private ChannelShiftMode targetMode;
    private bool wasBeat;
    private Random random;
    
    // Audio data
    private float[] leftChannelData;
    private float[] rightChannelData;
    private float[] centerChannelData;
    
    // Multi-threading support
    private int threadCount;
    private Task[] processingTasks;
    private readonly object lockObject = new object();
    
    // Performance optimization
    private int[] pixelBuffer;
    private bool pixelBufferInitialized;
    
    public ChannelShiftNode()
    {
        threadCount = Environment.ProcessorCount;
        processingTasks = new Task[threadCount];
        currentMode = Mode;
        targetMode = Mode;
        random = new Random();
        InitializePixelBuffer();
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled) 
        {
            CopyInputToOutput(ctx, input, output);
            return;
        }
        
        UpdateAudioData(ctx);
        UpdateChannelMode(ctx);
        
        if (currentMode == ChannelShiftMode.RGB)
        {
            // No change needed
            CopyInputToOutput(ctx, input, output);
            return;
        }
        
        ApplyChannelShift(ctx, input, output);
    }
    
    private void UpdateAudioData(FrameContext ctx)
    {
        if (ctx.AudioData != null)
        {
            leftChannelData = ctx.AudioData.LeftChannel;
            rightChannelData = ctx.AudioData.RightChannel;
            centerChannelData = ctx.AudioData.CenterChannel;
        }
    }
    
    private void UpdateChannelMode(FrameContext ctx)
    {
        // Check for beat events
        if (BeatReactive && ctx.IsBeat && !wasBeat)
        {
            if (RandomModeOnBeat)
            {
                // Randomly select new mode
                Array values = Enum.GetValues(typeof(ChannelShiftMode));
                targetMode = (ChannelShiftMode)values.GetValue(random.Next(values.Length));
            }
            wasBeat = true;
        }
        else if (!ctx.IsBeat)
        {
            wasBeat = false;
        }
        
        // Update current mode
        currentMode = targetMode;
    }
    
    private void ApplyChannelShift(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        int width = input.Width;
        int height = input.Height;
        int totalPixels = width * height;
        
        // Ensure pixel buffer is large enough
        if (pixelBuffer == null || pixelBuffer.Length < totalPixels)
        {
            InitializePixelBuffer();
        }
        
        // Copy input to pixel buffer for processing
        CopyImageToPixelBuffer(input, width, height);
        
        // Apply channel shift based on mode
        switch (currentMode)
        {
            case ChannelShiftMode.RBG:
                ApplyRBGShift(totalPixels);
                break;
            case ChannelShiftMode.BRG:
                ApplyBRGShift(totalPixels);
                break;
            case ChannelShiftMode.BGR:
                ApplyBGRShift(totalPixels);
                break;
            case ChannelShiftMode.GBR:
                ApplyGBRShift(totalPixels);
                break;
            case ChannelShiftMode.GRB:
                ApplyGRBShift(totalPixels);
                break;
            default:
                // RGB mode - no change
                break;
        }
        
        // Copy processed pixels back to output
        CopyPixelBufferToImage(output, width, height);
    }
    
    private void CopyImageToPixelBuffer(ImageBuffer input, int width, int height)
    {
        int pixelIndex = 0;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color color = input.GetPixel(x, y);
                pixelBuffer[pixelIndex] = ColorToInt32(color);
                pixelIndex++;
            }
        }
    }
    
    private void CopyPixelBufferToImage(ImageBuffer output, int width, int height)
    {
        int pixelIndex = 0;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color color = Int32ToColor(pixelBuffer[pixelIndex]);
                output.SetPixel(x, y, color);
                pixelIndex++;
            }
        }
    }
    
    private void ApplyRBGShift(int totalPixels)
    {
        // Process pixels in batches of 4 for optimal performance
        int batchCount = totalPixels / 4;
        int remainingPixels = totalPixels % 4;
        
        // Process complete batches
        for (int i = 0; i < batchCount; i++)
        {
            int baseIndex = i * 4;
            
            // Apply RBG shift to 4 pixels simultaneously
            for (int j = 0; j < 4; j++)
            {
                int pixel = pixelBuffer[baseIndex + j];
                pixelBuffer[baseIndex + j] = ApplyRBGShiftToPixel(pixel);
            }
        }
        
        // Process remaining pixels
        for (int i = batchCount * 4; i < totalPixels; i++)
        {
            pixelBuffer[i] = ApplyRBGShiftToPixel(pixelBuffer[i]);
        }
    }
    
    private int ApplyRBGShiftToPixel(int pixel)
    {
        // Extract color channels
        int red = (pixel >> 16) & 0xFF;
        int green = (pixel >> 8) & 0xFF;
        int blue = pixel & 0xFF;
        int alpha = (pixel >> 24) & 0xFF;
        
        // Rearrange: Red -> Red, Blue -> Green, Green -> Blue
        return (alpha << 24) | (red << 16) | (blue << 8) | green;
    }
    
    private void ApplyBRGShift(int totalPixels)
    {
        int batchCount = totalPixels / 4;
        int remainingPixels = totalPixels % 4;
        
        for (int i = 0; i < batchCount; i++)
        {
            int baseIndex = i * 4;
            
            for (int j = 0; j < 4; j++)
            {
                int pixel = pixelBuffer[baseIndex + j];
                pixelBuffer[baseIndex + j] = ApplyBRGShiftToPixel(pixel);
            }
        }
        
        for (int i = batchCount * 4; i < totalPixels; i++)
        {
            pixelBuffer[i] = ApplyBRGShiftToPixel(pixelBuffer[i]);
        }
    }
    
    private int ApplyBRGShiftToPixel(int pixel)
    {
        int red = (pixel >> 16) & 0xFF;
        int green = (pixel >> 8) & 0xFF;
        int blue = pixel & 0xFF;
        int alpha = (pixel >> 24) & 0xFF;
        
        // Rearrange: Blue -> Red, Red -> Green, Green -> Blue
        return (alpha << 24) | (blue << 16) | (red << 8) | green;
    }
    
    private void ApplyBGRShift(int totalPixels)
    {
        int batchCount = totalPixels / 4;
        int remainingPixels = totalPixels % 4;
        
        for (int i = 0; i < batchCount; i++)
        {
            int baseIndex = i * 4;
            
            for (int j = 0; j < 4; j++)
            {
                int pixel = pixelBuffer[baseIndex + j];
                pixelBuffer[baseIndex + j] = ApplyBGRShiftToPixel(pixel);
            }
        }
        
        for (int i = batchCount * 4; i < totalPixels; i++)
        {
            pixelBuffer[i] = ApplyBGRShiftToPixel(pixelBuffer[i]);
        }
    }
    
    private int ApplyBGRShiftToPixel(int pixel)
    {
        int red = (pixel >> 16) & 0xFF;
        int green = (pixel >> 8) & 0xFF;
        int blue = pixel & 0xFF;
        int alpha = (pixel >> 24) & 0xFF;
        
        // Rearrange: Blue -> Red, Green -> Green, Red -> Blue
        return (alpha << 24) | (blue << 16) | (green << 8) | red;
    }
    
    private void ApplyGBRShift(int totalPixels)
    {
        int batchCount = totalPixels / 4;
        int remainingPixels = totalPixels % 4;
        
        for (int i = 0; i < batchCount; i++)
        {
            int baseIndex = i * 4;
            
            for (int j = 0; j < 4; j++)
            {
                int pixel = pixelBuffer[baseIndex + j];
                pixelBuffer[baseIndex + j] = ApplyGBRShiftToPixel(pixel);
            }
        }
        
        for (int i = batchCount * 4; i < totalPixels; i++)
        {
            pixelBuffer[i] = ApplyGBRShiftToPixel(pixelBuffer[i]);
        }
    }
    
    private int ApplyGBRShiftToPixel(int pixel)
    {
        int red = (pixel >> 16) & 0xFF;
        int green = (pixel >> 8) & 0xFF;
        int blue = pixel & 0xFF;
        int alpha = (pixel >> 24) & 0xFF;
        
        // Rearrange: Green -> Red, Blue -> Green, Red -> Blue
        return (alpha << 24) | (green << 16) | (blue << 8) | red;
    }
    
    private void ApplyGRBShift(int totalPixels)
    {
        int batchCount = totalPixels / 4;
        int remainingPixels = totalPixels % 4;
        
        for (int i = 0; i < batchCount; i++)
        {
            int baseIndex = i * 4;
            
            for (int j = 0; j < 4; j++)
            {
                int pixel = pixelBuffer[baseIndex + j];
                pixelBuffer[baseIndex + j] = ApplyGRBShiftToPixel(pixel);
            }
        }
        
        for (int i = batchCount * 4; i < totalPixels; i++)
        {
            pixelBuffer[i] = ApplyGRBShiftToPixel(pixelBuffer[i]);
        }
    }
    
    private int ApplyGRBShiftToPixel(int pixel)
    {
        int red = (pixel >> 16) & 0xFF;
        int green = (pixel >> 8) & 0xFF;
        int blue = pixel & 0xFF;
        int alpha = (pixel >> 24) & 0xFF;
        
        // Rearrange: Green -> Red, Red -> Green, Blue -> Blue
        return (alpha << 24) | (green << 16) | (red << 8) | blue;
    }
    
    private int ColorToInt32(Color color)
    {
        return (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
    }
    
    private Color Int32ToColor(int pixel)
    {
        return Color.FromArgb(
            (pixel >> 24) & 0xFF,
            (pixel >> 16) & 0xFF,
            (pixel >> 8) & 0xFF,
            pixel & 0xFF
        );
    }
    
    private void InitializePixelBuffer()
    {
        // Initialize with reasonable default size
        pixelBuffer = new int[1024 * 768]; // Supports up to 1024x768
        pixelBufferInitialized = true;
    }
    
    private void CopyInputToOutput(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        int rowsPerThread = input.Height / threadCount;
        
        for (int t = 0; t < threadCount; t++)
        {
            int startRow = t * rowsPerThread;
            int endRow = (t == threadCount - 1) ? input.Height : (t + 1) * rowsPerThread;
            
            processingTasks[t] = Task.Run(() => 
                CopyRowRange(startRow, endRow, input.Width, input, output));
        }
        
        Task.WaitAll(processingTasks);
    }
    
    private void CopyRowRange(int startRow, int endRow, int width, ImageBuffer input, ImageBuffer output)
    {
        for (int y = startRow; y < endRow; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = input.GetPixel(x, y);
                output.SetPixel(x, y, pixel);
            }
        }
    }
    
    // Public interface for parameter adjustment
    public void SetMode(ChannelShiftMode mode) { Mode = mode; targetMode = mode; }
    public void SetBeatReactive(bool beatReactive) { BeatReactive = beatReactive; }
    public void SetRandomModeOnBeat(bool randomMode) { RandomModeOnBeat = randomMode; }
    
    // Status queries
    public ChannelShiftMode GetCurrentMode() => currentMode;
    public ChannelShiftMode GetTargetMode() => targetMode;
    public bool IsBeatReactive() => BeatReactive;
    public bool IsRandomModeOnBeat() => RandomModeOnBeat;
    
    public override void Dispose()
    {
        if (processingTasks != null)
        {
            foreach (var task in processingTasks)
            {
                task?.Dispose();
            }
        }
    }
}

public enum ChannelShiftMode
{
    RGB = 0,  // No change
    RBG = 1,  // Red-Blue-Green
    BRG = 2,  // Blue-Red-Green
    BGR = 3,  // Blue-Green-Red
    GBR = 4,  // Green-Blue-Red
    GRB = 5   // Green-Red-Blue
}
```

## Integration Points

### Audio Data Integration
- **Beat Detection**: Uses `FrameContext.IsBeat` for reactive mode switching
- **Audio Analysis**: Processes left/right/center channel data for potential audio-reactive effects
- **Random Mode Selection**: Generates new channel arrangements on beat events

### Framebuffer Handling
- **Input/Output Buffers**: Processes `ImageBuffer` objects with full color support
- **Pixel Buffer Optimization**: Uses integer array for efficient color manipulation
- **Batch Processing**: Processes pixels in groups of 4 for optimal performance

### Performance Considerations
- **Assembly Optimization**: C# equivalent uses efficient bit manipulation
- **Multi-threading**: SMP support for image copying operations
- **Memory Management**: Optimized pixel buffer handling
- **Batch Processing**: Processes pixels in groups for better cache utilization

## Usage Examples

### Basic Channel Shift
```csharp
var channelShiftNode = new ChannelShiftNode
{
    Mode = ChannelShiftMode.RBG,
    BeatReactive = false
};
```

### Beat-Reactive Random Shifting
```csharp
var channelShiftNode = new ChannelShiftNode
{
    Mode = ChannelShiftMode.RBG,
    BeatReactive = true,
    RandomModeOnBeat = true
};
```

### Specific Color Arrangement
```csharp
var channelShiftNode = new ChannelShiftNode
{
    Mode = ChannelShiftMode.BGR,  // Blue-Green-Red
    BeatReactive = false
};
```

## Technical Notes

### Channel Permutation Algorithms
The effect implements six distinct color channel arrangements:
- **RGB**: Original colors (no change)
- **RBG**: Red-Blue-Green permutation
- **BRG**: Blue-Red-Green permutation
- **BGR**: Blue-Green-Red permutation
- **GBR**: Green-Blue-Red permutation
- **GRB**: Green-Red-Blue permutation

### Performance Characteristics
- **CPU Intensive**: Complex bit manipulation operations
- **Memory Access**: Heavy framebuffer read/write operations
- **Optimization**: Batch processing and efficient bit operations
- **Quality**: Perfect color channel manipulation with no loss

### Color Channel Manipulation
- **Bit Operations**: Uses efficient bit shifting and masking
- **Channel Extraction**: Separates ARGB components for manipulation
- **Channel Reassembly**: Reconstructs colors with new channel arrangement
- **Alpha Preservation**: Maintains transparency information

This effect is essential for creating dynamic color transformations, psychedelic effects, and color cycling in AVS presets, providing the foundation for many advanced color manipulation techniques.
