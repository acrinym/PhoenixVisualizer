using System;
using System.Collections.Generic;
using System.IO;
using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Plugins.Avs;
using PhoenixVisualizer.Rendering;

namespace PhoenixVisualizer;

// 🎚️ Minimal preset manager – cycles through presets in the "Presets" folder
public static class Presets
{
    private static readonly List<string> _presetTexts = new();
    private static readonly Random _rng = new();
    private static int _index = -1;
    private static RenderSurface? _surface;

    public static void Initialize(RenderSurface? surface)
    {
        _surface = surface;
        _presetTexts.Clear();
        _index = -1;

        var dir = Path.Combine(AppContext.BaseDirectory, "Presets");
        if (!Directory.Exists(dir)) return;

        foreach (var file in Directory.GetFiles(dir, "*.avs"))
        {
            try
            {
                _presetTexts.Add(File.ReadAllText(file));
            }
            catch { /* ignore bad files */ }
        }

        if (_presetTexts.Count > 0)
            _index = 0;
    }

    public static void GoPrev()
    {
        if (_presetTexts.Count == 0 || _surface is null) return;
        _index = (_index - 1 + _presetTexts.Count) % _presetTexts.Count;
        ApplyCurrent();
    }

    public static void GoNext()
    {
        if (_presetTexts.Count == 0 || _surface is null) return;
        _index = (_index + 1) % _presetTexts.Count;
        ApplyCurrent();
    }

    public static void GoRandom()
    {
        if (_presetTexts.Count == 0 || _surface is null) return;
        _index = _rng.Next(_presetTexts.Count);
        ApplyCurrent();
    }

    private static void ApplyCurrent()
    {
        if (_surface is null || _index < 0 || _index >= _presetTexts.Count) return;
        var plug = PluginRegistry.Create("vis_avs") as IAvsHostPlugin;
        if (plug is null) return;
        
        // Cast to IVisualizerPlugin since AvsVisualizerPlugin implements both interfaces
        if (plug is IVisualizerPlugin visPlugin)
        {
            _surface.SetPlugin(visPlugin);
            plug.LoadPreset(_presetTexts[_index]);
        }
    }
}

