using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.PluginHost;
namespace PhoenixVisualizer.Visuals
{
    public sealed class NodeGeoLattice : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        private int _width, _height;

                public string Id => "node_geolattice";
                public string DisplayName => "Geo Lattice";
        public NodeGeoLattice()
        {
            var clearFrame = EffectRegistry.CreateByName("ClearFrame") ?? throw new InvalidOperationException("Failed to create ClearFrame effect");
            var grid = EffectRegistry.CreateByName("Grid") ?? throw new InvalidOperationException("Failed to create Grid effect");
            var deform = EffectRegistry.CreateByName("Deform") ?? throw new InvalidOperationException("Failed to create Deform effect");
            var colorFade = EffectRegistry.CreateByName("ColorFade") ?? throw new InvalidOperationException("Failed to create ColorFade effect");
            
            _stack = new IEffectNode[] {
                clearFrame,
                grid.With("Size", 20).With("Thickness", 1.0f),
                deform.With("Band", "Treble").With("Amount", 0.22f),
                colorFade.With("Mode", "HSV").With("Speed", 0.26f)
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
