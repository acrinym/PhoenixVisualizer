using PhoenixVisualizer.Core.Nodes;

namespace PhoenixVisualizer.Visuals
{
    // Hex grid that pulses with bass and shimmers with treble
    public sealed class NodeHexGridPulse : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodeHexGridPulse()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("HexGrid").With("Size", 28).With("Thickness", 1.0f),
                EffectRegistry.Create("BeatScale").With("Amount", 0.22f),
                EffectRegistry.Create("ColorFade").With("Mode", "HSV").With("Speed", 0.28f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
        {
            foreach (var n in _stack) n.Process(f, canvas);
        }
    }
}
