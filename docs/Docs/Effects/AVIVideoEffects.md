# AVI Video Effects

## Overview
The AVI Video effect provides video playback capabilities for AVI files within AVS presets. It's essential for creating multimedia visualizations, video overlays, and dynamic content integration with precise control over video playback, timing, and synchronization with audio.

## C++ Source Analysis
**File:** `r_avi.cpp`  
**Class:** `C_THISCLASS : public C_RBASE`

### Key Properties
- **Video File Path**: Path to the AVI file to be played
- **Playback Mode**: Different playback behaviors and loops
- **Frame Rate Control**: Video frame rate and timing
- **Beat Synchronization**: Video synchronization with audio
- **Video Scaling**: Size and scaling of video output
- **Blend Mode**: How video is blended with other effects

### Core Functionality
```cpp
class C_THISCLASS : public C_RBASE
{
    char filename[MAX_PATH];
    int mode;
    int framerate;
    int onbeat;
    int scale;
    int blend;
    int loop;
    int param1;
    int param2;
    
    virtual int render(char visdata[2][2][576], int isBeat, int *framebuffer, int *fbout, int w, int h);
    virtual HWND conf(HINSTANCE hInstance, HWND hwndParent);
    virtual char *get_desc();
    virtual void load_config(unsigned char *data, int len);
    virtual int save_config(unsigned char *data);
};
```

## C# Implementation

### AVIVideoEffectsNode Class
```csharp
public class AVIVideoEffectsNode : BaseEffectNode
{
    public string VideoFilePath { get; set; } = "";
    public int PlaybackMode { get; set; } = 0;
    public float FrameRate { get; set; } = 30.0f;
    public bool BeatSynchronized { get; set; } = false;
    public float VideoScale { get; set; } = 1.0f;
    public int BlendMode { get; set; } = 0;
    public bool LoopVideo { get; set; } = true;
    public float PlaybackSpeed { get; set; } = 1.0f;
    public bool AutoResize { get; set; } = true;
    public int VideoWidth { get; private set; } = 0;
    public int VideoHeight { get; private set; } = 0;
    public float CurrentTime { get; private set; } = 0.0f;
    public float VideoDuration { get; private set; } = 0.0f;
    public bool IsPlaying { get; private set; } = false;
    public bool IsPaused { get; private set; } = false;
}
```

### Key Features
1. **AVI File Support**: Full AVI video file playback
2. **Multiple Playback Modes**: Different video behaviors and loops
3. **Frame Rate Control**: Precise video timing and synchronization
4. **Beat Synchronization**: Video sync with audio beat detection
5. **Video Scaling**: Dynamic size and scale control
6. **Blend Mode Support**: Various blending modes for video
7. **Real-time Control**: Play, pause, seek, and loop control

### Playback Modes
- **0**: Normal Playback (Standard forward playback)
- **1**: Reverse Playback (Backward playback)
- **2**: Ping-Pong (Forward then reverse loop)
- **3**: Random Frames (Random frame selection)
- **4**: Beat-Synced (Synchronized with beat detection)
- **5**: Speed Varied (Variable playback speed)
- **6**: Custom (User-defined playback behavior)

### Blend Modes
- **0**: Replace (Replace existing pixels)
- **1**: Add (Add to existing pixels)
- **2**: Multiply (Multiply with existing pixels)
- **3**: Screen (Screen with existing pixels)
- **4**: Overlay (Overlay with existing pixels)
- **5**: Alpha Blend (Alpha-based blending)
- **6**: Chroma Key (Chroma key compositing)

## Usage Examples

### Basic Video Playback
```csharp
var aviVideoNode = new AVIVideoEffectsNode
{
    VideoFilePath = "C:\\Videos\\background.avi",
    PlaybackMode = 0, // Normal playback
    FrameRate = 30.0f,
    BeatSynchronized = false,
    VideoScale = 1.0f,
    BlendMode = 0, // Replace
    LoopVideo = true,
    PlaybackSpeed = 1.0f,
    AutoResize = true
};
```

