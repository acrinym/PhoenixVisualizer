using System; using System.Collections.Generic; using PhoenixVisualizer.Core.Effects.Models; using PhoenixVisualizer.Core.Models; namespace PhoenixVisualizer.Core.Effects.Nodes.AvsEffects { public class FeedbackEffectsNode : BaseEffectNode { public float FeedbackAmount { get; set; } = 0.5f; public float FeedbackDecay { get; set; } = 0.95f; public int MaxFeedbackFrames { get; set; } = 10; public bool BeatReactive { get; set; } = false; public float BeatFeedbackAmount { get; set; } = 0.8f; private readonly Queue<ImageBuffer> feedbackFrames = new Queue<ImageBuffer>(); public FeedbackEffectsNode() { Name = \
Feedback
Effects\; Description = \Creates
feedback
effects
by
blending
previous
frames\; Category = \AVS
Effects\; } protected override void InitializePorts() { _inputPorts.Add(new EffectPort(\Image\, typeof(ImageBuffer), true, null, \Input
image
for
feedback
processing\)); _outputPorts.Add(new EffectPort(\Output\, typeof(ImageBuffer), false, null, \Feedback
output
image\)); } protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures) { if (!inputs.TryGetValue(\Image\, out var input) || input is not ImageBuffer imageBuffer) return GetDefaultOutput(); var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height); float currentFeedbackAmount = FeedbackAmount; if (BeatReactive && audioFeatures?.IsBeat == true) { currentFeedbackAmount = BeatFeedbackAmount; } // Copy current frame to output Array.Copy(imageBuffer.Pixels, output.Pixels, imageBuffer.Pixels.Length); // Blend with feedback frames if (feedbackFrames.Count > 0) { float decay = 1.0f; foreach (var frame in feedbackFrames) { for (int i = 0; i < output.Pixels.Length; i++) { int currentPixel = output.Pixels[i]; int feedbackPixel = frame.Pixels[i]; int r1 = currentPixel & 0xFF; int g1 = (currentPixel >> 8) & 0xFF; int b1 = (currentPixel >> 16) & 0xFF; int r2 = feedbackPixel & 0xFF; int g2 = (feedbackPixel >> 8) & 0xFF; int b2 = (feedbackPixel >> 16) & 0xFF; int blendedR = (int)(r1 * (1.0f - currentFeedbackAmount * decay) + r2 * currentFeedbackAmount * decay); int blendedG = (int)(g1 * (1.0f - currentFeedbackAmount * decay) + g2 * currentFeedbackAmount * decay); int blendedB = (int)(b1 * (1.0f - currentFeedbackAmount * decay) + b2 * currentFeedbackAmount * decay); output.Pixels[i] = blendedR | (blendedG << 8) | (blendedB << 16); } decay *= FeedbackDecay; } } // Add current frame to feedback queue feedbackFrames.Enqueue(new ImageBuffer(imageBuffer)); if (feedbackFrames.Count > MaxFeedbackFrames) { feedbackFrames.Dequeue(); } return output; } protected override object GetDefaultOutput() { return new ImageBuffer(1, 1); } } }
