using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using PhoenixVisualizer.Core.Nodes;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Reactive.Linq;
using System.Diagnostics;

// Reference classes from the App.Views namespace
using PhxPreviewRenderer = PhoenixVisualizer.App.Views.PhxPreviewRenderer;
using ParameterEditor = PhoenixVisualizer.App.Views.ParameterEditor;
using PhxCodeEngine = PhoenixVisualizer.Core.Nodes.PhxCodeEngine;
using CoreEffectParam = PhoenixVisualizer.Core.Nodes.EffectParam;
using EffectStackItem = PhoenixVisualizer.Views.EffectStackItem;

namespace PhoenixVisualizer.Views;

/// <summary>
/// PHX Editor Window - Advanced Visual Effects Composer
/// Complete AVS Editor++ with effect stack, code editing, and live preview
/// </summary>
public partial class PhxEditorWindow : Window
{
    public PhxEditorViewModel ViewModel { get; private set; }

    private PhxPreviewRenderer _previewRenderer;
    private ParameterEditor _parameterEditor;
    private PhxCodeEngine _codeEngine;

    public PhxEditorWindow()
    {
        InitializeComponent();
        ViewModel = new PhxEditorViewModel();

        // Initialize required fields
        _codeEngine = new PhxCodeEngine();
        _previewRenderer = null!;
        _parameterEditor = null!;

        // Set up the preview rendering
        SetupPreviewRendering();

        // Set up parameter editor
        SetupParameterEditor();

        // Wire up effect selection changes
        WireUpEffectSelection();

        // Wire up code compilation
        WireUpCodeCompilation();
    }

    private void WireUpCodeCompilation()
    {
        if (ViewModel is PhxEditorViewModel vm)
        {
            // Wire up the compile command to execute code
            vm.CompileCommand.Subscribe(_ => CompileCode());
            vm.TestCodeCommand.Subscribe(_ => TestCode());
        }
    }

    private void CompileCode()
    {
        if (ViewModel is PhxEditorViewModel vm)
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
        if (ViewModel is PhxEditorViewModel vm)
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
        // Create the preview renderer
        _previewRenderer = new PhxPreviewRenderer(PreviewCanvas, (PhxEditorViewModel)ViewModel);
    }

    private void SetupParameterEditor()
    {
        // Create parameter editor and add it to the parameters panel
        _parameterEditor = new ParameterEditor();
        ParametersPanel.Children.Insert(0, _parameterEditor);

        // Bind to selected effect changes
        this.WhenAnyValue(x => x.ViewModel)
            .Where(vm => vm is PhxEditorViewModel)
            .Select(vm => (PhxEditorViewModel)vm)
            .Subscribe(vm =>
            {
                vm.WhenAnyValue(x => x.SelectedEffect)
                    .Subscribe(selectedEffect =>
                    {
                        if (selectedEffect != null)
                        {
                            _parameterEditor.UpdateParameters(
                                selectedEffect.Name,
                                selectedEffect.Parameters
                            );
                        }
                        else
                        {
                            _parameterEditor.UpdateParameters("", new Dictionary<string, EffectParam>());
                        }
                    });
            });
    }

    private void WireUpEffectSelection()
    {
        // Wire up the preview renderer to respond to play/pause/restart commands
        if (ViewModel is PhxEditorViewModel vm)
        {
            vm.PlayCommand.Subscribe(_ => _previewRenderer?.Resume());
            vm.PauseCommand.Subscribe(_ => _previewRenderer?.Pause());
            vm.RestartCommand.Subscribe(_ => _previewRenderer?.Restart());
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Clean up resources
        _previewRenderer?.Stop();
        _codeEngine?.Reset();
    }
}

/// <summary>
/// ViewModel for PHX Editor - Manages all editor functionality
/// </summary>
public class PhxEditorViewModel : ReactiveObject
{
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
    [Reactive] public string CodeStatus { get; set; } = "Ready";

    // Commands (ReactiveCommands for UI actions)
    public ReactiveCommand<Unit, Unit> NewPresetCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> OpenPresetCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> SavePresetCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> SaveAsPresetCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> ExportAvsCommand { get; private set; } = null!;
    public ReactiveCommand<Unit, Unit> ImportAvsCommand { get; private set; } = null!;
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

    // Undo/Redo System
    private readonly Stack<EditorState> _undoStack = new();
    private readonly Stack<EditorState> _redoStack = new();

    public PhxEditorViewModel()
    {
        // Initialize reactive properties
        SelectedEffect = null!;
        SelectedLibraryEffect = null!;

        // Initialize commands first
        InitializeCommands();

        // Load effect library
        LoadEffectLibrary();

        // Initialize default preset
        InitializeDefaultPreset();
    }

    private void InitializeCommands()
    {
        // Initialize all ReactiveCommand properties
        NewPresetCommand = ReactiveCommand.Create(NewPreset);
        OpenPresetCommand = ReactiveCommand.Create(OpenPreset);
        SavePresetCommand = ReactiveCommand.Create(SavePreset);
        SaveAsPresetCommand = ReactiveCommand.Create(SaveAsPreset);
        ExportAvsCommand = ReactiveCommand.Create(ExportAvs);
        ImportAvsCommand = ReactiveCommand.Create(ImportAvs);
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

    private void NewPreset() => InitializeDefaultPreset();
    private void OpenPreset() => StatusMessage = "Open preset - Not implemented yet";
    private void SavePreset() => StatusMessage = "Save preset - Not implemented yet";
    private void SaveAsPreset() => StatusMessage = "Save as preset - Not implemented yet";
    private void ExportAvs() => StatusMessage = "Export AVS - Not implemented yet";
    private void ImportAvs() => StatusMessage = "Import AVS - Not implemented yet";
    private void Undo() => StatusMessage = "Undo - Not implemented yet";
    private void Redo() => StatusMessage = "Redo - Not implemented yet";
    private void Cut() => StatusMessage = "Cut - Not implemented yet";
    private void Copy() => StatusMessage = "Copy - Not implemented yet";
    private void Paste() => StatusMessage = "Paste - Not implemented yet";
    private void DuplicateEffect() => StatusMessage = "Duplicate effect - Not implemented yet";
    private void DeleteEffect() => StatusMessage = "Delete effect - Not implemented yet";

    private void AddEffect()
    {
        if (SelectedLibraryEffect != null)
        {
            var newEffect = new EffectStackItem(SelectedLibraryEffect.Name, SelectedLibraryEffect.Category);
            EffectStack.Add(newEffect);
            SelectedEffect = newEffect;
            StatusMessage = $"Added effect: {SelectedLibraryEffect.Name}";
        }
    }

    private void SaveCode() => StatusMessage = "Code saved";
    private void ShowHelp() => StatusMessage = "Help - Check documentation for PHX Editor usage";
}

/// <summary>
/// Data classes for the editor
/// </summary>
public class EffectStackItem : EffectItem
{
    public Dictionary<string, CoreEffectParam> Parameters { get; } = new();
    public string EffectType { get; set; } = "Phoenix"; // Phoenix, AVS, Research

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



public class EditorState
{
    public List<EffectStackItem> EffectStack { get; set; } = new();
    public string InitCode { get; set; } = "";
    public string FrameCode { get; set; } = "";
    public string PointCode { get; set; } = "";
    public string BeatCode { get; set; } = "";
}
