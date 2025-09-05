
using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Core.Nodes.XSS;

namespace PhoenixVisualizer.Visuals
{
    public sealed class NodeXsLightning : IVisualizerPlugin
    {
        private IEffectNode _node = new XsLightningNode();
        public string Id => "node_xs_lightning";
        public string DisplayName => "XS Lightning (Node)";
        private int _w,_h;
        public void Initialize(int width, int height){ _w=width; _h=height; }
        public void Resize(int width, int height){ _w=width; _h=height; }
        public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
        {
            var ctx = new RenderContext { Width=_w, Height=_h, Waveform=features.Waveform, Spectrum=features.Spectrum, Time=features.Time, Beat=features.Beat, Volume=features.Volume, Canvas=canvas };
            _node.Render(features.Waveform, features.Spectrum, ctx);
        }
        public void Dispose(){}
    }
}
