using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals
{
    public sealed class NodeBarsReactive : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        private int _width, _height;

        public NodeBarsReactive()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.CreateByName("ClearFrame"),
                EffectRegistry.CreateByName("SpectrumAnalyzer"),
                EffectRegistry.CreateByName("ColorFade"),
                EffectRegistry.CreateByName("Glow")
            };
        }

        public string Id => "node_barsreactive";
        public string DisplayName => "Bars Reactive";

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
            // Convert AudioFeatures to the format expected by IEffectNode
            var waveform = f.Waveform ?? new float[512]; // Use actual waveform data
            var spectrum = f.Fft ?? new float[256]; // Use actual spectrum data
            
            // Create render context with adapter
            var ctx = new RenderContext
            {
                Width = _width,
                Height = _height,
                Waveform = waveform,
                Spectrum = spectrum,
                Time = f.Time,
                Beat = f.Beat,
                Volume = f.Volume,
                Canvas = new SkiaCanvasAdapter(canvas)
            };
            
            // Render each node in the stack
            foreach (var node in _stack)
            {
                node.Render(waveform, spectrum, ctx);
            }
        }
    }
}
