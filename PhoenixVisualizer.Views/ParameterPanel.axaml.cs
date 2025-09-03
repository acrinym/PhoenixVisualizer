using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PhoenixVisualizer.Views
{
    public partial class ParameterPanel : UserControl
    {
        public ParameterPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
