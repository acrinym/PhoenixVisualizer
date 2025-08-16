using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using PhoenixVisualizer.Core.Config;

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

    // Visualizer settings 📊
    private VisualizerSettings _vz = VisualizerSettings.Load();

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
        LoadVisualizerSettings();
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
        SaveVisualizerSettings();
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

    // --- Visualizer settings helpers ---
    private void LoadVisualizerSettings()
    {
        // sliders + labels
        if (GainSlider is { } gs && GainLabel is { }) { gs.Value = _vz.InputGainDb; GainLabel.Text = $"{_vz.InputGainDb:0.#} dB"; }
        if (SmoothSlider is { } ss && SmoothLabel is { }) { ss.Value = _vz.SmoothingMs; SmoothLabel.Text = $"{_vz.SmoothingMs:0}"; }
        if (GateSlider is { } gts && GateLabel is { }) { gts.Value = _vz.NoiseGateDb; GateLabel.Text = $"{_vz.NoiseGateDb:0}"; }
        if (BeatSlider is { } bs && BeatLabel is { }) { bs.Value = _vz.BeatSensitivityOrDefault(); BeatLabel.Text = $"{_vz.BeatSensitivity:0.00}×"; }
        if (BlendSlider is { } bls && BlendLabel is { }) { bls.Value = _vz.FrameBlend; BlendLabel.Text = $"{_vz.FrameBlend:0.00}"; }
        if (FftCombo is { }) FftCombo.SelectedIndex = _vz.FftSize == 1024 ? 0 : 1;
        if (ScaleCombo is { })
            ScaleCombo.SelectedIndex = _vz.SpectrumScale switch { SpectrumScale.Linear => 0, SpectrumScale.Log => 1, _ => 2 };
        if (AutoGainCheck is { }) AutoGainCheck.IsChecked = _vz.AutoGain;
        if (PeaksCheck is { }) PeaksCheck.IsChecked = _vz.ShowPeaks;
        if (RandomOnBeatCheck is { }) RandomOnBeatCheck.IsChecked = _vz.RandomPresetMode == RandomPresetMode.OnBeat;
        if (HotkeysCheck is { }) HotkeysCheck.IsChecked = _vz.EnableHotkeys;

        if (RandModeCombo is { })
            RandModeCombo.SelectedIndex = _vz.RandomPresetMode switch
            {
                RandomPresetMode.Off => 0,
                RandomPresetMode.OnBeat => 1,
                RandomPresetMode.Interval => 2,
                _ => 3
            };
        if (RandIntervalCombo is { })
            RandIntervalCombo.SelectedIndex = _vz.RandomPresetIntervalSeconds switch { <=15 => 0, <=30 => 1, _ => 2 };
        if (BeatsPerBarCombo is { }) BeatsPerBarCombo.SelectedIndex = _vz.BeatsPerBar == 3 ? 1 : 0;
        if (BarsPerStanzaCombo is { })
            BarsPerStanzaCombo.SelectedIndex = _vz.StanzaBars switch { <=8 => 0, <=16 => 1, <=32 => 2, _ => 3 };
        if (RandomWhenSilentCheck is { }) RandomWhenSilentCheck.IsChecked = _vz.RandomWhenSilent;
        if (RandCooldownUpDown is { }) RandCooldownUpDown.Value = _vz.RandomPresetCooldownMs;

        UpdateRandomPanels();

        // label updates on change
        if (GainSlider != null && GainLabel != null)
            GainSlider.PropertyChanged += (_, __) => GainLabel.Text = $"{GainSlider.Value:0.#} dB";
        if (SmoothSlider != null && SmoothLabel != null)
            SmoothSlider.PropertyChanged += (_, __) => SmoothLabel.Text = $"{SmoothSlider.Value:0}";
        if (GateSlider != null && GateLabel != null)
            GateSlider.PropertyChanged += (_, __) => GateLabel.Text = $"{GateSlider.Value:0}";
        if (BeatSlider != null && BeatLabel != null)
            BeatSlider.PropertyChanged += (_, __) => BeatLabel.Text = $"{BeatSlider.Value:0.00}×";
        if (BlendSlider != null && BlendLabel != null)
            BlendSlider.PropertyChanged += (_, __) => BlendLabel.Text = $"{BlendSlider.Value:0.00}";
        if (RandModeCombo != null) RandModeCombo.SelectionChanged += (_, __) => UpdateRandomPanels();
    }

    private void UpdateRandomPanels()
    {
        int mode = RandModeCombo?.SelectedIndex ?? 0;
        if (RandIntervalPanel is not null) RandIntervalPanel.IsVisible = mode == 2;
        if (RandStanzaPanel is not null) RandStanzaPanel.IsVisible = mode == 3;
    }

    private void SaveVisualizerSettings()
    {
        _vz.InputGainDb = (float)(GainSlider?.Value ?? 0);
        _vz.SmoothingMs = (float)(SmoothSlider?.Value ?? 0);
        _vz.NoiseGateDb = (float)(GateSlider?.Value ?? -60);
        _vz.BeatSensitivity = (float)(BeatSlider?.Value ?? 1.35f);
        _vz.FrameBlend = (float)(BlendSlider?.Value ?? 0.25f);
        _vz.FftSize = FftCombo?.SelectedIndex == 0 ? 1024 : 2048;
        _vz.SpectrumScale = ScaleCombo?.SelectedIndex switch
        {
            0 => SpectrumScale.Linear,
            1 => SpectrumScale.Log,
            _ => SpectrumScale.Sqrt
        };
        _vz.AutoGain = AutoGainCheck?.IsChecked ?? true;
        _vz.ShowPeaks = PeaksCheck?.IsChecked ?? true;
        _vz.EnableHotkeys = HotkeysCheck?.IsChecked ?? true;

        // random preset mode
        _vz.RandomPresetMode = RandModeCombo?.SelectedIndex switch
        {
            1 => RandomPresetMode.OnBeat,
            2 => RandomPresetMode.Interval,
            3 => RandomPresetMode.Stanza,
            _ => RandomPresetMode.Off
        };
        _vz.RandomPresetIntervalSeconds = RandIntervalCombo?.SelectedIndex switch
        {
            0 => 15,
            1 => 30,
            _ => 60
        };
        _vz.BeatsPerBar = BeatsPerBarCombo?.SelectedIndex == 1 ? 3 : 4;
        _vz.StanzaBars = BarsPerStanzaCombo?.SelectedIndex switch
        {
            0 => 8,
            1 => 16,
            2 => 32,
            _ => 64
        };
        _vz.RandomWhenSilent = RandomWhenSilentCheck?.IsChecked ?? false;
        _vz.RandomPresetCooldownMs = (int)(RandCooldownUpDown?.Value ?? 800);

        // legacy toggle from checkbox
        if (RandomOnBeatCheck?.IsChecked == true && _vz.RandomPresetMode == RandomPresetMode.Off)
            _vz.RandomPresetMode = RandomPresetMode.OnBeat;

        _vz.Save();
    }
}
