using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using PhoenixVisualizer.Core.Nodes;
using EffectParam = PhoenixVisualizer.Core.Nodes.EffectParam;

namespace PhoenixVisualizer.App.Views;

/// <summary>
/// Parameter Editor - Dynamically generates UI controls for effect parameters
/// Supports sliders, checkboxes, colors, and dropdowns
/// </summary>
public partial class ParameterEditor : UserControl
{
    public static readonly StyledProperty<Dictionary<string, EffectParam>> ParametersProperty =
        AvaloniaProperty.Register<ParameterEditor, Dictionary<string, EffectParam>>(nameof(Parameters));

    public static readonly StyledProperty<string> EffectNameProperty =
        AvaloniaProperty.Register<ParameterEditor, string>(nameof(EffectName));

    public Dictionary<string, EffectParam> Parameters
    {
        get => GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public string EffectName
    {
        get => GetValue(EffectNameProperty);
        set => SetValue(EffectNameProperty, value);
    }

    public ParameterEditor()
    {
        InitializeComponent();
        Parameters = new Dictionary<string, EffectParam>();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ParametersProperty || change.Property == EffectNameProperty)
        {
            UpdateParameterControls();
        }
    }

    private void UpdateParameterControls()
    {
        // Clear existing controls
        ParametersPanel.Children.Clear();

        if (Parameters == null || Parameters.Count == 0)
        {
            // Show default message
            var textBlock = new TextBlock
            {
                Text = "Select an effect to edit parameters",
                Foreground = Brushes.White,
                Opacity = 0.6,
                TextAlignment = Avalonia.Media.TextAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };
            ParametersPanel.Children.Add(textBlock);
            return;
        }

        // Add effect header
        var header = new TextBlock
        {
            Text = $"{EffectName} Parameters",
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 10)
        };
        ParametersPanel.Children.Add(header);

        // Generate controls for each parameter
        foreach (var kvp in Parameters)
        {
            var container = CreateParameterContainer(kvp.Key, kvp.Value);
            ParametersPanel.Children.Add(container);
        }
    }

    private Control CreateParameterContainer(string paramName, EffectParam param)
    {
        var container = new StackPanel
        {
            Spacing = 5,
            Margin = new Thickness(0, 0, 0, 15)
        };

        // Parameter label
        var label = new TextBlock
        {
            Text = param.Label,
            Foreground = Brushes.White,
            FontWeight = FontWeight.Medium,
            FontSize = 12
        };
        container.Children.Add(label);

        // Parameter control
        var control = CreateParameterControl(paramName, param);
        container.Children.Add(control);

        // Value display (for sliders)
        if (param.Type == "slider")
        {
            var valueText = new TextBlock
            {
                Text = $"{param.FloatValue:F2}",
                Foreground = Brushes.LightGray,
                FontSize = 10,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
            };

            // Bind value changes to update display
            if (control is Slider slider)
            {
                slider.PropertyChanged += (s, e) =>
                {
                    if (e.Property.Name == "Value")
                    {
                        valueText.Text = $"{slider.Value:F2}";
                        param.FloatValue = (float)slider.Value;
                    }
                };
            }

            container.Children.Add(valueText);
        }

        return container;
    }

    private Control CreateParameterControl(string paramName, EffectParam param)
    {
        return param.Type switch
        {
            "slider" => CreateSliderControl(param),
            "checkbox" => CreateCheckboxControl(param),
            "color" => CreateColorControl(param),
            "dropdown" => CreateDropdownControl(param),
            _ => CreateTextBlock($"Unsupported parameter type: {param.Type}")
        };
    }

    private Control CreateSliderControl(EffectParam param)
    {
        var slider = new Slider
        {
            Minimum = param.Min,
            Maximum = param.Max,
            Value = param.FloatValue,
            Height = 20,
            Margin = new Thickness(0, 5, 0, 0)
        };

        // Handle value changes
        slider.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == "Value")
            {
                param.FloatValue = (float)slider.Value;
            }
        };

        return slider;
    }

    private Control CreateCheckboxControl(EffectParam param)
    {
        var checkbox = new CheckBox
        {
            Content = param.Label,
            IsChecked = param.BoolValue,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 5, 0, 0)
        };

        // Handle value changes
        checkbox.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == "IsChecked")
            {
                param.BoolValue = checkbox.IsChecked ?? false;
            }
        };

        return checkbox;
    }

    private Control CreateColorControl(EffectParam param)
    {
        // For now, use a text box for color input
        // In a full implementation, this would be a color picker
        var textBox = new TextBox
        {
            Text = param.ColorValue,
            Watermark = "#RRGGBB",
            Margin = new Thickness(0, 5, 0, 0),
            Height = 25
        };

        // Handle value changes
        textBox.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == "Text")
            {
                param.ColorValue = textBox.Text;
            }
        };

        return textBox;
    }

    private Control CreateDropdownControl(EffectParam param)
    {
        var comboBox = new ComboBox
        {
            ItemsSource = param.Options,
            SelectedItem = param.StringValue,
            Margin = new Thickness(0, 5, 0, 0),
            Height = 25
        };

        // Handle selection changes
        comboBox.SelectionChanged += (s, e) =>
        {
            if (comboBox.SelectedItem is string selectedValue)
            {
                param.StringValue = selectedValue;
            }
        };

        return comboBox;
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
    /// Public method to update parameters programmatically
    /// </summary>
    public void UpdateParameters(string effectName, Dictionary<string, EffectParam> parameters)
    {
        EffectName = effectName;
        Parameters = parameters ?? new Dictionary<string, EffectParam>();
        UpdateParameterControls();
    }
}
