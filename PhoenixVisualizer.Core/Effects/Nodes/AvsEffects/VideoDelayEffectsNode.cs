using System;
using System.Collections.Generic;
using System.Numerics;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Effects;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

/// <summary>
/// Video Delay Effects Node - Real-time audio visualization with video delay and echo effects
/// Based on VIS_AVS video delay implementations
/// </summary>
public class VideoDelayEffectsNode : BaseEffectNode
{
    // Configuration
    public bool Enabled { get; set; } = true;
    public OscilloscopeChannel Channel { get; set; } = OscilloscopeChannel.Center;
    public OscilloscopePosition Position { get; set; } = OscilloscopePosition.Center;
    public int Size { get; set; } = 100;
    public AudioSourceType SourceType { get; set; } = AudioSourceType.Oscilloscope;
    
    // Delay configuration
    public int DelayFrames { get; set; } = 10; // Number of frames to delay
    public float DelayOpacity { get; set; } = 0.7f; // Opacity of delayed frames
    public float DelayScale { get; set; } = 0.9f; // Scale of delayed frames
    public float DelayRotation { get; set; } = 0.0f; // Rotation of delayed frames
    public bool BeatReactive { get; set; } = true; // Enable beat reactivity
    public bool AudioReactive { get; set; } = true; // Enable audio reactivity
    public DelayMode Mode { get; set; } = DelayMode.Echo; // Delay effect mode
    
    // Colors
    public Color[] DelayColors { get; set; } = new Color[8];
    public int ColorCount { get; set; } = 8;
    public Color BackgroundColor { get; set; } = Color.Transparent;
    
    // Audio processing
    private float[] audioBuffer = new float[576];
    private float[] previousAudioBuffer = new float[576];
    private readonly int audioBufferSize = 576; // Standard AVS audio buffer size
    
    // Delay state
    private Vector2 delayCenter;
    private float currentBeatIntensity;
    private float currentAudioIntensity;
    private float delayScale;
    private Queue<ImageBuffer> frameBuffer;
    private int currentFrame;
    
    // Performance tracking
    private float frameTime;
    private int frameCount;

    public VideoDelayEffectsNode()
    {
        // Initialize delay state
        currentBeatIntensity = 0.0f;
        currentAudioIntensity = 0.0f;
        delayScale = 1.0f;
        frameBuffer = new Queue<ImageBuffer>();
        currentFrame = 0;
        
        // Initialize timing
        frameTime = 0;
        frameCount = 0;
        
        // Initialize color palette
        InitializeColorPalette();
    }

    protected override void ProcessCore(ImageBuffer input, ImageBuffer output, AudioFeatures? audioFeatures)
    {
        if (!Enabled) return;
        
        // Update frame timing
        frameTime += 1.0f / 60.0f; // Assume 60fps
        frameCount++;
        
        // Store current frame in buffer
        StoreFrame(input);
        
        // Copy input to output
        input.CopyTo(output);
        
        // Update delay center position
        UpdateDelayPosition(output.Width, output.Height);
        
        // Process audio data
        ProcessAudioData(audioFeatures);
        
        // Update delay parameters
        UpdateDelayParameters();
        
        // Render video delay effects
        RenderVideoDelayEffects(output);
    }

    private void StoreFrame(ImageBuffer frame)
    {
        // Create a copy of the frame
        var frameCopy = new ImageBuffer(frame.Width, frame.Height);
        frame.CopyTo(frameCopy);
        
        // Add to buffer
        frameBuffer.Enqueue(frameCopy);
        
        // Maintain buffer size
        while (frameBuffer.Count > DelayFrames)
        {
            var oldFrame = frameBuffer.Dequeue();
            oldFrame.Dispose();
        }
        
        currentFrame++;
    }

    private void UpdateDelayPosition(int width, int height)
    {
        // Calculate delay center based on position setting
        switch (Position)
        {
            case OscilloscopePosition.Top:
                delayCenter = new Vector2(width / 2.0f, Size / 2.0f + 20);
                break;
            case OscilloscopePosition.Bottom:
                delayCenter = new Vector2(width / 2.0f, height - Size / 2.0f - 20);
                break;
            case OscilloscopePosition.Center:
            default:
                delayCenter = new Vector2(width / 2.0f, height / 2.0f);
                break;
        }
        
        // Apply delay scale
        delayScale = Size / 100.0f;
    }

