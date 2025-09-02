using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.Core;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using EffectParam = PhoenixVisualizer.Core.Nodes.EffectParam;

namespace PhoenixVisualizer.App.Views;

/// <summary>
/// ViewModel for Parameter Editor - handles reactive parameter binding with ParameterSystem
/// </summary>
public class ParameterEditorViewModel : ReactiveObject
{
    private Dictionary<string, ParameterSystem.ParameterDefinition> _parameterDefinitions = new();
    private Dictionary<string, ParameterSystem.ParameterValue> _parameterValues = new();
    private string _visualizerId = "";
    private string _visualizerName = "";

    public Dictionary<string, ParameterSystem.ParameterDefinition> ParameterDefinitions
    {
        get => _parameterDefinitions;
        set => this.RaiseAndSetIfChanged(ref _parameterDefinitions, value);
    }

    public Dictionary<string, ParameterSystem.ParameterValue> ParameterValues
    {
        get => _parameterValues;
        set => this.RaiseAndSetIfChanged(ref _parameterValues, value);
    }

    public string VisualizerId
    {
        get => _visualizerId;
        set => this.RaiseAndSetIfChanged(ref _visualizerId, value);
    }

    public string VisualizerName
    {
        get => _visualizerName;
        set => this.RaiseAndSetIfChanged(ref _visualizerName, value);
    }

    public ParameterEditorViewModel()
    {
        // React to parameter changes
        this.WhenAnyValue(x => x.ParameterDefinitions)
            .Subscribe(_ => UpdateParameterControls());

        this.WhenAnyValue(x => x.ParameterValues)
            .Subscribe(_ => UpdateParameterControls());
    }

    private void UpdateParameterControls()
    {
        // This will be handled by the view
    }

    /// <summary>
    /// Load parameters for a specific visualizer
    /// </summary>
    public void LoadVisualizerParameters(string visualizerId, string visualizerName)
    {
        VisualizerId = visualizerId;
        VisualizerName = visualizerName;

        ParameterDefinitions = ParameterSystem.GetVisualizerParameters(visualizerId);
        ParameterValues = ParameterSystem.GetVisualizerParameterValues(visualizerId);
    }

    /// <summary>
    /// Update a parameter value
    /// </summary>
    public void UpdateParameterValue(string parameterKey, object value)
    {
        if (!string.IsNullOrEmpty(VisualizerId))
        {
            ParameterSystem.SetParameterValue(VisualizerId, parameterKey, value);

            // Update local copy
            if (ParameterValues.ContainsKey(parameterKey))
            {
                ParameterValues[parameterKey].Value = value;
                ParameterValues[parameterKey].LastModified = DateTime.Now;
                ParameterValues[parameterKey].IsModified = true;

                // Notify UI of the change
                this.RaisePropertyChanged(nameof(ParameterValues));
            }
        }
    }

    /// <summary>
    /// Reset parameter to default
    /// </summary>
    public void ResetParameterToDefault(string parameterKey)
    {
        if (!string.IsNullOrEmpty(VisualizerId))
        {
            ParameterSystem.ResetParameterToDefault(VisualizerId, parameterKey);

            // Reload values
            ParameterValues = ParameterSystem.GetVisualizerParameterValues(VisualizerId);
        }
    }

    /// <summary>
    /// Reset all parameters to defaults
    /// </summary>
    public void ResetAllToDefaults()
    {
        if (!string.IsNullOrEmpty(VisualizerId))
        {
            ParameterSystem.ResetAllParametersToDefaults(VisualizerId);

            // Reload values
            ParameterValues = ParameterSystem.GetVisualizerParameterValues(VisualizerId);
        }
    }
}

/// <summary>
/// Parameter Editor - Dynamically generates UI controls for effect parameters
/// Supports sliders, checkboxes, colors, and dropdowns with real-time binding
/// </summary>
public partial class ParameterEditor : UserControl
{
    public static readonly StyledProperty<string> VisualizerIdProperty =
        AvaloniaProperty.Register<ParameterEditor, string>(nameof(VisualizerId));

    public static readonly StyledProperty<string> VisualizerNameProperty =
        AvaloniaProperty.Register<ParameterEditor, string>(nameof(VisualizerName));

    public string VisualizerId
    {
        get => GetValue(VisualizerIdProperty);
        set => SetValue(VisualizerIdProperty, value);
    }

    public string VisualizerName
    {
        get => GetValue(VisualizerNameProperty);
        set => SetValue(VisualizerNameProperty, value);
    }

    private ParameterEditorViewModel _viewModel;

