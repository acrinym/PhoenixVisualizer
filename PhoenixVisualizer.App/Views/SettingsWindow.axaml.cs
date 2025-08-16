using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace PhoenixVisualizer.Views;

public partial class SettingsWindow : Window
{
    // Public settings snapshot (matches your previous fields)
    public string SelectedPlugin     { get; private set; } = "avs";
    public int    SampleRate         { get; private set; } = 44100;
    public int    BufferSize         { get; private set; } = 512;
    public bool   EnableVsync        { get; private set; } = true;
    public bool   StartFullscreen    { get; private set; } = false;
    public bool   AutoHideUI         { get; private set; } = true;

    // Named controls (must match XAML x:Name)
    private RadioButton? AvsRadioControl        => this.FindControl<RadioButton>("AvsRadio");
    private RadioButton? PhoenixRadioControl    => this.FindControl<RadioButton>("PhoenixRadio");
    private ComboBox?    SampleRateComboControl => this.FindControl<ComboBox>("SampleRateCombo");
    private ComboBox?    BufferSizeComboControl => this.FindControl<ComboBox>("BufferSizeCombo");
    private CheckBox?    VsyncCheckControl      => this.FindControl<CheckBox>("VsyncCheck");
    private CheckBox?    FullscreenCheckControl => this.FindControl<CheckBox>("FullscreenCheck");
    private CheckBox?    AutoHideUICheckControl => this.FindControl<CheckBox>("AutoHideUICheck");

    public SettingsWindow()
    {
        InitializeComponent();

        // OPTIONAL: if you actually have a ViewModel type, you can set it here.
        // DataContext = new ViewModels.SettingsWindowViewModel();

        // Sync current fields -> UI controls
        LoadCurrentSettings();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // Wire to Button Clicks in XAML
    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        SaveSettingsFromUI();
        Close();
    }

    private void LoadCurrentSettings()
    {
        // Radios
        if (SelectedPlugin == "phoenix") { PhoenixRadioControl?.SetCurrentValue(RadioButton.IsCheckedProperty, true); }
        else                             { AvsRadioControl?.SetCurrentValue(RadioButton.IsCheckedProperty, true); }

        // SampleRate
        if (SampleRateComboControl is not null)
        {
            SampleRateComboControl.SelectedIndex = SampleRate switch
            {
                22050 => 0,
                44100 => 1,
                48000 => 2,
                96000 => 3,
                _     => 1
            };
        }

        // BufferSize
        if (BufferSizeComboControl is not null)
        {
            BufferSizeComboControl.SelectedIndex = BufferSize switch
            {
                256  => 0,
                512  => 1,
                1024 => 2,
                2048 => 3,
                _    => 1
            };
        }

        VsyncCheckControl?.SetCurrentValue(CheckBox.IsCheckedProperty,      EnableVsync);
        FullscreenCheckControl?.SetCurrentValue(CheckBox.IsCheckedProperty, StartFullscreen);
        AutoHideUICheckControl?.SetCurrentValue(CheckBox.IsCheckedProperty, AutoHideUI);
    }

    private void SaveSettingsFromUI()
    {
        SelectedPlugin = PhoenixRadioControl?.IsChecked == true ? "phoenix" : "avs";

        if (SampleRateComboControl is not null)
        {
            SampleRate = SampleRateComboControl.SelectedIndex switch
            {
                0 => 22050,
                1 => 44100,
                2 => 48000,
                3 => 96000,
                _ => 44100
            };
        }

        if (BufferSizeComboControl is not null)
        {
            BufferSize = BufferSizeComboControl.SelectedIndex switch
            {
                0 => 256,
                1 => 512,
                2 => 1024,
                3 => 2048,
                _ => 512
            };
        }

        EnableVsync     = VsyncCheckControl?.IsChecked      ?? true;
        StartFullscreen = FullscreenCheckControl?.IsChecked ?? false;
        AutoHideUI      = AutoHideUICheckControl?.IsChecked ?? true;
    }
}
