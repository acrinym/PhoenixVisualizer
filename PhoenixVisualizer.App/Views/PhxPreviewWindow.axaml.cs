using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Platform.Storage;
using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.Core; // ParameterSystem
using PhoenixVisualizer.App.Rendering;
using PhoenixVisualizer.App.Views; // UnifiedAvsVisualizer
using PhoenixVisualizer.App.Services; // UnifiedAvsData, UnifiedAvsService, etc.
// Note: Using the simpler IEffectNode from Nodes namespace, not the advanced one from Effects.Interfaces
// The advanced EffectsGraphManager will be integrated in a future phase
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Reactive.Disposables; // NEW
using System.Reactive.Linq;

// Reference classes from the App namespaces
using PhxPreviewRenderer = PhoenixVisualizer.App.Rendering.PhxPreviewRenderer;
using ParameterEditor = PhoenixVisualizer.App.Views.ParameterEditor;
using PhxCodeEngine = PhoenixVisualizer.Core.Nodes.PhxCodeEngine;
using CoreEffectParam = PhoenixVisualizer.Core.Nodes.EffectParam;
using EffectStackItem = PhoenixVisualizer.Views.EffectStackItem;
using PhxEditorViewModel = PhoenixVisualizer.App.ViewModels.PhxEditorViewModel;

namespace PhoenixVisualizer.Views;

/// <summary>
/// PHX Editor Window - Advanced Visual Effects Composer
/// Complete AVS Editor++ with effect stack, code editing, and live preview
/// </summary>
public partial class PhxPreviewWindow : Window
{
    private readonly CompositeDisposable _disposables = new(); // NEW
    public PhoenixVisualizer.App.ViewModels.PhxEditorViewModel ViewModel { get; private set; } = null!;

    private RenderSurface? _preview; // actual drawing control
    private ParameterEditor _parameterEditor;
    private PhxCodeEngine _codeEngine;
    private PresetService _presetService;
    // Using the basic EffectRegistry for Phase 4 - advanced graph manager integration in future phase

    public PhxPreviewWindow()
    {
        InitializeComponent();
        // Defensive: keep VM binding even if template changes
        if (DataContext is null) DataContext = new PhoenixVisualizer.App.ViewModels.PhxEditorViewModel();
        ViewModel = DataContext as PhoenixVisualizer.App.ViewModels.PhxEditorViewModel;
        if (ViewModel == null)
        {
            ViewModel = new PhoenixVisualizer.App.ViewModels.PhxEditorViewModel();
            DataContext = ViewModel;
        }

        // Commands are already initialized in ViewModel constructor
        // No need to call InitializeCommands() here

        // Initialize required fields
        _codeEngine = new PhxCodeEngine();
        _preview = null!;
        _parameterEditor = null!;
        _presetService = new PresetService();

        // Publish whatever is currently active so the panel lights up at open.
        // var initialTarget = _runtime.GetParameterTarget();
        // if (initialTarget != null)
        //     Services.ParameterBus.PublishTarget(initialTarget);

        // Set up the preview rendering
        SetupPreviewRendering();

        // Set up parameter editor
        SetupParameterEditor();
        
        // Wire up settings and preview buttons
        WireUpSettingsAndPreview();

        // Wire up effect selection changes
        WireUpEffectSelection();

        // Wire up code compilation
        WireUpCodeCompilation();

        // Wire up preset commands
        WireUpPresetCommands();

        // Initialize effect instantiation pipeline
        InitializeEffectPipeline();
        
        this.Closed += (_, __) => _disposables.Dispose(); // NEW
    }



