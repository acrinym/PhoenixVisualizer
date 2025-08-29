using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PhoenixVisualizer.Core.Nodes;
using PhoenixVisualizer.PluginHost;

// Reference classes from the current namespace
using PhxEditorViewModel = PhoenixVisualizer.Views.PhxEditorViewModel;
using EffectStackItem = PhoenixVisualizer.Views.EffectStackItem;
using PixelFormat = Avalonia.Platform.PixelFormat;
using CoreEffectParam = PhoenixVisualizer.Core.Nodes.EffectParam;

namespace PhoenixVisualizer.App.Views;

/// <summary>
/// PHX Preview Renderer - Renders effect nodes in the editor preview canvas
/// Handles real-time rendering, audio integration, and performance monitoring
/// </summary>
public class PhxPreviewRenderer
{
    private readonly Canvas _previewCanvas;
    private readonly PhxEditorViewModel _viewModel;
    private readonly DispatcherTimer _renderTimer;
    private readonly Stopwatch _frameTimer;
    private readonly List<double> _frameTimes;

    // Rendering state
    private WriteableBitmap _bitmap;
    private bool _isRendering;
    private int _frameCount;
    private DateTime _startTime;
    private float[] _waveformBuffer;
    private float[] _spectrumBuffer;

    // Audio integration
    private AudioFeaturesImpl _audioFeatures;

    // Performance monitoring
    public double CurrentFps { get; private set; }
    public double AverageFps { get; private set; }
    public long MemoryUsage { get; private set; }

    public PhxPreviewRenderer(Canvas previewCanvas, PhxEditorViewModel viewModel)
    {
        _previewCanvas = previewCanvas;
        _viewModel = viewModel;
        _renderTimer = new DispatcherTimer();
        _frameTimer = new Stopwatch();
        _frameTimes = new List<double>();

        // Initialize required fields
        _bitmap = null!;
        _waveformBuffer = Array.Empty<float>();
        _spectrumBuffer = Array.Empty<float>();
        _audioFeatures = new AudioFeaturesImpl();

        InitializeRenderer();
        SetupAudioIntegration();
        StartRendering();
    }

    private void InitializeRenderer()
    {
        // Create bitmap for rendering (300x250 as per XAML)
        _bitmap = new WriteableBitmap(new PixelSize(300, 250), new Vector(96, 96), PixelFormat.Bgra8888);

        // Set canvas background to display our bitmap
        _previewCanvas.Background = new ImageBrush(_bitmap)
        {
            Stretch = Stretch.Fill
        };

        // Initialize audio buffers
        _waveformBuffer = new float[512];
        _spectrumBuffer = new float[256];

        // Initialize audio features
        _audioFeatures = new AudioFeaturesImpl();

        _startTime = DateTime.Now;
        _frameCount = 0;
    }

    private void SetupAudioIntegration()
    {
        // Initialize audio features with default values
        _audioFeatures.Bass = 0.5f;
        _audioFeatures.Mid = 0.3f;
        _audioFeatures.Treble = 0.2f;
        _audioFeatures.Volume = 0.7f;
        _audioFeatures.Beat = false;
        _audioFeatures.Bpm = 120;
        _audioFeatures.TimeSeconds = 0;

        // Generate mock waveform and spectrum data
        GenerateMockAudioData();
    }

    private void StartRendering()
    {
        _renderTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
        _renderTimer.Tick += RenderFrame;
        _renderTimer.Start();

        _frameTimer.Start();
    }

    private void RenderFrame(object? sender, EventArgs e)
    {
        if (_isRendering || _bitmap == null) return;

        _isRendering = true;
        _frameCount++;

        try
        {
            // Update timing
            var currentTime = DateTime.Now;
            var elapsed = (currentTime - _startTime).TotalSeconds;
            _audioFeatures.TimeSeconds = (float)elapsed;

            // Update audio data
            UpdateAudioData();

            // Clear bitmap
            using (var framebuffer = _bitmap.Lock())
            {
                unsafe
                {
                    // Clear to black
                    var pixels = (uint*)framebuffer.Address;
                    for (int i = 0; i < framebuffer.Size.Width * framebuffer.Size.Height; i++)
                    {
                        pixels[i] = 0xFF000000; // ABGR format: Alpha=255, Blue=0, Green=0, Red=0
                    }
                }
            }

            // Render effect stack
            RenderEffectStack();

            // Update performance metrics
            UpdatePerformanceMetrics();

            // Update view model
            _viewModel.FpsCounter = $"{CurrentFps:F1} FPS";
            _viewModel.MemoryUsage = $"{MemoryUsage / 1024 / 1024} MB";

        }
        catch (Exception ex)
        {
            // Handle rendering errors gracefully
            _viewModel.StatusMessage = $"Render error: {ex.Message}";
        }
        finally
        {
            _isRendering = false;
        }
    }