    private void ProcessAudioData(AudioFeatures? audioFeatures)
    {
        if (audioFeatures == null) return;
        
        // Store previous audio buffer
        Array.Copy(audioBuffer, previousAudioBuffer, audioBufferSize);
        
        // Get audio data based on source type and channel
        float[] sourceData = GetAudioSourceData(audioFeatures);
        
        // Apply audio processing (smoothing, normalization)
        ProcessAudioBuffer(sourceData);
        
        // Update beat and audio intensity
        UpdateIntensities(audioFeatures);
    }

    private float[] GetAudioSourceData(AudioFeatures audioFeatures)
    {
        float[] sourceData = new float[audioBufferSize];
        
        if (SourceType == AudioSourceType.Oscilloscope)
        {
            // Get waveform data
            var waveform = audioFeatures.Waveform ?? Array.Empty<float>();
            var spectrum = audioFeatures.Fft ?? Array.Empty<float>();
            
            // Use waveform if available, otherwise use spectrum
            var leftData = waveform.Length > 0 ? waveform : spectrum;
            var rightData = leftData; // Mono for now
            
            // Select channel data
            switch (Channel)
            {
                case OscilloscopeChannel.Left:
                    sourceData = leftData;
                    break;
                case OscilloscopeChannel.Right:
                    sourceData = rightData;
                    break;
                case OscilloscopeChannel.Center:
                default:
                    // Mix left and right channels
                    for (int i = 0; i < Math.Min(audioBufferSize, leftData.Length); i++)
                    {
                        sourceData[i] = (leftData[i] + rightData[i]) * 0.5f;
                    }
                    break;
            }
        }
        else // Spectrum
        {
            // Get spectrum data
            var spectrum = audioFeatures.Fft ?? Array.Empty<float>();
            
            // Use spectrum data
            for (int i = 0; i < Math.Min(audioBufferSize, spectrum.Length); i++)
            {
                sourceData[i] = spectrum[i];
            }
        }
        
        return sourceData;
    }

    private void ProcessAudioBuffer(float[] sourceData)
    {
        // Apply smoothing and normalization
        for (int i = 0; i < audioBufferSize; i++)
        {
            if (i < sourceData.Length)
            {
                // Smooth with previous frame
                float smoothing = 0.7f;
                audioBuffer[i] = sourceData[i] * (1.0f - smoothing) + previousAudioBuffer[i] * smoothing;
                
                // Normalize to 0-1 range
                audioBuffer[i] = Math.Clamp(audioBuffer[i], -1.0f, 1.0f);
            }
            else
            {
                audioBuffer[i] = 0.0f;
            }
        }
    }

    private void UpdateIntensities(AudioFeatures audioFeatures)
    {
        // Calculate audio intensity
        if (AudioReactive)
        {
            float totalIntensity = 0.0f;
            int sampleCount = 0;
            
            for (int i = 0; i < audioBufferSize; i++)
            {
                if (i < audioBuffer.Length)
                {
                    totalIntensity += MathF.Abs(audioBuffer[i]);
                    sampleCount++;
                }
            }
            
            if (sampleCount > 0)
            {
                float avgIntensity = totalIntensity / sampleCount;
                
                // Smooth audio intensity
                float smoothing = 0.8f;
                currentAudioIntensity = avgIntensity * (1.0f - smoothing) + currentAudioIntensity * smoothing;
            }
        }
        
        // Update beat intensity
        if (BeatReactive)
        {
            if (audioFeatures.Beat)
            {
                currentBeatIntensity = Math.Min(1.0f, currentBeatIntensity + 0.3f);
            }
            else
            {
                // Decay beat intensity
                currentBeatIntensity *= 0.95f;
            }
        }
    }

    private void UpdateDelayParameters()
    {
        // Update delay parameters based on audio
        if (BeatReactive)
        {
            DelayOpacity = 0.7f + currentBeatIntensity * 0.3f;
            DelayScale = 0.9f + currentBeatIntensity * 0.1f;
        }
        
        if (AudioReactive)
        {
            DelayRotation += currentAudioIntensity * 0.1f;
            if (DelayRotation > 2.0f * MathF.PI)
            {
                DelayRotation -= 2.0f * MathF.PI;
            }
        }
    }

