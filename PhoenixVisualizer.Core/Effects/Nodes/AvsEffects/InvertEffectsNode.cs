using System; using System.Collections.Generic; using PhoenixVisualizer.Core.Effects.Models; using PhoenixVisualizer.Core.Models; namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects { public class InvertEffectsNode : BaseEffectNode { public bool InvertRed { get; set; } = true; public bool InvertGreen { get; set; } = true; public bool InvertBlue { get; set; } = true; public bool BeatReactive { get; set; } = false; public InvertEffectsNode() { Name = \
Invert
Effects\; Description = \Inverts
specified
color
channels
of
the
image\; Category = \AVS
Effects\; } protected override void InitializePorts() { _inputPorts.Add(new EffectPort(\Image\, typeof(ImageBuffer), true, null, \Input
image
for
inversion\)); _outputPorts.Add(new EffectPort(\Output\, typeof(ImageBuffer), false, null, \Inverted
output
image\)); } protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures) { if (!inputs.TryGetValue(\Image\, out var input) || input is not ImageBuffer imageBuffer) return GetDefaultOutput(); var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height); for (int y = 0; y < imageBuffer.Height; y++) { for (int x = 0; x < imageBuffer.Width; x++) { int pixel = imageBuffer.GetPixel(x, y); int r = pixel & 0xFF; int g = (pixel >> 8) & 0xFF; int b = (pixel >> 16) & 0xFF; if (InvertRed) r = 255 - r; if (InvertGreen) g = 255 - g; if (InvertBlue) b = 255 - b; int newPixel = r | (g << 8) | (b << 16); output.SetPixel(x, y, newPixel); } } return output; } protected override object GetDefaultOutput() { return new ImageBuffer(1, 1); } } }
