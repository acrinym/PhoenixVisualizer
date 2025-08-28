using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Plugins.Avs;

/// <summary>
/// Placeholder AVS effects visualizer.
/// Actual effect graph implementation pending.
/// </summary>
public class AvsEffectsVisualizer : IVisualizerPlugin
{
    public string Id => "avs_effects_engine";
    public string DisplayName => "AVS Effects Engine";

    // Stub properties for UI compatibility
    public int MaxActiveEffects { get; set; } = 8;
    public bool AutoRotateEffects { get; set; } = true;
    public float EffectRotationSpeed { get; set; } = 1.0f;
    public bool BeatReactive { get; set; } = true;
    public bool ShowEffectNames { get; set; } = true;
    public bool ShowEffectGrid { get; set; } = true;
    public float EffectSpacing { get; set; } = 20.0f;

    // Stub collections for effect management
    private readonly List<string> _availableEffects = new() { "placeholder_effect_1", "placeholder_effect_2" };
    private readonly List<string> _activeEffects = new();

    public void Initialize(int width, int height) { }
    public void Resize(int width, int height) { }
    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        canvas.Clear(0xFF000000);
    }
    public void Dispose() { }

    // Stub methods for effect management
    public List<string> GetAvailableEffectNames() => _availableEffects.ToList();
    public List<string> GetActiveEffectNames() => _activeEffects.ToList();
    public int GetAvailableEffectCount() => _availableEffects.Count;
    
    public void AddEffect(string effectName)
    {
        if (!_activeEffects.Contains(effectName) && _activeEffects.Count < MaxActiveEffects)
        {
            _activeEffects.Add(effectName);
        }
    }
    
    public void RemoveEffect(string effectName)
    {
        _activeEffects.Remove(effectName);
    }
    
    public void RemoveEffect(int index)
    {
        if (index >= 0 && index < _activeEffects.Count)
        {
            _activeEffects.RemoveAt(index);
        }
    }
    
    public void RefreshEffectsList()
    {
        // Placeholder - would refresh from actual effect system
    }
    
    public void DebugEffectDiscovery()
    {
        System.Diagnostics.Debug.WriteLine($"Available effects: {_availableEffects.Count}");
    }
    
    public void Configure()
    {
        // Placeholder for configuration
    }
    
    public void SetEffectCount(int count)
    {
        MaxActiveEffects = Math.Max(1, Math.Min(count, 16));
    }
}
