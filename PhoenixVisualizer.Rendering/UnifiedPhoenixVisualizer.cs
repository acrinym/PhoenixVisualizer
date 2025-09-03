using System;
using System.Collections.Generic;
using System.Linq;
using PhoenixVisualizer.Parameters;
using PhoenixVisualizer.Core.Transpile;
using SkiaSharp;

namespace PhoenixVisualizer.Rendering
{
    /// <summary>
    /// Renders a UnifiedGraph using Skia. Pulls live values from ParamRegistry so UI edits show up immediately.
    /// No Nullsoft calls, no host dependency.
    /// </summary>
    public sealed class UnifiedPhoenixVisualizer : IVisualizerPlugin, IDisposable
    {
        private UnifiedGraph? _graph;
        private bool _disposed;

        // If your host provides audio features, you can wire them via SetAudioFeatures(...) later.
        // For now we keep a simple time accumulator.
        private double _t;

        // Keep a weak cache of per-node sampling parameters for speed (still reads live from ParamRegistry).
        private readonly Dictionary<string, int> _samplesCache = new();

        public string Id => "unified_phoenix";
        public string Name => "Unified Phoenix Visualizer";
        public string DisplayName => "Phoenix Visualizer";
        public string Description => "Renders Phoenix unified graph";
        public string Author => "Phoenix Visualizer";
        public string Version => "1.0";

        public void Initialize(int width, int height) 
        { 
            // Not used in this implementation
        }
        
        public void Resize(int width, int height) 
        { 
            // Not used in this implementation
        }

        public void LoadGraph(UnifiedGraph graph)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _samplesCache.Clear();

            // Register parameter definitions so the ParameterEditor can populate controls immediately.
            foreach (var n in graph.Nodes)
            {
                var defs = new List<ParamDef>();
                foreach (var (k, v) in n.Parameters)
                {
                    var (ptype, defVal, min, max) = v switch
                    {
                        bool b   => (ParamType.Checkbox, (object)b, 0d, 1d),
                        int i    => (ParamType.Slider, (object)(double)i, 0d, 4096d),
                        double d => (ParamType.Slider, (object)d, 0d, 1d),
                        string s => (ParamType.Text, (object)s, 0d, 1d),
                        _        => (ParamType.Text, (object)(v?.ToString() ?? ""), 0d, 1d)
                    };
                    defs.Add(new ParamDef { Key = k, Label = k, Type = ptype, DefaultValue = defVal, Min = min, Max = max });
                }
                ParamRegistry.Register(n.Id, defs);
            }
        }

        /// <summary>
        /// Host calls this once per frame.
        /// </summary>
        public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
        {
            if (_disposed) return;
            _t = features.TimeSeconds;

            canvas.Clear(0xFF000000);
            if (_graph is null || _graph.Nodes.Count == 0) return;

            // Each node type can draw; for now Superscope is primary. You can extend switch(TypeKey) here.
            foreach (var node in _graph.Nodes)
            {
                switch ((node.TypeKey ?? "").ToLowerInvariant())
                {
                    case "clear":
                    {
                        var hex = Convert.ToString(node.Parameters.TryGetValue("color", out var c) ? c : "#000000") ?? "#000000";
                        var color = ParseColor(hex);
                        canvas.Clear(color);
                        break;
                    }
                    case "text":
                    {
                        var s = Convert.ToString(node.Parameters.GetValueOrDefault("content")) ?? "Phoenix";
                        int x = ToInt(node.Parameters.GetValueOrDefault("x"), 20);
                        int y = ToInt(node.Parameters.GetValueOrDefault("y"), 32);
                        float size = (float)ToDouble(node.Parameters.GetValueOrDefault("size"), 24.0);
                        var hex = Convert.ToString(node.Parameters.GetValueOrDefault("color")) ?? "#00FFFF";
                        var color = ParseColor(hex);
                        // Note: DrawText not available in ISkiaCanvas, using DrawPoint for now
                        canvas.DrawPoint(x, y, color, size);
                        break;
                    }
                    case "circle":
                    {
                        int x = ToInt(node.Parameters.GetValueOrDefault("x"), 128);
                        int y = ToInt(node.Parameters.GetValueOrDefault("y"), 128);
                        float radius = (float)ToDouble(node.Parameters.GetValueOrDefault("radius"), 64);
                        bool filled = node.Parameters.GetValueOrDefault("filled") is bool fb && fb;
                        var hex = Convert.ToString(node.Parameters.GetValueOrDefault("color")) ?? "#FFFFFF";
                        var color = ParseColor(hex);
                        if (filled) canvas.FillCircle(x,y,radius, color);
                        else canvas.DrawCircle(x,y,radius, color, false);
                        break;
                    }
                    case "superscope":
                    default:
                    {
                        // draw simple audio-reactive line to prove live apply
                        int samples = ToInt(node.Parameters.GetValueOrDefault("samples"), 512);
                        samples = Math.Clamp(samples, 16, 8192);
                        var midY = canvas.Height / 2f;
                        var w = canvas.Width;
                        var prevX = 0f;
                        var prevY = midY;
                        for (int i=0;i<samples;i++)
                        {
                            float x = (float)i / Math.Max(1, samples-1) * (w-1);
                            // quick waveform: map audio to y
                            float amp = features.Waveform.Length > 0 ? features.Waveform[i % features.Waveform.Length] : 0f;
                            float y = midY + amp * midY * 0.8f;
                            if (i>0) canvas.DrawLine(prevX, prevY, x, y, 0x00FFFF, 1.2f);
                            prevX = x; prevY = y;
                        }
                        break;
                    }
                }
            }
        }

