using PhoenixVisualizer.Core.Effects.Interfaces;
using PhoenixVisualizer.Core.Engine;

namespace PhoenixVisualizer.PluginHost;

/// <summary>
/// NS-EEL/PEL Expression Evaluator
/// Wrapper around PhoenixExpressionEngine for full PEL support
/// </summary>
public sealed class NsEelEvaluator : INsEelEvaluator
{
    private readonly PhoenixExpressionEngine _engine;
    private readonly Dictionary<string, object> _compiledCache = new();
    private string? _lastError;
    private EvaluationStats _stats = new();
    private readonly System.Diagnostics.Stopwatch _stopwatch = new();
    
    public NsEelEvaluator()
    {
        _engine = new PhoenixExpressionEngine();
    }
    
    // Core evaluation methods
    public double Evaluate(string expression)
    {
        try
        {
            _stopwatch.Restart();
            
            // PhoenixExpressionEngine doesn't have a public Evaluate method,
            // so we need to use Execute for single expressions
            if (expression.Contains("="))
            {
                // If it's an assignment, execute it
                _engine.Execute(expression);
                _stopwatch.Stop();
                UpdateStats(true, _stopwatch.Elapsed.TotalMilliseconds);
                ClearError();
                return 0.0; // Assignment doesn't return a value
            }
            else
            {
                // For simple expressions, we need to create a temporary variable
                var tempVar = $"__temp_{Guid.NewGuid():N}";
                var script = $"{tempVar}={expression}";
                _engine.Execute(script);
                
                var result = _engine.Get(tempVar, 0.0);
                
                // Clean up temp variable
                _engine.Set(tempVar, 0.0);
                
                _stopwatch.Stop();
                UpdateStats(true, _stopwatch.Elapsed.TotalMilliseconds);
                ClearError();
                return result;
            }
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            UpdateStats(false, 0);
            return 0.0;
        }
    }
    
    public void Execute(string script)
    {
        try
        {
            _stopwatch.Restart();
            _engine.Execute(script);
            _stopwatch.Stop();
            
            UpdateStats(true, _stopwatch.Elapsed.TotalMilliseconds);
            ClearError();
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            UpdateStats(false, 0);
        }
    }
    
    // Variable management
    public void Set(string name, double value) => _engine.Set(name, value);
    public double Get(string name, double defaultValue = 0.0) => _engine.Get(name, defaultValue);
    
    public bool HasVariable(string name)
    {
        try
        {
            var value = _engine.Get(name, double.NaN);
            return !double.IsNaN(value);
        }
        catch
        {
            return false;
        }
    }
    
    public void Reset() => _engine.Reset();
    
    // PEL-specific context methods
    public void SetFrameContext(int frame, double frameTime, double deltaTime)
    {
        _engine.Set("pel_frame", frame);
        _engine.Set("pel_time", frameTime);
        _engine.Set("pel_dt", deltaTime);
        _engine.Set("frame", frame);
        _engine.Set("time", frameTime);
        _engine.Set("dt", deltaTime);
    }
    
    public void SetAudioContext(double bass, double mid, double treble, double rms, double peak, bool beat)
    {
        _engine.Set("bass", bass);
        _engine.Set("mid", mid);
        _engine.Set("treble", treble);
        _engine.Set("rms", rms);
        _engine.Set("peak", peak);
        _engine.Set("beat", beat ? 1.0 : 0.0);
    }
    
    public void SetCanvasContext(double width, double height)
    {
        _engine.Set("w", width);
        _engine.Set("h", height);
        _engine.Set("width", width);
        _engine.Set("height", height);
    }
    
    public void SetPointContext(int point, int totalPoints, double x, double y)
    {
        _engine.Set("i", point);
        _engine.Set("n", totalPoints);
        _engine.Set("x", x);
        _engine.Set("y", y);
    }
    
    // Built-in PEL variables access
    public double Time => _engine.Get("pel_time", 0.0);
    public int Frame => (int)_engine.Get("pel_frame", 0.0);
    public double DeltaTime => _engine.Get("pel_dt", 0.016);
    public bool Beat => _engine.Get("beat", 0.0) > 0.5;
    public double Bass => _engine.Get("bass", 0.0);
    public double Mid => _engine.Get("mid", 0.0);
    public double Treble => _engine.Get("treble", 0.0);
    public double RMS => _engine.Get("rms", 0.0);
    public double Peak => _engine.Get("peak", 0.0);
    
    // Expression compilation and caching
    public object CompileExpression(string expression)
    {
        if (_compiledCache.TryGetValue(expression, out var cached))
            return cached;
        
        // For now, just return the expression string as a simple cache
        // In a full implementation, this would compile to bytecode or AST
        _compiledCache[expression] = expression;
        return expression;
    }
    
    public double EvaluateCompiled(object compiledExpression)
    {
        if (compiledExpression is string expr)
            return Evaluate(expr);
        
        return 0.0;
    }
    
    public void ClearCache()
    {
        _compiledCache.Clear();
    }
    
    // Error handling
    public string? GetLastError() => _lastError;
    public bool HasError() => !string.IsNullOrEmpty(_lastError);
    public void ClearError() => _lastError = null;
    
    // Performance and statistics
    public EvaluationStats GetStats() => _stats;
    
    public void ResetStats()
    {
        _stats = new EvaluationStats();
    }
    
    private void UpdateStats(bool success, double elapsedMs)
    {
        var newStats = _stats;
        newStats.TotalEvaluations++;
        if (success)
        {
            newStats.SuccessfulEvaluations++;
            newStats.TotalEvaluationTime += elapsedMs;
            newStats.AverageEvaluationTime = newStats.TotalEvaluationTime / newStats.SuccessfulEvaluations;
        }
        else
        {
            newStats.FailedEvaluations++;
        }
        _stats = newStats;
    }
    
    public void Dispose()
    {
        ClearCache();
        // PhoenixExpressionEngine doesn't implement IDisposable, so nothing to dispose
    }
    
    // Compatibility methods for existing code
    public void SetVariable(string name, double value) => Set(name, value);
    public double GetVariable(string name) => Get(name, 0.0);
}
