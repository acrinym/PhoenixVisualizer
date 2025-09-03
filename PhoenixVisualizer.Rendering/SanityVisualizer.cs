using PhoenixVisualizer.PluginHost;
using SkiaSharp;

namespace PhoenixVisualizer.Rendering
{
    public class SanityVisualizer : IVisualizerPlugin
    {
        public string Id => "sanity";
        public string Name => "Sanity Visualizer";
        public string DisplayName => "Sanity Visualizer";
        public string Description => "Default sanity check visualizer";
        public string Author => "Phoenix Visualizer";
        public string Version => "1.0";

        private int _width;
        private int _height;

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
        
        public void Dispose() { }

        public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
        {
            canvas.Clear(0xFF1A1A1A);
            var centerX = _width / 2f;
            var centerY = _height / 2f;
            canvas.DrawText("Sanity Visualizer", centerX - 100, centerY, 0xFFFFFFFF, 24);
            canvas.DrawText("Ready for content", centerX - 80, centerY + 30, 0xFFAAAAAA, 16);
        }
    }
}
