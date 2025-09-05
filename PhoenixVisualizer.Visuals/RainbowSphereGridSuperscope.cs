using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Rainbow Sphere Grid superscope visualization based on AVS superscope code
/// FIXED: Now beat/frequency reactive grid instead of just random color spinner
/// </summary>
public sealed class RainbowSphereGridSuperscope : IVisualizerPlugin
{
    public string Id => "rainbow_sphere_grid_superscope";
    public string DisplayName => "Rainbow Sphere Grid";

    private int _width;
    private int _height;
    private float _time;
    private float _phase;
    private readonly int _numPoints = 700;

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
        
        // FIXED: Audio-reactive time and phase updates
        var energy = features.Energy;
        var bass = features.Bass;
        var mid = features.Mid;
        var treble = features.Treble;
        var beat = features.Beat;
        var volume = features.Volume;
        
        // Audio-reactive animation speed
        var baseSpeed = 0.04f;
        var energySpeed = energy * 0.08f;
        var beatSpeed = beat ? 0.15f : 0f;
        _time += baseSpeed + energySpeed + beatSpeed;
        
        // Audio-reactive phase changes
        var basePhase = 0.02f;
        var bassPhase = bass * 0.05f;
        var treblePhase = treble * 0.03f;
        _phase += basePhase + bassPhase + treblePhase;
        
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
            
            // FIXED: Audio-reactive grid distortion
            var bassDistortion = bass * 0.3f;
            var trebleDistortion = treble * 0.2f;
            var energyDistortion = energy * 0.15f;
            
            float g = (0.1f + bassDistortion) * ((float)Math.Sin(phi * 6 + _phase) + (float)Math.Sin(theta * 6 + _phase));
            g += trebleDistortion * (float)Math.Sin(phi * 12 + _phase * 2);
            g += energyDistortion * (float)Math.Sin(theta * 8 + _phase * 1.5f);
            
            xs += g * xs;
            ys += g * ys;
            
            // Apply perspective projection
            float pers = 1.0f / (1.0f + zs);
            float x = xs * pers;
            float y = ys * pers;
            
            // FIXED: Audio-reactive scaling and centering
            var baseScale = 0.4f;
            var bassScale = bass * 0.2f;
            var energyScale = energy * 0.15f;
            var beatScale = beat ? 0.1f : 0f;
            var totalScale = baseScale + bassScale + energyScale + beatScale;
            
            x = x * _width * totalScale + _width * 0.5f;
            y = y * _height * totalScale + _height * 0.5f;
            
            points.Add((x, y));
        }
        
        // Draw the sphere grid with rainbow colors
        canvas.SetLineWidth(1.0f);
        
        // FIXED: Audio-reactive color generation
        for (int i = 0; i < points.Count - 1; i++)
        {
            float phi = i * 6.283f * 2;
            
            // Audio-reactive color modulation
            var bassMod = bass * 0.3f;
            var trebleMod = treble * 0.2f;
            var energyMod = energy * 0.25f;
            var beatMod = beat ? 0.4f : 0f;
            
            // Dynamic phase based on audio
            var dynamicPhase = _phase + bassMod + trebleMod + energyMod + beatMod;
            
            uint red = (uint)((0.5f + 0.5f * Math.Sin(phi * 3 + dynamicPhase)) * 255);
            uint green = (uint)((0.5f + 0.5f * Math.Sin(phi * 3 + dynamicPhase + 2.094f)) * 255);
            uint blue = (uint)((0.5f + 0.5f * Math.Sin(phi * 3 + dynamicPhase + 4.188f)) * 255);
            
            // Audio-reactive brightness
            var brightness = 0.7f + energy * 0.3f + beatMod;
            red = (uint)(red * brightness);
            green = (uint)(green * brightness);
            blue = (uint)(blue * brightness);
            
            uint color = (uint)((0xFF << 24) | (red << 16) | (green << 8) | blue);
            
            // Audio-reactive line thickness
            var lineThickness = 1.0f + bass * 0.5f + beatMod;
            canvas.DrawLine(points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, color, lineThickness);
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
