using Avalonia.Controls.Primitives;

using PhoenixVisualizer.Core.Config;
using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Rendering;

namespace PhoenixVisualizer.Views;

public partial class SettingsWindow : Window
{
    // Public settings snapshot (matches your previous fields)
    public string SelectedPlugin     { get; private set; } = "avs";
    public int    SampleRate         { get; private set; } = 44100;
    public int    BufferSize         { get; private set; } = 512;
    public bool   EnableVsync        { get; private set; } = true;
    public bool   StartFullscreen    { get; private set; } = false;
    public bool   AutoHideUI         { get; private set; } = true;

    // Visualizer settings 📊
    private readonly VisualizerSettings _vz = VisualizerSettings.Load();

    // Named controls (must match XAML x:Name)
    private RadioButton? AvsRadioControl        => this.FindControl<RadioButton>("AvsRadio");
    private RadioButton? PhoenixRadioControl    => this.FindControl<RadioButton>("PhoenixRadio");
    private ComboBox?    SampleRateComboControl => this.FindControl<ComboBox>("SampleRateCombo");
    private ComboBox?    BufferSizeComboControl => this.FindControl<ComboBox>("BufferSizeCombo");
    private CheckBox?    VsyncCheckControl      => this.FindControl<CheckBox>("VsyncCheck");
    private CheckBox?    FullscreenCheckControl => this.FindControl<CheckBox>("FullscreenCheck");
    private CheckBox?    AutoHideUICheckControl => this.FindControl<CheckBox>("AutoHideUICheck");

    // Plugin Manager controls
    private ListBox?     PluginListBoxControl       => this.FindControl<ListBox>("PluginListBox");
    private Border?      PluginDetailsPanelControl  => this.FindControl<Border>("PluginDetailsPanel");
    private TextBlock?   PluginNameTextControl      => this.FindControl<TextBlock>("PluginNameText");
    private TextBlock?   PluginDescriptionTextControl => this.FindControl<TextBlock>("PluginDescriptionText");
    private TextBlock?   PluginStatusTextControl    => this.FindControl<TextBlock>("PluginStatusText");
    private Button?      BtnConfigurePluginControl  => this.FindControl<Button>("BtnConfigurePlugin");
    private Button?      BtnTestPluginControl       => this.FindControl<Button>("BtnTestPlugin");
    private Button?      BtnPluginInfoControl       => this.FindControl<Button>("BtnPluginInfo");
    private TextBox?     PluginPathTextBox          => this.FindControl<TextBox>("PluginPathTextBox");

    public SettingsWindow()
    {
        InitializeComponent();

        // Wire up button event handlers
        WireUpEventHandlers();

        // OPTIONAL: if you actually have a ViewModel type, you can set it here.
        // DataContext = new ViewModels.SettingsWindowViewModel();

        // Sync current fields -> UI controls
        LoadCurrentSettings();
        LoadVisualizerSettings();
        
        // Initialize plugin list
        RefreshPluginList();
    }

    private void WireUpEventHandlers()
    {
        // Wire up button click events
        var btnBrowsePlugin = this.FindControl<Button>("BtnBrowsePlugin");
        var btnInstallPlugin = this.FindControl<Button>("BtnInstallPlugin");
        var btnInstallationWizard = this.FindControl<Button>("BtnInstallationWizard");
        var btnPresetManager = this.FindControl<Button>("BtnPresetManager");
        var btnRefreshPlugins = this.FindControl<Button>("BtnRefreshPlugins");
        var btnConfigurePlugin = this.FindControl<Button>("BtnConfigurePlugin");
        var btnTestPlugin = this.FindControl<Button>("BtnTestPlugin");
        var btnPluginInfo = this.FindControl<Button>("BtnPluginInfo");
        var btnPerformanceMonitor = this.FindControl<Button>("BtnPerformanceMonitor");
        var btnCancel = this.FindControl<Button>("BtnCancel");
        var btnApply = this.FindControl<Button>("BtnApply");

        if (btnBrowsePlugin != null) btnBrowsePlugin.Click += BrowseForPlugin;
        if (btnInstallPlugin != null) btnInstallPlugin.Click += InstallPlugin;
        if (btnInstallationWizard != null) btnInstallationWizard.Click += OnInstallationWizardClick;
        if (btnPresetManager != null) btnPresetManager.Click += OnPresetManagerClick;
        if (btnRefreshPlugins != null) btnRefreshPlugins.Click += OnRefreshPluginsClick;
        if (btnConfigurePlugin != null) btnConfigurePlugin.Click += OnConfigurePluginClick;
        if (btnTestPlugin != null) btnTestPlugin.Click += OnTestPluginClick;
        if (btnPluginInfo != null) btnPluginInfo.Click += OnPluginInfoClick;
        if (btnPerformanceMonitor != null) btnPerformanceMonitor.Click += OnPerformanceMonitorClick;
        if (btnCancel != null) btnCancel.Click += OnCancelClick;
        if (btnApply != null) btnApply.Click += OnApplyClick;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // Wire to Button Clicks in XAML
    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        SaveSettingsFromUI();
        SaveVisualizerSettings();
        Close();
    }

