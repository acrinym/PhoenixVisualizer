using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Core.Transpile;
using SkiaSharp;

namespace PhoenixVisualizer.App.Rendering
{
    public class UnifiedPhoenixVisualizer : IVisualizerPlugin
    {
        private UnifiedGraph? _graph;
        private int _width;
        private int _height;

        public string Id => "unified_phoenix";
        public string Name => "Unified Phoenix Visualizer";
        public string DisplayName => "Phoenix Visualizer";
        public string Description => "Renders Phoenix unified graph";
        public string Author => "Phoenix Visualizer";
        public string Version => "1.0";

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
            _graph = null;
        }

        public void LoadGraph(UnifiedGraph graph)
        {
            _graph = graph;
        }

        public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
        {
            canvas.Clear(0xFF0A0A0A);
            
            if (_graph == null || _graph.Nodes.Count == 0)
            {
                // Show no content message
                var centerX = _width / 2f;
                var centerY = _height / 2f;
                canvas.DrawText("No Phoenix content loaded", centerX - 120, centerY, 0xFFFFFFFF, 20);
                return;
            }

            // Render each node in the graph
            
            canvas.DrawText($"Phoenix Graph: {_graph.Nodes.Count} nodes", 10, 30, 0xFFAAAAAA, 16);
            
            // Simple visualization of the graph structure
            for (int i = 0; i < _graph.Nodes.Count; i++)
            {
                var node = _graph.Nodes[i];
                var y = 60 + (i * 30);
                canvas.DrawText($"{i + 1}. {node.DisplayName} ({node.TypeKey})", 10, y, 0xFFFFFFFF, 14);
            }
        }
    }
}
