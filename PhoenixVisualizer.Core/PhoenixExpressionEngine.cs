using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PhoenixVisualizer.Core
{
    /// <summary>
    /// PhoenixExpressionEngine - ns-eel compatible expression evaluator
    /// Supports AVS-style operators, functions, variables, and persistent state
    /// </summary>
    public class PhoenixExpressionEngine
    {
        private readonly Dictionary<string, double> _vars = new();
        private readonly Dictionary<string, Func<double[], double>> _funcs = new();
        private Random _rand;

        public PhoenixExpressionEngine()
        {
            _rand = new Random();
            RegisterStandardFunctions();
        }

        public void Set(string name, double value) => _vars[name] = value;
        public double Get(string name, double def = 0.0) => _vars.TryGetValue(name, out var v) ? v : def;

        public void RegisterStandardFunctions()
        {
            // Core trigonometric functions (NS-EEL compatible)
            _funcs["sin"] = args => Math.Sin(args[0]);
            _funcs["cos"] = args => Math.Cos(args[0]);
            _funcs["tan"] = args => Math.Tan(args[0]);
            _funcs["asin"] = args => Math.Asin(args[0]);
            _funcs["acos"] = args => Math.Acos(args[0]);
            _funcs["atan"] = args => Math.Atan(args[0]);
            _funcs["atan2"] = args => Math.Atan2(args[0], args[1]);

            // Mathematical functions
            _funcs["sqrt"] = args => Math.Sqrt(args[0]);
            _funcs["abs"] = args => Math.Abs(args[0]);
            _funcs["pow"] = args => Math.Pow(args[0], args[1]);
            _funcs["exp"] = args => Math.Exp(args[0]);
            _funcs["log"] = args => Math.Log(args[0]);
            _funcs["log10"] = args => Math.Log10(args[0]);
            _funcs["ceil"] = args => Math.Ceiling(args[0]);
            _funcs["floor"] = args => Math.Floor(args[0]);

            // Comparison and logic functions (NS-EEL style)
            _funcs["min"] = args => Math.Min(args[0], args[1]);
            _funcs["max"] = args => Math.Max(args[0], args[1]);
            _funcs["if"] = args => args[0] != 0 ? args[1] : args[2];
            _funcs["band"] = args => (args[0] != 0 && args[1] != 0) ? 1 : 0; // Logical AND
            _funcs["bor"] = args => (args[0] != 0 || args[1] != 0) ? 1 : 0;  // Logical OR
            _funcs["bnot"] = args => (args[0] == 0) ? 1 : 0;                  // Logical NOT
            _funcs["above"] = args => (args[0] > args[1]) ? 1 : 0;            // NS-EEL above()
            _funcs["below"] = args => (args[0] < args[1]) ? 1 : 0;            // NS-EEL below()
            _funcs["equal"] = args => (Math.Abs(args[0] - args[1]) < 1e-6) ? 1 : 0; // NS-EEL equal()

            // Random and utility functions
            _funcs["rand"] = args => {
                double max = (args.Length > 0) ? args[0] : 1.0;
                return _rand.NextDouble() * max;
            };

            // Sigmoid function (NS-EEL _sig)
            _funcs["sigmoid"] = args => {
                double constraint = (args.Length > 1) ? args[1] : 1.0;
                double t = 1.0 + Math.Exp(-args[0] * constraint);
                return (t != 0) ? 1.0 / t : 0.0;
            };

            // NS-EEL specific functions
            _funcs["sign"] = args => (args[0] > 0) ? 1 : (args[0] < 0) ? -1 : 0;
            _funcs["sqr"] = args => args[0] * args[0];
            _funcs["invsqrt"] = args => 1.0 / Math.Sqrt(args[0]);
        }

        public double Execute(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr)) return 0.0;
            var tokens = Tokenize(expr);
            var rpn = ToRpn(tokens);
            return EvalRpn(rpn);
        }

        private List<string> Tokenize(string expr)
        {
            var tokens = new List<string>();
            var sb = new System.Text.StringBuilder();
            int i = 0;

            while (i < expr.Length)
            {
                char c = expr[i];

                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                // Handle multi-character operators first
                if (i + 1 < expr.Length)
                {
                    string twoCharOp = expr.Substring(i, 2);
                    if (twoCharOp == "||" || twoCharOp == "&&" || twoCharOp == "==" ||
                        twoCharOp == "!=" || twoCharOp == "<=" || twoCharOp == ">=")
                    {
                        if (sb.Length > 0) { tokens.Add(sb.ToString()); sb.Clear(); }
                        tokens.Add(twoCharOp);
                        i += 2;
                        continue;
                    }
                }

                // Handle single-character operators and parentheses
                if ("+-*/%^()=,&|!<>.".Contains(c))
                {
                    if (sb.Length > 0) { tokens.Add(sb.ToString()); sb.Clear(); }
                    tokens.Add(c.ToString());
                }
                else
                {
                    sb.Append(c);
                }

                i++;
            }

            if (sb.Length > 0) tokens.Add(sb.ToString());
            return tokens;
        }

        private Queue<string> ToRpn(List<string> tokens)
        {
            var prec = new Dictionary<string, int> {
                ["="]=1,
                ["||"]=2, ["&&"]=2, // Logical operators (NS-EEL style)
                ["|"]=3, ["&"]=3,   // Bitwise operators
                ["<"]=4, ["<="]=4, [">"]=4, [">="]=4, ["=="]=4, ["!="]=4,
                ["+"]=5, ["-"]=5,
                ["*"]=6, ["/"]=6, ["%"]=6,
                ["^"]=7,            // Exponentiation
                ["!"]=8             // Logical NOT (unary)
            };
            var output = new Queue<string>();
            var stack = new Stack<string>();
            foreach (var token in tokens)
            {
                if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out _)
                    || _vars.ContainsKey(token))
                    output.Enqueue(token);
                else if (_funcs.ContainsKey(token))
                    stack.Push(token);
                else if (prec.ContainsKey(token))
                {
                    while (stack.Count > 0 && prec.ContainsKey(stack.Peek()) && prec[stack.Peek()] >= prec[token])
                        output.Enqueue(stack.Pop());
                    stack.Push(token);
                }
                else if (token == "(") stack.Push(token);
                else if (token == ")")
                {
                    while (stack.Count > 0 && stack.Peek() != "(")
                        output.Enqueue(stack.Pop());
                    if (stack.Count > 0) stack.Pop();
                    if (stack.Count > 0 && _funcs.ContainsKey(stack.Peek()))
                        output.Enqueue(stack.Pop());
                }
                else
                    output.Enqueue(token);
            }
            while (stack.Count > 0) output.Enqueue(stack.Pop());
            return output;
        }

        private double EvalRpn(Queue<string> rpn)
        {
            var stack = new Stack<double>();
            while (rpn.Count > 0)
            {
                var token = rpn.Dequeue();
                if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
                    stack.Push(num);
                else if (_vars.ContainsKey(token))
                    stack.Push(_vars[token]);
                else if (_funcs.ContainsKey(token))
                {
                    // assume max 3 args
                    var args = new List<double>();
                    for (int i = 0; i < 3 && stack.Count > 0; i++)
                        args.Insert(0, stack.Pop());
                    stack.Push(_funcs[token](args.ToArray()));
                }
                else if (token == "=")
                {
                    var val = stack.Pop();
                    var nameVal = stack.Pop();
                    var name = nameVal.ToString()!;
                    _vars[name] = val;
                    stack.Push(val);
                }
                else
                {
                    // Handle unary operators first
                    if (token == "!")
                    {
                        var a = stack.Pop();
                        stack.Push(a == 0 ? 1 : 0);
                    }
                    // Handle binary operators
                    else
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(token switch
                        {
                            "+" => a + b,
                            "-" => a - b,
                            "*" => a * b,
                            "/" => b != 0 ? a / b : 0, // Avoid division by zero
                            "%" => b != 0 ? a % b : 0,  // Avoid modulo by zero
                            "^" => Math.Pow(a, b),

                            // Comparison operators
                            "==" => Math.Abs(a - b) < 1e-10 ? 1 : 0,
                            "!=" => Math.Abs(a - b) >= 1e-10 ? 1 : 0,
                            ">" => a > b ? 1 : 0,
                            "<" => a < b ? 1 : 0,
                            ">=" => a >= b ? 1 : 0,
                            "<=" => a <= b ? 1 : 0,

                            // Logical operators (NS-EEL style)
                            "&&" => (a != 0 && b != 0) ? 1 : 0,
                            "||" => (a != 0 || b != 0) ? 1 : 0,

                            // Bitwise operators
                            "&" => (int)a & (int)b,
                            "|" => (int)a | (int)b,

                            _ => 0
                        });
                    }
                }
            }
            return stack.Count > 0 ? stack.Pop() : 0;
        }
    }
}
