using PhoenixVisualizer.Core.Nodes;
namespace PhoenixVisualizer.Visuals
{
    public sealed class NodeGeoLattice : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodeGeoLattice()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("Grid").With("Size", 20).With("Thickness", 1.0f),
                EffectRegistry.Create("Deform").With("Band", "Treble").With("Amount", 0.22f),
                EffectRegistry.Create("ColorFade").With("Mode", "HSV").With("Speed", 0.26f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas c){ foreach (var n in _stack) n.Process(f,c); }
    }
}
