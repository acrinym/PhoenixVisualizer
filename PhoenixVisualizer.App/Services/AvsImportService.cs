using System.Text.RegularExpressions;
using System.Text;
using System.IO;

namespace PhoenixVisualizer.App.Services
{
    public class AvsImportService
    {
        public class AvsSuperscope
        {
            public string Name { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
            public string FilePath { get; set; } = string.Empty;
            public DateTime ImportDate { get; set; } = DateTime.Now;
            public bool IsValid { get; set; } = true;
            public string ErrorMessage { get; set; } = string.Empty;
        }

        public class AvsFileInfo
        {
            public string FilePath { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
            public List<AvsSuperscope> Superscopes { get; set; } = [];
            public List<AvsEffect> Effects { get; set; } = [];
            public bool HasSuperscopes => Superscopes.Count > 0;
            public bool HasEffects => Effects.Count > 0;
            public string RawContent { get; set; } = string.Empty;
            public byte[] RawBinaryData { get; set; } = Array.Empty<byte>();
            public string PresetName { get; set; } = string.Empty;
            public string PresetDescription { get; set; } = string.Empty;
            public string Author { get; set; } = string.Empty;
            public DateTime LastModified { get; set; }
            public long FileSize { get; set; }
            public bool IsBinaryFormat { get; set; }
        }

        public class AvsEffect
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string ConfigData { get; set; } = string.Empty;
            public byte[] BinaryData { get; set; } = Array.Empty<byte>();
            public bool IsEnabled { get; set; } = true;
            public int Order { get; set; }
            public Dictionary<string, object> Parameters { get; set; } = new();
        }

        /// <summary>
        /// Parse an AVS file and extract superscopes and effects
        /// Supports both text and binary AVS formats
        /// </summary>
        public AvsFileInfo ParseAvsFile(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            var rawBytes = File.ReadAllBytes(filePath);

            var avsFile = new AvsFileInfo
            {
                FilePath = filePath,
                FileName = fileInfo.Name,
                RawBinaryData = rawBytes,
                LastModified = fileInfo.LastWriteTime,
                FileSize = fileInfo.Length
            };

            // Try to detect if this is a binary AVS file
            if (IsBinaryAvsFormat(rawBytes))
            {
                avsFile.IsBinaryFormat = true;
                ParseBinaryAvsFile(rawBytes, avsFile);
            }
            else
            {
                // Handle as text file
                var content = Encoding.Default.GetString(rawBytes);
                avsFile.RawContent = content;
                avsFile.IsBinaryFormat = false;
                avsFile.Superscopes = ExtractSuperscopes(content);
                avsFile.Effects = ExtractEffectsFromText(content);
            }

            return avsFile;
        }

        /// <summary>
        /// Check if the file is in binary AVS format
        /// </summary>
        private bool IsBinaryAvsFormat(byte[] data)
        {
            if (data.Length < 20) return false;

            // Check for AVS header
            var headerText = Encoding.ASCII.GetString(data.Take(20).ToArray());
            return headerText.Contains("Nullsoft AVS Preset") ||
                   headerText.Contains("AVS") ||
                   data.Length > 1000; // Binary files tend to be larger
        }

