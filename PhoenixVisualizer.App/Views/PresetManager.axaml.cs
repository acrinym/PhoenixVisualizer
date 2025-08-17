using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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
            InitializeComponent();
            PresetTypeList.SelectedIndex = 0;
            RefreshPresetList();
        }

        private void OnPresetTypeChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (PresetTypeList.SelectedItem is ListBoxItem item && item.Tag is string tag)
            {
                _selectedPresetType = tag;
                FilterPresets();
            }
        }

        private void OnPresetSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (PresetListBox.SelectedItem is PresetInfo preset)
            {
                _selectedPreset = preset;
                ShowPresetDetails(preset);
            }
        }

        private async void OnImportPreset(object? sender, RoutedEventArgs e)
        {
            if (StatusText == null) return;

            var options = new FilePickerOpenOptions
            {
                Title = "Select Preset File",
                AllowMultiple = false
            };

            var files = await StorageProvider.OpenFilePickerAsync(options);
            if (files.Count > 0)
            {
                await ImportPresetFile(files[0].Path.LocalPath);
            }
        }

        private async Task ImportPresetFile(string filePath)
        {
            if (StatusText == null) return;

            try
            {
                var fileName = Path.GetFileName(filePath);
                var extension = Path.GetExtension(filePath).ToLower();
                var targetDir = GetTargetDirectory(extension);
                var targetPath = Path.Combine(targetDir, fileName);

                Directory.CreateDirectory(targetDir);
                await Task.Run(() => File.Copy(filePath, targetPath, true));

                StatusText.Text = $"Preset imported: {fileName}";
                RefreshPresetList();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Import failed: {ex.Message}";
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

            PresetListBox.ItemsSource = filteredPresets;
            
            if (StatusText != null)
            {
                StatusText.Text = $"Found {filteredPresets.Count} presets";
            }
        }

        private void ShowPresetDetails(PresetInfo preset)
        {
            if (PresetNameText == null || PresetTypeText == null || 
                PresetSizeText == null || PresetModifiedText == null || 
                PresetPreviewText == null) return;

            try
            {
                var fileInfo = new FileInfo(preset.FilePath);
                
                PresetNameText.Text = preset.Name;
                PresetTypeText.Text = preset.Type;
                PresetSizeText.Text = $"{fileInfo.Length / 1024.0:F1} KB";
                PresetModifiedText.Text = fileInfo.LastWriteTime.ToString("g");

                var content = File.ReadAllText(preset.FilePath);
                PresetPreviewText.Text = content.Length > 500 
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
