using System;
using System.Collections.Generic;
using System.Numerics;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Effects;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

/// <summary>
/// Effect Stacking Effects Node - Real-time audio visualization with effect stacking and layering
/// Based on VIS_AVS effect stacking implementations
/// </summary>
public class EffectStackingEffectsNode : BaseEffectNode
{
    // Configuration
    public bool Enabled { get; set; } = true;
    public OscilloscopeChannel Channel { get; set; } = OscilloscopeChannel.Center;
    public OscilloscopePosition Position { get; set; } = OscilloscopePosition.Center;
    public int Size { get; set; } = 100;
    public AudioSourceType SourceType { get; set; } = AudioSourceType.Oscilloscope;
    
    // Stacking configuration
    public int LayerCount { get; set; } = 3; // Number of effect layers
    public float LayerOpacity { get; set; } = 0.7f; // Opacity of each layer
    public float LayerSpacing { get; set; } = 20.0f; // Spacing between layers
    public bool BeatReactive { get; set; } = true; // Enable beat reactivity
    public bool AudioReactive { get; set; } = true; // Enable audio reactivity
    public StackingMode Mode { get; set; } = StackingMode.Multiply; // Blending mode
    
    // Colors
    public Color[] LayerColors { get; set; } = new Color[8];
    public int ColorCount { get; set; } = 8;
    public Color BackgroundColor { get; set; } = Color.Transparent;
    
    // Audio processing
    private float[] audioBuffer = new float[576];
    private float[] previousAudioBuffer = new float[576];
    private readonly int audioBufferSize = 576; // Standard AVS audio buffer size
    
    // Stacking state
    private Vector2 stackCenter;
    private float currentBeatIntensity;
    private float currentAudioIntensity;
    private float stackScale;
    private List<EffectLayer> effectLayers;
    
    // Performance tracking
    private float frameTime;
    private int frameCount;

    public EffectStackingEffectsNode()
    {
        // Initialize stacking state
        currentBeatIntensity = 0.0f;
        currentAudioIntensity = 0.0f;
        stackScale = 1.0f;
        
        // Initialize timing
        frameTime = 0;
        frameCount = 0;
        
        // Initialize effect layers
        InitializeEffectLayers();
        
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
        
        // Update stack center position
        UpdateStackPosition(output.Width, output.Height);
        
        // Process audio data
        ProcessAudioData(audioFeatures);
        
        // Update effect layers
        UpdateEffectLayers();
        
        // Render stacked effects
        RenderStackedEffects(output);
    }

