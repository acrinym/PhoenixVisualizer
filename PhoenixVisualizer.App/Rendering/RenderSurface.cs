using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using PhoenixVisualizer.Audio;
using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Plugins.Avs;
using PhoenixVisualizer.Core.Config;
using PhoenixVisualizer.Core.Presets;
using PhoenixVisualizer; // preset manager
using System.IO;
using System.Linq;

namespace PhoenixVisualizer.Rendering;

public sealed class RenderSurface : Control
{
    private readonly AudioService _audio;
    private IVisualizerPlugin? _plugin = new AvsVisualizerPlugin(); // keep a sensible default
    private Timer? _timer;

    // FFT smoothing
    private readonly float[] _smoothFft = new float[2048];
    private bool _fftInit;

    // FPS
    private DateTime _fpsWindowStart = DateTime.UtcNow;
    private int _framesInWindow;

    // Simple beat/BPM estimation
    private float _prevEnergy;
    private DateTime _lastBeat = DateTime.MinValue;
    private double _bpm;

    // random preset scheduler
    private readonly PresetScheduler _presetScheduler = new();

    // Resize tracking
    private int _lastWidth;
    private int _lastHeight;

    // Events
    public event Action<double>? FpsChanged;
    public event Action<double>? BpmChanged;
    public event Action<double, double>? PositionChanged;

