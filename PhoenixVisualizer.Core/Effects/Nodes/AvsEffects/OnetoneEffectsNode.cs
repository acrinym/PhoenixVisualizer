using System; using System.Collections.Generic; using PhoenixVisualizer.Core.Effects.Models; using PhoenixVisualizer.Core.Models; namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects { public class OnetoneEffectsNode : BaseEffectNode { public bool Enabled { get; set; } = true; public int TargetColor { get; set; } = 0xFFFFFF; public bool InvertLuminance { get; set; } = false; public int BlendMode { get; set; } = 0; public float BlendAmount { get; set; } = 1.0f; public bool BeatReactive { get; set; } = false; public float BeatBlendAmount { get; set; } = 1.5f; public OnetoneEffectsNode() { Name = \
Onetone
Effects\; Description = \Converts
image
to
single
color
tone
with
luminance
preservation\; Category = \AVS
Effects\; } protected override void InitializePorts() { _inputPorts.Add(new EffectPort(\Image\, typeof(ImageBuffer), true, null, \Input
image
for
onetone
processing\)); _outputPorts.Add(new EffectPort(\Output\, typeof(ImageBuffer), false, null, \Onetone
output
image\)); } protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures) { if (!inputs.TryGetValue(\Image\, out var input) || input is not ImageBuffer imageBuffer) return GetDefaultOutput(); if (!Enabled) return imageBuffer; var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height); float currentBlendAmount = BlendAmount; if (BeatReactive && audioFeatures?.IsBeat == true) { currentBlendAmount *= BeatBlendAmount; } int targetR = TargetColor & 0xFF; int targetG = (TargetColor >> 8) & 0xFF; int targetB = (TargetColor >> 16) & 0xFF; for (int y = 0; y < imageBuffer.Height; y++) { for (int x = 0; x < imageBuffer.Width; x++) { int pixel = imageBuffer.GetPixel(x, y); int r = pixel & 0xFF; int g = (pixel >> 8) & 0xFF; int b = (pixel >> 16) & 0xFF; float luminance = (r * 0.299f + g * 0.587f + b * 0.114f) / 255.0f; if (InvertLuminance) luminance = 1.0f - luminance; int newR = (int)(targetR * luminance); int newG = (int)(targetG * luminance); int newB = (int)(targetB * luminance); int newPixel = newR | (newG << 8) | (newB << 16); int finalPixel = BlendPixels(pixel, newPixel, currentBlendAmount, BlendMode); output.SetPixel(x, y, finalPixel); } } return output; } private int BlendPixels(int original, int newPixel, float amount, int mode) { if (amount >= 1.0f) return newPixel; if (amount <= 0.0f) return original; int r1 = original & 0xFF; int g1 = (original >> 8) & 0xFF; int b1 = (original >> 16) & 0xFF; int r2 = newPixel & 0xFF; int g2 = (newPixel >> 8) & 0xFF; int b2 = (newPixel >> 16) & 0xFF; int finalR, finalG, finalB; switch (mode) { case 1: // Add finalR = Math.Min(255, r1 + (int)(r2 * amount)); finalG = Math.Min(255, g1 + (int)(g2 * amount)); finalB = Math.Min(255, b1 + (int)(b2 * amount)); break; case 2: // Multiply finalR = (int)(r1 * (1.0f - amount) + r2 * amount); finalG = (int)(g1 * (1.0f - amount) + g2 * amount); finalB = (int)(b1 * (1.0f - amount) + b2 * amount); break; default: // Normal blend finalR = (int)(r1 * (1.0f - amount) + r2 * amount); finalG = (int)(g1 * (1.0f - amount) + g2 * amount); finalB = (int)(b1 * (1.0f - amount) + b2 * amount); break; } return finalR | (finalG << 8) | (finalB << 16); } protected override object GetDefaultOutput() { return new ImageBuffer(1, 1); } } }
