using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using System.Drawing;
using PhoenixVisualizer.Core.Effects.Models;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects
{
    public class ConvolutionEffectsNode : BaseEffectNode
    {
        // Core configuration
        public bool Enabled { get; set; } = true;
        public int BlurMode { get; set; } = 0; // 0=Normal, 1=Enhanced
        public bool RoundingMode { get; set; } = false;
        public bool BeatReactive { get; set; } = false;
        public float BeatIntensity { get; set; } = 1.5f;
        public bool MultiThreaded { get; set; } = true;
        public int ThreadCount { get; set; } = 0; // 0 = auto-detect
        
        // Convolution kernel (5x5) - optimized weights
        private readonly int[,] convolutionKernel = new int[5, 5]
        {
            { 1,  4,  6,  4, 1 },
            { 4, 16, 24, 16, 4 },
            { 6, 24, 36, 24, 6 },
            { 4, 16, 24, 16, 4 },
            { 1,  4,  6,  4, 1 }
        };
        
        private readonly int kernelSum = 256; // Sum of all kernel values
        
        // Multi-threading support
        private Task[] processingTasks;
        private readonly object lockObject = new object();
        
        // Performance optimization
        private bool useSIMD = true;
        private int batchSize = 4;
        
        // Audio data for beat reactivity
        private float[] leftChannelData;
        private float[] rightChannelData;
        private float[] centerChannelData;
        
        public ConvolutionEffectsNode()
        {
            // Initialize multi-threading
            ThreadCount = ThreadCount == 0 ? Environment.ProcessorCount : ThreadCount;
            processingTasks = new Task[ThreadCount];
            
            // Check for SIMD support
            useSIMD = Vector.IsHardwareAccelerated;
        }
        
        protected override void InitializePorts()
        {
            _inputPorts.Add(new EffectPort("Input", typeof(ImageBuffer), true, null, "Input image buffer"));
            _inputPorts.Add(new EffectPort("Enabled", typeof(bool), false, true, "Enable/disable effect"));
            _inputPorts.Add(new EffectPort("BlurMode", typeof(int), false, 0, "Blur mode (0=Normal, 1=Enhanced)"));
            _inputPorts.Add(new EffectPort("RoundingMode", typeof(bool), false, false, "Enable rounding for quality"));
            _inputPorts.Add(new EffectPort("BeatReactive", typeof(bool), false, false, "Enable beat-reactive blur"));
            _inputPorts.Add(new EffectPort("BeatIntensity", typeof(float), false, 1.5f, "Beat intensity multiplier"));
            _inputPorts.Add(new EffectPort("MultiThreaded", typeof(bool), false, true, "Enable multi-threading"));
            
            _outputPorts.Add(new EffectPort("Output", typeof(ImageBuffer), true, null, "Processed image buffer"));
        }
        
        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!Enabled) return inputs["Input"];
            
            var input = inputs["Input"] as ImageBuffer;
            if (input == null) return inputs["Input"];
            
            // Update audio data for beat reactivity
            UpdateAudioData(audioFeatures);
            
            // Create output buffer
            var output = new ImageBuffer(input.Width, input.Height);
            
            // Apply convolution based on mode
            if (BlurMode == 1 && useSIMD)
            {
                ApplyConvolutionKernelSIMD(input, output, audioFeatures);
            }
            else
            {
                ApplyConvolutionKernel(input, output, audioFeatures);
            }
            
            return output;
        }
        
        private void UpdateAudioData(AudioFeatures audioFeatures)
        {
            if (audioFeatures?.SpectrumData != null && audioFeatures.SpectrumData.Length >= 2)
            {
                // Use spectrum data for channel information
                leftChannelData = new float[Math.Min(576, audioFeatures.SpectrumData.Length)];
                rightChannelData = new float[Math.Min(576, audioFeatures.SpectrumData.Length)];
                
                // Copy spectrum data (simplified - in real implementation this would be proper channel separation)
                for (int i = 0; i < leftChannelData.Length; i++)
                {
                    leftChannelData[i] = audioFeatures.SpectrumData[i];
                    rightChannelData[i] = audioFeatures.SpectrumData[i];
                }
                
                // Calculate center channel
                centerChannelData = new float[leftChannelData.Length];
                for (int i = 0; i < leftChannelData.Length; i++)
                {
                    centerChannelData[i] = (leftChannelData[i] + rightChannelData[i]) / 2.0f;
                }
            }
        }
        
        private void ApplyConvolutionKernel(ImageBuffer input, ImageBuffer output, AudioFeatures audioFeatures)
        {
            int width = input.Width;
            int height = input.Height;
            
            if (MultiThreaded && ThreadCount > 1)
            {
                ApplyConvolutionMultiThreaded(input, output, width, height, audioFeatures);
            }
            else
            {
                ApplyConvolutionSingleThreaded(input, output, width, height, audioFeatures);
            }
        }
        
        private void ApplyConvolutionMultiThreaded(ImageBuffer input, ImageBuffer output, int width, int height, AudioFeatures audioFeatures)
        {
            int rowsPerThread = height / ThreadCount;
            
            for (int threadIndex = 0; threadIndex < ThreadCount; threadIndex++)
            {
                int startRow = threadIndex * rowsPerThread;
                int endRow = (threadIndex == ThreadCount - 1) ? height : startRow + rowsPerThread;
                
                processingTasks[threadIndex] = Task.Run(() =>
                {
                    ProcessRowRange(startRow, endRow, width, height, input, output, audioFeatures);
                });
            }
            
            // Wait for all threads to complete
            Task.WaitAll(processingTasks);
        }
        
        private void ProcessRowRange(int startRow, int endRow, int width, int height, ImageBuffer input, ImageBuffer output, AudioFeatures audioFeatures)
        {
            for (int y = startRow; y < endRow; y++)
            {
                ProcessRow(y, width, height, input, output, audioFeatures);
            }
        }
        
        private void ProcessRow(int y, int width, int height, ImageBuffer input, ImageBuffer output, AudioFeatures audioFeatures)
        {
            // Process pixels in batches for optimization
            int processedPixels = 0;
            
            for (int x = 0; x < width; x += batchSize)
            {
                int remainingPixels = Math.Min(batchSize, width - x);
                
                if (remainingPixels == batchSize)
                {
                    // Process full batch
                    ProcessPixelBatch(x, y, width, height, input, output, audioFeatures);
                    processedPixels += batchSize;
                }
                else
                {
                    // Process remaining pixels individually
                    for (int i = 0; i < remainingPixels; i++)
                    {
                        ProcessPixel(x + i, y, width, height, input, output, audioFeatures);
                        processedPixels++;
                    }
                }
            }
        }
        
        private void ProcessPixelBatch(int startX, int y, int width, int height, ImageBuffer input, ImageBuffer output, AudioFeatures audioFeatures)
        {
            // Process 4 pixels in parallel
            for (int i = 0; i < 4; i++)
            {
                ProcessPixel(startX + i, y, width, height, input, output, audioFeatures);
            }
        }
        
        private void ProcessPixel(int x, int y, int width, int height, ImageBuffer input, ImageBuffer output, AudioFeatures audioFeatures)
        {
            // Apply 5x5 convolution kernel
            int redSum = 0, greenSum = 0, blueSum = 0, alphaSum = 0;
            int validKernelSum = 0;
            
            // Get beat-reactive intensity
            float intensity = GetBeatReactiveIntensity(audioFeatures);
            
            // Process kernel
            for (int ky = -2; ky <= 2; ky++)
            {
                for (int kx = -2; kx <= 2; kx++)
                {
                    int sampleX = x + kx;
                    int sampleY = y + ky;
                    
                    // Check bounds
                    if (sampleX >= 0 && sampleX < width && sampleY >= 0 && sampleY < height)
                    {
                        int samplePixel = input.GetPixel(sampleX, sampleY);
                        int kernelValue = convolutionKernel[ky + 2, kx + 2];
                        
                        // Extract color channels
                        int r = (samplePixel >> 16) & 0xFF;
                        int g = (samplePixel >> 8) & 0xFF;
                        int b = samplePixel & 0xFF;
                        int a = (samplePixel >> 24) & 0xFF;
                        
                        redSum += r * kernelValue;
                        greenSum += g * kernelValue;
                        blueSum += b * kernelValue;
                        alphaSum += a * kernelValue;
                        validKernelSum += kernelValue;
                    }
                }
            }
            
            // Normalize result
            if (validKernelSum > 0)
            {
                int red = redSum / validKernelSum;
                int green = greenSum / validKernelSum;
                int blue = blueSum / validKernelSum;
                int alpha = alphaSum / validKernelSum;
                
                // Apply rounding mode if enabled
                if (RoundingMode)
                {
                    red = ApplyRounding(red);
                    green = ApplyRounding(green);
                    blue = ApplyRounding(blue);
                    alpha = ApplyRounding(alpha);
                }
                
                // Apply beat-reactive intensity
                if (BeatReactive && intensity != 1.0f)
                {
                    red = (int)(red * intensity);
                    green = (int)(green * intensity);
                    blue = (int)(blue * intensity);
                    alpha = (int)(alpha * intensity);
                }
                
                // Clamp values
                red = Math.Clamp(red, 0, 255);
                green = Math.Clamp(green, 0, 255);
                blue = Math.Clamp(blue, 0, 255);
                alpha = Math.Clamp(alpha, 0, 255);
                
                // Combine channels
                int resultPixel = (alpha << 24) | (red << 16) | (green << 8) | blue;
                output.SetPixel(x, y, resultPixel);
            }
            else
            {
                // Return original color if no valid samples
                output.SetPixel(x, y, input.GetPixel(x, y));
            }
        }
        
        private void ApplyConvolutionSingleThreaded(ImageBuffer input, ImageBuffer output, int width, int height, AudioFeatures audioFeatures)
        {
            for (int y = 0; y < height; y++)
            {
                ProcessRow(y, width, height, input, output, audioFeatures);
            }
        }
        
        private void ApplyConvolutionKernelSIMD(ImageBuffer input, ImageBuffer output, AudioFeatures audioFeatures)
        {
            int width = input.Width;
            int height = input.Height;
            
            // Use Vector4 for SIMD processing
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x += Vector<float>.Count)
                {
                    ProcessPixelBatchSIMD(x, y, width, height, input, output, audioFeatures);
                }
            }
        }
        
        private void ProcessPixelBatchSIMD(int startX, int y, int width, int height, ImageBuffer input, ImageBuffer output, AudioFeatures audioFeatures)
        {
            // Process pixels using Vector4 for SIMD optimization
            for (int i = 0; i < Vector<float>.Count && startX + i < width; i++)
            {
                int resultPixel = ApplyConvolutionToPixelSIMD(startX + i, y, width, height, input, audioFeatures);
                output.SetPixel(startX + i, y, resultPixel);
            }
        }
        
        private int ApplyConvolutionToPixelSIMD(int x, int y, int width, int height, ImageBuffer input, AudioFeatures audioFeatures)
        {
            Vector4 colorSum = Vector4.Zero;
            float weightSum = 0;
            
            // Get beat-reactive intensity
            float intensity = GetBeatReactiveIntensity(audioFeatures);
            
            // Apply kernel using SIMD operations
            for (int ky = -2; ky <= 2; ky++)
            {
                for (int kx = -2; kx <= 2; kx++)
                {
                    int sampleX = x + kx;
                    int sampleY = y + ky;
                    
                    if (sampleX >= 0 && sampleX < width && sampleY >= 0 && sampleY < height)
                    {
                        int samplePixel = input.GetPixel(sampleX, sampleY);
                        float weight = convolutionKernel[ky + 2, kx + 2] / (float)kernelSum;
                        
                        // Extract color channels and normalize to 0-1
                        Vector4 sampleVector = new Vector4(
                            ((samplePixel >> 16) & 0xFF) / 255.0f,
                            ((samplePixel >> 8) & 0xFF) / 255.0f,
                            (samplePixel & 0xFF) / 255.0f,
                            ((samplePixel >> 24) & 0xFF) / 255.0f
                        );
                        
                        colorSum += sampleVector * weight;
                        weightSum += weight;
                    }
                }
            }
            
            // Normalize and convert back to ARGB int
            if (weightSum > 0)
            {
                Vector4 normalizedColor = colorSum / weightSum;
                
                // Apply beat-reactive intensity
                if (BeatReactive && intensity != 1.0f)
                {
                    normalizedColor *= intensity;
                }
                
                int red = (int)(Math.Clamp(normalizedColor.X, 0, 1) * 255);
                int green = (int)(Math.Clamp(normalizedColor.Y, 0, 1) * 255);
                int blue = (int)(Math.Clamp(normalizedColor.Z, 0, 1) * 255);
                int alpha = (int)(Math.Clamp(normalizedColor.W, 0, 1) * 255);
                
                // Combine channels into ARGB int
                return (alpha << 24) | (red << 16) | (green << 8) | blue;
            }
            
            return input.GetPixel(x, y);
        }
        
        private float GetBeatReactiveIntensity(AudioFeatures audioFeatures)
        {
            if (!BeatReactive || audioFeatures == null) return 1.0f;
            
            if (audioFeatures.IsBeat)
            {
                return BeatIntensity;
            }
            
            // Could also use RMS or spectrum data for gradual intensity changes
            if (audioFeatures.Rms > 0.1f)
            {
                return 1.0f + (audioFeatures.Rms * (BeatIntensity - 1.0f));
            }
            
            return 1.0f;
        }
        
        private int ApplyRounding(int value)
        {
            // Apply rounding similar to MMX rounding mode
            if (RoundingMode)
            {
                // Add 128 for proper rounding (equivalent to MMX rounding)
                value += 128;
            }
            return value;
        }
        
        // Public setters for dynamic configuration
        public void SetBlurMode(int mode) => BlurMode = Math.Clamp(mode, 0, 1);
        public void SetBeatIntensity(float intensity) => BeatIntensity = Math.Clamp(intensity, 0.1f, 3.0f);
        public void SetThreadCount(int count) => ThreadCount = Math.Max(1, count);
        public void SetBatchSize(int size) => batchSize = Math.Max(1, size);
        
        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }
    }
}
