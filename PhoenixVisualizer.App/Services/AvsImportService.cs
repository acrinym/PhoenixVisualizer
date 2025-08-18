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
            public bool IsValid { get; set; } = true;
            public string ErrorMessage { get; set; } = string.Empty;
        }

        public class AvsFileInfo
        {
            public string FilePath { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
            public List<AvsSuperscope> Superscopes { get; set; } = new();
            public bool HasSuperscopes => Superscopes.Count > 0;
            public string RawContent { get; set; } = string.Empty;
            public DateTime LastModified { get; set; }
            public long FileSize { get; set; }
        }

        /// <summary>
        /// Parse an AVS file and extract superscopes
        /// </summary>
        public AvsFileInfo ParseAvsFile(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            var content = File.ReadAllText(filePath);
            
            var avsFile = new AvsFileInfo
            {
                FilePath = filePath,
                FileName = fileInfo.Name,
                RawContent = content,
                LastModified = fileInfo.LastWriteTime,
                FileSize = fileInfo.Length
            };

            avsFile.Superscopes = ExtractSuperscopes(content);
            return avsFile;
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
        private bool ContainsMathFunctions(string line)
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
        /// </summary>
        public bool ImportAvsFile(string filePath, out string? errorMessage)
        {
            try
            {
                var avsFile = ParseAvsFile(filePath);
                if (!avsFile.HasSuperscopes)
                {
                    errorMessage = "No superscopes found in the AVS file";
                    return false;
                }

                // Create imported_superscopes directory
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var importDir = Path.Combine(baseDir, "imported_superscopes");
                Directory.CreateDirectory(importDir);

                // Generate C# files for each superscope
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
        private string ConvertAvsCodeToCSharp(string avsCode)
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
        private string SanitizeId(string name)
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
        public List<AvsSuperscope> GetImportedSuperscopes()
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
        public bool DeleteImportedSuperscope(string name)
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
