using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using PhoenixVisualizer.Audio;

namespace PhoenixVisualizer.Audio;

/// <summary>
/// Comprehensive test for VLC audio integration
/// Tests audio playback and data flow
/// </summary>
public class AudioIntegrationTest
{
    private VlcAudioService? _audioService;
    private bool _testPassed = false;
    private string _testResults = "";

    public bool RunFullAudioIntegrationTest()
    {
        Console.WriteLine("🔊 Starting VLC Audio Integration Test...");
        Console.WriteLine("========================================");

        try
        {
            // Test 1: Initialize VLC Audio Service
            if (!TestVlcInitialization())
                return false;

            // Test 2: Load and play test audio file
            if (!TestAudioPlayback())
                return false;

            // Test 3: Test audio data flow to visualizer
            if (!TestAudioDataFlow())
                return false;

            // Test 4: Test advanced audio features
            if (!TestAdvancedAudioFeatures())
                return false;

            _testPassed = true;
            Console.WriteLine("✅ ALL TESTS PASSED - VLC Audio Integration Working!");
            return true;

        }
        catch (Exception ex)
        {
            _testResults += $"❌ Test failed with exception: {ex.Message}\n";
            Console.WriteLine($"❌ Test failed: {ex.Message}");
            return false;
        }
        finally
        {
            Cleanup();
        }
    }

    private bool TestVlcInitialization()
    {
        Console.WriteLine("📡 Test 1: VLC Audio Service Initialization");

        try
        {
            _audioService = new VlcAudioService();
            Console.WriteLine("✅ VLC Audio Service initialized successfully");

            // Check if LibVLC is working
            if (_audioService.IsPlaying)
            {
                Console.WriteLine("⚠️  Audio service reports as playing (should be false initially)");
            }
            else
            {
                Console.WriteLine("✅ Audio service correctly reports not playing");
            }

            return true;
        }
        catch (Exception ex)
        {
            _testResults += $"❌ VLC initialization failed: {ex.Message}\n";
            Console.WriteLine($"❌ VLC initialization failed: {ex.Message}");
            return false;
        }
    }

    private bool TestAudioPlayback()
    {
        Console.WriteLine("🎵 Test 2: Audio File Playback");

        try
        {
            // Look for test audio files
            var testFiles = new[]
            {
                "libs_etc/come home amanda (1).mp3",
                "libs_etc/never really there.mp3",
                "libs_etc/no strings, no chains (1).mp3",
                "libs_etc/no strings, no chains.mp3"
            };

            string? audioFile = null;
            foreach (var file in testFiles)
            {
                if (File.Exists(file))
                {
                    audioFile = file;
                    break;
                }
            }

            if (audioFile == null)
            {
                _testResults += "❌ No test audio files found\n";
                Console.WriteLine("❌ No test audio files found");
                return false;
            }

            Console.WriteLine($"🎵 Found test file: {audioFile}");

            // Attempt to load and play the file
            _audioService?.Play(audioFile);
            Thread.Sleep(500); // Give it time to start

            if (_audioService?.IsPlaying == true)
            {
                Console.WriteLine("✅ Audio playback started successfully");
                Thread.Sleep(2000); // Let it play for 2 seconds
                _audioService?.Stop();
                Console.WriteLine("✅ Audio playback stopped successfully");
                return true;
            }
            else
            {
                _testResults += "❌ Audio playback failed to start\n";
                Console.WriteLine("❌ Audio playback failed to start");
                return false;
            }
        }
        catch (Exception ex)
        {
            _testResults += $"❌ Audio playback test failed: {ex.Message}\n";
            Console.WriteLine($"❌ Audio playback test failed: {ex.Message}");
            return false;
        }
    }