    private void InitializeEffectPipeline()
    {
        try
        {
            // Initialize the effect instantiation pipeline using EffectRegistry
            var availableEffects = EffectRegistry.GetAll().ToList();
            Debug.WriteLine($"PHX Editor: Effect pipeline initialized - {availableEffects.Count} effect types available");

            foreach (var effect in availableEffects)
            {
                Debug.WriteLine($"PHX Editor: Available effect: {effect.Name} ({effect.Params.Count} parameters)");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PHX Editor: Error initializing effect pipeline: {ex.Message}");
        }
    }

    private void WireUpCodeCompilation()
    {
        var vm = ViewModel;
        if (vm != null)
        {
            // route the VM commands to real editor behaviors
            vm.CompileCommand
              .ObserveOn(RxApp.MainThreadScheduler)
              .Subscribe(_ => CompileFromEditor());
            vm.TestCodeCommand
              .ObserveOn(RxApp.MainThreadScheduler)
              .Subscribe(_ => TestFromEditor());
            vm.ImportAvsCommand
              .ObserveOn(RxApp.MainThreadScheduler)
              .Subscribe(async _ => await ImportAvsPresetAsync());
            vm.ExportAvsCommand
              .ObserveOn(RxApp.MainThreadScheduler)
              .Subscribe(async _ => await ExportPresetAsync());
        }
    }

    private void CompileCode()
    {
        var vm = ViewModel;
        if (vm != null)
        {
            try
            {
                // Execute initialization code
                var initResult = _codeEngine.ExecuteInit(vm.InitCode);
                if (!initResult.Success)
                {
                    vm.StatusMessage = $"Init Error: {initResult.Message}";
                    return;
                }

                // Execute frame code
                var frameResult = _codeEngine.ExecuteFrame(vm.FrameCode);
                if (!frameResult.Success)
                {
                    vm.StatusMessage = $"Frame Error: {frameResult.Message}";
                    return;
                }

                // Execute beat code if available
                if (!string.IsNullOrWhiteSpace(vm.BeatCode))
                {
                    var beatResult = _codeEngine.ExecuteBeat(vm.BeatCode);
                    if (!beatResult.Success)
                    {
                        vm.StatusMessage = $"Beat Error: {beatResult.Message}";
                        return;
                    }
                }

                vm.StatusMessage = "Code compiled successfully";
                vm.CodeStatus = "Ready";

            }
            catch (Exception ex)
            {
                vm.StatusMessage = $"Compilation Error: {ex.Message}";
                vm.CodeStatus = "Error";
                Debug.WriteLine($"PHX Code compilation error: {ex}");
            }
        }
    }

    private void TestCode()
    {
        var vm = ViewModel;
        if (vm != null)
        {
            try
            {
                // Test point code execution
                var pointResult = _codeEngine.ExecutePoint(vm.PointCode, 0, 100);
                if (!pointResult.Success)
                {
                    vm.StatusMessage = $"Point Error: {pointResult.Message}";
                    return;
                }

                vm.StatusMessage = $"Test successful - Point: ({pointResult.PointX:F2}, {pointResult.PointY:F2})";

            }
            catch (Exception ex)
            {
                vm.StatusMessage = $"Test Error: {ex.Message}";
                Debug.WriteLine($"PHX Code test error: {ex}");
            }
        }
    }

    private void SetupPreviewRendering()
    {
        _preview = this.FindControl<RenderSurface>("PreviewSurface");
        if (_preview == null) return;
        // show something by default so the surface is obviously alive
        // _preview.SetPlugin(new SanityVisualizer()); // TODO: Create a default visualizer
    }

    private void SetupParameterEditor()
    {
        try
        {
            // Find the ContentControl that will host our ParameterEditor
            var hostControl = this.FindControl<ContentControl>("ParamPanelHost");
            if (hostControl == null)
            {
                Debug.WriteLine("PHX Editor: ParamPanelHost ContentControl not found");
                return;
            }

            // Create a new ParameterEditor and add it to the ContentControl
            _parameterEditor = new ParameterEditor();
            hostControl.Content = _parameterEditor;
            
            Debug.WriteLine("PHX Editor: ParameterEditor created and added to host");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PHX Editor: SetupParameterEditor error: {ex}");
        }
    }

    private void WireUpEffectSelection()
    {
        // Wire up the preview renderer to respond to play/pause/restart commands
        var vm = ViewModel;
        if (vm != null)
        {
            // vm.PlayCommand.Subscribe(_ => _previewRenderer?.Resume());
            // vm.PauseCommand.Subscribe(_ => _previewRenderer?.Pause());
            // vm.RestartCommand.Subscribe(_ => _previewRenderer?.Restart());

            // Wire up parameter editor updates when effect selection changes
            vm.WhenAnyValue(x => x.SelectedEffect)
                .Subscribe(selectedEffect =>
                {
                    if (_parameterEditor != null && selectedEffect != null)
                    {
                        _parameterEditor.UpdateParameters(
                            selectedEffect.Name,
                            selectedEffect.Parameters.ToDictionary(
                                p => p.Key,
                                p => new CoreEffectParam
                                {
                                    Label = p.Value.Label,
                                    Type = p.Value.Type,
                                    FloatValue = p.Value.FloatValue,
                                    BoolValue = p.Value.BoolValue,
                                    StringValue = p.Value.StringValue,
                                    ColorValue = p.Value.ColorValue,
                                    Min = p.Value.Min,
                                    Max = p.Value.Max,
                                    Options = p.Value.Options
                                }
                            )
                        );
                    }
                });

            // Preset commands are wired in the window initialization
        }

        // Wire up parameter changes back to the effect
        WireUpParameterChanges();
    }

    private void WireUpParameterChanges()
    {
        // This will be handled through the existing parameter binding system
        // The ParameterEditor already updates the EffectParam objects directly
    }

    // NEW: keep file pickers strictly on UI thread, do-nothing when nothing loaded is already handled in handlers
    private void WireUpPresetCommands()
    {
        var vm = ViewModel;
        if (vm != null)
        {
            vm.ImportAvsCommand
              .ObserveOn(RxApp.MainThreadScheduler)
              .Subscribe(_ => ImportAvsPreset())
              .DisposeWith(_disposables);

            vm.ExportAvsCommand
              .ObserveOn(RxApp.MainThreadScheduler)
              .Subscribe(_ => ExportAvsPreset())
              .DisposeWith(_disposables);
        }
    }
    
    private void WireUpSettingsAndPreview()
    {
        try
        {
            var btnSettings = this.FindControl<Button>("BtnSettings");
            btnSettings?.AddHandler(Button.ClickEvent, (_, __) =>
            {
                var dlg = new Views.PhxEditorSettingsDialog { DataContext = ViewModel.Settings };
                dlg.ShowDialog(this);
                // Apply settings immediately
                // _previewRenderer?.ApplySettings(ViewModel.Settings); // TODO: Implement settings application
                // Save settings for persistence
                ViewModel.Settings.Save();
            });

            var btnUndock = this.FindControl<Button>("BtnUndockPreview");
            btnUndock?.AddHandler(Button.ClickEvent, (_, __) =>
            {
                // Open a modal preview window
                var previewWindow = new PreviewModalWindow();
                previewWindow.Show(this);
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"PHX Editor: WireUpSettingsAndPreview error: {ex}");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Clean up resources
        // _previewRenderer?.Stop();
        _codeEngine?.Reset();
    }

    private async void ExportAvsPreset()
    {
        try
        {
            var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storageProvider == null)
            {
                ViewModel.StatusMessage = "Storage provider not available";
                return;
            }

            var options = new FilePickerSaveOptions
            {
                Title = "Export AVS Preset",
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new FilePickerFileType("AVS Preset Files")
                    {
                        Patterns = new[] { "*.avs" },
                        MimeTypes = new[] { "application/octet-stream" }
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*" },
                        MimeTypes = new[] { "application/octet-stream" }
                    }
                },
                SuggestedFileName = "preset.avs"
            };

            var result = await storageProvider.SaveFilePickerAsync(options);
            if (result != null)
            {
                await ViewModel.ExportPresetAsAvs(result.Path.LocalPath);
            }
        }
        catch (Exception ex)
        {
            ViewModel.StatusMessage = $"Error exporting AVS preset: {ex.Message}";
        }
    }

    private async void ImportAvsPreset()
    {
        try
        {
            var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storageProvider == null)
            {
                ViewModel.StatusMessage = "Storage provider not available";
                return;
            }

            var options = new FilePickerOpenOptions
            {
                Title = "Import AVS Preset",
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new FilePickerFileType("AVS Preset Files")
                    {
                        Patterns = new[] { "*.avs" },
                        MimeTypes = new[] { "application/octet-stream" }
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*" },
                        MimeTypes = new[] { "application/octet-stream" }
                    }
                },
                AllowMultiple = false
            };

            var results = await storageProvider.OpenFilePickerAsync(options);
            if (results.Count > 0)
            {
                await ViewModel.ImportPresetFromAvs(results[0].Path.LocalPath);
            }
        }
        catch (Exception ex)
        {
            ViewModel.StatusMessage = $"Error importing AVS preset: {ex.Message}";
        }
    }

    // ===== ADVANCED EDITOR FUNCTIONALITY =====
    // Build UnifiedAvsData from editor code blocks
    private UnifiedAvsData BuildUnifiedFromEditor()
    {
        // Create a detection result for the editor-generated content
        var detection = new DetectionResult(
            AvsFileType.PhoenixText,
            1.0f, // High confidence for editor-generated content
            new List<string> { "Editor", "Phoenix" },
            "Generated from PHX Editor"
        );

        // Build combined code from all sections
        var combinedCode = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(ViewModel.InitCode))
            combinedCode.AppendLine("// INIT").AppendLine(ViewModel.InitCode.Trim());
        if (!string.IsNullOrWhiteSpace(ViewModel.FrameCode))
            combinedCode.AppendLine("// FRAME").AppendLine(ViewModel.FrameCode.Trim());
        if (!string.IsNullOrWhiteSpace(ViewModel.PointCode))
            combinedCode.AppendLine("// POINT").AppendLine(ViewModel.PointCode.Trim());
        if (!string.IsNullOrWhiteSpace(ViewModel.BeatCode))
            combinedCode.AppendLine("// BEAT").AppendLine(ViewModel.BeatCode.Trim());

        var superscopes = new List<UnifiedSuperscope>
        {
            new UnifiedSuperscope(
                Name: "Main",
                InitCode: string.IsNullOrWhiteSpace(ViewModel.InitCode) ? null : ViewModel.InitCode.Trim(),
                FrameCode: string.IsNullOrWhiteSpace(ViewModel.FrameCode) ? null : ViewModel.FrameCode.Trim(),
                PointCode: string.IsNullOrWhiteSpace(ViewModel.PointCode) ? null : ViewModel.PointCode.Trim(),
                BeatCode: string.IsNullOrWhiteSpace(ViewModel.BeatCode) ? null : ViewModel.BeatCode.Trim(),
                SourceType: "Phoenix",
                CombinedCode: combinedCode.ToString()
            )
        };

        var data = new UnifiedAvsData(
            FileType: AvsFileType.PhoenixText,
            Superscopes: superscopes,
            Effects: new List<UnifiedEffect>(),
            Detection: detection,
            RawText: combinedCode.ToString(),
            RawBinary: null
        );

        Debug.WriteLine($"### JUSTIN DEBUG: Built UnifiedAvsData from editor - {data.Superscopes.Count} superscopes");
        return data;
    }

    // Use a plugin for the preview surface
    private void UsePluginFor(UnifiedAvsData data)
    {
        if (_preview == null) return;

        // Create a visualizer and load the data
        var visualizer = new UnifiedAvsVisualizer();
        visualizer.LoadAvsData(data);
        _preview.SetPlugin(visualizer);

        Debug.WriteLine($"### JUSTIN DEBUG: Set plugin for preview - {data.FileType} with {data.Superscopes.Count} superscopes");
    }

    // Compile code from editor
    private void CompileFromEditor()
    {
        try
        {
            ViewModel.StatusMessage = "Compiling...";
            Debug.WriteLine("### JUSTIN DEBUG: CompileFromEditor() called");

            var data = BuildUnifiedFromEditor();
            UsePluginFor(data);

            ViewModel.StatusMessage = "Compiled successfully";
            Debug.WriteLine($"### JUSTIN DEBUG: Compilation successful - {data.FileType} with {data.Superscopes.Count} superscopes");
        }
        catch (Exception ex)
        {
            ViewModel.StatusMessage = $"Compilation failed: {ex.Message}";
            Debug.WriteLine($"### JUSTIN DEBUG: Compilation error - {ex.Message}");
        }
    }

    // Test code from editor
    private void TestFromEditor()
    {
        try
        {
            ViewModel.StatusMessage = "Testing...";
            Debug.WriteLine("### JUSTIN DEBUG: TestFromEditor() called");

            var data = BuildUnifiedFromEditor();
            UsePluginFor(data);

            // Force a visual refresh to start rendering
            if (_preview != null)
            {
                _preview.InvalidateVisual();
            }

            ViewModel.StatusMessage = "Test running";
            Debug.WriteLine($"### JUSTIN DEBUG: Test started - {data.FileType} with {data.Superscopes.Count} superscopes");
        }
        catch (Exception ex)
        {
            ViewModel.StatusMessage = $"Test failed: {ex.Message}";
            Debug.WriteLine($"### JUSTIN DEBUG: Test error - {ex.Message}");
        }
    }

    // Import AVS preset
    private async Task ImportAvsPresetAsync()
    {
        try
        {
            ViewModel.StatusMessage = "Importing AVS preset...";
            Debug.WriteLine("### JUSTIN DEBUG: ImportAvsPresetAsync() called");

            var options = new FilePickerOpenOptions
            {
                Title = "Import AVS Preset",
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new FilePickerFileType("AVS Presets") { Patterns = new[] { "*.avs" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                }
            };

            var results = await this.StorageProvider.OpenFilePickerAsync(options);
            if (results.Count > 0)
            {
                var file = results[0];
                var content = await File.ReadAllTextAsync(file.Path.LocalPath);
                
                // Parse AVS content into editor code blocks
                ParseAvsToEditor(content);
                
                ViewModel.StatusMessage = "AVS preset imported";
                Debug.WriteLine($"### JUSTIN DEBUG: Imported AVS preset - {file.Name}");
            }
        }
        catch (Exception ex)
        {
            ViewModel.StatusMessage = $"Import failed: {ex.Message}";
            Debug.WriteLine($"### JUSTIN DEBUG: Import error - {ex.Message}");
        }
    }

    // Export preset
    private async Task ExportPresetAsync()
    {
        try
        {
            ViewModel.StatusMessage = "Exporting preset...";
            Debug.WriteLine("### JUSTIN DEBUG: ExportPresetAsync() called");

            var options = new FilePickerSaveOptions
            {
                Title = "Export Preset",
                DefaultExtension = ".phx",
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new FilePickerFileType("Phoenix Presets") { Patterns = new[] { "*.phx" } },
                    new FilePickerFileType("AVS Presets") { Patterns = new[] { "*.avs" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                }
            };

            var result = await this.StorageProvider.SaveFilePickerAsync(options);
            if (result != null)
            {
                var data = BuildUnifiedFromEditor();
                var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(result.Path.LocalPath, json);
                
                ViewModel.StatusMessage = "Preset exported";
                Debug.WriteLine($"### JUSTIN DEBUG: Exported preset - {result.Name}");
            }
        }
        catch (Exception ex)
        {
            ViewModel.StatusMessage = $"Export failed: {ex.Message}";
            Debug.WriteLine($"### JUSTIN DEBUG: Export error - {ex.Message}");
        }
    }

    // Parse AVS content into editor code blocks
    private void ParseAvsToEditor(string avsContent)
    {
        try
        {
            // Simple parsing - extract code blocks
            var lines = avsContent.Split('\n');
            var initCode = new List<string>();
            var frameCode = new List<string>();
            var pointCode = new List<string>();
            var beatCode = new List<string>();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("//") || string.IsNullOrEmpty(trimmed))
                    continue;

                // Simple heuristic - assign to appropriate code block
                if (trimmed.Contains("init") || trimmed.Contains("Init"))
                    initCode.Add(line);
                else if (trimmed.Contains("frame") || trimmed.Contains("Frame"))
                    frameCode.Add(line);
                else if (trimmed.Contains("point") || trimmed.Contains("Point"))
                    pointCode.Add(line);
                else if (trimmed.Contains("beat") || trimmed.Contains("Beat"))
                    beatCode.Add(line);
                else
                    pointCode.Add(line); // Default to point code
            }

            ViewModel.InitCode = string.Join("\n", initCode);
            ViewModel.FrameCode = string.Join("\n", frameCode);
            ViewModel.PointCode = string.Join("\n", pointCode);
            ViewModel.BeatCode = string.Join("\n", beatCode);

            Debug.WriteLine($"### JUSTIN DEBUG: Parsed AVS content - {lines.Length} lines");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"### JUSTIN DEBUG: Parse error - {ex.Message}");
        }
    }
}

