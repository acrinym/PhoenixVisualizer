using Avalonia.Media;

namespace PhoenixVisualizer.Rendering
{
    public class PhxPreviewViewModel
    {
        public PhxEditorSettings Settings { get; set; } = new();
    }

    public class PhxEditorSettings
    {
        public Color BackgroundColor { get; set; } = Colors.Black;
        public bool ClearEveryFrame { get; set; } = true;
    }
}
