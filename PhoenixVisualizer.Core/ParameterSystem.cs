using System;
using System.Collections.Generic;
using System.Text.Json;

namespace PhoenixVisualizer.Core;

/// <summary>
/// Comprehensive parameter system for user-editable visualizer settings
/// Supports sliders, checkboxes, dropdowns, color pickers, and dials
/// </summary>
public static class ParameterSystem
{
    /// <summary>
    /// Parameter types supported by the system
    /// </summary>
    public enum ParameterType
    {
        Slider,
        Checkbox,
        Dropdown,
        Color,
        Dial,
        Text,
        File,
        Directory
    }

    /// <summary>
    /// User-editable parameter definition
    /// </summary>
    public class ParameterDefinition
    {
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        public ParameterType Type { get; set; }
        public object DefaultValue { get; set; } = null!;
        public object MinValue { get; set; } = null!;
        public object MaxValue { get; set; } = null!;
        public List<string> Options { get; set; } = new();
        public string Description { get; set; } = "";
        public string Category { get; set; } = "General";
        public bool RequiresRestart { get; set; } = false;
    }

    /// <summary>
    /// Parameter value container
    /// </summary>
    public class ParameterValue
    {
        public object Value { get; set; } = null!;
        public DateTime LastModified { get; set; } = DateTime.Now;
        public bool IsModified { get; set; } = false;
    }

    private static readonly Dictionary<string, Dictionary<string, ParameterDefinition>> _visualizerParameters = new();
    private static readonly Dictionary<string, Dictionary<string, ParameterValue>> _parameterValues = new();

    /// <summary>
    /// Register parameters for a visualizer
    /// </summary>
    public static void RegisterVisualizerParameters(string visualizerId, List<ParameterDefinition> parameters)
    {
        if (!_visualizerParameters.ContainsKey(visualizerId))
        {
            _visualizerParameters[visualizerId] = new Dictionary<string, ParameterDefinition>();
            _parameterValues[visualizerId] = new Dictionary<string, ParameterValue>();
        }

        foreach (var param in parameters)
        {
            _visualizerParameters[visualizerId][param.Key] = param;
            _parameterValues[visualizerId][param.Key] = new ParameterValue
            {
                Value = param.DefaultValue,
                IsModified = false
            };
        }
    }

    /// <summary>
    /// Get parameter value for a visualizer
    /// </summary>
    public static T GetParameterValue<T>(string visualizerId, string parameterKey, T defaultValue = default!)
    {
        if (_parameterValues.ContainsKey(visualizerId) &&
            _parameterValues[visualizerId].ContainsKey(parameterKey))
        {
            var value = _parameterValues[visualizerId][parameterKey].Value;
            if (value is T typedValue)
                return typedValue;
        }

        return defaultValue;
    }

    /// <summary>
    /// Set parameter value for a visualizer
    /// </summary>
    public static void SetParameterValue(string visualizerId, string parameterKey, object value)
    {
        if (_parameterValues.ContainsKey(visualizerId) &&
            _parameterValues[visualizerId].ContainsKey(parameterKey))
        {
            _parameterValues[visualizerId][parameterKey].Value = value;
            _parameterValues[visualizerId][parameterKey].LastModified = DateTime.Now;
            _parameterValues[visualizerId][parameterKey].IsModified = true;
        }
    }

    /// <summary>
    /// Get all parameters for a visualizer
    /// </summary>
    public static Dictionary<string, ParameterDefinition> GetVisualizerParameters(string visualizerId)
    {
        return _visualizerParameters.ContainsKey(visualizerId)
            ? new Dictionary<string, ParameterDefinition>(_visualizerParameters[visualizerId])
            : new Dictionary<string, ParameterDefinition>();
    }

    /// <summary>
    /// Get all parameter values for a visualizer
    /// </summary>
    public static Dictionary<string, ParameterValue> GetVisualizerParameterValues(string visualizerId)
    {
        return _parameterValues.ContainsKey(visualizerId)
            ? new Dictionary<string, ParameterValue>(_parameterValues[visualizerId])
            : new Dictionary<string, ParameterValue>();
    }

