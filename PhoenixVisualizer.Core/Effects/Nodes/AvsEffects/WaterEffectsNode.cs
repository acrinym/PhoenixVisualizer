using System; using System.Collections.Generic; using PhoenixVisualizer.Core.Effects.Models; using PhoenixVisualizer.Core.Models; namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects { public class WaterEffectsNode : BaseEffectNode { public float WaveHeight { get; set; } = 10.0f; public float WaveSpeed { get; set; } = 1.0f; public float WaveLength { get; set; } = 50.0f; public bool BeatReactive { get; set; } = false; public float BeatWaveHeight { get; set; } = 20.0f; private float time = 0.0f; public WaterEffectsNode() { Name = \
Water
Effects\; Description = \Creates
realistic
water
ripple
and
wave
effects\; Category = \AVS
Effects\; } protected override void InitializePorts() { _inputPorts.Add(new EffectPort(\Image\, typeof(ImageBuffer), true, null, \Input
image
for
water
effects\)); _outputPorts.Add(new EffectPort(\Output\, typeof(ImageBuffer), false, null, \Water
effect
output
image\)); } protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures) { if (!inputs.TryGetValue(\Image\, out var input) || input is not ImageBuffer imageBuffer) return GetDefaultOutput(); var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height); float currentWaveHeight = WaveHeight; if (BeatReactive && audioFeatures?.IsBeat == true) { currentWaveHeight *= BeatWaveHeight; } time += WaveSpeed * 0.1f; for (int y = 0; y < imageBuffer.Height; y++) { for (int x = 0; x < imageBuffer.Width; x++) { float waveOffset = (float)Math.Sin((x + time * 10) / WaveLength) * currentWaveHeight; int sourceY = y + (int)waveOffset; if (sourceY >= 0 && sourceY < imageBuffer.Height) { int pixel = imageBuffer.GetPixel(x, sourceY); output.SetPixel(x, y, pixel); } else { output.SetPixel(x, y, 0); } } } return output; } protected override object GetDefaultOutput() { return new ImageBuffer(1, 1); } } }