    private void RenderEffectStack()
    {
        if (_viewModel.EffectStack.Count == 0)
        {
            RenderDefaultPattern();
            return;
        }

        // Create render context
        var context = new RenderContext
        {
            Width = 300,
            Height = 250,
            Time = (float)_audioFeatures.TimeSeconds,
            Beat = _audioFeatures.Beat,
            Volume = _audioFeatures.Volume,
            Waveform = _waveformBuffer,
            Spectrum = _spectrumBuffer
        };

        // Render each effect in the stack
        using (var framebuffer = _bitmap.Lock())
        {
            unsafe
            {
                var pixels = (uint*)framebuffer.Address;

                foreach (var effectItem in _viewModel.EffectStack)
                {
                    // Get the actual effect node
                    var effectNode = GetEffectNode(effectItem);
                    if (effectNode != null)
                    {
                        // Render effect to our pixel buffer
                        RenderEffectToBuffer(effectNode, effectItem.Parameters, context, pixels);
                    }
                }
            }
        }
    }

    private IEffectNode GetEffectNode(EffectStackItem effectItem)
    {
        // Map effect names to actual effect nodes
        return effectItem.EffectType switch
        {
            "Phoenix" => GetPhoenixEffect(effectItem.Name),
            "AVS" => GetAvsEffect(effectItem.Name),
            "Research" => GetResearchEffect(effectItem.Name),
            _ => null
        };
    }

    private IEffectNode GetPhoenixEffect(string name)
    {
        return name switch
        {
            "Cymatics Visualizer" => new CymaticsNode(),
            "Shader Visualizer" => new ShaderVisualizerNode(),
            "Sacred Geometry" => new SacredGeometryNode(),
            "Godrays" => new GodraysNode(),
            "Particle Swarm" => new ParticleSwarmNode(),
            _ => null
        };
    }

    private IEffectNode GetAvsEffect(string name)
    {
        // TODO: Implement AVS effect conversion
        // For now, return a basic effect
        return null;
    }

    private IEffectNode GetResearchEffect(string name)
    {
        // TODO: Implement research effects
        return null;
    }

    private unsafe void RenderEffectToBuffer(IEffectNode effect, Dictionary<string, CoreEffectParam> parameters,
                                    RenderContext context, uint* pixels)
    {
        // This is a simplified rendering approach
        // In a full implementation, we'd integrate with SkiaSharp or similar

        // For demonstration, we'll create a simple pattern based on the effect type
        var random = new Random();

        for (int y = 0; y < context.Height; y++)
        {
            for (int x = 0; x < context.Width; x++)
            {
                // Generate color based on effect type and parameters
                uint color = GenerateEffectColor(effect, parameters, x, y, context);

                // Set pixel (note: Avalonia uses RGBA but we need BGRA for the bitmap)
                int index = y * context.Width + x;
                if (index < context.Width * context.Height)
                {
                    // Blend with existing pixel
                    uint existing = pixels[index];
                    pixels[index] = BlendColors(existing, color);
                }
            }
        }
    }

    private uint GenerateEffectColor(IEffectNode effect, Dictionary<string, CoreEffectParam> parameters,
                                   int x, int y, RenderContext context)
    {
        // Simplified effect rendering - in practice, this would call the actual effect's render method
        float normalizedX = (float)x / context.Width;
        float normalizedY = (float)y / context.Height;

        // Base pattern generation
        float pattern = GenerateBasePattern(effect, normalizedX, normalizedY, context.Time);

        // Apply effect-specific modifications
        pattern = ApplyEffectModifications(effect, parameters, pattern, context);

        // Convert to color
        return PatternToColor(pattern, effect, parameters);
    }

