using System;
using System.Numerics;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Effects;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

/// <summary>
/// Interference Patterns Effects Node - Real-time audio visualization with interference pattern generation
/// Based on VIS_AVS interference pattern implementations
/// </summary>
public class InterferencePatternsEffectsNode : BaseEffectNode
{
    // Configuration
    public bool Enabled { get; set; } = true;
    public OscilloscopeChannel Channel { get; set; } = OscilloscopeChannel.Center;
    public OscilloscopePosition Position { get; set; } = OscilloscopePosition.Center;
    public int Size { get; set; } = 100;
    public AudioSourceType SourceType { get; set; } = AudioSourceType.Oscilloscope;
    
    // Interference configuration
    public int WaveCount { get; set; } = 3; // Number of interfering waves
    public float WaveFrequency { get; set; } = 1.0f; // Base wave frequency
    public float WaveAmplitude { get; set; } = 50.0f; // Base wave amplitude
    public float WaveSpeed { get; set; } = 1.0f; // Wave movement speed
    public float InterferenceStrength { get; set; } = 1.0f; // Interference effect strength
    public bool BeatReactive { get; set; } = true; // Enable beat reactivity
    
    // Colors
    public Color[] WaveColors { get; set; } = new Color[8];
    public int ColorCount { get; set; } = 8;
    public Color InterferenceColor { get; set; } = Color.White;
    public Color BackgroundColor { get; set; } = Color.Transparent;
    
    // Audio processing
    private float[] audioBuffer = new float[576];
    private float[] previousAudioBuffer = new float[576];
    private readonly int audioBufferSize = 576; // Standard AVS audio buffer size
    
    // Interference state
    private Vector2 patternCenter;
    private float currentTime;
    private float currentBeatIntensity;
    private float patternScale;
    
    // Performance tracking
    private float frameTime;
    private int frameCount;

    public InterferencePatternsEffectsNode()
    {
        // Initialize interference state
        currentTime = 0.0f;
        currentBeatIntensity = 0.0f;
        patternScale = 1.0f;
        
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
        
        // Update pattern center position
        UpdatePatternPosition(output.Width, output.Height);
        
        // Process audio data
        ProcessAudioData(audioFeatures);
        
        // Update interference time
        UpdateInterferenceTime();
        
        // Render interference patterns
        RenderInterferencePatterns(output);
    }

    private void UpdatePatternPosition(int width, int height)
    {
        // Calculate pattern center based on position setting
        switch (Position)
        {
            case OscilloscopePosition.Top:
                patternCenter = new Vector2(width / 2.0f, Size / 2.0f + 20);
                break;
            case OscilloscopePosition.Bottom:
                patternCenter = new Vector2(width / 2.0f, height - Size / 2.0f - 20);
                break;
            case OscilloscopePosition.Center:
            default:
                patternCenter = new Vector2(width / 2.0f, height / 2.0f);
                break;
        }
        
        // Apply pattern scale
        patternScale = Size / 100.0f;
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
        
        // Update beat intensity
        UpdateBeatIntensity(audioFeatures);
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

    private void UpdateBeatIntensity(AudioFeatures audioFeatures)
    {
        if (!BeatReactive) return;
        
        // Calculate beat intensity from audio data
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
            
            // Smooth beat intensity
            float smoothing = 0.8f;
            currentBeatIntensity = avgIntensity * (1.0f - smoothing) + currentBeatIntensity * smoothing;
        }
        
        // Apply beat detection if available
        if (audioFeatures.Beat)
        {
            currentBeatIntensity = Math.Min(1.0f, currentBeatIntensity + 0.3f);
        }
    }

    private void UpdateInterferenceTime()
    {
        // Update time based on wave speed and beat intensity
        float effectiveSpeed = WaveSpeed;
        if (BeatReactive)
        {
            effectiveSpeed += currentBeatIntensity * 2.0f;
        }
        
        currentTime += effectiveSpeed * frameTime;
        if (currentTime > 2.0f * MathF.PI)
        {
            currentTime -= 2.0f * MathF.PI;
        }
    }

