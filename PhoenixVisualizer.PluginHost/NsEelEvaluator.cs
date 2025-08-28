using PhoenixVisualizer.Core.Effects.Interfaces;

namespace PhoenixVisualizer.PluginHost;

/// <summary>
/// NS-EEL Expression Evaluator
/// Parses and executes Winamp AVS-style expressions
/// </summary>
public sealed class NsEelEvaluator : INsEelEvaluator
{
    // Variable storage
    private readonly Dictionary<string, double> _variables = new();
    private readonly Dictionary<string, double> _perFrameVariables = new();
    private readonly Dictionary<string, double> _perPointVariables = new();
    
    // Built-in functions
    private readonly Dictionary<string, Func<double[], double>> _functions = new();
    
    // Expression cache
    private readonly Dictionary<string, CompiledExpression> _expressionCache = new();
    
    // Current frame context
    private int _currentFrame;
    private int _currentPoint;
    private int _totalPoints;
    private double _frameTime;
    private double _beatTime;
    
    public NsEelEvaluator()
    {
        InitializeBuiltInFunctions();
        InitializeBuiltInVariables();
    }

    private void InitializeBuiltInFunctions()
    {
        // Math functions
        _functions["sin"] = args => Math.Sin(args[0]);
        _functions["cos"] = args => Math.Cos(args[0]);
        _functions["tan"] = args => Math.Tan(args[0]);
        _functions["asin"] = args => Math.Asin(args[0]);
        _functions["acos"] = args => Math.Acos(args[0]);
        _functions["atan"] = args => Math.Atan(args[0]);
        _functions["atan2"] = args => Math.Atan2(args[0], args[1]);
        _functions["sinh"] = args => Math.Sinh(args[0]);
        _functions["cosh"] = args => Math.Cosh(args[0]);
        _functions["tanh"] = args => Math.Tanh(args[0]);
        
        // Power and log functions
        _functions["pow"] = args => Math.Pow(args[0], args[1]);
        _functions["sqrt"] = args => Math.Sqrt(args[0]);
        _functions["log"] = args => Math.Log(args[0]);
        _functions["log10"] = args => Math.Log10(args[0]);
        _functions["exp"] = args => Math.Exp(args[0]);
        
        // Rounding functions
        _functions["floor"] = args => Math.Floor(args[0]);
        _functions["ceil"] = args => Math.Ceiling(args[0]);
        _functions["round"] = args => Math.Round(args[0]);
        _functions["abs"] = args => Math.Abs(args[0]);
        
        // Min/Max functions
        _functions["min"] = args => Math.Min(args[0], args[1]);
        _functions["max"] = args => Math.Max(args[0], args[1]);
        
        // Random functions
        _functions["rand"] = args => new Random().NextDouble();
        _functions["sigmoid"] = args => 1.0 / (1.0 + Math.Exp(-args[0]));
        
        // Audio analysis functions (simplified)
        _functions["bass"] = args => GetBassLevel();
        _functions["mid"] = args => GetMidLevel();
        _functions["treble"] = args => GetTrebleLevel();
        _functions["beat"] = args => IsBeat() ? 1.0 : 0.0;
    }

    private void InitializeBuiltInVariables()
    {
        // Per-frame variables
        _perFrameVariables["time"] = 0.0;
        _perFrameVariables["beat"] = 0.0;
        _perFrameVariables["bass"] = 0.0;
        _perFrameVariables["mid"] = 0.0;
        _perFrameVariables["treble"] = 0.0;
        _perFrameVariables["vol"] = 0.0;
        
        // Per-point variables
        _perPointVariables["x"] = 0.0;
        _perPointVariables["y"] = 0.0;
        _perPointVariables["i"] = 0.0;
        _perPointVariables["n"] = 0.0;
    }

    /// <summary>
    /// Set the current frame context
    /// </summary>
    public void SetFrameContext(int frame, double frameTime, double beatTime)
    {
        _currentFrame = frame;
        _frameTime = frameTime;
        _beatTime = beatTime;
        
        // Update per-frame variables
        _perFrameVariables["time"] = frameTime;
        _perFrameVariables["beat"] = beatTime;
    }

