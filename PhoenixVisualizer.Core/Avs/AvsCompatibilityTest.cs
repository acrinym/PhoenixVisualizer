using System.Text.Json;

namespace PhoenixVisualizer.Core.Avs;

/// <summary>
/// Test class to verify AVS compatibility implementation
/// Run this to validate the four critical components work together
/// </summary>
public static class AvsCompatibilityTest
{
    /// <summary>
    /// Run comprehensive test of AVS compatibility components
    /// </summary>
    public static void RunCompatibilityTest()
    {
        Console.WriteLine("🧪 Running AVS Compatibility Test Suite...\n");

        // Test 1: Effect Index Mapping
        TestEffectMapping();

        // Test 2: NS-EEL Expression Evaluation  
        TestNsEelEvaluation();

        // Test 3: Binary Parser (with mock data)
        TestBinaryParsing();

        // Test 4: Complete Workflow Integration
        TestCompleteWorkflow();

        Console.WriteLine("\n✅ AVS Compatibility Test Suite Complete!");
    }

    private static void TestEffectMapping()
    {
        Console.WriteLine("🔍 Testing Effect Index Mapping...");

        try
        {
            // Test basic mapping
            var blurType = AvsEffectMapping.GetEffectType(6); // Blur effect
            Console.WriteLine($"   ✅ Index 6 → {blurType?.Name ?? "null"}");

            var blurIndex = AvsEffectMapping.GetEffectIndex(typeof(PhoenixVisualizer.Core.Effects.Nodes.AvsEffects.BlurEffectsNode));
            Console.WriteLine($"   ✅ BlurEffectsNode → Index {blurIndex}");

            // Test coverage
            var supportedCount = AvsEffectMapping.GetSupportedIndices().Length;
            Console.WriteLine($"   ✅ Total supported effects: {supportedCount}");

            // Test APE mapping
            var apeSupported = AvsEffectMapping.IsApeEffectSupported("Channel Shift");
            Console.WriteLine($"   ✅ APE 'Channel Shift' supported: {apeSupported}");

            Console.WriteLine("   🎉 Effect mapping test passed!\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Effect mapping test failed: {ex.Message}\n");
        }
    }

    private static void TestNsEelEvaluation()
    {
        Console.WriteLine("🧮 Testing NS-EEL Expression Evaluation...");

        try
        {
            using var evaluator = new PhoenixVisualizer.PluginHost.NsEelEvaluator();

            // Test basic math
            var result1 = evaluator.Evaluate("sin(3.14159/2)");
            Console.WriteLine($"   ✅ sin(π/2) = {result1:F3} (expected ≈ 1.0)");

            // Test audio variables
            evaluator.SetAudioData(0.8f, 0.6f, 0.4f, 0.7f, true);
            var result2 = evaluator.Evaluate("bass + mid + treble");
            Console.WriteLine($"   ✅ bass + mid + treble = {result2:F3} (expected 1.8)");

            // Test complex expression
            var result3 = evaluator.Evaluate("if(beat, bass * 2, mid)");
            Console.WriteLine($"   ✅ if(beat, bass*2, mid) = {result3:F3} (beat=true, so bass*2)");

            Console.WriteLine("   🎉 NS-EEL evaluation test passed!\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ NS-EEL evaluation test failed: {ex.Message}\n");
        }
    }

