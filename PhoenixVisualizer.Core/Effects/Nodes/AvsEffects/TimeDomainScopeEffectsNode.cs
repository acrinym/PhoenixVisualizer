using System;
using System.Numerics;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Effects;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

/// <summary>
/// Time Domain Scope Effect - Real-time audio visualization with time-domain oscilloscope patterns
/// Based on VIS_AVS r_timedomain.cpp implementation
/// </summary>
public class TimeDomainScopeEffectsNode : BaseEffectNode
{
    // Configuration
    public bool Enabled { get; set; } = true;
    public OscilloscopeChannel Channel { get; set; } = OscilloscopeChannel.Center;
    public OscilloscopePosition Position { get; set; } = OscilloscopePosition.Center;
    public int Size { get; set; } = 100;
    public AudioSourceType SourceType { get; set; } = AudioSourceType.Oscilloscope;
    
    // Scope configuration
    public float ScopeHeight { get; set; } = 100.0f;
    public float ScopeWidth { get; set; } = 200.0f;
    public float LineThickness { get; set; } = 2.0f;
    public bool ShowGrid { get; set; } = true;
    public bool ShowCenterLine { get; set; } = true;
    public float ScrollSpeed { get; set; } = 1.0f;
    
    // Colors
    public Color WaveformColor { get; set; } = Color.Cyan;
    public Color GridColor { get; set; } = Color.DarkGray;
    public Color CenterLineColor { get; set; } = Color.White;
    public Color BackgroundColor { get; set; } = Color.Transparent;
    
    // Audio processing
    private float[] audioBuffer = new float[576];
    private float[] previousAudioBuffer = new float[576];
    private readonly int audioBufferSize = 576; // Standard AVS audio buffer size
    
    // Scope state
    private Vector2 scopeCenter;
    private float currentScrollOffset;
    private float scopeScale;
    
    // Performance tracking
    private float frameTime;
    private int frameCount;

    public TimeDomainScopeEffectsNode()
    {
        // Initialize scope state
        currentScrollOffset = 0.0f;
        scopeScale = 1.0f;
        
        // Initialize timing
        frameTime = 0;
        frameCount = 0;
    }

    protected override void ProcessCore(ImageBuffer input, ImageBuffer output, AudioFeatures? audioFeatures)
    {
        if (!Enabled) return;
        
        // Update frame timing
        frameTime += 1.0f / 60.0f; // Assume 60fps
        frameCount++;
        
        // Copy input to output
        input.CopyTo(output);
        
        // Update scope center position
        UpdateScopePosition(output.Width, output.Height);
        
        // Process audio data
        ProcessAudioData(audioFeatures);
        
        // Update scroll offset
        UpdateScrollOffset();
        
        // Render time domain scope
        RenderTimeDomainScope(output);
    }

    private void UpdateScopePosition(int width, int height)
    {
        // Calculate scope center based on position setting
        switch (Position)
        {
            case OscilloscopePosition.Top:
                scopeCenter = new Vector2(width / 2.0f, ScopeHeight / 2.0f + 20);
                break;
            case OscilloscopePosition.Bottom:
                scopeCenter = new Vector2(width / 2.0f, height - ScopeHeight / 2.0f - 20);
                break;
            case OscilloscopePosition.Center:
            default:
                scopeCenter = new Vector2(width / 2.0f, height / 2.0f);
                break;
        }
        
        // Apply scope scale
        scopeScale = Size / 100.0f;
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

    private void UpdateScrollOffset()
    {
        // Update scroll offset based on speed
        currentScrollOffset += ScrollSpeed * frameTime;
        if (currentScrollOffset > ScopeWidth)
        {
            currentScrollOffset = 0.0f;
        }
    }

    private void RenderTimeDomainScope(ImageBuffer output)
    {
        // Calculate scope bounds
        float halfWidth = ScopeWidth * scopeScale / 2.0f;
        float halfHeight = ScopeHeight * scopeScale / 2.0f;
        
        Vector2 topLeft = new Vector2(scopeCenter.X - halfWidth, scopeCenter.Y - halfHeight);
        Vector2 bottomRight = new Vector2(scopeCenter.X + halfWidth, scopeCenter.Y + halfHeight);
        
        // Draw background if specified
        if (BackgroundColor.A > 0)
        {
            DrawRectangle(output, topLeft, bottomRight, BackgroundColor);
        }
        
        // Draw grid if enabled
        if (ShowGrid)
        {
            DrawGrid(output, topLeft, bottomRight);
        }
        
        // Draw center line if enabled
        if (ShowCenterLine)
        {
            Vector2 centerLineStart = new Vector2(topLeft.X, scopeCenter.Y);
            Vector2 centerLineEnd = new Vector2(bottomRight.X, scopeCenter.Y);
            DrawLine(output, centerLineStart, centerLineEnd, CenterLineColor, (int)LineThickness);
        }
        
        // Draw waveform
        DrawWaveform(output, topLeft, bottomRight);
    }

    private void DrawGrid(ImageBuffer output, Vector2 topLeft, Vector2 bottomRight)
    {
        float gridSpacing = 20.0f * scopeScale;
        
        // Vertical grid lines
        for (float x = topLeft.X; x <= bottomRight.X; x += gridSpacing)
        {
            Vector2 start = new Vector2(x, topLeft.Y);
            Vector2 end = new Vector2(x, bottomRight.Y);
            DrawLine(output, start, end, GridColor, 1);
        }
        
        // Horizontal grid lines
        for (float y = topLeft.Y; y <= bottomRight.Y; y += gridSpacing)
        {
            Vector2 start = new Vector2(topLeft.X, y);
            Vector2 end = new Vector2(bottomRight.X, y);
            DrawLine(output, start, end, GridColor, 1);
        }
    }

    private void DrawWaveform(ImageBuffer output, Vector2 topLeft, Vector2 bottomRight)
    {
        float width = bottomRight.X - topLeft.X;
        float height = bottomRight.Y - topLeft.Y;
        float centerY = topLeft.Y + height / 2.0f;
        
        // Calculate step size for audio samples
        int stepSize = Math.Max(1, audioBufferSize / (int)width);
        
        Vector2? previousPoint = null;
        
        for (int i = 0; i < audioBufferSize; i += stepSize)
        {
            if (i >= audioBufferSize) break;
            
            // Calculate X position with scroll offset
            float x = topLeft.X + ((float)i / audioBufferSize) * width - currentScrollOffset;
            if (x < topLeft.X) x += width; // Wrap around
            
            // Calculate Y position from audio amplitude
            float amplitude = audioBuffer[i];
            float y = centerY - (amplitude * height / 2.0f);
            
            Vector2 currentPoint = new Vector2(x, y);
            
            // Draw line to previous point
            if (previousPoint.HasValue)
            {
                DrawLine(output, previousPoint.Value, currentPoint, WaveformColor, (int)LineThickness);
            }
            
            previousPoint = currentPoint;
        }
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

    public override void Dispose()
    {
        // Clean up resources
        audioBuffer = null!;
        previousAudioBuffer = null!;
        base.Dispose();
    }
}