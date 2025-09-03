using ReactiveUI;
using System.Collections.Generic;

namespace PhoenixVisualizer.Views
{
    public class ParameterEditorViewModel : ReactiveObject
    {
        private string _visualizerId = "";
        private string _visualizerName = "";
        private Dictionary<string, ParameterSystem.ParameterDefinition> _parameters = new();

        public string VisualizerId 
        { 
            get => _visualizerId; 
            set => this.RaiseAndSetIfChanged(ref _visualizerId, value); 
        }
        
        public string VisualizerName 
        { 
            get => _visualizerName; 
            set => this.RaiseAndSetIfChanged(ref _visualizerName, value); 
        }
        
        public Dictionary<string, ParameterSystem.ParameterDefinition> Parameters 
        { 
            get => _parameters; 
            set => this.RaiseAndSetIfChanged(ref _parameters, value); 
        }
    }
}
