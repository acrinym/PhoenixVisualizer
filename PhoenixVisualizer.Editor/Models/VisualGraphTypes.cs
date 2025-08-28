using Avalonia;
using Avalonia.Controls;
using PhoenixVisualizer.Core.Effects.Interfaces;

namespace PhoenixVisualizer.Editor.Models
{
    /// <summary>
    /// Represents a visual node in the effects graph editor
    /// </summary>
    public class VisualNode
    {
        public IEffectNode Node { get; set; }
        public Point Position { get; set; }
        public double Width { get; set; } = 120;
        public double Height { get; set; } = 80;
        public bool IsSelected { get; set; }
        public Control? VisualElement { get; set; }

        public VisualNode(IEffectNode node)
        {
            Node = node;
            Position = new Point(100, 100);
        }

        public VisualNode Clone()
        {
            return new VisualNode(Node)
            {
                Position = Position,
                Width = Width,
                Height = Height
            };
        }
    }

    /// <summary>
    /// Represents a visual connection between nodes in the effects graph editor
    /// </summary>
    public class VisualConnection
    {
        public VisualNode SourceNode { get; set; }
        public VisualNode TargetNode { get; set; }
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
        public bool IsSelected { get; set; }
        public Control? VisualElement { get; set; }

        public VisualConnection(VisualNode source, VisualNode target)
        {
            SourceNode = source;
            TargetNode = target;
            StartPoint = new Point(
                source.Position.X + source.Width / 2,
                source.Position.Y + source.Height / 2
            );
            EndPoint = new Point(
                target.Position.X + target.Width / 2,
                target.Position.Y + target.Height / 2
            );
        }
    }
}