    private void RenderInterferencePatterns(ImageBuffer output)
    {
        // Calculate pattern bounds
        float halfSize = Size * patternScale / 2.0f;
        
        Vector2 topLeft = new Vector2(patternCenter.X - halfSize, patternCenter.Y - halfSize);
        Vector2 bottomRight = new Vector2(patternCenter.X + halfSize, patternCenter.Y + halfSize);
        
        // Draw background if specified
        if (BackgroundColor.A > 0)
        {
            DrawRectangle(output, topLeft, bottomRight, BackgroundColor);
        }
        
        // Render interference patterns
        for (int y = (int)topLeft.Y; y <= (int)bottomRight.Y; y++)
        {
            for (int x = (int)topLeft.X; x <= (int)bottomRight.X; x++)
            {
                if (x >= 0 && x < output.Width && y >= 0 && y < output.Height)
                {
                    // Calculate interference value at this point
                    float interferenceValue = CalculateInterferenceValue(x, y);
                    
                    // Apply audio modulation
                    float audioModulation = GetAudioModulation(x, y);
                    interferenceValue *= (1.0f + audioModulation * InterferenceStrength);
                    
                    // Clamp and normalize
                    interferenceValue = Math.Clamp(interferenceValue, 0.0f, 1.0f);
                    
                    // Get color for this interference value
                    Color pixelColor = GetInterferenceColor(interferenceValue, x, y);
                    
                    // Set pixel
                    output.SetPixel(x, y, pixelColor);
                }
            }
        }
    }

    private float CalculateInterferenceValue(int x, int y)
    {
        // Convert to relative coordinates
        float relX = (x - patternCenter.X) / (Size * patternScale);
        float relY = (y - patternCenter.Y) / (Size * patternScale);
        
        float interference = 0.0f;
        
        // Calculate interference from multiple waves
        for (int wave = 0; wave < WaveCount; wave++)
        {
            // Calculate wave phase
            float wavePhase = (float)wave * 2.0f * MathF.PI / WaveCount;
            
            // Calculate wave value
            float waveValue = MathF.Sin(
                WaveFrequency * (relX * MathF.Cos(wavePhase) + relY * MathF.Sin(wavePhase)) + 
                currentTime + wavePhase
            );
            
            // Add to interference
            interference += waveValue;
        }
        
        // Normalize interference
        interference /= WaveCount;
        
        // Convert to 0-1 range
        return (interference + 1.0f) * 0.5f;
    }

    private float GetAudioModulation(int x, int y)
    {
        // Calculate distance from center
        float distance = Vector2.Distance(new Vector2(x, y), patternCenter);
        float maxDistance = Size * patternScale;
        float normalizedDistance = distance / maxDistance;
        
        // Get audio intensity at this distance
        int audioIndex = (int)(normalizedDistance * audioBufferSize);
        if (audioIndex >= audioBufferSize) audioIndex = audioBufferSize - 1;
        if (audioIndex < 0) audioIndex = 0;
        
        return audioBuffer[audioIndex];
    }

    private Color GetInterferenceColor(float interferenceValue, int x, int y)
    {
        // Calculate color based on interference value and position
        float normalizedX = (float)(x - patternCenter.X) / (Size * patternScale);
        float normalizedY = (float)(y - patternCenter.Y) / (Size * patternScale);
        
        // Calculate hue based on position and interference
        float hue = (normalizedX + normalizedY + interferenceValue) * 0.33f;
        hue = (hue + currentTime * 0.1f) % 1.0f;
        
        // Calculate saturation and value
        float saturation = 0.8f + interferenceValue * 0.2f;
        float value = 0.5f + interferenceValue * 0.5f;
        
        // Convert HSV to RGB
        Color hsvColor = HsvToRgb(hue, saturation, value);
        
        // Blend with interference color
        return Color.Lerp(hsvColor, InterferenceColor, interferenceValue * 0.3f);
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
        WaveColors[0] = Color.Red;
        WaveColors[1] = Color.Orange;
        WaveColors[2] = Color.Yellow;
        WaveColors[3] = Color.Green;
        WaveColors[4] = Color.Cyan;
        WaveColors[5] = Color.Blue;
        WaveColors[6] = Color.Magenta;
        WaveColors[7] = Color.Pink;
    }

    public override void Dispose()
    {
        // Clean up resources
        audioBuffer = null!;
        previousAudioBuffer = null!;
        WaveColors = null!;
        base.Dispose();
    }
}