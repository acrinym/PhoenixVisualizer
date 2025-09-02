using PhoenixVisualizer.Core.Config;
using PhoenixVisualizer.Core.Services;
using PhoenixVisualizer.Core.Avs;
using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Audio;
using System.Linq;
using PhoenixVisualizer.Plugins.Avs;
using PhoenixVisualizer.App.Rendering;
using PhoenixVisualizer.App.Controls;
using PhoenixVisualizer.Core.Diagnostics;
using PhoenixVisualizer.App.Utils;
using PhoenixVisualizer.App.Services;
using PhoenixVisualizer.App.Views;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;
using Avalonia.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace PhoenixVisualizer.Views;

public partial class MainWindow : Window
{
    private bool _handlersWired;
    private enum VisualMode { BuiltIn }
    private VisualMode _visualMode = VisualMode.BuiltIn;
    
    // Plugin mode tracking
    private enum PluginMode { BuiltIn }
    private PluginMode _currentPluginMode = PluginMode.BuiltIn;
    

    
    // Grab the render surface once on the UI thread so background tasks don't try
    // to traverse the visual tree later (which would throw 🙅‍♂️)
    private readonly RenderSurface? _renderSurface;
    private RenderSurface? RenderSurfaceControl => _renderSurface;

    private static readonly string[] AudioPatterns = { 
        "*.mp3", "*.wav", "*.flac", "*.ogg", "*.m4a", "*.aac", "*.wma", "*.ape", "*.mpc", "*.tta", "*.alac" 
    };

    // AVS engine overlay
    private readonly AvsEditorBridge _avsBridge = new();
    private readonly AvsAudioProvider _avsAudio = new();
    private readonly Canvas? _avsCanvas;
    private readonly AvsHostControl? _avsWin32Host;
    private readonly Control? _avsWin32HostControl;



