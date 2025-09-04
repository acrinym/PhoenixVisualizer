using PhoenixVisualizer.Core.Nodes;

namespace PhoenixVisualizer.Visuals
{
    // Plasma + warp with bass spin
    public sealed class NodePlasmaWarp : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodePlasmaWarp()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("Plasma").With("Detail", 0.8f),
                EffectRegistry.Create("PolarWarp").With("Spin", 0.18f).With("Zoom", 0.12f),
                EffectRegistry.Create("ColorFade").With("Mode", "HSV").With("Speed", 0.18f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
        {
            foreach (var n in _stack) n.Process(f, canvas);
        }
    }
}
