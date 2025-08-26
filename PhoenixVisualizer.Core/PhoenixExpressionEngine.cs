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

        public PhoenixExpressionEngine()
        {
            RegisterStandardFunctions();
        }

        public void Set(string name, double value) => _vars[name] = value;
        public double Get(string name, double def = 0.0) => _vars.TryGetValue(name, out var v) ? v : def;

        public void RegisterStandardFunctions()
        {
            _funcs["sin"] = args => Math.Sin(args[0]);
            _funcs["cos"] = args => Math.Cos(args[0]);
            _funcs["tan"] = args => Math.Tan(args[0]);
            _funcs["atan2"] = args => Math.Atan2(args[0], args[1]);
            _funcs["sqrt"] = args => Math.Sqrt(args[0]);
            _funcs["abs"] = args => Math.Abs(args[0]);
            _funcs["pow"] = args => Math.Pow(args[0], args[1]);
            _funcs["min"] = args => Math.Min(args[0], args[1]);
            _funcs["max"] = args => Math.Max(args[0], args[1]);
            _funcs["rand"] = args => new Random().NextDouble();
            _funcs["if"] = args => args[0] != 0 ? args[1] : args[2];
            _funcs["sigmoid"] = args => 1.0 / (1.0 + Math.Exp(-args[0]));
            _funcs["band"] = args => ((int)args[0] & (int)args[1]) != 0 ? 1 : 0;
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
            var ops = "+-*/%^()=,<>!";
            var tokens = new List<string>();
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];
                if (char.IsWhiteSpace(c)) continue;
                if (ops.Contains(c))
                {
                    if (sb.Length > 0) { tokens.Add(sb.ToString()); sb.Clear(); }
                    tokens.Add(c.ToString());
                }
                else
                {
                    sb.Append(c);
                }
            }
            if (sb.Length > 0) tokens.Add(sb.ToString());
            return tokens;
        }

        private Queue<string> ToRpn(List<string> tokens)
        {
            var prec = new Dictionary<string, int> {
                ["="]=1, ["<"]=2,[">"]=2,["=="]=2,["!="]=2,
                ["+"]=3, ["-"]=3, ["*"]=4, ["/"]=4, ["%"]=4, ["^"]=5
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
                    var b = stack.Pop();
                    var a = stack.Pop();
                    stack.Push(token switch
                    {
                        "+" => a + b,
                        "-" => a - b,
                        "*" => a * b,
                        "/" => a / b,
                        "%" => a % b,
                        "^" => Math.Pow(a, b),
                        "==" => a == b ? 1 : 0,
                        "!=" => a != b ? 1 : 0,
                        ">" => a > b ? 1 : 0,
                        "<" => a < b ? 1 : 0,
                        _ => 0
                    });
                }
            }
            return stack.Count > 0 ? stack.Pop() : 0;
        }
    }
}
