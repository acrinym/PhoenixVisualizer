using PhoenixVisualizer.Core.Nodes;
namespace PhoenixVisualizer.Visuals
{
    public sealed class NodePixelSortPlasma : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodePixelSortPlasma()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("Plasma").With("Detail", 0.9f),
                EffectRegistry.Create("PixelSort").With("Threshold", 0.65f).With("Direction", "Vertical"),
                EffectRegistry.Create("ColorFade").With("Mode", "HSV").With("Speed", 0.20f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas c){ foreach (var n in _stack) n.Process(f,c); }
    }
}
