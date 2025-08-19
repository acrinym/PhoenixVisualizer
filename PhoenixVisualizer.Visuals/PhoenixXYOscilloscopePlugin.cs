using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

public sealed class PhoenixXYOscilloscopePlugin : IVisualizerPlugin
{
    public string Id => "phoenix_xy_oscilloscope";
    public string DisplayName => "🔥 Phoenix XY Oscilloscope";
    public string Description => "Classic Lissajous patterns with left/right channel mapping and Phoenix fire colors";
    public bool IsEnabled { get; set; } = true;

    private int _w, _h;
    private readonly int _bufferSize = 1024;
    private readonly float[] _leftBuffer;
    private readonly float[] _rightBuffer;
    private int _bufferIndex = 0;
    private float _time = 0f;

    // Phoenix color palette (NO GREEN!)
    private readonly uint[] _fireColors = new uint[]
    {
        0xFFFF4400, // Hot red-orange
        0xFFFF6600, // Bright orange
        0xFFFF8800, // Warm orange
        0xFFFFAA00, // Golden orange
        0xFFFFCC00, // Bright yellow
        0xFFFFEE00, // Light yellow
        0xFFFFFFFF  // White
    };

    public PhoenixXYOscilloscopePlugin()
    {
        _leftBuffer = new float[_bufferSize];
        _rightBuffer = new float[_bufferSize];
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
        // Clear with dark background
        canvas.Clear(0xFF000000);

        // Update time
        _time += 0.02f;

        // Get audio data - use waveform data for left/right simulation
        var leftChannel = features.Waveform?.Length > 0 ? features.Waveform[features.Waveform.Length / 2] : 0f;
        var rightChannel = features.Waveform?.Length > 0 ? features.Waveform[features.Waveform.Length / 2 + 1] : 0f;
        var beat = features.Beat;
        var bass = features.Bass;

        // Update circular buffers
        _leftBuffer[_bufferIndex] = leftChannel;
        _rightBuffer[_bufferIndex] = rightChannel;
        _bufferIndex = (_bufferIndex + 1) % _bufferSize;

        var centerX = _w / 2f;
        var centerY = _h / 2f;
        var scale = Math.Min(_w, _h) * 0.35f;

        // Draw Lissajous pattern
        var points = new System.Collections.Generic.List<(float x, float y)>();
        
        for (int i = 0; i < _bufferSize; i++)
        {
            var index = (_bufferIndex - i + _bufferSize) % _bufferSize;
            var left = _leftBuffer[index];
            var right = _rightBuffer[index];
            
            // Map audio to screen coordinates
            var x = centerX + left * scale;
            var y = centerY + right * scale;
            
            points.Add((x, y));
        }

        // Draw the main Lissajous pattern
        var lineWidth = beat ? 3f : 1.5f; // Beat-pulsed line width
        canvas.SetLineWidth(lineWidth);

        // Draw with rainbow colors based on position
        for (int i = 0; i < points.Count - 1; i++)
        {
            var t = i / (float)points.Count;
            var color = GetRainbowColor(t, _time);
            
            // Add alpha based on audio intensity
            var intensity = Math.Abs(_leftBuffer[i]) + Math.Abs(_rightBuffer[i]);
            var alpha = (byte)(Math.Min(255, intensity * 400));
            color = (color & 0x00FFFFFF) | ((uint)alpha << 24);
            
            canvas.DrawLine(points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, color, lineWidth);
        }

        // Draw center crosshair
        var crosshairColor = beat ? 0xFFFF4400 : 0x44FFFFFF;
        var crosshairSize = 20f;
        canvas.SetLineWidth(1f);
        canvas.DrawLine(centerX - crosshairSize, centerY, centerX + crosshairSize, centerY, crosshairColor, 1f);
        canvas.DrawLine(centerX, centerY - crosshairSize, centerX, centerY + crosshairSize, crosshairColor, 1f);

        // Draw center circle
        var circleRadius = 5f + bass * 15f;
        var circleColor = (uint)(((byte)(bass * 150)) << 24 | 0xFFFF4400);
        canvas.FillCircle(centerX, centerY, circleRadius, circleColor);

        // Draw outer boundary circle
        canvas.DrawCircle(centerX, centerY, scale, 0x22FFFFFF, false);

        // Add sparkle effects on strong beats
        if (beat && bass > 0.5f)
        {
            for (int i = 0; i < 8; i++)
            {
                var angle = i * Math.PI * 2f / 8f + _time;
                var sparkleX = centerX + (float)Math.Cos(angle) * (scale + 20f);
                var sparkleY = centerY + (float)Math.Sin(angle) * (scale + 20f);
                var sparkleColor = GetRainbowColor(i / 8f, _time);
                canvas.FillCircle(sparkleX, sparkleY, 3f, sparkleColor);
            }
        }

        // Draw frequency response rings
        if (features.Fft?.Length > 0)
        {
            var numRings = 3;
            for (int ring = 1; ring <= numRings; ring++)
            {
                var ringRadius = scale * ring / numRings;
                var ringAlpha = (byte)(50 - ring * 15);
                var ringColor = (uint)(ringAlpha << 24 | 0xFFFFFF);
                canvas.DrawCircle(centerX, centerY, ringRadius, ringColor, false);
            }
        }
    }

    private uint GetRainbowColor(float t, float time)
    {
        // Create smooth rainbow color cycling
        var hue = (t + time * 0.5f) * 6.283f; // 2π
        var red = (uint)((0.5f + 0.5f * Math.Sin(hue)) * 255);
        var green = (uint)((0.5f + 0.5f * Math.Sin(hue + 2.094f)) * 255); // +2π/3
        var blue = (uint)((0.5f + 0.5f * Math.Sin(hue + 4.188f)) * 255); // +4π/3
        
        // Ensure no green (Phoenix constraint)
        if (green > red && green > blue)
        {
            green = (uint)(Math.Max(red, blue) * 0.7f);
        }
        
        return (uint)((0xFF << 24) | (red << 16) | (green << 8) | blue);
    }
}
