using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.ApeHost;

/// <summary>
/// APE (Advanced Plugin Extension) Host for PhoenixVisualizer
/// Implements the Winamp APE interface for advanced visualizer plugins
/// </summary>
public interface IApeHost
{
    /// <summary>
    /// Register an APE effect plugin
    /// </summary>
    void Register(IApeEffect effect);
    
    /// <summary>
    /// Unregister an APE effect plugin
    /// </summary>
    void Unregister(IApeEffect effect);
    
    /// <summary>
    /// Get all registered APE effects
    /// </summary>
    IReadOnlyList<IApeEffect> GetRegisteredEffects();
    
    /// <summary>
    /// Execute APE script code
    /// </summary>
    bool ExecuteScript(string scriptCode, out string errorMessage);
}

/// <summary>
/// APE Host implementation based on Winamp SDK specifications
/// </summary>
public sealed class ApeHost : IApeHost, IDisposable
{
    private readonly List<IApeEffect> _registeredEffects = new();
    private readonly Dictionary<string, object> _globalRegisters = new();
    private readonly object _lockObject = new();
    
    // APE VM context (simplified implementation)
    private readonly ApeVirtualMachine _vm;
    
    public ApeHost()
    {
        _vm = new ApeVirtualMachine();
        InitializeGlobalRegisters();
    }
    
    public void Register(IApeEffect effect)
    {
        lock (_lockObject)
        {
            if (!_registeredEffects.Contains(effect))
            {
                _registeredEffects.Add(effect);
                LogToFile($"[ApeHost] Registered effect: {effect.DisplayName}");
            }
        }
    }
    
    public void Unregister(IApeEffect effect)
    {
        lock (_lockObject)
        {
            if (_registeredEffects.Remove(effect))
            {
                LogToFile($"[ApeHost] Unregistered effect: {effect.DisplayName}");
            }
        }
    }
    
    public IReadOnlyList<IApeEffect> GetRegisteredEffects()
    {
        lock (_lockObject)
        {
            return _registeredEffects.AsReadOnly();
        }
    }
    
    public bool ExecuteScript(string scriptCode, out string errorMessage)
    {
        try
        {
            var result = _vm.Execute(scriptCode);
            if (result.Success)
            {
                errorMessage = string.Empty;
                return true;
            }
            else
            {
                errorMessage = result.ErrorMessage;
                LogToFile($"[ApeHost] Script execution failed: {errorMessage}");
                return false;
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Script execution error: {ex.Message}";
            LogToFile($"[ApeHost] Script execution exception: {ex.Message}");
            return false;
        }
    }
    
    private void InitializeGlobalRegisters()
    {
        // Initialize the 100 global registers as specified in the APE spec
        for (int i = 0; i < 100; i++)
        {
            _globalRegisters[$"reg{i}"] = 0.0;
        }
        
        // Set some common default values
        _globalRegisters["width"] = 640.0;
        _globalRegisters["height"] = 480.0;
        _globalRegisters["bass"] = 0.0;
        _globalRegisters["mid"] = 0.0;
        _globalRegisters["treble"] = 0.0;
        _globalRegisters["beat"] = 0.0;
        _globalRegisters["bpm"] = 120.0;
    }
    
    public void Dispose()
    {
        _vm?.Dispose();
    }
    
    private static void LogToFile(string message)
    {
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ape_host_debug.log");
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] {message}";
            File.AppendAllText(logPath, logMessage + Environment.NewLine);
        }
        catch
        {
            // Silently fail if logging fails
        }
    }
}

/// <summary>
/// Simplified APE Virtual Machine for script execution
/// </summary>
public sealed class ApeVirtualMachine : IDisposable
{
    private readonly Dictionary<string, double> _variables = new();
    private readonly Dictionary<string, Func<double[], double>> _functions = new();
    
    public ApeVirtualMachine()
    {
        InitializeBuiltinFunctions();
    }
    
    public ApeExecutionResult Execute(string scriptCode)
    {
        try
        {
            // Simple expression evaluator for now
            // TODO: Implement full NS-EEL parser from Winamp SDK
            var result = EvaluateSimpleExpression(scriptCode);
            return new ApeExecutionResult { Success = true, Result = result };
        }
        catch (Exception ex)
        {
            return new ApeExecutionResult { Success = false, ErrorMessage = ex.Message };
        }
    }
    
    private double EvaluateSimpleExpression(string expression)
    {
        // Very basic expression evaluator
        // This is a placeholder - the real implementation should use NS-EEL from Winamp SDK
        expression = expression.Trim();
        
        if (double.TryParse(expression, out double value))
        {
            return value;
        }
        
        if (_variables.TryGetValue(expression, out double varValue))
        {
            return varValue;
        }
        
        // Try to evaluate simple math expressions
        if (expression.Contains("+"))
        {
            var parts = expression.Split('+');
            if (parts.Length == 2)
            {
                return EvaluateSimpleExpression(parts[0]) + EvaluateSimpleExpression(parts[1]);
            }
        }
        
        if (expression.Contains("-"))
        {
            var parts = expression.Split('-');
            if (parts.Length == 2)
            {
                return EvaluateSimpleExpression(parts[0]) - EvaluateSimpleExpression(parts[1]);
            }
        }
        
        if (expression.Contains("*"))
        {
            var parts = expression.Split('*');
            if (parts.Length == 2)
            {
                return EvaluateSimpleExpression(parts[0]) * EvaluateSimpleExpression(parts[1]);
            }
        }
        
        if (expression.Contains("/"))
        {
            var parts = expression.Split('/');
            if (parts.Length == 2)
            {
                return EvaluateSimpleExpression(parts[0]) / EvaluateSimpleExpression(parts[1]);
            }
        }
        
        throw new InvalidOperationException($"Cannot evaluate expression: {expression}");
    }
    
    private void InitializeBuiltinFunctions()
    {
        // Add basic math functions
        _functions["sin"] = args => Math.Sin(args[0]);
        _functions["cos"] = args => Math.Cos(args[0]);
        _functions["tan"] = args => Math.Tan(args[0]);
        _functions["sqrt"] = args => Math.Sqrt(args[0]);
        _functions["pow"] = args => Math.Pow(args[0], args[1]);
        _functions["abs"] = args => Math.Abs(args[0]);
        _functions["min"] = args => Math.Min(args[0], args[1]);
        _functions["max"] = args => Math.Max(args[0], args[1]);
    }
    
    public void Dispose()
    {
        _variables.Clear();
        _functions.Clear();
    }
}

/// <summary>
/// Result of APE script execution
/// </summary>
public struct ApeExecutionResult
{
    public bool Success { get; set; }
    public double Result { get; set; }
    public string ErrorMessage { get; set; }
}
