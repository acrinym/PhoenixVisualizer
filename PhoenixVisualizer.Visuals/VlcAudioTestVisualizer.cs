using PhoenixVisualizer.PluginHost;
using System;
using System.Diagnostics;
using System.Linq; // Added for .Sum() and .Max()
using System.Collections.Generic; // Added for .Count()

namespace PhoenixVisualizer.Visuals;

/// <summary>
/// Test visualizer for debugging VLC audio data flow
/// Shows raw VLC audio buffer vs processed visualizer data side by side
/// </summary>
public sealed class VlcAudioTestVisualizer : IVisualizerPlugin
{
    public string Id => "vlc_audio_test";
    public string DisplayName => "VLC Audio Test Debug";

    private int _w, _h;
    private readonly float[] _lastRawAudio = new float[2048];
    private readonly float[] _lastProcessedFft = new float[2048];
    private readonly float[] _lastProcessedWaveform = new float[2048];
    private readonly DateTime _lastUpdate = DateTime.Now;
    private int _frameCount = 0;

    public void Initialize(int width, int height) 
    { 
        _w = width; 
        _h = height;
        Debug.WriteLine($"[VlcAudioTestVisualizer] Initialized with dimensions: {width}x{height}");
    }
    
    public void Resize(int width, int height) 
    { 
        _w = width; 
        _h = height;
        Debug.WriteLine($"[VlcAudioTestVisualizer] Resized to: {width}x{height}");
    }

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        _frameCount++;
        var now = DateTime.Now;
        var timeSinceLastUpdate = (now - _lastUpdate).TotalMilliseconds;
        
        // Store current data for comparison
        if (f.Fft != null && f.Fft.Length > 0)
        {
            Array.Copy(f.Fft, _lastProcessedFft, Math.Min(f.Fft.Length, _lastProcessedFft.Length));
        }
        if (f.Waveform != null && f.Waveform.Length > 0)
        {
            Array.Copy(f.Waveform, _lastProcessedWaveform, Math.Min(f.Waveform.Length, _lastProcessedWaveform.Length));
        }

        // Clear background
        canvas.Clear(0xFF000020); // Dark blue background

        // Draw title and debug info
        DrawDebugInfo(canvas, f, timeSinceLastUpdate);
        
        // Draw data comparison charts
        DrawDataComparison(canvas, f);
        
        // Draw real-time audio analysis
        DrawRealTimeAnalysis(canvas, f);
        
