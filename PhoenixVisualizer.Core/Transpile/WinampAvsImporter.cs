using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace PhoenixVisualizer.Core.Transpile
{
    public static class WinampAvsImporter
    {
        public static UnifiedGraph Import(byte[] bytes)
        {
            var graph = new UnifiedGraph();
            if (bytes == null || bytes.Length == 0) return graph;

            if (LooksLikeNullsoftBinary(bytes))
            {
                return ImportNullsoftBinary(bytes);
            }

            var text = DecodeBestEffort(bytes);
            if (LooksLikePhoenixAvsText(text))
            {
                return ImportPhoenixAvsText(text);
            }

            // Fallback: treat as single script payload (point)
            graph.Nodes.Add(MakeSuperscope("Imported Script", init: "", frame: "", beat: "", point: text));
            return graph;
        }

        // ---------- Phoenix AVS (text) ----------
        private static UnifiedGraph ImportPhoenixAvsText(string text)
        {
            var graph = new UnifiedGraph();
            // Split into [presetXX] blocks, keep header bits above first preset too
            var blocks = Regex.Split(text, @"(?im)^\s*\[(?:preset\d+)\]\s*$")
                              .Select(s => s.Trim()).ToList();

            // Quick scan for single-block (no [preset]) files
            if (blocks.Count == 1 && text.IndexOf("INIT", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                graph.Nodes.Add(ParsePresetBlock("Superscope", text));
                return graph;
            }

            // Multi-preset: scan the whole text for [presetXX] headers and take each region
            var presetMatches = Regex.Matches(text, @"(?im)^\s*\[(preset\d+)\]\s*$");
            var starts = presetMatches.Cast<Match>().Select(m => m.Index).Concat(new[] { text.Length }).ToArray();
            for (int i = 0; i < presetMatches.Count; i++)
            {
                var from = starts[i];
                var to   = starts[i + 1];
                var name = presetMatches[i].Groups[1].Value;
                var slice = text.Substring(from, to - from);

                // Try to read "sn=Superscope (...)" else default name
                var disp = Regex.Match(slice, @"(?im)^\s*sn\s*=\s*(.+)$").Success
                    ? Regex.Match(slice, @"(?im)^\s*sn\s*=\s*(.+)$").Groups[1].Value.Trim()
                    : name;

                var node = ParsePresetBlock(disp, slice);
                if (node != null)
                    graph.Nodes.Add(node);
            }

            if (graph.Nodes.Count == 0)
                graph.Nodes.Add(MakeSuperscope("Empty Preset"));

            return graph;
        }

        private static UnifiedEffectNode ParsePresetBlock(string displayName, string block)
        {
            // Sections are tagged with uppercase keywords on their own lines:
            // INIT / FRAME / BEAT / CODE(=POINT)
            string Grab(string tag)
            {
                // Capture everything from TAG line to next ALL CAPS tag or block end
                var rx = new Regex($@"(?ims)^\s*{tag}\s*\r?\n(.*?)(?=^\s*(INIT|FRAME|BEAT|CODE)\s*$|^\s*\[\w+]|$)");
                var m = rx.Match(block);
                return m.Success ? m.Groups[1].Value.Trim() : "";
            }

            var init  = Grab("INIT");
            var frame = Grab("FRAME");
            var beat  = Grab("BEAT");
            var point = Grab("CODE"); // Phoenix AVS uses CODE for point section

            // Samples (n=) if present
            var samples = 512;
            var nMatch = Regex.Match(block, @"(?im)^\s*n\s*=\s*(\d+(\.\d+)?)");
            if (nMatch.Success && int.TryParse(nMatch.Groups[1].Value.Split('.')[0], out var nVal))
                samples = Math.Clamp(nVal, 1, 128000);

            return MakeSuperscope(displayName, init, frame, beat, point, samples);
        }

        // ---------- Nullsoft AVS (binary) ----------
        private static UnifiedGraph ImportNullsoftBinary(byte[] bytes)
        {
            // Title / author if present
            var title = "Imported AVS";
            var mTitle = Regex.Match(DecodeLatin1(bytes), @"(?!Nullsoft AVS Preset)[ -~]{6,}");
            if (mTitle.Success)
            {
                var s = mTitle.Value.Trim();
                if (!s.StartsWith("Nullsoft AVS"))
                    title = s.Replace("\0", "").Trim();
            }

            // Extract readable ASCII "code-ish" fragments
            var ascii = ExtractAsciiChunks(bytes);

            // Heuristics:
            // - point: anything that sets x= or y= OR references i and trig/PI
            // - init: lines that set n= and avoid i-heavy math
            // - frame: the rest
            var pointLines = new List<string>();
            var initLines  = new List<string>();
            var frameLines = new List<string>();

            foreach (var line in ascii)
            {
                var s = line.Trim();
                if (s.Length < 8 || !s.Contains('=')) continue;

                bool looksPoint = s.Contains("x=") || s.Contains("y=") ||
                                  Regex.IsMatch(s, @"\bi\b") && Regex.IsMatch(s, @"\b(\$PI|sin|cos|rad)"); // i + trig
                bool setsN      = Regex.IsMatch(s, @"(?<![a-zA-Z])n\s*=");

                if (looksPoint)
                    pointLines.Add(s);
                else if (setsN && !Regex.IsMatch(s, @"\bi\b"))
                    initLines.Add(s);
                else
                    frameLines.Add(s);
            }

            // Coalesce and sanitize
            string Coalesce(IEnumerable<string> lines) =>
                string.Join("\n", lines.Distinct().Take(400));

            var init  = Coalesce(initLines);
            var frame = Coalesce(frameLines);
            var point = Coalesce(pointLines);

            // Reasonable default if nothing detected
            if (string.IsNullOrWhiteSpace(point)) point = "// (no obvious point code found)\n// x=...; y=...;";
            if (string.IsNullOrWhiteSpace(init))  init  = "n=400;";
            var node = MakeSuperscope(title, init, frame, beat: "", point: point);
            return new UnifiedGraph { Nodes = new List<UnifiedEffectNode> { node } };
        }

        // ---------- helpers ----------
        private static UnifiedEffectNode MakeSuperscope(string displayName,
            string init = "", string frame = "", string beat = "", string point = "", int samples = 512)
        {
            var node = new UnifiedEffectNode
            {
                TypeKey = "superscope",
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Superscope" : displayName
            };
            node.Parameters["init"] = init ?? "";
            node.Parameters["frame"] = frame ?? "";
            node.Parameters["beat"] = beat ?? "";
            node.Parameters["point"] = point ?? "";
            node.Parameters["samples"] = samples;
            return node;
        }

        private static bool LooksLikeNullsoftBinary(byte[] b)
        {
            var magic = Encoding.ASCII.GetBytes("Nullsoft AVS Preset 0.2");
            if (b.Length < magic.Length) return false;
            for (int i = 0; i < magic.Length; i++)
                if (b[i] != magic[i]) return false;
            // Binary guard: first 256 bytes not mostly printable
            int printable = 0, total = Math.Min(256, b.Length);
            for (int i = 0; i < total; i++)
            {
                byte x = b[i];
                if ((x >= 32 && x <= 126) || x == 9 || x == 10 || x == 13) printable++;
            }
            return printable < total * 0.9;
        }

        private static bool LooksLikePhoenixAvsText(string t)
            => t.IndexOf("[avs]", StringComparison.OrdinalIgnoreCase) >= 0
            || Regex.IsMatch(t, @"(?mi)^\s*sn\s*=\s*Superscope");

        private static string DecodeBestEffort(byte[] bytes)
        {
            try { return Encoding.UTF8.GetString(bytes); }
            catch { return DecodeLatin1(bytes); }
        }

        private static string DecodeLatin1(byte[] bytes) => Encoding.GetEncoding("latin-1").GetString(bytes);

        private static IEnumerable<string> ExtractAsciiChunks(byte[] b)
        {
            var s = DecodeLatin1(b);
            // pull out medium+ length printable runs, then split at semicolons for "statements"
            var runs = Regex.Matches(s, @"[ -~]{10,}").Cast<Match>().Select(m => m.Value);
            foreach (var run in runs)
            {
                foreach (var part in run.Split(';'))
                {
                    var line = (part + ";").Trim();
                    if (line.Length >= 8 && line.Contains('='))
                        yield return line;
                }
            }
        }
    }
}
