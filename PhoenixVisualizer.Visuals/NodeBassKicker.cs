using PhoenixVisualizer.Core.Nodes;
namespace PhoenixVisualizer.Visuals
{
    public sealed class NodeBassKicker : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodeBassKicker()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("BeatDetect").With("Sensitivity", 1.0f),
                EffectRegistry.Create("Rings").With("Count", 5).With("Thickness", 3.0f),
                EffectRegistry.Create("BeatScale").With("Amount", 0.35f),
                EffectRegistry.Create("ColorFade").With("Mode", "HSV").With("Speed", 0.18f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas c){ foreach (var n in _stack) n.Process(f,c); }
    }
}
