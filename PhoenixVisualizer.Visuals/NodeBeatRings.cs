using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals
{
    // Concentric rings pulsing on beats, deformed by spectrum
    public sealed class NodeBeatRings : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        private int _width, _height;

                public string Id => "node_beatrings";
                public string DisplayName => "Beat Rings";
        public NodeBeatRings()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.CreateByName("ClearFrame"),
                EffectRegistry.CreateByName("Rings").With("Count", 8).With("Thickness", 2.5f),
                EffectRegistry.CreateByName("BeatScale").With("Amount", 0.25f),
                EffectRegistry.CreateByName("Deform").With("Amount", 0.15f).With("Band", "All"),
                EffectRegistry.CreateByName("ColorFade").With("Mode", "HSV").With("Speed", 0.20f)
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
