using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

public sealed class PhoenixWaterfallPlugin : IVisualizerPlugin
{
    public string Id => "phoenix_waterfall";
    public string DisplayName => "Phoenix Waterfall";
    public string Description => "Classic Winamp-style scrolling spectrogram with Phoenix fire colors";
    public bool IsEnabled { get; set; } = true;

    private readonly float[,] _waterfallBuffer;
    private readonly int _bufferHeight = 128;  // Reduced from 256 for performance
    private readonly int _maxFftBins = 256;   // Reduced from 512 for performance
    private int _w, _h;
    private int _frameCount = 0;
    private const int RENDER_EVERY_N_FRAMES = 2; // Skip every other frame for performance

    // Phoenix color palette (NO GREEN!)
    private readonly uint[] _fireColors = new uint[]
    {
        0xFFFF4400, // Hot red-orange (bass)
        0xFFFF6600, // Bright orange
        0xFFFF8800, // Warm orange
        0xFFFFAA00, // Golden orange
        0xFFFFCC00, // Bright yellow
        0xFFFFEE00, // Light yellow
        0xFFFFFFFF  // White (treble sparkles)
    };

    public PhoenixWaterfallPlugin()
    {
        _waterfallBuffer = new float[_maxFftBins, _bufferHeight];
    }

    public void Initialize() { }
    public void Initialize(int width, int height) { _w = width; _h = height; }
    public void Resize(int width, int height) { _w = width; _h = height; }
    public void Shutdown() { }
    public void ProcessFrame(AudioFeatures features, ISkiaCanvas canvas) { RenderFrame(features, canvas); }
    public void Configure() { }
    public void Dispose() { }

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        if (features.Fft?.Length == 0) return;

        // Always update the buffer for smooth scrolling
        UpdateWaterfallBuffer(features);

        // Skip rendering every other frame for performance
        _frameCount++;
        if (_frameCount % RENDER_EVERY_N_FRAMES != 0)
        {
            return;
        }

        // Render the Phoenix waterfall with optimized drawing
        RenderWaterfallOptimized(canvas, features);

        // Optional: Add Phoenix flame effects on strong bass hits (less frequently)
        if (features.Bass > 0.7f && _frameCount % 4 == 0)
        {
            AddPhoenixFlameEffect(canvas, features);
        }
    }

    private void UpdateWaterfallBuffer(AudioFeatures features)
    {
        // Scroll buffer down (classic waterfall effect)
        for (int y = _bufferHeight - 1; y > 0; y--)
        {
            for (int x = 0; x < Math.Min(features.Fft.Length, _maxFftBins); x++)
            {
                _waterfallBuffer[x, y] = _waterfallBuffer[x, y - 1];
            }
        }

        // Write new FFT row at top with Phoenix energy mapping
        for (int x = 0; x < Math.Min(features.Fft.Length, _maxFftBins); x++)
        {
            var magnitude = MathF.Min(1f, features.Fft[x] * 4f); // Boost sensitivity
            
            // Phoenix energy mapping: bass = more intense, treble = more sparkly
            var bassBoost = x < 8 ? 1.5f : 1.0f; // Bass gets extra punch
            var trebleSparkle = x > features.Fft.Length / 2 ? 1.2f : 1.0f; // Treble sparkles
            
            magnitude *= bassBoost * trebleSparkle;
            _waterfallBuffer[x, 0] = magnitude;
        }
    }

    private void RenderWaterfallOptimized(ISkiaCanvas canvas, AudioFeatures features)
    {
        var cellWidth = _w / (float)Math.Min(features.Fft.Length, _maxFftBins);
        var cellHeight = _h / (float)_bufferHeight;

        // Use larger cells for better performance
        var renderCellWidth = Math.Max(cellWidth, 2f);
        var renderCellHeight = Math.Max(cellHeight, 2f);

        // Skip rendering very quiet areas and use larger cells
        for (int y = 0; y < _bufferHeight; y += 2) // Skip every other row
        {
            for (int x = 0; x < Math.Min(features.Fft.Length, _maxFftBins); x += 2) // Skip every other column
            {
                var intensity = _waterfallBuffer[x, y];
                if (intensity < 0.1f) continue; // Higher threshold for skipping

                // Phoenix color mapping based on intensity and frequency
                var color = GetPhoenixColor(intensity, x, features.Fft.Length);
                
                // Add alpha based on intensity for depth
                var alpha = (byte)(intensity * 255);
                color = (color & 0x00FFFFFF) | ((uint)alpha << 24);

                // Draw larger cells for better performance
                canvas.DrawRect(x * cellWidth, y * cellHeight, renderCellWidth, renderCellHeight, color, true);
            }
        }
    }

    private uint GetPhoenixColor(float intensity, int binIndex, int totalBins)
    {
        // Map frequency to color: bass = red/orange, treble = yellow/white
        var frequencyRatio = (float)binIndex / totalBins;
        
        if (frequencyRatio < 0.3f) // Bass frequencies
        {
            // Red to orange gradient
            var t = frequencyRatio / 0.3f;
            return InterpolateColor(_fireColors[0], _fireColors[2], t);
        }
        else if (frequencyRatio < 0.7f) // Mid frequencies
        {
            // Orange to yellow gradient
            var t = (frequencyRatio - 0.3f) / 0.4f;
            return InterpolateColor(_fireColors[2], _fireColors[4], t);
        }
        else // Treble frequencies
        {
            // Yellow to white gradient
            var t = (frequencyRatio - 0.7f) / 0.3f;
            return InterpolateColor(_fireColors[4], _fireColors[6], t);
        }
    }

    private uint InterpolateColor(uint color1, uint color2, float t)
    {
        var r1 = (color1 >> 16) & 0xFF;
        var g1 = (color1 >> 8) & 0xFF;
        var b1 = color1 & 0xFF;
        
        var r2 = (color2 >> 16) & 0xFF;
        var g2 = (color2 >> 8) & 0xFF;
        var b2 = color2 & 0xFF;

        var r = (byte)(r1 + (r2 - r1) * t);
        var g = (byte)(g1 + (g2 - g1) * t);
        var b = (byte)(b1 + (b2 - b1) * t);

        return (uint)((r << 16) | (g << 8) | b);
    }

    private void AddPhoenixFlameEffect(ISkiaCanvas canvas, AudioFeatures features)
    {
        // Add subtle flame wisps on strong bass hits
        var centerX = _w / 2f;
        var centerY = _h * 0.8f; // Near bottom
        
        for (int i = 0; i < 3; i++)
        {
            var offset = (i - 1) * 20f;
            var flameColor = _fireColors[i % _fireColors.Length];
            var alpha = (byte)(features.Bass * 100); // Bass-driven alpha
            flameColor = (flameColor & 0x00FFFFFF) | ((uint)alpha << 24);
            
            canvas.DrawCircle(centerX + offset, centerY, 3f, flameColor);
        }
    }
}