    public MainWindow()
    {
        // Manually load XAML so we don't depend on generated InitializeComponent()
        AvaloniaXamlLoader.Load(this);
        this.KeyDown += OnKeyDown_OpenParameterEditor;
        _renderSurface = this.FindControl<RenderSurface>("RenderHost");
        var avsCanvasHost = this.FindControl<Grid>("AvsCanvasHost");
        _avsCanvas = avsCanvasHost?.FindControl<Canvas>("AvsCanvas");
        _avsWin32Host = this.FindControl<AvsHostControl>("AvsWin32Host");
        _avsWin32HostControl = this.FindControl<Control>("AvsWin32Host");
        

        
        Presets.Initialize(_renderSurface);

        // ✅ Populate built-in plugins and select one immediately
        var cmb = this.FindControl<ComboBox>("CmbPlugin");
        if (cmb != null)
        {
            var items = PluginRegistry.AvailablePlugins?.ToList() ?? new List<PluginMetadata>();
            if (items.Count > 0)
            {
                cmb.ItemsSource = items;
                cmb.SelectionChanged += OnPluginSelectionChanged;
                
                // Initialize based on engine setting
                InitializeVisualizerFromSettings();
            }
        }

        // ✅ Wire plugin switcher button if present
        var btnSwitcher = this.FindControl<Button>("BtnPluginSwitcher");
        if (btnSwitcher != null) btnSwitcher.Click += OnPluginSwitcherClick;

        // Initialize AVS overlay renderer
        try
        {
            if (_avsCanvas is not null)
            {
                var renderer = new PhoenixVisualizer.App.Rendering.AvaloniaAvsRenderer();
                renderer.SetRenderCanvas(_avsCanvas);
                _avsBridge.SetRenderer(renderer);
                _avsBridge.SetAudioProvider(_avsAudio);
            }
        }
        catch
        {
            // AVS overlay initialization failed silently
        }

        // Set up drag and drop for preset files
        if (avsCanvasHost is not null)
        {
            avsCanvasHost.AddHandler(DragDrop.DropEvent, OnPresetDrop, RoutingStrategies.Tunnel);
            avsCanvasHost.AddHandler(DragDrop.DragOverEvent, OnPresetDragOver, RoutingStrategies.Tunnel);
        }

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
                var plugins = PluginRegistry.AvailablePlugins?.ToList()
                              ?? new List<PluginMetadata>();

                if (plugins.Count > 0)
                {
                    combo.ItemsSource = plugins.Select(p => p.DisplayName).ToList();

                    // Prefer the simple bars visual if it's registered
                    int idx = plugins.FindIndex(p => p.Id == "bars");
                    if (idx < 0) idx = 0;
                    combo.SelectedIndex = idx;

                    // Set initial plugin based on the resolved index
                    var initial = PluginRegistry.Create(plugins[idx].Id);
                    RenderSurfaceControl.SetPlugin(initial ?? new AvsVisualizerPlugin());

                    combo.SelectionChanged += (_, _) =>
                    {
                        if (RenderSurfaceControl is null) return;
                        int selected = combo.SelectedIndex;
                        if (selected >= 0 && selected < plugins.Count)
                        {
                            var selectedPlugin = plugins[selected];
                            var plug = PluginRegistry.Create(selectedPlugin.Id);
                            
                            if (plug != null)
                            {
                                RenderSurfaceControl.SetPlugin(plug);
                                
                                // Show status message
                                var statusText = this.FindControl<TextBlock>("LblTime");
                                if (statusText != null)
                                {
                                    statusText.Text = $"✅ Plugin changed to: {selectedPlugin.DisplayName}";
                                }
                                
                                // Force refresh
                                RenderSurfaceControl.InvalidateVisual();
                            }
                            else
                            {
                                // Fallback to AVS
                                var fallbackPlugin = new AvsVisualizerPlugin();
                                RenderSurfaceControl.SetPlugin(fallbackPlugin);
                                
                                var statusText = this.FindControl<TextBlock>("LblTime");
                                if (statusText != null)
                                {
                                    statusText.Text = $"⚠️ Failed to load {selectedPlugin.DisplayName}, using AVS fallback";
                                }
                            }
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
        
        // Wire up button event handlers
        WireUpEventHandlers();
        
        // Ensure we start in built-in mode so Skia host paints
        SetVisualMode(VisualMode.BuiltIn);
    }

    /// <summary>
    /// Initialize the visualizer based on the engine setting from VisualizerSettings
    /// </summary>
    private void InitializeVisualizerFromSettings()
    {
        try
        {
            var settings = VisualizerSettings.Load();
            var selectedEngine = settings.SelectedEngine;
            
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Initializing visualizer with engine: {selectedEngine}");
            
            if (selectedEngine == "phoenix")
            {
                // Use Phoenix engine - select a Phoenix visualizer
                var cmb = this.FindControl<ComboBox>("CmbPlugin");
                if (cmb != null)
                {
                    var items = PluginRegistry.AvailablePlugins?.ToList() ?? new List<PluginMetadata>();
                    
                    // Look for Phoenix visualizers first
                    var phoenixPlugin = items.FirstOrDefault(p => p.Id.Contains("phoenix") || p.DisplayName.Contains("Phoenix"));
                    if (phoenixPlugin != null)
                    {
                        var idx = items.IndexOf(phoenixPlugin);
                        cmb.SelectedIndex = idx;
                        
                        var plugin = PluginRegistry.Create(phoenixPlugin.Id);
                        if (plugin != null)
                        {
                            RenderSurfaceControl?.SetPlugin(plugin);
                            
                            // Switch to VLC audio service for Phoenix engine
                            if (RenderSurfaceControl != null)
                            {
                                var vlcAudioService = new PhoenixVisualizer.Audio.VlcAudioService();
                                RenderSurfaceControl.SetAudioService(vlcAudioService);
                                System.Diagnostics.Debug.WriteLine("[MainWindow] Switched to VLC audio service for Phoenix engine");
                            }
                            
                            System.Diagnostics.Debug.WriteLine($"[MainWindow] Phoenix engine initialized with: {phoenixPlugin.DisplayName}");
                        }
                    }
                    else
                    {
                        // Fallback to bars if no Phoenix visualizer found
                        var barsPlugin = items.FirstOrDefault(p => p.Id == "bars");
                        if (barsPlugin != null)
                        {
                            var idx = items.IndexOf(barsPlugin);
                            cmb.SelectedIndex = idx;
                            
                            var plugin = PluginRegistry.Create(barsPlugin.Id);
                            if (plugin != null)
                            {
                                RenderSurfaceControl?.SetPlugin(plugin);
                                System.Diagnostics.Debug.WriteLine($"[MainWindow] Phoenix engine fallback to: {barsPlugin.DisplayName}");
                            }
                        }
                    }
                }
            }
            else
            {
                // Use AVS engine - select bars visualizer as default
                var cmb = this.FindControl<ComboBox>("CmbPlugin");
                if (cmb != null)
                {
                    var items = PluginRegistry.AvailablePlugins?.ToList() ?? new List<PluginMetadata>();
                    var barsPlugin = items.FirstOrDefault(p => p.Id == "bars");
                    if (barsPlugin != null)
                    {
                        var idx = items.IndexOf(barsPlugin);
                        cmb.SelectedIndex = idx;
                        
                        var plugin = PluginRegistry.Create(barsPlugin.Id);
                        if (plugin != null)
                        {
                            RenderSurfaceControl?.SetPlugin(plugin);
                            System.Diagnostics.Debug.WriteLine($"[MainWindow] AVS engine initialized with: {barsPlugin.DisplayName}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to initialize visualizer from settings: {ex.Message}");
            // Fallback to default behavior
        }
    }

    private void WireUpEventHandlers()
    {
        if (_handlersWired) return;
        _handlersWired = true;

        // Wire up button click events
        var btnOpen = this.FindControl<Button>("BtnOpen");
        var btnPlay = this.FindControl<Button>("BtnPlay");
        var btnPause = this.FindControl<Button>("BtnPause");
        var btnStop = this.FindControl<Button>("BtnStop");
        var btnTempoPitch = this.FindControl<Button>("BtnTempoPitch");
        var btnSettings = this.FindControl<Button>("BtnSettings");
        var btnEditor = this.FindControl<Button>("BtnEditor");
        var btnPhxEditor = this.FindControl<Button>("BtnPhxEditor");
        var btnLoadPreset = this.FindControl<Button>("BtnLoadPreset");
        var btnExecutePreset = this.FindControl<Button>("BtnExecutePreset");
        var btnImportPreset = this.FindControl<Button>("BtnImportPreset");
        var btnPluginSwitcher = this.FindControl<Button>("BtnPluginSwitcher");
        if (btnOpen != null) btnOpen.Click += OnOpenClick;
        if (btnPlay != null) btnPlay.Click += OnPlayClick;
        if (btnPause != null) btnPause.Click += OnPauseClick;
        if (btnStop != null) btnStop.Click += OnStopClick;
        if (btnTempoPitch != null) btnTempoPitch.Click += OnTempoPitchClick;
        if (btnSettings != null) btnSettings.Click += OnSettingsClick;
        if (btnEditor != null) btnEditor.Click += OnAvsEditorClick;
        if (btnPhxEditor != null) btnPhxEditor.Click += OnPhxEditorClick;
        if (btnLoadPreset != null) btnLoadPreset.Click += OnLoadPreset;
        if (btnExecutePreset != null) btnExecutePreset.Click += OnExecutePreset;
        if (btnImportPreset != null) btnImportPreset.Click += OnImportPreset;
        if (btnPluginSwitcher != null) btnPluginSwitcher.Click += OnPluginSwitcherClick;

    }

    private void SetVisualMode(VisualMode mode)
    {
        _visualMode = mode;
        var skia = this.FindControl<RenderSurface>("RenderHost");
        var avsHost = this.FindControl<Control>("AvsWin32Host");
        var avsCanvas = this.FindControl<Grid>("AvsCanvasHost");
        
        if (mode == VisualMode.BuiltIn)
        {
            // Show Skia render surface, hide AVS elements
            if (skia != null)
            {
                skia.IsVisible = true;
                skia.ZIndex = 10; // Ensure it's on top
            }
            
            if (avsHost != null)
            {
                avsHost.IsVisible = false;
                avsHost.ZIndex = 0;
            }
            
            if (avsCanvas != null)
            {
                avsCanvas.IsVisible = false;
                avsCanvas.ZIndex = 0;
            }
        }
        else
        {
            // Hide Skia, show AVS elements
            if (skia != null)
            {
                skia.IsVisible = false;
                skia.ZIndex = 0;
            }
            
            if (avsHost != null)
            {
                avsHost.IsVisible = true;
                avsHost.ZIndex = 10;
            }
            
            if (avsCanvas != null)
            {
                avsCanvas.IsVisible = true;
                avsCanvas.ZIndex = 10;
            }
        }
    }

    private void OnPluginSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_currentPluginMode != PluginMode.BuiltIn) return;
        if (sender is ComboBox cb && cb.SelectedItem is PluginMetadata meta)
        {
            var plugin = PluginRegistry.Create(meta.Id);
            if (plugin != null && RenderSurfaceControl != null)
            {
                RenderSurfaceControl.SetPlugin(plugin);
                RenderSurfaceControl.InvalidateVisual();
                
                // Update status for AVS Effects Engine
                if (plugin is AvsEffectsVisualizer)
                {
                    UpdateAvsEffectsStatus();
                }
            }
        }
    }



    private void InitializePlugin()
    {
        // Load plugin from settings/config file
        if (RenderSurfaceControl is not null)
        {
            try
            {
                var settings = LoadApplicationSettings();
                var plugin = CreatePluginFromSettings(settings);
                RenderSurfaceControl.SetPlugin(plugin);
            }
            catch (Exception ex)
            {
                // Fallback to default plugin if settings loading fails
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
                var plugin = new AvsVisualizerPlugin(); // Default to AVS Engine
                RenderSurfaceControl.SetPlugin(plugin);
            }
        }
    }

    private ApplicationSettings LoadApplicationSettings()
    {
        var settingsPath = GetSettingsFilePath();

        if (File.Exists(settingsPath))
        {
            try
            {
                var json = File.ReadAllText(settingsPath);
                return JsonSerializer.Deserialize<ApplicationSettings>(json) ?? new ApplicationSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings file: {ex.Message}");
            }
        }

        // Return default settings if file doesn't exist or loading failed
        return new ApplicationSettings
        {
            DefaultPlugin = "AvsVisualizerPlugin",
            WindowWidth = 1200,
            WindowHeight = 800,
            EnableDebugLogging = false,
            Theme = "Dark",
            AudioDevice = "Default",
            LastOpenedFile = null
        };
    }

    private IVisualizerPlugin CreatePluginFromSettings(ApplicationSettings settings)
    {
        // For now, use the default AVS plugin since other plugin types may not be available
        return new AvsVisualizerPlugin();
    }

    private string GetSettingsFilePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var phoenixPath = Path.Combine(appDataPath, "PhoenixVisualizer");
        Directory.CreateDirectory(phoenixPath);
        return Path.Combine(phoenixPath, "settings.json");
    }

    private async void SaveApplicationSettings()
    {
        try
        {
            var settings = new ApplicationSettings
            {
                DefaultPlugin = RenderSurfaceControl?.CurrentPlugin?.GetType().Name ?? "AvsVisualizerPlugin",
                WindowWidth = (int)Width,
                WindowHeight = (int)Height,
                EnableDebugLogging = false, // This could be a UI setting
                Theme = "Dark", // This could be a UI setting
                AudioDevice = "Default",
                LastOpenedFile = null
            };

            var settingsPath = GetSettingsFilePath();
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(settingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    private async void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        if (RenderSurfaceControl is null) 
        {
            return;
        }

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
        if (file is null) 
        {
            return;
        }

        // Capture the control reference on the UI thread 👇
        var surface = RenderSurfaceControl;
        
        await Task.Run(() => 
        {
            var result = surface?.Open(file.RequireLocalPath());
            
            // Show user feedback on the UI thread
            Dispatcher.UIThread.Post(() =>
            {
                if (result == true)
                {
                    // Success - could show a brief success message
                }
                else
                {
                    // Failed to load - show error message
                    
                    // Show error dialog to user
                    var errorWindow = new Window
                    {
                        Title = "Audio Load Error",
                        Width = 400,
                        Height = 200,
                        CanResize = false,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };

                    var errorPanel = new StackPanel
                    {
                        Margin = new Thickness(20),
                        Spacing = 10
                    };

                    errorPanel.Children.Add(new TextBlock
                    {
                        Text = $"Failed to load audio file:",
                        FontWeight = FontWeight.Bold,
                        FontSize = 14
                    });

                    errorPanel.Children.Add(new TextBlock
                    {
                        Text = Path.GetFileName(file.RequireLocalPath()),
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap
                    });

                    errorPanel.Children.Add(new TextBlock
                    {
                        Text = "This file may be in an unsupported format or corrupted. Check the audio_debug.log file for details.",
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 11
                    });

                    var okButton = new Button
                    {
                        Content = "OK",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 10, 0, 0)
                    };
                    okButton.Click += (_, __) => errorWindow.Close();
                    errorPanel.Children.Add(okButton);

                    errorWindow.Content = errorPanel;
                    errorWindow.ShowDialog(this);
                }
            });
        });
    }

    private void OnPlayClick(object? sender, RoutedEventArgs e)
    {
        _ = sender; _ = e; // silence unused parameters
        try
        {
            if (RenderSurfaceControl is null)
            {
                return;
            }
            
            var playResult = RenderSurfaceControl.Play();
            if (playResult)
            {
                // TODO: Show user-friendly success message
            }
            else
            {
                // TODO: Show user-friendly message that they need to open an audio file first
            }
        }
        catch
        {
            // Play operation failed silently
        }
    }
    
    private void OnPauseClick(object? sender, RoutedEventArgs e)
    {
        _ = sender; _ = e; // silence unused parameters
        try
        {
            if (RenderSurfaceControl is null)
            {
                return;
            }
            
            RenderSurfaceControl.Pause();
        }
        catch
        {
            // Pause operation failed silently
        }
    }
    
    private void OnStopClick(object? sender, RoutedEventArgs e)
    {
        _ = sender; _ = e; // silence unused parameters
        try
        {
            // Stop native AVS if running
            try { NativeAvsHost.Stop(); } catch { /* ignore */ }
            
            if (RenderSurfaceControl is null)
            {
                return;
            }
            
            RenderSurfaceControl.Stop();
        }
        catch
        {
            // Stop operation failed silently
        }
    }

    private async void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        _ = sender; _ = e; // silence unused parameters
        try
        {
            var dlg = new SettingsWindow();
            await dlg.ShowDialog(this);
            
            // Settings dialog was closed, refresh the visualizer to pick up any changes
            System.Diagnostics.Debug.WriteLine("[MainWindow] Settings dialog closed, refreshing visualizer");
            InitializeVisualizerFromSettings();
        }
        catch
        {
            // Settings dialog failed silently
        }
    }

    private void OnEditorClick(object? sender, RoutedEventArgs e)
    {
        _ = sender; _ = e; // silence unused parameters
        // The Editor project reference was removed, so this functionality is disabled.
        // var editor = new EditorWindow();
        // await editor.ShowDialog(this);
    }

    private async void OnTempoPitchClick(object? sender, RoutedEventArgs e)
    {
        _ = sender; _ = e; // silence unused parameters
        try
        {
            if (RenderSurfaceControl is null) return;
            var audio = RenderSurfaceControl.GetAudioService(); // provided by RenderSurface
            if (audio is null) return;

            // Check if the audio service supports tempo/pitch features
            if (audio is VlcAudioService vlcAudio)
            {
                var dlg = new TempoPitchWindow(vlcAudio);
                await dlg.ShowDialog(this);
            }
            else
            {
                // Show message that tempo/pitch is not available with this audio service
                var statusText = this.FindControl<TextBlock>("LblTime");
                if (statusText != null)
                {
                    statusText.Text = "⚠️ Tempo/Pitch not available with this audio service";
                }
            }
        }
        catch
        {
            // TempoPitch dialog failed silently
        }
    }

            private void OnLoadPreset(object? sender, RoutedEventArgs e)
        {
            _ = sender; _ = e; // silence unused parameters
            var tb = this.FindControl<TextBox>("TxtPreset");
            if (tb is null || RenderSurfaceControl is null) return;

            var presetText = tb.Text ?? string.Empty;
            var statusText = this.FindControl<TextBlock>("LblTime");
            
            try
            {
                // FIX: For now, just use the standard preset loading flow
                // TODO: Add text-based preset updating for UnifiedAvsVisualizer later

                // Fallback for other cases (original logic)
                var plug = PluginRegistry.Create("vis_avs") as IAvsHostPlugin;
                if (plug is null)
                {
                    if (statusText != null) statusText.Text = "❌ Failed to create AVS plugin";
                    return;
                }

                // Cast to IVisualizerPlugin since AvsVisualizerPlugin implements both interfaces
                if (plug is IVisualizerPlugin visPlugin)
                {
                    RenderSurfaceControl.SetPlugin(visPlugin);
                    plug.LoadPreset(presetText);
                    
                    if (statusText != null) statusText.Text = $"✅ Preset loaded: {presetText}";
                    
                    // Force refresh
                    RenderSurfaceControl.InvalidateVisual();
                }
                else
                {
                    if (statusText != null) statusText.Text = "❌ Plugin is not IVisualizerPlugin";
                }
            }
            catch (Exception ex)
            {
                if (statusText != null) statusText.Text = $"❌ Load preset error: {ex.Message}";
            }
        }

        private void OnExecutePreset(object? sender, RoutedEventArgs e)
        {
            _ = sender; _ = e; // silence unused parameters
            var tb = this.FindControl<TextBox>("TxtPreset");
            if (tb is null || RenderSurfaceControl is null) return;

            var presetText = tb.Text;
            if (string.IsNullOrWhiteSpace(presetText))
            {
                // Show error message
                var statusText = this.FindControl<TextBlock>("LblTime");
                if (statusText != null)
                {
                    statusText.Text = "❌ No preset to execute!";
                }
                return;
            }

            try
            {
                var statusText = this.FindControl<TextBlock>("LblTime");
                
                // Create and set AVS plugin
                var plug = PluginRegistry.Create("vis_avs") as IAvsHostPlugin;
                if (plug is null)
                {
                    if (statusText != null) statusText.Text = "❌ Failed to create AVS plugin from registry";
                    return;
                }

                // Cast to IVisualizerPlugin since AvsVisualizerPlugin implements both interfaces
                if (plug is IVisualizerPlugin visPlugin)
                {
                    // CRITICAL: Set the plugin on the render surface FIRST
                    RenderSurfaceControl.SetPlugin(visPlugin);
                    
                    // THEN load the preset
                    plug.LoadPreset(presetText);
                    
                    // Force a visual refresh
                    RenderSurfaceControl.InvalidateVisual();
                    
                    // Show success message
                    if (statusText != null)
                    {
                        statusText.Text = $"✅ Preset executed: {presetText}";
                    }
                    
                    // Preset executed successfully
                }
                else
                {
                    if (statusText != null) statusText.Text = "❌ Plugin is not IVisualizerPlugin";
                }
            }
            catch (Exception ex)
            {
                // Show error message
                var statusText = this.FindControl<TextBlock>("LblTime");
                if (statusText != null)
                {
                    statusText.Text = $"❌ Preset execution failed: {ex.Message}";
                }
            }
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

            // Use the extension method to get the local path
            var filePath = file.RequireLocalPath();
            await ProcessAvsFile(filePath);
        }

        private async Task ProcessAvsFile(string filePath)
        {
            try
            {
                // Use the new unified AVS service - no more regex nightmares!
                Console.WriteLine("### JUSTIN DEBUG: Using NEW UnifiedAvsService! ###");
                var unifiedService = new UnifiedAvsService();
                var avsData = unifiedService.Load(filePath);
                
                Console.WriteLine($"### JUSTIN DEBUG: UnifiedAvsService detected type: {avsData.FileType} ###");
                Console.WriteLine($"### JUSTIN DEBUG: Found {avsData.Superscopes.Count} superscopes, {avsData.Effects.Count} effects ###");

                var fileExtension = Path.GetExtension(filePath).ToLower();

                // Show file content preview (handle both binary and text)
                string debugContent;
                if (avsData.FileType == AvsFileType.WinampBinary && avsData.RawBinary != null)
                {
                    // For binary files, show hex dump of first part
                    var hexBytes = avsData.RawBinary.Take(50).Select(b => b.ToString("X2")).ToArray();
                    debugContent = $"Binary data (hex): {string.Join(" ", hexBytes)}...";
                    if (avsData.RawBinary.Length > 50) debugContent += $" (total {avsData.RawBinary.Length} bytes)";
                }
                else
                {
                    // For text files, show actual content
                    var rawText = avsData.RawText ?? "";
                    debugContent = rawText.Length > 500
                        ? rawText.Substring(0, 500) + "..."
                        : rawText;
                }

                // Debug: Show parsing results with detection info
                var debugInfo = $"File: {Path.GetFileName(filePath)}\n" +
                               $"Extension: {fileExtension}\n" +
                               $"File Size: {new FileInfo(filePath).Length} bytes\n" +
                               $"Detected Type: {avsData.FileType}\n" +
                               $"Detection Confidence: {avsData.Detection.Confidence:F2}\n" +
                               $"Found Markers: {string.Join(", ", avsData.Detection.Markers)}\n" +
                               $"Binary Format: {avsData.FileType == AvsFileType.WinampBinary}\n" +
                               $"Superscopes: {avsData.Superscopes.Count}\n" +
                               $"Effects: {avsData.Effects.Count}\n" +
                               $"Content Length: {(avsData.RawText?.Length ?? avsData.RawBinary?.Length ?? 0)}\n" +
                               $"Has Superscopes: {avsData.Superscopes.Count > 0}\n" +
                               $"Has Effects: {avsData.Effects.Count > 0}\n\n";

                // Add superscope details
                if (avsData.Superscopes.Count > 0)
                {
                    debugInfo += "Superscope Details:\n";
                    foreach (var scope in avsData.Superscopes)
                    {
                        debugInfo += $"  - {scope.Name} ({scope.SourceType})\n";
                        var codeLength = scope.CombinedCode.Length;
                        debugInfo += $"    Code Length: {codeLength} chars\n";
                    }
                    debugInfo += "\n";
                }

                debugInfo += $"File Content Preview:\n{debugContent}";

                // Check if we have any importable content
                if (avsData.Superscopes.Count == 0 && avsData.Effects.Count == 0)
                {
                    await ShowDebugDialogAsync(debugInfo);
                    return;
                }

                // Show success with details
                await ShowToastAsync($"✅ AVS preset loaded: {Path.GetFileName(filePath)}\n" +
                                   $"Type: {avsData.FileType}\n" +
                                   $"Superscopes: {avsData.Superscopes.Count}, Effects: {avsData.Effects.Count}");

                // Create a proper AVS visualizer that can handle the unified data
                var avsVisualizer = new UnifiedAvsVisualizer();
                avsVisualizer.LoadAvsData(avsData);
                if (RenderSurfaceControl != null)
                {
                    RenderSurfaceControl.SetPlugin(avsVisualizer);
                }
                else
                {
                    await ShowDialogAsync("PhoenixVisualizer", "❌ Render surface not available");
                }
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("PhoenixVisualizer", $"❌ Failed to import AVS preset: {ex.Message}\n\n{ex.StackTrace}");
            }
        }

        private async Task ShowDebugDialogAsync(string debugInfo)
        {
            // Create a custom dialog window with copy to clipboard functionality
            var dialog = new Window
            {
                Title = "AVS Import Debug Info",
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = true
            };

            // Create the main content stack
            var mainStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(10)
            };

            // Title text
            var titleText = new TextBlock
            {
                Text = "❌ No superscopes or effects found in the AVS file",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainStack.Children.Add(titleText);

            // Debug info text box
            var debugTextBox = new TextBox
            {
                Text = debugInfo,
                FontFamily = "Consolas, Monaco, monospace",
                FontSize = 12,
                AcceptsReturn = true,
                AcceptsTab = true,
                IsReadOnly = true,
                Height = 400,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainStack.Children.Add(debugTextBox);

            // Button panel
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 10
            };

            // Copy to clipboard button
            var copyButton = new Button
            {
                Content = "📋 Copy to Clipboard",
                Width = 150,
                Height = 35
            };
            copyButton.Click += async (s, e) =>
            {
                try
                {
                    var topLevel = TopLevel.GetTopLevel(this);
                    if (topLevel?.Clipboard != null)
                    {
                        await topLevel.Clipboard.SetTextAsync(debugInfo);
                        await ShowToastAsync("✅ Debug info copied to clipboard!");
                    }
                    else
                    {
                        await ShowToastAsync("❌ Clipboard not available");
                    }
                }
                catch (Exception ex)
                {
                    await ShowToastAsync($"❌ Failed to copy: {ex.Message}");
                }
            };
            buttonPanel.Children.Add(copyButton);

            // OK button
            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Height = 35
            };
            okButton.Click += (s, e) =>
            {
                dialog.Close();
            };
            buttonPanel.Children.Add(okButton);

            mainStack.Children.Add(buttonPanel);
            dialog.Content = mainStack;

            // Show the dialog
            await dialog.ShowDialog(this);
        }

        private async Task ProcessPresetBytes(byte[] bytes, string fileName)
        {
            // Keep the old method for backward compatibility with existing calls
            // Route AVS binaries vs. our text NS-EEL path
            var route = AvsPresetRouter.Decide(bytes, fileName);
            if (route.Route == AvsRoute.NativeAvs)
            {
                if (!NativeAvsHost.TryLoad(out var why, null))
                {
                    await ShowDialogAsync("PhoenixVisualizer", $"❌ vis_avs.dll not available\n\n{why}");
                    return;
                }
                var mods = NativeAvsHost.ListModules();
                var stagedPath = NativeAvsHost.StagePreset(bytes);
                Log.Info($"AVS staged: {stagedPath}");

                if (_avsWin32Host?.Hwnd is nint hwnd && hwnd != 0)
                {
                    if (NativeAvsHost.Start(hwnd, out var msg, 44100, 2))
                        await ShowToastAsync($"🧩 {msg}");
                    else
                        await ShowDialogAsync("PhoenixVisualizer", $"❌ AVS failed to start\n\n{msg}");
                }
                else
                {
                    await ShowDialogAsync("PhoenixVisualizer", "❌ No native host handle available.");
                }
                return;
            }
            if (route.Route == AvsRoute.Unsupported)
            {
                await ShowDialogAsync("PhoenixVisualizer", route.Message ?? "❌ AVS preset not supported yet.");
                return;
            }

            // Not an AVS binary → treat as text preset (NS-EEL / Phoenix format)
            string text;
            try
            {
                text = Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                text = Encoding.Default.GetString(bytes);
            }

            var plug = PluginRegistry.Create("vis_avs") as IAvsHostPlugin;
            if (plug is null) return;

            // Cast to IVisualizerPlugin since AvsVisualizerPlugin implements both interfaces
            if (plug is IVisualizerPlugin visPlugin && RenderSurfaceControl != null)
            {
                RenderSurfaceControl.SetPlugin(visPlugin);
                plug.LoadPreset(text);
            }
        }

        private void OnAvsEditorClick(object? sender, RoutedEventArgs e)
        {
            var avsEditor = new Views.AvsEditor();

            // Subscribe to the AVS content import event
            avsEditor.AvsContentImported += (avsContent) =>
            {
                // Handle the AVS content in the main window
                HandleAvsContentFromEditor(avsContent);
            };

            avsEditor.Show();
        }

        private void OnPhxEditorClick(object? sender, RoutedEventArgs e)
        {
            var phxEditor = new PhxEditorWindow();
            phxEditor.Show();
        }



        private void HandleAvsContentFromEditor(string avsContent)
        {
            try
            {
                // Update the preset text box with the AVS content
                var presetTextBox = this.FindControl<TextBox>("TxtPreset");
                if (presetTextBox != null)
                {
                    presetTextBox.Text = avsContent;
                }

                // Prefer full AVS engine via overlay
                if (_avsCanvas is not null)
                {
                    var preset = new PhoenixVisualizer.Core.Models.AvsPreset
                    {
                        Name = "From Editor",
                        Description = "Sent from AVS Editor",
                        Author = "Editor"
                    };
                    preset.FrameEffects.Add(new PhoenixVisualizer.Core.Models.AvsEffect
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Custom",
                        DisplayName = "Custom Code",
                        Description = "Imported from text",
                        Type = PhoenixVisualizer.Core.Models.AvsEffectType.Custom,
                        Section = PhoenixVisualizer.Core.Models.AvsSection.Frame,
                        Code = avsContent,
                        IsEnabled = true,
                        ClearEveryFrame = true
                    });

                    Task.Run(async () =>
                    {
                        var ok = await _avsBridge.LoadPresetAsync(preset);
                        if (ok)
                        {
                            await _avsAudio.StartAsync();
                            await _avsBridge.StartPresetAsync();
                            Dispatcher.UIThread.Post(() =>
                            {
                                var statusText = this.FindControl<TextBlock>("LblTime");
                                if (statusText != null) statusText.Text = "AVS engine running (editor preset)";
                            });
                        }
                    });
                    return;
                }

                // Fallback to mini plugin
                if (RenderSurfaceControl != null)
                {
                    var plug = PluginRegistry.Create("vis_avs") as IAvsHostPlugin;
                    if (plug != null && plug is IVisualizerPlugin visPlugin)
                    {
                        RenderSurfaceControl.SetPlugin(visPlugin);
                        plug.LoadPreset(avsContent);
                        var statusText = this.FindControl<TextBlock>("LblTime");
                        if (statusText != null) statusText.Text = "AVS mini plugin executed from editor!";
                    }
                }
            }
            catch (Exception ex)
            {
                // Show error message
                var statusText = this.FindControl<TextBlock>("LblTime");
                if (statusText != null)
                {
                    statusText.Text = $"Failed to execute preset: {ex.Message}";
                }
            }
        }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!VisualizerSettings.Load().EnableHotkeys) return;

        switch (e.Key)
        {
            case Key.Y:
                Presets.GoPrev();
                break;
            case Key.U:
                Presets.GoNext();
                break;
            case Key.Space:
                Presets.GoRandom();
                break;
            case Key.R:
                var s = VisualizerSettings.Load();
                s.RandomPresetMode = s.RandomPresetMode == RandomPresetMode.OnBeat ? RandomPresetMode.Off : RandomPresetMode.OnBeat;
                s.Save();
                break;
            case Key.Enter:
                ToggleFullscreen();
                break;
            case Key.E:
                // Execute preset with E key
                OnExecutePreset(null, null!);
                break;
            case Key.C:
                // Configure AVS Effects Engine
                ConfigureAvsEffects();
                break;
            case Key.G:
                // Toggle effect grid
                ToggleEffectGrid();
                break;
            case Key.A:
                // Add random effect
                AddRandomEffect();
                break;
            case Key.X:
                // Remove last effect
                RemoveLastEffect();
                break;
        }
    }

