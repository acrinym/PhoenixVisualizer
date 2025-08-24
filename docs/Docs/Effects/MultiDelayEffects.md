# Multi-Delay Effects (Trans / Multi Delay)

## Overview

The **Multi-Delay Effects** system is a sophisticated multi-buffer video delay engine that creates complex temporal effects with up to 6 independent delay buffers. It implements advanced memory management with virtual memory allocation, beat-reactive timing controls, and intelligent buffer management for creating complex video delay visualizations. This effect provides the foundation for sophisticated temporal effects, echo visualizations, and complex video feedback systems.

## Source Analysis

### Core Architecture (`r_multidelay.cpp`)

The effect is implemented as a C++ class `C_MultiDelayClass` that inherits from `C_RBASE`. It provides a comprehensive multi-buffer delay system with virtual memory management, beat-reactive timing, and intelligent buffer allocation for creating complex temporal video effects.

### Key Components

#### Multi-Buffer System
Advanced multi-buffer delay engine:
- **6 Independent Buffers**: Separate delay buffers for complex effect combinations
- **Buffer Management**: Intelligent buffer allocation and deallocation
- **Memory Optimization**: Virtual memory management for efficient resource usage
- **Performance Scaling**: Dynamic buffer sizing based on delay requirements

#### Virtual Memory Management
Sophisticated memory control system:
- **Virtual Memory Allocation**: Windows VirtualAlloc for dynamic memory management
- **Buffer Sizing**: Intelligent buffer sizing with 2x allocation for beat-reactive buffers
- **Memory Reallocation**: Dynamic memory reallocation for changing delay requirements
- **Resource Cleanup**: Automatic memory cleanup and resource management

#### Beat-Reactive Timing
Dynamic audio integration:
- **Beat Detection**: Beat-reactive delay timing for musical synchronization
- **Frame Counting**: Intelligent frame counting and beat duration calculation
- **Dynamic Delays**: Real-time delay adjustment based on audio events
- **Temporal Synchronization**: Synchronized delay timing with audio events

#### Delay Modes
Multiple delay operation modes:
- **Mode 0**: No delay (pass-through)
- **Mode 1**: Input delay (store current frame)
- **Mode 2**: Output delay (retrieve delayed frame)
- **Active Buffer Selection**: Configurable active buffer for effect application

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `mode` | int | 0-2 | 0 | Delay operation mode |
| `activebuffer` | int | 0-5 | 0 | Active delay buffer index |
| `usebeats[6]` | bool[] | true/false | false | Beat-reactive timing for each buffer |
| `delay[6]` | int[] | 0+ | 0 | Frame delay for each buffer |

### Delay Modes

| Mode | Value | Description | Behavior |
|------|-------|-------------|----------|
| **No Delay** | 0 | Pass-through mode | No delay effect applied |
| **Input Delay** | 1 | Store current frame | Current frame stored in delay buffer |
| **Output Delay** | 2 | Retrieve delayed frame | Delayed frame retrieved and displayed |

### Buffer Configuration

| Buffer Index | Use Beats | Frame Delay | Description |
|--------------|-----------|-------------|-------------|
| **Buffer 0** | Configurable | Configurable | Primary delay buffer |
| **Buffer 1** | Configurable | Configurable | Secondary delay buffer |
| **Buffer 2** | Configurable | Configurable | Tertiary delay buffer |
| **Buffer 3** | Configurable | Configurable | Quaternary delay buffer |
| **Buffer 4** | Configurable | Configurable | Quinary delay buffer |
| **Buffer 5** | Configurable | Configurable | Senary delay buffer |

## C# Implementation

