using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

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
        // Calculate delta time for frame-rate independent animation
        float currentTime = (float)(DateTime.Now.Ticks / 10000000.0); // Current time in seconds
        float deltaTime = _lastFrameTime == 0 ? 0.016f : Math.Min(currentTime - _lastFrameTime, 0.033f); // Cap at ~30 FPS equivalent
        _lastFrameTime = currentTime;

        // Clear with black background
        canvas.Clear(0xFF000000);

        // Update amplitude from bass
        _amplitude = f.Bass;

        // Initialize columns if needed
        if (_cols == 0 || _y == null || _speed == null)
        {
            InitializeColumns();
        }

        // Parameters driven by amplitude
        float colWidth = _width / (float)_cols;
        float seg = Math.Max(6, _height / 40);
        int maxLen = (int)Math.Clamp(6 + _amplitude * 40, 8, 60);

        // Green matrix colors
        uint brightGreen = 0xFF00FF00;

        for (int x = 0; x < _cols; x++)
        {
            float y = _y![x];

            // Draw vertical tail
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

            // Advance column with proper delta time
            float speed = _speed![x] * (0.5f + _amplitude);
            _y[x] = (y + speed * deltaTime) % _height;
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
