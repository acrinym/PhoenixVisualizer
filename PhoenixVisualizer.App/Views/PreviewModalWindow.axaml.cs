using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Controls.Shapes;
using System;

namespace PhoenixVisualizer.Views
{
    public partial class PreviewModalWindow : Window
    {
        private Canvas? _host;

        public PreviewModalWindow()
        {
            InitializeComponent();
            _host = this.FindControl<Canvas>("PreviewHost");
            AttachGripHandlers();
        }

        private void AttachGripHandlers()
        {
            foreach (var grip in this.GetVisualDescendants().OfType<Rectangle>().Where(r => r.Classes.Contains("Grip")))
            {
                grip.PointerPressed += OnGripPressed;
            }
        }

        private void OnGripPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                var rect = sender as Rectangle;
                if (rect is null) return;
                var edge = 0; // WindowEdge.Undefined

                if (rect.Cursor == new Cursor(StandardCursorType.SizeWestEast))
                    edge = rect.HorizontalAlignment == HorizontalAlignment.Left ? 8 : 2; // WindowEdge.West : WindowEdge.East
                else if (rect.Cursor == new Cursor(StandardCursorType.SizeNorthSouth))
                    edge = rect.VerticalAlignment == VerticalAlignment.Top ? 1 : 4; // WindowEdge.North : WindowEdge.South
                else if (rect.Cursor == new Cursor(StandardCursorType.TopLeftCorner)) edge = 9; // WindowEdge.NorthWest
                else if (rect.Cursor == new Cursor(StandardCursorType.TopRightCorner)) edge = 3; // WindowEdge.NorthEast
                else if (rect.Cursor == new Cursor(StandardCursorType.BottomLeftCorner)) edge = 12; // WindowEdge.SouthWest
                else if (rect.Cursor == new Cursor(StandardCursorType.BottomRightCorner)) edge = 6; // WindowEdge.SouthEast

                if (edge != 0)
                    BeginResizeDrag((WindowEdge)edge, e);
            }
        }

        private void OnClose(object? sender, RoutedEventArgs e) => Close();
    }
}