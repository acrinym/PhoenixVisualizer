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
            _stack = new IEffectNode[] {
                EffectRegistry.CreateByName("ClearFrame"),
                EffectRegistry.CreateByName("Waveform").With("Mode", "Rings").With("Gain", 1.1f),
                EffectRegistry.CreateByName("PolarWarp").With("Spin", 0.2f).With("Zoom", 0.12f),
                EffectRegistry.CreateByName("Kaleidoscope").With("Segments", 6),
                EffectRegistry.CreateByName("Trails").With("Decay", 0.90f)
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
