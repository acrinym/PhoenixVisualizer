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
            _stack = new IEffectNode[] {
                EffectRegistry.CreateByName("ClearFrame"),
                EffectRegistry.CreateByName("HexGrid").With("Size", 28).With("Thickness", 1.0f),
                EffectRegistry.CreateByName("BeatScale").With("Amount", 0.22f),
                EffectRegistry.CreateByName("ColorFade").With("Mode", "HSV").With("Speed", 0.28f)
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

