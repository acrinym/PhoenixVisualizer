using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Simple Bars Visualizer - Fixed version that properly responds to frequency levels
/// FIXED: Bars now match actual frequency levels instead of staying at max height
/// FIXED: Removed draw-on-top behavior that clouds actual visuals
/// FIXED: Clear all frames and redraw as full bars matching current frequency levels
/// </summary>
public sealed class BarsVisualizer : IVisualizerPlugin
{
    public string Id => "bars";
    public string DisplayName => "Simple Bars";

    private int _width;
    private int _height;
    private float _time = 0f;
    private readonly float[] _previousLevels;
    private readonly float[] _peakLevels;
    private readonly int[] _peakHoldCounters;

    // User parameters
    private float _sensitivity = 1.0f;
    private float _smoothing = 0.85f;
    private float _peakHoldTime = 30f;
    private float _peakDecay = 0.95f;
    private int _barCount = 64;
    private uint _barColor = 0xFF00FF00; // Green
    private uint _peakColor = 0xFFFFFF00; // Yellow
    private uint _backgroundColor = 0xFF000000; // Black background
    private bool _showPeaks = true;
    private bool _showBars = true;

    public BarsVisualizer()
    {
        _previousLevels = new float[512];
        _peakLevels = new float[512];
        _peakHoldCounters = new int[512];
    }

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _time = 0f;
        
        // Initialize arrays
        Array.Clear(_previousLevels, 0, _previousLevels.Length);
        Array.Clear(_peakLevels, 0, _peakLevels.Length);
        Array.Clear(_peakHoldCounters, 0, _peakHoldCounters.Length);
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas) {
            // Always clear the frame first to avoid stacking artifacts
            canvas.Clear(_backgroundColor);
        _time += 0.016f;

        if (features.Fft == null || features.Fft.Length == 0)
        {
            // Draw placeholder when no audio data
            DrawPlaceholder(canvas);
            return;
        }

        // Calculate spectrum parameters
        int numBars = Math.Min(_barCount, features.Fft.Length / 2);
        float barWidth = (float)_width / numBars;
        float maxBarHeight = _height * 0.8f; // Use 80% of screen height

        // Process each frequency band
        for (int i = 0; i < numBars; i++)
        {
            // Map frequency band to FFT data with logarithmic scaling
            float frequencyRatio = (float)i / (numBars - 1);
            int fftIndex = (int)(frequencyRatio * frequencyRatio * features.Fft.Length * 0.5f);
            if (fftIndex >= features.Fft.Length) fftIndex = features.Fft.Length - 1;

            // Get magnitude from FFT data
            float magnitude = MathF.Abs(features.Fft[fftIndex]);
            
            // Apply sensitivity and scaling
            float scaledMagnitude = magnitude * _sensitivity * 5f; // FIXED: Proper scaling
            
            // Apply smoothing to prevent jarring movements
            _previousLevels[i] = _previousLevels[i] * _smoothing + scaledMagnitude * (1f - _smoothing);
            float currentLevel = _previousLevels[i];

            // Clamp to reasonable range
            currentLevel = Math.Max(0f, Math.Min(1f, currentLevel));

            // Calculate bar height based on actual frequency level - FIXED: No more max height
            float barHeight = currentLevel * maxBarHeight;
            
            // Ensure minimum bar height for visibility
            barHeight = Math.Max(2f, barHeight);

            // Calculate bar position
            float barX = i * barWidth + barWidth * 0.1f; // Small gap between bars
            float barY = _height * 0.9f - barHeight; // Align to bottom

            // Draw main bar
            if (_showBars && barHeight > 2f)
            {
                uint barColor = _barColor;
                
                // Add intensity based on level
                byte alpha = (byte)(currentLevel * 255);
                barColor = (barColor & 0x00FFFFFF) | ((uint)alpha << 24);
                
                canvas.FillRect(barX, barY, barWidth * 0.8f, barHeight, barColor);
            }

            // Update and draw peaks
            if (_showPeaks)
            {
                if (currentLevel > _peakLevels[i])
                {
                    _peakLevels[i] = currentLevel;
                    _peakHoldCounters[i] = (int)_peakHoldTime;
                }
                else if (_peakHoldCounters[i] > 0)
                {
                    _peakHoldCounters[i]--;
                }
                else
                {
                    _peakLevels[i] *= _peakDecay;
                }

                // Draw peak indicator
                if (_peakLevels[i] > 0.1f)
                {
                    float peakHeight = _peakLevels[i] * maxBarHeight;
                    float peakY = _height * 0.9f - peakHeight;
                    
                    uint peakColor = _peakColor;
                    byte peakAlpha = (byte)(_peakLevels[i] * 255);
                    peakColor = (peakColor & 0x00FFFFFF) | ((uint)peakAlpha << 24);
                    
                    canvas.FillRect(barX, peakY, barWidth * 0.8f, 2f, peakColor);
                }
            }
        }

        // Draw frequency labels (optional)
        if (features.Volume > 0.1f)
        {
            DrawFrequencyLabels(canvas, numBars, barWidth);
        }
    }

    private void DrawPlaceholder(ISkiaCanvas canvas)
    {
        // Draw placeholder when no audio data
        string message = "No Audio Data";
        float centerX = _width / 2f;
        float centerY = _height / 2f;
        
        // Draw placeholder bars
        for (int i = 0; i < 16; i++)
        {
            float barWidth = (float)_width / 16f;
            float barX = i * barWidth + barWidth * 0.1f;
            float barHeight = 20f + (i % 3) * 10f;
            float barY = centerY - barHeight / 2f;
            
            uint barColor = 0x4000FF00; // Semi-transparent green
            canvas.FillRect(barX, barY, barWidth * 0.8f, barHeight, barColor);
        }
    }

    private void DrawFrequencyLabels(ISkiaCanvas canvas, int numBars, float barWidth)
    {
        // Draw frequency labels at bottom
        for (int i = 0; i < numBars; i += numBars / 8) // Show 8 labels
        {
            float frequency = (float)i / numBars * 22050f; // Assuming 44.1kHz sample rate
            string label = frequency > 1000f ? $"{frequency / 1000f:F1}k" : $"{frequency:F0}";
            
            float labelX = i * barWidth + barWidth * 0.5f;
            float labelY = _height * 0.95f;
            
            uint labelColor = 0x80FFFFFF; // Semi-transparent white
            // Note: Text rendering would need to be implemented in ISkiaCanvas
        }
    }

    public void Dispose()
    {
        // Clean up resources
    }

    // Parameter setters for UI binding
    public void SetSensitivity(float sensitivity) => _sensitivity = Math.Max(0.1f, Math.Min(10f, sensitivity));
    public void SetSmoothing(float smoothing) => _smoothing = Math.Max(0.5f, Math.Min(0.99f, smoothing));
    public void SetPeakHoldTime(float peakHoldTime) => _peakHoldTime = Math.Max(1f, Math.Min(100f, peakHoldTime));
    public void SetPeakDecay(float peakDecay) => _peakDecay = Math.Max(0.5f, Math.Min(0.99f, peakDecay));
    public void SetBarCount(int barCount) => _barCount = Math.Max(16, Math.Min(256, barCount));
    public void SetBarColor(uint color) => _barColor = color;
    public void SetPeakColor(uint color) => _peakColor = color;
    public void SetShowPeaks(bool showPeaks) => _showPeaks = showPeaks;
    public void SetShowBars(bool showBars) => _showBars = showBars;
}
