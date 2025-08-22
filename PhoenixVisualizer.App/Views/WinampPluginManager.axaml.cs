using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.PluginHost.Services;

using System;
using System.Collections.Generic;

namespace PhoenixVisualizer.Views;

public partial class WinampPluginManager : Window
{
    private WinampIntegrationService? _integrationService;
    private readonly List<string> _plugins = new();
    private readonly List<SimpleWinampHost.LoadedPlugin> _loadedPlugins = new();
    
    // Event to notify main window when a plugin is selected
    public event Action<SimpleWinampHost.LoadedPlugin, int>? PluginSelected;
    
    public WinampPluginManager()
    {
        InitializeComponent();
        InitializeIntegrationService();
        RefreshPluginList();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeIntegrationService()
    {
        try
        {
            _integrationService = new WinampIntegrationService();
            SetStatus("✅ Winamp integration service initialized");
        }
        catch (Exception ex)
        {
            SetStatus($"❌ Failed to initialize integration service: {ex.Message}");
        }
    }

    private async void RefreshPluginList()
    {
        try
        {
            if (_integrationService == null)
            {
                SetStatus("❌ Integration service not initialized");
                return;
            }

            SetStatus("🔍 Resolving plugin directory...");
            
            // Resolve plugin directory and show it to user
            var resolved = await WinampIntegrationService.ResolvePluginDirectoryAsync();
            var pathText = this.FindControl<TextBlock>("PluginPathTextBlock");
            if (pathText != null)
            {
                pathText.Text = resolved ?? "(none found)";
            }

            if (string.IsNullOrWhiteSpace(resolved))
            {
                SetStatus("❌ No plugin directory found - check console for details");
                var countText1 = this.FindControl<TextBlock>("PluginCountTextBlock");
                if (countText1 != null)
                {
                    countText1.Text = "No plugin directory found";
                }
                return;
            }

            SetStatus("🔍 Scanning for plugins...");
            
            // Scan for plugins
            var result = await _integrationService.ScanForPluginsAsync();
            
            // Update UI on UI thread
            Dispatcher.UIThread.Post(() =>
            {
                if (result.Error != null)
                {
                    SetStatus($"❌ Error refreshing plugins: {result.Error.Message}");
                    var countText2 = this.FindControl<TextBlock>("PluginCountTextBlock");
                    if (countText2 != null)
                    {
                        countText2.Text = $"Error: {result.Error.Message}";
                    }
                    return;
                }

                _plugins.Clear();
                _loadedPlugins.Clear();
                foreach (var plugin in result.Plugins)
                {
                    _plugins.Add($"{plugin.FileName} - {plugin.Header.Description} ({plugin.Modules.Count} modules)");
                    _loadedPlugins.Add(plugin);
                }

                // Update UI
                var pluginList = this.FindControl<ListBox>("PluginList");
                if (pluginList != null)
                {
                    pluginList.ItemsSource = _plugins;
                }

                // Update plugin count
                var countText3 = this.FindControl<TextBlock>("PluginCountTextBlock");
                if (countText3 != null)
                {
                    countText3.Text = $"Found {result.Plugins.Count} plugins in {resolved}";
                }

                SetStatus(result.Status);
            });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => 
            {
                SetStatus($"❌ Error refreshing plugins: {ex.Message}");
                var countText = this.FindControl<TextBlock>("PluginCountTextBlock");
                if (countText != null)
                {
                    countText.Text = $"Exception: {ex.Message}";
                }
            });
        }
    }

    private void OnRefreshClick(object? sender, RoutedEventArgs e)
    {
        _ = sender; _ = e; // silence unused parameters
        RefreshPluginList();
    }

    private void OnSelectPluginClick(object? sender, RoutedEventArgs e)
    {
        _ = sender; _ = e; // silence unused parameters
        var pluginList = this.FindControl<ListBox>("PluginList");
        if (pluginList?.SelectedIndex >= 0 && pluginList.SelectedIndex < _loadedPlugins.Count)
        {
            try
            {
                var selectedPlugin = _loadedPlugins[pluginList.SelectedIndex];
                SetStatus($"✅ Selected plugin: {selectedPlugin.FileName}");
                
                // Fire event to notify main window
                PluginSelected?.Invoke(selectedPlugin, 0); // Use first module by default
                
                // Close the window
                this.Close();
            }
            catch (Exception ex)
            {
                SetStatus($"❌ Error selecting plugin: {ex.Message}");
            }
        }
        else
        {
            SetStatus("⚠️ Please select a plugin first");
        }
    }

    private void OnConfigureClick(object? sender, RoutedEventArgs e)
    {
        _ = sender; _ = e; // silence unused parameters
        var pluginList = this.FindControl<ListBox>("PluginList");
        if (pluginList?.SelectedItem is string selectedPluginText)
        {
            try
            {
                // Show plugin configuration
                // TODO: Implement plugin configuration dialog
                SetStatus($"⚙️ Configuring {selectedPluginText}");
            }
            catch (Exception ex)
            {
                SetStatus($"❌ Error configuring plugin: {ex.Message}");
            }
        }
        else
        {
            SetStatus("⚠️ Please select a plugin first");
        }
    }

    private void OnTestClick(object? sender, RoutedEventArgs e)
    {
        _ = sender; _ = e;
        // TODO: Implement plugin testing
        SetStatus("🧪 Plugin testing not yet implemented");
    }

    private async void OnDebugClick(object? sender, RoutedEventArgs e)
    {
        _ = sender; _ = e;
        
        try
        {
            if (_integrationService == null)
            {
                SetStatus("❌ Integration service not initialized");
                return;
            }

            SetStatus("🐛 Gathering debug information...");
            
            // Get detailed debug info
            var resolved = await WinampIntegrationService.ResolvePluginDirectoryAsync();
            var result = await _integrationService.ScanForPluginsAsync();
            
            var debugInfo = $"🔍 Plugin Directory: {resolved ?? "(none)"}\n";
            debugInfo += $"🔍 Integration Service Initialized: {_integrationService.IsInitialized}\n";
            debugInfo += $"🔍 Scan Result: {result.Status}\n";
            debugInfo += $"🔍 Plugins Found: {result.Plugins.Count}\n";
            
            if (result.Error != null)
            {
                debugInfo += $"❌ Error: {result.Error.Message}\n";
                debugInfo += $"❌ Stack Trace: {result.Error.StackTrace}\n";
            }
            
            // Show debug info in a simple dialog
            var debugWindow = new Window
            {
                Title = "🐛 Debug Information",
                Width = 600,
                Height = 400,
                Content = new ScrollViewer
                {
                    Content = new TextBox
                    {
                        Text = debugInfo,
                        IsReadOnly = true,
                        FontFamily = "Consolas",
                        FontSize = 11,
                        AcceptsReturn = true,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    }
                }
            };
            
            await debugWindow.ShowDialog(this);
            SetStatus("🐛 Debug information displayed");
        }
        catch (Exception ex)
        {
            SetStatus($"❌ Error getting debug info: {ex.Message}");
        }
    }

    private void SetStatus(string message)
    {
        var statusText = this.FindControl<TextBlock>("StatusText");
        if (statusText != null)
        {
            statusText.Text = message;
        }
    }



    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _integrationService?.Dispose();
    }
}
