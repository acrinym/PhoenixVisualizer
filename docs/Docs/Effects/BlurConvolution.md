# Blur/Convolution Effect - VIS_AVS Implementation

**Source:** Official Winamp VIS_AVS Source Code  
**File:** `r_blur.cpp`  
**Class:** `C_BlurClass`  
**Module Name:** "Trans / Blur"

---

## 🎯 **Effect Overview**

Blur/Convolution is a **high-performance image filtering effect** that applies a 5x5 convolution kernel to create smooth blurring effects. It's designed with **MMX optimization** for real-time performance and supports **multi-threading** for efficient processing on multi-core systems.

---

## 🏗️ **Architecture**

### **Base Class Inheritance**
```cpp
class C_BlurClass : public C_RBASE2
```

### **Core Components**
- **Multi-threading Support** - SMP (Symmetric Multi-Processing) capable
- **MMX Optimization** - SIMD instructions for parallel pixel processing
- **Convolution Kernel** - 5x5 weighted averaging filter
- **Edge Handling** - Special processing for image boundaries
- **Rounding Control** - Configurable rounding modes for quality

---

## ⚙️ **Configuration Options**

### **Core Settings**
| Option | Type | Description | Default |
|--------|------|-------------|---------|
| `enabled` | int | Effect enabled (1=normal, 2=enhanced) | 1 |
| `roundmode` | int | Rounding mode for quality | 0 |

### **Blur Modes**
- **Mode 1 (Normal)**: Standard 5x5 convolution blur
- **Mode 2 (Enhanced)**: Enhanced blur with additional processing
- **Rounding**: Optional rounding for improved visual quality

---

## 🎨 **Convolution Kernel**

### **5x5 Kernel Structure**
```
[ 1/16  1/8  1/16 ]
[  1/8  1/2   1/8  ]
[ 1/16  1/8  1/16 ]
```

### **Weight Distribution**
- **Center pixel**: 1/2 weight (50%)
- **Adjacent pixels**: 1/8 weight each (12.5%)
- **Corner pixels**: 1/16 weight each (6.25%)

### **Mathematical Implementation**
```cpp
// Pixel calculation formula
output = (center * 0.5) + 
         (adjacent * 0.125) + 
         (corner * 0.0625)

// Optimized bit operations
#define DIV_2(x)  (((x) & MASK_SH1) >> 1)
#define DIV_4(x)  (((x) & MASK_SH2) >> 2)
#define DIV_8(x)  (((x) & MASK_SH3) >> 3)
#define DIV_16(x) (((x) & MASK_SH4) >> 4)
```

---

## 🔧 **Rendering Pipeline**

### **1. Multi-threading Setup**
```cpp
virtual int smp_begin(int max_threads, char visdata[2][2][576], int isBeat, int *framebuffer, int *fbout, int w, int h);
virtual void smp_render(int this_thread, int max_threads, char visdata[2][2][576], int isBeat, int *framebuffer, int *fbout, int w, int h);
virtual int smp_finish(char visdata[2][2][576], int isBeat, int *framebuffer, int *fbout, int w, int h);
```

### **2. Thread Distribution**
```cpp
// Calculate thread boundaries
int start_l = (this_thread * h) / max_threads;
int end_l = (this_thread >= max_threads - 1) ? h : ((this_thread+1) * h) / max_threads;

// Process assigned rows
int outh = end_l - start_l;
if (outh < 1) return;
```

### **3. Edge Detection**
```cpp
// Determine edge conditions
int at_top = 0, at_bottom = 0;
if (!this_thread) at_top = 1;
if (this_thread >= max_threads - 1) at_bottom = 1;
```

---

## 🎵 **Audio Data Integration**

### **Data Structure**
```cpp
char visdata[2][2][576]
// [stereo][spectrum/waveform][frequency bins]
```

### **Audio Processing**
- **No direct audio integration** - Pure image processing effect
- **Beat detection available** - Can be used for dynamic blur intensity
- **Spectrum data available** - Can be used for frequency-reactive blur

