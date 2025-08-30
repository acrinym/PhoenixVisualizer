namespace PhoenixVisualizer.Core.Interfaces;

/// <summary>
/// Canvas interface for drawing operations
/// </summary>
public interface ISkiaCanvas
{
    int Width { get; }
    int Height { get; }
    
    // Basic drawing methods using simple types
    void Clear(uint color);
    void DrawLine(float x1, float y1, float x2, float y2, uint color, float thickness = 1.0f);
    void FillRectangle(float x, float y, float width, float height, uint color);
    void DrawCircle(float x, float y, float radius, uint color, bool filled = false);
    void FillCircle(float x, float y, float radius, uint color);
    void DrawRect(float x, float y, float width, float height, uint color, bool filled = false);
    void DrawPoint(float x, float y, uint color, float size = 1.0f);
}
