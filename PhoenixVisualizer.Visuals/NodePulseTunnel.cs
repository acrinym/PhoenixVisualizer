using PhoenixVisualizer.Core.Nodes;

namespace PhoenixVisualizer.Visuals
{
    public sealed class NodePulseTunnel : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodePulseTunnel()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("Waveform").With("Mode", "RadialRings").With("Gain", 1.0f),
                EffectRegistry.Create("PolarWarp").With("Spin", 0.12f).With("Zoom", 0.15f),
                EffectRegistry.Create("Trails").With("Decay", 0.86f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
        {
            foreach (var node in _stack) node.Process(f, canvas);
        }
    }
}
