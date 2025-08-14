using Avalonia.Controls;
using Avalonia.Interactivity;
using PhoenixVisualizer.Rendering;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using Avalonia.Media;

namespace PhoenixVisualizer.Views;

public partial class MainWindow : Window
{
    private Rendering.RenderSurface? RenderSurfaceControl => this.FindControl<Control>("RenderHost") as Rendering.RenderSurface;
    private static readonly string[] AudioPatterns = { "*.mp3", "*.wav", "*.flac", "*.ogg" };

    public MainWindow()
    {
        InitializeComponent();
        if (RenderSurfaceControl is not null)
        {
            RenderSurfaceControl.FpsChanged += fps =>
            {
                var lbl = this.FindControl<TextBlock>("LblFps");
                if (lbl is not null) lbl.Text = $"FPS: {fps:F1}";
            };
        }
    }

    private async void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        if (RenderSurfaceControl is null) return;
        var files = await this.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Open Audio File",
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new FilePickerFileType("Audio") { Patterns = AudioPatterns }
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