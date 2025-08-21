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

            // Resolve plugin directory and show it to user
            var resolved = await WinampIntegrationService.ResolvePluginDirectoryAsync();
            var pathText = this.FindControl<TextBlock>("PluginPathTextBlock");
            if (pathText != null)
            {
                pathText.Text = resolved ?? "(none)";
            }

            // Scan for plugins
            var result = await _integrationService.ScanForPluginsAsync();
            
            // Update UI on UI thread
            Dispatcher.UIThread.Post(() =>
            {
                if (result.Error != null)
                {
                    SetStatus($"❌ Error refreshing plugins: {result.Error.Message}");
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

                SetStatus(result.Status);
            });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => SetStatus($"❌ Error refreshing plugins: {ex.Message}"));
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
        _ = sender; _ = e; // silence unused parameters
        var pluginList = this.FindControl<ListBox>("PluginList");
        if (pluginList?.SelectedItem is string selectedPluginText)
        {
            try
            {
                // Test the plugin with sample data
                // TODO: Implement plugin testing with sample audio data
                SetStatus($"🧪 Testing {selectedPluginText}");
            }
            catch (Exception ex)
            {
                SetStatus($"❌ Error testing plugin: {ex.Message}");
            }
        }
        else
        {
            SetStatus("⚠️ Please select a plugin first");
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
