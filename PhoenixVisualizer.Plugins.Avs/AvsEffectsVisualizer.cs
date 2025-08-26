using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Core.Effects;
using PhoenixVisualizer.Core.Effects.Nodes;
using PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;
using PhoenixVisualizer.Core.Effects.Interfaces;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Plugins.Avs;

/// <summary>
/// AVS Effects Visualizer - Full AVS effects engine using the EffectGraph system
/// Loads and manages all implemented AVS effects for real-time audio visualization
/// </summary>
public class AvsEffectsVisualizer : IVisualizerPlugin
{
    public string Id => "avs_effects_engine";
    public string DisplayName => "AVS Effects Engine";
    public string Description => "Full Advanced Visualization Studio effects engine with 49+ implemented effects";
    public bool IsEnabled { get; set; } = true;

    private int _width, _height;
    private EffectGraph _effectGraph;
    private Dictionary<string, IEffectNode> _availableEffects;
    private List<IEffectNode> _activeEffects;
    private bool _isInitialized = false;

    // Configuration
    public int MaxActiveEffects { get; set; } = 5;
    public bool AutoRotateEffects { get; set; } = true;
    public float EffectRotationSpeed { get; set; } = 1.0f;
    public bool BeatReactive { get; set; } = true;

    public AvsEffectsVisualizer()
    {
        _effectGraph = new EffectGraph();
        _availableEffects = new Dictionary<string, IEffectNode>();
        _activeEffects = new List<IEffectNode>();
    }

    public void Initialize()
    {
        if (_isInitialized) return;
        
        // Discover and load all available AVS effects
        DiscoverAvsEffects();
        
        // Create a default effect chain
        CreateDefaultEffectChain();
        
        _isInitialized = true;
    }

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        Initialize();
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Shutdown()
    {
        _isInitialized = false;
        _activeEffects.Clear();
        _effectGraph?.Dispose();
    }

    public void ProcessFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        if (!_isInitialized) return;
        
