using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using ReactiveUI;
using System.Collections.ObjectModel;
using PhoenixVisualizer.Core.Transpile;

namespace PhoenixVisualizer.Editor.ViewModels
{
    public class PhxEditorViewModel : ReactiveObject
    {
        private static IScheduler Ui => RxApp.MainThreadScheduler;

        public ReactiveCommand<Unit, Unit> ImportAvsCommand { get; }
        public ReactiveCommand<Unit, Unit> ReimportCommand { get; }
        public ReactiveCommand<Unit, Unit> ExportAvsCommand { get; }
        public ReactiveCommand<Unit, Unit> ExportPhxVisCommand { get; }
        public ReactiveCommand<Unit, Unit> CompileCommand { get; }
        public ReactiveCommand<Unit, Unit> TestCodeCommand { get; }
        public ReactiveCommand<Unit, Unit> NewPhxVisCommand { get; }
        public ReactiveCommand<Unit, Unit> ToggleUndockCommand { get; }

        public ObservableCollection<UnifiedEffectNode> EffectStack { get; } = new();
        private UnifiedEffectNode? _selectedEffect;
        public UnifiedEffectNode? SelectedEffect
        {
            get => _selectedEffect;
            set => this.RaiseAndSetIfChanged(ref _selectedEffect, value);
        }

        // Code panes (Superscope only)
        private string _scriptInit = "", _scriptFrame = "", _scriptBeat = "", _scriptPoint = "";
        public string ScriptInit { get => _scriptInit; set => this.RaiseAndSetIfChanged(ref _scriptInit, value); }
        public string ScriptFrame { get => _scriptFrame; set => this.RaiseAndSetIfChanged(ref _scriptFrame, value); }
        public string ScriptBeat { get => _scriptBeat; set => this.RaiseAndSetIfChanged(ref _scriptBeat, value); }
        public string ScriptPoint { get => _scriptPoint; set => this.RaiseAndSetIfChanged(ref _scriptPoint, value); }

        private bool _isSuperscopeSelected;
        public bool IsSuperscopeSelected { get => _isSuperscopeSelected; private set => this.RaiseAndSetIfChanged(ref _isSuperscopeSelected, value); }

        private bool _liveApply = true;
        public bool LiveApply { get => _liveApply; set => this.RaiseAndSetIfChanged(ref _liveApply, value); }

        // File state
        private string? _currentFilePath;
        public string? CurrentFilePath { get => _currentFilePath; set => this.RaiseAndSetIfChanged(ref _currentFilePath, value); }

        private bool _isUndocked;
        public bool IsUndocked { get => _isUndocked; set => this.RaiseAndSetIfChanged(ref _isUndocked, value); }
        public string PreviewDockButtonText => IsUndocked ? "Redock Preview" : "Undock Preview";

        private string _statusText = "";
        public string StatusText { get => _statusText; set => this.RaiseAndSetIfChanged(ref _statusText, value); }

        public PhxEditorViewModel()
        {
            var canRun = Observable.Return(true).ObserveOn(Ui).DistinctUntilChanged();

            ImportAvsCommand = ReactiveCommand.CreateFromTask(
                () => Task.CompletedTask,
                canRun,
                outputScheduler: Ui);

            ReimportCommand = ReactiveCommand.CreateFromTask(
                () => Task.CompletedTask,
                canRun,
                outputScheduler: Ui);

            ExportAvsCommand = ReactiveCommand.CreateFromTask(
                () => Task.CompletedTask,
                canRun,
                outputScheduler: Ui);

            ExportPhxVisCommand = ReactiveCommand.CreateFromTask(
                () => Task.CompletedTask,
                canRun,
                outputScheduler: Ui);

            NewPhxVisCommand = ReactiveCommand.Create(
                () => { /* handled in window */ }, outputScheduler: Ui);

            CompileCommand = ReactiveCommand.Create(
                () => { /* handled in window */ },
                outputScheduler: Ui);

            TestCodeCommand = ReactiveCommand.Create(
                () => { /* handled in window */ },
                outputScheduler: Ui);

            ToggleUndockCommand = ReactiveCommand.Create(
                () => { /* handled in window */ },
                outputScheduler: Ui);

            // Selection drives code visibility + text panes (Superscope only)
            this.WhenAnyValue(x => x.SelectedEffect)
                .ObserveOn(Ui)
                .Subscribe(sel =>
                {
                    var isScope = sel?.TypeKey.Equals("superscope", StringComparison.OrdinalIgnoreCase) == true;
                    IsSuperscopeSelected = isScope;
                    if (isScope && sel != null)
                    {
                        ScriptInit  = sel.Parameters.TryGetValue("init", out var i) ? Convert.ToString(i) ?? "" : "";
                        ScriptFrame = sel.Parameters.TryGetValue("frame", out var f) ? Convert.ToString(f) ?? "" : "";
                        ScriptBeat  = sel.Parameters.TryGetValue("beat", out var b) ? Convert.ToString(b) ?? "" : "";
                        ScriptPoint = sel.Parameters.TryGetValue("point", out var p) ? Convert.ToString(p) ?? "" : "";
                    }
                    else
                    {
                        ScriptInit = ScriptFrame = ScriptBeat = ScriptPoint = "";
                    }
                    this.RaisePropertyChanged(nameof(PreviewDockButtonText));
                });

            // Push edits back into the selected Superscope node
            this.WhenAnyValue(x => x.ScriptInit,  x => x.ScriptFrame, x => x.ScriptBeat, x => x.ScriptPoint, x => x.SelectedEffect)
                .Throttle(TimeSpan.FromMilliseconds(120), Ui)
                .ObserveOn(Ui)
                .Subscribe(_ =>
                {
                    if (SelectedEffect?.TypeKey.Equals("superscope", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        SelectedEffect.Parameters["init"]  = ScriptInit;
                        SelectedEffect.Parameters["frame"] = ScriptFrame;
                        SelectedEffect.Parameters["beat"]  = ScriptBeat;
                        SelectedEffect.Parameters["point"] = ScriptPoint;
                    }
                });
        }
    }
}