    // Debug logging to file
    static void LogToFile(string message)
    {
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "render_debug.log");
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] {message}";
            File.AppendAllText(logPath, timestamp + " " + message + Environment.NewLine);
        }
        catch
        {
            // Silently fail if logging fails
        }
    }

    public RenderSurface()
    {
        _audio = new AudioService();
    }

    public void SetPlugin(IVisualizerPlugin plugin)
    {
        LogToFile($"[RenderSurface] SetPlugin called with: {plugin.DisplayName} ({plugin.Id})");
        _plugin?.Dispose();
        _plugin = plugin;
        LogToFile($"[RenderSurface] Plugin set to: {_plugin?.DisplayName} ({_plugin?.Id})");
        System.Diagnostics.Debug.WriteLine($"[RenderSurface] SetPlugin: {plugin.DisplayName} ({plugin.Id})");
        if (Bounds.Width > 0 && Bounds.Height > 0)
        {
            _plugin.Initialize((int)Bounds.Width, (int)Bounds.Height);
            LogToFile($"[RenderSurface] Plugin initialized with size: {Bounds.Width}x{Bounds.Height}");
        }
        else
        {
            LogToFile($"[RenderSurface] WARNING: Bounds not ready, plugin not initialized yet");
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _plugin?.Initialize((int)Bounds.Width, (int)Bounds.Height);
        _audio.Initialize();
        _timer = new Timer(_ => Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render), null, 0, 16);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _timer?.Dispose();
        _timer = null;
        _plugin?.Dispose();
        _audio.Dispose();
        base.OnDetachedFromVisualTree(e);
    }

    public bool Open(string path) 
    {
        LogToFile($"[RenderSurface] Opening audio file: {path}");
        System.Diagnostics.Debug.WriteLine($"[RenderSurface] Opening audio file: {path}");
        var result = _audio.Open(path);
        LogToFile($"[RenderSurface] Open result: {result}, Status: {_audio.GetStatus()}");
        System.Diagnostics.Debug.WriteLine($"[RenderSurface] Open result: {result}, Status: {_audio.GetStatus()}");
        return result;
    }
    
    public bool Play() 
    {
        LogToFile($"[RenderSurface] Play requested, Status: {_audio.GetStatus()}");
        System.Diagnostics.Debug.WriteLine($"[RenderSurface] Play requested, Status: {_audio.GetStatus()}");
        var result = _audio.Play();
        LogToFile($"[RenderSurface] Play result: {result}");
        if (!result)
        {
            LogToFile($"[RenderSurface] Play failed - no audio file loaded or other error");
            System.Diagnostics.Debug.WriteLine("[RenderSurface] Play failed - no audio file loaded or other error");
        }
        return result;
    }
    
    public void Pause() 
    {
        System.Diagnostics.Debug.WriteLine($"[RenderSurface] Pause requested, Status: {_audio.GetStatus()}");
        _audio.Pause();
    }
    
    public void Stop() 
    {
        System.Diagnostics.Debug.WriteLine($"[RenderSurface] Stop requested, Status: {_audio.GetStatus()}");
        _audio.Stop();
    }

    public AudioService? GetAudioService() => _audio;

    public override void Render(DrawingContext context)
    {
        var adapter = new CanvasAdapter(context, Bounds.Width, Bounds.Height);

        // Handle dynamic resize for plugins that support it
        int w = (int)Bounds.Width;
        int h = (int)Bounds.Height;
        if (w != _lastWidth || h != _lastHeight)
        {
            _lastWidth = w;
            _lastHeight = h;
            _plugin?.Resize(w, h);
        }

        // 1) Get fresh audio data
        var fft = _audio.ReadFft();
        var wave = _audio.ReadWaveform();
        var pos = _audio.GetPositionSeconds();
        var total = _audio.GetLengthSeconds();

        // Log audio data status for debugging
        LogToFile($"[RenderSurface] Audio data - FFT length: {fft.Length}, Wave length: {wave.Length}, Pos: {pos:F2}s, Total: {total:F2}s");

        // Validate FFT data before processing - check if it's stuck
        bool fftDataValid = true;
        float fftSum = 0f;
        float fftMax = 0f;
        int fftNonZero = 0;
        
        for (int i = 0; i < fft.Length; i++)
        {
            float absVal = MathF.Abs(fft[i]);
            fftSum += absVal;
            if (absVal > fftMax) fftMax = absVal;
            if (absVal > 0.001f) fftNonZero++;
        }
        
        LogToFile($"[RenderSurface] FFT validation - Sum: {fftSum:F6}, Max: {fftMax:F6}, Non-zero: {fftNonZero}/2048");
        
        // Check if FFT data is meaningful (not stuck)
        if (fftSum < 0.001f || fftMax < 0.001f || fftNonZero < 10)
        {
            LogToFile($"[RenderSurface] FFT data appears stuck (sum: {fftSum:F6}, max: {fftMax:F6}, non-zero: {fftNonZero})");
            fftDataValid = false;
            
            // If FFT is stuck, try to force a refresh by calling audio service methods
            _audio.ReadFft(); // Force another read
            fft = _audio.ReadFft(); // Get fresh data
            
            // Re-validate
            fftSum = 0f;
            fftMax = 0f;
            fftNonZero = 0;
            for (int i = 0; i < fft.Length; i++)
            {
                float absVal = MathF.Abs(fft[i]);
                fftSum += absVal;
                if (absVal > fftMax) fftMax = absVal;
                if (absVal > 0.001f) fftNonZero++;
            }
            
            LogToFile($"[RenderSurface] After refresh - Sum: {fftSum:F6}, Max: {fftMax:F6}, Non-zero: {fftNonZero}/2048");
            
            if (fftSum < 0.001f || fftMax < 0.001f || fftNonZero < 10)
            {
                LogToFile($"[RenderSurface] FFT data still stuck after refresh attempt");
                // Use a fallback pattern instead of stuck data
                for (int i = 0; i < fft.Length; i++)
                {
                    fft[i] = MathF.Sin(i * 0.1f) * 0.1f; // Generate a simple sine wave pattern
                }
                LogToFile($"[RenderSurface] Applied fallback sine wave pattern");
            }
        }

        // Load settings each frame (cheap JSON)
        var vz = VisualizerSettings.Load();

        // 2) FFT smoothing with validation
        if (!_fftInit)
        {
            // First time: copy raw data
            Array.Copy(fft, _smoothFft, fft.Length);
            _fftInit = true;
        }
        else if (fftDataValid)
        {
            // Only apply smoothing if we have valid data
            float smoothingAlpha = TimeDeltaToAlpha(vz.SmoothingMs);
            for (int i = 0; i < _smoothFft.Length; i++)
            {
                // Ensure we're not smoothing with stuck data
                if (MathF.Abs(fft[i] - _smoothFft[i]) > 0.001f)
                {
                    _smoothFft[i] = _smoothFft[i] * (1 - smoothingAlpha) + fft[i] * smoothingAlpha;
                }
            }
        }

        // 1) Input gain
        float gain = MathF.Pow(10f, vz.InputGainDb / 20f);
        for (int i = 0; i < _smoothFft.Length; i++) _smoothFft[i] *= gain;
        for (int i = 0; i < wave.Length; i++) wave[i] = Math.Clamp(wave[i] * gain, -1f, 1f);

        // 2) Noise gate
        float gateLin = MathF.Pow(10f, vz.NoiseGateDb / 20f);
        for (int i = 0; i < _smoothFft.Length; i++)
            if (_smoothFft[i] < gateLin) _smoothFft[i] = 0f;

        // 3) Spectral scaling
        if (vz.SpectrumScale == SpectrumScale.Sqrt)
        {
            for (int i = 0; i < _smoothFft.Length; i++) _smoothFft[i] = MathF.Sqrt(_smoothFft[i]);
        }
        else if (vz.SpectrumScale == SpectrumScale.Log)
        {
            const float eps = 1e-12f;
            for (int i = 0; i < _smoothFft.Length; i++)
                _smoothFft[i] = MathF.Log10(_smoothFft[i] + eps) * 0.5f + 1f;
        }

        // 4) Floor/Ceiling clamp
        float floorLin = MathF.Pow(10f, vz.FloorDb / 20f);
        float ceilingLin = MathF.Pow(10f, vz.CeilingDb / 20f);
        for (int i = 0; i < _smoothFft.Length; i++)
            _smoothFft[i] = Math.Clamp(_smoothFft[i], floorLin, ceilingLin);

        // Feature extraction
        int len = _smoothFft.Length;
        float energy = 0f;
        float volumeSum = 0f;
        float peak = 0f;
        float bass = 0f, mid = 0f, treble = 0f;
        int bassEnd = len / 3;
        int midEnd = 2 * len / 3;

        for (int i = 0; i < len; i++)
        {
            float v = MathF.Abs(_smoothFft[i]);
            volumeSum += v;
            energy += v * v;
            if (v > peak) peak = v;
            if (i < bassEnd) bass += v;
            else if (i < midEnd) mid += v;
            else treble += v;
        }

        float volume = volumeSum / len;
        float rms = MathF.Sqrt(energy / len);

        // 5) Auto gain control
        if (vz.AutoGain)
        {
            float err = vz.TargetRms - rms;
            float agc = 1f + err * 0.5f;
            agc = Math.Clamp(agc, 0.85f, 1.15f);
            for (int i = 0; i < _smoothFft.Length; i++) _smoothFft[i] *= agc;
            for (int i = 0; i < wave.Length; i++) wave[i] *= agc;
            volume *= agc;
            rms *= agc;
            energy *= agc * agc;
        }

        // 6) Beat detection with user sensitivity + cooldown
        bool beat = false;
        var now = DateTime.UtcNow;
        double mult = Math.Max(1.05, vz.BeatSensitivityOrDefault());
        if (energy > _prevEnergy * mult && energy > 1e-8)
        {
            if ((now - _lastBeat).TotalMilliseconds > Math.Max(0, vz.BeatCooldownMs))
            {
                beat = true;
                if (_lastBeat != DateTime.MinValue)
                {
                    _bpm = 60.0 / (now - _lastBeat).TotalSeconds;
                    Dispatcher.UIThread.Post(() => BpmChanged?.Invoke(_bpm), DispatcherPriority.Background);
                }
                _lastBeat = now;
            }
        }
        float alpha = TimeDeltaToAlpha(vz.SmoothingMs);
        _prevEnergy = _prevEnergy * (1 - alpha) + energy * alpha;

        // 7) Optional frame blending
        adapter.FrameBlend = Math.Clamp(vz.FrameBlend, 0f, 1f);

        // Use playback position as t (preferred for visual sync)
        double t = pos;

        // Use AudioFeaturesImpl.Create() instead of direct constructor
        var features = AudioFeaturesImpl.Create(
            _smoothFft,  // fft
            wave,        // waveform
            rms,         // rms
            _bpm,        // bpm
            beat         // beat
        );

        // Random preset switching via scheduler
        if (_presetScheduler.ShouldSwitch(features, vz))
        {
            Presets.GoRandom();
            _presetScheduler.NotifySwitched();
        }

        try
        {
            if (_plugin == null)
            {
                LogToFile($"[RenderSurface] WARNING: _plugin is NULL! Cannot render frame");
                System.Diagnostics.Debug.WriteLine("[RenderSurface] WARNING: _plugin is NULL! Cannot render frame");
                return;
            }
            
            LogToFile($"[RenderSurface] Rendering frame with plugin: {_plugin.DisplayName} ({_plugin.Id})");
            _plugin.RenderFrame(features, adapter);
        }
        catch (Exception ex)
        {
            LogToFile($"[RenderSurface] Plugin render failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Plugin render failed: {ex}");
        }

        // push position to UI listeners
        Dispatcher.UIThread.Post(() => PositionChanged?.Invoke(pos, total), DispatcherPriority.Background);

        // FPS tracking
        _framesInWindow++;
        var span = now - _fpsWindowStart;
        if (span.TotalSeconds >= 1)
        {
            double fps = _framesInWindow / span.TotalSeconds;
            _framesInWindow = 0;
            _fpsWindowStart = now;
            Dispatcher.UIThread.Post(() => FpsChanged?.Invoke(fps), DispatcherPriority.Background);
        }
    }

    private static float TimeDeltaToAlpha(float smoothingMs)
    {
        if (smoothingMs <= 0) return 1f;
        float dt = 1f / 60f; // ~60 FPS
        float tau = smoothingMs / 1000f;
        return Math.Clamp(dt / (tau + dt), 0.01f, 1f);
    }
}