    public ParameterEditor()
    {
        InitializeComponent();
        _viewModel = new ParameterEditorViewModel();

        // React to parameter changes
        _viewModel.WhenAnyValue(x => x.ParameterDefinitions)
            .Subscribe(_ => UpdateParameterControls());

        _viewModel.WhenAnyValue(x => x.ParameterValues)
            .Subscribe(_ => UpdateParameterControls());
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VisualizerIdProperty || change.Property == VisualizerNameProperty)
        {
            if (!string.IsNullOrEmpty(VisualizerId) && !string.IsNullOrEmpty(VisualizerName))
            {
                _viewModel.LoadVisualizerParameters(VisualizerId, VisualizerName);
            }
        }
    }

    private void UpdateParameterControls()
    {
        // Clear existing controls
        ParametersPanel.Children.Clear();

        if (_viewModel?.ParameterDefinitions == null || _viewModel.ParameterDefinitions.Count == 0)
        {
            // Show default message
            var textBlock = new TextBlock
            {
                Text = "Select a visualizer to edit parameters",
                Foreground = Brushes.White,
                Opacity = 0.6,
                TextAlignment = Avalonia.Media.TextAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };
            ParametersPanel.Children.Add(textBlock);
            return;
        }

        // Add visualizer header
        var header = new TextBlock
        {
            Text = $"{_viewModel.VisualizerName} Parameters",
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 10)
        };
        ParametersPanel.Children.Add(header);

        // Group parameters by category
        var categories = _viewModel.ParameterDefinitions
            .GroupBy(p => p.Value.Category)
            .OrderBy(g => g.Key);

        foreach (var categoryGroup in categories)
        {
            // Category header
            if (!string.IsNullOrEmpty(categoryGroup.Key) && categoryGroup.Key != "General")
            {
                var categoryHeader = new TextBlock
                {
                    Text = categoryGroup.Key,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Brushes.LightBlue,
                    FontSize = 12,
                    Margin = new Thickness(0, 10, 0, 5)
                };
                ParametersPanel.Children.Add(categoryHeader);
            }

            // Generate controls for each parameter in this category
            foreach (var kvp in categoryGroup.OrderBy(p => p.Value.Label))
            {
                var container = CreateParameterContainer(kvp.Key, kvp.Value);
                ParametersPanel.Children.Add(container);
            }
        }

        // Add reset buttons at the bottom
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Margin = new Thickness(0, 20, 0, 0)
        };

        var resetAllButton = new Button
        {
            Content = "Reset All",
            Background = Brushes.DarkRed,
            Foreground = Brushes.White,
            Height = 30,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        resetAllButton.Click += (s, e) => _viewModel.ResetAllToDefaults();

        buttonPanel.Children.Add(resetAllButton);
        ParametersPanel.Children.Add(buttonPanel);
    }

    private Control CreateParameterContainer(string paramKey, ParameterSystem.ParameterDefinition paramDef)
    {
        var container = new StackPanel
        {
            Spacing = 5,
            Margin = new Thickness(0, 0, 0, 15)
        };

        // Parameter label and description
        var label = new TextBlock
        {
            Text = paramDef.Label,
            Foreground = Brushes.White,
            FontWeight = FontWeight.Medium,
            FontSize = 12
        };
        container.Children.Add(label);

        if (!string.IsNullOrEmpty(paramDef.Description))
        {
            var description = new TextBlock
            {
                Text = paramDef.Description,
                Foreground = Brushes.Gray,
                FontSize = 10,
                Margin = new Thickness(0, 0, 0, 5)
            };
            container.Children.Add(description);
        }

        // Parameter control
        var control = CreateParameterControl(paramKey, paramDef);
        container.Children.Add(control);

        return container;
    }

    private Control CreateParameterControl(string paramKey, ParameterSystem.ParameterDefinition paramDef)
    {
        return paramDef.Type switch
        {
            ParameterSystem.ParameterType.Slider => CreateSliderControl(paramKey, paramDef),
            ParameterSystem.ParameterType.Checkbox => CreateCheckboxControl(paramKey, paramDef),
            ParameterSystem.ParameterType.Color => CreateColorControl(paramKey, paramDef),
            ParameterSystem.ParameterType.Dropdown => CreateDropdownControl(paramKey, paramDef),
            ParameterSystem.ParameterType.Dial => CreateDialControl(paramKey, paramDef),
            ParameterSystem.ParameterType.Text => CreateTextControl(paramKey, paramDef),
            ParameterSystem.ParameterType.File => CreateFileControl(paramKey, paramDef),
            ParameterSystem.ParameterType.Directory => CreateDirectoryControl(paramKey, paramDef),
            _ => CreateTextBlock($"Unsupported parameter type: {paramDef.Type}")
        };
    }

    private Control CreateSliderControl(string paramKey, ParameterSystem.ParameterDefinition paramDef)
    {
        var currentValue = _viewModel.ParameterValues.ContainsKey(paramKey)
            ? _viewModel.ParameterValues[paramKey].Value
            : paramDef.DefaultValue;

        var slider = new Slider
        {
            Minimum = Convert.ToDouble(paramDef.MinValue ?? 0),
            Maximum = Convert.ToDouble(paramDef.MaxValue ?? 100),
            Value = Convert.ToDouble(currentValue ?? paramDef.DefaultValue),
            Height = 25,
            Margin = new Thickness(0, 5, 0, 0)
        };

        // Value display
        var valueText = new TextBlock
        {
            Text = $"{slider.Value:F2}",
            Foreground = Brushes.LightGray,
            FontSize = 10,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 0, 5)
        };

        // Handle value changes
        slider.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == "Value")
            {
                valueText.Text = $"{slider.Value:F2}";
                _viewModel.UpdateParameterValue(paramKey, slider.Value);
            }
        };

        // Container for slider and value display
        var container = new StackPanel { Spacing = 5 };
        container.Children.Add(slider);
        container.Children.Add(valueText);

        return container;
    }

    private Control CreateCheckboxControl(string paramKey, ParameterSystem.ParameterDefinition paramDef)
    {
        var currentValue = _viewModel.ParameterValues.ContainsKey(paramKey)
            ? _viewModel.ParameterValues[paramKey].Value
            : paramDef.DefaultValue;

        var checkbox = new CheckBox
        {
            Content = paramDef.Label,
            IsChecked = Convert.ToBoolean(currentValue ?? paramDef.DefaultValue),
            Foreground = Brushes.White,
            Margin = new Thickness(0, 5, 0, 0)
        };

        // Handle value changes
        checkbox.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == "IsChecked")
            {
                _viewModel.UpdateParameterValue(paramKey, checkbox.IsChecked ?? false);
            }
        };

        return checkbox;
    }

    private Control CreateColorControl(string paramKey, ParameterSystem.ParameterDefinition paramDef)
    {
        var currentValue = _viewModel.ParameterValues.ContainsKey(paramKey)
            ? _viewModel.ParameterValues[paramKey].Value?.ToString()
            : paramDef.DefaultValue?.ToString();

        // For now, use a text box for color input
        // In a full implementation, this would be a color picker
        var textBox = new TextBox
        {
            Text = currentValue ?? "#FFFFFF",
            Watermark = "#RRGGBB",
            Margin = new Thickness(0, 5, 0, 0),
            Height = 30
        };

        // Handle value changes
        textBox.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == "Text")
            {
                _viewModel.UpdateParameterValue(paramKey, textBox.Text);
            }
        };

        return textBox;
    }

    private Control CreateDropdownControl(string paramKey, ParameterSystem.ParameterDefinition paramDef)
    {
        var currentValue = _viewModel.ParameterValues.ContainsKey(paramKey)
            ? _viewModel.ParameterValues[paramKey].Value?.ToString()
            : paramDef.DefaultValue?.ToString();

        var comboBox = new ComboBox
        {
            ItemsSource = paramDef.Options,
            SelectedItem = currentValue ?? paramDef.DefaultValue?.ToString(),
            Margin = new Thickness(0, 5, 0, 0),
            Height = 30
        };

        // Handle selection changes
        comboBox.SelectionChanged += (s, e) =>
        {
            if (comboBox.SelectedItem is string selectedValue)
            {
                _viewModel.UpdateParameterValue(paramKey, selectedValue);
            }
        };

        return comboBox;
    }

    private Control CreateDialControl(string paramKey, ParameterSystem.ParameterDefinition paramDef)
    {
        // For now, use a slider as a dial substitute
        return CreateSliderControl(paramKey, paramDef);
    }

    private Control CreateTextControl(string paramKey, ParameterSystem.ParameterDefinition paramDef)
    {
        var currentValue = _viewModel.ParameterValues.ContainsKey(paramKey)
            ? _viewModel.ParameterValues[paramKey].Value?.ToString()
            : paramDef.DefaultValue?.ToString();

        var textBox = new TextBox
        {
            Text = currentValue ?? "",
            Margin = new Thickness(0, 5, 0, 0),
            Height = 30
        };

        // Handle value changes
        textBox.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == "Text")
            {
                _viewModel.UpdateParameterValue(paramKey, textBox.Text);
            }
        };

        return textBox;
    }

    private Control CreateFileControl(string paramKey, ParameterSystem.ParameterDefinition paramDef)
    {
        var currentValue = _viewModel.ParameterValues.ContainsKey(paramKey)
            ? _viewModel.ParameterValues[paramKey].Value?.ToString()
            : paramDef.DefaultValue?.ToString();

        var container = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

        var textBox = new TextBox
        {
            Text = currentValue ?? "",
            Margin = new Thickness(0, 5, 0, 0),
            Height = 30,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var browseButton = new Button
        {
            Content = "Browse...",
            Height = 30,
            Margin = new Thickness(0, 5, 0, 0)
        };

        browseButton.Click += (s, e) =>
        {
            // TODO: Implement file dialog
            // For now, just update with current value
            _viewModel.UpdateParameterValue(paramKey, textBox.Text);
        };

        textBox.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == "Text")
            {
                _viewModel.UpdateParameterValue(paramKey, textBox.Text);
            }
        };

        container.Children.Add(textBox);
        container.Children.Add(browseButton);

        return container;
    }

    private Control CreateDirectoryControl(string paramKey, ParameterSystem.ParameterDefinition paramDef)
    {
        var currentValue = _viewModel.ParameterValues.ContainsKey(paramKey)
            ? _viewModel.ParameterValues[paramKey].Value?.ToString()
            : paramDef.DefaultValue?.ToString();

        var container = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

        var textBox = new TextBox
        {
            Text = currentValue ?? "",
            Margin = new Thickness(0, 5, 0, 0),
            Height = 30,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var browseButton = new Button
        {
            Content = "Browse...",
            Height = 30,
            Margin = new Thickness(0, 5, 0, 0)
        };

        browseButton.Click += (s, e) =>
        {
            // TODO: Implement directory dialog
            // For now, just update with current value
            _viewModel.UpdateParameterValue(paramKey, textBox.Text);
        };

        textBox.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == "Text")
            {
                _viewModel.UpdateParameterValue(paramKey, textBox.Text);
            }
        };

        container.Children.Add(textBox);
        container.Children.Add(browseButton);

        return container;
    }

    private Control CreateTextBlock(string text)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = Brushes.Orange,
            FontSize = 11,
            Margin = new Thickness(0, 5, 0, 0)
        };
    }

    /// <summary>
    /// Public method to update parameters programmatically for visualizers
    /// </summary>
    public void UpdateParameters(string visualizerId, string visualizerName)
    {
        _viewModel.LoadVisualizerParameters(visualizerId, visualizerName);
    }

    /// <summary>
    /// Public method to update parameters for effects (legacy support)
    /// </summary>
    public void UpdateParameters(string effectName, Dictionary<string, EffectParam> parameters)
    {
        // For backward compatibility with effect parameters
        // This is a simplified implementation that just shows the effect name
        ParametersPanel.Children.Clear();

        var header = new TextBlock
        {
            Text = $"{effectName} Parameters (Legacy)",
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 10)
        };
        ParametersPanel.Children.Add(header);

        if (parameters != null && parameters.Count > 0)
        {
            foreach (var kvp in parameters)
            {
                var container = new StackPanel
                {
                    Spacing = 5,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var label = new TextBlock
                {
                    Text = kvp.Value.Label,
                    Foreground = Brushes.White,
                    FontWeight = FontWeight.Medium,
                    FontSize = 12
                };
                container.Children.Add(label);

                var valueText = new TextBlock
                {
                    Text = kvp.Value.Type switch
                    {
                        "slider" => $"{kvp.Value.FloatValue:F2}",
                        "checkbox" => kvp.Value.BoolValue.ToString(),
                        "color" => kvp.Value.ColorValue,
                        "dropdown" => kvp.Value.StringValue,
                        _ => "N/A"
                    },
                    Foreground = Brushes.LightGray,
                    FontSize = 10
                };
                container.Children.Add(valueText);

                ParametersPanel.Children.Add(container);
            }
        }
        else
        {
            var noParamsText = new TextBlock
            {
                Text = "No parameters available for this effect",
                Foreground = Brushes.Gray,
                FontSize = 11,
                TextAlignment = Avalonia.Media.TextAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };
            ParametersPanel.Children.Add(noParamsText);
        }
    }

    /// <summary>
    /// Public method to get current parameter values
    /// </summary>
    public Dictionary<string, object> GetCurrentParameterValues()
    {
        var result = new Dictionary<string, object>();
        foreach (var kvp in _viewModel.ParameterValues)
        {
            result[kvp.Key] = kvp.Value.Value;
        }
        return result;
    }
}
