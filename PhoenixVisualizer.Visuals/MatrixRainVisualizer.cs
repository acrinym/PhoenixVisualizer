using PhoenixVisualizer.PluginHost;
using System;

namespace PhoenixVisualizer.Visuals;

public struct Column
{
    public float X, Y;
    public int Size;
    public char Char;
}

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
    private Column[] _columns = new Column[64];

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

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        canvas.Clear(0xFF000000);
        float speed = 2.0f + features.Volume * 12.0f + (features.Beat ? 6.0f : 0f);

        for (int c = 0; c < _columns.Length; c++)
        {
            var col = _columns[c];
            col.Y += speed;
            if (col.Y > _height + 40)
            {
                col.Y = -Random.Shared.Next(0, 120);
                col.Size = Random.Shared.Next(10, 28);
                col.Char = (char)Random.Shared.Next(0x30, 0x7A);
            }

            // head
            canvas.DrawText(col.Char.ToString(), col.X, col.Y, 0xFFFFFFFF, col.Size);
            // tail
            for (int t = 1; t <= 8; t++)
                canvas.DrawText(col.Char.ToString(), col.X, col.Y - t * col.Size, 0x66A0FFA0, col.Size);

            _columns[c] = col;
        }

    }

    private void InitializeColumns()
    {
        _cols = Math.Clamp(_desiredCols, 16, 160);
        _y = new float[_cols];
        _speed = new float[_cols];
        _columns = new Column[_cols];

        for (int i = 0; i < _cols; i++)
        {
            _y[i] = (float)_rng.NextDouble() * _height;
            _speed[i] = 40 + (float)_rng.NextDouble() * 120;
            _columns[i] = new Column
            {
                X = i * (_width / (float)_cols),
                Y = (float)_rng.NextDouble() * _height,
                Size = _rng.Next(10, 28),
                Char = (char)_rng.Next(0x30, 0x7A)
            };
        }
    }


}
