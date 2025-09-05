using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals
{
    // Spectrum-driven nebula using blur+color layers
    public sealed class NodeSpectrumNebula : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        private int _width, _height;

                public string Id => "node_spectrumnebula";
                public string DisplayName => "Spectrum Nebula";
        public NodeSpectrumNebula()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.CreateByName("ClearFrame"),
                EffectRegistry.CreateByName("SpectrumAnalyzer").With("Bars", false).With("Smoothing", 0.35f),
                EffectRegistry.CreateByName("Colorize").With("Palette", "Nebula"),
                EffectRegistry.CreateByName("GaussianBlur").With("Radius", 3.0f),
                EffectRegistry.CreateByName("Glow").With("Radius", 6.0f).With("Intensity", 0.5f)
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
            foreach (var n in _stack) n.Render(f.Spectrum, f.Spectrum, new RenderContext { Width = _width, Height = _height, Waveform = f.Spectrum, Spectrum = f.Spectrum, Time = f.Time, Beat = f.Beat, Volume = f.Volume, Canvas = canvas });
        }
    }
}
