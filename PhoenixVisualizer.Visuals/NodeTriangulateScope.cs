using PhoenixVisualizer.Core.Nodes;
namespace PhoenixVisualizer.Visuals
{
    public sealed class NodeTriangulateScope : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodeTriangulateScope()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("Superscope").With("Thickness", 1.2f).With("Smoothing", 0.4f),
                EffectRegistry.Create("Triangulate").With("Density", 0.7f),
                EffectRegistry.Create("Colorize").With("Palette", "Aurora")
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas c){ foreach (var n in _stack) n.Process(f,c); }
    }
}
