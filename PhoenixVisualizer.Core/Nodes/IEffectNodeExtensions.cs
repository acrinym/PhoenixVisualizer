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
        if (node.Params.ContainsKey(paramName))
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
                    if (float.TryParse(value.ToString(), out float floatVal))
                    {
                        param.FloatValue = floatVal;
                    }
                    break;
            }
        }
        
        return node;
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


