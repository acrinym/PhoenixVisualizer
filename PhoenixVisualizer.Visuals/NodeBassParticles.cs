using PhoenixVisualizer.Core.Nodes;
namespace PhoenixVisualizer.Visuals
{
    public sealed class NodeBassParticles : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodeBassParticles()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("BeatDetect").With("Sensitivity", 0.95f),
                EffectRegistry.Create("ParticleSystem").With("Max", 1500).With("EmitOnBeat", true).With("Rate", 260),
                EffectRegistry.Create("Trails").With("Decay", 0.92f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas c){ foreach (var n in _stack) n.Process(f,c); }
    }
}
