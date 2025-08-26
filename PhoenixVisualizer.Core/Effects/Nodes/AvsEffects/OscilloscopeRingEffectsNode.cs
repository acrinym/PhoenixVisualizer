using System;
using System.Numerics;
using System.Threading.Tasks;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Effects;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

/// <summary>
/// Oscilloscope Ring Effect - Real-time audio visualization with circular oscilloscope patterns
/// Based on VIS_AVS r_oscring.cpp implementation
/// </summary>
public class OscilloscopeRingEffectsNode : BaseEffectNode
{
    // Configuration
    public bool Enabled { get; set; } = true;
    public OscilloscopeChannel Channel { get; set; } = OscilloscopeChannel.Center;
    public OscilloscopePosition Position { get; set; } = OscilloscopePosition.Center;
    public int Size { get; set; } = 100;
    public AudioSourceType SourceType { get; set; } = AudioSourceType.Oscilloscope;
    
    // Colors
    public Color[] ColorPalette { get; set; } = new Color[16];
    public int ColorCount { get; set; } = 16;
    
    // Ring configuration
    public int RingSegments { get; set; } = 80;
    public float RingThickness { get; set; } = 2.0f;
    public bool SmoothInterpolation { get; set; } = true;
    public float RotationSpeed { get; set; } = 0.0f;
    
    // Audio processing
    private float[] audioBuffer = new float[576];
    private float[] previousAudioBuffer = new float[576];
    private readonly int audioBufferSize = 576; // Standard AVS audio buffer size
    private const float sampleRate = 44100.0f;
    
    // Ring state
    private Vector2 ringCenter;
    private float currentRotation;
    private float ringScale;
    
    // Multi-threading support
    private readonly int threadCount;
    private readonly Task[] processingTasks;
    private readonly object lockObject = new object();
    
    // Performance tracking
    private float frameTime;
    private int frameCount;

    public OscilloscopeRingEffectsNode()
    {
        // Initialize multi-threading
        threadCount = Environment.ProcessorCount;
        processingTasks = new Task[threadCount];
        
        // Initialize ring state
        currentRotation = 0.0f;
        ringScale = 1.0f;
        
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
        
        // Update ring center position
        UpdateRingPosition(output.Width, output.Height);
        
        // Process audio data
        ProcessAudioData(audioFeatures);
        
        // Update ring rotation
        UpdateRingRotation();
        
        // Render oscilloscope ring
        RenderOscilloscopeRing(output);
    }

