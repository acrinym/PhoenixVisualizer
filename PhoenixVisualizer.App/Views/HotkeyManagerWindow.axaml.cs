using System.Collections.ObjectModel;
using System.Text.Json;
using PhoenixVisualizer.Services;
using PhoenixVisualizer.App.Models;

namespace PhoenixVisualizer.Views;

public partial class HotkeyManagerWindow : Window
{
    private readonly WinampHotkeyService _hotkeyService;
    private readonly ObservableCollection<HotkeyItem> _hotkeyItems;
    private HotkeyItem? _editingItem;

    public HotkeyManagerWindow(WinampHotkeyService hotkeyService)
    {
        _hotkeyService = hotkeyService;
        _hotkeyItems = new ObservableCollection<HotkeyItem>();
        
        AvaloniaXamlLoader.Load(this);
        WireUpEventHandlers();
        InitializeHotkeyList();
    }

    private void InitializeHotkeyList()
    {
        var descriptions = _hotkeyService.GetHotkeyDescriptions();
        _hotkeyItems.Clear();

        // Group hotkeys by category
        var coreHotkeys = new List<string> { "Y", "U", "Space", "F", "V", "B", "R", "Escape" };
        var enhancedHotkeys = new List<string> { "Ctrl+P", "Ctrl+M", "Ctrl+S", "Shift+S", "Shift+L", "Ctrl+A", "1", "2", "3" };
        var modifierHotkeys = new List<string> { "Ctrl+N", "Ctrl+P", "Ctrl+R", "Ctrl+F", "Ctrl+V", "Shift+R", "Shift+B", "Alt+Enter", "Alt+V" };

        foreach (var hotkey in descriptions)
        {
            string category = "Core";
            if (enhancedHotkeys.Contains(hotkey.Key))
                category = "Enhanced";
            else if (modifierHotkeys.Contains(hotkey.Key))
                category = "Modifier";
            else if (hotkey.Value.StartsWith("Custom:"))
                category = "Custom";

            _hotkeyItems.Add(new HotkeyItem
            {
                Key = hotkey.Key,
                Description = hotkey.Value,
                Category = category,
                CurrentBinding = hotkey.Key
            });
        }

        var hotkeyList = this.FindControl<ItemsControl>("HotkeyList");
        if (hotkeyList != null)
        {
            hotkeyList.ItemsSource = _hotkeyItems;
        }
    }

