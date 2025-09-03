using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Linq;
using PhoenixVisualizer.Core.Transpile;
using PhoenixVisualizer.Core.Catalog;

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
        public ReactiveCommand<object, Unit> AddByTypeKeyCommand { get; }
        public ReactiveCommand<Unit, Unit> RemoveSelectedCommand { get; }
        public ReactiveCommand<Unit, Unit> DuplicateSelectedCommand { get; }
        public ReactiveCommand<Unit, Unit> MoveUpCommand { get; }
        public ReactiveCommand<Unit, Unit> MoveDownCommand { get; }

        public ObservableCollection<UnifiedEffectNode> EffectStack { get; } = new();
        private UnifiedEffectNode? _selectedEffect;
        public UnifiedEffectNode? SelectedEffect { get => _selectedEffect; set => this.RaiseAndSetIfChanged(ref _selectedEffect, value); }

        // catalog
        public ObservableCollection<NodeMeta> Catalog { get; } = new();
        private string _catalogFilter = "";
        public string CatalogFilter { get => _catalogFilter; set { this.RaiseAndSetIfChanged(ref _catalogFilter, value); RefreshCatalog(); } }
        private string _catalogCategory = "All";
        public string CatalogCategory { get => _catalogCategory; set { this.RaiseAndSetIfChanged(ref _catalogCategory, value); RefreshCatalog(); } }
        public ReadOnlyObservableCollection<NodeMeta> CatalogView => _catalogView;
        private ReadOnlyObservableCollection<NodeMeta> _catalogView = default!;
        private readonly ObservableCollection<NodeMeta> _catalogBacking = new();

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

            AddByTypeKeyCommand = ReactiveCommand.Create<object>(param =>
            {
                string typeKey;
                if (param is NodeMeta meta)
                {
                    typeKey = meta.TypeKey;
                }
                else if (param is string str)
                {
                    typeKey = str;
                }
                else
                {
                    return;
                }
                
                var nodeMeta = EffectNodeCatalog.TryGet(typeKey, out var m) ? m :
                    new NodeMeta(typeKey, typeKey, "Custom", () => new UnifiedEffectNode{ TypeKey = typeKey, DisplayName = typeKey });
                var node = nodeMeta.CreateNode();
                EffectStack.Add(node);
                SelectedEffect = node;
                StatusText = $"Added: {nodeMeta.DisplayName}";
            }, outputScheduler: Ui);

            RemoveSelectedCommand = ReactiveCommand.Create(() =>
            {
                if (SelectedEffect != null)
                {
                    var i = EffectStack.IndexOf(SelectedEffect);
                    EffectStack.Remove(SelectedEffect);
                    if (EffectStack.Count > 0)
                        SelectedEffect = EffectStack[Math.Clamp(i-1, 0, EffectStack.Count-1)];
                    StatusText = "Removed selected effect.";
                }
            }, outputScheduler: Ui);

            DuplicateSelectedCommand = ReactiveCommand.Create(() =>
            {
                if (SelectedEffect == null) return;
                var dupe = new UnifiedEffectNode
                {
                    TypeKey = SelectedEffect.TypeKey,
                    DisplayName = SelectedEffect.DisplayName + " (Copy)"
                };
                foreach (var kv in SelectedEffect.Parameters) dupe.Parameters[kv.Key] = kv.Value;
                var idx = EffectStack.IndexOf(SelectedEffect);
                EffectStack.Insert(Math.Max(0, idx+1), dupe);
                SelectedEffect = dupe;
                StatusText = "Duplicated effect.";
            }, outputScheduler: Ui);

            MoveUpCommand = ReactiveCommand.Create(() =>
            {
                if (SelectedEffect == null) return;
                var i = EffectStack.IndexOf(SelectedEffect);
                if (i > 0) { EffectStack.Move(i, i-1); }
            }, outputScheduler: Ui);

            MoveDownCommand = ReactiveCommand.Create(() =>
            {
                if (SelectedEffect == null) return;
                var i = EffectStack.IndexOf(SelectedEffect);
                if (i >= 0 && i < EffectStack.Count-1) { EffectStack.Move(i, i+1); }
            }, outputScheduler: Ui);

            // setup catalog backing view
            _catalogView = new ReadOnlyObservableCollection<NodeMeta>(_catalogBacking);
            EffectNodeCatalog.CatalogChanged += RefreshCatalog;
            RefreshCatalog();

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

        private void RefreshCatalog()
        {
            var all = EffectNodeCatalog.All();
            var filter = _catalogFilter?.Trim() ?? "";
            var cat = _catalogCategory ?? "All";
            var filtered = all.Where(m =>
            {
                var okCat = cat == "All" || string.Equals(m.Category, cat, StringComparison.OrdinalIgnoreCase);
                var okTxt = string.IsNullOrEmpty(filter) ||
                            m.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                            m.TypeKey.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                            (m.Tags != null && m.Tags.Any(t => t.Contains(filter, StringComparison.OrdinalIgnoreCase)));
                return okCat && okTxt;
            }).ToList();
            _catalogBacking.Clear();
            foreach (var m in filtered) _catalogBacking.Add(m);
        }
    }
}
