using PhoenixVisualizer.AvsEngine;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Plugins.Avs;

public sealed class AvsVisualizerPlugin : IAvsHostPlugin
{
	private readonly IAvsEngine _engine = new AvsEngine.AvsEngine();
	private int _w;
	private int _h;

	public string Id => "vis_avs";
	public string DisplayName => "AVS Runtime";

	public void Initialize(int width, int height)
	{
		_w = width; _h = height;
		_engine.Initialize(width, height);
	}

	public void Resize(int width, int height)
	{
		_w = width; _h = height;
		_engine.Resize(width, height);
	}

	public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
	{
		_engine.RenderFrame(features, canvas);
	}

	public void Dispose() { }

    public void LoadPreset(string presetText) => _engine.LoadPreset(presetText);
}
