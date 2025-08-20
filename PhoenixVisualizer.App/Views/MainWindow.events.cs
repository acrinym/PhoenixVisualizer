using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Views;

public partial class MainWindow : Window
{
    protected override void OnClosed(EventArgs e)
    {
        try { NativeAvsHost.Stop(); } catch { /* ignore */ }
        base.OnClosed(e);
    }


}