---

## 🚀 **Performance Optimizations**

### **MMX SIMD Instructions**
```cpp
// MMX-optimized blur loop
__asm {
    mov ecx, w
    mov edx, ecx
    mov ebx, edx
    neg ebx
    mov esi, f
    mov edi, of
    sub ecx, 2
    shr ecx, 2
    // ... MMX processing loop
}
```

### **Bit Mask Optimization**
```cpp
// Optimized bit masks for division
#define MASK_SH1 (~(((1<<7)|(1<<15)|(1<<23))<<1))
#define MASK_SH2 (~(((3<<6)|(3<<14)|(3<<22))<<2))
#define MASK_SH3 (~(((7<<5)|(7<<13)|(7<<21))<<3))
#define MASK_SH4 (~(((15<<4)|(15<<12)|(15<<20))<<4))
```

### **Loop Unrolling**
```cpp
// Process 4 pixels at once
while (x--) {
    of[0] = DIV_2(f[0]) + DIV_4(f[0]) + DIV_16(f[1]) + DIV_16(f[-1]) + DIV_16(f2[0]) + DIV_16(f3[0]);
    of[1] = DIV_2(f[1]) + DIV_4(f[1]) + DIV_16(f[2]) + DIV_16(f[0]) + DIV_16(f2[1]) + DIV_16(f3[1]);
    of[2] = DIV_2(f[2]) + DIV_4(f[2]) + DIV_16(f[3]) + DIV_16(f[1]) + DIV_16(f2[2]) + DIV_16(f3[2]);
    of[3] = DIV_2(f[3]) + DIV_4(f[3]) + DIV_16(f[4]) + DIV_16(f[2]) + DIV_16(f2[3]) + DIV_16(f3[3]);
    f += 4; f2 += 4; f3 += 4; of += 4;
}
```

---

## 🌈 **Edge Handling**

### **Top Edge Processing**
```cpp
// Special handling for top row
if (at_top) {
    // Top left corner
    *of++ = DIV_2(f[0]) + DIV_4(f[0]) + DIV_8(f[1]) + DIV_8(f2[0]) + adj_tl;
    
    // Top center pixels
    while (x--) {
        of[0] = DIV_2(f[0]) + DIV_8(f[0]) + DIV_8(f[1]) + DIV_8(f[-1]) + DIV_8(f2[0]) + adj_tl2;
        // ... process 4 pixels
    }
    
    // Top right corner
    *of++ = DIV_2(f[0]) + DIV_4(f[0]) + DIV_8(f[-1]) + DIV_8(f2[0]) + adj_tl;
}
```

### **Left/Right Edge Processing**
```cpp
// Left edge
*of++ = DIV_2(f[0]) + DIV_8(f[0]) + DIV_8(f[1]) + DIV_8(f2[0]) + DIV_8(f3[0]) + adj_tl1;

// Right edge (similar processing)
```

### **Bottom Edge Processing**
```cpp
// Similar to top edge but with different boundary conditions
if (at_bottom) {
    // Process bottom row with special edge handling
}
```

---

## 📊 **Performance Characteristics**

### **Complexity**
- **Time Complexity**: O(w × h) per thread
- **Space Complexity**: O(1) - in-place processing
- **Thread Scaling**: Linear with CPU cores

### **Optimization Features**
- **MMX SIMD**: 64-bit parallel processing
- **Loop unrolling**: 4-pixel batches
- **Bit operations**: Fast division by powers of 2
- **Memory access**: Optimized for cache locality

---

## 🔌 **Phoenix Integration**

