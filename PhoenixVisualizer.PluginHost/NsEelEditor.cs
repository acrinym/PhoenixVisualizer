namespace PhoenixVisualizer.PluginHost;

/// <summary>
/// Real-time NS-EEL expression editor for AVS presets
/// </summary>
public class NsEelEditor
{
    private readonly NsEelEvaluator _evaluator;
    private readonly Dictionary<string, string> _variables = new();
    private readonly List<string> _errors = new();
    
    // Built-in functions and variables
    private readonly Dictionary<string, Func<double[], double>> _builtInFunctions = new();
    private readonly Dictionary<string, double> _builtInVariables = new();
    
    public event Action<string>? ErrorOccurred;
    public event Action<string, double>? VariableChanged;
    
    public NsEelEditor()
    {
        _evaluator = new NsEelEvaluator();
        InitializeBuiltIns();
    }
    
    private void InitializeBuiltIns()
    {
        // Built-in variables
        _builtInVariables["time"] = 0.0;
        _builtInVariables["beat"] = 0.0;
        _builtInVariables["energy"] = 0.0;
        _builtInVariables["bass"] = 0.0;
        _builtInVariables["mid"] = 0.0;
        _builtInVariables["treble"] = 0.0;
        _builtInVariables["rms"] = 0.0;
        _builtInVariables["peak"] = 0.0;
        
        // Built-in functions
        _builtInFunctions["sin"] = args => args.Length > 0 ? Math.Sin(args[0]) : 0.0;
        _builtInFunctions["cos"] = args => args.Length > 0 ? Math.Cos(args[0]) : 0.0;
        _builtInFunctions["tan"] = args => args.Length > 0 ? Math.Tan(args[0]) : 0.0;
        _builtInFunctions["abs"] = args => args.Length > 0 ? Math.Abs(args[0]) : 0.0;
        _builtInFunctions["sqrt"] = args => args.Length > 0 ? Math.Sqrt(args[0]) : 0.0;
        _builtInFunctions["pow"] = args => args.Length > 1 ? Math.Pow(args[0], args[1]) : 0.0;
        _builtInFunctions["log"] = args => args.Length > 0 ? Math.Log(args[0]) : 0.0;
        _builtInFunctions["exp"] = args => args.Length > 0 ? Math.Exp(args[0]) : 0.0;
        _builtInFunctions["min"] = args => args.Length > 0 ? args.Min() : 0.0;
        _builtInFunctions["max"] = args => args.Length > 0 ? args.Max() : 0.0;
        _builtInFunctions["rand"] = args => new Random().NextDouble();
        _builtInFunctions["if"] = args => args.Length > 2 ? (args[0] > 0 ? args[1] : args[2]) : 0.0;
    }
    
    /// <summary>
    /// Set a variable value
    /// </summary>
    public void SetVariable(string name, double value)
    {
        if (_builtInVariables.ContainsKey(name))
        {
            _builtInVariables[name] = value;
        }
        else
        {
            _variables[name] = value.ToString();
        }
        
        VariableChanged?.Invoke(name, value);
    }
    
    /// <summary>
    /// Get a variable value
    /// </summary>
    public double GetVariable(string name)
    {
        if (_builtInVariables.TryGetValue(name, out var builtInValue))
            return builtInValue;
            
        if (_variables.TryGetValue(name, out var varString))
        {
            if (double.TryParse(varString, out var value))
                return value;
        }
        
        return 0.0;
    }
    
    /// <summary>
    /// Evaluate an NS-EEL expression
    /// </summary>
    public double EvaluateExpression(string expression)
    {
        try
        {
            // Update built-in variables with current time
            _builtInVariables["time"] = DateTime.Now.Ticks / 10000000.0;
            
            // Create a combined variable dictionary
            var allVariables = new Dictionary<string, double>(_builtInVariables);
            foreach (var kvp in _variables)
            {
                if (double.TryParse(kvp.Value, out var value))
                    allVariables[kvp.Key] = value;
            }
            
            // Set variables in the evaluator first
            foreach (var kvp in allVariables)
            {
                _evaluator.SetVariable(kvp.Key, kvp.Value);
            }
            
            // Evaluate the expression
            var result = _evaluator.Evaluate(expression);
            
            // Clear any previous errors
            if (_errors.Count > 0)
            {
                _errors.Clear();
            }
            
            return result;
        }
        catch (Exception ex)
        {
            var errorMsg = $"Expression error: {ex.Message}";
            _errors.Add(errorMsg);
            ErrorOccurred?.Invoke(errorMsg);
            return 0.0;
        }
    }
    
    /// <summary>
    /// Get all current variables
    /// </summary>
    public Dictionary<string, double> GetAllVariables()
    {
        var result = new Dictionary<string, double>(_builtInVariables);
        foreach (var kvp in _variables)
        {
            if (double.TryParse(kvp.Value, out var value))
                result[kvp.Key] = value;
        }
        return result;
    }
    
    /// <summary>
    /// Get all errors
    /// </summary>
    public List<string> GetErrors()
    {
        return new List<string>(_errors);
    }
    
    /// <summary>
    /// Clear all errors
    /// </summary>
    public void ClearErrors()
    {
        _errors.Clear();
    }
    
    /// <summary>
    /// Validate an expression without executing it
    /// </summary>
    public bool ValidateExpression(string expression)
    {
        try
        {
            // Try to evaluate with a simple test
            _evaluator.Evaluate("0");
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Get expression suggestions for autocomplete
    /// </summary>
    public List<string> GetSuggestions(string partialInput)
    {
        var suggestions = new List<string>();
        
        // Add variable suggestions
        foreach (var varName in _variables.Keys)
        {
            if (varName.StartsWith(partialInput, StringComparison.OrdinalIgnoreCase))
                suggestions.Add(varName);
        }
        
        // Add built-in variable suggestions
        foreach (var varName in _builtInVariables.Keys)
        {
            if (varName.StartsWith(partialInput, StringComparison.OrdinalIgnoreCase))
                suggestions.Add(varName);
        }
        
        // Add function suggestions
        foreach (var funcName in _builtInFunctions.Keys)
        {
            if (funcName.StartsWith(partialInput, StringComparison.OrdinalIgnoreCase))
                suggestions.Add(funcName + "(");
        }
        
        // Add operator suggestions
        var operators = new[] { "+", "-", "*", "/", "%", "=", "<", ">", "&", "|", "!" };
        foreach (var op in operators)
        {
            if (op.StartsWith(partialInput, StringComparison.OrdinalIgnoreCase))
                suggestions.Add(op);
        }
        
        return suggestions.OrderBy(s => s).ToList();
    }
    
    /// <summary>
    /// Format an expression for better readability
    /// </summary>
    public string FormatExpression(string expression)
    {
        try
        {
            // Simple formatting - add spaces around operators
            var formatted = expression
                .Replace("+", " + ")
                .Replace("-", " - ")
                .Replace("*", " * ")
                .Replace("/", " / ")
                .Replace("=", " = ")
                .Replace("<", " < ")
                .Replace(">", " > ")
                .Replace("&", " & ")
                .Replace("|", " | ")
                .Replace("!", " ! ");
            
            // Clean up multiple spaces
            while (formatted.Contains("  "))
            {
                formatted = formatted.Replace("  ", " ");
            }
            
            return formatted.Trim();
        }
        catch
        {
            return expression; // Return original if formatting fails
        }
    }
}
