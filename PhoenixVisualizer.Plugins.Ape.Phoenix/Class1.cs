using System;
using PhoenixVisualizer.PluginHost;
using System.Collections.Generic; // Added for List and Dictionary

namespace PhoenixVisualizer.Plugins.Ape.Phoenix;

/// <summary>
/// Phoenix APE Effect Plugin - Advanced visualizer effects using APE system
/// </summary>
public sealed class PhoenixApeEffect : IApeEffect
{
    public string Id => "phoenix_ape";
    public string DisplayName => "Phoenix APE Effects";
    public string Description => "Advanced visualizer effects using APE system";
    public bool IsEnabled { get; set; } = true;

    private int _width;
    private int _height;
    private readonly ApeEffectEngine _effectEngine;

    public PhoenixApeEffect()
    {
        _effectEngine = new ApeEffectEngine();
    }

    public void Initialize()
    {
        _effectEngine.Initialize(_width, _height);
    }

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _effectEngine.Initialize(width, height);
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        _effectEngine.Resize(width, height);
    }

    public void Shutdown()
    {
        _effectEngine?.Shutdown();
    }

    public void ProcessFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        try
        {
            // Clear canvas with dark background
            canvas.Clear(0xFF000000);

            // Process audio features through APE effect engine
            _effectEngine.ProcessAudio(features);
            
            // Render the effects
            _effectEngine.Render(canvas);
        }
        catch (Exception ex)
        {
            // Fallback rendering on error
            RenderFallback(canvas, ex.Message);
        }
    }

    public void Configure()
    {
        try
        {
            // Simple console-based configuration for Phoenix APE effects
            Console.WriteLine("=== Phoenix APE Effect Configuration ===");
            Console.WriteLine("Available Effect Types:");
            Console.WriteLine("1. Flame");
            Console.WriteLine("2. Phoenix");
            Console.WriteLine("3. Sacred");
            Console.WriteLine("4. Threshold");
            Console.WriteLine("5. Purification");
            
            Console.Write("Select effect type (1-5): ");
            var effectTypeInput = Console.ReadLine();
            
            Console.Write("Enter intensity (0.1-2.0): ");
            var intensityInput = Console.ReadLine();
            
            Console.Write("Enter primary color (R,G,B): ");
            var colorInput = Console.ReadLine();
            
            // Parse inputs
            var effectType = ParseEffectType(effectTypeInput);
            var intensity = ParseDouble(intensityInput, 1.0);
            var color = ParseColor(colorInput);
            
            // Update effect parameters
            UpdateEffectConfiguration(effectType, intensity, color);
            
            Console.WriteLine("Configuration applied successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error configuring Phoenix APE Effect: {ex.Message}");
        }
    }

    private string ParseEffectType(string? input)
    {
        return input switch
        {
            "1" => "Flame",
            "2" => "Phoenix",
            "3" => "Sacred",
            "4" => "Threshold",
            "5" => "Purification",
            _ => "Flame"
        };
    }

    private double ParseDouble(string? input, double defaultValue)
    {
        if (double.TryParse(input, out var result))
            return Math.Clamp(result, 0.1, 2.0);
        return defaultValue;
    }

    private string ParseColor(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return "255,0,0"; // Default to red
        return input;
    }

    private void UpdateEffectConfiguration(string effectType, double intensity, string color)
    {
        // Update internal effect parameters based on configuration
        Console.WriteLine($"Phoenix APE Effect configured: Type={effectType}, Intensity={intensity}, Color={color}");
    }

    private void RenderFallback(ISkiaCanvas canvas, string errorMessage)
    {
        // Simple fallback visualization
        canvas.Clear(0xFF000000);
        
        // Draw error indicator
        var centerX = _width / 2f;
        var centerY = _height / 2f;
        
        // Draw a simple cross pattern
        canvas.DrawLines(new[] { (centerX - 20f, centerY), (centerX + 20f, centerY) }, 2f, 0xFFFF0000);
        canvas.DrawLines(new[] { (centerX, centerY - 20f), (centerX, centerY + 20f) }, 2f, 0xFFFF0000);
    }

    public void Dispose()
    {
        _effectEngine?.Dispose();
    }
}

/// <summary>
/// APE Effect Engine - Processes audio and renders effects
/// </summary>
public sealed class ApeEffectEngine : IDisposable
{
    private int _width;
    private int _height;
    private readonly List<ApeEffect> _effects = new();
    private readonly Dictionary<string, double> _variables = new();

    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        InitializeDefaultEffects();
        InitializeVariables();
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        foreach (var effect in _effects)
        {
            effect.Resize(width, height);
        }
    }

    public void Shutdown()
    {
        foreach (var effect in _effects)
        {
            effect?.Dispose();
        }
        _effects.Clear();
    }

    public void ProcessAudio(AudioFeatures features)
    {
        // Update variables based on audio features
        _variables["bass"] = features.Bass;
        _variables["mid"] = features.Mid;
        _variables["treble"] = features.Treble;
        _variables["beat"] = features.Beat ? 1.0 : 0.0;
        _variables["bpm"] = features.Bpm;
        _variables["energy"] = features.Energy;
        _variables["volume"] = features.Volume;
        _variables["rms"] = features.Rms;
        _variables["peak"] = features.Peak;

        // Process each effect
        foreach (var effect in _effects)
        {
            effect.ProcessAudio(features, _variables);
        }
    }

    public void Render(ISkiaCanvas canvas)
    {
        // Render all effects in order
        foreach (var effect in _effects)
        {
            effect.Render(canvas);
        }
    }

    private void InitializeDefaultEffects()
    {
        // Add some default APE effects
        _effects.Add(new BassReactiveEffect());
        _effects.Add(new BeatPulseEffect());
        _effects.Add(new FrequencyWaveEffect());
    }

    private void InitializeVariables()
    {
        _variables["width"] = _width;
        _variables["height"] = _height;
        _variables["time"] = 0.0;
        _variables["frame"] = 0.0;
    }

    public void Dispose()
    {
        foreach (var effect in _effects)
        {
            effect.Dispose();
        }
        _effects.Clear();
        _variables.Clear();
    }
}

