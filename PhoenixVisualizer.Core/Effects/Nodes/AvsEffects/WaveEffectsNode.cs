using System; using System.Collections.Generic; using PhoenixVisualizer.Core.Effects.Models; using PhoenixVisualizer.Core.Models; namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects { public class WaveEffectsNode : BaseEffectNode { public float WaveAmplitude { get; set; } = 20.0f; public float WaveFrequency { get; set; } = 0.1f; public float WaveSpeed { get; set; } = 1.0f; public int WaveType { get; set; } = 0; public bool BeatReactive { get; set; } = false; public float BeatAmplitude { get; set; } = 2.0f; private float time = 0.0f; public WaveEffectsNode() { Name = \
Wave
Effects\; Description = \Creates
various
wave
distortions
on
the
image\; Category = \AVS
Effects\; } protected override void InitializePorts() { _inputPorts.Add(new EffectPort(\Image\, typeof(ImageBuffer), true, null, \Input
image
for
wave
effects\)); _outputPorts.Add(new EffectPort(\Output\, typeof(ImageBuffer), false, null, \Wave
effect
output
image\)); } protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures) { if (!inputs.TryGetValue(\Image\, out var input) || input is not ImageBuffer imageBuffer) return GetDefaultOutput(); var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height); float currentAmplitude = WaveAmplitude; if (BeatReactive && audioFeatures?.IsBeat == true) { currentAmplitude *= BeatAmplitude; } time += WaveSpeed * 0.1f; for (int y = 0; y < imageBuffer.Height; y++) { for (int x = 0; x < imageBuffer.Width; x++) { float waveOffset = CalculateWaveOffset(x, y, currentAmplitude, WaveFrequency, time, WaveType); int sourceX = x + (int)waveOffset; int sourceY = y; if (sourceX >= 0 && sourceX < imageBuffer.Width && sourceY >= 0 && sourceY < imageBuffer.Height) { int pixel = imageBuffer.GetPixel(sourceX, sourceY); output.SetPixel(x, y, pixel); } else { output.SetPixel(x, y, 0); } } } return output; } private float CalculateWaveOffset(int x, int y, float amplitude, float frequency, float time, int waveType) { float offset = 0.0f; switch (waveType) { case 0: // Sine wave offset = (float)Math.Sin(x * frequency + time) * amplitude; break; case 1: // Cosine wave offset = (float)Math.Cos(x * frequency + time) * amplitude; break; case 2: // Square wave offset = Math.Sign((float)Math.Sin(x * frequency + time)) * amplitude; break; case 3: // Triangle wave float phase = (x * frequency + time) % (2 * (float)Math.PI); if (phase < (float)Math.PI) { offset = (phase / (float)Math.PI) * amplitude; } else { offset = (2 - phase / (float)Math.PI) * amplitude; } break; } return offset; } protected override object GetDefaultOutput() { return new ImageBuffer(1, 1); } } }
