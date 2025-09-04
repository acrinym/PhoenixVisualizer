
using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.Core.Nodes.XSS;

namespace PhoenixVisualizer.Visuals
{
    public sealed class NodeXsRotor : IVisualizerPlugin
    {
        private IEffectNode _rotor = new XsRotorNode();
        public string Id => "node_xs_rotor";
        public string DisplayName => "XS Rotor (Node)";
        private int _w,_h;
        public void Initialize(int width, int height){ _w=width; _h=height; }
        public void Resize(int width, int height){ _w=width; _h=height; }
        public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
        {
            var ctx = new RenderContext { Width=_w, Height=_h, Waveform=features.Waveform, Spectrum=features.Spectrum, Time=features.Time, Beat=features.Beat, Volume=features.Volume, Canvas=canvas };
            _rotor.Render(features.Waveform, features.Spectrum, ctx);
        }
        public void Dispose(){}
    }
}
