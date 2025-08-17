using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Rotating Bow Thing superscope visualization based on AVS superscope code
/// </summary>
public sealed class RotatingBowSuperscope : IVisualizerPlugin
{
    public string Id => "rotating_bow_superscope";
    public string DisplayName => "Rotating Bow Thing";

    private int _width;
    private int _height;
    private float _time;
    private int _numPoints = 80;

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        // Clear with dark background
        canvas.Clear(0xFF000000);
        
        // Update time
        _time += 0.01f;
        
        // Get audio data
        float volume = features.Volume;
        bool beat = features.Beat;
        
        // Create points array for the rotating bow
        var points = new System.Collections.Generic.List<(float x, float y)>();
        
        for (int i = 0; i < _numPoints; i++)
        {
            float t = i / (float)_numPoints;
            
            // Rotating bow formula from AVS: r=i*$PI*2; d=sin(r*3)+v*0.5; x=cos(t+r)*d; y=sin(t-r)*d
            float r = t * (float)Math.PI * 2;
            float d = (float)Math.Sin(r * 3) + volume * 0.5f;
            float x = (float)Math.Cos(_time + r) * d;
            float y = (float)Math.Sin(_time - r) * d;
            
            // Scale and center
            x = x * _width * 0.4f + _width * 0.5f;
            y = y * _height * 0.4f + _height * 0.5f;
            
            points.Add((x, y));
        }
        
        // Draw the rotating bow
        uint color = beat ? 0xFFFF8000 : 0xFF0080FF; // Orange on beat, blue otherwise
        canvas.SetLineWidth(1.0f);
        canvas.DrawLines(points.ToArray(), 1.0f, color);
    }

    public void Dispose()
    {
        // Nothing to clean up
    }
}
