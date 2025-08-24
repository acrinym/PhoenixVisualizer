using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Models;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Effects;

namespace PhoenixVisualizer.Core.Effects.Interfaces
{
    public interface IEffectNode
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }
        string Category { get; }
        Version Version { get; }
        bool IsEnabled { get; set; }
        EffectGraph Graph { get; set; }
        IReadOnlyList<EffectPort> InputPorts { get; }
        IReadOnlyList<EffectPort> OutputPorts { get; }
        object Process(Dictionary<string, object> inputs, AudioFeatures audioFeatures);
        bool ValidateConfiguration();
        void Reset();
        void Initialize();
        string GetSettingsSummary();
    }
}
