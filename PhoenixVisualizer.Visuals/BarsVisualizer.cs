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
            // log-ish scale + clamp
            float v = f.Fft[i];
            float mag = MathF.Min(1f, (float)Math.Log(1 + 8 * Math.Max(0, v)));
            float h = mag * (_h - 10);

            float x = i * barW;
            seg[0] = (x + barW * 0.5f, _h - 5);
            seg[1] = (x + barW * 0.5f, _h - 5 - h);
            canvas.DrawLines(seg, Math.Max(1f, barW * 0.6f), 0xFF40C4FF);
        }
    }

    public void Dispose() { }
}
