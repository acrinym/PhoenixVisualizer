using Avalonia.Controls;
using Avalonia.Interactivity;
using PhoenixVisualizer.Parameters;
using System.Collections.Generic;

namespace PhoenixVisualizer.Editor.Views;

public partial class ParameterEditor : UserControl
{
    public string? VisualizerId { get; private set; }
    public string VisualizerName { get; private set; } = "";

    public ParameterEditor()
    {
        InitializeComponent();
        ParamRegistry.DefinitionsChanged += OnDefs;
    }

    public void LoadFor(string visualizerId, string visualizerName)
    {
        VisualizerId = visualizerId;
        VisualizerName = visualizerName;
        Rebuild();
    }

    private void OnDefs(string vid) { if (VisualizerId == vid) Rebuild(); }

    private void Rebuild()
    {
        Root.Children.Clear();
        if (VisualizerId is null) return;
        
        var defs = ParamRegistry.GetDefs(VisualizerId);
        var vals = ParamRegistry.GetValues(VisualizerId);
        
        foreach (var def in defs.Values)
        {
            Control ctrl = def.Type switch
            {
                ParamType.Checkbox => BuildCheckbox(def, vals),
                ParamType.Dropdown => BuildDropdown(def, vals),
                ParamType.Color    => BuildText(def, vals),
                ParamType.Slider or ParamType.Dial => BuildSlider(def, vals),
                ParamType.File or ParamType.Directory => BuildText(def, vals),
                _ => BuildText(def, vals)
            };
            
            var panel = new StackPanel { Spacing = 4 };
            panel.Children.Add(new TextBlock { Text = def.Label, FontWeight = Avalonia.Media.FontWeight.Bold });
            panel.Children.Add(ctrl);
            if (!string.IsNullOrWhiteSpace(def.Description))
                panel.Children.Add(new TextBlock { Text = def.Description, Classes = { "muted" } });
            
            Root.Children.Add(panel);
        }
    }

    private Control BuildCheckbox(ParamDef def, IReadOnlyDictionary<string, object?> vals)
    {
        var cb = new CheckBox { IsChecked = vals.TryGetValue(def.Key, out var v) && v is bool b && b };
        cb.IsCheckedChanged += (_, __) => Update(def.Key, cb.IsChecked ?? false);
        return cb;
    }

    private Control BuildDropdown(ParamDef def, IReadOnlyDictionary<string, object?> vals)
    {
        var combo = new ComboBox { ItemsSource = def.Options ?? new(), SelectedIndex = 0 };
        if (vals.TryGetValue(def.Key, out var v) && v is string s && def.Options is { Count: >0 })
            combo.SelectedIndex = Math.Max(0, def.Options.IndexOf(s));
        combo.SelectionChanged += (_, __) =>
        {
            var sel = combo.SelectedItem as string ?? "";
            Update(def.Key, sel);
        };
        return combo;
    }

    private Control BuildSlider(ParamDef def, IReadOnlyDictionary<string, object?> vals)
    {
        var min = def.Min ?? 0; var max = def.Max ?? 1;
        var s = new Slider { Minimum = min, Maximum = max, TickFrequency = (max-min)/100.0 };
        if (vals.TryGetValue(def.Key, out var v) && v is IConvertible)
            s.Value = Convert.ToDouble(v);
        s.PropertyChanged += (_, e) =>
        {
            if (e.Property.Name == nameof(Slider.Value))
                Update(def.Key, s.Value);
        };
        return s;
    }

    private Control BuildText(ParamDef def, IReadOnlyDictionary<string, object?> vals)
    {
        var tb = new TextBox { Text = vals.TryGetValue(def.Key, out var v) ? v?.ToString() ?? "" : "" };
        tb.LostFocus += (_, __) => Update(def.Key, tb.Text ?? "");
        return tb;
    }

    private void Update(string key, object? value)
    {
        if (VisualizerId is null) return;
        ParamRegistry.Set(VisualizerId, key, value);
    }
}