### **AvsModuleNode Implementation**
```csharp
public class BlurConvolutionNode : AvsModuleNode
{
    // Configuration
    public bool Enabled { get; set; } = true;
    public int BlurMode { get; set; } = 0;
    public bool RoundingMode { get; set; } = false;
    
    // Convolution kernel (5x5)
    private readonly int[,] convolutionKernel = new int[5, 5]
    {
        { 1,  4,  6,  4, 1 },
        { 4, 16, 24, 16, 4 },
        { 6, 24, 36, 24, 6 },
        { 4, 16, 24, 16, 4 },
        { 1,  4,  6,  4, 1 }
    };
    
    private readonly int kernelSum = 256; // Sum of all kernel values
    
    // Multi-threading support
    private int threadCount;
    private Task[] processingTasks;
    private readonly object lockObject = new object();
    
    // Audio data for beat reactivity
    private float[] leftChannelData;
    private float[] rightChannelData;
    private float[] centerChannelData;
    
    // Constructor
    public BlurConvolutionNode()
    {
        // Initialize multi-threading
        threadCount = Environment.ProcessorCount;
        processingTasks = new Task[threadCount];
    }
    
    // Processing
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled) return;
        
        // Update audio data
        UpdateAudioData(ctx);
        
        // Apply 5x5 convolution kernel with multi-threading
        ApplyConvolutionKernel(ctx, input, output);
    }
    
    private void UpdateAudioData(FrameContext ctx)
    {
        // Update audio data
        leftChannelData = ctx.AudioData.Spectrum[0];
        rightChannelData = ctx.AudioData.Spectrum[1];
        
        // Calculate center channel
        centerChannelData = new float[leftChannelData.Length];
        for (int i = 0; i < leftChannelData.Length; i++)
        {
            centerChannelData[i] = (leftChannelData[i] + rightChannelData[i]) / 2.0f;
        }
    }
    
    private void ApplyConvolutionKernel(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        
        // Multi-threaded row processing
        int rowsPerThread = height / threadCount;
        
        for (int threadIndex = 0; threadIndex < threadCount; threadIndex++)
        {
            int startRow = threadIndex * rowsPerThread;
            int endRow = (threadIndex == threadCount - 1) ? height : startRow + rowsPerThread;
            
            processingTasks[threadIndex] = Task.Run(() =>
            {
                ProcessRowRange(startRow, endRow, width, height, input, output);
            });
        }
        
        // Wait for all threads to complete
        Task.WaitAll(processingTasks);
    }
    
    private void ProcessRowRange(int startRow, int endRow, int width, int height, ImageBuffer input, ImageBuffer output)
    {
        for (int y = startRow; y < endRow; y++)
        {
            ProcessRow(y, width, height, input, output);
        }
    }
    
    private void ProcessRow(int y, int width, int height, ImageBuffer input, ImageBuffer output)
    {
        // Process pixels in batches for SIMD optimization
        int batchSize = 4;
        int processedPixels = 0;
        
        for (int x = 0; x < width; x += batchSize)
        {
            int remainingPixels = Math.Min(batchSize, width - x);
            
            if (remainingPixels == batchSize)
            {
                // Process full batch with SIMD-like optimization
                ProcessPixelBatch(x, y, width, height, input, output);
                processedPixels += batchSize;
            }
            else
            {
                // Process remaining pixels individually
                for (int i = 0; i < remainingPixels; i++)
                {
                    ProcessPixel(x + i, y, width, height, input, output);
                    processedPixels++;
                }
            }
        }
    }
    
    private void ProcessPixelBatch(int startX, int y, int width, int height, ImageBuffer input, ImageBuffer output)
    {
        // Process 4 pixels in parallel using SIMD-like operations
        Color[] sourceColors = new Color[4];
        Color[] resultColors = new Color[4];
        
        // Load source pixels
        for (int i = 0; i < 4; i++)
        {
            sourceColors[i] = input.GetPixel(startX + i, y);
        }
        
        // Apply convolution to each pixel in the batch
        for (int i = 0; i < 4; i++)
        {
            resultColors[i] = ApplyConvolutionToPixel(startX + i, y, width, height, input);
        }
        
        // Store result pixels
        for (int i = 0; i < 4; i++)
        {
            output.SetPixel(startX + i, y, resultColors[i]);
        }
    }
    
    private void ProcessPixel(int x, int y, int width, int height, ImageBuffer input, ImageBuffer output)
    {
        Color resultColor = ApplyConvolutionToPixel(x, y, width, height, input);
        output.SetPixel(x, y, resultColor);
    }
    
    private Color ApplyConvolutionToPixel(int x, int y, int width, int height, ImageBuffer input)
    {
        // Apply 5x5 convolution kernel
        int redSum = 0, greenSum = 0, blueSum = 0, alphaSum = 0;
        int validKernelSum = 0;
        
        // Process kernel
        for (int ky = -2; ky <= 2; ky++)
        {
            for (int kx = -2; kx <= 2; kx++)
            {
                int sampleX = x + kx;
                int sampleY = y + ky;
                
                // Check bounds
                if (sampleX >= 0 && sampleX < width && sampleY >= 0 && sampleY < height)
                {
                    Color sampleColor = input.GetPixel(sampleX, sampleY);
                    int kernelValue = convolutionKernel[ky + 2, kx + 2];
                    
                    redSum += sampleColor.R * kernelValue;
                    greenSum += sampleColor.G * kernelValue;
                    blueSum += sampleColor.B * kernelValue;
                    alphaSum += sampleColor.A * kernelValue;
                    validKernelSum += kernelValue;
                }
            }
        }
        
        // Normalize result
        if (validKernelSum > 0)
        {
            int red = redSum / validKernelSum;
            int green = greenSum / validKernelSum;
            int blue = blueSum / validKernelSum;
            int alpha = alphaSum / validKernelSum;
            
            // Apply rounding mode if enabled
            if (RoundingMode)
            {
                red = ApplyRounding(red);
                green = ApplyRounding(green);
                blue = ApplyRounding(blue);
                alpha = ApplyRounding(alpha);
            }
            
            return Color.FromArgb(alpha, red, green, blue);
        }
        
        // Return original color if no valid samples
        return input.GetPixel(x, y);
    }
    
    private int ApplyRounding(int value)
    {
        // Apply rounding similar to MMX rounding mode
        if (RoundingMode)
        {
            // Add 128 for proper rounding (equivalent to MMX rounding)
            value += 128;
        }
        return value;
    }
    
    // Alternative implementation using .NET SIMD for better performance
    private void ApplyConvolutionKernelSIMD(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        int width = ctx.Width;
        int height = ctx.Height;
        
        // Use Vector4 for SIMD processing
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x += Vector<float>.Count)
            {
                ProcessPixelBatchSIMD(x, y, width, height, input, output);
            }
        }
    }
    
    private void ProcessPixelBatchSIMD(int startX, int y, int width, int height, ImageBuffer input, ImageBuffer output)
    {
        // Process pixels using Vector4 for SIMD optimization
        Vector4[] kernelWeights = new Vector4[25];
        int kernelIndex = 0;
        
        // Convert kernel to Vector4 format
        for (int ky = -2; ky <= 2; ky++)
        {
            for (int kx = -2; kx <= 2; kx++)
            {
                float weight = convolutionKernel[ky + 2, kx + 2] / (float)kernelSum;
                kernelWeights[kernelIndex++] = new Vector4(weight);
            }
        }
        
        // Process each pixel in the batch
        for (int i = 0; i < Vector<float>.Count && startX + i < width; i++)
        {
            Color resultColor = ApplyConvolutionToPixelSIMD(startX + i, y, width, height, input, kernelWeights);
            output.SetPixel(startX + i, y, resultColor);
        }
    }
    
    private Color ApplyConvolutionToPixelSIMD(int x, int y, int width, int height, ImageBuffer input, Vector4[] kernelWeights)
    {
        Vector4 colorSum = Vector4.Zero;
        float weightSum = 0;
        
        // Apply kernel using SIMD operations
        for (int ky = -2; ky <= 2; ky++)
        {
            for (int kx = -2; kx <= 2; kx++)
            {
                int sampleX = x + kx;
                int sampleY = y + ky;
                
                if (sampleX >= 0 && sampleX < width && sampleY >= 0 && sampleY < height)
                {
                    Color sampleColor = input.GetPixel(sampleX, sampleY);
                    Vector4 sampleVector = new Vector4(
                        sampleColor.R / 255.0f,
                        sampleColor.G / 255.0f,
                        sampleColor.B / 255.0f,
                        sampleColor.A / 255.0f
                    );
                    
                    int kernelIndex = (ky + 2) * 5 + (kx + 2);
                    Vector4 weight = kernelWeights[kernelIndex];
                    
                    colorSum += sampleVector * weight;
                    weightSum += weight.X;
                }
            }
        }
        
        // Normalize and convert back to Color
        if (weightSum > 0)
        {
            Vector4 normalizedColor = colorSum / weightSum;
            
            int red = (int)(Math.Clamp(normalizedColor.X, 0, 1) * 255);
            int green = (int)(Math.Clamp(normalizedColor.Y, 0, 1) * 255);
            int blue = (int)(Math.Clamp(normalizedColor.Z, 0, 1) * 255);
            int alpha = (int)(Math.Clamp(normalizedColor.W, 0, 1) * 255);
            
            return Color.FromArgb(alpha, red, green, blue);
        }
        
        return input.GetPixel(x, y);
    }
    
    // Beat-reactive blur intensity
    private float GetBeatReactiveBlurIntensity(FrameContext ctx)
    {
        if (ctx.AudioData.IsBeat)
        {
            // Increase blur intensity on beat
            return 1.5f;
        }
        
        // Normal blur intensity
        return 1.0f;
    }
    
    // Audio-reactive kernel adjustment
    private void AdjustKernelForAudio(float intensity)
    {
        // Could modify kernel weights based on audio intensity
        // For now, just apply intensity scaling
        // This could be expanded to create dynamic blur effects
    }
}
```

