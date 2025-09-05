using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals
{
    // Superscope ribbon with HSV color fade and glow
    public sealed class NodeScopeRibbon : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        private int _width, _height;

                public string Id => "node_scoperibbon";
                public string DisplayName => "Scope Ribbon";
        public NodeScopeRibbon()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.CreateByName("ClearFrame"),
                EffectRegistry.CreateByName("Superscope").With("Thickness", 2.0f).With("Smoothing", 0.4f),
                EffectRegistry.CreateByName("ColorFade").With("Mode", "HSV").With("Speed", 0.30f),
                EffectRegistry.CreateByName("Glow").With("Radius", 5f).With("Intensity", 0.55f)
            };
        }
        public void Initialize(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public void Resize(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public void Dispose()
        {
            // Clean up resources
        }

        public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
        {
            foreach (var n in _stack) n.Render(f.Spectrum, f.Spectrum, new RenderContext { Width = _width, Height = _height, Waveform = f.Spectrum, Spectrum = f.Spectrum, Time = f.Time, Beat = f.Beat, Volume = f.Volume, Canvas = new SkiaCanvasAdapter(canvas) });
        }
    }
}

