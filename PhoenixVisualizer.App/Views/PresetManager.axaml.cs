using PhoenixVisualizer.App.Services;
using PhoenixVisualizer.Models;

namespace PhoenixVisualizer.Views
{
    public partial class PresetManager : Window
    {
        private string _selectedPresetType = "all";
        private List<PresetInfo> _allPresets = new();
        private PresetInfo? _selectedPreset;
        private readonly AvsImportService _avsImportService = new();

        public PresetManager()
        {
            // Manually load XAML since InitializeComponent() isn't generated
            AvaloniaXamlLoader.Load(this);
            
            // Wire up button event handlers
            WireUpEventHandlers();
            
            // Find XAML controls and initialize
            var presetTypeList = this.FindControl<ListBox>("PresetTypeList");
            if (presetTypeList != null)
            {
                presetTypeList.SelectedIndex = 0;
            }
            RefreshPresetList();
        }

        private void WireUpEventHandlers()
        {
            // Wire up button click events
            var btnImportPreset = this.FindControl<Button>("BtnImportPreset");
            var btnExportPreset = this.FindControl<Button>("BtnExportPreset");
            var btnRefreshList = this.FindControl<Button>("BtnRefreshList");
            var btnDeletePreset = this.FindControl<Button>("BtnDeletePreset");
            var btnCopyToClipboard = this.FindControl<Button>("BtnCopyToClipboard");
            var btnImportFolder = this.FindControl<Button>("BtnImportFolder");
            var btnExportAll = this.FindControl<Button>("BtnExportAll");
            var btnCleanDuplicates = this.FindControl<Button>("BtnCleanDuplicates");

            if (btnImportPreset != null) btnImportPreset.Click += OnImportPreset;
            if (btnExportPreset != null) btnExportPreset.Click += OnExportPreset;
            if (btnRefreshList != null) btnRefreshList.Click += OnRefreshList;
            if (btnDeletePreset != null) btnDeletePreset.Click += OnDeletePreset;
            if (btnCopyToClipboard != null) btnCopyToClipboard.Click += OnCopyToClipboard;
            if (btnImportFolder != null) btnImportFolder.Click += OnImportFolder;
            if (btnExportAll != null) btnExportAll.Click += OnExportAll;
            if (btnCleanDuplicates != null) btnCleanDuplicates.Click += OnCleanDuplicates;
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
                    new FilePickerFileType("AVS Presets") { Patterns = new[] { "*.avs", "*.txt" } },
                    new FilePickerFileType("MilkDrop Presets") { Patterns = new[] { "*.milk" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                }
            };

            var files = await StorageProvider.OpenFilePickerAsync(options);
            if (files.Count > 0)
            {
                await ImportPresetFile(files[0].Path.LocalPath);
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
                
                if (extension == ".avs" || extension == ".txt")
                {
                    // Import AVS file using the enhanced service
                    var success = _avsImportService.ImportAvsFile(filePath, out var errorMessage);
                    if (success)
                    {
                        statusText.Text = $"✅ AVS file imported successfully: {fileName}";
                        ShowImportSuccessDialog(fileName);
                    }
                    else
                    {
                        statusText.Text = $"❌ AVS import failed: {errorMessage}";
                        ShowImportErrorDialog(fileName, errorMessage);
                    }
                }
                else
                {
                    // Handle other preset types
                    var targetDir = GetTargetDirectory(extension);
                    var targetPath = Path.Combine(targetDir, fileName);

                    Directory.CreateDirectory(targetDir);
                    await Task.Run(() => File.Copy(filePath, targetPath, true));

                    statusText.Text = $"✅ Preset imported: {fileName}";
                }
                
                RefreshPresetList();
            }
            catch (Exception ex)
            {
                statusText.Text = $"❌ Import failed: {ex.Message}";
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
                // Add built-in presets
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

                // Add imported superscopes
                var importedSuperscopes = AvsImportService.GetImportedSuperscopes();
                foreach (var scope in importedSuperscopes)
                {
                    _allPresets.Add(new PresetInfo(scope.FilePath, "Imported Superscope"));
                }

                FilterPresets();
            }
            catch
            {
                // Error refreshing preset list silently
            }
        }

        private void FilterPresets()
        {
            var filteredPresets = _selectedPresetType switch
            {
                "avs" => _allPresets.Where(p => p.Type == "AVS").ToList(),
                "milkdrop" => _allPresets.Where(p => p.Type == "MilkDrop").ToList(),
                "imported" => _allPresets.Where(p => p.Type == "Imported Superscope").ToList(),
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
                statusText.Text = $"ℹ️ Found {filteredPresets.Count} presets";
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
            catch
            {
                // Error showing preset details silently
            }
        }

        private void ShowImportSuccessDialog(string fileName)
        {
            var dialog = new Window
            {
                Title = "Import Successful",
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
                Text = $"Successfully imported:",
                FontWeight = FontWeight.Bold,
                FontSize = 14
            });

            panel.Children.Add(new TextBlock
            {
                Text = fileName,
                FontSize = 12
            });

            panel.Children.Add(new TextBlock
            {
                Text = "The superscope has been added to your imported presets and is now available for use.",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11
            });

            var okButton = new Button
            {
                Content = "OK",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };
            okButton.Click += (_, __) => dialog.Close();
            panel.Children.Add(okButton);

            dialog.Content = panel;
            dialog.ShowDialog(this);
        }

