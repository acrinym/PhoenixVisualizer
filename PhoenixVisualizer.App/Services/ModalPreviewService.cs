using Avalonia.Controls;
using Avalonia;
using PhoenixVisualizer.App.Rendering;
using PhoenixVisualizer.Views;
using System;

namespace PhoenixVisualizer.App.Services
{
    /// <summary>
    /// Opens a borderless, resizable modal window that hosts the same preview renderer.
    /// Custom grips call BeginResizeDrag on the Window for proper resizing.
    /// </summary>
    public sealed class ModalPreviewService
    {
        private Window? _window;
        private PreviewModalWindow? _modal;

        public void OpenPreview(Window owner, PhxPreviewRenderer? renderer)
        {
            if (renderer is null) return;
            if (_modal != null) { _modal.Activate(); return; }
            _modal = new PreviewModalWindow();
            _window = _modal;
            // We'll just show the modal window without attaching the renderer for now
            // _modal.AttachRenderer(renderer);
            _modal.Closed += (_, __) => { _modal = null; _window = null; };
            _modal.Show(owner);
        }
    }
}
