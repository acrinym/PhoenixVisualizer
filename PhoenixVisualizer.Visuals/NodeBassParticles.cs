using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.PluginHost;
namespace PhoenixVisualizer.Visuals
{
    public sealed class NodeBassParticles : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        private int _width, _height;

                public string Id => "node_bassparticles";
                public string DisplayName => "Bass Particles";
        public NodeBassParticles()
        {
            var clearFrame = EffectRegistry.CreateByName("ClearFrame") ?? throw new InvalidOperationException("ClearFrame effect not found");
            var beatDetect = EffectRegistry.CreateByName("BeatDetect") ?? throw new InvalidOperationException("BeatDetect effect not found");
            var particleSystem = EffectRegistry.CreateByName("ParticleSystem") ?? throw new InvalidOperationException("ParticleSystem effect not found");
            var trails = EffectRegistry.CreateByName("Trails") ?? throw new InvalidOperationException("Trails effect not found");

            _stack = new IEffectNode[] {
                clearFrame,
                beatDetect.With("Sensitivity", 0.95f),
                particleSystem.With("Max", 1500).With("EmitOnBeat", true).With("Rate", 260),
                trails.With("Decay", 0.92f)
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
