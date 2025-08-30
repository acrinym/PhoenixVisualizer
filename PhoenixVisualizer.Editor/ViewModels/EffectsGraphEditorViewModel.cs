using Avalonia;
using Avalonia.Platform;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using Avalonia.Layout;
using PhoenixVisualizer.Core.Effects.Graph;
using PhoenixVisualizer.Core.Effects.Interfaces;
using PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Editor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

// Suppress obsolete API warnings for file dialogs - will be updated in future phase
#pragma warning disable CS0618

namespace PhoenixVisualizer.Editor.ViewModels
{
    public partial class EffectsGraphEditorViewModel : ViewModelBase
    {
        #region Private Fields

        private EffectsGraphManager _graphManager;
        private EffectsGraph _currentGraph;
        private IEffectNode? _selectedNode;
        private string _selectedTab = "Properties";
        private string _nodeSearchText = "";
        private bool _isPlaying = false;
        private int _previewFPS = 60;
        private Size _previewResolution = new Size(640, 480);
        private int _targetFPS = 60;
        private string _qualityLevel = "High";
        private string _statusMessage = "Ready";
        private Window? _previewWindow;

        #endregion

        #region Properties

        public string GraphName
        {
            get => _currentGraph?.Name ?? "Untitled Graph";
            set
            {
                if (_currentGraph != null)
                {
                    _currentGraph.Name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string GraphDescription
        {
            get => _currentGraph?.Description ?? "";
            set
            {
                if (_currentGraph != null)
                {
                    _currentGraph.Description = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool GraphEnabled
        {
            get => _currentGraph?.IsEnabled ?? false;
            set
            {
                if (_currentGraph != null)
                {
                    _currentGraph.IsEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CurrentGraphName => _currentGraph?.Name ?? "No Graph";
        public int NodeCount => _currentGraph?.GetNodes().Count ?? 0;
        public int ConnectionCount => _currentGraph?.GetConnections().Count ?? 0;

        public string SelectedTab
        {
            get => _selectedTab;
            set => SetProperty(ref _selectedTab, value);
        }

        public string NodeSearchText
        {
            get => _nodeSearchText;
            set
            {
                SetProperty(ref _nodeSearchText, value);
                FilterNodes();
            }
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set => SetProperty(ref _isPlaying, value);
        }

        public int PreviewFPS
        {
            get => _previewFPS;
            set => SetProperty(ref _previewFPS, value);
        }

        public Size PreviewResolution
        {
            get => _previewResolution;
            set => SetProperty(ref _previewResolution, value);
        }

        public int TargetFPS
        {
            get => _targetFPS;
            set => SetProperty(ref _targetFPS, value);
        }

        public string QualityLevel
        {
            get => _qualityLevel;
            set => SetProperty(ref _qualityLevel, value);
        }

        public string SelectedNodeName => _selectedNode?.Name ?? "No Node Selected";
        public bool IsFullscreenPreviewActive
        {
            get => _previewWindow != null;
            private set => OnPropertyChanged(nameof(IsFullscreenPreviewActive));
        }

        #endregion

        #region Collections

        public ObservableCollection<IEffectNode> PatternEffectNodes { get; } = new();
        public ObservableCollection<IEffectNode> ColorEffectNodes { get; } = new();
        public ObservableCollection<IEffectNode> VideoEffectNodes { get; } = new();
        public ObservableCollection<IEffectNode> AudioEffectNodes { get; } = new();
        public ObservableCollection<IEffectNode> UtilityEffectNodes { get; } = new();

        public ObservableCollection<string> AvailableQualityLevels { get; } = new()
        {
            "Low", "Medium", "High", "Ultra"
        };

        public ObservableCollection<NodePropertyViewModel> SelectedNodeProperties { get; } = new();
        public ObservableCollection<ConnectionViewModel> SelectedNodeConnections { get; } = new();

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        public ICommand? NewGraphCommand { get; private set; }
        public ICommand? OpenGraphCommand { get; private set; }
        public ICommand? SaveGraphCommand { get; private set; }
        public ICommand? PlayCommand { get; private set; }
        public ICommand? PauseCommand { get; private set; }
        public ICommand? StopCommand { get; private set; }
        public ICommand? ValidateGraphCommand { get; private set; }
        public ICommand? ClearGraphCommand { get; private set; }
        public ICommand? SelectTabCommand { get; private set; }
        public ICommand? ToggleFullscreenPreviewCommand { get; private set; }

        #endregion

        #region Constructor

        public EffectsGraphEditorViewModel()
        {
            _graphManager = new EffectsGraphManager();
            _currentGraph = _graphManager.CreateGraph("New Effects Graph", "A new effects composition");
            
            InitializeCommands();
            InitializeNodePalette();
            StartPreviewTimer();
        }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            NewGraphCommand = new RelayCommand(NewGraph);
            OpenGraphCommand = new RelayCommand(OpenGraph);
            SaveGraphCommand = new RelayCommand(SaveGraph);
            PlayCommand = new RelayCommand(Play);
            PauseCommand = new RelayCommand(Pause);
            StopCommand = new RelayCommand(Stop);
            ValidateGraphCommand = new RelayCommand(ValidateGraph);
            ClearGraphCommand = new RelayCommand(ClearGraph);
            SelectTabCommand = new RelayCommand<string>(SelectTab);
            ToggleFullscreenPreviewCommand = new RelayCommand(ToggleFullscreenPreview);
        }

        private void InitializeNodePalette()
        {
            // Register all available effect node types
            RegisterEffectNodes();
            
            // Categorize nodes
            CategorizeNodes();
        }

        private void RegisterEffectNodes()
        {
            // Pattern Effects
            _graphManager.RegisterNodeType(new StarfieldEffectsNode());
            _graphManager.RegisterNodeType(new ParticleSwarmEffectsNode());
            _graphManager.RegisterNodeType(new OscilloscopeStarEffectsNode());
            _graphManager.RegisterNodeType(new RotatingStarPatternsNode());
            _graphManager.RegisterNodeType(new VectorFieldEffectsNode());
            
            // Color Effects
            _graphManager.RegisterNodeType(new ColorFadeEffectsNode());
            _graphManager.RegisterNodeType(new ContrastEffectsNode());
            _graphManager.RegisterNodeType(new BrightnessEffectsNode());
            _graphManager.RegisterNodeType(new ColorReductionEffectsNode());
            _graphManager.RegisterNodeType(new ColorreplaceEffectsNode());
            
            // Video Effects
            _graphManager.RegisterNodeType(new AVIVideoEffectsNode());
            _graphManager.RegisterNodeType(new BlurEffectsNode());
            _graphManager.RegisterNodeType(new BlitEffectsNode());
            _graphManager.RegisterNodeType(new CompositeEffectsNode());
            _graphManager.RegisterNodeType(new MirrorEffectsNode());
            
            // Audio Effects
            _graphManager.RegisterNodeType(new BeatDetectionEffectsNode());
            _graphManager.RegisterNodeType(new BPMEffectsNode());
            _graphManager.RegisterNodeType(new CustomBPMEffectsNode());
            _graphManager.RegisterNodeType(new OscilloscopeRingEffectsNode());
            _graphManager.RegisterNodeType(new TimeDomainScopeEffectsNode());
            
            // Utility Effects
            _graphManager.RegisterNodeType(new ClearFrameEffectsNode());
            _graphManager.RegisterNodeType(new CommentEffectsNode());
            _graphManager.RegisterNodeType(new DotFontRenderingNode());
            _graphManager.RegisterNodeType(new PictureEffectsNode());
#pragma warning disable CA1416 // TextEffectsNode is only supported on Windows
            _graphManager.RegisterNodeType(new TextEffectsNode());
#pragma warning restore CA1416
        }

        private void CategorizeNodes()
        {
            var allNodes = _graphManager.GetAvailableNodeTypes();
            
            foreach (var node in allNodes.Values)
            {
                switch (node.Category.ToLower())
                {
                    case "pattern effects":
                    case "pattern":
                        PatternEffectNodes.Add(node);
                        break;
                    case "color effects":
                    case "color":
                        ColorEffectNodes.Add(node);
                        break;
                    case "video effects":
                    case "video":
                        VideoEffectNodes.Add(node);
                        break;
                    case "audio effects":
                    case "audio":
                        AudioEffectNodes.Add(node);
                        break;
                    default:
                        UtilityEffectNodes.Add(node);
                        break;
                }
            }
        }

        private void FilterNodes()
        {
            // Apply search filter to all node collections
            ApplyFilterToCollection(PatternEffectNodes);
            ApplyFilterToCollection(ColorEffectNodes);
            ApplyFilterToCollection(VideoEffectNodes);
            ApplyFilterToCollection(AudioEffectNodes);
            ApplyFilterToCollection(UtilityEffectNodes);
        }

        private void ApplyFilterToCollection(ObservableCollection<IEffectNode> collection)
        {
            // In a real implementation, you'd want to maintain the original list
            // and filter the view. For now, we'll just show/hide based on search
            var filteredItems = collection.Where(n => 
                string.IsNullOrEmpty(_nodeSearchText) ||
                n.Name.Contains(_nodeSearchText, StringComparison.OrdinalIgnoreCase) ||
                n.Description.Contains(_nodeSearchText, StringComparison.OrdinalIgnoreCase) ||
                n.Category.Contains(_nodeSearchText, StringComparison.OrdinalIgnoreCase));
            
            // Update visibility or create filtered collection
        }

        #endregion

        #region Graph Operations

        private void NewGraph()
        {
            _currentGraph = _graphManager.CreateGraph("New Effects Graph", "A new effects composition");
            OnPropertyChanged(nameof(GraphName));
            OnPropertyChanged(nameof(GraphDescription));
            OnPropertyChanged(nameof(CurrentGraphName));
            OnPropertyChanged(nameof(NodeCount));
            OnPropertyChanged(nameof(ConnectionCount));
        }

        private async void OpenGraph()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Open Effects Graph",
                    Filters = new List<FileDialogFilter>
                    {
                        new FileDialogFilter { Name = "Phoenix Effects Graph", Extensions = new List<string> { "phx" } },
                        new FileDialogFilter { Name = "AVS Preset Files", Extensions = new List<string> { "avs" } },
                        new FileDialogFilter { Name = "All Files", Extensions = new List<string> { "*" } }
                    },
                    AllowMultiple = false
                };

                var result = await dialog.ShowAsync(Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.Window ?? throw new InvalidOperationException("No main window found"));
                if (result != null && result.Length > 0)
                {
                    string filePath = result[0];
                    string extension = Path.GetExtension(filePath).ToLower();

                    if (extension == ".avs")
                    {
                        await LoadAvsPreset(filePath);
                    }
                    else if (extension == ".phx")
                    {
                        await LoadPhoenixGraph(filePath);
                    }

                    StatusMessage = $"Opened: {Path.GetFileName(filePath)}";
                    OnPropertyChanged(nameof(GraphName));
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error opening graph: {ex.Message}";
            }
        }

        private async void SaveGraph()
        {
            try
            {
                if (_currentGraph == null)
                {
                    StatusMessage = "No graph to save";
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Title = "Save Effects Graph",
                    Filters = new List<FileDialogFilter>
                    {
                        new FileDialogFilter { Name = "Phoenix Effects Graph", Extensions = new List<string> { "phx" } },
                        new FileDialogFilter { Name = "All Files", Extensions = new List<string> { "*" } }
                    },
                    DefaultExtension = "phx",
                    InitialFileName = $"{_currentGraph.Name.Replace(" ", "_")}.phx"
                };

                var result = await dialog.ShowAsync(Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.Window ?? throw new InvalidOperationException("No main window found"));
                if (result != null)
                {
                    await SavePhoenixGraph(result);
                    StatusMessage = $"Saved: {Path.GetFileName(result)}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving graph: {ex.Message}";
            }
        }

        private async Task LoadAvsPreset(string filePath)
        {
            try
            {
                // Use the existing AVS converter
                var phoenixJson = PhoenixVisualizer.Core.Avs.AvsPresetConverter.LoadAvs(filePath);

                // Parse the JSON and create a graph from it
                using var doc = JsonDocument.Parse(phoenixJson);
                var root = doc.RootElement;

                string graphName = Path.GetFileNameWithoutExtension(filePath);
                _currentGraph = _graphManager.CreateGraph(graphName, "Loaded from AVS preset");

                // Parse effects and add to graph
                if (root.TryGetProperty("effects", out var effects))
                {
                    foreach (var effect in effects.EnumerateArray())
                    {
                        string effectType = effect.GetProperty("type").GetString() ?? "";
                        // Map AVS effect type to Phoenix node type
                        string nodeType = MapAvsEffectToPhoenixNode(effectType);

                        if (!string.IsNullOrEmpty(nodeType))
                        {
                            var node = _graphManager.CreateNodeInstance(nodeType);
                            if (node != null)
                            {
                                _currentGraph.AddNode(node);
                            }
                        }
                    }
                }

                // Load embedded code if present
                if (root.TryGetProperty("code", out var code))
                {
                    // Code handling would go here
                }

                OnPropertyChanged(nameof(GraphName));
                OnPropertyChanged(nameof(NodeCount));
                OnPropertyChanged(nameof(ConnectionCount));
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load AVS preset: {ex.Message}");
            }
        }

        private async Task LoadPhoenixGraph(string filePath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string graphName = root.GetProperty("name").GetString() ?? "Loaded Graph";
                _currentGraph = _graphManager.CreateGraph(graphName, "Loaded from file");

                // Parse nodes
                if (root.TryGetProperty("nodes", out var nodes))
                {
                    foreach (var nodeElement in nodes.EnumerateArray())
                    {
                        string nodeType = nodeElement.GetProperty("type").GetString() ?? "";
                        var node = _graphManager.CreateNodeInstance(nodeType);
                        if (node != null)
                        {
                            _currentGraph.AddNode(node);
                        }
                    }
                }

                OnPropertyChanged(nameof(GraphName));
                OnPropertyChanged(nameof(NodeCount));
                OnPropertyChanged(nameof(ConnectionCount));
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load Phoenix graph: {ex.Message}");
            }
        }

        private async Task SavePhoenixGraph(string filePath)
        {
            try
            {
                var graphData = new
                {
                    name = _currentGraph?.Name ?? "Effects Graph",
                    description = "Saved Phoenix effects graph",
                    nodes = _currentGraph != null
                        ? _currentGraph.GetNodes().Select(n => new
                        {
                            id = n.Key,
                            type = n.Value.GetType().Name,
                            name = n.Value.Name,
                            position = new { x = 0, y = 0 } // Would need actual position data
                        }).Cast<object>().ToArray()
                        : Array.Empty<object>(),
                    connections = _currentGraph != null
                        ? _currentGraph.GetConnections().Select(c => new
                        {
                            from = c.Value.SourceNodeId,
                            to = c.Value.TargetNodeId,
                            fromPort = c.Value.SourcePortName,
                            toPort = c.Value.TargetPortName
                        }).Cast<object>().ToArray()
                        : Array.Empty<object>()
                };

                var json = JsonSerializer.Serialize(graphData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save Phoenix graph: {ex.Message}");
            }
        }

        private string MapAvsEffectToPhoenixNode(string avsEffectType)
        {
            // Map common AVS effect types to Phoenix node types
            return avsEffectType switch
            {
                "superscope_script" => "SuperscopeEffectsNode",
                "blur" => "BlurEffectsNode",
                "brightness" => "BrightnessEffectsNode",
                "contrast" => "ContrastEffectsNode",
                "color_fade" => "ColorfadeEffectsNode",
                "invert" => "InvertEffectsNode",
                "mosaic" => "MosaicEffectsNode",
                _ => "" // Unknown effect type
            };
        }

        private void CreateDemoGraph()
        {
            _currentGraph = _graphManager.CreateEffectChain("Demo Graph", 
                "StarfieldEffectsNode", "ParticleSwarmEffectsNode", "ColorFadeEffectsNode");
            
            OnPropertyChanged(nameof(GraphName));
            OnPropertyChanged(nameof(GraphDescription));
            OnPropertyChanged(nameof(CurrentGraphName));
            OnPropertyChanged(nameof(NodeCount));
            OnPropertyChanged(nameof(ConnectionCount));
        }

        private void ValidateGraph()
        {
            if (_currentGraph != null)
            {
                var isValid = _currentGraph.ValidateGraph();
                var errors = _currentGraph.GetValidationErrors();
                
                if (isValid)
                {
                    // Show success message
                    System.Diagnostics.Debug.WriteLine("Graph validation successful!");
                }
                else
                {
                    // Show validation errors
                    foreach (var error in errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Validation error: {error}");
                    }
                }
            }
        }

        private void ClearGraph()
        {
            if (_currentGraph != null)
            {
                _currentGraph.Clear();
                OnPropertyChanged(nameof(NodeCount));
                OnPropertyChanged(nameof(ConnectionCount));
            }
        }

        #endregion

        #region Playback Control

        private void Play()
        {
            IsPlaying = true;
            // Start processing the graph
            StartGraphProcessing();
        }

        private void Pause()
        {
            IsPlaying = false;
            // Pause graph processing
        }

        private void Stop()
        {
            IsPlaying = false;
            // Stop graph processing
        }

        private void StartGraphProcessing()
        {
            if (_currentGraph != null && IsPlaying)
            {
                Task.Run(async () =>
                {
                    while (IsPlaying)
                    {
                        try
                        {
                            // Create mock audio features for preview
                            var audioFeatures = CreateMockAudioFeatures();
                            
                            // Process the graph
                            var results = _currentGraph.ProcessGraph(audioFeatures);
                            
                            // Update preview
                            UpdatePreview(results);
                            
                            // Maintain target FPS
                            await Task.Delay(1000 / TargetFPS);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Graph processing error: {ex.Message}");
                            break;
                        }
                    }
                });
            }
        }

        private AudioFeatures CreateMockAudioFeatures()
        {
            // Generate dynamic mock audio data that changes over time
            var time = (float)(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) * 0.001f;

            // Create realistic audio data arrays
            var leftChannel = new float[1024];
            var rightChannel = new float[1024];
            var centerChannel = new float[1024];
            var fftData = new float[512];

            // Generate sine wave with some harmonics
            float baseFrequency = 440f; // A4 note
            for (int i = 0; i < 1024; i++)
            {
                float t = i / 44100f; // Sample at 44.1kHz

                // Generate a mix of frequencies
                float sample = (float)(
                    Math.Sin(2 * Math.PI * baseFrequency * t) * 0.5f +           // Fundamental
                    Math.Sin(2 * Math.PI * baseFrequency * 2 * t) * 0.3f +       // Octave
                    Math.Sin(2 * Math.PI * baseFrequency * 3 * t) * 0.2f +       // Fifth
                    Math.Sin(2 * Math.PI * baseFrequency * 4 * t) * 0.1f         // Third octave
                );

                // Add some noise for realism
                sample += (float)(Random.Shared.NextDouble() - 0.5) * 0.05f;

                // Apply envelope (attack/decay)
                float envelope = Math.Min(t * 10f, 1f - (t - 0.1f) * 2f);
                envelope = Math.Max(0, envelope);
                sample *= envelope;

                leftChannel[i] = sample;
                rightChannel[i] = sample * 0.8f; // Slight stereo difference
                centerChannel[i] = sample * 0.6f;
            }

            // Generate FFT data
            for (int i = 0; i < 512; i++)
            {
                float frequency = i * 44100f / 1024f;
                float magnitude = 0f;

                // Add peaks at harmonic frequencies
                if (Math.Abs(frequency - baseFrequency) < 10f) magnitude = 0.8f;
                else if (Math.Abs(frequency - baseFrequency * 2) < 10f) magnitude = 0.6f;
                else if (Math.Abs(frequency - baseFrequency * 3) < 10f) magnitude = 0.4f;
                else if (Math.Abs(frequency - baseFrequency * 4) < 10f) magnitude = 0.2f;

                // Add some random variation
                magnitude += (float)Random.Shared.NextDouble() * 0.1f;
                fftData[i] = Math.Max(0, magnitude);
            }

            // Calculate derived values
            float rms = 0f, bass = 0f, mid = 0f, treble = 0f;
            for (int i = 0; i < 1024; i++)
            {
                rms += leftChannel[i] * leftChannel[i];
            }
            rms = (float)Math.Sqrt(rms / 1024f);

            // Frequency band analysis
            for (int i = 0; i < 512; i++)
            {
                float frequency = i * 44100f / 1024f;
                if (frequency < 250) bass += fftData[i];
                else if (frequency < 2000) mid += fftData[i];
                else treble += fftData[i];
            }

            bass = Math.Min(1f, bass / 50f);
            mid = Math.Min(1f, mid / 100f);
            treble = Math.Min(1f, treble / 200f);

            // Dynamic beat detection
            bool beat = Math.Sin(time * 2f) > 0.7f;
            float beatIntensity = beat ? (float)(0.5f + 0.5f * Math.Sin(time * 4f)) : 0f;

            return new AudioFeatures
            {
                Beat = beat,
                BeatIntensity = beatIntensity,
                RMS = rms,
                Bass = bass,
                Mid = mid,
                Treble = treble,
                LeftChannel = leftChannel,
                RightChannel = rightChannel,
                CenterChannel = centerChannel,
                BPM = 120.0f,
                SampleRate = 44100,
                Volume = rms,
                IsPlaying = true,
                PlaybackPosition = 0f,
                TotalDuration = 60f
            };
        }

        private void UpdatePreview(Dictionary<string, object> results)
        {
            // Update preview surface with graph results
            // This would integrate with the RenderSurface
            PreviewFPS = (int)(1000.0 / (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond));
        }

        #endregion

        #region Preview Management

        private void StartPreviewTimer()
        {
            // Start a timer to update preview FPS
            var timer = new System.Timers.Timer(1000);
            timer.Elapsed += (s, e) =>
            {
                // Update FPS display
                OnPropertyChanged(nameof(PreviewFPS));
            };
            timer.Start();
        }

        private void ToggleFullscreenPreview()
        {
            // Implement fullscreen preview
            if (_previewWindow == null)
            {
                // Create fullscreen preview window
                _previewWindow = new Window
                {
                    Title = "Effects Graph Preview - Fullscreen",
                    WindowState = WindowState.FullScreen,
                    CanResize = false,
                    ShowInTaskbar = false
                };
                
                // Create preview content
                var previewContent = new Border
                {
                    Background = new SolidColorBrush(Colors.Black),
                    Child = new TextBlock
                    {
                        Text = "Fullscreen Preview Mode",
                        Foreground = new SolidColorBrush(Colors.White),
                        FontSize = 24,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                
                _previewWindow.Content = previewContent;
                _previewWindow.Show();
                
                // Update UI state
                IsFullscreenPreviewActive = true;
            }
            else
            {
                // Close fullscreen preview
                _previewWindow.Close();
                _previewWindow = null;
                
                // Update UI state
                IsFullscreenPreviewActive = false;
            }
        }

        #endregion

        #region Tab Management

        private void SelectTab(string? tabName)
        {
            if (tabName != null)
            {
                SelectedTab = tabName;
            }
        }

        #endregion

        #region Node Selection

        public void SelectNode(IEffectNode node)
        {
            _selectedNode = node;
            OnPropertyChanged(nameof(SelectedNodeName));
            
            // Update node properties
            UpdateSelectedNodeProperties();
            
            // Update node connections
            UpdateSelectedNodeConnections();
        }

        private void UpdateSelectedNodeProperties()
        {
            SelectedNodeProperties.Clear();
            
            if (_selectedNode != null)
            {
                // Add basic properties
                SelectedNodeProperties.Add(new NodePropertyViewModel("ID", _selectedNode.Id, true));
                SelectedNodeProperties.Add(new NodePropertyViewModel("Name", _selectedNode.Name, false));
                SelectedNodeProperties.Add(new NodePropertyViewModel("Description", _selectedNode.Description, false));
                SelectedNodeProperties.Add(new NodePropertyViewModel("Category", _selectedNode.Category, true));
                SelectedNodeProperties.Add(new NodePropertyViewModel("Version", _selectedNode.Version.ToString(), true));
                SelectedNodeProperties.Add(new NodePropertyViewModel("Enabled", _selectedNode.IsEnabled.ToString(), false));
            }
        }

        private void UpdateSelectedNodeConnections()
        {
            SelectedNodeConnections.Clear();
            
            if (_selectedNode != null && _currentGraph != null)
            {
                var connections = _currentGraph.GetConnectionsForNode(_selectedNode.Id);
                
                foreach (var connection in connections)
                {
                    var isInput = connection.TargetNodeId == _selectedNode.Id;
                    var description = isInput ? 
                        $"Input: {connection.TargetPortName}" : 
                        $"Output: {connection.SourcePortName}";
                    
                    SelectedNodeConnections.Add(new ConnectionViewModel
                    {
                        Description = description,
                        Type = connection.DataType.Name
                    });
                }
            }
        }

        #endregion

        #region Visual Integration

        public void UpdateGraphFromVisual(List<VisualNode> visualNodes, List<VisualConnection> visualConnections)
        {
            // This method would be called from the view to update the graph model
            // based on visual changes (node positions, connections, etc.)
            
            // For now, just update the property change notifications
            OnPropertyChanged(nameof(NodeCount));
            OnPropertyChanged(nameof(ConnectionCount));
        }

        #endregion
    }

    #region Helper ViewModels

    public class NodePropertyViewModel
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsReadOnly { get; set; }

        public NodePropertyViewModel(string name, string value, bool isReadOnly)
        {
            Name = name;
            Value = value;
            IsReadOnly = isReadOnly;
        }
    }

    public class ConnectionViewModel
    {
        public string Description { get; set; } = "";
        public string Type { get; set; } = "";
    }

    #endregion
}