using PhoenixVisualizer.PluginHost;
using PhoenixVisualizer.Core;

namespace PhoenixVisualizer.Visuals;

// Enhanced spectrum analyzer with customizable parameters and smooth animation 🎵📊
public sealed class SpectrumVisualizer : IVisualizerPlugin
{
    public string Id => "spectrum";
    public string DisplayName => "Spectrum Analyzer";

    private int _width;
    private int _height;
    private float _time;

    // Parameter backing fields
    private int _barCount = 64;
    private float _sensitivity = 1.0f;
    private float _decayRate = 0.95f;
    private bool _showPeaks = true;
    private float _colorShift = 0.0f;
    private float _barWidth = 0.8f;
    private bool _mirrorMode = false;

    // State for smoothing and peak detection
    private float[] _previousHeights = Array.Empty<float>();
    private float[] _peakHeights = Array.Empty<float>();
    private float[] _peakTimes = Array.Empty<float>();

    public void Initialize(int width, int height)
    {
        Resize(width, height);

        // Register global parameters
        this.RegisterGlobalParameters(Id, new[]
        {
            GlobalParameterSystem.GlobalCategory.General,
            GlobalParameterSystem.GlobalCategory.Audio,
            GlobalParameterSystem.GlobalCategory.Visual,
            GlobalParameterSystem.GlobalCategory.Motion,
            GlobalParameterSystem.GlobalCategory.Effects
        });

        // Register specific parameters
        RegisterParameters();
    }
    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;

