using System; using System.Collections.Generic; using PhoenixVisualizer.Core.Effects.Models; using PhoenixVisualizer.Core.Models; namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects { public class ClearFrameEffectsNode : BaseEffectNode { public int ClearColor { get; set; } = 0; public bool BeatReactive { get; set; } = false; public int BeatClearColor { get; set; } = 0xFFFFFF; public ClearFrameEffectsNode() { Name = \
Clear
Frame
Effects\; Description = \Clears
the
frame
with
specified
color
or
beat-reactive
colors\; Category = \AVS
Effects\; } protected override void InitializePorts() { _inputPorts.Add(new EffectPort(\Image\, typeof(ImageBuffer), true, null, \Input
image
for
clearing\)); _outputPorts.Add(new EffectPort(\Output\, typeof(ImageBuffer), false, null, \Cleared
output
image\)); } protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures) { if (!inputs.TryGetValue(\Image\, out var input) || input is not ImageBuffer imageBuffer) return GetDefaultOutput(); var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height); int clearColor = BeatReactive && audioFeatures?.IsBeat == true ? BeatClearColor : ClearColor; output.Clear(clearColor); return output; } protected override object GetDefaultOutput() { return new ImageBuffer(1, 1); } } }
