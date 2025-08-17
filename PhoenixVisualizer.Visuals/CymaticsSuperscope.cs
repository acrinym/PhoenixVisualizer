using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Cymatics Frequency superscope visualization based on AVS superscope code
/// </summary>
public sealed class CymaticsSuperscope : IVisualizerPlugin
{
    public string Id => "cymatics_superscope";
    public string DisplayName => "Cymatics Frequency";

    private int _width;
    private int _height;
    private float _time;
    private int _numPoints = 360;
    private float _frequency = 174.0f; // Start with 174Hz (Solfeggio frequency)

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
        _time += 0.02f;
        
        // Get audio data
        float volume = features.Volume;
        bool beat = features.Beat;
        
        // Handle beat events - cycle through different frequencies
        if (beat)
        {
            // Cycle through Solfeggio frequencies
            float[] frequencies = { 174.0f, 285.0f, 396.0f, 417.0f, 528.0f, 639.0f, 741.0f, 852.0f, 963.0f };
            _frequency = frequencies[Random.Shared.Next(frequencies.Length)];
        }
        
        // Create points array for the cymatics pattern
        var points = new System.Collections.Generic.List<(float x, float y)>();
        
        for (int i = 0; i < _numPoints; i++)
        {
            float t = i / (float)_numPoints;
            
            // Cymatics formula from AVS: r=i*$PI*2; d=0.35+0.05*sin(freq*t+r*freq); x=cos(r)*d; y=sin(r)*d
            float r = t * (float)Math.PI * 2;
            float d = 0.35f + 0.05f * (float)Math.Sin(_frequency * _time + r * _frequency);
            float x = (float)Math.Cos(r) * d;
            float y = (float)Math.Sin(r) * d;
            
            // Scale and center
            x = x * _width * 0.4f + _width * 0.5f;
            y = y * _height * 0.4f + _height * 0.5f;
            
            points.Add((x, y));
        }
        
        // Draw the cymatics pattern with rainbow colors
        canvas.SetLineWidth(1.0f);
        
        // Draw each point with different colors based on frequency
        for (int i = 0; i < points.Count - 1; i++)
        {
            float phi = i * 6.283f * 2;
            uint red = (uint)((0.5f + 0.5f * Math.Sin(phi * _frequency / 100.0f)) * 255);
            uint green = (uint)((0.5f + 0.5f * Math.Sin(phi * _frequency / 100.0f + 2.094f)) * 255);
            uint blue = (uint)((0.5f + 0.5f * Math.Sin(phi * _frequency / 100.0f + 4.188f)) * 255);
            
            uint color = (uint)((0xFF << 24) | (red << 16) | (green << 8) | blue);
            
            canvas.DrawLine(points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, color, 1.0f);
        }
        
        // Draw frequency indicator
        uint textColor = beat ? 0xFFFFFF00 : 0xFF00FFFF;
        canvas.DrawText($"{_frequency:F0}Hz", 10, 30, textColor, 16.0f);
    }

    public void Dispose()
    {
        // Nothing to clean up
    }
}