    /// <summary>
    /// Set the current point context
    /// </summary>
    public void SetPointContext(int point, int totalPoints, double x, double y)
    {
        _currentPoint = point;
        _totalPoints = totalPoints;
        _perPointVariables["x"] = x;
        _perPointVariables["y"] = y;
        _perPointVariables["i"] = point;
        _perPointVariables["n"] = totalPoints;
    }

    /// <summary>
    /// Set audio analysis data
    /// </summary>
    public void SetAudioData(float bass, float mid, float treble, float volume, bool beat)
    {
        _perFrameVariables["bass"] = bass;
        _perFrameVariables["mid"] = mid;
        _perFrameVariables["treble"] = treble;
        _perFrameVariables["vol"] = volume;
        _perFrameVariables["beat"] = beat ? 1.0 : 0.0;
    }

    /// <summary>
    /// Set a custom variable
    /// </summary>
    public void SetVariable(string name, double value)
    {
        _variables[name] = value;
    }

    /// <summary>
    /// Get a variable value
    /// </summary>
    public double GetVariable(string name)
    {
        if (_variables.TryGetValue(name, out var value))
            return value;
        if (_perFrameVariables.TryGetValue(name, out value))
            return value;
        if (_perPointVariables.TryGetValue(name, out value))
            return value;
        return 0.0;
    }

