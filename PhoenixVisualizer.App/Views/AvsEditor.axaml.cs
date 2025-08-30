using PhoenixVisualizer.App.Services;

namespace PhoenixVisualizer.Views
{
    public partial class AvsEditor : Window
    {
        private readonly AvsImportService _avsImportService = new();
        private string _currentFilePath = string.Empty;

        // Event to communicate with main window
        public event Action<string>? AvsContentImported;

        public AvsEditor()
        {
            AvaloniaXamlLoader.Load(this);
            WireUpEventHandlers();
            InitializeCodeEditor();
        }

        private void InitializeCodeEditor()
        {
            var codeEditor = this.FindControl<TextBox>("CodeEditor");
            if (codeEditor != null)
            {
                // Update stats when text changes
                codeEditor.TextChanged += (sender, args) =>
                {
                    UpdateCodeStats();
                    UpdatePreview();
                };

                // Initial stats update
                UpdateCodeStats();
            }
        }

        private void UpdateCodeStats()
        {
            var codeEditor = this.FindControl<TextBox>("CodeEditor");
            var lineCountText = this.FindControl<TextBlock>("LineCountText");
            var charCountText = this.FindControl<TextBlock>("CharCountText");

            if (codeEditor != null && lineCountText != null && charCountText != null)
            {
                var text = codeEditor.Text ?? string.Empty;
                var lines = text.Split('\n').Length;
                var chars = text.Length;

                lineCountText.Text = $"Lines: {lines}";
                charCountText.Text = $"Chars: {chars}";
            }
        }

        private async void OnLoadFile(object? sender, RoutedEventArgs e)
        {
            _ = sender; _ = e; // silence unused parameters
            var options = new FilePickerOpenOptions
            {
                Title = "Load AVS File",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("AVS Files") { Patterns = ["*.avs", "*.txt"] },
                    new FilePickerFileType("All Files") { Patterns = ["*.*"] }
                ]
            };

            var files = await StorageProvider.OpenFilePickerAsync(options);
            if (files.Count > 0)
            {
                try
                {
                    var filePath = files[0].Path.LocalPath;
                    var content = await File.ReadAllTextAsync(filePath);
                    
                    var codeEditor = this.FindControl<TextBox>("CodeEditor");
                    if (codeEditor != null)
                    {
                        codeEditor.Text = content;
                        _currentFilePath = filePath;

                        var statusText = this.FindControl<TextBlock>("StatusText");
                        if (statusText != null)
                        {
                            statusText.Text = $"Loaded: {Path.GetFileName(filePath)}";
                        }

                        UpdateCodeStats();
                        UpdatePreview();
                    }
                }
                catch (Exception ex)
                {
                    ShowErrorDialog("Load Error", $"Failed to load file: {ex.Message}");
                }
            }
        }

        private async void OnSaveFile(object? sender, RoutedEventArgs e)
        {
            _ = sender; _ = e; // silence unused parameters
            var codeEditor = this.FindControl<TextBox>("CodeEditor");
            if (codeEditor == null) return;

            var options = new FilePickerSaveOptions
            {
                Title = "Save AVS File",
                DefaultExtension = "avs",
                FileTypeChoices =
                [
                    new FilePickerFileType("AVS Files") { Patterns = ["*.avs"] },
                    new FilePickerFileType("Text Files") { Patterns = ["*.txt"] }
                ]
            };

            var file = await StorageProvider.SaveFilePickerAsync(options);
            if (file != null)
            {
                try
                {
                    await File.WriteAllTextAsync(file.Path.LocalPath, codeEditor.Text);
                    _currentFilePath = file.Path.LocalPath;
                    
                    var statusText = this.FindControl<TextBlock>("StatusText");
                    if (statusText != null)
                    {
                        statusText.Text = $"Saved: {Path.GetFileName(_currentFilePath)}";
                    }
                }
                catch (Exception ex)
                {
                    ShowErrorDialog("Save Error", $"Failed to save file: {ex.Message}");
                }
            }
        }

