using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals
{
    // Audio-reactive flow field particles
    public sealed class NodeAudioFlowField : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        private int _width, _height;

                public string Id => "node_audioflowfield";
                public string DisplayName => "Audio Flow Field";
        public NodeAudioFlowField()
        {
            _stack = new IEffectNode[] {
                EffectRegistry.CreateByName("ClearFrame"),
                EffectRegistry.CreateByName("FlowField").With("Scale", 0.015f).With("Strength", 0.8f),
                EffectRegistry.CreateByName("ParticleSystem").With("Max", 2000).With("Rate", 400),
                EffectRegistry.CreateByName("Trails").With("Decay", 0.93f)
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
