using System.Collections.Generic;

namespace PhoenixVisualizer.App.Services
{
    /// <summary>
    /// Defines constants and types for AVS file detection and parsing
    /// No regex nightmares - just clean type definitions
    /// </summary>
    public static class AvsFileTypes
    {
        /// <summary>
        /// Winamp AVS binary file constants
        /// </summary>
        public static class WinampAVS
        {
            public const string BINARY_HEADER = "Nullsoft AVS Preset";
            public const int HEADER_SIZE = 16;
            public const int VERSION_SIZE = 2;
            
            /// <summary>
            /// Winamp AVS effect type IDs from original Winamp source
            /// </summary>
            public static readonly Dictionary<int, string> EffectTypes = new()
            {
                {0, "Simple Spectrum"},
                {1, "Dot Plane"},
                {2, "Oscilloscope Star"},
                {3, "Fade Out"},
                {4, "Blitter Feedback"},
                {5, "NF Clear"},
                {6, "Blur"},
                {7, "Bass Spin"},
                {8, "Moving Particles"},
                {9, "Rotoblitter"},
                {10, "SVP Loader"},
                {11, "Color Fade"},
                {12, "Contrast Enhancement"},
                {13, "Rotating Stars"},
                {14, "Oscilloscope Rings"},
                {15, "Movement"},
                {16, "Scatter"},
                {17, "Dot Grid"},
                {18, "Stack"},
                {19, "Dot Fountain"},
                {20, "Water"},
                {21, "Comment"},
                {22, "Brightness"},
                {23, "Interleave"},
                {24, "Grain"},
                {25, "Clear Screen"},
                {26, "Mirror"},
                {27, "Star Field"},
                {28, "Text"},
                {29, "Bump"},
                {30, "Mosaic"},
                {31, "Water Bump"},
                {32, "AVI Player"},
                {33, "Custom BPM"},
                {34, "Picture"},
                {35, "Dynamic Distance Modifier"},
                {36, "SuperScope"},
                {37, "Invert"},
                {38, "Unique Tone"},
                {39, "Timescope"},
                {40, "Color Clip"},
                {41, "Color Reduction"},
                {42, "Channel Shift"},
                {43, "Video Delay"},
                {44, "Multiplier"},
                {45, "Color Map"}
            };

            /// <summary>
            /// Effect types that contain superscope code
            /// </summary>
            public static readonly HashSet<int> SuperscopeEffectTypes = new()
            {
                3,   // Fade Out (can have code)
                36,  // SuperScope (main superscope effect)
                93   // SuperScope (Trans)
            };
        }

        /// <summary>
        /// Phoenix AVS text file constants
        /// </summary>
        public static class PhoenixAVS
        {
            /// <summary>
            /// Text markers that identify Phoenix AVS files
            /// </summary>
            public static readonly string[] TextMarkers = 
            {
                "[avs]",
                "PRESET_NAME=",
                "DESCRIPTION=",
                "[preset",
                "sn=Superscope",
                "POINT",
                "INIT",
                "CODE"
            };

            /// <summary>
            /// Phoenix AVS effect node types from our documentation
            /// </summary>
            public static readonly HashSet<string> EffectNodeTypes = new()
            {
                // From our docs/effects documentation
                "BassSpinEffects",
                "BlurEffects", 
                "BrightnessEffects",
                "ChannelShiftEffects",
                "ClearScreenEffects",
                "ColorreductionEffects",
                "ColorreplaceEffects",
                "CommentEffects",
                "CustomBPMEffects",
                "DotFountainEffects",
                "DotGridEffects",
                "DotPlaneEffects",
                "DynamicColorModulationEffects",
                "DynamicDistanceMovementEffects",
                "DynamicMovementEffects",
                "FadeoutEffects",
                "GrainEffects",
                "InterleaveEffects",
                "InvertEffects",
                "LineModeEffects",
                "ListEffects",
                "MosaicEffects",
                "MultiDelayEffects",
                "MultiplierEffects",
                "NFClearEffects",
                "TextEffects",
                "TransitionEffects",
                "WaterBumpEffects"
            };

            /// <summary>
            /// Section headers in Phoenix AVS files
            /// </summary>
            public static readonly string[] SectionHeaders = 
            {
                "POINT",
                "INIT", 
                "CODE",
                "FRAME",
                "BEAT"
            };
        }

        /// <summary>
        /// Common AVS scripting language keywords (NS-EEL)
        /// </summary>
        public static class NSEELKeywords
        {
            public static readonly string[] MathFunctions =
            {
                "sin", "cos", "tan", "asin", "acos", "atan", "atan2",
                "sqrt", "pow", "exp", "log", "log10", 
                "abs", "floor", "ceil", "round",
                "min", "max", "rand", "sqr"
            };

            public static readonly string[] AudioFunctions =
            {
                "getspec", "getosc", "getmidi", "gettime",
                "bass", "mid", "treble", "volume"
            };

            public static readonly string[] Variables =
            {
                "x", "y", "r", "g", "b", "a",
                "n", "i", "v", "t", "dt",
                "w", "h", "sw", "sh",
                "PI", "E", "PHI"
            };
        }
    }
}
