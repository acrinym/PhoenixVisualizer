using PhoenixVisualizer.Core.Nodes;

namespace PhoenixVisualizer.Visuals
{
    // Vector grid deforms with mid frequencies; subtle glow
    public sealed class NodeVectorGrid : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodeVectorGrid()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("Grid").With("Size", 16).With("Thickness", 1.2f),
                EffectRegistry.Create("Deform").With("Amount", 0.2f).With("Band", "Mid"),
                EffectRegistry.Create("Glow").With("Radius", 4f).With("Intensity", 0.5f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
        {
            foreach (var n in _stack) n.Process(f, canvas);
        }
    }
}
