using Avalonia;
using Avalonia.Controls;
using SkiaSharp;
using System;
using System.Diagnostics;
using PhoenixVisualizer.Core;

namespace PhoenixVisualizer.Rendering
{
    public sealed class PhxPreviewRenderer
    {
        private readonly Canvas _canvas;
        private readonly PhxPreviewViewModel _vm;
        private readonly object _sync = new();
        private PhxEditorSettings _settings;

        public PhxPreviewRenderer(Canvas canvas, PhxPreviewViewModel vm)
        {
            _canvas = canvas;
            _vm = vm;
            _settings = vm.Settings;
            HookRenderLoop();
        }

        public void ApplySettings(PhxEditorSettings settings)
        {
            lock (_sync) _settings = settings;
        }

        private void HookRenderLoop()
        {
            _canvas.AttachedToVisualTree += (_, __) => Start();
            _canvas.DetachedFromVisualTree += (_, __) => Stop();
        }

        private void Start() { /* start timer/renderer */ }
        private void Stop() { /* stop timer/renderer */ }

        private void Render(SKCanvas sk, Size size, float t, float dt)
        {
            PhxEditorSettings s; lock (_sync) s = _settings;
            var bg = new SKColor(s.BackgroundColor.R, s.BackgroundColor.G, s.BackgroundColor.B, s.BackgroundColor.A);
            if (s.ClearEveryFrame) sk.Clear(bg);
            // TODO: if !ClearEveryFrame, rely on visualizer draw style (accumulation trails)
            // PixelDoubling: render to smaller RT and scale with nearest
            // VSync handled by swapchain setup (see platform-specific)
            // ShowFps: overlay after drawing
            // draw active visualizer frame here...
        }
    }
}
