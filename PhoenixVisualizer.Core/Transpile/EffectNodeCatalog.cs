using System;
using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Transpile
{
    public static class EffectNodeCatalog
    {
        public static UnifiedEffectNode Create(string kind)
        {
            kind = (kind ?? "superscope").Trim().ToLowerInvariant();
            return kind switch
            {
                "oscilloscope" => NewOscilloscope(),
                "movement"     => NewMovement(),
                "colorize"     => NewColorize(),
                _              => NewSuperscope()
            };
        }

        public static UnifiedEffectNode NewSuperscope() => new()
        {
            TypeKey = "superscope",
            DisplayName = "Superscope",
            Parameters = new Dictionary<string, object>
            {
                ["init"] = "n=512;",
                ["frame"] = "",
                ["beat"] = "",
                ["point"] = "x = 2*(i/(n-1))-1; y = sin(6.28318*i/(n-1) + t);",
                ["samples"] = 512,
                ["color"] = "#80FF80FF",
                ["thickness"] = 1.2
            }
        };

        public static UnifiedEffectNode NewOscilloscope() => new()
        {
            TypeKey = "oscilloscope",
            DisplayName = "Oscilloscope",
            Parameters = new Dictionary<string, object>
            {
                ["mode"] = "wave", // wave|spectrum
                ["channel"] = "mono", // left|right|mono
                ["color"] = "#80A0FFFF",
                ["thickness"] = 1.5,
                ["gain"] = 1.0,
                ["samples"] = 1024,
                ["smooth"] = 0.0
            }
        };

        public static UnifiedEffectNode NewMovement() => new()
        {
            TypeKey = "movement",
            DisplayName = "Movement",
            Parameters = new Dictionary<string, object>
            {
                ["translateX"] = 0.0,
                ["translateY"] = 0.0,
                ["rotate"] = 0.0,    // degrees
                ["scaleX"] = 1.0,
                ["scaleY"] = 1.0,
                ["centerX"] = 0.0,   // center in pixels relative to canvas center (0 = center)
                ["centerY"] = 0.0
            }
        };

        public static UnifiedEffectNode NewColorize() => new()
        {
            TypeKey = "colorize",
            DisplayName = "Colorize",
            Parameters = new Dictionary<string, object>
            {
                ["tint"] = "#FFFFFFFF",
                ["opacity"] = 1.0,
                ["thicknessMul"] = 1.0
            }
        };
    }
}
