using System;
using System.Numerics;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Effects;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

/// <summary>
/// Onetone Effects Node - Real-time audio visualization with monochrome and single-tone effects
/// Based on VIS_AVS r_onetone.cpp implementation
/// </summary>
public class OnetoneEffectsNode : BaseEffectNode
{
    // Configuration
    public bool Enabled { get; set; } = true;
    public OscilloscopeChannel Channel { get; set; } = OscilloscopeChannel.Center;
    public OscilloscopePosition Position { get; set; } = OscilloscopePosition.Center;
    public int Size { get; set; } = 100;
    public AudioSourceType SourceType { get; set; } = AudioSourceType.Oscilloscope;
    
    // Onetone configuration
    public OnetoneMode Mode { get; set; } = OnetoneMode.Monochrome;
    public Color BaseColor { get; set; } = Color.White;
    public float ColorIntensity { get; set; } = 1.0f;
    public float Contrast { get; set; } = 1.0f;
    public float Brightness { get; set; } = 0.5f;
    public bool BeatReactive { get; set; } = true; // Enable beat reactivity
    public bool AudioReactive { get; set; } = true; // Enable audio reactivity
    
    // Colors
    public Color[] ToneColors { get; set; } = new Color[8];
    public int ColorCount { get; set; } = 8;
    public Color BackgroundColor { get; set; } = Color.Transparent;
    
    // Audio processing
    private float[] audioBuffer = new float[576];
    private float[] previousAudioBuffer = new float[576];
    private readonly int audioBufferSize = 576; // Standard AVS audio buffer size
    
    // Onetone state
    private Vector2 effectCenter;
    private float currentBeatIntensity;
    private float currentAudioIntensity;
    private float effectScale;
    
    // Performance tracking
    private float frameTime;
    private int frameCount;

    public OnetoneEffectsNode()
    {
        // Initialize onetone state
        currentBeatIntensity = 0.0f;
        currentAudioIntensity = 0.0f;
        effectScale = 1.0f;
        
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
        
        // Update effect center position
        UpdateEffectPosition(output.Width, output.Height);
        
        // Process audio data
        ProcessAudioData(audioFeatures);
        
        // Apply onetone effect
        ApplyOnetoneEffect(output);
    }

