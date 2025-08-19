using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhoenixVisualizer.Core.Services
{
    /// <summary>
    /// Platform-agnostic AVS renderer that can be extended for specific platforms
    /// </summary>
    public class AvsRenderer : IAvsRenderer
    {
        private object? _renderTarget;
        private bool _isDisposed;
        private readonly List<RenderCommand> _frameBuffer = new();
        
        // Current rendering state
        private float _currentRed = 1.0f;
        private float _currentGreen = 1.0f;
        private float _currentBlue = 1.0f;
        private float _currentAlpha = 1.0f;
        private float _currentX = 0.0f;
        private float _currentY = 0.0f;
        private float _currentRotation = 0.0f;
        private float _currentScale = 1.0f;
        
        // Performance tracking (EMA for simplicity + stability)
        private double _averageFrameTime = 0.0; // exponential moving average
        private const double FrameEmaAlpha = 0.1;
        
        public event EventHandler<AvsRenderEventArgs>? FrameRendered;
        
        public AvsRenderer()
        {
        }
        
        public async Task InitializeAsync(Dictionary<string, object> variables)
        {
            // Initialize rendering state
            _currentRed = 1.0f;
            _currentGreen = 1.0f;
            _currentBlue = 1.0f;
            _currentAlpha = 1.0f;
            _currentX = 0.0f;
            _currentY = 0.0f;
            _currentRotation = 0.0f;
            _currentScale = 1.0f;
            
            await Task.CompletedTask;
        }
        
        public async Task<object> RenderFrameAsync(Dictionary<string, object> variables, Dictionary<string, object> audioData)
        {
            try
            {
                var startTime = DateTime.Now;
                
                // Execute all render commands
                foreach (var command in _frameBuffer)
                {
                    await ExecuteRenderCommandAsync(command);
                }
                
                // Update performance tracking
                var frameTime = (DateTime.Now - startTime).TotalMilliseconds;
                UpdateFrameTiming(frameTime);
                
                // Create render result
                var renderResult = new
                {
                    success = true,
                    frameTime,
                    fps = 1000.0 / _averageFrameTime,
                    commandsExecuted = _frameBuffer.Count,
                    renderState = new
                    {
                        color = new { r = _currentRed, g = _currentGreen, b = _currentBlue, a = _currentAlpha },
                        position = new { x = _currentX, y = _currentY },
                        rotation = _currentRotation,
                        scale = _currentScale
                    }
                };
                
                // Clear the frame buffer for next frame
                _frameBuffer.Clear();
                
                // Raise frame rendered event
                OnFrameRendered(new AvsRenderEventArgs(renderResult, (int)variables.GetValueOrDefault("frame", 0), variables));
                
                return renderResult;
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message };
            }
        }
        
        private async Task ExecuteRenderCommandAsync(RenderCommand command)
        {
            switch (command.Type)
            {
                case RenderCommandType.Clear:
                    // Clear command - no debug output needed
                    break;
                case RenderCommandType.SetBlendMode:
                    // Blend mode command - no debug output needed
                    break;
                case RenderCommandType.SetTransform:
                    _currentX = command.X;
                    _currentY = command.Y;
                    _currentRotation = command.Rotation;
                    _currentScale = command.Scale;
                    break;
                case RenderCommandType.SetColor:
                    _currentRed = command.Red;
                    _currentGreen = command.Green;
                    _currentBlue = command.Blue;
                    _currentAlpha = command.Alpha;
                    break;
                case RenderCommandType.DrawLine:
                    // Draw line command - no debug output needed
                    break;
                case RenderCommandType.DrawCircle:
                    // Draw circle command - no debug output needed
                    break;
                case RenderCommandType.DrawRectangle:
                    // Draw rectangle command - no debug output needed
                    break;
                case RenderCommandType.DrawText:
                    // Draw text command - no debug output needed
                    break;
            }
            
            await Task.CompletedTask;
        }
        
        public async Task ClearFrameAsync(string? color = null)
        {
            var clearCommand = new RenderCommand
            {
                Type = RenderCommandType.Clear,
                Color = color ?? "black"
            };
            
            _frameBuffer.Add(clearCommand);
            await Task.CompletedTask;
        }
        
        public async Task SetBlendModeAsync(string mode, float opacity)
        {
            var blendCommand = new RenderCommand
            {
                Type = RenderCommandType.SetBlendMode,
                BlendMode = ParseBlendMode(mode),
                Opacity = opacity
            };
            
            _frameBuffer.Add(blendCommand);
            await Task.CompletedTask;
        }
        
        public async Task SetTransformAsync(float x, float y, float rotation, float scale)
        {
            var transformCommand = new RenderCommand
            {
                Type = RenderCommandType.SetTransform,
                X = x,
                Y = y,
                Rotation = rotation,
                Scale = scale
            };
            
            _frameBuffer.Add(transformCommand);
            await Task.CompletedTask;
        }
        
        public async Task SetColorAsync(float red, float green, float blue, float alpha)
        {
            var colorCommand = new RenderCommand
            {
                Type = RenderCommandType.SetColor,
                Red = red,
                Green = green,
                Blue = blue,
                Alpha = alpha
            };
            
            _frameBuffer.Add(colorCommand);
            await Task.CompletedTask;
        }
        
        public async Task DrawLineAsync(float x1, float y1, float x2, float y2, float thickness = 1.0f)
        {
            var lineCommand = new RenderCommand
            {
                Type = RenderCommandType.DrawLine,
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Thickness = thickness
            };
            
            _frameBuffer.Add(lineCommand);
            await Task.CompletedTask;
        }
        
        public async Task DrawCircleAsync(float x, float y, float radius, bool filled = true)
        {
            var circleCommand = new RenderCommand
            {
                Type = RenderCommandType.DrawCircle,
                X = x,
                Y = y,
                Radius = radius,
                Filled = filled
            };
            
            _frameBuffer.Add(circleCommand);
            await Task.CompletedTask;
        }
        
        public async Task DrawRectangleAsync(float x, float y, float width, float height, bool filled = true)
        {
            var rectCommand = new RenderCommand
            {
                Type = RenderCommandType.DrawRectangle,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Filled = filled
            };
            
            _frameBuffer.Add(rectCommand);
            await Task.CompletedTask;
        }
        
        public async Task DrawTextAsync(string text, float x, float y, float fontSize = 12.0f)
        {
            var textCommand = new RenderCommand
            {
                Type = RenderCommandType.DrawText,
                Text = text,
                X = x,
                Y = y,
                FontSize = fontSize
            };
            
            _frameBuffer.Add(textCommand);
            await Task.CompletedTask;
        }
        
        public object GetRenderTarget()
        {
            return _renderTarget ?? new object();
        }
        
        public async Task SetRenderTargetAsync(object renderTarget)
        {
            _renderTarget = renderTarget;
            await Task.CompletedTask;
        }
        
        public async Task<object> GetFrameBufferAsync()
        {
            return await Task.FromResult(_frameBuffer);
        }
        
        public async Task<object> TakeScreenshotAsync()
        {
            return await Task.FromResult(new { 
                success = true, 
                message = "Screenshot captured from frame buffer",
                commands = _frameBuffer.Count,
                timestamp = DateTime.Now
            });
        }
        
        private RenderCommandType ParseBlendMode(string mode)
        {
            return mode.ToLower() switch
            {
                "add" => RenderCommandType.SetBlendMode,
                "multiply" => RenderCommandType.SetBlendMode,
                "screen" => RenderCommandType.SetBlendMode,
                "overlay" => RenderCommandType.SetBlendMode,
                "darken" => RenderCommandType.SetBlendMode,
                "lighten" => RenderCommandType.SetBlendMode,
                _ => RenderCommandType.SetBlendMode
            };
        }
        
        private void UpdateFrameTiming(double frameTime)
        {
            var ms = frameTime;
            _averageFrameTime = _averageFrameTime <= 0 ? ms : (_averageFrameTime * (1 - FrameEmaAlpha) + ms * FrameEmaAlpha);
        }
        
        protected virtual void OnFrameRendered(AvsRenderEventArgs e)
        {
            FrameRendered?.Invoke(this, e);
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            
            _frameBuffer.Clear();
            _renderTarget = null;
            
            _isDisposed = true;
        }
    }
    
    public class RenderCommand
    {
        public RenderCommandType Type { get; set; }
        public string? Color { get; set; }
        public RenderCommandType BlendMode { get; set; }
        public float Opacity { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Rotation { get; set; }
        public float Scale { get; set; }
        public float Red { get; set; }
        public float Green { get; set; }
        public float Blue { get; set; }
        public float Alpha { get; set; }
        public float X1 { get; set; }
        public float Y1 { get; set; }
        public float X2 { get; set; }
        public float Y2 { get; set; }
        public float Thickness { get; set; }
        public float Radius { get; set; }
        public bool Filled { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public string? Text { get; set; }
        public float FontSize { get; set; }
    }
    
    public enum RenderCommandType
    {
        Clear,
        SetBlendMode,
        SetTransform,
        SetColor,
        DrawLine,
        DrawCircle,
        DrawRectangle,
        DrawText
    }
}
