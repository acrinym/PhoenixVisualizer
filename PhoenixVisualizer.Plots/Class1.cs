using System;
using System.Collections.Generic;

namespace PhoenixVisualizer.Plots;

// Core plotting primitives for audio visualization
public sealed class LineSeries
{
	public float[] X { get; set; } = Array.Empty<float>();
	public float[] Y { get; set; } = Array.Empty<float>();
	public uint Color { get; set; } = 0xFFFF8800; // Orange
	public float Thickness { get; set; } = 2.0f;
}

public sealed class ScatterPlot
{
	public float[] X { get; set; } = Array.Empty<float>();
	public float[] Y { get; set; } = Array.Empty<float>();
	public uint Color { get; set; } = 0xFF44AAFF; // Blue
	public float PointSize { get; set; } = 4.0f;
}

public sealed class BarChart
{
	public float[] Values { get; set; } = Array.Empty<float>();
	public uint Color { get; set; } = 0xFF44AAFF; // Blue
	public float BarWidth { get; set; } = 2.0f;
	public float Spacing { get; set; } = 1.0f;
}

public sealed class PolarPlot
{
	public float[] Radii { get; set; } = Array.Empty<float>();
	public float[] Thetas { get; set; } = Array.Empty<float>();
	public uint Color { get; set; } = 0xFF44AAFF; // Blue
	public float Thickness { get; set; } = 2.0f;
}

// NEW: Matplotlib-inspired advanced plot types
public sealed class Heatmap
{
	public float[][] Data { get; set; } = Array.Empty<float[]>();
	public uint[] Colors { get; set; } = Array.Empty<uint>();
	public int Width { get; set; } = 0;
	public int Height { get; set; } = 0;
}

public sealed class SurfacePlot
{
	public float[] Data { get; set; } = Array.Empty<float>();
	public uint BaseColor { get; set; } = 0xFF44AAFF; // Blue
	public float HeightScale { get; set; } = 2.0f;
	public bool Wireframe { get; set; } = false;
}

// Audio-specific visualization helpers
public static class AudioPlots
{
	// Generate spectrum bars from FFT data
	public static BarChart CreateSpectrumBars(float[] fft, uint color = 0xFF44AAFF)
	{
		return new BarChart
		{
			Values = fft,
			Color = color,
			BarWidth = 2.0f,
			Spacing = 1.0f
		};
	}
	
	// Generate oscilloscope line from FFT data
	public static LineSeries CreateOscilloscope(float[] fft, uint color = 0xFFFF8800)
	{
		var x = new float[fft.Length];
		var y = new float[fft.Length];
		
		for (int i = 0; i < fft.Length; i++)
		{
			x[i] = (float)i / (fft.Length - 1);
			y[i] = fft[i];
		}
		
		return new LineSeries { X = x, Y = y, Color = color };
	}
	
	// Generate polar wheel from FFT data
	public static PolarPlot CreatePolarWheel(float[] fft, uint color = 0xFF44AAFF)
	{
		var radii = new float[fft.Length];
		var thetas = new float[fft.Length];
		
		for (int i = 0; i < fft.Length; i++)
		{
			thetas[i] = (float)i / fft.Length * 2 * (float)Math.PI;
			radii[i] = fft[i] * 0.5f + 0.5f; // Scale and offset
		}
		
		return new PolarPlot { Radii = radii, Thetas = thetas, Color = color };
	}
	
	// NEW: Matplotlib-inspired advanced plots
	
	// Generate waterfall/spectrogram from FFT data over time
	public static Heatmap CreateSpectrogram(float[][] fftHistory, uint[]? colors = null)
	{
		var defaultColors = new uint[] { 0xFF000000, 0xFF0000FF, 0xFF00FFFF, 0xFF00FF00, 0xFFFFFF00, 0xFFFF0000 };
		var finalColors = colors ?? defaultColors;
		
		return new Heatmap
		{
			Data = fftHistory,
			Colors = finalColors,
			Width = fftHistory.Length > 0 ? fftHistory[0].Length : 0,
			Height = fftHistory.Length
		};
	}
	
	// Generate 3D-like surface plot (simulated with height mapping)
	public static SurfacePlot CreateSurfacePlot(float[] fft, uint baseColor = 0xFF44AAFF)
	{
		return new SurfacePlot
		{
			Data = fft,
			BaseColor = baseColor,
			HeightScale = 2.0f,
			Wireframe = true
		};
	}
	
	// Generate animated scatter plot with beat detection
	public static ScatterPlot CreateBeatScatter(float[] fft, bool[] beats, uint beatColor = 0xFFFF0000)
	{
		var x = new float[fft.Length];
		var y = new float[fft.Length];
		
		for (int i = 0; i < fft.Length; i++)
		{
			x[i] = (float)i / (fft.Length - 1);
			y[i] = fft[i];
		}
		
		return new ScatterPlot { X = x, Y = y, Color = beatColor, PointSize = 4.0f };
	}
}
