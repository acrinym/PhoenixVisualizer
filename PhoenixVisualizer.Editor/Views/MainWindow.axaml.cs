using PhoenixVisualizer.Editor.Rendering;
using PhoenixVisualizer.Editor.ViewModels;
using PhoenixVisualizer.Plugins.Avs;

namespace PhoenixVisualizer.Editor.Views;

public partial class MainWindow : Window
{
    private RenderSurface? RenderSurfaceControl => this.FindControl<RenderSurface>("RenderHost");

    public MainWindow()
    {
        InitializeComponent();
        
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
}