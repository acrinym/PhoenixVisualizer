using System; using System.Collections.Generic; using PhoenixVisualizer.Core.Effects.Models; using PhoenixVisualizer.Core.Models; namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects { public class ChannelShiftEffectsNode : BaseEffectNode { public int ShiftAmount { get; set; } = 10; public int Channel { get; set; } = 0; // 0=all, 1=red, 2=green, 3=blue public bool BeatReactive { get; set; } = false; public int BeatShiftAmount { get; set; } = 20; public ChannelShiftEffectsNode() { Name = \
Channel
Shift
Effects\; Description = \Shifts
color
channels
to
create
dynamic
color
effects\; Category = \AVS
Effects\; } protected override void InitializePorts() { _inputPorts.Add(new EffectPort(\Image\, typeof(ImageBuffer), true, null, \Input
image
for
channel
shifting\)); _outputPorts.Add(new EffectPort(\Output\, typeof(ImageBuffer), false, null, \Channel
shifted
output
image\)); } protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures) { if (!inputs.TryGetValue(\Image\, out var input) || input is not ImageBuffer imageBuffer) return GetDefaultOutput(); var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height); int currentShift = BeatReactive && audioFeatures?.IsBeat == true ? BeatShiftAmount : ShiftAmount; for (int y = 0; y < imageBuffer.Height; y++) { for (int x = 0; x < imageBuffer.Width; x++) { int sourceX = x - currentShift; int sourceY = y; if (sourceX < 0) sourceX = 0; if (sourceX >= imageBuffer.Width) sourceX = imageBuffer.Width - 1; int sourcePixel = imageBuffer.GetPixel(sourceX, sourceY); int targetPixel = imageBuffer.GetPixel(x, y); int shiftedPixel = ApplyChannelShift(sourcePixel, targetPixel, Channel); output.SetPixel(x, y, shiftedPixel); } } return output; } private int ApplyChannelShift(int sourcePixel, int targetPixel, int channel) { int r1 = sourcePixel & 0xFF; int g1 = (sourcePixel >> 8) & 0xFF; int b1 = (sourcePixel >> 16) & 0xFF; int r2 = targetPixel & 0xFF; int g2 = (targetPixel >> 8) & 0xFF; int b2 = (targetPixel >> 16) & 0xFF; switch (channel) { case 1: return r1 | (g2 << 8) | (b2 << 16); // Red channel shift case 2: return r2 | (g1 << 8) | (b2 << 16); // Green channel shift case 3: return r2 | (g2 << 8) | (b1 << 16); // Blue channel shift default: return r1 | (g1 << 8) | (b1 << 8); // All channels shift } } protected override object GetDefaultOutput() { return new ImageBuffer(1, 1); } } }