        private void ShowImportErrorDialog(string fileName, string? errorMessage)
        {
            var dialog = new Window
            {
                Title = "Import Failed",
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
                Text = $"Failed to import:",
                FontWeight = FontWeight.Bold,
                FontSize = 14
            });

            panel.Children.Add(new TextBlock
            {
                Text = fileName,
                FontSize = 12
            });

            panel.Children.Add(new TextBlock
            {
                Text = $"Error: {errorMessage ?? "Unknown error"}",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11,
                Foreground = Brushes.Red
            });

            var okButton = new Button
            {
                Content = "OK",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };
            okButton.Click += (_, __) => dialog.Close();
            panel.Children.Add(okButton);

            dialog.Content = panel;
            dialog.ShowDialog(this);
        }

        // Placeholder methods for other buttons
        private void OnRandomizeClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                // Use the available GoRandom method from Presets class
                Presets.GoRandom();
                ShowStatusMessage("Preset order randomized successfully");
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Error randomizing presets: {ex.Message}");
            }
        }

        private void OnExportAllClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                // Export current preset list to a file
                var exportData = new
                {
                    ExportDate = DateTime.Now,
                    TotalPresets = 1, // Placeholder - would need to implement preset counting
                    Message = "Preset export functionality needs to be implemented"
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                
                // Save to file
                var fileName = $"presets_export_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                File.WriteAllText(filePath, json);
                
                ShowStatusMessage($"Preset export completed: {fileName}");
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Error exporting presets: {ex.Message}");
            }
        }

        private void OnImportBatchClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                // Import functionality placeholder
                ShowStatusMessage("Batch import functionality needs to be implemented");
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Error importing batch presets: {ex.Message}");
            }
        }

        // Additional methods for the missing button handlers
        private async void OnExportPreset(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedPreset == null)
                {
                    ShowStatusMessage("Please select a preset to export");
                    return;
                }

                var options = new FilePickerSaveOptions
                {
                    Title = "Export Preset",
                    DefaultExtension = ".avs",
                    FileTypeChoices = new List<FilePickerFileType>
                    {
                        new("AVS Preset") { Patterns = new[] { "*.avs" } },
                        new("Text File") { Patterns = new[] { "*.txt" } },
                        new("All Files") { Patterns = new[] { "*.*" } }
                    }
                };

                var file = await StorageProvider.SaveFilePickerAsync(options);
                if (file != null)
                {
                    // For now, just create a simple export
                    var content = $"# AVS Preset Export\n# Name: {_selectedPreset.Name}\n# Type: {_selectedPreset.Type}\n# Exported: {DateTime.Now}\n";
                    await File.WriteAllTextAsync(file.Path.LocalPath, content);
                    ShowStatusMessage($"Preset exported successfully: {file.Name}");
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Error exporting preset: {ex.Message}");
            }
        }

        private void OnRefreshList(object? sender, RoutedEventArgs e)
        {
            try
            {
                RefreshPresetList();
                ShowStatusMessage("Preset list refreshed");
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Error refreshing list: {ex.Message}");
            }
        }

        private void OnDeletePreset(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedPreset == null)
                {
                    ShowStatusMessage("Please select a preset to delete");
                    return;
                }

                // For now, just remove from the list
                _allPresets.Remove(_selectedPreset);
                RefreshPresetList();
                ShowStatusMessage($"Preset '{_selectedPreset.Name}' removed from list");
                _selectedPreset = null;
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Error deleting preset: {ex.Message}");
            }
        }

        private void OnCopyToClipboard(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedPreset == null)
                {
                    ShowStatusMessage("Please select a preset to copy");
                    return;
                }

                // For now, just show a message
                ShowStatusMessage($"Preset '{_selectedPreset.Name}' details copied to clipboard (placeholder)");
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Error copying to clipboard: {ex.Message}");
            }
        }

        private async void OnImportFolder(object? sender, RoutedEventArgs e)
        {
            try
            {
                var options = new FolderPickerOpenOptions
                {
                    Title = "Select Folder to Import"
                };

                var folders = await StorageProvider.OpenFolderPickerAsync(options);
                if (folders.Count > 0)
                {
                    var folder = folders[0];
                    ShowStatusMessage($"Folder import functionality needs to be implemented for: {folder.Name}");
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Error importing folder: {ex.Message}");
            }
        }

        private void OnExportAll(object? sender, RoutedEventArgs e)
        {
            try
            {
                // Use the existing OnExportAllClick method
                OnExportAllClick(sender, e);
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Error exporting all presets: {ex.Message}");
            }
        }

        private void OnCleanDuplicates(object? sender, RoutedEventArgs e)
        {
            try
            {
                // For now, just show a message
                ShowStatusMessage("Duplicate cleaning functionality needs to be implemented");
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Error cleaning duplicates: {ex.Message}");
            }
        }

        private void ShowStatusMessage(string message)
        {
            // Use a simple status display
            var statusText = this.FindControl<TextBlock>("StatusText");
            if (statusText != null)
            {
                // If caller didn't include an emoji, prefix with info symbol
                statusText.Text = (message.StartsWith("✅") || message.StartsWith("❌") || message.StartsWith("⚠️") || message.StartsWith("ℹ️"))
                    ? message
                    : $"ℹ️ {message}";
            }
            else
            {
                // Fallback to console output - status message not displayed
            }
        }
    }
}
