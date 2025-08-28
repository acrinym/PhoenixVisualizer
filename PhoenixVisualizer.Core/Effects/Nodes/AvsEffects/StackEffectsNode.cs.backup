using System;
using System.Collections.Generic;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    /// <summary>
    /// Layer stacking effect with various blending modes
    /// Based on r_stack.cpp from original AVS
    /// Creates layered visual compositions by stacking multiple images
    /// </summary>
    public class StackEffectsNode : BaseEffectNode
    {
        #region Properties

        /// <summary>
        /// Controls whether the Stack effect is active
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Stack mode
        /// 0 = Normal Stack, 1 = Beat Stack, 2 = Random Stack, 3 = Sequence Stack
        /// </summary>
        public int StackMode { get; set; } = 0;

        /// <summary>
        /// Number of layers to stack
        /// </summary>
        public int LayerCount { get; set; } = 3;

        /// <summary>
        /// Blending mode between layers
        /// 0 = Replace, 1 = Additive, 2 = Multiply, 3 = Screen, 4 = Overlay, 5 = Difference
        /// </summary>
        public int BlendMode { get; set; } = 1;

        /// <summary>
        /// Layer order mode
        /// 0 = Forward, 1 = Reverse, 2 = Random
        /// </summary>
        public int LayerOrder { get; set; } = 0;

        /// <summary>
        /// Base transparency for all layers (0.0 to 1.0)
        /// </summary>
        public float BaseAlpha { get; set; } = 0.8f;

        /// <summary>
        /// Beat reactivity - changes layer properties on beat
        /// </summary>
        public bool BeatReactive { get; set; } = true;

        /// <summary>
        /// Additional alpha modifier on beat
        /// </summary>
        public float BeatAlpha { get; set; } = 1.2f;

        /// <summary>
        /// Layer fade time in frames
        /// </summary>
        public int FadeTime { get; set; } = 20;

        /// <summary>
        /// Offset between layers in pixels
        /// </summary>
        public int LayerOffset { get; set; } = 2;

        /// <summary>
        /// Whether to use historical frames for stacking
        /// </summary>
        public bool UseHistoricalFrames { get; set; } = true;

        #endregion

        #region Private Fields

        private readonly Queue<ImageBuffer> _frameHistory = new Queue<ImageBuffer>();
        private readonly Random _random = new Random();
        private int _currentLayerIndex = 0;
        private float _beatAlphaMultiplier = 1.0f;
        private int _fadeCounter = 0;

        private const int MAX_FRAME_HISTORY = 10;

        #endregion

        #region Constructor

        public StackEffectsNode()
        {
            Name = "Stack Effects";
            Description = "Creates layered visual compositions by stacking multiple images";
            Category = "Composite Effects";
        }

        #endregion

        #region Initialization

        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Image", typeof(ImageBuffer), true, null, "Primary image to stack"));
            _inputPorts.Add(new EffectPort("Layer1", typeof(ImageBuffer), false, null, "Additional layer 1"));
            _inputPorts.Add(new EffectPort("Layer2", typeof(ImageBuffer), false, null, "Additional layer 2"));
            _inputPorts.Add(new EffectPort("Layer3", typeof(ImageBuffer), false, null, "Additional layer 3"));
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), false, null, "Stacked output image"));
        }

        #endregion

        #region Effect Processing

        public override void ProcessFrame(Dictionary<string, object> inputData, Dictionary<string, object> outputData, AudioFeatures audioFeatures)
        {
            if (!Enabled) return;

            try
            {
                var primaryImage = GetInputValue<ImageBuffer>("Image", inputData);
                if (primaryImage?.Data == null) return;

                // Update frame history if using historical frames
                if (UseHistoricalFrames)
                {
                    var historyCopy = new ImageBuffer(primaryImage.Width, primaryImage.Height);
                    Array.Copy(primaryImage.Data, historyCopy.Data, primaryImage.Data.Length);
                    
                    _frameHistory.Enqueue(historyCopy);
                    if (_frameHistory.Count > MAX_FRAME_HISTORY)
                    {
                        _frameHistory.Dequeue();
                    }
                }

                // Handle beat reactivity
                if (BeatReactive && audioFeatures.Beat)
                {
                    _beatAlphaMultiplier = BeatAlpha;
                    _fadeCounter = FadeTime;
                    
                    if (StackMode == 2) // Random mode - change layer on beat
                    {
                        _currentLayerIndex = _random.Next(LayerCount);
                    }
                    else if (StackMode == 3) // Sequence mode - advance layer on beat
                    {
                        _currentLayerIndex = (_currentLayerIndex + 1) % LayerCount;
                    }
                }
                else if (_fadeCounter > 0)
                {
                    _fadeCounter--;
                    _beatAlphaMultiplier = 1.0f + (BeatAlpha - 1.0f) * (_fadeCounter / (float)FadeTime);
                }
                else
                {
                    _beatAlphaMultiplier = 1.0f;
                }

                // Create output image
                var outputImage = new ImageBuffer(primaryImage.Width, primaryImage.Height);
                Array.Copy(primaryImage.Data, outputImage.Data, primaryImage.Data.Length);

                // Collect layers to stack
                var layers = CollectLayers(inputData, primaryImage);

                // Apply stacking
                ApplyStacking(outputImage, layers);

                outputData["Output"] = outputImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Stack Effects] Error processing frame: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        private List<ImageBuffer> CollectLayers(Dictionary<string, object> inputData, ImageBuffer primaryImage)
        {
            var layers = new List<ImageBuffer>();

            // Add primary image as first layer
            layers.Add(primaryImage);

            if (UseHistoricalFrames && _frameHistory.Count > 0)
            {
                // Add historical frames as layers
                var historyArray = _frameHistory.ToArray();
                int step = Math.Max(1, _frameHistory.Count / Math.Max(1, LayerCount - 1));
                
                for (int i = 0; i < LayerCount - 1 && i * step < historyArray.Length; i++)
                {
                    layers.Add(historyArray[i * step]);
                }
            }
            else
            {
                // Add explicit layer inputs
                for (int i = 1; i < LayerCount; i++)
                {
                    var layer = GetInputValue<ImageBuffer>($"Layer{i}", inputData);
                    if (layer?.Data != null)
                    {
                        layers.Add(layer);
                    }
                    else if (layers.Count > 1)
                    {
                        // Duplicate previous layer if not available
                        layers.Add(layers[layers.Count - 1]);
                    }
                }
            }

            // Handle layer ordering
            if (LayerOrder == 1) // Reverse
            {
                layers.Reverse();
            }
            else if (LayerOrder == 2 && StackMode == 2) // Random when in random mode
            {
                for (int i = layers.Count - 1; i > 0; i--)
                {
                    int j = _random.Next(i + 1);
                    var temp = layers[i];
                    layers[i] = layers[j];
                    layers[j] = temp;
                }
            }

            return layers;
        }

        private void ApplyStacking(ImageBuffer outputImage, List<ImageBuffer> layers)
        {
            if (layers.Count <= 1) return;

            for (int layerIndex = 1; layerIndex < layers.Count; layerIndex++)
            {
                var layer = layers[layerIndex];
                if (layer?.Data == null) continue;

                // Calculate layer alpha
                float layerAlpha = BaseAlpha * _beatAlphaMultiplier;
                
                // Reduce alpha for distant layers
                if (layerIndex > 1)
                {
                    layerAlpha *= (1.0f - (layerIndex - 1) * 0.2f);
                }

                // Calculate layer offset
                int offsetX = LayerOffset * (layerIndex - 1);
                int offsetY = LayerOffset * (layerIndex - 1);

                // Apply stacking mode adjustments
                if (StackMode == 1 && layerIndex == _currentLayerIndex) // Beat mode - highlight current layer
                {
                    layerAlpha *= 1.5f;
                }

                BlendLayer(outputImage, layer, layerAlpha, offsetX, offsetY);
            }
        }

        private void BlendLayer(ImageBuffer destination, ImageBuffer source, float alpha, int offsetX, int offsetY)
        {
            alpha = Math.Max(0.0f, Math.Min(1.0f, alpha));
            if (alpha <= 0.001f) return;

            for (int y = 0; y < destination.Height; y++)
            {
                for (int x = 0; x < destination.Width; x++)
                {
                    int srcX = x - offsetX;
                    int srcY = y - offsetY;

                    // Check bounds
                    if (srcX < 0 || srcX >= source.Width || srcY < 0 || srcY >= source.Height)
                        continue;

                    int destIndex = y * destination.Width + x;
                    int srcIndex = srcY * source.Width + srcX;

                    uint destPixel = destination.Data[destIndex];
                    uint srcPixel = source.Data[srcIndex];

                    destination.Data[destIndex] = BlendPixels(destPixel, srcPixel, alpha);
                }
            }
        }

        private uint BlendPixels(uint dest, uint src, float alpha)
        {
            // Extract color components
            uint destA = (dest >> 24) & 0xFF;
            uint destR = (dest >> 16) & 0xFF;
            uint destG = (dest >> 8) & 0xFF;
            uint destB = dest & 0xFF;

            uint srcA = (src >> 24) & 0xFF;
            uint srcR = (src >> 16) & 0xFF;
            uint srcG = (src >> 8) & 0xFF;
            uint srcB = src & 0xFF;

            uint resultR, resultG, resultB;

            // Apply blending mode
            switch (BlendMode)
            {
                case 0: // Replace
                    resultR = (uint)(destR * (1 - alpha) + srcR * alpha);
                    resultG = (uint)(destG * (1 - alpha) + srcG * alpha);
                    resultB = (uint)(destB * (1 - alpha) + srcB * alpha);
                    break;

                case 1: // Additive
                    resultR = Math.Min(255u, (uint)(destR + srcR * alpha));
                    resultG = Math.Min(255u, (uint)(destG + srcG * alpha));
                    resultB = Math.Min(255u, (uint)(destB + srcB * alpha));
                    break;

                case 2: // Multiply
                    resultR = (uint)((destR * (srcR * alpha + 255 * (1 - alpha))) / 255);
                    resultG = (uint)((destG * (srcG * alpha + 255 * (1 - alpha))) / 255);
                    resultB = (uint)((destB * (srcB * alpha + 255 * (1 - alpha))) / 255);
                    break;

                case 3: // Screen
                    resultR = (uint)(255 - ((255 - destR) * (255 - srcR * alpha)) / 255);
                    resultG = (uint)(255 - ((255 - destG) * (255 - srcG * alpha)) / 255);
                    resultB = (uint)(255 - ((255 - destB) * (255 - srcB * alpha)) / 255);
                    break;

                case 4: // Overlay
                    resultR = destR < 128 ? 
                        (uint)((2 * destR * srcR * alpha) / 255) : 
                        (uint)(255 - (2 * (255 - destR) * (255 - srcR * alpha)) / 255);
                    resultG = destG < 128 ? 
                        (uint)((2 * destG * srcG * alpha) / 255) : 
                        (uint)(255 - (2 * (255 - destG) * (255 - srcG * alpha)) / 255);
                    resultB = destB < 128 ? 
                        (uint)((2 * destB * srcB * alpha) / 255) : 
                        (uint)(255 - (2 * (255 - destB) * (255 - srcB * alpha)) / 255);
                    break;

                case 5: // Difference
                    resultR = (uint)(destR + Math.Abs((int)destR - (int)(srcR * alpha)));
                    resultG = (uint)(destG + Math.Abs((int)destG - (int)(srcG * alpha)));
                    resultB = (uint)(destB + Math.Abs((int)destB - (int)(srcB * alpha)));
                    resultR = Math.Min(255u, resultR);
                    resultG = Math.Min(255u, resultG);
                    resultB = Math.Min(255u, resultB);
                    break;

                default:
                    resultR = destR;
                    resultG = destG;
                    resultB = destB;
                    break;
            }

            return (Math.Max(destA, srcA) << 24) | (resultR << 16) | (resultG << 8) | resultB;
        }

        #endregion

        #region Configuration

        public override Dictionary<string, object> GetConfiguration()
        {
            return new Dictionary<string, object>
            {
                { "Enabled", Enabled },
                { "StackMode", StackMode },
                { "LayerCount", LayerCount },
                { "BlendMode", BlendMode },
                { "LayerOrder", LayerOrder },
                { "BaseAlpha", BaseAlpha },
                { "BeatReactive", BeatReactive },
                { "BeatAlpha", BeatAlpha },
                { "FadeTime", FadeTime },
                { "LayerOffset", LayerOffset },
                { "UseHistoricalFrames", UseHistoricalFrames }
            };
        }

        public override void ApplyConfiguration(Dictionary<string, object> config)
        {
            if (config.TryGetValue("Enabled", out var enabled))
                Enabled = Convert.ToBoolean(enabled);
            
            if (config.TryGetValue("StackMode", out var stackMode))
                StackMode = Convert.ToInt32(stackMode);
            
            if (config.TryGetValue("LayerCount", out var layerCount))
                LayerCount = Convert.ToInt32(layerCount);
            
            if (config.TryGetValue("BlendMode", out var blendMode))
                BlendMode = Convert.ToInt32(blendMode);
            
            if (config.TryGetValue("LayerOrder", out var layerOrder))
                LayerOrder = Convert.ToInt32(layerOrder);
            
            if (config.TryGetValue("BaseAlpha", out var baseAlpha))
                BaseAlpha = Convert.ToSingle(baseAlpha);
            
            if (config.TryGetValue("BeatReactive", out var beatReactive))
                BeatReactive = Convert.ToBoolean(beatReactive);
            
            if (config.TryGetValue("BeatAlpha", out var beatAlpha))
                BeatAlpha = Convert.ToSingle(beatAlpha);
            
            if (config.TryGetValue("FadeTime", out var fadeTime))
                FadeTime = Convert.ToInt32(fadeTime);
            
            if (config.TryGetValue("LayerOffset", out var layerOffset))
                LayerOffset = Convert.ToInt32(layerOffset);
            
            if (config.TryGetValue("UseHistoricalFrames", out var useHistorical))
                UseHistoricalFrames = Convert.ToBoolean(useHistorical);
        }

        #endregion
    }
}