using PhoenixVisualizer.Core.Config;
using PhoenixVisualizer.Core.Services;
using PhoenixVisualizer.Core.Avs;
using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.PluginHost.Services;
using PhoenixVisualizer.Plugins.Avs;
using PhoenixVisualizer.App.Rendering;
using PhoenixVisualizer.App.Controls;
using PhoenixVisualizer.Core.Diagnostics;
using PhoenixVisualizer.App.Utils;
using System.Text;

namespace PhoenixVisualizer.Views;

public partial class MainWindow : Window
{
    private bool _handlersWired;
    private WinampPluginManager? _pluginManager;
    private enum VisualMode { BuiltIn, Winamp }
    private VisualMode _visualMode = VisualMode.BuiltIn;
    
    // Plugin mode tracking
    private enum PluginMode { BuiltIn, Winamp }
    private PluginMode _currentPluginMode = PluginMode.BuiltIn;
    private WinampIntegrationService? _winampService;
    

    
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
    private Canvas? _avsCanvas;
    private AvsHostControl? _avsWin32Host;
    private Control? _avsWin32HostControl;



    public MainWindow()
    {
        // Manually load XAML so we don't depend on generated InitializeComponent()
        AvaloniaXamlLoader.Load(this);
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
                cmb.SelectedIndex = 0; // trigger selection -> SetPlugin
            }
        }

        // ✅ Wire Winamp UI buttons if present
        var btnWinamp = this.FindControl<Button>("BtnWinampPlugins");
        if (btnWinamp != null) btnWinamp.Click += (_, __) => OpenWinampManager();
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
        var btnLoadPreset = this.FindControl<Button>("BtnLoadPreset");
        var btnExecutePreset = this.FindControl<Button>("BtnExecutePreset");
        var btnImportPreset = this.FindControl<Button>("BtnImportPreset");
        var btnHotkeyManager = this.FindControl<Button>("BtnHotkeyManager");
        var btnPluginSwitcher = this.FindControl<Button>("BtnPluginSwitcher");
        var btnWinampPlugins = this.FindControl<Button>("BtnWinampPlugins");

        if (btnOpen != null) btnOpen.Click += OnOpenClick;
        if (btnPlay != null) btnPlay.Click += OnPlayClick;
        if (btnPause != null) btnPause.Click += OnPauseClick;
        if (btnStop != null) btnStop.Click += OnStopClick;
        if (btnTempoPitch != null) btnTempoPitch.Click += OnTempoPitchClick;
        if (btnSettings != null) btnSettings.Click += OnSettingsClick;
        if (btnEditor != null) btnEditor.Click += OnAvsEditorClick;
        if (btnLoadPreset != null) btnLoadPreset.Click += OnLoadPreset;
        if (btnExecutePreset != null) btnExecutePreset.Click += OnExecutePreset;
        if (btnImportPreset != null) btnImportPreset.Click += OnImportPreset;
        if (btnHotkeyManager != null) btnHotkeyManager.Click += OnHotkeyManagerClick;
        if (btnPluginSwitcher != null) btnPluginSwitcher.Click += OnPluginSwitcherClick;
        if (btnWinampPlugins != null) btnWinampPlugins.Click += OnWinampPluginsClick;
    }

    private void SetVisualMode(VisualMode mode)
    {
        _visualMode = mode;
        var winamp = this.FindControl<Control>("AvsWin32Host");
        var skia = this.FindControl<RenderSurface>("RenderHost");
        
        if (mode == VisualMode.BuiltIn)
        {
            if (winamp != null)
            {
                winamp.IsEnabled = false;
                winamp.IsVisible = false;
            }
            if (skia != null)
            {
                skia.IsVisible = true;
            }
        }
        else
        {
            if (skia != null)
            {
                skia.IsVisible = false;
            }
            if (winamp != null)
            {
                winamp.IsEnabled = true;
                winamp.IsVisible = true;
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
            }
        }
    }

    private void OpenWinampManager()
    {
        try
        {
            var mgr = new Views.WinampPluginManager();
            mgr.Show(this);
        }
        catch (Exception ex)
        {
            // emoji-friendly status already in the manager; just log
            Console.WriteLine($"Winamp manager error: {ex.Message}");
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

            var dlg = new TempoPitchWindow(audio);
            await dlg.ShowDialog(this);
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
            var bytes = await File.ReadAllBytesAsync(filePath);
            await ProcessPresetBytes(bytes, Path.GetFileName(filePath));
        }

        private async Task ProcessPresetBytes(byte[] bytes, string fileName)
        {
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
        }
    }

    private void ToggleFullscreen()
    {
        WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
    }

    private void OnHotkeyManagerClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var hotkeyService = new PhoenixVisualizer.Services.WinampHotkeyService(
                PhoenixVisualizer.Core.Config.VisualizerSettings.Load()
            );
            var hotkeyWindow = new PhoenixVisualizer.Views.HotkeyManagerWindow(hotkeyService);
            hotkeyWindow.Show();
        }
        catch
        {
            // Error opening hotkey manager silently
        }
    }

    private async void OnPluginSwitcherClick(object? sender, RoutedEventArgs e)
    {
        _ = sender; _ = e; // silence unused parameters
        try
        {
            // Toggle between built-in and Winamp plugins
            if (_currentPluginMode == PluginMode.BuiltIn)
            {
                // Switch to Winamp mode
                await SwitchToWinampMode();
            }
            else
            {
                // Switch to built-in mode
                SwitchToBuiltInMode();
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

    private async Task SwitchToWinampMode()
    {
        try
        {
            // Initialize Winamp service if not already done
            if (_winampService == null)
            {
                _winampService = new WinampIntegrationService();
            }

            // Scan for available Winamp plugins
            var result = await _winampService.ScanForPluginsAsync();
            
            if (result.Error != null)
            {
                throw new InvalidOperationException($"Winamp scan failed: {result.Error.Message}");
            }

            if (result.Plugins.Count == 0)
            {
                throw new InvalidOperationException("No Winamp plugins found. Please check your plugin directory.");
            }

            // Switch to Winamp mode
            _currentPluginMode = PluginMode.Winamp;
            
            // Update button text
            var btnSwitcher = this.FindControl<Button>("BtnPluginSwitcher");
            if (btnSwitcher != null)
            {
                btnSwitcher.Content = "🔄 Switch to Built-in";
            }

            // Show status
            var statusText = this.FindControl<TextBlock>("LblTime");
            if (statusText != null)
            {
                statusText.Text = $"✅ Switched to Winamp mode - {result.Plugins.Count} plugins available";
            }

            // Update UI visibility
            UpdatePluginModeUI();

            // TODO: Set the first available Winamp plugin as active
            // For now, just indicate the mode change
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to switch to Winamp mode: {ex.Message}");
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
                btnSwitcher.Content = "🔄 Switch to Winamp";
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
        var isWinamp = _currentPluginMode == PluginMode.Winamp;
        // Show/hide Winamp host if present; always show RenderSurface for built-ins
        if (_avsWin32HostControl != null) _avsWin32HostControl.IsVisible = isWinamp;
        if (_renderSurface != null) _renderSurface.IsVisible = !isWinamp;
    }

    // Single-instance window open; avoid double Show() if handler is wired twice, etc.
    private void OnWinampPluginsClick(object? sender, RoutedEventArgs e)
    {
        if (_pluginManager is { } existing && existing.IsVisible)
        {
            existing.Activate();
            return;
        }
        _pluginManager = new WinampPluginManager();
        _pluginManager.Closed += (_, __) => _pluginManager = null;
        _pluginManager.Show(this);
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

    private void LogError(string context, Exception ex)
        => Console.Error.WriteLine($"[PhoenixVisualizer] {context}: {ex.Message}");
}