    private void ToggleFullscreen()
    {
        WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
    }

    private void ConfigureAvsEffects()
    {
        try
        {
            // Find and configure the AVS Effects Engine plugin
            if (RenderSurfaceControl?.CurrentPlugin is AvsEffectsVisualizer avsVisualizer)
            {
                avsVisualizer.Configure();
            }
            else
            {
                // Show status message
                var statusText = this.FindControl<TextBlock>("LblTime");
                if (statusText != null)
                {
                    statusText.Text = "Press 'C' to configure AVS Effects Engine (when active)";
                }
            }
        }
        catch (Exception ex)
        {
            var statusText = this.FindControl<TextBlock>("LblTime");
            if (statusText != null)
            {
                statusText.Text = $"❌ AVS config failed: {ex.Message}";
            }
        }
    }

    private void ToggleEffectGrid()
    {
        try
        {
            if (RenderSurfaceControl?.CurrentPlugin is AvsEffectsVisualizer avsVisualizer)
            {
                avsVisualizer.ShowEffectGrid = !avsVisualizer.ShowEffectGrid;
                
                // Show status message
                var statusText = this.FindControl<TextBlock>("LblTime");
                if (statusText != null)
                {
                    statusText.Text = $"Effect Grid: {(avsVisualizer.ShowEffectGrid ? "ON" : "OFF")}";
                }
            }
        }
        catch (Exception ex)
        {
            var statusText = this.FindControl<TextBlock>("LblTime");
            if (statusText != null)
            {
                statusText.Text = $"❌ Grid toggle failed: {ex.Message}";
            }
        }
    }

