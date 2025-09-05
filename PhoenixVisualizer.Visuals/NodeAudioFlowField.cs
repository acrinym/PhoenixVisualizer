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
            var clearFrame = EffectRegistry.CreateByName("ClearFrame") ?? throw new InvalidOperationException("ClearFrame effect not found");
            var flowField = EffectRegistry.CreateByName("FlowField") ?? throw new InvalidOperationException("FlowField effect not found");
            var particleSystem = EffectRegistry.CreateByName("ParticleSystem") ?? throw new InvalidOperationException("ParticleSystem effect not found");
            var trails = EffectRegistry.CreateByName("Trails") ?? throw new InvalidOperationException("Trails effect not found");

            _stack = new IEffectNode[] {
                clearFrame,
                flowField.With("Scale", 0.015f).With("Strength", 0.8f),
                particleSystem.With("Max", 2000).With("Rate", 400),
                trails.With("Decay", 0.93f)
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

