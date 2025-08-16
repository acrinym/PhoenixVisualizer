using Avalonia.Controls;
using Avalonia.Interactivity;
using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Plugins.Avs;
using PhoenixVisualizer.Editor.Rendering;

namespace PhoenixVisualizer.Editor.Views;

public partial class MainWindow : Window
{
    private RenderSurface? RenderSurfaceControl => this.FindControl<RenderSurface>("RenderHost");

    public MainWindow()
    {
        InitializeComponent();
        // spin up a default AVS plugin so the canvas isn't blank
        RenderSurfaceControl?.SetPlugin(new AvsVisualizerPlugin());
    }

    private void OnLoadPreset(object? sender, RoutedEventArgs e)
    {
        var tb = this.FindControl<TextBox>("PresetInput");
        if (tb is null || RenderSurfaceControl is null) return;

        var plug = PluginRegistry.Create("vis_avs") as IAvsHostPlugin ?? new AvsVisualizerPlugin();
        RenderSurfaceControl.SetPlugin(plug);
        plug.LoadPreset(tb.Text ?? string.Empty);
    }
}