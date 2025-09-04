using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Matrix Rain Visualizer - Classic Matrix-style falling code with audio-reactive effects
/// FIXED: Implemented proper downward movement with enhanced audio reactivity
/// </summary>
public sealed class MatrixRainVisualizer : IVisualizerPlugin
{
    public string Id => "matrix_rain";
    public string DisplayName => "Matrix Rain";

    private readonly Random _rng = new();
    private float[]? _y;
    private float[]? _speed;
    private int _cols;
    private int _desiredCols = 64;
    private float _amplitude;
    private int _width, _height;
    private float _lastFrameTime;

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        InitializeColumns();
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        InitializeColumns();
    }

    public void Dispose() { }

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        // FIXED: Audio-reactive time and animation updates
        var energy = f.Energy;
        var bass = f.Bass;
        var mid = f.Mid;
        var treble = f.Treble;
        var beat = f.Beat;
        var volume = f.Volume;
        
        // Audio-reactive animation speed
        var baseSpeed = 0.016f;
        var energySpeed = energy * 0.02f;
        var trebleSpeed = treble * 0.015f;
        var beatSpeed = beat ? 0.03f : 0f;
        var totalSpeed = baseSpeed + energySpeed + trebleSpeed + beatSpeed;
        
        // Calculate delta time for frame-rate independent animation
        float currentTime = (float)(DateTime.Now.Ticks / 10000000.0); // Current time in seconds
        float deltaTime = _lastFrameTime == 0 ? totalSpeed : Math.Min(currentTime - _lastFrameTime, 0.033f); // Cap at ~30 FPS equivalent
        _lastFrameTime = currentTime;

        // FIXED: Enhanced audio-reactive background
        uint bgColor = 0xFF000000; // Black background
        if (beat)
            bgColor = 0xFF001100; // Slightly green on beat
        else if (bass > 0.5f)
            bgColor = 0xFF110000; // Slightly red for bass
        else if (treble > 0.4f)
            bgColor = 0xFF001111; // Slightly cyan for treble
        else if (energy > 0.6f)
            bgColor = 0xFF111100; // Slightly yellow for energy
            
        canvas.Clear(bgColor);

        // FIXED: Enhanced amplitude calculation
        var baseAmplitude = bass;
        var energyAmplitude = energy * 0.5f;
        var beatAmplitude = beat ? 0.3f : 0f;
        _amplitude = baseAmplitude + energyAmplitude + beatAmplitude;

        // Initialize columns if needed
        if (_cols == 0 || _y == null || _speed == null)
        {
            InitializeColumns();
        }

        // FIXED: Enhanced parameters driven by audio
        float colWidth = _width / (float)_cols;
        float seg = Math.Max(6, _height / 40);
        int maxLen = (int)Math.Clamp(6 + _amplitude * 40, 8, 60);

        // FIXED: Enhanced matrix colors with audio reactivity
        uint brightGreen = 0xFF00FF00;
        if (beat)
            brightGreen = 0xFFFFFF00; // Yellow on beat
        else if (bass > 0.5f)
            brightGreen = 0xFFFF0000; // Red for bass
        else if (treble > 0.4f)
            brightGreen = 0xFF00FFFF; // Cyan for treble
        else if (energy > 0.6f)
            brightGreen = 0xFFFF00FF; // Magenta for energy

        for (int x = 0; x < _cols; x++)
        {
            float y = _y![x];

            // FIXED: Enhanced vertical tail drawing
            for (int i = 0; i < maxLen; i++)
            {
                float yy = (y - i * seg + _height) % _height;
                float alpha = 1.0f - i / (float)maxLen;
                uint color = i == 0 ? brightGreen : (uint)(0xFF << 24 | (int)(68 * alpha) << 8 | (int)(136 * alpha));

                // Draw rectangle for this segment using efficient fill
                float rectX = x * colWidth + 1;
                float rectY = yy;
                float rectW = colWidth - 2;
                float rectH = seg - 1;

                // Use FillRect instead of inefficient line-by-line drawing
                canvas.FillRect(rectX, rectY, rectW, rectH, color);
            }

            // FIXED: Enhanced column advancement with proper downward movement
            var columnBaseSpeed = _speed![x];
            var columnEnergySpeed = energy * 100f;
            var columnBeatSpeed = beat ? 50f : 0f;
            var columnTotalSpeed = columnBaseSpeed + columnEnergySpeed + columnBeatSpeed;
            _y[x] = (y + columnTotalSpeed * deltaTime) % _height;
        }
    }

    private void InitializeColumns()
    {
        _cols = Math.Clamp(_desiredCols, 16, 160);
        _y = new float[_cols];
        _speed = new float[_cols];

        for (int i = 0; i < _cols; i++)
        {
            _y[i] = (float)_rng.NextDouble() * _height;
            _speed[i] = 40 + (float)_rng.NextDouble() * 120;
        }
    }


}
