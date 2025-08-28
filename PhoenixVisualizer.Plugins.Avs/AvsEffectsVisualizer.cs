using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.Core.Effects;

namespace PhoenixVisualizer.Plugins.Avs;

/// <summary>
/// AVS Effects Visualizer - Integrates with the Phoenix Effects Graph System
/// Provides access to all 42+ implemented AVS effects through the EffectRegistry
/// </summary>
public class AvsEffectsVisualizer : IVisualizerPlugin
{
    public string Id => "avs_effects_engine";
    public string DisplayName => "AVS Effects Engine";

    // Configuration properties for UI compatibility
    public int MaxActiveEffects { get; set; } = 8;
    public bool AutoRotateEffects { get; set; } = true;
    public float EffectRotationSpeed { get; set; } = 1.0f;
    public bool BeatReactive { get; set; } = true;
    public bool ShowEffectNames { get; set; } = true;
    public bool ShowEffectGrid { get; set; } = true;
    public float EffectSpacing { get; set; } = 20.0f;

    // Effect graph and management
    private EffectGraph? _effectGraph;
    private readonly List<IEffectNode> _activeEffects = new();
    private int _width, _height;

    public void Initialize(int width, int height) 
    { 
        _width = width;
        _height = height;
        _effectGraph = new EffectGraph();
        RefreshEffectsList();
    }

    public void Resize(int width, int height) 
    { 
        _width = width;
        _height = height;
    }

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        // Clear the canvas
        canvas.Clear(0xFF000000);
        
        // If we have active effects and an effect graph, process them
        if (_effectGraph != null && _activeEffects.Count > 0)
        {
            try
            {
                // Create render context for effects
                var renderContext = new RenderContext
                {
                    Width = _width,
                    Height = _height
                };

                // Convert AudioFeatures to waveform/spectrum arrays
                var waveform = features.Waveform ?? new float[1024];
                var spectrum = features.Fft ?? new float[512]; // Use Fft instead of Spectrum

                // Render each active effect
                foreach (var effect in _activeEffects.Where(e => e != null))
                {
                    effect.Render(waveform, spectrum, renderContext);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AVS Effects] Render error: {ex.Message}");
            }
        }
    }

    public void Dispose() 
    { 
        _effectGraph?.Dispose();
        _activeEffects.Clear();
    }

    // Effect management methods for UI integration
    public List<string> GetAvailableEffectNames() 
    {
        try
        {
            return EffectRegistry.GetAll().Select(e => e.Name).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error getting available effects: {ex.Message}");
            return new List<string>();
        }
    }

    public List<string> GetActiveEffectNames() 
    {
        return _activeEffects.Select(e => e?.Name ?? "Unknown").ToList();
    }

    public int GetAvailableEffectCount() 
    {
        try
        {
            return EffectRegistry.GetAll().Count();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error counting effects: {ex.Message}");
            return 0;
        }
    }
    
    public void AddEffect(string effectName)
    {
        if (_activeEffects.Count >= MaxActiveEffects)
            return;

        try
        {
            var effect = EffectRegistry.CreateByName(effectName);
            if (effect != null && !_activeEffects.Any(e => e?.Name == effectName))
            {
                _activeEffects.Add(effect);
                System.Diagnostics.Debug.WriteLine($"[AVS Effects] Added effect: {effectName}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error adding effect {effectName}: {ex.Message}");
        }
    }
    
    public void RemoveEffect(string effectName)
    {
        try
        {
            var effect = _activeEffects.FirstOrDefault(e => e?.Name == effectName);
            if (effect != null)
            {
                _activeEffects.Remove(effect);
                System.Diagnostics.Debug.WriteLine($"[AVS Effects] Removed effect: {effectName}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error removing effect {effectName}: {ex.Message}");
        }
    }
    
    public void RemoveEffect(int index)
    {
        if (index >= 0 && index < _activeEffects.Count)
        {
            var effectName = _activeEffects[index]?.Name ?? "Unknown";
            _activeEffects.RemoveAt(index);
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Removed effect at index {index}: {effectName}");
        }
    }
    
    public void RefreshEffectsList()
    {
        try
        {
            var availableCount = GetAvailableEffectCount();
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Refreshed effect list - {availableCount} effects available");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error refreshing effects list: {ex.Message}");
        }
    }
    
    public void DebugEffectDiscovery()
    {
        try
        {
            var available = GetAvailableEffectNames();
            var count = available.Count;
            
            System.Diagnostics.Debug.WriteLine($"[AVS Effects Debug] Total effects discovered: {count}");
            System.Diagnostics.Debug.WriteLine($"[AVS Effects Debug] Available effects:");
            
            foreach (var effectName in available.Take(10)) // Show first 10
            {
                System.Diagnostics.Debug.WriteLine($"  - {effectName}");
            }
            
            if (available.Count > 10)
            {
                System.Diagnostics.Debug.WriteLine($"  ... and {available.Count - 10} more");
            }
            
            System.Diagnostics.Debug.WriteLine($"[AVS Effects Debug] Active effects: {_activeEffects.Count}/{MaxActiveEffects}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects Debug] Discovery error: {ex.Message}");
        }
    }
    
    public void Configure()
    {
        System.Diagnostics.Debug.WriteLine("[AVS Effects] Configure called - opening configuration UI");
    }
    
    public void SetEffectCount(int count)
    {
        MaxActiveEffects = Math.Max(1, Math.Min(count, 16));
        
        // If we now have too many active effects, remove the excess
        while (_activeEffects.Count > MaxActiveEffects)
        {
            _activeEffects.RemoveAt(_activeEffects.Count - 1);
        }
        
        System.Diagnostics.Debug.WriteLine($"[AVS Effects] Max active effects set to: {MaxActiveEffects}");
    }
}
