using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia.Input;
using PhoenixVisualizer.Core.Config;

namespace PhoenixVisualizer.Services
{
    /// <summary>
    /// Service for handling Winamp-style hotkeys in the visualizer
    /// </summary>
    public class WinampHotkeyService
    {
        private readonly Dictionary<Key, Action> _hotkeyActions;
        private readonly VisualizerSettings _settings;
        private bool _isEnabled = true;

        public event Action? OnNextPreset;
        public event Action? OnPreviousPreset;
        public event Action? OnRandomPreset;
        public event Action? OnToggleFullscreen;
        public event Action? OnToggleVisualizer;
        public event Action? OnToggleBeatDetection;
        public event Action? OnToggleRandomMode;

        public WinampHotkeyService(VisualizerSettings settings)
        {
            _settings = settings;
            _hotkeyActions = new Dictionary<Key, Action>
            {
                { Key.Y, () => OnNextPreset?.Invoke() },           // Next preset
                { Key.U, () => OnPreviousPreset?.Invoke() },       // Previous preset
                { Key.Space, () => OnRandomPreset?.Invoke() },     // Random preset
                { Key.F, () => OnToggleFullscreen?.Invoke() },     // Toggle fullscreen
                { Key.V, () => OnToggleVisualizer?.Invoke() },     // Toggle visualizer
                { Key.B, () => OnToggleBeatDetection?.Invoke() },  // Toggle beat detection
                { Key.R, () => OnToggleRandomMode?.Invoke() },     // Toggle random mode
                { Key.Escape, () => OnToggleFullscreen?.Invoke() } // Exit fullscreen
            };
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

            if (_hotkeyActions.TryGetValue(key, out var action))
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

            // Handle modifier key combinations
            if (modifiers.HasFlag(KeyModifiers.Control))
                return ProcessCtrlCombination(key);
            else if (modifiers.HasFlag(KeyModifiers.Shift))
                return ProcessShiftCombination(key);
            else if (modifiers.HasFlag(KeyModifiers.Alt))
                return ProcessAltCombination(key);
            else
                return false;
        }

        private bool ProcessCtrlCombination(Key key)
        {
            switch (key)
            {
                case Key.N:
                    OnNextPreset?.Invoke();
                    return true;
                case Key.P:
                    OnPreviousPreset?.Invoke();
                    return true;
                case Key.R:
                    OnRandomPreset?.Invoke();
                    return true;
                case Key.F:
                    OnToggleFullscreen?.Invoke();
                    return true;
                case Key.V:
                    OnToggleVisualizer?.Invoke();
                    return true;
                default:
                    return false;
            }
        }

        private bool ProcessShiftCombination(Key key)
        {
            switch (key)
            {
                case Key.R:
                    OnToggleRandomMode?.Invoke();
                    return true;
                case Key.B:
                    OnToggleBeatDetection?.Invoke();
                    return true;
                default:
                    return false;
            }
        }

        private bool ProcessAltCombination(Key key)
        {
            switch (key)
            {
                case Key.Enter:
                    OnToggleFullscreen?.Invoke();
                    return true;
                case Key.V:
                    OnToggleVisualizer?.Invoke();
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Get a description of all available hotkeys
        /// </summary>
        public Dictionary<string, string> GetHotkeyDescriptions()
        {
            return new Dictionary<string, string>
            {
                { "Y", "Next preset" },
                { "U", "Previous preset" },
                { "Space", "Random preset" },
                { "F", "Toggle fullscreen" },
                { "V", "Toggle visualizer" },
                { "B", "Toggle beat detection" },
                { "R", "Toggle random mode" },
                { "Escape", "Exit fullscreen" },
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
        }

        /// <summary>
        /// Register a custom hotkey action
        /// </summary>
        public void RegisterHotkey(Key key, Action action)
        {
            if (_hotkeyActions.ContainsKey(key))
            {
                _hotkeyActions[key] = action;
            }
            else
            {
                _hotkeyActions.Add(key, action);
            }
        }

        /// <summary>
        /// Unregister a hotkey
        /// </summary>
        public void UnregisterHotkey(Key key)
        {
            if (_hotkeyActions.ContainsKey(key))
            {
                _hotkeyActions.Remove(key);
            }
        }
    }
}