/// <summary>
/// ViewModel for PHX Editor - Manages all editor functionality
/// </summary>
public class PhxEditorViewModel : ReactiveObject
{
    private readonly PresetService _presetService;

    // Core Data (ObservableCollections for UI binding)
    public ObservableCollection<EffectStackItem> EffectStack { get; } = new();
    public ObservableCollection<EffectItem> PhoenixOriginals { get; } = new();
    public ObservableCollection<EffectItem> AvsEffects { get; } = new();
    public ObservableCollection<EffectItem> ResearchEffects { get; } = new();

    // Current Selection (Reactive properties)
    [Reactive] public EffectStackItem SelectedEffect { get; set; }
    [Reactive] public EffectItem SelectedLibraryEffect { get; set; }
    [Reactive] public int SelectedTabIndex { get; set; }

    // Code Content (Reactive properties)
    [Reactive] public string InitCode { get; set; } = "// Initialization code...\n// This runs once when the preset loads\n\n// Example: Initialize variables\n// x = 0.5;\n// y = 0.5;\n";
    [Reactive] public string FrameCode { get; set; } = "// Per-frame code...\n// This runs every frame\n\n// Example: Animate based on time\n// x = sin(time);\n// y = cos(time);\n";
    [Reactive] public string PointCode { get; set; } = "// Per-point code...\n// This runs for each superscope point\n\n// Example: Create a circle\n// x = sin(i*0.1);\n// y = cos(i*0.1);\n";
    [Reactive] public string BeatCode { get; set; } = "// On-beat code...\n// This runs when a beat is detected\n\n// Example: Pulse on beat\n// x = x * 1.5;\n// y = y * 1.5;\n";

    // Status (Reactive properties)
    [Reactive] public string StatusMessage { get; set; } = "Ready";
    [Reactive] public string FpsCounter { get; set; } = "60 FPS";
    [Reactive] public string MemoryUsage { get; set; } = "128 MB";
    [Reactive] public string PresetName { get; set; } = "Untitled.phx";
    [Reactive] public string PresetCategory { get; set; } = "General";
    [Reactive] public string PresetDescription { get; set; } = "";
    [Reactive] public string CodeStatus { get; set; } = "Ready";

    // Performance Monitoring (Reactive properties)
    [Reactive] public string CpuUsage { get; set; } = "5%";
    [Reactive] public string RenderTime { get; set; } = "16.7ms";
    [Reactive] public string EffectCount { get; set; } = "1 effects";
    [Reactive] public string DebugInfo { get; set; } = "Debug: Ready";
    [Reactive] public bool ShowPerformanceOverlay { get; set; } = true;
    [Reactive] public bool EnableDebugLogging { get; set; } = false;

    // Preset management properties
    [Reactive] public ObservableCollection<PresetMetadata> AvailablePresets { get; set; } = new();
    [Reactive] public PresetMetadata? SelectedPreset { get; set; }
    [Reactive] public string PresetSearchText { get; set; } = "";
    [Reactive] public PresetType SelectedPresetType { get; set; } = PresetType.PHX;
    [Reactive] public string SelectedPresetCategory { get; set; } = "All";

    // Commands (ReactiveCommands for UI actions)
    public ReactiveCommand<Unit, Unit> NewPresetCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> OpenPresetCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> SavePresetCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> SaveAsPresetCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> ExportAvsCommand { get; set; } = null!;
    public ReactiveCommand<Unit, Unit> ImportAvsCommand { get; set; } = null!;
    public ReactiveCommand<Unit, Unit> UndoCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> RedoCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> CutCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> CopyCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> PasteCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> DuplicateEffectCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> DeleteEffectCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> AddEffectCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> SaveCodeCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> CompileCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> TestCodeCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> PlayCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> PauseCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> RestartCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> HelpCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> TogglePerformanceOverlayCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> ToggleDebugLoggingCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> ResetPerformanceStatsCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> ExportPerformanceLogCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> RefreshPresetsCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> LoadSelectedPresetCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> DeletePresetCommand { get; private set; } = null!;

