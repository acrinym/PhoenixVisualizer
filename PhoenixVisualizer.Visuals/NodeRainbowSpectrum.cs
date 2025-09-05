using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals
{
    public sealed class NodeRainbowSpectrum : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        private int _width, _height;

        public NodeRainbowSpectrum()
        {
            var clearFrame = EffectRegistry.CreateByName("ClearFrame") ?? throw new InvalidOperationException("Failed to create ClearFrame effect");
            var spectrumAnalyzer = EffectRegistry.CreateByName("SpectrumAnalyzer") ?? throw new InvalidOperationException("Failed to create SpectrumAnalyzer effect");
            var colorFade = EffectRegistry.CreateByName("ColorFade") ?? throw new InvalidOperationException("Failed to create ColorFade effect");
            var glow = EffectRegistry.CreateByName("Glow") ?? throw new InvalidOperationException("Failed to create Glow effect");
            
            _stack = new IEffectNode[] {
                clearFrame,
                spectrumAnalyzer,
                colorFade,
                glow
            };
        }

        public string Id => "node_rainbowspectrum";
        public string DisplayName => "Rainbow Spectrum";

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