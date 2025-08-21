using PhoenixVisualizer.Models;

namespace PhoenixVisualizer.App.Services
{
    /// <summary>
    /// Service for loading and managing MilkDrop presets
    /// </summary>
    public class MilkDropPresetLoader
    {
        private readonly string _presetDirectory;
        private readonly List<PresetInfo> _loadedPresets;
        private int _currentPresetIndex = -1;

        public event Action<PresetInfo>? OnPresetLoaded;
        public event Action<string>? OnPresetError;

        public MilkDropPresetLoader(string presetDirectory = "presets/milkdrop")
        {
            _presetDirectory = presetDirectory;
            _loadedPresets = [];
            LoadPresets();
        }

        /// <summary>
        /// Load all MilkDrop presets from the preset directory
        /// </summary>
        public void LoadPresets()
        {
            _loadedPresets.Clear();
            
            if (!Directory.Exists(_presetDirectory))
            {
                Directory.CreateDirectory(_presetDirectory);
                return;
            }

            var milkFiles = Directory.GetFiles(_presetDirectory, "*.milk", SearchOption.AllDirectories);
            
            foreach (var file in milkFiles)
            {
                try
                {
                    var preset = new PresetInfo(file, "MilkDrop");
                    _loadedPresets.Add(preset);
                }
                catch (Exception ex)
                {
                    OnPresetError?.Invoke($"Error loading preset {file}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Get the current preset
        /// </summary>
        public PresetInfo? CurrentPreset => 
            _currentPresetIndex >= 0 && _currentPresetIndex < _loadedPresets.Count 
                ? _loadedPresets[_currentPresetIndex] 
                : null;

        /// <summary>
        /// Get all loaded presets
        /// </summary>
        public IReadOnlyList<PresetInfo> LoadedPresets => _loadedPresets.AsReadOnly();

        /// <summary>
        /// Load the next preset
        /// </summary>
        public PresetInfo? LoadNextPreset()
        {
            if (_loadedPresets.Count == 0) return null;
            
            _currentPresetIndex = (_currentPresetIndex + 1) % _loadedPresets.Count;
            var preset = _loadedPresets[_currentPresetIndex];
            OnPresetLoaded?.Invoke(preset);
            return preset;
        }

        /// <summary>
        /// Load the previous preset
        /// </summary>
        public PresetInfo? LoadPreviousPreset()
        {
            if (_loadedPresets.Count == 0) return null;
            
            _currentPresetIndex = _currentPresetIndex <= 0 
                ? _loadedPresets.Count - 1 
                : _currentPresetIndex - 1;
            var preset = _loadedPresets[_currentPresetIndex];
            OnPresetLoaded?.Invoke(preset);
            return preset;
        }

        /// <summary>
        /// Load a random preset
        /// </summary>
        public PresetInfo? LoadRandomPreset()
        {
            if (_loadedPresets.Count == 0) return null;
            
            var random = new Random();
            _currentPresetIndex = random.Next(_loadedPresets.Count);
            var preset = _loadedPresets[_currentPresetIndex];
            OnPresetLoaded?.Invoke(preset);
            return preset;
        }

        /// <summary>
        /// Validate MilkDrop preset file
        /// </summary>
        public static bool ValidatePreset(PresetInfo preset)
        {
            try
            {
                var content = File.ReadAllText(preset.FilePath);
                if (string.IsNullOrEmpty(content)) return false;

                // Basic MilkDrop validation - check for common keywords
                var lowerContent = content.ToLower();
                return lowerContent.Contains("milkdrop") || 
                       lowerContent.Contains("preset") || 
                       lowerContent.Contains("code");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get preset statistics
        /// </summary>
        public (int total, int valid, int invalid) GetPresetStats()
        {
            var total = _loadedPresets.Count;
            var valid = _loadedPresets.Count(p => ValidatePreset(p));
            var invalid = total - valid;
            
            return (total, valid, invalid);
        }
    }
}
