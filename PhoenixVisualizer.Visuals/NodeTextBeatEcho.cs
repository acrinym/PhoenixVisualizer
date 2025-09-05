using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.PluginHost;
namespace PhoenixVisualizer.Visuals
{
    public sealed class NodeTextBeatEcho : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        private int _width, _height;

                public string Id => "node_textbeatecho";
                public string DisplayName => "Text Beat Echo";
        public NodeTextBeatEcho()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.CreateByName("ClearFrame"),
                EffectRegistry.CreateByName("Text").With("Content", "PHOENIX").With("Size", 72),
                EffectRegistry.CreateByName("BeatScale").With("Amount", 0.28f),
                EffectRegistry.CreateByName("Echo").With("Count", 10).With("Decay", 0.90f)
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
