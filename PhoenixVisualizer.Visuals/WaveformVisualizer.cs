using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Core;

namespace PhoenixVisualizer.Visuals;

// Time-domain waveform visualizer 🩵
public sealed class WaveformVisualizer : IVisualizerPlugin
{
    public string Id => "waveform";
    public string DisplayName => "Waveform";

    private int _width;
    private int _height;

    // Parameter backing fields
    private uint _waveformColor = 0xFF00FF00;
    private uint _backgroundColor = 0xFF000000;
    private float _lineThickness = 1.5f;
    private float _amplitudeScale = 0.4f;
    private float _centerPosition = 0.5f;

    public void Initialize(int width, int height)
    {
        Resize(width, height);

        // Register global parameters
        this.RegisterGlobalParameters(Id, new[]
        {
            GlobalParameterSystem.GlobalCategory.General,
            GlobalParameterSystem.GlobalCategory.Audio,
            GlobalParameterSystem.GlobalCategory.Visual,
            GlobalParameterSystem.GlobalCategory.Motion
        });

        // Register specific parameters
        RegisterParameters();
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    private void RegisterParameters()
    {
        var parameters = new List<ParameterSystem.ParameterDefinition>
        {
            new ParameterSystem.ParameterDefinition
            {
                Key = "waveformColor",
                Label = "Waveform Color",
                Type = ParameterSystem.ParameterType.Color,
                DefaultValue = "#00FF00",
                Description = "Color of the waveform line",
                Category = "Appearance"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = "backgroundColor",
                Label = "Background Color",
                Type = ParameterSystem.ParameterType.Color,
                DefaultValue = "#000000",
                Description = "Background color of the visualizer",
                Category = "Appearance"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = "lineThickness",
                Label = "Line Thickness",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 1.5f,
                MinValue = 0.5f,
                MaxValue = 5.0f,
                Description = "Thickness of the waveform line",
                Category = "Appearance"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = "amplitudeScale",
                Label = "Amplitude Scale",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.4f,
                MinValue = 0.1f,
                MaxValue = 1.0f,
                Description = "How much of the screen height to use for waveform amplitude",
                Category = "Layout"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = "centerPosition",
                Label = "Center Position",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.5f,
                MinValue = 0.1f,
                MaxValue = 0.9f,
                Description = "Vertical position of the waveform center (0.5 = middle)",
                Category = "Layout"
            }
        };

        ParameterSystem.RegisterVisualizerParameters(Id, parameters);
    }

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        // Update parameters from the parameter system
        UpdateParametersFromSystem();

        canvas.Clear(_backgroundColor);
        var wave = features.Waveform;
        if (wave.Length < 2) return;
        int len = wave.Length;
        Span<(float x, float y)> pts = stackalloc (float x, float y)[len];
        for (int i = 0; i < len; i++)
        {
            // Proper normalization from 0 to 1
            float nx = len > 1 ? (float)i / (len - 1) : 0f;

            // Convert to screen coordinates
            float x = nx * (_width - 1);

            // Configurable waveform scaling with center baseline
            float centerY = _height * _centerPosition;
            float amplitude = _height * _amplitudeScale;
            float y = centerY - wave[i] * amplitude;

            // Clamp to prevent drawing outside screen bounds
            y = MathF.Max(0, MathF.Min(_height - 1, y));

            pts[i] = (x, y);
        }
        canvas.DrawLines(pts, _lineThickness, _waveformColor);
    }

    private void UpdateParametersFromSystem()
    {
        // Update global parameter values
        var globalEnabled = GlobalParameterSystem.GetGlobalParameter<bool>(Id, GlobalParameterSystem.CommonParameters.Enabled, true);
        if (!globalEnabled) return; // Early exit if disabled

        var globalOpacity = GlobalParameterSystem.GetGlobalParameter<float>(Id, GlobalParameterSystem.CommonParameters.Opacity, 1.0f);
        var globalBrightness = GlobalParameterSystem.GetGlobalParameter<float>(Id, GlobalParameterSystem.CommonParameters.Brightness, 1.0f);
        var globalScale = GlobalParameterSystem.GetGlobalParameter<float>(Id, GlobalParameterSystem.CommonParameters.Scale, 1.0f);

        // Update specific parameter values
        _lineThickness = ParameterSystem.GetParameterValue<float>(Id, "lineThickness", 1.5f) * globalScale;
        _amplitudeScale = ParameterSystem.GetParameterValue<float>(Id, "amplitudeScale", 0.4f) * globalScale;
        _centerPosition = ParameterSystem.GetParameterValue<float>(Id, "centerPosition", 0.5f);

        // Apply global parameters to colors
        var baseWaveformColor = ParameterSystem.GetParameterValue<string>(Id, "waveformColor", "#00FF00") ?? "#00FF00";
        var baseBgColor = ParameterSystem.GetParameterValue<string>(Id, "backgroundColor", "#000000") ?? "#000000";

        _waveformColor = ApplyGlobalEffects(ColorFromHex(baseWaveformColor), globalBrightness, globalOpacity);
        _backgroundColor = ApplyGlobalEffects(ColorFromHex(baseBgColor), globalBrightness, globalOpacity);
    }

    private uint ApplyGlobalEffects(uint color, float brightness, float opacity)
    {
        // Extract RGBA components
        var a = ((color >> 24) & 0xFF) / 255.0f;
        var r = ((color >> 16) & 0xFF) / 255.0f;
        var g = ((color >> 8) & 0xFF) / 255.0f;
        var b = (color & 0xFF) / 255.0f;

        // Apply brightness
        r = Math.Clamp(r * brightness, 0, 1);
        g = Math.Clamp(g * brightness, 0, 1);
        b = Math.Clamp(b * brightness, 0, 1);

        // Apply opacity
        a *= opacity;

        // Convert back to uint
        return ((uint)(a * 255) << 24) |
               ((uint)(r * 255) << 16) |
               ((uint)(g * 255) << 8) |
               (uint)(b * 255);
    }

    private uint ColorFromHex(string hexColor)
    {
        if (string.IsNullOrEmpty(hexColor) || !hexColor.StartsWith("#"))
            return 0xFF00FF00; // Default green

        try
        {
            // Remove # and parse
            var hex = hexColor.Substring(1);
            if (hex.Length == 6)
            {
                // RGB format
                var r = Convert.ToByte(hex.Substring(0, 2), 16);
                var g = Convert.ToByte(hex.Substring(2, 2), 16);
                var b = Convert.ToByte(hex.Substring(4, 2), 16);
                return (uint)((255 << 24) | (r << 16) | (g << 8) | b);
            }
            else if (hex.Length == 8)
            {
                // ARGB format
                var a = Convert.ToByte(hex.Substring(0, 2), 16);
                var r = Convert.ToByte(hex.Substring(2, 2), 16);
                var g = Convert.ToByte(hex.Substring(4, 2), 16);
                var b = Convert.ToByte(hex.Substring(6, 2), 16);
                return (uint)((a << 24) | (r << 16) | (g << 8) | b);
            }
        }
        catch
        {
            // Fall back to default on parsing error
        }

        return 0xFF00FF00; // Default green
    }

    public void Dispose() { }
}
