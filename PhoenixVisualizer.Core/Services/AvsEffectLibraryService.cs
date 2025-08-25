using System.Collections.Generic;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Services;

public class AvsEffectLibraryService
{
    private static readonly List<AvsEffect> _effectLibrary = new();
    private static bool _isInitialized = false;

    public static List<AvsEffect> EffectLibrary
    {
        get
        {
            if (!_isInitialized)
                InitializeEffectLibrary();
            return _effectLibrary;
        }
    }

    public static void InitializeEffectLibrary()
    {
        if (_isInitialized) return;

        _effectLibrary.Clear();

        // === UTILITY EFFECTS ===
        AddEffect(new AvsEffect
        {
            Id = "comment",
            Name = "comment",
            DisplayName = "Comment",
            Description = "Adds a comment that does not affect rendering",
            Type = AvsEffectType.Comment,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["text"] = string.Empty
            }
        });

        // === INIT SECTION EFFECTS ===
        AddEffect(new AvsEffect
        {
            Id = "set",
            Name = "set",
            DisplayName = "Set Variable",
            Description = "Sets a variable value in init section",
            Type = AvsEffectType.Set,
            Section = AvsSection.Init,
            Parameters = new Dictionary<string, object>
            {
                ["variable"] = "var1",
                ["value"] = 0.0f,
                ["expression"] = ""
            },
            Code = "// Example: set(\"var1\", 100);\n// or: set(\"var1\", sin(t) * 50);"
        });

        AddEffect(new AvsEffect
        {
            Id = "bpm",
            Name = "bpm",
            DisplayName = "BPM Detection",
            Description = "Configures BPM detection settings",
            Type = AvsEffectType.BPM,
            Section = AvsSection.Init,
            Parameters = new Dictionary<string, object>
            {
                ["minBPM"] = 60,
                ["maxBPM"] = 200,
                ["sensitivity"] = 0.5f,
                ["adapt"] = true
            }
        });

        // === BEAT SECTION EFFECTS ===
        AddEffect(new AvsEffect
        {
            Id = "onbeat",
            Name = "onbeat",
            DisplayName = "On Beat",
            Description = "Executes code when beat is detected",
            Type = AvsEffectType.OnBeat,
            Section = AvsSection.Beat,
            Parameters = new Dictionary<string, object>
            {
                ["skip"] = 0,
                ["code"] = ""
            },
            Code = "// Example: onbeat(0, \"var1 = var1 + 1;\");"
        });

        AddEffect(new AvsEffect
        {
            Id = "beatdetect",
            Name = "beatdetect",
            DisplayName = "Beat Detection",
            Description = "Custom beat detection algorithm",
            Type = AvsEffectType.BeatDetect,
            Section = AvsSection.Beat,
            Parameters = new Dictionary<string, object>
            {
                ["sensitivity"] = 0.5f,
                ["threshold"] = 0.1f,
                ["decay"] = 0.9f
            }
        });

