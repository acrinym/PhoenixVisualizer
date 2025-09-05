// PhoenixVisualizer/PhoenixVisualizer.App/App.axaml.cs
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using ReactiveUI;
using System;
using System.IO;
using System.Linq;

using PhoenixVisualizer.App.ViewModels;
using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Plugins.Avs;
using PhoenixVisualizer.Views;
using PhoenixVisualizer.Visuals;

using PhoenixVisualizer.App.Services;

namespace PhoenixVisualizer.App;

public partial class App : Application 
{ 
    private static SettingsService _settings = new SettingsService();
    public override void Initialize()
    {
        // Runtime XAML load (works even if the XAML generator isn't running)
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {

#if PHOENIX_ONLY
        try
        {
            var exeDir = AppContext.BaseDirectory;
            var legacyVis = Path.Combine(exeDir, "plugins", "vis");
            if (Directory.Exists(legacyVis) &&
                Directory.EnumerateFiles(legacyVis, "*.dll", SearchOption.AllDirectories).Any())
            {
                throw new InvalidOperationException("PHOENIX-ONLY assert: found legacy Winamp host DLLs under plugins\\vis. Remove them from the build.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            throw;
        }
#endif

        // Load and apply settings
        var settings = PhxEditorSettings.Load();
        settings.ApplyTheme();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // ============================================================================
            // 🎵 PHOENIX VISUALIZER - BUILT-IN VISUALIZERS
            // ============================================================================

            // --- Basic Visualizers ---
            PluginRegistry.Register("bars", "📊 Simple Bars", () => new BarsVisualizer());
            PluginRegistry.Register("spectrum", "📊 Spectrum Bars", () => new SpectrumVisualizer());
            PluginRegistry.Register("waveform", "📊 Waveform", () => new WaveformVisualizer());
            PluginRegistry.Register("pulse", "📊 Pulse Circle", () => new PulseVisualizer());
            PluginRegistry.Register("energy", "📊 Energy Ring", () => new EnergyVisualizer());
            PluginRegistry.Register("sanity", "🔧 Sanity Check", () => new SanityVisualizer());

            // --- AVS Engine ---
            PluginRegistry.Register("vis_avs", "🎵 AVS Runtime", () => new AvsVisualizerPlugin());
            PluginRegistry.Register("avs_effects_engine", "🌟 AVS Effects Engine", () => new AvsEffectsVisualizer(), "Full AVS effects engine with 48+ implemented effects", "1.0", "PhoenixVisualizer");

            // ============================================================================
            // 🎨 PHOENIX NATIVE VISUALIZERS
            // ============================================================================

            // --- Phoenix Classic Collection ---
            PluginRegistry.Register("phoenix_waterfall", "🔥 Phoenix Waterfall", () => new PhoenixWaterfallPlugin());
            PluginRegistry.Register("phoenix_radial_bars", "🔥 Phoenix Radial Bars", () => new PhoenixRadialBarsPlugin());
            PluginRegistry.Register("phoenix_xy_oscilloscope", "🔥 Phoenix XY Oscilloscope", () => new PhoenixXYOscilloscopePlugin());
            PluginRegistry.Register("phoenix_kaleidoscope", "🔥 Phoenix Kaleidoscope", () => new PhoenixKaleidoscopePlugin());
            PluginRegistry.Register("phoenix_particle_fountain", "🔥 Phoenix Particle Fountain", () => new PhoenixParticleFountainPlugin());
            PluginRegistry.Register("phoenix_circular_bars", "🎨 Phoenix Circular Bars", () => new PhoenixCircularBarsPlugin());
            PluginRegistry.Register("sacred_snowflakes", "❄️ Sacred Snowflakes", () => new SacredSnowflakeVisualizer(),
                "Symmetrical snowflakes from sacred geometry; reacts to bass/mid/treble and 7.83Hz base", "1.0", "Phoenix Team");

            // ============================================================================
            // 🎭 SUPERSCOPE VISUALIZERS
            // ============================================================================

            // --- Classic Superscopes ---
            PluginRegistry.Register("spiral_superscope", "🎭 Spiral Superscope", () => new SpiralSuperscope());
            PluginRegistry.Register("scope_dish_superscope", "🎭 3D Scope Dish", () => new ScopeDishSuperscope());
            PluginRegistry.Register("rotating_bow_superscope", "🎭 Rotating Bow", () => new RotatingBowSuperscope());
            PluginRegistry.Register("bouncing_scope_superscope", "🎭 Bouncing Scope", () => new BouncingScopeSuperscope());
            PluginRegistry.Register("spiral_graph_superscope", "🎭 Spiral Graph", () => new SpiralGraphSuperscope());
            PluginRegistry.Register("rainbow_merkaba_superscope", "🎭 Rainbow Merkaba", () => new RainbowMerkabaSuperscope());
            PluginRegistry.Register("rainbow_sphere_grid_superscope", "🎭 Rainbow Sphere Grid", () => new RainbowSphereGridSuperscope());

            // --- Interactive Superscopes ---
            PluginRegistry.Register("cat_face_superscope", "🎭 Cat Face", () => new CatFaceSuperscope());
            PluginRegistry.Register("pong_superscope", "🎭 Pong Game", () => new PongSuperscope());
            PluginRegistry.Register("butterfly_superscope", "🎭 Butterfly", () => new ButterflySuperscope());
            PluginRegistry.Register("cymatics_superscope", "🎭 Cymatics", () => new CymaticsSuperscope());

            // ============================================================================
            // 🪟 WINDOWS CLASSICS (95/98/2000)
            // ============================================================================

            // --- Iconic Windows 95/98 Screensavers ---
            PluginRegistry.Register("win95_mystify", "🎭 Win95 Mystify", () => new Win95Mystify(), "Classic Windows 95 Mystify screensaver with bouncing geometric shapes and trails", "1.0", "Microsoft");
            PluginRegistry.Register("win95_beziers", "✨ Win95 Béziers", () => new Win95Beziers(), "Classic Windows 95 Béziers screensaver with smooth curved lines", "1.0", "Microsoft");
            PluginRegistry.Register("win95_3d_flying_objects", "🪟 Win95 3D Flying Objects", () => new Win953DFlyingObjects(), "Classic Windows 95 3D Flying Objects screensaver with evolving geometric shapes", "1.0", "Microsoft");
            PluginRegistry.Register("win95_3d_twister", "🌀 Win95 3D Twister", () => new Win953DTwister(), "Classic Windows 95/98 inspired 3D Twister with audio-reactive tornado funnel", "1.0", "Microsoft");

            // --- Iconic Windows 2000 Screensavers ---
            PluginRegistry.Register("win2k_pipes", "🪟 Win2K 3D Pipes", () => new Win2KPipes(), "Classic Windows 2000 3D Pipes screensaver with branching pipes", "2.0", "Microsoft");
            PluginRegistry.Register("win2k_maze", "🪟 Win2K 3D Maze", () => new Win2KMaze(), "Classic Windows 2000 3D Maze screensaver with navigation", "2.0", "Microsoft");
            PluginRegistry.Register("win2k_3d_text", "🪟 Win2K 3D Text", () => new Win2K3DText(), "Classic Windows 2000 3D Text screensaver with rotating text", "2.0", "Microsoft");

            // ============================================================================
            // 🎭 WMP-INSPIRED VISUALIZERS (ORIGINAL PHOENIX CREATIONS)
            // ============================================================================

            // --- Windows Media Player Inspired Visualizers ---
            PluginRegistry.Register("phoenix_spectrum_pulse", "🔥 Phoenix Spectrum Pulse", () => new PhoenixSpectrumPulse(), "Enhanced spectrum analyzer with pulsing effects and audio-reactive colors", "1.0", "Phoenix Team");
            PluginRegistry.Register("phoenix_vortex", "🌪️ Phoenix Vortex", () => new PhoenixVortex(), "Audio-reactive volumetric vortex with dynamic tendrils and energy bursts", "1.0", "Phoenix Team");
            PluginRegistry.Register("phoenix_polygon_storm", "⚡ Phoenix Polygon Storm", () => new PhoenixPolygonStorm(), "Audio-reactive expanding polygons with dynamic scaling and rotation", "1.0", "Phoenix Team");
            PluginRegistry.Register("phoenix_grid_pulse", "🔳 Phoenix Grid Pulse", () => new PhoenixGridPulse(), "Dynamic grid structure with audio-reactive scaling and pulsing effects", "1.0", "Phoenix Team");
            PluginRegistry.Register("phoenix_wave_garden", "🌊 Phoenix Wave Garden", () => new PhoenixWaveGarden(), "Circular wave patterns with particle systems and fluid animations", "1.0", "Phoenix Team");

            // ============================================================================
            // 🏜️ 3D ARCHITECTURAL VISUALIZERS
            // ============================================================================

            // --- Ancient Architecture ---
            PluginRegistry.Register("pyramid_crumble", "🏜️ Pyramid Crumble", () => new PyramidCrumbleVisualizer(), "3D pyramid that crumbles to bass hits with physics-based falling blocks", "1.0", "Phoenix Team");

            // New visualizers
            PluginRegistry.Register("flappy_bird", "🐤 Flappy Beats", () => new FlappyBirdVisualizer(), "Audio-reactive Flappy Bird with multiple birds, collision detection, and particle effects", "1.0", "Phoenix Team");
            PluginRegistry.Register("minecart_rollercoaster", "🛤️ Phoenix Cart Ride", () => new MinecartRollercoasterVisualizer(), "Procedural track generation with audio-reactive physics and multiple carts", "1.0", "Phoenix Team");

            // ============================================================================
            // 🐱 MEME & CULTURE VISUALIZERS
            // ============================================================================

            // --- Internet Classics ---
            PluginRegistry.Register("nyan_cat", "🐱🌈 Nyan Cat", () => new NyanCatVisualizer(), "Classic rainbow cat that flies across the screen with audio-reactive rainbow trail", "1.0", "Phoenix Team");

            // ============================================================================
            // 🌌 XSCREENSAVER PORTS (FROM CYCLOSIDE)
            // ============================================================================

            // --- Fractal & Mathematical Visualizers ---
            PluginRegistry.Register("flame_fractal", "🌌 Fractal Flame", () => new FlameFractal(), "Iterated function system fractal flames with audio reactivity", "1.0", "XScreenSaver");
            PluginRegistry.Register("moebius_strip", "🌌 Möbius Strip", () => new MoebiusStrip(), "Parametric Möbius strip with audio-driven rotation", "1.0", "XScreenSaver");
            PluginRegistry.Register("ever_evolving_squares", "🌌 Evolving Squares", () => new EverEvolvingSquares(), "Recursive evolving squares pattern", "1.0", "XScreenSaver");

            // --- Audio Reactive Visualizers ---
            PluginRegistry.Register("raver_hoop", "🌌 Raver Hoop", () => new RaverHoop(), "Audio-reactive hoop with color cycling trails", "1.0", "XScreenSaver");
            PluginRegistry.Register("fiber_lamp", "🌌 Fiber Lamp", () => new FiberLamp(), "Fiber optic lamp effect", "1.0", "XScreenSaver");
            PluginRegistry.Register("lava_lamp", "🌌 Lava Lamp", () => new LavaLampVisualizer(), "Classic lava lamp simulation", "1.0", "XScreenSaver");
            PluginRegistry.Register("matrix_rain", "🌌 Matrix Rain", () => new MatrixRainVisualizer(), "Digital rain effect", "1.0", "XScreenSaver");

            // ============================================================================
            // 🔧 ADVANCED & DEBUG VISUALIZERS
            // ============================================================================

            // --- Advanced Effects ---
            PluginRegistry.Register("advanced_avs", "🌟 Advanced AVS", () => new AdvancedAvsPlugin(), "Advanced AVS effects with transitions, SuperScope, and awesome visuals", "1.0", "PhoenixVisualizer");
            PluginRegistry.Register("superscope_pro", "🎯 SuperScope Pro", () => new SuperScopePlugin(), "Professional SuperScope visualizations with multiple rendering modes", "1.0", "PhoenixVisualizer");

            // --- Analysis & Debug ---
            PluginRegistry.Register("spectrum_analyzer", "🎵 Spectrum Analyzer", () => new SpectrumAnalyzerPlugin());
            PluginRegistry.Register("vlc_audio_test", "🔍 VLC Audio Test Debug", () => new VlcAudioTestVisualizer(), "Debug visualizer for testing VLC audio data flow and buffer analysis", "1.0", "PhoenixVisualizer");

            // --- Fun & Experimental ---
            PluginRegistry.Register("fun.chicken.peck", "🐔 Chicken Field (Wireframe)", () => new ChickenVisualizer());

            // ============================================================================
            // 🔧 NODE-BASED VISUALIZERS
            // ============================================================================

            // --- Node-based Built-ins ---
            PluginRegistry.Register("node_bars_reactive", "📊 Bars Reactive (Node)", () => new NodeBarsReactive());
            PluginRegistry.Register("node_pulse_tunnel", "🌀 Pulse Tunnel (Node)", () => new NodePulseTunnel());
            PluginRegistry.Register("node_butterfly_field", "🦋 Butterfly Field (Node)", () => new NodeButterflyField());
            PluginRegistry.Register("node_rainbow_spectrum", "🌈 Rainbow Spectrum (Node)", () => new NodeRainbowSpectrum());
            PluginRegistry.Register("node_bass_bloom", "💥 Bass Bloom (Node)", () => new NodeBassBloom());
            PluginRegistry.Register("node_vector_grid", "🕸 Vector Grid (Node)", () => new NodeVectorGrid());
            PluginRegistry.Register("node_particles_beat", "✨ Particles Beat (Node)", () => new NodeParticlesBeat());
            PluginRegistry.Register("node_plasma_warp", "🌀 Plasma Warp (Node)", () => new NodePlasmaWarp());
            PluginRegistry.Register("node_text_echo", "🅿️ Text Echo (Node)", () => new NodeTextEcho());

            // --- Node-based Combos (Block 11) ---
            PluginRegistry.Register("node_wave_starfield", "✳️ Wave Starfield (Node)", () => new NodeWaveStarfield());
            PluginRegistry.Register("node_scope_ribbon", "〰️ Scope Ribbon (Node)", () => new NodeScopeRibbon());
            PluginRegistry.Register("node_beat_rings", "⭕ Beat Rings (Node)", () => new NodeBeatRings());
            PluginRegistry.Register("node_hexgrid_pulse", "⬡ HexGrid Pulse (Node)", () => new NodeHexGridPulse());
            PluginRegistry.Register("node_audio_flowfield", "🌬 Flow Field (Node)", () => new NodeAudioFlowField());
            PluginRegistry.Register("node_spectrum_nebula", "🌌 Spectrum Nebula (Node)", () => new NodeSpectrumNebula());

            // --- Node-based Combos (Block 12) ---
            PluginRegistry.Register("node_kaleido_beats", "✴️ Kaleido Beats (Node)", () => new NodeKaleidoBeats());
            PluginRegistry.Register("node_vectorfield_scope", "〽️ VectorField Scope (Node)", () => new NodeVectorFieldScope());
            PluginRegistry.Register("node_bass_kicker", "💣 Bass Kicker (Node)", () => new NodeBassKicker());
            PluginRegistry.Register("node_triangulate_scope", "△ Triangulate Scope (Node)", () => new NodeTriangulateScope());
            PluginRegistry.Register("node_beat_kaleido_tunnel", "🛸 Beat Kaleido Tunnel (Node)", () => new NodeBeatKaleidoTunnel());
            PluginRegistry.Register("node_geo_lattice", "🔷 Geo Lattice (Node)", () => new NodeGeoLattice());
            PluginRegistry.Register("node_bass_particles", "✨ Bass Particles (Node)", () => new NodeBassParticles());
            PluginRegistry.Register("node_scope_kaleido_glow", "🌟 Scope Kaleido Glow (Node)", () => new NodeScopeKaleidoGlow());
            PluginRegistry.Register("node_pixelsort_plasma", "🧪 PixelSort Plasma (Node)", () => new NodePixelSortPlasma());
            PluginRegistry.Register("node_text_beat_echo", "🅿️ Text Beat Echo (Node)", () => new NodeTextBeatEcho());

            // ============================================================================
            // 🌌 XSS (X-Style Shader) VISUALIZERS
            // ============================================================================

            // --- Core XSS Visualizers ---
            PluginRegistry.Register("node_xs_fireworks", "🎆 XS Fireworks (Node)", () => new NodeXsFireworks());
            PluginRegistry.Register("node_xs_plasma", "🌀 XS Plasma (Node)", () => new NodeXsPlasma());
            PluginRegistry.Register("node_xs_vortex", "🌪️ XS Vortex (Node)", () => new NodeXsVortex());

            // --- XScreenSaver-Inspired XSS Visualizers ---
            PluginRegistry.Register("node_xs_lcdscrub", "🖥️ XS LCD Scrub (Node)", () => new NodeXsLcdScrub());
            PluginRegistry.Register("node_xs_lightning", "⚡ XS Lightning (Node)", () => new NodeXsLightning());
            PluginRegistry.Register("node_xs_lisa", "📐 XS Lisa (Node)", () => new NodeXsLisa());
            PluginRegistry.Register("node_xs_penrose", "🔷 XS Penrose (Node)", () => new NodeXsPenrose());
            PluginRegistry.Register("node_xs_rorschach", "🦋 XS Rorschach (Node)", () => new NodeXsRorschach());
            PluginRegistry.Register("node_xs_rotor", "🌀 XS Rotor (Node)", () => new NodeXsRotor());

            // Avoid duplicate validations from Avalonia + CommunityToolkit
            DisableAvaloniaDataAnnotationValidation();

            // Boot main window
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
            _settings.Load();
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
