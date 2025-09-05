using System.Text.Json;
using PhoenixVisualizer.Core.Nodes;

namespace PhoenixVisualizer.App.Services
{
    public static class NodePresetStorage
    {
        public static void Save(string path, IEffectNode[] stack)
        {
            // Create a simple model from the node's name and parameters
            var model = stack.Select(n => new NodeModel 
            { 
                Type = n.Name, 
                Params = n.Params.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value.FloatValue) 
            }).ToArray();
            var json = JsonSerializer.Serialize(model, new JsonSerializerOptions{ WriteIndented = true });
            File.WriteAllText(path, json);
        }

        public static IEffectNode[] Load(string path)
        {
            var json = File.ReadAllText(path);
            var model = JsonSerializer.Deserialize<NodeModel[]>(json) ?? Array.Empty<NodeModel>();
            return model.Select(m => EffectRegistry.CreateByName(m.Type) ?? new ClearFrameNode()).ToArray();
        }
    }

    // Fallback serializable model; adapt to your nodes' schema.
    public sealed class NodeModel
    {
        public string Type { get; set; } = "";
        public Dictionary<string, object>? Params { get; set; }
    }
}
