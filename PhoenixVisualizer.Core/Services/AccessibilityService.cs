using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using PhoenixVisualizer.Core.Config;

namespace PhoenixVisualizer.Core.Services
{
    /// <summary>
    /// Adapter interface for accessibility announcements
    /// </summary>
    public interface IAccessibilityAdapter
    {
        void Announce(string message);
    }

    /// <summary>
    /// Service for managing accessibility features and user experience enhancements
    /// </summary>
    public sealed class AccessibilityService
    {
        private readonly string _configPath;
        private AccessibilitySettings _settings;
        private readonly Dictionary<string, string> _screenReaderTexts;
        private readonly IAccessibilityAdapter _adapter;

        public event Action? OnSettingsChanged;

        public AccessibilityService(IAccessibilityAdapter? adapter = null)
        {
            _adapter = adapter ?? new NoopAdapter();
            _configPath = Path.Combine(
                AppContext.BaseDirectory,
                "config",
                "accessibility.json"
            );
            
            _settings = new AccessibilitySettings();
            _screenReaderTexts = new Dictionary<string, string>();
            
            LoadSettings();
            InitializeScreenReaderTexts();
        }

        /// <summary>
        /// Current accessibility settings
        /// </summary>
        public AccessibilitySettings Settings => _settings;

        /// <summary>
        /// Enable or disable high contrast mode
        /// </summary>
        public bool HighContrastMode
        {
            get => _settings.HighContrastMode;
            set
            {
                if (_settings.HighContrastMode != value)
                {
                    _settings.HighContrastMode = value;
                    SaveSettings();
                    OnSettingsChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Enable or disable screen reader support
        /// </summary>
        public bool ScreenReaderSupport
        {
            get => _settings.ScreenReaderSupport;
            set
            {
                if (_settings.ScreenReaderSupport != value)
                {
                    _settings.ScreenReaderSupport = value;
                    SaveSettings();
                    OnSettingsChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Enable or disable keyboard navigation enhancements
        /// </summary>
        public bool EnhancedKeyboardNavigation
        {
            get => _settings.EnhancedKeyboardNavigation;
            set
            {
                if (_settings.EnhancedKeyboardNavigation != value)
                {
                    _settings.EnhancedKeyboardNavigation = value;
                    SaveSettings();
                    OnSettingsChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Enable or disable focus indicators
        /// </summary>
        public bool ShowFocusIndicators
        {
            get => _settings.ShowFocusIndicators;
            set
            {
                if (_settings.ShowFocusIndicators != value)
                {
                    _settings.ShowFocusIndicators = value;
                    SaveSettings();
                    OnSettingsChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Enable or disable large text mode
        /// </summary>
        public bool LargeTextMode
        {
            get => _settings.LargeTextMode;
            set
            {
                if (_settings.LargeTextMode != value)
                {
                    _settings.LargeTextMode = value;
                    SaveSettings();
                    OnSettingsChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Enable or disable reduced motion
        /// </summary>
        public bool ReducedMotion
        {
            get => _settings.ReducedMotion;
            set
            {
                if (_settings.ReducedMotion != value)
                {
                    _settings.ReducedMotion = value;
                    SaveSettings();
                    OnSettingsChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Get screen reader text for a specific element
        /// </summary>
        public string GetScreenReaderText(string elementId)
        {
            if (_screenReaderTexts.TryGetValue(elementId, out var text))
            {
                return text;
            }
            return elementId;
        }

        /// <summary>
        /// Set screen reader text for a specific element
        /// </summary>
        public void SetScreenReaderText(string elementId, string text)
        {
            _screenReaderTexts[elementId] = text;
        }

        /// <summary>
        /// Announce text to screen readers
        /// </summary>
        public void AnnounceToScreenReader(string text)
        {
            if (ScreenReaderSupport)
            {
                // This would integrate with the platform's screen reader API
                // For now, we'll use the adapter
                _adapter.Announce(text);
            }
        }

        /// <summary>
        /// Get accessibility information for the current application state
        /// </summary>
        public string GetApplicationAccessibilityInfo()
        {
            var info = new List<string>
            {
                $"PhoenixVisualizer - Audio Visualizer Application",
                $"Current Plugin: {GetCurrentPluginInfo()}",
                $"Audio Status: {GetAudioStatusInfo()}",
                $"Preset Count: {GetPresetCountInfo()}"
            };

            return string.Join(". ", info);
        }

        /// <summary>
        /// Apply accessibility settings to the application
        /// </summary>
        public void ApplySettings()
        {
            // Apply high contrast mode
            if (HighContrastMode)
            {
                ApplyHighContrastTheme();
            }

            // Apply large text mode
            if (LargeTextMode)
            {
                ApplyLargeTextMode();
            }

            // Apply reduced motion
            if (ReducedMotion)
            {
                ApplyReducedMotion();
            }

            // Announce settings change
            AnnounceToScreenReader("Accessibility settings applied");
        }

        /// <summary>
        /// Reset all accessibility settings to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            _settings = new AccessibilitySettings();
            SaveSettings();
            OnSettingsChanged?.Invoke();
            AnnounceToScreenReader("Accessibility settings reset to defaults");
        }

        /// <summary>
        /// Load accessibility settings from configuration file
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var loadedSettings = JsonSerializer.Deserialize<AccessibilitySettings>(json);
                    if (loadedSettings != null)
                    {
                        _settings = loadedSettings;
                    }
                }
            }
            catch
            {
                // Failed to load accessibility settings silently
            }
        }

        /// <summary>
        /// Save accessibility settings to configuration file
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                var configDir = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
            }
            catch
            {
                // Failed to save accessibility settings silently
            }
        }

        /// <summary>
        /// Initialize default screen reader texts
        /// </summary>
        private void InitializeScreenReaderTexts()
        {
            _screenReaderTexts["BtnOpen"] = "Open audio file button";
            _screenReaderTexts["BtnPlay"] = "Play audio button";
            _screenReaderTexts["BtnPause"] = "Pause audio button";
            _screenReaderTexts["BtnStop"] = "Stop audio button";
            _screenReaderTexts["BtnTempoPitch"] = "Tempo and pitch control button";
            _screenReaderTexts["BtnSettings"] = "Settings button";
            _screenReaderTexts["BtnEditor"] = "AVS editor button";
            _screenReaderTexts["CmbPlugin"] = "Plugin selection dropdown";
            _screenReaderTexts["TxtPreset"] = "Preset text input field";
            _screenReaderTexts["RenderHost"] = "Visualization render area";
        }

        /// <summary>
        /// Get current plugin information for accessibility
        /// </summary>
        private string GetCurrentPluginInfo()
        {
            // This would integrate with the plugin system
            return "AVS Runtime Engine";
        }

        /// <summary>
        /// Get audio status information for accessibility
        /// </summary>
        private string GetAudioStatusInfo()
        {
            // This would integrate with the audio system
            return "No audio file loaded";
        }

        /// <summary>
        /// Get preset count information for accessibility
        /// </summary>
        private string GetPresetCountInfo()
        {
            // This would integrate with the preset system
            return "0 presets available";
        }

        /// <summary>
        /// Apply high contrast theme
        /// </summary>
        private void ApplyHighContrastTheme()
        {
            // This would integrate with the theming system
            // High contrast theme applied
        }

        /// <summary>
        /// Apply large text mode
        /// </summary>
        private void ApplyLargeTextMode()
        {
            // This would integrate with the UI scaling system
            // Large text mode applied
        }

        /// <summary>
        /// Apply reduced motion
        /// </summary>
        private void ApplyReducedMotion()
        {
            // This would integrate with the animation system
            // Reduced motion applied
        }

        /// <summary>
        /// Announce text using the accessibility adapter
        /// </summary>
        public void Announce(string key)
        {
            var text = GetScreenReaderText(key);
            _adapter.Announce(text);
        }

        /// <summary>
        /// No-operation adapter for when no accessibility system is available
        /// </summary>
        private sealed class NoopAdapter : IAccessibilityAdapter
        {
            public void Announce(string message) { /* intentionally noop */ }
        }
    }

    /// <summary>
    /// Accessibility settings configuration
    /// </summary>
    public class AccessibilitySettings
    {
        public bool HighContrastMode { get; set; } = false;
        public bool ScreenReaderSupport { get; set; } = true;
        public bool EnhancedKeyboardNavigation { get; set; } = true;
        public bool ShowFocusIndicators { get; set; } = true;
        public bool LargeTextMode { get; set; } = false;
        public bool ReducedMotion { get; set; } = false;
        public double TextScaleFactor { get; set; } = 1.0;
        public string PreferredColorScheme { get; set; } = "auto";
        public bool EnableSoundEffects { get; set; } = true;
        public bool EnableHapticFeedback { get; set; } = false;
    }
}
