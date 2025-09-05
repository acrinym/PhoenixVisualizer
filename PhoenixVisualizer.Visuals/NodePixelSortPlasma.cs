using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.PluginHost;
namespace PhoenixVisualizer.Visuals
{
    public sealed class NodePixelSortPlasma : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        private int _width, _height;

                public string Id => "node_pixelsortplasma";
                public string DisplayName => "Pixel Sort Plasma";
        public NodePixelSortPlasma()
        {
            var clearFrame = EffectRegistry.CreateByName("ClearFrame") ?? throw new InvalidOperationException("Failed to create ClearFrame effect");
            var plasma = EffectRegistry.CreateByName("Plasma") ?? throw new InvalidOperationException("Failed to create Plasma effect");
            var pixelSort = EffectRegistry.CreateByName("PixelSort") ?? throw new InvalidOperationException("Failed to create PixelSort effect");
            var colorFade = EffectRegistry.CreateByName("ColorFade") ?? throw new InvalidOperationException("Failed to create ColorFade effect");
            
            _stack = new IEffectNode[] {
                clearFrame,
                plasma.With("Detail", 0.9f),
                pixelSort.With("Threshold", 0.65f).With("Direction", "Vertical"),
                colorFade.With("Mode", "HSV").With("Speed", 0.20f)
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
