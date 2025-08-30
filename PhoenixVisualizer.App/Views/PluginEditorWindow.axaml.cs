using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.IO;
using AvaloniaEdit;                // ✨ Syntax highlighting
using AvaloniaEdit.Highlighting;
using System.Collections.Generic; // Added for List<object>
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Linq;
using System.ComponentModel;
using System.IO;
using PhoenixVisualizer.Core.Nodes;
using Microsoft.CodeAnalysis;       // ✨ Roslyn for compilation
using Microsoft.CodeAnalysis.CSharp;

namespace PhoenixVisualizer.Views
{

public partial class PluginEditorWindow : Window, INotifyPropertyChanged
{
    private string? _currentFile;
    private readonly TextEditor? _editor;
    private readonly ObservableCollection<EffectDescriptor> _effects = new();
    private readonly ObservableCollection<string> _availableEffects = new();
    private readonly System.Threading.Timer? _refreshTimer;
    private FileSystemWatcher? _pluginWatcher;
    
    public new event PropertyChangedEventHandler? PropertyChanged;
    
    /// <summary>
    /// Observable collection of available effects for binding
    /// </summary>
    public ObservableCollection<string> AvailableEffects => _availableEffects;
    
    /// <summary>
    /// Observable collection of current effects in the editor
    /// </summary>
    public ObservableCollection<EffectDescriptor> CurrentEffects => _effects;

    public PluginEditorWindow()
    {
        InitializeComponent();
        _editor = this.FindControl<TextEditor>("CodeEditor");
        if (_editor != null)
        {
            _editor.ShowLineNumbers = true;
            _editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
        }
        
        // Initialize and populate available effects
        RefreshAvailableEffects();
        
        // Set up data context for binding
        DataContext = this;
        
        // Auto-refresh effects periodically (for dynamic plugin loading)
        _refreshTimer = new System.Threading.Timer(
            callback: _ => Dispatcher.UIThread.Post(RefreshAvailableEffects),
            state: null,
            dueTime: TimeSpan.FromSeconds(2),  // Initial delay
            period: TimeSpan.FromSeconds(5)    // Refresh every 5 seconds
        );
        
        // Set up file system watcher for plugin directories (if they exist)
        SetupPluginWatcher();
    }

    private async void OnOpenClick(object? _, RoutedEventArgs __)
    {
        #pragma warning disable CS0618 // Using obsolete file dialog API - will be updated to StorageProvider in future
        var dlg = new OpenFileDialog { Title = "Open Plugin" };
        dlg.Filters.Add(new FileDialogFilter { Name = "Phoenix Plugins", Extensions = { "phx", "avs", "txt" } });
        var result = await dlg.ShowAsync(this);
        #pragma warning restore CS0618
        if (result is { Length: > 0 } && !string.IsNullOrEmpty(result[0]))
        {
            var text = File.ReadAllText(result[0]);
            if (_editor != null)
            {
                _editor.Text = text;
            }
            _currentFile = result[0];
            this.Title = $"Phoenix Plugin Editor - {Path.GetFileName(_currentFile)}";
        }
    }