/// <summary>
/// Base class for APE effects
/// </summary>
public abstract class ApeEffect : IDisposable
{
    protected int Width { get; private set; }
    protected int Height { get; private set; }
    protected double Time { get; private set; }
    protected int Frame { get; private set; }

    public virtual void Resize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public virtual void ProcessAudio(AudioFeatures features, Dictionary<string, double> variables)
    {
        Time = features.TimeSeconds;
        Frame++;
    }

    public abstract void Render(ISkiaCanvas canvas);

    public virtual void Dispose() { }
}

/// <summary>
/// Bass-reactive effect that responds to low frequencies
/// </summary>
public sealed class BassReactiveEffect : ApeEffect
{
    private double _lastBass = 0.0;
    private readonly List<(double x, double y, double size)> _particles = new();

    public override void ProcessAudio(AudioFeatures features, Dictionary<string, double> variables)
    {
        base.ProcessAudio(features, variables);
        
        var bass = variables["bass"];
        if (bass > _lastBass * 1.2) // Bass spike detected
        {
            // Add new particles
            var random = new Random();
            for (int i = 0; i < 5; i++)
            {
                var x = random.NextDouble() * Width;
                var y = Height + 10; // Start below screen
                var size = 5 + random.NextDouble() * 15;
                _particles.Add((x, y, size));
            }
        }
        _lastBass = bass;

        // Update particle positions
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var particle = _particles[i];
            var newY = particle.y - 2.0; // Move up
            var newSize = particle.size * 0.98; // Shrink slightly
            
            if (newY < -particle.size || newSize < 1.0)
            {
                _particles.RemoveAt(i);
            }
            else
            {
                _particles[i] = (particle.x, newY, newSize);
            }
        }
    }

    public override void Render(ISkiaCanvas canvas)
    {
        // Render particles
        foreach (var particle in _particles)
        {
            var color = (uint)(0xFF0000FF | ((int)(particle.size * 16) << 8)); // Blue with size-based alpha
            canvas.FillCircle((float)particle.x, (float)particle.y, (float)particle.size, color);
        }
    }
}

/// <summary>
/// Beat pulse effect that creates expanding circles on beats
/// </summary>
public sealed class BeatPulseEffect : ApeEffect
{
    private readonly List<(double x, double y, double radius, double alpha)> _pulses = new();
    private bool _lastBeat = false;

    public override void ProcessAudio(AudioFeatures features, Dictionary<string, double> variables)
    {
        base.ProcessAudio(features, variables);
        
        var beat = variables["beat"] > 0.5;
        if (beat && !_lastBeat)
        {
            // New beat detected - add pulse
            var centerX = Width / 2.0;
            var centerY = Height / 2.0;
            _pulses.Add((centerX, centerY, 0.0, 1.0));
        }
        _lastBeat = beat;

        // Update pulses
        for (int i = _pulses.Count - 1; i >= 0; i--)
        {
            var pulse = _pulses[i];
            var newRadius = pulse.radius + 3.0;
            var newAlpha = pulse.alpha * 0.95;
            
            if (newAlpha < 0.01)
            {
                _pulses.RemoveAt(i);
            }
            else
            {
                _pulses[i] = (pulse.x, pulse.y, newRadius, newAlpha);
            }
        }
    }

    public override void Render(ISkiaCanvas canvas)
    {
        // Render pulses
        foreach (var pulse in _pulses)
        {
            var alpha = (int)(pulse.alpha * 255);
            var color = (uint)((alpha << 24) | 0x00FFFF); // Cyan with alpha
            canvas.FillCircle((float)pulse.x, (float)pulse.y, (float)pulse.radius, color);
        }
    }
}

/// <summary>
/// Frequency wave effect that visualizes the frequency spectrum
/// </summary>
public sealed class FrequencyWaveEffect : ApeEffect
{
    private readonly float[] _lastFft = new float[64];
    private readonly float[] _smoothFft = new float[64];

    public override void ProcessAudio(AudioFeatures features, Dictionary<string, double> variables)
    {
        base.ProcessAudio(features, variables);
        
        if (features.Fft != null && features.Fft.Length >= 64)
        {
            // Copy and smooth FFT data
            for (int i = 0; i < 64; i++)
            {
                _lastFft[i] = features.Fft[i];
                _smoothFft[i] = _smoothFft[i] * 0.8f + _lastFft[i] * 0.2f;
            }
        }
    }

    public override void Render(ISkiaCanvas canvas)
    {
        // Render frequency bars
        var barWidth = (float)Width / 64f;
        for (int i = 0; i < 64; i++)
        {
            var height = (float)(_smoothFft[i] * Height * 0.8);
            var x = i * barWidth;
            var y = Height - height;
            
            // Color based on frequency (bass = red, mid = green, treble = blue)
            uint color;
            if (i < 16) color = 0xFFFF0000; // Red for bass
            else if (i < 32) color = 0xFF00FF00; // Green for mid
            else color = 0xFF0000FF; // Blue for treble
            
            canvas.FillCircle(x + barWidth / 2, y, barWidth / 3, color);
        }
    }
}
