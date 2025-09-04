using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System.Collections.ObjectModel;

namespace PhoenixVisualizer.Editor.Behaviors
{
    /// <summary>
    /// Enables drag-reorder for a ListBox bound to ObservableCollection<T>.
    /// </summary>
    public static class ReorderableListBoxBehavior
    {
        public static readonly AttachedProperty<bool> IsEnabledProperty =
            AvaloniaProperty.RegisterAttached<ListBox, bool>("IsEnabled", typeof(ReorderableListBoxBehavior));

        public static bool GetIsEnabled(ListBox element) => element.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(ListBox element, bool value) => element.SetValue(IsEnabledProperty, value);

        static ReorderableListBoxBehavior()
        {
            IsEnabledProperty.Changed.AddClassHandler<ListBox>((lb, e) =>
            {
                if ((bool)e.NewValue!)
                {
                    lb.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
                    lb.AddHandler(DragDrop.DropEvent, OnDrop);
                    lb.AddHandler(DragDrop.DragOverEvent, OnDragOver);
                }
                else
                {
                    lb.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
                    lb.RemoveHandler(DragDrop.DropEvent, OnDrop);
                    lb.RemoveHandler(DragDrop.DragOverEvent, OnDragOver);
                }
            });
        }

        private const string DataFormat = "phx/reorder-index";

        private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not ListBox lb) return;
            if (e.GetCurrentPoint(lb).Properties.IsLeftButtonPressed == false) return;
            var item = (e.Source as Avalonia.Visual)?.FindAncestorOfType<ListBoxItem>();
            if (item == null) return;
            var index = lb.IndexFromContainer(item);
            if (index < 0) return;

            var data = new DataObject();
            data.Set(DataFormat, index);
            DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
        }

        private static void OnDragOver(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormat)) e.DragEffects = DragDropEffects.Move;
            else if (e.Data.Contains("phx/palette-node")) e.DragEffects = DragDropEffects.Copy;
            else e.DragEffects = DragDropEffects.None;
        }

        private static void OnDrop(object? sender, DragEventArgs e)
        {
            if (sender is not ListBox lb) return;
            if (lb.ItemsSource is not System.Collections.IList list) return;
            var pos = e.GetPosition(lb);
            int insertIndex = list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                if (lb.ContainerFromIndex(i) is Control c)
                {
                    var r = c.Bounds;
                    var y = c.TranslatePoint(new Avalonia.Point(0, 0), lb)!.Value.Y;
                    if (pos.Y < y + r.Height / 2) { insertIndex = i; break; }
                }
            }

            if (e.Data.Contains(DataFormat))
            {
                // Reorder
                var from = (int)e.Data.Get(DataFormat)!;
                if (from == insertIndex) return;
                if (list is ObservableCollection<object> oc)
                {
                    var item = oc[from];
                    oc.RemoveAt(from);
                    oc.Insert(Math.Clamp(insertIndex, 0, oc.Count), item);
                }
                else
                {
                    var item = list[from];
                    list.RemoveAt(from);
                    list.Insert(Math.Clamp(insertIndex, 0, list.Count), item);
                }
                e.Handled = true;
                return;
            }

            if (e.Data.Contains("phx/palette-node"))
            {
                // Creation from palette handled in Window code-behind (we just signal).
                e.Handled = false;
            }
        }
    }
}
