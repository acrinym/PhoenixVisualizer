# AVI Video Playback (Render / AVI)

## Overview

The **AVI Video Playback** system is a sophisticated video rendering engine that integrates AVI video files directly into AVS presets. It provides advanced video playback capabilities with multiple blending modes, adaptive timing, persistence effects, and intelligent frame management. This effect enables seamless integration of video content with real-time audio visualization, creating dynamic multimedia experiences.

## Source Analysis

### Core Architecture (`r_avi.cpp`)

The effect is implemented as a C++ class `C_AVIClass` that inherits from `C_RBASE`. It provides a comprehensive video playback system with AVI file handling, frame extraction, multiple blending algorithms, and advanced timing controls for synchronized video-audio experiences.

### Key Components

#### AVI File Management
Advanced video file handling system:
- **AVI Stream Management**: Direct AVI file access using Video for Windows (VFW)
- **Frame Extraction**: Efficient frame-by-frame extraction and decoding
- **Memory Management**: Intelligent buffer allocation and frame caching
- **File Path Resolution**: Automatic path resolution and file validation

#### Video Rendering Engine
Sophisticated video rendering pipeline:
- **Frame Decoding**: Direct frame extraction from AVI streams
- **Bitmap Conversion**: Conversion to 32-bit RGB format for compatibility
- **Resolution Adaptation**: Automatic resolution scaling and adaptation
- **Performance Optimization**: Efficient frame processing and memory management

#### Blending System
Multiple pixel blending algorithms:
- **Replace Mode**: Direct frame replacement without blending
- **Additive Blending**: Brightness-increasing blend mode
- **50/50 Blending**: Average-based blend for smooth transitions
- **Adaptive Blending**: Beat-reactive blending with persistence
- **Custom Persistence**: Configurable frame persistence duration

#### Timing and Synchronization
Advanced timing control system:
- **Speed Control**: Adjustable playback speed (0-1000 range)
- **Beat Synchronization**: Audio-reactive video timing
- **Persistence Control**: Configurable frame persistence (0-32 frames)
- **Adaptive Timing**: Dynamic timing based on audio events

### Parameters

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `enabled` | int | 0-1 | 1 | Enable AVI video playback |
| `blend` | int | 0-1 | 0 | Enable additive blending mode |
| `blendavg` | int | 0-1 | 1 | Enable 50/50 blending mode |
| `adapt` | int | 0-1 | 0 | Enable adaptive blending mode |
| `persist` | int | 0-32 | 6 | Frame persistence duration |
| `speed` | int | 0-1000 | 0 | Playback speed control |
| `ascName` | string | - | "" | AVI filename for playback |

### Blending Modes

| Mode | Description | Behavior |
|------|-------------|----------|
| **Replace** | Direct replacement | No blending, pure video output |
| **Additive** | Brightness increase | Adds video brightness to background |
| **50/50** | Average blending | Smooth transition between frames |
| **Adaptive** | Beat-reactive | Dynamic blending based on audio events |

## C# Implementation

