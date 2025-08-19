using PhoenixVisualizer.Core.Config;
using System.Text.Json;

namespace PhoenixVisualizer.Services
{
    /// <summary>
    /// Enhanced service for handling Winamp-style hotkeys in the visualizer with customization support
    /// </summary>
    public class WinampHotkeyService
    {
        private readonly Dictionary<KeyGesture, Action> _hotkeyActions;
        private readonly VisualizerSettings _settings;
        private readonly string _hotkeyConfigPath;
        private bool _isEnabled = true;
        private Dictionary<string, KeyGesture> _customBindings;

        public event Action? OnNextPreset;
        public event Action? OnPreviousPreset;
        public event Action? OnRandomPreset;
        public event Action? OnToggleFullscreen;
        public event Action? OnToggleVisualizer;
        public event Action? OnToggleBeatDetection;
        public event Action? OnToggleRandomMode;
        public event Action? OnTogglePluginBrowser;
        public event Action? OnTogglePresetManager;
        public event Action? OnToggleSettings;
        public event Action? OnQuickSavePreset;
        public event Action? OnQuickLoadPreset;
        public event Action? OnToggleAudioSource;
        public event Action? OnToggleSpectrumMode;
        public event Action? OnToggleWaveformMode;
        public event Action? OnToggleOscilloscopeMode;

        public WinampHotkeyService(VisualizerSettings settings)
        {
            _settings = settings;
            _hotkeyActions = new Dictionary<KeyGesture, Action>();
            _customBindings = new Dictionary<string, KeyGesture>();
            
            // Set config path for custom hotkey bindings
            _hotkeyConfigPath = Path.Combine(
                AppContext.BaseDirectory, 
                "config", 
                "hotkeys.json"
            );
            
            InitializeDefaultHotkeys();
            LoadCustomBindings();
        }

        private void InitializeDefaultHotkeys()
        {
            // Clear existing hotkeys
            _hotkeyActions.Clear();

            // Core navigation hotkeys
            _hotkeyActions[new KeyGesture(Key.Y)] = () => OnNextPreset?.Invoke();
            _hotkeyActions[new KeyGesture(Key.U)] = () => OnPreviousPreset?.Invoke();
            _hotkeyActions[new KeyGesture(Key.Space)] = () => OnRandomPreset?.Invoke();
            
            // Display and mode hotkeys
            _hotkeyActions[new KeyGesture(Key.F)] = () => OnToggleFullscreen?.Invoke();
            _hotkeyActions[new KeyGesture(Key.V)] = () => OnToggleVisualizer?.Invoke();
            _hotkeyActions[new KeyGesture(Key.B)] = () => OnToggleBeatDetection?.Invoke();
            _hotkeyActions[new KeyGesture(Key.R)] = () => OnToggleRandomMode?.Invoke();
            _hotkeyActions[new KeyGesture(Key.Escape)] = () => OnToggleFullscreen?.Invoke();
            
            // Enhanced functionality hotkeys
            _hotkeyActions[new KeyGesture(Key.P, KeyModifiers.Control)] = () => OnTogglePluginBrowser?.Invoke();
            _hotkeyActions[new KeyGesture(Key.M, KeyModifiers.Control)] = () => OnTogglePresetManager?.Invoke();
            _hotkeyActions[new KeyGesture(Key.S, KeyModifiers.Control)] = () => OnToggleSettings?.Invoke();
            _hotkeyActions[new KeyGesture(Key.S, KeyModifiers.Shift)] = () => OnQuickSavePreset?.Invoke();
            _hotkeyActions[new KeyGesture(Key.L, KeyModifiers.Shift)] = () => OnQuickLoadPreset?.Invoke();
            
            // Audio and visualization mode hotkeys
            _hotkeyActions[new KeyGesture(Key.A, KeyModifiers.Control)] = () => OnToggleAudioSource?.Invoke();
            _hotkeyActions[new KeyGesture(Key.D1)] = () => OnToggleSpectrumMode?.Invoke();
            _hotkeyActions[new KeyGesture(Key.D2)] = () => OnToggleWaveformMode?.Invoke();
            _hotkeyActions[new KeyGesture(Key.D3)] = () => OnToggleOscilloscopeMode?.Invoke();
            
            // Modifier combinations for existing actions
            _hotkeyActions[new KeyGesture(Key.N, KeyModifiers.Control)] = () => OnNextPreset?.Invoke();
            _hotkeyActions[new KeyGesture(Key.P, KeyModifiers.Control)] = () => OnPreviousPreset?.Invoke();
            _hotkeyActions[new KeyGesture(Key.R, KeyModifiers.Control)] = () => OnRandomPreset?.Invoke();
            _hotkeyActions[new KeyGesture(Key.F, KeyModifiers.Control)] = () => OnToggleFullscreen?.Invoke();
            _hotkeyActions[new KeyGesture(Key.V, KeyModifiers.Control)] = () => OnToggleVisualizer?.Invoke();
            _hotkeyActions[new KeyGesture(Key.R, KeyModifiers.Shift)] = () => OnToggleRandomMode?.Invoke();
            _hotkeyActions[new KeyGesture(Key.B, KeyModifiers.Shift)] = () => OnToggleBeatDetection?.Invoke();
            _hotkeyActions[new KeyGesture(Key.Enter, KeyModifiers.Alt)] = () => OnToggleFullscreen?.Invoke();
            _hotkeyActions[new KeyGesture(Key.V, KeyModifiers.Alt)] = () => OnToggleVisualizer?.Invoke();
        }

