using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.PluginHost;
namespace PhoenixVisualizer.Visuals
{
    public sealed class NodeScopeKaleidoGlow : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        private int _width, _height;

                public string Id => "node_scopekaleidoglow";
                public string DisplayName => "Scope Kaleido Glow";
        public NodeScopeKaleidoGlow()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.CreateByName("ClearFrame"),
                EffectRegistry.CreateByName("Superscope").With("Thickness", 1.5f).With("Smoothing", 0.42f),
                EffectRegistry.CreateByName("Kaleidoscope").With("Segments", 7),
                EffectRegistry.CreateByName("Glow").With("Radius", 5.5f).With("Intensity", 0.6f)
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

        public void RenderFrame(AudioFeatures f, ISkiaCanvas c){ foreach (var n in _stack) n.Render(f.Spectrum, f.Spectrum, new RenderContext { Width = _width, Height = _height, Waveform = f.Spectrum, Spectrum = f.Spectrum, Time = f.Time, Beat = f.Beat, Volume = f.Volume, Canvas = new SkiaCanvasAdapter(c) }); }
    }
}
