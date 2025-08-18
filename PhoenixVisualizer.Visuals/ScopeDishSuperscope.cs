using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// 3D Scope Dish superscope visualization based on AVS superscope code
/// </summary>
public sealed class ScopeDishSuperscope : IVisualizerPlugin
{
    public string Id => "scope_dish_superscope";
    public string DisplayName => "3D Scope Dish";

    private int _width;
    private int _height;
    private float _time;
    private int _numPoints = 200;

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
        
        // Get audio data
        float volume = features.Volume;
        bool beat = features.Beat;

        // Advance time for animation
        _time += 0.02f;
        
        // Create points array for the 3D dish
        var points = new System.Collections.Generic.List<(float x, float y)>();
        
        for (int i = 0; i < _numPoints; i++)
        {
            float t = i / (float)(_numPoints - 1);
            
            // 3D dish formula (fixed):
            // r varies smoothly around the circle for each point; use t, not i twice
            float r = _time + t * ((float)Math.PI * 2f);
            float v = 0.3f + volume * 0.7f; // radius influenced by volume
            float iz = 1.3f + (float)Math.Sin(r) * (v + 0.5f) * 0.88f;
            float ix = (float)Math.Cos(r) * (v + 0.5f) * 0.88f;
            float iy = -0.3f + Math.Abs((float)Math.Cos(t * (float)Math.PI));
            
            // Perspective projection
            float x = ix / iz;
            float y = iy / iz;
            
            // Scale and center
            x = x * _width * 0.4f + _width * 0.5f;
            y = y * _height * 0.4f + _height * 0.5f;
            
            points.Add((x, y));
        }
        
        // Draw the 3D dish
        // Phoenix-friendly colors (avoid green)
        uint color = beat ? 0xFFFF55AA : 0xFFFFAA33; // Magenta-orange blend on beat, amber otherwise
        canvas.SetLineWidth(1.0f);
        canvas.DrawLines(points.ToArray(), 1.0f, color);
    }

    public void Dispose()
    {
        // Nothing to clean up
    }
}
