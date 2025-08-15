using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using PhoenixVisualizer.ViewModels;
using PhoenixVisualizer.Views;
using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Visuals;
using PhoenixVisualizer.Plugins.Avs;

namespace PhoenixVisualizer;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Register bundled visualizer plugins 🎉
            PluginRegistry.Register("bars", "Simple Bars", () => new BarsVisualizer());
            PluginRegistry.Register("spectrum", "Spectrum Bars", () => new SpectrumVisualizer());
            PluginRegistry.Register("waveform", "Waveform", () => new WaveformVisualizer());
            PluginRegistry.Register("pulse", "Pulse Circle", () => new PulseVisualizer());
            PluginRegistry.Register("energy", "Energy Ring", () => new EnergyVisualizer());
            PluginRegistry.Register("vis_avs", "AVS Runtime", () => new AvsVisualizerPlugin());

            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}