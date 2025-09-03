using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhoenixVisualizer.Parameters;

public enum ParamType { Slider, Checkbox, Dropdown, Color, Text, Dial, File, Directory }

public sealed class ParamDef
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public ParamType Type { get; init; } = ParamType.Text;
    public object? DefaultValue { get; init; }
    public double? Min { get; init; }
    public double? Max { get; init; }
    public List<string>? Options { get; init; }
    public string Category { get; init; } = "General";
    public string? Description { get; init; }
    public bool RequiresRestart { get; init; }
}

public static class ParamRegistry
{
    private static readonly ConcurrentDictionary<string, Dictionary<string, ParamDef>> _defs = new();
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object?>> _vals = new();

    public static void Register(string visualizerId, IEnumerable<ParamDef> defs)
    {
        _defs[visualizerId] = defs.ToDictionary(d => d.Key, d => d, StringComparer.OrdinalIgnoreCase);
        _vals.TryAdd(visualizerId, new());
        foreach (var d in defs)
            _vals[visualizerId].TryAdd(d.Key, d.DefaultValue);
        DefinitionsChanged?.Invoke(visualizerId);
    }

    public static IReadOnlyDictionary<string, ParamDef> GetDefs(string visualizerId)
        => _defs.TryGetValue(visualizerId, out var d) ? d : new Dictionary<string, ParamDef>();

    public static IReadOnlyDictionary<string, object?> GetValues(string visualizerId)
        => _vals.TryGetValue(visualizerId, out var v) ? v : new ConcurrentDictionary<string, object?>();

    public static event Action<string, string, object?>? ValueChanged;
    public static event Action<string>? DefinitionsChanged;

    public static void Set(string visualizerId, string key, object? value)
    {
        var map = _vals.GetOrAdd(visualizerId, _ => new());
        map[key] = value;
        ValueChanged?.Invoke(visualizerId, key, value);
    }

    public static bool TryGet(string visualizerId, string key, out object? value)
    {
        value = null;
        return _vals.TryGetValue(visualizerId, out var map) && map.TryGetValue(key, out value);
    }
}

public static class ParamJson
{
    private static readonly JsonSerializerOptions Opt = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public sealed class ParamDoc
    {
        public string VisualizerId { get; set; } = "";
        public string VisualizerName { get; set; } = "";
        public List<ParamDef> Definitions { get; set; } = new();
        public Dictionary<string, object?> Values { get; set; } = new();
    }

    public static byte[] Save(string vizId, string vizName)
    {
        var doc = new ParamDoc
        {
            VisualizerId = vizId,
            VisualizerName = vizName,
            Definitions = ParamRegistry.GetDefs(vizId).Values.ToList(),
            Values = ParamRegistry.GetValues(vizId).ToDictionary(kv => kv.Key, kv => kv.Value)
        };
        return JsonSerializer.SerializeToUtf8Bytes(doc, Opt);
    }

    public static void Load(byte[] json)
    {
        var doc = JsonSerializer.Deserialize<ParamDoc>(json, Opt) ?? new();
        ParamRegistry.Register(doc.VisualizerId, doc.Definitions);
        foreach (var (k, v) in doc.Values)
            ParamRegistry.Set(doc.VisualizerId, k, v);
    }

    // Load all *.json param docs from a folder
    public static void LoadFolder(string folder)
    {
        if (!Directory.Exists(folder)) return;
        foreach (var file in Directory.EnumerateFiles(folder, "*.json"))
        {
            try { Load(File.ReadAllBytes(file)); } catch { /* skip bad docs */ }
        }
    }
}
