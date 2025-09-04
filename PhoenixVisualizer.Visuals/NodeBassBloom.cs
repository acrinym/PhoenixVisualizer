using PhoenixVisualizer.Core.Nodes;

namespace PhoenixVisualizer.Visuals
{
    // Bass-heavy bloom pulses with chroma
    public sealed class NodeBassBloom : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodeBassBloom()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("BeatDetect").With("Sensitivity", 0.9f),
                EffectRegistry.Create("Bloom").With("Radius", 8f).With("Intensity", 0.7f),
                EffectRegistry.Create("ColorCycle").With("Speed", 0.25f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
        {
            foreach (var n in _stack) n.Process(f, canvas);
        }
    }
}
