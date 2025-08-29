using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals;

public sealed class BarsVisualizer : IVisualizerPlugin
{
    public string Id => "bars";
    public string DisplayName => "Simple Bars";

    private int _w, _h;

    public void Initialize(int width, int height) { _w = width; _h = height; }
    public void Resize(int width, int height)     { _w = width; _h = height; }

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        canvas.Clear(0xFF101010); // opaque background

        // Debug: Log what we're receiving
        float debugFftSum = f.Fft?.Sum(ff => MathF.Abs(ff)) ?? 0f;
        float debugWaveSum = f.Waveform?.Sum(w => MathF.Abs(w)) ?? 0f;
        System.Diagnostics.Debug.WriteLine($"[BarsVisualizer] Received: FFT sum: {debugFftSum:F6}, Wave sum: {debugWaveSum:F6}, RMS: {f.Rms:F6}, Beat: {f.Beat}");

        if (f.Fft is null || f.Fft.Length == 0) return;

        // Validate FFT data - check if it's stuck
        float fftSum = 0f;
        float fftMax = 0f;
        int fftNonZero = 0;
        
        for (int i = 0; i < f.Fft.Length; i++)
        {
            float absVal = MathF.Abs(f.Fft[i]);
            fftSum += absVal;
            if (absVal > fftMax) fftMax = absVal;
            if (absVal > 0.001f) fftNonZero++;
        }
        
        // If FFT data appears stuck, use a fallback pattern
        if (fftSum < 0.001f || fftMax < 0.001f || fftNonZero < 10)
        {
            // Generate a simple animated pattern instead of stuck data
            var time = DateTime.Now.Ticks / 10000000.0; // Current time in seconds
            for (int i = 0; i < f.Fft.Length; i++)
            {
                f.Fft[i] = MathF.Sin((float)(time * 2.0 + i * 0.1)) * 0.3f;
            }
        }

        int n = Math.Min(64, f.Fft.Length);
        float barW = Math.Max(1f, (float)_w / n);
        Span<(float x, float y)> seg = stackalloc (float, float)[2];

        for (int i = 0; i < n; i++)
        {
            // Proper FFT magnitude calculation (handle negative values correctly)
            float v = MathF.Abs(f.Fft[i]);

            // Improved logarithmic scaling with better sensitivity
            float mag = MathF.Min(1f, MathF.Log(1 + 12 * v) / MathF.Log(13));

            // Scale height with proper screen coordinate system
            float h = mag * (_h * 0.8f); // Use 80% of screen height

            // Calculate bar position with proper centering
            float x = i * barW;
            float barCenterX = x + barW * 0.5f;
            float barBottomY = _h * 0.9f; // Leave 10% margin at bottom
            float barTopY = barBottomY - h;

            // Ensure bars don't go off-screen
            barTopY = MathF.Max(0, barTopY);

            seg[0] = (barCenterX, barBottomY);
            seg[1] = (barCenterX, barTopY);

            // Dynamic bar thickness based on magnitude
            float thickness = MathF.Max(1f, barW * (0.4f + mag * 0.4f));
            canvas.DrawLines(seg, thickness, 0xFF40C4FF);
        }
    }

    public void Dispose() { }
}
