using System;
using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Utils
{
    public static class NodeUtils
    {
        public static T SafeGet<T>(this Dictionary<string, object> dict, string key, T defaultValue = default!)
        {
            if (dict != null && dict.TryGetValue(key, out var value) && value is T tVal)
                return tVal;
            return defaultValue;
        }

        public static float[] EnsureSize(float[] source, int size)
        {
            if (source == null) return new float[size];
            if (source.Length == size) return source;
            var result = new float[size];
            Array.Copy(source, result, Math.Min(source.Length, size));
            return result;
        }
    }
}
