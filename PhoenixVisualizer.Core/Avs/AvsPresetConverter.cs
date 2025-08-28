using System.Text.Json;
using PhoenixVisualizer.Core.Effects.Interfaces;

namespace PhoenixVisualizer.Core.Avs;

/// <summary>
/// Enhanced AVS preset converter with effect mapping and parameter parsing
/// Replaces the basic AvsConverter with full Phoenix integration
/// </summary>
public static class AvsPresetConverter
{
    /// <summary>
    /// Load an AVS preset file and convert to Phoenix format
    /// </summary>
    /// <param name="path">Path to .avs file</param>
    /// <returns>Phoenix-formatted JSON representation</returns>
    public static string LoadAvs(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);

        // Verify header
        var header = new string(br.ReadChars(32)).TrimEnd('\0');
        if (!header.Contains("Nullsoft AVS"))
            throw new InvalidDataException("Not a valid AVS preset file.");

        // Read effect count
        int effectCount = br.ReadInt32();
        var effects = new List<object>();
        string init = "", frame = "", point = "", beat = "";
        bool clearEveryFrame = true;

        for (int i = 0; i < effectCount; i++)
        {
            int id = br.ReadInt32();
            int size = br.ReadInt32();
            byte[] blob = br.ReadBytes(size);

            // Handle special AVS system components
            switch (id)
            {
                case 0x01: // Superscope / point script
                    point = ExtractString(blob);
                    effects.Add(new { 
                        type = "superscope_script",
                        code = point,
                        effectIndex = id
                    });
                    break;
                    
                case 0x02: // Trans / per frame
                    frame = ExtractString(blob);
                    effects.Add(new { 
                        type = "frame_script",
                        code = frame,
                        effectIndex = id
                    });
                    break;
                    
                case 0x03: // Init code
                    init = ExtractString(blob);
                    effects.Add(new { 
                        type = "init_script",
                        code = init,
                        effectIndex = id
                    });
                    break;
                    
                case 0x04: // On beat
                    beat = ExtractString(blob);
                    effects.Add(new { 
                        type = "beat_script",
                        code = beat,
                        effectIndex = id
                    });
                    break;
                    
                case 0x05: // Clear every frame toggle
                    clearEveryFrame = blob[0] != 0;
                    effects.Add(new { 
                        type = "clear_option",
                        enabled = clearEveryFrame,
                        effectIndex = id
                    });
                    break;
                    
                default:
                    // Try to map to Phoenix effect
                    var phoenixEffect = MapAvsEffectToPhoenix(id, blob);
                    effects.Add(phoenixEffect);
                    break;
            }
        }

        // Build Phoenix-compatible JSON
        var phoenixPreset = new
        {
            format = "phoenix_avs_preset",
            version = "1.0",
            originalFile = Path.GetFileName(path),
            metadata = new
            {
                init,
                frame,
                point,
                beat,
                clearEveryFrame
            },
            effects = effects
        };

