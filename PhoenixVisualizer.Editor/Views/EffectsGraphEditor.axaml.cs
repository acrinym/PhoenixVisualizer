using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Controls.Shapes;

using PhoenixVisualizer.Core.Effects.Graph;
using PhoenixVisualizer.Core.Effects.Interfaces;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;
using PhoenixVisualizer.Editor.Models;
using PhoenixVisualizer.Editor.Rendering;
using PhoenixVisualizer.Editor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoenixVisualizer.Editor.Views
{
    public partial class EffectsGraphEditor : UserControl
    {
        private Canvas? _graphCanvas;
        private RenderSurface? _previewSurface;
        private Point _lastMousePosition;
        private bool _isDragging = false;
        private bool _isConnecting = false;
        private VisualNode? _draggedNode = null;
        private VisualNode? _sourceNode = null;
        private Point _connectionStartPoint;
        private List<VisualNode> _visualNodes = new();
        private List<VisualConnection> _visualConnections = new();
        private Shape? _connectionPreview;  // Connection preview field

        public EffectsGraphEditor()
        {
            InitializeComponent();
            InitializeCanvas();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _graphCanvas = this.FindControl<Canvas>("GraphCanvas");
            _previewSurface = this.FindControl<RenderSurface>("PreviewSurface");
        }

        private void InitializeCanvas()
        {
            if (_graphCanvas != null)
            {
                // Set up canvas properties
                _graphCanvas.ClipToBounds = true;
                
                // Add grid background
                var gridBrush = new DrawingBrush
                {
                    Drawing = new GeometryDrawing
                    {
                        Brush = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                        Geometry = new RectangleGeometry(new Rect(0, 0, 1000, 1000))
                    }
                };
                
                // Add grid lines
                for (int x = 0; x <= 1000; x += 20)
                {
                    var line = new Avalonia.Controls.Shapes.Line
                    {
                        StartPoint = new Point(x, 0),
                        EndPoint = new Point(x, 1000),
                        Stroke = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                        StrokeThickness = 1
                    };
                    _graphCanvas.Children.Add(line);
                }
                
                for (int y = 0; y <= 1000; y += 20)
                {
                    var line = new Avalonia.Controls.Shapes.Line
                    {
                        StartPoint = new Point(0, y),
                        EndPoint = new Point(1000, y),
                        Stroke = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                        StrokeThickness = 1
                    };
                    _graphCanvas.Children.Add(line);
                }
            }
        }

        #region Connection Preview Methods
        private void CreateConnectionPreview(Point startPoint, Point endPoint)
        {
            if (_graphCanvas == null) return;

            var line = new Line
            {
                StartPoint = startPoint,
                EndPoint = endPoint,
                Stroke = new SolidColorBrush(Colors.DodgerBlue),
                StrokeThickness = 2,
                StrokeDashArray = [5, 5],
                IsHitTestVisible = false
            };

            _connectionPreview = line;
            _graphCanvas.Children.Add(line);
        }

        private void UpdateConnectionPreview(Point endPoint)
        {
            if (_connectionPreview is Line line)
            {
                line.EndPoint = endPoint;
            }
        }

        private void RemoveConnectionPreview()
        {
            if (_graphCanvas == null) return;
            if (_connectionPreview == null) return;
            _graphCanvas.Children.Remove(_connectionPreview);
            _connectionPreview = null;
        }
        #endregion

        #region Event Handlers

        private void OnNodeDragStarted(object sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.DataContext is IEffectNode node)
            {
                _draggedNode = new VisualNode(node);
                _isDragging = true;
                e.Handled = true;
            }
        }

        private void OnCanvasPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (_graphCanvas == null) return;

            var position = e.GetPosition(_graphCanvas);
            _lastMousePosition = position;

            if (e.GetCurrentPoint(_graphCanvas).Properties.IsLeftButtonPressed)
            {
                // Check if clicking on a node
                var clickedNode = FindNodeAtPosition(position);
                if (clickedNode != null)
                {
                    if (_isConnecting)
                    {
                        // Complete connection
                        CompleteConnection(clickedNode);
                    }
                    else
                    {
                        // Start dragging node
                        StartDraggingNode(clickedNode, position);
                    }
                }
                else
                {
                    // Check if clicking on a connection
                    var clickedConnection = FindConnectionAtPosition(position);
                    if (clickedConnection != null)
                    {
                        SelectConnection(clickedConnection);
                    }
                    else
                    {
                        // Clicked on empty space - deselect
                        DeselectAll();
                    }
                }
            }
            else if (e.GetCurrentPoint(_graphCanvas).Properties.IsRightButtonPressed)
            {
                // Right click - show context menu
                ShowContextMenu(position);
            }

            e.Handled = true;
        }

        private void OnCanvasPointerMoved(object sender, PointerEventArgs e)
        {
            if (_graphCanvas == null) return;

            var position = e.GetPosition(_graphCanvas);

            if (_isDragging && _draggedNode != null)
            {
                // Update dragged node position
                _draggedNode.Position = position;
                UpdateVisualNode(_draggedNode);
            }
            else if (_isConnecting && _sourceNode != null)
            {
                // Update connection preview
                UpdateConnectionPreview(position);
            }

            _lastMousePosition = position;
        }

        private void OnCanvasPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (_isDragging)
            {
                // Complete node drag
                CompleteNodeDrag();
            }

            e.Handled = true;
        }

        #endregion

        #region Node Management

        private VisualNode? FindNodeAtPosition(Point position)
        {
            return _visualNodes.FirstOrDefault(n => 
                position.X >= n.Position.X && position.X <= n.Position.X + n.Width &&
                position.Y >= n.Position.Y && position.Y <= n.Position.Y + n.Height);
        }

        private void StartDraggingNode(VisualNode node, Point position)
        {
            _draggedNode = node;
            _isDragging = true;
            node.IsSelected = true;
            UpdateVisualNode(node);
        }

        private void CompleteNodeDrag()
        {
            if (_draggedNode != null)
            {
                // Snap to grid
                _draggedNode.Position = SnapToGrid(_draggedNode.Position);
                UpdateVisualNode(_draggedNode);
                
                // Update graph model
                UpdateGraphModel();
            }

            _isDragging = false;
            _draggedNode = null;
        }

        private Point SnapToGrid(Point position)
        {
            const double gridSize = 20;
            return new Point(
                Math.Round(position.X / gridSize) * gridSize,
                Math.Round(position.Y / gridSize) * gridSize
            );
        }

        private void UpdateVisualNode(VisualNode node)
        {
            // Update the visual representation of the node
            if (node.VisualElement != null)
            {
                Canvas.SetLeft(node.VisualElement, node.Position.X);
                Canvas.SetTop(node.VisualElement, node.Position.Y);
            }
        }

        #endregion

        #region Connection Management

        private VisualConnection? FindConnectionAtPosition(Point position)
        {
            // Simple distance check - in a real implementation, you'd want more sophisticated hit testing
            return _visualConnections.FirstOrDefault(c => 
                IsPointNearLine(position, c.StartPoint, c.EndPoint, 5));
        }

        private bool IsPointNearLine(Point point, Point lineStart, Point lineEnd, double threshold)
        {
            var A = point.X - lineStart.X;
            var B = point.Y - lineStart.Y;
            var C = lineEnd.X - lineStart.X;
            var D = lineEnd.Y - lineStart.Y;

            var dot = A * C + B * D;
            var lenSq = C * C + D * D;
            var param = dot / lenSq;

            double xx, yy;
            if (param < 0)
            {
                xx = lineStart.X;
                yy = lineStart.Y;
            }
            else if (param > 1)
            {
                xx = lineEnd.X;
                yy = lineEnd.Y;
            }
            else
            {
                xx = lineStart.X + param * C;
                yy = lineStart.Y + param * D;
            }

            var dx = point.X - xx;
            var dy = point.Y - yy;
            var distance = Math.Sqrt(dx * dx + dy * dy);

            return distance <= threshold;
        }

        private void StartConnection(VisualNode sourceNode, Point startPoint)
        {
            _sourceNode = sourceNode;
            _isConnecting = true;
            _connectionStartPoint = startPoint;
            
            // Create connection preview
            CreateConnectionPreview(startPoint, startPoint); // Initial preview is just a point
        }

        private void CompleteConnection(VisualNode targetNode)
        {
            if (_sourceNode != null && _isConnecting)
            {
                // Create the actual connection
                var connection = new VisualConnection(_sourceNode, targetNode);
                _visualConnections.Add(connection);
                
                // Add to canvas
                AddConnectionToCanvas(connection);
                
                // Update graph model
                UpdateGraphModel();
                
                // Clean up
                RemoveConnectionPreview();
                _isConnecting = false;
                _sourceNode = null;
            }
        }

        private void AddConnectionToCanvas(VisualConnection connection)
        {
            if (_graphCanvas == null) return;

            var line = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = connection.StartPoint,
                EndPoint = connection.EndPoint,
                Stroke = new SolidColorBrush(Colors.Blue),
                StrokeThickness = 2
            };

            connection.VisualElement = line;
            _graphCanvas.Children.Add(line);
        }

        #endregion

        #region Selection Management

        private void SelectNode(VisualNode node)
        {
            DeselectAll();
            node.IsSelected = true;
            UpdateVisualNode(node);
            
            // Update view model
            if (DataContext is EffectsGraphEditorViewModel vm)
            {
                vm.SelectNode(node.Node);
            }
        }

        private void SelectConnection(VisualConnection connection)
        {
            DeselectAll();
            connection.IsSelected = true;
            UpdateVisualConnection(connection);
        }

        private void DeselectAll()
        {
            foreach (var node in _visualNodes)
            {
                node.IsSelected = false;
                UpdateVisualNode(node);
            }
            
            foreach (var connection in _visualConnections)
            {
                connection.IsSelected = false;
                UpdateVisualConnection(connection);
            }
        }

        private void UpdateVisualConnection(VisualConnection connection)
        {
            if (connection.VisualElement is Avalonia.Controls.Shapes.Line line)
            {
                line.Stroke = connection.IsSelected ? 
                    new SolidColorBrush(Colors.Red) : 
                    new SolidColorBrush(Colors.Blue);
                line.StrokeThickness = connection.IsSelected ? 3 : 2;
            }
        }

        #endregion

        #region Context Menu

        private void ShowContextMenu(Point position)
        {
            var contextMenu = new ContextMenu();
            
            var addNodeItem = new MenuItem { Header = "Add Node" };
            addNodeItem.Click += (s, e) => ShowAddNodeDialog(position);
            
            var deleteItem = new MenuItem { Header = "Delete Selected" };
            deleteItem.Click += (s, e) => DeleteSelected();
            
            var duplicateItem = new MenuItem { Header = "Duplicate Selected" };
            duplicateItem.Click += (s, e) => DuplicateSelected();
            
            contextMenu.Items.Add(addNodeItem);
            contextMenu.Items.Add(deleteItem);
            contextMenu.Items.Add(duplicateItem);
            
            contextMenu.Open();
        }

        private void ShowAddNodeDialog(Point position)
        {
            // Show dialog to select node type
            // This would integrate with the node palette
        }

        private void DeleteSelected()
        {
            var selectedNodes = _visualNodes.Where(n => n.IsSelected).ToList();
            var selectedConnections = _visualConnections.Where(c => c.IsSelected).ToList();
            
            foreach (var node in selectedNodes)
            {
                RemoveNode(node);
            }
            
            foreach (var connection in selectedConnections)
            {
                RemoveConnection(connection);
            }
            
            UpdateGraphModel();
        }

        private void DuplicateSelected()
        {
            var selectedNodes = _visualNodes.Where(n => n.IsSelected).ToList();
            
            foreach (var node in selectedNodes)
            {
                var duplicate = node.Clone();
                duplicate.Position = new Point(
                    node.Position.X + 50,
                    node.Position.Y + 50
                );
                
                AddNodeToCanvas(duplicate);
                _visualNodes.Add(duplicate);
            }
            
            UpdateGraphModel();
        }

        #endregion

        #region Canvas Management

        private void AddNodeToCanvas(VisualNode node)
        {
            if (_graphCanvas == null) return;

            // Create visual representation
            var border = new Border
            {
                Width = node.Width,
                Height = node.Height,
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Colors.Black),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(5)
            };
            
            var stackPanel = new StackPanel { Margin = new Thickness(5) };
            
            var nameText = new TextBlock
            {
                Text = node.Node.Name,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            var descText = new TextBlock
            {
                Text = node.Node.Description,
                FontSize = 10,
                Foreground = new SolidColorBrush(Colors.Gray),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            
            stackPanel.Children.Add(nameText);
            stackPanel.Children.Add(descText);
            border.Child = stackPanel;
            
            // Position on canvas
            Canvas.SetLeft(border, node.Position.X);
            Canvas.SetTop(border, node.Position.Y);
            
            // Add to canvas
            _graphCanvas.Children.Add(border);
            node.VisualElement = border;
        }

        private void RemoveNode(VisualNode node)
        {
            if (_graphCanvas != null && node.VisualElement != null)
            {
                _graphCanvas.Children.Remove(node.VisualElement);
            }
            
            // Remove associated connections
            var connectionsToRemove = _visualConnections
                .Where(c => c.SourceNode == node || c.TargetNode == node)
                .ToList();
            
            foreach (var connection in connectionsToRemove)
            {
                RemoveConnection(connection);
            }
            
            _visualNodes.Remove(node);
        }

        private void RemoveConnection(VisualConnection connection)
        {
            if (_graphCanvas != null && connection.VisualElement != null)
            {
                _graphCanvas.Children.Remove(connection.VisualElement);
            }
            
            _visualConnections.Remove(connection);
        }

        #endregion

        #region Graph Model Integration

        private void UpdateGraphModel()
        {
            // Update the underlying EffectsGraph model
            if (DataContext is EffectsGraphEditorViewModel vm)
            {
                vm.UpdateGraphFromVisual(_visualNodes, _visualConnections);
            }
        }

        #endregion
    }
}