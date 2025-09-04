using System.Text.Json;
using PhoenixVisualizer.Core.Nodes;

namespace PhoenixVisualizer.App.Services
{
    public static class NodePresetStorage
    {
        public static void Save(string path, IEffectNode[] stack)
        {
            var model = stack.Select(n => n.ToModel()).ToArray(); // assumes each node can produce a serializable model
            var json = JsonSerializer.Serialize(model, new JsonSerializerOptions{ WriteIndented = true });
            File.WriteAllText(path, json);
        }

        public static IEffectNode[] Load(string path)
        {
            var json = File.ReadAllText(path);
            var model = JsonSerializer.Deserialize<NodeModel[]>(json) ?? Array.Empty<NodeModel>();
            return model.Select(m => EffectRegistry.Create(m.Type).WithModel(m)).ToArray();
        }
    }

    // Fallback serializable model; adapt to your nodes' schema.
    public sealed class NodeModel
    {
        public string Type { get; set; } = "";
        public Dictionary<string, object>? Params { get; set; }
    }
}
