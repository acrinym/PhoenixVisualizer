using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia;
using System;
using System.Diagnostics;

namespace PhoenixVisualizer.Views;

public partial class SettingsWindow : Window
{
    public string SelectedPlugin { get; private set; } = "avs";
    public int SampleRate { get; private set; } = 44100;
    public int BufferSize { get; private set; } = 512;
    public bool EnableVsync { get; private set; } = true;
    public bool StartFullscreen { get; private set; } = false;
    public bool AutoHideUI { get; private set; } = true;

    public SettingsWindow()
    {
        InitializeComponent();
        
        // Set the DataContext to our ViewModel
        DataContext = new ViewModels.SettingsWindowViewModel();
        
        // Set current values
        LoadCurrentSettings();
    }
    
    private void InitializeComponent()
    {
        // Since Avalonia code generation isn't working, we need to manually create the controls
        // This is a temporary workaround until we resolve the code generation issue
        
        // Create the main grid
        var grid = new Grid();
        grid.Margin = new Thickness(20);
        
        // Add row definitions
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        
        // Header
        var header = new TextBlock
        {
            Text = "Settings",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(header, 0);
        grid.Children.Add(header);
        
        // Content area - simplified without ScrollViewer
        var contentPanel = new StackPanel { Spacing = 20 };
        
        // Plugin selection section
        var pluginBorder = new Border
        {
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(15),
            CornerRadius = new CornerRadius(5)
        };
        
        var pluginPanel = new StackPanel();
        pluginPanel.Children.Add(new TextBlock { Text = "Visualization Plugin", FontSize = 16, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 10) });
        pluginPanel.Children.Add(new TextBlock { Text = "Choose the default visualization plugin:", Margin = new Thickness(0, 0, 0, 10), Foreground = Brushes.Gray });
        
        var avsRadio = new RadioButton { Content = "AVS Engine (Winamp-style)", Tag = "avs", IsChecked = true, Margin = new Thickness(0, 5) };
        var phoenixRadio = new RadioButton { Content = "Phoenix Visualizer", Tag = "phoenix", Margin = new Thickness(0, 5) };
        
        pluginPanel.Children.Add(avsRadio);
        pluginPanel.Children.Add(phoenixRadio);
        
        pluginBorder.Child = pluginPanel;
        contentPanel.Children.Add(pluginBorder);
        
        Grid.SetRow(contentPanel, 1);
        grid.Children.Add(contentPanel);
        
        // Buttons
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Margin = new Thickness(0, 20, 0, 0)
        };
        
        var cancelBtn = new Button { Content = "Cancel", Width = 80 };
        cancelBtn.Click += OnCancelClick;
        
        var applyBtn = new Button { Content = "Apply", Width = 80, IsDefault = true };
        applyBtn.Click += OnApplyClick;
        
        buttonPanel.Children.Add(cancelBtn);
        buttonPanel.Children.Add(applyBtn);
        
        Grid.SetRow(buttonPanel, 2);
        grid.Children.Add(buttonPanel);
        
        // Set as content
        this.Content = grid;
    }

    private void LoadCurrentSettings()
    {
        // Set radio buttons based on current plugin
        if (SelectedPlugin == "avs")
        {
            this.FindControl<RadioButton>("AvsRadio")?.SetValue(RadioButton.IsCheckedProperty, true);
        }
        else
        {
            this.FindControl<RadioButton>("PhoenixRadio")?.SetValue(RadioButton.IsCheckedProperty, true);
        }

        // Set sample rate combo
        var sampleRateCombo = this.FindControl<ComboBox>("SampleRateCombo");
        if (sampleRateCombo != null)
        {
            switch (SampleRate)
            {
                case 22050: sampleRateCombo.SelectedIndex = 0; break;
                case 44100: sampleRateCombo.SelectedIndex = 1; break;
                case 48000: sampleRateCombo.SelectedIndex = 2; break;
                case 96000: sampleRateCombo.SelectedIndex = 3; break;
            }
        }

        // Set buffer size combo
        var bufferSizeCombo = this.FindControl<ComboBox>("BufferSizeCombo");
        if (bufferSizeCombo != null)
        {
            switch (BufferSize)
            {
                case 256: bufferSizeCombo.SelectedIndex = 0; break;
                case 512: bufferSizeCombo.SelectedIndex = 1; break;
                case 1024: bufferSizeCombo.SelectedIndex = 2; break;
                case 2048: bufferSizeCombo.SelectedIndex = 3; break;
            }
        }

        // Set checkboxes
        this.FindControl<CheckBox>("VsyncCheck")?.SetValue(CheckBox.IsCheckedProperty, EnableVsync);
        this.FindControl<CheckBox>("FullscreenCheck")?.SetValue(CheckBox.IsCheckedProperty, StartFullscreen);
        this.FindControl<CheckBox>("AutoHideUICheck")?.SetValue(CheckBox.IsCheckedProperty, AutoHideUI);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        // Close without saving changes
        Close();
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        // Save current settings from UI
        SaveSettingsFromUI();
        
        // Close and return to main window
        Close();
    }

    private void SaveSettingsFromUI()
    {
        // Get plugin selection
        var avsRadio = this.FindControl<RadioButton>("AvsRadio");
        if (avsRadio?.IsChecked == true)
        {
            SelectedPlugin = "avs";
        }
        else
        {
            SelectedPlugin = "phoenix";
        }

        // Get sample rate
        var sampleRateCombo = this.FindControl<ComboBox>("SampleRateCombo");
        if (sampleRateCombo?.SelectedIndex >= 0)
        {
            SampleRate = sampleRateCombo.SelectedIndex switch
            {
                0 => 22050,
                1 => 44100,
                2 => 48000,
                3 => 96000,
                _ => 44100
            };
        }

        // Get buffer size
        var bufferSizeCombo = this.FindControl<ComboBox>("BufferSizeCombo");
        if (bufferSizeCombo?.SelectedIndex >= 0)
        {
            BufferSize = bufferSizeCombo.SelectedIndex switch
            {
                0 => 256,
                1 => 512,
                2 => 1024,
                3 => 2048,
                _ => 512
            };
        }

        // Get checkboxes
        var vsyncCheck = this.FindControl<CheckBox>("VsyncCheck");
        EnableVsync = vsyncCheck?.IsChecked ?? true;

        var fullscreenCheck = this.FindControl<CheckBox>("FullscreenCheck");
        StartFullscreen = fullscreenCheck?.IsChecked ?? false;

        var autoHideCheck = this.FindControl<CheckBox>("AutoHideUICheck");
        AutoHideUI = autoHideCheck?.IsChecked ?? true;

        System.Diagnostics.Debug.WriteLine($"Settings saved: Plugin={SelectedPlugin}, SampleRate={SampleRate}, BufferSize={BufferSize}");
    }
}