        /// <summary>
        /// Parse binary AVS file format using actual AVS binary specification
        /// </summary>
        private void ParseBinaryAvsFile(byte[] data, AvsFileInfo avsFile)
        {
            try
            {
                using var ms = new MemoryStream(data);
                using var reader = new BinaryReader(ms, Encoding.Default);

                // Read AVS header - first 16 bytes contain "Nullsoft AVS Preset"
                var headerBytes = reader.ReadBytes(16);
                var header = Encoding.ASCII.GetString(headerBytes);
                avsFile.PresetName = "AVS Preset";

                // Read version (2 bytes)
                var version = reader.ReadInt16();
                avsFile.PresetDescription = $"AVS Version: {version}";

                // Parse the effect list - this is where the actual effect data begins
                avsFile.Effects = ParseAvsEffectList(reader, data);

                // Extract any text sections that might contain descriptions or comments
                var textSections = ExtractTextSections(data);
                if (textSections.Any(t => t.Length > 20))
                {
                    avsFile.PresetDescription += "\n" + string.Join("\n", textSections.Where(t => t.Length > 20));
                }

                // Try to extract superscopes from any text sections
                foreach (var text in textSections)
                {
                    if (text.Contains("superscope") || ContainsMathFunctions(text))
                    {
                        var scopes = ExtractSuperscopes(text);
                        avsFile.Superscopes.AddRange(scopes);
                    }
                }
            }
            catch (Exception ex)
            {
                // If binary parsing fails, try as text fallback
                try
                {
                    var content = Encoding.Default.GetString(data);
                    avsFile.RawContent = content;
                    avsFile.IsBinaryFormat = false;
                    avsFile.Superscopes = ExtractSuperscopes(content);
                    avsFile.Effects = ExtractEffectsFromText(content);
                    avsFile.PresetDescription = "Parsed as text file (binary parsing failed)";
                }
                catch
                {
                    avsFile.RawContent = $"Error parsing AVS file: {ex.Message}";
                }
            }
        }

        /// <summary>
        /// Extract text sections from binary data
        /// </summary>
        private List<string> ExtractTextSections(byte[] data)
        {
            var textSections = new List<string>();
            var currentText = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] >= 32 && data[i] <= 126) // Printable ASCII
                {
                    currentText.Append((char)data[i]);
                }
                else if (currentText.Length > 0)
                {
                    var text = currentText.ToString().Trim();
                    if (text.Length > 3) // Only add meaningful text
                    {
                        textSections.Add(text);
                    }
                    currentText.Clear();
                }
            }

            // Add any remaining text
            if (currentText.Length > 3)
            {
                textSections.Add(currentText.ToString().Trim());
            }

