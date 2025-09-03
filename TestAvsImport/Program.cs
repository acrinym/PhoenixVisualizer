using System;
using System.IO;
using PhoenixVisualizer.Core.Transpile;

class Program
{
    static void Main()
    {
        // Test with the binary AVS file that's causing artifacts
        string filePath = @"presets\avs\Community Picks\duo - alien intercourse 4.avs";
        
        if (File.Exists(filePath))
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            var graph = WinampAvsImporter.Import(bytes);
            
            Console.WriteLine($"Imported {graph.Nodes.Count} nodes:");
            foreach (var node in graph.Nodes)
            {
                Console.WriteLine($"\nNode: {node.DisplayName}");
                Console.WriteLine($"Type: {node.TypeKey}");
                Console.WriteLine($"Samples: {node.Parameters["samples"]}");
                
                Console.WriteLine("\nINIT:");
                Console.WriteLine(node.Parameters["init"]);
                
                Console.WriteLine("\nFRAME:");
                Console.WriteLine(node.Parameters["frame"]);
                
                Console.WriteLine("\nCODE:");
                Console.WriteLine(node.Parameters["point"]);
                
                Console.WriteLine("\n" + new string('-', 50));
            }
        }
        else
        {
            Console.WriteLine($"File not found: {filePath}");
            Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
        }
    }
}