    private float GenerateBasePattern(IEffectNode effect, float x, float y, float time)
    {
        // Generate different patterns based on effect type
        if (effect is CymaticsNode)
        {
            // Cymatics pattern - concentric circles
            float centerX = 0.5f;
            float centerY = 0.5f;
            float distance = (float)Math.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
            return (float)Math.Sin(distance * 20 + time * 2) * 0.5f + 0.5f;
        }
        else if (effect is ShaderVisualizerNode)
        {
            // Shader pattern - fractal-like
            return (float)(Math.Sin(x * 10 + time) * Math.Cos(y * 10 + time) * 0.5f + 0.5f);
        }
        else if (effect is SacredGeometryNode)
        {
            // Sacred geometry - geometric patterns
            float angle = (float)Math.Atan2(y - 0.5f, x - 0.5f);
            float radius = (float)Math.Sqrt((x - 0.5f) * (x - 0.5f) + (y - 0.5f) * (y - 0.5f));
            return (float)(Math.Sin(angle * 6 + radius * 10 + time) * 0.5f + 0.5f);
        }

        // Default pattern
        return (float)(Math.Sin(x * 5 + time) * Math.Cos(y * 5 + time) * 0.5f + 0.5f);
    }

    private float ApplyEffectModifications(IEffectNode effect, Dictionary<string, CoreEffectParam> parameters,
                                         float basePattern, RenderContext context)
    {
        // Apply parameter modifications
        if (parameters.TryGetValue("intensity", out var intensity))
        {
            basePattern *= intensity.FloatValue;
        }

        if (parameters.TryGetValue("speed", out var speed))
        {
            basePattern *= speed.FloatValue;
        }

        // Apply audio reactivity
        if (context.Volume > 0.1f)
        {
            basePattern += context.Volume * 0.2f;
        }

        if (context.Beat)
        {
            basePattern += 0.3f;
        }

        return Math.Clamp(basePattern, 0f, 1f);
    }

    private uint PatternToColor(float pattern, IEffectNode effect, Dictionary<string, CoreEffectParam> parameters)
    {
        // Get base color from parameters
        uint baseColor = 0xFF00FFFF; // Default cyan

        if (parameters.TryGetValue("baseColor", out var colorParam))
        {
            // Parse hex color (simplified)
            baseColor = ParseHexColor(colorParam.ColorValue);
        }

        // Apply pattern intensity
        float r = ((baseColor >> 16) & 0xFF) / 255f;
        float g = ((baseColor >> 8) & 0xFF) / 255f;
        float b = (baseColor & 0xFF) / 255f;

        r *= pattern;
        g *= pattern;
        b *= pattern;

        // Convert back to uint (BGRA format for Avalonia)
        return (uint)(
            (255 << 24) |                    // Alpha
            ((byte)(b * 255) << 16) |        // Blue
            ((byte)(g * 255) << 8) |         // Green
            (byte)(r * 255)                  // Red
        );
    }

    private uint ParseHexColor(string hexColor)
    {
        if (string.IsNullOrEmpty(hexColor) || !hexColor.StartsWith("#"))
            return 0xFF00FFFF; // Default cyan

        try
        {
            if (hexColor.Length == 7) // #RRGGBB
            {
                uint r = Convert.ToUInt32(hexColor.Substring(1, 2), 16);
                uint g = Convert.ToUInt32(hexColor.Substring(3, 2), 16);
                uint b = Convert.ToUInt32(hexColor.Substring(5, 2), 16);
                return 0xFF000000 | (b << 16) | (g << 8) | r; // BGRA
            }
        }
        catch
        {
            // Fall back to default
        }

        return 0xFF00FFFF;
    }

    private uint BlendColors(uint existing, uint newColor)
    {
        // Simple alpha blending
        float alpha = ((newColor >> 24) & 0xFF) / 255f;

        byte existingR = (byte)(existing & 0xFF);
        byte existingG = (byte)((existing >> 8) & 0xFF);
        byte existingB = (byte)((existing >> 16) & 0xFF);

        byte newR = (byte)(newColor & 0xFF);
        byte newG = (byte)((newColor >> 8) & 0xFF);
        byte newB = (byte)((newColor >> 16) & 0xFF);

        byte blendedR = (byte)(existingR * (1 - alpha) + newR * alpha);
        byte blendedG = (byte)(existingG * (1 - alpha) + newG * alpha);
        byte blendedB = (byte)(existingB * (1 - alpha) + newB * alpha);

        return (uint)((existing & 0xFF000000) | (blendedB << 16) | (blendedG << 8) | blendedR);
    }

