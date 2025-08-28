using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace EffectNodeFixer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== C# EFFECT NODE FIXER ===");
            Console.WriteLine("NO REGEX - EXACT LINE MATCHING ONLY!");
            
            // Batch 3 files that need fixing (based on build errors)
            var batchFiles = new[]
            {
                "VectorFieldEffectsNode.cs",
                "TexturedParticleSystemEffectsNode.cs",
                "StarfieldEffectsNode.cs"
            };
            
            Console.WriteLine($"\nProcessing Batch 3: {batchFiles.Length} files");
            Console.WriteLine(new string('=', 50));
            
            int fixedCount = 0;
            for (int i = 0; i < batchFiles.Length; i++)
            {
                var fileName = batchFiles[i];
                Console.WriteLine($"\n[{i + 1}/{batchFiles.Length}] Processing: {fileName}");
                
                if (File.Exists(fileName))
                {
                    bool success = FixEffectFile(fileName);
                    if (success)
                    {
                        Console.WriteLine($"  ✅ SUCCESS: Fixed {fileName}");
                        fixedCount++;
                    }
                    else
                    {
                        Console.WriteLine($"  ⚠ No changes needed for {fileName}");
                    }
                }
                else
                {
                    Console.WriteLine($"  ❌ File not found: {fileName}");
                }
            }
            
            Console.WriteLine("\n" + new string('=', 50));
            Console.WriteLine($"✅ BATCH 3 COMPLETE: Fixed {fixedCount}/{batchFiles.Length} files");
            Console.WriteLine("Ready to test build and continue with next batch!");
        }
        
        static bool FixEffectFile(string filePath)
        {
            Console.WriteLine($"Fixing: {filePath}");
            
            // Read the file line by line
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            var newLines = new List<string>();
            bool changesMade = false;
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                
                // Check if this is the ProcessFrame method signature - ONLY change the signature
                if (line.Trim() == "public override void ProcessFrame(Dictionary<string, object> inputData, Dictionary<string, object> outputData, AudioFeatures audioFeatures)")
                {
                    Console.WriteLine("  ✓ Found ProcessFrame method - changing signature to ProcessCore");
                    changesMade = true;
                    
                    // Replace ONLY the method signature, keep everything else
                    newLines.Add("        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)");
                    continue;
                }
                
                // Check if this is the GetConfiguration method - remove the entire method
                if (line.Trim() == "public override Dictionary<string, object> GetConfiguration()")
                {
                    Console.WriteLine("  ✓ Found GetConfiguration method - removing");
                    changesMade = true;
                    
                    // Skip the entire GetConfiguration method by finding its exact end using brace counting
                    int braceDepth = 0;
                    bool foundMethodStart = false;
                    while (i < lines.Length)
                    {
                        string currentLine = lines[i];
                        string trimmedLine = currentLine.Trim();
                        
                        if (trimmedLine == "{")
                        {
                            if (!foundMethodStart)
                            {
                                foundMethodStart = true;
                                braceDepth = 1;
                            }
                            else
                            {
                                braceDepth++;
                            }
                        }
                        else if (trimmedLine == "}")
                        {
                            braceDepth--;
                            if (foundMethodStart && braceDepth == 0)
                            {
                                // Found the end of the GetConfiguration method
                                break;
                            }
                        }
                        
                        i++;
                    }
                    continue;
                }
                
                // Check if this is the ApplyConfiguration method - remove the entire method
                if (line.Trim() == "public override void ApplyConfiguration(Dictionary<string, object> config)")
                {
                    Console.WriteLine("  ✓ Found ApplyConfiguration method - removing");
                    changesMade = true;
                    
                    // Skip the entire ApplyConfiguration method by finding its exact end using brace counting
                    int braceDepth = 0;
                    bool foundMethodStart = false;
                    while (i < lines.Length)
                    {
                        string currentLine = lines[i];
                        string trimmedLine = currentLine.Trim();
                        
                        if (trimmedLine == "{")
                        {
                            if (!foundMethodStart)
                            {
                                foundMethodStart = true;
                                braceDepth = 1;
                            }
                            else
                            {
                                braceDepth++;
                            }
                        }
                        else if (trimmedLine == "}")
                        {
                            braceDepth--;
                            if (foundMethodStart && braceDepth == 0)
                            {
                                // Found the end of the ApplyConfiguration method
                                break;
                            }
                        }
                        
                        i++;
                    }
                    continue;
                }
                
                // Check if we need to add InitializePorts method after the class declaration
                if (line.Trim().StartsWith("public class") && line.Trim().EndsWith(": BaseEffectNode"))
                {
                    Console.WriteLine("  ✓ Found class declaration - will add InitializePorts method");
                    // Don't add it yet, just mark that we found the class
                }
                
                // Check if we need to add InitializePorts method after the first property/field
                if (line.Trim().StartsWith("public ") && line.Contains(" { get; set; }") && !changesMade)
                {
                    // Add InitializePorts method right after the first property
                    newLines.Add(line);
                    newLines.Add("");
                    newLines.Add("        protected override void InitializePorts()");
                    newLines.Add("        {");
                    newLines.Add("            // TODO: Initialize input/output ports");
                    newLines.Add("        }");
                    newLines.Add("");
                    changesMade = true;
                    continue;
                }
                
                // Check if we need to modify the ProcessFrame method body to return the output
                if (line.Trim() == "outputData[\"Output\"] = outputImage;")
                {
                    Console.WriteLine("  ✓ Found ProcessFrame output assignment - changing to return");
                    newLines.Add("                return outputImage;");
                    continue;
                }
                
                // Add the current line if we haven't skipped it
                newLines.Add(line);
            }
            
            // Check if content changed
            if (changesMade)
            {
                // Backup original file
                string backupPath = filePath + ".backup";
                File.Copy(filePath, backupPath, true);
                Console.WriteLine($"  ✓ Created backup: {backupPath}");
                
                // Write fixed content
                File.WriteAllLines(filePath, newLines, Encoding.UTF8);
                Console.WriteLine($"  ✓ Fixed file: {filePath}");
                
                return true;
            }
            else
            {
                Console.WriteLine("  ⚠ No changes made");
                return false;
            }
        }
    }
}