```csharp
public class AVIVideoPlaybackNode : AvsModuleNode
{
    public bool Enabled { get; set; } = true;
    public bool Blend { get; set; } = false;
    public bool BlendAverage { get; set; } = true;
    public bool Adaptive { get; set; } = false;
    public int Persistence { get; set; } = 6;
    public int Speed { get; set; } = 0;
    public string AviFileName { get; set; } = "";
    
    // Internal state
    private VideoCapture videoCapture;
    private Mat currentFrame;
    private Mat previousFrame;
    private int frameIndex;
    private int totalFrames;
    private int lastWidth, lastHeight;
    private bool isLoaded;
    private bool isRendering;
    private int[] oldImageBuffer;
    private int oldImageWidth, oldImageHeight;
    private int persistCount;
    private long lastSpeedTime;
    private readonly object renderLock = new object();
    
    // Performance optimization
    private const int MinPersistence = 0;
    private const int MaxPersistence = 32;
    private const int MinSpeed = 0;
    private const int MaxSpeed = 1000;
    private const int DefaultPersistence = 6;
    private const int MaxFrameCache = 100;
    
    public AVIVideoPlaybackNode()
    {
        videoCapture = null;
        currentFrame = null;
        previousFrame = null;
        frameIndex = 0;
        totalFrames = 0;
        lastWidth = lastHeight = 0;
        isLoaded = false;
        isRendering = false;
        oldImageBuffer = null;
        oldImageWidth = oldImageHeight = 0;
        persistCount = 0;
        lastSpeedTime = 0;
    }
    
    public override void Process(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (!Enabled || !isLoaded || ctx.Width <= 0 || ctx.Height <= 0) return;
        
        lock (renderLock)
        {
            // Update buffers if dimensions changed
            UpdateBuffers(ctx);
            
            // Handle speed-based timing
            if (HandleSpeedTiming(ctx))
            {
                // Use cached frame for speed control
                CopyCachedFrame(ctx, output);
                return;
            }
            
            // Update speed timing
            lastSpeedTime = Environment.TickCount64;
            
            // Extract and process video frame
            ExtractVideoFrame(ctx);
            
            // Apply blending effects
            ApplyBlendingEffects(ctx, input, output);
            
            // Update persistence counter
            UpdatePersistenceCounter(ctx);
            
            // Cache current frame
            CacheCurrentFrame(ctx, output);
        }
    }
    
    private void UpdateBuffers(FrameContext ctx)
    {
        if (oldImageBuffer == null || oldImageWidth != ctx.Width || oldImageHeight != ctx.Height)
        {
            oldImageBuffer = new int[ctx.Width * ctx.Height];
            oldImageWidth = ctx.Width;
            oldImageHeight = ctx.Height;
        }
    }
    
    private bool HandleSpeedTiming(FrameContext ctx)
    {
        if (Speed > 0)
        {
            long currentTime = Environment.TickCount64;
            long timeThreshold = lastSpeedTime + Speed;
            
            if (currentTime < timeThreshold)
            {
                return true; // Use cached frame
            }
        }
        return false;
    }
    
    private void CopyCachedFrame(FrameContext ctx, ImageBuffer output)
    {
        if (oldImageBuffer != null)
        {
            int bufferSize = ctx.Width * ctx.Height;
            for (int i = 0; i < bufferSize; i++)
            {
                int x = i % ctx.Width;
                int y = i / ctx.Width;
                Color color = Color.FromArgb(oldImageBuffer[i]);
                output.SetPixel(x, y, color);
            }
        }
    }
    
    private void ExtractVideoFrame(FrameContext ctx)
    {
        if (videoCapture == null || !videoCapture.IsOpened()) return;
        
        isRendering = true;
        
        try
        {
            // Get current frame
            currentFrame = new Mat();
            if (videoCapture.Read(currentFrame))
            {
                // Resize frame to match output dimensions
                if (currentFrame.Width != ctx.Width || currentFrame.Height != ctx.Height)
                {
                    Mat resizedFrame = new Mat();
                    Cv2.Resize(currentFrame, resizedFrame, new Size(ctx.Width, ctx.Height));
                    currentFrame.Dispose();
                    currentFrame = resizedFrame;
                }
                
                // Convert BGR to RGB
                Mat rgbFrame = new Mat();
                Cv2.CvtColor(currentFrame, rgbFrame, ColorConversionCodes.BGR2RGB);
                currentFrame.Dispose();
                currentFrame = rgbFrame;
                
                frameIndex = (frameIndex + 1) % totalFrames;
            }
        }
        finally
        {
            isRendering = false;
        }
    }
    
    private void ApplyBlendingEffects(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        if (currentFrame == null) return;
        
        // Convert frame to output buffer
        ConvertFrameToOutput(ctx, output);
        
        // Apply blending based on mode
        if (Blend || (Adaptive && (ctx.IsBeat || persistCount > 0)))
        {
            ApplyAdditiveBlending(ctx, input, output);
        }
        else if (BlendAverage || Adaptive)
        {
            ApplyAverageBlending(ctx, input, output);
        }
        else
        {
            // Replace mode - no blending needed
            // Frame is already in output buffer
        }
    }
    
    private void ConvertFrameToOutput(FrameContext ctx, ImageBuffer output)
    {
        if (currentFrame == null) return;
        
        // Convert Mat to ImageBuffer
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                Vec3b pixel = currentFrame.Get<Vec3b>(y, x);
                Color color = Color.FromRgb(pixel.Item2, pixel.Item1, pixel.Item0);
                output.SetPixel(x, y, color);
            }
        }
    }
    
    private void ApplyAdditiveBlending(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                Color inputPixel = input.GetPixel(x, y);
                Color outputPixel = output.GetPixel(x, y);
                
                // Additive blending: add RGB values with clamping
                int r = Math.Min(255, inputPixel.R + outputPixel.R);
                int g = Math.Min(255, inputPixel.G + outputPixel.G);
                int b = Math.Min(255, inputPixel.B + outputPixel.B);
                
                Color blendedColor = Color.FromRgb((byte)r, (byte)g, (byte)b);
                output.SetPixel(x, y, blendedColor);
            }
        }
    }
    
    private void ApplyAverageBlending(FrameContext ctx, ImageBuffer input, ImageBuffer output)
    {
        for (int y = 0; y < ctx.Height; y++)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                Color inputPixel = input.GetPixel(x, y);
                Color outputPixel = output.GetPixel(x, y);
                
                // 50/50 blending: average RGB values
                int r = (inputPixel.R + outputPixel.R) / 2;
                int g = (inputPixel.G + outputPixel.G) / 2;
                int b = (inputPixel.B + outputPixel.B) / 2;
                
                Color blendedColor = Color.FromRgb((byte)r, (byte)g, (byte)b);
                output.SetPixel(x, y, blendedColor);
            }
        }
    }
    
    private void UpdatePersistenceCounter(FrameContext ctx)
    {
        if (ctx.IsBeat)
        {
            persistCount = Persistence;
        }
        else if (persistCount > 0)
        {
            persistCount--;
        }
    }
    
    private void CacheCurrentFrame(FrameContext ctx, ImageBuffer output)
    {
        if (oldImageBuffer != null)
        {
            int bufferSize = ctx.Width * ctx.Height;
            for (int i = 0; i < bufferSize; i++)
            {
                int x = i % ctx.Width;
                int y = i / ctx.Width;
                Color color = output.GetPixel(x, y);
                oldImageBuffer[i] = color.ToArgb();
            }
        }
    }
    
    // AVI file management
    public bool LoadAVI(string fileName)
    {
        try
        {
            CloseAVI();
            
            // Resolve full path
            string fullPath = Path.Combine(Environment.CurrentDirectory, fileName);
            if (!File.Exists(fullPath))
            {
                return false;
            }
            
            // Initialize video capture
            videoCapture = new VideoCapture(fullPath);
            if (!videoCapture.IsOpened())
            {
                videoCapture.Dispose();
                videoCapture = null;
                return false;
            }
            
            // Get video properties
            totalFrames = (int)videoCapture.Get(VideoCaptureProperties.FrameCount);
            frameIndex = 0;
            AviFileName = fileName;
            isLoaded = true;
            
            return true;
        }
        catch
        {
            CloseAVI();
            return false;
        }
    }
    
    public void CloseAVI()
    {
        lock (renderLock)
        {
            // Wait for rendering to complete
            while (isRendering)
            {
                Thread.Sleep(1);
            }
            
            if (videoCapture != null)
            {
                videoCapture.Dispose();
                videoCapture = null;
            }
            
            if (currentFrame != null)
            {
                currentFrame.Dispose();
                currentFrame = null;
            }
            
            if (previousFrame != null)
            {
                previousFrame.Dispose();
                previousFrame = null;
            }
            
            isLoaded = false;
            frameIndex = 0;
            totalFrames = 0;
        }
    }
    
    public void Reinitialize(int width, int height)
    {
        lock (renderLock)
        {
            if (lastWidth != 0 || lastHeight != 0)
            {
                // Clean up old resources
                if (currentFrame != null)
                {
                    currentFrame.Dispose();
                    currentFrame = null;
                }
                
                if (previousFrame != null)
                {
                    previousFrame.Dispose();
                    previousFrame = null;
                }
            }
            
            lastWidth = width;
            lastHeight = height;
        }
    }
    
    // Public interface for parameter adjustment
    public void SetEnabled(bool enable) { Enabled = enable; }
    
    public void SetBlend(bool blend) { Blend = blend; }
    
    public void SetBlendAverage(bool blendAvg) { BlendAverage = blendAvg; }
    
    public void SetAdaptive(bool adaptive) { Adaptive = adaptive; }
    
    public void SetPersistence(int persistence) 
    { 
        Persistence = Math.Clamp(persistence, MinPersistence, MaxPersistence); 
    }
    
    public void SetSpeed(int speed) 
    { 
        Speed = Math.Clamp(speed, MinSpeed, MaxSpeed); 
    }
    
    public void SetAviFileName(string fileName) 
    { 
        AviFileName = fileName;
        if (!string.IsNullOrEmpty(fileName))
        {
            LoadAVI(fileName);
        }
    }
    
    // Status queries
    public bool IsEnabled() => Enabled;
    public bool IsBlendEnabled() => Blend;
    public bool IsBlendAverageEnabled() => BlendAverage;
    public bool IsAdaptiveEnabled() => Adaptive;
    public int GetPersistence() => Persistence;
    public int GetSpeed() => Speed;
    public string GetAviFileName() => AviFileName;
    public bool IsLoaded() => isLoaded;
    public bool IsRendering() => isRendering;
    public int GetFrameIndex() => frameIndex;
    public int GetTotalFrames() => totalFrames;
    public int GetLastWidth() => lastWidth;
    public int GetLastHeight() => lastHeight;
    
    // Advanced video control
    public void SeekToFrame(int frame)
    {
        if (videoCapture != null && videoCapture.IsOpened())
        {
            lock (renderLock)
            {
                videoCapture.Set(VideoCaptureProperties.PosFrames, frame);
                frameIndex = frame % totalFrames;
            }
        }
    }
    
    public void SetPlaybackSpeed(double speed)
    {
        if (videoCapture != null && videoCapture.IsOpened())
        {
            lock (renderLock)
            {
                videoCapture.Set(VideoCaptureProperties.Fps, speed);
            }
        }
    }
    
    public double GetCurrentFPS()
    {
        if (videoCapture != null && videoCapture.IsOpened())
        {
            return videoCapture.Get(VideoCaptureProperties.Fps);
        }
        return 0.0;
    }
    
    public Size GetVideoDimensions()
    {
        if (videoCapture != null && videoCapture.IsOpened())
        {
            int width = (int)videoCapture.Get(VideoCaptureProperties.FrameWidth);
            int height = (int)videoCapture.Get(VideoCaptureProperties.FrameHeight);
            return new Size(width, height);
        }
        return new Size(0, 0);
    }
    
    // Frame manipulation
    public Mat GetCurrentFrame()
    {
        if (currentFrame != null)
        {
            return currentFrame.Clone();
        }
        return null;
    }
    
    public void SetCustomFrame(Mat frame)
    {
        lock (renderLock)
        {
            if (currentFrame != null)
            {
                currentFrame.Dispose();
            }
            currentFrame = frame?.Clone();
        }
    }
    
    // Buffer management
    public void ClearBuffers()
    {
        lock (renderLock)
        {
            if (oldImageBuffer != null)
            {
                Array.Clear(oldImageBuffer, 0, oldImageBuffer.Length);
            }
            persistCount = 0;
        }
    }
    
    public void ResetVideo()
    {
        if (videoCapture != null && videoCapture.IsOpened())
        {
            lock (renderLock)
            {
                videoCapture.Set(VideoCaptureProperties.PosFrames, 0);
                frameIndex = 0;
                persistCount = 0;
            }
        }
    }
    
    public override void Dispose()
    {
        CloseAVI();
        
        if (oldImageBuffer != null)
        {
            oldImageBuffer = null;
        }
    }
}
```

