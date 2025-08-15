using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using PhoenixVisualizer.Audio;
using PhoenixVisualizer.PluginHost;
using Avalonia.Threading;

namespace PhoenixVisualizer.Rendering;

public sealed class RenderSurface : Control
{
    private readonly AudioService _audio;
    private IVisualizerPlugin? _plugin;
    private Timer? _timer;
    private readonly float[] _smoothFft = new float[2048];
    private bool _fftInit;
    private DateTime _fpsWindowStart = DateTime.UtcNow;
    private int _framesInWindow;
    private float _prevEnergy;
    private DateTime _lastBeat = DateTime.MinValue;
    private double _bpm;
    private int _lastWidth;
    private int _lastHeight;
    public event Action<double>? FpsChanged;
    public event Action<double>? BpmChanged;
    public event Action<double, double>? PositionChanged;

    public RenderSurface()
    {
        _audio = new AudioService();
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

    public bool Open(string path) => _audio.Open(path);
    public void Play() => _audio.Play();
    public void Pause() => _audio.Pause();
    public void Stop() => _audio.Stop();

    public override void Render(DrawingContext context)
    {
        var adapter = new CanvasAdapter(context, Bounds.Width, Bounds.Height);
        int w = (int)Bounds.Width;
        int h = (int)Bounds.Height;
        if (w != _lastWidth || h != _lastHeight)
        {
            _lastWidth = w; _lastHeight = h;
            _plugin?.Resize(w, h);
        }
        var fft = _audio.ReadFft();
        var wave = _audio.ReadWaveform();
        double pos = _audio.GetPositionSeconds();
        double total = _audio.GetLengthSeconds();
        // Smooth FFT
        if (!_fftInit)
        {
            Array.Copy(fft, _smoothFft, Math.Min(fft.Length, _smoothFft.Length));
            _fftInit = true;
        }
        else
        {
            int n = Math.Min(fft.Length, _smoothFft.Length);
            const float alpha = 0.2f;
            for (int i = 0; i < n; i++)
            {
                _smoothFft[i] = _smoothFft[i] + alpha * (fft[i] - _smoothFft[i]);
            }
        }

        // Basic feature extraction
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
        bool beat = false;
        var now = DateTime.UtcNow;
        double t = pos;
        if (energy > _prevEnergy * 1.5f && energy > 1e-6f)
        {
            beat = true;
            if (_lastBeat != DateTime.MinValue)
            {
                _bpm = 60.0 / (now - _lastBeat).TotalSeconds;
                Dispatcher.UIThread.Post(() => BpmChanged?.Invoke(_bpm), DispatcherPriority.Background);
            }
            _lastBeat = now;
        }
        _prevEnergy = _prevEnergy * 0.9f + energy * 0.1f;

        var features = new AudioFeatures(
            t,
            _bpm,
            beat,
            volume,
            rms,
            peak,
            energy,
            _smoothFft,
            wave,
            bass,
            mid,
            treble,
            null,
            null
        );
        _plugin?.RenderFrame(features, adapter);

        Dispatcher.UIThread.Post(() => PositionChanged?.Invoke(pos, total), DispatcherPriority.Background);

        // FPS update
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
}


