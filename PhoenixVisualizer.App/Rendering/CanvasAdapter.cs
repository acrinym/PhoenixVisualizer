using Avalonia.Media;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Rendering;

public sealed class CanvasAdapter : ISkiaCanvas
{
	private readonly DrawingContext _context;

	public CanvasAdapter(DrawingContext context)
	{
		_context = context;
	}

	public void Clear(uint argb)
	{
		var color = Color.FromUInt32(argb);
		_context.FillRectangle(new SolidColorBrush(color), new Avalonia.Rect(0, 0, _context.Bounds.Width, _context.Bounds.Height));
	}

	public void DrawLines(ReadOnlySpan<(float x, float y)> points, float thickness, uint argb)
	{
		if (points.Length < 2) return;
		var geometry = new StreamGeometry();
		using (var ctx = geometry.Open())
		{
			ctx.BeginFigure(new Avalonia.Point(points[0].x, points[0].y), false);
			for (int i = 1; i < points.Length; i++)
			{
				ctx.LineTo(new Avalonia.Point(points[i].x, points[i].y));
			}
			ctx.EndFigure(false);
		}
		var pen = new Pen(new SolidColorBrush(Color.FromUInt32(argb)), thickness);
		_context.DrawGeometry(null, pen, geometry);
	}

	public void FillCircle(float cx, float cy, float radius, uint argb)
	{
		var brush = new SolidColorBrush(Color.FromUInt32(argb));
		_context.FillEllipse(brush, new Avalonia.Point(cx, cy), radius, radius);
	}
}


