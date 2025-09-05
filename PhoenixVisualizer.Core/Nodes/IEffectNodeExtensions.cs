using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Nodes;

/// <summary>
/// Extension methods for IEffectNode to provide fluent API support
/// </summary>
public static class IEffectNodeExtensions
{
    /// <summary>
    /// Configure an effect node parameter using fluent API
    /// </summary>
    public static IEffectNode With(this IEffectNode node, string paramName, object value)
    {
        if (node?.Params?.ContainsKey(paramName) == true)
        {
            var param = node.Params[paramName];
            
            // Set the appropriate value based on type
            switch (value)
            {
                case float f:
                    param.FloatValue = f;
                    break;
                case double d:
                    param.FloatValue = (float)d;
                    break;
                case int i:
                    param.FloatValue = i;
                    break;
                case bool b:
                    param.BoolValue = b;
                    break;
                case string s:
                    param.StringValue = s;
                    break;
                default:
                    // Try to convert to float if possible
                    if (value != null && float.TryParse(value.ToString(), out float floatVal))
                    {
                        param.FloatValue = floatVal;
                    }
                    break;
            }
        }
        
        return node ?? throw new System.ArgumentNullException(nameof(node), "Node cannot be null");
    }
    
    /// <summary>
    /// Configure multiple parameters at once
    /// </summary>
    public static IEffectNode With(this IEffectNode node, Dictionary<string, object> parameters)
    {
        foreach (var kvp in parameters)
        {
            node.With(kvp.Key, kvp.Value);
        }
        return node;
    }
}