    private void UpdateStackPosition(int width, int height)
    {
        // Calculate stack center based on position setting
        switch (Position)
        {
            case OscilloscopePosition.Top:
                stackCenter = new Vector2(width / 2.0f, Size / 2.0f + 20);
                break;
            case OscilloscopePosition.Bottom:
                stackCenter = new Vector2(width / 2.0f, height - Size / 2.0f - 20);
                break;
            case OscilloscopePosition.Center:
            default:
                stackCenter = new Vector2(width / 2.0f, height / 2.0f);
                break;
        }
        
        // Apply stack scale
        stackScale = Size / 100.0f;
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

    private void UpdateEffectLayers()
    {
        // Update each effect layer
        for (int i = 0; i < effectLayers.Count; i++)
        {
            var layer = effectLayers[i];
            
            // Update layer position based on audio
            float audioOffset = 0.0f;
            if (AudioReactive)
            {
                int audioIndex = (i * audioBufferSize) / effectLayers.Count;
                if (audioIndex < audioBufferSize)
                {
                    audioOffset = audioBuffer[audioIndex] * LayerSpacing * 0.5f;
                }
            }
            
            // Update layer rotation
            layer.Rotation += layer.RotationSpeed * frameTime;
            if (layer.Rotation > 2.0f * MathF.PI)
            {
                layer.Rotation -= 2.0f * MathF.PI;
            }
            
            // Update layer scale based on beat
            if (BeatReactive)
            {
                layer.Scale = 1.0f + currentBeatIntensity * 0.3f;
            }
            
            // Update layer position
            layer.Position = new Vector2(
                stackCenter.X + (i - effectLayers.Count / 2.0f) * LayerSpacing + audioOffset,
                stackCenter.Y
            );
        }
    }

    private void RenderStackedEffects(ImageBuffer output)
    {
        // Render each layer from back to front
        for (int i = 0; i < effectLayers.Count; i++)
        {
            var layer = effectLayers[i];
            RenderEffectLayer(output, layer, i);
        }
    }

    private void RenderEffectLayer(ImageBuffer output, EffectLayer layer, int layerIndex)
    {
        // Calculate layer bounds
        float halfSize = Size * stackScale * layer.Scale / 2.0f;
        
        Vector2 topLeft = new Vector2(layer.Position.X - halfSize, layer.Position.Y - halfSize);
        Vector2 bottomRight = new Vector2(layer.Position.X + halfSize, layer.Position.Y + halfSize);
        
        // Get layer color
        Color layerColor = GetLayerColor(layerIndex);
        
        // Apply layer opacity
        layerColor = new Color(
            layerColor.R,
            layerColor.G,
            layerColor.B,
            (byte)(layerColor.A * LayerOpacity)
        );
        
        // Render layer based on type
        switch (layer.Type)
        {
            case LayerType.Circle:
                RenderCircleLayer(output, layer.Position, halfSize, layerColor, layer.Rotation);
                break;
            case LayerType.Square:
                RenderSquareLayer(output, layer.Position, halfSize, layerColor, layer.Rotation);
                break;
            case LayerType.Triangle:
                RenderTriangleLayer(output, layer.Position, halfSize, layerColor, layer.Rotation);
                break;
            case LayerType.Star:
                RenderStarLayer(output, layer.Position, halfSize, layerColor, layer.Rotation);
                break;
        }
    }

    private void RenderCircleLayer(ImageBuffer output, Vector2 center, float radius, Color color, float rotation)
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

    private void RenderSquareLayer(ImageBuffer output, Vector2 center, float size, Color color, float rotation)
    {
        int sizeInt = (int)size;
        
        for (int y = -sizeInt; y <= sizeInt; y++)
        {
            for (int x = -sizeInt; x <= sizeInt; x++)
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

    private void RenderTriangleLayer(ImageBuffer output, Vector2 center, float size, Color color, float rotation)
    {
        // Calculate triangle vertices
        Vector2[] vertices = new Vector2[3];
        for (int i = 0; i < 3; i++)
        {
            float angle = (2.0f * MathF.PI * i / 3.0f) + rotation;
            vertices[i] = new Vector2(
                center.X + size * MathF.Cos(angle),
                center.Y + size * MathF.Sin(angle)
            );
        }
        
        // Draw triangle using line drawing
        for (int i = 0; i < 3; i++)
        {
            int nextIndex = (i + 1) % 3;
            DrawLine(output, vertices[i], vertices[nextIndex], color, 2);
        }
    }

    private void RenderStarLayer(ImageBuffer output, Vector2 center, float size, Color color, float rotation)
    {
        int points = 5;
        Vector2[] vertices = new Vector2[points * 2];
        
        for (int i = 0; i < points; i++)
        {
            float angle = (2.0f * MathF.PI * i / points) + rotation;
            
            // Outer point
            vertices[i * 2] = new Vector2(
                center.X + size * MathF.Cos(angle),
                center.Y + size * MathF.Sin(angle)
            );
            
            // Inner point
            vertices[i * 2 + 1] = new Vector2(
                center.X + size * 0.5f * MathF.Cos(angle + MathF.PI / points),
                center.Y + size * 0.5f * MathF.Sin(angle + MathF.PI / points)
            );
        }
        
        // Draw star using line drawing
        for (int i = 0; i < vertices.Length; i++)
        {
            int nextIndex = (i + 1) % vertices.Length;
            DrawLine(output, vertices[i], vertices[nextIndex], color, 2);
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

    private Color GetLayerColor(int layerIndex)
    {
        // Get color based on layer index
        int colorIndex = layerIndex % ColorCount;
        return LayerColors[colorIndex];
    }

    private void InitializeEffectLayers()
    {
        effectLayers = new List<EffectLayer>();
        
        for (int i = 0; i < LayerCount; i++)
        {
            var layer = new EffectLayer
            {
                Type = (LayerType)(i % 4), // Cycle through layer types
                Position = Vector2.Zero,
                Rotation = 0.0f,
                RotationSpeed = 0.5f + i * 0.2f, // Different rotation speeds
                Scale = 1.0f
            };
            
            effectLayers.Add(layer);
        }
    }

    private void InitializeColorPalette()
    {
        // Initialize with a vibrant color palette
        LayerColors[0] = Color.Red;
        LayerColors[1] = Color.Orange;
        LayerColors[2] = Color.Yellow;
        LayerColors[3] = Color.Green;
        LayerColors[4] = Color.Cyan;
        LayerColors[5] = Color.Blue;
        LayerColors[6] = Color.Magenta;
        LayerColors[7] = Color.Pink;
    }

    public override void Dispose()
    {
        // Clean up resources
        audioBuffer = null!;
        previousAudioBuffer = null!;
        LayerColors = null!;
        effectLayers?.Clear();
        effectLayers = null!;
        base.Dispose();
    }
}

/// <summary>
/// Effect layer information
/// </summary>
public class EffectLayer
{
    public LayerType Type { get; set; }
    public Vector2 Position { get; set; }
    public float Rotation { get; set; }
    public float RotationSpeed { get; set; }
    public float Scale { get; set; }
}

/// <summary>
/// Layer types for effect stacking
/// </summary>
public enum LayerType
{
    Circle,
    Square,
    Triangle,
    Star
}

/// <summary>
/// Stacking blending modes
/// </summary>
public enum StackingMode
{
    Normal,
    Multiply,
    Screen,
    Overlay,
    Add,
    Subtract
}