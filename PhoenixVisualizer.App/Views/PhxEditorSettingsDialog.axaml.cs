using Avalonia.Controls;
using Avalonia.Interactivity;
using PhoenixVisualizer.App.ViewModels;

namespace PhoenixVisualizer.Views
{
    public partial class PhxEditorSettingsDialog : Window
    {
        private bool _settingsChanged = false;
        private PhxEditorSettings? _originalSettings;
        
        public PhxEditorSettingsDialog()
        {
            InitializeComponent();
            _originalSettings = DataContext as PhxEditorSettings;
        }
        
        private void OnApply(object? sender, RoutedEventArgs e)
        {
            if (DataContext is PhxEditorSettings settings)
            {
                settings.Save();
                settings.ApplyTheme();
                _settingsChanged = true;
            }
        }
        
        private void OnOK(object? sender, RoutedEventArgs e)
        {
            OnApply(sender, e);
            Close();
        }
        
        private void OnClose(object? sender, RoutedEventArgs e)
        {
            // If settings weren't applied, restore original settings
            if (!_settingsChanged && _originalSettings != null && DataContext is PhxEditorSettings currentSettings)
            {
                // Restore original values
                DataContext = _originalSettings;
            }
            Close();
        }
    }
}