        private void OnClear(object? sender, RoutedEventArgs e)
        {
            _ = sender; _ = e; // silence unused parameters
            var codeEditor = this.FindControl<TextBox>("CodeEditor");
            if (codeEditor != null)
            {
                codeEditor.Text = "// AVS Preset Code\n// Enter your superscope code here";
                _currentFilePath = string.Empty;
                
                var statusText = this.FindControl<TextBlock>("StatusText");
                if (statusText != null)
                {
                    statusText.Text = "Editor cleared";
                }

                UpdateCodeStats();
                UpdatePreview();
            }
        }

        private void OnTestPreset(object? sender, RoutedEventArgs e)
        {
            _ = sender; _ = e; // silence unused parameters
            var codeEditor = this.FindControl<TextBox>("CodeEditor");
            if (codeEditor == null) return;

            try
            {
                // Create a temporary file for testing
                var tempFile = Path.GetTempFileName() + ".avs";
                File.WriteAllText(tempFile, codeEditor.Text);
                
                // Test the preset by parsing it
                var avsFile = _avsImportService.ParseAvsFile(tempFile);
                
                var statusText = this.FindControl<TextBlock>("StatusText");
                if (statusText != null)
                {
                    if (avsFile.HasSuperscopes)
                    {
                        var validCount = avsFile.Superscopes.Count(s => s.IsValid);
                        statusText.Text = $"Test successful: {validCount}/{avsFile.Superscopes.Count} superscopes valid";
                    }
                    else
                    {
                        statusText.Text = "Test failed: No valid superscopes found";
                    }
                }
                
                // Clean up temp file
                try { File.Delete(tempFile); } catch { }
                
                UpdatePreview();
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Test Error", $"Failed to test preset: {ex.Message}");
            }
        }

        private void OnImportToLibrary(object? sender, RoutedEventArgs e)
        {
            _ = sender; _ = e; // silence unused parameters
            var codeEditor = this.FindControl<TextBox>("CodeEditor");
            if (codeEditor == null) return;

            try
            {
                // Create a temporary file for import
                var tempFile = Path.GetTempFileName() + ".avs";
                File.WriteAllText(tempFile, codeEditor.Text);

                // Import the preset
                var success = _avsImportService.ImportAvsFile(tempFile, out var errorMessage);

                var statusText = this.FindControl<TextBlock>("StatusText");
                if (statusText != null)
                {
                    if (success)
                    {
                        statusText.Text = "Preset imported to library successfully!";
                        ShowSuccessDialog("Import Successful", "Your AVS preset has been imported to the library and is now available for use.");
                    }
                    else
                    {
                        statusText.Text = $"Import failed: {errorMessage}";
                        ShowErrorDialog("Import Failed", $"Failed to import preset: {errorMessage}");
                    }
                }

                // Clean up temp file
                try { File.Delete(tempFile); } catch { }
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Import Error", $"Failed to import preset: {ex.Message}");
            }
        }

        private async void OnExportCSharp(object? sender, RoutedEventArgs e)
        {
            _ = sender; _ = e; // silence unused parameters
            var codeEditor = this.FindControl<TextBox>("CodeEditor");
            if (codeEditor == null) return;

            try
            {
                // Create a temporary file for export
                var tempFile = Path.GetTempFileName() + ".avs";
                File.WriteAllText(tempFile, codeEditor.Text);
                
                // Parse and generate C# code
                var avsFile = _avsImportService.ParseAvsFile(tempFile);
                if (avsFile.HasSuperscopes)
                {
                    var options = new FilePickerSaveOptions
                    {
                        Title = "Export C# Code",
                        DefaultExtension = "cs",
                        FileTypeChoices = new List<FilePickerFileType>
                        {
                            new FilePickerFileType("C# Files") { Patterns = new[] { "*.cs" } }
                        }
                    };

                    var file = await StorageProvider.SaveFilePickerAsync(options);
                    if (file != null)
                    {
                        var firstScope = avsFile.Superscopes.First();
                        var csharpCode = GenerateCSharpCode(firstScope, "ExportedPreset");
                        await File.WriteAllTextAsync(file.Path.LocalPath, csharpCode);
                        
                        var statusText = this.FindControl<TextBlock>("StatusText");
                        if (statusText != null)
                        {
                            statusText.Text = "C# code exported successfully!";
                        }
                    }
                }
                else
                {
                    ShowErrorDialog("Export Error", "No valid superscopes found to export.");
                }
                
                // Clean up temp file
                try { File.Delete(tempFile); } catch { }
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Export Error", $"Failed to export C# code: {ex.Message}");
            }
        }

