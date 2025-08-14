using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.AvsEngine;

public interface IAvsEngine
{
	void Initialize(int width, int height);
	void LoadPreset(string presetText);
	void Resize(int width, int height);
	void RenderFrame(AudioFeatures features, ISkiaCanvas canvas);
}

// Minimal Superscope-like evaluator (stub)
public sealed class AvsEngine : IAvsEngine
{
	private int _width;
	private int _height;
	private Preset _preset = Preset.CreateDefault();

	public void Initialize(int width, int height)
	{
		_width = width; _height = height;
	}

	public void LoadPreset(string presetText)
	{
		// Very small parser: supports tokens like "points=256;mode=line;source=fft"
		try
		{
			var p = new Preset();
			foreach (var seg in presetText.Split(';', StringSplitOptions.RemoveEmptyEntries))
			{
				var kv = seg.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
				if (kv.Length != 2) continue;
				var key = kv[0].Trim().ToLowerInvariant();
				var val = kv[1].Trim().ToLowerInvariant();
				switch (key)
				{
					case "points":
						if (int.TryParse(val, out var n)) p.Points = Math.Clamp(n, 16, 2048);
						break;
					case "mode":
						p.Mode = val == "bars" ? RenderMode.Bars : RenderMode.Line;
						break;
					case "source":
						p.Source = val == "sin" ? SourceMode.Sin : SourceMode.Fft;
						break;
				}
			}
			_preset = p;
		}
		catch { _preset = Preset.CreateDefault(); }
	}

	public void Resize(int width, int height)
	{
		_width = width; _height = height;
	}

	public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
	{
		canvas.Clear(0xFF000000);
		// Draw a simple superscope-like output based on preset
		int npts = Math.Min(_preset.Points, 1024);
		Span<(float x, float y)> pts = stackalloc (float x, float y)[npts];
		ReadOnlySpan<float> fft = features.Fft;
		for (int i = 0; i < npts; i++)
		{
			float nx = npts > 1 ? (float)i / (npts - 1) : 0f;
			float x = nx * (float)(_width - 1);
			float v = _preset.Source switch
			{
				SourceMode.Sin => (float)Math.Sin((features.TimeSeconds * 2 * Math.PI) + nx * 4 * Math.PI),
				_ => fft.Length > 0 ? fft[(int)(nx * (fft.Length - 1))] : 0f
			};
			float y = (float)(_height * 0.5 - v * (_height * 0.4));
			pts[i] = (x, y);
		}
		uint color = _preset.Mode == RenderMode.Bars ? 0xFF44AAFFu : 0xFFFF8800u;
		canvas.DrawLines(pts, _preset.Mode == RenderMode.Bars ? 3f : 2f, color);
	}
}

file sealed class Preset
{
	public int Points { get; set; } = 256;
	public RenderMode Mode { get; set; } = RenderMode.Line;
	public SourceMode Source { get; set; } = SourceMode.Fft;

	public static Preset CreateDefault() => new();
}

file enum RenderMode { Line, Bars }
file enum SourceMode { Fft, Sin }
