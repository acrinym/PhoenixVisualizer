using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.App
{
    public class TestVisualizer : IVisualizerPlugin
    {
        public string Id => "test_visualizer";
        public string DisplayName => "Test Visualizer";

        private int _width, _height;

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

        public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
        {
            // Clear with black background
            canvas.Clear(0xFF000000);
            
            // Draw a simple circle that responds to audio
            var energy = features.Energy;
            var radius = 50 + (energy * 100);
            var color = (uint)(0xFF0000FF + (int)(energy * 255));
            
            canvas.FillCircle(_width / 2, _height / 2, radius, color);
        }

        public void Dispose()
        {
            // Clean up resources
        }
    }
}


