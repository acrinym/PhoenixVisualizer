using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Plugins.Avs;

/// <summary>
/// Placeholder AVS effects visualizer.
/// Actual effect graph implementation pending.
/// </summary>
public class AvsEffectsVisualizer : IVisualizerPlugin
{
    public string Id => "avs_effects_engine";
    public string DisplayName => "AVS Effects Engine";

    public void Initialize(int width, int height) { }
    public void Resize(int width, int height) { }
    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        canvas.Clear(0xFF000000);
    }
    public void Dispose() { }
}
