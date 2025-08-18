using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Markup.Xaml;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using PhoenixVisualizer.Models;

namespace PhoenixVisualizer.Views
{
    public partial class PresetManager : Window
    {
        private string _selectedPresetType = "all";
        private List<PresetInfo> _allPresets = new();
        private PresetInfo? _selectedPreset;

        public PresetManager()
        {
            // Manually load XAML since InitializeComponent() isn't generated
            AvaloniaXamlLoader.Load(this);
            
            var presetTypeList = this.FindControl<ListBox>("PresetTypeList");
            if (presetTypeList != null)
            {
                presetTypeList.SelectedIndex = 0;
            }
            RefreshPresetList();
        }

        private void OnPresetTypeChanged(object? sender, SelectionChangedEventArgs e)
        {
            var presetTypeList = this.FindControl<ListBox>("PresetTypeList");
            if (presetTypeList?.SelectedItem is ListBoxItem item && item.Tag is string tag)
            {
                _selectedPresetType = tag;
                FilterPresets();
            }
        }

        private void OnPresetSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var presetListBox = this.FindControl<ListBox>("PresetListBox");
            if (presetListBox?.SelectedItem is PresetInfo preset)
            {
                _selectedPreset = preset;
                ShowPresetDetails(preset);
            }
        }

        private async void OnImportPreset(object? sender, RoutedEventArgs e)
        {
            var statusText = this.FindControl<TextBlock>("StatusText");
            if (statusText == null) return;

            var options = new FilePickerOpenOptions
            {
                Title = "Select Preset File",
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new("AVS Preset") { Patterns = new[] { "*.avs", "*.txt" } },
                    new("MilkDrop Preset") { Patterns = new[] { "*.milk" } },
                    new("All Files") { Patterns = new[] { "*.*" } }
                }
            };

            var files = await StorageProvider.OpenFilePickerAsync(options);
            if (files.Count > 0)
            {
                var file = files[0];
                var extension = Path.GetExtension(file.Path.LocalPath).ToLower();
                
                if (extension == ".avs" || extension == ".txt")
                {
                    // TODO: Implement AVS import functionality
                    statusText.Text = "AVS import functionality coming soon!";
                }
                else
                {
                    await ImportPresetFile(file.Path.LocalPath);
                }
            }
        }

        private async Task ImportPresetFile(string filePath)
        {
            var statusText = this.FindControl<TextBlock>("StatusText");
            if (statusText == null) return;

            try
            {
                var fileName = Path.GetFileName(filePath);
                var extension = Path.GetExtension(filePath).ToLower();
                var targetDir = GetTargetDirectory(extension);
                var targetPath = Path.Combine(targetDir, fileName);

                Directory.CreateDirectory(targetDir);
                await Task.Run(() => File.Copy(filePath, targetPath, true));

                statusText.Text = $"Preset imported: {fileName}";
                RefreshPresetList();
            }
            catch (Exception ex)
            {
                statusText.Text = $"Import failed: {ex.Message}";
            }
        }

        private string GetTargetDirectory(string extension)
        {
            return extension switch
            {
                ".avs" => "presets/avs",
                ".milk" => "presets/milkdrop",
                _ => "presets"
            };
        }

        private void RefreshPresetList()
        {
            _allPresets.Clear();
            
            try
            {
                var avsDir = "presets/avs";
                if (Directory.Exists(avsDir))
                {
                    foreach (var file in Directory.GetFiles(avsDir, "*.avs"))
                    {
                        _allPresets.Add(new PresetInfo(file, "AVS"));
                    }
                }

                var milkDir = "presets/milkdrop";
                if (Directory.Exists(milkDir))
                {
                    foreach (var file in Directory.GetFiles(milkDir, "*.milk"))
                    {
                        _allPresets.Add(new PresetInfo(file, "MilkDrop"));
                    }
                }

                FilterPresets();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing preset list: {ex.Message}");
            }
        }

        private void FilterPresets()
        {
            var filteredPresets = _selectedPresetType switch
            {
                "avs" => _allPresets.Where(p => p.Type == "AVS").ToList(),
                "milkdrop" => _allPresets.Where(p => p.Type == "MilkDrop").ToList(),
                _ => _allPresets
            };

            var presetListBox = this.FindControl<ListBox>("PresetListBox");
            if (presetListBox != null)
            {
                presetListBox.ItemsSource = filteredPresets;
            }
            
            var statusText = this.FindControl<TextBlock>("StatusText");
            if (statusText != null)
            {
                statusText.Text = $"Found {filteredPresets.Count} presets";
            }
        }

        private void ShowPresetDetails(PresetInfo preset)
        {
            var presetNameText = this.FindControl<TextBlock>("PresetNameText");
            var presetTypeText = this.FindControl<TextBlock>("PresetTypeText");
            var presetSizeText = this.FindControl<TextBlock>("PresetSizeText");
            var presetModifiedText = this.FindControl<TextBlock>("PresetModifiedText");
            var presetPreviewText = this.FindControl<TextBox>("PresetPreviewText");

            if (presetNameText == null || presetTypeText == null || 
                presetSizeText == null || presetModifiedText == null || 
                presetPreviewText == null) return;

            try
            {
                var fileInfo = new FileInfo(preset.FilePath);
                
                presetNameText.Text = preset.Name;
                presetTypeText.Text = preset.Type;
                presetSizeText.Text = $"{fileInfo.Length / 1024.0:F1} KB";
                presetModifiedText.Text = fileInfo.LastWriteTime.ToString("g");

                var content = File.ReadAllText(preset.FilePath);
                presetPreviewText.Text = content.Length > 500 
                    ? content.Substring(0, 500) + "..." 
                    : content;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing preset details: {ex.Message}");
            }
        }

        // Placeholder methods for other buttons
        private void OnExportPreset(object? sender, RoutedEventArgs e) { }
        private void OnRefreshList(object? sender, RoutedEventArgs e) => RefreshPresetList();
        private void OnDeletePreset(object? sender, RoutedEventArgs e) { }
        private void OnCopyToClipboard(object? sender, RoutedEventArgs e) { }
        private void OnImportFolder(object? sender, RoutedEventArgs e) { }
        private void OnExportAll(object? sender, RoutedEventArgs e) { }
        private void OnCleanDuplicates(object? sender, RoutedEventArgs e) { }
    }
}
