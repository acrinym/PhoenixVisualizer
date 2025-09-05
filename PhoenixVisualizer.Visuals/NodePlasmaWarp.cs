using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals
{
    // Plasma + warp with bass spin
    public sealed class NodePlasmaWarp : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        private int _width, _height;

                public string Id => "node_plasmawarp";
                public string DisplayName => "Plasma Warp";
        public NodePlasmaWarp()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.CreateByName("ClearFrame"),
                EffectRegistry.CreateByName("Plasma").With("Detail", 0.8f),
                EffectRegistry.CreateByName("PolarWarp").With("Spin", 0.18f).With("Zoom", 0.12f),
                EffectRegistry.CreateByName("ColorFade").With("Mode", "HSV").With("Speed", 0.18f)
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

