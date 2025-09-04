using PhoenixVisualizer.Core.Nodes;

namespace PhoenixVisualizer.Visuals
{
    // Rainbow spectrum with HSV drift and trail
    public sealed class NodeRainbowSpectrum : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodeRainbowSpectrum()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("SpectrumAnalyzer").With("Bars", true).With("Smoothing", 0.30f),
                EffectRegistry.Create("ColorFade").With("Mode", "HSV").With("Speed", 0.35f),
                EffectRegistry.Create("Trails").With("Decay", 0.88f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
        {
            foreach (var n in _stack) n.Process(f, canvas);
        }
    }
}