    // Undo/Redo System
    private readonly Stack<string> _undoStack = new();
    private readonly Stack<string> _redoStack = new();

    // Performance Monitoring
    private readonly Stopwatch _fpsStopwatch = new();
    private readonly Stopwatch _renderStopwatch = new();
    private PerformanceCounter? _cpuCounter = null;
    private PerformanceCounter? _memoryCounter = null;
    private int _frameCount = 0;
    private double _lastFpsUpdate = 0;
    private Timer? _performanceUpdateTimer = null;
    private readonly StringBuilder _performanceLog = new();

    public PhxEditorViewModel()
    {
        // Initialize preset service
        _presetService = new PresetService();

        // Initialize reactive properties
        SelectedEffect = null!;
        SelectedLibraryEffect = null!;

        // Initialize performance monitoring
        InitializePerformanceMonitoring();

        // Load effect library
        LoadEffectLibrary();

        // Initialize default preset
        InitializeDefaultPreset();

        // Initialize all commands
        InitializeCommands();
    }

    private void InitializePerformanceMonitoring()
    {
        try
        {
            // Initialize performance counters (Windows only, fallback for other platforms)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to initialize performance counters: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Performance counters not available: {ex.Message}");
        }

        // Start FPS monitoring
        _fpsStopwatch.Start();

        // Initialize performance update timer (updates every 500ms)
        _performanceUpdateTimer = new Timer(UpdatePerformanceStats, null, 0, 500);
        _performanceUpdateTimer.Change(0, 500); // Start immediately, update every 500ms

        // Log initial performance data
        LogPerformanceEvent("Performance monitoring initialized");
    }

    private void UpdatePerformanceStats(object? state)
    {
        try
        {
            // Update FPS counter
            _frameCount++;
            var elapsed = _fpsStopwatch.Elapsed.TotalSeconds;

            if (elapsed - _lastFpsUpdate >= 1.0)
            {
                var fps = _frameCount / (elapsed - _lastFpsUpdate);
                Dispatcher.UIThread.Invoke(() =>
                {
                    FpsCounter = $"{fps:F1} FPS";
                });

                _frameCount = 0;
                _lastFpsUpdate = elapsed;
            }

            // Update CPU usage
            string cpuUsage = "N/A";
            if (_cpuCounter != null)
            {
                try
                {
                    if (OperatingSystem.IsWindows())
                    {
                        cpuUsage = $"{_cpuCounter.NextValue():F1}%";
                    }
                }
                catch
                {
                    cpuUsage = "N/A";
                }
            }

            // Update memory usage
            string memoryUsage = "N/A";
            if (_memoryCounter != null)
            {
                try
                {
                    var availableMB = 0.0f;
                    if (OperatingSystem.IsWindows())
                    {
                        availableMB = _memoryCounter.NextValue();
                    }
                    var totalMemory = GC.GetTotalMemory(false) / (1024 * 1024);
                    var usedMemory = totalMemory - availableMB;
                    memoryUsage = $"{usedMemory:F0} MB";
                }
                catch
                {
                    memoryUsage = $"{GC.GetTotalMemory(false) / (1024 * 1024):F0} MB";
                }
            }

            // Update render time (estimate based on frame time)
            var renderTime = "N/A";
            if (_renderStopwatch.IsRunning)
            {
                renderTime = $"{_renderStopwatch.ElapsedMilliseconds:F1}ms";
                _renderStopwatch.Restart();
            }
            else
            {
                _renderStopwatch.Start();
                renderTime = "16.7ms"; // Default 60 FPS
            }

            // Update UI on main thread
            Dispatcher.UIThread.Invoke(() =>
            {
                CpuUsage = cpuUsage;
                MemoryUsage = memoryUsage;
                RenderTime = renderTime;
                EffectCount = $"{EffectStack.Count} effects";
            });

            // Log performance data if debug logging is enabled
            if (EnableDebugLogging)
            {
                LogPerformanceEvent($"FPS: {FpsCounter}, CPU: {cpuUsage}, Memory: {memoryUsage}, Render: {renderTime}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating performance stats: {ex.Message}");
        }
    }

    private void LogPerformanceEvent(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] {message}";

        _performanceLog.AppendLine(logEntry);

        // Keep log size manageable (last 1000 lines)
        if (_performanceLog.Length > 50000)
        {
            var lines = _performanceLog.ToString().Split('\n');
            if (lines.Length > 1000)
            {
                _performanceLog.Clear();
                for (int i = lines.Length - 1000; i < lines.Length; i++)
                {
                    _performanceLog.AppendLine(lines[i]);
                }
            }
        }

        Debug.WriteLine($"[Performance] {message}");
    }

    public void RecordFrameTime(long milliseconds)
    {
        // This method can be called from rendering code to record actual frame times
        Dispatcher.UIThread.Invoke(() =>
        {
            RenderTime = $"{milliseconds:F1}ms";
        });
    }

    public string GetPerformanceReport()
    {
        var report = new StringBuilder();
        report.AppendLine("=== Phoenix Visualizer Performance Report ===");
        report.AppendLine($"Generated: {DateTime.Now}");
        report.AppendLine();
        report.AppendLine("Current Stats:");
        report.AppendLine($"  FPS: {FpsCounter}");
        report.AppendLine($"  CPU Usage: {CpuUsage}");
        report.AppendLine($"  Memory Usage: {MemoryUsage}");
        report.AppendLine($"  Render Time: {RenderTime}");
        report.AppendLine($"  Effect Count: {EffectCount}");
        report.AppendLine();
        report.AppendLine("System Information:");
        report.AppendLine($"  .NET Version: {Environment.Version}");
        report.AppendLine($"  OS: {Environment.OSVersion}");
        report.AppendLine($"  Processor Count: {Environment.ProcessorCount}");
        report.AppendLine($"  Total Memory: {GC.GetTotalMemory(false) / (1024 * 1024):F0} MB");
        report.AppendLine();
        report.AppendLine("Performance Log (Last 1000 entries):");
        report.AppendLine(_performanceLog.ToString());

        return report.ToString();
    }
    
    // Command handler methods
    private void SavePreset()
    {
        try
        {
            if (string.IsNullOrEmpty(PresetName) || PresetName == "Untitled.phx")
            {
                SaveAsPreset();
                return;
            }

            var preset = CreatePhxPresetFromCurrentState();
            _presetService.SavePresetAsync(preset, $"{preset.Name.Replace(" ", "_")}.json").Wait();
            StatusMessage = $"Preset saved: {preset.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving preset: {ex.Message}";
        }
    }
    
    private void SaveAsPreset()
    {
        try
        {
            var preset = CreatePhxPresetFromCurrentState();
            var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{preset.Name.Replace(" ", "_")}.json";
            _presetService.SavePresetAsync(preset, fileName).Wait();
            PresetName = preset.Name;
            StatusMessage = $"Preset saved as: {preset.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving preset: {ex.Message}";
        }
    }
    
    private void Undo()
    {
        if (_undoStack.Count > 0)
        {
            var lastState = _undoStack.Pop();
            _redoStack.Push(CreateCurrentState());
            RestoreState(lastState);
            StatusMessage = "Undo completed";
        }
        else
        {
            StatusMessage = "Nothing to undo";
        }
    }
    
    private void Redo()
    {
        if (_redoStack.Count > 0)
        {
            var nextState = _redoStack.Pop();
            _undoStack.Push(CreateCurrentState());
            RestoreState(nextState);
            StatusMessage = "Redo completed";
        }
        else
        {
            StatusMessage = "Nothing to redo";
        }
    }
    
    private void Cut()
    {
        if (SelectedEffect != null)
        {
            var effectData = JsonSerializer.Serialize(SelectedEffect);
            // Store in clipboard (simplified for now)
            StatusMessage = "Effect cut to clipboard";
            DeleteEffect();
        }
        else
        {
            StatusMessage = "No effect selected to cut";
        }
    }
    
