using PhoenixVisualizer.Core.Nodes;

namespace PhoenixVisualizer.Visuals
{
    // Superscope ribbon with HSV color fade and glow
    public sealed class NodeScopeRibbon : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        public NodeScopeRibbon()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.Create("ClearFrame"),
                EffectRegistry.Create("Superscope").With("Thickness", 2.0f).With("Smoothing", 0.4f),
                EffectRegistry.Create("ColorFade").With("Mode", "HSV").With("Speed", 0.30f),
                EffectRegistry.Create("Glow").With("Radius", 5f).With("Intensity", 0.55f)
            };
        }
        public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
        {
            foreach (var n in _stack) n.Process(f, canvas);
        }
    }
}