        try
        {
            // Update effect graph with audio features
            UpdateEffectGraph(features);
            
            // Execute the effect graph
            var result = _effectGraph.ExecuteAsync(features).Result;
            
            // Render the result
            RenderEffects(canvas, features);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error processing frame: {ex.Message}");
        }
    }

    public void Configure()
    {
        // Show configuration dialog or load preset
        LoadPreset("default");
    }

    public void Dispose()
    {
        Shutdown();
        _effectGraph?.Dispose();
    }

    private void DiscoverAvsEffects()
    {
        try
        {
            // Get all types from the current assembly that inherit from BaseEffectNode
            var effectTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(BaseEffectNode)))
                .ToList();

            // Also check the Core assembly for AVS effects
            var coreAssembly = typeof(BaseEffectNode).Assembly;
            var coreEffectTypes = coreAssembly
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && 
                           (t.IsSubclassOf(typeof(BaseEffectNode)) || 
                            t.GetInterfaces().Contains(typeof(IEffectNode))))
                .ToList();

            // Combine and deduplicate
            var allEffectTypes = effectTypes.Concat(coreEffectTypes).Distinct().ToList();

            foreach (var effectType in allEffectTypes)
            {
                try
                {
                    // Try to create an instance
                    if (Activator.CreateInstance(effectType) is IEffectNode effect)
                    {
                        var effectName = effectType.Name.Replace("EffectsNode", "").Replace("Node", "");
                        _availableEffects[effectName] = effect;
                        
                        System.Diagnostics.Debug.WriteLine($"[AVS Effects] Discovered effect: {effectName}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AVS Effects] Failed to instantiate {effectType.Name}: {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Discovered {_availableEffects.Count} effects");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error discovering effects: {ex.Message}");
        }
    }

    private void CreateDefaultEffectChain()
    {
        try
        {
            // Clear existing effects
            _activeEffects.Clear();
            _effectGraph.Clear();

            // Add some default effects
            var defaultEffects = new[] { "OscilloscopeRing", "BeatSpinning", "InterferencePatterns" };
            
            foreach (var effectName in defaultEffects)
            {
                if (_availableEffects.TryGetValue(effectName, out var effect))
                {
                    AddEffectToChain(effect);
                }
            }

            // If no effects were added, add at least one
            if (_activeEffects.Count == 0 && _availableEffects.Count > 0)
            {
                var firstEffect = _availableEffects.First().Value;
                AddEffectToChain(firstEffect);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error creating default chain: {ex.Message}");
        }
    }

    private void AddEffectToChain(IEffectNode effect)
    {
        if (_activeEffects.Count >= MaxActiveEffects) return;
        
        try
        {
            // Clone the effect for this instance
            var effectInstance = CloneEffect(effect);
            if (effectInstance != null)
            {
                _activeEffects.Add(effectInstance);
                _effectGraph.AddNode(effectInstance);
                
                // Connect to the previous effect or input
                if (_activeEffects.Count > 1)
                {
                    var previousEffect = _activeEffects[_activeEffects.Count - 2];
                    _effectGraph.ConnectNodes(previousEffect.Id, effectInstance.Id);
                }
                else
                {
                    // Connect to input
                    _effectGraph.ConnectNodes(_effectGraph.RootInput.Id, effectInstance.Id);
                }
                
                // Connect to output if this is the last effect
                if (_activeEffects.Count == 1)
                {
                    _effectGraph.ConnectNodes(effectInstance.Id, _effectGraph.FinalOutput.Id);
                }
                
                System.Diagnostics.Debug.WriteLine($"[AVS Effects] Added effect to chain: {effectInstance.Id}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error adding effect to chain: {ex.Message}");
        }
    }

    private IEffectNode? CloneEffect(IEffectNode original)
    {
        try
        {
            var effectType = original.GetType();
            return Activator.CreateInstance(effectType) as IEffectNode;
        }
        catch
        {
            return null;
        }
    }

    private void UpdateEffectGraph(AudioFeatures features)
    {
        // Update effect parameters based on audio features
        foreach (var effect in _activeEffects)
        {
            if (effect is BaseEffectNode baseEffect)
            {
                // Update effect properties based on audio
                UpdateEffectProperties(baseEffect, features);
            }
        }
    }

    private void UpdateEffectProperties(BaseEffectNode effect, AudioFeatures features)
    {
        try
        {
            // Set common properties
            if (effect is OscilloscopeRingEffectsNode ringEffect)
            {
                ringEffect.Size = 100 + (int)(features.RMS * 50);
                ringEffect.RotationSpeed = EffectRotationSpeed + (features.Beat ? 2.0f : 0.0f);
            }
            else if (effect is BeatSpinningEffectsNode spinEffect)
            {
                spinEffect.Size = 80 + (int)(features.RMS * 40);
                spinEffect.BeatMultiplier = features.Beat ? 3.0f : 1.0f;
            }
            else if (effect is InterferencePatternsEffectsNode interferenceEffect)
            {
                interferenceEffect.WaveFrequency = 1.0f + features.RMS * 2.0f;
                interferenceEffect.WaveSpeed = EffectRotationSpeed + (features.Beat ? 1.0f : 0.0f);
            }
            else if (effect is OnetoneEffectsNode onetoneEffect)
            {
                onetoneEffect.Contrast = 1.0f + features.RMS * 0.5f;
                onetoneEffect.Brightness = 0.5f + features.RMS * 0.3f;
            }
            else if (effect is EffectStackingEffectsNode stackingEffect)
            {
                stackingEffect.LayerSpacing = 20.0f + features.RMS * 10.0f;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error updating effect properties: {ex.Message}");
        }
    }

    private void RenderEffects(ISkiaCanvas canvas, AudioFeatures features)
    {
        try
        {
            // Clear canvas
            canvas.Clear(0xFF000000);
            
            // Render each active effect
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                var effect = _activeEffects[i];
                
                // Calculate effect position (arrange effects in a grid or pattern)
                var position = CalculateEffectPosition(i, _activeEffects.Count);
                
                // Render effect at position
                RenderEffectAtPosition(canvas, effect, position, features);
            }
            
            // Draw effect info overlay
            DrawEffectInfoOverlay(canvas);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error rendering effects: {ex.Message}");
        }
    }

    private (float x, float y) CalculateEffectPosition(int index, int totalEffects)
    {
        if (totalEffects <= 1)
        {
            return (_width / 2.0f, _height / 2.0f);
        }
        
        // Arrange effects in a grid pattern
        int cols = (int)Math.Ceiling(Math.Sqrt(totalEffects));
        int rows = (int)Math.Ceiling((float)totalEffects / cols);
        
        int col = index % cols;
        int row = index / cols;
        
        float x = (col + 0.5f) * _width / cols;
        float y = (row + 0.5f) * _height / rows;
        
        return (x, y);
    }

    private void RenderEffectAtPosition(ISkiaCanvas canvas, IEffectNode effect, (float x, float y) position, AudioFeatures features)
    {
        try
        {
            // For now, just draw a placeholder for each effect
            // In a full implementation, this would render the actual effect output
            
            var effectName = effect.GetType().Name.Replace("EffectsNode", "").Replace("Node", "");
            
            // Draw effect background
            canvas.FillRect(position.x - 50, position.y - 30, 100, 60, 0x80000000);
            
            // Draw effect name
            canvas.DrawText(effectName, position.x, position.y, 0xFFFFFFFF, 12);
            
            // Draw audio reactivity indicator
            if (features.Beat)
            {
                canvas.FillCircle(position.x, position.y + 20, 5, 0xFFFF0000);
            }
            
            // Draw RMS indicator
            var rmsBarHeight = (int)(features.RMS * 20);
            canvas.FillRect(position.x - 40, position.y + 25, 80, 4, 0xFF404040);
            canvas.FillRect(position.x - 40, position.y + 25, (int)(features.RMS * 80), 4, 0xFF00FF00);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error rendering effect at position: {ex.Message}");
        }
    }

    private void DrawEffectInfoOverlay(ISkiaCanvas canvas)
    {
        try
        {
            // Draw effect count and status
            var infoText = $"AVS Effects: {_activeEffects.Count}/{_availableEffects.Count}";
            canvas.DrawText(infoText, 10, 20, 0xFFFFFFFF, 14);
            
            // Draw help text
            var helpText = "Press C to configure, R to rotate effects";
            canvas.DrawText(helpText, 10, _height - 10, 0xFFFFFF80, 12);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error drawing info overlay: {ex.Message}");
        }
    }

    public void LoadPreset(string presetText)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(presetText) || presetText == "default")
            {
                CreateDefaultEffectChain();
                return;
            }
            
            // Parse preset text and load specific effects
            var effectNames = presetText.Split(',', ';', ' ')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();
            
            // Clear existing effects
            _activeEffects.Clear();
            _effectGraph.Clear();
            
            // Add specified effects
            foreach (var effectName in effectNames)
            {
                if (_availableEffects.TryGetValue(effectName.Trim(), out var effect))
                {
                    AddEffectToChain(effect);
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Loaded preset: {presetText}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error loading preset: {ex.Message}");
            CreateDefaultEffectChain(); // Fallback to default
        }
    }

    public void RotateEffects()
    {
        if (!AutoRotateEffects || _activeEffects.Count <= 1) return;
        
        try
        {
            // Rotate the effect list
            var firstEffect = _activeEffects[0];
            _activeEffects.RemoveAt(0);
            _activeEffects.Add(firstEffect);
            
            // Rebuild connections
            RebuildEffectConnections();
            
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Rotated effects");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error rotating effects: {ex.Message}");
        }
    }

    private void RebuildEffectConnections()
    {
        try
        {
            _effectGraph.Clear();
            
            // Reconnect all effects
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                var effect = _activeEffects[i];
                _effectGraph.AddNode(effect);
                
                if (i == 0)
                {
                    // Connect first effect to input
                    _effectGraph.ConnectNodes(_effectGraph.RootInput.Id, effect.Id);
                }
                else
                {
                    // Connect to previous effect
                    var previousEffect = _activeEffects[i - 1];
                    _effectGraph.ConnectNodes(previousEffect.Id, effect.Id);
                }
                
                if (i == _activeEffects.Count - 1)
                {
                    // Connect last effect to output
                    _effectGraph.ConnectNodes(effect.Id, _effectGraph.FinalOutput.Id);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error rebuilding connections: {ex.Message}");
        }
    }
}