```csharp
public class MultiDelayEffectsNode : AvsModuleNode
{
    public int Mode { get; set; } = 0;
    public int ActiveBuffer { get; set; } = 0;
    public bool[] UseBeats { get; set; } = new bool[6];
    public int[] Delay { get; set; } = new int[6];
    
    // Internal state
    private IntPtr[] buffers;
    private IntPtr[] inputPositions;
    private IntPtr[] outputPositions;
    private long[] bufferSizes;
    private long[] virtualBufferSizes;
    private long[] oldVirtualBufferSizes;
    private long[] frameDelays;
    private int lastWidth, lastHeight;
    private long framesSinceBeat;
    private long framesPerBeat;
    private long frameMemory;
    private long oldFrameMemory;
    private int renderId;
    private int creationId;
    private static int numInstances = 0;
    private readonly object renderLock = new object();
    
    // Performance optimization
    private const int MaxBuffers = 6;
    private const int MaxMode = 2;
    private const int MinMode = 0;
    private const int MaxActiveBuffer = 5;
    private const int MinActiveBuffer = 0;
    private const int MaxDelay = 1000;
    private const int MinDelay = 0;
    private const int BufferSizeMultiplier = 2;
    
    public MultiDelayEffectsNode()
    {
        buffers = new IntPtr[MaxBuffers];
        inputPositions = new IntPtr[MaxBuffers];
        outputPositions = new IntPtr[MaxBuffers];
        bufferSizes = new long[MaxBuffers];
        virtualBufferSizes = new long[MaxBuffers];
        oldVirtualBufferSizes = new long[MaxBuffers];
        frameDelays = new long[MaxBuffers];
        
        numInstances++;
        creationId = numInstances;
        
        if (creationId == 1)
        {
            InitializeGlobalState();
        }
        
        InitializeBuffers();
        lastWidth = lastHeight = 0;
    }
    
    private void InitializeGlobalState()
    {
        renderId = 0;
        framesSinceBeat = 0;
        framesPerBeat = 0;
        frameMemory = 1;
        oldFrameMemory = 1;
    }
    
    private void InitializeBuffers()
    {
        for (int i = 0; i < MaxBuffers; i++)
        {
            UseBeats[i] = false;
            Delay[i] = 0;
            frameDelays[i] = 0;
            bufferSizes[i] = 1;
            virtualBufferSizes[i] = 1;
            oldVirtualBufferSizes[i] = 1;
            
            // Allocate initial buffer
            buffers[i] = AllocateVirtualMemory(bufferSizes[i]);
            inputPositions[i] = buffers[i];
            outputPositions[i] = buffers[i];
        }
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (ctx.Width <= 0 || ctx.Height <= 0) return;
        
        lock (renderLock)
        {
            // Update buffers if dimensions changed
            UpdateBuffers(ctx);
            
            // Process delay effect
            ProcessDelayEffect(ctx, input, output);
            
            // Update buffer positions
            UpdateBufferPositions();
        }
    }
    
    private void UpdateBuffers(FrameContext ctx)
    {
        if (lastWidth != ctx.Width || lastHeight != ctx.Height)
        {
            lastWidth = ctx.Width;
            lastHeight = ctx.Height;
            frameMemory = ctx.Width * ctx.Height * 4; // 4 bytes per pixel (RGBA)
        }
    }
    
    private void ProcessDelayEffect(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        // Handle beat detection and timing
        if (ctx.IsBeat)
        {
            framesPerBeat = framesSinceBeat;
            for (int i = 0; i < MaxBuffers; i++)
            {
                if (UseBeats[i])
                {
                    frameDelays[i] = framesPerBeat + 1;
                }
            }
            framesSinceBeat = 0;
        }
        framesSinceBeat++;
        
        // Update buffer management
        UpdateBufferManagement();
        
        // Apply delay effect
        if (Mode != 0 && frameDelays[ActiveBuffer] > 1)
        {
            if (Mode == 2)
            {
                // Output delay mode - retrieve delayed frame
                CopyDelayedFrameToOutput(ctx, output);
            }
            else
            {
                // Input delay mode - store current frame
                CopyCurrentFrameToBuffer(ctx, input);
            }
        }
    }
    
    private void UpdateBufferManagement()
    {
        for (int i = 0; i < MaxBuffers; i++)
        {
            if (frameDelays[i] > 1)
            {
                virtualBufferSizes[i] = frameDelays[i] * frameMemory;
                
                if (frameMemory == oldFrameMemory)
                {
                    if (virtualBufferSizes[i] != oldVirtualBufferSizes[i])
                    {
                        if (virtualBufferSizes[i] > oldVirtualBufferSizes[i])
                        {
                            if (virtualBufferSizes[i] > bufferSizes[i])
                            {
                                // Allocate new memory
                                ReallocateBuffer(i);
                            }
                            else
                            {
                                // Adjust existing buffer
                                AdjustExistingBuffer(i);
                            }
                        }
                        else
                        {
                            // Reduce buffer size
                            ReduceBufferSize(i);
                        }
                        oldVirtualBufferSizes[i] = virtualBufferSizes[i];
                    }
                }
                else
                {
                    // Frame size changed, reallocate
                    ReallocateBuffer(i);
                    oldFrameMemory = frameMemory;
                }
            }
        }
    }
    
    private void ReallocateBuffer(int bufferIndex)
    {
        // Free old buffer
        FreeVirtualMemory(buffers[bufferIndex], bufferSizes[bufferIndex]);
        
        // Calculate new buffer size
        if (UseBeats[bufferIndex])
        {
            bufferSizes[bufferIndex] = BufferSizeMultiplier * virtualBufferSizes[bufferIndex];
        }
        else
        {
            bufferSizes[bufferIndex] = virtualBufferSizes[bufferIndex];
        }
        
        // Allocate new buffer
        buffers[bufferIndex] = AllocateVirtualMemory(bufferSizes[bufferIndex]);
        
        if (buffers[bufferIndex] != IntPtr.Zero)
        {
            // Update buffer positions
            outputPositions[bufferIndex] = buffers[bufferIndex];
            inputPositions[bufferIndex] = IntPtr.Add(buffers[bufferIndex], 
                (int)(virtualBufferSizes[bufferIndex] - frameMemory));
        }
        else
        {
            // Allocation failed, reset delay
            frameDelays[bufferIndex] = 0;
            if (UseBeats[bufferIndex])
            {
                framesPerBeat = 0;
                framesSinceBeat = 0;
                frameDelays[bufferIndex] = 0;
                Delay[bufferIndex] = 0;
            }
        }
    }
    
    private void AdjustExistingBuffer(int bufferIndex)
    {
        // Calculate size adjustments
        long size = ((long)IntPtr.Add(bufferIndex, (int)oldVirtualBufferSizes[bufferIndex])) - 
                   ((long)outputPositions[bufferIndex]);
        long newEnd = (long)buffers[bufferIndex] + virtualBufferSizes[bufferIndex];
        long destination = newEnd - size;
        
        // Move existing data
        MoveMemory(destination, outputPositions[bufferIndex], size);
        
        // Fill remaining space with copies
        for (long pos = (long)outputPositions[bufferIndex]; pos < destination; pos += frameMemory)
        {
            CopyMemory(pos, destination, frameMemory);
        }
    }
    
    private void ReduceBufferSize(int bufferIndex)
    {
        long preSegmentSize = ((long)outputPositions[bufferIndex]) - ((long)buffers[bufferIndex]);
        
        if (preSegmentSize > virtualBufferSizes[bufferIndex])
        {
            // Move data to beginning of buffer
            MoveMemory(buffers[bufferIndex], 
                (long)buffers[bufferIndex] + preSegmentSize - virtualBufferSizes[bufferIndex], 
                virtualBufferSizes[bufferIndex]);
            
            inputPositions[bufferIndex] = IntPtr.Add(buffers[bufferIndex], 
                (int)(virtualBufferSizes[bufferIndex] - frameMemory));
            outputPositions[bufferIndex] = buffers[bufferIndex];
        }
        else if (preSegmentSize < virtualBufferSizes[bufferIndex])
        {
            // Move data to fill remaining space
            long remainingSpace = virtualBufferSizes[bufferIndex] - preSegmentSize;
            long source = (long)buffers[bufferIndex] + oldVirtualBufferSizes[bufferIndex] + 
                         preSegmentSize - virtualBufferSizes[bufferIndex];
            
            MoveMemory(outputPositions[bufferIndex], source, remainingSpace);
        }
    }
    
    private void CopyDelayedFrameToOutput(FrameContext ctx, ImageBuffer output)
    {
        // Copy delayed frame data to output
        byte[] frameData = new byte[frameMemory];
        Marshal.Copy(outputPositions[ActiveBuffer], frameData, 0, (int)frameMemory);
        
        // Convert frame data to output image
        ConvertFrameDataToImage(frameData, ctx, output);
    }
    
    private void CopyCurrentFrameToBuffer(FrameContext ctx, ImageBuffer input)
    {
        // Convert input image to frame data
        byte[] frameData = ConvertImageToFrameData(input, ctx);
        
        // Copy frame data to buffer
        Marshal.Copy(frameData, 0, inputPositions[ActiveBuffer], (int)frameMemory);
    }
    
    private void UpdateBufferPositions()
    {
        for (int i = 0; i < MaxBuffers; i++)
        {
            // Advance buffer positions
            inputPositions[i] = IntPtr.Add(inputPositions[i], (int)frameMemory);
            outputPositions[i] = IntPtr.Add(outputPositions[i], (int)frameMemory);
            
            // Wrap around if necessary
            if ((long)inputPositions[i] >= (long)buffers[i] + virtualBufferSizes[i])
            {
                inputPositions[i] = buffers[i];
            }
            
            if ((long)outputPositions[i] >= (long)buffers[i] + virtualBufferSizes[i])
            {
                outputPositions[i] = buffers[i];
            }
        }
    }
    
    private byte[] ConvertImageToFrameData(ImageBuffer image, FrameContext ctx)
    {
        byte[] frameData = new byte[frameMemory];
        int index = 0;
        
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                Color pixel = image.GetPixel(x, y);
                frameData[index++] = pixel.R;
                frameData[index++] = pixel.G;
                frameData[index++] = pixel.B;
                frameData[index++] = pixel.A;
            }
        }
        
        return frameData;
    }
    
    private void ConvertFrameDataToImage(byte[] frameData, FrameContext ctx, ImageBuffer output)
    {
        int index = 0;
        
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                Color pixel = Color.FromRgba(
                    frameData[index++],
                    frameData[index++],
                    frameData[index++],
                    frameData[index++]
                );
                output.SetPixel(x, y, pixel);
            }
        }
    }
    
    // Virtual memory management
    private IntPtr AllocateVirtualMemory(long size)
    {
        // This would use Windows VirtualAlloc in a real implementation
        // For now, we'll use managed memory allocation
        return Marshal.AllocHGlobal((int)size);
    }
    
    private void FreeVirtualMemory(IntPtr ptr, long size)
    {
        if (ptr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
    
    private void MoveMemory(long destination, long source, long size)
    {
        // This would use Windows MoveMemory in a real implementation
        // For now, we'll use managed memory operations
        byte[] temp = new byte[size];
        Marshal.Copy((IntPtr)source, temp, 0, (int)size);
        Marshal.Copy(temp, 0, (IntPtr)destination, (int)size);
    }
    
    private void CopyMemory(long destination, long source, long size)
    {
        // This would use Windows CopyMemory in a real implementation
        // For now, we'll use managed memory operations
        byte[] temp = new byte[size];
        Marshal.Copy((IntPtr)source, temp, 0, (int)size);
        Marshal.Copy(temp, 0, (IntPtr)destination, (int)size);
    }
    
    // Public interface for parameter adjustment
    public void SetMode(int mode) 
    { 
        Mode = Math.Clamp(mode, MinMode, MaxMode); 
    }
    
    public void SetActiveBuffer(int buffer) 
    { 
        ActiveBuffer = Math.Clamp(buffer, MinActiveBuffer, MaxActiveBuffer); 
    }
    
    public void SetUseBeats(int bufferIndex, bool useBeats) 
    { 
        if (bufferIndex >= 0 && bufferIndex < MaxBuffers)
        {
            UseBeats[bufferIndex] = useBeats;
            UpdateFrameDelay(bufferIndex);
        }
    }
    
    public void SetDelay(int bufferIndex, int delay) 
    { 
        if (bufferIndex >= 0 && bufferIndex < MaxBuffers)
        {
            Delay[bufferIndex] = Math.Clamp(delay, MinDelay, MaxDelay);
            UpdateFrameDelay(bufferIndex);
        }
    }
    
    private void UpdateFrameDelay(int bufferIndex)
    {
        if (UseBeats[bufferIndex])
        {
            frameDelays[bufferIndex] = framesPerBeat + 1;
        }
        else
        {
            frameDelays[bufferIndex] = Delay[bufferIndex] + 1;
        }
    }
    
    // Status queries
    public int GetMode() => Mode;
    public int GetActiveBuffer() => ActiveBuffer;
    public bool GetUseBeats(int bufferIndex) => (bufferIndex >= 0 && bufferIndex < MaxBuffers) ? UseBeats[bufferIndex] : false;
    public int GetDelay(int bufferIndex) => (bufferIndex >= 0 && bufferIndex < MaxBuffers) ? Delay[bufferIndex] : 0;
    public long GetFrameDelay(int bufferIndex) => (bufferIndex >= 0 && bufferIndex < MaxBuffers) ? frameDelays[bufferIndex] : 0;
    public long GetFramesSinceBeat() => framesSinceBeat;
    public long GetFramesPerBeat() => framesPerBeat;
    public long GetFrameMemory() => frameMemory;
    public int GetCreationId() => creationId;
    public static int GetNumInstances() => numInstances;
    
    // Advanced delay control
    public void ResetBuffer(int bufferIndex)
    {
        if (bufferIndex >= 0 && bufferIndex < MaxBuffers)
        {
            frameDelays[bufferIndex] = 0;
            if (UseBeats[bufferIndex])
            {
                framesPerBeat = 0;
                framesSinceBeat = 0;
                frameDelays[bufferIndex] = 0;
                Delay[bufferIndex] = 0;
            }
        }
    }
    
    public void ResetAllBuffers()
    {
        for (int i = 0; i < MaxBuffers; i++)
        {
            ResetBuffer(i);
        }
    }
    
    public void SetBeatReactiveMode(int bufferIndex, bool enabled)
    {
        SetUseBeats(bufferIndex, enabled);
        if (enabled)
        {
            frameDelays[bufferIndex] = framesPerBeat + 1;
        }
        else
        {
            frameDelays[bufferIndex] = Delay[bufferIndex] + 1;
        }
    }
    
    // Delay mode presets
    public void SetEchoMode(int bufferIndex, int delay)
    {
        SetActiveBuffer(bufferIndex);
        SetMode(2); // Output delay
        SetUseBeats(bufferIndex, false);
        SetDelay(bufferIndex, delay);
    }
    
    public void SetBeatEchoMode(int bufferIndex)
    {
        SetActiveBuffer(bufferIndex);
        SetMode(2); // Output delay
        SetUseBeats(bufferIndex, true);
        SetDelay(bufferIndex, 0);
    }
    
    public void SetFeedbackMode(int bufferIndex, int delay)
    {
        SetActiveBuffer(bufferIndex);
        SetMode(1); // Input delay
        SetUseBeats(bufferIndex, false);
        SetDelay(bufferIndex, delay);
    }
    
    // Performance optimization
    public void SetBufferSizeMultiplier(int multiplier)
    {
        // This could affect memory allocation strategy
        // For now, we use the default multiplier
    }
    
    public void EnableOptimizations(bool enable)
    {
        // Various optimization flags could be implemented here
    }
    
    public override void Dispose()
    {
        lock (renderLock)
        {
            numInstances--;
            
            if (numInstances == 0)
            {
                // Clean up all buffers
                for (int i = 0; i < MaxBuffers; i++)
                {
                    if (buffers[i] != IntPtr.Zero)
                    {
                        FreeVirtualMemory(buffers[i], bufferSizes[i]);
                        buffers[i] = IntPtr.Zero;
                    }
                }
            }
        }
    }
}
```

