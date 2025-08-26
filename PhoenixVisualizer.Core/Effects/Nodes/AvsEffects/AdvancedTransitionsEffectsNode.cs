using System;
using System.Numerics;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Effects;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

/// <summary>
/// Advanced Transitions Effects Node - Real-time audio visualization with advanced transition effects
/// Based on VIS_AVS advanced transitions implementations
/// </summary>
public class AdvancedTransitionsEffectsNode : BaseEffectNode
{
    // Configuration
    public bool Enabled { get; set; } = true;
    public OscilloscopeChannel Channel { get; set; } = OscilloscopeChannel.Center;
    public OscilloscopePosition Position { get; set; } = OscilloscopePosition.Center;
    public int Size { get; set; } = 100;
    public AudioSourceType SourceType { get; set; } = AudioSourceType.Oscilloscope;
    
    // Transition configuration
    public TransitionType TransitionType { get; set; } = TransitionType.Fade;
    public float TransitionSpeed { get; set; } = 1.0f;
    public float TransitionProgress { get; set; } = 0.0f;
    public bool BeatReactive { get; set; } = true;
    public bool AudioReactive { get; set; } = true;
    public int TransitionSteps { get; set; } = 60;
    
    // Colors
    public Color[] TransitionColors { get; set; } = new Color[8];
    public int ColorCount { get; set; } = 8;
    public Color BackgroundColor { get; set; } = Color.Transparent;
    
    // Audio processing
    private float[] audioBuffer = new float[576];
    private float[] previousAudioBuffer = new float[576];
    private readonly int audioBufferSize = 576; // Standard AVS audio buffer size
    
    // Transition state
    private Vector2 transitionCenter;
    private float currentBeatIntensity;
    private float currentAudioIntensity;
    private float transitionScale;
    private float transitionDirection = 1.0f;
    
    // Performance tracking
    private float frameTime;
    private int frameCount;

    public AdvancedTransitionsEffectsNode()
    {
        // Initialize transition state
        currentBeatIntensity = 0.0f;
        currentAudioIntensity = 0.0f;
        transitionScale = 1.0f;
        
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
        
        // Copy input to output
        input.CopyTo(output);
        
        // Update transition center position
        UpdateTransitionPosition(output.Width, output.Height);
        
        // Process audio data
        ProcessAudioData(audioFeatures);
        
        // Update transition progress
        UpdateTransitionProgress();
        
        // Render advanced transitions
        RenderAdvancedTransitions(output);
    }

