using PhoenixVisualizer.Core.Interfaces;

namespace PhoenixVisualizer.Core.Nodes;

/// <summary>
/// Simple text rendering node for Node visualizers
/// </summary>
public sealed class TextNode : IEffectNode
{
    public string Name => "Text";
    public Dictionary<string, EffectParam> Params { get; } = new()
    {
        ["Content"] = new EffectParam { Label = "Text Content", Type = "text", StringValue = "PHOENIX" },
        ["Size"] = new EffectParam { Label = "Font Size", Type = "slider", FloatValue = 48f, Min = 12f, Max = 128f },
        ["Color"] = new EffectParam { Label = "Text Color", Type = "color", ColorValue = "#FFFFFF" }
    };

    public void Render(float[] waveform, float[] spectrum, RenderContext ctx)
    {
        try
        {
            // Get text content and size from parameters
            var content = Params["Content"].StringValue ?? "PHOENIX";
            var size = Params["Size"].FloatValue;
            
            // Convert color from hex string to uint
            var colorHex = Params["Color"].ColorValue ?? "#FFFFFF";
            var color = ConvertHexToUint(colorHex);
            
            // Since ISkiaCanvas doesn't have DrawText, draw rectangles to represent text
            DrawTextAsRectangles(ctx.Canvas, content, ctx.Width * 0.5f - 100, ctx.Height * 0.5f, color, size);
        }
        catch
        {
            // Fallback: draw simple rectangles
            DrawTextAsRectangles(ctx.Canvas, "TEXT", ctx.Width * 0.5f - 50, ctx.Height * 0.5f, 0xFFFFFFFF, 48f);
        }
    }
    
    private static uint ConvertHexToUint(string hex)
    {
        if (hex.StartsWith("#")) hex = hex[1..];
        if (hex.Length == 6) hex = "FF" + hex; // Add alpha if missing
        return Convert.ToUInt32(hex, 16);
    }
    
    private static void DrawTextAsRectangles(ISkiaCanvas? canvas, string text, float x, float y, uint color, float size)
    {
        if (canvas == null) return;
        
        // Draw simple rectangles to represent each character
        var charWidth = size * 0.6f;
        var charHeight = size;
        
        for (int i = 0; i < text.Length; i++)
        {
            var charX = x + i * charWidth;
            canvas.FillRect(charX, y, charWidth * 0.8f, charHeight, color);
        }
    }
}