            return textSections;
        }

        /// <summary>
        /// Parse AVS effect list from binary format
        /// </summary>
        private List<AvsEffect> ParseAvsEffectList(BinaryReader reader, byte[] data)
        {
            var effects = new List<AvsEffect>();

            try
            {
                // Skip to the effect data section (after header)
                reader.BaseStream.Position = 18; // Skip header (16) + version (2)

                // Read number of effects (4 bytes, little-endian)
                var effectCount = reader.ReadInt32();
                if (effectCount < 0 || effectCount > 1000) // Sanity check
                    effectCount = 0;

                for (int i = 0; i < effectCount; i++)
                {
                    var effect = ParseSingleAvsEffect(reader, data);
                    if (effect != null)
                    {
                        effect.Order = i;
                        effects.Add(effect);
                    }
                }
            }
            catch (Exception ex)
            {
                // If parsing fails, fall back to text-based detection
                Console.WriteLine($"Binary effect parsing failed: {ex.Message}");
                return ParseBinaryEffectsFallback(data);
            }

            return effects;
        }

        /// <summary>
        /// Parse a single AVS effect from binary data
        /// </summary>
        private AvsEffect? ParseSingleAvsEffect(BinaryReader reader, byte[] data)
        {
            try
            {
                // Read effect type ID (4 bytes)
                var effectType = reader.ReadInt32();

                // Read effect configuration size (4 bytes)
                var configSize = reader.ReadInt32();

                // Read configuration data
                var configData = reader.ReadBytes(configSize);

                // Map effect type to name
                var effectName = MapAvsEffectType(effectType);
                var effectTypeName = MapAvsEffectTypeToName(effectType);

                // Parse configuration parameters based on effect type
                var parameters = ParseEffectConfig(configData, effectType);

                return new AvsEffect
                {
                    Name = effectName,
                    Type = effectTypeName,
                    ConfigData = FormatConfigData(configData, effectType),
                    BinaryData = configData,
                    IsEnabled = true,
                    Parameters = parameters
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Map AVS effect type ID to human-readable name
        /// </summary>
        private string MapAvsEffectType(int effectType)
        {
            var effectNames = new Dictionary<int, string>
            {
                {0, "Simple Spectrum"},
                {1, "Oscilloscope"},
                {2, "Spectrum Analyzer"},
                {3, "Superscope"},
                {4, "Text Overlay"},
                {5, "Picture Display"},
                {6, "AVI Player"},
                {7, "Clear Screen"},
                {8, "Buffer Save"},
                {9, "Buffer Restore"},
                {10, "Movement"},
                {11, "Blur"},
                {12, "Color Modifier"},
                {13, "Convolution Filter"},
                {14, "Texer II"},
                {15, "Color Map"},
                {16, "Dynamic Movement"},
                {17, "Dynamic Distance Modifier"},
                {18, "Triangle"},
                {19, "Star"},
                {20, "Dot Grid"},
                {21, "Dot Plane"},
                {22, "MIDI Trace"}
            };

            return effectNames.TryGetValue(effectType, out var name) ? name : $"Effect_{effectType}";
        }

        /// <summary>
        /// Map AVS effect type to category name
        /// </summary>
        private string MapAvsEffectTypeToName(int effectType)
        {
            if (effectType >= 0 && effectType <= 7) return "Basic Render";
            if (effectType >= 8 && effectType <= 11) return "Buffer Effects";
            if (effectType >= 12 && effectType <= 17) return "Visual Effects";
            if (effectType >= 18 && effectType <= 22) return "Geometric Effects";
            return "Unknown";
        }

        /// <summary>
        /// Parse effect configuration data based on effect type
        /// </summary>
        private Dictionary<string, object> ParseEffectConfig(byte[] configData, int effectType)
        {
            var parameters = new Dictionary<string, object>();

            try
            {
                using var ms = new MemoryStream(configData);
                using var reader = new BinaryReader(ms);

                // Different effects have different parameter structures
                switch (effectType)
                {
                    case 0: // Simple Spectrum
                        if (configData.Length >= 8)
                        {
                            parameters["effect_mode"] = reader.ReadInt32();
                            parameters["num_colors"] = reader.ReadInt32();
                            // Read color array if present
                            var colors = new List<int>();
                            while (reader.BaseStream.Position < configData.Length)
                            {
                                if (reader.BaseStream.Position + 4 <= configData.Length)
                                    colors.Add(reader.ReadInt32());
                                else
                                    break;
                            }
                            parameters["colors"] = colors.ToArray();
                        }
                        break;

                    case 3: // Superscope
                        if (configData.Length >= 4)
                        {
                            var codeLength = reader.ReadInt32();
                            if (codeLength > 0 && codeLength < configData.Length - 4)
                            {
                                var codeBytes = reader.ReadBytes(codeLength);
                                var code = Encoding.Default.GetString(codeBytes);
                                parameters["code"] = code;
                            }
                        }
                        break;

                    case 13: // Convolution Filter
                        if (configData.Length >= 4)
                        {
                            parameters["intensity"] = reader.ReadInt32();
                        }
                        break;

                    default:
                        // Generic parameter parsing for unknown effects
                        var intParams = new List<int>();
                        for (int i = 0; i < configData.Length; i += 4)
                        {
                            if (i + 4 <= configData.Length)
                            {
                                intParams.Add(BitConverter.ToInt32(configData, i));
                            }
                        }
                        if (intParams.Count > 0)
                        {
                            parameters["parameters"] = intParams.ToArray();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                parameters["parse_error"] = ex.Message;
            }

            return parameters;
        }

        /// <summary>
        /// Format configuration data for display
        /// </summary>
        private string FormatConfigData(byte[] configData, int effectType)
        {
            if (configData.Length == 0) return "No configuration";

            var sb = new StringBuilder();
            sb.AppendLine($"Config Size: {configData.Length} bytes");

            // Format based on effect type
            switch (effectType)
            {
                case 0: // Simple Spectrum
                    if (configData.Length >= 8)
                    {
                        var effectMode = BitConverter.ToInt32(configData, 0);
                        var numColors = BitConverter.ToInt32(configData, 4);
                        sb.AppendLine($"Effect Mode: {effectMode}");
                        sb.AppendLine($"Number of Colors: {numColors}");

                        // Show colors if present
                        for (int i = 8; i < configData.Length; i += 4)
                        {
                            if (i + 4 <= configData.Length)
                            {
                                var color = BitConverter.ToInt32(configData, i);
                                sb.AppendLine($"Color {((i-8)/4) + 1}: #{color:X8}");
                            }
                        }
                    }
                    break;

                default:
                    // Show raw data for unknown effects
                    sb.AppendLine("Raw Config Data:");
                    for (int i = 0; i < Math.Min(configData.Length, 32); i += 4)
                    {
                        if (i + 4 <= configData.Length)
                        {
                            var value = BitConverter.ToInt32(configData, i);
                            sb.AppendLine($"  [{i/4}]: {value} (0x{value:X8})");
                        }
                    }
                    if (configData.Length > 32)
                    {
                        sb.AppendLine($"  ... and {configData.Length - 32} more bytes");
                    }
                    break;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Fallback binary effects parsing using text extraction
        /// </summary>
        private List<AvsEffect> ParseBinaryEffectsFallback(byte[] data)
        {
            var effects = new List<AvsEffect>();
            var textSections = ExtractTextSections(data);

            // Look for known effect names in the binary data
            var knownEffects = new[]
            {
                "Convolution Filter", "Texer II", "Color Map", "Dynamic Movement",
                "Buffer Save", "Movement", "Dynamic Distance Modifier",
                "Color Modifier", "Simple", "Superscope", "Text", "Picture",
                "AVI", "Clear Screen", "MIDI Trace", "Triangle", "Star",
                "Dot Grid", "Dot Plane", "Oscilloscope", "Spectrum"
            };

            foreach (var text in textSections)
            {
                foreach (var effectName in knownEffects)
                {
                    if (text.Contains(effectName, StringComparison.OrdinalIgnoreCase))
                    {
                        effects.Add(new AvsEffect
                        {
                            Name = effectName,
                            Type = effectName,
                            ConfigData = text,
                            BinaryData = data,
                            IsEnabled = true
                        });
                        break;
                    }
                }
            }

            // If no specific effects found, create a generic effect entry
            if (effects.Count == 0 && textSections.Count > 0)
            {
                effects.Add(new AvsEffect
                {
                    Name = "AVS Effects Chain",
                    Type = "Binary Effects",
                    ConfigData = string.Join("\n", textSections.Take(5)),
                    BinaryData = data,
                    IsEnabled = true
                });
            }

            return effects;
        }

        /// <summary>
        /// Extract effects from text-based AVS files
        /// </summary>
        private List<AvsEffect> ExtractEffectsFromText(string content)
        {
            var effects = new List<AvsEffect>();
            var lines = content.Split('\n');

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("//") || string.IsNullOrWhiteSpace(trimmed))
                    continue;

                // Look for effect definitions or configurations
                if (trimmed.Contains("=") || ContainsMathFunctions(trimmed))
                {
                    effects.Add(new AvsEffect
                    {
                        Name = $"Effect_{effects.Count + 1}",
                        Type = "Custom",
                        ConfigData = trimmed,
                        IsEnabled = true
                    });
                }
            }

            return effects;
        }

        /// <summary>
        /// Read null-terminated string from binary reader
        /// </summary>
        private string ReadNullTerminatedString(BinaryReader reader)
        {
            var bytes = new List<byte>();
            byte b;
            while ((b = reader.ReadByte()) != 0)
            {
                bytes.Add(b);
            }
            return Encoding.ASCII.GetString(bytes.ToArray());
        }

        /// <summary>
        /// Extract superscopes from AVS content using multiple regex patterns
        /// </summary>
        private List<AvsSuperscope> ExtractSuperscopes(string content)
        {
            var superscopes = new List<AvsSuperscope>();
            
            // Pattern 1: superscope("name", "code")
            var pattern1 = @"superscope\s*\(\s*""([^""]+)""\s*,\s*""([^""]+)""\s*\)";
            var matches1 = Regex.Matches(content, pattern1, RegexOptions.IgnoreCase);
            foreach (Match match in matches1)
            {
                superscopes.Add(new AvsSuperscope
                {
                    Name = match.Groups[1].Value.Trim(),
                    Code = match.Groups[2].Value.Trim()
                });
            }

            // Pattern 2: superscope(name, code) without quotes
            var pattern2 = @"superscope\s*\(\s*([^,]+)\s*,\s*([^)]+)\s*\)";
            var matches2 = Regex.Matches(content, pattern2, RegexOptions.IgnoreCase);
            foreach (Match match in matches2)
            {
                superscopes.Add(new AvsSuperscope
                {
                    Name = match.Groups[1].Value.Trim(),
                    Code = match.Groups[2].Value.Trim()
                });
            }

            // Pattern 3: name = "code" // superscope
            var pattern3 = @"(\w+)\s*=\s*""([^""]+)""\s*//\s*superscope";
            var matches3 = Regex.Matches(content, pattern3, RegexOptions.IgnoreCase);
            foreach (Match match in matches3)
            {
                superscopes.Add(new AvsSuperscope
                {
                    Name = match.Groups[1].Value.Trim(),
                    Code = match.Groups[2].Value.Trim()
                });
            }

            // Pattern 4: Look for code blocks that might be superscopes
            var additionalScopes = ExtractAdditionalSuperscopes(content);
            superscopes.AddRange(additionalScopes);

            // Remove duplicates and validate
            var uniqueScopes = superscopes
                .GroupBy(s => s.Name.ToLower())
                .Select(g => g.First())
                .ToList();

            foreach (var scope in uniqueScopes)
            {
                ValidateSuperscope(scope);
            }

            return uniqueScopes;
        }

        /// <summary>
        /// Extract additional superscope patterns that might be missed by standard regex
        /// </summary>
        private List<AvsSuperscope> ExtractAdditionalSuperscopes(string content)
        {
            var additionalScopes = new List<AvsSuperscope>();
            
            // Look for code blocks that contain mathematical expressions
            var lines = content.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("//") || string.IsNullOrWhiteSpace(line)) continue;
                
                // Check if line contains mathematical functions commonly used in superscopes
                if (ContainsMathFunctions(line))
                {
                    // Look for the next few lines to see if this is a code block
                    var codeBlock = new List<string>();
                    for (int j = i; j < Math.Min(i + 10, lines.Length); j++)
                    {
                        var nextLine = lines[j].Trim();
                        if (string.IsNullOrWhiteSpace(nextLine) || nextLine.StartsWith("//"))
                            break;
                        codeBlock.Add(nextLine);
                    }
                    
                    if (codeBlock.Count > 1)
                    {
                        var code = string.Join("\n", codeBlock);
                        additionalScopes.Add(new AvsSuperscope
                        {
                            Name = $"AutoDetected_{i}",
                            Code = code,
                            IsValid = false,
                            ErrorMessage = "Auto-detected - may need manual review"
                        });
                    }
                }
            }
            
            return additionalScopes;
        }

        /// <summary>
        /// Check if a line contains mathematical functions commonly used in superscopes
        /// </summary>
        private static bool ContainsMathFunctions(string line)
        {
            var mathFunctions = new[] { "sin", "cos", "tan", "sqrt", "pow", "abs", "log", "exp" };
            return mathFunctions.Any(func => line.Contains(func + "("));
        }

        /// <summary>
        /// Validate a superscope and set error messages if invalid
        /// </summary>
        private void ValidateSuperscope(AvsSuperscope scope)
        {
            if (string.IsNullOrWhiteSpace(scope.Code))
            {
                scope.IsValid = false;
                scope.ErrorMessage = "Empty code block";
                return;
            }

            if (scope.Code.Length < 10)
            {
                scope.IsValid = false;
                scope.ErrorMessage = "Code too short - likely not a valid superscope";
                return;
            }

            // Check for basic mathematical structure
            if (!ContainsMathFunctions(scope.Code))
            {
                scope.IsValid = false;
                scope.ErrorMessage = "No mathematical functions detected";
                return;
            }

            scope.IsValid = true;
        }

        /// <summary>
        /// Import an AVS file and create superscope visualizer plugins
        /// Now supports both binary and text AVS formats
        /// </summary>
        public bool ImportAvsFile(string filePath, out string? errorMessage)
        {
            try
            {
                var avsFile = ParseAvsFile(filePath);

                // Check if we have any importable content
                if (!avsFile.HasSuperscopes && !avsFile.HasEffects)
                {
                    errorMessage = "No superscopes or effects found in the AVS file";
                    return false;
                }

                // Create imported_avs directory
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var importDir = Path.Combine(baseDir, "imported_avs");
                Directory.CreateDirectory(importDir);

                // Generate C# files for superscopes
                foreach (var scope in avsFile.Superscopes)
                {
                    if (scope.IsValid)
                    {
                        var csharpCode = GenerateSuperscopeFile(scope, avsFile.FileName);
                        var fileName = SanitizeFileName(scope.Name) + ".cs";
                        var filePath2 = Path.Combine(importDir, fileName);
                        File.WriteAllText(filePath2, csharpCode);
                    }
                }

                // Generate C# files for binary effects
                if (avsFile.HasEffects && avsFile.IsBinaryFormat)
                {
                    var binaryEffectsCode = GenerateBinaryEffectsFile(avsFile);
                    var binaryFileName = SanitizeFileName(avsFile.FileName) + "_effects.cs";
                    var binaryFilePath = Path.Combine(importDir, binaryFileName);
                    File.WriteAllText(binaryFilePath, binaryEffectsCode);
                }

                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Import failed: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Generate C# code for binary AVS effects
        /// </summary>
        private string GenerateBinaryEffectsFile(AvsFileInfo avsFile)
        {
            var codeBuilder = new StringBuilder();

            codeBuilder.AppendLine($"// Imported from: {avsFile.FileName}");
            codeBuilder.AppendLine($"// Binary AVS Preset: {avsFile.PresetName}");
            codeBuilder.AppendLine($"// Import Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            codeBuilder.AppendLine($"// File Size: {avsFile.FileSize} bytes");
            codeBuilder.AppendLine($"// Binary Format: {avsFile.IsBinaryFormat}");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("using PhoenixVisualizer.Visuals;");
            codeBuilder.AppendLine("using PhoenixVisualizer.Audio;");
            codeBuilder.AppendLine("using SkiaSharp;");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("namespace PhoenixVisualizer.ImportedAvsEffects");
            codeBuilder.AppendLine("{");
            codeBuilder.AppendLine($"    public class {SanitizeClassName(avsFile.FileName)}_BinaryEffects : IVisualizerPlugin");
            codeBuilder.AppendLine("    {");
            codeBuilder.AppendLine($"        public string Id => \"binary_{SanitizeId(avsFile.FileName)}\";");
            codeBuilder.AppendLine($"        public string DisplayName => \"{avsFile.PresetName} (Binary Effects)\";");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        private int _width, _height;");
            codeBuilder.AppendLine("        private float _time;");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        public void Initialize(int width, int height)");
            codeBuilder.AppendLine("        {");
            codeBuilder.AppendLine("            _width = width;");
            codeBuilder.AppendLine("            _height = height;");
            codeBuilder.AppendLine("            _time = 0;");
            codeBuilder.AppendLine("        }");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)");
            codeBuilder.AppendLine("        {");
            codeBuilder.AppendLine("            canvas.Clear(0xFF000000);");
            codeBuilder.AppendLine("            _time += 0.02f;");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("            // Binary AVS Effects - Configuration Data:");
            foreach (var effect in avsFile.Effects.Take(10))
            {
                codeBuilder.AppendLine($"            // Effect: {effect.Name}");
                codeBuilder.AppendLine($"            // Type: {effect.Type}");
                if (!string.IsNullOrEmpty(effect.ConfigData))
                {
                    codeBuilder.AppendLine($"            // Config: {effect.ConfigData.Replace("\n", "\n            // ")}");
                }
            }
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("            // Fallback rendering - binary effects require native AVS runtime");
            codeBuilder.AppendLine("            var centerX = _width / 2f;");
            codeBuilder.AppendLine("            var centerY = _height / 2f;");
            codeBuilder.AppendLine("            var radius = 50 + features.Volume * 100;");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("            // Create a pulsing effect to indicate binary AVS content");
            codeBuilder.AppendLine("            var pulse = (float)Math.Sin(_time * 2) * 0.5f + 0.5f;");
            codeBuilder.AppendLine("            canvas.DrawCircle(centerX, centerY, radius * pulse, 0xFFFFAA00);");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("            // Display effect names as text");
            foreach (var effect in avsFile.Effects.Take(5))
            {
                codeBuilder.AppendLine($"            // Would render: {effect.Name}");
            }
            codeBuilder.AppendLine("        }");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        public void Resize(int width, int height)");
            codeBuilder.AppendLine("        {");
            codeBuilder.AppendLine("            _width = width;");
            codeBuilder.AppendLine("            _height = height;");
            codeBuilder.AppendLine("        }");
            codeBuilder.AppendLine();
            codeBuilder.AppendLine("        public void Dispose()");
            codeBuilder.AppendLine("        {");
            codeBuilder.AppendLine("            // Clean up resources if any");
            codeBuilder.AppendLine("        }");
            codeBuilder.AppendLine("    }");
            codeBuilder.AppendLine("}");

            return codeBuilder.ToString();
        }

        /// <summary>
        /// Generate a superscope file that can be loaded by the main application
        /// </summary>
        private string GenerateSuperscopeFile(AvsSuperscope scope, string originalFileName)
        {
            return $@"// Imported from: {originalFileName}
// Superscope: {scope.Name}
// Import Date: {scope.ImportDate:yyyy-MM-dd HH:mm:ss}

using PhoenixVisualizer.Visuals;
using PhoenixVisualizer.Audio;
using SkiaSharp;

namespace PhoenixVisualizer.ImportedSuperscopes
{{
    public class {SanitizeClassName(scope.Name)} : IVisualizerPlugin
    {{
        public string Id => ""imported_{SanitizeId(scope.Name)}"";
        public string DisplayName => ""{scope.Name} (Imported)"";
        
        private int _width, _height;
        private float _time;
        
        public void Initialize(int width, int height)
        {{
            _width = width;
            _height = height;
            _time = 0;
        }}
        
        public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
        {{
            canvas.Clear(0xFF000000);
            _time += 0.02f;
            
            // Original AVS code converted to C#:
            {ConvertAvsCodeToCSharp(scope.Code)}
        }}
        
        public void Resize(int width, int height)
        {{
            _width = width;
            _height = height;
        }}
        
        public void Dispose()
        {{
            // Clean up resources if any
        }}
    }}
}}";
        }

        /// <summary>
        /// Convert AVS code to C# code with basic transformations
        /// </summary>
        private static string ConvertAvsCodeToCSharp(string avsCode)
        {
            var csharpCode = avsCode;
            
            // Basic AVS to C# conversions
            csharpCode = Regex.Replace(csharpCode, @"\bsin\s*\(", "Math.Sin(", RegexOptions.IgnoreCase);
            csharpCode = Regex.Replace(csharpCode, @"\bcos\s*\(", "Math.Cos(", RegexOptions.IgnoreCase);
            csharpCode = Regex.Replace(csharpCode, @"\btan\s*\(", "Math.Tan(", RegexOptions.IgnoreCase);
            csharpCode = Regex.Replace(csharpCode, @"\bsqrt\s*\(", "Math.Sqrt(", RegexOptions.IgnoreCase);
            csharpCode = Regex.Replace(csharpCode, @"\bpow\s*\(", "Math.Pow(", RegexOptions.IgnoreCase);
            csharpCode = Regex.Replace(csharpCode, @"\babs\s*\(", "Math.Abs(", RegexOptions.IgnoreCase);
            csharpCode = Regex.Replace(csharpCode, @"\blog\s*\(", "Math.Log(", RegexOptions.IgnoreCase);
            csharpCode = Regex.Replace(csharpCode, @"\bexp\s*\(", "Math.Exp(", RegexOptions.IgnoreCase);
            
            // Replace AVS variables with C# equivalents
            csharpCode = Regex.Replace(csharpCode, @"\bt\b", "_time");
            csharpCode = Regex.Replace(csharpCode, @"\bw\b", "_width");
            csharpCode = Regex.Replace(csharpCode, @"\bh\b", "_height");
            
            // Add basic rendering logic if none exists
            if (!csharpCode.Contains("canvas.Draw"))
            {
                csharpCode += @"
            // Basic rendering fallback
            var centerX = _width / 2f;
            var centerY = _height / 2f;
            var radius = 50 + features.Volume * 100;
            
            canvas.DrawCircle(centerX, centerY, radius, 0xFF00FF00);";
            }
            
            return csharpCode;
        }

        /// <summary>
        /// Sanitize a filename for safe file creation
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return invalidChars.Aggregate(name, (current, c) => current.Replace(c, '_'));
        }

        /// <summary>
        /// Sanitize a class name for C# compilation
        /// </summary>
        private static string SanitizeClassName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "ImportedSuperscope";
            
            // Remove invalid characters and ensure it starts with a letter
            var sanitized = Regex.Replace(name, @"[^a-zA-Z0-9_]", "");
            if (sanitized.Length == 0 || char.IsDigit(sanitized[0]))
                sanitized = "S" + sanitized;
            
            return sanitized;
        }

        /// <summary>
        /// Sanitize an ID for safe use
        /// </summary>
        private static string SanitizeId(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "imported_scope";
            
            var sanitized = Regex.Replace(name.ToLower(), @"[^a-z0-9_]", "");
            if (sanitized.Length == 0)
                sanitized = "imported_scope";
            
            return sanitized;
        }

        /// <summary>
        /// Get all imported superscopes
        /// </summary>
        public static List<AvsSuperscope> GetImportedSuperscopes()
        {
            var superscopes = new List<AvsSuperscope>();
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var importDir = Path.Combine(baseDir, "imported_superscopes");
            
            if (!Directory.Exists(importDir)) return superscopes;
            
            foreach (var file in Directory.GetFiles(importDir, "*.cs"))
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var fileInfo = new FileInfo(file);
                    
                    superscopes.Add(new AvsSuperscope
                    {
                        Name = fileName,
                        Code = content,
                        FilePath = file,
                        ImportDate = fileInfo.LastWriteTime,
                        IsValid = true
                    });
                }
                catch (Exception ex)
                {
                    superscopes.Add(new AvsSuperscope
                    {
                        Name = Path.GetFileName(file),
                        Code = string.Empty,
                        FilePath = file,
                        ImportDate = DateTime.Now,
                        IsValid = false,
                        ErrorMessage = ex.Message
                    });
                }
            }
            
            return superscopes;
        }

        /// <summary>
        /// Delete an imported superscope
        /// </summary>
        public static bool DeleteImportedSuperscope(string name)
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var importDir = Path.Combine(baseDir, "imported_superscopes");
                var filePath = Path.Combine(importDir, name + ".cs");
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
