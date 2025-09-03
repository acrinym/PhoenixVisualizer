using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PhoenixVisualizer.App.Views
{
    public partial class ParameterEditorWindow : Window
    {
        public ParameterEditorWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
