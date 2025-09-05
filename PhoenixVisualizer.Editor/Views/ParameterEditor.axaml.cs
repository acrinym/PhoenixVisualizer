using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls.Primitives;
using System;
using System.Linq;

namespace PhoenixVisualizer.Editor.Views
{
    public partial class ParameterEditor : UserControl
    {
        public ParameterEditor()
        {
            InitializeComponent();

            var live = this.FindControl<ToggleSwitch>("LivePreviewToggle");
            var apply = this.FindControl<Button>("ApplyBtn");
            var revert = this.FindControl<Button>("RevertBtn");
            var search = this.FindControl<TextBox>("SearchBox");

            live.IsCheckedChanged += (_,__) => RaiseEvent(new RoutedEventArgs(LivePreviewChangedEvent));
            apply.Click += (_,__) => RaiseEvent(new RoutedEventArgs(ApplyRequestedEvent));
            revert.Click += (_,__) => RaiseEvent(new RoutedEventArgs(RevertRequestedEvent));
            search.GetObservable(TextBox.TextProperty).Subscribe(_ => FilterParameters(search.Text));
        }

        public static readonly RoutedEvent<RoutedEventArgs> LivePreviewChangedEvent =
            RoutedEvent.Register<ParameterEditor, RoutedEventArgs>(nameof(LivePreviewChanged), RoutingStrategies.Bubble);
        public static readonly RoutedEvent<RoutedEventArgs> ApplyRequestedEvent =
            RoutedEvent.Register<ParameterEditor, RoutedEventArgs>(nameof(ApplyRequested), RoutingStrategies.Bubble);
        public static readonly RoutedEvent<RoutedEventArgs> RevertRequestedEvent =
            RoutedEvent.Register<ParameterEditor, RoutedEventArgs>(nameof(RevertRequested), RoutingStrategies.Bubble);

        public event EventHandler<RoutedEventArgs> LivePreviewChanged { add => AddHandler(LivePreviewChangedEvent, value); remove => RemoveHandler(LivePreviewChangedEvent, value); }
        public event EventHandler<RoutedEventArgs> ApplyRequested { add => AddHandler(ApplyRequestedEvent, value); remove => RemoveHandler(ApplyRequestedEvent, value); }
        public event EventHandler<RoutedEventArgs> RevertRequested { add => AddHandler(RevertRequestedEvent, value); remove => RemoveHandler(RevertRequestedEvent, value); }

        private void FilterParameters(string? term)
        {
            var root = this.FindControl<StackPanel>("Root");
            if (root == null) return;
            foreach (var child in root.Children.OfType<Control>())
            {
                if (string.IsNullOrWhiteSpace(term)) { child.IsVisible = true; continue; }
                var text = child.ToString() ?? "";
                child.IsVisible = text.Contains(term, StringComparison.OrdinalIgnoreCase);
            }
        }

        // Missing method that PhxEditorWindow expects
        public void LoadFor(object? target, string? displayName = null)
        {
            // TODO: Implement parameter loading for target object
            System.Diagnostics.Debug.WriteLine($"Loading parameters for: {target?.GetType().Name ?? "null"} (Display: {displayName ?? "N/A"})");
        }
    }
}
