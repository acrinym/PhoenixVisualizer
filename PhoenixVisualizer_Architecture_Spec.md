# PhoenixVisualizer Architecture Specification

## Project Overview
**PhoenixVisualizer** is a Windows audio visualizer application that completely bypasses VLC libraries and uses transpiled open-source components for MP3 decoding and audio visualization.

## Core Architecture

### 1. Audio Pipeline
```
MP3 File → LAME Decoder (C#) → PCM Data → NAudio → Speakers
```

**Components:**
- **LAME Decoder** (`LameDecoder.cs`) - Transpiled from C source, handles MP3 frame decoding
- **LAME Audio Processor** (`LameAudioProcessor.cs`) - Handles MP3 frame parsing and audio processing
- **LAME FFT** (`LameFFT.cs`) - Fast Fourier Transform for spectrum analysis
- **LAME Psychoacoustic Model** (`LamePsychoacousticModel.cs`) - Audio quality analysis
- **NAudio** - Handles audio output via DirectSound/WASAPI

### 2. Visualization Pipeline
```
PCM Data → Audio Features → Visualizer → SkiaSharp → Screen
```

**Components:**
- **NativeAudioService** - Orchestrates audio processing and feature extraction
- **VlcVisualizerManager** - Manages visualizer switching
- **Transpiled Visualizers:**
  - GOOM (`GoomVisualizer.cs`) - Psychedelic visualizer
  - ProjectM (`ProjectMVisualizer.cs`) - Milkdrop-style visualizer  
  - VSXu (`VsxuVisualizer.cs`) - Geometric visualizer
  - VLC Built-in (`VlcBuiltinVisualizer.cs`) - VLC's native effects

### 3. Project Structure
```
PhoenixVisualizer/
├── PhoenixVisualizer.Core/          # Core interfaces and data structures
├── PhoenixVisualizer.Audio/          # Legacy VLC audio service (bypassed)
├── PhoenixVisualizer.NativeAudio/    # New native audio system
│   ├── LameDecoder.cs               # Main MP3 decoder
│   ├── LameAudioProcessor.cs         # MP3 frame processing
│   ├── LameFFT.cs                   # FFT routines
│   ├── LamePsychoacousticModel.cs   # Audio analysis
│   ├── NativeAudioService.cs        # Main audio service
│   ├── DirectSoundPInvoke.cs        # Audio output (NAudio)
│   └── Visualizers/                 # Transpiled VLC visualizers
├── PhoenixVisualizer.App/            # Main application UI
└── PhoenixVisualizer.Visuals/        # Legacy visual components
```

## Technical Implementation

### Audio Processing Flow
1. **File Loading**: User selects MP3 file
2. **LAME Initialization**: Complete LAME decoder pipeline initializes
3. **Streaming Decode**: MP3 frames decoded frame-by-frame (4608 bytes each)
4. **PCM Conversion**: MP3 data converted to 16-bit PCM samples
5. **Audio Features**: Spectrum, waveform, RMS, peak, BPM calculated
6. **Audio Output**: PCM data sent to NAudio for speaker output
7. **Visualization**: Audio features drive visualizer rendering

### Key Data Structures
- **PCM Data**: 16-bit signed samples, 44.1kHz, stereo
- **Audio Features**: 1024-point FFT spectrum, waveform data, audio levels
- **Frame Processing**: 1152 samples per frame, ~26ms playback time

### Current Status
- ✅ **LAME Decoder**: Working perfectly (1507 frames processed)
- ✅ **Audio Features**: Spectrum/waveform data generated
- ✅ **Visualizers**: All transpiled and integrated
- ✅ **Time Tracking**: Duration calculation working (39.4 seconds)
- ❌ **Audio Output**: No audible sound (NAudio integration issue)
- ❌ **UI Feedback**: Position stuck at 0 (disposal timing issue)

## Problem Areas

### 1. Audio Output Issue
**Problem**: LAME produces perfect PCM data, but no sound comes through speakers
**Root Cause**: NAudio integration not properly configured
**Solution**: Fix NAudio BufferedWaveProvider setup

### 2. Premature Disposal
**Problem**: Audio service disposed while LAME still streaming
**Root Cause**: `PlayAudioFile` completes before streaming finishes
**Solution**: Proper async/await pattern for streaming completion

### 3. UI Time Display
**Problem**: Position shows 0:00, length shows 0:00
**Root Cause**: `GetPositionSeconds()` and `GetLengthSeconds()` not updating UI
**Solution**: Implement proper time tracking and UI updates

## Dependencies
- **NAudio 2.2.1** - Audio output
- **SkiaSharp 2.88.8** - Graphics rendering
- **Avalonia.Skia 11.0.10** - UI framework integration
- **MathNet.Numerics 5.0.0** - Mathematical operations

## Next Steps
1. Fix NAudio audio output (replace winmm.dll with proper NAudio)
2. Fix audio service disposal timing
3. Implement proper UI time updates
4. Test audible audio output
5. Optimize performance and memory usage

## Success Metrics
- ✅ No VLC crashes (completely bypassed)
- ✅ MP3 decoding working (LAME transpiled successfully)
- ✅ Visualizers working (all VLC visualizers transpiled)
- 🔄 Audio output audible (in progress)
- 🔄 UI time display working (in progress)
