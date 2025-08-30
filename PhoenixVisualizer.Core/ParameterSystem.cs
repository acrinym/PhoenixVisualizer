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
/// Extension methods for parameter system integration
/// </summary>
public static class ParameterSystemExtensions
{
    /// <summary>
    /// Register common visualizer parameters
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
}