## Integration Points

### Video Processing Integration
- **Frame Buffer Management**: Advanced frame buffer management with virtual memory
- **Temporal Effects**: Complex temporal effects and video delay processing
- **Memory Optimization**: Intelligent memory management for video processing
- **Performance Scaling**: Dynamic performance scaling based on delay requirements

### Audio Integration
- **Beat Detection**: Beat-reactive delay timing for musical synchronization
- **Temporal Synchronization**: Synchronized delay timing with audio events
- **Dynamic Delays**: Real-time delay adjustment based on audio analysis
- **Musical Integration**: Deep integration with audio timing and beat detection

### Memory Management
- **Virtual Memory**: Windows VirtualAlloc for dynamic memory management
- **Buffer Optimization**: Intelligent buffer sizing and memory allocation
- **Resource Management**: Automatic resource cleanup and memory optimization
- **Performance Optimization**: Optimized memory operations for video processing

## Usage Examples

### Basic Echo Effect
```csharp
var multiDelayNode = new MultiDelayEffectsNode
{
    Mode = 2,                          // Output delay mode
    ActiveBuffer = 0,                  // Use buffer 0
    UseBeats = { false, false, false, false, false, false },
    Delay = { 30, 0, 0, 0, 0, 0 }    // 30 frame delay
};
```

