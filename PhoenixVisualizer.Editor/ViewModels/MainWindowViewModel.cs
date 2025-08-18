using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Input;
using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Plugins.Avs;

namespace PhoenixVisualizer.Editor.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private int _points = 256;
    private string _mode = "Line";
    private string _source = "FFT";
    private string _presetCode = "points=256;mode=line;source=fft";
    private bool _isPlaying = false;
    private double _currentTime = 0.0;
    private double _totalTime = 0.0;

    public int Points
    {
        get => _points;
        set => SetProperty(ref _points, value);
    }

    public string Mode
    {
        get => _mode;
        set => SetProperty(ref _mode, value);
    }

    public string Source
    {
        get => _source;
        set => SetProperty(ref _source, value);
    }

    public string PresetCode
    {
        get => _presetCode;
        set => SetProperty(ref _presetCode, value);
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set => SetProperty(ref _isPlaying, value);
    }

    public double CurrentTime
    {
        get => _currentTime;
        set => SetProperty(ref _currentTime, value);
    }

    public double TotalTime
    {
        get => _totalTime;
        set => SetProperty(ref _totalTime, value);
    }

    public ObservableCollection<string> RecentPresets { get; } = new()
    {
        "points=256;mode=line;source=fft",
        "points=128;mode=bars;source=fft",
        "points=512;mode=line;source=sin",
        "points=64;mode=circle;source=fft",
        "points=1024;mode=wave;source=fft"
    };

    public ObservableCollection<string> AvailableModes { get; } = new()
    {
        "Line", "Bars", "Circle", "Wave", "Pulse", "Spiral"
    };

    public ObservableCollection<string> AvailableSources { get; } = new()
    {
        "FFT", "Sine", "Square", "Triangle", "Sawtooth", "Noise"
    };

    // Commands
    public ICommand LoadPresetCommand { get; }
    public ICommand SavePresetCommand { get; }
    public ICommand PlayCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand AddToRecentCommand { get; }

    public MainWindowViewModel()
    {
        LoadPresetCommand = new RelayCommand(LoadPreset);
        SavePresetCommand = new RelayCommand(SavePreset);
        PlayCommand = new RelayCommand(Play);
        PauseCommand = new RelayCommand(Pause);
        StopCommand = new RelayCommand(Stop);
        AddToRecentCommand = new RelayCommand<string>(AddToRecent);

        // Watch for property changes to update preset code
        PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Points) || e.PropertyName == nameof(Mode) || e.PropertyName == nameof(Source))
        {
            UpdatePresetCode();
        }
    }

    private void UpdatePresetCode()
    {
        PresetCode = $"points={Points};mode={Mode.ToLower()};source={Source.ToLower()}";
    }

    private void LoadPreset()
    {
        // Parse preset code and update properties
        try
        {
            var parts = PresetCode.Split(';');
            foreach (var part in parts)
            {
                var kvp = part.Split('=');
                if (kvp.Length == 2)
                {
                    var key = kvp[0].Trim().ToLower();
                    var value = kvp[1].Trim();

                    switch (key)
                    {
                        case "points":
                            if (int.TryParse(value, out var points))
                                Points = Math.Clamp(points, 16, 1024);
                            break;
                        case "mode":
                            Mode = value;
                            break;
                        case "source":
                            Source = value;
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to parse preset: {ex.Message}");
        }
    }

    private void SavePreset()
    {
        if (!RecentPresets.Contains(PresetCode))
        {
            RecentPresets.Insert(0, PresetCode);
            if (RecentPresets.Count > 10)
                RecentPresets.RemoveAt(RecentPresets.Count - 1);
        }
    }

    private void Play()
    {
        IsPlaying = true;
        // TODO: Integrate with audio service
    }

    private void Pause()
    {
        IsPlaying = false;
        // TODO: Integrate with audio service
    }

    private void Stop()
    {
        IsPlaying = false;
        CurrentTime = 0.0;
        // TODO: Integrate with audio service
    }

    private void AddToRecent(string? preset)
    {
        if (!string.IsNullOrEmpty(preset) && !RecentPresets.Contains(preset))
        {
            RecentPresets.Insert(0, preset);
            if (RecentPresets.Count > 10)
                RecentPresets.RemoveAt(RecentPresets.Count - 1);
        }
    }
}
