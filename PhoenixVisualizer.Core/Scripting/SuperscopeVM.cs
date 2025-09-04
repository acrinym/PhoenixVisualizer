using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PhoenixVisualizer.Core.Transpile;

namespace PhoenixVisualizer.Core.Scripting
{
    /// <summary>
    /// AVS-style Superscope VM supporting INIT / FRAME / BEAT / POINT scripts.
    /// Features:
    ///  - Variables: i, n, t (seconds), w, h, pi, beat (0/1), frame (int), time (alias of t)
    ///  - Functions: sin, cos, tan, asin, acos, atan, sqrt, abs, floor, ceil, pow, min, max, clamp,
    ///               rand([a],[b]), sign, lerp(a,b,t), frac, exp, log, log10, atan2(y,x)
    ///  - Buffers: megabuf(int), gmegabuf(int). Write via assign(megabuf(idx), value) / assign(gmegabuf(idx), value)
    ///  - Statements separated by ';' or newlines. Assignments: a=..., b=...
    ///  - POINT script evaluated per-sample with variable 'i' in [0..n-1]; must set 'x' and 'y'.
    ///  - INIT/FRAME/BEAT can modify 'n' live (clamped).
    /// </summary>
    public sealed class SuperscopeVM
    {
        private readonly Env _env;
        // private readonly Func<int, double>? _randSeeded; // Unused for now
        private Program _progInit = Program.Empty;
        private Program _progFrame = Program.Empty;
        private Program _progBeat = Program.Empty;
        private Program _progPoint = Program.Empty;
        private readonly int _minSamples;
        private readonly int _maxSamples;

        // Keep source + hash for change detection
        private string _srcInit = "", _srcFrame = "", _srcBeat = "", _srcPoint = "";
        private int _hashInit, _hashFrame, _hashBeat, _hashPoint;

        public int Samples { get; private set; }
        public int FrameIndex { get; private set; }

        // Global megabuf shared across all SuperscopeVM instances (per app domain)
        private static readonly SparseBuffer GlobalG = new();
        // Local megabuf per VM (per node)
        private readonly SparseBuffer _localM = new();

        public SuperscopeVM(int minSamples = 16, int maxSamples = 8192, int defaultSamples = 512)
        {
            _minSamples = minSamples;
            _maxSamples = maxSamples;
            _env = new Env(GlobalG, _localM);
            Samples = ClampSamples(defaultSamples);
            FrameIndex = 0;
            // _randSeeded = null; // Unused for now
        }

        public void LoadScripts(string init, string frame, string beat, string point)
        {
            LoadScript(ref _srcInit, ref _hashInit, ref _progInit, init);
            LoadScript(ref _srcFrame, ref _hashFrame, ref _progFrame, frame);
            LoadScript(ref _srcBeat, ref _hashBeat, ref _progBeat, beat);
            LoadScript(ref _srcPoint, ref _hashPoint, ref _progPoint, point);
        }

        private static void LoadScript(ref string src, ref int hash, ref Program prog, string newSrc)
        {
            newSrc ??= string.Empty;
            var h = newSrc.GetHashCode();
            if (h == hash) return;
            src = newSrc;
            hash = h;
            prog = Compiler.Compile(newSrc);
        }

        public void Init(double t, int w, int h, int? forcedSamples = null)
        {
            _env.Reset();
            SeedCommon(t, w, h, beat: 0);
            _progInit.Execute(_env);
            if (forcedSamples.HasValue) Samples = ClampSamples(forcedSamples.Value);
            else Samples = ClampSamples((int)_env.Get("n", Samples));
            // ensure sensible defaults
            if (!_env.Has("x")) _env.Set("x", 0);
            if (!_env.Has("y")) _env.Set("y", 0);
        }

        public void Beat(double t, int w, int h)
        {
            SeedCommon(t, w, h, beat: 1);
            _progBeat.Execute(_env);
            Samples = ClampSamples((int)_env.Get("n", Samples));
        }

        public void Frame(double t, int w, int h)
        {
            SeedCommon(t, w, h, beat: 0);
            _progFrame.Execute(_env);
            Samples = ClampSamples((int)_env.Get("n", Samples));
            FrameIndex++;
        }