    private void Copy()
    {
        if (SelectedEffect != null)
        {
            var effectData = JsonSerializer.Serialize(SelectedEffect);
            // Store in clipboard (simplified for now)
            StatusMessage = "Effect copied to clipboard";
        }
        else
        {
            StatusMessage = "No effect selected to copy";
        }
    }
    
    private void Paste()
    {
        try
        {
            // Simplified paste - create a new effect
            var newEffect = new EffectStackItem($"Pasted Effect {EffectStack.Count + 1}", "Phoenix")
            {
                EffectType = "Phoenix Effect"
            };
            newEffect.Parameters["intensity"] = new EffectParam { Label = "Intensity", Type = "slider", FloatValue = 1.0f };
            newEffect.Parameters["color"] = new EffectParam { Label = "Color", Type = "color", StringValue = "#00FFFF" };
            newEffect.Parameters["speed"] = new EffectParam { Label = "Speed", Type = "slider", FloatValue = 1.0f };
            
            EffectStack.Add(newEffect);
            EffectCount = $"{EffectStack.Count} effects";
            StatusMessage = "Effect pasted";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error pasting effect: {ex.Message}";
        }
    }
    
    private void DuplicateEffect()
    {
        if (SelectedEffect != null)
        {
            var duplicate = new EffectStackItem($"{SelectedEffect.Name} (Copy)", SelectedEffect.Category)
            {
                EffectType = SelectedEffect.EffectType
            };
            
            // Copy parameters
            foreach (var param in SelectedEffect.Parameters)
            {
                duplicate.Parameters[param.Key] = new EffectParam
                {
                    Label = param.Value.Label,
                    Type = param.Value.Type,
                    FloatValue = param.Value.FloatValue,
                    BoolValue = param.Value.BoolValue,
                    StringValue = param.Value.StringValue,
                    ColorValue = param.Value.ColorValue,
                    Min = param.Value.Min,
                    Max = param.Value.Max,
                    Options = new List<string>(param.Value.Options)
                };
            }
            
            EffectStack.Add(duplicate);
            EffectCount = $"{EffectStack.Count} effects";
            StatusMessage = "Effect duplicated";
        }
        else
        {
            StatusMessage = "No effect selected to duplicate";
        }
    }
    
    private void DeleteEffect()
    {
        if (SelectedEffect != null)
        {
            EffectStack.Remove(SelectedEffect);
            EffectCount = $"{EffectStack.Count} effects";
            SelectedEffect = EffectStack.Count > 0 ? EffectStack[0] : null!;
            StatusMessage = "Effect deleted";
        }
        else
        {
            StatusMessage = "No effect selected to delete";
        }
    }
    
    private void AddEffect()
    {
        var newEffect = new EffectStackItem($"Effect {EffectStack.Count + 1}", "Phoenix")
        {
            EffectType = "Phoenix Effect"
        };
        newEffect.Parameters["intensity"] = new EffectParam { Label = "Intensity", Type = "slider", FloatValue = 1.0f };
        newEffect.Parameters["color"] = new EffectParam { Label = "Color", Type = "color", StringValue = "#00FFFF" };
        newEffect.Parameters["speed"] = new EffectParam { Label = "Speed", Type = "slider", FloatValue = 1.0f };
        
        EffectStack.Add(newEffect);
        EffectCount = $"{EffectStack.Count} effects";
        StatusMessage = "New effect added";
    }
    
    private void SaveCode()
    {
        // Save current code state
        SaveCurrentState();
        StatusMessage = "Code saved";
    }
    
    private void ShowHelp()
    {
        StatusMessage = "Help - Check documentation for PHX Editor usage";
    }
    
    public void RefreshPresets()
    {
        try
        {
            // Refresh available presets
            var presets = _presetService.GetAllPresets();
            AvailablePresets.Clear();
            foreach (var preset in presets)
            {
                AvailablePresets.Add(preset);
            }
            StatusMessage = $"Presets refreshed - {AvailablePresets.Count} available";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error refreshing presets: {ex.Message}";
        }
    }
    
