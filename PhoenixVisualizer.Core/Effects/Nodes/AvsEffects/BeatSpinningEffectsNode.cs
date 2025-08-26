using System;
using System.Numerics;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Effects;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

/// <summary>
/// Beat Spinning Effects Node - Real-time audio visualization with beat-reactive spinning patterns
/// Based on VIS_AVS r_bspin.cpp implementation
/// </summary>
public class BeatSpinningEffectsNode : BaseEffectNode
{
    // Configuration
    public bool Enabled { get; set; } = true;
    public OscilloscopeChannel Channel { get; set; } = OscilloscopeChannel.Center;
    public OscilloscopePosition Position { get; set; } = OscilloscopePosition.Center;
    public int Size { get; set; } = 100;
    public AudioSourceType SourceType { get; set; } = AudioSourceType.Oscilloscope;
    
    // Spinning configuration
    public int SpinningArms { get; set; } = 2; // Number of spinning arms
    public float BaseRotationSpeed { get; set; } = 1.0f; // Base rotation speed
    public float BeatMultiplier { get; set; } = 2.0f; // Beat acceleration multiplier
    public float ArmLength { get; set; } = 80.0f; // Length of spinning arms
    public float ArmThickness { get; set; } = 3.0f; // Thickness of spinning arms
    public bool BeatReactive { get; set; } = true; // Enable beat reactivity
    
    // Colors
    public Color[] ArmColors { get; set; } = new Color[8];
    public int ColorCount { get; set; } = 8;
    public Color CenterColor { get; set; } = Color.White;
    public Color GlowColor { get; set; } = Color.Cyan;
    
    // Audio processing
    private float[] audioBuffer = new float[576];
    private float[] previousAudioBuffer = new float[576];
    private readonly int audioBufferSize = 576; // Standard AVS audio buffer size
    
    // Spinning state
    private Vector2 spinningCenter;
    private float currentRotation;
    private float currentBeatIntensity;
    private float spinningScale;
    
    // Performance tracking
    private float frameTime;
    private int frameCount;

    public BeatSpinningEffectsNode()
    {
        // Initialize spinning state
        currentRotation = 0.0f;
        currentBeatIntensity = 0.0f;
        spinningScale = 1.0f;
        
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
        
        // Update spinning center position
        UpdateSpinningPosition(output.Width, output.Height);
        
        // Process audio data
        ProcessAudioData(audioFeatures);
        
        // Update spinning rotation
        UpdateSpinningRotation();
        
        // Render beat spinning effects
        RenderBeatSpinning(output);
    }

