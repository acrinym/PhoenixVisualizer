using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Reactive;
using System.Reactive.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;
using PhoenixVisualizer.Views;
using Avalonia.Threading;

namespace PhoenixVisualizer.App.ViewModels
{
    public partial class PhxEditorViewModel : ReactiveObject
    {
        public PhxEditorSettings Settings { get; } = new PhxEditorSettings();

        [Reactive] public string StatusText { get; set; } = "Ready";
        [Reactive] public string StatusMessage { get; set; } = "Ready";
        [Reactive] public string InitCode { get; set; } = "// Initialization code";
        [Reactive] public string RenderCode { get; set; } = "// Render code";
        [Reactive] public string FrameCode { get; set; } = "// Frame code";
        [Reactive] public string BeatCode { get; set; } = "// Beat code";
        [Reactive] public string PointCode { get; set; } = "// Point code";
        [Reactive] public string CodeStatus { get; set; } = "Ready";
        [Reactive] public EffectStackItem? SelectedEffect { get; set; } = null;

        private bool _isCompiling;
        public bool IsCompiling 
        { 
            get => _isCompiling; 
            private set 
            { 
                if (Dispatcher.UIThread.CheckAccess())
                {
                    this.RaiseAndSetIfChanged(ref _isCompiling, value);
                    this.RaisePropertyChanged(nameof(IsCompileEnabled));
                }
                else
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        this.RaiseAndSetIfChanged(ref _isCompiling, value);
                        this.RaisePropertyChanged(nameof(IsCompileEnabled));
                    });
                }
            } 
        }
        public bool IsCompileEnabled => !IsCompiling;
        public ObservableCollection<string> Logs { get; } = new();
        public void Log(string msg) => Logs.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");

        // Commands
        public ReactiveCommand<Unit, Unit> SavePresetCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> SaveAsPresetCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> ImportAvsCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> ExportAvsCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> UndoCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> RedoCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> CutCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> CopyCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> PasteCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> DuplicateEffectCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> DeleteEffectCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> AddEffectCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> SaveCodeCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> CompileCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> TestCodeCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> PlayCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> PauseCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> RestartCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> HelpCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> TogglePerformanceOverlayCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> ToggleDebugLoggingCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> ResetPerformanceStatsCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> RefreshPresetsCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> LoadSelectedPresetCommand { get; set; } = ReactiveCommand.Create(() => { });
        public ReactiveCommand<Unit, Unit> DeletePresetCommand { get; set; } = ReactiveCommand.Create(() => { });

        // Events
        public event Action? CompileStarted;
        public event Action<bool>? CompileCompleted;

        public PhxEditorViewModel()
        {
            var canRun = this.WhenAnyValue(x => x.IsCompiling).Select(c => !c);
            InitializeCommands();
            
            // Create a dummy SelectedEffect to avoid null reference exceptions
            SelectedEffect = new EffectStackItem("Default Effect", "Phoenix");
        }

        public void InitializeCommands()
        {
            // Ensure all command updates happen on the UI thread
            var canRun = this.WhenAnyValue(x => x.IsCompiling)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(c => !c);
            
            SavePresetCommand = ReactiveCommand.Create(SavePreset);
            SaveAsPresetCommand = ReactiveCommand.Create(SaveAsPreset);
            UndoCommand = ReactiveCommand.Create(Undo);
            RedoCommand = ReactiveCommand.Create(Redo);
            CutCommand = ReactiveCommand.Create(Cut);
            CopyCommand = ReactiveCommand.Create(Copy);
            PasteCommand = ReactiveCommand.Create(Paste);
            DuplicateEffectCommand = ReactiveCommand.Create(DuplicateEffect);
            DeleteEffectCommand = ReactiveCommand.Create(DeleteEffect);
            AddEffectCommand = ReactiveCommand.Create(AddEffect);
            SaveCodeCommand = ReactiveCommand.Create(SaveCode);
            
            CompileCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                CompileStarted?.Invoke();
                IsCompiling = true;
                var ok = false;
                try
                {
                    Log("Compile started.");
                    await DoCompileAsync(); // parse/transpile/build → swap into runtime/preview
                    ok = true;
                    Log("Compile completed.");
                }
                catch (Exception ex)
                {
                    Log("Compile failed: " + ex.Message);
                    ok = false;
                }
                finally
                {
                    IsCompiling = false;
                    CompileCompleted?.Invoke(ok);
                }
            }, canRun);
            
            TestCodeCommand = ReactiveCommand.CreateFromTask(async () => await DoTestAsync(), canRun);
            ImportAvsCommand = ReactiveCommand.CreateFromTask(async () => await DoImportAsync(), canRun);
            ExportAvsCommand = ReactiveCommand.CreateFromTask(async () => await DoExportAsync(), canRun);
            PlayCommand = ReactiveCommand.Create(Play);
            PauseCommand = ReactiveCommand.Create(Pause);
            RestartCommand = ReactiveCommand.Create(Restart);
            HelpCommand = ReactiveCommand.Create(ShowHelp);
            TogglePerformanceOverlayCommand = ReactiveCommand.Create(TogglePerformanceOverlay);
            ToggleDebugLoggingCommand = ReactiveCommand.Create(ToggleDebugLogging);
            ResetPerformanceStatsCommand = ReactiveCommand.Create(ResetPerformanceStats);
            RefreshPresetsCommand = ReactiveCommand.Create(RefreshPresets);
            LoadSelectedPresetCommand = ReactiveCommand.Create(LoadSelectedPreset);
            DeletePresetCommand = ReactiveCommand.Create(DeleteSelectedPreset);
        }

        // Command implementations
        private void SavePreset() { Debug.WriteLine("SavePreset called"); }
        private void SaveAsPreset() { Debug.WriteLine("SaveAsPreset called"); }
        private void Undo() { Debug.WriteLine("Undo called"); }
        private void Redo() { Debug.WriteLine("Redo called"); }
        private void Cut() { Debug.WriteLine("Cut called"); }
        private void Copy() { Debug.WriteLine("Copy called"); }
        private void Paste() { Debug.WriteLine("Paste called"); }
        private void DuplicateEffect() { Debug.WriteLine("DuplicateEffect called"); }
        private void DeleteEffect() { Debug.WriteLine("DeleteEffect called"); }
        private void AddEffect() { Debug.WriteLine("AddEffect called"); }
        private void SaveCode() { Debug.WriteLine("SaveCode called"); }
        private void Play() { Debug.WriteLine("Play called"); }
        private void Pause() { Debug.WriteLine("Pause called"); }
        private void Restart() { Debug.WriteLine("Restart called"); }
        private void ShowHelp() { Debug.WriteLine("ShowHelp called"); }
        private void TogglePerformanceOverlay() { Debug.WriteLine("TogglePerformanceOverlay called"); }
        private void ToggleDebugLogging() { Debug.WriteLine("ToggleDebugLogging called"); }
        private void ResetPerformanceStats() { Debug.WriteLine("ResetPerformanceStats called"); }
        private void RefreshPresets() { Debug.WriteLine("RefreshPresets called"); }
        private void LoadSelectedPreset() { Debug.WriteLine("LoadSelectedPreset called"); }
        private void DeleteSelectedPreset() { Debug.WriteLine("DeleteSelectedPreset called"); }

        // Async methods
        private async Task DoCompileAsync() 
        { 
            await Task.Delay(100); 
            Debug.WriteLine("DoCompileAsync called"); 
        }
        private async Task DoTestAsync() 
        { 
            await Task.Delay(50); 
            Debug.WriteLine("DoTestAsync called"); 
        }
        private async Task DoImportAsync() 
        { 
            await Task.Delay(50); 
            Debug.WriteLine("DoImportAsync called"); 
        }
        private async Task DoExportAsync() 
        { 
            await Task.Delay(50); 
            Debug.WriteLine("DoExportAsync called"); 
        }

        // AVS Import/Export methods
        public async Task ImportPresetFromAvs(string path)
        {
            Debug.WriteLine($"ImportPresetFromAvs called with path: {path}");
            await Task.Delay(100);
        }

        public async Task ExportPresetAsAvs(string path)
        {
            Debug.WriteLine($"ExportPresetAsAvs called with path: {path}");
            await Task.Delay(100);
        }
    }
}
