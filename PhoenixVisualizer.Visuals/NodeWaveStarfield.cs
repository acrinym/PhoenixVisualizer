using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals
{
    // Waveform-driven starfield emitter with trail persistence
    public sealed class NodeWaveStarfield : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        private int _width, _height;

                public string Id => "node_wavestarfield";
                public string DisplayName => "Wave Starfield";
        public NodeWaveStarfield()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.CreateByName("ClearFrame"),
                EffectRegistry.CreateByName("Waveform").With("Mode", "Centered"),   // drives emission rate
                EffectRegistry.CreateByName("Starfield").With("Count", 1200).With("Speed", 0.9f),
                EffectRegistry.CreateByName("Trails").With("Decay", 0.92f)
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

