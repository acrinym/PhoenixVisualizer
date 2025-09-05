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
            var clearFrame = EffectRegistry.CreateByName("ClearFrame") ?? throw new InvalidOperationException("Failed to create ClearFrame effect");
            var rings = EffectRegistry.CreateByName("Rings") ?? throw new InvalidOperationException("Failed to create Rings effect");
            var beatScale = EffectRegistry.CreateByName("BeatScale") ?? throw new InvalidOperationException("Failed to create BeatScale effect");
            var deform = EffectRegistry.CreateByName("Deform") ?? throw new InvalidOperationException("Failed to create Deform effect");
            var colorFade = EffectRegistry.CreateByName("ColorFade") ?? throw new InvalidOperationException("Failed to create ColorFade effect");
            
            _stack = new IEffectNode[] {
                clearFrame,
                rings.With("Count", 8).With("Thickness", 2.5f),
                beatScale.With("Amount", 0.25f),
                deform.With("Amount", 0.15f).With("Band", "All"),
                colorFade.With("Mode", "HSV").With("Speed", 0.20f)
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