        // === FRAME SECTION EFFECTS ===
        AddEffect(new AvsEffect
        {
            Id = "clear",
            Name = "clear",
            DisplayName = "Clear",
            Description = "Clears the screen with specified color",
            Type = AvsEffectType.Clear,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["color"] = 0xFF000000,
                ["firstFrameOnly"] = false
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "blend",
            Name = "blend",
            DisplayName = "Blend",
            Description = "Blends current frame with previous frame",
            Type = AvsEffectType.Blend,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["mode"] = "Normal",
                ["opacity"] = 0.5f
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "buffer",
            Name = "buffer",
            DisplayName = "Buffer",
            Description = "Saves or restores frame buffer",
            Type = AvsEffectType.Buffer,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["operation"] = "Save",
                ["bufferId"] = 0
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "text",
            Name = "text",
            DisplayName = "Text",
            Description = "Renders text on screen",
            Type = AvsEffectType.Text,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["text"] = "Hello World",
                ["x"] = 0.5f,
                ["y"] = 0.5f,
                ["color"] = 0xFFFFFFFF,
                ["size"] = 24.0f,
                ["font"] = "Arial"
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "picture",
            Name = "picture",
            DisplayName = "Picture",
            Description = "Renders an image on screen",
            Type = AvsEffectType.Picture,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["file"] = "",
                ["x"] = 0.0f,
                ["y"] = 0.0f,
                ["width"] = 100.0f,
                ["height"] = 100.0f,
                ["aspectRatio"] = true
            }
        });

        // === MOVEMENT EFFECTS ===
        AddEffect(new AvsEffect
        {
            Id = "movement",
            Name = "movement",
            DisplayName = "Movement",
            Description = "Moves the entire visualization",
            Type = AvsEffectType.Movement,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["x"] = 0f,
                ["y"] = 0f,
                ["mode"] = "Absolute"
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "rotation",
            Name = "rotation",
            DisplayName = "Rotation",
            Description = "Rotates the visualization",
            Type = AvsEffectType.Rotation,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["angle"] = 0f,
                ["centerX"] = 0.5f,
                ["centerY"] = 0.5f
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "zoom",
            Name = "zoom",
            DisplayName = "Zoom",
            Description = "Zooms the visualization in/out",
            Type = AvsEffectType.Zoom,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["scale"] = 1.0f,
                ["centerX"] = 0.5f,
                ["centerY"] = 0.5f
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "scroll",
            Name = "scroll",
            DisplayName = "Scroll",
            Description = "Scrolls the visualization",
            Type = AvsEffectType.Scroll,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["direction"] = "Horizontal",
                ["speed"] = 1.0f,
                ["wrap"] = true
            }
        });

        // === COLOR EFFECTS ===
        AddEffect(new AvsEffect
        {
            Id = "color",
            Name = "color",
            DisplayName = "Color",
            Description = "Applies color transformations",
            Type = AvsEffectType.Color,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["red"] = 1.0f,
                ["green"] = 1.0f,
                ["blue"] = 1.0f,
                ["alpha"] = 1.0f
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "brightness",
            Name = "brightness",
            DisplayName = "Brightness",
            Description = "Adjusts brightness of the visualization",
            Type = AvsEffectType.Brightness,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["value"] = 0.0f,
                ["mode"] = "Additive"
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "contrast",
            Name = "contrast",
            DisplayName = "Contrast",
            Description = "Adjusts contrast of the visualization",
            Type = AvsEffectType.Contrast,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["value"] = 1.0f
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "saturation",
            Name = "saturation",
            DisplayName = "Saturation",
            Description = "Adjusts color saturation",
            Type = AvsEffectType.Saturation,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["value"] = 1.0f
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "hue",
            Name = "hue",
            DisplayName = "Hue Shift",
            Description = "Shifts the hue of colors",
            Type = AvsEffectType.Hue,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["shift"] = 0.0f
            }
        });

        // === DISTORTION EFFECTS ===
        AddEffect(new AvsEffect
        {
            Id = "bump",
            Name = "bump",
            DisplayName = "Bump",
            Description = "Creates bump mapping distortion",
            Type = AvsEffectType.Bump,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["intensity"] = 0.5f,
                ["source"] = "Audio",
                ["invert"] = false
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "water",
            Name = "water",
            DisplayName = "Water",
            Description = "Creates water ripple effects",
            Type = AvsEffectType.Water,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["intensity"] = 0.5f,
                ["speed"] = 1.0f,
                ["scale"] = 1.0f
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "ripple",
            Name = "ripple",
            DisplayName = "Ripple",
            Description = "Creates circular ripple effects",
            Type = AvsEffectType.Ripple,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["centerX"] = 0.5f,
                ["centerY"] = 0.5f,
                ["amplitude"] = 0.1f,
                ["frequency"] = 1.0f
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "wave",
            Name = "wave",
            DisplayName = "Wave",
            Description = "Creates wave distortion effects",
            Type = AvsEffectType.Wave,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["amplitude"] = 0.1f,
                ["frequency"] = 1.0f,
                ["direction"] = "Horizontal"
            }
        });

        // === PARTICLE EFFECTS ===
        AddEffect(new AvsEffect
        {
            Id = "particle",
            Name = "particle",
            DisplayName = "Particle",
            Description = "Creates particle systems",
            Type = AvsEffectType.Particle,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["count"] = 100,
                ["size"] = 2.0f,
                ["speed"] = 1.0f,
                ["color"] = 0xFFFFFFFF
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "dot",
            Name = "dot",
            DisplayName = "Dot",
            Description = "Creates dot patterns",
            Type = AvsEffectType.Dot,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["size"] = 1.0f,
                ["spacing"] = 10.0f,
                ["color"] = 0xFFFFFFFF
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "fountain",
            Name = "fountain",
            DisplayName = "Fountain",
            Description = "Creates fountain particle effects",
            Type = AvsEffectType.Fountain,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["count"] = 50,
                ["speed"] = 1.0f,
                ["gravity"] = 0.1f,
                ["color"] = 0xFFFFFFFF
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "scatter",
            Name = "scatter",
            DisplayName = "Scatter",
            Description = "Creates scattered particle effects",
            Type = AvsEffectType.Scatter,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["count"] = 200,
                ["radius"] = 100.0f,
                ["speed"] = 1.0f,
                ["color"] = 0xFFFFFFFF
            }
        });

        // === AUDIO REACTIVE EFFECTS ===
        AddEffect(new AvsEffect
        {
            Id = "spectrum",
            Name = "spectrum",
            DisplayName = "Spectrum",
            Description = "Displays FFT spectrum data",
            Type = AvsEffectType.Spectrum,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["mode"] = "Bars",
                ["scale"] = 1.0f,
                ["color"] = 0xFFFFFFFF
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "oscilloscope",
            Name = "oscilloscope",
            DisplayName = "Oscilloscope",
            Description = "Displays waveform data",
            Type = AvsEffectType.Oscilloscope,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["mode"] = "Line",
                ["scale"] = 1.0f,
                ["color"] = 0xFFFFFFFF
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "beat",
            Name = "beat",
            DisplayName = "Beat",
            Description = "Beat-reactive effects",
            Type = AvsEffectType.Beat,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["intensity"] = 1.0f,
                ["decay"] = 0.9f
            }
        });

        // === SPECIAL EFFECTS ===
        AddEffect(new AvsEffect
        {
            Id = "mosaic",
            Name = "mosaic",
            DisplayName = "Mosaic",
            Description = "Creates mosaic/pixelation effect",
            Type = AvsEffectType.Mosaic,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["size"] = 10.0f,
                ["mode"] = "Square"
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "grain",
            Name = "grain",
            DisplayName = "Grain",
            Description = "Adds film grain effect",
            Type = AvsEffectType.Grain,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["intensity"] = 0.1f,
                ["size"] = 1.0f
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "blur",
            Name = "blur",
            DisplayName = "Blur",
            Description = "Applies blur effect",
            Type = AvsEffectType.Blur,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["radius"] = 1.0f,
                ["mode"] = "Gaussian"
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "mirror",
            Name = "mirror",
            DisplayName = "Mirror",
            Description = "Creates mirror/reflection effects",
            Type = AvsEffectType.Mirror,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["mode"] = "Horizontal",
                ["intensity"] = 1.0f
            }
        });

        AddEffect(new AvsEffect
        {
            Id = "kaleidoscope",
            Name = "kaleidoscope",
            DisplayName = "Kaleidoscope",
            Description = "Creates kaleidoscope effects",
            Type = AvsEffectType.Kaleidoscope,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["segments"] = 6,
                ["rotation"] = 0.0f
            }
        });

        // === SUPERSCOPES (POINT SECTION) ===
        AddEffect(new AvsEffect
        {
            Id = "superscope",
            Name = "superscope",
            DisplayName = "Superscope",
            Description = "Custom mathematical visualization",
            Type = AvsEffectType.Superscope,
            Section = AvsSection.Point,
            Parameters = new Dictionary<string, object>
            {
                ["name"] = "MyScope",
                ["points"] = 256
            },
            Code = "// Your superscope code here\nx = sin(t) * 100;\ny = cos(t) * 100;\nred = sin(t) * 0.5 + 0.5;\ngreen = cos(t) * 0.5 + 0.5;\nblue = 0.5;"
        });

        // === CUSTOM/APE EFFECTS ===
        AddEffect(new AvsEffect
        {
            Id = "custom",
            Name = "custom",
            DisplayName = "Custom Effect",
            Description = "Custom effect with user-defined code",
            Type = AvsEffectType.Custom,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["name"] = "CustomEffect"
            },
            Code = "// Your custom effect code here"
        });

        AddEffect(new AvsEffect
        {
            Id = "ape",
            Name = "ape",
            DisplayName = "APE Plugin",
            Description = "Advanced Plugin Extension",
            Type = AvsEffectType.APE,
            Section = AvsSection.Frame,
            Parameters = new Dictionary<string, object>
            {
                ["pluginName"] = "PluginName",
                ["parameters"] = ""
            }
        });

        _isInitialized = true;
    }

    private static void AddEffect(AvsEffect effect)
    {
        _effectLibrary.Add(effect);
    }

    public static AvsEffect? GetEffectById(string id)
    {
        return EffectLibrary.FirstOrDefault(e => e.Id == id);
    }

    public static List<AvsEffect> GetEffectsByType(AvsEffectType type)
    {
        return EffectLibrary.Where(e => e.Type == type).ToList();
    }

    public static List<AvsEffect> GetEffectsBySection(AvsSection section)
    {
        return EffectLibrary.Where(e => e.Section == section).ToList();
    }

    public static List<AvsEffect> SearchEffects(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return EffectLibrary;

        return EffectLibrary.Where(e => 
            e.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            e.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            e.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    public static AvsEffect CreateEffectInstance(string effectId)
    {
        var template = GetEffectById(effectId);
        if (template == null)
            throw new ArgumentException($"Effect with ID '{effectId}' not found");

        return template.Clone();
    }

    // Get effects organized by section for the editor
    public static Dictionary<AvsSection, List<AvsEffect>> GetEffectsBySection()
    {
        return EffectLibrary
            .GroupBy(e => e.Section)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    // Get effect categories for better organization
    public static Dictionary<string, List<AvsEffect>> GetEffectsByCategory()
    {
        var categories = new Dictionary<string, List<AvsEffect>>
        {
            ["Utility"] = GetEffectsByType(AvsEffectType.Comment).ToList(),
            ["Rendering"] = GetEffectsByType(AvsEffectType.Clear).Concat(GetEffectsByType(AvsEffectType.Blend)).ToList(),
            ["Movement"] = GetEffectsByType(AvsEffectType.Movement).Concat(GetEffectsByType(AvsEffectType.Rotation)).Concat(GetEffectsByType(AvsEffectType.Zoom)).ToList(),
            ["Color"] = GetEffectsByType(AvsEffectType.Color).Concat(GetEffectsByType(AvsEffectType.Brightness)).Concat(GetEffectsByType(AvsEffectType.Contrast)).ToList(),
            ["Distortion"] = GetEffectsByType(AvsEffectType.Bump).Concat(GetEffectsByType(AvsEffectType.Water)).Concat(GetEffectsByType(AvsEffectType.Ripple)).ToList(),
            ["Particles"] = GetEffectsByType(AvsEffectType.Particle).Concat(GetEffectsByType(AvsEffectType.Dot)).Concat(GetEffectsByType(AvsEffectType.Fountain)).ToList(),
            ["Audio"] = GetEffectsByType(AvsEffectType.Spectrum).Concat(GetEffectsByType(AvsEffectType.Oscilloscope)).Concat(GetEffectsByType(AvsEffectType.Beat)).ToList(),
            ["Special"] = GetEffectsByType(AvsEffectType.Mosaic).Concat(GetEffectsByType(AvsEffectType.Grain)).Concat(GetEffectsByType(AvsEffectType.Blur)).ToList(),
            ["Custom"] = GetEffectsByType(AvsEffectType.Superscope).Concat(GetEffectsByType(AvsEffectType.Custom)).Concat(GetEffectsByType(AvsEffectType.APE)).ToList()
        };

        return categories;
    }
}
