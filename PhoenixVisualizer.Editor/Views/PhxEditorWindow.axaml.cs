using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.IO;
using PhoenixVisualizer.Visuals;
using System.Linq;
using System.Threading.Tasks;
using PhoenixVisualizer.Core.Transpile;
using PhoenixVisualizer.Core.Serialization;
using PhoenixVisualizer.Core.Catalog;
using PhoenixVisualizer.Editor.ViewModels;
using PhoenixVisualizer.Core;
using PhoenixVisualizer.Editor.Rendering;
using PhoenixVisualizer.Editor.Views;
using PhoenixVisualizer.Parameters;
using Avalonia.Input;
using Avalonia;
using Avalonia.VisualTree;

namespace PhoenixVisualizer.Editor.Views
{
    public partial class PhxEditorWindow : Window
    {
        private readonly CompositeDisposable _disposables = new();
        private RenderSurface? _preview;
        private PhxEditorViewModel? _vm;
        private ParameterEditor? _parameterEditor;
        private Window? _undockedPreviewWindow;
        private RenderSurface? _undockedSurface;
        private bool _fileDialogOpen = false;

        public PhxEditorWindow()
        {
            InitializeComponent();
            this.Opened += OnOpened;
            this.Closed += (_, __) => _disposables.Dispose();
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            _preview = this.FindControl<RenderSurface>("PreviewSurface");
            _parameterEditor = this.FindControl<ParameterEditor>("ParameterEditor");
            if (_preview != null) _preview.SetPlugin(new SanityVisualizer());

            _vm = DataContext as PhxEditorViewModel;
            if (_vm == null) return;

            // Register baseline parameter for samples
            const string VizId = "unified_phoenix";
            ParamRegistry.Register(VizId, new List<ParamDef>
            {
                new() { Key = "samples", Label = "Samples", Type = ParamType.Slider, DefaultValue = 512.0, Min = 16, Max = 4096 }
            });
            _parameterEditor?.LoadFor(VizId, "Unified Phoenix");

            WireUpCommands(_vm);
            WireUpSelection(_vm);
            
            // Wire up live compilation
            _vm.CompileRequested += (_, __) => CompileFromStack();

            // Optionally load builtin parameter docs (*.json) from "Presets/Params"
            TryLoadBuiltinParamDocs();

            // Load catalog from disk (Presets/Effects) in addition to reflection-based defaults
            TryLoadEffectCatalogDocs();
            
            // Wire up drag and drop for Effect Stack
            WireUpDragAndDrop();
        }

        private void WireUpCommands(PhxEditorViewModel vm)
        {
            // Wire up compile and test commands
            vm.CompileCommand
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => CompileFromStack())
                .DisposeWith(_disposables);

            vm.TestCodeCommand
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => { CompileFromStack(); vm.StatusText = "Test running."; })
                .DisposeWith(_disposables);

