using PhoenixVisualizer.Core.Nodes;

namespace PhoenixVisualizer.Visuals
{
    public sealed class NodeButterflyField : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodeButterflyField()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("Parametric").With("Formula", "butterfly").With("Count", 1800),
                EffectRegistry.Create("ColorCycle").With("Speed", 0.35f),
                EffectRegistry.Create("GaussianBlur").With("Radius", 2.0f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
        {
            foreach (var node in _stack) node.Process(f, canvas);
        }
    }
}
