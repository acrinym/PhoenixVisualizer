using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.AvsEngine;

public interface IAvsEngine
{
	void Initialize(int width, int height);
	void LoadPreset(string presetText);
	void Resize(int width, int height);
	void RenderFrame(AudioFeatures features, ISkiaCanvas canvas);
}

public sealed class AvsEngine : IAvsEngine
{
	public void Initialize(int width, int height) { }
	public void LoadPreset(string presetText) { }
	public void Resize(int width, int height) { }
	public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas) { }
}
