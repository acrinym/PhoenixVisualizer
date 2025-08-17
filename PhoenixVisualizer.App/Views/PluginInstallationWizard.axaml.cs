using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PhoenixVisualizer.Views
{
    public partial class PluginInstallationWizard : Window
    {
        private string _selectedPluginType = string.Empty;



        public PluginInstallationWizard()
        {
            InitializeComponent();
            // Set initial selection
            if (PluginTypeList != null)
            {
                PluginTypeList.SelectedIndex = 0;
            }
        }

        private void OnPluginTypeChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (PluginTypeList.SelectedItem is ListBoxItem item && item.Tag is string tag)
            {
                _selectedPluginType = tag;
                UpdatePluginInfo();
            }
        }

        private void UpdatePluginInfo()
        {
            if (SelectedTypeText == null || InstallDirText == null || RequirementsText == null || InstallButton == null)
                return;

            switch (_selectedPluginType)
            {
                case "winamp":
                    SelectedTypeText.Text = "Winamp Visualizer Plugins (.dll)";
                    InstallDirText.Text = "plugins/vis/";
                    RequirementsText.Text = "Windows DLL, Winamp SDK compatible";
                    break;
                case "ape":
                    SelectedTypeText.Text = "APE Effect Plugins (.ape)";
                    InstallDirText.Text = "plugins/ape/";
                    RequirementsText.Text = "APE format, Winamp compatible";
                    break;
                case "avs":
                    SelectedTypeText.Text = "AVS Preset Files (.avs)";
                    InstallDirText.Text = "presets/avs/";
                    RequirementsText.Text = "AVS script format";
                    break;
                case "milkdrop":
                    SelectedTypeText.Text = "MilkDrop Preset Files (.milk)";
                    InstallDirText.Text = "presets/milkdrop/";
                    RequirementsText.Text = "MilkDrop preset format";
                    break;
            }
            InstallButton.IsEnabled = true;
        }

        private async void OnScanForPlugins(object? sender, RoutedEventArgs e)
        {
            if (StatusText == null) return;
            
            StatusText.Text = "Scanning for plugins...";
            await Task.Delay(1000); // Simulate scanning
            
            var foundCount = await ScanDirectoryForPlugins();
            if (StatusText != null)
            {
                StatusText.Text = $"Found {foundCount} plugins in system";
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

        private async void OnInstallFromFile(object? sender, RoutedEventArgs e)
        {
            if (StatusText == null) return;
            
            // Use modern file picker API
            var options = new FilePickerOpenOptions
            {
                Title = "Select Plugin File",
                AllowMultiple = false
            };

            var file = await StorageProvider.OpenFilePickerAsync(options);
            if (file.Count > 0)
            {
                await InstallPluginFromFile(file[0].Path.LocalPath);
            }
        }



        private async Task InstallPluginFromFile(string filePath)
        {
            if (StatusText == null) return;
            
            try
            {
                StatusText.Text = "Installing plugin...";
                
                var fileName = Path.GetFileName(filePath);
                var targetDir = GetTargetDirectory();
                var targetPath = Path.Combine(targetDir, fileName);
                
                // Ensure target directory exists
                Directory.CreateDirectory(targetDir);
                
                // Copy file
                File.Copy(filePath, targetPath, true);
                
                StatusText.Text = $"Plugin installed: {fileName}";
                await Task.Delay(2000);
                StatusText.Text = "Ready to install plugins";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Installation failed: {ex.Message}";
            }
        }

        private string GetTargetDirectory()
        {
            return _selectedPluginType switch
            {
                "winamp" => "plugins/vis",
                "ape" => "plugins/ape",
                "avs" => "presets/avs",
                "milkdrop" => "presets/milkdrop",
                _ => "plugins"
            };
        }

        private async void OnDownloadSample(object? sender, RoutedEventArgs e)
        {
            if (StatusText == null) return;
            
            StatusText.Text = "Downloading sample plugin...";
            await Task.Delay(1500); // Simulate download
            
            // Create a sample plugin file
            var sampleContent = GetSamplePluginContent();
            var targetDir = GetTargetDirectory();
            var samplePath = Path.Combine(targetDir, $"sample_{_selectedPluginType}.txt");
            
            try
            {
                Directory.CreateDirectory(targetDir);
                await File.WriteAllTextAsync(samplePath, sampleContent);
                StatusText.Text = "Sample plugin downloaded successfully!";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Failed to create sample: {ex.Message}";
            }
        }

        private string GetSamplePluginContent()
        {
            return _selectedPluginType switch
            {
                "winamp" => "// Sample Winamp Visualizer Plugin\n// This is a placeholder for a real .dll file",
                "ape" => "// Sample APE Effect\n// This is a placeholder for a real .ape file",
                "avs" => "// Sample AVS Preset\n// This is a placeholder for a real .avs file",
                "milkdrop" => "// Sample MilkDrop Preset\n// This is a placeholder for a real .milk file",
                _ => "// Sample Plugin\n// This is a placeholder"
            };
        }

        private async void OnInstallPlugin(object? sender, RoutedEventArgs e)
        {
            if (StatusText == null) return;
            
            if (string.IsNullOrEmpty(_selectedPluginType))
            {
                StatusText.Text = "Please select a plugin type first";
                return;
            }

            StatusText.Text = "Starting plugin installation...";
            await Task.Delay(1000);
            
            // Open file dialog for installation
            OnInstallFromFile(sender, e);
        }

        private void OnHelp(object? sender, RoutedEventArgs e)
        {
            if (StatusText == null) return;
            
            StatusText.Text = "Help: Select a plugin type and use Install from File to add plugins";
        }

        private void OnClose(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