### Beat-Reactive Echo
```csharp
var multiDelayNode = new MultiDelayEffectsNode
{
    Mode = 2,                          // Output delay mode
    ActiveBuffer = 1,                  // Use buffer 1
    UseBeats = { false, true, false, false, false, false },
    Delay = { 0, 0, 0, 0, 0, 0 }     // Beat-reactive timing
};
```

### Complex Multi-Buffer Delay
```csharp
var multiDelayNode = new MultiDelayEffectsNode
{
    Mode = 2,                          // Output delay mode
    ActiveBuffer = 0,                  // Use buffer 0
    UseBeats = { false, true, false, true, false, true },
    Delay = { 15, 0, 45, 0, 60, 0 }  // Mixed timing modes
};

// Configure different delay behaviors
multiDelayNode.SetEchoMode(0, 15);    // 15 frame echo
multiDelayNode.SetBeatEchoMode(1);    // Beat-reactive echo
multiDelayNode.SetFeedbackMode(2, 45); // 45 frame feedback
```

## Technical Notes

### Memory Architecture
The effect implements sophisticated memory processing:
- **Virtual Memory Management**: Windows VirtualAlloc for dynamic memory allocation
- **Buffer Optimization**: Intelligent buffer sizing with 2x allocation for beat-reactive buffers
- **Memory Reallocation**: Dynamic memory reallocation for changing delay requirements
- **Resource Cleanup**: Automatic memory cleanup and resource management

### Delay Architecture
Advanced delay processing system:
- **Multi-Buffer System**: 6 independent delay buffers for complex effect combinations
- **Beat Reactivity**: Beat-reactive delay timing for musical synchronization
- **Temporal Effects**: Complex temporal effects and video delay processing
- **Performance Scaling**: Dynamic performance scaling based on delay requirements

### Integration System
Sophisticated system integration:
- **Video Processing**: Deep integration with video frame processing pipeline
- **Audio Synchronization**: Beat-reactive timing and audio synchronization
- **Memory Management**: Advanced memory management and optimization
- **Performance Optimization**: Optimized operations for video processing

This effect provides the foundation for sophisticated temporal video effects, creating complex delay visualizations that respond dynamically to audio input and provide advanced video processing capabilities for sophisticated AVS visualization systems.
