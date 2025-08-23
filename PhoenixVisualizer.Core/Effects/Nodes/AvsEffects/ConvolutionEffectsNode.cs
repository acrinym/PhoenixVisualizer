using System; using System.Collections.Generic; using PhoenixVisualizer.Core.Effects.Models; using PhoenixVisualizer.Core.Models; namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects { public class ConvolutionEffectsNode : BaseEffectNode { public float[,] Kernel { get; set; } = new float[3, 3] { { 0.0625f, 0.125f, 0.0625f }, { 0.125f, 0.25f, 0.125f }, { 0.0625f, 0.125f, 0.0625f } }; public int KernelSize { get; set; } = 3; public float Divisor { get; set; } = 1.0f; public float Offset { get; set; } = 0.0f; public ConvolutionEffectsNode() { Name = \
Convolution
Effects\; Description = \Applies
convolution
kernels
for
advanced
image
filtering\; Category = \AVS
Effects\; } protected override void InitializePorts() { _inputPorts.Add(new EffectPort(\Image\, typeof(ImageBuffer), true, null, \Input
image
for
convolution\)); _outputPorts.Add(new EffectPort(\Output\, typeof(ImageBuffer), false, null, \Convolved
output
image\)); } protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures) { if (!inputs.TryGetValue(\Image\, out var input) || input is not ImageBuffer imageBuffer) return GetDefaultOutput(); var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height); int halfKernel = KernelSize / 2; for (int y = halfKernel; y < imageBuffer.Height - halfKernel; y++) { for (int x = halfKernel; x < imageBuffer.Width - halfKernel; x++) { float r = 0, g = 0, b = 0; for (int ky = 0; ky < KernelSize; ky++) { for (int kx = 0; kx < KernelSize; kx++) { int pixelX = x + kx - halfKernel; int pixelY = y + ky - halfKernel; int pixel = imageBuffer.GetPixel(pixelX, pixelY); float weight = Kernel[ky, kx]; r += (pixel & 0xFF) * weight; g += ((pixel >> 8) & 0xFF) * weight; b += ((pixel >> 16) & 0xFF) * weight; } } r = Math.Clamp((r / Divisor) + Offset, 0, 255); g = Math.Clamp((g / Divisor) + Offset, 0, 255); b = Math.Clamp((b / Divisor) + Offset, 0, 255); output.SetPixel(x, y, (int)r | ((int)g << 8) | ((int)b << 16)); } } return output; } protected override object GetDefaultOutput() { return new ImageBuffer(1, 1); } } }