    private void LoadCurrentSettings()
    {
        // Radios
        if (SelectedPlugin == "phoenix") { PhoenixRadioControl?.SetCurrentValue(RadioButton.IsCheckedProperty, true); }
        else                             { AvsRadioControl?.SetCurrentValue(RadioButton.IsCheckedProperty, true); }

        // SampleRate
        if (SampleRateComboControl is not null)
        {
            SampleRateComboControl.SelectedIndex = SampleRate switch
            {
                22050 => 0,
                44100 => 1,
                48000 => 2,
                96000 => 3,
                _     => 1
            };
        }

        // BufferSize
        if (BufferSizeComboControl is not null)
        {
            BufferSizeComboControl.SelectedIndex = BufferSize switch
            {
                256  => 0,
                512  => 1,
                1024 => 2,
                2048 => 3,
                _    => 1
            };
        }

        VsyncCheckControl?.SetCurrentValue(CheckBox.IsCheckedProperty,      EnableVsync);
        FullscreenCheckControl?.SetCurrentValue(CheckBox.IsCheckedProperty, StartFullscreen);
        AutoHideUICheckControl?.SetCurrentValue(CheckBox.IsCheckedProperty, AutoHideUI);
    }

    private void SaveSettingsFromUI()
    {
        SelectedPlugin = PhoenixRadioControl?.IsChecked == true ? "phoenix" : "avs";

        if (SampleRateComboControl is not null)
        {
            SampleRate = SampleRateComboControl.SelectedIndex switch
            {
                0 => 22050,
                1 => 44100,
                2 => 48000,
                3 => 96000,
                _ => 44100
            };
        }

        if (BufferSizeComboControl is not null)
        {
            BufferSize = BufferSizeComboControl.SelectedIndex switch
            {
                0 => 256,
                1 => 512,
                2 => 1024,
                3 => 2048,
                _ => 512
            };
        }

        EnableVsync     = VsyncCheckControl?.IsChecked      ?? true;
        StartFullscreen = FullscreenCheckControl?.IsChecked ?? false;
        AutoHideUI      = AutoHideUICheckControl?.IsChecked ?? true;
    }