    /// <summary>
    /// Check if a variable exists
    /// </summary>
    public bool HasVariable(string name)
    {
        return _variables.ContainsKey(name) || 
               _perFrameVariables.ContainsKey(name) || 
               _perPointVariables.ContainsKey(name);
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        _expressionCache.Clear();
        _variables.Clear();
        _perFrameVariables.Clear();
        _perPointVariables.Clear();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Evaluate an NS-EEL expression
    /// </summary>
    public double Evaluate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return 0.0;

        try
        {
            // Check cache first
            if (_expressionCache.TryGetValue(expression, out var compiled))
            {
                return compiled.Execute(this);
            }

            // Parse and compile
            compiled = ParseExpression(expression);
            _expressionCache[expression] = compiled;
            
            return compiled.Execute(this);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NsEelEvaluator] Error evaluating expression '{expression}': {ex.Message}");
            return 0.0;
        }
    }

    /// <summary>
    /// Parse an expression into a compiled form
    /// </summary>
    private CompiledExpression ParseExpression(string expression)
    {
        // Simple expression parser - supports basic math operations
        var tokens = Tokenize(expression);
        var postfix = ConvertToPostfix(tokens);
        return new CompiledExpression(postfix);
    }

    /// <summary>
    /// Tokenize the expression string
    /// </summary>
    private List<Token> Tokenize(string expression)
    {
        var tokens = new List<Token>();
        var current = 0;
        
        while (current < expression.Length)
        {
            var ch = expression[current];
            
            if (char.IsWhiteSpace(ch))
            {
                current++;
                continue;
            }
            
            if (char.IsLetter(ch))
            {
                // Variable or function
                var start = current;
                while (current < expression.Length && (char.IsLetterOrDigit(ch) || ch == '_'))
                {
                    current++;
                    if (current < expression.Length) ch = expression[current];
                }
                
                var name = expression.Substring(start, current - start);
                
                if (current < expression.Length && expression[current] == '(')
                {
                    // Function
                    tokens.Add(new Token(TokenType.Function, name));
                    tokens.Add(new Token(TokenType.LeftParen, "("));
                    current++;
                }
                else
                {
                    // Variable
                    tokens.Add(new Token(TokenType.Variable, name));
                }
            }
            else if (char.IsDigit(ch) || ch == '.')
            {
                // Number
                var start = current;
                while (current < expression.Length && (char.IsDigit(expression[current]) || expression[current] == '.'))
                {
                    current++;
                }
                
                var number = expression.Substring(start, current - start);
                if (double.TryParse(number, out var value))
                {
                    tokens.Add(new Token(TokenType.Number, value));
                }
            }
            else if (ch == '(')
            {
                tokens.Add(new Token(TokenType.LeftParen, "("));
                current++;
            }
            else if (ch == ')')
            {
                tokens.Add(new Token(TokenType.RightParen, ")"));
                current++;
            }
            else if (ch == '+' || ch == '-' || ch == '*' || ch == '/' || ch == '^' || ch == '%')
            {
                var precedence = GetOperatorPrecedence(ch);
                tokens.Add(new Token(TokenType.Operator, ch.ToString(), precedence));
                current++;
            }
            else
            {
                current++;
            }
        }
        
        return tokens;
    }

    /// <summary>
    /// Convert infix tokens to postfix notation
    /// </summary>
    private List<Token> ConvertToPostfix(List<Token> tokens)
    {
        var output = new List<Token>();
        var operators = new Stack<Token>();
        
        foreach (var token in tokens)
        {
            switch (token.Type)
            {
                case TokenType.Number:
                case TokenType.Variable:
                    output.Add(token);
                    break;
                    
                case TokenType.Function:
                    operators.Push(token);
                    break;
                    
                case TokenType.LeftParen:
                    operators.Push(token);
                    break;
                    
                case TokenType.RightParen:
                    while (operators.Count > 0 && operators.Peek().Type != TokenType.LeftParen)
                    {
                        output.Add(operators.Pop());
                    }
                    if (operators.Count > 0 && operators.Peek().Type == TokenType.LeftParen)
                    {
                        operators.Pop(); // Remove left paren
                    }
                    break;
                    
                case TokenType.Operator:
                    while (operators.Count > 0 && 
                           operators.Peek().Type == TokenType.Operator && 
                           operators.Peek().Precedence >= token.Precedence)
                    {
                        output.Add(operators.Pop());
                    }
                    operators.Push(token);
                    break;
            }
        }
        
        while (operators.Count > 0)
        {
            output.Add(operators.Pop());
        }
        
        return output;
    }

    private int GetOperatorPrecedence(char op)
    {
        return op switch
        {
            '^' => 4,
            '*' or '/' or '%' => 3,
            '+' or '-' => 2,
            _ => 1
        };
    }

    // Simplified audio analysis functions
    private double GetBassLevel() => _perFrameVariables["bass"];
    private double GetMidLevel() => _perFrameVariables["mid"];
    private double GetTrebleLevel() => _perFrameVariables["treble"];
    private bool IsBeat() => _perFrameVariables["beat"] > 0.5;

    // Token types for parsing
    private enum TokenType
    {
        Number,
        Variable,
        Function,
        Operator,
        LeftParen,
        RightParen
    }

    // Token structure
    private class Token
    {
        public TokenType Type { get; }
        public object Value { get; }
        public int Precedence { get; }

        public Token(TokenType type, object value, int precedence = 0)
        {
            Type = type;
            Value = value;
            Precedence = precedence;
        }
    }

    // Compiled expression for execution
    private class CompiledExpression
    {
        private readonly List<Token> _postfixTokens;

        public CompiledExpression(List<Token> postfixTokens)
        {
            _postfixTokens = postfixTokens;
        }

        public double Execute(NsEelEvaluator evaluator)
        {
            var stack = new Stack<double>();

            foreach (var token in _postfixTokens)
            {
                switch (token.Type)
                {
                    case TokenType.Number:
                        stack.Push((double)token.Value);
                        break;

                    case TokenType.Variable:
                        stack.Push(evaluator.GetVariable((string)token.Value));
                        break;

                    case TokenType.Operator:
                        if (stack.Count < 2) return 0.0;
                        var b = stack.Pop();
                        var a = stack.Pop();
                        var result = ApplyOperator((string)token.Value, a, b);
                        stack.Push(result);
                        break;

                    case TokenType.Function:
                        if (stack.Count < 1) return 0.0;
                        var arg = stack.Pop();
                        var funcResult = ApplyFunction((string)token.Value, arg);
                        stack.Push(funcResult);
                        break;
                }
            }

            return stack.Count > 0 ? stack.Pop() : 0.0;
        }

        private double ApplyOperator(string op, double a, double b)
        {
            return op switch
            {
                "+" => a + b,
                "-" => a - b,
                "*" => a * b,
                "/" => b != 0 ? a / b : 0.0,
                "^" => Math.Pow(a, b),
                "%" => b != 0 ? a % b : 0.0,
                _ => 0.0
            };
        }

        private double ApplyFunction(string func, double arg)
        {
            // Simplified function application
            return func switch
            {
                "sin" => Math.Sin(arg),
                "cos" => Math.Cos(arg),
                "tan" => Math.Tan(arg),
                "sqrt" => Math.Sqrt(arg),
                "abs" => Math.Abs(arg),
                "floor" => Math.Floor(arg),
                "ceil" => Math.Ceiling(arg),
                _ => 0.0
            };
        }
    }
}
