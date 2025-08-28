namespace PhoenixVisualizer.Core.Effects.Interfaces;

/// <summary>
/// Interface for NS-EEL expression evaluation
/// This breaks the circular dependency between Core and PluginHost projects
/// </summary>
public interface INsEelEvaluator : IDisposable
{
    /// <summary>
    /// Evaluate a mathematical expression
    /// </summary>
    /// <param name="expression">NS-EEL expression string</param>
    /// <returns>Result of the evaluation</returns>
    double Evaluate(string expression);
    
    /// <summary>
    /// Set audio data for the current frame
    /// </summary>
    /// <param name="bass">Bass level (0.0 to 1.0)</param>
    /// <param name="mid">Mid level (0.0 to 1.0)</param>
    /// <param name="treble">Treble level (0.0 to 1.0)</param>
    /// <param name="volume">Overall volume (0.0 to 1.0)</param>
    /// <param name="isBeat">Whether a beat was detected</param>
    void SetAudioData(float bass, float mid, float treble, float volume, bool isBeat);
    
    /// <summary>
    /// Set the current frame context
    /// </summary>
    /// <param name="frame">Current frame number</param>
    /// <param name="frameTime">Time since start in seconds</param>
    /// <param name="beatTime">Time since last beat in seconds</param>
    void SetFrameContext(int frame, double frameTime, double beatTime);
    
    /// <summary>
    /// Set a variable value
    /// </summary>
    /// <param name="name">Variable name</param>
    /// <param name="value">Variable value</param>
    void SetVariable(string name, double value);
    
    /// <summary>
    /// Get a variable value
    /// </summary>
    /// <param name="name">Variable name</param>
    /// <returns>Variable value or 0.0 if not found</returns>
    double GetVariable(string name);
    
    /// <summary>
    /// Check if a variable exists
    /// </summary>
    /// <param name="name">Variable name</param>
    /// <returns>True if variable exists</returns>
    bool HasVariable(string name);
}