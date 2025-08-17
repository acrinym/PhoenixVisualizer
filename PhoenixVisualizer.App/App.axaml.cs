// PhoenixVisualizer/PhoenixVisualizer.App/App.axaml.cs
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Plugins.Avs;
using PhoenixVisualizer.ViewModels;
using PhoenixVisualizer.Views;
using PhoenixVisualizer.Visuals;

namespace PhoenixVisualizer;

public partial class App : Application
{
    public override void Initialize()
    {
        // Runtime XAML load (works even if the XAML generator isn't running)
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // --- Register bundled visualizer plugins BEFORE creating MainWindow ---
            // If any of these classes aren't present in this branch, comment that line out.
            PluginRegistry.Register("bars", "Simple Bars", () => new BarsVisualizer());
            PluginRegistry.Register("spectrum", "Spectrum Bars", () => new SpectrumVisualizer());
            PluginRegistry.Register("waveform", "Waveform", () => new WaveformVisualizer());
            PluginRegistry.Register("pulse", "Pulse Circle", () => new PulseVisualizer());
            PluginRegistry.Register("energy", "Energy Ring", () => new EnergyVisualizer());
            PluginRegistry.Register("sanity", "Sanity Check", () => new SanityVisualizer());
            PluginRegistry.Register("vis_avs", "AVS Runtime", () => new AvsVisualizerPlugin());

            // Avoid duplicate validations from Avalonia + CommunityToolkit
            DisableAvaloniaDataAnnotationValidation();

            // Boot main window
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var toRemove = BindingPlugins.DataValidators
            .OfType<DataAnnotationsValidationPlugin>()
            .ToArray();

        foreach (var plugin in toRemove)
            BindingPlugins.DataValidators.Remove(plugin);
    }
}