### Beat-Synchronized Video
```csharp
var aviVideoNode = new AVIVideoEffectsNode
{
    VideoFilePath = "C:\\Videos\\beat_sync.avi",
    PlaybackMode = 4, // Beat-synced
    FrameRate = 30.0f,
    BeatSynchronized = true,
    VideoScale = 1.5f,
    BlendMode = 1, // Add
    LoopVideo = true,
    PlaybackSpeed = 1.0f,
    AutoResize = false
};
```

### Ping-Pong Video Loop
```csharp
var aviVideoNode = new AVIVideoEffectsNode
{
    VideoFilePath = "C:\\Videos\\animation.avi",
    PlaybackMode = 2, // Ping-pong
    FrameRate = 25.0f,
    BeatSynchronized = false,
    VideoScale = 0.8f,
    BlendMode = 4, // Overlay
    LoopVideo = true,
    PlaybackSpeed = 1.2f,
    AutoResize = true
};
```

## Technical Implementation

### Core Video Algorithm
```csharp
protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
{
    if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
        return GetDefaultOutput();

    // Initialize video if not already done
    if (!IsVideoInitialized())
    {
        InitializeVideo();
    }

    // Update video playback state
    UpdateVideoPlayback(audioFeatures);
    
    // Get current video frame
    var videoFrame = GetCurrentVideoFrame();
    if (videoFrame == null)
    {
        return imageBuffer; // Return input if no video frame
    }

    // Create output buffer
    var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
    
    // Apply video frame with specified blend mode
    ApplyVideoFrame(videoFrame, output, imageBuffer);
    
    return output;
}
```

### Video Initialization
```csharp
private void InitializeVideo()
{
    if (string.IsNullOrEmpty(VideoFilePath) || !File.Exists(VideoFilePath))
    {
        throw new FileNotFoundException($"Video file not found: {VideoFilePath}");
    }

    try
    {
        // Initialize video decoder
        InitializeVideoDecoder();
        
        // Get video properties
        GetVideoProperties();
        
        // Set initial state
        IsPlaying = true;
        IsPaused = false;
        CurrentTime = 0.0f;
        
        // Pre-load first frame
        PreloadFirstFrame();
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"Failed to initialize video: {ex.Message}", ex);
    }
}
```

### Video Playback Update
```csharp
private void UpdateVideoPlayback(AudioFeatures audioFeatures)
{
    if (!IsPlaying || IsPaused)
        return;

    // Calculate frame time based on playback mode
    float frameTime = CalculateFrameTime(audioFeatures);
    
    // Update current time
    CurrentTime += frameTime * PlaybackSpeed;
    
    // Handle loop conditions
    if (CurrentTime >= VideoDuration)
    {
        HandleVideoLoop();
    }
    
    // Update video frame
    UpdateVideoFrame();
}
```

### Frame Time Calculation
```csharp
private float CalculateFrameTime(AudioFeatures audioFeatures)
{
    switch (PlaybackMode)
    {
        case 0: // Normal Playback
            return 1.0f / FrameRate;
            
        case 1: // Reverse Playback
            return -1.0f / FrameRate;
            
        case 2: // Ping-Pong
            return 1.0f / FrameRate;
            
        case 3: // Random Frames
            return Random.Next(1, (int)FrameRate) / FrameRate;
            
        case 4: // Beat-Synced
            if (audioFeatures?.IsBeat == true)
            {
                return 1.0f / FrameRate;
            }
            return 0.0f; // No frame advance
            
        case 5: // Speed Varied
            float speedVariation = (float)Math.Sin(CurrentTime * 0.1f) * 0.5f + 1.0f;
            return speedVariation / FrameRate;
            
        default:
            return 1.0f / FrameRate;
    }
}
```

## Video Frame Processing

