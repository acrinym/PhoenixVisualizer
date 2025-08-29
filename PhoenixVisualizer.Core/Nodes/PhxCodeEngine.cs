using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PhoenixVisualizer.Core.Nodes;

/// <summary>
/// PHX Code Engine - Executes user-written code in the PHX Editor
/// Provides AVS-compatible functions and safe code execution
/// </summary>
public class PhxCodeEngine
{
    private readonly Dictionary<string, object> _globalVariables = new();
    private readonly Dictionary<string, Delegate> _compiledFunctions = new();

    // AVS function implementations
    private float _time = 0;
    private readonly Random _random = new();

    public PhxCodeEngine()
    {
        InitializeBuiltInFunctions();
    }

    private void InitializeBuiltInFunctions()
    {
        // Add built-in AVS functions
        _globalVariables["gettime"] = new Func<int, float>(GetTime);
        _globalVariables["getosc"] = new Func<float, float, float, float>(GetOsc);
        _globalVariables["sin"] = new Func<float, float>(x => (float)Math.Sin(x));
        _globalVariables["cos"] = new Func<float, float>(x => (float)Math.Cos(x));
        _globalVariables["tan"] = new Func<float, float>(x => (float)Math.Tan(x));
        _globalVariables["sqrt"] = new Func<float, float>(x => (float)Math.Sqrt(x));
        _globalVariables["pow"] = new Func<float, float, float>((x, y) => (float)Math.Pow(x, y));
        _globalVariables["log"] = new Func<float, float>(x => (float)Math.Log(x));
        _globalVariables["abs"] = new Func<float, float>(x => Math.Abs(x));
        _globalVariables["min"] = new Func<float, float, float>((a, b) => Math.Min(a, b));
        _globalVariables["max"] = new Func<float, float, float>((a, b) => Math.Max(a, b));
        _globalVariables["clamp"] = new Func<float, float, float, float>((x, min, max) => Math.Clamp(x, min, max));
        _globalVariables["rand"] = new Func<int, int>(x => _random.Next(x));
        _globalVariables["bor"] = new Func<float, float, float>((a, b) => (float)((int)a | (int)b));
        _globalVariables["band"] = new Func<float, float, float>((a, b) => (float)((int)a & (int)b));
        _globalVariables["bnot"] = new Func<float, float>(x => (float)(~(int)x));
    }

    /// <summary>
    /// Execute initialization code (runs once when preset loads)
    /// </summary>
    public ExecutionResult ExecuteInit(string code, Dictionary<string, object>? context = null)
    {
        return ExecuteCode("init", code, context ?? new Dictionary<string, object>());
    }

    /// <summary>
    /// Execute frame code (runs every frame)
    /// </summary>
    public ExecutionResult ExecuteFrame(string code, Dictionary<string, object>? context = null)
    {
        var ctx = context ?? new Dictionary<string, object>();
        ctx["time"] = _time;
        ctx["frame"] = (int)(_time * 60); // Approximate frame number
        return ExecuteCode("frame", code, ctx);
    }

    /// <summary>
    /// Execute point code (runs for each superscope point)
    /// </summary>
    public ExecutionResult ExecutePoint(string code, int pointIndex, int totalPoints, Dictionary<string, object>? context = null)
    {
        var ctx = context ?? new Dictionary<string, object>();
        ctx["i"] = pointIndex;
        ctx["n"] = totalPoints;
        ctx["v"] = pointIndex / (float)totalPoints; // Normalized position
        ctx["x"] = 0f; // Will be set by the code
        ctx["y"] = 0f; // Will be set by the code

        var result = ExecuteCode("point", code, ctx);

        // Extract x,y values if set by the code
        if (result.Variables.ContainsKey("x"))
            result.PointX = Convert.ToSingle(result.Variables["x"]);
        if (result.Variables.ContainsKey("y"))
            result.PointY = Convert.ToSingle(result.Variables["y"]);

        return result;
    }

    /// <summary>
    /// Execute beat code (runs when beat is detected)
    /// </summary>
    public ExecutionResult ExecuteBeat(string code, Dictionary<string, object>? context = null)
    {
        return ExecuteCode("beat", code, context ?? new Dictionary<string, object>());
    }

    private ExecutionResult ExecuteCode(string section, string code, Dictionary<string, object> context)
    {
        var result = new ExecutionResult { Section = section };

        try
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                result.Success = true;
                result.Message = "Empty code - nothing to execute";
                return result;
            }

