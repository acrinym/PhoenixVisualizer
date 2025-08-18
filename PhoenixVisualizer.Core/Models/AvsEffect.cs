using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoenixVisualizer.Core.Models;

public enum AvsEffectType
{
    // Init section effects
    Set,
    BPM,
    
    // Beat section effects
    OnBeat,
    BeatDetect,
    
    // Rendering effects
    Clear,
    Blend,
    Buffer,
    Text,
    Picture,
    
    // Movement effects
    Movement,
    Rotation,
    Zoom,
    Scroll,
    
    // Color effects
    Color,
    Brightness,
    Contrast,
    Saturation,
    Hue,
    
    // Distortion effects
    Bump,
    Water,
    Ripple,
    Wave,
    
    // Particle effects
    Particle,
    Dot,
    Fountain,
    Scatter,
    
    // Audio reactive
    Spectrum,
    Oscilloscope,
    Beat,
    
    // Special effects
    Mosaic,
    Grain,
    Blur,
    Mirror,
    Kaleidoscope,
    
    // Custom/APE
    Custom,
    APE,
    
    // Superscopes (existing)
    Superscope
}

public class AvsEffect
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AvsEffectType Type { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Order { get; set; }
    
    // Effect parameters
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    // AVS section this effect belongs to
    public AvsSection Section { get; set; } = AvsSection.Frame;
    
    // Clear every frame option
    public bool ClearEveryFrame { get; set; } = false;
    
    // Effect-specific code (for superscopes, custom effects, etc.)
    public string Code { get; set; } = string.Empty;
    
    // Parent effect (for hierarchical effects)
    public AvsEffect? Parent { get; set; }
    public List<AvsEffect> Children { get; set; } = new();
    
    // Validation
    public bool IsValid { get; set; } = true;
    public string ValidationMessage { get; set; } = string.Empty;
    
    public AvsEffect Clone()
    {
        return new AvsEffect
        {
            Id = this.Id,
            Name = this.Name,
            DisplayName = this.DisplayName,
            Description = this.Description,
            Type = this.Type,
            IsEnabled = this.IsEnabled,
            Order = this.Order,
            Parameters = new Dictionary<string, object>(this.Parameters),
            Section = this.Section,
            ClearEveryFrame = this.ClearEveryFrame,
            Code = this.Code,
            Parent = this.Parent,
            Children = new List<AvsEffect>(this.Children),
            IsValid = this.IsValid,
            ValidationMessage = this.ValidationMessage
        };
    }
}

public enum AvsSection
{
    Init,       // Initialization code
    Beat,       // Beat detection code
    Frame,      // Per-frame code
    Point,      // Per-point code (for superscopes)
    PerFrame,   // Alternative per-frame
    PerPixel    // Per-pixel code
}

public class AvsPreset
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime ModifiedDate { get; set; } = DateTime.Now;
    
    // Effect lists organized by section
    public List<AvsEffect> InitEffects { get; set; } = new();
    public List<AvsEffect> BeatEffects { get; set; } = new();
    public List<AvsEffect> FrameEffects { get; set; } = new();
    public List<AvsEffect> PointEffects { get; set; } = new();
    
    // Settings
    public bool ClearEveryFrame { get; set; } = true;
    public int FrameRate { get; set; } = 30;
    public bool BeatDetection { get; set; } = true;
    public bool RandomPresetSwitching { get; set; } = false;
    
    // Get all effects in order
    public List<AvsEffect> GetAllEffects()
    {
        var allEffects = new List<AvsEffect>();
        allEffects.AddRange(InitEffects);
        allEffects.AddRange(BeatEffects);
        allEffects.AddRange(FrameEffects);
        allEffects.AddRange(PointEffects);
        return allEffects.OrderBy(e => e.Order).ToList();
    }
    
    // Get effects by section
    public List<AvsEffect> GetEffectsBySection(AvsSection section)
    {
        return section switch
        {
            AvsSection.Init => InitEffects,
            AvsSection.Beat => BeatEffects,
            AvsSection.Frame => FrameEffects,
            AvsSection.Point => PointEffects,
            _ => new List<AvsEffect>()
        };
    }
    
    // Add effect to appropriate section
    public void AddEffect(AvsEffect effect)
    {
        var targetList = GetEffectsBySection(effect.Section);
        effect.Order = targetList.Count;
        targetList.Add(effect);
    }
    
    // Remove effect
    public void RemoveEffect(AvsEffect effect)
    {
        var targetList = GetEffectsBySection(effect.Section);
        targetList.Remove(effect);
        
        // Reorder remaining effects
        for (int i = 0; i < targetList.Count; i++)
        {
            targetList[i].Order = i;
        }
    }
    
    // Move effect up/down in its section
    public void MoveEffect(AvsEffect effect, bool moveUp)
    {
        var targetList = GetEffectsBySection(effect.Section);
        var index = targetList.IndexOf(effect);
        
        if (index == -1) return;
        
        if (moveUp && index > 0)
        {
            // Swap with previous
            var temp = targetList[index - 1];
            targetList[index - 1] = effect;
            targetList[index] = temp;
            
            effect.Order = index - 1;
            temp.Order = index;
        }
        else if (!moveUp && index < targetList.Count - 1)
        {
            // Swap with next
            var temp = targetList[index + 1];
            targetList[index + 1] = effect;
            targetList[index] = temp;
            
            effect.Order = index + 1;
            temp.Order = index;
        }
    }
}
