using PhoenixVisualizer.Core.Nodes;

namespace PhoenixVisualizer.Visuals
{
    public sealed class NodeBarsReactive : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodeBarsReactive()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("SpectrumAnalyzer").With("Bars", true).With("Smoothing", 0.35f),
                EffectRegistry.Create("ColorFade").With("Mode", "HSV").With("Speed", 0.25f),
                EffectRegistry.Create("Glow").With("Radius", 6f).With("Intensity", 0.6f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
        {
            foreach (var node in _stack) node.Process(f, canvas);
        }
    }
}
