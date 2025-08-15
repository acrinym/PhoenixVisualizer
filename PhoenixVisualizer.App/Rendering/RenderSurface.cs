using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using PhoenixVisualizer.Audio;
using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Plugins.Avs;
using Avalonia.Threading;

namespace PhoenixVisualizer.Rendering;

public sealed class RenderSurface : Control
{
    private readonly AudioService _audio;
    private IVisualizerPlugin? _plugin = new AvsVisualizerPlugin();
	private Timer? _timer;
	private DateTime _start = DateTime.UtcNow;
	private readonly float[] _smoothFft = new float[2048];
	private bool _fftInit;
	private DateTime _fpsWindowStart = DateTime.UtcNow;
	private int _framesInWindow;
	public event Action<double>? FpsChanged;

    public RenderSurface()
    {
        _audio = new AudioService();
    }

    	public void SetPlugin(IVisualizerPlugin plugin)
	{
		_plugin = plugin;
		if (Bounds.Width > 0 && Bounds.Height > 0)
		{
			_plugin.Initialize((int)Bounds.Width, (int)Bounds.Height);
		}
	}
	
	public IVisualizerPlugin? GetCurrentPlugin() => _plugin;

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
        base.OnAttachedToVisualTree(e);
        _plugin?.Initialize((int)Bounds.Width, (int)Bounds.Height);
        _audio.Initialize();
        _timer = new Timer(_ => Dispatcher.UIThread.Post(InvalidateVisual, Avalonia.Threading.DispatcherPriority.Render), null, 0, 16);
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
		var fft = _audio.ReadFft();
		
		// Debug: log FFT data
		var first5 = fft.Length >= 5 ? $"{fft[0]},{fft[1]},{fft[2]},{fft[3]},{fft[4]}" : "insufficient data";
		System.Diagnostics.Debug.WriteLine($"RenderSurface.Render: FFT length={fft.Length}, first 5 values=[{first5}]");
		
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
		
		var now = DateTime.UtcNow;
		double t = (now - _start).TotalSeconds;
		var features = new AudioFeatures(
			TimeSeconds: t,
			Bpm: 0,
			Beat: false,
			Volume: 0,
			Rms: 0,
			Peak: 0,
			Energy: 0,
			Fft: _smoothFft,
			Bass: 0,
			Mid: 0,
			Treble: 0,
			Genre: null,
			SuggestedColorArgb: null
		);
		
		System.Diagnostics.Debug.WriteLine($"RenderSurface.Render: Plugin={_plugin?.GetType().Name}, Features={features.Fft.Length} FFT values");
		
        if (_plugin != null)
        {
            try
            {
                _plugin.RenderFrame(features, adapter);
                System.Diagnostics.Debug.WriteLine("RenderSurface.Render: RenderFrame completed successfully");
            }
            catch (Exception ex)
            {
                // Debug: log any rendering errors
                System.Diagnostics.Debug.WriteLine($"RenderFrame error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("RenderSurface.Render: No plugin loaded!");
        }

		// FPS update
		_framesInWindow++;
		var span = now - _fpsWindowStart;
        if (span.TotalSeconds >= 1)
		{
			double fps = _framesInWindow / span.TotalSeconds;
			_framesInWindow = 0;
			_fpsWindowStart = now;
            Dispatcher.UIThread.Post(() => FpsChanged?.Invoke(fps), Avalonia.Threading.DispatcherPriority.Background);
		}
	}
}


