using PhoenixVisualizer.Core.Nodes;

namespace PhoenixVisualizer.Visuals
{
    // Spectrum-driven nebula using blur+color layers
    public sealed class NodeSpectrumNebula : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodeSpectrumNebula()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("SpectrumAnalyzer").With("Bars", false).With("Smoothing", 0.35f),
                EffectRegistry.Create("Colorize").With("Palette", "Nebula"),
                EffectRegistry.Create("GaussianBlur").With("Radius", 3.0f),
                EffectRegistry.Create("Glow").With("Radius", 6.0f).With("Intensity", 0.5f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
        {
            foreach (var n in _stack) n.Process(f, canvas);
        }
    }
}
