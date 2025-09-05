using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals
{
    public sealed class NodePulseTunnel : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        private int _width, _height;

        public NodePulseTunnel()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.CreateByName("ClearFrame"),
                EffectRegistry.CreateByName("Waveform"),
                EffectRegistry.CreateByName("PolarWarp"),
                EffectRegistry.CreateByName("Trails")
            };
        }

        public string Id => "node_pulsetunnel";
        public string DisplayName => "Pulse Tunnel";

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
            var waveform = new float[512]; // Placeholder - would need actual waveform data
            var spectrum = new float[256]; // Placeholder - would need actual spectrum data
            
            // Create render context
            var ctx = new RenderContext
            {
                Width = _width,
                Height = _height,
                Waveform = waveform,
                Spectrum = spectrum,
                Time = 0.0f, // Placeholder - would need actual time
                Beat = f.Beat,
                Volume = f.Volume,
                Canvas = canvas
            };
            
            // Render each node in the stack
            foreach (var node in _stack)
            {
                node.Render(waveform, spectrum, ctx);
            }
        }
    }
}
