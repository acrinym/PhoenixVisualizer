using PhoenixVisualizer.Rendering;
using PhoenixVisualizer.Editor.ViewModels;
using PhoenixVisualizer.Plugins.Avs;

namespace PhoenixVisualizer.Editor.Views;

using PhoenixVisualizer.App.Services;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _uiTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private RenderSurface? RenderSurfaceControl => this.FindControl<RenderSurface>("RenderHost");

    public MainWindow()
    {
        InitializeComponent();

        var prev = this.FindControl<Button>("BtnPrev");
        var next = this.FindControl<Button>("BtnNext");
        var play = this.FindControl<Button>("BtnPlayPause");
        var rand = this.FindControl<Button>("BtnRandom");
        var diag = this.FindControl<ToggleSwitch>("DiagToggle");
        var sens = this.FindControl<Slider>("SensitivitySlider");
        var smooth = this.FindControl<Slider>("SmoothingSlider");
        var maxdraw = this.FindControl<Slider>("MaxDrawSlider");

        prev.Click += (_,__) => (DataContext as MainWindowViewModel)?.PrevPreset();
        next.Click += (_,__) => (DataContext as MainWindowViewModel)?.NextPreset();
        rand.Click += (_,__) => (DataContext as MainWindowViewModel)?.RandomizePreset();
        play.Click += (_,__) => (DataContext as MainWindowViewModel)?.TogglePlayPause();

        diag.Checked += (_,__) => RenderSurfaceControl?.ToggleDiagnostics();
        diag.Unchecked += (_,__) => RenderSurfaceControl?.ToggleDiagnostics();

        sens.PropertyChanged += (_,e) => { if (e.Property == RangeBase.ValueProperty) RenderSurfaceControl?.SetSensitivity((float)sens.Value); };
        smooth.PropertyChanged += (_,e) => { if (e.Property == RangeBase.ValueProperty) RenderSurfaceControl?.SetSmoothing((float)smooth.Value); };
        maxdraw.PropertyChanged += (_,e) => { if (e.Property == RangeBase.ValueProperty) RenderSurfaceControl?.SetMaxDrawCalls((int)maxdraw.Value); };

        _uiTimer.Tick += (_,__) => UpdateStatusBar();
        _uiTimer.Start();
        
        // Hotkeys: Ctrl+S saves current node stack, Ctrl+O loads
        this.KeyDown += (s,e) => {
            if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Control && e.Key == Avalonia.Input.Key.S) { try { SaveNodePreset(); } catch {} e.Handled = true; }
            if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Control && e.Key == Avalonia.Input.Key.O) { try { LoadNodePreset(); } catch {} e.Handled = true; }
        };
        
        // Set up the ViewModel
        DataContext = new MainWindowViewModel();
        
        // spin up a default AVS plugin so the canvas isn't blank
        RenderSurfaceControl?.SetPlugin(new AvsVisualizerPlugin());
        
        // Wire up preset loading from the ViewModel
        if (DataContext is MainWindowViewModel vm)
        {
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainWindowViewModel.PresetCode) && RenderSurfaceControl != null)
                {
                    // Auto-load preset when code changes
                    var plug = new AvsVisualizerPlugin();
                    RenderSurfaceControl.SetPlugin(plug);
                    plug.LoadPreset(vm.PresetCode);
                }
            };
        }
    }

    private void OnRecentPresetDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && sender is ListBox listBox && listBox.SelectedItem is string preset)
        {
            vm.PresetCode = preset;
        }
    }

    private void SaveNodePreset()
    {
        try {
            // Assuming ViewModel exposes CurrentNodeStack
            var stack = (DataContext as MainWindowViewModel)?.CurrentNodeStack;
            if (stack == null) return;
            var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "phoenix_nodepreset.json");
            NodePresetStorage.Save(path, stack);
        } catch {}
    }
    
    private void LoadNodePreset()
    {
        try {
            var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "phoenix_nodepreset.json");
            var stack = NodePresetStorage.Load(path);
            (DataContext as MainWindowViewModel)?.LoadNodeStack(stack);
        } catch {}
    }
}