        return JsonSerializer.Serialize(phoenixPreset, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Map an AVS effect to Phoenix representation
    /// </summary>
    private static object MapAvsEffectToPhoenix(int effectIndex, byte[] data)
    {
        var effectType = AvsEffectMapping.GetEffectType(effectIndex);
        var effectName = AvsEffectMapping.GetEffectName(effectIndex);
        
        if (effectType != null)
        {
            // Successfully mapped to Phoenix effect
            var parameters = ExtractEffectParameters(effectIndex, data);
            
            return new
            {
                type = "phoenix_effect",
                effectIndex,
                effectName,
                phoenixType = effectType.Name,
                parameters,
                supported = true,
                rawData = Convert.ToBase64String(data) // Keep for round-trip
            };
        }
        else
        {
            // Unmapped effect - preserve as raw data
            return new
            {
                type = "avs_raw",
                effectIndex,
                effectName = $"Unknown Effect {effectIndex}",
                supported = false,
                rawData = Convert.ToBase64String(data)
            };
        }
    }

    /// <summary>
    /// Extract effect parameters from binary data
    /// TODO: Implement specific parameter parsing for each effect type
    /// </summary>
    private static Dictionary<string, object> ExtractEffectParameters(int effectIndex, byte[] data)
    {
        var parameters = new Dictionary<string, object>();
        
        // For now, we'll implement basic parameter extraction
        // This needs to be expanded with effect-specific parsing
        
        try
        {
            using var ms = new MemoryStream(data);
            using var br = new BinaryReader(ms);
            
            // Common patterns in AVS effect parameters
            switch (effectIndex)
            {
                case 6: // Blur
                    if (data.Length >= 4)
                    {
                        parameters["blur_amount"] = br.ReadInt32();
                    }
                    break;
                    
                case 22: // Brightness  
                    if (data.Length >= 8)
                    {
                        parameters["brightness"] = br.ReadInt32();
                        parameters["contrast"] = br.ReadInt32();
                    }
                    break;
                    
                case 36: // Superscope
                    // Superscope has complex structure with code sections
                    var superscopeParams = ParseSuperscopeParameters(data);
                    foreach (var kvp in superscopeParams)
                    {
                        parameters[kvp.Key] = kvp.Value;
                    }
                    break;
                    
                default:
                    // Generic parameter extraction - try to read common patterns
                    if (data.Length >= 4)
                    {
                        parameters["param1"] = br.ReadInt32();
                    }
                    if (data.Length >= 8)
                    {
                        parameters["param2"] = br.ReadInt32();
                    }
                    if (data.Length >= 12)
                    {
                        parameters["param3"] = br.ReadInt32();
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            // If parameter parsing fails, add error info
            parameters["parsing_error"] = ex.Message;
        }
        
        return parameters;
    }

    /// <summary>
    /// Parse Superscope effect parameters (complex structure)
    /// </summary>
    private static Dictionary<string, object> ParseSuperscopeParameters(byte[] data)
    {
        var parameters = new Dictionary<string, object>();
        
        try
        {
            using var ms = new MemoryStream(data);
            using var br = new BinaryReader(ms);
            
            // Superscope structure (simplified)
            // TODO: Implement full Superscope parameter parsing based on r_sscope.cpp
            if (data.Length > 0)
            {
                parameters["enabled"] = br.ReadByte() != 0;
            }
            
            // Look for embedded code strings
            var codeBlocks = ExtractCodeBlocks(data);
            for (int i = 0; i < codeBlocks.Count; i++)
            {
                parameters[$"code_block_{i}"] = codeBlocks[i];
            }
        }
        catch
        {
            parameters["parsing_error"] = "Failed to parse Superscope parameters";
        }
        
        return parameters;
    }

    /// <summary>
    /// Extract embedded code blocks from effect data
    /// </summary>
    private static List<string> ExtractCodeBlocks(byte[] data)
    {
        var codeBlocks = new List<string>();
        
        // Look for null-terminated strings in the data
        var text = System.Text.Encoding.ASCII.GetString(data);
        var blocks = text.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var block in blocks)
        {
            if (block.Trim().Length > 0 && IsLikelyCode(block))
            {
                codeBlocks.Add(block.Trim());
            }
        }
        
        return codeBlocks;
    }

    /// <summary>
    /// Heuristic to determine if a string is likely NS-EEL code
    /// </summary>
    private static bool IsLikelyCode(string text)
    {
        // Simple heuristics for NS-EEL code
        return text.Contains('=') || 
               text.Contains('(') || 
               text.Contains(';') ||
               text.Contains("sin") ||
               text.Contains("cos") ||
               text.Contains("bass") ||
               text.Contains("mid") ||
               text.Contains("treble");
    }

    /// <summary>
    /// Extract null-terminated string from binary data
    /// </summary>
    private static string ExtractString(byte[] data)
    {
        try
        {
            var str = System.Text.Encoding.ASCII.GetString(data).TrimEnd('\0');
            return str;
        }
        catch
        {
            return "// (unreadable code block)";
        }
    }

    /// <summary>
    /// Save Phoenix preset as AVS file
    /// Enhanced version with effect mapping support
    /// </summary>
    public static void SaveAvs(string path, string phoenixJson)
    {
        var doc = JsonDocument.Parse(phoenixJson);
        var root = doc.RootElement;
        
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);

        // Write AVS header (32 bytes, null-padded)
        var header = "Nullsoft AVS Preset 0.2";
        var headerBytes = new byte[32];
        System.Text.Encoding.ASCII.GetBytes(header, 0, header.Length, headerBytes, 0);
        bw.Write(headerBytes);

        // Collect effects
        var effects = new List<JsonElement>();
        if (root.TryGetProperty("effects", out var effectsArray))
        {
            foreach (var effect in effectsArray.EnumerateArray())
            {
                effects.Add(effect);
            }
        }

        // Write effect count (effects + metadata blocks)
        var metadataBlockCount = 0;
        if (root.TryGetProperty("metadata", out var metadata))
        {
            if (metadata.TryGetProperty("init", out _)) metadataBlockCount++;
            if (metadata.TryGetProperty("frame", out _)) metadataBlockCount++;
            if (metadata.TryGetProperty("point", out _)) metadataBlockCount++;
            if (metadata.TryGetProperty("beat", out _)) metadataBlockCount++;
            metadataBlockCount++; // clearEveryFrame option
        }
        
        bw.Write(effects.Count + metadataBlockCount);

        // Write metadata blocks
        if (root.TryGetProperty("metadata", out metadata))
        {
            WriteMetadataBlock(bw, 0x03, metadata, "init");    // Init code
            WriteMetadataBlock(bw, 0x02, metadata, "frame");   // Frame code  
            WriteMetadataBlock(bw, 0x01, metadata, "point");   // Point code
            WriteMetadataBlock(bw, 0x04, metadata, "beat");    // Beat code
            
            // Clear every frame option
            bool clearEveryFrame = metadata.TryGetProperty("clearEveryFrame", out var cef) && cef.GetBoolean();
            bw.Write(0x05);
            bw.Write(1);
            bw.Write(clearEveryFrame ? (byte)1 : (byte)0);
        }

        // Write effects
        foreach (var effect in effects)
        {
            WriteEffect(bw, effect);
        }
    }

    /// <summary>
    /// Write metadata block (init, frame, point, beat)
    /// </summary>
    private static void WriteMetadataBlock(BinaryWriter bw, int blockId, JsonElement metadata, string propertyName)
    {
        if (metadata.TryGetProperty(propertyName, out var prop))
        {
            var text = prop.GetString() ?? "";
            var bytes = System.Text.Encoding.ASCII.GetBytes(text);
            bw.Write(blockId);
            bw.Write(bytes.Length);
            bw.Write(bytes);
        }
    }

    /// <summary>
    /// Write individual effect to AVS file
    /// </summary>
    private static void WriteEffect(BinaryWriter bw, JsonElement effect)
    {
        if (effect.TryGetProperty("type", out var typeEl))
        {
            var type = typeEl.GetString();
            
            if (type == "phoenix_effect" && effect.TryGetProperty("effectIndex", out var indexEl))
            {
                // Phoenix effect with known index
                var index = indexEl.GetInt32();
                var rawData = Convert.FromBase64String(effect.GetProperty("rawData").GetString() ?? "");
                
                bw.Write(index);
                bw.Write(rawData.Length);
                bw.Write(rawData);
            }
            else if (type == "avs_raw" && effect.TryGetProperty("effectIndex", out indexEl))
            {
                // Raw AVS effect
                var index = indexEl.GetInt32();
                var rawData = Convert.FromBase64String(effect.GetProperty("rawData").GetString() ?? "");
                
                bw.Write(index);
                bw.Write(rawData.Length);
                bw.Write(rawData);
            }
            else
            {
                // Unknown effect type - write as placeholder
                var unknownData = System.Text.Encoding.ASCII.GetBytes("unknown");
                bw.Write(0x99); // Placeholder ID
                bw.Write(unknownData.Length);
                bw.Write(unknownData);
            }
        }
    }
}