            vm.ImportAvsCommand
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async _ => await ImportAvsAsync())
                .DisposeWith(_disposables);

            vm.ReimportCommand
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async _ => await ReimportAsync())
                .DisposeWith(_disposables);

            vm.ExportAvsCommand
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async _ => await ExportAvsAsync())
                .DisposeWith(_disposables);

            vm.ExportPhxVisCommand
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async _ => await ExportPhxVisAsync())
                .DisposeWith(_disposables);

            vm.NewPhxVisCommand
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => NewPhxVis())
                .DisposeWith(_disposables);

            vm.ToggleUndockCommand
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => ToggleUndockPreview())
                .DisposeWith(_disposables);

            vm.AddNodeCommand
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => AddSelectedNode())
                .DisposeWith(_disposables);

            // Live apply: when scripts change and LiveApply is on, recompile
            vm.WhenAnyValue(x => x.ScriptInit, x => x.ScriptFrame, x => x.ScriptBeat, x => x.ScriptPoint, x => x.LiveApply)
                .Throttle(TimeSpan.FromMilliseconds(120), RxApp.MainThreadScheduler)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => { if (_vm?.LiveApply == true) CompileFromStack(); })
                .DisposeWith(_disposables);

            // Parameters live updates: refresh compile if a node param changes
            ParamRegistry.ValueChanged += (vid, key, value) =>
            {
                if (_vm?.EffectStack.Any(n => n.Id == vid) == true && _vm.LiveApply)
                    CompileFromStack();
            };

            // Add-by-typekey from catalog (e.g., context menu/Enter)
            vm.AddByTypeKeyCommand.Subscribe(_ => CompileFromStack()).DisposeWith(_disposables);
        }

        private void WireUpSelection(PhxEditorViewModel vm)
        {
            vm.WhenAnyValue(x => x.SelectedEffect)
              .ObserveOn(RxApp.MainThreadScheduler)
              .Subscribe(sel =>
              {
                  if (sel == null || _parameterEditor == null) return;
                  
                  // Convert node parameters to ParamDef structure
                  var defs = new List<ParamDef>();
                  foreach (var (k, v) in sel.Parameters)
                  {
                      var (ptype, defVal, min, max) = v switch
                      {
                          bool b   => (ParamType.Checkbox, (object)b, 0d, 1d),
                          int i    => (ParamType.Slider, (object)(double)i, 0d, 4096d),
                          double d => (ParamType.Slider, (object)d, 0d, 1d),
                          string s => (ParamType.Text, (object)s, 0d, 1d),
                          _        => (ParamType.Text, (object)(v?.ToString() ?? ""), 0d, 1d)
                      };
                      defs.Add(new ParamDef { Key = k, Label = k, Type = ptype, DefaultValue = defVal, Min = min, Max = max });
                  }
                  
                  ParamRegistry.Register(sel.Id, defs);
                  _parameterEditor.LoadFor(sel.Id, sel.DisplayName ?? "Unknown");
                  
                  // Live-sync parameter changes back into SelectedEffect.Parameters
                  ParamRegistry.ValueChanged += (vizId, key, value) =>
                  {
                      if (vizId == sel.Id && sel.Parameters.ContainsKey(key))
                          sel.Parameters[key] = value ?? "";
                  };
              })
              .DisposeWith(_disposables);
        }

        private void CompileFromStack()
        {
            if (_vm == null) return;
            var target = _vm.IsUndocked ? _undockedSurface : _preview;
            if (target == null)
            {
                _vm.StatusText = "No preview surface.";
                return;
            }
            if (_vm.EffectStack.Count == 0)
            {
                target.SetPlugin(new SanityVisualizer()); // only when truly empty
                _vm.StatusText = "No nodes; showing generic visualizer.";
                return;
            }
            var plugin = new SanityVisualizer(); // TODO: Replace with proper unified visualizer
            // plugin.LoadGraph(new UnifiedGraph { Nodes = _vm.EffectStack.ToList() }); // TODO: Implement graph loading
            target.SetPlugin(plugin);
            _vm.StatusText = "Compiled current stack.";
        }

        private async Task ImportAvsAsync()
        {
            if (StorageProvider == null || _vm == null || _fileDialogOpen) return;
            _fileDialogOpen = true;
            
            try
            {
                var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Import AVS",
                    AllowMultiple = false,
                    FileTypeFilter = new List<FilePickerFileType>
                    {
                        new FilePickerFileType("Winamp AVS (*.avs)") { Patterns = new[] { "*.avs" } },
                        new FilePickerFileType("Phoenix Preset (*.phxviz)") { Patterns = new[] { "*.phxviz" } }
                    }
                });
                var f = files?.FirstOrDefault(); if (f == null) return;
                await LoadFileAsync(f.Path.LocalPath);
            }
            finally
            {
                _fileDialogOpen = false;
            }
        }

        private async Task ReimportAsync()
        {
            if (_vm?.CurrentFilePath is string path && File.Exists(path))
            {
                await LoadFileAsync(path);
            }
            else
            {
                await ImportAvsAsync(); // fall back to picker if nothing loaded
            }
        }

        private async Task LoadFileAsync(string path)
        {
            if (_vm == null) return;
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".avs")
            {
                var bytes = await File.ReadAllBytesAsync(path);
                var graph = WinampAvsImporter.Import(bytes);
                _vm.EffectStack.Clear();
                foreach (var n in graph.Nodes) _vm.EffectStack.Add(n);
                _vm.SelectedEffect = _vm.EffectStack.FirstOrDefault();
                _vm.CurrentFilePath = path;
                _vm.StatusText = $"Imported AVS: {Path.GetFileName(path)}";
            }
            else if (ext == ".phxviz")
            {
                var bytes = await File.ReadAllBytesAsync(path);
                var graph = PhxVizSerializer.Load(bytes);
                _vm.EffectStack.Clear();
                foreach (var n in graph.Nodes) _vm.EffectStack.Add(n);
                _vm.SelectedEffect = _vm.EffectStack.FirstOrDefault();
                _vm.CurrentFilePath = path;
                _vm.StatusText = $"Loaded Phoenix preset: {Path.GetFileName(path)}";
            }
            else
            {
                _vm.StatusText = "Unsupported file type.";
            }
            
            // The ViewModel will automatically populate script panes when SelectedEffect changes
            
            // Immediately compile so user sees content
            CompileFromStack();
        }

        private async Task ExportAvsAsync()
        {
            if (StorageProvider == null || _vm == null || _vm.EffectStack.Count == 0) return;
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export AVS",
                SuggestedFileName = "preset.avs",
                DefaultExtension = "avs",
                FileTypeChoices = new List<FilePickerFileType> { new FilePickerFileType("Winamp AVS (*.avs)") { Patterns = new[] { "*.avs" } } }
            });
            if (file == null) return;
            var bytes = WinampAvsExporter.Export(new UnifiedGraph { Nodes = _vm.EffectStack.ToList() });
            await File.WriteAllBytesAsync(file.Path.LocalPath, bytes);
            _vm.StatusText = $"Exported AVS: {file.Name}";
        }

        private async Task ExportPhxVisAsync()
        {
            if (StorageProvider == null || _vm == null) return;
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Phoenix Preset",
                SuggestedFileName = "preset.phxviz",
                DefaultExtension = "phxviz",
                FileTypeChoices = new List<FilePickerFileType> { new FilePickerFileType("Phoenix Preset (*.phxviz)") { Patterns = new[] { "*.phxviz" } } }
            });
            if (file == null) return;
            var bytes = PhxVizSerializer.Save(new UnifiedGraph { Nodes = _vm.EffectStack.ToList() });
            await File.WriteAllBytesAsync(file.Path.LocalPath, bytes);
            _vm.StatusText = $"Exported Phoenix preset: {file.Name}";
        }

        private void NewPhxVis()
        {
            if (_vm == null) return;
            _vm.EffectStack.Clear();
            var node = PhoenixVisualizer.Core.Transpile.EffectNodeCatalog.Create("superscope");
            _vm.EffectStack.Add(node);
            _vm.SelectedEffect = node;
            _vm.CurrentFilePath = null;
            _vm.StatusText = "New Phoenix preset created.";
            CompileFromStack();
        }

        private void ToggleUndockPreview()
        {
            if (_vm == null) return;
            if (!_vm.IsUndocked)
            {
                // Undock: create a modal window and move the surface
                _undockedSurface = new RenderSurface();
                _undockedPreviewWindow = new Window
                {
                    Width = 800, Height = 600, Title = "Preview",
                    Content = _undockedSurface
                };
                _undockedPreviewWindow.Closed += (_, __) =>
                {
                    // Redock
                    if (_preview != null)
                    {
                        // Refresh compile into docked surface
                        CompileFromStack();
                    }
                    _vm.IsUndocked = false;
                    _vm.RaisePropertyChanged(nameof(_vm.PreviewDockButtonText));
                    _undockedSurface = null;
                    _undockedPreviewWindow = null;
                };
                _vm.IsUndocked = true;
                _vm.RaisePropertyChanged(nameof(_vm.PreviewDockButtonText));
                _undockedPreviewWindow.Show(this);
                // render current content into undocked surface
                CompileFromStack();
            }
            else
            {
                // Redock explicitly
                _undockedPreviewWindow?.Close();
            }
        }

        private void TryLoadBuiltinParamDocs()
        {
            try
            {
                var exe = AppContext.BaseDirectory;
                var folder = Path.Combine(exe, "Presets", "Params");
                PhoenixVisualizer.Parameters.ParamJson.LoadFolder(folder);
            }
            catch { /* optional */ }
        }

        private void TryLoadEffectCatalogDocs()
        {
            try
            {
                var exe = AppContext.BaseDirectory;
                var folder = Path.Combine(exe, "Presets", "Effects");
                PhoenixVisualizer.Core.Catalog.EffectNodeCatalog.LoadFolder(folder);
            }
            catch { /* optional */ }
        }
        
        private void WireUpDragAndDrop()
        {
            var effectStackListBox = this.FindControl<ListBox>("EffectStackListBox");
            if (effectStackListBox == null) return;
            
            // Enable drag and drop for reordering and file drops
            effectStackListBox.AddHandler(DragDrop.DragOverEvent, OnStackDragOver);
            effectStackListBox.AddHandler(DragDrop.DropEvent, OnStackDrop);
        }

        // ----- Drag & Drop from catalog into stack -----
        public void OnStackDragEnter(object? sender, DragEventArgs e) { e.DragEffects = DragDropEffects.Copy; }
        public void OnStackDragLeave(object? sender, DragEventArgs e) { }
        public void OnStackDragOver(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains("application/x-phx-node"))
            {
                e.DragEffects = DragDropEffects.Copy;
            }
            else if (e.Data.Contains(DataFormats.Files))
            {
                var files = e.Data.GetFiles()?.ToList();
                if (files != null && files.Any() && Path.GetExtension(files.First().Name).ToLowerInvariant() == ".avs")
                {
                    e.DragEffects = DragDropEffects.Copy;
                }
                else
                {
                    e.DragEffects = DragDropEffects.None;
                }
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
        }
        public void OnStackDrop(object? sender, DragEventArgs e)
        {
            if (_vm == null) return;
            
            // Handle effect node drops
            if (e.Data.Get("application/x-phx-node") is string typeKey)
            {
                var node = PhoenixVisualizer.Core.Transpile.EffectNodeCatalog.Create(typeKey);
                _vm.EffectStack.Add(node);
                _vm.SelectedEffect = node;
                CompileFromStack();
            }
            // Handle file drops
            else if (e.Data.Contains(DataFormats.Files))
            {
                var files = e.Data.GetFiles()?.ToList();
                if (files != null && files.Any())
                {
                    var file = files.First();
                    if (Path.GetExtension(file.Name).ToLowerInvariant() == ".avs")
                    {
                        _ = Task.Run(async () => await LoadFileAsync(file.Path.LocalPath));
                    }
                }
            }
        }

        private void AddSelectedNode()
        {
            if (_vm == null) return;
            var kind = _vm.SelectedNodeType?.ToLowerInvariant() ?? "superscope";
            var node = PhoenixVisualizer.Core.Transpile.EffectNodeCatalog.Create(kind);
            _vm.EffectStack.Add(node);
            _vm.SelectedEffect = node;
            CompileFromStack();
            _vm.StatusText = $"Added node: {node.DisplayName}";
        }

        // ----- Palette → start drag -----
        public async void OnPalettePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not ListBox lb) return;
            if (e.GetCurrentPoint(lb).Properties.IsLeftButtonPressed == false) return;
            var sel = lb.SelectedItem as string;
            if (string.IsNullOrEmpty(sel)) return;
            var data = new DataObject();
            data.Set("phx/palette-node", sel);
            await DragDrop.DoDragDrop(e, data, DragDropEffects.Copy);
        }

        // ----- Stack accept palette drop / support external reorder behavior -----
        public void OnEffectStackDragOver(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains("phx/palette-node")) e.DragEffects = DragDropEffects.Copy;
            else e.DragEffects = DragDropEffects.Move; // Reorder handled by behavior
        }
        public void OnEffectStackDrop(object? sender, DragEventArgs e)
        {
            if (_vm == null) return;
            if (!e.Data.Contains("phx/palette-node")) return; // reorder handled in behavior
            var type = e.Data.Get("phx/palette-node") as string ?? "Superscope";
            var node = PhoenixVisualizer.Core.Transpile.EffectNodeCatalog.Create(type);
            // compute insert index from drop position
            var lb = sender as ListBox;
            int insert = _vm.EffectStack.Count;
            if (lb != null)
            {
                var pos = e.GetPosition(lb);
                for (int i = 0; i < _vm.EffectStack.Count; i++)
                {
                    if (lb.ContainerFromIndex(i) is Control c)
                    {
                        var r = c.Bounds;
                        var y = c.TranslatePoint(new Avalonia.Point(0, 0), lb)!.Value.Y;
                        if (pos.Y < y + r.Height / 2) { insert = i; break; }
                    }
                }
            }
            _vm.EffectStack.Insert(Math.Clamp(insert, 0, _vm.EffectStack.Count), node);
            _vm.SelectedEffect = node;
            _vm.IsDirty = true;
            CompileFromStack();
            e.Handled = true;
        }
    }


}