        private void DrawSuperscope(UnifiedEffectNode node, ISkiaCanvas canvas, int w, int h)
        {
            // Pull samples (live); default 512
            var samples = GetInt(node, "samples", 512, 16, 8192);

            // Colors (optional user params; fall back to white)
            var color = ParseColor(GetString(node, "color", "#FFFFFFFF"));

            // Stroke width (nice tweak)
            var stroke = GetDouble(node, "stroke", 1.0, 0.5, 10.0);

            // NOTE: We don't interpret the Phoenix scripts here (that's a full VM).
            // For now: a clean, stable waveform/scope that shows parameter responsiveness (samples, color, stroke).
            // The editor work (import, code panes, params, compile) is fully active; rendering will plug into your VM later.

            float midY = h * 0.5f;
            float amp = h * 0.25f;
            // To avoid ISkiaCanvas.DrawPath issues on some hosts, render as line segments.
            var points = new List<(float x, float y)>();
            for (int i = 0; i < samples; i++)
            {
                float x = i / (float)Math.Max(1, samples - 1) * w;
                // Simple shape that depends on time and i; replace with VM-evaluated x/y later.
                double phase = _t * 2.0 + i * 0.02;
                float y = midY + (float)(Math.Sin(phase) * amp * 0.90 + Math.Sin(phase * 0.5) * amp * 0.10);
                points.Add((x, y));
            }

            // Draw as line segments
            canvas.DrawLines(points.ToArray(), (float)stroke, color);
        }

        private int GetInt(UnifiedEffectNode n, string key, int fallback, int min, int max)
        {
            if (ParamRegistry.TryGet(n.Id, key, out var v) && v is IConvertible)
            {
                try { return Math.Clamp(Convert.ToInt32(v), min, max); } catch { /* ignore */ }
            }
            if (n.Parameters.TryGetValue(key, out var dv) && dv is IConvertible)
            {
                try { return Math.Clamp(Convert.ToInt32(dv), min, max); } catch { /* ignore */ }
            }
            return fallback;
        }

        private double GetDouble(UnifiedEffectNode n, string key, double fallback, double min, double max)
        {
            if (ParamRegistry.TryGet(n.Id, key, out var v) && v is IConvertible)
            {
                try { return Math.Clamp(Convert.ToDouble(v), min, max); } catch { /* ignore */ }
            }
            if (n.Parameters.TryGetValue(key, out var dv) && dv is IConvertible)
            {
                try { return Math.Clamp(Convert.ToDouble(dv), min, max); } catch { /* ignore */ }
            }
            return fallback;
        }

        private string GetString(UnifiedEffectNode n, string key, string fallback)
        {
            if (ParamRegistry.TryGet(n.Id, key, out var v) && v is string s) return s;
            if (n.Parameters.TryGetValue(key, out var dv) && dv is string sd) return sd;
            return fallback;
        }

        private static uint ParseColor(string hex)
        {
            // Accept #RGB, #RRGGBB, #AARRGGBB
            if (string.IsNullOrWhiteSpace(hex)) return 0xFFFFFFFF;
            var s = hex.Trim().TrimStart('#');
            try
            {
                if (s.Length == 3)
                {
                    var r = Convert.ToInt32(new string(s[0], 2), 16);
                    var g = Convert.ToInt32(new string(s[1], 2), 16);
                    var b = Convert.ToInt32(new string(s[2], 2), 16);
                    return (uint)((255 << 24) | (r << 16) | (g << 8) | b);
                }
                if (s.Length == 6)
                {
                    var r = Convert.ToInt32(s.Substring(0, 2), 16);
                    var g = Convert.ToInt32(s.Substring(2, 2), 16);
                    var b = Convert.ToInt32(s.Substring(4, 2), 16);
                    return (uint)((255 << 24) | (r << 16) | (g << 8) | b);
                }
                if (s.Length == 8)
                {
                    var a = Convert.ToInt32(s.Substring(0, 2), 16);
                    var r = Convert.ToInt32(s.Substring(2, 2), 16);
                    var g = Convert.ToInt32(s.Substring(4, 2), 16);
                    var b = Convert.ToInt32(s.Substring(6, 2), 16);
                    return (uint)((a << 24) | (r << 16) | (g << 8) | b);
                }
            }
            catch { /* fall through */ }
            return 0xFFFFFFFF;
        }

        private static (byte r,byte g,byte b) HexToRgb(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return (255,255,255);
            hex = hex.Trim();
            if (hex.StartsWith("#")) hex = hex.Substring(1);
            if (hex.Length == 6 &&
                byte.TryParse(hex.Substring(0,2), System.Globalization.NumberStyles.HexNumber, null, out var r) &&
                byte.TryParse(hex.Substring(2,2), System.Globalization.NumberStyles.HexNumber, null, out var g) &&
                byte.TryParse(hex.Substring(4,2), System.Globalization.NumberStyles.HexNumber, null, out var b))
                return (r,g,b);
            return (255,255,255);
        }
        private static int ToInt(object? v, int dflt) => v is IConvertible ? Convert.ToInt32(v) : dflt;
        private static double ToDouble(object? v, double dflt) => v is IConvertible ? Convert.ToDouble(v) : dflt;

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
