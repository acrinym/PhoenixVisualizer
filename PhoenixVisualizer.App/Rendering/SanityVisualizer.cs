using PhoenixVisualizer.PluginHost;
using SkiaSharp;

namespace PhoenixVisualizer.App.Rendering
{
    public class SanityVisualizer : IVisualizerPlugin
    {
        public string Id => "sanity";
        public string Name => "Sanity Visualizer";
        public string DisplayName => "Sanity Visualizer";
        public string Description => "Default sanity check visualizer";
        public string Author => "Phoenix Visualizer";
        public string Version => "1.0";

        public void Initialize(int width, int height)
        {
            // No initialization needed
        }

        public void Resize(int width, int height)
        {
            // No resize handling needed
        }

        public void Dispose()
        {
            // No cleanup needed
        }

        public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
        {
            // Draw a simple test pattern
            canvas.Clear(0xFF1A1A1A);
            
            var centerX = canvas.Width / 2f;
            var centerY = canvas.Height / 2f;
            
            canvas.DrawText("Sanity Visualizer", centerX - 100, centerY, 0xFFFFFFFF, 24);
            canvas.DrawText("Ready for content", centerX - 80, centerY + 30, 0xFFAAAAAA, 16);
        }
    }
}