    private async void OnSaveClick(object? _, RoutedEventArgs __)
    {
        if (_editor == null) return;
        var path = _currentFile;
        if (string.IsNullOrEmpty(path))
        {
            #pragma warning disable CS0618 // Using obsolete file dialog API - will be updated to StorageProvider in future
            var dlg = new SaveFileDialog { Title = "Save Plugin As..." };
            path = await dlg.ShowAsync(this);
            #pragma warning restore CS0618
        }
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, _editor.Text ?? "");
            _currentFile = path;
            this.Title = $"Phoenix Plugin Editor - {Path.GetFileName(_currentFile)} (saved)";
        }
    }

    private async void OnSaveAsClick(object? _, RoutedEventArgs __)
    {
        #pragma warning disable CS0618 // Using obsolete file dialog API - will be updated to StorageProvider in future
        var dlg = new SaveFileDialog { Title = "Save Plugin As..." };
        dlg.Filters.Add(new FileDialogFilter { Name = "Phoenix Plugin", Extensions = { "phx" } });
        dlg.Filters.Add(new FileDialogFilter { Name = "Winamp AVS Preset", Extensions = { "avs" } });
        var path = await dlg.ShowAsync(this);
        #pragma warning restore CS0618
        if (!string.IsNullOrEmpty(path))
        {
            if (path.EndsWith(".avs", StringComparison.OrdinalIgnoreCase))
            {
                AvsConverter.SaveAvs(path, CollectPhx());
            }
            else
            {
                SavePhx(path);
            }
            _currentFile = path;
            this.Title = $"Phoenix Plugin Editor - {Path.GetFileName(path)}";
        }
    }

    private string CollectPhx() => _editor?.Text ?? "{}";

    private void SavePhx(string path)
    {
        File.WriteAllText(path, CollectPhx());
    }

    private void OnAddEffect(object? _, RoutedEventArgs __)
    {
        // Refresh available effects before showing dialog (in case new plugins were loaded)
        RefreshAvailableEffects();
        
        // Show registry picker dialog to select an effect
        var dlg = new Window
        {
            Title = "Add Effect - Available Effects",
            Width = 400,
            Height = 500
        };

        var stackPanel = new StackPanel { Margin = new Thickness(10) };
        
        // Add refresh button
        var refreshButton = new Button 
        { 
            Content = "🔄 Refresh Effects List", 
            Margin = new Thickness(0, 0, 0, 10),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
        };
        
        var list = new ListBox
        {
            Height = 350,
            ItemsSource = _availableEffects  // Bind to the observable collection
        };
        
        var infoText = new TextBlock
        {
            Text = $"Found {_availableEffects.Count} available effects. Double-click to add.",
            FontSize = 12,
            Foreground = Avalonia.Media.Brushes.Gray,
            Margin = new Thickness(0, 10, 0, 0)
        };

        refreshButton.Click += (s, e) =>
        {
            RefreshAvailableEffects();
            infoText.Text = $"Found {_availableEffects.Count} available effects. Double-click to add.";
        };

        list.DoubleTapped += (s, e) =>
        {
            if (list.SelectedItem is string name)
            {
                var node = EffectRegistry.CreateByName(name);
                if (node != null)
                {
                    var newEffect = new EffectDescriptor { Name = name, Enabled = true, Node = node };
                    _effects.Add(newEffect);
                    
                    System.Diagnostics.Debug.WriteLine($"[PluginEditor] Added effect: {name}");
                    
                    // The ObservableCollection will automatically update the UI
                    RefreshEffects();
                }
                dlg.Close();
            }
        };

        stackPanel.Children.Add(refreshButton);
        stackPanel.Children.Add(list);
        stackPanel.Children.Add(infoText);
        
        dlg.Content = stackPanel;
        dlg.ShowDialog(this);
    }

    private void OnDuplicateEffect(object? _, RoutedEventArgs __)
    {
        var listBox = this.FindControl<ListBox>("EffectList");
        if (listBox?.SelectedItem is EffectDescriptor eff && eff.Node != null)
        {
            var clone = EffectRegistry.CreateByName(eff.Node.Name);
            if (clone != null)
            {
                foreach (var kv in eff.Node.Params)
                {
                    if (clone.Params.TryGetValue(kv.Key, out var dest))
                    {
                        dest.FloatValue = kv.Value.FloatValue;
                        dest.BoolValue = kv.Value.BoolValue;
                        dest.ColorValue = kv.Value.ColorValue;
                    }
                }
                _effects.Add(new EffectDescriptor { Name = eff.Name + " Copy", Enabled = eff.Enabled, Node = clone });
                RefreshEffects();
            }
        }
    }

    /// <summary>
    /// Refreshes the list of available effects from the EffectRegistry
    /// This enables dynamic discovery of new plugins/effects
    /// </summary>
    private void RefreshAvailableEffects()
    {
        try
        {
            var currentEffects = EffectRegistry.GetAll().Select(e => e.Name).OrderBy(name => name).ToList();
            
            // Only update if the list has actually changed
            if (!_availableEffects.SequenceEqual(currentEffects))
            {
                _availableEffects.Clear();
                foreach (var effectName in currentEffects)
                {
                    _availableEffects.Add(effectName);
                }
                
                System.Diagnostics.Debug.WriteLine($"[PluginEditor] Refreshed available effects: {_availableEffects.Count} found");
                
                // Trigger property changed for any bound UI elements
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableEffects)));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PluginEditor] Error refreshing available effects: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Refreshes the current effects list in the editor
    /// </summary>
    private void RefreshEffects()
    {
        // With ObservableCollection, the UI automatically updates
        // But we can still manually trigger updates if needed
        var listBox = this.FindControl<ListBox>("EffectList");
        if (listBox != null && listBox.ItemsSource != _effects)
        {
            listBox.ItemsSource = _effects;
        }
        
        System.Diagnostics.Debug.WriteLine($"[PluginEditor] Current effects count: {_effects.Count}");
    }

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnCompileClick(object? sender, RoutedEventArgs e)
    {
        if (_editor?.Text == null || string.IsNullOrWhiteSpace(_editor.Text))
        {
            ShowMessage("No code to compile", "Please enter some C# code in the editor first.");
            return;
        }

        try
        {
            var compilationResult = CompilePluginCode(_editor.Text);
            if (compilationResult.Success)
            {
                ShowMessage("Compilation Successful", $"Plugin compiled successfully!\n\nOutput: {compilationResult.OutputFile}\n\n{compilationResult.Message}");
                System.Diagnostics.Debug.WriteLine($"[PluginEditor] Compilation successful: {compilationResult.OutputFile}");
            }
            else
            {
                ShowMessage("Compilation Failed", $"Compilation failed:\n\n{compilationResult.Message}");
                System.Diagnostics.Debug.WriteLine($"[PluginEditor] Compilation failed: {compilationResult.Message}");
            }
        }
        catch (Exception ex)
        {
            ShowMessage("Compilation Error", $"An error occurred during compilation:\n\n{ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[PluginEditor] Compilation error: {ex.Message}");
        }
    }

    private void OnValidateClick(object? sender, RoutedEventArgs e)
    {
        if (_editor?.Text == null || string.IsNullOrWhiteSpace(_editor.Text))
        {
            ShowMessage("No code to validate", "Please enter some C# code in the editor first.");
            return;
        }

        try
        {
            var validationResult = ValidatePluginCode(_editor.Text);
            if (validationResult.IsValid)
            {
                ShowMessage("Validation Successful", $"Plugin code is valid!\n\n{validationResult.Message}");
                System.Diagnostics.Debug.WriteLine($"[PluginEditor] Validation successful: {validationResult.Message}");
            }
            else
            {
                ShowMessage("Validation Failed", $"Validation failed:\n\n{validationResult.Message}");
                System.Diagnostics.Debug.WriteLine($"[PluginEditor] Validation failed: {validationResult.Message}");
            }
        }
        catch (Exception ex)
        {
            ShowMessage("Validation Error", $"An error occurred during validation:\n\n{ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[PluginEditor] Validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Manual refresh button handler
    /// </summary>
    private void OnRefreshEffects(object? sender, RoutedEventArgs e)
    {
        RefreshAvailableEffects();
        System.Diagnostics.Debug.WriteLine($"[PluginEditor] Manual refresh completed - {_availableEffects.Count} effects available");
    }

    /// <summary>
    /// Compiles the plugin code into a dynamic assembly
    /// </summary>
    private PluginCompilationResult CompilePluginCode(string code)
    {
        var result = new PluginCompilationResult();

        try
        {
            // Get the current directory and create output path
            var currentDir = Directory.GetCurrentDirectory();
            var outputDir = Path.Combine(currentDir, "CompiledPlugins");
            Directory.CreateDirectory(outputDir);

            var assemblyName = $"PhoenixPlugin_{DateTime.Now:yyyyMMdd_HHmmss}";
            var outputFile = Path.Combine(outputDir, $"{assemblyName}.dll");

            // Add required references
            var references = new List<string>
            {
                typeof(object).Assembly.Location, // mscorlib/System.Private.CoreLib
                typeof(System.Linq.Enumerable).Assembly.Location, // System.Linq
                typeof(System.Collections.Generic.List<>).Assembly.Location, // System.Collections
                typeof(Avalonia.Controls.Control).Assembly.Location, // Avalonia.Controls
                typeof(PhoenixVisualizer.Core.Nodes.IEffectNode).Assembly.Location, // PhoenixVisualizer.Core
            };

            // Add PhoenixVisualizer assemblies
            var phoenixCore = Path.Combine(currentDir, "PhoenixVisualizer.Core.dll");
            var phoenixApp = Path.Combine(currentDir, "PhoenixVisualizer.App.dll");

            if (File.Exists(phoenixCore)) references.Add(phoenixCore);
            if (File.Exists(phoenixApp)) references.Add(phoenixApp);

            // Create compilation options
            var compilationOptions = new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(
                Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: Microsoft.CodeAnalysis.OptimizationLevel.Release,
                warningLevel: 4
            );

            // Add necessary using statements to the code if not present
            var enhancedCode = EnsureRequiredUsings(code);

            // Create syntax tree
            var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(
                enhancedCode,
                new Microsoft.CodeAnalysis.CSharp.CSharpParseOptions()
            );

            // Create compilation
            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                references.Select(r => Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(r)),
                compilationOptions
            );

            // Compile
            var emitResult = compilation.Emit(outputFile);

            if (emitResult.Success)
            {
                result.Success = true;
                result.OutputFile = outputFile;
                result.Message = $"Successfully compiled plugin assembly.\n" +
                               $"Location: {outputFile}\n" +
                               $"Assembly: {assemblyName}.dll";

                // Try to load and validate the compiled assembly
                try
                {
                    var assembly = System.Reflection.Assembly.LoadFrom(outputFile);
                    var pluginTypes = assembly.GetTypes()
                        .Where(t => typeof(PhoenixVisualizer.Core.Nodes.IEffectNode).IsAssignableFrom(t))
                        .ToList();

                    result.Message += $"\n\nFound {pluginTypes.Count} effect node types: {string.Join(", ", pluginTypes.Select(t => t.Name))}";
                }
                catch (Exception ex)
                {
                    result.Message += $"\n\nWarning: Could not validate plugin types: {ex.Message}";
                }
            }
            else
            {
                result.Success = false;
                var errors = string.Join("\n", emitResult.Diagnostics
                    .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                    .Select(d => $"Error {d.Id}: {d.GetMessage()}"));
                result.Message = $"Compilation failed with errors:\n{errors}";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Compilation setup failed: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Validates the plugin code for syntax and structure
    /// </summary>
    private PluginValidationResult ValidatePluginCode(string code)
    {
        var result = new PluginValidationResult();

        try
        {
            // Parse the code to check for syntax errors
            var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(
                code,
                new Microsoft.CodeAnalysis.CSharp.CSharpParseOptions()
            );

            var diagnostics = syntaxTree.GetDiagnostics();
            var errors = diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).ToList();
            var warnings = diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Warning).ToList();

            if (errors.Any())
            {
                result.IsValid = false;
                result.Message = $"Syntax errors found:\n{string.Join("\n", errors.Select(e => $"Line {e.Location.GetLineSpan().StartLinePosition.Line + 1}: {e.GetMessage()}"))}";
            }
            else
            {
                result.IsValid = true;

                // Check for required interfaces and patterns
                var validationMessages = new List<string>();

                // Check if code contains class definition
                if (!code.Contains("class ") && !code.Contains("partial class "))
                {
                    validationMessages.Add("⚠️ No class definition found. Plugin should contain at least one class.");
                }

                // Check for IEffectNode interface implementation
                if (!code.Contains("IEffectNode") && !code.Contains("implements IEffectNode"))
                {
                    validationMessages.Add("⚠️ No IEffectNode interface implementation found. Plugin should implement IEffectNode.");
                }

                // Check for Render method
                if (!code.Contains("void Render(") && !code.Contains("public void Render("))
                {
                    validationMessages.Add("⚠️ No Render method found. Effect nodes should have a Render method.");
                }

                // Check for using statements
                if (!code.Contains("using PhoenixVisualizer.Core.Nodes;") &&
                    !code.Contains("PhoenixVisualizer.Core.Nodes"))
                {
                    validationMessages.Add("ℹ️ Consider adding 'using PhoenixVisualizer.Core.Nodes;' for better code completion.");
                }

                if (warnings.Any())
                {
                    validationMessages.Add($"⚠️ {warnings.Count} warnings found during syntax analysis.");
                }

                if (validationMessages.Any())
                {
                    result.Message = "Code is syntactically valid but has suggestions:\n\n" +
                                   string.Join("\n", validationMessages);
                }
                else
                {
                    result.Message = "✅ Code is valid and well-structured!\n\n" +
                                   "✓ Syntax is correct\n" +
                                   "✓ No compilation errors\n" +
                                   "✓ Follows plugin conventions";
                }
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Message = $"Validation failed: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Ensures required using statements are present in the code
    /// </summary>
    private string EnsureRequiredUsings(string code)
    {
        var requiredUsings = new[]
        {
            "using System;",
            "using System.Collections.Generic;",
            "using System.Linq;",
            "using PhoenixVisualizer.Core.Nodes;",
            "using PhoenixVisualizer.Core;",
            "using Avalonia.Media;"
        };

        var enhancedCode = code;

        foreach (var usingStmt in requiredUsings)
        {
            if (!enhancedCode.Contains(usingStmt))
            {
                enhancedCode = usingStmt + "\n" + enhancedCode;
            }
        }

        return enhancedCode;
    }

    /// <summary>
    /// Shows a message dialog to the user
    /// </summary>
    private async void ShowMessage(string title, string message)
    {
        var dialog = new Avalonia.Controls.Window
        {
            Title = title,
            Width = 500,
            Height = 300,
            WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var textBlock = new Avalonia.Controls.TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(20),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };

        var closeButton = new Avalonia.Controls.Button
        {
            Content = "Close",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Avalonia.Thickness(20),
            Padding = new Avalonia.Thickness(20, 10, 20, 10)
        };

        closeButton.Click += (_, _) => dialog.Close();

        var stackPanel = new Avalonia.Controls.StackPanel();
        stackPanel.Children.Add(textBlock);
        stackPanel.Children.Add(closeButton);

        dialog.Content = stackPanel;

        // Show dialog modally
        await dialog.ShowDialog(this);
    }

    /// <summary>
    /// Remove selected effect
    /// </summary>
    private void OnRemoveEffect(object? sender, RoutedEventArgs e)
    {
        var listBox = this.FindControl<ListBox>("EffectList");
        if (listBox?.SelectedItem is EffectDescriptor selectedEffect)
        {
            _effects.Remove(selectedEffect);
            System.Diagnostics.Debug.WriteLine($"[PluginEditor] Removed effect: {selectedEffect.Name}");
        }
    }

    /// <summary>
    /// Add method to manually trigger effect discovery (for external plugins)
    /// </summary>
    public void ForceRefreshEffects()
    {
        RefreshAvailableEffects();
        System.Diagnostics.Debug.WriteLine($"[PluginEditor] Forced refresh from external call - {_availableEffects.Count} effects found");
    }

    /// <summary>
    /// Get current effect count for monitoring
    /// </summary>
    public int GetAvailableEffectCount() => _availableEffects.Count;
    
    /// <summary>
    /// Get current active effects count
    /// </summary>
    public int GetActiveEffectCount() => _effects.Count;

    /// <summary>
    /// Set up file system watcher for dynamic plugin discovery
    /// </summary>
    private void SetupPluginWatcher()
    {
        try
        {
            // Watch for plugin assemblies in common locations
            var pluginPaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "plugins"),
                Path.Combine(AppContext.BaseDirectory, "effects"),
                Path.Combine(AppContext.BaseDirectory, "bin")
            };

            foreach (var path in pluginPaths.Where(Directory.Exists))
            {
                var watcher = new FileSystemWatcher(path)
                {
                    Filter = "*.dll",
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite,
                    EnableRaisingEvents = true
                };

                watcher.Created += OnPluginFileChanged;
                watcher.Changed += OnPluginFileChanged;
                watcher.Deleted += OnPluginFileChanged;

                _pluginWatcher = watcher; // Keep reference (simplified - could watch multiple directories)
                
                System.Diagnostics.Debug.WriteLine($"[PluginEditor] Watching for plugins in: {path}");
                break; // For now, just watch the first existing directory
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PluginEditor] Could not set up plugin watcher: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle file system changes in plugin directories
    /// </summary>
    private void OnPluginFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce rapid file changes and refresh effects
        Task.Delay(1000).ContinueWith(_ => 
        {
            Dispatcher.UIThread.Post(() =>
            {
                RefreshAvailableEffects();
                System.Diagnostics.Debug.WriteLine($"[PluginEditor] Plugin file changed: {e.FullPath}, refreshing effects");
            });
        });
    }

    /// <summary>
    /// Clean up resources when window closes
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        _refreshTimer?.Dispose();
        _pluginWatcher?.Dispose();
        base.OnClosed(e);
    }
}

/// <summary>
/// Result of a plugin compilation operation
/// </summary>
public class PluginCompilationResult
{
    public bool Success { get; set; }
    public string OutputFile { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Result of a plugin validation operation
/// </summary>
public class PluginValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class EffectDescriptor
{
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public IEffectNode? Node { get; set; }
}

// Full AVS binary converter
public static class AvsConverter
{
    public static string LoadAvs(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);

        // Verify header
        var header = new string(br.ReadChars(32)).TrimEnd('\0');
        if (!header.Contains("Nullsoft AVS"))
            throw new InvalidDataException("Not a valid AVS preset.");

        // AVS presets store number of objects, then serialized ops
        int effectCount = br.ReadInt32();
        var effects = new List<object>();
        string init = "", frame = "", point = "", beat = "";
        bool clearEveryFrame = true;

        for (int i = 0; i < effectCount; i++)
        {
            int id = br.ReadInt32();
            int size = br.ReadInt32();
            byte[] blob = br.ReadBytes(size);

            // Known IDs for AVS components
            switch (id)
            {
                case 0x01: // Superscope / point script
                    point = ExtractString(blob);
                    effects.Add(new { type = "superscope" });
                    break;
                case 0x02: // Trans / per frame
                    frame = ExtractString(blob);
                    break;
                case 0x03: // Init code
                    init = ExtractString(blob);
                    break;
                case 0x04: // On beat
                    beat = ExtractString(blob);
                    break;
                case 0x05: // Clear every frame toggle
                    clearEveryFrame = blob[0] != 0;
                    break;
                default:
                    // Preserve raw AVS effect data for round-trip
                    effects.Add(new {
                        type = "avs_raw",
                        id,
                        blob = Convert.ToBase64String(blob)
                    });
                    break;
            }
        }

        // Build PHX JSON schema
        var json = new
        {
            init,
            frame,
            point,
            beat,
            clearEveryFrame,
            effects
        };
        return System.Text.Json.JsonSerializer.Serialize(json, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static string ExtractString(byte[] data)
    {
        try
        {
            return System.Text.Encoding.ASCII.GetString(data).TrimEnd('\0');
        }
        catch
        {
            return "// (unreadable AVS code block)";
        }
    }

    public static void SaveAvs(string path, string phxJson)
    {
        var doc = JsonDocument.Parse(phxJson);
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);

        // Header: 32 bytes, padded
        var header = "Nullsoft AVS Preset";
        var headerBytes = new byte[32];
        System.Text.Encoding.ASCII.GetBytes(header, 0, header.Length, headerBytes, 0);
        bw.Write(headerBytes);

        var effects = new List<JsonElement>();
        if (doc.RootElement.TryGetProperty("effects", out var effs))
        {
            foreach (var e in effs.EnumerateArray()) effects.Add(e);
        }

        bw.Write(effects.Count + 4); // effects + 4 code ops

        // Write each block as id + size + data
        void WriteBlock(int id, string? text)
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes(text ?? "");
            bw.Write(id);
            bw.Write(bytes.Length);
            bw.Write(bytes);
        }

        if (doc.RootElement.TryGetProperty("point", out var point))
            WriteBlock(0x01, point.GetString());
        if (doc.RootElement.TryGetProperty("frame", out var frame))
            WriteBlock(0x02, frame.GetString());
        if (doc.RootElement.TryGetProperty("init", out var init))
            WriteBlock(0x03, init.GetString());
        if (doc.RootElement.TryGetProperty("beat", out var beat))
            WriteBlock(0x04, beat.GetString());

        bool clear = doc.RootElement.TryGetProperty("clearEveryFrame", out var cf) && cf.GetBoolean();
        bw.Write(0x05);
        bw.Write(1);
        bw.Write(new byte[] { clear ? (byte)1 : (byte)0 });

        // Remaining effects
        foreach (var e in effects)
        {
            if (e.TryGetProperty("type", out var typeEl) &&
                typeEl.GetString() == "avs_raw")
            {
                int id = e.GetProperty("id").GetInt32();
                var blob = Convert.FromBase64String(e.GetProperty("blob").GetString() ?? "");
                bw.Write(id);
                bw.Write(blob.Length);
                bw.Write(blob);
            }
            else
            {
                var type = e.GetProperty("type").GetString() ?? "unknown";
                var bytes = System.Text.Encoding.ASCII.GetBytes(type);
                bw.Write(0x99); // generic placeholder ID
                bw.Write(bytes.Length);
                bw.Write(bytes);
            }
        }
    }
}
}