        /// <summary>Evaluate point for current 'i' index. Returns (x,y) in normalized [-1..1].</summary>
        public (double x, double y) Point(int i)
        {
            _env.Set("i", i);
            _progPoint.Execute(_env);
            var x = _env.Get("x", 0.0);
            var y = _env.Get("y", 0.0);
            return (x, y);
        }

        private void SeedCommon(double t, int w, int h, int beat)
        {
            _env.Set("t", t);
            _env.Set("time", t);
            _env.Set("w", w);
            _env.Set("h", h);
            _env.Set("n", Samples);
            _env.Set("pi", Math.PI);
            _env.Set("beat", beat);
            _env.Set("frame", FrameIndex);
        }

        private int ClampSamples(int n) => Math.Clamp(n, _minSamples, _maxSamples);

        // ====== Environment, sparse buffers, program/runtime ======
        private sealed class SparseBuffer
        {
            private readonly Dictionary<int, double> _map = new();
            public double Get(int i) => _map.TryGetValue(i, out var v) ? v : 0.0;
            public void Set(int i, double v) { _map[i] = v; }
        }

        private sealed class Env
        {
            private readonly Dictionary<string, double> _vars = new(StringComparer.OrdinalIgnoreCase);
            private readonly SparseBuffer _g, _m;
            private readonly Random _rng = new();
            public Env(SparseBuffer g, SparseBuffer m) { _g = g; _m = m; }
            public void Reset()
            {
                _vars.Clear();
            }
            public bool Has(string k) => _vars.ContainsKey(k);
            public double Get(string k, double dflt = 0.0) => _vars.TryGetValue(k, out var v) ? v : dflt;
            public void Set(string k, double v) => _vars[k] = v;

            // Function dispatch
            public double Call(string name, IReadOnlyList<double> args)
            {
                switch (name.ToLowerInvariant())
                {
                    case "sin": return Math.Sin(arg(0));
                    case "cos": return Math.Cos(arg(0));
                    case "tan": return Math.Tan(arg(0));
                    case "asin": return Math.Asin(arg(0));
                    case "acos": return Math.Acos(arg(0));
                    case "atan": return Math.Atan(arg(0));
                    case "atan2": return Math.Atan2(arg(0), arg(1));
                    case "sqrt": return Math.Sqrt(arg(0));
                    case "abs": return Math.Abs(arg(0));
                    case "floor": return Math.Floor(arg(0));
                    case "ceil": return Math.Ceiling(arg(0));
                    case "pow": return Math.Pow(arg(0), arg(1));
                    case "exp": return Math.Exp(arg(0));
                    case "log": return Math.Log(arg(0));
                    case "log10": return Math.Log10(arg(0));
                    case "min": return Math.Min(arg(0), arg(1));
                    case "max": return Math.Max(arg(0), arg(1));
                    case "clamp": { var x = arg(0); var a = arg(1); var b = arg(2); return Math.Min(Math.Max(x, a), b); }
                    case "sign": { var x = arg(0); return x < 0 ? -1 : (x > 0 ? 1 : 0); }
                    case "lerp": { var a = arg(0); var b = arg(1); var t = arg(2); return a + (b - a) * t; }
                    case "frac": { var x = arg(0); return x - Math.Floor(x); }
                    case "rand":
                        if (args.Count == 0) return _rng.NextDouble();
                        if (args.Count == 1) { var hi = arg(0); return _rng.NextDouble() * hi; }
                        else { var lo = arg(0); var hi = arg(1); return lo + _rng.NextDouble() * (hi - lo); }
                    case "megabuf": return _m.Get((int)arg(0));
                    case "gmegabuf": return _g.Get((int)arg(0));
                    case "assign": // assign(megabuf(i), value) or assign(gmegabuf(j), value) or assign(varname,value)
                    {
                        if (_lastFuncRef is FuncRef fr)
                        {
                            var val = arg(args.Count - 1);
                            if (fr.Kind == FuncRef.RefKind.Mega) _m.Set(fr.Index, val);
                            else if (fr.Kind == FuncRef.RefKind.GMega) _g.Set(fr.Index, val);
                            _lastFuncRef = null;
                            return val;
                        }
                        // assign("var", value)
                        if (_lastIdentRef is string ident)
                        {
                            var val = arg(1);
                            Set(ident, val);
                            _lastIdentRef = null;
                            return val;
                        }
                        return arg(args.Count - 1);
                    }
                    default: return 0.0;
                }
                double arg(int i) => i < args.Count ? args[i] : 0.0;
            }