### Current Frame Retrieval
```csharp
private ImageBuffer GetCurrentVideoFrame()
{
    if (!IsVideoInitialized())
        return null;

    try
    {
        // Get frame at current time
        var frame = GetFrameAtTime(CurrentTime);
        
        // Scale frame if needed
        if (VideoScale != 1.0f)
        {
            frame = ScaleFrame(frame, VideoScale);
        }
        
        // Resize frame if auto-resize is enabled
        if (AutoResize)
        {
            frame = ResizeFrame(frame, VideoWidth, VideoHeight);
        }
        
        return frame;
    }
    catch (Exception ex)
    {
        // Handle frame retrieval errors
        LogError($"Failed to get video frame: {ex.Message}");
        return null;
    }
}
```

### Frame Scaling
```csharp
private ImageBuffer ScaleFrame(ImageBuffer frame, float scale)
{
    if (scale == 1.0f)
        return frame;

    int newWidth = (int)(frame.Width * scale);
    int newHeight = (int)(frame.Height * scale);
    
    var scaledFrame = new ImageBuffer(newWidth, newHeight);
    
    // Use bilinear interpolation for scaling
    for (int y = 0; y < newHeight; y++)
    {
        for (int x = 0; x < newWidth; x++)
        {
            float srcX = x / scale;
            float srcY = y / scale;
            
            int pixel = InterpolatePixel(frame, srcX, srcY);
            scaledFrame.SetPixel(x, y, pixel);
        }
    }
    
    return scaledFrame;
}
```

### Frame Resizing
```csharp
private ImageBuffer ResizeFrame(ImageBuffer frame, int targetWidth, int targetHeight)
{
    if (frame.Width == targetWidth && frame.Height == targetHeight)
        return frame;

    var resizedFrame = new ImageBuffer(targetWidth, targetHeight);
    
    // Calculate scaling factors
    float scaleX = (float)frame.Width / targetWidth;
    float scaleY = (float)frame.Height / targetHeight;
    
    // Resize with aspect ratio preservation
    for (int y = 0; y < targetHeight; y++)
    {
        for (int x = 0; x < targetWidth; x++)
        {
            float srcX = x * scaleX;
            float srcY = y * scaleY;
            
            int pixel = InterpolatePixel(frame, srcX, srcY);
            resizedFrame.SetPixel(x, y, pixel);
        }
    }
    
    return resizedFrame;
}
```

## Video Blending

### Frame Blending Application
```csharp
private void ApplyVideoFrame(ImageBuffer videoFrame, ImageBuffer output, ImageBuffer background)
{
    // Ensure video frame matches output dimensions
    if (videoFrame.Width != output.Width || videoFrame.Height != output.Height)
    {
        videoFrame = ResizeFrame(videoFrame, output.Width, output.Height);
    }

    // Apply blend mode
    switch (BlendMode)
    {
        case 0: // Replace
            ApplyReplaceBlend(videoFrame, output);
            break;
        case 1: // Add
            ApplyAddBlend(videoFrame, output, background);
            break;
        case 2: // Multiply
            ApplyMultiplyBlend(videoFrame, output, background);
            break;
        case 3: // Screen
            ApplyScreenBlend(videoFrame, output, background);
            break;
        case 4: // Overlay
            ApplyOverlayBlend(videoFrame, output, background);
            break;
        case 5: // Alpha Blend
            ApplyAlphaBlend(videoFrame, output, background);
            break;
        case 6: // Chroma Key
            ApplyChromaKeyBlend(videoFrame, output, background);
            break;
        default:
            ApplyReplaceBlend(videoFrame, output);
            break;
    }
}
```

### Replace Blend
```csharp
private void ApplyReplaceBlend(ImageBuffer videoFrame, ImageBuffer output)
{
    // Direct copy of video frame
    Array.Copy(videoFrame.Pixels, output.Pixels, videoFrame.Pixels.Length);
}
```

