using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals
{
    // Text echo: text -> extrude/echo -> trails; tie to beat
    public sealed class NodeTextEcho : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        private int _width, _height;

                public string Id => "node_textecho";
                public string DisplayName => "Text Echo";
        public NodeTextEcho()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.CreateByName("ClearFrame"),
                EffectRegistry.CreateByName("Text").With("Content", "PHOENIX").With("Size", 64),
                EffectRegistry.CreateByName("Echo").With("Count", 12).With("Decay", 0.88f),
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

        public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
        {
            foreach (var n in _stack) n.Render(f.Spectrum, f.Spectrum, new RenderContext { Width = _width, Height = _height, Waveform = f.Spectrum, Spectrum = f.Spectrum, Time = f.Time, Beat = f.Beat, Volume = f.Volume, Canvas = new SkiaCanvasAdapter(canvas) });
        }
    }
}

