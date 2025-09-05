using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.NativeAudio;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.App.Rendering;

/// <summary>
/// Plugin wrapper for NativeAudioVisualizerService to integrate with the existing plugin system
/// </summary>
public class NativeVisualizerPlugin : IVisualizerPlugin
{
    private readonly NativeAudioVisualizerService _nativeAudioService;
    private int _width = 800;
    private int _height = 600;
    private bool _initialized = false;
    
    public string Id => "native_visualizer";
    public string DisplayName => "Native VLC Visualizer";
    public string Description => "Transpiled VLC visualizers (GOOM, ProjectM, VSXu, VLC Visual)";
    
    public NativeVisualizerPlugin(NativeAudioVisualizerService nativeAudioService)
    {
        _nativeAudioService = nativeAudioService ?? throw new ArgumentNullException(nameof(nativeAudioService));
    }
    
    public void Initialize(int width, int height)
    {
        _width = width;
        _height = height;
        _initialized = true;
        System.Diagnostics.Debug.WriteLine($"[NativeVisualizerPlugin] Initialized: {width}x{height}");
    }
    
    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        System.Diagnostics.Debug.WriteLine($"[NativeVisualizerPlugin] Resized: {width}x{height}");
    }
    
    public void RenderFrame(PhoenixVisualizer.PluginHost.AudioFeatures audioFeatures, PhoenixVisualizer.PluginHost.ISkiaCanvas canvas)
    {
        if (!_initialized)
            return;
            
        try
        {
            // Clear the canvas with black background
            canvas.Clear(0xFF000000);
            
            // For now, we'll render a simple test pattern
            // TODO: Integrate with the native visualizer's rendering system
            RenderTestPattern(canvas);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NativeVisualizerPlugin] Render failed: {ex.Message}");
        }
    }
    
    private void RenderTestPattern(PhoenixVisualizer.PluginHost.ISkiaCanvas canvas)
    {
        // Render a simple test pattern to verify the plugin is working
        var colors = new uint[] { 0xFF00FF00, 0xFF0000FF, 0xFFFF0000, 0xFFFFFF00 };
        
        for (int i = 0; i < 4; i++)
        {
            float x = i * (_width / 4.0f);
            float y = i * (_height / 4.0f);
            float w = _width / 4.0f;
            float h = _height / 4.0f;
            
            canvas.FillRect(x, y, w, h, colors[i]);
        }
        
        // Draw a border
        canvas.DrawRect(0, 0, _width, _height, 0xFFFFFFFF, false);
        
        // Draw center text area
        float centerX = _width / 2.0f;
        float centerY = _height / 2.0f;
        canvas.FillCircle(centerX, centerY, 50, 0xFFFFFFFF);
    }
    
    public void Dispose()
    {
        _initialized = false;
        System.Diagnostics.Debug.WriteLine("[NativeVisualizerPlugin] Disposed");
    }
}
