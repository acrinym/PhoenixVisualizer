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
	private readonly AvsVisualizerPlugin _plugin = new();
	private Timer? _timer;
	private DateTime _start = DateTime.UtcNow;

	public RenderSurface()
	{
		_audio = new AudioService();
		_audio.Initialize();
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		_plugin.Initialize((int)Bounds.Width, (int)Bounds.Height);
		_timer = new Timer(_ => Dispatcher.UIThread.Post(InvalidateVisual), null, 0, 16);
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		_timer?.Dispose();
		_timer = null;
		_plugin.Dispose();
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
		var now = DateTime.UtcNow;
		double t = (now - _start).TotalSeconds;
		var features = new AudioFeatures(
			t,
			0,
			false,
			0,
			0,
			0,
			0,
			fft,
			0,0,0,
			null,
			null
		);
		_plugin.RenderFrame(features, adapter);
	}
}


