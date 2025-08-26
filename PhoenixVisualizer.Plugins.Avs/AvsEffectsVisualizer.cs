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
    public string Description => "Full Advanced Visualization Studio effects engine with 48+ implemented effects";
    public bool IsEnabled { get; set; } = true;

    private int _width, _height;
    private EffectGraph _effectGraph;
    private Dictionary<string, IEffectNode> _availableEffects;
    private List<IEffectNode> _activeEffects;
    private bool _isInitialized = false;

    // Configuration
    public int MaxActiveEffects { get; set; } = 8; // Increased from 5 to 8
    public bool AutoRotateEffects { get; set; } = true;
    public float EffectRotationSpeed { get; set; } = 1.0f;
    public bool BeatReactive { get; set; } = true;
    public bool ShowEffectNames { get; set; } = true;
    public bool ShowEffectGrid { get; set; } = true;
    public float EffectSpacing { get; set; } = 20.0f;

    // Effect selection
    public List<string> SelectedEffectTypes { get; set; } = new List<string>();
    public bool RandomEffectSelection { get; set; } = false;
    public int EffectChangeInterval { get; set; } = 300; // frames

    // Performance tracking
    private int _frameCount = 0;
    private DateTime _lastEffectChange = DateTime.UtcNow;

    public AvsEffectsVisualizer()
    {
        _effectGraph = new EffectGraph();
        _availableEffects = new Dictionary<string, IEffectNode>();
        _activeEffects = new List<IEffectNode>();
        
        // Initialize with popular effect types
        SelectedEffectTypes = new List<string>
        {
            "OscilloscopeRing", "BeatSpinning", "InterferencePatterns", 
            "TimeDomainScope", "OscilloscopeStar", "Onetone"
        };
    }

    public void Initialize()
    {
        if (_isInitialized) return;
        
        // Discover and load all available AVS effects
        DiscoverAvsEffects();
        
        // Create a default effect chain
        CreateDefaultEffectChain();
        
        _isInitialized = true;
        
        System.Diagnostics.Debug.WriteLine($"[AVS Effects] Initialized with {_availableEffects.Count} available effects");
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
            _frameCount++;
            
            // Update effect graph with audio features
            UpdateEffectGraph(features);
            
            // Execute the effect graph
            var result = _effectGraph.ExecuteAsync(features).Result;
            
            // Render the result
            RenderEffects(canvas, features);
            
            // Auto-rotate effects if enabled
            if (AutoRotateEffects && _frameCount % EffectChangeInterval == 0)
            {
                RotateEffects();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error processing frame: {ex.Message}");
        }
    }

    public void Configure()
    {
        try
        {
            // Show configuration dialog
            var configWindow = new PhoenixVisualizer.Views.AvsEffectsConfigWindow(this);
            configWindow.Show();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error opening configuration: {ex.Message}");
            // Fallback to preset loading
            LoadPreset("default");
        }
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

            // Add effects based on selection or use random selection
            var effectsToAdd = RandomEffectSelection ? 
                GetRandomEffects() : 
                GetSelectedEffects();

            foreach (var effectName in effectsToAdd)
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

    private List<string> GetSelectedEffects()
    {
        var effects = new List<string>();
        
        // Add selected effect types if they exist
        foreach (var effectType in SelectedEffectTypes)
        {
            if (_availableEffects.ContainsKey(effectType))
            {
                effects.Add(effectType);
            }
        }
        
        // If no selected effects found, add some defaults
        if (effects.Count == 0)
        {
            var defaultEffects = new[] { "OscilloscopeRing", "BeatSpinning", "InterferencePatterns" };
            foreach (var effect in defaultEffects)
            {
                if (_availableEffects.ContainsKey(effect))
                {
                    effects.Add(effect);
                }
            }
        }
        
        return effects.Take(MaxActiveEffects).ToList();
    }

    private List<string> GetRandomEffects()
    {
        var availableEffectNames = _availableEffects.Keys.ToList();
        var randomEffects = new List<string>();
        var random = new Random();
        
        // Randomly select effects
        for (int i = 0; i < Math.Min(MaxActiveEffects, availableEffectNames.Count); i++)
        {
            var randomIndex = random.Next(availableEffectNames.Count);
            randomEffects.Add(availableEffectNames[randomIndex]);
            availableEffectNames.RemoveAt(randomIndex);
        }
        
        return randomEffects;
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
            else if (effect is TimeDomainScopeEffectsNode scopeEffect)
            {
                scopeEffect.ScopeHeight = 100.0f + features.RMS * 50.0f;
                scopeEffect.ScrollSpeed = EffectRotationSpeed + (features.Beat ? 2.0f : 0.0f);
            }
            else if (effect is OscilloscopeStarEffectsNode starEffect)
            {
                starEffect.Size = 80 + (int)(features.RMS * 40);
                starEffect.StarRotationSpeed = EffectRotationSpeed + (features.Beat ? 1.5f : 0.0f);
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
            
            // Draw effect grid if enabled
            if (ShowEffectGrid)
            {
                DrawEffectGrid(canvas);
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
        
        // Arrange effects in a grid pattern with spacing
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
            // Create a temporary image buffer for the effect
            var effectBuffer = new ImageBuffer(200, 150); // Fixed size for each effect
            
            // Set effect position and size
            if (effect is BaseEffectNode baseEffect)
            {
                // Configure effect properties
                ConfigureEffectForPosition(baseEffect, position, effectBuffer.Width, effectBuffer.Height);
                
                // Process the effect
                var inputBuffer = new ImageBuffer(effectBuffer.Width, effectBuffer.Height);
                baseEffect.Process(inputBuffer, effectBuffer, features);
                
                // Render the effect buffer to canvas
                RenderEffectBuffer(canvas, effectBuffer, position);
            }
            
            // Draw effect name if enabled
            if (ShowEffectNames)
            {
                var effectName = effect.GetType().Name.Replace("EffectsNode", "").Replace("Node", "");
                canvas.DrawText(effectName, position.x, position.y + 80, 0xFFFFFFFF, 10);
            }
            
            // Draw audio reactivity indicator
            if (features.Beat)
            {
                canvas.FillCircle(position.x, position.y + 90, 3, 0xFFFF0000);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error rendering effect at position: {ex.Message}");
            
            // Fallback: draw placeholder
            DrawEffectPlaceholder(canvas, position, effect, features);
        }
    }

    private void ConfigureEffectForPosition(BaseEffectNode effect, (float x, float y) position, int width, int height)
    {
        // Configure effect to render in its allocated space
        if (effect is OscilloscopeRingEffectsNode ringEffect)
        {
            ringEffect.Size = Math.Min(width, height) / 2;
        }
        else if (effect is BeatSpinningEffectsNode spinEffect)
        {
            spinEffect.Size = Math.Min(width, height) / 2;
        }
        else if (effect is InterferencePatternsEffectsNode interferenceEffect)
        {
            interferenceEffect.Size = Math.Min(width, height) / 2;
        }
        else if (effect is TimeDomainScopeEffectsNode scopeEffect)
        {
            scopeEffect.ScopeWidth = width * 0.8f;
            scopeEffect.ScopeHeight = height * 0.6f;
        }
        else if (effect is OscilloscopeStarEffectsNode starEffect)
        {
            starEffect.Size = Math.Min(width, height) / 2;
        }
        else if (effect is OnetoneEffectsNode onetoneEffect)
        {
            // Onetone effects work on the full buffer
        }
    }

    private void RenderEffectBuffer(ISkiaCanvas canvas, ImageBuffer effectBuffer, (float x, float y) position)
    {
        try
        {
            // Calculate render bounds
            int startX = (int)(position.x - effectBuffer.Width / 2);
            int startY = (int)(position.y - effectBuffer.Height / 2);
            
            // Render the effect buffer pixel by pixel
            for (int y = 0; y < effectBuffer.Height; y++)
            {
                for (int x = 0; x < effectBuffer.Width; x++)
                {
                    var color = effectBuffer.GetPixel(x, y);
                    if (color.A > 0) // Only render non-transparent pixels
                    {
                        int canvasX = startX + x;
                        int canvasY = startY + y;
                        
                        if (canvasX >= 0 && canvasX < _width && canvasY >= 0 && canvasY < _height)
                        {
                            uint colorValue = (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | color.B);
                            canvas.SetPixel(canvasX, canvasY, colorValue);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error rendering effect buffer: {ex.Message}");
        }
    }

    private void DrawEffectPlaceholder(ISkiaCanvas canvas, (float x, float y) position, IEffectNode effect, AudioFeatures features)
    {
        try
        {
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
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error drawing effect placeholder: {ex.Message}");
        }
    }

    private void DrawEffectGrid(ISkiaCanvas canvas)
    {
        try
        {
            // Draw grid lines between effects
            if (_activeEffects.Count > 1)
            {
                var positions = new List<(float x, float y)>();
                for (int i = 0; i < _activeEffects.Count; i++)
                {
                    positions.Add(CalculateEffectPosition(i, _activeEffects.Count));
                }
                
                // Draw connecting lines
                for (int i = 0; i < positions.Count - 1; i++)
                {
                    var start = positions[i];
                    var end = positions[i + 1];
                    
                    canvas.DrawLine(start.x, start.y, end.x, end.y, 0x40FFFFFF, 1);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error drawing effect grid: {ex.Message}");
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
            var helpText = "C: Configure | G: Toggle Grid | A: Add Effect | X: Remove Effect | R: Rotate";
            canvas.DrawText(helpText, 10, _height - 10, 0xFFFFFF80, 12);
            
            // Draw effect names
            if (ShowEffectNames)
            {
                var yPos = 40;
                foreach (var effect in _activeEffects)
                {
                    var effectName = effect.GetType().Name.Replace("EffectsNode", "").Replace("Node", "");
                    canvas.DrawText(effectName, 10, yPos, 0xFFFFFF80, 10);
                    yPos += 15;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error drawing info overlay: {ex.Message}");
        }
    }

    private void RotateEffects()
    {
        try
        {
            if (_activeEffects.Count <= 1) return;
            
            // Rotate effects by moving the first one to the end
            var firstEffect = _activeEffects[0];
            _activeEffects.RemoveAt(0);
            _activeEffects.Add(firstEffect);
            
            // Rebuild effect graph connections
            RebuildEffectGraph();
            
            System.Diagnostics.Debug.WriteLine("[AVS Effects] Rotated effects");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error rotating effects: {ex.Message}");
        }
    }

    private void RebuildEffectGraph()
    {
        try
        {
            _effectGraph.Clear();
            
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
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error rebuilding effect graph: {ex.Message}");
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
            
            // Parse preset text and create effect chain
            var effectNames = presetText.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();
            
            // Clear existing effects
            _activeEffects.Clear();
            _effectGraph.Clear();
            
            // Add effects from preset
            foreach (var effectName in effectNames)
            {
                if (_availableEffects.TryGetValue(effectName, out var effect))
                {
                    AddEffectToChain(effect);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Effects] Error loading preset: {ex.Message}");
            CreateDefaultEffectChain(); // Fallback to default
        }
    }

    // Public methods for external control
    public void AddEffect(string effectName)
    {
        if (_availableEffects.TryGetValue(effectName, out var effect))
        {
            AddEffectToChain(effect);
        }
    }

    public void RemoveEffect(int index)
    {
        if (index >= 0 && index < _activeEffects.Count)
        {
            var effect = _activeEffects[index];
            _activeEffects.RemoveAt(index);
            _effectGraph.RemoveNode(effect.Id);
            
            // Rebuild connections
            RebuildEffectGraph();
        }
    }

    public void SetEffectCount(int count)
    {
        MaxActiveEffects = Math.Max(1, Math.Min(count, 16)); // Limit to 16 effects max
        
        // Adjust active effects if needed
        while (_activeEffects.Count > MaxActiveEffects)
        {
            RemoveEffect(_activeEffects.Count - 1);
        }
    }

    public List<string> GetAvailableEffectNames()
    {
        return _availableEffects.Keys.ToList();
    }

    public List<string> GetActiveEffectNames()
    {
        return _activeEffects.Select(e => e.GetType().Name.Replace("EffectsNode", "").Replace("Node", "")).ToList();
    }
}