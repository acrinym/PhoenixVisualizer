using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;              // <-- manual XAML load
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Plugins.Avs;
using PhoenixVisualizer.Rendering;

namespace PhoenixVisualizer.Views;

public partial class MainWindow : Window
{
    // Strongly-typed reference to the render surface in XAML
    private RenderSurface? RenderSurfaceControl => this.FindControl<RenderSurface>("RenderHost");

    private static readonly string[] AudioPatterns = { "*.mp3", "*.wav", "*.flac", "*.ogg" };

    public MainWindow()
    {
        // Manually load XAML so we don't depend on generated InitializeComponent()
        AvaloniaXamlLoader.Load(this);

        // Wire runtime UI updates if the render surface is present
        if (RenderSurfaceControl is not null)
        {
            // FPS
            RenderSurfaceControl.FpsChanged += fps =>
            {
                var lbl = this.FindControl<TextBlock>("LblFps");
                if (lbl is not null)
                {
                    Dispatcher.UIThread.Post(
                        () => lbl.Text = $"FPS: {fps:F1}",
                        DispatcherPriority.Background
                    );
                }
            };

            // BPM
            RenderSurfaceControl.BpmChanged += bpm =>
            {
                var lbl = this.FindControl<TextBlock>("LblBpm");
                if (lbl is not null)
                {
                    Dispatcher.UIThread.Post(
                        () => lbl.Text = $"BPM: {bpm:F1}",
                        DispatcherPriority.Background
                    );
                }
            };

            // Position (current / total)
            RenderSurfaceControl.PositionChanged += (pos, len) =>
            {
                var lbl = this.FindControl<TextBlock>("LblTime");
                if (lbl is not null)
                {
                    string cur = TimeSpan.FromSeconds(pos).ToString(@"mm\\:ss");
                    string tot = TimeSpan.FromSeconds(len).ToString(@"mm\\:ss");
                    Dispatcher.UIThread.Post(
                        () => lbl.Text = $"{cur} / {tot}",
                        DispatcherPriority.Background
                    );
                }
            };

            // Plugin ComboBox: populate from registry, fallback to AVS
            var combo = this.FindControl<ComboBox>("CmbPlugin");
            if (combo is not null)
            {
                var plugins = PluginRegistry.Available?.ToList()
                              ?? new List<(string id, string displayName)>();

                if (plugins.Count > 0)
                {
                    combo.ItemsSource = plugins.Select(p => p.displayName).ToList();
                    combo.SelectedIndex = 0;

                    // Set initial plugin
                    var first = PluginRegistry.Create(plugins[0].id);
                    RenderSurfaceControl.SetPlugin(first ?? new AvsVisualizerPlugin());

                    combo.SelectionChanged += (_, _) =>
                    {
                        if (RenderSurfaceControl is null) return;
                        int idx = combo.SelectedIndex;
                        if (idx >= 0 && idx < plugins.Count)
                        {
                            var plug = PluginRegistry.Create(plugins[idx].id)
                                       ?? new AvsVisualizerPlugin();
                            RenderSurfaceControl.SetPlugin(plug);
                        }
                    };
                }
                else
                {
                    // Fallback: no registry entries — default to AVS and disable the combo
                    combo.ItemsSource = new[] { "AVS (built-in)" };
                    combo.SelectedIndex = 0;
                    RenderSurfaceControl.SetPlugin(new AvsVisualizerPlugin());
                    combo.IsEnabled = false;
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

    private void OnPlayClick(object? sender, RoutedEventArgs e)  => RenderSurfaceControl?.Play();
    private void OnPauseClick(object? sender, RoutedEventArgs e) => RenderSurfaceControl?.Pause();
    private void OnStopClick(object? sender, RoutedEventArgs e)  => RenderSurfaceControl?.Stop();

    private async void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        var dlg = new SettingsWindow();
        await dlg.ShowDialog(this);
    }

    private void OnLoadPreset(object? sender, RoutedEventArgs e)
    {
        var tb = this.FindControl<TextBox>("TxtPreset");
        if (tb is null || RenderSurfaceControl is null) return;

        // Prefer registry AVS; fallback to built-in
        var plugin = PluginRegistry.Create("vis_avs") as AvsVisualizerPlugin
                     ?? new AvsVisualizerPlugin();

        RenderSurfaceControl.SetPlugin(plugin);
        plugin.LoadPreset(tb.Text ?? string.Empty);
    }
}