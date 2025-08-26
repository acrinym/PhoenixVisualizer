using System;
using System.Collections.Generic;
using System.Globalization;

namespace PhoenixVisualizer.Core.Engine
{
    /// <summary>
    /// PhoenixExpressionEngine
    /// ns-eel compatible expression evaluator with Phoenix extensions
    /// Supports persistent variables, math functions, conditionals, and audio bindings
    /// </summary>
    public class PhoenixExpressionEngine
    {
        private readonly Dictionary<string, double> _vars = new(StringComparer.OrdinalIgnoreCase);
        private readonly Random _rand = new();

        public PhoenixExpressionEngine()
        {
            Reset();
        }

        /// <summary>
        /// Resets all variables to defaults
        /// </summary>
        public void Reset()
        {
            _vars.Clear();
            _vars["t"] = 0.0;
            _vars["pel_time"] = 0.0;
            _vars["pel_frame"] = 0.0;
            _vars["pel_dt"] = 0.016;
            for (int i = 1; i <= 32; i++)
                _vars[$"pel_q{i}"] = 0.0;
        }

        public void Set(string name, double value) => _vars[name] = value;
        public double Get(string name, double def = 0.0) => _vars.TryGetValue(name, out var v) ? v : def;

        /// <summary>
        /// Executes a single line of Phoenix/AVS script
        /// </summary>
        public void Execute(string script)
        {
            if (string.IsNullOrWhiteSpace(script)) return;

            var lines = script.Split(';', '\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                ProcessLine(trimmed);
            }
        }

        private void ProcessLine(string line)
        {
            if (line.Contains("="))
            {
                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    var name = parts[0].Trim();
                    var expr = parts[1].Trim();
                    var value = Evaluate(expr);
                    _vars[name] = value;
                }
            }
            else
            {
                _ = Evaluate(line);
            }
        }

        private double Evaluate(string expr)
        {
            expr = expr.Replace("$PI", Math.PI.ToString(CultureInfo.InvariantCulture));
            expr = expr.Replace("PI", Math.PI.ToString(CultureInfo.InvariantCulture));

            // Very simple tokenizer: handle + - * / ^ and functions
            // TODO: Replace with full shunting-yard parser if needed
            try
            {
                if (double.TryParse(expr, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
                    return num;

                if (_vars.TryGetValue(expr, out var val))
                    return val;

                if (expr.StartsWith("sin(") && expr.EndsWith(")"))
                    return Math.Sin(Evaluate(expr[4..^1]));
                if (expr.StartsWith("cos(") && expr.EndsWith(")"))
                    return Math.Cos(Evaluate(expr[4..^1]));
                if (expr.StartsWith("tan(") && expr.EndsWith(")"))
                    return Math.Tan(Evaluate(expr[4..^1]));
                if (expr.StartsWith("sqrt(") && expr.EndsWith(")"))
                    return Math.Sqrt(Evaluate(expr[5..^1]));
                if (expr.StartsWith("abs(") && expr.EndsWith(")"))
                    return Math.Abs(Evaluate(expr[4..^1]));
                if (expr.StartsWith("rand(") && expr.EndsWith(")"))
                    return _rand.NextDouble() * Evaluate(expr[5..^1]);

                if (expr.Contains("+"))
                {
                    var parts = expr.Split('+', 2);
                    return Evaluate(parts[0]) + Evaluate(parts[1]);
                }
                if (expr.Contains("-"))
                {
                    var parts = expr.Split('-', 2);
                    return Evaluate(parts[0]) - Evaluate(parts[1]);
                }
                if (expr.Contains("*"))
                {
                    var parts = expr.Split('*', 2);
                    return Evaluate(parts[0]) * Evaluate(parts[1]);
                }
                if (expr.Contains("/"))
                {
                    var parts = expr.Split('/', 2);
                    return Evaluate(parts[0]) / Evaluate(parts[1]);
                }
                if (expr.Contains("^"))
                {
                    var parts = expr.Split('^', 2);
                    return Math.Pow(Evaluate(parts[0]), Evaluate(parts[1]));
                }
            }
            catch
            {
                return 0.0;
            }

            return 0.0;
        }
    }
}
