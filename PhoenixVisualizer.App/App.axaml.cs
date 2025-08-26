// PhoenixVisualizer/PhoenixVisualizer.App/App.axaml.cs
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;

using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Plugins.Avs;
using PhoenixVisualizer.Views;
using PhoenixVisualizer.Visuals;

namespace PhoenixVisualizer.App;

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
            PluginRegistry.Register("spectrum_analyzer", "🎵 Spectrum Analyzer", () => new SpectrumAnalyzerPlugin());
            
            // --- Register Phoenix Waterfall (Classic Winamp DNA) ---
            PluginRegistry.Register("phoenix_waterfall", "🔥 Phoenix Waterfall", () => new PhoenixWaterfallPlugin());
            
            // --- Register Classic Winamp DNA Visuals ---
            PluginRegistry.Register("phoenix_radial_bars", "🔥 Phoenix Radial Bars", () => new PhoenixRadialBarsPlugin());
            PluginRegistry.Register("phoenix_xy_oscilloscope", "🔥 Phoenix XY Oscilloscope", () => new PhoenixXYOscilloscopePlugin());
            PluginRegistry.Register("phoenix_kaleidoscope", "🔥 Phoenix Kaleidoscope", () => new PhoenixKaleidoscopePlugin());
            PluginRegistry.Register("phoenix_particle_fountain", "🔥 Phoenix Particle Fountain", () => new PhoenixParticleFountainPlugin());
            
            // --- Register Fun Visuals ---
            PluginRegistry.Register("fun.chicken.peck", "🐔 Chicken Field (Wireframe)", () => new ChickenVisualizer());
            PluginRegistry.Register("phoenix_circular_bars", "🎨 Phoenix Circular Bars", () => new PhoenixCircularBarsPlugin());
            
            // --- Register all superscopes ---
            PluginRegistry.Register("spiral_superscope", "🎭 Spiral Superscope", () => new SpiralSuperscope());
            PluginRegistry.Register("scope_dish_superscope", "🎭 3D Scope Dish", () => new ScopeDishSuperscope());
            PluginRegistry.Register("rotating_bow_superscope", "🎭 Rotating Bow", () => new RotatingBowSuperscope());
            PluginRegistry.Register("bouncing_scope_superscope", "🎭 Bouncing Scope", () => new BouncingScopeSuperscope());
            PluginRegistry.Register("spiral_graph_superscope", "🎭 Spiral Graph", () => new SpiralGraphSuperscope());
            PluginRegistry.Register("rainbow_merkaba_superscope", "🎭 Rainbow Merkaba", () => new RainbowMerkabaSuperscope());
            PluginRegistry.Register("cat_face_superscope", "🎭 Cat Face", () => new CatFaceSuperscope());
            PluginRegistry.Register("cymatics_superscope", "🎭 Cymatics", () => new CymaticsSuperscope());
            PluginRegistry.Register("pong_superscope", "🎭 Pong Game", () => new PongSuperscope());
            PluginRegistry.Register("butterfly_superscope", "🎭 Butterfly", () => new ButterflySuperscope());
            PluginRegistry.Register("rainbow_sphere_grid_superscope", "🎭 Rainbow Sphere Grid", () => new RainbowSphereGridSuperscope());

            // --- Register Advanced AVS Effects ---
            PluginRegistry.Register("advanced_avs", "🌟 Advanced AVS", () => new AdvancedAvsPlugin(), "Advanced AVS effects with transitions, SuperScope, and awesome visuals", "1.0", "PhoenixVisualizer");
            PluginRegistry.Register("superscope_pro", "🎯 SuperScope Pro", () => new SuperScopePlugin(), "Professional SuperScope visualizations with multiple rendering modes", "1.0", "PhoenixVisualizer");
            
            // --- Register VLC Audio Test Visualizer ---
            PluginRegistry.Register("vlc_audio_test", "🔍 VLC Audio Test Debug", () => new VlcAudioTestVisualizer(), "Debug visualizer for testing VLC audio data flow and buffer analysis", "1.0", "PhoenixVisualizer");

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
