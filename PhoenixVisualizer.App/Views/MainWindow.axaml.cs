using System;
using System.Collections.Generic;
using System.IO;
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
using EditorWindow = PhoenixVisualizer.Editor.Views.MainWindow;

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
                    // Display current and total time as mm:ss 👇
                    // NOTE: Use a single escaped colon; the previous double escape
                    // threw a FormatException on runtime. 😅
                    string cur = TimeSpan.FromSeconds(pos).ToString(@"mm\:ss");
                    string tot = TimeSpan.FromSeconds(len).ToString(@"mm\:ss");
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

    private void InitializePlugin()
    {
        // Set default plugin after controls are ready
        // TODO: Load from settings/config file
        if (RenderSurfaceControl is not null)
        {
            var plugin = new AvsVisualizerPlugin(); // Default to AVS Engine
            RenderSurfaceControl.SetPlugin(plugin);
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

    private void OnPlayClick(object? sender, RoutedEventArgs e) => RenderSurfaceControl?.Play();
    private void OnPauseClick(object? sender, RoutedEventArgs e) => RenderSurfaceControl?.Pause();
    private void OnStopClick(object? sender, RoutedEventArgs e) => RenderSurfaceControl?.Stop();

    private async void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new SettingsWindow();
            await dlg.ShowDialog(this);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Settings dialog failed: {ex}");
        }
    }

    private async void OnEditorClick(object? sender, RoutedEventArgs e)
    {
        var editor = new EditorWindow();
        await editor.ShowDialog(this);
    }

    private void OnLoadPreset(object? sender, RoutedEventArgs e)
    {
        var tb = this.FindControl<TextBox>("TxtPreset");
        if (tb is null || RenderSurfaceControl is null) return;

        var plug = PluginRegistry.Create("vis_avs") as IAvsHostPlugin;
        if (plug is null) return;

        RenderSurfaceControl.SetPlugin(plug);
        plug.LoadPreset(tb.Text ?? string.Empty);
    }

    private async void OnImportPreset(object? sender, RoutedEventArgs e)
    {
        if (RenderSurfaceControl is null) return;

        var files = await this.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Import AVS Preset",
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new("AVS Preset") { Patterns = new[] { "*.avs", "*.txt" } }
                }
            });

        var file = files.Count > 0 ? files[0] : null;
        if (file is null) return;

        var plug = PluginRegistry.Create("vis_avs") as IAvsHostPlugin;
        if (plug is null) return;

        using var stream = await file.OpenReadAsync();
        using var reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync();

        RenderSurfaceControl.SetPlugin(plug);
        plug.LoadPreset(text);
    }
}