using Avalonia.Controls;
using Avalonia.Interactivity;
using PhoenixVisualizer.Rendering;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using Avalonia.Media;
using PhoenixVisualizer.Plugins.Avs;
using PhoenixVisualizer.Plugins.Ape.Phoenix;
using PhoenixVisualizer.PluginHost;
using Avalonia.Threading;

namespace PhoenixVisualizer.Views;

public partial class MainWindow : Window
{
    private Rendering.RenderSurface? RenderSurfaceControl => this.FindControl<Control>("RenderHost") as Rendering.RenderSurface;
    private static readonly string[] AudioPatterns = { "*.mp3", "*.wav", "*.flac", "*.ogg" };

    public MainWindow()
    {
        InitializeComponent();
        
        // Defer plugin initialization until after the control tree is fully built
        Dispatcher.UIThread.Post(() => InitializePlugin(), DispatcherPriority.Loaded);
        
        if (RenderSurfaceControl is not null)
        {
            RenderSurfaceControl.FpsChanged += fps =>
            {
                var lbl = this.FindControl<TextBlock>("LblFps");
                if (lbl is not null)
                {
                    // ensure UI-thread update
                    Dispatcher.UIThread.Post(() => lbl.Text = $"FPS: {fps:F1}", Avalonia.Threading.DispatcherPriority.Background);
                }
            };
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
        // Open audio file on UI thread since RenderSurfaceControl is a UI control
        RenderSurfaceControl.Open(file.Path.LocalPath);
    }

    private void OnPlayClick(object? sender, RoutedEventArgs e)
    {
        if (RenderSurfaceControl is null) return;
        
        RenderSurfaceControl.Play();
        
        // Show feedback
        var lbl = this.FindControl<TextBlock>("LblFps");
        if (lbl is not null)
        {
            lbl.Text = "Playing...";
            // Clear after 2 seconds
            Dispatcher.UIThread.Post(async () => 
            {
                await Task.Delay(2000);
                lbl.Text = "";
            }, DispatcherPriority.Background);
        }
    }

    private void OnPauseClick(object? sender, RoutedEventArgs e)
    {
        RenderSurfaceControl?.Pause();
    }

    private void OnStopClick(object? sender, RoutedEventArgs e)
    {
        RenderSurfaceControl?.Stop();
    }

    private void OnLoadPreset(object? sender, RoutedEventArgs e)
    {
        var tb = this.FindControl<TextBox>("TxtPreset");
        if (tb is null) return;
        
        // Load preset into the active AVS Engine via RenderSurface
        if (RenderSurfaceControl is not null)
        {
            // Get the current plugin and load preset if it's an AVS plugin
            var currentPlugin = RenderSurfaceControl.GetCurrentPlugin();
            if (currentPlugin is IAvsHostPlugin avsPlugin)
            {
                avsPlugin.LoadPreset(tb.Text ?? string.Empty);
                
                // Show feedback
                var lbl = this.FindControl<TextBlock>("LblFps");
                if (lbl is not null)
                {
                    lbl.Text = "Preset loaded!";
                    Dispatcher.UIThread.Post(async () => 
                    {
                        await Task.Delay(2000);
                        lbl.Text = "";
                    }, DispatcherPriority.Background);
                }
            }
        }
    }
    
    private async void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        // Open settings dialog
        var settingsWindow = new SettingsWindow();
        var result = await settingsWindow.ShowDialog<string>(this);
        
        if (result != null)
        {
            // Apply the selected plugin if it changed
            if (settingsWindow.SelectedPlugin != GetCurrentPluginType())
            {
                ApplyPluginFromSettings(settingsWindow.SelectedPlugin);
                
                // Show feedback
                var lbl = this.FindControl<TextBlock>("LblFps");
                if (lbl is not null)
                {
                    lbl.Text = $"Plugin changed to: {GetPluginDisplayName(settingsWindow.SelectedPlugin)}";
                    Dispatcher.UIThread.Post(async () => 
                    {
                        await Task.Delay(3000);
                        lbl.Text = "";
                    }, DispatcherPriority.Background);
                }
            }
        }
    }
    
    private string GetCurrentPluginType()
    {
        // Determine current plugin type from RenderSurface
        if (RenderSurfaceControl is not null)
        {
            // This is a simplified check - in a real app you'd store the plugin type
            return "avs"; // Default for now
        }
        return "avs";
    }
    
    private void ApplyPluginFromSettings(string pluginType)
    {
        if (RenderSurfaceControl is null) return;
        
        IVisualizerPlugin plugin = pluginType switch
        {
            "phoenix" => new PhoenixPlugin(),
            _ => new AvsVisualizerPlugin()
        };
        
        RenderSurfaceControl.SetPlugin(plugin);
    }
    
    private string GetPluginDisplayName(string pluginType)
    {
        return pluginType switch
        {
            "phoenix" => "Phoenix Visualizer",
            _ => "AVS Engine"
        };
    }
}