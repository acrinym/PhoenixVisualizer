using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using PhoenixVisualizer.App.Services;
using PhoenixVisualizer.App.ViewModels;

namespace PhoenixVisualizer.App.Controls
{
    public partial class ParameterPanel : UserControl
    {
        public ParameterPanel()
        {
            InitializeComponent();
            ParameterBus.TargetChanged += OnParamTargetChanged;
            // Initialize with current target if one already exists
            if (ParameterBus.CurrentTarget != null)
                OnParamTargetChanged(ParameterBus.CurrentTarget);
        }

        private void OnParamTargetChanged(object? obj)
        {
            var vm = DataContext as ParameterEditorViewModel;
            if (vm is null) return;
            if (Dispatcher.UIThread.CheckAccess()) vm.SetTarget(obj);
            else Dispatcher.UIThread.Post(() => vm.SetTarget(obj));
        }

        public void SetTarget(object? obj)
        {
            ParameterBus.PublishTarget(obj);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