    private void RenderVideoDelayEffects(ImageBuffer output)
    {
        if (frameBuffer.Count == 0) return;
        
        // Calculate delay bounds
        float halfSize = Size * delayScale / 2.0f;
        
        Vector2 topLeft = new Vector2(delayCenter.X - halfSize, delayCenter.Y - halfSize);
        Vector2 bottomRight = new Vector2(delayCenter.X + halfSize, delayCenter.Y + halfSize);
        
        // Draw background if specified
        if (BackgroundColor.A > 0)
        {
            DrawRectangle(output, topLeft, bottomRight, BackgroundColor);
        }
        
        // Render delayed frames based on mode
        switch (Mode)
        {
            case DelayMode.Echo:
                RenderEchoMode(output, topLeft, bottomRight);
                break;
            case DelayMode.Trail:
                RenderTrailMode(output, topLeft, bottomRight);
                break;
            case DelayMode.Multiply:
                RenderMultiplyMode(output, topLeft, bottomRight);
                break;
            case DelayMode.Overlay:
                RenderOverlayMode(output, topLeft, bottomRight);
                break;
        }
    }

    private void RenderEchoMode(ImageBuffer output, Vector2 topLeft, Vector2 bottomRight)
    {
        // Render delayed frames as echoes
        var frames = frameBuffer.ToArray();
        for (int i = 0; i < frames.Length; i++)
        {
            var frame = frames[i];
            float delayOpacity = DelayOpacity * (1.0f - (float)i / frames.Length);
            float delayScale = DelayScale * (1.0f - (float)i / frames.Length * 0.5f);
            
            // Calculate offset based on frame age
            float offsetX = (i * 5.0f) * (1.0f + currentBeatIntensity);
            float offsetY = (i * 3.0f) * (1.0f + currentAudioIntensity);
            
            // Render delayed frame
            RenderDelayedFrame(output, frame, topLeft, bottomRight, delayOpacity, delayScale, offsetX, offsetY);
        }
    }

    private void RenderTrailMode(ImageBuffer output, Vector2 topLeft, Vector2 bottomRight)
    {
        // Render delayed frames as trails
        var frames = frameBuffer.ToArray();
        for (int i = 0; i < frames.Length; i++)
        {
            var frame = frames[i];
            float delayOpacity = DelayOpacity * (0.5f - (float)i / frames.Length * 0.4f);
            float delayScale = 1.0f - (float)i / frames.Length * 0.2f;
            
            // Calculate trail offset
            float trailOffset = i * 2.0f;
            float offsetX = trailOffset * MathF.Cos(DelayRotation);
            float offsetY = trailOffset * MathF.Sin(DelayRotation);
            
            // Render delayed frame
            RenderDelayedFrame(output, frame, topLeft, bottomRight, delayOpacity, delayScale, offsetX, offsetY);
        }
    }

    private void RenderMultiplyMode(ImageBuffer output, Vector2 topLeft, Vector2 bottomRight)
    {
        // Render delayed frames as multiplied layers
        var frames = frameBuffer.ToArray();
        for (int i = 0; i < frames.Length; i++)
        {
            var frame = frames[i];
            float delayOpacity = DelayOpacity * 0.3f;
            float delayScale = DelayScale * (0.8f + (float)i / frames.Length * 0.4f);
            
            // Calculate rotation offset
            float rotationOffset = DelayRotation + (i * 0.1f);
            float offsetX = MathF.Sin(rotationOffset) * 20.0f;
            float offsetY = MathF.Cos(rotationOffset) * 20.0f;
            
            // Render delayed frame
            RenderDelayedFrame(output, frame, topLeft, bottomRight, delayOpacity, delayScale, offsetX, offsetY);
        }
    }

    private void RenderOverlayMode(ImageBuffer output, Vector2 topLeft, Vector2 bottomRight)
    {
        // Render delayed frames as overlays
        var frames = frameBuffer.ToArray();
        for (int i = 0; i < frames.Length; i++)
        {
            var frame = frames[i];
            float delayOpacity = DelayOpacity * (0.2f + (float)i / frames.Length * 0.3f);
            float delayScale = 1.0f;
            
            // Calculate overlay offset
            float overlayOffset = i * 10.0f;
            float offsetX = overlayOffset * (1.0f + currentBeatIntensity);
            float offsetY = overlayOffset * (1.0f + currentAudioIntensity);
            
            // Render delayed frame
            RenderDelayedFrame(output, frame, topLeft, bottomRight, delayOpacity, delayScale, offsetX, offsetY);
        }
    }

