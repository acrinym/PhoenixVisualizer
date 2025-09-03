using Avalonia.Media;
using ReactiveUI;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhoenixVisualizer.App.ViewModels
{
    public class PhxEditorSettings : ReactiveObject
    {
        bool _clearEveryFrame = true;
        bool _pixelDoubling;
        bool _vsync = true;
        bool _showFps;
        bool _showOverlay;
        double _overlayScale = 1.0;
        bool _linearColor = true;
        Color _backgroundColor = Colors.Black;
        string _themeName = "Dark";
        
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PhoenixVisualizer",
            "settings.json");
            
        [JsonIgnore]
        public static readonly string[] AvailableThemes = new[] { "Dark", "Light", "Neon", "Minimal" };

        // Hex representation for binding (since ColorPicker isn't available)
        public string BackgroundColorHex
        {
            get => $"#{_backgroundColor.R:X2}{_backgroundColor.G:X2}{_backgroundColor.B:X2}";
            set
            {
                if (string.IsNullOrWhiteSpace(value) || value.Length != 7 || !value.StartsWith("#"))
                    return;

                try
                {
                    var r = Convert.ToByte(value.Substring(1, 2), 16);
                    var g = Convert.ToByte(value.Substring(3, 2), 16);
                    var b = Convert.ToByte(value.Substring(5, 2), 16);
                    BackgroundColor = Color.FromRgb(r, g, b);
                }
                catch
                {
                    // Ignore invalid hex values
                }
            }
        }

        public bool ClearEveryFrame { get => _clearEveryFrame; set => this.RaiseAndSetIfChanged(ref _clearEveryFrame, value); }
        public bool PixelDoubling   { get => _pixelDoubling;   set => this.RaiseAndSetIfChanged(ref _pixelDoubling, value); }
        public bool VSync           { get => _vsync;           set => this.RaiseAndSetIfChanged(ref _vsync, value); }
        public bool ShowFps         { get => _showFps;         set => this.RaiseAndSetIfChanged(ref _showFps, value); }
        public bool ShowOverlay     { get => _showOverlay;     set => this.RaiseAndSetIfChanged(ref _showOverlay, value); }
        public double OverlayScale  { get => _overlayScale;    set => this.RaiseAndSetIfChanged(ref _overlayScale, Math.Clamp(value, 0.75, 2.0)); }
        public bool LinearColor     { get => _linearColor;     set => this.RaiseAndSetIfChanged(ref _linearColor, value); }
        public string ThemeName     { get => _themeName;     set => this.RaiseAndSetIfChanged(ref _themeName, value); }
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor != value)
                {
                    this.RaiseAndSetIfChanged(ref _backgroundColor, value);
                    this.RaisePropertyChanged(nameof(BackgroundColorHex));
                }
            }
        }
        
        public void Save()
        {
            try
            {
                var dirPath = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                    var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(SettingsPath, json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
        
        public static PhxEditorSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<PhxEditorSettings>(json);
                    if (settings != null)
                        return settings;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
            
            return new PhxEditorSettings();
        }
        
        public void ApplyTheme()
        {
            // Apply theme based on ThemeName
            var app = Avalonia.Application.Current;
            if (app == null) return;
            
            // Set theme resources based on theme name
            switch (ThemeName)
            {
                case "Light":
                    app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
                    break;
                case "Dark":
                    app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
                    break;
                case "Neon":
                    app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
                    // Additional neon theme resources would be applied here
                    break;
                case "Minimal":
                    app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
                    // Additional minimal theme resources would be applied here
                    break;
            }
        }
    }
}
