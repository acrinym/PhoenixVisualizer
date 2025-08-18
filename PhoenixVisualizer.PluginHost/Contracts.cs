using System;
using System.Collections.Generic;

namespace PhoenixVisualizer.PluginHost;

/// <summary>
/// Audio features extracted from audio data
/// </summary>
public interface AudioFeatures
{
    float[] Fft { get; }
    float[] Waveform { get; }
    float Rms { get; }
    double Bpm { get; }
    bool Beat { get; }
    
    // Restore missing properties
    float Bass { get; }
    float Mid { get; }
    float Treble { get; }
    float Energy { get; }
    float Volume { get; }
    float Peak { get; }
    double TimeSeconds { get; }
}

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
    void DrawLines(System.Span<(float x, float y)> points, float thickness, uint color);
    void DrawRect(float x, float y, float width, float height, uint color, bool filled = false);
    void FillRect(float x, float y, float width, float height, uint color);
    void DrawCircle(float x, float y, float radius, uint color, bool filled = false);
    void FillCircle(float x, float y, float radius, uint color);
    void DrawText(string text, float x, float y, uint color, float size = 12.0f);
    void DrawPoint(float x, float y, uint color, float size = 1.0f);
    void Fade(uint color, float alpha);
    
    // Additional methods for superscopes
    void DrawPolygon(System.Span<(float x, float y)> points, uint color, bool filled = false);
    void DrawArc(float x, float y, float radius, float startAngle, float sweepAngle, uint color, float thickness = 1.0f);
    void SetLineWidth(float width);
    float GetLineWidth();
}

/// <summary>
/// Base interface for all visualizer plugins
/// </summary>
public interface IVisualizerPlugin
{
    string Id { get; }
    string DisplayName { get; }
    void Initialize(int width, int height);
    void Resize(int width, int height);
    void RenderFrame(AudioFeatures features, ISkiaCanvas canvas);
    void Dispose();
}

/// <summary>
/// APE Host interface for managing APE plugins
/// </summary>
public interface IApeHost
{
    string Name { get; }
    string Version { get; }
    bool IsInitialized { get; }
    
    void Initialize();
    void Shutdown();
    void ProcessFrame(AudioFeatures features, ISkiaCanvas canvas);
    List<IApeEffect> GetAvailableEffects();
}

/// <summary>
/// APE Effect interface for individual effects
/// </summary>
public interface IApeEffect
{
    string Id { get; }
    string DisplayName { get; }
    string Description { get; }
    bool IsEnabled { get; set; }
    
    void Initialize();
    void Shutdown();
    void ProcessFrame(AudioFeatures features, ISkiaCanvas canvas);
    void Configure();
}

/// <summary>
/// AVS Host Plugin interface for Advanced Visualization Studio
/// </summary>
public interface IAvsHostPlugin
{
    string Id { get; }
    string DisplayName { get; }
    string Description { get; }
    bool IsEnabled { get; set; }
    
    void Initialize();
    void Shutdown();
    void ProcessFrame(AudioFeatures features, ISkiaCanvas canvas);
    void Configure();
    void LoadPreset(string presetText);
}

/// <summary>
/// Winamp Visualizer Plugin interface
/// </summary>
public interface IWinampVisPlugin
{
    string Description { get; }
    int SampleRate { get; }
    int Channels { get; }
    int LatencyMs { get; }
    int DelayMs { get; }
    int SpectrumChannels { get; }
    int WaveformChannels { get; }
    
    bool Initialize(IntPtr hwndParent);
    bool Render();
    void Shutdown();
    void Configure();
}

/// <summary>
/// Winamp Visualizer Plugin Header interface
/// </summary>
public interface IWinampVisHeader
{
    int Version { get; }
    string Description { get; }
    IWinampVisPlugin GetModule(int index);
}

/// <summary>
/// Winamp Visualizer Plugin Properties interface
/// </summary>
public interface IWinampVisPluginProperties
{
    string FilePath { get; }
    string Extension { get; }
    string FileName { get; }
    uint NumberOfModules { get; }
    IntPtr HDll { get; }
    IntPtr Module { get; }
}
