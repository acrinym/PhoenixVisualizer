using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhoenixVisualizer.Core.Services
{
    /// <summary>
    /// Interface for rendering AVS visualizations
    /// </summary>
    public interface IAvsRenderer : IDisposable
    {
        /// <summary>
        /// Initializes the renderer with preset variables
        /// </summary>
        Task InitializeAsync(Dictionary<string, object> variables);
        
        /// <summary>
        /// Renders a single frame with the current state
        /// </summary>
        Task<object> RenderFrameAsync(Dictionary<string, object> variables, Dictionary<string, object> audioData);
        
        /// <summary>
        /// Clears the current frame
        /// </summary>
        Task ClearFrameAsync(string? color = null);
        
        /// <summary>
        /// Sets the blend mode for rendering
        /// </summary>
        Task SetBlendModeAsync(string mode, float opacity);
        
        /// <summary>
        /// Sets the current transform (position, rotation, scale)
        /// </summary>
        Task SetTransformAsync(float x, float y, float rotation, float scale);
        
        /// <summary>
        /// Sets the current color for rendering
        /// </summary>
        Task SetColorAsync(float red, float green, float blue, float alpha);
        
        /// <summary>
        /// Draws a line between two points
        /// </summary>
        Task DrawLineAsync(float x1, float y1, float x2, float y2, float thickness = 1.0f);
        
        /// <summary>
        /// Draws a circle at the specified position
        /// </summary>
        Task DrawCircleAsync(float x, float y, float radius, bool filled = true);
        
        /// <summary>
        /// Draws a rectangle at the specified position
        /// </summary>
        Task DrawRectangleAsync(float x, float y, float width, float height, bool filled = true);
        
        /// <summary>
        /// Draws text at the specified position
        /// </summary>
        Task DrawTextAsync(string text, float x, float y, float fontSize = 12.0f);
        
        /// <summary>
        /// Gets the current render target (for integration with main window)
        /// </summary>
        object GetRenderTarget();
        
        /// <summary>
        /// Sets the render target (for integration with main window)
        /// </summary>
        Task SetRenderTargetAsync(object renderTarget);
        
        /// <summary>
        /// Gets the current frame buffer as an image
        /// </summary>
        Task<object> GetFrameBufferAsync();
        
        /// <summary>
        /// Takes a screenshot of the current frame
        /// </summary>
        Task<object> TakeScreenshotAsync();
    }
}
