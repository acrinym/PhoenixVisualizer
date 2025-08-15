using Avalonia.Controls;
using Avalonia.Interactivity;
using PhoenixVisualizer.Rendering;
using System;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using Avalonia.Media;
using PhoenixVisualizer.Plugins.Avs;
using Avalonia.Threading;
using PhoenixVisualizer.PluginHost;
using System.Linq;
using Avalonia.Layout;

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
                if (lbl is not null)
                {
                    // ensure UI-thread update
                    Dispatcher.UIThread.Post(() => lbl.Text = $"FPS: {fps:F1}", DispatcherPriority.Background);
                }
            };
            RenderSurfaceControl.BpmChanged += bpm =>
            {
                var lbl = this.FindControl<TextBlock>("LblBpm");
                if (lbl is not null)
                {
                    Dispatcher.UIThread.Post(() => lbl.Text = $"BPM: {bpm:F1}", DispatcherPriority.Background);
                }
            };
            RenderSurfaceControl.PositionChanged += (pos, len) =>
            {
                var lbl = this.FindControl<TextBlock>("LblTime");
                if (lbl is not null)
                {
                    string cur = TimeSpan.FromSeconds(pos).ToString(@"mm\:ss");
                    string tot = TimeSpan.FromSeconds(len).ToString(@"mm\:ss");
                    Dispatcher.UIThread.Post(() => lbl.Text = $"{cur} / {tot}", DispatcherPriority.Background);
                }
            };

            var combo = this.FindControl<ComboBox>("CmbPlugin");
            if (combo is not null)
            {
                var plugins = PluginRegistry.Available.ToList();
                combo.ItemsSource = plugins.Select(p => p.displayName).ToList();
                combo.SelectedIndex = 0;
                combo.SelectionChanged += (_, _) =>
                {
                    if (RenderSurfaceControl is null) return;
                    int idx = combo.SelectedIndex;
                    if (idx >= 0 && idx < plugins.Count)
                    {
                        var plug = PluginRegistry.Create(plugins[idx].id);
                        if (plug is not null)
                        {
                            RenderSurfaceControl.SetPlugin(plug);
                        }
                    }
                };

                if (plugins.Count > 0)
                {
                    var plug = PluginRegistry.Create(plugins[0].id);
                    if (plug is not null)
                    {
                        RenderSurfaceControl.SetPlugin(plug);
                    }
                }
            }
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

    private async void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        var dlg = new SettingsWindow();
        await dlg.ShowDialog(this);
    }

    private void OnLoadPreset(object? sender, RoutedEventArgs e)
    {
        var tb = this.FindControl<TextBox>("TxtPreset");
        if (tb is null || RenderSurfaceControl is null) return;
        var plugin = PluginRegistry.Create("vis_avs") as AvsVisualizerPlugin ?? new AvsVisualizerPlugin();
        RenderSurfaceControl.SetPlugin(plugin);
        plugin.LoadPreset(tb.Text ?? string.Empty);
    }
}