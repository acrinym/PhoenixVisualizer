using Avalonia.Controls;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.Editor.Rendering;

/// <summary>
/// Stub RenderSurface class for the Editor project to avoid circular dependencies
/// </summary>
public sealed class RenderSurface : Control
{
    private IVisualizerPlugin? _plugin;
    private bool _showDiagnostics = false;
    private float _uiSensitivity = 1.0f;
    private float _uiSmoothing = 0.35f;

    public void SetPlugin(IVisualizerPlugin? plugin)
    {
        _plugin = plugin;
        // TODO: Implement plugin setting in editor context
    }

    public void ToggleDiagnostics() => _showDiagnostics = !_showDiagnostics;
    
    public void SetSensitivity(float sensitivity) => _uiSensitivity = sensitivity;
    
    public void SetSmoothing(float smoothing) => _uiSmoothing = smoothing;
    
    public void SetMaxDrawCalls(int maxCalls) 
    { 
        // TODO: Implement max draw calls setting
    }
}