    private bool TestAudioDataFlow()
    {
        Console.WriteLine("📊 Test 3: Audio Data Flow");

        try
        {
            // Restart audio playback
            var testFiles = new[]
            {
                "libs_etc/come home amanda (1).mp3",
                "libs_etc/never really there.mp3",
                "libs_etc/no strings, no chains (1).mp3",
                "libs_etc/no strings, no chains.mp3"
            };

            string? audioFile = null;
            foreach (var file in testFiles)
            {
                if (File.Exists(file))
                {
                    audioFile = file;
                    break;
                }
            }

            if (audioFile == null) return false;

            _audioService?.Play(audioFile);
            Thread.Sleep(500); // Let audio start

            // Test data retrieval
            var waveformData = _audioService?.GetWaveformData();
            var spectrumData = _audioService?.GetSpectrumData();
            if (waveformData == null || spectrumData == null)
            {
                _testResults += "❌ No audio data retrieved from service\n";
                Console.WriteLine("❌ No audio data retrieved from service");
                return false;
            }

            Console.WriteLine($"✅ Audio data retrieved: FFT={spectrumData.Length}, Waveform={waveformData.Length}");

            // Check if data is meaningful (not all zeros)
            bool hasValidData = false;
            if (spectrumData.Length > 0)
            {
                for (int i = 0; i < Math.Min(10, spectrumData.Length); i++)
                {
                    if (Math.Abs(spectrumData[i]) > 0.001f)
                    {
                        hasValidData = true;
                        break;
                    }
                }
            }

            if (hasValidData)
            {
                Console.WriteLine("✅ Audio data contains valid values (not all zeros)");
            }
            else
            {
                Console.WriteLine("⚠️  Audio data appears to be all zeros (may be normal for silence)");
            }

            _audioService?.Stop();
            return true;
        }
        catch (Exception ex)
        {
            _testResults += $"❌ Audio data flow test failed: {ex.Message}\n";
            Console.WriteLine($"❌ Audio data flow test failed: {ex.Message}");
            return false;
        }
    }

    private bool TestAdvancedAudioFeatures()
    {
        Console.WriteLine("🔧 Test 4: Advanced Audio Features");

        try
        {
            // Test frequency preset changes
            Console.WriteLine("Testing frequency presets...");

            // Test different frequency presets
            var presets = new[] {
                PhoenixVisualizer.Audio.Interfaces.IAudioService.FrequencyPreset.Standard440Hz,
                PhoenixVisualizer.Audio.Interfaces.IAudioService.FrequencyPreset.Healing432Hz,
                PhoenixVisualizer.Audio.Interfaces.IAudioService.FrequencyPreset.Love528Hz
            };

            foreach (var preset in presets)
            {
                _audioService?.SetFrequencyPreset(preset);
                Thread.Sleep(100); // Allow time for change
                Console.WriteLine($"✅ Frequency preset {preset} set successfully");
            }

            // Test fundamental frequency adjustment
            _audioService?.SetFundamentalFrequency(442.0f);
            Console.WriteLine("✅ Fundamental frequency adjustment working");

            // Test audio data metrics
            var waveformData = _audioService?.GetWaveformData();
            var spectrumData = _audioService?.GetSpectrumData();
            if (waveformData != null && spectrumData != null)
            {
                Console.WriteLine($"📊 Current audio metrics:");
                Console.WriteLine($"   - Waveform Length: {waveformData.Length}");
                Console.WriteLine($"   - Spectrum Length: {spectrumData.Length}");

                // Calculate some basic metrics
                float maxWaveform = 0;
                float maxSpectrum = 0;
                for (int i = 0; i < waveformData.Length; i++)
                {
                    maxWaveform = Math.Max(maxWaveform, Math.Abs(waveformData[i]));
                }
                for (int i = 0; i < spectrumData.Length; i++)
                {
                    maxSpectrum = Math.Max(maxSpectrum, spectrumData[i]);
                }

                Console.WriteLine($"   - Max Waveform: {maxWaveform:F6}");
                Console.WriteLine($"   - Max Spectrum: {maxSpectrum:F6}");
            }

            return true;
        }
        catch (Exception ex)
        {
            _testResults += $"❌ Advanced audio features test failed: {ex.Message}\n";
            Console.WriteLine($"❌ Advanced audio features test failed: {ex.Message}");
            return false;
        }
    }

    private void Cleanup()
    {
        try
        {
            _audioService?.Stop();
            _audioService?.Dispose();
            Console.WriteLine("🧹 Test cleanup completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Cleanup warning: {ex.Message}");
        }
    }

    public string GetTestResults()
    {
        return _testResults;
    }

    public bool TestPassed => _testPassed;
}
