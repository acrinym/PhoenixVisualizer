using System; using System.Collections.Generic; using PhoenixVisualizer.Core.Effects.Models; using PhoenixVisualizer.Core.Models; namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects { public class MirrorEffectsNode : BaseEffectNode { public int MirrorMode { get; set; } = 1; public bool OnBeat { get; set; } = false; public bool Smooth { get; set; } = false; public int Slower { get; set; } = 4; public MirrorEffectsNode() { Name = \
Mirror
Effects\; Description = \Creates
symmetrical
reflections
across
horizontal
and
vertical
axes\; Category = \AVS
Effects\; } protected override void InitializePorts() { _inputPorts.Add(new EffectPort(\Image\, typeof(ImageBuffer), true, null, \Input
image
for
mirroring\)); _outputPorts.Add(new EffectPort(\Output\, typeof(ImageBuffer), false, null, \Mirrored
output
image\)); } protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures) { if (!inputs.TryGetValue(\Image\, out var input) || input is not ImageBuffer imageBuffer) return GetDefaultOutput(); var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height); switch (MirrorMode) { case 1: // Horizontal mirror for (int y = 0; y < imageBuffer.Height; y++) { for (int x = 0; x < imageBuffer.Width; x++) { int sourceX = x < imageBuffer.Width / 2 ? x : imageBuffer.Width - 1 - x; int pixel = imageBuffer.GetPixel(sourceX, y); output.SetPixel(x, y, pixel); } } break; case 2: // Vertical mirror for (int y = 0; y < imageBuffer.Height; y++) { for (int x = 0; x < imageBuffer.Width; x++) { int sourceY = y < imageBuffer.Height / 2 ? y : imageBuffer.Height - 1 - y; int pixel = imageBuffer.GetPixel(x, sourceY); output.SetPixel(x, y, pixel); } } break; case 3: // Both mirrors for (int y = 0; y < imageBuffer.Height; y++) { for (int x = 0; x < imageBuffer.Width; x++) { int sourceX = x < imageBuffer.Width / 2 ? x : imageBuffer.Width - 1 - x; int sourceY = y < imageBuffer.Height / 2 ? y : imageBuffer.Height - 1 - y; int pixel = imageBuffer.GetPixel(sourceX, sourceY); output.SetPixel(x, y, pixel); } } break; } return output; } protected override object GetDefaultOutput() { return new ImageBuffer(1, 1); } } }
