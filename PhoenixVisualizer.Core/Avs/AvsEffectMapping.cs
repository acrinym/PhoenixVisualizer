using PhoenixVisualizer.Core.Effects.Nodes.AvsEffects;

namespace PhoenixVisualizer.Core.Avs;

/// <summary>
/// Maps AVS effect indices to Phoenix C# effect classes
/// Based on original VIS_AVS rlib.cpp initfx() function
/// </summary>
public static class AvsEffectMapping
{
    /// <summary>
    /// Built-in effect indices from original VIS_AVS (0-45)
    /// Order matches DECLARE_EFFECT calls in rlib.cpp lines 115-161
    /// </summary>
    public static readonly Dictionary<int, Type> BuiltinEffects = new()
    {
        // From original VIS_AVS rlib.cpp initfx() function:
        { 0, typeof(SpectrumVisualizationEffectsNode) },  // R_SimpleSpectrum
        { 1, typeof(DotPlaneEffectsNode) },               // R_DotPlane  
        { 2, typeof(OscilloscopeStarEffectsNode) },        // R_OscStars
        { 3, typeof(FadeoutEffectsNode) },                 // R_FadeOut
        { 4, typeof(BlitterFeedbackEffectsNode) },         // R_BlitterFB
        { 5, typeof(NFClearEffectsNode) },                 // R_NFClear
        { 6, typeof(BlurEffectsNode) },                    // R_Blur
        { 7, typeof(BassSpinEffectsNode) },                // R_BSpin
        { 8, typeof(PartsEffectsNode) },                   // R_Parts
        { 9, typeof(RotBlitEffectsNode) },                 // R_RotBlit
        { 10, typeof(SVPEffectsNode) },                    // R_SVP (Superscope/Vector/Point)
        { 11, typeof(ColorFadeEffectsNode) },              // R_ColorFade
        { 12, typeof(ContrastEnhancementEffectsNode) },    // R_ContrastEnhance
        { 13, typeof(RotatingStarPatternsNode) },          // R_RotStar
        { 14, typeof(OscilloscopeRingEffectsNode) },       // R_OscRings
        { 15, typeof(TransitionEffectsNode) },             // R_Trans
        { 16, typeof(ScatterEffectsNode) },                // R_Scat
        { 17, typeof(DotGridEffectsNode) },                // R_DotGrid
        { 18, typeof(StackEffectsNode) },                  // R_Stack
        { 19, typeof(DotFountainEffectsNode) },            // R_DotFountain
        { 20, typeof(WaterEffectsNode) },                  // R_Water
        { 21, typeof(CommentEffectsNode) },                // R_Comment
        { 22, typeof(BrightnessEffectsNode) },             // R_Brightness
        { 23, typeof(InterleaveEffectsNode) },             // R_Interleave
        { 24, typeof(GrainEffectsNode) },                  // R_Grain
        { 25, typeof(ClearFrameEffectsNode) },             // R_Clear
        { 26, typeof(MirrorEffectsNode) },                 // R_Mirror
        { 27, typeof(StarfieldEffectsNode) },              // R_StarField
        { 28, typeof(TextEffectsNode) },                   // R_Text
        { 29, typeof(BumpMappingEffectsNode) },            // R_Bump
        { 30, typeof(MosaicEffectsNode) },                 // R_Mosaic
        { 31, typeof(WaterBumpEffectsNode) },              // R_WaterBump
        { 32, typeof(AVIVideoEffectsNode) },               // R_AVI
        { 33, typeof(BPMEffectsNode) },                    // R_Bpm
        { 34, typeof(PictureEffectsNode) },                // R_Picture
        { 35, typeof(DDMEffectsNode) },                    // R_DDM (Dynamic Distance Modifier)
        { 36, typeof(SuperscopeEffectsNode) },             // R_SScope
        { 37, typeof(InvertEffectsNode) },                 // R_Invert
        { 38, typeof(OnetoneEffectsNode) },                // R_Onetone
        { 39, typeof(TimeDomainScopeEffectsNode) },        // R_Timescope
        { 40, typeof(LinesEffectsNode) },                  // R_LineMode
        { 41, typeof(InterferencePatternsEffectsNode) },   // R_Interferences
        { 42, typeof(ShiftEffectsNode) },                  // R_Shift
        { 43, typeof(DynamicMovementEffectsNode) },        // R_DMove
        { 44, typeof(FastBrightnessEffectsNode) },         // R_FastBright
        { 45, typeof(DynamicColorModulationEffectsNode) }  // R_DColorMod
    };

