using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.PluginHost.Services;
using PhoenixVisualizer.App.Utils;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PhoenixVisualizer.Views;

public partial class WinampPluginManager : Window
{
    private WinampIntegrationService? _integrationService;
    private List<string> _plugins = new();
    
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
            _integrationService.StatusChanged += OnStatusChanged;
            _integrationService.ErrorOccurred += OnErrorOccurred;
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

            // Scan for plugins
            var loadedPlugins = await _integrationService.ScanForPluginsAsync();

            _plugins.Clear();
            foreach (var plugin in loadedPlugins)
            {
                _plugins.Add($"{plugin.FileName} - {plugin.Header.Description} ({plugin.Modules.Count} modules)");
            }

            // Update UI
            var pluginList = this.FindControl<ListBox>("PluginList");
            if (pluginList != null)
            {
                pluginList.ItemsSource = _plugins;
            }

            SetStatus($"✅ Found {_plugins.Count} Winamp plugins");
        }
        catch (Exception ex)
        {
            SetStatus($"❌ Error refreshing plugins: {ex.Message}");
        }
    }

    private void OnRefreshClick(object? sender, RoutedEventArgs e)
    {
        RefreshPluginList();
    }

    private void OnSelectPluginClick(object? sender, RoutedEventArgs e)
    {
        var pluginList = this.FindControl<ListBox>("PluginList");
        if (pluginList?.SelectedItem is string selectedPluginText)
        {
            try
            {
                // For now, just show success message
                SetStatus($"✅ Selected plugin: {selectedPluginText}");
                
                // TODO: Integrate with main visualization system
                // For now, just close the window
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

    private void OnStatusChanged(string message)
    {
        SetStatus(message);
    }

    private void OnErrorOccurred(Exception ex)
    {
        SetStatus($"❌ Error: {ex.Message}");
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _integrationService?.Dispose();
    }
}
