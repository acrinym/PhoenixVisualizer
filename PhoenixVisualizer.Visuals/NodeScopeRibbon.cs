using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Visuals
{
    // Superscope ribbon with HSV color fade and glow
    public sealed class NodeScopeRibbon : IVisualizerPlugin
    {
        private IEffectNode[] _stack;
        private int _width, _height;

                public string Id => "node_scoperibbon";
                public string DisplayName => "Scope Ribbon";
        public NodeScopeRibbon()
        {
            var clearFrame = EffectRegistry.CreateByName("ClearFrame") ?? throw new InvalidOperationException("Failed to create ClearFrame effect");
            var superscope = EffectRegistry.CreateByName("Superscope") ?? throw new InvalidOperationException("Failed to create Superscope effect");
            var colorFade = EffectRegistry.CreateByName("ColorFade") ?? throw new InvalidOperationException("Failed to create ColorFade effect");
            var glow = EffectRegistry.CreateByName("Glow") ?? throw new InvalidOperationException("Failed to create Glow effect");
            
            _stack = new IEffectNode[] {
                clearFrame,
                superscope.With("Thickness", 2.0f).With("Smoothing", 0.4f),
                colorFade.With("Mode", "HSV").With("Speed", 0.30f),
                glow.With("Radius", 5f).With("Intensity", 0.55f)
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