    private void AddRandomEffect()
    {
        try
        {
            if (RenderSurfaceControl?.CurrentPlugin is AvsEffectsVisualizer avsVisualizer)
            {
                var availableEffects = avsVisualizer.GetAvailableEffectNames();
                var activeEffects = avsVisualizer.GetActiveEffectNames();
                
                // Find effects that aren't currently active
                var unusedEffects = availableEffects.Except(activeEffects).ToList();
                
                if (unusedEffects.Count > 0)
                {
                    var random = new Random();
                    var randomEffect = unusedEffects[random.Next(unusedEffects.Count)];
                    avsVisualizer.AddEffect(randomEffect);
                    
                    // Show status message
                    var statusText = this.FindControl<TextBlock>("LblTime");
                    if (statusText != null)
                    {
                        statusText.Text = $"Added effect: {randomEffect}";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var statusText = this.FindControl<TextBlock>("LblTime");
            if (statusText != null)
            {
                statusText.Text = $"❌ Add effect failed: {ex.Message}";
            }
        }
    }

    private void RemoveLastEffect()
    {
        try
        {
            if (RenderSurfaceControl?.CurrentPlugin is AvsEffectsVisualizer avsVisualizer)
            {
                var activeEffects = avsVisualizer.GetActiveEffectNames();
                
                if (activeEffects.Count > 0)
                {
                    var lastEffect = activeEffects[activeEffects.Count - 1];
                    avsVisualizer.RemoveEffect(activeEffects.Count - 1);
                    
                    // Show status message
                    var statusText = this.FindControl<TextBlock>("LblTime");
                    if (statusText != null)
                    {
                        statusText.Text = $"Removed effect: {lastEffect}";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var statusText = this.FindControl<TextBlock>("LblTime");
            if (statusText != null)
            {
                statusText.Text = $"❌ Remove effect failed: {ex.Message}";
            }
        }
    }

    private void UpdateAvsEffectsStatus()
    {
        try
        {
            if (RenderSurfaceControl?.CurrentPlugin is AvsEffectsVisualizer avsVisualizer)
            {
                var activeEffects = avsVisualizer.GetActiveEffectNames();
                var totalEffects = avsVisualizer.GetAvailableEffectNames().Count;
                
                var statusText = this.FindControl<TextBlock>("LblTime");
                if (statusText != null)
                {
                    statusText.Text = $"AVS Effects: {activeEffects.Count}/{totalEffects} | Press C to configure";
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Error updating AVS effects status: {ex.Message}");
        }
    }



    private void OnPluginSwitcherClick(object? sender, RoutedEventArgs e)
    {
        _ = sender; _ = e; // silence unused parameters
        try
        {
            // Currently only built-in mode is supported
            var statusText = this.FindControl<TextBlock>("LblTime");
            if (statusText != null)
            {
                statusText.Text = "✅ Built-in visualizers active";
            }
        }
        catch (Exception ex)
        {
            var statusText = this.FindControl<TextBlock>("LblTime");
            if (statusText != null)
            {
                statusText.Text = $"❌ Plugin switch failed: {ex.Message}";
            }
        }
    }



    private void SwitchToBuiltInMode()
    {
        try
        {
            // Switch back to built-in mode
            _currentPluginMode = PluginMode.BuiltIn;
            
            // Update button text
            var btnSwitcher = this.FindControl<Button>("BtnPluginSwitcher");
            if (btnSwitcher != null)
            {
                btnSwitcher.Content = "🔄 Built-in Mode";
            }

            // Restore built-in plugin
            if (RenderSurfaceControl != null)
            {
                var plugin = new AvsVisualizerPlugin();
                RenderSurfaceControl.SetPlugin(plugin);
                RenderSurfaceControl.InvalidateVisual();
            }

            // Show status
            var statusText = this.FindControl<TextBlock>("LblTime");
            if (statusText != null)
            {
                statusText.Text = "✅ Switched back to built-in visualizers";
            }

            // Update UI visibility
            UpdatePluginModeUI();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to switch to built-in mode: {ex.Message}");
        }
    }

    private void UpdatePluginModeUI()
    {
        // Always show RenderSurface for built-ins
        if (_renderSurface != null) _renderSurface.IsVisible = true;
    }



    private void OnPresetDragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files)) e.DragEffects = DragDropEffects.Copy;
        else e.DragEffects = DragDropEffects.None;
    }

    private async void OnPresetDrop(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files)) return;
        var items = e.Data.GetFiles()?.ToList();
        if (items is null || items.Count == 0) return;

        // Drag-n-drop gives IStorageItem; get local path
        var item = items[0];
        var filePath = item.TryGetLocalPath();
        if (string.IsNullOrEmpty(filePath))
        {
            // Skip items without local paths for now
            return;
        }

        var bytes = await File.ReadAllBytesAsync(filePath);
        var displayName = Path.GetFileName(filePath);

        var route = AvsPresetRouter.Decide(bytes, displayName);
        if (route.Route == AvsRoute.NativeAvs)
        {
            if (!NativeAvsHost.TryLoad(out var why, null))
            {
                await ShowDialogAsync("PhoenixVisualizer", $"❌ vis_avs.dll not available\n\n{why}");
                return;
            }
            var mods = NativeAvsHost.ListModules();
            var stagedPath = NativeAvsHost.StagePreset(bytes);
            Log.Info($"AVS staged: {stagedPath}");
            if (_avsWin32Host?.Hwnd is nint hwnd && hwnd != 0)
            {
                                    if (NativeAvsHost.Start(hwnd, out var msg, 44100, 2))
                    await ShowToastAsync($"🧩 {msg}");
                else
                    await ShowDialogAsync("PhoenixVisualizer", $"❌ AVS failed to start\n\n{msg}");
            }
            else
            {
                await ShowDialogAsync("PhoenixVisualizer", "❌ No native host handle available.");
            }
            return;
        }
        if (route.Route == AvsRoute.Unsupported)
        {
            await ShowDialogAsync("PhoenixVisualizer", route.Message ?? "❌ AVS preset not supported yet.");
            return;
        }

        // Fallback to text path
        string text;
        try { text = Encoding.UTF8.GetString(bytes); }
        catch { text = Encoding.Default.GetString(bytes); }
        var tb = this.FindControl<TextBox>("TxtPreset");
        if (tb != null) tb.Text = text;
        OnExecutePreset(sender, e);
    }

    private async Task ShowToastAsync(string message)
    {
        // Simple toast implementation - could be enhanced with a proper toast service
        var lbl = this.FindControl<TextBlock>("LblTime");
        if (lbl != null) lbl.Text = message;
        await Task.Delay(3000); // Show for 3 seconds
    }

    private async Task ShowDialogAsync(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 200,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 10
        };

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12
        });

        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0)
            };

        okButton.Click += (_, __) => dialog.Close();
        panel.Children.Add(okButton);
        dialog.Content = panel;

        await dialog.ShowDialog(this);
    }

    private void OnKeyDown_OpenParameterEditor(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F4)
        {
            var w = new PhoenixVisualizer.App.Views.ParameterEditorWindow();
            w.Show(this);
            e.Handled = true;
        }
    }

    private void LogError(string context, Exception ex)
        => Console.Error.WriteLine($"[PhoenixVisualizer] {context}: {ex.Message}");
}

/// <summary>
/// Application settings for persistence
/// </summary>
public class ApplicationSettings
{
    public string DefaultPlugin { get; set; } = "AvsVisualizerPlugin";
    public int WindowWidth { get; set; } = 1200;
    public int WindowHeight { get; set; } = 800;
    public bool EnableDebugLogging { get; set; } = false;
    public string Theme { get; set; } = "Dark";
    public string AudioDevice { get; set; } = "Default";
    public string? LastOpenedFile { get; set; }
}