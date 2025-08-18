namespace PhoenixVisualizer.ViewModels;

public partial class SettingsWindowViewModel : ViewModelBase
{
    // Properties for the settings
    public string SelectedPlugin { get; set; } = "avs";
    public int SampleRate { get; set; } = 44100;
    public int BufferSize { get; set; } = 1024;
    public bool EnableVsync { get; set; } = true;
    public bool StartFullscreen { get; set; } = false;
    public bool AutoHideUI { get; set; } = true;
}
