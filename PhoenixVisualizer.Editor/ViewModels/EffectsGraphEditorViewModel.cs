using Avalonia;
using Avalonia.Platform;
using PhoenixVisualizer.Core.Effects.Graph;
using PhoenixVisualizer.Core.Effects.Interfaces;
using PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Editor.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

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

        private void OpenGraph()
        {
            // TODO: Implement file open dialog
            // For now, create a demo graph
            CreateDemoGraph();
        }

        private void SaveGraph()
        {
            // TODO: Implement file save dialog
            // For now, just validate the graph
            ValidateGraph();
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
            return new AudioFeatures
            {
                Beat = true,
                BeatIntensity = 0.8f,
                RMS = 0.6f,
                Bass = 0.7f,
                Mid = 0.5f,
                Treble = 0.4f,
                LeftChannel = new float[1024],
                RightChannel = new float[1024],
                CenterChannel = new float[1024]
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
            // TODO: Implement fullscreen preview
            System.Diagnostics.Debug.WriteLine("Toggle fullscreen preview");
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