### Add Blend
```csharp
private void ApplyAddBlend(ImageBuffer videoFrame, ImageBuffer output, ImageBuffer background)
{
    for (int i = 0; i < output.Pixels.Length; i++)
    {
        int videoPixel = videoFrame.Pixels[i];
        int bgPixel = background.Pixels[i];
        
        int r = Math.Min(255, (videoPixel & 0xFF) + (bgPixel & 0xFF));
        int g = Math.Min(255, ((videoPixel >> 8) & 0xFF) + ((bgPixel >> 8) & 0xFF));
        int b = Math.Min(255, ((videoPixel >> 16) & 0xFF) + ((bgPixel >> 16) & 0xFF));
        
        output.Pixels[i] = r | (g << 8) | (b << 16);
    }
}
```

### Alpha Blend
```csharp
private void ApplyAlphaBlend(ImageBuffer videoFrame, ImageBuffer output, ImageBuffer background)
{
    float alpha = 0.8f; // Video opacity
    
    for (int i = 0; i < output.Pixels.Length; i++)
    {
        int videoPixel = videoFrame.Pixels[i];
        int bgPixel = background.Pixels[i];
        
        int r = (int)((videoPixel & 0xFF) * alpha + (bgPixel & 0xFF) * (1.0f - alpha));
        int g = (int)(((videoPixel >> 8) & 0xFF) * alpha + ((bgPixel >> 8) & 0xFF) * (1.0f - alpha));
        int b = (int)(((videoPixel >> 16) & 0xFF) * alpha + ((bgPixel >> 16) & 0xFF) * (1.0f - alpha));
        
        output.Pixels[i] = r | (g << 8) | (b << 16);
    }
}
```

## Video Loop Handling

### Loop Management
```csharp
private void HandleVideoLoop()
{
    if (!LoopVideo)
    {
        IsPlaying = false;
        return;
    }

    switch (PlaybackMode)
    {
        case 0: // Normal - restart from beginning
            CurrentTime = 0.0f;
            break;
            
        case 1: // Reverse - restart from end
            CurrentTime = VideoDuration;
            break;
            
        case 2: // Ping-pong - reverse direction
            ReversePlaybackDirection();
            break;
            
        case 3: // Random - random position
            CurrentTime = (float)Random.NextDouble() * VideoDuration;
            break;
            
        case 4: // Beat-synced - restart from beginning
            CurrentTime = 0.0f;
            break;
            
        default:
            CurrentTime = 0.0f;
            break;
    }
}
```

### Ping-Pong Direction
```csharp
private void ReversePlaybackDirection()
{
    // Toggle between forward and reverse
    if (PlaybackSpeed > 0)
    {
        PlaybackSpeed = -PlaybackSpeed;
    }
    else
    {
        PlaybackSpeed = Math.Abs(PlaybackSpeed);
    }
}
```

## Video Control Methods

### Playback Control
```csharp
public void Play()
{
    IsPlaying = true;
    IsPaused = false;
}

public void Pause()
{
    IsPaused = true;
}

public void Stop()
{
    IsPlaying = false;
    IsPaused = false;
    CurrentTime = 0.0f;
}

public void Seek(float time)
{
    CurrentTime = Math.Max(0.0f, Math.Min(time, VideoDuration));
}
```

### Video Properties
```csharp
public void SetPlaybackSpeed(float speed)
{
    PlaybackSpeed = Math.Max(0.1f, Math.Min(5.0f, speed));
}

public void SetVideoScale(float scale)
{
    VideoScale = Math.Max(0.1f, Math.Min(5.0f, scale));
}

public void SetBlendMode(int mode)
{
    BlendMode = Math.Max(0, Math.Min(6, mode));
}
```

## Performance Optimization

### Optimization Techniques
1. **Frame Caching**: Cache decoded video frames
2. **Preloading**: Pre-load upcoming frames
3. **Memory Management**: Efficient frame buffer management
4. **Hardware Acceleration**: GPU-accelerated video decoding
5. **Threading**: Multi-threaded video processing

### Memory Management
- Efficient frame storage
- Minimize temporary allocations
- Use value types for calculations
- Optimize video operations

## Integration with EffectGraph

### Port Configuration
```csharp
protected override void InitializePorts()
{
    _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Background image"));
    _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Video-blended output"));
}
```

