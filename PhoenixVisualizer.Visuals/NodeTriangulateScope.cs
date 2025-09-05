using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.PluginHost;
namespace PhoenixVisualizer.Visuals
{
    public sealed class NodeTriangulateScope : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        private int _width, _height;

                public string Id => "node_triangulatescope";
                public string DisplayName => "Triangulate Scope";
        public NodeTriangulateScope()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.CreateByName("ClearFrame"),
                EffectRegistry.CreateByName("Superscope").With("Thickness", 1.2f).With("Smoothing", 0.4f),
                EffectRegistry.CreateByName("Triangulate").With("Density", 0.7f),
                EffectRegistry.CreateByName("Colorize").With("Palette", "Aurora")
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
