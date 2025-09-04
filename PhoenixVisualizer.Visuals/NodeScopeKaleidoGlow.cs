using PhoenixVisualizer.Core.Nodes;
namespace PhoenixVisualizer.Visuals
{
    public sealed class NodeScopeKaleidoGlow : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodeScopeKaleidoGlow()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("Superscope").With("Thickness", 1.5f).With("Smoothing", 0.42f),
                EffectRegistry.Create("Kaleidoscope").With("Segments", 7),
                EffectRegistry.Create("Glow").With("Radius", 5.5f).With("Intensity", 0.6f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas c){ foreach (var n in _stack) n.Process(f,c); }
    }
}