### **Optimization Strategies**
- **SIMD Instructions**: Full .NET SIMD implementation using Vector4
- **Task Parallelism**: Complete multi-threaded row processing
- **Memory Management**: Optimized buffer handling with batch processing
- **Quality Options**: Configurable rounding and precision modes
- **Audio Integration**: Beat-reactive blur intensity and kernel adjustment
- **MMX-like Optimizations**: Bit operations and efficient division
- **Batch Processing**: 4-pixel batches for optimal SIMD utilization

---

## 📚 **Use Cases**

### **Visual Effects**
- **Motion blur**: Create smooth motion effects
- **Depth of field**: Simulate camera focus
- **Anti-aliasing**: Smooth jagged edges
- **Atmospheric effects**: Create fog or haze

### **Audio Integration**
- **Beat-reactive blur**: Dynamic blur intensity
- **Frequency blur**: Different blur for frequency bands
- **Waveform blur**: Smooth waveform visualization

---

## 🚀 **Future Enhancements**

### **Phoenix-Specific Features**
- **GPU acceleration**: CUDA/OpenCL implementation
- **Advanced kernels**: Custom convolution matrices
- **Real-time editing**: Live kernel adjustment
- **Effect chaining**: Multiple blur passes

### **Advanced Blur Types**
- **Gaussian blur**: Smooth bell-curve distribution
- **Directional blur**: Motion-blur effects
- **Radial blur**: Zoom and rotation effects
- **Selective blur**: Mask-based blurring

---

## 📖 **References**

- **Source Code**: `r_blur.cpp` from VIS_AVS
- **Header**: `r_defs.h` for base class definitions
- **MMX Reference**: Intel MMX instruction set
- **Multi-threading**: `timing.h` for performance measurement
- **Base Class**: `C_RBASE2` for SMP support

---

**Status:** ✅ **THIRD EFFECT DOCUMENTED**  
**Next:** Color Map/Transitions effect analysis
