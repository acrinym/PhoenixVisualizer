using System;
using System.Text.Json;
using PhoenixVisualizer.Core.Transpile;

namespace PhoenixVisualizer.Core.Serialization
{
    public static class PhxVizSerializer
    {
        public static byte[] Save(UnifiedGraph graph)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(graph, options);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        public static UnifiedGraph Load(byte[] bytes)
        {
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Deserialize<UnifiedGraph>(json, options) ?? new UnifiedGraph();
        }
    }
}
