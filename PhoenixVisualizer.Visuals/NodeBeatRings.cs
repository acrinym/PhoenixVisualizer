using PhoenixVisualizer.Core.Nodes;

namespace PhoenixVisualizer.Visuals
{
    // Concentric rings pulsing on beats, deformed by spectrum
    public sealed class NodeBeatRings : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodeBeatRings()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("Rings").With("Count", 8).With("Thickness", 2.5f),
                EffectRegistry.Create("BeatScale").With("Amount", 0.25f),
                EffectRegistry.Create("Deform").With("Amount", 0.15f).With("Band", "All"),
                EffectRegistry.Create("ColorFade").With("Mode", "HSV").With("Speed", 0.20f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
        {
            foreach (var n in _stack) n.Process(f, canvas);
        }
    }
}
