using System.Text.Json;
using PhoenixVisualizer.Core.Effects.Interfaces;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Avs;

/// <summary>
/// Complete AVS preset loader that converts .avs files to Phoenix effect chains
/// Integrates binary parsing, effect mapping, parameter extraction, and NS-EEL evaluation
/// </summary>
public class CompleteAvsPresetLoader
{
    private readonly INsEelEvaluator _eelEvaluator;
    
    public CompleteAvsPresetLoader(INsEelEvaluator eelEvaluator)
    {
        _eelEvaluator = eelEvaluator ?? throw new ArgumentNullException(nameof(eelEvaluator));
    }

    /// <summary>
    /// Load an AVS preset file and create a Phoenix effect chain
    /// </summary>
    /// <param name="avsFilePath">Path to .avs file</param>
    /// <returns>Phoenix effect chain</returns>
    public EffectChain LoadFromFile(string avsFilePath)
    {
        if (!File.Exists(avsFilePath))
            throw new FileNotFoundException($"AVS preset file not found: {avsFilePath}");

        try
        {
            // Step 1: Parse AVS binary format using enhanced converter
            var phoenixJson = AvsPresetConverter.LoadAvs(avsFilePath);
            var presetData = JsonDocument.Parse(phoenixJson);
            var root = presetData.RootElement;

            // Step 2: Extract metadata (init, frame, point, beat code)
            var metadata = ExtractMetadata(root);

            // Step 3: Convert effects to Phoenix nodes
            var effectNodes = new List<IEffectNode>();
            
            if (root.TryGetProperty("effects", out var effectsArray))
            {
                foreach (var effectData in effectsArray.EnumerateArray())
                {
                    var effectNode = CreateEffectNode(effectData, metadata);
                    if (effectNode != null)
                    {
                        effectNodes.Add(effectNode);
                    }
                }
            }

            // Step 4: Create and configure effect chain
            var effectChain = new EffectChain(effectNodes);
            
            // Step 5: Apply global metadata (init/frame/beat scripts)
            ConfigureGlobalScripts(effectChain, metadata);

            return effectChain;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load AVS preset '{avsFilePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Extract metadata (scripts and settings) from parsed AVS data
    /// </summary>
    private PresetMetadata ExtractMetadata(JsonElement root)
    {
        var metadata = new PresetMetadata();

        if (root.TryGetProperty("metadata", out var metadataElement))
        {
            if (metadataElement.TryGetProperty("init", out var init))
                metadata.InitScript = init.GetString() ?? "";
                
            if (metadataElement.TryGetProperty("frame", out var frame))
                metadata.FrameScript = frame.GetString() ?? "";
                
            if (metadataElement.TryGetProperty("point", out var point))
                metadata.PointScript = point.GetString() ?? "";
                
            if (metadataElement.TryGetProperty("beat", out var beat))
                metadata.BeatScript = beat.GetString() ?? "";
                
            if (metadataElement.TryGetProperty("clearEveryFrame", out var clear))
                metadata.ClearEveryFrame = clear.GetBoolean();
        }

        return metadata;
    }

    /// <summary>
    /// Create a Phoenix effect node from AVS effect data
    /// </summary>
    private IEffectNode? CreateEffectNode(JsonElement effectData, PresetMetadata metadata)
    {
        try
        {
            if (!effectData.TryGetProperty("type", out var typeElement))
                return null;

            var type = typeElement.GetString();

            switch (type)
            {
                case "phoenix_effect":
                    return CreatePhoenixEffect(effectData);
                    
                case "superscope_script":
                case "frame_script":
                case "init_script":
                case "beat_script":
                    // These are handled globally, not as individual effect nodes
                    return null;
                    
                case "clear_option":
                    // This is handled in metadata
                    return null;
                    
                case "avs_raw":
                    // Unsupported effect - create placeholder or skip
                    return CreateUnsupportedEffectPlaceholder(effectData);
                    
                default:
                    return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CompleteAvsPresetLoader] Error creating effect node: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Create a Phoenix effect from mapped AVS effect data
    /// </summary>
    private IEffectNode? CreatePhoenixEffect(JsonElement effectData)
    {
        if (!effectData.TryGetProperty("phoenixType", out var phoenixTypeElement) ||
            !effectData.TryGetProperty("effectIndex", out var indexElement))
            return null;

        var phoenixTypeName = phoenixTypeElement.GetString();
        var effectIndex = indexElement.GetInt32();

        // Get the actual Phoenix effect type
        var effectType = AvsEffectMapping.GetEffectType(effectIndex);
        if (effectType == null)
            return null;

        try
        {
            // Create instance of the Phoenix effect
            var effectNode = (IEffectNode?)Activator.CreateInstance(effectType);
            if (effectNode == null)
                return null;

            // Load parameters if available
            if (effectData.TryGetProperty("parameters", out var parametersElement))
            {
                LoadEffectParameters(effectNode, parametersElement);
            }

            // Load embedded code if available
            LoadEmbeddedCode(effectNode, effectData);

            return effectNode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CompleteAvsPresetLoader] Error creating Phoenix effect {phoenixTypeName}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Load effect parameters into Phoenix effect node
    /// </summary>
    private void LoadEffectParameters(IEffectNode effectNode, JsonElement parameters)
    {
        // TODO: Implement effect-specific parameter loading
        // This would require knowledge of each effect's parameter structure
        
        try
        {
            foreach (var parameter in parameters.EnumerateObject())
            {
                var name = parameter.Name;
                var value = parameter.Value;

                // Try to set parameter using reflection or configuration interface
                // This is a simplified implementation - real version would need
                // effect-specific parameter mapping
                
                if (effectNode is IConfigurableEffect configurable)
                {
                    switch (value.ValueKind)
                    {
                        case JsonValueKind.Number:
                            configurable.SetParameter(name, value.GetDouble());
                            break;
                        case JsonValueKind.String:
                            configurable.SetParameter(name, value.GetString() ?? "");
                            break;
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            configurable.SetParameter(name, value.GetBoolean());
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CompleteAvsPresetLoader] Error loading parameters: {ex.Message}");
        }
    }

    /// <summary>
    /// Load embedded NS-EEL code into effect node
    /// </summary>
    private void LoadEmbeddedCode(IEffectNode effectNode, JsonElement effectData)
    {
        try
        {
            // Look for code in parameters
            if (effectData.TryGetProperty("parameters", out var parameters))
            {
                foreach (var param in parameters.EnumerateObject())
                {
                    if (param.Name.StartsWith("code_block_") && param.Value.ValueKind == JsonValueKind.String)
                    {
                        var code = param.Value.GetString() ?? "";
                        if (!string.IsNullOrWhiteSpace(code))
                        {
                            // Load code into effect using NS-EEL evaluator
                            if (effectNode is IScriptableEffect scriptable)
                            {
                                scriptable.LoadScript(code);
                            }
                            
                            // Validate the code syntax using NsEelEvaluator
                            try
                            {
                                var result = _eelEvaluator.Evaluate(code);
                                Console.WriteLine($"[CompleteAvsPresetLoader] Code validation successful, result: {result}");
                            }
                            catch (Exception evalEx)
                            {
                                Console.WriteLine($"[CompleteAvsPresetLoader] Code validation failed: {evalEx.Message}");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CompleteAvsPresetLoader] Error loading embedded code: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a placeholder for unsupported effects
    /// </summary>
    private IEffectNode? CreateUnsupportedEffectPlaceholder(JsonElement effectData)
    {
        // For now, skip unsupported effects
        // In the future, could create a generic "UnsupportedEffect" node
        // that preserves the raw data for round-trip compatibility
        
        if (effectData.TryGetProperty("effectName", out var nameElement))
        {
            var effectName = nameElement.GetString() ?? "Unknown";
            Console.WriteLine($"[CompleteAvsPresetLoader] Skipping unsupported effect: {effectName}");
        }
        
        return null;
    }

    /// <summary>
    /// Configure global scripts (init, frame, beat) on the effect chain
    /// </summary>
    private void ConfigureGlobalScripts(EffectChain effectChain, PresetMetadata metadata)
    {
        try
        {
            // Set up NS-EEL evaluator with global variables
            _eelEvaluator.SetVariable("clearEveryFrame", metadata.ClearEveryFrame ? 1.0 : 0.0);
            
            // Compile and validate init script
            if (!string.IsNullOrWhiteSpace(metadata.InitScript))
            {
                try
                {
                    var initResult = _eelEvaluator.Evaluate(metadata.InitScript);
                    Console.WriteLine($"[CompleteAvsPresetLoader] Init script compiled successfully, result: {initResult}");
                    
                    // Store the script for later execution
                    effectChain.InitScript = metadata.InitScript;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CompleteAvsPresetLoader] Init script compilation failed: {ex.Message}");
                }
            }
            
            // Compile and validate frame script
            if (!string.IsNullOrWhiteSpace(metadata.FrameScript))
            {
                try
                {
                    var frameResult = _eelEvaluator.Evaluate(metadata.FrameScript);
                    Console.WriteLine($"[CompleteAvsPresetLoader] Frame script compiled successfully, result: {frameResult}");
                    
                    // Store the script for later execution
                    effectChain.FrameScript = metadata.FrameScript;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CompleteAvsPresetLoader] Frame script compilation failed: {ex.Message}");
                }
            }
            
            // Compile and validate beat script
            if (!string.IsNullOrWhiteSpace(metadata.BeatScript))
            {
                try
                {
                    var beatResult = _eelEvaluator.Evaluate(metadata.BeatScript);
                    Console.WriteLine($"[CompleteAvsPresetLoader] Beat script compiled successfully, result: {beatResult}");
                    
                    // Store the script for later execution
                    effectChain.BeatScript = metadata.BeatScript;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CompleteAvsPresetLoader] Beat script compilation failed: {ex.Message}");
                }
            }
            
            if (metadata.ClearEveryFrame)
            {
                effectChain.ClearEveryFrame = true;
                Console.WriteLine("[CompleteAvsPresetLoader] Clear every frame enabled");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CompleteAvsPresetLoader] Error configuring global scripts: {ex.Message}");
        }
    }

    /// <summary>
    /// Load multiple AVS presets from a directory
    /// </summary>
    public List<EffectChain> LoadFromDirectory(string directoryPath)
    {
        var effectChains = new List<EffectChain>();
        
        if (!Directory.Exists(directoryPath))
            return effectChains;

        var avsFiles = Directory.GetFiles(directoryPath, "*.avs", SearchOption.AllDirectories);
        
        foreach (var avsFile in avsFiles)
        {
            try
            {
                var effectChain = LoadFromFile(avsFile);
                effectChains.Add(effectChain);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CompleteAvsPresetLoader] Failed to load {avsFile}: {ex.Message}");
            }
        }
        
        return effectChains;
    }

    /// <summary>
    /// Get AVS preset information without fully loading
    /// </summary>
    public AvsPresetInfoExtended GetPresetInfo(string avsFilePath)
    {
        try
        {
            var phoenixJson = AvsPresetConverter.LoadAvs(avsFilePath);
            var presetData = JsonDocument.Parse(phoenixJson);
            var root = presetData.RootElement;

            var info = new AvsPresetInfoExtended
            {
                FilePath = avsFilePath,
                FileName = Path.GetFileName(avsFilePath),
                FileSize = new FileInfo(avsFilePath).Length
            };

            // Count effects
            if (root.TryGetProperty("effects", out var effectsArray))
            {
                var supportedCount = 0;
                var unsupportedCount = 0;
                
                foreach (var effect in effectsArray.EnumerateArray())
                {
                    if (effect.TryGetProperty("supported", out var supported) && supported.GetBoolean())
                        supportedCount++;
                    else
                        unsupportedCount++;
                }
                
                info.SupportedEffectCount = supportedCount;
                info.UnsupportedEffectCount = unsupportedCount;
                info.TotalEffectCount = supportedCount + unsupportedCount;
            }

            // Extract metadata
            if (root.TryGetProperty("metadata", out var metadata))
            {
                info.HasInitScript = metadata.TryGetProperty("init", out var init) && !string.IsNullOrWhiteSpace(init.GetString());
                info.HasFrameScript = metadata.TryGetProperty("frame", out var frame) && !string.IsNullOrWhiteSpace(frame.GetString());
                info.HasBeatScript = metadata.TryGetProperty("beat", out var beat) && !string.IsNullOrWhiteSpace(beat.GetString());
            }

            return info;
        }
        catch (Exception ex)
        {
            return new AvsPresetInfoExtended
            {
                FilePath = avsFilePath,
                FileName = Path.GetFileName(avsFilePath),
                LoadError = ex.Message
            };
        }
    }
}

/// <summary>
/// Metadata extracted from AVS preset
/// </summary>
public class PresetMetadata
{
    public string InitScript { get; set; } = "";
    public string FrameScript { get; set; } = "";
    public string PointScript { get; set; } = "";
    public string BeatScript { get; set; } = "";
    public bool ClearEveryFrame { get; set; } = false;
}

/// <summary>
/// Information about an AVS preset without full loading
/// </summary>
public class AvsPresetInfoExtended
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public long FileSize { get; set; }
    public int TotalEffectCount { get; set; }
    public int SupportedEffectCount { get; set; }
    public int UnsupportedEffectCount { get; set; }
    public bool HasInitScript { get; set; }
    public bool HasFrameScript { get; set; }
    public bool HasBeatScript { get; set; }
    public string? LoadError { get; set; }
}

/// <summary>
/// Interface for effects that can be configured with parameters
/// </summary>
public interface IConfigurableEffect
{
    void SetParameter(string name, object value);
}

/// <summary>
/// Interface for effects that can load scripts
/// </summary>
public interface IScriptableEffect
{
    void LoadScript(string script);
}

/// <summary>
/// Simple effect chain implementation
/// </summary>
public class EffectChain
{
    public List<IEffectNode> Effects { get; }
    
    // Global scripts
    public string? InitScript { get; set; }
    public string? FrameScript { get; set; }
    public string? BeatScript { get; set; }
    public bool ClearEveryFrame { get; set; }
    
    public EffectChain(List<IEffectNode> effects)
    {
        Effects = effects ?? new List<IEffectNode>();
    }
    
    public int Count => Effects.Count;
    
    /// <summary>
    /// Execute the init script
    /// </summary>
    public void ExecuteInitScript(INsEelEvaluator evaluator)
    {
        if (!string.IsNullOrWhiteSpace(InitScript))
        {
            try
            {
                evaluator.Evaluate(InitScript);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EffectChain] Error executing init script: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Execute the frame script
    /// </summary>
    public void ExecuteFrameScript(INsEelEvaluator evaluator)
    {
        if (!string.IsNullOrWhiteSpace(FrameScript))
        {
            try
            {
                evaluator.Evaluate(FrameScript);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EffectChain] Error executing frame script: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Execute the beat script
    /// </summary>
    public void ExecuteBeatScript(INsEelEvaluator evaluator)
    {
        if (!string.IsNullOrWhiteSpace(BeatScript))
        {
            try
            {
                evaluator.Evaluate(BeatScript);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EffectChain] Error executing beat script: {ex.Message}");
            }
        }
    }
}