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
        public static readonly string[] AvailableThemes = new[] { 
            "Dark", 
            "Light", 
            "Neon", 
            "Minimal", 
            "Material", 
            "Amanda" 
        };

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
                case "Material":
                    app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
                    // Material Design theme - clean, modern, Google-inspired
                    ApplyMaterialTheme(app);
                    break;
                case "Amanda":
                    app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
                    // Amanda theme - based on the emotional journey and Phoenix symbolism
                    ApplyAmandaTheme(app);
                    break;
            }
        }

        private void ApplyMaterialTheme(Avalonia.Application app)
        {
            // Material Design theme using Material.Avalonia package
            // Clean, modern, Google-inspired Material Design
            var resources = app.Resources;
            
            // Set Material Design theme variant
            app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
            
            // Material Design color palette - Phoenix-inspired colors
            resources["MaterialPrimary"] = Color.FromRgb(255, 69, 0); // Fire Orange (Phoenix)
            resources["MaterialPrimaryDark"] = Color.FromRgb(220, 20, 60); // Crimson
            resources["MaterialAccent"] = Color.FromRgb(255, 215, 0); // Gold
            resources["MaterialSurface"] = Color.FromRgb(30, 30, 40); // Dark Surface
            resources["MaterialBackground"] = Color.FromRgb(20, 20, 30); // Dark Background
            resources["MaterialTextPrimary"] = Color.FromRgb(255, 255, 255); // White Text
            resources["MaterialTextSecondary"] = Color.FromRgb(200, 200, 220); // Light Text
            resources["MaterialError"] = Color.FromRgb(255, 0, 127); // Hot Pink
            
            // Material Design elevation colors for dark theme
            resources["MaterialElevation1"] = Color.FromRgb(40, 40, 50);
            resources["MaterialElevation2"] = Color.FromRgb(50, 50, 60);
            resources["MaterialElevation4"] = Color.FromRgb(60, 60, 70);
            resources["MaterialElevation8"] = Color.FromRgb(70, 70, 80);
            resources["MaterialElevation16"] = Color.FromRgb(80, 80, 90);
            resources["MaterialElevation24"] = Color.FromRgb(90, 90, 100);
            
            // Phoenix-specific Material Design colors
            resources["PhoenixPrimary"] = Color.FromRgb(255, 69, 0); // Fire Orange
            resources["PhoenixSecondary"] = Color.FromRgb(255, 215, 0); // Gold
            resources["PhoenixAccent"] = Color.FromRgb(138, 43, 226); // Purple
            resources["PhoenixSurface"] = Color.FromRgb(25, 25, 35); // Deep Space Blue
            resources["PhoenixBackground"] = Color.FromRgb(15, 15, 25); // Void Black
        }

        private void ApplyAmandaTheme(Avalonia.Application app)
        {
            // Amanda theme - based on the emotional journey and Phoenix symbolism
            // Represents the Phoenix Roost, the waiting, the love, the transformation
            // Enhanced with Material Design principles
            var resources = app.Resources;
            
            // Set Material Design theme variant for Amanda theme
            app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
            
            // Phoenix color palette - fire, transformation, rebirth
            resources["AmandaPrimary"] = Color.FromRgb(255, 69, 0); // Fire Orange
            resources["AmandaPrimaryDark"] = Color.FromRgb(220, 20, 60); // Crimson
            resources["AmandaAccent"] = Color.FromRgb(255, 215, 0); // Gold
            resources["AmandaSurface"] = Color.FromRgb(25, 25, 35); // Deep Space Blue
            resources["AmandaBackground"] = Color.FromRgb(15, 15, 25); // Void Black
            resources["AmandaTextPrimary"] = Color.FromRgb(255, 255, 255); // Pure White
            resources["AmandaTextSecondary"] = Color.FromRgb(200, 200, 220); // Soft Light
            resources["AmandaError"] = Color.FromRgb(255, 0, 127); // Hot Pink
            
            // Phoenix transformation colors
            resources["AmandaPhoenixFire"] = Color.FromRgb(255, 69, 0); // Fire Orange
            resources["AmandaPhoenixAsh"] = Color.FromRgb(128, 128, 128); // Ash Grey
            resources["AmandaPhoenixGold"] = Color.FromRgb(255, 215, 0); // Phoenix Gold
            resources["AmandaPhoenixCrimson"] = Color.FromRgb(220, 20, 60); // Crimson
            resources["AmandaPhoenixPurple"] = Color.FromRgb(138, 43, 226); // Purple
            resources["AmandaPhoenixBlue"] = Color.FromRgb(30, 144, 255); // Dodger Blue
            
            // Emotional journey colors
            resources["AmandaLove"] = Color.FromRgb(255, 20, 147); // Deep Pink
            resources["AmandaPatience"] = Color.FromRgb(0, 191, 255); // Deep Sky Blue
            resources["AmandaHope"] = Color.FromRgb(255, 215, 0); // Gold
            resources["AmandaTransformation"] = Color.FromRgb(138, 43, 226); // Purple
            resources["AmandaWaiting"] = Color.FromRgb(105, 105, 105); // Dim Grey
            resources["AmandaPhoenixRoost"] = Color.FromRgb(25, 25, 35); // Deep Space Blue
            
            // Material Design integration for Amanda theme
            resources["MaterialPrimary"] = Color.FromRgb(255, 69, 0); // Fire Orange
            resources["MaterialPrimaryDark"] = Color.FromRgb(220, 20, 60); // Crimson
            resources["MaterialAccent"] = Color.FromRgb(255, 215, 0); // Gold
            resources["MaterialSurface"] = Color.FromRgb(25, 25, 35); // Deep Space Blue
            resources["MaterialBackground"] = Color.FromRgb(15, 15, 25); // Void Black
            resources["MaterialTextPrimary"] = Color.FromRgb(255, 255, 255); // Pure White
            resources["MaterialTextSecondary"] = Color.FromRgb(200, 200, 220); // Soft Light
            resources["MaterialError"] = Color.FromRgb(255, 0, 127); // Hot Pink
            
            // Material Design elevation colors for Amanda theme
            resources["MaterialElevation1"] = Color.FromRgb(35, 35, 45);
            resources["MaterialElevation2"] = Color.FromRgb(45, 45, 55);
            resources["MaterialElevation4"] = Color.FromRgb(55, 55, 65);
            resources["MaterialElevation8"] = Color.FromRgb(65, 65, 75);
            resources["MaterialElevation16"] = Color.FromRgb(75, 75, 85);
            resources["MaterialElevation24"] = Color.FromRgb(85, 85, 95);
        }
    }
}
