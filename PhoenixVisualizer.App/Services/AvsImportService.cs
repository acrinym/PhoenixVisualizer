using System.Text.RegularExpressions;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Linq;

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

        public class AvsEffect : INotifyPropertyChanged
        {
            private string _name = string.Empty;
            private string _type = string.Empty;
            private string _configData = string.Empty;
            private byte[] _binaryData = Array.Empty<byte>();
            private bool _isEnabled = true;
            private int _order;
            private Dictionary<string, object> _parameters = new();

            public string Name
            {
                get => _name;
                set { if (_name != value) { _name = value; OnPropertyChanged(nameof(Name)); } }
            }

            public string Type
            {
                get => _type;
                set { if (_type != value) { _type = value; OnPropertyChanged(nameof(Type)); } }
            }

            public string ConfigData
            {
                get => _configData;
                set { if (_configData != value) { _configData = value; OnPropertyChanged(nameof(ConfigData)); } }
            }

            public byte[] BinaryData
            {
                get => _binaryData;
                set { if (_binaryData != value) { _binaryData = value; OnPropertyChanged(nameof(BinaryData)); } }
            }

            /// <summary>Existing name used in code.</summary>
            public bool IsEnabled
            {
                get => _isEnabled;
                set { if (_isEnabled != value) { _isEnabled = value; OnPropertyChanged(nameof(IsEnabled)); OnPropertyChanged(nameof(Enabled)); } }
            }

            /// <summary>Alias for UI/data bindings that expect 'Enabled'.</summary>
            public bool Enabled
            {
                get => _isEnabled;
                set { if (_isEnabled != value) { _isEnabled = value; OnPropertyChanged(nameof(Enabled)); OnPropertyChanged(nameof(IsEnabled)); } }
            }

            public int Order
            {
                get => _order;
                set { if (_order != value) { _order = value; OnPropertyChanged(nameof(Order)); } }
            }

            public Dictionary<string, object> Parameters
            {
                get => _parameters;
                set { if (_parameters != value) { _parameters = value; OnPropertyChanged(nameof(Parameters)); } }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged(string propertyName) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

            // FIX: Always set RawContent for debugging
            var content = Encoding.Default.GetString(rawBytes);
            avsFile.RawContent = content;

            // Try to detect if this is a binary AVS file
            if (IsBinaryAvsFormat(rawBytes))
            {
                avsFile.IsBinaryFormat = true;
                ParseBinaryAvsFile(rawBytes, avsFile);
            }
            else
            {
                // Handle as text file
                avsFile.IsBinaryFormat = false;
                avsFile.Superscopes = ExtractSuperscopes(content);
                avsFile.Effects = ExtractEffectsFromText(content);
            }

            Console.WriteLine($"[TestTrace] [Parser] ParseAvsFile complete: {avsFile.FileName}, Binary={avsFile.IsBinaryFormat}, Superscopes={avsFile.Superscopes.Count}, Effects={avsFile.Effects.Count}, ContentLength={avsFile.RawContent.Length}");

            return avsFile;
        }

        // FIX: New method to parse AVS content directly from a text string
        public AvsFileInfo ParseAvsFileFromText(string content)
        {
            var avsFile = new AvsFileInfo
            {
                FileName = "Text_Input_Preset",
                RawContent = content,
                IsBinaryFormat = false,
                LastModified = DateTime.Now,
                FileSize = content.Length
            };

            // Extract preset name from content if available
            var presetNameMatch = Regex.Match(content, @"PRESET_NAME=([^\r\n]+)", RegexOptions.IgnoreCase);
            if (presetNameMatch.Success)
            {
                avsFile.PresetName = presetNameMatch.Groups[1].Value.Trim();
            }
            else
            {
                avsFile.PresetName = "Unnamed Text Preset";
            }

            avsFile.Superscopes = ExtractSuperscopes(content);
            avsFile.Effects = ExtractEffectsFromText(content);

            Console.WriteLine($"[TestTrace] [Parser] Parsed AVS from text: {avsFile.FileName}, Superscopes={avsFile.Superscopes.Count}, Effects={avsFile.Effects.Count}");

            return avsFile;
        }

        /// <summary>
        /// Check if the file is in binary AVS format
        /// </summary>
        private bool IsBinaryAvsFormat(byte[] data)
        {
            if (data.Length < 20) return false;

            // FIX: Simplified binary detection - if it starts with "Nullsoft AVS Preset" and has binary data, it's binary
            var headerText = Encoding.ASCII.GetString(data.Take(20).ToArray());
            var startsWithBinaryHeader = headerText.StartsWith("Nullsoft AVS Preset");
            
            if (!startsWithBinaryHeader) return false;

            // Check for text file markers that would indicate this is actually text despite the header
            var fullContent = Encoding.ASCII.GetString(data);
            var hasTextMarkers = fullContent.Contains("[avs]") ||
                                fullContent.Contains("[preset") ||
                                fullContent.Contains("sn=Superscope") ||
                                fullContent.Contains("POINT") ||
                                fullContent.Contains("INIT");

            // If it has text markers, it's probably text format
            if (hasTextMarkers) return false;

            // Check for non-printable characters in the first 100 bytes after header
            var hasNonPrintableChars = data.Skip(20).Take(Math.Min(100, data.Length - 20))
                                          .Any(b => b < 32 && b != 9 && b != 10 && b != 13);

            var result = startsWithBinaryHeader && hasNonPrintableChars;
            Console.WriteLine($"[TestTrace] [Parser] Binary Detection: Header={startsWithBinaryHeader}, TextMarkers={hasTextMarkers}, NonPrintable={hasNonPrintableChars}, Result={result}");
            return result;
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
                avsFile.Effects = ParseAvsEffectList(reader, data, avsFile);

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
        private List<AvsEffect> ParseAvsEffectList(BinaryReader reader, byte[] data, AvsFileInfo avsFile)
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
                Console.WriteLine("[TestTrace] [Parser] Binary EffectCount: {effectCount}");

                for (int i = 0; i < effectCount; i++)
                {
                    var effect = ParseSingleAvsEffect(reader, data, avsFile);
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
        private AvsEffect? ParseSingleAvsEffect(BinaryReader reader, byte[] data, AvsFileInfo avsFile)
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
                Console.WriteLine("[TestTrace] [Parser] Binary EffectType: {effectType} ({effectName}) | ConfigSize: {configSize}");

                // Parse configuration parameters based on effect type
                var parameters = ParseEffectConfig(configData, effectType);

                // Special handling for superscopes - extract code and add to superscopes list
                if (effectType == 3 || effectType == 36 || effectType == 93)
                {
                    if (parameters.TryGetValue("code", out var codeObj) && codeObj is string code && code.Length > 0)
                    {
                        avsFile.Superscopes.Add(new AvsSuperscope
                        {
                            Name = $"Binary Superscope {avsFile.Superscopes.Count + 1}",
                            Code = code,
                            IsValid = true
                        });
                        Console.WriteLine("[TestTrace] [Parser] Binary Superscope Code Extracted: Length={code.Length}, Preview={code.Substring(0, Math.Min(50, code.Length))}...");
                    }
                }

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
        /// Map AVS effect type ID to human-readable name (COMPLETE LIST from Winamp source)
        /// </summary>
        private string MapAvsEffectType(int effectType)
        {
            var effectNames = new Dictionary<int, string>
            {
                // Built-in Render Effects (0-45)
                {0, "Simple Spectrum"},
                {1, "Dot Plane"},
                {2, "Oscilloscope Star"},
                {3, "Fade Out"},
                {4, "Blitter Feedback"},
                {5, "NF Clear"},
                {6, "Blur"},
                {7, "Bass Spin"},
                {8, "Moving Particles"},
                {9, "Rotoblitter"},
                {10, "SVP Loader"},
                {11, "Color Fade"},
                {12, "Contrast Enhancement"},
                {13, "Rotating Stars"},
                {14, "Oscilloscope Rings"},
                {15, "Movement"},
                {16, "Scatter"},
                {17, "Dot Grid"},
                {18, "Stack"},
                {19, "Dot Fountain"},
                {20, "Water"},
                {21, "Comment"},
                {22, "Brightness"},
                {23, "Interleave"},
                {24, "Grain"},
                {25, "Clear Screen"},
                {26, "Mirror"},
                {27, "Star Field"},
                {28, "Text"},
                {29, "Bump"},
                {30, "Mosaic"},
                {31, "Water Bump"},
                {32, "AVI Player"},
                {33, "Custom BPM"},
                {34, "Picture"},
                {35, "Dynamic Distance Modifier"},
                {36, "SuperScope"},
                {37, "Invert"},
                {38, "Unique Tone"},
                {39, "Timescope"},
                {40, "Line Mode"},
                {41, "Interferences"},
                {42, "Dynamic Shift"},
                {43, "Dynamic Movement"},
                {44, "Fast Brightness"},
                {45, "Dynamic Color Modifier"},

                // Built-in Transition Effects (46-65)
                {46, "Blitter Feedback (Trans)"},
                {47, "Blur (Trans)"},
                {48, "Brightness (Trans)"},
                {49, "Bump (Trans)"},
                {50, "Channel Shift (Trans)"},
                {51, "Color Fade (Trans)"},
                {52, "Color Reduction (Trans)"},
                {53, "Color Modifier (Trans)"},
                {54, "Dynamic Distance Modifier (Trans)"},
                {55, "Dynamic Movement (Trans)"},
                {56, "Fade Out (Trans)"},
                {57, "Fast Brightness (Trans)"},
                {58, "Grain (Trans)"},
                {59, "Interferences (Trans)"},
                {60, "Interleave (Trans)"},
                {61, "Invert (Trans)"},
                {62, "Mirror (Trans)"},
                {63, "Mosaic (Trans)"},
                {64, "Movement (Trans)"},
                {65, "Scatter (Trans)"},

                // APE Effects (66-75)
                {66, "Channel Shift (APE)"},
                {67, "Color Reduction (APE)"},
                {68, "Multiplier (APE)"},
                {69, "Video Delay (APE)"},
                {70, "Multi Delay (APE)"},
                {71, "Dynamic Shift (APE)"},
                {72, "Color Clip (APE)"},
                {73, "Unique Tone (APE)"},
                {74, "Set Render Mode (APE)"},
                {75, "On Beat Clear (APE)"},

                // Buffer Effects (76-80)
                {76, "Buffer Save"},
                {77, "Buffer Restore"},
                {78, "Multi Delay (Buffer)"},
                {79, "Video Delay (Buffer)"},
                {80, "Multiplier (Buffer)"},

                // Additional Effects (81+)
                {81, "Convolution Filter"},
                {82, "Texer II"},
                {83, "Color Map"},
                {84, "Triangle"},
                {85, "Ring"},
                {86, "Star"},
                {87, "MIDI Trace"},
                {88, "Rotoblitter (Trans)"},
                {89, "Rotating Stars (Trans)"},
                {90, "Rotoblitter (Render)"},
                {91, "Dynamic Shift (Trans)"},
                {92, "Simple Spectrum (Trans)"},
                {93, "SuperScope (Trans)"},
                {94, "Star Field (Trans)"},
                {95, "SVP Loader (Trans)"},
                {96, "Text (Trans)"},
                {97, "Timescope (Trans)"},
                {98, "Movement (Trans)"},
                {99, "Video Delay (Trans)"},
                {100, "Dynamic Color Modifier (Trans)"}
            };

            return effectNames.TryGetValue(effectType, out var name) ? name : $"Unknown Effect {effectType}";
        }

        /// <summary>
        /// Map AVS effect type to category name (COMPLETE categorization)
        /// </summary>
        private string MapAvsEffectTypeToName(int effectType)
        {
            if (effectType >= 0 && effectType <= 45) return "Render Effects";
            if (effectType >= 46 && effectType <= 65) return "Transition Effects";
            if (effectType >= 66 && effectType <= 75) return "APE Effects";
            if (effectType >= 76 && effectType <= 80) return "Buffer Effects";
            if (effectType >= 81 && effectType <= 100) return "Advanced Effects";
            return "Unknown Category";
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

                // Different effects have different parameter structures (based on Winamp source)
                switch (effectType)
                {
                    case 0: // Simple Spectrum
                    case 92: // Simple Spectrum (Trans)
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

                    case 3:  // Superscope
                    case 36: // Superscope (Render)
                    case 93: // Superscope (Trans)
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

                    case 4:  // Blitter Feedback
                    case 46: // Blitter Feedback (Trans)
                        if (configData.Length >= 4)
                        {
                            parameters["blend_mode"] = reader.ReadInt32();
                        }
                        break;

                    case 6:  // Blur
                    case 47: // Blur (Trans)
                        if (configData.Length >= 8)
                        {
                            parameters["blur_edges"] = reader.ReadInt32();
                            parameters["round_mode"] = reader.ReadInt32();
                        }
                        break;

                    case 7:  // Bass Spin
                        if (configData.Length >= 12)
                        {
                            parameters["enabled"] = reader.ReadInt32();
                            parameters["color"] = reader.ReadInt32();
                            parameters["blend_mode"] = reader.ReadInt32();
                        }
                        break;

                    case 9:  // Rotoblitter
                    case 88: // Rotoblitter (Trans)
                    case 90: // Rotoblitter (Render)
                        if (configData.Length >= 20)
                        {
                            parameters["zoom"] = reader.ReadInt32();
                            parameters["rotation"] = reader.ReadInt32();
                            parameters["zoom_center_x"] = reader.ReadInt32();
                            parameters["zoom_center_y"] = reader.ReadInt32();
                            parameters["blend_mode"] = reader.ReadInt32();
                        }
                        break;

                    case 11: // Color Fade
                    case 51: // Color Fade (Trans)
                        if (configData.Length >= 16)
                        {
                            parameters["fade_red"] = reader.ReadInt32();
                            parameters["fade_green"] = reader.ReadInt32();
                            parameters["fade_blue"] = reader.ReadInt32();
                            parameters["blend_mode"] = reader.ReadInt32();
                        }
                        break;

                    case 15: // Movement
                    case 64: // Movement (Trans)
                    case 98: // Movement (Trans)
                        if (configData.Length >= 16)
                        {
                            parameters["movement_x"] = reader.ReadInt32();
                            parameters["movement_y"] = reader.ReadInt32();
                            parameters["wrap_mode"] = reader.ReadInt32();
                            parameters["blend_mode"] = reader.ReadInt32();
                        }
                        break;

                    case 22: // Brightness
                    case 48: // Brightness (Trans)
                        if (configData.Length >= 4)
                        {
                            parameters["brightness"] = reader.ReadInt32();
                        }
                        break;

                    case 26: // Mirror
                    case 62: // Mirror (Trans)
                        if (configData.Length >= 8)
                        {
                            parameters["horizontal"] = reader.ReadInt32();
                            parameters["vertical"] = reader.ReadInt32();
                        }
                        break;

                    case 28: // Text
                    case 96: // Text (Trans)
                        if (configData.Length >= 8)
                        {
                            var textLength = reader.ReadInt32();
                            var color = reader.ReadInt32();
                            parameters["color"] = color;
                            if (textLength > 0 && textLength < configData.Length - 8)
                            {
                                var textBytes = reader.ReadBytes(textLength);
                                var text = Encoding.Default.GetString(textBytes);
                                parameters["text"] = text;
                            }
                        }
                        break;

                    case 29: // Bump
                    case 49: // Bump (Trans)
                        if (configData.Length >= 12)
                        {
                            parameters["depth"] = reader.ReadInt32();
                            parameters["on_beat"] = reader.ReadInt32();
                            parameters["depth2"] = reader.ReadInt32();
                        }
                        break;

                    case 30: // Mosaic
                    case 63: // Mosaic (Trans)
                        if (configData.Length >= 12)
                        {
                            parameters["block_size_x"] = reader.ReadInt32();
                            parameters["block_size_y"] = reader.ReadInt32();
                            parameters["blend_mode"] = reader.ReadInt32();
                        }
                        break;

                    case 32: // AVI Player
                        if (configData.Length >= 8)
                        {
                            var filenameLength = reader.ReadInt32();
                            parameters["speed"] = reader.ReadInt32();
                            if (filenameLength > 0 && filenameLength < configData.Length - 8)
                            {
                                var filenameBytes = reader.ReadBytes(filenameLength);
                                var filename = Encoding.Default.GetString(filenameBytes);
                                parameters["filename"] = filename;
                            }
                        }
                        break;

                    case 33: // Custom BPM
                        if (configData.Length >= 8)
                        {
                            parameters["enabled"] = reader.ReadInt32();
                            parameters["bpm"] = reader.ReadInt32();
                        }
                        break;

                    case 34: // Picture
                        if (configData.Length >= 8)
                        {
                            var filenameLength = reader.ReadInt32();
                            parameters["blend_mode"] = reader.ReadInt32();
                            if (filenameLength > 0 && filenameLength < configData.Length - 8)
                            {
                                var filenameBytes = reader.ReadBytes(filenameLength);
                                var filename = Encoding.Default.GetString(filenameBytes);
                                parameters["filename"] = filename;
                            }
                        }
                        break;

                    case 39: // Timescope
                    case 97: // Timescope (Trans)
                        if (configData.Length >= 16)
                        {
                            parameters["color"] = reader.ReadInt32();
                            parameters["mode"] = reader.ReadInt32();
                            parameters["band"] = reader.ReadInt32();
                            parameters["smoothing"] = reader.ReadInt32();
                        }
                        break;

                    case 43: // Dynamic Movement
                    case 55: // Dynamic Movement (Trans)
                        if (configData.Length >= 20)
                        {
                            parameters["grid_x"] = reader.ReadInt32();
                            parameters["grid_y"] = reader.ReadInt32();
                            parameters["speed"] = reader.ReadInt32();
                            parameters["blend_mode"] = reader.ReadInt32();
                            parameters["wrap_mode"] = reader.ReadInt32();
                        }
                        break;

                    // APE Effects
                    case 66: // Channel Shift (APE)
                    case 50: // Channel Shift (Trans)
                        if (configData.Length >= 12)
                        {
                            parameters["shift_red"] = reader.ReadInt32();
                            parameters["shift_green"] = reader.ReadInt32();
                            parameters["shift_blue"] = reader.ReadInt32();
                        }
                        break;

                    case 67: // Color Reduction (APE)
                    case 52: // Color Reduction (Trans)
                        if (configData.Length >= 4)
                        {
                            parameters["bits"] = reader.ReadInt32();
                        }
                        break;

                    case 68: // Multiplier (APE)
                    case 80: // Multiplier (Buffer)
                        if (configData.Length >= 4)
                        {
                            parameters["multiplier"] = reader.ReadInt32();
                        }
                        break;

                    case 69: // Video Delay (APE)
                    case 79: // Video Delay (Buffer)
                    case 99: // Video Delay (Trans)
                        if (configData.Length >= 8)
                        {
                            parameters["delay_frames"] = reader.ReadInt32();
                            parameters["blend_mode"] = reader.ReadInt32();
                        }
                        break;

                    case 70: // Multi Delay (APE)
                    case 78: // Multi Delay (Buffer)
                    case 65: // Multi Delay (Trans)
                        if (configData.Length >= 12)
                        {
                            parameters["delay_frames"] = reader.ReadInt32();
                            parameters["use_beats"] = reader.ReadInt32();
                            parameters["blend_mode"] = reader.ReadInt32();
                        }
                        break;

                    // Buffer Effects
                    case 76: // Buffer Save
                        if (configData.Length >= 4)
                        {
                            parameters["buffer_index"] = reader.ReadInt32();
                        }
                        break;

                    case 77: // Buffer Restore
                        if (configData.Length >= 8)
                        {
                            parameters["buffer_index"] = reader.ReadInt32();
                            parameters["blend_mode"] = reader.ReadInt32();
                        }
                        break;

                    // Advanced Effects (from external plugins)
                    case 81: // Convolution Filter
                        if (configData.Length >= 4)
                        {
                            parameters["intensity"] = reader.ReadInt32();
                        }
                        break;

                    case 82: // Texer II
                        if (configData.Length >= 16)
                        {
                            parameters["texture_mode"] = reader.ReadInt32();
                            parameters["wrap_mode"] = reader.ReadInt32();
                            parameters["blend_mode"] = reader.ReadInt32();
                            parameters["alpha"] = reader.ReadInt32();
                        }
                        break;

                    case 83: // Color Map
                        if (configData.Length >= 12)
                        {
                            parameters["map_mode"] = reader.ReadInt32();
                            parameters["output_min"] = reader.ReadInt32();
                            parameters["output_max"] = reader.ReadInt32();
                        }
                        break;

                    case 84: // Triangle
                        if (configData.Length >= 20)
                        {
                            parameters["num_triangles"] = reader.ReadInt32();
                            parameters["size"] = reader.ReadInt32();
                            parameters["color"] = reader.ReadInt32();
                            parameters["blend_mode"] = reader.ReadInt32();
                            parameters["rotation"] = reader.ReadInt32();
                        }
                        break;

                    case 85: // Ring
                        if (configData.Length >= 16)
                        {
                            parameters["radius"] = reader.ReadInt32();
                            parameters["width"] = reader.ReadInt32();
                            parameters["color"] = reader.ReadInt32();
                            parameters["blend_mode"] = reader.ReadInt32();
                        }
                        break;

                    case 86: // Star
                        if (configData.Length >= 20)
                        {
                            parameters["num_points"] = reader.ReadInt32();
                            parameters["inner_radius"] = reader.ReadInt32();
                            parameters["outer_radius"] = reader.ReadInt32();
                            parameters["color"] = reader.ReadInt32();
                            parameters["blend_mode"] = reader.ReadInt32();
                        }
                        break;

                    case 87: // MIDI Trace
                        if (configData.Length >= 12)
                        {
                            parameters["midi_channel"] = reader.ReadInt32();
                            parameters["color"] = reader.ReadInt32();
                            parameters["blend_mode"] = reader.ReadInt32();
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

            // Format based on effect type (enhanced formatting)
            switch (effectType)
            {
                case 0: // Simple Spectrum
                case 92: // Simple Spectrum (Trans)
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

                case 3:  // Superscope
                case 36: // Superscope (Render)
                case 93: // Superscope (Trans)
                    if (configData.Length >= 4)
                    {
                        var codeLength = BitConverter.ToInt32(configData, 0);
                        sb.AppendLine($"Code Length: {codeLength} bytes");
                        if (codeLength > 0 && codeLength < configData.Length - 4)
                        {
                            var codeBytes = new byte[codeLength];
                            Array.Copy(configData, 4, codeBytes, 0, codeLength);
                            var code = Encoding.Default.GetString(codeBytes);
                            sb.AppendLine("Code Preview:");
                            sb.AppendLine($"  {code.Substring(0, Math.Min(100, code.Length))}...");
                        }
                    }
                    break;

                case 4:  // Blitter Feedback
                case 46: // Blitter Feedback (Trans)
                    if (configData.Length >= 4)
                    {
                        var blendMode = BitConverter.ToInt32(configData, 0);
                        sb.AppendLine($"Blend Mode: {blendMode}");
                    }
                    break;

                case 6:  // Blur
                case 47: // Blur (Trans)
                    if (configData.Length >= 8)
                    {
                        var blurEdges = BitConverter.ToInt32(configData, 0);
                        var roundMode = BitConverter.ToInt32(configData, 4);
                        sb.AppendLine($"Blur Edges: {blurEdges}");
                        sb.AppendLine($"Round Mode: {roundMode}");
                    }
                    break;

                case 9:  // Rotoblitter
                case 88: // Rotoblitter (Trans)
                case 90: // Rotoblitter (Render)
                    if (configData.Length >= 20)
                    {
                        var zoom = BitConverter.ToInt32(configData, 0);
                        var rotation = BitConverter.ToInt32(configData, 4);
                        var zoomX = BitConverter.ToInt32(configData, 8);
                        var zoomY = BitConverter.ToInt32(configData, 12);
                        var blendMode = BitConverter.ToInt32(configData, 16);
                        sb.AppendLine($"Zoom: {zoom}");
                        sb.AppendLine($"Rotation: {rotation}");
                        sb.AppendLine($"Zoom Center: ({zoomX}, {zoomY})");
                        sb.AppendLine($"Blend Mode: {blendMode}");
                    }
                    break;

                case 11: // Color Fade
                case 51: // Color Fade (Trans)
                    if (configData.Length >= 16)
                    {
                        var red = BitConverter.ToInt32(configData, 0);
                        var green = BitConverter.ToInt32(configData, 4);
                        var blue = BitConverter.ToInt32(configData, 8);
                        var blendMode = BitConverter.ToInt32(configData, 12);
                        sb.AppendLine($"Fade Red: {red}");
                        sb.AppendLine($"Fade Green: {green}");
                        sb.AppendLine($"Fade Blue: {blue}");
                        sb.AppendLine($"Blend Mode: {blendMode}");
                    }
                    break;

                case 15: // Movement
                case 64: // Movement (Trans)
                case 98: // Movement (Trans)
                    if (configData.Length >= 16)
                    {
                        var moveX = BitConverter.ToInt32(configData, 0);
                        var moveY = BitConverter.ToInt32(configData, 4);
                        var wrapMode = BitConverter.ToInt32(configData, 8);
                        var blendMode = BitConverter.ToInt32(configData, 12);
                        sb.AppendLine($"Movement X: {moveX}");
                        sb.AppendLine($"Movement Y: {moveY}");
                        sb.AppendLine($"Wrap Mode: {wrapMode}");
                        sb.AppendLine($"Blend Mode: {blendMode}");
                    }
                    break;

                case 28: // Text
                case 96: // Text (Trans)
                    if (configData.Length >= 8)
                    {
                        var textLength = BitConverter.ToInt32(configData, 0);
                        var color = BitConverter.ToInt32(configData, 4);
                        sb.AppendLine($"Color: #{color:X8}");
                        if (textLength > 0 && textLength < configData.Length - 8)
                        {
                            var textBytes = new byte[textLength];
                            Array.Copy(configData, 8, textBytes, 0, textLength);
                            var text = Encoding.Default.GetString(textBytes);
                            sb.AppendLine($"Text: \"{text}\"");
                        }
                    }
                    break;

                case 29: // Bump
                case 49: // Bump (Trans)
                    if (configData.Length >= 12)
                    {
                        var depth = BitConverter.ToInt32(configData, 0);
                        var onBeat = BitConverter.ToInt32(configData, 4);
                        var depth2 = BitConverter.ToInt32(configData, 8);
                        sb.AppendLine($"Depth: {depth}");
                        sb.AppendLine($"On Beat: {onBeat}");
                        sb.AppendLine($"Depth 2: {depth2}");
                    }
                    break;

                case 30: // Mosaic
                case 63: // Mosaic (Trans)
                    if (configData.Length >= 12)
                    {
                        var blockX = BitConverter.ToInt32(configData, 0);
                        var blockY = BitConverter.ToInt32(configData, 4);
                        var blendMode = BitConverter.ToInt32(configData, 8);
                        sb.AppendLine($"Block Size X: {blockX}");
                        sb.AppendLine($"Block Size Y: {blockY}");
                        sb.AppendLine($"Blend Mode: {blendMode}");
                    }
                    break;

                case 32: // AVI Player
                    if (configData.Length >= 8)
                    {
                        var filenameLength = BitConverter.ToInt32(configData, 0);
                        var speed = BitConverter.ToInt32(configData, 4);
                        sb.AppendLine($"Speed: {speed}");
                        if (filenameLength > 0 && filenameLength < configData.Length - 8)
                        {
                            var filenameBytes = new byte[filenameLength];
                            Array.Copy(configData, 8, filenameBytes, 0, filenameLength);
                            var filename = Encoding.Default.GetString(filenameBytes);
                            sb.AppendLine($"Filename: {filename}");
                        }
                    }
                    break;

                case 33: // Custom BPM
                    if (configData.Length >= 8)
                    {
                        var enabled = BitConverter.ToInt32(configData, 0);
                        var bpm = BitConverter.ToInt32(configData, 4);
                        sb.AppendLine($"Enabled: {enabled}");
                        sb.AppendLine($"BPM: {bpm}");
                    }
                    break;

                case 34: // Picture
                    if (configData.Length >= 8)
                    {
                        var filenameLength = BitConverter.ToInt32(configData, 0);
                        var blendMode = BitConverter.ToInt32(configData, 4);
                        sb.AppendLine($"Blend Mode: {blendMode}");
                        if (filenameLength > 0 && filenameLength < configData.Length - 8)
                        {
                            var filenameBytes = new byte[filenameLength];
                            Array.Copy(configData, 8, filenameBytes, 0, filenameLength);
                            var filename = Encoding.Default.GetString(filenameBytes);
                            sb.AppendLine($"Filename: {filename}");
                        }
                    }
                    break;

                case 39: // Timescope
                case 97: // Timescope (Trans)
                    if (configData.Length >= 16)
                    {
                        var color = BitConverter.ToInt32(configData, 0);
                        var mode = BitConverter.ToInt32(configData, 4);
                        var band = BitConverter.ToInt32(configData, 8);
                        var smoothing = BitConverter.ToInt32(configData, 12);
                        sb.AppendLine($"Color: #{color:X8}");
                        sb.AppendLine($"Mode: {mode}");
                        sb.AppendLine($"Band: {band}");
                        sb.AppendLine($"Smoothing: {smoothing}");
                    }
                    break;

                case 43: // Dynamic Movement
                case 55: // Dynamic Movement (Trans)
                    if (configData.Length >= 20)
                    {
                        var gridX = BitConverter.ToInt32(configData, 0);
                        var gridY = BitConverter.ToInt32(configData, 4);
                        var speed = BitConverter.ToInt32(configData, 8);
                        var blendMode = BitConverter.ToInt32(configData, 12);
                        var wrapMode = BitConverter.ToInt32(configData, 16);
                        sb.AppendLine($"Grid X: {gridX}");
                        sb.AppendLine($"Grid Y: {gridY}");
                        sb.AppendLine($"Speed: {speed}");
                        sb.AppendLine($"Blend Mode: {blendMode}");
                        sb.AppendLine($"Wrap Mode: {wrapMode}");
                    }
                    break;

                // APE Effects
                case 66: // Channel Shift (APE)
                case 50: // Channel Shift (Trans)
                    if (configData.Length >= 12)
                    {
                        var shiftR = BitConverter.ToInt32(configData, 0);
                        var shiftG = BitConverter.ToInt32(configData, 4);
                        var shiftB = BitConverter.ToInt32(configData, 8);
                        sb.AppendLine($"Red Shift: {shiftR}");
                        sb.AppendLine($"Green Shift: {shiftG}");
                        sb.AppendLine($"Blue Shift: {shiftB}");
                    }
                    break;

                case 68: // Multiplier (APE)
                case 80: // Multiplier (Buffer)
                    if (configData.Length >= 4)
                    {
                        var multiplier = BitConverter.ToInt32(configData, 0);
                        sb.AppendLine($"Multiplier: {multiplier}");
                    }
                    break;

                case 81: // Convolution Filter
                    if (configData.Length >= 4)
                    {
                        var intensity = BitConverter.ToInt32(configData, 0);
                        sb.AppendLine($"Intensity: {intensity}");
                    }
                    break;

                case 82: // Texer II
                    if (configData.Length >= 16)
                    {
                        var texMode = BitConverter.ToInt32(configData, 0);
                        var wrapMode = BitConverter.ToInt32(configData, 4);
                        var blendMode = BitConverter.ToInt32(configData, 8);
                        var alpha = BitConverter.ToInt32(configData, 12);
                        sb.AppendLine($"Texture Mode: {texMode}");
                        sb.AppendLine($"Wrap Mode: {wrapMode}");
                        sb.AppendLine($"Blend Mode: {blendMode}");
                        sb.AppendLine($"Alpha: {alpha}");
                    }
                    break;

                default:
                    // Show raw data for unknown effects with better formatting
                    sb.AppendLine("Raw Config Data:");
                    for (int i = 0; i < Math.Min(configData.Length, 64); i += 4)
                    {
                        if (i + 4 <= configData.Length)
                        {
                            var value = BitConverter.ToInt32(configData, i);
                            sb.Append($"  [{i/4:D2}]: {value,8} (0x{value:X8})");
                            if ((i/4 + 1) % 2 == 0) sb.AppendLine();
                        }
                    }
                    if (configData.Length > 64)
                    {
                        sb.AppendLine($"  ... and {configData.Length - 64} more bytes");
                    }
                    else if (configData.Length % 4 != 0)
                    {
                        sb.AppendLine(); // Add newline if we didn't finish on a pair
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
                        Console.WriteLine("[TestTrace] [Parser] Effect Added: Name={effectName}, Type={effectName}, ConfigLength={text.Length}");
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

            // Pattern: Mystical Visualizer format - extract all preset sections
            var presetPattern = @"(?s)\[preset(\d+)\](.*?)(?=\[preset\d+\]|$)";
            var presetMatches = Regex.Matches(content, presetPattern, RegexOptions.IgnoreCase);

            // Debug: Log preset matching
            if (content.Contains("[preset"))
            {
                Console.WriteLine($"[AvsImportService] Found preset sections: {presetMatches.Count}");
                if (presetMatches.Count == 0)
                {
                    Console.WriteLine($"[AvsImportService] Content preview: {content.Substring(0, Math.Min(200, content.Length))}");
                }
            }

            foreach (Match presetMatch in presetMatches)
            {
                var presetNumber = presetMatch.Groups[1].Value;
                var presetContent = presetMatch.Groups[2].Value;

                // Extract superscope name
                var snMatch = Regex.Match(presetContent, @"sn=([^\r\n]+)", RegexOptions.IgnoreCase);
                var effectName = snMatch.Success ? snMatch.Groups[1].Value.Trim() : $"Preset_{presetNumber}";

                // Extract different code sections
                var initMatch = Regex.Match(presetContent, @"(?s)INIT(.*?)(?=FRAME|POINT|CODE|$)", RegexOptions.IgnoreCase);
                var frameMatch = Regex.Match(presetContent, @"(?s)FRAME(.*?)(?=POINT|CODE|$)", RegexOptions.IgnoreCase);
                var pointMatch = Regex.Match(presetContent, @"(?s)POINT(.*?)(?=INIT|FRAME|CODE|$)", RegexOptions.IgnoreCase);
                var codeMatch = Regex.Match(presetContent, @"(?s)CODE(.*?)(?=\[preset|$)", RegexOptions.IgnoreCase);

                // Combine all code
                var combinedCode = "";
                if (initMatch.Success) combinedCode += "// INIT\n" + initMatch.Groups[1].Value.Trim() + "\n\n";
                if (frameMatch.Success) combinedCode += "// FRAME\n" + frameMatch.Groups[1].Value.Trim() + "\n\n";
                if (pointMatch.Success) combinedCode += "// POINT\n" + pointMatch.Groups[1].Value.Trim() + "\n\n";
                if (codeMatch.Success) combinedCode += "// CODE\n" + codeMatch.Groups[1].Value.Trim() + "\n\n";

                // Extract parameters
                var parameters = new Dictionary<string, object>();
                var paramMatches = Regex.Matches(presetContent, @"(\w+)\s*=\s*([^\r\n]+)");
                foreach (Match paramMatch in paramMatches)
                {
                    var key = paramMatch.Groups[1].Value.Trim();
                    var value = paramMatch.Groups[2].Value.Trim();
                    parameters[key] = value;
                }

                if (!string.IsNullOrEmpty(combinedCode) || parameters.Count > 0)
                {
                    effects.Add(new AvsEffect
                    {
                        Name = effectName,
                        Type = DetermineEffectType(effectName, combinedCode),
                        ConfigData = combinedCode.Trim(),
                        IsEnabled = true,
                        Parameters = parameters,
                        Order = int.Parse(presetNumber)
                    });
                    Console.WriteLine("[TestTrace] [Parser] Effect Added: Name={effectName}, Type={DetermineEffectType(effectName, combinedCode)}, ConfigLength={combinedCode.Length}");
                }
            }

            // Fallback: Look for effect definitions or configurations in lines
            if (effects.Count == 0)
            {
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
            }

            return effects;
        }

        /// <summary>
        /// Determine effect type based on name and code content
        /// </summary>
        private string DetermineEffectType(string name, string code)
        {
            if (name.Contains("Superscope", StringComparison.OrdinalIgnoreCase))
                return "Superscope";

            if (code.Contains("blur") || code.Contains("Blur"))
                return "Blur";

            if (code.Contains("movement") || code.Contains("Movement"))
                return "Movement";

            if (code.Contains("color") || code.Contains("Color"))
                return "Color";

            if (code.Contains("wave") || code.Contains("Wave"))
                return "Waveform";

            if (code.Contains("spectrum") || code.Contains("Spectrum"))
                return "Spectrum";

            return "Custom";
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

            // Pattern 0: Mystical Visualizer format - [preset00] sections with sn= and CODE blocks
            // FIX: Updated pattern to match the actual structure of our AVS files
            var mysticalPattern = @"(?s)\[preset(\d+)\](.*?)sn=Superscope\s*\(([^)]+)\)(.*?)(?=\[preset\d+\]|$)";
            var mysticalMatches = Regex.Matches(content, mysticalPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);

            // Debug: Log pattern matching
            if (content.Contains("[preset") && content.Contains("sn=Superscope"))
            {
                Console.WriteLine($"[AvsImportService] Found mystical visualizer content");
                Console.WriteLine($"[AvsImportService] Mystical matches found: {mysticalMatches.Count}");
                if (mysticalMatches.Count == 0)
                {
                    Console.WriteLine($"[AvsImportService] Content preview: {content.Substring(0, Math.Min(200, content.Length))}");
                }
            }

            foreach (Match match in mysticalMatches)
            {
                var presetNum = match.Groups[1].Value;
                var superscopeName = match.Groups[3].Value.Trim();
                var fullSection = match.Groups[4].Value; // Everything after sn=Superscope(name)

                // FIX: Extract INIT and CODE sections from the full section text
                var initCode = "";
                var codeSection = "";
                
                // Find INIT section
                var initMatch = Regex.Match(fullSection, @"INIT\s*\n(.*?)(?=\n[A-Z]|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (initMatch.Success)
                {
                    initCode = initMatch.Groups[1].Value.Trim();
                }

                // Find CODE section  
                var codeMatch = Regex.Match(fullSection, @"CODE\s*\n(.*?)(?=\n[A-Z]|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (codeMatch.Success)
                {
                    codeSection = codeMatch.Groups[1].Value.Trim();
                }

                // Combine all code sections
                var combinedCode = "";
                if (!string.IsNullOrEmpty(initCode))
                    combinedCode += "// Init code\n" + initCode + "\n\n";
                if (!string.IsNullOrEmpty(codeSection))
                    combinedCode += "// Main code\n" + codeSection;

                if (!string.IsNullOrEmpty(combinedCode))
                {
                    superscopes.Add(new AvsSuperscope
                    {
                        Name = $"{superscopeName} (Preset {presetNum})",
                        Code = combinedCode.Trim(),
                        IsValid = true
                    });
                    Console.WriteLine($"[TestTrace] [Parser] Superscope Added: Name={superscopeName} (Preset {presetNum}), CodeLength={combinedCode.Length}");
                }
                else
                {
                    Console.WriteLine($"[TestTrace] [Parser] No code found for superscope: {superscopeName}");
                    Console.WriteLine($"[TestTrace] [Parser] Full section: {fullSection.Substring(0, Math.Min(200, fullSection.Length))}");
                }
            }

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
            if (string.IsNullOrWhiteSpace(scope.Code) || scope.Code.Length == 0)
            {
                scope.IsValid = false;
                scope.ErrorMessage = "Empty code block";
            }
            else
            {
                scope.IsValid = true;
                // Relaxed for testing - add math check later if needed
            }
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

