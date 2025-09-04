using PhoenixVisualizer.Core.Nodes;

namespace PhoenixVisualizer.Visuals
{
    // Particles emit on beats; gravity + fade
    public sealed class NodeParticlesBeat : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodeParticlesBeat()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("ParticleSystem").With("Max", 1200).With("EmitOnBeat", true).With("Rate", 220),
                EffectRegistry.Create("Gravity").With("Y", 0.15f),
                EffectRegistry.Create("Trails").With("Decay", 0.90f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
        {
            foreach (var n in _stack) n.Process(f, canvas);
        }
    }
}
