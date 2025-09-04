using PhoenixVisualizer.Core.Nodes;

namespace PhoenixVisualizer.Visuals
{
    // Waveform-driven starfield emitter with trail persistence
    public sealed class NodeWaveStarfield : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodeWaveStarfield()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("Waveform").With("Mode", "Centered"),   // drives emission rate
                EffectRegistry.Create("Starfield").With("Count", 1200).With("Speed", 0.9f),
                EffectRegistry.Create("Trails").With("Decay", 0.92f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
        {
            foreach (var n in _stack) n.Process(f, canvas);
        }
    }
}