            // For capturing l-values like megabuf(i) or an identifier on left side of assign(...)
            private FuncRef? _lastFuncRef;
            private string? _lastIdentRef;
            public void SetLastFuncRef(string name, IReadOnlyList<double> args)
            {
                if (name.Equals("megabuf", StringComparison.OrdinalIgnoreCase))
                    _lastFuncRef = new FuncRef(FuncRef.RefKind.Mega, (int)(args.Count > 0 ? args[0] : 0));
                else if (name.Equals("gmegabuf", StringComparison.OrdinalIgnoreCase))
                    _lastFuncRef = new FuncRef(FuncRef.RefKind.GMega, (int)(args.Count > 0 ? args[0] : 0));
            }
            public void SetLastIdentRef(string ident) { _lastIdentRef = ident; }

            private readonly struct FuncRef
            {
                public enum RefKind { Mega, GMega }
                public FuncRef(RefKind k, int idx) { Kind = k; Index = idx; }
                public RefKind Kind { get; }
                public int Index { get; }
            }
        }

        private sealed class Program
        {
            private readonly List<IStmt> _stmts;
            public static Program Empty { get; } = new(new List<IStmt>());
            public Program(List<IStmt> stmts) { _stmts = stmts; }
            public void Execute(Env env)
            {
                for (int i = 0; i < _stmts.Count; i++) _stmts[i].Run(env);
            }
        }

        private interface IStmt { void Run(Env env); }
        private sealed class AssignStmt : IStmt
        {
            private readonly string _ident;
            private readonly IExpr _rhs;
            public AssignStmt(string ident, IExpr rhs) { _ident = ident; _rhs = rhs; }
            public void Run(Env env)
            {
                env.SetLastIdentRef(_ident);
                env.Set(_ident, _rhs.Eval(env));
            }
        }
        private sealed class ExprStmt : IStmt
        {
            private readonly IExpr _expr;
            public ExprStmt(IExpr e) { _expr = e; }
            public void Run(Env env) { _expr.Eval(env); }
        }

        private interface IExpr { double Eval(Env env); }
        private sealed class ConstExpr : IExpr
        {
            private readonly double _v; public ConstExpr(double v) { _v = v; }
            public double Eval(Env env) => _v;
        }
        private sealed class VarExpr : IExpr
        {
            private readonly string _name; public VarExpr(string n) { _name = n; }
            public double Eval(Env env) => env.Get(_name, 0.0);
        }
        private sealed class UnaryExpr : IExpr
        {
            private readonly string _op; private readonly IExpr _a;
            public UnaryExpr(string op, IExpr a) { _op = op; _a = a; }
            public double Eval(Env env) => _op switch { "-" => -_a.Eval(env), "+" => +_a.Eval(env), "!" => (_a.Eval(env) == 0 ? 1 : 0), _ => 0.0 };
        }
        private sealed class BinaryExpr : IExpr
        {
            private readonly string _op; private readonly IExpr _a, _b;
            public BinaryExpr(string op, IExpr a, IExpr b) { _op = op; _a = a; _b = b; }
            public double Eval(Env env)
            {
                var x = _a.Eval(env); var y = _b.Eval(env);
                return _op switch
                {
                    "+" => x + y,
                    "-" => x - y,
                    "*" => x * y,
                    "/" => y != 0 ? x / y : 0,
                    "%" => y != 0 ? x % y : 0,
                    "^" => Math.Pow(x, y),
                    "==" => x == y ? 1 : 0,
                    "!=" => x != y ? 1 : 0,
                    "<"  => x < y ? 1 : 0,
                    "<=" => x <= y ? 1 : 0,
                    ">"  => x > y ? 1 : 0,
                    ">=" => x >= y ? 1 : 0,
                    "&&" => (x != 0 && y != 0) ? 1 : 0,
                    "||" => (x != 0 || y != 0) ? 1 : 0,
                    _ => 0.0
                };
            }
        }
        private sealed class CallExpr : IExpr
        {
            private readonly string _name; private readonly IExpr[] _args;
            public CallExpr(string n, IExpr[] args) { _name = n; _args = args; }
            public double Eval(Env env)
            {
                var vs = new double[_args.Length];
                for (int i = 0; i < _args.Length; i++) vs[i] = _args[i].Eval(env);
                // allow assign(megabuf(i), val) to capture lvalue
                if (_name.Equals("megabuf", StringComparison.OrdinalIgnoreCase) ||
                    _name.Equals("gmegabuf", StringComparison.OrdinalIgnoreCase))
                {
                    env.SetLastFuncRef(_name, vs);
                }
                return env.Call(_name, vs);
            }
        }