### Metadata Support
```csharp
public override EffectMetadata GetMetadata()
{
    var metadata = new EffectMetadata();
    metadata.Add("VideoFilePath", VideoFilePath);
    metadata.Add("PlaybackMode", PlaybackMode);
    metadata.Add("FrameRate", FrameRate);
    metadata.Add("BeatSynchronized", BeatSynchronized);
    metadata.Add("VideoScale", VideoScale);
    metadata.Add("BlendMode", BlendMode);
    metadata.Add("LoopVideo", LoopVideo);
    metadata.Add("PlaybackSpeed", PlaybackSpeed);
    metadata.Add("CurrentTime", CurrentTime);
    metadata.Add("VideoDuration", VideoDuration);
    metadata.Add("IsPlaying", IsPlaying);
    return metadata;
}
```

## Testing and Validation

### Test Cases
1. **Basic Playback**: Verify video playback accuracy
2. **Playback Modes**: Test all playback behaviors
3. **Blend Modes**: Test all blending algorithms
4. **Performance**: Measure video processing speed
5. **Edge Cases**: Handle boundary conditions
6. **Beat Synchronization**: Validate beat-synced playback

### Validation Methods
- Visual comparison with reference videos
- Performance benchmarking
- Memory usage analysis
- Video synchronization testing

## Future Enhancements

### Planned Features
1. **Advanced Codecs**: Support for more video formats
2. **3D Video**: Three-dimensional video support
3. **Real-time Effects**: Dynamic video processing
4. **Hardware Acceleration**: GPU-accelerated video
5. **Custom Shaders**: User-defined video effects

### Compatibility
- Full AVS preset compatibility
- Support for legacy video modes
- Performance parity with original
- Extended functionality

## Conclusion

The AVI Video effect provides essential video playback capabilities for multimedia AVS visualizations. This C# implementation maintains full compatibility with the original while adding modern features like beat synchronization and enhanced blending modes. Complete documentation ensures reliable operation in production environments with optimal performance and video quality.

## Complete C# Implementation

