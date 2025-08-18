using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Markup.Xaml;              // <-- manual XAML load
using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Plugins.Avs;
using PhoenixVisualizer.Rendering;
using PhoenixVisualizer.Core.Config;
using PhoenixVisualizer.Core;
using PhoenixVisualizer.ViewModels;

namespace PhoenixVisualizer.Views;

public partial class MainWindow : Window
{
    // Grab the render surface once on the UI thread so background tasks don't try
    // to traverse the visual tree later (which would throw 🙅‍♂️)
    private readonly RenderSurface? _renderSurface;
    private RenderSurface? RenderSurfaceControl => _renderSurface;

    private static readonly string[] AudioPatterns = { 
        "*.mp3", "*.wav", "*.flac", "*.ogg", "*.m4a", "*.aac", "*.wma", "*.ape", "*.mpc", "*.tta", "*.alac" 
    };

    // Debug logging to file
    static void LogToFile(string message)
    {
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "main_debug.log");
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] {message}";
            File.AppendAllText(logPath, logMessage + Environment.NewLine);
        }
        catch
        {
            // Silently fail if logging fails
        }
    }

    public MainWindow()
    {
        // Manually load XAML so we don't depend on generated InitializeComponent()
        AvaloniaXamlLoader.Load(this);
        _renderSurface = this.FindControl<RenderSurface>("RenderHost");
        
        System.Diagnostics.Debug.WriteLine($"[MainWindow] Constructor: _renderSurface found: {_renderSurface != null}");
        if (_renderSurface != null)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Constructor: RenderSurface bounds: {_renderSurface.Bounds}");
        }
        
        Presets.Initialize(_renderSurface);

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
                            var plug = PluginRegistry.Create(plugins[selected].Id)
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
        LogToFile($"[MainWindow] OnOpenClick: Starting file open process");
        if (RenderSurfaceControl is null) 
        {
            LogToFile($"[MainWindow] OnOpenClick: RenderSurfaceControl is null");
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
            LogToFile($"[MainWindow] OnOpenClick: No file selected");
            return;
        }

        LogToFile($"[MainWindow] OnOpenClick: File selected: {file.Path.LocalPath}");

        // Capture the control reference on the UI thread 👇
        var surface = RenderSurfaceControl;
        LogToFile($"[MainWindow] OnOpenClick: RenderSurfaceControl is: {surface != null}");
        
        await Task.Run(() => 
        {
            LogToFile($"[MainWindow] OnOpenClick: Calling surface.Open from background thread");
            var result = surface?.Open(file.Path.LocalPath);
            LogToFile($"[MainWindow] OnOpenClick: surface.Open result: {result}");
            
            // Show user feedback on the UI thread
            Dispatcher.UIThread.Post(() =>
            {
                if (result == true)
                {
                    // Success - could show a brief success message
                    LogToFile($"[MainWindow] Audio file loaded successfully: {file.Name}");
                }
                else
                {
                    // Failed to load - show error message
                    LogToFile($"[MainWindow] Failed to load audio file: {file.Name}");
                    
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
                        Text = file.Name,
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
        try
        {
            LogToFile($"[MainWindow] OnPlayClick: Button clicked, RenderSurfaceControl is: {RenderSurfaceControl != null}");
            System.Diagnostics.Debug.WriteLine($"[MainWindow] OnPlayClick: Button clicked, RenderSurfaceControl is: {RenderSurfaceControl != null}");
            System.Diagnostics.Debug.WriteLine($"[MainWindow] OnPlayClick: _renderSurface field is: {_renderSurface != null}");
            
            if (RenderSurfaceControl is null)
            {
                LogToFile($"[MainWindow] OnPlayClick: RenderSurfaceControl is null");
                System.Diagnostics.Debug.WriteLine("[MainWindow] OnPlayClick: RenderSurfaceControl is null");
                return;
            }
            
            LogToFile($"[MainWindow] OnPlayClick: Starting playback");
            System.Diagnostics.Debug.WriteLine("[MainWindow] OnPlayClick: Starting playback");
            var playResult = RenderSurfaceControl.Play();
            LogToFile($"[MainWindow] OnPlayClick: Play() result: {playResult}");
            if (playResult)
            {
                LogToFile($"[MainWindow] OnPlayClick: Play() called successfully");
                System.Diagnostics.Debug.WriteLine("[MainWindow] OnPlayClick: Play() called successfully");
            }
            else
            {
                LogToFile($"[MainWindow] OnPlayClick: Play() failed - no audio file loaded");
                System.Diagnostics.Debug.WriteLine("[MainWindow] OnPlayClick: Play() failed - no audio file loaded");
                // TODO: Show user-friendly message that they need to open an audio file first
            }
        }
        catch (Exception ex)
        {
            LogToFile($"[MainWindow] OnPlayClick failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[MainWindow] OnPlayClick failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[MainWindow] OnPlayClick stack trace: {ex.StackTrace}");
        }
    }
    
    private void OnPauseClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (RenderSurfaceControl is null)
            {
                System.Diagnostics.Debug.WriteLine("[MainWindow] OnPauseClick: RenderSurfaceControl is null");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine("[MainWindow] OnPauseClick: Pausing playback");
            RenderSurfaceControl.Pause();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] OnPauseClick failed: {ex.Message}");
        }
    }
    
    private void OnStopClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (RenderSurfaceControl is null)
            {
                System.Diagnostics.Debug.WriteLine("[MainWindow] OnStopClick: RenderSurfaceControl is null");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine("[MainWindow] OnStopClick: Stopping playback");
            RenderSurfaceControl.Stop();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] OnStopClick failed: {ex.Message}");
        }
    }

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

    private void OnEditorClick(object? sender, RoutedEventArgs e)
    {
        // The Editor project reference was removed, so this functionality is disabled.
        // var editor = new EditorWindow();
        // await editor.ShowDialog(this);
    }

    private async void OnTempoPitchClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (RenderSurfaceControl is null) return;
            var audio = RenderSurfaceControl.GetAudioService(); // provided by RenderSurface
            if (audio is null) return;

            var dlg = new TempoPitchWindow(audio);
            await dlg.ShowDialog(this);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] TempoPitch dialog failed: {ex.Message}");
        }
    }

            private void OnLoadPreset(object? sender, RoutedEventArgs e)
        {
            var tb = this.FindControl<TextBox>("TxtPreset");
            if (tb is null || RenderSurfaceControl is null) return;

            var plug = PluginRegistry.Create("vis_avs") as IAvsHostPlugin;
            if (plug is null) return;

            // Cast to IVisualizerPlugin since AvsVisualizerPlugin implements both interfaces
            if (plug is IVisualizerPlugin visPlugin)
            {
                RenderSurfaceControl.SetPlugin(visPlugin);
                plug.LoadPreset(tb.Text ?? string.Empty);
            }
        }

        private void OnExecutePreset(object? sender, RoutedEventArgs e)
        {
            var tb = this.FindControl<TextBox>("TxtPreset");
            if (tb is null || RenderSurfaceControl is null) return;

            var presetText = tb.Text;
            if (string.IsNullOrWhiteSpace(presetText))
            {
                // Show error message
                var statusText = this.FindControl<TextBlock>("LblTime");
                if (statusText != null)
                {
                    statusText.Text = "No preset to execute!";
                }
                return;
            }

            try
            {
                // Create and set AVS plugin
                var plug = PluginRegistry.Create("vis_avs") as IAvsHostPlugin;
                if (plug is null)
                {
                    // Fallback to direct AVS plugin creation
                    plug = new AvsVisualizerPlugin();
                }

                // Cast to IVisualizerPlugin since AvsVisualizerPlugin implements both interfaces
                if (plug is IVisualizerPlugin visPlugin)
                {
                    RenderSurfaceControl.SetPlugin(visPlugin);
                    plug.LoadPreset(presetText);
                    
                    // Show success message
                    var statusText = this.FindControl<TextBlock>("LblTime");
                    if (statusText != null)
                    {
                        statusText.Text = "Preset executed successfully!";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to execute preset: {ex.Message}");
                
                // Show error message
                var statusText = this.FindControl<TextBlock>("LblTime");
                if (statusText != null)
                {
                    statusText.Text = $"Preset execution failed: {ex.Message}";
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

            var plug = PluginRegistry.Create("vis_avs") as IAvsHostPlugin;
            if (plug is null) return;

            using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream);
            var text = await reader.ReadToEndAsync();

            // Cast to IVisualizerPlugin since AvsVisualizerPlugin implements both interfaces
            if (plug is IVisualizerPlugin visPlugin)
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

                // Automatically execute the preset
                if (RenderSurfaceControl != null)
                {
                    var plug = PluginRegistry.Create("vis_avs") as IAvsHostPlugin;
                    if (plug != null && plug is IVisualizerPlugin visPlugin)
                    {
                        RenderSurfaceControl.SetPlugin(visPlugin);
                        plug.LoadPreset(avsContent);
                        
                        // Show success message
                        var statusText = this.FindControl<TextBlock>("LblTime");
                        if (statusText != null)
                        {
                            statusText.Text = "AVS preset loaded and executed from editor!";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to handle AVS content from editor: {ex.Message}");
                
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
}