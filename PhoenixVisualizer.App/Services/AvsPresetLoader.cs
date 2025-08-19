using System.Text;

using PhoenixVisualizer.Models;

namespace PhoenixVisualizer.Services
{
    /// <summary>
    /// Service for loading and managing AVS presets with Winamp compatibility
    /// </summary>
    public class AvsPresetLoader
    {
        private readonly string _presetDirectory;
        private readonly List<PresetInfo> _loadedPresets;
        private int _currentPresetIndex = -1;

        public event Action<PresetInfo>? OnPresetLoaded;
        public event Action<string>? OnPresetError;

        public AvsPresetLoader(string presetDirectory = "presets/avs")
        {
            _presetDirectory = presetDirectory;
            _loadedPresets = new List<PresetInfo>();
            LoadPresets();
        }

        /// <summary>
        /// Load all AVS presets from the preset directory
        /// </summary>
        public void LoadPresets()
        {
            _loadedPresets.Clear();
            
            if (!Directory.Exists(_presetDirectory))
            {
                Directory.CreateDirectory(_presetDirectory);
                return;
            }

            var avsFiles = Directory.GetFiles(_presetDirectory, "*.avs", SearchOption.AllDirectories);
            
            foreach (var file in avsFiles)
            {
                try
                {
                    var preset = new PresetInfo(file, "AVS");
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
        /// Load a specific preset by index
        /// </summary>
        public PresetInfo? LoadPresetByIndex(int index)
        {
            if (index < 0 || index >= _loadedPresets.Count) return null;
            
            _currentPresetIndex = index;
            var preset = _loadedPresets[_currentPresetIndex];
            OnPresetLoaded?.Invoke(preset);
            return preset;
        }

        /// <summary>
        /// Load a preset by name
        /// </summary>
        public PresetInfo? LoadPresetByName(string name)
        {
            var preset = _loadedPresets.FirstOrDefault(p => 
                string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            
            if (preset != null)
            {
                _currentPresetIndex = _loadedPresets.IndexOf(preset);
                OnPresetLoaded?.Invoke(preset);
            }
            
            return preset;
        }

        /// <summary>
        /// Get preset content as string
        /// </summary>
        public string? GetPresetContent(PresetInfo preset)
        {
            try
            {
                return File.ReadAllText(preset.FilePath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                OnPresetError?.Invoke($"Error reading preset {preset.Name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Validate AVS preset syntax
        /// </summary>
        public bool ValidatePreset(PresetInfo preset)
        {
            try
            {
                var content = GetPresetContent(preset);
                if (string.IsNullOrEmpty(content)) return false;

                // Basic AVS syntax validation
                var lines = content.Split('\n', '\r');
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//")) continue;
                    
                    // Check for basic AVS functions
                    if (trimmed.Contains("=") || trimmed.Contains("(") || trimmed.Contains(")"))
                        continue;
                    
                    // If we find an unrecognized line, mark as potentially invalid
                    if (!trimmed.StartsWith("Set") && !trimmed.StartsWith("Init"))
                        return false;
                }
                
                return true;
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
