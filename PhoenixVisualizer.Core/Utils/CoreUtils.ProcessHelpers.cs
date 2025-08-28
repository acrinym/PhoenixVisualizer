using System;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Models;
using System.Linq;

namespace PhoenixVisualizer.Core.Utils
{
    public static class ProcessHelpers
    {
        /// <summary>
        /// Safely gets a float parameter from a dictionary.
        /// </summary>
        public static float GetFloat(Dictionary<string, object> dict, string key, float defaultValue = 0f)
        {
            if (dict.TryGetValue(key, out var val) && val is float f)
                return f;
            return defaultValue;
        }

        /// <summary>
        /// Safely gets a Color parameter from a dictionary.
        /// </summary>
        public static Avalonia.Media.Color GetColor(Dictionary<string, object> dict, string key, Avalonia.Media.Color defaultValue)
        {
            if (dict.TryGetValue(key, out var val) && val is Avalonia.Media.Color c)
                return c;
            return defaultValue;
        }

        /// <summary>
        /// Checks if the audio features are valid and returns true if data is available.
        /// </summary>
        public static bool HasAudio(AudioFeatures features)
        {
            return features != null &&
                   (features.Waveform.Length > 0 || features.Spectrum.Length > 0);
        }

        // Helper methods for different effect types
        public static object PassThrough(Dictionary<string, object> inputs, AudioFeatures audio) => inputs.Values.FirstOrDefault() ?? new object();
        public static object Mix(Dictionary<string, object> inputs, AudioFeatures audio) => inputs.Values.FirstOrDefault() ?? new object();
        public static object AdjustContrast(Dictionary<string, object> inputs, AudioFeatures audio) => inputs.Values.FirstOrDefault() ?? new object();
        public static object Video(Dictionary<string, object> inputs, AudioFeatures audio) => inputs.Values.FirstOrDefault() ?? new object();
        public static object BeatSync(Dictionary<string, object> inputs, AudioFeatures audio) => inputs.Values.FirstOrDefault() ?? new object();
        public static object ChannelShift(Dictionary<string, object> inputs, AudioFeatures audio) => inputs.Values.FirstOrDefault() ?? new object();
        public static object BumpMap(Dictionary<string, object> inputs, AudioFeatures audio) => inputs.Values.FirstOrDefault() ?? new object();
        public static object Stack(Dictionary<string, object> inputs, AudioFeatures audio) => inputs.Values.FirstOrDefault() ?? new object();
        public static object DynamicMove(Dictionary<string, object> inputs, AudioFeatures audio) => inputs.Values.FirstOrDefault() ?? new object();
        public static object Starfield(Dictionary<string, object> inputs, AudioFeatures audio) => inputs.Values.FirstOrDefault() ?? new object();
        public static object Scatter(Dictionary<string, object> inputs, AudioFeatures audio) => inputs.Values.FirstOrDefault() ?? new object();
        public static object Particles(Dictionary<string, object> inputs, AudioFeatures audio) => inputs.Values.FirstOrDefault() ?? new object();
        public static object Water(Dictionary<string, object> inputs, AudioFeatures audio) => inputs.Values.FirstOrDefault() ?? new object();
        public static object Scope(Dictionary<string, object> inputs, AudioFeatures audio) => inputs.Values.FirstOrDefault() ?? new object();
        public static object VectorField(Dictionary<string, object> inputs, AudioFeatures audio) => inputs.Values.FirstOrDefault() ?? new object();
        public static object Composite(Dictionary<string, object> inputs, AudioFeatures audio) => inputs.Values.FirstOrDefault() ?? new object();
        public static object SVP(Dictionary<string, object> inputs, AudioFeatures audio) => inputs.Values.FirstOrDefault() ?? new object();
        public static object Picture(Dictionary<string, object> inputs, AudioFeatures audio) => inputs.Values.FirstOrDefault() ?? new object();
        public static object DotFont(Dictionary<string, object> inputs, AudioFeatures audio) => inputs.Values.FirstOrDefault() ?? new object();
        public static object Simple(Dictionary<string, object> inputs, AudioFeatures audio) => inputs.Values.FirstOrDefault() ?? new object();
        public static object Shift(Dictionary<string, object> inputs, AudioFeatures audio) => inputs.Values.FirstOrDefault() ?? new object();
    }
}
