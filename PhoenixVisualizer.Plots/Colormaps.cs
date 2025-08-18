using System;
using System.Collections.Generic;

namespace PhoenixVisualizer.Plots;

/// <summary>
/// Handy-dandy color palettes 🎨 for plots and visualizers.
/// Includes Matplotlib-esque ramps and a couple genre-based presets.
/// </summary>
public static class Colormaps
{
    // Stop values encoded as ARGB hex (alpha=FF for opaque)
    private static readonly uint[] ViridisStops =
    [
        0xFF440154, // purple
        0xFF472C7A, // indigo
        0xFF3B528B, // blue
        0xFF21908C, // teal
        0xFF5EC962, // green
        0xFFFDE725  // yellow
    ];

    private static readonly uint[] PlasmaStops =
    [
        0xFF0D0887, // deep purple
        0xFF6A00A8, // violet
        0xFFCB4679, // magenta
        0xFFF89441, // orange
        0xFFF0F921  // yellow
    ];

    private static readonly uint[] MagmaStops =
    [
        0xFF000004, // black
        0xFF3B0F70, // indigo
        0xFF8C2981, // purple
        0xFFDE4968, // pink
        0xFFF66E5B, // orange
        0xFFFEE08B  // yellow
    ];

    private static readonly uint[] InfernoStops =
    [
        0xFF000004, // black
        0xFF320A5A, // indigo
        0xFF7F1D4E, // maroon
        0xFFBA3655, // crimson
        0xFFF1711F, // orange
        0xFFFEE51A  // yellow
    ];

    // Genre → palette mapping (just for fun 🎶)
    private static readonly Dictionary<string, uint[]> GenrePalettes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["rock"] = InfernoStops,
        ["metal"] = InfernoStops,
        ["trance"] = PlasmaStops,
        ["edm"] = PlasmaStops,
        ["jazz"] = ViridisStops,
        ["classical"] = MagmaStops,
    };

    /// <summary>Sample Viridis at position t ∈ [0,1].</summary>
    public static uint Viridis(float t) => Sample(ViridisStops, t);

    /// <summary>Sample Plasma at position t ∈ [0,1].</summary>
    public static uint Plasma(float t) => Sample(PlasmaStops, t);

    /// <summary>Sample Magma at position t ∈ [0,1].</summary>
    public static uint Magma(float t) => Sample(MagmaStops, t);

    /// <summary>Sample Inferno at position t ∈ [0,1].</summary>
    public static uint Inferno(float t) => Sample(InfernoStops, t);

    /// <summary>Grab a palette by genre name (fallback to Viridis).</summary>
    public static uint Genre(string genre, float t)
        => Sample(GenrePalettes.TryGetValue(genre, out var stops) ? stops : ViridisStops, t);

    private static uint Sample(uint[] stops, float t)
    {
        if (stops.Length == 0) return 0xFF000000;
        t = Math.Clamp(t, 0f, 1f);
        float scaled = t * (stops.Length - 1);
        int i = (int)scaled;
        if (i >= stops.Length - 1) return stops[^1];
        float frac = scaled - i;
        uint a = stops[i];
        uint b = stops[i + 1];
        return LerpArgb(a, b, frac);
    }

    private static uint LerpArgb(uint a, uint b, float t)
    {
        byte ar = (byte)((a >> 16) & 0xFF), ag = (byte)((a >> 8) & 0xFF), ab = (byte)(a & 0xFF);
        byte br = (byte)((b >> 16) & 0xFF), bg = (byte)((b >> 8) & 0xFF), bb = (byte)(b & 0xFF);
        byte rr = (byte)(ar + (br - ar) * t);
        byte gg = (byte)(ag + (bg - ag) * t);
        byte bb2 = (byte)(ab + (bb - ab) * t);
        return 0xFF000000u | ((uint)rr << 16) | ((uint)gg << 8) | bb2;
    }
}
