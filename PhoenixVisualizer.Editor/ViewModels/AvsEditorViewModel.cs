using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Services;
using PhoenixVisualizer.Editor.Services;
using System.IO;
using Avalonia.Platform;
using Avalonia.Controls;

namespace PhoenixVisualizer.Editor.ViewModels
{
    public class AvsEditorViewModel : ViewModelBase
    {
        private readonly AvsEditorBridge _bridge;
        private readonly AvaloniaAvsRenderer _renderer;
        private readonly AvsAudioProvider _audioProvider;
        
        private string _searchTerm = "";
        private AvsEffect? _selectedEffect;
        private bool _isAudioPlaying;
        
        // Collections for effect libraries
        public ObservableCollection<AvsEffect> FilteredEffectLibrary { get; } = new();
        public ObservableCollection<AvsEffect> InitEffectsLibrary { get; } = new();
        public ObservableCollection<AvsEffect> BeatEffectsLibrary { get; } = new();
        public ObservableCollection<AvsEffect> FrameEffectsLibrary { get; } = new();
        public ObservableCollection<AvsEffect> PointEffectsLibrary { get; } = new();
        
        // Collections for effect categories
        public ObservableCollection<AvsEffect> RenderingEffects { get; } = new();
        public ObservableCollection<AvsEffect> MovementEffects { get; } = new();
        public ObservableCollection<AvsEffect> ColorEffects { get; } = new();
        public ObservableCollection<AvsEffect> DistortionEffects { get; } = new();
        public ObservableCollection<AvsEffect> ParticleEffects { get; } = new();
        public ObservableCollection<AvsEffect> AudioEffects { get; } = new();
        public ObservableCollection<AvsEffect> SpecialEffects { get; } = new();
        public ObservableCollection<AvsEffect> CustomEffects { get; } = new();
        
        // Active effects in the current preset
        public ObservableCollection<AvsEffect> InitEffects { get; } = new();
        public ObservableCollection<AvsEffect> BeatEffects { get; } = new();
        public ObservableCollection<AvsEffect> FrameEffects { get; } = new();
        public ObservableCollection<AvsEffect> PointEffects { get; } = new();
        
        // Selected effect parameters for editing
        public ObservableCollection<KeyValuePair<string, object>> SelectedEffectParameters { get; } = new();
        
        // Current preset
        public AvsPreset CurrentPreset { get; private set; }
        
        // Commands
        public ICommand NewPresetCommand { get; }
        public ICommand LoadPresetCommand { get; }
        public ICommand SavePresetCommand { get; }
        public ICommand TestPresetCommand { get; }
        public ICommand ImportPresetCommand { get; }
        public ICommand ExportPresetCommand { get; }
        public ICommand PlayAudioCommand { get; }
        public ICommand StopAudioCommand { get; }
        public ICommand ClearSectionCommand { get; }
        public ICommand MoveEffectUpCommand { get; }
        public ICommand MoveEffectDownCommand { get; }
        public ICommand CopyEffectCommand { get; }
        public ICommand RemoveEffectCommand { get; }
        public ICommand SendToMainWindowCommand { get; }
        
