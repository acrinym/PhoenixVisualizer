using PhoenixVisualizer.Core.Nodes;
namespace PhoenixVisualizer.Visuals
{
    public sealed class NodeKaleidoBeats : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodeKaleidoBeats()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("SpectrumAnalyzer").With("Bars", false).With("Smoothing", 0.35f),
                EffectRegistry.Create("Kaleidoscope").With("Segments", 8),
                EffectRegistry.Create("ColorFade").With("Mode", "HSV").With("Speed", 0.22f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas c){ foreach (var n in _stack) n.Process(f,c); }
    }
}
