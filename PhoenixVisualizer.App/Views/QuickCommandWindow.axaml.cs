using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Linq;

namespace PhoenixVisualizer.App.Views
{
    public partial class QuickCommandWindow : Window
    {
        private readonly string[] _items;
        public event Action<string>? ItemChosen;

        public QuickCommandWindow(string[] items)
        {
            InitializeComponent();
            _items = items;
            var list = this.FindControl<ListBox>("List");
            if (list != null)
            {
                list.ItemsSource = _items;
                var search = this.FindControl<TextBox>("Search");
                if (search != null)
                {
                    search.GetObservable(TextBox.TextProperty).Subscribe(t => {
                        if (list != null)
                        {
                            list.ItemsSource = string.IsNullOrWhiteSpace(t) ? _items : _items.Where(i => i.Contains(t, StringComparison.OrdinalIgnoreCase)).ToArray();
                        }
                    });
                }
            }
            if (list != null)
            {
                list.DoubleTapped += (_,__) => Choose();
                list.KeyDown += (s,e) => { if (e.Key == Avalonia.Input.Key.Enter) Choose(); };
            }
        }

        private void Choose()
        {
            var list = this.FindControl<ListBox>("List");
            if (list?.SelectedItem is string s) { ItemChosen?.Invoke(s); Close(); }
        }
    }
}