        // Properties
        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                if (SetProperty(ref _searchTerm, value))
                {
                    FilterEffects();
                }
            }
        }
        
        public AvsEffect? SelectedEffect
        {
            get => _selectedEffect;
            set
            {
                if (SetProperty(ref _selectedEffect, value))
                {
                    UpdateSelectedEffectParameters();
                }
            }
        }
        
        public bool IsAudioPlaying
        {
            get => _isAudioPlaying;
            set => SetProperty(ref _isAudioPlaying, value);
        }
        
        public AvsEditorViewModel()
        {
            // Initialize services
            _renderer = new AvaloniaAvsRenderer();
            _audioProvider = new AvsAudioProvider();
            _bridge = new AvsEditorBridge();
            
            // Set up the bridge
            _bridge.SetRenderer(_renderer);
            _bridge.SetAudioProvider(_audioProvider);
            
            // Wire up bridge events
            _bridge.PresetLoaded += OnPresetLoaded;
            _bridge.PresetStarted += OnPresetStarted;
            _bridge.PresetStopped += OnPresetStopped;
            _bridge.ErrorOccurred += OnBridgeError;
            
            // Initialize commands
            NewPresetCommand = new RelayCommand(NewPreset);
            LoadPresetCommand = new RelayCommand(LoadPreset);
            SavePresetCommand = new RelayCommand(SavePreset);
            TestPresetCommand = new RelayCommand(TestPreset);
            ImportPresetCommand = new RelayCommand(ImportPreset);
            ExportPresetCommand = new RelayCommand(ExportPreset);
            PlayAudioCommand = new RelayCommand(ToggleAudio);
            StopAudioCommand = new RelayCommand(StopAudio);
            ClearSectionCommand = new RelayCommand<AvsSection>(ClearSection);
            MoveEffectUpCommand = new RelayCommand<AvsEffect?>(MoveEffectUp);
            MoveEffectDownCommand = new RelayCommand<AvsEffect?>(MoveEffectDown);
            CopyEffectCommand = new RelayCommand<AvsEffect?>(CopyEffect);
            RemoveEffectCommand = new RelayCommand<AvsEffect?>(RemoveEffect);
            SendToMainWindowCommand = new RelayCommand(SendToMainWindow);
            
            // Initialize current preset
            CurrentPreset = new AvsPreset
            {
                Name = "New Preset",
                Author = "AVS Editor",
                Description = "A new AVS preset",
                ClearEveryFrame = true,
                BeatDetection = true,
                RandomPresetSwitching = false,
                FrameRate = 60
            };
            
            // Initialize effect library
            InitializeEffectLibrary();
        }
        
        private void InitializeEffectLibrary()
        {
            // Clear existing collections
            FilteredEffectLibrary.Clear();
            InitEffectsLibrary.Clear();
            BeatEffectsLibrary.Clear();
            FrameEffectsLibrary.Clear();
            PointEffectsLibrary.Clear();
            
            // Clear category collections
            RenderingEffects.Clear();
            MovementEffects.Clear();
            ColorEffects.Clear();
            DistortionEffects.Clear();
            ParticleEffects.Clear();
            AudioEffects.Clear();
            SpecialEffects.Clear();
            CustomEffects.Clear();
            
            // Get effects organized by section and category
            var effectsBySection = AvsEffectLibraryService.GetEffectsBySection();
            var effectsByCategory = AvsEffectLibraryService.GetEffectsByCategory();
            
            // Populate all collections
            foreach (var effect in AvsEffectLibraryService.EffectLibrary)
            {
                // Add to filtered library
                FilteredEffectLibrary.Add(effect);
                
                // Add to section-specific collections
                switch (effect.Section)
                {
                    case AvsSection.Init:
                        InitEffectsLibrary.Add(effect);
                        break;
                    case AvsSection.Beat:
                        BeatEffectsLibrary.Add(effect);
                        break;
                    case AvsSection.Frame:
                        FrameEffectsLibrary.Add(effect);
                        break;
                    case AvsSection.Point:
                        PointEffectsLibrary.Add(effect);
                        break;
                }
                
                // Add to category collections
                if (effectsByCategory.ContainsKey("Rendering"))
                {
                    if (effectsByCategory["Rendering"].Any(e => e.Id == effect.Id))
                        RenderingEffects.Add(effect);
                }
                
                if (effectsByCategory.ContainsKey("Movement"))
                {
                    if (effectsByCategory["Movement"].Any(e => e.Id == effect.Id))
                        MovementEffects.Add(effect);
                }
                
                if (effectsByCategory.ContainsKey("Color"))
                {
                    if (effectsByCategory["Color"].Any(e => e.Id == effect.Id))
                        ColorEffects.Add(effect);
                }
                
                if (effectsByCategory.ContainsKey("Distortion"))
                {
                    if (effectsByCategory["Distortion"].Any(e => e.Id == effect.Id))
                        DistortionEffects.Add(effect);
                }
                
                if (effectsByCategory.ContainsKey("Particles"))
                {
                    if (effectsByCategory["Particles"].Any(e => e.Id == effect.Id))
                        ParticleEffects.Add(effect);
                }
                
                if (effectsByCategory.ContainsKey("Audio"))
                {
                    if (effectsByCategory["Audio"].Any(e => e.Id == effect.Id))
                        AudioEffects.Add(effect);
                }
                
                if (effectsByCategory.ContainsKey("Special"))
                {
                    if (effectsByCategory["Special"].Any(e => e.Id == effect.Id))
                        SpecialEffects.Add(effect);
                }
                
                if (effectsByCategory.ContainsKey("Custom"))
                {
                    if (effectsByCategory["Custom"].Any(e => e.Id == effect.Id))
                        CustomEffects.Add(effect);
                }
            }
        }
        
        private void FilterEffects()
        {
            if (string.IsNullOrWhiteSpace(_searchTerm))
            {
                // Show all effects when no search term
                foreach (var effect in AvsEffectLibraryService.EffectLibrary)
                {
                    if (!FilteredEffectLibrary.Contains(effect))
                        FilteredEffectLibrary.Add(effect);
                }
                return;
            }

            var searchLower = _searchTerm.ToLowerInvariant();
            var filteredEffects = AvsEffectLibraryService.EffectLibrary
                .Where(effect => 
                    effect.Name.ToLowerInvariant().Contains(searchLower) ||
                    effect.DisplayName.ToLowerInvariant().Contains(searchLower) ||
                    effect.Description.ToLowerInvariant().Contains(searchLower) ||
                    effect.Type.ToString().ToLowerInvariant().Contains(searchLower) ||
                    effect.Section.ToString().ToLowerInvariant().Contains(searchLower))
                .ToList();

            // Update filtered collection
            FilteredEffectLibrary.Clear();
            foreach (var effect in filteredEffects)
            {
                FilteredEffectLibrary.Add(effect);
            }

            // Update category collections based on search
            UpdateCategoryCollections(filteredEffects);
        }

        private void UpdateCategoryCollections(List<AvsEffect> filteredEffects)
        {
            // Clear all category collections
            RenderingEffects.Clear();
            MovementEffects.Clear();
            ColorEffects.Clear();
            DistortionEffects.Clear();
            ParticleEffects.Clear();
            AudioEffects.Clear();
            SpecialEffects.Clear();
            CustomEffects.Clear();

            var effectsByCategory = AvsEffectLibraryService.GetEffectsByCategory();

            foreach (var effect in filteredEffects)
            {
                // Add to category collections based on search results
                if (effectsByCategory.ContainsKey("Rendering") && effectsByCategory["Rendering"].Any(e => e.Id == effect.Id))
                    RenderingEffects.Add(effect);
                
                if (effectsByCategory.ContainsKey("Movement") && effectsByCategory["Movement"].Any(e => e.Id == effect.Id))
                    MovementEffects.Add(effect);
                
                if (effectsByCategory.ContainsKey("Color") && effectsByCategory["Color"].Any(e => e.Id == effect.Id))
                    ColorEffects.Add(effect);
                
                if (effectsByCategory.ContainsKey("Distortion") && effectsByCategory["Distortion"].Any(e => e.Id == effect.Id))
                    DistortionEffects.Add(effect);
                
                if (effectsByCategory.ContainsKey("Particles") && effectsByCategory["Particles"].Any(e => e.Id == effect.Id))
                    ParticleEffects.Add(effect);
                
                if (effectsByCategory.ContainsKey("Audio") && effectsByCategory["Audio"].Any(e => e.Id == effect.Id))
                    AudioEffects.Add(effect);
                
                if (effectsByCategory.ContainsKey("Special") && effectsByCategory["Special"].Any(e => e.Id == effect.Id))
                    SpecialEffects.Add(effect);
                
                if (effectsByCategory.ContainsKey("Custom") && effectsByCategory["Custom"].Any(e => e.Id == effect.Id))
                    CustomEffects.Add(effect);
            }
        }
        
        public void AddEffect(string? effectId)
        {
            if (string.IsNullOrEmpty(effectId)) return;
            
            var effect = AvsEffectLibraryService.EffectLibrary.FirstOrDefault(e => e.Id == effectId);
            if (effect == null) return;
            
            // Create a copy of the effect for the preset
            var effectCopy = new AvsEffect
            {
                Id = effect.Id,
                Name = effect.Name,
                DisplayName = effect.DisplayName,
                Description = effect.Description,
                Type = effect.Type,
                Section = effect.Section,
                Parameters = new Dictionary<string, object>(effect.Parameters),
                Code = effect.Code,
                ClearEveryFrame = effect.ClearEveryFrame,
                IsEnabled = true
            };
            
            // Add to appropriate section
            switch (effect.Section)
            {
                case AvsSection.Init:
                    InitEffects.Add(effectCopy);
                    break;
                case AvsSection.Beat:
                    BeatEffects.Add(effectCopy);
                    break;
                case AvsSection.Frame:
                    FrameEffects.Add(effectCopy);
                    break;
                case AvsSection.Point:
                    PointEffects.Add(effectCopy);
                    break;
            }
            
            // Update the current preset
            UpdateCurrentPreset();
        }
        
        private void UpdateCurrentPreset()
        {
            CurrentPreset.InitEffects.Clear();
            CurrentPreset.BeatEffects.Clear();
            CurrentPreset.FrameEffects.Clear();
            CurrentPreset.PointEffects.Clear();
            
            foreach (var effect in InitEffects)
                CurrentPreset.InitEffects.Add(effect);
            foreach (var effect in BeatEffects)
                CurrentPreset.BeatEffects.Add(effect);
            foreach (var effect in FrameEffects)
                CurrentPreset.FrameEffects.Add(effect);
            foreach (var effect in PointEffects)
                CurrentPreset.PointEffects.Add(effect);
        }
        
        private void UpdateSelectedEffectParameters()
        {
            SelectedEffectParameters.Clear();
            
            if (SelectedEffect?.Parameters != null)
            {
                foreach (var param in SelectedEffect.Parameters)
                {
                    SelectedEffectParameters.Add(param);
                }
            }
        }
        
        // Command implementations
        private void NewPreset()
        {
            // Clear all effects
            InitEffects.Clear();
            BeatEffects.Clear();
            FrameEffects.Clear();
            PointEffects.Clear();
            
            // Reset preset
            CurrentPreset = new AvsPreset
            {
                Name = "New Preset",
                Author = "AVS Editor",
                Description = "A new AVS preset",
                ClearEveryFrame = true,
                BeatDetection = true,
                RandomPresetSwitching = false,
                FrameRate = 60
            };
        }
        
        private async void LoadPreset()
        {
            try
            {
                var options = new FilePickerOpenOptions
                {
                    Title = "Load AVS Preset",
                    AllowMultiple = false,
                    FileTypeFilter = new List<FilePickerFileType>
                    {
                        new("AVS Preset") { Patterns = new[] { "*.avs", "*.json" } },
                        new("All Files") { Patterns = new[] { "*.*" } }
                    }
                };

                var files = await GetStorageProvider().OpenFilePickerAsync(options);
                if (files.Count == 0) return;

                var file = files[0];
                using var stream = await file.OpenReadAsync();
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();

                // Try to parse as JSON first, then fall back to AVS format
                try
                {
                    var preset = System.Text.Json.JsonSerializer.Deserialize<AvsPreset>(content);
                    if (preset != null)
                    {
                        LoadPresetIntoUI(preset);
                        await ShowInfoAsync($"Preset loaded: {preset.Name}");
                        return;
                    }
                }
                catch
                {
                    // Not JSON, try AVS format
                }

                // Parse AVS format
                var avsPreset = await ParseAvsFormat(content);
                if (avsPreset != null)
                {
                    LoadPresetIntoUI(avsPreset);
                    await ShowInfoAsync($"Preset loaded: {avsPreset.Name}");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Failed to load preset", ex);
            }
        }

        private async void SavePreset()
        {
            try
            {
                UpdateCurrentPreset();
                
                var options = new FilePickerSaveOptions
                {
                    Title = "Save AVS Preset",
                    DefaultExtension = "avs",
                    FileTypeChoices = new List<FilePickerFileType>
                    {
                        new("AVS Preset") { Patterns = new[] { "*.avs" } },
                        new("JSON Format") { Patterns = new[] { "*.json" } }
                    }
                };

                var file = await GetStorageProvider().SaveFilePickerAsync(options);
                if (file == null) return;

                using var stream = await file.OpenWriteAsync();
                using var writer = new StreamWriter(stream);

                if (file.Name.EndsWith(".json"))
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(CurrentPreset, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    await writer.WriteAsync(json);
                }
                else
                {
                    var avsContent = ConvertToAvsFormat(CurrentPreset);
                    await writer.WriteAsync(avsContent);
                }

                await ShowInfoAsync($"Preset saved: {file.Name}");
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Failed to save preset", ex);
            }
        }

        private async void ImportPreset()
        {
            try
            {
                var options = new FilePickerOpenOptions
                {
                    Title = "Import AVS Preset",
                    AllowMultiple = false,
                    FileTypeFilter = new List<FilePickerFileType>
                    {
                        new("AVS Preset") { Patterns = new[] { "*.avs", "*.txt" } },
                        new("All Files") { Patterns = new[] { "*.*" } }
                    }
                };

                var files = await GetStorageProvider().OpenFilePickerAsync(options);
                if (files.Count == 0) return;

                var file = files[0];
                using var stream = await file.OpenReadAsync();
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();

                var preset = await ParseAvsFormat(content);
                if (preset != null)
                {
                    LoadPresetIntoUI(preset);
                    await ShowInfoAsync($"Preset imported successfully: {preset.Name}");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error importing preset.", ex);
            }
        }

        private async void ExportPreset()
        {
            try
            {
                UpdateCurrentPreset();
                
                var options = new FilePickerSaveOptions
                {
                    Title = "Export AVS Preset",
                    DefaultExtension = "avs",
                    FileTypeChoices = new List<FilePickerFileType>
                    {
                        new("AVS Format") { Patterns = new[] { "*.avs" } },
                        new("JSON Format") { Patterns = new[] { "*.json" } },
                        new("C# Code") { Patterns = new[] { "*.cs" } }
                    }
                };

                var file = await GetStorageProvider().SaveFilePickerAsync(options);
                if (file == null) return;

                using var stream = await file.OpenWriteAsync();
                using var writer = new StreamWriter(stream);

                if (file.Name.EndsWith(".json"))
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(CurrentPreset, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    await writer.WriteAsync(json);
                }
                else if (file.Name.EndsWith(".cs"))
                {
                    var csharpCode = ConvertToCSharpCode(CurrentPreset);
                    await writer.WriteAsync(csharpCode);
                }
                else
                {
                    var avsContent = ConvertToAvsFormat(CurrentPreset);
                    await writer.WriteAsync(avsContent);
                }

                await ShowInfoAsync($"Preset exported successfully: {file.Name}");
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error exporting preset.", ex);
            }
        }

        private void LoadPresetIntoUI(AvsPreset preset)
        {
            // Clear existing effects
            InitEffects.Clear();
            BeatEffects.Clear();
            FrameEffects.Clear();
            PointEffects.Clear();

            // Load effects into appropriate sections
            foreach (var effect in preset.InitEffects)
                InitEffects.Add(effect.Clone());
            foreach (var effect in preset.BeatEffects)
                BeatEffects.Add(effect.Clone());
            foreach (var effect in preset.FrameEffects)
                FrameEffects.Add(effect.Clone());
            foreach (var effect in preset.PointEffects)
                PointEffects.Add(effect.Clone());

            // Update current preset
            CurrentPreset = preset;
            UpdateCurrentPreset();
        }

        private async Task<AvsPreset?> ParseAvsFormat(string content)
        {
            try
            {
                var preset = new AvsPreset
                {
                    Name = "Imported Preset",
                    Description = "Imported from AVS file",
                    Author = "Unknown",
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };

                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var currentSection = AvsSection.Frame;
                var effectCounter = 0;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("//"))
                        continue;

                    // Check for section headers
                    if (trimmedLine.StartsWith("Init:", StringComparison.OrdinalIgnoreCase))
                    {
                        currentSection = AvsSection.Init;
                        continue;
                    }
                    if (trimmedLine.StartsWith("Beat:", StringComparison.OrdinalIgnoreCase))
                    {
                        currentSection = AvsSection.Beat;
                        continue;
                    }
                    if (trimmedLine.StartsWith("Frame:", StringComparison.OrdinalIgnoreCase))
                    {
                        currentSection = AvsSection.Frame;
                        continue;
                    }
                    if (trimmedLine.StartsWith("Point:", StringComparison.OrdinalIgnoreCase))
                    {
                        currentSection = AvsSection.Point;
                        continue;
                    }

                    // Parse effect lines
                    if (trimmedLine.Contains("(") && trimmedLine.Contains(")"))
                    {
                        var effect = ParseEffectLine(trimmedLine, currentSection, effectCounter++);
                        if (effect != null)
                        {
                            preset.AddEffect(effect);
                        }
                    }
                }

                return preset;
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error parsing AVS format.", ex);
                return null;
            }
        }

        private AvsEffect? ParseEffectLine(string line, AvsSection section, int order)
        {
            try
            {
                var openParen = line.IndexOf('(');
                var closeParen = line.LastIndexOf(')');
                
                if (openParen == -1 || closeParen == -1 || openParen >= closeParen)
                    return null;

                var effectName = line.Substring(0, openParen).Trim();
                var parameters = line.Substring(openParen + 1, closeParen - openParen - 1);

                var effect = new AvsEffect
                {
                    Id = $"effect_{order}",
                    Name = effectName,
                    DisplayName = effectName,
                    Description = $"Imported {effectName} effect",
                    Type = GetEffectTypeFromName(effectName),
                    Section = section,
                    Order = order,
                    IsEnabled = true,
                    ClearEveryFrame = false
                };

                // Parse parameters
                var paramPairs = parameters.Split(',');
                foreach (var param in paramPairs)
                {
                    var kv = param.Split('=', 2);
                    if (kv.Length == 2)
                    {
                        var key = kv[0].Trim();
                        var value = kv[1].Trim().Trim('"', '\'');
                        effect.Parameters[key] = value;
                    }
                }

                return effect;
            }
            catch
            {
                return null;
            }
        }

        private AvsEffectType GetEffectTypeFromName(string name)
        {
            return name.ToLowerInvariant() switch
            {
                "clear" => AvsEffectType.Clear,
                "blend" => AvsEffectType.Blend,
                "superscope" => AvsEffectType.Superscope,
                "spectrum" => AvsEffectType.Spectrum,
                "text" => AvsEffectType.Text,
                "picture" => AvsEffectType.Picture,
                "movement" => AvsEffectType.Movement,
                "color" => AvsEffectType.Color,
                "particle" => AvsEffectType.Particle,
                "wave" => AvsEffectType.Wave,
                "fountain" => AvsEffectType.Fountain,
                "scatter" => AvsEffectType.Scatter,
                "beat" => AvsEffectType.Beat,
                "set" => AvsEffectType.Set,
                "bpm" => AvsEffectType.BPM,
                "onbeat" => AvsEffectType.OnBeat,
                "beatdetect" => AvsEffectType.BeatDetect,
                _ => AvsEffectType.Custom
            };
        }

        private string ConvertToAvsFormat(AvsPreset preset)
        {
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine($"// AVS Preset: {preset.Name}");
            sb.AppendLine($"// Author: {preset.Author}");
            sb.AppendLine($"// Description: {preset.Description}");
            sb.AppendLine($"// Created: {preset.CreatedDate:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            // Init section
            if (preset.InitEffects.Count > 0)
            {
                sb.AppendLine("Init:");
                foreach (var effect in preset.InitEffects)
                {
                    sb.AppendLine($"  {effect.Name}({FormatEffectParameters(effect)})");
                }
                sb.AppendLine();
            }

            // Beat section
            if (preset.BeatEffects.Count > 0)
            {
                sb.AppendLine("Beat:");
                foreach (var effect in preset.BeatEffects)
                {
                    sb.AppendLine($"  {effect.Name}({FormatEffectParameters(effect)})");
                }
                sb.AppendLine();
            }

            // Frame section
            if (preset.FrameEffects.Count > 0)
            {
                sb.AppendLine("Frame:");
                foreach (var effect in preset.FrameEffects)
                {
                    sb.AppendLine($"  {effect.Name}({FormatEffectParameters(effect)})");
                }
                sb.AppendLine();
            }

            // Point section
            if (preset.PointEffects.Count > 0)
            {
                sb.AppendLine("Point:");
                foreach (var effect in preset.PointEffects)
                {
                    sb.AppendLine($"  {effect.Name}({FormatEffectParameters(effect)})");
                }
            }

            return sb.ToString();
        }

        private string FormatEffectParameters(AvsEffect effect)
        {
            if (effect.Parameters.Count == 0)
                return "";

            var paramList = effect.Parameters.Select(kv => $"{kv.Key}={kv.Value}");
            return string.Join(", ", paramList);
        }

        private string ConvertToCSharpCode(AvsPreset preset)
        {
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine("using PhoenixVisualizer.Core.Models;");
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine("public static class GeneratedAvsPreset");
            sb.AppendLine("{");
            sb.AppendLine("    public static AvsPreset Create()");
            sb.AppendLine("    {");
            sb.AppendLine("        var preset = new AvsPreset");
            sb.AppendLine("        {");
            sb.AppendLine($"            Name = \"{preset.Name}\",");
            sb.AppendLine($"            Author = \"{preset.Author}\",");
            sb.AppendLine($"            Description = \"{preset.Description}\",");
            sb.AppendLine($"            CreatedDate = DateTime.Parse(\"{preset.CreatedDate:yyyy-MM-dd HH:mm:ss}\"),");
            sb.AppendLine($"            ModifiedDate = DateTime.Parse(\"{preset.ModifiedDate:yyyy-MM-dd HH:mm:ss}\")");
            sb.AppendLine("        };");
            sb.AppendLine();

            // Add effects
            foreach (var section in new[] { preset.InitEffects, preset.BeatEffects, preset.FrameEffects, preset.PointEffects })
            {
                foreach (var effect in section)
                {
                    sb.AppendLine($"        var {effect.Name.ToLowerInvariant()}Effect = new AvsEffect");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            Id = \"{effect.Id}\",");
                    sb.AppendLine($"            Name = \"{effect.Name}\",");
                    sb.AppendLine($"            DisplayName = \"{effect.DisplayName}\",");
                    sb.AppendLine($"            Description = \"{effect.Description}\",");
                    sb.AppendLine($"            Type = AvsEffectType.{effect.Type},");
                    sb.AppendLine($"            Section = AvsSection.{effect.Section},");
                    sb.AppendLine($"            Order = {effect.Order},");
                    sb.AppendLine($"            IsEnabled = {effect.IsEnabled.ToString().ToLower()},");
                    sb.AppendLine($"            ClearEveryFrame = {effect.ClearEveryFrame.ToString().ToLower()}");
                    sb.AppendLine("        };");

                    // Add parameters
                    if (effect.Parameters.Count > 0)
                    {
                        sb.AppendLine();
                        foreach (var param in effect.Parameters)
                        {
                            var value = param.Value is string ? $"\"{param.Value}\"" : param.Value.ToString();
                            sb.AppendLine($"        {effect.Name.ToLowerInvariant()}Effect.Parameters[\"{param.Key}\"] = {value};");
                        }
                    }

                    sb.AppendLine();
                    sb.AppendLine($"        preset.AddEffect({effect.Name.ToLowerInvariant()}Effect);");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("        return preset;");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private IStorageProvider GetStorageProvider()
        {
            // This would need to be injected or accessed through the UI context
            // For now, return null and handle the case in the calling methods
            return null!;
        }
        
        private async void TestPreset()
        {
            try
            {
                UpdateCurrentPreset();
                
                // Send to bridge for testing
                var success = await _bridge.LoadPresetAsync(CurrentPreset);
                if (success)
                {
                    success = await _bridge.StartPresetAsync();
                    if (success)
                    {
                        await ShowInfoAsync("Preset started successfully for testing.");
                    }
                    else
                    {
                        await ShowErrorAsync("Failed to start preset for testing.", 
                            new Exception("Bridge.StartPresetAsync returned false"));
                    }
                }
                else
                {
                    await ShowErrorAsync("Failed to load preset for testing.", 
                        new Exception("Bridge.LoadPresetAsync returned false"));
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error testing preset.", ex);
            }
        }
        
        private async void ToggleAudio()
        {
            if (_isAudioPlaying)
            {
                await _audioProvider.StopAsync();
                IsAudioPlaying = false;
            }
            else
            {
                await _audioProvider.StartAsync();
                IsAudioPlaying = true;
            }
        }
        
        private async void StopAudio()
        {
            await _audioProvider.StopAsync();
            IsAudioPlaying = false;
        }
        
        private void ClearSection(AvsSection section)
        {
            switch (section)
            {
                case AvsSection.Init:
                    InitEffects.Clear();
                    break;
                case AvsSection.Beat:
                    BeatEffects.Clear();
                    break;
                case AvsSection.Frame:
                    FrameEffects.Clear();
                    break;
                case AvsSection.Point:
                    PointEffects.Clear();
                    break;
            }
            
            UpdateCurrentPreset();
        }
        
        private void MoveEffectUp(AvsEffect? effect)
        {
            if (effect == null) return;
            
            // Find which collection contains this effect
            var collection = GetEffectCollection(effect);
            if (collection == null) return;
            
            var index = collection.IndexOf(effect);
            if (index > 0)
            {
                collection.Move(index, index - 1);
                UpdateCurrentPreset();
            }
        }
        
        private void MoveEffectDown(AvsEffect? effect)
        {
            if (effect == null) return;
            
            // Find which collection contains this effect
            var collection = GetEffectCollection(effect);
            if (collection == null) return;
            
            var index = collection.IndexOf(effect);
            if (index >= 0 && index < collection.Count - 1)
            {
                collection.Move(index, index + 1);
                UpdateCurrentPreset();
            }
        }
        
        private void CopyEffect(AvsEffect? effect)
        {
            if (effect == null) return;
            
            // Create a deep copy of the effect
            var effectCopy = new AvsEffect
            {
                Id = effect.Id,
                Name = effect.Name,
                DisplayName = effect.DisplayName + " (Copy)",
                Description = effect.Description,
                Type = effect.Type,
                Section = effect.Section,
                Parameters = new Dictionary<string, object>(effect.Parameters),
                Code = effect.Code,
                ClearEveryFrame = effect.ClearEveryFrame,
                IsEnabled = effect.IsEnabled
            };
            
            // Add to appropriate collection
            switch (effect.Section)
            {
                case AvsSection.Init:
                    InitEffects.Add(effectCopy);
                    break;
                case AvsSection.Beat:
                    BeatEffects.Add(effectCopy);
                    break;
                case AvsSection.Frame:
                    FrameEffects.Add(effectCopy);
                    break;
                case AvsSection.Point:
                    PointEffects.Add(effectCopy);
                    break;
            }
            
            UpdateCurrentPreset();
        }
        
        private ObservableCollection<AvsEffect>? GetEffectCollection(AvsEffect effect)
        {
            if (InitEffects.Contains(effect)) return InitEffects;
            if (BeatEffects.Contains(effect)) return BeatEffects;
            if (FrameEffects.Contains(effect)) return FrameEffects;
            if (PointEffects.Contains(effect)) return PointEffects;
            return null;
        }
        
        private void RemoveEffect(AvsEffect? effect)
        {
            if (effect == null) return;
            
            // Remove from appropriate collection
            if (InitEffects.Contains(effect))
                InitEffects.Remove(effect);
            else if (BeatEffects.Contains(effect))
                BeatEffects.Remove(effect);
            else if (FrameEffects.Contains(effect))
                FrameEffects.Remove(effect);
            else if (PointEffects.Contains(effect))
                PointEffects.Remove(effect);
            
            UpdateCurrentPreset();
        }
        
        private async void SendToMainWindow()
        {
            try
            {
                UpdateCurrentPreset();
                
                // Send the preset to the main window via the bridge
                var success = await _bridge.LoadPresetAsync(CurrentPreset);
                if (success)
                {
                    await ShowInfoAsync("Preset sent to main window.");
                    // Optionally notify main window here if needed
                }
                else
                {
                    await ShowErrorAsync("Failed to send preset to main window.", 
                        new Exception("Bridge.LoadPresetAsync returned false"));
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error sending preset to main window.", ex);
            }
        }
        
        // Bridge event handlers
        private void OnPresetLoaded(object? sender, AvsPresetEventArgs e)
        {
            _ = ShowInfoAsync($"Preset loaded: {e.Message}");
        }
        
        private void OnPresetStarted(object? sender, AvsPresetEventArgs e)
        {
            _ = ShowInfoAsync($"Preset started: {e.Message}");
        }
        
        private void OnPresetStopped(object? sender, AvsPresetEventArgs e)
        {
            _ = ShowInfoAsync($"Preset stopped: {e.Message}");
        }
        
        private void OnBridgeError(object? sender, AvsErrorEventArgs e)
        {
            _ = ShowErrorAsync($"Bridge error: {e.Context}", e.Error);
        }
        
        // Centralized UX helpers
        private Task ShowInfoAsync(string message) => Task.CompletedTask; // TODO: Implement toast notification
        private Task ShowErrorAsync(string message, Exception ex) => Task.CompletedTask; // TODO: Implement error dialog
        
        public void Dispose()
        {
            _bridge?.Dispose();
            _renderer?.Dispose();
            _audioProvider?.Dispose();
        }
        
        /// <summary>
        /// Sets the preview canvas for the renderer
        /// </summary>
        public void SetPreviewCanvas(object canvas)
        {
            if (_renderer is AvaloniaAvsRenderer avaloniaRenderer && canvas is Avalonia.Controls.Canvas avaloniaCanvas)
            {
                avaloniaRenderer.SetRenderCanvas(avaloniaCanvas);
            }
        }
    }
}

