using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

public sealed class LavaLampVisualizer : IVisualizerPlugin
{
    public string Id => "lava_lamp";
    public string DisplayName => "Lava Lamp";

    private sealed class Blob
    {
        public float X, Y;
        public float VX, VY;
        public float R;
    }

    private readonly Random _rng = new();
    private Blob[] _blobs = Array.Empty<Blob>();
    private float _amp;
    private int _width, _height;

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;

        // Init blobs
        int n = 8;
        _blobs = new Blob[n];
        for (int i = 0; i < n; i++)
        {
            _blobs[i] = new Blob
            {
                X = (float)(_rng.NextDouble() * width),
                Y = (float)(_rng.NextDouble() * height),
                VX = (float)((_rng.NextDouble() - 0.5) * 40),
                VY = (float)((_rng.NextDouble() - 0.5) * 40),
                R = Math.Min(width, height) * (0.06f + (float)_rng.NextDouble() * 0.12f)
            };
        }
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Dispose() { }

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        // Clear with dark background gradient
        canvas.Clear(0xFF120606); // Dark reddish background

        // Update amplitude from audio
        _amp = f.Volume;

        // Update and draw blobs
        foreach (var b in _blobs)
        {
            float speed = 1 + _amp * 2;
            b.X = (b.X + b.VX * 0.033f * speed + _width) % _width;
            b.Y = (b.Y + b.VY * 0.033f * speed + _height) % _height;

            // Draw blob with gradient effect
            DrawBlob(canvas, b);
        }
    }

    private void DrawBlob(ISkiaCanvas canvas, Blob blob)
    {
        // Draw multiple circles with decreasing opacity to simulate gradient
        int layers = 8;
        for (int i = 0; i < layers; i++)
        {
            float radius = blob.R * (1.0f - i * 0.1f);
            float alpha = (1.0f - i * 0.1f) * 0.8f;
            uint color = (uint)(0xFF << 24 | (int)(255 * alpha) << 16 | (int)(60 * alpha) << 8 | 0);

            canvas.FillCircle(blob.X, blob.Y, radius, color);
        }
    }
}
