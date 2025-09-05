using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.PluginHost;
namespace PhoenixVisualizer.Visuals
{
    public sealed class NodeBeatKaleidoTunnel : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        private int _width, _height;

                public string Id => "node_beatkaleidotunnel";
                public string DisplayName => "Beat Kaleido Tunnel";
        public NodeBeatKaleidoTunnel()
        {
            var clearFrame = EffectRegistry.CreateByName("ClearFrame") ?? throw new InvalidOperationException("Failed to create ClearFrame effect");
            var waveform = EffectRegistry.CreateByName("Waveform") ?? throw new InvalidOperationException("Failed to create Waveform effect");
            var polarWarp = EffectRegistry.CreateByName("PolarWarp") ?? throw new InvalidOperationException("Failed to create PolarWarp effect");
            var kaleidoscope = EffectRegistry.CreateByName("Kaleidoscope") ?? throw new InvalidOperationException("Failed to create Kaleidoscope effect");
            var trails = EffectRegistry.CreateByName("Trails") ?? throw new InvalidOperationException("Failed to create Trails effect");
            
            _stack = new IEffectNode[] {
                clearFrame,
                waveform.With("Mode", "Rings").With("Gain", 1.1f),
                polarWarp.With("Spin", 0.2f).With("Zoom", 0.12f),
                kaleidoscope.With("Segments", 6),
                trails.With("Decay", 0.90f)
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
