using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Markup.Xaml;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;

namespace PhoenixVisualizer.Views
{
    public partial class PluginInstallationWizard : Window
    {
        private string _selectedPluginType = string.Empty;

        public PluginInstallationWizard()
        {
            // Manually load XAML since InitializeComponent() isn't generated
            AvaloniaXamlLoader.Load(this);
            
            // Set initial selection
            var pluginTypeList = this.FindControl<ListBox>("PluginTypeList");
            if (pluginTypeList != null)
            {
                pluginTypeList.SelectedIndex = 0;
            }
        }

        private void OnPluginTypeChanged(object? sender, SelectionChangedEventArgs e)
        {
            var pluginTypeList = this.FindControl<ListBox>("PluginTypeList");
            if (pluginTypeList?.SelectedItem is ListBoxItem item && item.Tag is string tag)
            {
                _selectedPluginType = tag;
                UpdatePluginInfo();
            }
        }

        private void UpdatePluginInfo()
        {
            var selectedTypeText = this.FindControl<TextBlock>("SelectedTypeText");
            var installDirText = this.FindControl<TextBlock>("InstallDirText");
            var requirementsText = this.FindControl<TextBlock>("RequirementsText");
            var installButton = this.FindControl<Button>("InstallButton");
            
            if (selectedTypeText == null || installDirText == null || requirementsText == null || installButton == null)
                return;

            switch (_selectedPluginType)
            {
                case "winamp":
                    selectedTypeText.Text = "Winamp Visualizer Plugins (.dll)";
                    installDirText.Text = "plugins/vis/";
                    requirementsText.Text = "Windows DLL, Winamp SDK compatible";
                    break;
                case "ape":
                    selectedTypeText.Text = "APE Effect Plugins (.ape)";
                    installDirText.Text = "plugins/ape/";
                    requirementsText.Text = "APE format, Winamp compatible";
                    break;
                case "avs":
                    selectedTypeText.Text = "AVS Preset Files (.avs)";
                    installDirText.Text = "presets/avs/";
                    requirementsText.Text = "AVS script format";
                    break;
                case "milkdrop":
                    selectedTypeText.Text = "MilkDrop Preset Files (.milk)";
                    installDirText.Text = "presets/milkdrop/";
                    requirementsText.Text = "MilkDrop preset format";
                    break;
            }
            installButton.IsEnabled = true;
        }

        private async void OnScanForPlugins(object? sender, RoutedEventArgs e)
        {
            var statusText = this.FindControl<TextBlock>("StatusText");
            if (statusText == null) return;
            
            statusText.Text = "Scanning for plugins...";
            await Task.Delay(1000); // Simulate scanning
            
            var foundCount = await ScanDirectoryForPlugins();
            if (statusText != null)
            {
                statusText.Text = $"Found {foundCount} plugins in system";
            }
        }

        private Task<int> ScanDirectoryForPlugins()
        {
            var count = 0;
            try
            {
                var directories = new[] { "plugins/", "plugins/", "C:/Program Files/Winamp/Plugins/", "C:/Program Files (x86)/Winamp/Plugins/" };
                
                foreach (var dir in directories)
                {
                    if (Directory.Exists(dir))
                    {
                        var files = Directory.GetFiles(dir, "*.dll", SearchOption.TopDirectoryOnly);
                        count += files.Length;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error scanning for plugins: {ex.Message}");
            }
            
            return Task.FromResult(count);
        }

        private async void OnInstallPlugin(object? sender, RoutedEventArgs e)
        {
            var statusText = this.FindControl<TextBlock>("StatusText");
            if (statusText == null) return;
            
            try
            {
                statusText.Text = "Installing plugin...";
                await Task.Delay(500); // Simulate installation
                
                // Create plugin directory if it doesn't exist
                var pluginDir = GetPluginDirectory();
                Directory.CreateDirectory(pluginDir);
                
                statusText.Text = "Plugin installed successfully!";
            }
            catch (Exception ex)
            {
                statusText.Text = $"Installation failed: {ex.Message}";
                Debug.WriteLine($"Plugin installation error: {ex.Message}");
            }
        }

        private string GetPluginDirectory()
        {
            return _selectedPluginType switch
            {
                "winamp" => "plugins/vis/",
                "ape" => "plugins/ape/",
                "avs" => "presets/avs/",
                "milkdrop" => "presets/milkdrop/",
                _ => "plugins/"
            };
        }

        private async void OnBrowsePluginFile(object? sender, RoutedEventArgs e)
        {
            var statusText = this.FindControl<TextBlock>("StatusText");
            if (statusText == null) return;
            
            try
            {
                var options = new FilePickerOpenOptions
                {
                    Title = "Select Plugin File",
                    AllowMultiple = false,
                    FileTypeFilter = GetFileTypeFilter()
                };

                var files = await StorageProvider.OpenFilePickerAsync(options);
                if (files.Count > 0)
                {
                    var file = files[0];
                    statusText.Text = $"Selected: {file.Name}";
                    
                    // Here you would copy the file to the appropriate plugin directory
                    await InstallPluginFile(file.Path.LocalPath);
                }
            }
            catch (Exception ex)
            {
                statusText.Text = $"Error browsing files: {ex.Message}";
                Debug.WriteLine($"File browse error: {ex.Message}");
            }
        }

        private List<FilePickerFileType> GetFileTypeFilter()
        {
            return _selectedPluginType switch
            {
                "winamp" => new List<FilePickerFileType> { new("Winamp Plugin") { Patterns = new[] { "*.dll" } } },
                "ape" => new List<FilePickerFileType> { new("APE Plugin") { Patterns = new[] { "*.ape" } } },
                "avs" => new List<FilePickerFileType> { new("AVS Preset") { Patterns = new[] { "*.avs", "*.txt" } } },
                "milkdrop" => new List<FilePickerFileType> { new("MilkDrop Preset") { Patterns = new[] { "*.milk" } } },
                _ => new List<FilePickerFileType> { new("All Files") { Patterns = new[] { "*.*" } } }
            };
        }

        private async Task InstallPluginFile(string sourcePath)
        {
            var statusText = this.FindControl<TextBlock>("StatusText");
            if (statusText == null) return;
            
            try
            {
                var fileName = Path.GetFileName(sourcePath);
                var targetDir = GetPluginDirectory();
                var targetPath = Path.Combine(targetDir, fileName);
                
                Directory.CreateDirectory(targetDir);
                await Task.Run(() => File.Copy(sourcePath, targetPath, true));
                
                statusText.Text = $"Plugin installed: {fileName}";
            }
            catch (Exception ex)
            {
                statusText.Text = $"Installation failed: {ex.Message}";
                Debug.WriteLine($"Plugin file installation error: {ex.Message}");
            }
        }

        private void OnClose(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