    private async void OnEditHotkey(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is HotkeyItem item)
        {
            _editingItem = item;
            var result = await ShowHotkeyInputDialog(item);
            
            if (result != null)
            {
                // Update the hotkey binding
                item.CurrentBinding = result;
                
                // Register the new binding with the service
                if (TryParseKeyGesture(result, out var key, out var modifiers))
                {
                    _hotkeyService.RegisterCustomBinding(item.Key, key, modifiers);
                }
                
                // Refresh the list
                InitializeHotkeyList();
            }
        }
    }

    private async Task<string?> ShowHotkeyInputDialog(HotkeyItem item)
    {
        var dialog = new Window
        {
            Title = $"Edit Hotkey: {item.Description}",
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var panel = new StackPanel { Margin = new Avalonia.Thickness(20) };
        
        var instructionText = new TextBlock
        {
            Text = $"Press the key combination for '{item.Description}':",
            Margin = new Avalonia.Thickness(0, 0, 0, 20),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };
        
        var keyDisplay = new TextBlock
        {
            Text = "Press keys...",
            FontFamily = "Consolas",
            FontSize = 16,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 0, 0, 20)
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 10
        };

        var okButton = new Button { Content = "OK", IsEnabled = false };
        var cancelButton = new Button { Content = "Cancel" };
        var resetButton = new Button { Content = "Reset to Default" };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(resetButton);

        panel.Children.Add(instructionText);
        panel.Children.Add(keyDisplay);
        panel.Children.Add(buttonPanel);

        dialog.Content = panel;

        string? result = null;
        var currentKeys = new List<string>();

        // Handle key events
        dialog.KeyDown += (s, e) =>
        {
            e.Handled = true;
            currentKeys.Clear();

            if (e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control))
                currentKeys.Add("Ctrl");
            if (e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Shift))
                currentKeys.Add("Shift");
            if (e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Alt))
                currentKeys.Add("Alt");

            // Add the main key (avoid modifier keys)
            var keyName = e.Key.ToString();
            if (!keyName.Contains("Control") && !keyName.Contains("Shift") && !keyName.Contains("Alt"))
            {
                currentKeys.Add(keyName);
            }

            if (currentKeys.Count > 0)
            {
                keyDisplay.Text = string.Join("+", currentKeys);
                okButton.IsEnabled = true;
            }
        };

        okButton.Click += (s, e) =>
        {
            if (currentKeys.Count > 0)
            {
                result = string.Join("+", currentKeys);
                dialog.Close();
            }
        };

        cancelButton.Click += (s, e) => dialog.Close();
        
        resetButton.Click += (s, e) =>
        {
            // Reset to default binding
            var defaultBinding = GetDefaultBinding(item.Key);
            if (defaultBinding != null)
            {
                result = defaultBinding;
                dialog.Close();
            }
        };

        await dialog.ShowDialog(this);
        return result;
    }

    private string? GetDefaultBinding(string key)
    {
        // Return the default binding for common hotkeys
        return key switch
        {
            "Y" => "Y",
            "U" => "U",
            "Space" => "Space",
            "F" => "F",
            "V" => "V",
            "B" => "B",
            "R" => "R",
            "Escape" => "Escape",
            _ => null
        };
    }

    private bool TryParseKeyGesture(string gestureString, out Avalonia.Input.Key key, out Avalonia.Input.KeyModifiers modifiers)
    {
        key = Avalonia.Input.Key.None;
        modifiers = Avalonia.Input.KeyModifiers.None;

        try
        {
            var parts = gestureString.Split('+');
            if (parts.Length == 0) return false;

            var keyPart = parts[parts.Length - 1];

            for (int i = 0; i < parts.Length - 1; i++)
            {
                var modifier = parts[i].ToLower();
                switch (modifier)
                {
                    case "ctrl":
                    case "control":
                        modifiers |= Avalonia.Input.KeyModifiers.Control;
                        break;
                    case "shift":
                        modifiers |= Avalonia.Input.KeyModifiers.Shift;
                        break;
                    case "alt":
                        modifiers |= Avalonia.Input.KeyModifiers.Alt;
                        break;
                }
            }

            if (Enum.TryParse<Avalonia.Input.Key>(keyPart, true, out var parsedKey))
            {
                key = parsedKey;
                return true;
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return false;
    }

    private void OnResetToDefaults(object? sender, RoutedEventArgs e)
    {
        _hotkeyService.ResetToDefaults();
        InitializeHotkeyList();
    }

    private async void OnExportBindings(object? sender, RoutedEventArgs e)
    {
        try
        {
            var file = await this.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Hotkey Bindings",
                DefaultExtension = "json",
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } }
                }
            });

            if (file != null)
            {
                var bindings = new Dictionary<string, string>();
                foreach (var item in _hotkeyItems)
                {
                    bindings[item.Key] = item.CurrentBinding;
                }

                var json = JsonSerializer.Serialize(bindings, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(file.Path.LocalPath, json);
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog("Export Failed", $"Failed to export hotkey bindings: {ex.Message}");
        }
    }

    private async void OnImportBindings(object? sender, RoutedEventArgs e)
    {
        try
        {
            var files = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import Hotkey Bindings",
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } }
                }
            });

            if (files.Count > 0)
            {
                var file = files[0];
                var json = await File.ReadAllTextAsync(file.Path.LocalPath);
                var bindings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                if (bindings != null)
                {
                    foreach (var binding in bindings)
                    {
                        if (TryParseKeyGesture(binding.Value, out var key, out var modifiers))
                        {
                            _hotkeyService.RegisterCustomBinding(binding.Key, key, modifiers);
                        }
                    }

                    InitializeHotkeyList();
                }
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog("Import Failed", $"Failed to import hotkey bindings: {ex.Message}");
        }
    }

    private void OnClearCustomBindings(object? sender, RoutedEventArgs e)
    {
        _hotkeyService.ResetToDefaults();
        InitializeHotkeyList();
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async Task ShowErrorDialog(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var panel = new StackPanel { Margin = new Avalonia.Thickness(20) };
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        
        var okButton = new Button { Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Avalonia.Thickness(0, 20, 0, 0) };
        okButton.Click += (s, e) => dialog.Close();
        panel.Children.Add(okButton);

        dialog.Content = panel;
        await dialog.ShowDialog(this);
    }

    private void WireUpEventHandlers()
    {
        // Wire up button click events
        var btnResetToDefaults = this.FindControl<Button>("BtnResetToDefaults");
        var btnExportBindings = this.FindControl<Button>("BtnExportBindings");
        var btnImportBindings = this.FindControl<Button>("BtnImportBindings");
        var btnClearCustomBindings = this.FindControl<Button>("BtnClearCustomBindings");
        var btnClose = this.FindControl<Button>("BtnClose");

        if (btnResetToDefaults != null) btnResetToDefaults.Click += OnResetToDefaults;
        if (btnExportBindings != null) btnExportBindings.Click += OnExportBindings;
        if (btnImportBindings != null) btnImportBindings.Click += OnImportBindings;
        if (btnClearCustomBindings != null) btnClearCustomBindings.Click += OnClearCustomBindings;
        if (btnClose != null) btnClose.Click += OnClose;
    }
}


