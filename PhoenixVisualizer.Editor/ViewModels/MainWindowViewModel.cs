using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

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
    private string _selectedTab = "Properties";
    
    // Project properties
    private string _projectName = "Phoenix Visualizer Project";
    private string _projectDescription = "A new Phoenix Visualizer project";
    private string _projectAuthor = "User";
    private string _projectVersion = "1.0.0";
    private int _targetFPS = 60;
    private string _resolution = "1920x1080";
    private string _selectedItemName = "No item selected";

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

    public string SelectedTab
    {
        get => _selectedTab;
        set => SetProperty(ref _selectedTab, value);
    }

    public string ProjectName
    {
        get => _projectName;
        set => SetProperty(ref _projectName, value);
    }

    public string ProjectDescription
    {
        get => _projectDescription;
        set => SetProperty(ref _projectDescription, value);
    }

    public string ProjectAuthor
    {
        get => _projectAuthor;
        set => SetProperty(ref _projectAuthor, value);
    }

    public string ProjectVersion
    {
        get => _projectVersion;
        set => SetProperty(ref _projectVersion, value);
    }

    public int TargetFPS
    {
        get => _targetFPS;
        set => SetProperty(ref _targetFPS, value);
    }

    public string Resolution
    {
        get => _resolution;
        set => SetProperty(ref _resolution, value);
    }

    public string SelectedItemName
    {
        get => _selectedItemName;
        set => SetProperty(ref _selectedItemName, value);
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

    public ObservableCollection<string> AvailableResolutions { get; } = new()
    {
        "640x480", "800x600", "1024x768", "1280x720", "1920x1080", "2560x1440", "3840x2160"
    };

    // Commands
    public ICommand LoadPresetCommand { get; }
    public ICommand SavePresetCommand { get; }
    public ICommand PlayCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand AddToRecentCommand { get; }
    public ICommand SelectTabCommand { get; }
    public ICommand SwitchToPresetEditorCommand { get; }
    public ICommand SwitchToEffectsGraphEditorCommand { get; }

    public MainWindowViewModel()
    {
        LoadPresetCommand = new RelayCommand(LoadPreset);
        SavePresetCommand = new RelayCommand(SavePreset);
        PlayCommand = new RelayCommand(Play);
        PauseCommand = new RelayCommand(Pause);
        StopCommand = new RelayCommand(Stop);
        AddToRecentCommand = new RelayCommand<string>(AddToRecent);
        SelectTabCommand = new RelayCommand<string>(SelectTab);
        SwitchToPresetEditorCommand = new RelayCommand(SwitchToPresetEditor);
        SwitchToEffectsGraphEditorCommand = new RelayCommand(SwitchToEffectsGraphEditor);

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

    private void SelectTab(string? tabName)
    {
        if (!string.IsNullOrEmpty(tabName))
        {
            SelectedTab = tabName;
        }
    }

    private void SwitchToPresetEditor()
    {
        // TODO: Switch to preset editor tab
        System.Diagnostics.Debug.WriteLine("Switching to Preset Editor");
    }

    private void SwitchToEffectsGraphEditor()
    {
        // TODO: Switch to effects graph editor tab
        System.Diagnostics.Debug.WriteLine("Switching to Effects Graph Editor");
    }
}
