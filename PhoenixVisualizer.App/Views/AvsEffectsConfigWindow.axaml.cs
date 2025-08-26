using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Text.Json;

namespace PhoenixVisualizer.Views;

public partial class AvsEffectsConfigWindow : Window
{
    private readonly AvsEffectsVisualizer _visualizer;
    private readonly ObservableCollection<EffectItem> _availableEffects;
    private readonly ObservableCollection<ActiveEffectItem> _activeEffects;
    
    // Configuration properties
    private int _maxActiveEffects = 8;
    private bool _autoRotateEffects = true;
    private float _rotationSpeed = 1.0f;
    private bool _beatReactive = true;
    private bool _showEffectNames = true;
    private bool _showEffectGrid = true;
    private float _effectSpacing = 20.0f;
    
    // Preset management
    private readonly string _presetsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PhoenixVisualizer", "avs_presets.json");
    private Dictionary<string, AvsEffectsPreset> _presets;

    public AvsEffectsConfigWindow(AvsEffectsVisualizer visualizer)
    {
        _visualizer = visualizer;
        _availableEffects = new ObservableCollection<EffectItem>();
        _activeEffects = new ObservableCollection<ActiveEffectItem>();
        _presets = new Dictionary<string, AvsEffectsPreset>();
        
        InitializeComponent();
        LoadConfiguration();
        LoadPresets();
        PopulateEffectsList();
        UpdateActiveEffectsList();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void LoadConfiguration()
    {
        // Load current configuration from visualizer
        _maxActiveEffects = _visualizer.MaxActiveEffects;
        _autoRotateEffects = _visualizer.AutoRotateEffects;
        _rotationSpeed = _visualizer.EffectRotationSpeed;
        _beatReactive = _visualizer.BeatReactive;
        _showEffectNames = _visualizer.ShowEffectNames;
        _showEffectGrid = _visualizer.ShowEffectGrid;
        _effectSpacing = _visualizer.EffectSpacing;
        
        // Update UI controls
        var maxEffectsSlider = this.FindControl<Slider>("MaxEffectsSlider");
        var autoRotateCheckBox = this.FindControl<CheckBox>("AutoRotateCheckBox");
        var rotationSpeedSlider = this.FindControl<Slider>("RotationSpeedSlider");
        var beatReactiveCheckBox = this.FindControl<CheckBox>("BeatReactiveCheckBox");
        var showNamesCheckBox = this.FindControl<CheckBox>("ShowNamesCheckBox");
        var showGridCheckBox = this.FindControl<CheckBox>("ShowGridCheckBox");
        var spacingSlider = this.FindControl<Slider>("SpacingSlider");
        
        if (maxEffectsSlider != null) maxEffectsSlider.Value = _maxActiveEffects;
        if (autoRotateCheckBox != null) autoRotateCheckBox.IsChecked = _autoRotateEffects;
        if (rotationSpeedSlider != null) rotationSpeedSlider.Value = _rotationSpeed;
        if (beatReactiveCheckBox != null) beatReactiveCheckBox.IsChecked = _beatReactive;
        if (showNamesCheckBox != null) showNamesCheckBox.IsChecked = _showEffectNames;
        if (showGridCheckBox != null) showGridCheckBox.IsChecked = _showEffectGrid;
        if (spacingSlider != null) spacingSlider.Value = _effectSpacing;
    }

    private void LoadPresets()
    {
        try
        {
            if (File.Exists(_presetsPath))
            {
                var json = File.ReadAllText(_presetsPath);
                _presets = JsonSerializer.Deserialize<Dictionary<string, AvsEffectsPreset>>(json) ?? new Dictionary<string, AvsEffectsPreset>();
            }
            
            // Add default presets if none exist
            if (_presets.Count == 0)
            {
                _presets["Default"] = new AvsEffectsPreset
                {
                    Name = "Default",
                    Description = "Default AVS effects configuration",
                    MaxActiveEffects = 8,
                    AutoRotateEffects = true,
                    RotationSpeed = 1.0f,
                    BeatReactive = true,
                    ShowEffectNames = true,
                    ShowEffectGrid = true,
                    EffectSpacing = 20.0f,
                    SelectedEffects = new List<string> { "OscilloscopeRing", "BeatSpinning", "InterferencePatterns" }
                };
                
                _presets["Oscilloscope Focus"] = new AvsEffectsPreset
                {
                    Name = "Oscilloscope Focus",
                    Description = "Focus on oscilloscope and scope effects",
                    MaxActiveEffects = 6,
                    AutoRotateEffects = false,
                    RotationSpeed = 0.5f,
                    BeatReactive = true,
                    ShowEffectNames = true,
                    ShowEffectGrid = false,
                    EffectSpacing = 15.0f,
                    SelectedEffects = new List<string> { "OscilloscopeRing", "TimeDomainScope", "OscilloscopeStar" }
                };
                
                _presets["Beat Reactive"] = new AvsEffectsPreset
                {
                    Name = "Beat Reactive",
                    Description = "High-energy beat-reactive effects",
                    MaxActiveEffects = 10,
                    AutoRotateEffects = true,
                    RotationSpeed = 2.0f,
                    BeatReactive = true,
                    ShowEffectNames = false,
                    ShowEffectGrid = true,
                    EffectSpacing = 25.0f,
                    SelectedEffects = new List<string> { "BeatSpinning", "InterferencePatterns", "Onetone" }
                };
            }
            
            // Update preset combo box
            var presetComboBox = this.FindControl<ComboBox>("PresetComboBox");
            if (presetComboBox != null)
            {
                presetComboBox.ItemsSource = _presets.Keys.ToList();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Config] Error loading presets: {ex.Message}");
        }
    }

    private void PopulateEffectsList()
    {
        try
        {
            _availableEffects.Clear();
            
            var availableEffectNames = _visualizer.GetAvailableEffectNames();
            var selectedEffectNames = _visualizer.GetActiveEffectNames();
            
            foreach (var effectName in availableEffectNames)
            {
                var effectItem = new EffectItem
                {
                    DisplayName = effectName,
                    IsSelected = selectedEffectNames.Contains(effectName),
                    Category = GetEffectCategory(effectName)
                };
                
                _availableEffects.Add(effectItem);
            }
            
            // Update effects list box
            var effectsListBox = this.FindControl<ListBox>("EffectsListBox");
            if (effectsListBox != null)
            {
                effectsListBox.ItemsSource = _availableEffects;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Config] Error populating effects list: {ex.Message}");
        }
    }

    private string GetEffectCategory(string effectName)
    {
        if (effectName.Contains("Oscilloscope") || effectName.Contains("Scope"))
            return "oscilloscope";
        else if (effectName.Contains("Beat") || effectName.Contains("Spinning"))
            return "beat";
        else if (effectName.Contains("Pattern") || effectName.Contains("Interference"))
            return "pattern";
        else
            return "utility";
    }

    private void UpdateActiveEffectsList()
    {
        try
        {
            _activeEffects.Clear();
            
            var activeEffectNames = _visualizer.GetActiveEffectNames();
            for (int i = 0; i < activeEffectNames.Count; i++)
            {
                var effectItem = new ActiveEffectItem
                {
                    DisplayName = activeEffectNames[i],
                    Index = i
                };
                
                _activeEffects.Add(effectItem);
            }
            
            // Update active effects list box
            var activeEffectsListBox = this.FindControl<ListBox>("ActiveEffectsListBox");
            if (activeEffectsListBox != null)
            {
                activeEffectsListBox.ItemsSource = _activeEffects;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Config] Error updating active effects list: {ex.Message}");
        }
    }

    // Event handlers
    private void OnCategoryChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem is ComboBoxItem item && item.Tag is string category)
            {
                var filteredEffects = category == "all" ? 
                    _availableEffects : 
                    _availableEffects.Where(e => e.Category == category);
                
                var effectsListBox = this.FindControl<ListBox>("EffectsListBox");
                if (effectsListBox != null)
                {
                    effectsListBox.ItemsSource = filteredEffects;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Config] Error changing category: {ex.Message}");
        }
    }

    private void OnEffectSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Handle effect selection change
    }

    private void OnEffectChecked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var checkBox = sender as CheckBox;
            if (checkBox?.DataContext is EffectItem effectItem)
            {
                // Add effect to visualizer
                _visualizer.AddEffect(effectItem.DisplayName);
                UpdateActiveEffectsList();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Config] Error checking effect: {ex.Message}");
        }
    }

    private void OnEffectUnchecked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var checkBox = sender as CheckBox;
            if (checkBox?.DataContext is EffectItem effectItem)
            {
                // Find and remove effect from visualizer
                var activeEffects = _visualizer.GetActiveEffectNames();
                var index = activeEffects.IndexOf(effectItem.DisplayName);
                if (index >= 0)
                {
                    _visualizer.RemoveEffect(index);
                    UpdateActiveEffectsList();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Config] Error unchecking effect: {ex.Message}");
        }
    }

    private void OnSelectAll(object? sender, RoutedEventArgs e)
    {
        try
        {
            foreach (var effect in _availableEffects)
            {
                effect.IsSelected = true;
                _visualizer.AddEffect(effect.DisplayName);
            }
            UpdateActiveEffectsList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Config] Error selecting all: {ex.Message}");
        }
    }

    private void OnClearAll(object? sender, RoutedEventArgs e)
    {
        try
        {
            foreach (var effect in _availableEffects)
            {
                effect.IsSelected = false;
            }
            
            // Clear active effects
            while (_visualizer.GetActiveEffectNames().Count > 0)
            {
                _visualizer.RemoveEffect(0);
            }
            UpdateActiveEffectsList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Config] Error clearing all: {ex.Message}");
        }
    }

    private void OnRandomSelection(object? sender, RoutedEventArgs e)
    {
        try
        {
            // Clear current selection
            OnClearAll(sender, e);
            
            // Randomly select effects
            var random = new Random();
            var availableEffects = _availableEffects.ToList();
            var maxEffects = Math.Min(_maxActiveEffects, availableEffects.Count);
            
            for (int i = 0; i < maxEffects; i++)
            {
                var randomIndex = random.Next(availableEffects.Count);
                var effect = availableEffects[randomIndex];
                effect.IsSelected = true;
                _visualizer.AddEffect(effect.DisplayName);
                availableEffects.RemoveAt(randomIndex);
            }
            
            UpdateActiveEffectsList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Config] Error random selection: {ex.Message}");
        }
    }

    private void OnMaxEffectsChanged(object? sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (sender is Slider slider)
        {
            _maxActiveEffects = (int)slider.Value;
        }
    }

    private void OnRotationSpeedChanged(object? sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (sender is Slider slider)
        {
            _rotationSpeed = (float)slider.Value;
        }
    }

    private void OnSpacingChanged(object? sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (sender is Slider slider)
        {
            _effectSpacing = (float)slider.Value;
        }
    }

    private void OnRemoveEffect(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button button && button.Tag is int index)
            {
                _visualizer.RemoveEffect(index);
                UpdateActiveEffectsList();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Config] Error removing effect: {ex.Message}");
        }
    }

    private void OnMoveEffectUp(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement effect reordering
    }

    private void OnMoveEffectDown(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement effect reordering
    }

    private void OnClearChain(object? sender, RoutedEventArgs e)
    {
        try
        {
            OnClearAll(sender, e);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Config] Error clearing chain: {ex.Message}");
        }
    }

    private void OnLoadPreset(object? sender, RoutedEventArgs e)
    {
        try
        {
            var presetComboBox = this.FindControl<ComboBox>("PresetComboBox");
            if (presetComboBox?.SelectedItem is string presetName && _presets.ContainsKey(presetName))
            {
                var preset = _presets[presetName];
                LoadPresetConfiguration(preset);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Config] Error loading preset: {ex.Message}");
        }
    }

    private void LoadPresetConfiguration(AvsEffectsPreset preset)
    {
        try
        {
            // Update configuration properties
            _maxActiveEffects = preset.MaxActiveEffects;
            _autoRotateEffects = preset.AutoRotateEffects;
            _rotationSpeed = preset.RotationSpeed;
            _beatReactive = preset.BeatReactive;
            _showEffectNames = preset.ShowEffectNames;
            _showEffectGrid = preset.ShowEffectGrid;
            _effectSpacing = preset.EffectSpacing;
            
            // Update UI controls
            var maxEffectsSlider = this.FindControl<Slider>("MaxEffectsSlider");
            var autoRotateCheckBox = this.FindControl<CheckBox>("AutoRotateCheckBox");
            var rotationSpeedSlider = this.FindControl<Slider>("RotationSpeedSlider");
            var beatReactiveCheckBox = this.FindControl<CheckBox>("BeatReactiveCheckBox");
            var showNamesCheckBox = this.FindControl<CheckBox>("ShowNamesCheckBox");
            var showGridCheckBox = this.FindControl<CheckBox>("ShowGridCheckBox");
            var spacingSlider = this.FindControl<Slider>("SpacingSlider");
            
            if (maxEffectsSlider != null) maxEffectsSlider.Value = _maxActiveEffects;
            if (autoRotateCheckBox != null) autoRotateCheckBox.IsChecked = _autoRotateEffects;
            if (rotationSpeedSlider != null) rotationSpeedSlider.Value = _rotationSpeed;
            if (beatReactiveCheckBox != null) beatReactiveCheckBox.IsChecked = _beatReactive;
            if (showNamesCheckBox != null) showNamesCheckBox.IsChecked = _showEffectNames;
            if (showGridCheckBox != null) showGridCheckBox.IsChecked = _showEffectGrid;
            if (spacingSlider != null) spacingSlider.Value = _effectSpacing;
            
            // Clear current effects and load preset effects
            OnClearAll(null, null);
            foreach (var effectName in preset.SelectedEffects)
            {
                var effect = _availableEffects.FirstOrDefault(e => e.DisplayName == effectName);
                if (effect != null)
                {
                    effect.IsSelected = true;
                    _visualizer.AddEffect(effectName);
                }
            }
            UpdateActiveEffectsList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Config] Error loading preset configuration: {ex.Message}");
        }
    }

    private void OnSavePreset(object? sender, RoutedEventArgs e)
    {
        try
        {
            // TODO: Show dialog to get preset name and description
            var presetName = $"Custom Preset {DateTime.Now:yyyy-MM-dd HH:mm}";
            
            var preset = new AvsEffectsPreset
            {
                Name = presetName,
                Description = "Custom AVS effects configuration",
                MaxActiveEffects = _maxActiveEffects,
                AutoRotateEffects = _autoRotateEffects,
                RotationSpeed = _rotationSpeed,
                BeatReactive = _beatReactive,
                ShowEffectNames = _showEffectNames,
                ShowEffectGrid = _showEffectGrid,
                EffectSpacing = _effectSpacing,
                SelectedEffects = _visualizer.GetActiveEffectNames()
            };
            
            _presets[presetName] = preset;
            SavePresets();
            
            // Update preset combo box
            var presetComboBox = this.FindControl<ComboBox>("PresetComboBox");
            if (presetComboBox != null)
            {
                presetComboBox.ItemsSource = _presets.Keys.ToList();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Config] Error saving preset: {ex.Message}");
        }
    }

    private void OnDeletePreset(object? sender, RoutedEventArgs e)
    {
        try
        {
            var presetComboBox = this.FindControl<ComboBox>("PresetComboBox");
            if (presetComboBox?.SelectedItem is string presetName && _presets.ContainsKey(presetName))
            {
                _presets.Remove(presetName);
                SavePresets();
                
                // Update preset combo box
                if (presetComboBox != null)
                {
                    presetComboBox.ItemsSource = _presets.Keys.ToList();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Config] Error deleting preset: {ex.Message}");
        }
    }

    private void SavePresets()
    {
        try
        {
            var directory = Path.GetDirectoryName(_presetsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var json = JsonSerializer.Serialize(_presets, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_presetsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Config] Error saving presets: {ex.Message}");
        }
    }

    private void OnApply(object? sender, RoutedEventArgs e)
    {
        try
        {
            // Apply configuration to visualizer
            _visualizer.MaxActiveEffects = _maxActiveEffects;
            _visualizer.AutoRotateEffects = _autoRotateEffects;
            _visualizer.EffectRotationSpeed = _rotationSpeed;
            _visualizer.BeatReactive = _beatReactive;
            _visualizer.ShowEffectNames = _showEffectNames;
            _visualizer.ShowEffectGrid = _showEffectGrid;
            _visualizer.EffectSpacing = _effectSpacing;
            
            // Set effect count
            _visualizer.SetEffectCount(_maxActiveEffects);
            
            System.Diagnostics.Debug.WriteLine("[AVS Config] Configuration applied successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Config] Error applying configuration: {ex.Message}");
        }
    }

    private void OnResetDefaults(object? sender, RoutedEventArgs e)
    {
        try
        {
            LoadPresetConfiguration(_presets["Default"]);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AVS Config] Error resetting to defaults: {ex.Message}");
        }
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}

// Data models
public class EffectItem
{
    public string DisplayName { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class ActiveEffectItem
{
    public string DisplayName { get; set; } = string.Empty;
    public int Index { get; set; }
}

public class AvsEffectsPreset
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxActiveEffects { get; set; } = 8;
    public bool AutoRotateEffects { get; set; } = true;
    public float RotationSpeed { get; set; } = 1.0f;
    public bool BeatReactive { get; set; } = true;
    public bool ShowEffectNames { get; set; } = true;
    public bool ShowEffectGrid { get; set; } = true;
    public float EffectSpacing { get; set; } = 20.0f;
    public List<string> SelectedEffects { get; set; } = new List<string>();
}