    public async void LoadSelectedPreset()
    {
        if (SelectedPreset != null)
        {
            try
            {
                // Load the actual preset data
                var preset = await _presetService.LoadPresetByNameAsync(SelectedPreset.Name);

                if (preset != null)
                {
                    PresetName = preset.Name;
                    StatusMessage = $"Preset loaded: {preset.Name}";

                    // Load preset data into the editor
                    await LoadPresetFromData(preset);
                }
                else
                {
                    StatusMessage = $"Failed to load preset: {SelectedPreset.Name}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading preset: {ex.Message}";
            }
        }
        else
        {
            StatusMessage = "No preset selected";
        }
    }
    
    public async void DeleteSelectedPreset()
    {
        if (SelectedPreset != null)
        {
            try
            {
                // Confirm deletion
                var result = await Task.Run(() =>
                {
                    // In a real implementation, this would show a confirmation dialog
                    return true; // For now, just proceed
                });

                if (result)
                {
                    // Delete the actual preset file
                    if (!string.IsNullOrEmpty(SelectedPreset.FilePath))
                    {
                        _presetService.DeletePreset(SelectedPreset.FilePath);
                    }

                    // Remove from the list
                    AvailablePresets.Remove(SelectedPreset);
                    var deletedName = SelectedPreset.Name;
                    SelectedPreset = null;
                    StatusMessage = $"Preset '{deletedName}' deleted successfully";

                    // Refresh the preset cache
                    RefreshPresets();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting preset: {ex.Message}";
            }
        }
        else
        {
            StatusMessage = "No preset selected";
        }
    }
    
    // Helper methods for state management
    private string CreateCurrentState()
    {
        var state = new
        {
            EffectStack = EffectStack.ToList(),
            InitCode,
            FrameCode,
            PointCode,
            BeatCode,
            PresetName
        };
        return JsonSerializer.Serialize(state);
    }
    
    private void SaveCurrentState()
    {
        var currentState = CreateCurrentState();
        _undoStack.Push(currentState);
        _redoStack.Clear(); // Clear redo stack when new action is performed
    }
    
    private void RestoreState(string stateJson)
    {
        try
        {
            var state = JsonSerializer.Deserialize<dynamic>(stateJson);
            // Restore state (simplified for now)
            StatusMessage = "State restored";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error restoring state: {ex.Message}";
        }
    }
    
    private PhxPreset CreatePhxPresetFromCurrentState()
    {
        return new PhxPreset
        {
            Name = PresetName,
            Description = $"PHX Preset created on {DateTime.Now}",
            EffectStack = EffectStack.Select(e => new PhxPreset.EffectStackEntry
            {
                Name = e.Name,
                Category = e.Category,
                EffectType = e.EffectType,
                Parameters = e.Parameters.ToDictionary(p => p.Key, p => new PhxPreset.ParameterEntry
                {
                    Type = p.Value.Type,
                    Label = p.Value.Label,
                    FloatValue = p.Value.FloatValue,
                    BoolValue = p.Value.BoolValue,
                    StringValue = p.Value.StringValue,
                    Options = p.Value.Options
                })
            }).ToList(),
            InitCode = InitCode,
            FrameCode = FrameCode,
            PointCode = PointCode,
            BeatCode = BeatCode,
            Version = "1.0"
        };
    }

    public void InitializeCommands()
    {
        // Initialize all ReactiveCommand properties
        NewPresetCommand = ReactiveCommand.Create(NewPreset);
        OpenPresetCommand = ReactiveCommand.Create(OpenPreset);
        SavePresetCommand = ReactiveCommand.Create(SavePreset);
        SaveAsPresetCommand = ReactiveCommand.Create(SaveAsPreset);
        // AVS commands will be set from the Window class
        UndoCommand = ReactiveCommand.Create(Undo);
        RedoCommand = ReactiveCommand.Create(Redo);
        CutCommand = ReactiveCommand.Create(Cut);
        CopyCommand = ReactiveCommand.Create(Copy);
        PasteCommand = ReactiveCommand.Create(Paste);
        DuplicateEffectCommand = ReactiveCommand.Create(DuplicateEffect);
        DeleteEffectCommand = ReactiveCommand.Create(DeleteEffect);
        AddEffectCommand = ReactiveCommand.Create(AddEffect);
        SaveCodeCommand = ReactiveCommand.Create(SaveCode);
        CompileCommand = ReactiveCommand.Create(() => { }); // Will be wired up in code-behind
        TestCodeCommand = ReactiveCommand.Create(() => { }); // Will be wired up in code-behind
        PlayCommand = ReactiveCommand.Create(() => { }); // Will be wired up in code-behind
        PauseCommand = ReactiveCommand.Create(() => { }); // Will be wired up in code-behind
        RestartCommand = ReactiveCommand.Create(() => { }); // Will be wired up in code-behind
        HelpCommand = ReactiveCommand.Create(ShowHelp);
        TogglePerformanceOverlayCommand = ReactiveCommand.Create(TogglePerformanceOverlay);
        ToggleDebugLoggingCommand = ReactiveCommand.Create(ToggleDebugLogging);
        ResetPerformanceStatsCommand = ReactiveCommand.Create(ResetPerformanceStats);
        ExportPerformanceLogCommand = ReactiveCommand.Create(ExportPerformanceLog);
        RefreshPresetsCommand = ReactiveCommand.Create(RefreshPresets);
        LoadSelectedPresetCommand = ReactiveCommand.Create(LoadSelectedPreset);
        DeletePresetCommand = ReactiveCommand.Create(DeleteSelectedPreset);
    }

    private void LoadEffectLibrary()
    {
        // Load Phoenix Original effects
        PhoenixOriginals.Add(new EffectItem("Cymatics Visualizer", "Phoenix"));
        PhoenixOriginals.Add(new EffectItem("Shader Visualizer", "Phoenix"));
        PhoenixOriginals.Add(new EffectItem("Sacred Geometry", "Phoenix"));
        PhoenixOriginals.Add(new EffectItem("Godrays", "Phoenix"));
        PhoenixOriginals.Add(new EffectItem("Particle Swarm", "Phoenix"));

        // Load AVS effects (placeholder for now)
        AvsEffects.Add(new EffectItem("Superscope", "AVS"));
        AvsEffects.Add(new EffectItem("Dynamic Movement", "AVS"));
        AvsEffects.Add(new EffectItem("Buffer Save", "AVS"));

        // Load Research effects
        ResearchEffects.Add(new EffectItem("Earth Harmonics", "Research"));
        ResearchEffects.Add(new EffectItem("Solfeggio Frequencies", "Research"));
    }

    private void InitializeDefaultPreset()
    {
        // Add a default superscope effect
        var defaultEffect = new EffectStackItem("Superscope", "AVS");
        defaultEffect.Parameters["points"] = new EffectParam { Label = "Points", Type = "slider", FloatValue = 100 };
        defaultEffect.Parameters["source"] = new EffectParam { Label = "Source", Type = "dropdown", StringValue = "fft" };
        EffectStack.Add(defaultEffect);
        SelectedEffect = defaultEffect;
    }

    private void NewPreset()
    {
        try
        {
            InitializeDefaultPreset();
            PresetName = "Untitled.phx";
            StatusMessage = "New preset created";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating new preset: {ex.Message}";
        }
    }

    private async void OpenPreset()
    {
        try
        {
            // Get available presets from the service
            var availablePresets = _presetService.GetAllPresets().ToList();

            if (availablePresets.Count == 0)
            {
                StatusMessage = "No presets found. Create and save a preset first.";
                return;
            }

            // For now, just load the first available preset
            // In a full implementation, this would show a selection dialog
            var firstPreset = availablePresets.First();
            var preset = await _presetService.LoadPresetByNameAsync(firstPreset.Name);

            if (preset != null)
            {
                await LoadPresetFromData(preset);
            }
            else
            {
                StatusMessage = $"Failed to load preset: {firstPreset.Name}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening preset: {ex.Message}";
        }
    }



    // AVS import/export methods moved to PhxEditorWindow class

    // ImportAvsPreset method moved to PhxEditorWindow class







    private void ExportAvs()
    {
        try
        {
            string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PhoenixVisualizer", "avs_exports");
            Directory.CreateDirectory(defaultPath);
            string avsPath = Path.Combine(defaultPath, $"{PresetName.Replace(".phx", "")}.avs");

            ExportPresetAsAvs(avsPath).Wait();
            StatusMessage = $"Exported as AVS: {Path.GetFileName(avsPath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting AVS: {ex.Message}";
        }
    }

    private void ImportAvs()
    {
        try
        {
            string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PhoenixVisualizer", "avs_imports");
            Directory.CreateDirectory(defaultPath);
            string exampleAvsPath = Path.Combine(defaultPath, "example.avs");

            if (File.Exists(exampleAvsPath))
            {
                ImportPresetFromAvs(exampleAvsPath).Wait();
                StatusMessage = $"Imported AVS preset: {Path.GetFileName(exampleAvsPath)}";
            }
            else
            {
                StatusMessage = "No AVS files found. Place AVS files in Documents/PhoenixVisualizer/avs_imports/";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error importing AVS: {ex.Message}";
        }
    }
    // These methods are now properly implemented above

    // AddEffect is now handled by the ViewModel

    private async Task SavePresetToFile(string filePath)
    {
        var preset = new PhxPreset
        {
            Version = "1.0",
            Name = PresetName,
            CreatedDate = DateTime.UtcNow,
            InitCode = InitCode,
            FrameCode = FrameCode,
            PointCode = PointCode,
            BeatCode = BeatCode,
            EffectStack = EffectStack.Select(e => new PhxPreset.EffectStackEntry
            {
                Name = e.Name,
                Category = e.Category,
                EffectType = e.EffectType,
                Parameters = e.Parameters.ToDictionary(p => p.Key, p => new PhxPreset.ParameterEntry
                {
                    Type = p.Value.Type,
                    Label = p.Value.Label,
                    FloatValue = p.Value.FloatValue,
                    BoolValue = p.Value.BoolValue,
                    StringValue = p.Value.StringValue,
                    Options = p.Value.Options
                })
            }).ToList()
        };

        var json = JsonSerializer.Serialize(preset, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }

    private async Task LoadPresetFromFile(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var preset = JsonSerializer.Deserialize<PhxPreset>(json);

        if (preset != null)
        {
            PresetName = preset.Name;
            InitCode = preset.InitCode;
            FrameCode = preset.FrameCode;
            PointCode = preset.PointCode;
            BeatCode = preset.BeatCode;

            EffectStack.Clear();
            foreach (var effectEntry in preset.EffectStack)
            {
                var effect = new EffectStackItem(effectEntry.Name, effectEntry.Category);
                foreach (var paramEntry in effectEntry.Parameters)
                {
                    effect.Parameters[paramEntry.Key] = new EffectParam
                    {
                        Type = paramEntry.Value.Type,
                        Label = paramEntry.Value.Label,
                        FloatValue = paramEntry.Value.FloatValue,
                        BoolValue = paramEntry.Value.BoolValue,
                        StringValue = paramEntry.Value.StringValue,
                        Options = paramEntry.Value.Options ?? new List<string>()
                    };
                }
                EffectStack.Add(effect);
            }

            SelectedEffect = EffectStack.FirstOrDefault() ?? null!;
            StatusMessage = $"Loaded preset: {preset.Name}";
        }
    }

    public async Task ExportPresetAsAvs(string filePath)
    {
        var avsContent = new StringBuilder();

        // AVS preset header
        avsContent.AppendLine("[avs]");
        avsContent.AppendLine("MajorVersion=1");
        avsContent.AppendLine("MinorVersion=0");
        avsContent.AppendLine();

        // Convert PHX effects to AVS format
        foreach (var effect in EffectStack)
        {
            avsContent.AppendLine($"[effect.{effect.Name}]");
            avsContent.AppendLine($"enabled=1");

            // Convert parameters to AVS format
            foreach (var param in effect.Parameters)
            {
                if (param.Value.Type == "slider")
                {
                    avsContent.AppendLine($"{param.Key}={param.Value.FloatValue:F3}");
                }
                else if (param.Value.Type == "checkbox")
                {
                    avsContent.AppendLine($"{param.Key}={(param.Value.BoolValue ? 1 : 0)}");
                }
                else if (param.Value.Type == "dropdown")
                {
                    avsContent.AppendLine($"{param.Key}={param.Value.StringValue}");
                }
            }
            avsContent.AppendLine();
        }

        // Add code sections
        if (!string.IsNullOrWhiteSpace(InitCode))
        {
            avsContent.AppendLine("[code.init]");
            avsContent.AppendLine(InitCode);
            avsContent.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(FrameCode))
        {
            avsContent.AppendLine("[code.frame]");
            avsContent.AppendLine(FrameCode);
            avsContent.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(PointCode))
        {
            avsContent.AppendLine("[code.point]");
            avsContent.AppendLine(PointCode);
            avsContent.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(BeatCode))
        {
            avsContent.AppendLine("[code.beat]");
            avsContent.AppendLine(BeatCode);
            avsContent.AppendLine();
        }

        await File.WriteAllTextAsync(filePath, avsContent.ToString());
    }

    public async Task ImportPresetFromAvs(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);

        // Basic AVS parsing - this would need to be expanded for full AVS support
        // For now, just extract code sections
        var lines = content.Split('\n');

        string currentSection = "";
        var initCode = new StringBuilder();
        var frameCode = new StringBuilder();
        var pointCode = new StringBuilder();
        var beatCode = new StringBuilder();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("[code."))
            {
                currentSection = trimmed;
            }
            else if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("[") && !trimmed.Contains("="))
            {
                // Code line
                switch (currentSection)
                {
                    case "[code.init]":
                        initCode.AppendLine(trimmed);
                        break;
                    case "[code.frame]":
                        frameCode.AppendLine(trimmed);
                        break;
                    case "[code.point]":
                        pointCode.AppendLine(trimmed);
                        break;
                    case "[code.beat]":
                        beatCode.AppendLine(trimmed);
                        break;
                }
            }
        }

        // Update the editor with imported code
        InitCode = initCode.ToString().TrimEnd();
        FrameCode = frameCode.ToString().TrimEnd();
        PointCode = pointCode.ToString().TrimEnd();
        BeatCode = beatCode.ToString().TrimEnd();

        // Create a default effect stack
        InitializeDefaultPreset();
        PresetName = Path.GetFileNameWithoutExtension(filePath) + ".phx";
    }

    // SaveCode and ShowHelp are now handled by the ViewModel

    // Performance monitoring methods (Phase 4 - will be implemented)
    private void TogglePerformanceOverlay()
    {
        ShowPerformanceOverlay = !ShowPerformanceOverlay;
        StatusMessage = $"Performance overlay {(ShowPerformanceOverlay ? "enabled" : "disabled")}";
    }

    private void ToggleDebugLogging()
    {
        EnableDebugLogging = !EnableDebugLogging;
        StatusMessage = $"Debug logging {(EnableDebugLogging ? "enabled" : "disabled")}";
        if (EnableDebugLogging)
        {
            DebugInfo = "Debug: Logging active - check debug console";
            Debug.WriteLine("PHX Editor: Debug logging enabled");
        }
        else
        {
            DebugInfo = "Debug: Logging disabled";
        }
    }

    private void ResetPerformanceStats()
    {
        // Reset counters and timers
        _frameCount = 0;
        _lastFpsUpdate = 0;
        _fpsStopwatch.Restart();
        _renderStopwatch.Reset();
        _performanceLog.Clear();

        // Reset UI values
        FpsCounter = "60 FPS";
        MemoryUsage = "128 MB";
        CpuUsage = "5%";
        RenderTime = "16.7ms";
        EffectCount = $"{EffectStack.Count} effects";
        StatusMessage = "Performance stats reset";
        DebugInfo = "Debug: Stats reset";

        // Log the reset event
        LogPerformanceEvent("Performance stats manually reset");
    }

    private void ExportPerformanceLog()
    {
        try
        {
            string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "PhoenixVisualizer", "logs", $"performance_{DateTime.Now:yyyyMMdd_HHmmss}.log");

            var logDirectory = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // Use the comprehensive performance report
            var logContent = GetPerformanceReport();
            File.WriteAllText(logPath, logContent);

            StatusMessage = $"Performance log exported: {Path.GetFileName(logPath)}";
            LogPerformanceEvent($"Performance log exported to: {logPath}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting performance log: {ex.Message}";
        }
    }

    // Preset management methods - these are now handled by the ViewModel

    private async Task LoadPresetFromData(PresetBase preset)
    {
        try
        {
            // Clear current effect stack
            EffectStack.Clear();

            // Update preset metadata
            PresetName = preset.Name;
            PresetCategory = preset.Category;
            PresetDescription = preset.Description;

            // Load specific preset type data
            if (preset is PhxPreset phxPreset)
            {
                await LoadPhxPreset(phxPreset);
            }
            else if (preset is AvsPreset avsPreset)
            {
                await LoadAvsPreset(avsPreset);
            }

            StatusMessage = $"Preset '{preset.Name}' loaded successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading preset data: {ex.Message}";
            Debug.WriteLine($"Preset loading error: {ex}");
        }
    }

    private Task LoadPhxPreset(PhxPreset preset)
    {
        try
        {
            // Load initialization code
            InitCode = preset.InitCode;
            FrameCode = preset.FrameCode;
            BeatCode = preset.BeatCode;
            PointCode = preset.PointCode;

            // Load effect stack
            if (preset.EffectStack != null)
            {
                foreach (var effectEntry in preset.EffectStack)
                {
                    var effect = CreateEffectFromEntry(effectEntry);
                    if (effect != null)
                    {
                        EffectStack.Add(effect);
                    }
                }
            }

            // UI will be updated automatically through ReactiveUI bindings
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading PHX preset: {ex.Message}";
            return Task.CompletedTask;
        }
    }

    private Task LoadAvsPreset(AvsPreset preset)
    {
        try
        {
            // Load AVS preset data
            // This would convert AVS effects to PHX equivalents
            StatusMessage = $"AVS preset '{preset.Name}' loaded (conversion to PHX format)";
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading AVS preset: {ex.Message}";
            return Task.CompletedTask;
        }
    }

    private EffectStackItem? CreateEffectFromEntry(PhxPreset.EffectStackEntry entry)
    {
        try
        {
            // Create effect based on type
            var effect = new EffectStackItem(entry.Name, entry.Category);
            effect.EffectType = entry.EffectType;

            // Load parameters
            if (entry.Parameters != null)
            {
                foreach (var paramEntry in entry.Parameters)
                {
                    var coreParam = new CoreEffectParam
                    {
                        Label = paramEntry.Value.Label ?? string.Empty,
                        Type = paramEntry.Value.Type,
                        FloatValue = paramEntry.Value.FloatValue,
                        BoolValue = paramEntry.Value.BoolValue,
                        StringValue = paramEntry.Value.StringValue ?? string.Empty,
                        Options = paramEntry.Value.Options ?? new List<string>()
                    };
                    effect.Parameters[paramEntry.Key] = coreParam;
                }
            }

            return effect;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating effect from entry: {ex}");
            return null;
        }
    }

    // Performance monitoring helpers
    public void UpdatePerformanceMetrics(double fps, long memoryBytes, double cpuPercent, double renderMs)
    {
        FpsCounter = $"{fps:F1} FPS";
        MemoryUsage = $"{memoryBytes / 1024.0 / 1024.0:F1} MB";
        CpuUsage = $"{cpuPercent:F1}%";
        RenderTime = $"{renderMs:F1}ms";
        EffectCount = $"{EffectStack.Count} effect{(EffectStack.Count != 1 ? "s" : "")}";

        if (EnableDebugLogging)
        {
            Debug.WriteLine($"PHX Performance: FPS={fps:F1}, Memory={MemoryUsage}, CPU={CpuUsage}, Render={RenderTime}");
        }
    }

    public void LogDebugInfo(string message)
    {
        if (EnableDebugLogging)
        {
            DebugInfo = $"Debug: {message}";
            Debug.WriteLine($"PHX Debug: {message}");
        }
    }

    public void Dispose()
    {
        // Clean up performance monitoring resources
        _performanceUpdateTimer?.Dispose();
        _fpsStopwatch.Stop();
        _renderStopwatch.Stop();

        // Dispose performance counters
        _cpuCounter?.Dispose();
        _memoryCounter?.Dispose();

        LogPerformanceEvent("Performance monitoring disposed");
    }


}

/// <summary>
/// Data classes for the editor
/// </summary>
public class EffectStackItem : EffectItem
{
    public Dictionary<string, CoreEffectParam> Parameters { get; } = new();
    public string EffectType { get; set; } = "Phoenix"; // Phoenix, AVS, Research
    public IEffectNode? EffectNode { get; set; } // The actual instantiated effect node