            // Parse and execute the code
            var variables = ParseAndExecuteCode(code, context);
            result.Variables = variables;
            result.Success = true;
            result.Message = $"Executed {section} code successfully";

        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error in {section}: {ex.Message}";
            result.ErrorDetails = ex.ToString();
        }

        return result;
    }

    private Dictionary<string, object> ParseAndExecuteCode(string code, Dictionary<string, object> context)
    {
        var variables = new Dictionary<string, object>(context);

        // Simple expression parser for basic AVS-style code
        // This is a simplified implementation - a full implementation would use a proper parser

        var lines = code.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var localVariables = new Dictionary<string, object>();

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Skip comments and empty lines
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("//"))
                continue;

            // Handle variable assignments (simple case)
            if (TryParseAssignment(trimmedLine, localVariables, variables))
                continue;

            // Handle function calls and expressions
            if (TryParseExpression(trimmedLine, localVariables, variables))
                continue;
        }

        // Merge local variables back
        foreach (var kvp in localVariables)
        {
            variables[kvp.Key] = kvp.Value;
        }

        return variables;
    }

    private bool TryParseAssignment(string line, Dictionary<string, object> localVars, Dictionary<string, object> globalVars)
    {
        // Simple assignment parser: variable = expression
        var match = Regex.Match(line, @"^\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*(.+)\s*;?\s*$");
        if (!match.Success)
            return false;

        var varName = match.Groups[1].Value;
        var expression = match.Groups[2].Value;

        try
        {
            var value = EvaluateExpression(expression, localVars, globalVars);
            localVars[varName] = value;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryParseExpression(string line, Dictionary<string, object> localVars, Dictionary<string, object> globalVars)
    {
        // Try to evaluate the expression
        try
        {
            EvaluateExpression(line, localVars, globalVars);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private object EvaluateExpression(string expression, Dictionary<string, object> localVars, Dictionary<string, object> globalVars)
    {
        expression = expression.Trim();

        // Handle numbers
        if (float.TryParse(expression, out float number))
            return number;

        // Handle strings (simple case)
        if (expression.StartsWith("\"") && expression.EndsWith("\""))
            return expression.Substring(1, expression.Length - 2);

        // Handle variable references
        if (localVars.ContainsKey(expression))
            return localVars[expression];
        if (globalVars.ContainsKey(expression))
            return globalVars[expression];

        // Handle function calls
        var funcMatch = Regex.Match(expression, @"^([a-zA-Z_][a-zA-Z0-9_]*)\s*\((.*)\)$");
        if (funcMatch.Success)
        {
            var funcName = funcMatch.Groups[1].Value;
            var argsStr = funcMatch.Groups[2].Value;

            if (_globalVariables.ContainsKey(funcName))
            {
                var func = _globalVariables[funcName];
                if (func is Delegate del)
                {
                    var args = ParseArguments(argsStr, localVars, globalVars);
                    return del.DynamicInvoke(args.ToArray());
                }
            }
        }

        // Handle binary operations (very simplified)
        var binaryOps = new[] { "+", "-", "*", "/", "%", "==", "!=", "<", ">", "<=", ">=" };
        foreach (var op in binaryOps)
        {
            if (expression.Contains(op))
            {
                var parts = expression.Split(new[] { op }, 2, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var left = EvaluateExpression(parts[0].Trim(), localVars, globalVars);
                    var right = EvaluateExpression(parts[1].Trim(), localVars, globalVars);

                    return PerformBinaryOperation(left, op, right);
                }
            }
        }

        // If we can't evaluate, return 0
        return 0f;
    }

    private object PerformBinaryOperation(object left, string op, object right)
    {
        // Convert to float for mathematical operations
        float leftVal = Convert.ToSingle(left);
        float rightVal = Convert.ToSingle(right);

        return op switch
        {
            "+" => leftVal + rightVal,
            "-" => leftVal - rightVal,
            "*" => leftVal * rightVal,
            "/" => rightVal != 0 ? leftVal / rightVal : 0,
            "%" => leftVal % rightVal,
            "==" => leftVal == rightVal ? 1f : 0f,
            "!=" => leftVal != rightVal ? 1f : 0f,
            "<" => leftVal < rightVal ? 1f : 0f,
            ">" => leftVal > rightVal ? 1f : 0f,
            "<=" => leftVal <= rightVal ? 1f : 0f,
            ">=" => leftVal >= rightVal ? 1f : 0f,
            _ => 0f
        };
    }

    private List<object> ParseArguments(string argsStr, Dictionary<string, object> localVars, Dictionary<string, object> globalVars)
    {
        var args = new List<object>();
        if (string.IsNullOrWhiteSpace(argsStr))
            return args;

        var argParts = argsStr.Split(',');
        foreach (var arg in argParts)
        {
            args.Add(EvaluateExpression(arg.Trim(), localVars, globalVars));
        }

        return args;
    }

    // AVS Built-in Function Implementations
    private float GetTime(int mode)
    {
        return mode switch
        {
            0 => _time, // Current time in seconds
            1 => _time * 1000, // Current time in milliseconds
            2 => (_time % 1) * 1000, // Milliseconds within current second
            _ => _time
        };
    }

    private float GetOsc(float band, float channel, float mode)
    {
        // Simplified oscillator - in real implementation would use actual audio data
        float frequency = band switch
        {
            0 => 60,   // Bass
            0.5f => 1000, // Mid
            1 => 8000, // Treble
            _ => 440   // Default A4
        };

        float phase = _time * frequency * 2 * (float)Math.PI;

        return mode switch
        {
            0 => (float)Math.Sin(phase), // Sine wave
            1 => phase % (2 * (float)Math.PI) > (float)Math.PI ? 1 : -1, // Square wave
            2 => (float)Math.Sin(phase) > 0 ? 1 : -1, // Pulse wave
            _ => (float)Math.Sin(phase)
        };
    }

    /// <summary>
    /// Update the time for the code engine
    /// </summary>
    public void UpdateTime(float deltaTime)
    {
        _time += deltaTime;
    }

    /// <summary>
    /// Reset the code engine state
    /// </summary>
    public void Reset()
    {
        _time = 0;
        _globalVariables.Clear();
        InitializeBuiltInFunctions();
    }

    /// <summary>
    /// Add a custom variable to the global scope
    /// </summary>
    public void SetGlobalVariable(string name, object value)
    {
        _globalVariables[name] = value;
    }

    /// <summary>
    /// Get a global variable value
    /// </summary>
    public object GetGlobalVariable(string name)
    {
        return _globalVariables.ContainsKey(name) ? _globalVariables[name] : null;
    }
}

/// <summary>
/// Result of code execution
/// </summary>
public class ExecutionResult
{
    public string Section { get; set; } = "";
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string ErrorDetails { get; set; } = "";
    public Dictionary<string, object> Variables { get; set; } = new();
    public float PointX { get; set; }
    public float PointY { get; set; }
}


