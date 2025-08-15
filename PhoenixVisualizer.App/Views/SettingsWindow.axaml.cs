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
    private RadioButton? AvsRadio        => this.FindControl<RadioButton>("AvsRadio");
    private RadioButton? PhoenixRadio    => this.FindControl<RadioButton>("PhoenixRadio");
    private ComboBox?    SampleRateCombo => this.FindControl<ComboBox>("SampleRateCombo");
    private ComboBox?    BufferSizeCombo => this.FindControl<ComboBox>("BufferSizeCombo");
    private CheckBox?    VsyncCheck      => this.FindControl<CheckBox>("VsyncCheck");
    private CheckBox?    FullscreenCheck => this.FindControl<CheckBox>("FullscreenCheck");
    private CheckBox?    AutoHideUICheck => this.FindControl<CheckBox>("AutoHideUICheck");

    public SettingsWindow()
    {
        // Load the full XAML you pasted
        AvaloniaXamlLoader.Load(this);

        // OPTIONAL: if you actually have a ViewModel type, you can set it here.
        // DataContext = new ViewModels.SettingsWindowViewModel();

        // Sync current fields -> UI controls
        LoadCurrentSettings();
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
        if (SelectedPlugin == "phoenix") { PhoenixRadio?.SetCurrentValue(RadioButton.IsCheckedProperty, true); }
        else                             { AvsRadio?.SetCurrentValue(RadioButton.IsCheckedProperty, true); }

        // SampleRate
        if (SampleRateCombo is not null)
        {
            SampleRateCombo.SelectedIndex = SampleRate switch
            {
                22050 => 0,
                44100 => 1,
                48000 => 2,
                96000 => 3,
                _     => 1
            };
        }

        // BufferSize
        if (BufferSizeCombo is not null)
        {
            BufferSizeCombo.SelectedIndex = BufferSize switch
            {
                256  => 0,
                512  => 1,
                1024 => 2,
                2048 => 3,
                _    => 1
            };
        }

        VsyncCheck?.SetCurrentValue(CheckBox.IsCheckedProperty,      EnableVsync);
        FullscreenCheck?.SetCurrentValue(CheckBox.IsCheckedProperty, StartFullscreen);
        AutoHideUICheck?.SetCurrentValue(CheckBox.IsCheckedProperty, AutoHideUI);
    }

    private void SaveSettingsFromUI()
    {
        SelectedPlugin = PhoenixRadio?.IsChecked == true ? "phoenix" : "avs";

        if (SampleRateCombo is not null)
        {
            SampleRate = SampleRateCombo.SelectedIndex switch
            {
                0 => 22050,
                1 => 44100,
                2 => 48000,
                3 => 96000,
                _ => 44100
            };
        }

        if (BufferSizeCombo is not null)
        {
            BufferSize = BufferSizeCombo.SelectedIndex switch
            {
                0 => 256,
                1 => 512,
                2 => 1024,
                3 => 2048,
                _ => 512
            };
        }

        EnableVsync     = VsyncCheck?.IsChecked      ?? true;
        StartFullscreen = FullscreenCheck?.IsChecked ?? false;
        AutoHideUI      = AutoHideUICheck?.IsChecked ?? true;
    }
}
