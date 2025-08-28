using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Utils
{
    public static class CoreUtils
    {
        public static object GetInputBuffer(Dictionary<string, object> inputs, string key)
        {
            if (inputs != null && inputs.ContainsKey(key))
                return inputs[key];
            return new object(); // safe fallback buffer
        }
    }
}
