using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals
{
    // Hex grid that pulses with bass and shimmers with treble
    public sealed class NodeHexGridPulse : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        private int _width, _height;

                public string Id => "node_hexgridpulse";
                public string DisplayName => "Hex Grid Pulse";
        public NodeHexGridPulse()
        {
            var clearFrame = EffectRegistry.CreateByName("ClearFrame") ?? throw new InvalidOperationException("Failed to create ClearFrame effect");
            var hexGrid = EffectRegistry.CreateByName("HexGrid") ?? throw new InvalidOperationException("Failed to create HexGrid effect");
            var beatScale = EffectRegistry.CreateByName("BeatScale") ?? throw new InvalidOperationException("Failed to create BeatScale effect");
            var colorFade = EffectRegistry.CreateByName("ColorFade") ?? throw new InvalidOperationException("Failed to create ColorFade effect");
            
            _stack = new IEffectNode[] {
                clearFrame,
                hexGrid.With("Size", 28).With("Thickness", 1.0f),
                beatScale.With("Amount", 0.22f),
                colorFade.With("Mode", "HSV").With("Speed", 0.28f)
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

