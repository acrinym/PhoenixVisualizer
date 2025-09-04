using PhoenixVisualizer.Core.Nodes;

namespace PhoenixVisualizer.Visuals
{
    // Audio-reactive flow field particles
    public sealed class NodeAudioFlowField : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodeAudioFlowField()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("FlowField").With("Scale", 0.015f).With("Strength", 0.8f),
                EffectRegistry.Create("ParticleSystem").With("Max", 2000).With("Rate", 400),
                EffectRegistry.Create("Trails").With("Decay", 0.93f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
        {
            foreach (var n in _stack) n.Process(f, canvas);
        }
    }
}
