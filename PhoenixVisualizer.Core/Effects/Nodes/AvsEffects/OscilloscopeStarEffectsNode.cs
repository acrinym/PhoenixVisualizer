using System;
using System.Numerics;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Effects;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

/// <summary>
/// Oscilloscope Star Effect - Real-time audio visualization with star-shaped oscilloscope patterns
/// Based on VIS_AVS r_oscstar.cpp implementation
/// </summary>
public class OscilloscopeStarEffectsNode : BaseEffectNode
{
    // Configuration
    public bool Enabled { get; set; } = true;
    public OscilloscopeChannel Channel { get; set; } = OscilloscopeChannel.Center;
    public OscilloscopePosition Position { get; set; } = OscilloscopePosition.Center;
    public int Size { get; set; } = 100;
    public AudioSourceType SourceType { get; set; } = AudioSourceType.Oscilloscope;
    
    // Star configuration
    public int StarPoints { get; set; } = 5; // 5-pointed star
    public float StarRotation { get; set; } = 0.0f;
    public float StarRotationSpeed { get; set; } = 0.0f;
    public float StarThickness { get; set; } = 2.0f;
    
    // Colors
    public Color[] ColorPalette { get; set; } = new Color[16];
    public int ColorCount { get; set; } = 16;
    
    // Audio processing
    private float[] audioBuffer = new float[576];
    private float[] previousAudioBuffer = new float[576];
    private readonly int audioBufferSize = 576; // Standard AVS audio buffer size
    
    // Star state
    private Vector2 starCenter;
    private float currentRotation;
    private float starScale;
    
    // Performance tracking
    private float frameTime;
    private int frameCount;

    public OscilloscopeStarEffectsNode()
    {
        // Initialize star state
        currentRotation = 0.0f;
        starScale = 1.0f;
        
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
        
        // Update star center position
        UpdateStarPosition(output.Width, output.Height);
        
        // Process audio data
        ProcessAudioData(audioFeatures);
        
        // Update star rotation
        UpdateStarRotation();
        
        // Render oscilloscope star
        RenderOscilloscopeStar(output);
    }

    private void UpdateStarPosition(int width, int height)
    {
        // Calculate star center based on position setting
        switch (Position)
        {
            case OscilloscopePosition.Top:
                starCenter = new Vector2(width / 2.0f, Size / 2.0f + 20);
                break;
            case OscilloscopePosition.Bottom:
                starCenter = new Vector2(width / 2.0f, height - Size / 2.0f - 20);
                break;
            case OscilloscopePosition.Center:
            default:
                starCenter = new Vector2(width / 2.0f, height / 2.0f);
                break;
        }
        
        // Apply star scale
        starScale = Size / 100.0f;
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

    private void UpdateStarRotation()
    {
        // Update rotation based on speed
        currentRotation += StarRotationSpeed * frameTime;
        if (currentRotation > 2.0f * MathF.PI)
        {
            currentRotation -= 2.0f * MathF.PI;
        }
    }

    private void RenderOscilloscopeStar(ImageBuffer output)
    {
        // Calculate star parameters
        float baseRadius = Size * starScale;
        float maxAmplitude = Size * 0.3f; // Maximum star expansion
        
        // Calculate star points
        Vector2[] starPoints = CalculateStarPoints(baseRadius);
        
        // Render star arms
        for (int i = 0; i < StarPoints; i++)
        {
            // Calculate start and end points for this arm
            Vector2 startPoint = starCenter;
            Vector2 endPoint = starPoints[i];
            
            // Get audio amplitude for this arm
            int audioIndex = (i * audioBufferSize) / StarPoints;
            float amplitude = 0.0f;
            if (audioIndex < audioBufferSize)
            {
                amplitude = MathF.Abs(audioBuffer[audioIndex]) * maxAmplitude;
            }
            
            // Expand end point based on audio amplitude
            Vector2 direction = Vector2.Normalize(endPoint - startPoint);
            Vector2 expandedEndPoint = endPoint + direction * amplitude;
            
            // Get color for this arm
            Color armColor = GetArmColor(i, amplitude);
            
            // Draw star arm
            DrawStarArm(output, startPoint, expandedEndPoint, armColor);
        }
        
        // Render star outline
        RenderStarOutline(output, starPoints, maxAmplitude);
    }

    private Vector2[] CalculateStarPoints(float baseRadius)
    {
        Vector2[] points = new Vector2[StarPoints];
        
        for (int i = 0; i < StarPoints; i++)
        {
            // Calculate angle for this point
            float angle = (2.0f * MathF.PI * i / StarPoints) + currentRotation + StarRotation;
            
            // Calculate point position
            points[i] = new Vector2(
                starCenter.X + baseRadius * MathF.Cos(angle),
                starCenter.Y + baseRadius * MathF.Sin(angle)
            );
        }
        
        return points;
    }

    private void RenderStarOutline(ImageBuffer output, Vector2[] basePoints, float maxAmplitude)
    {
        // Get average audio amplitude for outline
        float avgAmplitude = 0.0f;
        for (int i = 0; i < audioBufferSize; i++)
        {
            avgAmplitude += MathF.Abs(audioBuffer[i]);
        }
        avgAmplitude = (avgAmplitude / audioBufferSize) * maxAmplitude;
        
        // Calculate expanded outline points
        Vector2[] outlinePoints = new Vector2[StarPoints];
        for (int i = 0; i < StarPoints; i++)
        {
            Vector2 direction = Vector2.Normalize(basePoints[i] - starCenter);
            outlinePoints[i] = starCenter + direction * (Size * starScale + avgAmplitude);
        }
        
        // Draw outline
        Color outlineColor = GetOutlineColor(avgAmplitude);
        for (int i = 0; i < StarPoints; i++)
        {
            int nextIndex = (i + 1) % StarPoints;
            DrawLine(output, outlinePoints[i], outlinePoints[nextIndex], outlineColor, (int)StarThickness);
        }
    }

    private void DrawStarArm(ImageBuffer output, Vector2 start, Vector2 end, Color color)
    {
        // Draw the star arm
        DrawLine(output, start, end, color, (int)StarThickness);
        
        // Add glow effect
        Color glowColor = new Color(
            (byte)(color.R / 2),
            (byte)(color.G / 2),
            (byte)(color.B / 2),
            (byte)(color.A / 2)
        );
        DrawLine(output, start, end, glowColor, (int)(StarThickness * 2));
    }

    private Color GetArmColor(int armIndex, float amplitude)
    {
        // Calculate color based on arm index and amplitude
        float normalizedIndex = (float)armIndex / StarPoints;
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

    private Color GetOutlineColor(float amplitude)
    {
        // Calculate outline color based on amplitude
        float normalizedAmplitude = Math.Clamp(amplitude / (Size * 0.3f), 0.0f, 1.0f);
        
        // Use white with amplitude-based alpha
        byte alpha = (byte)(128 + normalizedAmplitude * 127);
        return new Color(255, 255, 255, alpha);
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