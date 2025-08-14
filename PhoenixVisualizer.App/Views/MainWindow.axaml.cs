using Avalonia.Controls;
using Avalonia.Interactivity;
using PhoenixVisualizer.Rendering;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace PhoenixVisualizer.Views;

public partial class MainWindow : Window
{
    private Rendering.RenderSurface? RenderSurfaceControl => this.FindControl<Control>("RenderHost") as Rendering.RenderSurface;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        if (RenderSurfaceControl is null) return;
        var files = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Audio File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Audio")
                {
                    Patterns = new[] { "*.mp3", "*.wav", "*.flac", "*.ogg" }
                }
            }
        });
        var file = files.Count > 0 ? files[0] : null;
        if (file is null) return;
        await Task.Run(() => RenderSurfaceControl.Open(file.Path.LocalPath));
    }

    private void OnPlayClick(object? sender, RoutedEventArgs e)
    {
        RenderSurfaceControl?.Play();
    }

    private void OnPauseClick(object? sender, RoutedEventArgs e)
    {
        RenderSurfaceControl?.Pause();
    }

    private void OnStopClick(object? sender, RoutedEventArgs e)
    {
        RenderSurfaceControl?.Stop();
    }
}