    // --- Visualizer settings helpers ---
    private void LoadVisualizerSettings()
    {
        // sliders + labels
        if (GainSlider is { } gs && GainLabel is { }) { gs.Value = _vz.InputGainDb; GainLabel.Text = $"{_vz.InputGainDb:0.#} dB"; }
        if (SmoothSlider is { } ss && SmoothLabel is { }) { ss.Value = _vz.SmoothingMs; SmoothLabel.Text = $"{_vz.SmoothingMs:0}"; }
        if (GateSlider is { } gts && GateLabel is { }) { gts.Value = _vz.NoiseGateDb; GateLabel.Text = $"{_vz.NoiseGateDb:0}"; }
        if (BeatSlider is { } bs && BeatLabel is { }) { bs.Value = _vz.BeatSensitivityOrDefault(); BeatLabel.Text = $"{_vz.BeatSensitivity:0.00}×"; }
        if (BlendSlider is { } bls && BlendLabel is { }) { bls.Value = _vz.FrameBlend; BlendLabel.Text = $"{_vz.FrameBlend:0.00}"; }
        if (FftCombo is { }) FftCombo.SelectedIndex = _vz.FftSize == 1024 ? 0 : 1;
        if (ScaleCombo is { })
            ScaleCombo.SelectedIndex = _vz.SpectrumScale switch { SpectrumScale.Linear => 0, SpectrumScale.Log => 1, _ => 2 };
        if (AutoGainCheck is { }) AutoGainCheck.IsChecked = _vz.AutoGain;
        if (PeaksCheck is { }) PeaksCheck.IsChecked = _vz.ShowPeaks;
        if (RandomOnBeatCheck is { }) RandomOnBeatCheck.IsChecked = _vz.RandomPresetMode == RandomPresetMode.OnBeat;
        if (HotkeysCheck is { }) HotkeysCheck.IsChecked = _vz.EnableHotkeys;

        if (RandModeCombo is { })
            RandModeCombo.SelectedIndex = _vz.RandomPresetMode switch
            {
                RandomPresetMode.Off => 0,
                RandomPresetMode.OnBeat => 1,
                RandomPresetMode.Interval => 2,
                _ => 3
            };
        if (RandIntervalCombo is { })
            RandIntervalCombo.SelectedIndex = _vz.RandomPresetIntervalSeconds switch { <=15 => 0, <=30 => 1, _ => 2 };
        if (BeatsPerBarCombo is { }) BeatsPerBarCombo.SelectedIndex = _vz.BeatsPerBar == 3 ? 1 : 0;
        if (BarsPerStanzaCombo is { })
            BarsPerStanzaCombo.SelectedIndex = _vz.StanzaBars switch { <=8 => 0, <=16 => 1, <=32 => 2, _ => 3 };
        if (RandomWhenSilentCheck is { }) RandomWhenSilentCheck.IsChecked = _vz.RandomWhenSilent;
        if (RandCooldownUpDown is { }) RandCooldownUpDown.Value = _vz.RandomPresetCooldownMs;

        UpdateRandomPanels();

        // label updates on change
        if (GainSlider != null && GainLabel != null)
            GainSlider.PropertyChanged += (_, __) => GainLabel.Text = $"{GainSlider.Value:0.#} dB";
        if (SmoothSlider != null && SmoothLabel != null)
            SmoothSlider.PropertyChanged += (_, __) => SmoothLabel.Text = $"{SmoothSlider.Value:0}";
        if (GateSlider != null && GateLabel != null)
            GateSlider.PropertyChanged += (_, __) => GateLabel.Text = $"{GateSlider.Value:0}";
        if (BeatSlider != null && BeatLabel != null)
            BeatSlider.PropertyChanged += (_, __) => BeatLabel.Text = $"{BeatSlider.Value:0.00}×";
        if (BlendSlider != null && BlendLabel != null)
            BlendSlider.PropertyChanged += (_, __) => BlendLabel.Text = $"{BlendSlider.Value:0.00}";
        if (RandModeCombo != null) RandModeCombo.SelectionChanged += (_, __) => UpdateRandomPanels();
    }

    private void UpdateRandomPanels()
    {
        int mode = RandModeCombo?.SelectedIndex ?? 0;
        if (RandIntervalPanel is not null) RandIntervalPanel.IsVisible = mode == 2;
        if (RandStanzaPanel is not null) RandStanzaPanel.IsVisible = mode == 3;
    }

