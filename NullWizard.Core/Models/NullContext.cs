namespace NullWizard.Core.Models;

/// <summary>
/// Represents a context where null handling occurs
/// </summary>
public enum NullContext
{
    /// <summary>JSON deserialization - values can be null from external sources</summary>
    JsonDeserialization,
    
    /// <summary>Database results - columns can be null from database</summary>
    DatabaseResult,
    
    /// <summary>API response - fields can be null from external APIs</summary>
    ApiResponse,
    
    /// <summary>User input - parameters can be null from user</summary>
    UserInput,
    
    /// <summary>Legacy code - old APIs that don't know about nullables</summary>
    LegacyCode,
    
    /// <summary>Constructor parameter - required but can be validated</summary>
    ConstructorParameter,
    
    /// <summary>Method parameter - optional or required</summary>
    MethodParameter,
    
    /// <summary>Property setter - can allow or disallow null</summary>
    PropertySetter,
    
    /// <summary>Return value - can return null or not</summary>
    ReturnValue
}

/// <summary>
/// Represents a null handling strategy
/// </summary>
public enum NullStrategy
{
    /// <summary>Use null-coalescing operator (??)</summary>
    NullCoalescing,
    
    /// <summary>Use null-conditional operator (?.)</summary>
    NullConditional,
    
    /// <summary>Use null-forgiving operator (!)</summary>
    NullForgiving,
    
    /// <summary>Throw if null</summary>
    ThrowIfNull,
    
    /// <summary>Allow null to pass through</summary>
    AllowNull,
    
    /// <summary>Use default value</summary>
    UseDefault,
    
    /// <summary>Validate and transform</summary>
    ValidateAndTransform
}

/// <summary>
/// Represents a null handling pattern learned from codebase analysis
/// </summary>
public class NullPattern
{
    public string PatternId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public NullContext Context { get; set; }
    public NullStrategy Strategy { get; set; }
    public string CodePattern { get; set; } = string.Empty;
    public string FixPattern { get; set; } = string.Empty;
    public int OccurrenceCount { get; set; }
    public double Confidence { get; set; }
    public string[] ApplicableFrameworks { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Represents a codebase analysis result
/// </summary>
public class CodebaseAnalysis
{
    public string ProjectPath { get; set; } = string.Empty;
    public string TargetFramework { get; set; } = string.Empty;
    public List<NullPattern> Patterns { get; set; } = new();
    public Dictionary<string, int> NullWarningCounts { get; set; } = new();
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}