    /// <summary>
    /// Reset parameter to default value
    /// </summary>
    public static void ResetParameterToDefault(string visualizerId, string parameterKey)
    {
        if (_visualizerParameters.ContainsKey(visualizerId) &&
            _visualizerParameters[visualizerId].ContainsKey(parameterKey) &&
            _parameterValues.ContainsKey(visualizerId) &&
            _parameterValues[visualizerId].ContainsKey(parameterKey))
        {
            var defaultValue = _visualizerParameters[visualizerId][parameterKey].DefaultValue;
            _parameterValues[visualizerId][parameterKey].Value = defaultValue;
            _parameterValues[visualizerId][parameterKey].LastModified = DateTime.Now;
            _parameterValues[visualizerId][parameterKey].IsModified = false;
        }
    }

    /// <summary>
    /// Reset all parameters to defaults for a visualizer
    /// </summary>
    public static void ResetAllParametersToDefaults(string visualizerId)
    {
        if (_visualizerParameters.ContainsKey(visualizerId) &&
            _parameterValues.ContainsKey(visualizerId))
        {
            foreach (var param in _visualizerParameters[visualizerId])
            {
                _parameterValues[visualizerId][param.Key].Value = param.Value.DefaultValue;
                _parameterValues[visualizerId][param.Key].LastModified = DateTime.Now;
                _parameterValues[visualizerId][param.Key].IsModified = false;
            }
        }
    }