    private void SaveVisualizerSettings()
    {
        _vz.InputGainDb = (float)(GainSlider?.Value ?? 0);
        _vz.SmoothingMs = (float)(SmoothSlider?.Value ?? 0);
        _vz.NoiseGateDb = (float)(GateSlider?.Value ?? -60);
        _vz.BeatSensitivity = (float)(BeatSlider?.Value ?? 1.35f);
        _vz.FrameBlend = (float)(BlendSlider?.Value ?? 0.25f);
        _vz.FftSize = FftCombo?.SelectedIndex == 0 ? 1024 : 2048;
        _vz.SpectrumScale = ScaleCombo?.SelectedIndex switch
        {
            0 => SpectrumScale.Linear,
            1 => SpectrumScale.Log,
            _ => SpectrumScale.Sqrt
        };
        _vz.AutoGain = AutoGainCheck?.IsChecked ?? true;
        _vz.ShowPeaks = PeaksCheck?.IsChecked ?? true;
        _vz.EnableHotkeys = HotkeysCheck?.IsChecked ?? true;

        // random preset mode
        _vz.RandomPresetMode = RandModeCombo?.SelectedIndex switch
        {
            1 => RandomPresetMode.OnBeat,
            2 => RandomPresetMode.Interval,
            3 => RandomPresetMode.Stanza,
            _ => RandomPresetMode.Off
        };
        _vz.RandomPresetIntervalSeconds = RandIntervalCombo?.SelectedIndex switch
        {
            0 => 15,
            1 => 30,
            _ => 60
        };
        _vz.BeatsPerBar = BeatsPerBarCombo?.SelectedIndex == 1 ? 3 : 4;
        _vz.StanzaBars = BarsPerStanzaCombo?.SelectedIndex switch
        {
            0 => 8,
            1 => 16,
            2 => 32,
            _ => 64
        };
        _vz.RandomWhenSilent = RandomWhenSilentCheck?.IsChecked ?? false;
        _vz.RandomPresetCooldownMs = (int)(RandCooldownUpDown?.Value ?? 800);

        // legacy toggle from checkbox
        if (RandomOnBeatCheck?.IsChecked == true && _vz.RandomPresetMode == RandomPresetMode.Off)
            _vz.RandomPresetMode = RandomPresetMode.OnBeat;

        _vz.Save();
    }

    #region Plugin Management

    private void OnRefreshPluginsClick(object? sender, RoutedEventArgs e)
    {
        RefreshPluginList();
    }

