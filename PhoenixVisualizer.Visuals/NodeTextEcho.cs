using PhoenixVisualizer.Core.Nodes;

namespace PhoenixVisualizer.Visuals
{
    // Text echo: text -> extrude/echo -> trails; tie to beat
    public sealed class NodeTextEcho : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodeTextEcho()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("Text").With("Content", "PHOENIX").With("Size", 64),
                EffectRegistry.Create("Echo").With("Count", 12).With("Decay", 0.88f),
                EffectRegistry.Create("Trails").With("Decay", 0.90f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
        {
            foreach (var n in _stack) n.Process(f, canvas);
        }
    }
}
