using System.Diagnostics;

using PhoenixVisualizer.Core.Services;
using PhoenixVisualizer.Core.Config;
using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Plugins.Avs;

namespace PhoenixVisualizer.App.Rendering;

public sealed class RenderSurface : Control
{
    private IAudioProvider _audio;
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

    // cache settings; reload at most once per second
    private VisualizerSettings _vz = VisualizerSettings.Load();
    private DateTime _lastVzLoad = DateTime.UtcNow;
    private const int SettingsReloadMs = 1000; // 1 Hz
    
    // Performance monitoring
    private readonly PluginPerformanceMonitor _perfMonitor = new();
    private readonly Stopwatch _renderStopwatch = new();

    // Events
    public event Action<double>? FpsChanged;
    public event Action<double>? BpmChanged;
    public event Action<double, double>? PositionChanged;

    public RenderSurface()
    {
        // Default to VLC audio service
        var vlcBus = new VlcAudioBus();
        _audio = new VlcAudioService(vlcBus);
    }
    
    public RenderSurface(IAudioProvider audioService)
    {
        _audio = audioService ?? throw new ArgumentNullException(nameof(audioService));
    }

    public void SetPlugin(IVisualizerPlugin plugin)
    {
        _plugin?.Dispose();
        _plugin = plugin;
        if (Bounds.Width > 0 && Bounds.Height > 0)
        {
            _plugin.Initialize((int)Bounds.Width, (int)Bounds.Height);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _plugin?.Initialize((int)Bounds.Width, (int)Bounds.Height);
        var audioInitResult = _audio.Initialize();
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
        var result = _audio.Open(path);
        return result;
    }
    
    public bool Play() 
    {
        var result = _audio.Play();
        return result;
    }
    
    public void Pause() 
    {
        _audio.Pause();
    }
    
    public void Stop() 
    {
        _audio.Stop();
    }

    public IAudioProvider? GetAudioService() => _audio;
    
    public void SetAudioService(IAudioProvider audioService)
    {
        if (audioService == null) return;
        
        // Dispose the old audio service
        _audio?.Dispose();
        
        // Set the new one
        _audio = audioService;
        
        Debug.WriteLine($"[RenderSurface] Audio service switched to: {audioService.GetType().Name}");
    }
    
    public PluginPerformanceMonitor GetPerformanceMonitor() => _perfMonitor;

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

        // 1) Get fresh audio data - but only if audio service is ready
        float[] fft;
        float[] wave;
        double pos = 0;
        double total = 0;
        
        if (_audio != null && _audio.IsReadyToPlay)
        {
            fft = _audio.GetSpectrumData();
            wave = _audio.GetWaveformData();
            pos = _audio.GetPositionSeconds();
            total = _audio.GetLengthSeconds();
            
            // Debug: Check if we're getting actual audio data
            float fftSum = fft.Sum(f => MathF.Abs(f));
            float waveSum = wave.Sum(f => MathF.Abs(f));
            
            if (fftSum < 0.001f || waveSum < 0.001f)
            {
                // Audio data appears to be silent/zero - this might be the issue
                System.Diagnostics.Debug.WriteLine($"[RenderSurface] Audio data appears silent - FFT sum: {fftSum:F6}, Wave sum: {waveSum:F6}");
            }
        }
        else
        {
            // Fallback if audio service is not ready
            fft = new float[2048];
            wave = new float[2048];
            
            // Debug: Log why audio service isn't ready
            if (_audio != null)
            {
                System.Diagnostics.Debug.WriteLine($"[RenderSurface] Audio service not ready: {_audio.GetStatus()}");
            }
        }

        // Load settings at most once per second
        if ((DateTime.UtcNow - _lastVzLoad).TotalMilliseconds >= SettingsReloadMs)
        {
            _vz = VisualizerSettings.Load();
            _lastVzLoad = DateTime.UtcNow;
        }
        var vz = _vz;

        // 2) FFT smoothing
        if (!_fftInit)
        {
            // First time: copy raw data
            Array.Copy(fft, _smoothFft, fft.Length);
            _fftInit = true;
        }
        else
        {
            // Apply smoothing
            float smoothingAlpha = TimeDeltaToAlpha(vz.SmoothingMs);
            for (int i = 0; i < _smoothFft.Length; i++)
            {
                _smoothFft[i] = _smoothFft[i] * (1 - smoothingAlpha) + fft[i] * smoothingAlpha;
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

        // Create Core AudioFeatures for scheduler
        var features = new PhoenixVisualizer.Core.Models.AudioFeatures
        {
            Rms = rms,
            Beat = beat
        };

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
                return;
            }
            
            // Start performance monitoring
            _renderStopwatch.Restart();
            
            // Debug: Log what we're sending to the plugin
            float fftSum = _smoothFft.Sum(f => MathF.Abs(f));
            float waveSum = wave.Sum(f => MathF.Abs(f));
            System.Diagnostics.Debug.WriteLine($"[RenderSurface] Sending to plugin '{_plugin.Id}': FFT sum: {fftSum:F6}, Wave sum: {waveSum:F6}, RMS: {rms:F6}, Beat: {beat}, BPM: {_bpm:F1}");
            
            // Create PluginHost AudioFeatures for plugin rendering
            var pluginFeatures = AudioFeaturesImpl.CreateEnhanced(
                _smoothFft,  // fft
                wave,        // waveform
                rms,         // rms
                _bpm,        // bpm
                beat,        // beat
                t            // timeSeconds
            );
            
            _plugin.RenderFrame(pluginFeatures, adapter);
            
            // Record performance metrics
            _renderStopwatch.Stop();
            var renderTimeMs = _renderStopwatch.Elapsed.TotalMilliseconds;
            
            // Start monitoring if not already monitoring this plugin
            if (_plugin.Id != null)
            {
                if (_perfMonitor.GetMetrics(_plugin.Id) == null)
                {
                    _perfMonitor.StartMonitoring(_plugin.Id, _plugin.DisplayName);
                }
                _perfMonitor.RecordFrame(_plugin.Id, renderTimeMs);
            }
        }
        catch (Exception ex)
        {
            // Plugin render failed - log the error for debugging
            System.Diagnostics.Debug.WriteLine($"[RenderSurface] Plugin render failed: {ex.Message}");
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
