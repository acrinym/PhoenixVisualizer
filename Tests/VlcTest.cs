using System;
using System.Diagnostics;
using System.IO;
using LibVLCSharp.Shared;
using PhoenixVisualizer.Audio;
using PhoenixVisualizer.Visuals;
using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer;

/// <summary>
/// Test program for VLC audio integration and visualizer data flow
/// </summary>
public class VlcTest
{
    private VlcAudioService? _audioService;
    private VlcAudioTestVisualizer? _testVisualizer;
    private bool _isRunning = false;

    public void RunTest()
    {
        Console.WriteLine("=== VLC Audio Integration Test ===");
        Console.WriteLine("Testing VLC audio service and visualizer data flow...");
        
        try
        {
            // Initialize VLC audio service
            Console.WriteLine("1. Initializing VLC Audio Service...");
            _audioService = new VlcAudioService();
            Console.WriteLine("   ✓ VLC Audio Service initialized");
            
            // Initialize test visualizer
            Console.WriteLine("2. Initializing Test Visualizer...");
            _testVisualizer = new VlcAudioTestVisualizer();
            _testVisualizer.Initialize(800, 600);
            Console.WriteLine("   ✓ Test Visualizer initialized");
            
            // Test with sample audio file
            var testAudioFile = Path.Combine("libs_etc", "Come home Amanda (1).mp3");
            if (File.Exists(testAudioFile))
            {
                Console.WriteLine($"3. Testing with audio file: {testAudioFile}");
                
                // Open and play the file
                if (_audioService.Open(testAudioFile))
                {
                    Console.WriteLine("   ✓ Audio file opened successfully");
                    
                    // Start playback
                    if (_audioService.Play())
                    {
                        Console.WriteLine("   ✓ Playback started");
                        _isRunning = true;
                        
                        // Test audio data flow
                        TestAudioDataFlow();
                    }
                    else
                    {
                        Console.WriteLine("   ✗ Failed to start playback");
                    }
                }
                else
                {
                    Console.WriteLine("   ✗ Failed to open audio file");
                }
            }
            else
            {
                Console.WriteLine($"3. Test audio file not found: {testAudioFile}");
                Console.WriteLine("   Creating simulated audio data for testing...");
                TestWithSimulatedData();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during test: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine("\n=== Test Complete ===");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private void TestAudioDataFlow()
    {
        Console.WriteLine("\n4. Testing Audio Data Flow...");
        
        var testDuration = TimeSpan.FromSeconds(10);
        var startTime = DateTime.Now;
        var frameCount = 0;
        
        Console.WriteLine($"   Testing for {testDuration.TotalSeconds} seconds...");
        
        while (_isRunning && (DateTime.Now - startTime) < testDuration)
        {
            try
            {
                // Get audio data from VLC service
                var spectrumData = _audioService?.GetSpectrumData();
                var waveformData = _audioService?.GetWaveformData();
                
                if (spectrumData != null && waveformData != null)
                {
                    // Create test audio features
                    var audioFeatures = AudioFeaturesImpl.CreateEnhanced(
                        spectrumData,
                        waveformData,
                        0.5f, // RMS
                        120.0, // BPM
                        false,  // Beat
                        0.0     // Time
                    );
                    
                    // Test visualizer rendering (simulated)
                    if (_testVisualizer != null)
                    {
                        // Create a mock canvas for testing
                        var mockCanvas = new MockCanvas();
                        _testVisualizer.RenderFrame(audioFeatures, mockCanvas);
                        
                        frameCount++;
                        
                        // Log data every 100 frames
                        if (frameCount % 100 == 0)
                        {
                            var fftSum = spectrumData.Sum(f => MathF.Abs(f));
                            var waveSum = waveformData.Sum(w => MathF.Abs(w));
                            var fftNonZero = spectrumData.Count(f => MathF.Abs(f) > 0.001f);
                            var waveNonZero = waveformData.Count(w => MathF.Abs(w) > 0.001f);
                            
                            Console.WriteLine($"   Frame {frameCount}: FFT[{fftSum:F6}, {fftNonZero}], Wave[{waveSum:F6}, {waveNonZero}]");
                        }
                    }
                }
                
                // Small delay to simulate real-time rendering
                System.Threading.Thread.Sleep(16); // ~60 FPS
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Error during frame {frameCount}: {ex.Message}");
            }
        }
        
        Console.WriteLine($"   ✓ Completed {frameCount} frames");
        Console.WriteLine($"   Average FPS: {frameCount / testDuration.TotalSeconds:F1}");
    }

    private void TestWithSimulatedData()
    {
        Console.WriteLine("\n4. Testing with Simulated Audio Data...");
        
        var frameCount = 0;
        var testDuration = TimeSpan.FromSeconds(5);
        var startTime = DateTime.Now;
        
        while ((DateTime.Now - startTime) < testDuration)
        {
            try
            {
                // Generate simulated audio data
                var spectrumData = GenerateSimulatedSpectrumData();
                var waveformData = GenerateSimulatedWaveformData();
                
                // Create test audio features
                var audioFeatures = AudioFeaturesImpl.CreateEnhanced(
                    spectrumData,
                    waveformData,
                    0.3f,  // RMS
                    120.0,  // BPM
                    false,   // Beat
                    0.0     // Time
                );
                
                // Test visualizer rendering
                if (_testVisualizer != null)
                {
                    var mockCanvas = new MockCanvas();
                    _testVisualizer.RenderFrame(audioFeatures, mockCanvas);
                    
                    frameCount++;
                    
                    if (frameCount % 50 == 0)
                    {
                        Console.WriteLine($"   Frame {frameCount}: Simulated data rendered");
                    }
                }
                
                System.Threading.Thread.Sleep(16);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Error during simulated frame {frameCount}: {ex.Message}");
            }
        }
        
        Console.WriteLine($"   ✓ Completed {frameCount} simulated frames");
    }

    private float[] GenerateSimulatedSpectrumData()
    {
        var data = new float[2048];
        var time = DateTime.Now.Ticks * 0.0001;
        
        for (int i = 0; i < data.Length; i++)
        {
            var frequency = i / (float)data.Length;
            data[i] = (float)(Math.Sin(frequency * Math.PI * 4 + time) * 0.5 + 0.5);
        }
        
        return data;
    }

    private float[] GenerateSimulatedWaveformData()
    {
        var data = new float[2048];
        var time = DateTime.Now.Ticks * 0.0001;
        
        for (int i = 0; i < data.Length; i++)
        {
            var t = i / (float)data.Length;
            data[i] = (float)(Math.Sin(t * Math.PI * 8 + time) * 0.6);
        }
        
        return data;
    }

    public void Cleanup()
    {
        _isRunning = false;
        _testVisualizer?.Dispose();
        _audioService?.Dispose();
    }
}

/// <summary>
/// Mock canvas for testing visualizer rendering
/// </summary>
public class MockCanvas : ISkiaCanvas
{
    public void Clear(uint color) { }
    public void DrawText(string text, float x, float y, uint color, float size) { }
    public void DrawLine(float x1, float y1, float x2, float y2, uint color, float thickness) { }
    public void DrawRect(float x, float y, float width, float height, uint color) { }
    public void DrawLines(Span<(float x, float y)> points, float thickness, uint color) { }
    public void DrawCircle(float x, float y, float radius, uint color) { }
    public void DrawEllipse(float x, float y, float width, float height, uint color) { }
    public void DrawPolygon(Span<(float x, float y)> points, uint color) { }
    public void DrawPath(Span<(float x, float y)> points, uint color, float thickness) { }
    public void DrawImage(byte[] imageData, float x, float y, float width, float height) { }
    public void SetTransform(float m11, float m12, float m21, float m22, float m31, float m32) { }
    public void ResetTransform() { }
    public void PushClip(float x, float y, float width, float height) { }
    public void PopClip() { }
    public void SaveState() { }
    public void RestoreState() { }
}
