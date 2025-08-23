using System; using System.Collections.Generic; using PhoenixVisualizer.Core.Effects.Models; using PhoenixVisualizer.Core.Models; namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects { public class ColorMapEffectsNode : BaseEffectNode { public int ColorMapType { get; set; } = 0; public float Intensity { get; set; } = 1.0f; public ColorMapEffectsNode() { Name = \
Color
Map
Effects\; Description = \Applies
color
mapping
transformations
to
images\; Category = \AVS
Effects\; } protected override void InitializePorts() { _inputPorts.Add(new EffectPort(\Image\, typeof(ImageBuffer), true, null, \Input
image
for
color
mapping\)); _outputPorts.Add(new EffectPort(\Output\, typeof(ImageBuffer), false, null, \Color
mapped
output
image\)); } protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures) { if (!inputs.TryGetValue(\Image\, out var input) || input is not ImageBuffer imageBuffer) return GetDefaultOutput(); var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height); for (int i = 0; i < imageBuffer.Pixels.Length; i++) { int pixel = imageBuffer.Pixels[i]; int mappedPixel = ApplyColorMap(pixel); output.Pixels[i] = mappedPixel; } return output; } private int ApplyColorMap(int pixel) { int r = pixel & 0xFF; int g = (pixel >> 8) & 0xFF; int b = (pixel >> 16) & 0xFF; switch (ColorMapType) { case 0: // Invert return (255 - r) | ((255 - g) << 8) | ((255 - b) << 16); case 1: // Grayscale int gray = (r + g + b) / 3; return gray | (gray << 8) | (gray << 16); case 2: // Sepia int tr = (int)((r * 0.393) + (g * 0.769) + (b * 0.189)); int tg = (int)((r * 0.349) + (g * 0.686) + (b * 0.168)); int tb = (int)((r * 0.272) + (g * 0.534) + (b * 0.131)); tr = Math.Clamp(tr, 0, 255); tg = Math.Clamp(tg, 0, 255); tb = Math.Clamp(tb, 0, 255); return tr | (tg << 8) | (tb << 16); default: return pixel; } } protected override object GetDefaultOutput() { return new ImageBuffer(1, 1); } } }