        private static class Compiler
        {
            public static Program Compile(string src)
            {
                var tokens = Tokenize(src);
                var p = new Parser(tokens);
                return new Program(p.ParseStatements());
            }

            private static List<Token> Tokenize(string src)
            {
                var list = new List<Token>();
                if (string.IsNullOrWhiteSpace(src)) return list;
                int i = 0;
                while (i < src.Length)
                {
                    char c = src[i];
                    if (char.IsWhiteSpace(c))
                    {
                        if (c == '\n' || c == '\r') list.Add(new Token(TokenKind.Semi, ";"));
                        i++; continue;
                    }
                    if (char.IsLetter(c) || c == '_' || c == '$')
                    {
                        int j = i + 1;
                        while (j < src.Length && (char.IsLetterOrDigit(src[j]) || src[j] == '_' || src[j] == '$')) j++;
                        list.Add(new Token(TokenKind.Ident, src.Substring(i, j - i)));
                        i = j; continue;
                    }
                    if (char.IsDigit(c) || (c == '.' && i + 1 < src.Length && char.IsDigit(src[i + 1])))
                    {
                        int j = i + 1;
                        while (j < src.Length && (char.IsDigit(src[j]) || src[j] == '.')) j++;
                        var num = src.Substring(i, j - i);
                        if (!double.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out var _))
                        {
                            // tolerate malformed, treat as 0
                            num = "0";
                        }
                        list.Add(new Token(TokenKind.Number, num));
                        i = j; continue;
                    }
                    switch (c)
                    {
                        case ';': list.Add(new Token(TokenKind.Semi, ";")); i++; break;
                        case ',': list.Add(new Token(TokenKind.Comma, ",")); i++; break;
                        case '(': list.Add(new Token(TokenKind.LParen, "(")); i++; break;
                        case ')': list.Add(new Token(TokenKind.RParen, ")")); i++; break;
                        case '+': case '-': case '*': case '/': case '%': case '^':
                            list.Add(new Token(TokenKind.Op, c.ToString())); i++; break;
                        case '!':
                            if (i + 1 < src.Length && src[i + 1] == '=') { list.Add(new Token(TokenKind.Op, "!=")); i += 2; }
                            else { list.Add(new Token(TokenKind.Op, "!")); i++; }
                            break;
                        case '=':
                            if (i + 1 < src.Length && src[i + 1] == '=') { list.Add(new Token(TokenKind.Op, "==")); i += 2; }
                            else { list.Add(new Token(TokenKind.Assign, "=")); i++; }
                            break;
                        case '<':
                            if (i + 1 < src.Length && src[i + 1] == '=') { list.Add(new Token(TokenKind.Op, "<=")); i += 2; }
                            else { list.Add(new Token(TokenKind.Op, "<")); i++; }
                            break;
                        case '>':
                            if (i + 1 < src.Length && src[i + 1] == '=') { list.Add(new Token(TokenKind.Op, ">=")); i += 2; }
                            else { list.Add(new Token(TokenKind.Op, ">")); i++; }
                            break;
                        case '&':
                            if (i + 1 < src.Length && src[i + 1] == '&') { list.Add(new Token(TokenKind.Op, "&&")); i += 2; }
                            else i++; // ignore invalid single &
                            break;
                        case '|':
                            if (i + 1 < src.Length && src[i + 1] == '|') { list.Add(new Token(TokenKind.Op, "||")); i += 2; }
                            else i++;
                            break;
                        default:
                            i++; // ignore unknown character
                            break;
                    }
                }
                return list;
            }

            private enum TokenKind { Number, Ident, Op, Assign, LParen, RParen, Comma, Semi, End }
            private readonly struct Token
            {
                public TokenKind Kind { get; }
                public string Text { get; }
                public Token(TokenKind k, string t) { Kind = k; Text = t; }
                public override string ToString() => $"{Kind}:{Text}";
            }

