using PhoenixVisualizer.PluginHost;
using System;
using System.Diagnostics;
using System.Linq;

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Debug visualizer to test audio data flow
/// </summary>
public sealed class AudioDebugVisualizer : IVisualizerPlugin
{
    public string Id => "audio_debug";
    public string DisplayName => "Audio Debug";

    private int _w, _h;
    private int _frameCount = 0;
    private DateTime _lastLogTime = DateTime.Now;

    public void Initialize(int width, int height)
    {
        _w = width;
        _h = height;
        Debug.WriteLine($"[AudioDebugVisualizer] Initialized: {width}x{height}");
    }

    public void Resize(int width, int height)
    {
        _w = width;
        _h = height;
    }

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        _frameCount++;
        var now = DateTime.Now;
        
        // Log every 60 frames (about once per second at 60fps)
        if (_frameCount % 60 == 0)
        {
            var timeSinceLastLog = (now - _lastLogTime).TotalMilliseconds;
            
            Debug.WriteLine($"[AudioDebugVisualizer] Frame {_frameCount} - Audio Data Analysis:");
            Debug.WriteLine($"  FFT: {(f.Fft?.Length ?? 0)} samples, Sum: {(f.Fft?.Sum(x => Math.Abs(x)) ?? 0):F6}");
            Debug.WriteLine($"  Waveform: {(f.Waveform?.Length ?? 0)} samples, Sum: {(f.Waveform?.Sum(x => Math.Abs(x)) ?? 0):F6}");
            Debug.WriteLine($"  Volume: {f.Volume:F6}, RMS: {f.Rms:F6}");
            Debug.WriteLine($"  Bass: {f.Bass:F6}, Mid: {f.Mid:F6}, Treble: {f.Treble:F6}");
            Debug.WriteLine($"  Beat: {f.Beat}, BPM: {f.Bpm:F1}");
            Debug.WriteLine($"  Time: {f.TimeSeconds:F3}s");
            Debug.WriteLine($"  Energy: {f.Energy:F6}, Peak: {f.Peak:F6}");
            
            _lastLogTime = now;
        }

        // Clear canvas
        canvas.Clear(0xFF000000);

        // Draw audio data visualization
        DrawAudioDataVisualization(canvas, f);
    }

    private void DrawAudioDataVisualization(ISkiaCanvas canvas, AudioFeatures f)
    {
        // Draw a semi-transparent background so UI is still visible
        canvas.FillRect(0, 0, _w, _h, 0x80000000); // Semi-transparent black
        
        // Draw FFT data as bars in bottom third
        if (f.Fft != null && f.Fft.Length > 0)
        {
            DrawFFTBars(canvas, f.Fft);
        }
        else
        {
            // Draw "NO FFT DATA" message in small text
            canvas.DrawText("NO FFT DATA", 10, _h - 60, 0xFFFF0000, 12f);
        }

        // Draw waveform data in middle third
        if (f.Waveform != null && f.Waveform.Length > 0)
        {
            DrawWaveform(canvas, f.Waveform);
        }
        else
        {
            // Draw "NO WAVEFORM DATA" message in small text
            canvas.DrawText("NO WAVEFORM DATA", 10, _h - 40, 0xFFFF0000, 12f);
        }

        // Draw audio properties in top area with smaller text
        DrawAudioProperties(canvas, f);
    }

    private void DrawFFTBars(ISkiaCanvas canvas, float[] fft)
    {
        int numBars = Math.Min(64, fft.Length);
        float barWidth = (float)_w / numBars;
        float maxHeight = _h * 0.3f;

        for (int i = 0; i < numBars; i++)
        {
            float magnitude = Math.Abs(fft[i]);
            float barHeight = magnitude * maxHeight * 10f; // Scale up for visibility
            float x = i * barWidth;
            float y = _h * 0.7f - barHeight;

            uint color = magnitude > 0.01f ? 0xFF00FF00 : 0xFF444444;
            canvas.FillRect(x, y, barWidth - 1, barHeight, color);
        }
    }

    private void DrawWaveform(ISkiaCanvas canvas, float[] waveform)
    {
        if (waveform.Length < 2) return;

        int numPoints = Math.Min(512, waveform.Length);
        float pointSpacing = (float)_w / numPoints;
        float centerY = _h * 0.5f;
        float amplitude = 50f;

        var points = new (float x, float y)[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            float x = i * pointSpacing;
            float y = centerY + waveform[i] * amplitude;
            points[i] = (x, y);
        }

        canvas.DrawLines(points, 2f, 0xFF00FFFF);
    }

    private void DrawAudioProperties(ISkiaCanvas canvas, AudioFeatures f)
    {
        float startY = 10f;
        float lineHeight = 15f;
        int line = 0;

        void DrawProperty(string name, object value, uint color = 0xFFFFFFFF)
        {
            string text = $"{name}: {value}";
            canvas.DrawText(text, 10, startY + line * lineHeight, color, 10f);
            line++;
        }

        // Only show key properties to keep it compact
        DrawProperty("Vol", f.Volume.ToString("F2"));
        DrawProperty("Bass", f.Bass.ToString("F2"));
        DrawProperty("Mid", f.Mid.ToString("F2"));
        DrawProperty("Treble", f.Treble.ToString("F2"));
        DrawProperty("Beat", f.Beat ? "YES" : "NO", f.Beat ? 0xFF00FF00 : 0xFFFF0000);
        DrawProperty("BPM", f.Bpm.ToString("F0"));
    }

    public void Dispose() { }
}
