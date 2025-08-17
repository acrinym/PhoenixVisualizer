using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Rainbow Sphere Grid superscope visualization based on AVS superscope code
/// </summary>
public sealed class RainbowSphereGridSuperscope : IVisualizerPlugin
{
    public string Id => "rainbow_sphere_grid_superscope";
    public string DisplayName => "Rainbow Sphere Grid";

    private int _width;
    private int _height;
    private float _time;
    private float _phase;
    private int _numPoints = 700;

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0;
        _phase = 0;
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
        
        // Update time and phase
        _time += 0.04f;
        _phase += 0.02f;
        
        // Get audio data
        float volume = features.Volume;
        bool beat = features.Beat;
        
        // Create points array for the sphere grid
        var points = new System.Collections.Generic.List<(float x, float y)>();
        
        for (int i = 0; i < _numPoints; i++)
        {
            float t = i / (float)_numPoints;
            
            // Sphere grid formula from AVS: theta=acos(1-2*i); phi=i*$PI*6; xs=sin(theta)*cos(phi+t); ys=sin(theta)*sin(phi+t); zs=cos(theta);
            float theta = (float)Math.Acos(1 - 2 * t);
            float phi = t * (float)Math.PI * 6;
            
            // Calculate sphere coordinates
            float xs = (float)Math.Sin(theta) * (float)Math.Cos(phi + _time);
            float ys = (float)Math.Sin(theta) * (float)Math.Sin(phi + _time);
            float zs = (float)Math.Cos(theta);
            
            // Add grid distortion
            float g = 0.1f * ((float)Math.Sin(phi * 6 + _phase) + (float)Math.Sin(theta * 6 + _phase));
            xs += g * xs;
            ys += g * ys;
            
            // Apply perspective projection
            float pers = 1.0f / (1.0f + zs);
            float x = xs * pers;
            float y = ys * pers;
            
            // Scale and center
            x = x * _width * 0.4f + _width * 0.5f;
            y = y * _height * 0.4f + _height * 0.5f;
            
            points.Add((x, y));
        }
        
        // Draw the sphere grid with rainbow colors
        canvas.SetLineWidth(1.0f);
        
        // Draw each point with different colors
        for (int i = 0; i < points.Count - 1; i++)
        {
            float phi = i * 6.283f * 2;
            uint red = (uint)((0.5f + 0.5f * Math.Sin(phi * 3 + _phase)) * 255);
            uint green = (uint)((0.5f + 0.5f * Math.Sin(phi * 3 + _phase + 2.094f)) * 255);
            uint blue = (uint)((0.5f + 0.5f * Math.Sin(phi * 3 + _phase + 4.188f)) * 255);
            
            uint color = (uint)((0xFF << 24) | (red << 16) | (green << 8) | blue);
            
            canvas.DrawLine(points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, color, 1.0f);
        }
        
        // Draw additional grid lines for more detail
        if (beat)
        {
            uint gridColor = 0xFFFFFF00; // Yellow on beat
            canvas.SetLineWidth(0.5f);
            
            // Draw some vertical and horizontal grid lines
            for (int i = 0; i < points.Count; i += 50)
            {
                if (i < points.Count - 1)
                {
                    canvas.DrawLine(points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, gridColor, 0.5f);
                }
            }
        }
    }

    public void Dispose()
    {
        // Nothing to clean up
    }
}
