using System;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Plugins.Ape.Phoenix;

// Phoenix visualizer as an APE-style plugin
public sealed class PhoenixPlugin : IVisualizerPlugin
{
	public string Id => "phoenix";
	public string DisplayName => "Phoenix Visualizer";
	
	private int _width;
	private int _height;
	private float _time;
	private float _lastBpm;
	private bool _isActive;
	
	// Phoenix state
	private float _phoenixX;
	private float _phoenixY;
	private float _phoenixScale = 1.0f;
	private uint _phoenixColor = 0xFFFF8800; // Orange base
	private float _flameIntensity = 0.5f;
	
	public void Initialize(int width, int height)
	{
		_width = width;
		_height = height;
		_phoenixX = width * 0.5f;
		_phoenixY = height * 0.5f;
		_isActive = true;
	}
	
	public void Resize(int width, int height)
	{
		_width = width;
		_height = height;
		_phoenixX = width * 0.5f;
		_phoenixY = height * 0.5f;
	}
	
	public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
	{
		if (!_isActive) return;
		
		_time = (float)features.TimeSeconds;
		
		// Update Phoenix based on audio
		UpdatePhoenix(features);
		
		// Clear canvas
		canvas.Clear(0xFF000000);
		
		// Draw Phoenix
		DrawPhoenix(canvas);
	}
	
	private void UpdatePhoenix(AudioFeatures features)
	{
		// BPM affects animation speed
		if (features.Bpm > 0)
		{
			_lastBpm = (float)features.Bpm;
			_phoenixScale = 0.8f + (float)((features.Bpm / 200.0f) * 0.4f); // Scale with BPM
		}
		
		// Energy affects flame intensity
		_flameIntensity = Math.Min(1.0f, features.Energy * 2.0f);
		
		// Beat detection for "burst" effect
		if (features.Beat)
		{
			_phoenixScale *= 1.2f; // Quick scale up on beat
		}
		
		// Frequency bands affect color
		UpdatePhoenixColor(features);
		
		// Gentle movement
		_phoenixX = _width * 0.5f + (float)(Math.Sin(_time * 0.5) * 50);
		_phoenixY = _height * 0.5f + (float)(Math.Cos(_time * 0.3) * 30);
	}
	
	private void UpdatePhoenixColor(AudioFeatures features)
	{
		// Base color from genre or fallback to frequency mapping
		if (!string.IsNullOrEmpty(features.Genre))
		{
			_phoenixColor = GetGenreColor(features.Genre);
		}
		else
		{
			// Frequency-based color mapping
			float bass = features.Bass;
			float mid = features.Mid;
			float treble = features.Treble;
			
			// Mix RGB based on frequency bands
			uint r = (uint)(bass * 255);
			uint g = (uint)(mid * 255);
			uint b = (uint)(treble * 255);
			
			_phoenixColor = (r << 16) | (g << 8) | b;
		}
	}
	
	private uint GetGenreColor(string genre)
	{
		return genre.ToLowerInvariant() switch
		{
			"blues" or "jazz" => 0xFF0000FF,      // Blue
			"bluegrass" => 0xFF00AAFF,             // Light blue
			"classical" => 0xFFFFFF00,             // Gold
			"metal" => 0xFF800080,                 // Purple
			"electronic" or "trance" => 0xFFFF00FF, // Pink
			"hip hop" or "rap" => 0xFF00FF00,      // Green
			"pop" => 0xFFFF8800,                   // Orange
			_ => 0xFFFF8800                        // Default orange
		};
	}
	
	private void DrawPhoenix(ISkiaCanvas canvas)
	{
		// Simple Phoenix representation (circle with flame effect)
		float size = 50.0f * _phoenixScale;
		
		// Draw main body
		canvas.FillCircle(_phoenixX, _phoenixY, size, _phoenixColor);
		
		// Draw flame effect based on energy
		if (_flameIntensity > 0.1f)
		{
			uint flameColor = BlendColors(_phoenixColor, 0xFFFF0000, _flameIntensity);
			canvas.FillCircle(_phoenixX, _phoenixY - size * 0.8f, size * 0.6f * _flameIntensity, flameColor);
		}
		
		// Draw wings (simple lines)
		uint wingColor = BlendColors(_phoenixColor, 0xFFFFFFFF, 0.3f);
		float wingLength = size * 1.2f;
		
		// Left wing
		canvas.DrawLines(new[] { (_phoenixX, _phoenixY), (_phoenixX - wingLength, _phoenixY - size * 0.5f) }, 3.0f, wingColor);
		// Right wing
		canvas.DrawLines(new[] { (_phoenixX, _phoenixY), (_phoenixX + wingLength, _phoenixY - size * 0.5f) }, 3.0f, wingColor);
	}
	
	private uint BlendColors(uint color1, uint color2, float ratio)
	{
		// Simple color blending
		uint r1 = (color1 >> 16) & 0xFF;
		uint g1 = (color1 >> 8) & 0xFF;
		uint b1 = color1 & 0xFF;
		
		uint r2 = (color2 >> 16) & 0xFF;
		uint g2 = (color2 >> 8) & 0xFF;
		uint b2 = color2 & 0xFF;
		
		uint r = (uint)(r1 * (1 - ratio) + r2 * ratio);
		uint g = (uint)(g1 * (1 - ratio) + g2 * ratio);
		uint b = (uint)(b1 * (1 - ratio) + b2 * ratio);
		
		return (r << 16) | (g << 8) | b;
	}
	
	public void LoadPreset(string preset)
	{
		// Phoenix plugin doesn't use text presets like AVS
		// But could load color schemes or animation styles
	}
	
	public void Dispose()
	{
		_isActive = false;
	}
}