    /// <summary>
    /// Save parameters to JSON file
    /// </summary>
    public static void SaveParametersToFile(string visualizerId, string filePath)
    {
        if (_parameterValues.ContainsKey(visualizerId))
        {
            var data = new
            {
                VisualizerId = visualizerId,
                Parameters = _parameterValues[visualizerId],
                SavedAt = DateTime.Now
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(filePath, json);
        }
    }

    /// <summary>
    /// Load parameters from JSON file
    /// </summary>
    public static void LoadParametersFromFile(string visualizerId, string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (data != null && data.ContainsKey("Parameters"))
                {
                    var parameters = JsonSerializer.Deserialize<Dictionary<string, ParameterValue>>(
                        data["Parameters"].ToString() ?? "{}");

                    if (parameters != null)
                    {
                        if (!_parameterValues.ContainsKey(visualizerId))
                            _parameterValues[visualizerId] = new Dictionary<string, ParameterValue>();

                        foreach (var param in parameters)
                        {
                            _parameterValues[visualizerId][param.Key] = param.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading parameters from {filePath}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Create parameter preset
    /// </summary>
    public static void CreateParameterPreset(string visualizerId, string presetName, string presetFilePath)
    {
        if (_parameterValues.ContainsKey(visualizerId))
        {
            var preset = new
            {
                PresetName = presetName,
                VisualizerId = visualizerId,
                Parameters = _parameterValues[visualizerId],
                CreatedAt = DateTime.Now
            };

            var json = JsonSerializer.Serialize(preset, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(presetFilePath, json);
        }
    }

    /// <summary>
    /// Load parameter preset
    /// </summary>
    public static void LoadParameterPreset(string visualizerId, string presetFilePath)
    {
        if (File.Exists(presetFilePath))
        {
            try
            {
                var json = File.ReadAllText(presetFilePath);
                var preset = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (preset != null && preset.ContainsKey("Parameters"))
                {
                    var parameters = JsonSerializer.Deserialize<Dictionary<string, ParameterValue>>(
                        preset["Parameters"].ToString() ?? "{}");

                    if (parameters != null)
                    {
                        if (!_parameterValues.ContainsKey(visualizerId))
                            _parameterValues[visualizerId] = new Dictionary<string, ParameterValue>();

                        foreach (var param in parameters)
                        {
                            _parameterValues[visualizerId][param.Key] = param.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading preset from {presetFilePath}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Get parameter presets directory for a visualizer
    /// </summary>
    public static string GetVisualizerPresetsDirectory(string visualizerId)
    {
        var presetsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PhoenixVisualizer",
            "Presets",
            visualizerId
        );

        Directory.CreateDirectory(presetsDir);
        return presetsDir;
    }
}

/// <summary>
/// Global parameter system for universal visualizer controls
/// </summary>
public static class GlobalParameterSystem
{
    /// <summary>
    /// Global parameter categories
    /// </summary>
    public enum GlobalCategory
    {
        General,
        Audio,
        Visual,
        Motion,
        Effects
    }

    /// <summary>
    /// Universal visualizer parameters that most visualizers can use
    /// </summary>
    public static class CommonParameters
    {
        // General Parameters
        public const string Enabled = "global_enabled";
        public const string Opacity = "global_opacity";
        public const string Brightness = "global_brightness";
        public const string Saturation = "global_saturation";
        public const string Contrast = "global_contrast";

        // Audio Parameters
        public const string AudioSensitivity = "global_audio_sensitivity";
        public const string BassMultiplier = "global_bass_multiplier";
        public const string MidMultiplier = "global_mid_multiplier";
        public const string TrebleMultiplier = "global_treble_multiplier";
        public const string BeatThreshold = "global_beat_threshold";

        // Visual Parameters
        public const string Scale = "global_scale";
        public const string Blur = "global_blur";
        public const string Glow = "global_glow";
        public const string ColorShift = "global_color_shift";
        public const string ColorSpeed = "global_color_speed";

        // Motion Parameters
        public const string Speed = "global_speed";
        public const string Rotation = "global_rotation";
        public const string PositionX = "global_position_x";
        public const string PositionY = "global_position_y";
        public const string Bounce = "global_bounce";

        // Effect Parameters
        public const string TrailLength = "global_trail_length";
        public const string Decay = "global_decay";
        public const string ParticleCount = "global_particle_count";
        public const string Waveform = "global_waveform";
        public const string Mirror = "global_mirror";
    }

    /// <summary>
    /// Register global parameters for a visualizer
    /// </summary>
    public static void RegisterGlobalParameters(string visualizerId, GlobalCategory[] categories = null)
    {
        categories ??= Enum.GetValues<GlobalCategory>();

        var parameters = new List<ParameterSystem.ParameterDefinition>();

        foreach (var category in categories)
        {
            switch (category)
            {
                case GlobalCategory.General:
                    parameters.AddRange(CreateGeneralParameters());
                    break;
                case GlobalCategory.Audio:
                    parameters.AddRange(CreateAudioParameters());
                    break;
                case GlobalCategory.Visual:
                    parameters.AddRange(CreateVisualParameters());
                    break;
                case GlobalCategory.Motion:
                    parameters.AddRange(CreateMotionParameters());
                    break;
                case GlobalCategory.Effects:
                    parameters.AddRange(CreateEffectParameters());
                    break;
            }
        }

        ParameterSystem.RegisterVisualizerParameters(visualizerId, parameters);
    }

    private static List<ParameterSystem.ParameterDefinition> CreateGeneralParameters()
    {
        return new List<ParameterSystem.ParameterDefinition>
        {
            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.Enabled,
                Label = "Enabled",
                Type = ParameterSystem.ParameterType.Checkbox,
                DefaultValue = true,
                Description = "Enable or disable this visualizer",
                Category = "Global - General"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.Opacity,
                Label = "Opacity",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 1.0f,
                MinValue = 0.0f,
                MaxValue = 1.0f,
                Description = "Overall opacity multiplier",
                Category = "Global - General"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.Brightness,
                Label = "Brightness",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 1.0f,
                MinValue = 0.0f,
                MaxValue = 3.0f,
                Description = "Brightness multiplier",
                Category = "Global - General"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.Saturation,
                Label = "Saturation",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 1.0f,
                MinValue = 0.0f,
                MaxValue = 2.0f,
                Description = "Color saturation multiplier",
                Category = "Global - General"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.Contrast,
                Label = "Contrast",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 1.0f,
                MinValue = 0.1f,
                MaxValue = 3.0f,
                Description = "Color contrast multiplier",
                Category = "Global - General"
            }
        };
    }

    private static List<ParameterSystem.ParameterDefinition> CreateAudioParameters()
    {
        return new List<ParameterSystem.ParameterDefinition>
        {
            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.AudioSensitivity,
                Label = "Audio Sensitivity",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 1.0f,
                MinValue = 0.1f,
                MaxValue = 5.0f,
                Description = "Overall audio responsiveness multiplier",
                Category = "Global - Audio"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.BassMultiplier,
                Label = "Bass Multiplier",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 1.0f,
                MinValue = 0.0f,
                MaxValue = 3.0f,
                Description = "Low frequency response multiplier",
                Category = "Global - Audio"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.MidMultiplier,
                Label = "Mid Multiplier",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 1.0f,
                MinValue = 0.0f,
                MaxValue = 3.0f,
                Description = "Mid frequency response multiplier",
                Category = "Global - Audio"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.TrebleMultiplier,
                Label = "Treble Multiplier",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 1.0f,
                MinValue = 0.0f,
                MaxValue = 3.0f,
                Description = "High frequency response multiplier",
                Category = "Global - Audio"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.BeatThreshold,
                Label = "Beat Threshold",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.3f,
                MinValue = 0.1f,
                MaxValue = 1.0f,
                Description = "Minimum audio level to trigger beat detection",
                Category = "Global - Audio"
            }
        };
    }

    private static List<ParameterSystem.ParameterDefinition> CreateVisualParameters()
    {
        return new List<ParameterSystem.ParameterDefinition>
        {
            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.Scale,
                Label = "Scale",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 1.0f,
                MinValue = 0.1f,
                MaxValue = 3.0f,
                Description = "Overall size scaling multiplier",
                Category = "Global - Visual"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.Blur,
                Label = "Blur Amount",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.0f,
                MinValue = 0.0f,
                MaxValue = 10.0f,
                Description = "Gaussian blur intensity",
                Category = "Global - Visual"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.Glow,
                Label = "Glow Intensity",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.0f,
                MinValue = 0.0f,
                MaxValue = 5.0f,
                Description = "Glow effect intensity",
                Category = "Global - Visual"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.ColorShift,
                Label = "Color Shift",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.0f,
                MinValue = 0.0f,
                MaxValue = 360.0f,
                Description = "Hue shift in degrees",
                Category = "Global - Visual"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.ColorSpeed,
                Label = "Color Speed",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.0f,
                MinValue = -5.0f,
                MaxValue = 5.0f,
                Description = "Automatic color cycling speed",
                Category = "Global - Visual"
            }
        };
    }

    private static List<ParameterSystem.ParameterDefinition> CreateMotionParameters()
    {
        return new List<ParameterSystem.ParameterDefinition>
        {
            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.Speed,
                Label = "Animation Speed",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 1.0f,
                MinValue = -2.0f,
                MaxValue = 5.0f,
                Description = "Animation speed multiplier (negative = reverse)",
                Category = "Global - Motion"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.Rotation,
                Label = "Rotation Speed",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.0f,
                MinValue = -5.0f,
                MaxValue = 5.0f,
                Description = "Rotation speed in degrees per second",
                Category = "Global - Motion"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.PositionX,
                Label = "Position X",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.0f,
                MinValue = -1.0f,
                MaxValue = 1.0f,
                Description = "Horizontal position offset (as fraction of screen)",
                Category = "Global - Motion"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.PositionY,
                Label = "Position Y",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.0f,
                MinValue = -1.0f,
                MaxValue = 1.0f,
                Description = "Vertical position offset (as fraction of screen)",
                Category = "Global - Motion"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.Bounce,
                Label = "Bounce Factor",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.0f,
                MinValue = 0.0f,
                MaxValue = 1.0f,
                Description = "How bouncy/elastic the motion is",
                Category = "Global - Motion"
            }
        };
    }

    private static List<ParameterSystem.ParameterDefinition> CreateEffectParameters()
    {
        return new List<ParameterSystem.ParameterDefinition>
        {
            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.TrailLength,
                Label = "Trail Length",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.0f,
                MinValue = 0.0f,
                MaxValue = 1.0f,
                Description = "Length of motion trails (0 = no trails)",
                Category = "Global - Effects"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.Decay,
                Label = "Decay Rate",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0.95f,
                MinValue = 0.8f,
                MaxValue = 0.99f,
                Description = "How quickly elements fade/decay",
                Category = "Global - Effects"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.ParticleCount,
                Label = "Particle Count",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 0,
                MinValue = 0,
                MaxValue = 1000,
                Description = "Number of particles/elements to render",
                Category = "Global - Effects"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.Waveform,
                Label = "Waveform Mode",
                Type = ParameterSystem.ParameterType.Dropdown,
                DefaultValue = "Normal",
                Options = new List<string> { "Normal", "Circular", "Spiral", "Random" },
                Description = "How elements are arranged/distributed",
                Category = "Global - Effects"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = CommonParameters.Mirror,
                Label = "Mirror Mode",
                Type = ParameterSystem.ParameterType.Dropdown,
                DefaultValue = "None",
                Options = new List<string> { "None", "Horizontal", "Vertical", "Both", "Radial" },
                Description = "Mirroring/symmetry mode",
                Category = "Global - Effects"
            }
        };
    }

    /// <summary>
    /// Get the current global parameter value for a visualizer
    /// </summary>
    public static T GetGlobalParameter<T>(string visualizerId, string parameterKey, T defaultValue = default!)
    {
        return ParameterSystem.GetParameterValue<T>(visualizerId, parameterKey, defaultValue);
    }

    /// <summary>
    /// Set a global parameter value for a visualizer
    /// </summary>
    public static void SetGlobalParameter(string visualizerId, string parameterKey, object value)
    {
        ParameterSystem.SetParameterValue(visualizerId, parameterKey, value);
    }
}

/// <summary>
/// Extension methods for parameter system integration
/// </summary>
public static class ParameterSystemExtensions
{
    /// <summary>
    /// Register common visualizer parameters (legacy method)
    /// </summary>
    public static void RegisterCommonParameters(this object visualizer, string visualizerId)
    {
        var parameters = new List<ParameterSystem.ParameterDefinition>
        {
            new ParameterSystem.ParameterDefinition
            {
                Key = "enabled",
                Label = "Enabled",
                Type = ParameterSystem.ParameterType.Checkbox,
                DefaultValue = true,
                Description = "Enable or disable this visualizer",
                Category = "General"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = "opacity",
                Label = "Opacity",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 1.0f,
                MinValue = 0.0f,
                MaxValue = 1.0f,
                Description = "Overall opacity of the visualizer",
                Category = "Appearance"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = "brightness",
                Label = "Brightness",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 1.0f,
                MinValue = 0.0f,
                MaxValue = 2.0f,
                Description = "Brightness adjustment",
                Category = "Appearance"
            },

            new ParameterSystem.ParameterDefinition
            {
                Key = "sensitivity",
                Label = "Audio Sensitivity",
                Type = ParameterSystem.ParameterType.Slider,
                DefaultValue = 1.0f,
                MinValue = 0.1f,
                MaxValue = 3.0f,
                Description = "How responsive the visualizer is to audio",
                Category = "Audio"
            }
        };

        ParameterSystem.RegisterVisualizerParameters(visualizerId, parameters);
    }

    /// <summary>
    /// Register global parameters for a visualizer with specific categories
    /// </summary>
    public static void RegisterGlobalParameters(this object visualizer, string visualizerId,
        GlobalParameterSystem.GlobalCategory[] categories = null)
    {
        GlobalParameterSystem.RegisterGlobalParameters(visualizerId, categories);
    }

    /// <summary>
    /// Get a global parameter value
    /// </summary>
    public static T GetGlobalParameter<T>(this object visualizer, string visualizerId, string parameterKey, T defaultValue = default!)
    {
        return GlobalParameterSystem.GetGlobalParameter<T>(visualizerId, parameterKey, defaultValue);
    }
}
