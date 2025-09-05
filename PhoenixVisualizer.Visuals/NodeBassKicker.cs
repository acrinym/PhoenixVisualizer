using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.PluginHost;
namespace PhoenixVisualizer.Visuals
{
    public sealed class NodeBassKicker : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        private int _width, _height;

                public string Id => "node_basskicker";
                public string DisplayName => "Bass Kicker";
        public NodeBassKicker()
        {
            var clearFrame = EffectRegistry.CreateByName("ClearFrame") ?? throw new InvalidOperationException("ClearFrame effect not found");
            var beatDetect = EffectRegistry.CreateByName("BeatDetect") ?? throw new InvalidOperationException("BeatDetect effect not found");
            var rings = EffectRegistry.CreateByName("Rings") ?? throw new InvalidOperationException("Rings effect not found");
            var beatScale = EffectRegistry.CreateByName("BeatScale") ?? throw new InvalidOperationException("BeatScale effect not found");
            var colorFade = EffectRegistry.CreateByName("ColorFade") ?? throw new InvalidOperationException("ColorFade effect not found");
            
            _stack = new IEffectNode[] {
                clearFrame,
                beatDetect.With("Sensitivity", 1.0f),
                rings.With("Count", 5).With("Thickness", 3.0f),
                beatScale.With("Amount", 0.35f),
                colorFade.With("Mode", "HSV").With("Speed", 0.18f)
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
