namespace PhoenixVisualizer.Core.Effects.Interfaces;

/// <summary>
/// Comprehensive interface for NS-EEL/PEL expression evaluation
/// This breaks the circular dependency between Core and PluginHost projects
/// Uses the existing PhoenixExpressionEngine for full PEL support
/// </summary>
public interface INsEelEvaluator : IDisposable
{
    // Core evaluation methods
    /// <summary>
    /// Evaluate a mathematical expression
    /// </summary>
    /// <param name="expression">NS-EEL/PEL expression string</param>
    /// <returns>Result of the evaluation</returns>
    double Evaluate(string expression);
    
    /// <summary>
    /// Execute a script with multiple expressions
    /// </summary>
    /// <param name="script">Multi-line script with semicolon-separated expressions</param>
    void Execute(string script);
    
    // Variable management
    /// <summary>
    /// Set a variable value
    /// </summary>
    void Set(string name, double value);
    
    /// <summary>
    /// Get a variable value with default
    /// </summary>
    double Get(string name, double defaultValue = 0.0);
    
    /// <summary>
    /// Check if a variable exists
    /// </summary>
    bool HasVariable(string name);
    
    /// <summary>
    /// Reset all variables to defaults
    /// </summary>
    void Reset();
    
    // PEL-specific variables and context
    /// <summary>
    /// Set frame context variables
    /// </summary>
    void SetFrameContext(int frame, double frameTime, double deltaTime);
    
    /// <summary>
    /// Set audio context variables
    /// </summary>
    void SetAudioContext(double bass, double mid, double treble, double rms, double peak, bool beat);
    
    /// <summary>
    /// Set canvas context variables
    /// </summary>
    void SetCanvasContext(double width, double height);
    
    /// <summary>
    /// Set point context variables
    /// </summary>
    void SetPointContext(int point, int totalPoints, double x, double y);
    
    // Built-in PEL variables access
    /// <summary>
    /// Get current time variable
    /// </summary>
    double Time { get; }
    
    /// <summary>
    /// Get current frame variable
    /// </summary>
    int Frame { get; }
    
    /// <summary>
    /// Get delta time variable
    /// </summary>
    double DeltaTime { get; }
    
    /// <summary>
    /// Get audio beat state
    /// </summary>
    bool Beat { get; }
    
    /// <summary>
    /// Get bass level
    /// </summary>
    double Bass { get; }
    
    /// <summary>
    /// Get mid level
    /// </summary>
    double Mid { get; }
    
    /// <summary>
    /// Get treble level
    /// </summary>
    double Treble { get; }
    
    /// <summary>
    /// Get RMS level
    /// </summary>
    double RMS { get; }
    
    /// <summary>
    /// Get peak level
    /// </summary>
    double Peak { get; }
    
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