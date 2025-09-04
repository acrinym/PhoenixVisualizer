using PhoenixVisualizer.Core.Nodes;
namespace PhoenixVisualizer.Visuals
{
    public sealed class NodeVectorFieldScope : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodeVectorFieldScope()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("Superscope").With("Thickness", 1.8f).With("Smoothing", 0.45f),
                EffectRegistry.Create("FlowField").With("Scale", 0.02f).With("Strength", 0.7f),
                EffectRegistry.Create("Glow").With("Radius", 4.5f).With("Intensity", 0.5f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas c){ foreach (var n in _stack) n.Process(f,c); }
    }
}
