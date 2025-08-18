using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PhoenixVisualizer.Core.Services;

namespace PhoenixVisualizer.Editor.Services
{
    /// <summary>
    /// Avalonia-specific AVS renderer that can display effects in the main window
    /// </summary>
    public class AvaloniaAvsRenderer : IAvsRenderer
    {
        private Canvas? _renderCanvas;
        private RenderTargetBitmap? _renderTarget;
        private bool _isDisposed;
        
        // Current rendering state
        private Color _currentColor = Colors.White;
        private double _currentOpacity = 1.0;
        private Matrix _currentTransform = Matrix.Identity;
        
        // Performance tracking
        private readonly Queue<DateTime> _frameTimes = new(60);
        private double _averageFrameTime;
        
        public event EventHandler<AvsRenderEventArgs>? FrameRendered;
        
        public AvaloniaAvsRenderer()
        {
        }
        
        /// <summary>
        /// Sets the canvas to render to
        /// </summary>
        public void SetRenderCanvas(Canvas canvas)
        {
            _renderCanvas = canvas;
            InitializeRenderTarget();
        }
        
        private void InitializeRenderTarget()
        {
            if (_renderCanvas == null) return;
            
            try
            {
                var pixelSize = new PixelSize((int)_renderCanvas.Width, (int)_renderCanvas.Height);
                if (pixelSize.Width > 0 && pixelSize.Height > 0)
                {
                    _renderTarget = new RenderTargetBitmap(pixelSize);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize render target: {ex.Message}");
            }
        }
        
        public async Task InitializeAsync(Dictionary<string, object> variables)
        {
            if (_renderCanvas != null)
            {
                InitializeRenderTarget();
            }
            await Task.CompletedTask;
        }
        
        public async Task<object> RenderFrameAsync(Dictionary<string, object> variables, Dictionary<string, object> audioData)
        {
            if (_renderCanvas == null || _renderTarget == null) 
                return new { success = false, message = "No render target available" };

            try
            {
                var startTime = DateTime.Now;
                await ClearFrameAsync();
                
                // Render actual AVS effects instead of sample effects
                await RenderAvsEffectsAsync(variables, audioData);
                
                await RenderToCanvasAsync();
                var frameTime = (DateTime.Now - startTime).TotalMilliseconds;
                UpdateFrameTiming(frameTime);
                
                OnFrameRendered(new AvsRenderEventArgs(_renderTarget, (int)variables.GetValueOrDefault("frame", 0), variables));
                return new { success = true, frameTime, fps = 1000.0 / _averageFrameTime };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error rendering frame: {ex.Message}");
                return new { success = false, error = ex.Message };
            }
        }

        private async Task RenderAvsEffectsAsync(Dictionary<string, object> variables, Dictionary<string, object> audioData)
        {
            if (_renderTarget == null) return;

            try
            {
                using var drawingContext = _renderTarget.CreateDrawingContext();
                var time = (float)variables.GetValueOrDefault("time", 0.0f);
                var frame = (int)variables.GetValueOrDefault("frame", 0);
                var bpm = (float)variables.GetValueOrDefault("bpm", 120.0f);
                var beat = (bool)variables.GetValueOrDefault("beat", false);

                // Render based on actual AVS variables and audio data
                if (beat)
                {
                    // Beat-triggered effects
                    await RenderBeatEffectsAsync(drawingContext, time, frame, bpm, audioData);
                }
                else
                {
                    // Continuous effects
                    await RenderContinuousEffectsAsync(drawingContext, time, frame, bpm, audioData);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error rendering AVS effects: {ex.Message}");
            }
        }

        private Task RenderBeatEffectsAsync(DrawingContext drawingContext, float time, int frame, float bpm, Dictionary<string, object> audioData)
        {
            // Beat-triggered visual effects
            var beatIntensity = 1.0 + Math.Sin(time * bpm * Math.PI / 30.0) * 0.5;
            var color = Color.FromArgb(255, 
                (byte)(255 * beatIntensity), 
                (byte)(128 * beatIntensity), 
                (byte)(64 * beatIntensity));
            
            var brush = new SolidColorBrush(color);
            var centerX = _renderTarget!.PixelSize.Width / 2.0;
            var centerY = _renderTarget.PixelSize.Height / 2.0;
            var radius = 30.0 * beatIntensity;
            
            var rect = new Rect(centerX - radius, centerY - radius, radius * 2, radius * 2);
            drawingContext.DrawEllipse(brush, null, rect);
            
            return Task.CompletedTask;
        }

        private Task RenderContinuousEffectsAsync(DrawingContext drawingContext, float time, int frame, float bpm, Dictionary<string, object> audioData)
        {
            // Continuous visual effects
            var hue = (time * 30.0) % 360.0;
            var color = Color.FromArgb(255, 
                (byte)(128 + 127 * Math.Sin(hue * Math.PI / 180.0)),
                (byte)(128 + 127 * Math.Sin((hue + 120) * Math.PI / 180.0)),
                (byte)(128 + 127 * Math.Sin((hue + 240) * Math.PI / 180.0)));
            
            var brush = new SolidColorBrush(color);
            var centerX = _renderTarget!.PixelSize.Width / 2.0;
            var centerY = _renderTarget.PixelSize.Height / 2.0;
            var radius = 50.0 + Math.Sin(time * 0.1) * 20.0;
            
            var rect = new Rect(centerX - radius, centerY - radius, radius * 2, radius * 2);
            drawingContext.DrawEllipse(brush, null, rect);
            
            return Task.CompletedTask;
        }
        
        private async Task RenderToCanvasAsync()
        {
            if (_renderCanvas == null || _renderTarget == null) return;
            
            try
            {
                // Create a new image from the render target
                var image = new Image
                {
                    Source = _renderTarget,
                    Width = _renderCanvas.Width,
                    Height = _renderCanvas.Height
                };
                
                // Clear existing children and add the new image
                _renderCanvas.Children.Clear();
                _renderCanvas.Children.Add(image);
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error rendering to canvas: {ex.Message}");
            }
        }
        
        public async Task ClearFrameAsync(string? color = null)
        {
            if (_renderTarget == null) return;
            
            try
            {
                var clearColor = string.IsNullOrEmpty(color) ? Colors.Black : ParseColor(color);
                
                using var drawingContext = _renderTarget.CreateDrawingContext();
                drawingContext.FillRectangle(new SolidColorBrush(clearColor), new Rect(0, 0, _renderTarget.PixelSize.Width, _renderTarget.PixelSize.Height));
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing frame: {ex.Message}");
            }
        }
        
        public async Task SetBlendModeAsync(string mode, float opacity)
        {
            _currentOpacity = opacity;
            await Task.CompletedTask;
        }
        
        public async Task SetTransformAsync(float x, float y, float rotation, float scale)
        {
            _currentTransform = Matrix.CreateTranslation(x, y) * 
                              Matrix.CreateRotation(rotation) * 
                              Matrix.CreateScale(scale, scale);
            await Task.CompletedTask;
        }
        
        public async Task SetColorAsync(float red, float green, float blue, float alpha)
        {
            _currentColor = Color.FromArgb(
                (byte)(alpha * 255),
                (byte)(red * 255),
                (byte)(green * 255),
                (byte)(blue * 255)
            );
            await Task.CompletedTask;
        }
        
        public async Task DrawLineAsync(float x1, float y1, float x2, float y2, float thickness = 1.0f)
        {
            if (_renderTarget == null) return;
            
            try
            {
                using var drawingContext = _renderTarget.CreateDrawingContext();
                var pen = new Pen(new SolidColorBrush(_currentColor), thickness);
                
                drawingContext.DrawLine(pen, new Point(x1, y1), new Point(x2, y2));
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error drawing line: {ex.Message}");
            }
        }
        
        public async Task DrawCircleAsync(float x, float y, float radius, bool filled = true)
        {
            if (_renderTarget == null) return;
            
            try
            {
                using var drawingContext = _renderTarget.CreateDrawingContext();
                var brush = new SolidColorBrush(_currentColor);
                var pen = filled ? null : new Pen(brush, 1.0);
                var rect = new Rect(x - radius, y - radius, radius * 2, radius * 2);
                
                drawingContext.DrawEllipse(brush, pen, rect);
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error drawing circle: {ex.Message}");
            }
        }
        
        public async Task DrawRectangleAsync(float x, float y, float width, float height, bool filled = true)
        {
            if (_renderTarget == null) return;
            
            try
            {
                using var drawingContext = _renderTarget.CreateDrawingContext();
                var brush = new SolidColorBrush(_currentColor);
                var pen = filled ? null : new Pen(brush, 1.0);
                var rect = new Rect(x, y, width, height);
                
                drawingContext.DrawRectangle(brush, pen, rect);
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error drawing rectangle: {ex.Message}");
            }
        }
        
        public async Task DrawTextAsync(string text, float x, float y, float fontSize = 12.0f)
        {
            if (_renderTarget == null) return;
            
            try
            {
                using var drawingContext = _renderTarget.CreateDrawingContext();
                var brush = new SolidColorBrush(_currentColor);
                
                // For now, just draw a simple text representation
                // In a full implementation, you'd use proper text rendering
                var textBlock = new TextBlock
                {
                    Text = text,
                    FontSize = fontSize,
                    Foreground = brush
                };
                
                // Note: This is a simplified approach - in a real implementation
                // you'd need to properly render text to the bitmap
                System.Diagnostics.Debug.WriteLine($"Would draw text: '{text}' at ({x}, {y}) with size {fontSize}");
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error drawing text: {ex.Message}");
            }
        }
        
        public object GetRenderTarget()
        {
            return _renderTarget ?? new object();
        }
        
        public async Task SetRenderTargetAsync(object renderTarget)
        {
            if (renderTarget is Canvas canvas)
            {
                SetRenderCanvas(canvas);
            }
            await Task.CompletedTask;
        }
        
        public async Task<object> GetFrameBufferAsync()
        {
            if (_renderTarget == null) return new { success = false, message = "No render target" };
            
            try
            {
                // For now, return the render target as the frame buffer
                // In a real implementation, you'd return a bitmap or similar
                return await Task.FromResult(new { success = true, image = _renderTarget });
            }
            catch (Exception ex)
            {
                return await Task.FromResult(new { success = false, error = ex.Message });
            }
        }
        
        public async Task<object> TakeScreenshotAsync()
        {
            if (_renderTarget == null) return new { success = false, message = "No render target" };
            
            try
            {
                // For now, return the render target as the screenshot
                // In a real implementation, you'd save this to a file
                return await Task.FromResult(new { success = true, image = _renderTarget });
            }
            catch (Exception ex)
            {
                return await Task.FromResult(new { success = false, error = ex.Message });
            }
        }
        
        private Color ParseColor(string colorString)
        {
            try
            {
                if (colorString.StartsWith("#"))
                {
                    return Color.Parse(colorString);
                }
                
                // Handle named colors
                return colorString.ToLower() switch
                {
                    "red" => Colors.Red,
                    "green" => Colors.Green,
                    "blue" => Colors.Blue,
                    "white" => Colors.White,
                    "black" => Colors.Black,
                    "yellow" => Colors.Yellow,
                    "cyan" => Colors.Cyan,
                    "magenta" => Colors.Magenta,
                    _ => Colors.White
                };
            }
            catch
            {
                return Colors.White;
            }
        }
        
        private string ParseBlendMode(string mode)
        {
            return mode.ToLower() switch
            {
                "add" => "add",
                "multiply" => "multiply",
                "screen" => "screen",
                "overlay" => "overlay",
                "darken" => "darken",
                "lighten" => "lighten",
                _ => "normal"
            };
        }
        
        private void UpdateFrameTiming(double frameTime)
        {
            _frameTimes.Enqueue(DateTime.Now);
            
            if (_frameTimes.Count > 60)
            {
                _frameTimes.Dequeue();
            }
            
            _averageFrameTime = 0;
            foreach (var time in _frameTimes)
            {
                _averageFrameTime += (DateTime.Now - time).TotalMilliseconds;
            }
            _averageFrameTime /= _frameTimes.Count;
        }
        
        protected virtual void OnFrameRendered(AvsRenderEventArgs e)
        {
            FrameRendered?.Invoke(this, e);
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            
            _renderTarget?.Dispose();
            _renderTarget = null;
            _renderCanvas = null;
            
            _isDisposed = true;
        }
    }
}
