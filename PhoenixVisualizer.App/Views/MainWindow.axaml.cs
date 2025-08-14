using Avalonia.Controls;
using Avalonia.Interactivity;
using PhoenixVisualizer.Rendering;

namespace PhoenixVisualizer.Views;

public partial class MainWindow : Window
{
    private RenderSurface? Render => this.FindControl<RenderSurface>("Render");

    public MainWindow()
    {
        InitializeComponent();
    }
}