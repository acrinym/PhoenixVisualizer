using PhoenixVisualizer.Core.Nodes;
namespace PhoenixVisualizer.Visuals
{
    public sealed class NodeBeatKaleidoTunnel : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodeBeatKaleidoTunnel()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("Waveform").With("Mode", "Rings").With("Gain", 1.1f),
                EffectRegistry.Create("PolarWarp").With("Spin", 0.2f).With("Zoom", 0.12f),
                EffectRegistry.Create("Kaleidoscope").With("Segments", 6),
                EffectRegistry.Create("Trails").With("Decay", 0.90f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas c){ foreach (var n in _stack) n.Process(f,c); }
    }
}
