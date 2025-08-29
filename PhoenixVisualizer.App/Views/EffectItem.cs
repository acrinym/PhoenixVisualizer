using System.ComponentModel;

namespace PhoenixVisualizer.Views;

public class EffectItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    // Add parameterless constructor for inheritance
    public EffectItem() { }

    // Keep existing constructor for backward compatibility
    public EffectItem(string name, string category) : this()
    {
        Name = name;
        Category = category;
        DisplayName = $"{Name} ({Category})";
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
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