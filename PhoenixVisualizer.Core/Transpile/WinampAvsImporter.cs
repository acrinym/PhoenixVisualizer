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

            // Extract readable ASCII "code-ish" fragments using proper binary parsing
            var ascii = ExtractBinaryAsciiChunks(bytes);

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
            string Coalesce(IEnumerable<string> lines)
            {
                var cleaned = lines.Distinct()
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => CleanScriptLine(line))
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Take(400);
                return string.Join("\n", cleaned);
            };

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

        private static string DecodeLatin1(byte[] bytes) => Encoding.GetEncoding("iso-8859-1").GetString(bytes);

        private static IEnumerable<string> ExtractBinaryAsciiChunks(byte[] b)
        {
            // Based on Winamp AVS binary format analysis
            // Skip the signature: "Nullsoft AVS Preset 0.2\x1a"
            var signature = Encoding.ASCII.GetBytes("Nullsoft AVS Preset 0.2");
            int startPos = 0;
            
            // Find signature
            for (int i = 0; i <= b.Length - signature.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < signature.Length; j++)
                {
                    if (b[i + j] != signature[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    startPos = i + signature.Length;
                    // Skip the version byte and terminator
                    if (startPos < b.Length && b[startPos] == 0x1A) startPos++;
                    break;
                }
            }

            // Extract strings using Winamp's length-prefixed format
            var strings = new List<string>();
            int pos = startPos;
            
            while (pos < b.Length - 4)
            {
                // Read 4-byte length (little-endian)
                int length = b[pos] | (b[pos + 1] << 8) | (b[pos + 2] << 16) | (b[pos + 3] << 24);
                pos += 4;
                
                if (length <= 0 || length > 10000 || pos + length > b.Length) break;
                
                // Extract the string data
                var stringData = new byte[length];
                Array.Copy(b, pos, stringData, 0, length);
                pos += length;
                
                // Convert to string and check if it looks like code
                var str = Encoding.ASCII.GetString(stringData);
                if (ContainsScriptPattern(str))
                {
                    strings.Add(str);
                }
            }
            
            // Also look for embedded ASCII in the binary data
            var asciiRuns = ExtractAsciiRuns(b, startPos);
            strings.AddRange(asciiRuns);
            
            return strings.Distinct();
        }
        
        private static IEnumerable<string> ExtractAsciiRuns(byte[] data, int startPos)
        {
            var runs = new List<string>();
            var currentRun = new List<byte>();
            
            for (int i = startPos; i < data.Length; i++)
            {
                byte b = data[i];
                if (b >= 32 && b <= 126) // Printable ASCII
                {
                    currentRun.Add(b);
                }
                else if (b == 9 || b == 10 || b == 13) // Tab, LF, CR
                {
                    currentRun.Add(b);
                }
                else
                {
                    // End of run
                    if (currentRun.Count >= 10)
                    {
                        var runStr = Encoding.ASCII.GetString(currentRun.ToArray());
                        if (ContainsScriptPattern(runStr))
                        {
                            runs.Add(runStr);
                        }
                    }
                    currentRun.Clear();
                }
            }
            
            // Handle final run
            if (currentRun.Count >= 10)
            {
                var runStr = Encoding.ASCII.GetString(currentRun.ToArray());
                if (ContainsScriptPattern(runStr))
                {
                    runs.Add(runStr);
                }
            }
            
            return runs;
        }
        
        private static bool ContainsScriptPattern(string text)
        {
            // Look for common AVS script patterns
            return text.Contains("=") || // Variable assignment
                   text.Contains("sin(") || text.Contains("cos(") || text.Contains("tan(") || // Math functions
                   text.Contains("getosc(") || text.Contains("getspec(") || // AVS functions
                   text.Contains("if(") || text.Contains("above(") || text.Contains("below(") || // Conditionals
                   text.Contains("x=") || text.Contains("y=") || text.Contains("n=") || // Common variables
                   text.Contains("red=") || text.Contains("green=") || text.Contains("blue=") || // Color variables
                   Regex.IsMatch(text, @"[a-zA-Z_][a-zA-Z0-9_]*\s*=") || // Variable assignments
                   Regex.IsMatch(text, @"[a-zA-Z_][a-zA-Z0-9_]*\s*\("); // Function calls
        }
        
        private static string CleanScriptLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return "";
            
            // Remove any remaining non-printable characters
            var cleaned = new string(line.Where(c => 
                (c >= 32 && c <= 126) || // Printable ASCII
                c == 9 || c == 10 || c == 13 // Tab, LF, CR
            ).ToArray());
            
            // Remove common artifacts and noise
            cleaned = Regex.Replace(cleaned, @"[◇◆■□▪▫▬▭▮▯▰▱▲△▴▵▶▷▸▹►▻▼▽▾▿◀◁◂◃◄◅◆◇◈◉◊○◌◍◎●◐◑◒◓◔◕◖◗◘◙◚◛◜◝◞◟◠◡◢◣◤◥◦◧◨◩◪◫◬◭◮◯]", "");
            cleaned = Regex.Replace(cleaned, @"[^\x20-\x7E\t\n\r]", ""); // Remove any remaining non-printable
            
            // Remove specific binary artifacts that appear in AVS files
            cleaned = Regex.Replace(cleaned, @"\$@[A-Za-z\s]+Config\$\/", ""); // Remove $@AVS Config$/ patterns
            cleaned = Regex.Replace(cleaned, @"\$[A-Za-z\s]{3,}\$", ""); // Remove $...$ patterns (only long ones)
            cleaned = Regex.Replace(cleaned, @"@[A-Z]{2,}[0-9_]*", ""); // Remove @variable patterns (only uppercase)
            cleaned = Regex.Replace(cleaned, @"\+%[A-Z]{2,}[0-9_]*", ""); // Remove +%variable patterns (only uppercase)
            cleaned = Regex.Replace(cleaned, @"\\""[A-Za-z0-9_]+", ""); // Remove "variable patterns
            
            // Remove common binary noise patterns
            cleaned = Regex.Replace(cleaned, @"[A-Z]{3,}[0-9]*\.[0-9]+\+", ""); // Remove version-like patterns
            cleaned = Regex.Replace(cleaned, @"[A-Z]{3,}\([A-Za-z0-9\s,]*\)", ""); // Remove function-like noise (only uppercase)
            
            // Clean up whitespace and normalize
            cleaned = Regex.Replace(cleaned, @"\s+", " "); // Normalize whitespace
            cleaned = cleaned.Trim();
            
            // Ensure proper statement termination
            if (!cleaned.EndsWith(";") && !cleaned.EndsWith("}") && !cleaned.EndsWith(")"))
            {
                cleaned += ";";
            }
            
            return cleaned;
        }
    }
}