    private void UpdateRingPosition(int width, int height)
    {
        // Calculate ring center based on position setting
        switch (Position)
        {
            case OscilloscopePosition.Top:
                ringCenter = new Vector2(width / 2.0f, Size / 2.0f + 20);
                break;
            case OscilloscopePosition.Bottom:
                ringCenter = new Vector2(width / 2.0f, height - Size / 2.0f - 20);
                break;
            case OscilloscopePosition.Center:
            default:
                ringCenter = new Vector2(width / 2.0f, height / 2.0f);
                break;
        }
        
        // Apply ring scale
        ringScale = Size / 100.0f;
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

    private void UpdateRingRotation()
    {
        // Update rotation based on speed
        currentRotation += RotationSpeed * frameTime;
        if (currentRotation > 2.0f * MathF.PI)
        {
            currentRotation -= 2.0f * MathF.PI;
        }
    }

    private void RenderOscilloscopeRing(ImageBuffer output)
    {
        // Calculate ring parameters
        float baseRadius = Size * ringScale;
        float maxAmplitude = Size * 0.3f; // Maximum ring expansion
        
        // Render ring segments
        for (int i = 0; i < RingSegments; i++)
        {
            // Calculate angle for this segment
            float angle = (2.0f * MathF.PI * i / RingSegments) + currentRotation;
            
            // Get audio amplitude for this segment
            int audioIndex = (i * audioBufferSize) / RingSegments;
            float amplitude = 0.0f;
            if (audioIndex < audioBufferSize)
            {
                amplitude = MathF.Abs(audioBuffer[audioIndex]) * maxAmplitude;
            }
            
            // Calculate ring radius
            float radius = baseRadius + amplitude;
            
            // Calculate start and end points
            float startAngle = angle;
            float endAngle = angle + (2.0f * MathF.PI / RingSegments);
            
            // Calculate segment points
            Vector2 startPoint = new Vector2(
                ringCenter.X + radius * MathF.Cos(startAngle),
                ringCenter.Y + radius * MathF.Sin(startAngle)
            );
            
            Vector2 endPoint = new Vector2(
                ringCenter.X + radius * MathF.Cos(endAngle),
                ringCenter.Y + radius * MathF.Sin(endAngle)
            );
            
            // Get color for this segment
            Color segmentColor = GetSegmentColor(i, amplitude);
            
            // Draw ring segment
            DrawRingSegment(output, startPoint, endPoint, segmentColor);
        }
    }

    private Color GetSegmentColor(int segmentIndex, float amplitude)
    {
        // Calculate color based on segment index and amplitude
        float normalizedIndex = (float)segmentIndex / RingSegments;
        float normalizedAmplitude = Math.Clamp(amplitude / (Size * 0.3f), 0.0f, 1.0f);
        
        // Interpolate between colors based on amplitude
        int colorIndex = (int)(normalizedIndex * ColorCount) % ColorCount;
        int nextColorIndex = (colorIndex + 1) % ColorCount;
        
        Color currentColor = ColorPalette[colorIndex];
        Color nextColor = ColorPalette[nextColorIndex];
        
        // Interpolate colors
        float interpolation = normalizedAmplitude;
        Color interpolatedColor = Color.Lerp(currentColor, nextColor, interpolation);
        
        // Apply amplitude-based brightness
        float brightness = 0.5f + normalizedAmplitude * 0.5f;
        interpolatedColor = new Color(
            (byte)(interpolatedColor.R * brightness),
            (byte)(interpolatedColor.G * brightness),
            (byte)(interpolatedColor.B * brightness),
            interpolatedColor.A
        );
        
        return interpolatedColor;
    }

    private void DrawRingSegment(ImageBuffer output, Vector2 start, Vector2 end, Color color)
    {
        // Simple line drawing for ring segments
        DrawLine(output, start, end, color, (int)RingThickness);
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
        // Initialize with a rainbow color palette
        for (int i = 0; i < ColorCount; i++)
        {
            float hue = (float)i / ColorCount;
            ColorPalette[i] = HsvToRgb(hue, 1.0f, 1.0f);
        }
    }

    private Color HsvToRgb(float h, float s, float v)
    {
        // Convert HSV to RGB
        int hi = (int)(h * 6) % 6;
        float f = h * 6 - hi;
        float p = v * (1 - s);
        float q = v * (1 - f * s);
        float t = v * (1 - (1 - f) * s);
        
        Vector3 rgb = hi switch
        {
            0 => new Vector3(v, t, p),
            1 => new Vector3(q, v, p),
            2 => new Vector3(p, v, t),
            3 => new Vector3(p, q, v),
            4 => new Vector3(t, p, v),
            _ => new Vector3(v, p, q)
        };
        
        return new Color(
            (byte)(rgb.X * 255),
            (byte)(rgb.Y * 255),
            (byte)(rgb.Z * 255),
            255
        );
    }

    public override void Dispose()
    {
        // Clean up resources
        audioBuffer = null!;
        previousAudioBuffer = null!;
        ColorPalette = null!;
        base.Dispose();
    }
}

/// <summary>
/// Oscilloscope channel selection
/// </summary>
public enum OscilloscopeChannel
{
    Left,
    Right,
    Center
}

/// <summary>
/// Oscilloscope position on screen
/// </summary>
public enum OscilloscopePosition
{
    Top,
    Center,
    Bottom
}

/// <summary>
/// Audio source type for oscilloscope
/// </summary>
public enum AudioSourceType
{
    Oscilloscope, // Waveform data
    Spectrum      // Frequency spectrum data
}