        // Draw buffer statistics
        DrawBufferStats(canvas, f);
    }

    private void DrawDebugInfo(ISkiaCanvas canvas, AudioFeatures f, double timeSinceLastUpdate)
    {
        var titleY = 30;
        var infoY = 60;
        var lineHeight = 20;
        
        // Title
        canvas.DrawText($"VLC Audio Test Visualizer - Frame {_frameCount}", 20, titleY, 0xFFFFFFFF, 18);
        
        // Basic info
        var info = new[]
        {
            $"Time since last update: {timeSinceLastUpdate:F1}ms",
            $"FFT Data Length: {f.Fft?.Length ?? 0}",
            $"Waveform Data Length: {f.Waveform?.Length ?? 0}",
            $"RMS: {f.Rms:F6}",
            $"Beat: {f.Beat}",
            $"BPM: {f.Bpm:F1}"
        };
        
        for (int i = 0; i < info.Length; i++)
        {
            canvas.DrawText(info[i], 20, infoY + (i * lineHeight), 0xFFCCCCCC, 14);
        }
    }

    private void DrawDataComparison(ISkiaCanvas canvas, AudioFeatures f)
    {
        var chartWidth = _w - 40;
        var chartHeight = 120;
        var leftChartX = 20;
        var rightChartX = leftChartX + chartWidth / 2 + 10;
        var chartY = 200;
        
        // Left chart: FFT Data
        canvas.DrawText("FFT Spectrum Data", leftChartX, chartY - 20, 0xFF00FF00, 14);
        DrawSpectrumChart(canvas, f.Fft ?? Array.Empty<float>(), leftChartX, chartY, chartWidth / 2 - 5, chartHeight, 0xFF00FF00);
        
        // Right chart: Waveform Data
        canvas.DrawText("Waveform Data", rightChartX, chartY - 20, 0xFFFF8000, 14);
        DrawWaveformChart(canvas, f.Waveform ?? Array.Empty<float>(), rightChartX, chartY, chartWidth / 2 - 5, chartHeight, 0xFFFF8000);
    }

    private void DrawSpectrumChart(ISkiaCanvas canvas, float[] data, float x, float y, float width, float height, uint color)
    {
        if (data.Length == 0) return;
        
        var barWidth = width / data.Length;
        var maxValue = data.Length > 0 ? data.Max() : 1.0f;
        if (maxValue <= 0) maxValue = 1.0f;
        
        for (int i = 0; i < data.Length; i++)
        {
            var barHeight = (data[i] / maxValue) * height;
            var barX = x + (i * barWidth);
            var barY = y + height - barHeight;
            
            canvas.DrawRect(barX, barY, barWidth - 1, barHeight, color);
        }
    }

    private void DrawWaveformChart(ISkiaCanvas canvas, float[] data, float x, float y, float width, float height, uint color)
    {
        if (data.Length == 0) return;
        
        var centerY = y + height / 2;
        var scaleX = width / data.Length;
        var scaleY = height / 2;
        
        // Draw center line
        canvas.DrawLine(x, centerY, x + width, centerY, 0xFF404040, 1);
        
        // Draw waveform
        for (int i = 0; i < data.Length - 1; i++)
        {
            var x1 = x + (i * scaleX);
            var y1 = centerY + (data[i] * scaleY);
            var x2 = x + ((i + 1) * scaleX);
            var y2 = centerY + (data[i + 1] * scaleY);
            
            canvas.DrawLine(x1, y1, x2, y2, color, 2);
        }
    }

    private void DrawRealTimeAnalysis(ISkiaCanvas canvas, AudioFeatures f)
    {
        var analysisY = 350;
        var lineHeight = 18;
        
        // Calculate real-time statistics
        var fftSum = f.Fft?.Sum(ff => MathF.Abs(ff)) ?? 0f;
        var waveSum = f.Waveform?.Sum(w => MathF.Abs(w)) ?? 0f;
        var fftMax = f.Fft?.Length > 0 ? f.Fft.Max() : 0f;
        var waveMax = f.Waveform?.Length > 0 ? f.Waveform.Max() : 0f;
        var fftNonZero = f.Fft?.Count(ff => MathF.Abs(ff) > 0.001f) ?? 0;
        var waveNonZero = f.Waveform?.Count(w => MathF.Abs(w) > 0.001f) ?? 0;
        
        var analysis = new[]
        {
            $"FFT Sum: {fftSum:F6} | Max: {fftMax:F6} | Non-zero: {fftNonZero}",
            $"Wave Sum: {waveSum:F6} | Max: {waveMax:F6} | Non-zero: {waveNonZero}",
            $"Data Quality: {(fftSum > 0.001f && waveSum > 0.001f ? "GOOD" : "POOR")}",
            $"Buffer Status: {(fftNonZero > 10 && waveNonZero > 10 ? "ACTIVE" : "INACTIVE")}"
        };
        
        for (int i = 0; i < analysis.Length; i++)
        {
            var textColor = analysis[i].Contains("GOOD") || analysis[i].Contains("ACTIVE") ? 0xFF00FF00 : 0xFFFF0000;
            canvas.DrawText(analysis[i], 20, analysisY + (i * lineHeight), textColor, 14);
        }
    }

    private void DrawBufferStats(ISkiaCanvas canvas, AudioFeatures f)
    {
        var statsY = 450;
        var lineHeight = 16;
        
        // Buffer statistics
        var stats = new[]
        {
            $"Buffer Analysis:",
            $"  FFT Buffer Size: {f.Fft?.Length ?? 0}",
            $"  Wave Buffer Size: {f.Waveform?.Length ?? 0}",
            $"  Expected Rate: ~44.1kHz",
            $"  Frame Rate: {_frameCount / Math.Max(1, (DateTime.Now - _lastUpdate).TotalSeconds):F1} FPS",
            $"  Last Update: {_lastUpdate:HH:mm:ss.fff}"
        };
        
        for (int i = 0; i < stats.Length; i++)
        {
            var textColor = i == 0 ? 0xFFFFFF00 : 0xFFCCCCCC;
            canvas.DrawText(stats[i], 20, statsY + (i * lineHeight), textColor, 14);
        }
    }

    public void Dispose() 
    {
        Debug.WriteLine("[VlcAudioTestVisualizer] Disposed");
    }
}