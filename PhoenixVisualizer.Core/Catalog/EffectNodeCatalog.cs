using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using PhoenixVisualizer.Core.Transpile;
using PhoenixVisualizer.Core.Effects.Nodes;

namespace PhoenixVisualizer.Core.Catalog;

public sealed record NodeMeta(
    string TypeKey,
    string DisplayName,
    string Category,
    Func<UnifiedEffectNode> CreateNode,
    IReadOnlyList<string>? Tags = null,
    string? Icon = null
);

/// <summary>
/// Discovers built-in effect nodes (via reflection) and allows JSON-defined nodes.
/// Produces UnifiedEffectNode instances for the editor/renderer stack.
/// </summary>
public static class EffectNodeCatalog
{
    private static readonly ConcurrentDictionary<string, NodeMeta> _byKey =
        new(StringComparer.OrdinalIgnoreCase);

    public static event Action? CatalogChanged;

    static EffectNodeCatalog()
    {
        // Built-ins we guarantee:
        Register(new NodeMeta(
            TypeKey: "superscope",
            DisplayName: "Superscope",
            Category: "Scopes",
            CreateNode: () =>
            {
                var n = new UnifiedEffectNode { TypeKey = "superscope", DisplayName = "Superscope" };
                n.Parameters["init"] = "";
                n.Parameters["frame"] = "";
                n.Parameters["beat"] = "";
                n.Parameters["point"] = "";
                n.Parameters["samples"] = 512;
                return n;
            },
            Tags: new []{ "avs", "phoenix", "code", "scope" }
        ));

        Register(new NodeMeta(
            TypeKey: "clear",
            DisplayName: "Clear",
            Category: "Render",
            CreateNode: () =>
            {
                var n = new UnifiedEffectNode { TypeKey = "clear", DisplayName = "Clear" };
                n.Parameters["color"] = "#000000";
                return n;
            },
            Tags: new []{ "fill","background" }
        ));

        Register(new NodeMeta(
            TypeKey: "text",
            DisplayName: "Text",
            Category: "Render",
            CreateNode: () =>
            {
                var n = new UnifiedEffectNode { TypeKey = "text", DisplayName = "Text" };
                n.Parameters["content"] = "Phoenix";
                n.Parameters["x"] = 20;
                n.Parameters["y"] = 32;
                n.Parameters["size"] = 24;
                n.Parameters["color"] = "#00FFFF";
                return n;
            },
            Tags: new []{ "label","font" }
        ));

        Register(new NodeMeta(
            TypeKey: "circle",
            DisplayName: "Circle",
            Category: "Render",
            CreateNode: () =>
            {
                var n = new UnifiedEffectNode { TypeKey = "circle", DisplayName = "Circle" };
                n.Parameters["x"] = 128;
                n.Parameters["y"] = 128;
                n.Parameters["radius"] = 64;
                n.Parameters["filled"] = false;
                n.Parameters["color"] = "#FFFFFF";
                n.Parameters["stroke"] = 2.0;
                return n;
            },
            Tags: new []{ "shape" }
        ));

        // Discover advanced nodes if present (Core.Effects.Nodes.*). This aligns with your EffectsGraph/Nodes infra. 
        // NOTE: We convert them to UnifiedEffectNode so the editor+renderer pipeline stays consistent.
        try { ReflectBuiltInNodesFromAssembly(typeof(BaseEffectNode).Assembly); } catch { /* optional */ }
    }

    public static void Register(NodeMeta meta)
    {
        _byKey[meta.TypeKey] = meta;
        CatalogChanged?.Invoke();
    }

    public static IEnumerable<NodeMeta> All() => _byKey.Values.OrderBy(m => m.Category).ThenBy(m => m.DisplayName);
    public static bool TryGet(string typeKey, out NodeMeta meta) => _byKey.TryGetValue(typeKey, out meta);
    public static UnifiedEffectNode Create(string typeKey) => _byKey.TryGetValue(typeKey, out var m) ? m.CreateNode() :
        new UnifiedEffectNode { TypeKey = typeKey, DisplayName = typeKey };

    public static void LoadFolder(string folder)
    {
        if (!Directory.Exists(folder)) return;
        foreach (var file in Directory.EnumerateFiles(folder, "*.json"))
        {
            try
            {
                var doc = JsonSerializer.Deserialize<EffectDoc>(File.ReadAllText(file), new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                }) ?? new EffectDoc();
                foreach (var def in doc.Effects)
                {
                    var localDef = def; // avoid modified closure
                    Register(new NodeMeta(
                        localDef.TypeKey, localDef.DisplayName ?? localDef.TypeKey, localDef.Category ?? "Custom",
                        CreateNode: () =>
                        {
                            var n = new UnifiedEffectNode { TypeKey = localDef.TypeKey, DisplayName = localDef.DisplayName ?? localDef.TypeKey };
                            if (localDef.Parameters is not null)
                                foreach (var (k, v) in localDef.Parameters) n.Parameters[k] = v;
                            return n;
                        },
                        Tags: localDef.Tags,
                        Icon: localDef.Icon
                    ));
                }
            }
            catch { /* ignore malformed */ }
        }
    }

    private static void ReflectBuiltInNodesFromAssembly(Assembly asm)
    {
        var nodeTypes = asm.GetTypes()
            .Where(t => !t.IsAbstract && typeof(BaseEffectNode).IsAssignableFrom(t))
            .ToList();
        foreach (var t in nodeTypes)
        {
            var typeKey = t.Name; // stable-ish; you can map explicitly later
            Register(new NodeMeta(
                TypeKey: typeKey,
                DisplayName: t.Name.Replace("Node",""),
                Category: "Advanced",
                CreateNode: () =>
                {
                    var n = new UnifiedEffectNode { TypeKey = typeKey, DisplayName = t.Name.Replace("Node","") };
                    // seed parameter placeholders; your advanced nodes can refine via ParamEditor later
                    return n;
                }
            ));
        }
    }

    // JSON schema
    public sealed class EffectDoc
    {
        public List<EffectDef> Effects { get; set; } = new();
    }
    public sealed class EffectDef
    {
        public string TypeKey { get; set; } = "";
        public string? DisplayName { get; set; }
        public string? Category { get; set; }
        public Dictionary<string, object?>? Parameters { get; set; }
        public List<string>? Tags { get; set; }
        public string? Icon { get; set; }
    }
}