        /// <summary>
        /// Enable or disable hotkey processing
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled && _settings.EnableHotkeys;
            set => _isEnabled = value;
        }

        /// <summary>
        /// Process a key press event and trigger appropriate hotkey actions
        /// </summary>
        public bool ProcessKeyPress(Key key)
        {
            if (!IsEnabled) return false;

            var gesture = new KeyGesture(key);
            if (_hotkeyActions.TryGetValue(gesture, out var action))
            {
                try
                {
                    action.Invoke();
                    return true; // Hotkey was processed
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error executing hotkey action for {key}: {ex.Message}");
                }
            }

            return false; // No hotkey was processed
        }

        /// <summary>
        /// Process a key combination (e.g., Ctrl+Key)
        /// </summary>
        public bool ProcessKeyCombination(Key key, KeyModifiers modifiers)
        {
            if (!IsEnabled) return false;

            var gesture = new KeyGesture(key, modifiers);
            if (_hotkeyActions.TryGetValue(gesture, out var action))
            {
                try
                {
                    action.Invoke();
                    return true; // Hotkey was processed
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error executing hotkey combination {gesture}: {ex.Message}");
                }
            }

            return false; // No hotkey was processed
        }

        /// <summary>
        /// Load custom hotkey bindings from configuration file
        /// </summary>
        private void LoadCustomBindings()
        {
            try
            {
                if (File.Exists(_hotkeyConfigPath))
                {
                    var json = File.ReadAllText(_hotkeyConfigPath);
                    var bindings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    
                    if (bindings != null)
                    {
                        foreach (var binding in bindings)
                        {
                            if (TryParseKeyGesture(binding.Value, out var gesture))
                            {
                                _customBindings[binding.Key] = gesture;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load custom hotkey bindings: {ex.Message}");
            }
        }

        /// <summary>
        /// Save custom hotkey bindings to configuration file
        /// </summary>
        public void SaveCustomBindings()
        {
            try
            {
                var configDir = Path.GetDirectoryName(_hotkeyConfigPath);
                if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                var bindings = new Dictionary<string, string>();
                foreach (var binding in _customBindings)
                {
                    bindings[binding.Key] = binding.Value.ToString();
                }

                var json = JsonSerializer.Serialize(bindings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_hotkeyConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save custom hotkey bindings: {ex.Message}");
            }
        }

        /// <summary>
        /// Try to parse a key gesture string (e.g., "Ctrl+A", "Shift+F1")
        /// </summary>
        private bool TryParseKeyGesture(string gestureString, out KeyGesture gesture)
        {
            gesture = new KeyGesture(Key.None);
            
            try
            {
                var parts = gestureString.Split('+');
                if (parts.Length == 0) return false;

                var key = parts[parts.Length - 1];
                var modifiers = KeyModifiers.None;

                for (int i = 0; i < parts.Length - 1; i++)
                {
                    var modifier = parts[i].ToLower();
                    switch (modifier)
                    {
                        case "ctrl":
                        case "control":
                            modifiers |= KeyModifiers.Control;
                            break;
                        case "shift":
                            modifiers |= KeyModifiers.Shift;
                            break;
                        case "alt":
                            modifiers |= KeyModifiers.Alt;
                            break;
                    }
                }

                if (Enum.TryParse<Key>(key, true, out var parsedKey))
                {
                    gesture = new KeyGesture(parsedKey, modifiers);
                    return true;
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return false;
        }

        /// <summary>
        /// Get a description of all available hotkeys
        /// </summary>
        public Dictionary<string, string> GetHotkeyDescriptions()
        {
            var descriptions = new Dictionary<string, string>
            {
                // Core navigation
                { "Y", "Next preset" },
                { "U", "Previous preset" },
                { "Space", "Random preset" },
                { "F", "Toggle fullscreen" },
                { "V", "Toggle visualizer" },
                { "B", "Toggle beat detection" },
                { "R", "Toggle random mode" },
                { "Escape", "Exit fullscreen" },
                
                // Enhanced functionality
                { "Ctrl+P", "Toggle plugin browser" },
                { "Ctrl+M", "Toggle preset manager" },
                { "Ctrl+S", "Toggle settings" },
                { "Shift+S", "Quick save preset" },
                { "Shift+L", "Quick load preset" },
                { "Ctrl+A", "Toggle audio source" },
                { "1", "Toggle spectrum mode" },
                { "2", "Toggle waveform mode" },
                { "3", "Toggle oscilloscope mode" },
                
                // Modifier combinations
                { "Ctrl+N", "Next preset" },
                { "Ctrl+P", "Previous preset" },
                { "Ctrl+R", "Random preset" },
                { "Ctrl+F", "Toggle fullscreen" },
                { "Ctrl+V", "Toggle visualizer" },
                { "Shift+R", "Toggle random mode" },
                { "Shift+B", "Toggle beat detection" },
                { "Alt+Enter", "Toggle fullscreen" },
                { "Alt+V", "Toggle visualizer" }
            };

            // Add custom bindings
            foreach (var binding in _customBindings)
            {
                descriptions[binding.Value.ToString()] = $"Custom: {binding.Key}";
            }

            return descriptions;
        }

        /// <summary>
        /// Register a custom hotkey action
        /// </summary>
        public void RegisterHotkey(Key key, Action action)
        {
            var gesture = new KeyGesture(key);
            if (_hotkeyActions.ContainsKey(gesture))
            {
                _hotkeyActions[gesture] = action;
            }
            else
            {
                _hotkeyActions.Add(gesture, action);
            }
        }

        /// <summary>
        /// Register a custom hotkey action with modifiers
        /// </summary>
        public void RegisterHotkey(Key key, KeyModifiers modifiers, Action action)
        {
            var gesture = new KeyGesture(key, modifiers);
            if (_hotkeyActions.ContainsKey(gesture))
            {
                _hotkeyActions[gesture] = action;
            }
            else
            {
                _hotkeyActions.Add(gesture, action);
            }
        }

        /// <summary>
        /// Register a custom hotkey binding by name
        /// </summary>
        public void RegisterCustomBinding(string name, Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            var gesture = new KeyGesture(key, modifiers);
            _customBindings[name] = gesture;
            SaveCustomBindings();
        }

        /// <summary>
        /// Unregister a hotkey
        /// </summary>
        public void UnregisterHotkey(Key key)
        {
            var gesture = new KeyGesture(key);
            if (_hotkeyActions.ContainsKey(gesture))
            {
                _hotkeyActions.Remove(gesture);
            }
        }

        /// <summary>
        /// Unregister a hotkey with modifiers
        /// </summary>
        public void UnregisterHotkey(Key key, KeyModifiers modifiers)
        {
            var gesture = new KeyGesture(key, modifiers);
            if (_hotkeyActions.ContainsKey(gesture))
            {
                _hotkeyActions.Remove(gesture);
            }
        }

        /// <summary>
        /// Get all registered hotkey gestures
        /// </summary>
        public IEnumerable<KeyGesture> GetRegisteredHotkeys()
        {
            return _hotkeyActions.Keys;
        }

        /// <summary>
        /// Check if a specific key gesture is registered
        /// </summary>
        public bool IsHotkeyRegistered(Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            var gesture = new KeyGesture(key, modifiers);
            return _hotkeyActions.ContainsKey(gesture);
        }

        /// <summary>
        /// Reset all hotkeys to default bindings
        /// </summary>
        public void ResetToDefaults()
        {
            _customBindings.Clear();
            InitializeDefaultHotkeys();
            SaveCustomBindings();
        }
    }
}