    private void RenderDelayedFrame(ImageBuffer output, ImageBuffer delayedFrame, Vector2 topLeft, Vector2 bottomRight, float opacity, float scale, float offsetX, float offsetY)
    {
        try
        {
            // Calculate scaled bounds
            float width = (bottomRight.X - topLeft.X) * scale;
            float height = (bottomRight.Y - topLeft.Y) * scale;
            
            Vector2 scaledTopLeft = new Vector2(
                topLeft.X + offsetX - (width - (bottomRight.X - topLeft.X)) / 2.0f,
                topLeft.Y + offsetY - (height - (bottomRight.Y - topLeft.Y)) / 2.0f
            );
            
            Vector2 scaledBottomRight = new Vector2(
                scaledTopLeft.X + width,
                scaledTopLeft.Y + height
            );
            
            // Render delayed frame with opacity
            RenderFrameWithOpacity(output, delayedFrame, scaledTopLeft, scaledBottomRight, opacity);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VideoDelay] Error rendering delayed frame: {ex.Message}");
        }
    }

    private void RenderFrameWithOpacity(ImageBuffer output, ImageBuffer sourceFrame, Vector2 topLeft, Vector2 bottomRight, float opacity)
    {
        try
        {
            int startX = Math.Max(0, (int)topLeft.X);
            int startY = Math.Max(0, (int)topLeft.Y);
            int endX = Math.Min(output.Width, (int)bottomRight.X);
            int endY = Math.Min(output.Height, (int)bottomRight.Y);
            
            int sourceStartX = Math.Max(0, (int)(-topLeft.X));
            int sourceStartY = Math.Max(0, (int)(-topLeft.Y));
            
            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    int sourceX = sourceStartX + (x - startX);
                    int sourceY = sourceStartY + (y - startY);
                    
                    if (sourceX < sourceFrame.Width && sourceY < sourceFrame.Height)
                    {
                        var sourceColor = sourceFrame.GetPixel(sourceX, sourceY);
                        var currentColor = output.GetPixel(x, y);
                        
                        // Blend colors with opacity
                        var blendedColor = BlendColors(currentColor, sourceColor, opacity);
                        output.SetPixel(x, y, blendedColor);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VideoDelay] Error rendering frame with opacity: {ex.Message}");
        }
    }

    private Color BlendColors(Color baseColor, Color overlayColor, float opacity)
    {
        return new Color(
            (byte)(baseColor.R * (1.0f - opacity) + overlayColor.R * opacity),
            (byte)(baseColor.G * (1.0f - opacity) + overlayColor.G * opacity),
            (byte)(baseColor.B * (1.0f - opacity) + overlayColor.B * opacity),
            (byte)(baseColor.A * (1.0f - opacity) + overlayColor.A * opacity)
        );
    }

    private void DrawRectangle(ImageBuffer output, Vector2 topLeft, Vector2 bottomRight, Color color)
    {
        int x1 = (int)topLeft.X;
        int y1 = (int)topLeft.Y;
        int x2 = (int)bottomRight.X;
        int y2 = (int)bottomRight.Y;
        
        // Ensure coordinates are within bounds
        x1 = Math.Max(0, Math.Min(x1, output.Width - 1));
        y1 = Math.Max(0, Math.Min(y1, output.Height - 1));
        x2 = Math.Max(0, Math.Min(x2, output.Width - 1));
        y2 = Math.Max(0, Math.Min(y2, output.Height - 1));
        
        // Draw filled rectangle
        for (int y = y1; y <= y2; y++)
        {
            for (int x = x1; x <= x2; x++)
            {
                output.SetPixel(x, y, color);
            }
        }
    }

    private void InitializeColorPalette()
    {
        // Initialize with a vibrant color palette
        DelayColors[0] = Color.Red;
        DelayColors[1] = Color.Orange;
        DelayColors[2] = Color.Yellow;
        DelayColors[3] = Color.Green;
        DelayColors[4] = Color.Cyan;
        DelayColors[5] = Color.Blue;
        DelayColors[6] = Color.Magenta;
        DelayColors[7] = Color.Pink;
    }

    public override void Dispose()
    {
        // Clean up resources
        audioBuffer = null!;
        previousAudioBuffer = null!;
        DelayColors = null!;
        
        // Dispose frame buffer
        foreach (var frame in frameBuffer)
        {
            frame.Dispose();
        }
        frameBuffer.Clear();
        
        base.Dispose();
    }
}

/// <summary>
/// Video delay effect modes
/// </summary>
public enum DelayMode
{
    Echo,       // Echo effect with fading
    Trail,      // Trail effect with rotation
    Multiply,   // Multiply effect with scaling
    Overlay     // Overlay effect with blending
}