    /// <summary>
    /// Named APE effects that were converted to built-ins
    /// From NamedApeToBuiltinTrans[] in rlib.cpp lines 167-181
    /// </summary>
    public static readonly Dictionary<string, int> NamedApeToBuiltin = new()
    {
        { "Winamp Brightness APE v1", 22 },
        { "Winamp Interleave APE v1", 23 },
        { "Winamp Grain APE v1", 24 },
        { "Winamp ClearScreen APE v1", 25 },
        { "Nullsoft MIRROR v1", 26 },
        { "Winamp Starfield v1", 27 },
        { "Winamp Text v1", 28 },
        { "Winamp Bump v1", 29 },
        { "Winamp Mosaic v1", 30 },
        { "Winamp AVIAPE v1", 32 },
        { "Nullsoft Picture Rendering v1", 34 },
        { "Winamp Interf APE v1", 41 }
    };

    /// <summary>
    /// Additional built-in APE effects from initbuiltinape()
    /// These have indices starting from DLLRENDERBASE (typically 46+)
    /// </summary>
    public static readonly Dictionary<string, Type> BuiltinApeEffects = new()
    {
        { "Channel Shift", typeof(ChannelShiftEffectsNode) },
        { "Color Reduction", typeof(ColorReductionEffectsNode) },
        { "Multiplier", typeof(MultiplierEffectsNode) },
        { "Holden04: Video Delay", typeof(VideoDelayEffectsNode) },
        { "Holden05: Multi Delay", typeof(MultiDelayEffectsNode) }
    };

    /// <summary>
    /// Get Phoenix effect type from AVS effect index
    /// </summary>
    /// <param name="index">AVS effect index (0-based)</param>
    /// <returns>Phoenix effect type or null if not found</returns>
    public static Type? GetEffectType(int index)
    {
        return BuiltinEffects.TryGetValue(index, out var type) ? type : null;
    }

    /// <summary>
    /// Get Phoenix effect type from named APE effect
    /// </summary>
    /// <param name="apeName">APE effect name</param>
    /// <returns>Phoenix effect type or null if not found</returns>
    public static Type? GetEffectTypeFromApe(string apeName)
    {
        // Check if it's a named APE that maps to a builtin
        if (NamedApeToBuiltin.TryGetValue(apeName, out var builtinIndex))
        {
            return GetEffectType(builtinIndex);
        }

        // Check builtin APE effects
        if (BuiltinApeEffects.TryGetValue(apeName, out var type))
        {
            return type;
        }

        return null;
    }

    /// <summary>
    /// Get AVS effect index from Phoenix effect type
    /// </summary>
    /// <param name="effectType">Phoenix effect type</param>
    /// <returns>AVS effect index or -1 if not found</returns>
    public static int GetEffectIndex(Type effectType)
    {
        foreach (var kvp in BuiltinEffects)
        {
            if (kvp.Value == effectType)
                return kvp.Key;
        }
        return -1;
    }

    /// <summary>
    /// Get all supported effect indices
    /// </summary>
    /// <returns>Array of supported effect indices</returns>
    public static int[] GetSupportedIndices()
    {
        return BuiltinEffects.Keys.ToArray();
    }

    /// <summary>
    /// Get all supported APE effect names
    /// </summary>
    /// <returns>Array of supported APE effect names</returns>
    public static string[] GetSupportedApeNames()
    {
        return NamedApeToBuiltin.Keys.Concat(BuiltinApeEffects.Keys).ToArray();
    }

    /// <summary>
    /// Check if an effect index is supported
    /// </summary>
    /// <param name="index">AVS effect index</param>
    /// <returns>True if supported, false otherwise</returns>
    public static bool IsEffectSupported(int index)
    {
        return BuiltinEffects.ContainsKey(index);
    }

    /// <summary>
    /// Check if an APE effect name is supported
    /// </summary>
    /// <param name="apeName">APE effect name</param>
    /// <returns>True if supported, false otherwise</returns>
    public static bool IsApeEffectSupported(string apeName)
    {
        return NamedApeToBuiltin.ContainsKey(apeName) || BuiltinApeEffects.ContainsKey(apeName);
    }

    /// <summary>
    /// Get a human-readable name for an effect index
    /// </summary>
    /// <param name="index">AVS effect index</param>
    /// <returns>Effect name or "Unknown Effect" if not found</returns>
    public static string GetEffectName(int index)
    {
        var type = GetEffectType(index);
        if (type != null)
        {
            // Remove "EffectsNode" suffix and make it readable
            var name = type.Name.Replace("EffectsNode", "").Replace("Node", "");
            return System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
        }
        return "Unknown Effect";
    }
}