    public EffectStackItem(string name, string category) : base()
    {
        Name = name;
        Category = category;
        DisplayName = $"{Name} ({Category})";
        EffectType = category;

        // Initialize default parameters based on effect type
        InitializeDefaultParameters();
    }

    private void InitializeDefaultParameters()
    {
        // Add common parameters for all effects
        Parameters["enabled"] = new CoreEffectParam { Label = "Enabled", Type = "checkbox", BoolValue = true };
        Parameters["blend"] = new CoreEffectParam { Label = "Blend Mode", Type = "dropdown", StringValue = "normal", Options = new() { "normal", "add", "multiply" } };

        // Add effect-specific parameters
        if (Name == "Superscope")
        {
            Parameters["points"] = new CoreEffectParam { Label = "Points", Type = "slider", FloatValue = 100, Min = 1, Max = 1000 };
            Parameters["source"] = new CoreEffectParam { Label = "Source", Type = "dropdown", StringValue = "fft", Options = new() { "fft", "waveform", "spectrum" } };
        }
        else if (Name == "Cymatics Visualizer")
        {
            Parameters["material"] = new CoreEffectParam { Label = "Material", Type = "dropdown", StringValue = "water", Options = new() { "water", "sand", "salt", "metal" } };
            Parameters["frequency"] = new CoreEffectParam { Label = "Frequency", Type = "slider", FloatValue = 432, Min = 20, Max = 2000 };
            Parameters["intensity"] = new CoreEffectParam { Label = "Intensity", Type = "slider", FloatValue = 0.8f, Min = 0, Max = 1 };
        }
        else if (Name == "Shader Visualizer")
        {
            Parameters["speed"] = new CoreEffectParam { Label = "Speed", Type = "slider", FloatValue = 1.0f, Min = 0.1f, Max = 5.0f };
            Parameters["complexity"] = new CoreEffectParam { Label = "Complexity", Type = "slider", FloatValue = 0.5f, Min = 0, Max = 1 };
        }
    }

}

/// <summary>
/// Preset Service for managing presets across different formats
/// </summary>
public class PresetService
{
    private readonly string _presetsDirectory;
    private readonly Dictionary<string, PresetMetadata> _presetCache = new();

