using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Services;
using PhoenixVisualizer.Editor.Services;

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
            // This would implement search filtering
            // For now, just show all effects
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
        
        private void LoadPreset()
        {
            // TODO: Implement preset loading
            Debug.WriteLine("Load preset not implemented yet");
        }
        
        private void SavePreset()
        {
            // TODO: Implement preset saving
            Debug.WriteLine("Save preset not implemented yet");
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
                        Debug.WriteLine("Preset started successfully for testing");
                    }
                    else
                    {
                        Debug.WriteLine("Failed to start preset for testing");
                    }
                }
                else
                {
                    Debug.WriteLine("Failed to load preset for testing");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error testing preset: {ex.Message}");
            }
        }
        
        private void ImportPreset()
        {
            // TODO: Implement preset importing
            Debug.WriteLine("Import preset not implemented yet");
        }
        
        private void ExportPreset()
        {
            // TODO: Implement preset exporting
            Debug.WriteLine("Export preset not implemented yet");
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
                    Debug.WriteLine("Preset sent to main window successfully");
                    // TODO: Notify main window that preset is ready
                }
                else
                {
                    Debug.WriteLine("Failed to send preset to main window");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending preset to main window: {ex.Message}");
            }
        }
        
        // Bridge event handlers
        private void OnPresetLoaded(object? sender, AvsPresetEventArgs e)
        {
            Debug.WriteLine($"Preset loaded: {e.Message}");
        }
        
        private void OnPresetStarted(object? sender, AvsPresetEventArgs e)
        {
            Debug.WriteLine($"Preset started: {e.Message}");
        }
        
        private void OnPresetStopped(object? sender, AvsPresetEventArgs e)
        {
            Debug.WriteLine($"Preset stopped: {e.Message}");
        }
        
        private void OnBridgeError(object? sender, AvsErrorEventArgs e)
        {
            Debug.WriteLine($"Bridge error: {e.Context} - {e.Error.Message}");
        }
        
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