            private sealed class Parser
            {
                private readonly List<Token> _toks;
                private int _i;
                public Parser(List<Token> toks) { _toks = toks; _i = 0; }
                private Token Peek(int k = 0) => _i + k < _toks.Count ? _toks[_i + k] : new Token(TokenKind.End, "");
                private Token Pop() => _i < _toks.Count ? _toks[_i++] : new Token(TokenKind.End, "");
                private bool Match(TokenKind k, string? text = null)
                {
                    var t = Peek();
                    if (t.Kind != k) return false;
                    if (text != null && !string.Equals(t.Text, text, StringComparison.Ordinal)) return false;
                    _i++;
                    return true;
                }

                public List<IStmt> ParseStatements()
                {
                    var stmts = new List<IStmt>();
                    while (true)
                    {
                        SkipSemis();
                        var t = Peek();
                        if (t.Kind == TokenKind.End) break;
                        var st = ParseStatement();
                        if (st != null) stmts.Add(st);
                        // eat optional semis
                        while (Match(TokenKind.Semi)) { }
                    }
                    return stmts;
                }

                private void SkipSemis() { while (Match(TokenKind.Semi)) { } }

                private IStmt? ParseStatement()
                {
                    // Try assignment: IDENT '=' expr
                    if (Peek().Kind == TokenKind.Ident && Peek(1).Kind == TokenKind.Assign)
                    {
                        var ident = Pop().Text;
                        Pop(); // '='
                        var rhs = ParseExpr();
                        return new AssignStmt(ident, rhs);
                    }
                    // Otherwise expression statement
                    var e = ParseExpr();
                    return e != null ? new ExprStmt(e) : null;
                }

                // Pratt parser
                private IExpr ParseExpr(int rbp = 0)
                {
                    var t = Pop();
                    IExpr left = Nud(t);
                    while (true)
                    {
                        var n = Peek();
                        if (n.Kind == TokenKind.End || n.Kind == TokenKind.Semi || n.Kind == TokenKind.RParen || n.Kind == TokenKind.Comma) break;
                        int lbp = Lbp(n);
                        if (lbp <= rbp) break;
                        Pop();
                        left = Led(n, left);
                    }
                    return left;
                }

                private IExpr Nud(Token t)
                {
                    switch (t.Kind)
                    {
                        case TokenKind.Number:
                            double.TryParse(t.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v);
                            return new ConstExpr(v);
                        case TokenKind.Ident:
                        {
                            var name = t.Text;
                            if (Match(TokenKind.LParen))
                            {
                                var args = new List<IExpr>();
                                if (!Match(TokenKind.RParen))
                                {
                                    do { args.Add(ParseExpr()); } while (Match(TokenKind.Comma));
                                    Match(TokenKind.RParen);
                                }
                                return new CallExpr(name, args.ToArray());
                            }
                            return new VarExpr(name);
                        }
                        case TokenKind.Op:
                            if (t.Text is "+" or "-" or "!")
                                return new UnaryExpr(t.Text, ParseExpr(70));
                            break;
                        case TokenKind.LParen:
                        {
                            var e = ParseExpr();
                            Match(TokenKind.RParen);
                            return e;
                        }
                    }
                    // default
                    return new ConstExpr(0);
                }

                private IExpr Led(Token t, IExpr left)
                {
                    switch (t.Kind)
                    {
                        case TokenKind.Op:
                            var op = t.Text;
                            var rbp = op switch
                            {
                                "||" => 10,
                                "&&" => 15,
                                "==" or "!=" => 20,
                                "<" or "<=" or ">" or ">=" => 30,
                                "+" or "-" => 40,
                                "*" or "/" or "%" => 50,
                                "^" => 60,
                                _ => 40
                            };
                            var right = ParseExpr(rbp);
                            return new BinaryExpr(op, left, right);
                    }
                    return left;
                }

                private static int Lbp(Token t) => t.Kind switch
                {
                    TokenKind.Op => t.Text switch
                    {
                        "||" => 10,
                        "&&" => 15,
                        "==" or "!=" => 20,
                        "<" or "<=" or ">" or ">=" => 30,
                        "+" or "-" => 40,
                        "*" or "/" or "%" => 50,
                        "^" => 60,
                        _ => 0
                    },
                    _ => 0
                };
            }
        }
    }
}