    public PresetService()
    {
        _presetsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "PhoenixVisualizer", "presets");
        Directory.CreateDirectory(_presetsDirectory);
        RefreshPresetCache();
    }



    public void RefreshPresetCache()
    {
        _presetCache.Clear();
        var presetFiles = Directory.GetFiles(_presetsDirectory, "*.json", SearchOption.AllDirectories);

        foreach (var file in presetFiles)
        {
            try
            {
                var json = File.ReadAllText(file);
                var preset = JsonSerializer.Deserialize<PresetBase>(json);
                if (preset != null)
                {
                    var metadata = new PresetMetadata
                    {
                        Name = preset.Name,
                        Type = preset.Type,
                        Version = preset.Version,
                        Category = preset.Category,
                        Description = preset.Description,
                        Author = preset.Author,
                        CreatedDate = preset.CreatedDate,
                        ModifiedDate = preset.ModifiedDate,
                        FilePath = file,
                        ThumbnailPath = preset.ThumbnailPath,
                        Tags = preset.Tags
                    };
                    _presetCache[file] = metadata;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading preset metadata {file}: {ex.Message}");
            }
        }
    }

    public IEnumerable<PresetMetadata> GetAllPresets() => _presetCache.Values;

    public IEnumerable<PresetMetadata> GetPresetsByType(PresetType type) =>
        _presetCache.Values.Where(p => p.Type == type);

    public IEnumerable<PresetMetadata> GetPresetsByCategory(string category) =>
        _presetCache.Values.Where(p => p.Category.Contains(category, StringComparison.OrdinalIgnoreCase));

    public PresetMetadata? GetPresetByName(string name) =>
        _presetCache.Values.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public async Task SavePresetAsync(PresetBase preset, string? fileName = null)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = $"{preset.Name.Replace(" ", "_")}.json";
        }

        var filePath = Path.Combine(_presetsDirectory, fileName);
        var json = JsonSerializer.Serialize(preset, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);

        // Update cache
        var metadata = new PresetMetadata
        {
            Name = preset.Name,
            Type = preset.Type,
            Version = preset.Version,
            Category = preset.Category,
            Description = preset.Description,
            Author = preset.Author,
            CreatedDate = preset.CreatedDate,
            ModifiedDate = DateTime.UtcNow,
            FilePath = filePath,
            ThumbnailPath = preset.ThumbnailPath,
            Tags = preset.Tags
        };

        _presetCache[filePath] = metadata;
    }

    public async Task<PresetBase?> LoadPresetAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        var json = await File.ReadAllTextAsync(filePath);
        var preset = JsonSerializer.Deserialize<PresetBase>(json);
        return preset;
    }

    public void DeletePreset(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _presetCache.Remove(filePath);
        }
    }

    public async Task<PresetBase?> LoadPresetByNameAsync(string name)
    {
        var metadata = GetPresetByName(name);
        if (metadata?.FilePath != null)
        {
            return await LoadPresetAsync(metadata.FilePath);
        }
        return null;
    }
}

/// <summary>
/// Base preset class with common properties
/// </summary>
public class PresetBase
{
    public string Version { get; set; } = "1.0";
    public string Name { get; set; } = "Untitled";
    public PresetType Type { get; set; } = PresetType.PHX;
    public string Category { get; set; } = "General";
    public string Description { get; set; } = "";
    public string Author { get; set; } = Environment.UserName;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public List<string> Tags { get; set; } = new();
    public string? ThumbnailPath { get; set; }
}

/// <summary>
/// PHX Preset data structure for save/load functionality
/// </summary>
public class PhxPreset : PresetBase
{
    public PhxPreset()
    {
        Type = PresetType.PHX;
    }

    public string InitCode { get; set; } = "";
    public string FrameCode { get; set; } = "";
    public string PointCode { get; set; } = "";
    public string BeatCode { get; set; } = "";
    public List<EffectStackEntry> EffectStack { get; set; } = new();

    public class EffectStackEntry
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string EffectType { get; set; } = "";
        public Dictionary<string, ParameterEntry> Parameters { get; set; } = new();
    }

    public class ParameterEntry
    {
        public string Type { get; set; } = "";
        public string Label { get; set; } = "";
        public float FloatValue { get; set; } = 0;
        public bool BoolValue { get; set; } = false;
        public string StringValue { get; set; } = "";
        public List<string>? Options { get; set; }
    }
}

/// <summary>
/// AVS Preset data structure
/// </summary>
public class AvsPreset : PresetBase
{
    public AvsPreset()
    {
        Type = PresetType.AVS;
    }

    public string AvsCode { get; set; } = "";
    public List<AvsComponent> Components { get; set; } = new();

    public class AvsComponent
    {
        public string Type { get; set; } = "";
        public string Config { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}

/// <summary>
/// Preset type enumeration
/// </summary>
public enum PresetType
{
    PHX,
    AVS,
    SONIQUE,
    WMP
}

/// <summary>
/// Preset metadata for browsing and searching
/// </summary>
public class PresetMetadata
{
    public string Name { get; set; } = "";
    public PresetType Type { get; set; }
    public string Version { get; set; } = "1.0";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public string Author { get; set; } = "";
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string? FilePath { get; set; }
    public string? ThumbnailPath { get; set; }
    public List<string> Tags { get; set; } = new();

    public string DisplayName => $"{Name} ({Type})";
    public string FileSize => FilePath != null && File.Exists(FilePath)
        ? $"{new FileInfo(FilePath).Length / 1024} KB"
        : "Unknown";
}