    private void UpdateTransitionPosition(int width, int height)
    {
        // Calculate transition center based on position setting
        switch (Position)
        {
            case OscilloscopePosition.Top:
                transitionCenter = new Vector2(width / 2.0f, Size / 2.0f + 20);
                break;
            case OscilloscopePosition.Bottom:
                transitionCenter = new Vector2(width / 2.0f, height - Size / 2.0f - 20);
                break;
            case OscilloscopePosition.Center:
            default:
                transitionCenter = new Vector2(width / 2.0f, height / 2.0f);
                break;
        }
        
        // Apply transition scale
        transitionScale = Size / 100.0f;
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

    private void UpdateTransitionProgress()
    {
        // Calculate effective transition speed
        float effectiveSpeed = TransitionSpeed;
        if (BeatReactive)
        {
            effectiveSpeed += currentBeatIntensity * 2.0f;
        }
        if (AudioReactive)
        {
            effectiveSpeed += currentAudioIntensity * 1.0f;
        }
        
        // Update transition progress
        TransitionProgress += effectiveSpeed * frameTime / TransitionSteps;
        
        // Handle transition completion and reversal
        if (TransitionProgress >= 1.0f)
        {
            TransitionProgress = 1.0f;
            transitionDirection = -1.0f;
        }
        else if (TransitionProgress <= 0.0f)
        {
            TransitionProgress = 0.0f;
            transitionDirection = 1.0f;
        }
    }

    private void RenderAdvancedTransitions(ImageBuffer output)
    {
        // Calculate transition bounds
        float halfSize = Size * transitionScale / 2.0f;
        
        Vector2 topLeft = new Vector2(transitionCenter.X - halfSize, transitionCenter.Y - halfSize);
        Vector2 bottomRight = new Vector2(transitionCenter.X + halfSize, transitionCenter.Y + halfSize);
        
        // Draw background if specified
        if (BackgroundColor.A > 0)
        {
            DrawRectangle(output, topLeft, bottomRight, BackgroundColor);
        }
        
        // Render transition based on type
        switch (TransitionType)
        {
            case TransitionType.Fade:
                RenderFadeTransition(output, topLeft, bottomRight);
                break;
            case TransitionType.Slide:
                RenderSlideTransition(output, topLeft, bottomRight);
                break;
            case TransitionType.Zoom:
                RenderZoomTransition(output, topLeft, bottomRight);
                break;
            case TransitionType.Rotate:
                RenderRotateTransition(output, topLeft, bottomRight);
                break;
            case TransitionType.Wipe:
                RenderWipeTransition(output, topLeft, bottomRight);
                break;
            case TransitionType.Dissolve:
                RenderDissolveTransition(output, topLeft, bottomRight);
                break;
        }
    }

    private void RenderFadeTransition(ImageBuffer output, Vector2 topLeft, Vector2 bottomRight)
    {
        // Calculate fade alpha based on transition progress
        float alpha = TransitionProgress;
        
        // Create fade color
        Color fadeColor = new Color(
            (byte)(255 * alpha),
            (byte)(255 * alpha),
            (byte)(255 * alpha),
            (byte)(255 * alpha)
        );
        
        // Draw fade overlay
        DrawRectangle(output, topLeft, bottomRight, fadeColor);
    }

    private void RenderSlideTransition(ImageBuffer output, Vector2 topLeft, Vector2 bottomRight)
    {
        // Calculate slide offset
        float slideOffset = TransitionProgress * (bottomRight.X - topLeft.X);
        
        // Create sliding rectangle
        Vector2 slideTopLeft = new Vector2(topLeft.X + slideOffset, topLeft.Y);
        Vector2 slideBottomRight = new Vector2(bottomRight.X + slideOffset, bottomRight.Y);
        
        // Draw sliding element
        Color slideColor = GetTransitionColor(0);
        DrawRectangle(output, slideTopLeft, slideBottomRight, slideColor);
    }

    private void RenderZoomTransition(ImageBuffer output, Vector2 topLeft, Vector2 bottomRight)
    {
        // Calculate zoom scale
        float zoomScale = 0.1f + TransitionProgress * 0.9f;
        
        // Calculate zoomed bounds
        float centerX = (topLeft.X + bottomRight.X) / 2.0f;
        float centerY = (topLeft.Y + bottomRight.Y) / 2.0f;
        float width = (bottomRight.X - topLeft.X) * zoomScale;
        float height = (bottomRight.Y - topLeft.Y) * zoomScale;
        
        Vector2 zoomTopLeft = new Vector2(centerX - width / 2.0f, centerY - height / 2.0f);
        Vector2 zoomBottomRight = new Vector2(centerX + width / 2.0f, centerY + height / 2.0f);
        
        // Draw zoomed element
        Color zoomColor = GetTransitionColor(1);
        DrawRectangle(output, zoomTopLeft, zoomBottomRight, zoomColor);
    }

    private void RenderRotateTransition(ImageBuffer output, Vector2 topLeft, Vector2 bottomRight)
    {
        // Calculate rotation angle
        float rotationAngle = TransitionProgress * 2.0f * MathF.PI;
        
        // Calculate center
        float centerX = (topLeft.X + bottomRight.X) / 2.0f;
        float centerY = (topLeft.Y + bottomRight.Y) / 2.0f;
        
        // Draw rotating elements
        for (int i = 0; i < 8; i++)
        {
            float angle = rotationAngle + (i * MathF.PI / 4.0f);
            float radius = Size * transitionScale * 0.3f;
            
            float x = centerX + radius * MathF.Cos(angle);
            float y = centerY + radius * MathF.Sin(angle);
            
            Color elementColor = GetTransitionColor(i);
            DrawCircle(output, new Vector2(x, y), 10, elementColor);
        }
    }

    private void RenderWipeTransition(ImageBuffer output, Vector2 topLeft, Vector2 bottomRight)
    {
        // Calculate wipe position
        float wipePosition = TransitionProgress * (bottomRight.X - topLeft.X);
        
        // Draw wipe line
        Vector2 wipeStart = new Vector2(topLeft.X + wipePosition, topLeft.Y);
        Vector2 wipeEnd = new Vector2(topLeft.X + wipePosition, bottomRight.Y);
        
        Color wipeColor = GetTransitionColor(2);
        DrawLine(output, wipeStart, wipeEnd, wipeColor, 5);
        
        // Fill wiped area
        Vector2 wipeAreaTopLeft = new Vector2(topLeft.X, topLeft.Y);
        Vector2 wipeAreaBottomRight = new Vector2(topLeft.X + wipePosition, bottomRight.Y);
        
        DrawRectangle(output, wipeAreaTopLeft, wipeAreaBottomRight, wipeColor);
    }

    private void RenderDissolveTransition(ImageBuffer output, Vector2 topLeft, Vector2 bottomRight)
    {
        // Create dissolve pattern based on transition progress
        Random random = new Random(42); // Fixed seed for consistent pattern
        
        for (int y = (int)topLeft.Y; y <= (int)bottomRight.Y; y += 4)
        {
            for (int x = (int)topLeft.X; x <= (int)bottomRight.X; x += 4)
            {
                if (random.NextDouble() < TransitionProgress)
                {
                    Color dissolveColor = GetTransitionColor((x + y) % ColorCount);
                    DrawCircle(output, new Vector2(x, y), 2, dissolveColor);
                }
            }
        }
    }

    private Color GetTransitionColor(int index)
    {
        return TransitionColors[index % ColorCount];
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

    private void DrawCircle(ImageBuffer output, Vector2 center, float radius, Color color)
    {
        int radiusInt = (int)radius;
        
        for (int y = -radiusInt; y <= radiusInt; y++)
        {
            for (int x = -radiusInt; x <= radiusInt; x++)
            {
                float distance = MathF.Sqrt(x * x + y * y);
                if (distance <= radius)
                {
                    int drawX = (int)center.X + x;
                    int drawY = (int)center.Y + y;
                    
                    if (drawX >= 0 && drawX < output.Width && drawY >= 0 && drawY < output.Height)
                    {
                        output.SetPixel(drawX, drawY, color);
                    }
                }
            }
        }
    }

    private void DrawLine(ImageBuffer output, Vector2 start, Vector2 end, Color color, int thickness)
    {
        // Bresenham's line algorithm for efficient line drawing
        int x0 = (int)start.X;
        int y0 = (int)start.Y;
        int x1 = (int)end.X;
        int y1 = (int)end.Y;
        
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        
        while (true)
        {
            // Draw pixel with thickness
            for (int tx = -thickness / 2; tx <= thickness / 2; tx++)
            {
                for (int ty = -thickness / 2; ty <= thickness / 2; ty++)
                {
                    int drawX = x0 + tx;
                    int drawY = y0 + ty;
                    
                    if (drawX >= 0 && drawX < output.Width && drawY >= 0 && drawY < output.Height)
                    {
                        output.SetPixel(drawX, drawY, color);
                    }
                }
            }
            
            if (x0 == x1 && y0 == y1) break;
            
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    private void InitializeColorPalette()
    {
        // Initialize with a vibrant color palette
        TransitionColors[0] = Color.Red;
        TransitionColors[1] = Color.Orange;
        TransitionColors[2] = Color.Yellow;
        TransitionColors[3] = Color.Green;
        TransitionColors[4] = Color.Cyan;
        TransitionColors[5] = Color.Blue;
        TransitionColors[6] = Color.Magenta;
        TransitionColors[7] = Color.Pink;
    }

    public override void Dispose()
    {
        // Clean up resources
        audioBuffer = null!;
        previousAudioBuffer = null!;
        TransitionColors = null!;
        base.Dispose();
    }
}

/// <summary>
/// Advanced transition types
/// </summary>
public enum TransitionType
{
    Fade,       // Fade in/out
    Slide,      // Slide in/out
    Zoom,       // Zoom in/out
    Rotate,     // Rotate in/out
    Wipe,       // Wipe across screen
    Dissolve    // Dissolve pattern
}