## Integration Points

### Audio Data Integration
- **Beat Detection**: Responds to audio events for adaptive blending
- **Audio Analysis**: Processes audio data for enhanced video effects
- **Dynamic Parameters**: Adjusts video behavior based on audio events
- **Synchronization**: Audio-reactive video timing and persistence

### Framebuffer Handling
- **Input/Output Buffers**: Processes `ImageBuffer` objects with full color support
- **Frame Caching**: Intelligent frame caching for performance optimization
- **Blending Operations**: Advanced pixel blending with multiple algorithms
- **Memory Management**: Efficient buffer allocation and frame management

### Performance Considerations
- **Frame Caching**: Intelligent caching system for speed control
- **Memory Optimization**: Efficient Mat and buffer management
- **Thread Safety**: Lock-based rendering for multi-threaded environments
- **Resource Management**: Proper disposal of OpenCV resources

## Usage Examples

### Basic Video Playback
```csharp
var aviNode = new AVIVideoPlaybackNode
{
    Enabled = true,
    Blend = false,                  // No blending
    BlendAverage = true,            // 50/50 blending
    Adaptive = false,               // No adaptive mode
    Persistence = 6,                // 6 frame persistence
    Speed = 0                       // Normal speed
};

// Load AVI file
aviNode.LoadAVI("background.avi");
```