    private void UpdateSpinningPosition(int width, int height)
    {
        // Calculate spinning center based on position setting
        switch (Position)
        {
            case OscilloscopePosition.Top:
                spinningCenter = new Vector2(width / 2.0f, Size / 2.0f + 20);
                break;
            case OscilloscopePosition.Bottom:
                spinningCenter = new Vector2(width / 2.0f, height - Size / 2.0f - 20);
                break;
            case OscilloscopePosition.Center:
            default:
                spinningCenter = new Vector2(width / 2.0f, height / 2.0f);
                break;
        }
        
        // Apply spinning scale
        spinningScale = Size / 100.0f;
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

    private void UpdateSpinningRotation()
    {
        // Calculate rotation speed based on beat intensity
        float rotationSpeed = BaseRotationSpeed;
        if (BeatReactive)
        {
            rotationSpeed += currentBeatIntensity * BeatMultiplier;
        }
        
        // Update rotation
        currentRotation += rotationSpeed * frameTime;
        if (currentRotation > 2.0f * MathF.PI)
        {
            currentRotation -= 2.0f * MathF.PI;
        }
    }

    private void RenderBeatSpinning(ImageBuffer output)
    {
        // Calculate spinning parameters
        float armLength = ArmLength * spinningScale;
        float maxAmplitude = Size * 0.2f; // Maximum arm expansion
        
        // Draw center point
        DrawCenterPoint(output);
        
        // Render spinning arms
        for (int i = 0; i < SpinningArms; i++)
        {
            // Calculate angle for this arm
            float angle = (2.0f * MathF.PI * i / SpinningArms) + currentRotation;
            
            // Get audio amplitude for this arm
            int audioIndex = (i * audioBufferSize) / SpinningArms;
            float amplitude = 0.0f;
            if (audioIndex < audioBufferSize)
            {
                amplitude = MathF.Abs(audioBuffer[audioIndex]) * maxAmplitude;
            }
            
            // Calculate arm end point
            float effectiveLength = armLength + amplitude;
            Vector2 armEnd = new Vector2(
                spinningCenter.X + effectiveLength * MathF.Cos(angle),
                spinningCenter.Y + effectiveLength * MathF.Sin(angle)
            );
            
            // Get color for this arm
            Color armColor = GetArmColor(i, amplitude);
            
            // Draw spinning arm
            DrawSpinningArm(output, spinningCenter, armEnd, armColor);
        }
        
        // Draw glow effects
        DrawGlowEffects(output, armLength, maxAmplitude);
    }

    private void DrawCenterPoint(ImageBuffer output)
    {
        // Draw center point with glow effect
        int centerSize = (int)(8 * spinningScale);
        
        // Draw glow
        for (int y = -centerSize * 2; y <= centerSize * 2; y++)
        {
            for (int x = -centerSize * 2; x <= centerSize * 2; x++)
            {
                int drawX = (int)spinningCenter.X + x;
                int drawY = (int)spinningCenter.Y + y;
                
                if (drawX >= 0 && drawX < output.Width && drawY >= 0 && drawY < output.Height)
                {
                    float distance = MathF.Sqrt(x * x + y * y);
                    if (distance <= centerSize * 2)
                    {
                        float alpha = 1.0f - (distance / (centerSize * 2));
                        Color glowColor = new Color(
                            GlowColor.R,
                            GlowColor.G,
                            GlowColor.B,
                            (byte)(GlowColor.A * alpha)
                        );
                        output.SetPixel(drawX, drawY, glowColor);
                    }
                }
            }
        }
        
        // Draw center
        for (int y = -centerSize; y <= centerSize; y++)
        {
            for (int x = -centerSize; x <= centerSize; x++)
            {
                int drawX = (int)spinningCenter.X + x;
                int drawY = (int)spinningCenter.Y + y;
                
                if (drawX >= 0 && drawX < output.Width && drawY >= 0 && drawY < output.Height)
                {
                    float distance = MathF.Sqrt(x * x + y * y);
                    if (distance <= centerSize)
                    {
                        output.SetPixel(drawX, drawY, CenterColor);
                    }
                }
            }
        }
    }

    private void DrawSpinningArm(ImageBuffer output, Vector2 start, Vector2 end, Color color)
    {
        // Draw the spinning arm
        DrawLine(output, start, end, color, (int)ArmThickness);
        
        // Add glow effect
        Color glowColor = new Color(
            (byte)(color.R / 2),
            (byte)(color.G / 2),
            (byte)(color.B / 2),
            (byte)(color.A / 2)
        );
        DrawLine(output, start, end, glowColor, (int)(ArmThickness * 2));
    }

    private void DrawGlowEffects(ImageBuffer output, float armLength, float maxAmplitude)
    {
        // Draw circular glow around the spinning center
        float glowRadius = armLength + maxAmplitude;
        int glowSize = (int)(glowRadius * spinningScale);
        
        for (int y = -glowSize; y <= glowSize; y++)
        {
            for (int x = -glowSize; x <= glowSize; x++)
            {
                int drawX = (int)spinningCenter.X + x;
                int drawY = (int)spinningCenter.Y + y;
                
                if (drawX >= 0 && drawX < output.Width && drawY >= 0 && drawY < output.Height)
                {
                    float distance = MathF.Sqrt(x * x + y * y);
                    if (distance <= glowRadius * spinningScale)
                    {
                        float alpha = 0.1f * (1.0f - (distance / (glowRadius * spinningScale)));
                        Color glowColor = new Color(
                            GlowColor.R,
                            GlowColor.G,
                            GlowColor.B,
                            (byte)(GlowColor.A * alpha)
                        );
                        output.SetPixel(drawX, drawY, glowColor);
                    }
                }
            }
        }
    }

    private Color GetArmColor(int armIndex, float amplitude)
    {
        // Calculate color based on arm index and amplitude
        float normalizedIndex = (float)armIndex / SpinningArms;
        float normalizedAmplitude = Math.Clamp(amplitude / (Size * 0.2f), 0.0f, 1.0f);
        
        // Interpolate between colors based on amplitude
        int colorIndex = (int)(normalizedIndex * ColorCount) % ColorCount;
        int nextColorIndex = (colorIndex + 1) % ColorCount;
        
        Color currentColor = ArmColors[colorIndex];
        Color nextColor = ArmColors[nextColorIndex];
        
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
        ArmColors[0] = Color.Red;
        ArmColors[1] = Color.Orange;
        ArmColors[2] = Color.Yellow;
        ArmColors[3] = Color.Green;
        ArmColors[4] = Color.Cyan;
        ArmColors[5] = Color.Blue;
        ArmColors[6] = Color.Magenta;
        ArmColors[7] = Color.Pink;
    }

    public override void Dispose()
    {
        // Clean up resources
        audioBuffer = null!;
        previousAudioBuffer = null!;
        ArmColors = null!;
        base.Dispose();
    }
}