using System; using System.Collections.Generic; using PhoenixVisualizer.Core.Effects.Models; using PhoenixVisualizer.Core.Models; namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects { public class TransEffectsNode : BaseEffectNode { public float TransitionProgress { get; set; } = 0.0f; public int TransitionType { get; set; } = 0; public TransEffectsNode() { Name = \
Trans
Effects\; Description = \Creates
smooth
transitions
between
visual
states\; Category = \AVS
Effects\; } protected override void InitializePorts() { _inputPorts.Add(new EffectPort(\Image1\, typeof(ImageBuffer), true, null, \First
image
for
transition\)); _inputPorts.Add(new EffectPort(\Image2\, typeof(ImageBuffer), true, null, \Second
image
for
transition\)); _outputPorts.Add(new EffectPort(\Output\, typeof(ImageBuffer), false, null, \Transitioned
output
image\)); } protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures) { if (!inputs.TryGetValue(\Image1\, out var img1) || img1 is not ImageBuffer image1) return GetDefaultOutput(); if (!inputs.TryGetValue(\Image2\, out var img2) || img2 is not ImageBuffer image2) return image1; var output = new ImageBuffer(image1.Width, image1.Height); for (int i = 0; i < image1.Pixels.Length; i++) { int pixel1 = image1.Pixels[i]; int pixel2 = image2.Pixels[i]; int blendedPixel = BlendPixels(pixel1, pixel2, TransitionProgress); output.Pixels[i] = blendedPixel; } return output; } private int BlendPixels(int pixel1, int pixel2, float progress) { int r1 = pixel1 & 0xFF; int g1 = (pixel1 >> 8) & 0xFF; int b1 = (pixel1 >> 16) & 0xFF; int r2 = pixel2 & 0xFF; int g2 = (pixel2 >> 8) & 0xFF; int b2 = (pixel2 >> 16) & 0xFF; int r = (int)(r1 * (1 - progress) + r2 * progress); int g = (int)(g1 * (1 - progress) + g2 * progress); int b = (int)(b1 * (1 - progress) + b2 * progress); return r | (g << 8) | (b << 16); } protected override object GetDefaultOutput() { return new ImageBuffer(1, 1); } } }