        private void OnApplyPreset(object? sender, RoutedEventArgs e)
        {
            var codeEditor = this.FindControl<TextBox>("CodeEditor");
            if (codeEditor == null) return;

            try
            {
                // Create a temporary file and import it
                var tempFile = Path.GetTempFileName() + ".avs";
                File.WriteAllText(tempFile, codeEditor.Text);
                
                var success = _avsImportService.ImportAvsFile(tempFile, out var errorMessage);
                
                if (success)
                {
                    ShowSuccessDialog("Preset Applied", "Your AVS preset has been imported and is now available in the main application!");
                    Close();
                }
                else
                {
                    ShowErrorDialog("Apply Failed", $"Failed to apply preset: {errorMessage}");
                }
                
                // Clean up temp file
                try { File.Delete(tempFile); } catch { }
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Apply Error", $"Failed to apply preset: {ex.Message}");
            }
        }

        private void OnClose(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnSendToMainWindow(object? sender, RoutedEventArgs e)
        {
            var codeEditor = this.FindControl<TextBox>("CodeEditor");
            if (codeEditor == null) return;

            var content = codeEditor.Text;
            if (string.IsNullOrWhiteSpace(content))
            {
                ShowErrorDialog("No Content", "Please enter some AVS code before sending to main window.");
                return;
            }

            // Trigger the event to send content to main window
            AvsContentImported?.Invoke(content);
            
            var statusText = this.FindControl<TextBlock>("StatusText");
            if (statusText != null)
            {
                statusText.Text = "Content sent to main window!";
            }
            
            // Close the editor after sending
            Close();
        }

        private void UpdatePreview()
        {
            var codeEditor = this.FindControl<TextBox>("CodeEditor");
            var previewText = this.FindControl<TextBlock>("PreviewText");
            
            if (codeEditor == null || previewText == null) return;

            try
            {
                var content = codeEditor.Text;
                if (string.IsNullOrWhiteSpace(content))
                {
                    previewText.Text = "No code to preview";
                    return;
                }

                // Parse the content to show a preview
                var tempFile = Path.GetTempFileName() + ".avs";
                File.WriteAllText(tempFile, content);
                
                var avsFile = _avsImportService.ParseAvsFile(tempFile);
                
                if (avsFile.HasSuperscopes)
                {
                    var validCount = avsFile.Superscopes.Count(s => s.IsValid);
                    var totalCount = avsFile.Superscopes.Count;
                    
                    previewText.Text = $"Preview: {validCount}/{totalCount} superscopes\n" +
                                     $"File size: {content.Length} characters\n" +
                                     $"Ready for import";
                }
                else
                {
                    previewText.Text = "Preview: No valid superscopes found\nCheck your AVS syntax";
                }
                
                // Clean up temp file
                try { File.Delete(tempFile); } catch { }
            }
            catch (Exception ex)
            {
                previewText.Text = $"Preview error: {ex.Message}";
            }
        }

        private string GenerateCSharpCode(AvsImportService.AvsSuperscope scope, string className)
        {
            return $@"using PhoenixVisualizer.Visuals;
using PhoenixVisualizer.Audio;
using SkiaSharp;

namespace PhoenixVisualizer.ExportedPresets
{{
    public class {className} : IVisualizerPlugin
    {{
        public string Id => ""exported_{className.ToLower()}"";
        public string DisplayName => ""{scope.Name} (Exported)"";
        
        private int _width, _height;
        private float _time;
        
        public void Initialize(int width, int height)
        {{
            _width = width;
            _height = height;
            _time = 0;
        }}
        
        public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
        {{
            canvas.Clear(0xFF000000);
            _time += 0.02f;
            
            // Converted AVS code:
            {ConvertAvsToCSharp(scope.Code)}
        }}
        
        public void Resize(int width, int height)
        {{
            _width = width;
            _height = height;
        }}
        
        public void Dispose()
        {{
            // Clean up resources if any
        }}
    }}
}}";
        }

        private string ConvertAvsToCSharp(string avsCode)
        {
            // Basic AVS to C# conversion
            var csharpCode = avsCode;
            
            // Replace mathematical functions
            csharpCode = System.Text.RegularExpressions.Regex.Replace(csharpCode, @"\bsin\s*\(", "Math.Sin(", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            csharpCode = System.Text.RegularExpressions.Regex.Replace(csharpCode, @"\bcos\s*\(", "Math.Cos(", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            csharpCode = System.Text.RegularExpressions.Regex.Replace(csharpCode, @"\btan\s*\(", "Math.Tan(", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            csharpCode = System.Text.RegularExpressions.Regex.Replace(csharpCode, @"\bsqrt\s*\(", "Math.Sqrt(", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            csharpCode = System.Text.RegularExpressions.Regex.Replace(csharpCode, @"\bpow\s*\(", "Math.Pow(", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            csharpCode = System.Text.RegularExpressions.Regex.Replace(csharpCode, @"\babs\s*\(", "Math.Abs(", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            // Replace AVS variables
            csharpCode = System.Text.RegularExpressions.Regex.Replace(csharpCode, @"\bt\b", "_time");
            csharpCode = System.Text.RegularExpressions.Regex.Replace(csharpCode, @"\bw\b", "_width");
            csharpCode = System.Text.RegularExpressions.Regex.Replace(csharpCode, @"\bh\b", "_height");
            
            return csharpCode;
        }

        private void ShowErrorDialog(string title, string message)
        {
            var dialog = new Window
            {
                Title = title,
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
                Text = title,
                FontWeight = FontWeight.Bold,
                FontSize = 14
            });

            panel.Children.Add(new TextBlock
            {
                Text = message,
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

        private void ShowSuccessDialog(string title, string message)
        {
            var dialog = new Window
            {
                Title = title,
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
                Text = title,
                FontWeight = FontWeight.Bold,
                FontSize = 14
            });

            panel.Children.Add(new TextBlock
            {
                Text = message,
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

        private void WireUpEventHandlers()
        {
            // Wire up button click events
            var btnLoadFile = this.FindControl<Button>("BtnLoadFile");
            var btnSaveFile = this.FindControl<Button>("BtnSaveFile");
            var btnClear = this.FindControl<Button>("BtnClear");
            var btnTestPreset = this.FindControl<Button>("BtnTestPreset");
            var btnImportToLibrary = this.FindControl<Button>("BtnImportToLibrary");
            var btnExportCSharp = this.FindControl<Button>("BtnExportCSharp");
            var btnSendToMainWindow = this.FindControl<Button>("BtnSendToMainWindow");
            var btnClose = this.FindControl<Button>("BtnClose");
            var applyButton = this.FindControl<Button>("ApplyButton");

            if (btnLoadFile != null) btnLoadFile.Click += OnLoadFile;
            if (btnSaveFile != null) btnSaveFile.Click += OnSaveFile;
            if (btnClear != null) btnClear.Click += OnClear;
            if (btnTestPreset != null) btnTestPreset.Click += OnTestPreset;
            if (btnImportToLibrary != null) btnImportToLibrary.Click += OnImportToLibrary;
            if (btnExportCSharp != null) btnExportCSharp.Click += OnExportCSharp;
            if (btnSendToMainWindow != null) btnSendToMainWindow.Click += OnSendToMainWindow;
            if (btnClose != null) btnClose.Click += OnClose;
            if (applyButton != null) applyButton.Click += OnApplyPreset;
        }
    }
}