    private void RenderDefaultPattern()
    {
        // Render a default animated pattern when no effects are active
        using (var framebuffer = _bitmap.Lock())
        {
            unsafe
            {
                var pixels = (uint*)framebuffer.Address;

                for (int y = 0; y < 250; y++)
                {
                    for (int x = 0; x < 300; x++)
                    {
                        float u = (float)x / 300f;
                        float v = (float)y / 250f;
                        float time = (float)_audioFeatures.TimeSeconds;

                        // Create animated pattern
                        float pattern = (float)(Math.Sin(u * 10 + time) * Math.Cos(v * 10 + time));
                        pattern = (pattern + 1) * 0.5f; // Normalize to 0-1

                        // Audio-reactive color
                        float hue = _audioFeatures.Bass * 360f;
                        uint color = HsvToRgb(hue, 0.8f, pattern);

                        pixels[y * 300 + x] = color;
                    }
                }
            }
        }
    }

    private uint HsvToRgb(float hue, float saturation, float brightness)
    {
        float c = brightness * saturation;
        float x = c * (1 - (float)Math.Abs((hue / 60) % 2 - 1));
        float m = brightness - c;

        float r = 0, g = 0, b = 0;

        if (hue < 60) { r = c; g = x; b = 0; }
        else if (hue < 120) { r = x; g = c; b = 0; }
        else if (hue < 180) { r = 0; g = c; b = x; }
        else if (hue < 240) { r = 0; g = x; b = c; }
        else if (hue < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        byte red = (byte)((r + m) * 255);
        byte green = (byte)((g + m) * 255);
        byte blue = (byte)((b + m) * 255);

        return (uint)(0xFF000000 | (blue << 16) | (green << 8) | red);
    }

    private void UpdateAudioData()
    {
        // Simulate audio data changes
        var random = new Random();

        // Update bass/mid/treble with some variation
        _audioFeatures.Bass = Math.Clamp(_audioFeatures.Bass + (float)(random.NextDouble() - 0.5) * 0.1f, 0f, 1f);
        _audioFeatures.Mid = Math.Clamp(_audioFeatures.Mid + (float)(random.NextDouble() - 0.5) * 0.1f, 0f, 1f);
        _audioFeatures.Treble = Math.Clamp(_audioFeatures.Treble + (float)(random.NextDouble() - 0.5) * 0.1f, 0f, 1f);

        // Occasional beats
        _audioFeatures.Beat = random.NextDouble() < 0.05; // 5% chance per frame

        // Generate waveform
        for (int i = 0; i < _waveformBuffer.Length; i++)
        {
            float phase = (float)(_audioFeatures.TimeSeconds * 2) + i * 0.1f;
            _waveformBuffer[i] = (float)(Math.Sin(phase) * 0.5 + Math.Sin(phase * 2) * 0.3f);
        }

        // Generate spectrum
        for (int i = 0; i < _spectrumBuffer.Length; i++)
        {
            float freq = (float)i / _spectrumBuffer.Length;
            _spectrumBuffer[i] = (float)(Math.Exp(-freq * 2) * (0.5 + Math.Sin(_audioFeatures.TimeSeconds + freq * 10) * 0.5f));
        }
    }

    private void GenerateMockAudioData()
    {
        var random = new Random();

        for (int i = 0; i < _waveformBuffer.Length; i++)
        {
            _waveformBuffer[i] = (float)(random.NextDouble() * 2 - 1) * 0.5f;
        }

        for (int i = 0; i < _spectrumBuffer.Length; i++)
        {
            _spectrumBuffer[i] = (float)random.NextDouble() * 0.8f;
        }
    }

    private void UpdatePerformanceMetrics()
    {
        // Update FPS calculation
        double currentFrameTime = _frameTimer.Elapsed.TotalMilliseconds;
        _frameTimer.Restart();

        _frameTimes.Add(currentFrameTime);
        if (_frameTimes.Count > 60) // Keep last 60 frames
        {
            _frameTimes.RemoveAt(0);
        }

        CurrentFps = 1000.0 / currentFrameTime;
        AverageFps = _frameTimes.Count / _frameTimes.Sum() * 1000.0;

        // Update memory usage (simplified)
        MemoryUsage = GC.GetTotalMemory(false);
    }

    public void Stop()
    {
        _renderTimer.Stop();
        _frameTimer.Stop();
    }

    public void Pause()
    {
        _renderTimer.Stop();
    }

    public void Resume()
    {
        _renderTimer.Start();
    }

    public void Restart()
    {
        _startTime = DateTime.Now;
        _frameCount = 0;
        _frameTimes.Clear();
    }
}
