namespace PhoenixVisualizer.Core.Effects.Interfaces;

/// <summary>
/// Comprehensive interface for NS-EEL expression evaluation
/// This breaks the circular dependency between Core and PluginHost projects
/// </summary>
public interface INsEelEvaluator : IDisposable
{
    // Core evaluation methods
    /// <summary>
    /// Evaluate a mathematical expression
    /// </summary>
    /// <param name="expression">NS-EEL expression string</param>
    /// <returns>Result of the evaluation</returns>
    double Evaluate(string expression);
    
    /// <summary>
    /// Evaluate an expression with custom variables
    /// </summary>
    /// <param name="expression">NS-EEL expression string</param>
    /// <param name="variables">Custom variables to use</param>
    /// <returns>Result of the evaluation</returns>
    double Evaluate(string expression, Dictionary<string, double> variables);
    
    // Variable management
    /// <summary>
    /// Set a global variable
    /// </summary>
    void SetVariable(string name, double value);
    
    /// <summary>
    /// Get a variable value
    /// </summary>
    double GetVariable(string name);
    
    /// <summary>
    /// Check if a variable exists
    /// </summary>
    bool HasVariable(string name);
    
    /// <summary>
    /// Remove a variable
    /// </summary>
    void RemoveVariable(string name);
    
    /// <summary>
    /// Clear all variables
    /// </summary>
    void ClearVariables();
    
    // Per-frame variables (reset each frame)
    /// <summary>
    /// Set a per-frame variable
    /// </summary>
    void SetPerFrameVariable(string name, double value);
    
    /// <summary>
    /// Get a per-frame variable
    /// </summary>
    double GetPerFrameVariable(string name);
    
    /// <summary>
    /// Clear all per-frame variables
    /// </summary>
    void ClearPerFrameVariables();
    
    // Per-point variables (reset each point)
    /// <summary>
    /// Set a per-point variable
    /// </summary>
    void SetPerPointVariable(string name, double value);
    
    /// <summary>
    /// Get a per-point variable
    /// </summary>
    double GetPerPointVariable(string name);
    
    /// <summary>
    /// Clear all per-point variables
    /// </summary>
    void ClearPerPointVariables();
    
    // Context management
    /// <summary>
    /// Set the current frame context
    /// </summary>
    void SetFrameContext(int frame, double frameTime, double beatTime);
    
    /// <summary>
    /// Set the current point context
    /// </summary>
    void SetPointContext(int point, int totalPoints, double x, double y);
    
    /// <summary>
    /// Reset all context variables
    /// </summary>
    void ResetContext();
    
    // Audio analysis (simplified interface)
    /// <summary>
    /// Set audio data for the current frame
    /// </summary>
    void SetAudioData(double bass, double mid, double treble, double volume, bool isBeat);
    
    // Function management
    /// <summary>
    /// Register a custom function
    /// </summary>
    void RegisterFunction(string name, Func<double[], double> function);
    
    /// <summary>
    /// Unregister a custom function
    /// </summary>
    void UnregisterFunction(string name);
    
    /// <summary>
    /// Check if a function exists
    /// </summary>
    bool HasFunction(string name);
    
    // Expression compilation and caching
    /// <summary>
    /// Compile an expression for faster evaluation
    /// </summary>
    object CompileExpression(string expression);
    
    /// <summary>
    /// Evaluate a compiled expression
    /// </summary>
    double EvaluateCompiled(object compiledExpression);
    
    /// <summary>
    /// Clear the expression cache
    /// </summary>
    void ClearCache();
    
    // Error handling
    /// <summary>
    /// Get the last error message
    /// </summary>
    string? GetLastError();
    
    /// <summary>
    /// Check if the last evaluation had an error
    /// </summary>
    bool HasError();
    
    /// <summary>
    /// Clear the last error
    /// </summary>
    void ClearError();
    
    // Performance and statistics
    /// <summary>
    /// Get evaluation statistics
    /// </summary>
    EvaluationStats GetStats();
    
    /// <summary>
    /// Reset evaluation statistics
    /// </summary>
    void ResetStats();
}

/// <summary>
/// Statistics about expression evaluation
/// </summary>
public struct EvaluationStats
{
    public int TotalEvaluations { get; set; }
    public int SuccessfulEvaluations { get; set; }
    public int FailedEvaluations { get; set; }
    public double AverageEvaluationTime { get; set; }
    public double TotalEvaluationTime { get; set; }
    public int CacheHits { get; set; }
    public int CacheMisses { get; set; }
}