    private void OnPluginSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (PluginListBoxControl?.SelectedItem is PluginInfo plugin)
        {
            ShowPluginDetails(plugin);
        }
        else
        {
            HidePluginDetails();
        }
    }

    private void OnPluginEnabledChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is PluginInfo plugin)
        {
            plugin.IsEnabled = checkBox.IsChecked ?? false;
            UpdatePluginStatus(plugin);
        }
    }

    private void OnConfigurePluginClick(object? sender, RoutedEventArgs e)
    {
        if (PluginListBoxControl?.SelectedItem is PluginInfo plugin)
        {
            ConfigurePlugin(plugin);
        }
    }

    private void OnTestPluginClick(object? sender, RoutedEventArgs e)
    {
        if (PluginListBoxControl?.SelectedItem is PluginInfo plugin)
        {
            TestPlugin(plugin);
        }
    }

    private void OnPluginInfoClick(object? sender, RoutedEventArgs e)
    {
        if (PluginListBoxControl?.SelectedItem is PluginInfo plugin)
        {
            ShowPluginInfo(plugin);
        }
    }

    private async void BrowseForPlugin(object? sender, RoutedEventArgs e)
    {
        try
        {
            var options = new FilePickerOpenOptions
            {
                Title = "Select Plugin File",
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new("Plugin files") { Patterns = new[] { "*.dll" } },
                    new("All files") { Patterns = new[] { "*.*" } }
                }
            };

            var files = await this.StorageProvider.OpenFilePickerAsync(options);
            if (files.Count == 0) return;

            var selectedFile = files[0];
            
            // Update the plugin path text box
            var pluginPathTextBox = this.FindControl<TextBox>("PluginPathTextBox");
            if (pluginPathTextBox != null)
            {
                pluginPathTextBox.Text = selectedFile.Path.LocalPath;
            }
            
            // Validate the plugin file
            if (ValidatePluginFile(selectedFile.Path.LocalPath))
            {
                ShowStatusMessage($"Plugin file selected: {selectedFile.Name}");
            }
            else
            {
                ShowStatusMessage("Warning: Selected file may not be a valid plugin");
            }
        }
        catch (Exception ex)
        {
            ShowStatusMessage($"Error browsing for plugin: {ex.Message}");
        }
    }

    private bool ValidatePluginFile(string filePath)
    {
        try
        {
            // Basic validation - check if it's a .NET assembly
            var assembly = System.Reflection.Assembly.LoadFrom(filePath);
            
            // Check if it implements required plugin interfaces
            var pluginTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && 
                           (t.GetInterfaces().Any(i => i.Name.Contains("IPlugin") || 
                                                      i.Name.Contains("IVisualizerPlugin"))))
                .ToList();
            
            return pluginTypes.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private async void InstallPlugin(object? sender, RoutedEventArgs e)
    {
        try
        {
            var pluginPathTextBox = this.FindControl<TextBox>("PluginPathTextBox");
            if (pluginPathTextBox == null || string.IsNullOrWhiteSpace(pluginPathTextBox.Text))
            {
                ShowStatusMessage("Please select a plugin file first");
                return;
            }
            
            var sourcePath = pluginPathTextBox.Text;
            if (!File.Exists(sourcePath))
            {
                ShowStatusMessage("Selected plugin file does not exist");
                return;
            }
            
            // Get the plugins directory
            var pluginsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            if (!Directory.Exists(pluginsDir))
            {
                Directory.CreateDirectory(pluginsDir);
            }
            
            // Copy plugin to plugins directory
            var fileName = Path.GetFileName(sourcePath);
            var targetPath = Path.Combine(pluginsDir, fileName);
            
            // Check if plugin already exists
            if (File.Exists(targetPath))
            {
                var result = ShowConfirmationDialog(
                    "Plugin already exists. Do you want to replace it?",
                    "Plugin Installation");
                
                if (result != true)
                    return;
            }
            
            // Copy the file
            File.Copy(sourcePath, targetPath, true);
            
            // Update plugin registry
            await RefreshPluginRegistry();
            
            ShowStatusMessage($"Plugin installed successfully: {fileName}");
            
            // Clear the text box
            pluginPathTextBox.Text = "";
        }
        catch (Exception ex)
        {
            ShowStatusMessage($"Error installing plugin: {ex.Message}");
        }
    }

    private async Task RefreshPluginRegistry()
    {
        try
        {
            // This would typically call the plugin manager to refresh
            // Plugin registry refresh requested
            await Task.CompletedTask;
        }
        catch
        {
            // Error refreshing plugin registry silently
        }
    }
    
            private void OnPerformanceMonitorClick(object? sender, RoutedEventArgs e)
        {
            ShowPerformancePanel();
        }

        private void OnInstallationWizardClick(object? sender, RoutedEventArgs e)
        {
            var wizard = new PluginInstallationWizard();
            wizard.Show(this);
        }

        private void OnPresetManagerClick(object? sender, RoutedEventArgs e)
        {
            var presetManager = new PresetManager();
            presetManager.Show(this);
        }

    private void RefreshPluginList()
    {
        try
        {
                    var plugins = PluginRegistry.AvailablePlugins;
        var pluginInfos = plugins.Select(p => new PluginInfo
        {
            Id = p.Id,
            DisplayName = p.DisplayName,
            Description = p.Description,
            IsEnabled = p.IsEnabled
        }).ToList();

            PluginListBoxControl?.SetCurrentValue(ListBox.ItemsSourceProperty, pluginInfos);
        }
        catch (Exception)
        {
            // TODO: Show error message to user
            // Error logged silently - consider showing user-friendly message
        }
    }

    private void ShowPluginDetails(PluginInfo plugin)
    {
        if (PluginDetailsPanelControl != null)
        {
            PluginDetailsPanelControl.IsVisible = true;
        }

        if (PluginNameTextControl != null)
        {
            PluginNameTextControl.Text = plugin.DisplayName;
        }

        if (PluginDescriptionTextControl != null)
        {
            PluginDescriptionTextControl.Text = plugin.Description;
        }

        UpdatePluginStatus(plugin);
    }

    private void HidePluginDetails()
    {
        if (PluginDetailsPanelControl != null)
        {
            PluginDetailsPanelControl.IsVisible = false;
        }
    }

    private void UpdatePluginStatus(PluginInfo plugin)
    {
        if (PluginStatusTextControl != null)
        {
            var status = plugin.IsEnabled ? "Enabled" : "Disabled";
            PluginStatusTextControl.Text = status;
        }
    }

    private void ConfigurePlugin(PluginInfo plugin)
    {
        try
        {
            var pluginInstance = PluginRegistry.Create(plugin.Id);
            if (pluginInstance is IVisualizerPlugin visualizerPluginPlugin)
            {
                // Try to call Configure if the plugin supports it
                if (pluginInstance is IAvsHostPlugin avsPlugin)
                {
                    avsPlugin.Configure();
                }
                else
                {
                    // Show a simple configuration dialog
                    ShowSimpleConfigDialog(plugin);
                }
            }
        }
        catch (Exception ex)
        {
            ShowErrorDialog($"Error configuring plugin: {ex.Message}");
        }
    }

    private void ShowSimpleConfigDialog(PluginInfo plugin)
    {
        // Create a simple configuration dialog
        var dialog = new Window
        {
            Title = $"Configure {plugin.DisplayName}",
            Width = 400,
            Height = 300,
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
            Text = $"Plugin: {plugin.DisplayName}",
            FontSize = 16,
            FontWeight = FontWeight.Bold
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"ID: {plugin.Id}",
            FontSize = 12
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"Description: {plugin.Description}",
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        });

        var enabledCheckBox = new CheckBox
        {
            Content = "Enable Plugin",
            IsChecked = plugin.IsEnabled
        };

        enabledCheckBox.IsCheckedChanged += (_, _) => 
        {
            if (enabledCheckBox.IsChecked == true)
            {
                plugin.IsEnabled = true;
                PluginRegistry.SetPluginEnabled(plugin.Id, true);
            }
            else
            {
                plugin.IsEnabled = false;
                PluginRegistry.SetPluginEnabled(plugin.Id, false);
            }
        };

        panel.Children.Add(enabledCheckBox);

        var closeButton = new Button
        {
            Content = "Close",
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 20, 0, 0)
        };

        closeButton.Click += (_, _) => dialog.Close();
        panel.Children.Add(closeButton);

        dialog.Content = panel;
        dialog.ShowDialog(this);
    }

    private void ShowErrorDialog(string message)
    {
        var dialog = new Window
        {
            Title = "Error",
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
            Text = "An error occurred:",
            FontSize = 14,
            FontWeight = FontWeight.Bold
        });

        panel.Children.Add(new TextBlock
        {
            Text = message,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        });

        var closeButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 20, 0, 0)
        };

        closeButton.Click += (_, _) => dialog.Close();
        panel.Children.Add(closeButton);

        dialog.Content = panel;
        dialog.ShowDialog(this);
    }
    
    /// <summary>
    /// Show plugin performance monitoring panel
    /// </summary>
    public void ShowPerformancePanel()
    {
        var dialog = new Window
        {
            Title = "Plugin Performance Monitor",
            Width = 800,
            Height = 600,
            CanResize = true,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var mainPanel = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 15
        };

        // Summary section
        var summaryPanel = new Border
        {
            BorderBrush = new SolidColorBrush(Colors.LightGray),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            Child = new TextBlock
            {
                Text = "Performance Summary",
                FontSize = 16,
                FontWeight = FontWeight.Bold
            }
        };
        mainPanel.Children.Add(summaryPanel);

        // Performance metrics list
        var metricsList = new ListBox
        {
            Height = 400
        };

        // Get performance data from the main window (if available)
        var mainWindow = this.Owner as MainWindow;
        if (mainWindow != null)
        {
            var renderSurface = mainWindow.FindControl<RenderSurface>("RenderHost");
            if (renderSurface != null)
            {
                var perfMonitor = renderSurface.GetPerformanceMonitor();
                var allMetrics = perfMonitor.GetAllMetrics().ToList();
                
                if (allMetrics.Any())
                {
                    // Update summary
                    summaryPanel.Child = new TextBlock
                    {
                        Text = perfMonitor.GetPerformanceSummary(),
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap
                    };
                    
                    // Create metrics items
                    var metricsItems = allMetrics.Select(m => new ListBoxItem
                    {
                        Content = CreateMetricsItem(m)
                    }).ToList();
                    
                    metricsList.ItemsSource = metricsItems;
                }
                else
                {
                    metricsList.ItemsSource = new[] { new ListBoxItem { Content = "No performance data available yet. Run some plugins first." } };
                }
            }
        }

        mainPanel.Children.Add(metricsList);

        // Buttons
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10
        };

        var refreshButton = new Button { Content = "Refresh" };
        refreshButton.Click += (_, _) => 
        {
            dialog.Close();
            ShowPerformancePanel(); // Refresh by reopening
        };

        var closeButton = new Button { Content = "Close" };
        closeButton.Click += (_, _) => dialog.Close();

        buttonPanel.Children.Add(refreshButton);
        buttonPanel.Children.Add(closeButton);
        mainPanel.Children.Add(buttonPanel);

        dialog.Content = mainPanel;
        dialog.ShowDialog(this);
    }
    
    private Control CreateMetricsItem(PluginPerformanceMetrics metrics)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(5),
            Spacing = 5
        };

        // Plugin name and status
        var headerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };

        headerPanel.Children.Add(new TextBlock
        {
            Text = metrics.PluginName,
            FontWeight = FontWeight.Bold,
            FontSize = 14
        });

        var statusText = new TextBlock
        {
            Text = $"({metrics.PerformanceStatus})",
            Foreground = new SolidColorBrush(metrics.IsPerformingWell ? Colors.Green : Colors.Red),
            FontSize = 12
        };
        headerPanel.Children.Add(statusText);

        panel.Children.Add(headerPanel);

        // Performance details
        var detailsPanel = new UniformGrid
        {
            Columns = 2,
            Margin = new Thickness(0, 5, 0, 0)
        };

        detailsPanel.Children.Add(new TextBlock { Text = $"Current FPS: {metrics.CurrentFps:F1}" });
        detailsPanel.Children.Add(new TextBlock { Text = $"Avg FPS: {metrics.AverageFps:F1}" });
        detailsPanel.Children.Add(new TextBlock { Text = $"Render Time: {metrics.LastRenderTimeMs:F2}ms" });
        detailsPanel.Children.Add(new TextBlock { Text = $"Avg Render: {metrics.AverageRenderTimeMs:F2}ms" });
        detailsPanel.Children.Add(new TextBlock { Text = $"Frames: {metrics.TotalFramesRendered}" });
        detailsPanel.Children.Add(new TextBlock { Text = $"Memory: {metrics.CurrentMemoryBytes / 1024 / 1024:F1}MB" });

        panel.Children.Add(detailsPanel);

        return panel;
    }

    private void TestPlugin(PluginInfo plugin)
    {
        try
        {
            var pluginInstance = PluginRegistry.Create(plugin.Id);
            if (pluginInstance is IVisualizerPlugin visualizerPlugin)
            {
                // TODO: Test the plugin with sample audio data
                // Plugin testing initiated
            }
        }
        catch (Exception)
        {
            // Error testing plugin - consider showing user-friendly message
        }
    }

    private void ShowPluginInfo(PluginInfo plugin)
    {
        // TODO: Show detailed plugin information dialog
        // Plugin info display initiated
    }

    private bool ShowConfirmationDialog(string message, string title)
    {
        // Simple confirmation using console for now
        // In a real implementation, you'd use a proper dialog
        return true; // Assume user confirms for now
    }

    private void ShowStatusMessage(string message)
    {
        // Use a simple status display instead of MessageBox
        var statusText = this.FindControl<TextBlock>("StatusText");
        if (statusText != null)
        {
            statusText.Text = message;
        }
        else
        {
            // Fallback to console output - status message not displayed
        }
    }

    #endregion
}

// Plugin information model for the UI
public class PluginInfo
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}