    private void UpdateEffectPosition(int width, int height)
    {
        // Calculate effect center based on position setting
        switch (Position)
        {
            case OscilloscopePosition.Top:
                effectCenter = new Vector2(width / 2.0f, Size / 2.0f + 20);
                break;
            case OscilloscopePosition.Bottom:
                effectCenter = new Vector2(width / 2.0f, height - Size / 2.0f - 20);
                break;
            case OscilloscopePosition.Center:
            default:
                effectCenter = new Vector2(width / 2.0f, height / 2.0f);
                break;
        }
        
        // Apply effect scale
        effectScale = Size / 100.0f;
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

    private void ApplyOnetoneEffect(ImageBuffer output)
    {
        // Calculate effect bounds
        float halfSize = Size * effectScale / 2.0f;
        
        Vector2 topLeft = new Vector2(effectCenter.X - halfSize, effectCenter.Y - halfSize);
        Vector2 bottomRight = new Vector2(effectCenter.X + halfSize, effectCenter.Y + halfSize);
        
        // Apply onetone effect to the specified region
        for (int y = (int)topLeft.Y; y <= (int)bottomRight.Y; y++)
        {
            for (int x = (int)topLeft.X; x <= (int)bottomRight.X; x++)
            {
                if (x >= 0 && x < output.Width && y >= 0 && y < output.Height)
                {
                    // Get original pixel color
                    Color originalColor = output.GetPixel(x, y);
                    
                    // Apply onetone transformation
                    Color onetoneColor = TransformToOnetone(originalColor, x, y);
                    
                    // Set transformed pixel
                    output.SetPixel(x, y, onetoneColor);
                }
            }
        }
    }

    private Color TransformToOnetone(Color originalColor, int x, int y)
    {
        // Calculate distance from center for radial effects
        float distance = Vector2.Distance(new Vector2(x, y), effectCenter);
        float maxDistance = Size * effectScale;
        float normalizedDistance = distance / maxDistance;
        
        // Get audio modulation
        float audioModulation = 0.0f;
        if (AudioReactive)
        {
            int audioIndex = (int)(normalizedDistance * audioBufferSize);
            if (audioIndex >= 0 && audioIndex < audioBufferSize)
            {
                audioModulation = audioBuffer[audioIndex];
            }
        }
        
        // Calculate beat modulation
        float beatModulation = BeatReactive ? currentBeatIntensity : 0.0f;
        
        // Apply onetone transformation based on mode
        Color transformedColor = Mode switch
        {
            OnetoneMode.Monochrome => TransformToMonochrome(originalColor, audioModulation, beatModulation),
            OnetoneMode.SingleTone => TransformToSingleTone(originalColor, audioModulation, beatModulation),
            OnetoneMode.Grayscale => TransformToGrayscale(originalColor, audioModulation, beatModulation),
            OnetoneMode.Sepia => TransformToSepia(originalColor, audioModulation, beatModulation),
            OnetoneMode.CustomTone => TransformToCustomTone(originalColor, audioModulation, beatModulation, x, y),
            _ => originalColor
        };
        
        return transformedColor;
    }

    private Color TransformToMonochrome(Color color, float audioModulation, float beatModulation)
    {
        // Convert to grayscale
        float luminance = (color.R * 0.299f + color.G * 0.587f + color.B * 0.114f) / 255.0f;
        
        // Apply audio and beat modulation
        float modulation = (audioModulation + beatModulation) * 0.5f;
        luminance = Math.Clamp(luminance + modulation * 0.3f, 0.0f, 1.0f);
        
        // Apply contrast and brightness
        luminance = (luminance - 0.5f) * Contrast + 0.5f + Brightness;
        luminance = Math.Clamp(luminance, 0.0f, 1.0f);
        
        // Convert back to RGB
        byte grayValue = (byte)(luminance * 255);
        return new Color(grayValue, grayValue, grayValue, color.A);
    }

    private Color TransformToSingleTone(Color color, float audioModulation, float beatModulation)
    {
        // Convert to grayscale first
        float luminance = (color.R * 0.299f + color.G * 0.587f + color.B * 0.114f) / 255.0f;
        
        // Apply audio and beat modulation
        float modulation = (audioModulation + beatModulation) * 0.5f;
        luminance = Math.Clamp(luminance + modulation * 0.3f, 0.0f, 1.0f);
        
        // Apply contrast and brightness
        luminance = (luminance - 0.5f) * Contrast + 0.5f + Brightness;
        luminance = Math.Clamp(luminance, 0.0f, 1.0f);
        
        // Apply single tone color
        float intensity = luminance * ColorIntensity;
        return new Color(
            (byte)(BaseColor.R * intensity),
            (byte)(BaseColor.G * intensity),
            (byte)(BaseColor.B * intensity),
            color.A
        );
    }

    private Color TransformToGrayscale(Color color, float audioModulation, float beatModulation)
    {
        // Convert to grayscale
        float luminance = (color.R * 0.299f + color.G * 0.587f + color.B * 0.114f) / 255.0f;
        
        // Apply audio and beat modulation
        float modulation = (audioModulation + beatModulation) * 0.5f;
        luminance = Math.Clamp(luminance + modulation * 0.3f, 0.0f, 1.0f);
        
        // Apply contrast and brightness
        luminance = (luminance - 0.5f) * Contrast + 0.5f + Brightness;
        luminance = Math.Clamp(luminance, 0.0f, 1.0f);
        
        // Convert back to RGB
        byte grayValue = (byte)(luminance * 255);
        return new Color(grayValue, grayValue, grayValue, color.A);
    }

    private Color TransformToSepia(Color color, float audioModulation, float beatModulation)
    {
        // Convert to grayscale first
        float luminance = (color.R * 0.299f + color.G * 0.587f + color.B * 0.114f) / 255.0f;
        
        // Apply audio and beat modulation
        float modulation = (audioModulation + beatModulation) * 0.5f;
        luminance = Math.Clamp(luminance + modulation * 0.3f, 0.0f, 1.0f);
        
        // Apply contrast and brightness
        luminance = (luminance - 0.5f) * Contrast + 0.5f + Brightness;
        luminance = Math.Clamp(luminance, 0.0f, 1.0f);
        
        // Apply sepia tone
        float r = luminance * 1.2f;
        float g = luminance * 0.9f;
        float b = luminance * 0.6f;
        
        // Clamp values
        r = Math.Clamp(r, 0.0f, 1.0f);
        g = Math.Clamp(g, 0.0f, 1.0f);
        b = Math.Clamp(b, 0.0f, 1.0f);
        
        return new Color(
            (byte)(r * 255),
            (byte)(g * 255),
            (byte)(b * 255),
            color.A
        );
    }

    private Color TransformToCustomTone(Color color, float audioModulation, float beatModulation, int x, int y)
    {
        // Convert to grayscale first
        float luminance = (color.R * 0.299f + color.G * 0.587f + color.B * 0.114f) / 255.0f;
        
        // Apply audio and beat modulation
        float modulation = (audioModulation + beatModulation) * 0.5f;
        luminance = Math.Clamp(luminance + modulation * 0.3f, 0.0f, 1.0f);
        
        // Apply contrast and brightness
        luminance = (luminance - 0.5f) * Contrast + 0.5f + Brightness;
        luminance = Math.Clamp(luminance, 0.0f, 1.0f);
        
        // Calculate custom tone based on position
        float normalizedX = (float)(x - effectCenter.X) / (Size * effectScale);
        float normalizedY = (float)(y - effectCenter.Y) / (Size * effectScale);
        
        // Create custom tone based on position
        float hue = (normalizedX + normalizedY + modulation) * 0.5f;
        hue = (hue + frameTime * 0.1f) % 1.0f;
        
        // Convert HSV to RGB
        Color customColor = HsvToRgb(hue, 0.8f, luminance);
        
        // Blend with base color
        return Color.Lerp(customColor, BaseColor, 0.3f);
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

    private void InitializeColorPalette()
    {
        // Initialize with a warm tone palette
        ToneColors[0] = Color.Wheat;
        ToneColors[1] = Color.Tan;
        ToneColors[2] = Color.SandyBrown;
        ToneColors[3] = Color.BurlyWood;
        ToneColors[4] = Color.PeachPuff;
        ToneColors[5] = Color.Moccasin;
        ToneColors[6] = Color.NavajoWhite;
        ToneColors[7] = Color.Cornsilk;
    }

    public override void Dispose()
    {
        // Clean up resources
        audioBuffer = null!;
        previousAudioBuffer = null!;
        ToneColors = null!;
        base.Dispose();
    }
}

/// <summary>
/// Onetone effect modes
/// </summary>
public enum OnetoneMode
{
    Monochrome,    // Pure black and white
    SingleTone,    // Single color tone
    Grayscale,     // Grayscale conversion
    Sepia,         // Sepia tone effect
    CustomTone     // Custom tone generation
}