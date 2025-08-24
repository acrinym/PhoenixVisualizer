using System;
using System.Threading.Tasks;

namespace PhoenixVisualizer.Core.Services.AudioProcessing
{
    /// <summary>
    /// Simple test class to verify audio processing components compile correctly
    /// </summary>
    public static class AudioProcessingTest
    {
        /// <summary>
        /// Tests the FFT processor
        /// </summary>
        public static async Task TestFftProcessor()
        {
            try
            {
                using var fftProcessor = new FftProcessor(576, 44100);
                var testData = new float[576];
                
                // Fill with test data
                for (int i = 0; i < testData.Length; i++)
                {
                    testData[i] = (float)Math.Sin(2.0 * Math.PI * 440.0 * i / 44100.0);
                }
                
                var result = await fftProcessor.ProcessAsync(testData);
                Console.WriteLine($"FFT Test: Success - {result.FftSize} bins, {result.FrequencyResolution:F1} Hz resolution");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FFT Test: Failed - {ex.Message}");
            }
        }
        
        /// <summary>
        /// Tests the beat detector
        /// </summary>
        public static async Task TestBeatDetector()
        {
            try
            {
                using var beatDetector = new BeatDetector();
                var testData = new float[576];
                
                // Fill with test data
                for (int i = 0; i < testData.Length; i++)
                {
                    testData[i] = (float)Math.Sin(2.0 * Math.PI * 120.0 * i / 44100.0);
                }
                
                var result = await beatDetector.ProcessFrameAsync(testData, testData);
                Console.WriteLine($"Beat Detection Test: Success - Beat: {result}, BPM: {beatDetector.CurrentBPM:F1}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Beat Detection Test: Failed - {ex.Message}");
            }
        }
        
        /// <summary>
        /// Tests the channel processor
        /// </summary>
        public static async Task TestChannelProcessor()
        {
            try
            {
                using var channelProcessor = new ChannelProcessor(44100, 576);
                var testData = new float[1152]; // Stereo data
                
                // Fill with test data
                for (int i = 0; i < testData.Length; i += 2)
                {
                    testData[i] = (float)Math.Sin(2.0 * Math.PI * 440.0 * (i/2) / 44100.0); // Left
                    testData[i + 1] = (float)Math.Sin(2.0 * Math.PI * 880.0 * (i/2) / 44100.0); // Right
                }
                
                var (left, right) = channelProcessor.SeparateChannels(testData);
                var downsampled = channelProcessor.Downsample(left, 576);
                
                Console.WriteLine($"Channel Processing Test: Success - Left: {left.Length}, Right: {right.Length}, Downsampled: {downsampled.Length}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Channel Processing Test: Failed - {ex.Message}");
            }
        }
        
        /// <summary>
        /// Runs all tests
        /// </summary>
        public static async Task RunAllTests()
        {
            Console.WriteLine("Running Audio Processing Component Tests...");
            
            await TestFftProcessor();
            await TestBeatDetector();
            await TestChannelProcessor();
            
            Console.WriteLine("Audio Processing Component Tests Complete!");
        }
    }
}
