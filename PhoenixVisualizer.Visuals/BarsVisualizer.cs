using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Core;

namespace PhoenixVisualizer.Visuals;

public sealed class BarsVisualizer : IVisualizerPlugin
{
    public string Id => "bars";
    public string DisplayName => "Simple Bars";

    private int _w, _h;

    // Parameter backing fields
    private int _maxBars = 64;
    private uint _barColor = 0xFF40C4FF;
    private uint _backgroundColor = 0xFF101010;
    private float _barThicknessBase = 0.4f;
    private float _barThicknessScale = 0.4f;
    private float _logSensitivity = 12f;
    private float _heightScale = 0.8f;
    private float _bottomMargin = 0.9f;

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
        _w = width;
        _h = height;
    }

    private void RegisterParameters()
    {
        var parameters = new List<ParameterSystem.ParameterDefinition>
        {
            new ParameterSystem.ParameterDefinition
            {
                Key = "maxBars",
                Label = "Maximum Bars",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 64,
                MinValue = 8,
                MaxValue = 128,
                Description = "Maximum number of frequency bars to display",
                Category = "Layout"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = "barColor",
                Label = "Bar Color",
                Type = ParameterSystem.ParameterType.Color,
                DefaultValue = "#40C4FF",
                Description = "Color of the frequency bars",
                Category = "Appearance"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = "backgroundColor",
                Label = "Background Color",
                Type = ParameterSystem.ParameterType.Color,
                DefaultValue = "#101010",
                Description = "Background color of the visualizer",
                Category = "Appearance"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = "barThicknessBase",
                Label = "Bar Thickness Base",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.4f,
                MinValue = 0.1f,
                MaxValue = 1.0f,
                Description = "Base thickness multiplier for bars",
                Category = "Appearance"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = "barThicknessScale",
                Label = "Bar Thickness Scale",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.4f,
                MinValue = 0.0f,
                MaxValue = 1.0f,
                Description = "How much bar thickness scales with magnitude",
                Category = "Appearance"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = "logSensitivity",
                Label = "Logarithmic Sensitivity",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 12f,
                MinValue = 1f,
                MaxValue = 50f,
                Description = "Sensitivity of logarithmic scaling for frequency magnitudes",
                Category = "Audio"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = "heightScale",
                Label = "Height Scale",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.8f,
                MinValue = 0.3f,
                MaxValue = 1.0f,
                Description = "Percentage of screen height to use for bars",
                Category = "Layout"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = "bottomMargin",
                Label = "Bottom Margin",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.9f,
                MinValue = 0.7f,
                MaxValue = 1.0f,
                Description = "Bottom margin as percentage of screen height",
                Category = "Layout"
            }
        };

        ParameterSystem.RegisterVisualizerParameters(Id, parameters);
    }

    public void RenderFrame(AudioFeatures f, ISkiaCanvas canvas)
    {
        // Update parameters from the parameter system
        UpdateParametersFromSystem();

        canvas.Clear(_backgroundColor);

        // Debug: Log what we're receiving
        float debugFftSum = f.Fft?.Sum(ff => MathF.Abs(ff)) ?? 0f;
        float debugWaveSum = f.Waveform?.Sum(w => MathF.Abs(w)) ?? 0f;
        System.Diagnostics.Debug.WriteLine($"[BarsVisualizer] Received: FFT sum: {debugFftSum:F6}, Wave sum: {debugWaveSum:F6}, RMS: {f.Rms:F6}, Beat: {f.Beat}");

        if (f.Fft is null || f.Fft.Length == 0) return;

        // Validate FFT data - check if it's stuck
        float fftSum = 0f;
        float fftMax = 0f;
        int fftNonZero = 0;

        for (int i = 0; i < f.Fft.Length; i++)
        {
            float absVal = MathF.Abs(f.Fft[i]);
            fftSum += absVal;
            if (absVal > fftMax) fftMax = absVal;
            if (absVal > 0.001f) fftNonZero++;
        }

        // If FFT data appears stuck, use a fallback pattern
        if (fftSum < 0.001f || fftMax < 0.001f || fftNonZero < 10)
        {
            // Generate a simple animated pattern instead of stuck data
            var time = DateTime.Now.Ticks / 10000000.0; // Current time in seconds
            for (int i = 0; i < f.Fft.Length; i++)
            {
                f.Fft[i] = MathF.Sin((float)(time * 2.0 + i * 0.1)) * 0.3f;
            }
        }

        int n = Math.Min(_maxBars, f.Fft.Length);
        float barW = Math.Max(1f, (float)_w / n);
        Span<(float x, float y)> seg = stackalloc (float, float)[2];

        for (int i = 0; i < n; i++)
        {
            // Proper FFT magnitude calculation (handle negative values correctly)
            float v = MathF.Abs(f.Fft[i]);

            // Improved logarithmic scaling with configurable sensitivity
            float mag = MathF.Min(1f, MathF.Log(1 + _logSensitivity * v) / MathF.Log(_logSensitivity + 1));

            // Scale height with configurable screen usage
            float h = mag * (_h * _heightScale);

            // Calculate bar position with configurable bottom margin
            float x = i * barW;
            float barCenterX = x + barW * 0.5f;
            float barBottomY = _h * _bottomMargin;
            float barTopY = barBottomY - h;

            // Ensure bars don't go off-screen
            barTopY = MathF.Max(0, barTopY);

            seg[0] = (barCenterX, barBottomY);
            seg[1] = (barCenterX, barTopY);

            // Dynamic bar thickness based on configurable parameters
            float thickness = MathF.Max(1f, barW * (_barThicknessBase + mag * _barThicknessScale));
            canvas.DrawLines(seg, thickness, _barColor);
        }
    }

    private void UpdateParametersFromSystem()
    {
        // Update global parameter values
        var globalEnabled = GlobalParameterSystem.GetGlobalParameter<bool>(Id, GlobalParameterSystem.CommonParameters.Enabled, true);
        if (!globalEnabled) return; // Early exit if disabled

        var globalOpacity = GlobalParameterSystem.GetGlobalParameter<float>(Id, GlobalParameterSystem.CommonParameters.Opacity, 1.0f);
        var globalBrightness = GlobalParameterSystem.GetGlobalParameter<float>(Id, GlobalParameterSystem.CommonParameters.Brightness, 1.0f);
        var globalScale = GlobalParameterSystem.GetGlobalParameter<float>(Id, GlobalParameterSystem.CommonParameters.Scale, 1.0f);
        var globalSpeed = GlobalParameterSystem.GetGlobalParameter<float>(Id, GlobalParameterSystem.CommonParameters.Speed, 1.0f);

        // Update specific parameter values
        _maxBars = ParameterSystem.GetParameterValue<int>(Id, "maxBars", 64);
        _barThicknessBase = ParameterSystem.GetParameterValue<float>(Id, "barThicknessBase", 0.4f);
        _barThicknessScale = ParameterSystem.GetParameterValue<float>(Id, "barThicknessScale", 0.4f);
        _logSensitivity = ParameterSystem.GetParameterValue<float>(Id, "logSensitivity", 12f);
        _heightScale = ParameterSystem.GetParameterValue<float>(Id, "heightScale", 0.8f) * globalScale;
        _bottomMargin = ParameterSystem.GetParameterValue<float>(Id, "bottomMargin", 0.9f);

        // Apply global parameters to colors
        var baseBarColor = ParameterSystem.GetParameterValue<string>(Id, "barColor", "#40C4FF") ?? "#40C4FF";
        var baseBgColor = ParameterSystem.GetParameterValue<string>(Id, "backgroundColor", "#101010") ?? "#101010";

        _barColor = ApplyGlobalEffects(ColorFromHex(baseBarColor), globalBrightness, globalOpacity);
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
            return 0xFF40C4FF; // Default blue

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

        return 0xFF40C4FF; // Default blue
    }

    public void Dispose() { }
}
