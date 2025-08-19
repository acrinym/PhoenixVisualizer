using Avalonia.Media.Imaging;

using PhoenixVisualizer.Core.Services;

namespace PhoenixVisualizer.Rendering
{
    public sealed class AvaloniaAvsRenderer : IAvsRenderer
    {
        private Canvas? _canvas;
        private RenderTargetBitmap? _rt;
        private readonly List<RenderCommand> _commands = new();
        private bool _disposed;

        // Current state
        private float _r = 1, _g = 1, _b = 1, _a = 1;

        public Task InitializeAsync(Dictionary<string, object> variables)
        {
            return Task.CompletedTask;
        }

        public async Task<object> RenderFrameAsync(Dictionary<string, object> variables, Dictionary<string, object> audioData)
        {
            if (_canvas == null)
            {
                return new { success = false, message = "No canvas" };
            }

            EnsureRenderTarget();
            if (_rt == null) return new { success = false, message = "No render target" };

            using (var dc = _rt.CreateDrawingContext())
            {
                foreach (var cmd in _commands)
                {
                    switch (cmd.Type)
                    {
                        case RenderCommandType.Clear:
                            dc.FillRectangle(new SolidColorBrush(Colors.Black), new Rect(new Point(0, 0), _rt.Size));
                            break;
                        case RenderCommandType.SetColor:
                            _r = cmd.Red; _g = cmd.Green; _b = cmd.Blue; _a = cmd.Alpha;
                            break;
                        case RenderCommandType.DrawLine:
                            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(
                                (byte)(_a * 255), (byte)(_r * 255), (byte)(_g * 255), (byte)(_b * 255))), cmd.Thickness),
                                new Point(cmd.X1, cmd.Y1), new Point(cmd.X2, cmd.Y2));
                            break;
                        case RenderCommandType.DrawCircle:
                            var brush = new SolidColorBrush(Color.FromArgb(
                                (byte)(_a * 255), (byte)(_r * 255), (byte)(_g * 255), (byte)(_b * 255)));
                            var pen = cmd.Filled ? null : new Pen(brush, 1);
                            dc.DrawEllipse(cmd.Filled ? brush : null, pen, new Point(cmd.X, cmd.Y), cmd.Radius, cmd.Radius);
                            break;
                        case RenderCommandType.DrawRectangle:
                            var rectBrush = new SolidColorBrush(Color.FromArgb(
                                (byte)(_a * 255), (byte)(_r * 255), (byte)(_g * 255), (byte)(_b * 255)));
                            var rectPen = cmd.Filled ? null : new Pen(rectBrush, 1);
                            dc.DrawRectangle(cmd.Filled ? rectBrush : null, rectPen, new Rect(cmd.X, cmd.Y, cmd.Width, cmd.Height));
                            break;
                        case RenderCommandType.DrawText:
                            var textBrush = new SolidColorBrush(Color.FromArgb(
                                (byte)(_a * 255), (byte)(_r * 255), (byte)(_g * 255), (byte)(_b * 255)));
                            var ft = new FormattedText(cmd.Text ?? string.Empty, System.Globalization.CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight, new Typeface("Arial"), cmd.FontSize, textBrush);
                            dc.DrawText(ft, new Point(cmd.X, cmd.Y));
                            break;
                    }
                }
            }

            // Present: replace canvas content with Image of RT
            _canvas.Children.Clear();
            _canvas.Children.Add(new Image { Source = _rt, Width = _canvas.Bounds.Width, Height = _canvas.Bounds.Height });

            var result = new { success = true, commands = _commands.Count };
            _commands.Clear();
            return await Task.FromResult(result);
        }

        public Task ClearFrameAsync(string? color = null)
        {
            _commands.Add(new RenderCommand { Type = RenderCommandType.Clear, Color = color });
            return Task.CompletedTask;
        }

        public Task SetBlendModeAsync(string mode, float opacity)
        {
            // Not yet used in this minimal implementation
            return Task.CompletedTask;
        }

        public Task SetTransformAsync(float x, float y, float rotation, float scale)
        {
            // Not applied in this minimal implementation
            return Task.CompletedTask;
        }

        public Task SetColorAsync(float red, float green, float blue, float alpha)
        {
            _commands.Add(new RenderCommand { Type = RenderCommandType.SetColor, Red = red, Green = green, Blue = blue, Alpha = alpha });
            return Task.CompletedTask;
        }

        public Task DrawLineAsync(float x1, float y1, float x2, float y2, float thickness = 1.0f)
        {
            _commands.Add(new RenderCommand { Type = RenderCommandType.DrawLine, X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Thickness = thickness });
            return Task.CompletedTask;
        }

        public Task DrawCircleAsync(float x, float y, float radius, bool filled = true)
        {
            _commands.Add(new RenderCommand { Type = RenderCommandType.DrawCircle, X = x, Y = y, Radius = radius, Filled = filled });
            return Task.CompletedTask;
        }

        public Task DrawRectangleAsync(float x, float y, float width, float height, bool filled = true)
        {
            _commands.Add(new RenderCommand { Type = RenderCommandType.DrawRectangle, X = x, Y = y, Width = width, Height = height, Filled = filled });
            return Task.CompletedTask;
        }

        public Task DrawTextAsync(string text, float x, float y, float fontSize = 12.0f)
        {
            _commands.Add(new RenderCommand { Type = RenderCommandType.DrawText, Text = text, X = x, Y = y, FontSize = fontSize });
            return Task.CompletedTask;
        }

        public object GetRenderTarget() => _rt ?? (object)new();

        public Task SetRenderTargetAsync(object renderTarget)
        {
            // Not used; we bind via SetRenderCanvas
            return Task.CompletedTask;
        }

        public Task<object> GetFrameBufferAsync()
        {
            return Task.FromResult<object>(_rt ?? new object());
        }

        public Task<object> TakeScreenshotAsync()
        {
            return Task.FromResult<object>(new { success = _rt != null });
        }

        public void SetRenderCanvas(Canvas canvas)
        {
            _canvas = canvas;
            EnsureRenderTarget();
        }

        private void EnsureRenderTarget()
        {
            if (_canvas == null) return;
            var size = new PixelSize(Math.Max(1, (int)_canvas.Bounds.Width), Math.Max(1, (int)_canvas.Bounds.Height));
            if (_rt == null || !_rt.Size.Equals(size))
            {
                _rt = new RenderTargetBitmap(size);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _rt = null;
            _commands.Clear();
            _canvas = null;
        }
    }
}


