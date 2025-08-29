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
                uint color = i == 0 ? brightGreen : (uint)(0xFF << 24 | (int)(128 * alpha) << 8 | (int)(255 * alpha));

                // Draw rectangle for this segment
                float rectX = x * colWidth + 1;
                float rectY = yy;
                float rectW = colWidth - 2;
                float rectH = seg - 1;

                // Draw filled rectangle
                DrawFilledRect(canvas, rectX, rectY, rectW, rectH, color);
            }

            // Advance column
            float speed = _speed![x] * (0.5f + _amplitude);
            _y[x] = (y + speed * 0.033f) % _height;
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

    private void DrawFilledRect(ISkiaCanvas canvas, float x, float y, float w, float h, uint color)
    {
        // Draw rectangle by drawing four lines (filled)
        for (float yy = y; yy < y + h; yy++)
        {
            canvas.DrawLine(x, yy, x + w, yy, color, 1.0f);
        }
    }
}
