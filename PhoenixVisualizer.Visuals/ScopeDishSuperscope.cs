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
        
        // Create points array for the 3D dish
        var points = new System.Collections.Generic.List<(float x, float y)>();
        
        for (int i = 0; i < _numPoints; i++)
        {
            float t = i / (float)_numPoints;
            
            // 3D dish formula from AVS: iz=1.3+sin(r+i*$PI*2)*(v+0.5)*0.88; ix=cos(r+i*$PI*2)*(v+0.5)*.88; iy=-0.3+abs(cos(v*$PI)); x=ix/iz;y=iy/iz;
            float r = _time + i * (float)Math.PI * 2;
            float iz = 1.3f + (float)Math.Sin(r + i * (float)Math.PI * 2) * (volume + 0.5f) * 0.88f;
            float ix = (float)Math.Cos(r + i * (float)Math.PI * 2) * (volume + 0.5f) * 0.88f;
            float iy = -0.3f + Math.Abs((float)Math.Cos(volume * (float)Math.PI));
            
            // Perspective projection
            float x = ix / iz;
            float y = iy / iz;
            
            // Scale and center
            x = x * _width * 0.4f + _width * 0.5f;
            y = y * _height * 0.4f + _height * 0.5f;
            
            points.Add((x, y));
        }
        
        // Draw the 3D dish
        uint color = beat ? 0xFFFF00FF : 0xFF00FF00; // Magenta on beat, green otherwise
        canvas.SetLineWidth(1.0f);
        canvas.DrawLines(points.ToArray(), 1.0f, color);
    }

    public void Dispose()
    {
        // Nothing to clean up
    }
}
