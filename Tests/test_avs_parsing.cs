using System;
using System.IO;
using PhoenixVisualizer.App.Services;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: test_avs_parsing.exe <path_to_avs_file>");
            return;
        }

        var filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"File not found: {filePath}");
            return;
        }

        Console.WriteLine($"Testing AVS parsing for: {filePath}");
        Console.WriteLine(new string('=', 50));

        try
        {
            var avsImportService = new AvsImportService();
            var avsFile = avsImportService.ParseAvsFile(filePath);

            Console.WriteLine($"File: {avsFile.FileName}");
            Console.WriteLine($"Size: {avsFile.FileSize} bytes");
            Console.WriteLine($"Binary Format: {avsFile.IsBinaryFormat}");
            Console.WriteLine($"Content Length: {avsFile.RawContent?.Length ?? 0}");
            Console.WriteLine($"Superscopes Found: {avsFile.Superscopes.Count}");
            Console.WriteLine($"Effects Found: {avsFile.Effects.Count}");
            Console.WriteLine();

            if (avsFile.Superscopes.Count > 0)
            {
                Console.WriteLine("SUPERSCOPES:");
                foreach (var scope in avsFile.Superscopes)
                {
                    Console.WriteLine($"  - {scope.Name} (Valid: {scope.IsValid})");
                    if (!string.IsNullOrEmpty(scope.Code))
                    {
                        Console.WriteLine($"    Code: {scope.Code.Substring(0, Math.Min(100, scope.Code.Length))}...");
                    }
                }
                Console.WriteLine();
            }

            if (avsFile.Effects.Count > 0)
            {
                Console.WriteLine("EFFECTS:");
                foreach (var effect in avsFile.Effects)
                {
                    Console.WriteLine($"  - {effect.Name} (Type: {effect.Type})");
                }
                Console.WriteLine();
            }

            if (!string.IsNullOrEmpty(avsFile.RawContent))
            {
                Console.WriteLine("CONTENT PREVIEW:");
                var preview = avsFile.RawContent.Length > 500 
                    ? avsFile.RawContent.Substring(0, 500) + "..." 
                    : avsFile.RawContent;
                Console.WriteLine(preview);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
        }
    }
}