        // Update arrays based on current bar count
        UpdateArrays();
    }

    private void RegisterParameters()
    {
        var parameters = new List<ParameterSystem.ParameterDefinition>
        {
            new ParameterSystem.ParameterDefinition
            {
                Key = "barCount",
                Label = "Bar Count",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 64,
                MinValue = 16,
                MaxValue = 128,
                Description = "Number of frequency bars to display",
                Category = "Layout"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = "sensitivity",
                Label = "Sensitivity",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 1.0f,
                MinValue = 0.1f,
                MaxValue = 3.0f,
                Description = "Audio sensitivity multiplier",
                Category = "Audio"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = "decayRate",
                Label = "Decay Rate",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.95f,
                MinValue = 0.8f,
                MaxValue = 0.99f,
                Description = "How quickly bars decay when audio is quiet",
                Category = "Animation"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = "showPeaks",
                Label = "Show Peaks",
                Type = ParameterSystem.ParameterType.Checkbox,
                DefaultValue = true,
                Description = "Show peak indicators on bars",
                Category = "Appearance"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = "colorShift",
                Label = "Color Shift",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.0f,
                MinValue = 0.0f,
                MaxValue = 360.0f,
                Description = "Base color shift in degrees (0-360)",
                Category = "Appearance"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = "barWidth",
                Label = "Bar Width",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.8f,
                MinValue = 0.1f,
                MaxValue = 1.0f,
                Description = "Width of each frequency bar as fraction of available space",
                Category = "Layout"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = "mirrorMode",
                Label = "Mirror Mode",
                Type = ParameterSystem.ParameterType.Checkbox,
                DefaultValue = false,
                Description = "Mirror bars on both sides of center",
                Category = "Layout"
            }
        };

        ParameterSystem.RegisterVisualizerParameters(Id, parameters);
    }

    private void UpdateArrays()
    {
        // Update arrays based on current bar count
        int maxBars = _barCount * (_mirrorMode ? 2 : 1);
        if (_previousHeights == null || _previousHeights.Length < maxBars)
        {
            _previousHeights = new float[maxBars];
            _peakHeights = new float[maxBars];
            _peakTimes = new float[maxBars];
        }
    }

    public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
    {
        // Update parameters from the parameter system
        UpdateParametersFromSystem();

        _time += 0.016f;

        // Dynamic background based on audio
        uint bgColor = HsvToArgb((_colorShift + features.Bass * 0.5f) % 360f, 0.1f, 0.05f);
        canvas.Clear(bgColor);

        var fft = features.Fft;
        int len = fft.Length;
        int step = Math.Max(1, len / _barCount);
        float barWidth = _width / (float)(_barCount * (_mirrorMode ? 2 : 1));
        float maxHeight = _height * 0.8f;
        float baseY = _height * 0.95f;

        int totalBars = _barCount * (_mirrorMode ? 2 : 1);

        for (int i = 0; i < _barCount; i++)
        {
            // Calculate FFT magnitude for this bar
            int start = i * step;
            int end = Math.Min(start + step, len);
            float sum = 0f;
            for (int j = start; j < end; j++) sum += MathF.Abs(fft[j]);
            float magnitude = sum / (end - start);

            // Apply sensitivity and smoothing
            float targetHeight = Math.Clamp(magnitude * _sensitivity * 15f, 0f, 1f) * maxHeight;
            float currentHeight = _previousHeights[i];
            float smoothedHeight = currentHeight * _decayRate + targetHeight * (1 - _decayRate);
            _previousHeights[i] = smoothedHeight;

            // Update peak detection
            if (_showPeaks)
            {
                if (smoothedHeight > _peakHeights[i])
                {
                    _peakHeights[i] = smoothedHeight;
                    _peakTimes[i] = _time;
                }
                else if (_time - _peakTimes[i] > 2.0f) // Peak decay time
                {
                    _peakHeights[i] *= 0.98f;
                }
            }

            // Render main bar
            float x = i * barWidth + barWidth / 2f;
            RenderSpectrumBar(canvas, x, baseY, smoothedHeight, barWidth * _barWidth,
                            GetBarColor(i, _barCount, smoothedHeight / maxHeight), i);

            // Render mirror if enabled
            if (_mirrorMode)
            {
                int mirrorIndex = i + _barCount;
                float mirrorX = (_barCount + i) * barWidth + barWidth / 2f;
                RenderSpectrumBar(canvas, mirrorX, baseY, smoothedHeight, barWidth * _barWidth,
                                GetBarColor(i, _barCount, smoothedHeight / maxHeight), mirrorIndex);
            }
        }

        // Render frequency labels if there's space
        if (_barCount <= 32 && _width > 800)
        {
            RenderFrequencyLabels(canvas);
        }
    }

    private void RenderSpectrumBar(ISkiaCanvas canvas, float x, float baseY, float height, float width, uint color, int barIndex)
    {
        // Main bar body
        canvas.FillRect(x - width / 2, baseY - height, width, height, color);

        // Add gradient effect
        uint highlightColor = AdjustBrightness(color, 1.3f);
        canvas.FillRect(x - width / 2, baseY - height, width, height * 0.3f, highlightColor);

        // Peak indicator
        if (_showPeaks && _peakHeights[barIndex] > height * 0.95f)
        {
            float peakY = baseY - _peakHeights[barIndex];
            canvas.FillRect(x - width / 2, peakY - 1, width, 2, 0xFFFFFFFF);
        }

        // Reflection effect
        uint reflectionColor = AdjustBrightness(color, 0.3f);
        canvas.FillRect(x - width / 2, baseY, width, height * 0.2f, reflectionColor);
    }

    private uint GetBarColor(int barIndex, int totalBars, float intensity)
    {
        // Create rainbow spectrum from bass to treble
        float hue = (_colorShift + (barIndex / (float)totalBars) * 270f) % 360f;
        float saturation = 0.8f + intensity * 0.2f;
        float brightness = 0.6f + intensity * 0.4f;

        return HsvToArgb(hue, saturation, brightness);
    }

    private void RenderFrequencyLabels(ISkiaCanvas canvas)
    {
        // Simple frequency labels for reference
        string[] labels = { "60Hz", "250Hz", "1K", "4K", "16K" };
        float[] positions = { 0.1f, 0.3f, 0.5f, 0.7f, 0.9f };

        for (int i = 0; i < labels.Length; i++)
        {
            float x = _width * positions[i];
            canvas.FillRect(x - 1, _height - 15, 2, 10, 0xFF888888);
            // Note: Text rendering would require additional font support
        }
    }

    private uint AdjustBrightness(uint color, float factor)
    {
        byte r = (byte)((color >> 16) & 0xFF);
        byte g = (byte)((color >> 8) & 0xFF);
        byte b = (byte)(color & 0xFF);

        r = (byte)Math.Min(255, r * factor);
        g = (byte)Math.Min(255, g * factor);
        b = (byte)Math.Min(255, b * factor);

        return (uint)(0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | (uint)b);
    }

    public void Dispose() { }

    // Tiny HSV→ARGB helper 🎨
    private static uint HsvToArgb(float h, float s, float v)
    {
        float c = v * s;
        float x = c * (1 - MathF.Abs((h / 60f % 2) - 1));
        float m = v - c;
        float r, g, b;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }
        byte R = (byte)Math.Clamp((r + m) * 255f, 0, 255);
        byte G = (byte)Math.Clamp((g + m) * 255f, 0, 255);
        byte B = (byte)Math.Clamp((b + m) * 255f, 0, 255);
        return 0xFF000000u | ((uint)R << 16) | ((uint)G << 8) | B;
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

        var oldBarCount = _barCount;
        var oldMirrorMode = _mirrorMode;

        // Update specific parameter values
        _barCount = ParameterSystem.GetParameterValue<int>(Id, "barCount", 64);
        _sensitivity = ParameterSystem.GetParameterValue<float>(Id, "sensitivity", 1.0f);
        _decayRate = ParameterSystem.GetParameterValue<float>(Id, "decayRate", 0.95f);
        _showPeaks = ParameterSystem.GetParameterValue<bool>(Id, "showPeaks", true);
        _barWidth = ParameterSystem.GetParameterValue<float>(Id, "barWidth", 0.8f);
        _mirrorMode = ParameterSystem.GetParameterValue<bool>(Id, "mirrorMode", false);

        // Apply global parameters
        _sensitivity *= GlobalParameterSystem.GetGlobalParameter<float>(Id, GlobalParameterSystem.CommonParameters.AudioSensitivity, 1.0f);
        _colorShift = ParameterSystem.GetParameterValue<float>(Id, "colorShift", 0.0f) +
                     GlobalParameterSystem.GetGlobalParameter<float>(Id, GlobalParameterSystem.CommonParameters.ColorShift, 0.0f);

        // Update arrays if bar count or mirror mode changed
        if (oldBarCount != _barCount || oldMirrorMode != _mirrorMode)
        {
            UpdateArrays();
        }
    }
}
