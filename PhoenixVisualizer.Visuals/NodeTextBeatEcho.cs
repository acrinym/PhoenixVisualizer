using PhoenixVisualizer.Core.Nodes;
namespace PhoenixVisualizer.Visuals
{
    public sealed class NodeTextBeatEcho : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodeTextBeatEcho()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("Text").With("Content", "PHOENIX").With("Size", 72),
                EffectRegistry.Create("BeatScale").With("Amount", 0.28f),
                EffectRegistry.Create("Echo").With("Count", 10).With("Decay", 0.90f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas c){ foreach (var n in _stack) n.Process(f,c); }
    }
}
