using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PhoenixVisualizer.Models;

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
        }

        public class AvsFileInfo
        {
            public string FilePath { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
            public List<AvsSuperscope> Superscopes { get; set; } = new();
            public bool HasSuperscopes => Superscopes.Count > 0;
            public string RawContent { get; set; } = string.Empty;
        }

        /// <summary>
        /// Parse an AVS file and extract superscopes
        /// </summary>
        public AvsFileInfo ParseAvsFile(string filePath)
        {
            var result = new AvsFileInfo
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath)
            };

            try
            {
                if (!File.Exists(filePath))
                {
                    return result;
                }

                var content = File.ReadAllText(filePath);
                result.RawContent = content;

                // Extract superscopes using regex
                var superscopes = ExtractSuperscopes(content);
                result.Superscopes.AddRange(superscopes);

                // Also look for any other superscope-like patterns
                var additionalScopes = ExtractAdditionalSuperscopes(content);
                result.Superscopes.AddRange(additionalScopes);

                // Remove duplicates
                result.Superscopes = result.Superscopes
                    .GroupBy(s => s.Name)
                    .Select(g => g.First())
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing AVS file {filePath}: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Extract superscopes from AVS content using regex patterns
        /// </summary>
        private List<AvsSuperscope> ExtractSuperscopes(string content)
        {
            var superscopes = new List<AvsSuperscope>();
            
            try
            {
                // Pattern 1: Standard superscope format
                var pattern1 = @"superscope\s*\(\s*""([^""]+)""\s*,\s*""([^""]+)""\s*\)";
                var matches1 = Regex.Matches(content, pattern1, RegexOptions.IgnoreCase);
                
                foreach (Match match in matches1)
                {
                    if (match.Groups.Count >= 3)
                    {
                        var name = match.Groups[1].Value.Trim();
                        var code = match.Groups[2].Value.Trim();
                        
                        superscopes.Add(new AvsSuperscope
                        {
                            Name = name,
                            Code = code
                        });
                    }
                }

                // Pattern 2: Alternative superscope format
                var pattern2 = @"superscope\s*\(\s*([^,]+)\s*,\s*([^)]+)\s*\)";
                var matches2 = Regex.Matches(content, pattern2, RegexOptions.IgnoreCase);
                
                foreach (Match match in matches2)
                {
                    if (match.Groups.Count >= 3)
                    {
                        var name = match.Groups[1].Value.Trim().Trim('"', '\'');
                        var code = match.Groups[2].Value.Trim().Trim('"', '\'');
                        
                        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(code))
                        {
                            superscopes.Add(new AvsSuperscope
                            {
                                Name = name,
                                Code = code
                            });
                        }
                    }
                }

                // Pattern 3: Look for any code blocks that might be superscopes
                var pattern3 = @"(\w+)\s*=\s*""([^""]+)""\s*;?\s*//\s*superscope";
                var matches3 = Regex.Matches(content, pattern3, RegexOptions.IgnoreCase);
                
                foreach (Match match in matches3)
                {
                    if (match.Groups.Count >= 3)
                    {
                        var name = match.Groups[1].Value.Trim();
                        var code = match.Groups[2].Value.Trim();
                        
                        superscopes.Add(new AvsSuperscope
                        {
                            Name = name,
                            Code = code
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting superscopes: {ex.Message}");
            }

            return superscopes;
        }

        /// <summary>
        /// Extract additional superscope patterns that might be missed by standard regex
        /// </summary>
        private List<AvsSuperscope> ExtractAdditionalSuperscopes(string content)
        {
            var superscopes = new List<AvsSuperscope>();
            
            try
            {
                // Look for lines that contain mathematical expressions that could be superscopes
                var lines = content.Split('\n', '\r');
                var currentScope = "";
                var currentCode = new List<string>();
                var inScope = false;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    
                    // Check if this line starts a superscope
                    if (trimmedLine.StartsWith("//", StringComparison.OrdinalIgnoreCase) && 
                        trimmedLine.Contains("superscope", StringComparison.OrdinalIgnoreCase))
                    {
                        // End previous scope if we were in one
                        if (inScope && !string.IsNullOrWhiteSpace(currentScope))
                        {
                            superscopes.Add(new AvsSuperscope
                            {
                                Name = currentScope,
                                Code = string.Join("\n", currentCode)
                            });
                        }
                        
                        // Start new scope
                        currentScope = trimmedLine.Replace("//", "").Trim();
                        currentCode.Clear();
                        inScope = true;
                    }
                    // Check if this line contains mathematical expressions
                    else if (inScope && (trimmedLine.Contains("=") || 
                                       trimmedLine.Contains("+") || 
                                       trimmedLine.Contains("-") || 
                                       trimmedLine.Contains("*") || 
                                       trimmedLine.Contains("/") ||
                                       trimmedLine.Contains("sin") ||
                                       trimmedLine.Contains("cos") ||
                                       trimmedLine.Contains("tan")))
                    {
                        currentCode.Add(trimmedLine);
                    }
                    // Check if this line ends the scope
                    else if (inScope && (trimmedLine.StartsWith("//") || 
                                       trimmedLine.StartsWith("/*") ||
                                       string.IsNullOrWhiteSpace(trimmedLine)))
                    {
                        // End current scope
                        if (!string.IsNullOrWhiteSpace(currentScope) && currentCode.Count > 0)
                        {
                            superscopes.Add(new AvsSuperscope
                            {
                                Name = currentScope,
                                Code = string.Join("\n", currentCode)
                            });
                        }
                        
                        currentScope = "";
                        currentCode.Clear();
                        inScope = false;
                    }
                }

                // Don't forget the last scope
                if (inScope && !string.IsNullOrWhiteSpace(currentScope) && currentCode.Count > 0)
                {
                    superscopes.Add(new AvsSuperscope
                    {
                        Name = currentScope,
                        Code = string.Join("\n", currentCode)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting additional superscopes: {ex.Message}");
            }

            return superscopes;
        }

        /// <summary>
        /// Import an AVS file and create a superscope visualizer plugin
        /// </summary>
        public bool ImportAvsFile(string filePath, out string? errorMessage)
        {
            errorMessage = null;
            
            try
            {
                var avsInfo = ParseAvsFile(filePath);
                
                if (!avsInfo.HasSuperscopes)
                {
                    errorMessage = "No superscopes found in this AVS file";
                    return false;
                }

                // Create a directory for imported superscopes
                var importDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "imported_superscopes");
                Directory.CreateDirectory(importDir);

                // Save each superscope as a separate file
                foreach (var scope in avsInfo.Superscopes)
                {
                    var scopeFileName = $"{SanitizeFileName(scope.Name)}.superscope";
                    var scopeFilePath = Path.Combine(importDir, scopeFileName);
                    
                    var scopeContent = GenerateSuperscopeFile(scope, avsInfo.FileName);
                    File.WriteAllText(scopeFilePath, scopeContent);
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Import failed: {ex.Message}";
                return false;
            }
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
        /// Convert AVS code to C# code
        /// </summary>
        private string ConvertAvsCodeToCSharp(string avsCode)
        {
            // Basic conversion - this is a simplified version
            var converted = avsCode
                .Replace("sin(", "Math.Sin(")
                .Replace("cos(", "Math.Cos(")
                .Replace("tan(", "Math.Tan(")
                .Replace("abs(", "Math.Abs(")
                .Replace("pow(", "Math.Pow(")
                .Replace("sqrt(", "Math.Sqrt(")
                .Replace("log(", "Math.Log(")
                .Replace("exp(", "Math.Exp(");

            // Add basic superscope rendering
            return $@"
            // Converted AVS code:
            // {avsCode}
            
            // Basic superscope rendering (you may need to customize this)
            float centerX = _width / 2f;
            float centerY = _height / 2f;
            float length = 100 + features.Volume * 200;
            float angle = _time * 50;
            
            float x1 = centerX + (float)(length * Math.Cos(angle));
            float y1 = centerY + (float)(length * Math.Sin(angle));
            float x2 = centerX - (float)(length * Math.Cos(angle));
            float y2 = centerY - (float)(length * Math.Sin(angle));
            
            uint color = 0xFF00FF00;
            if (features.IsBeat)
            {{
                color = 0xFFFF0000;
            }}
            
            canvas.DrawLine(x1, y1, x2, y2, color, 2.0f);";
        }

        /// <summary>
        /// Sanitize a filename for safe file creation
        /// </summary>
        private string SanitizeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return invalidChars.Aggregate(name, (current, c) => current.Replace(c, '_'));
        }

        /// <summary>
        /// Sanitize a class name for C# compilation
        /// </summary>
        private string SanitizeClassName(string name)
        {
            var invalidChars = new[] { ' ', '-', '.', '(', ')', '[', ']', '{', '}', '<', '>', ':', ';', ',', '!', '@', '#', '$', '%', '^', '&', '*', '+', '=', '|', '\\', '/', '?', '"', '\'' };
            var result = invalidChars.Aggregate(name, (current, c) => current.Replace(c, '_'));
            
            // Ensure it starts with a letter
            if (result.Length > 0 && !char.IsLetter(result[0]))
            {
                result = "Scope_" + result;
            }
            
            return result;
        }

        /// <summary>
        /// Sanitize an ID for safe use
        /// </summary>
        private string SanitizeId(string name)
        {
            var invalidChars = new[] { ' ', '-', '.', '(', ')', '[', ']', '{', '}', '<', '>', ':', ';', ',', '!', '@', '#', '$', '%', '^', '&', '*', '+', '=', '|', '\\', '/', '?', '"', '\'' };
            var result = invalidChars.Aggregate(name.ToLower(), (current, c) => current.Replace(c, '_'));
            
            // Ensure it starts with a letter
            if (result.Length > 0 && !char.IsLetter(result[0]))
            {
                result = "scope_" + result;
            }
            
            return result;
        }
    }
}