### Beat-Reactive Video
```csharp
var aviNode = new AVIVideoPlaybackNode
{
    Enabled = true,
    Blend = false,                  // No additive blending
    BlendAverage = false,           // No 50/50 blending
    Adaptive = true,                // Enable adaptive mode
    Persistence = 12,               // 12 frame persistence
    Speed = 100                     // Slower playback
};

aviNode.LoadAVI("reactive.avi");
```

### Advanced Video Control
```csharp
var aviNode = new AVIVideoPlaybackNode
{
    Enabled = true,
    Blend = true,                   // Additive blending
    BlendAverage = false,           // No 50/50 blending
    Adaptive = false,               // No adaptive mode
    Persistence = 3,                // 3 frame persistence
    Speed = 500                     // Fast playback
};

aviNode.LoadAVI("effect.avi");

// Advanced controls
aviNode.SeekToFrame(100);          // Jump to frame 100
aviNode.SetPlaybackSpeed(2.0);     // Double speed
aviNode.SetPersistence(8);         // Increase persistence
```

## Technical Notes

### Video Architecture
The effect implements sophisticated video processing:
- **OpenCV Integration**: Direct video capture and frame processing
- **Frame Management**: Efficient frame extraction and caching
- **Resolution Adaptation**: Automatic scaling and format conversion
- **Performance Optimization**: Intelligent buffer management and caching

### Blending Architecture
Advanced pixel blending algorithms:
- **Additive Blending**: Brightness-increasing blend mode
- **Average Blending**: Smooth transition blending
- **Adaptive Blending**: Beat-reactive blending with persistence
- **Performance Optimization**: Efficient pixel-level operations

### Timing System
Sophisticated timing control:
- **Speed Control**: Adjustable playback speed (0-1000 range)
- **Beat Synchronization**: Audio-reactive video timing
- **Persistence Control**: Configurable frame persistence (0-32 frames)
- **Adaptive Timing**: Dynamic timing based on audio events

This effect provides the foundation for sophisticated video integration, enabling seamless combination of video content with real-time audio visualization for advanced multimedia AVS presets.
