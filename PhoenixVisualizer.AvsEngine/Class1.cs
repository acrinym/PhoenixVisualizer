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

	public void Initialize(int width, int height)
	{
		_width = width; _height = height;
	}

	public void LoadPreset(string presetText)
	{
		// TODO: parse preset to internal representation
	}

	public void Resize(int width, int height)
	{
		_width = width; _height = height;
	}

	public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
	{
		canvas.Clear(0xFF000000);
		// Draw a simple line scope from FFT as placeholder
		var fft = features.Fft;
		if (fft.Length == 0) return;
		int samples = Math.Min(256, fft.Length);
		Span<(float x, float y)> pts = stackalloc (float x, float y)[samples];
		for (int i = 0; i < samples; i++)
		{
			float x = (float)i / (samples - 1) * (_width - 1);
			float mag = fft[i];
			float y = (float)(_height * 0.5 - mag * (_height * 0.4));
			pts[i] = (x, y);
		}
		canvas.DrawLines(pts, 2f, 0xFFFF8800);
	}
}