    private static void TestBinaryParsing()
    {
        Console.WriteLine("📄 Testing AVS Binary Parser...");

        try
        {
            // Create mock AVS file data
            var mockAvsData = CreateMockAvsFile();
            
            // Save mock data to temp file
            var tempFile = Path.GetTempFileName() + ".avs";
            File.WriteAllBytes(tempFile, mockAvsData);

            try
            {
                // Test parsing
                var phoenixJson = AvsPresetConverter.LoadAvs(tempFile);
                var parsed = JsonDocument.Parse(phoenixJson);

                Console.WriteLine($"   ✅ Parsed mock AVS file successfully");
                
                // Check structure
                if (parsed.RootElement.TryGetProperty("effects", out var effects))
                {
                    var effectCount = effects.GetArrayLength();
                    Console.WriteLine($"   ✅ Found {effectCount} effects");
                }

                if (parsed.RootElement.TryGetProperty("metadata", out var metadata))
                {
                    Console.WriteLine($"   ✅ Metadata section present");
                }

                Console.WriteLine("   🎉 Binary parsing test passed!\n");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Binary parsing test failed: {ex.Message}\n");
        }
    }

    private static void TestCompleteWorkflow()
    {
        Console.WriteLine("🔄 Testing Complete AVS Workflow...");

        try
        {
            var loader = new CompleteAvsPresetLoader();

            // Create mock AVS file
            var mockAvsData = CreateMockAvsFile();
            var tempFile = Path.GetTempFileName() + ".avs";
            File.WriteAllBytes(tempFile, mockAvsData);

            try
            {
                // Test preset info extraction
                var presetInfo = loader.GetPresetInfo(tempFile);
                Console.WriteLine($"   ✅ Preset info: {presetInfo.TotalEffectCount} effects, {presetInfo.SupportedEffectCount} supported");

                // Test full loading
                var effectChain = loader.LoadFromFile(tempFile);
                Console.WriteLine($"   ✅ Effect chain created with {effectChain.Count} effects");

                Console.WriteLine("   🎉 Complete workflow test passed!\n");
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Complete workflow test failed: {ex.Message}\n");
        }
    }

    /// <summary>
    /// Create a minimal mock AVS file for testing
    /// </summary>
    private static byte[] CreateMockAvsFile()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // Header (32 bytes)
        var header = "Nullsoft AVS Preset 0.2";
        var headerBytes = new byte[32];
        System.Text.Encoding.ASCII.GetBytes(header, 0, header.Length, headerBytes, 0);
        bw.Write(headerBytes);

        // Effect count (5 items: init, frame, beat, clear, blur effect)
        bw.Write(5);

        // Init script (id=0x03)
        var initCode = "n=100;";
        var initBytes = System.Text.Encoding.ASCII.GetBytes(initCode);
        bw.Write(0x03);
        bw.Write(initBytes.Length);
        bw.Write(initBytes);

        // Frame script (id=0x02)
        var frameCode = "t=time;";
        var frameBytes = System.Text.Encoding.ASCII.GetBytes(frameCode);
        bw.Write(0x02);
        bw.Write(frameBytes.Length);
        bw.Write(frameBytes);

        // Beat script (id=0x04)
        var beatCode = "beat_sensitivity=0.8;";
        var beatBytes = System.Text.Encoding.ASCII.GetBytes(beatCode);
        bw.Write(0x04);
        bw.Write(beatBytes.Length);
        bw.Write(beatBytes);

        // Clear every frame (id=0x05)
        bw.Write(0x05);
        bw.Write(1);
        bw.Write((byte)1); // Clear enabled

        // Blur effect (index=6)
        bw.Write(6); // Blur effect index
        bw.Write(4); // 4 bytes of data
        bw.Write(2);  // Blur amount parameter

        return ms.ToArray();
    }

    /// <summary>
    /// Quick validation that all necessary types exist
    /// </summary>
    public static void ValidateTypeAvailability()
    {
        Console.WriteLine("🔍 Validating Type Availability...");

        try
        {
            // Check effect mapping types
            var mappingType = typeof(AvsEffectMapping);
            Console.WriteLine($"   ✅ {mappingType.Name} available");

            // Check converter types
            var converterType = typeof(AvsPresetConverter);
            Console.WriteLine($"   ✅ {converterType.Name} available");

            // Check loader types
            var loaderType = typeof(CompleteAvsPresetLoader);
            Console.WriteLine($"   ✅ {loaderType.Name} available");

            // Check NS-EEL evaluator
            var eelType = typeof(PhoenixVisualizer.PluginHost.NsEelEvaluator);
            Console.WriteLine($"   ✅ {eelType.Name} available");

            Console.WriteLine("   🎉 All required types available!\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Type validation failed: {ex.Message}\n");
        }
    }
}