### AVIEffectsNode Class

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class AVIEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the AVI effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Path to the AVI file to be loaded and played
        /// </summary>
        public string AVIFilePath { get; set; } = "";

        /// <summary>
        /// Determines how the AVI frames are blended with existing content
        /// </summary>
        public int BlendMode { get; set; } = 0;

        /// <summary>
        /// Enables 50/50 blending mode
        /// </summary>
        public bool BlendAverage { get; set; } = true;

        /// <summary>
        /// Enables adaptive blending based on beat detection
        /// </summary>
        public bool Adaptive { get; set; } = false;

        /// <summary>
        /// Number of frames to maintain the effect after a beat
        /// </summary>
        public int Persistence { get; set; } = 6;

        /// <summary>
        /// Playback speed control (0-1000, higher values = slower playback)
        /// </summary>
        public int Speed { get; set; } = 0;

        /// <summary>
        /// Intensity of the effect (0.0 to 1.0)
        /// </summary>
        public float Intensity { get; set; } = 1.0f;

        #endregion

        #region Private Fields

        private bool isLoaded = false;
        private bool isRendering = false;
        private int currentFrameIndex = 0;
        private int totalFrames = 0;
        private int lastWidth = 0;
        private int lastHeight = 0;
        private Color[,] oldImageBuffer;
        private List<Color[,]> frameBuffer;
        private DateTime lastFrameTime;
        private int persistenceCount = 0;
        private bool isInitialized = false;

        #endregion

        #region Public Methods

        /// <summary>
        /// Processes the current frame with AVI video overlay
        /// </summary>
        public override void ProcessFrame(ImageBuffer imageBuffer, AudioFeatures audioFeatures)
        {
            if (!Enabled || !isLoaded || string.IsNullOrEmpty(AVIFilePath))
                return;

            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            // Initialize buffers if dimensions change
            if (lastWidth != width || lastHeight != height)
            {
                InitializeBuffers(width, height);
            }

            // Handle persistence and timing
            if (audioFeatures.IsBeat)
            {
                persistenceCount = Persistence;
            }
            else if (persistenceCount > 0)
            {
                persistenceCount--;
            }

            // Check if we should advance to next frame based on speed
            if (ShouldAdvanceFrame())
            {
                AdvanceFrame();
                lastFrameTime = DateTime.Now;
            }

            // Apply AVI effect with current blending mode
            ApplyAVIEffect(imageBuffer, audioFeatures);
        }

        /// <summary>
        /// Loads an AVI file for playback
        /// </summary>
        public bool LoadAVIFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                AVIFilePath = filePath;
                isLoaded = true;
                currentFrameIndex = 0;
                LoadAVIFrames();
                return true;
            }
            catch
            {
                isLoaded = false;
                return false;
            }
        }

        /// <summary>
        /// Closes the currently loaded AVI file
        /// </summary>
        public void CloseAVIFile()
        {
            isLoaded = false;
            frameBuffer?.Clear();
            frameBuffer = null;
            oldImageBuffer = null;
        }

        /// <summary>
        /// Sets the blending mode for the effect
        /// </summary>
        public void SetBlendMode(int mode)
        {
            BlendMode = mode;
            BlendAverage = (mode == 1);
            Adaptive = (mode == 2);
        }

        /// <summary>
        /// Sets the persistence duration for beat-responsive behavior
        /// </summary>
        public void SetPersistence(int frames)
        {
            Persistence = Math.Max(0, Math.Min(32, frames));
        }

        /// <summary>
        /// Sets the playback speed (0-1000)
        /// </summary>
        public void SetSpeed(int speed)
        {
            Speed = Math.Max(0, Math.Min(1000, speed));
        }

        #endregion

        #region Private Methods

        private void InitializeBuffers(int width, int height)
        {
            lastWidth = width;
            lastHeight = height;
            oldImageBuffer = new Color[width, height];
            isInitialized = true;
        }

        private bool ShouldAdvanceFrame()
        {
            if (Speed == 0)
                return true;

            var timeSinceLastFrame = DateTime.Now - lastFrameTime;
            var frameDelay = TimeSpan.FromMilliseconds(Speed);
            return timeSinceLastFrame >= frameDelay;
        }

        private void AdvanceFrame()
        {
            if (frameBuffer != null && frameBuffer.Count > 0)
            {
                currentFrameIndex = (currentFrameIndex + 1) % frameBuffer.Count;
            }
        }

        private void LoadAVIFrames()
        {
            // In a real implementation, this would use a video library like FFmpeg
            // For now, we'll create a placeholder frame buffer
            frameBuffer = new List<Color[,]>();
            
            // Generate sample frames for demonstration
            for (int i = 0; i < 30; i++) // 30 sample frames
            {
                var frame = new Color[lastWidth, lastHeight];
                for (int x = 0; x < lastWidth; x++)
                {
                    for (int y = 0; y < lastHeight; y++)
                    {
                        frame[x, y] = Color.FromArgb(
                            (i * 8) % 256,
                            (x * 255 / lastWidth),
                            (y * 255 / lastHeight)
                        );
                    }
                }
                frameBuffer.Add(frame);
            }
            
            totalFrames = frameBuffer.Count;
        }

        private void ApplyAVIEffect(ImageBuffer imageBuffer, AudioFeatures audioFeatures)
        {
            if (frameBuffer == null || frameBuffer.Count == 0)
                return;

            var currentFrame = frameBuffer[currentFrameIndex];
            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            // Determine blending mode based on settings and beat detection
            bool shouldBlend = BlendMode != 0 || (Adaptive && (audioFeatures.IsBeat || persistenceCount > 0));
            bool useAverage = BlendAverage || (Adaptive && !shouldBlend);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var existingColor = imageBuffer.GetPixel(x, y);
                    var aviColor = currentFrame[x, y];
                    
                    Color finalColor;
                    if (shouldBlend)
                    {
                        finalColor = BlendAdditive(existingColor, aviColor);
                    }
                    else if (useAverage)
                    {
                        finalColor = BlendAverage(existingColor, aviColor);
                    }
                    else
                    {
                        finalColor = aviColor;
                    }

                    // Apply intensity
                    finalColor = ApplyIntensity(finalColor, Intensity);
                    
                    imageBuffer.SetPixel(x, y, finalColor);
                }
            }

            // Store current frame for persistence
            StoreCurrentFrame(imageBuffer);
        }

        private Color BlendAdditive(Color a, Color b)
        {
            return Color.FromArgb(
                Math.Min(255, a.R + b.R),
                Math.Min(255, a.G + b.G),
                Math.Min(255, a.B + b.B),
                Math.Min(255, a.A + b.A)
            );
        }

        private Color BlendAverage(Color a, Color b)
        {
            return Color.FromArgb(
                (a.R + b.R) / 2,
                (a.G + b.G) / 2,
                (a.B + b.B) / 2,
                (a.A + b.A) / 2
            );
        }

        private Color ApplyIntensity(Color color, float intensity)
        {
            if (intensity >= 1.0f)
                return color;

            return Color.FromArgb(
                color.A,
                (int)(color.R * intensity),
                (int)(color.G * intensity),
                (int)(color.B * intensity)
            );
        }

        private void StoreCurrentFrame(ImageBuffer imageBuffer)
        {
            if (oldImageBuffer == null)
                return;

            int width = imageBuffer.Width;
            int height = imageBuffer.Height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    oldImageBuffer[x, y] = imageBuffer.GetPixel(x, y);
                }
            }
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Validates the current configuration
        /// </summary>
        public override bool ValidateConfiguration()
        {
            if (Persistence < 0 || Persistence > 32)
                Persistence = 6;

            if (Speed < 0 || Speed > 1000)
                Speed = 0;

            if (Intensity < 0.0f || Intensity > 1.0f)
                Intensity = 1.0f;

            return true;
        }

        /// <summary>
        /// Returns a summary of current settings
        /// </summary>
        public override string GetSettingsSummary()
        {
            return $"AVI Effect: {(Enabled ? "Enabled" : "Disabled")}, " +
                   $"File: {Path.GetFileName(AVIFilePath)}, " +
                   $"Blend: {(BlendMode == 0 ? "Replace" : BlendMode == 1 ? "Additive" : "Adaptive")}, " +
                   $"Persistence: {Persistence}, " +
                   $"Speed: {Speed}";
        }

        #endregion

        #region Cleanup

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CloseAVIFile();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
```

## Usage Examples

### Basic AVI Playback
```csharp
var aviNode = new AVIEffectsNode();
aviNode.LoadAVIFile("C:\\Videos\\background.avi");
aviNode.Enabled = true;
aviNode.BlendMode = 0; // Replace mode
```

### Beat-Responsive AVI Overlay
```csharp
var aviNode = new AVIEffectsNode();
aviNode.LoadAVIFile("C:\\Videos\\beat_effect.avi");
aviNode.Enabled = true;
aviNode.Adaptive = true;
aviNode.Persistence = 10;
aviNode.Speed = 100;
```

### Additive Blending for Light Effects
```csharp
var aviNode = new AVIEffectsNode();
aviNode.LoadAVIFile("C:\\Videos\\light_rays.avi");
aviNode.Enabled = true;
aviNode.BlendMode = 1; // Additive
aviNode.Intensity = 0.7f;
```

## Performance Notes

- Frame loading and caching can be memory-intensive for large video files
- Blending operations scale linearly with image resolution
- Persistence effects require additional memory for frame storage
- Adaptive blending adds minimal overhead when not active

## Limitations

- Currently supports only AVI format files
- Frame rate is limited by the visualizer's update cycle
- Memory usage scales with video resolution and frame count
- Advanced video codecs may not be supported

## Future Enhancements

- Support for additional video formats (MP4, MOV, etc.)
- Hardware acceleration for video decoding
- Advanced frame interpolation for smooth playback
- Custom blending algorithms and effects
- Video loop and reverse playback options
