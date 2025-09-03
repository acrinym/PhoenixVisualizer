# Phoenix Visualizer App - Complete Technical Specification

**Project:** Phoenix Visualizer  
**Framework:** Avalonia UI (.NET/C#)  
**Target:** Cross-platform music visualizer with Phoenix theme  
**Status:** Ready for Codex/AI Development  
**Date:** 2025-08-13  

---

## 🎯 Project Overview

The Phoenix Visualizer is a cross-platform music visualization application that creates a living, animated phoenix whose appearance and behavior dynamically respond to music in real-time. The phoenix changes colors based on genre detection, animates according to BPM and frequency analysis, and provides a sophisticated visual experience that goes beyond simple audio reactivity.

### Core Philosophy
- **One vibe per track** - Each song gets a focused, genre-specific visual experience
- **Real-time audio analysis** - Live BPM, frequency, and energy detection
- **Phoenix as living entity** - The bird responds emotionally to music, not just technically
- **Cross-platform excellence** - Windows, Mac, and Linux support from day one

---

## 🏗️ Technical Architecture

### 1. Framework & Dependencies
```
Primary Framework: Avalonia UI (.NET 8+)
Graphics Engine: SkiaSharp
Audio Engine: BASS.NET (primary) / NAudio (fallback)
Metadata: TagLib# for ID3/audio tags
Configuration: Newtonsoft.Json
Platform: Cross-platform (Windows, macOS, Linux)
```

### 2. Project Structure
```
PhoenixVisualizer/
├── PhoenixVisualizer.Core/           # Core business logic
├── PhoenixVisualizer.Audio/          # Audio engine & analysis
├── PhoenixVisualizer.Visuals/        # Phoenix rendering & animation
├── PhoenixVisualizer.UI/             # Avalonia UI components
├── PhoenixVisualizer.Plugins/        # Plugin system (future)
├── PhoenixVisualizer.Tests/          # Unit tests
└── PhoenixVisualizer/                 # Main application
```

---

## 🎵 Audio Engine & Analysis

### 1. Music Playback
- **Supported Formats:** MP3 (primary), FLAC, WAV, OGG (stretch goals)
- **Playback Controls:** Play, Pause, Stop, Seek, Volume, Loop
- **Library Management:** Local file support, folder scanning, playlist support (Phase 2)

### 2. Real-Time Audio Analysis
- **FFT Analysis:** 1024 or 2048 frequency bands for real-time spectrum
- **BPM Detection:** Automatic BPM calculation on track load + continuous updates
- **Frequency Bands:** Bass (20-250 Hz), Mid (250-2000 Hz), Treble (2000-20000 Hz)
- **Energy Analysis:** Peak detection, dynamic range, quiet/loud moment identification
- **Audio Events:** Beat detection, drop detection, chorus identification

### 3. Genre Detection System
- **Primary Method:** ID3 tag reading (Genre field)
- **Fallback Method:** Frequency spectrum analysis for genre estimation
- **Custom Mappings:** User-configurable genre-to-color assignments
- **Hybrid Approach:** Combine tag data with spectral analysis for confidence scoring

---

## 🦅 Phoenix Visualizer Engine

### 1. Visual Design
- **Phoenix Anatomy:** Body, wings, tail, eyes, flame aura, particle effects
- **Rendering Method:** Vector graphics via SkiaSharp for smooth scaling
- **Animation States:** Idle, Active, Cocoon, Burst/Rebirth, Transition
- **Visual Effects:** Glow, flame trails, particle systems, color transitions

### 2. Animation System
- **State Machine:** Smooth transitions between animation states
- **BPM Integration:** Wing flap speed, movement tempo, energy bursts
- **Frequency Response:** Bass affects body glow, mids affect aura, treble affects details
- **Energy Mapping:** Volume changes trigger visual intensity shifts

### 3. Color & Vibe System
- **Genre-Based Colors:** Each genre gets a primary color palette
- **Frequency Fallback:** Real-world frequency-to-color mapping when genre unavailable
- **Dynamic Shading:** Subtle color variations based on energy and mood
- **Emotional Intelligence:** Colors reflect the emotional content of the music

### 4. Animation States
```
Idle State:
- Gentle wing flapping
- Soft glow
- Subtle breathing motion
- Calm, peaceful presence

Active State:
- Responsive wing movement
- Bright glow
- Dynamic flame effects
- Engaged with music

Cocoon State:
- Wings wrap around body
- Dimmed glow
- Minimal movement
- Triggered by quiet passages

Burst/Rebirth State:
- Wings spread dramatically
- Intense flame bursts
- Particle explosions
- Triggered by drops/chorus
```

---

## 🎨 UI/UX Design

### 1. Main Window Layout
```
┌─────────────────────────────────────────┐
│ Phoenix Visualizer                    │
├─────────────────────────────────────────┤
│                                         │
│           Phoenix Canvas                │
│         (Resizable, Fullscreen)        │
│                                         │
├─────────────────────────────────────────┤
│ Spectrum Visualizer                     │
│ [Real-time frequency bars]             │
├─────────────────────────────────────────┤
│ [Play] [Pause] [Stop] [Open File]     │
│ Track: [Title] Artist: [Artist]        │
│ Progress: [████████░░░░] [3:45/5:20]  │
│ Volume: [██████░░░░] [BPM: 128]       │
│ Genre: [Electronic] Vibe: [Neon]      │
└─────────────────────────────────────────┘
```

### 2. Controls & Information
- **Transport Controls:** Play, Pause, Stop, Previous, Next
- **File Management:** Open File, Open Folder, Recent Files
- **Track Information:** Title, Artist, Album, Duration, Progress
- **Audio Analysis Display:** BPM, Genre, Detected Vibe, Energy Level
- **Visual Settings:** Phoenix size, animation speed, effect intensity

### 3. Screensaver Mode
- **Activation:** Automatic after configurable idle time (default: 3 minutes)
- **Behavior:** Fullscreen, UI hidden, phoenix continues animating
- **Exit:** Any mouse/keyboard input returns to main window
- **Customization:** Background patterns, idle animations

---

## 🌈 Color & Genre Mapping System

### 1. Primary Genre Colors
| Genre | Primary Color | Secondary | Animation Style | Mood |
|-------|---------------|-----------|-----------------|------|
| Blues/Jazz | Deep Blue | Navy | Smooth, flowing | Contemplative |
| Bluegrass | Sky Blue | Light Blue | Lively, bouncy | Uplifting |
| Classical | Gold | Yellow | Elegant, graceful | Sophisticated |
| Metal | Purple | Deep Red | Sharp, aggressive | Intense |
| Love/Trance | Pink | Gold | Gentle, spiraling | Romantic |
| Hip Hop/Rap | Silver | Green | Rippling, rhythmic | Urban |
| Pop | Orange | Bright Yellow | Peppy, energetic | Fun |
| Electronic | Neon | Multi-color | Strobing, fast | Futuristic |
| Rock | Red | Orange | Powerful, driving | Energetic |
| Folk | Brown | Green | Organic, flowing | Natural |

### 2. Frequency-to-Color Fallback
When genre detection fails, use real-world frequency-to-light mapping:
- **20-250 Hz (Bass):** Red to Orange
- **250-2000 Hz (Mid):** Yellow to Green  
- **2000-20000 Hz (Treble):** Blue to Violet
- **Calculation:** Weighted average based on frequency energy distribution

### 3. Dynamic Color Adjustments
- **Energy Scaling:** Brighter colors for high-energy passages
- **Mood Shifting:** Subtle hue variations based on emotional content
- **Transition Smoothing:** Gradual color changes between sections
- **Contrast Management:** Ensure visibility across different backgrounds

---

## 🔧 Technical Implementation Details

### 1. Audio Processing Pipeline
```
Audio File → BASS.NET → FFT Analysis → Frequency Bands → BPM Detection
     ↓
Genre Detection → Color Assignment → Animation Parameters → Visual Engine
     ↓
Real-time Updates → Phoenix Animation → Spectrum Display → UI Updates
```

### 2. Performance Requirements
- **Target FPS:** 60 FPS minimum
- **Audio Latency:** <50ms from audio to visual response
- **Memory Usage:** <200MB for typical usage
- **CPU Usage:** <30% on mid-tier hardware
- **GPU Acceleration:** Optional, fallback to CPU rendering

### 3. Error Handling
- **File Format Support:** Graceful fallback for unsupported formats
- **Metadata Issues:** Default values when tags are missing/corrupt
- **Audio Engine Failures:** Fallback to system audio if BASS fails
- **Performance Degradation:** Automatic quality reduction if FPS drops

---

## 🚀 Development Phases

### Phase 1: Core Foundation (Weeks 1-4)
- [ ] Avalonia project setup and basic UI
- [ ] Audio playback with BASS.NET integration
- [ ] Basic FFT analysis and frequency band extraction
- [ ] Simple phoenix rendering (static image)
- [ ] Basic BPM detection

### Phase 2: Visual Engine (Weeks 5-8)
- [ ] Phoenix animation system
- [ ] Color mapping and genre detection
- [ ] Animation state machine
- [ ] Frequency-to-color fallback system
- [ ] Basic spectrum visualizer

### Phase 3: Polish & Features (Weeks 9-12)
- [ ] Screensaver mode
- [ ] Advanced animation effects
- [ ] Performance optimization
- [ ] Error handling and edge cases
- [ ] User configuration options

### Phase 4: Advanced Features (Weeks 13-16)
- [ ] Plugin system architecture
- [ ] Winamp AVS compatibility layer
- [ ] Advanced audio analysis
- [ ] Export/sharing features
- [ ] Community features

---

## 🔌 Winamp AVS Integration

### 1. Compatibility Goals
- **Superscope Support:** Basic superscope script execution
- **APE Compatibility:** Simple APE effect rendering
- **Plugin Architecture:** Modular system for custom effects
- **Legacy Support:** Import existing Winamp visualizations

### 2. Implementation Strategy
- **Scripting Engine:** Lua or JavaScript for superscope scripts
- **Effect Renderer:** Custom rendering pipeline for APE effects
- **Plugin Manager:** Load, manage, and execute AVS plugins
- **Performance Optimization:** Ensure smooth operation with multiple effects

### 3. Migration Path
- **Phase 1:** Basic superscope variable support
- **Phase 2:** APE effect rendering
- **Phase 3:** Full plugin compatibility
- **Phase 4:** Enhanced features beyond Winamp capabilities

---

## 🎯 Codex Development Instructions

### 1. Project Setup
```
1. Create new Avalonia project with .NET 8
2. Install required NuGet packages (BASS.NET, SkiaSharp, TagLib#)
3. Set up project structure with separate assemblies
4. Configure cross-platform build targets
```

### 2. Development Order
```
1. Audio Engine (BASS.NET integration, FFT analysis)
2. Basic UI (main window, controls, file handling)
3. Phoenix Rendering (basic graphics, animation system)
4. Audio-Visual Integration (real-time response)
5. Advanced Features (screensaver, effects, optimization)
```

### 3. Key Implementation Notes
- **Separation of Concerns:** Keep audio, visual, and UI logic separate
- **Performance First:** Optimize for 60 FPS from the start
- **Error Handling:** Implement robust error handling throughout
- **Testing:** Create unit tests for core audio analysis functions
- **Documentation:** Comment all complex algorithms and state machines

---

## 📋 Testing & Quality Assurance

### 1. Audio Testing
- **Format Support:** Test all supported audio formats
- **Analysis Accuracy:** Verify BPM and frequency detection
- **Performance:** Test with various file sizes and qualities
- **Edge Cases:** Very short/long files, corrupted metadata

### 2. Visual Testing
- **Animation Smoothness:** Ensure 60 FPS across different hardware
- **Color Accuracy:** Verify genre-to-color mappings
- **State Transitions:** Test all animation state changes
- **Responsiveness:** Audio-to-visual latency measurement

### 3. Cross-Platform Testing
- **Windows:** Windows 10/11 compatibility
- **macOS:** Latest macOS versions
- **Linux:** Ubuntu, Fedora, Arch Linux
- **Hardware:** Various GPU/CPU combinations

---

## 🎉 Success Metrics

### 1. Technical Goals
- ✅ 60 FPS animation performance
- ✅ <50ms audio-to-visual latency
- ✅ Cross-platform compatibility
- ✅ Professional-grade audio analysis

### 2. User Experience Goals
- ✅ Intuitive, beautiful interface
- ✅ Responsive, engaging animations
- ✅ Accurate genre and mood detection
- ✅ Smooth, professional operation

### 3. Development Goals
- ✅ Clean, maintainable codebase
- ✅ Comprehensive error handling
- ✅ Extensible plugin architecture
- ✅ Thorough testing coverage

---

## 🔮 Future Enhancements

### 1. Advanced Audio Features
- **AI Genre Detection:** Machine learning-based genre classification
- **Mood Analysis:** Emotional content analysis beyond genre
- **Beat Prediction:** Advanced beat and pattern recognition
- **Multi-track Support:** Layered visualization for complex audio

### 2. Visual Enhancements
- **3D Phoenix:** Three-dimensional phoenix model
- **Particle Systems:** Advanced particle effects and physics
- **Custom Themes:** User-created phoenix designs
- **Animation Presets:** Pre-built animation sequences

### 3. Social Features
- **Visualization Sharing:** Export and share phoenix moments
- **Community Effects:** User-created visual effects
- **Collaborative Projects:** Multi-user visualization sessions
- **Live Streaming:** Real-time visualization broadcasting

---

## 📚 Resources & References

### 1. Technical Documentation
- **Avalonia UI:** https://avaloniaui.net/
- **BASS.NET:** https://www.un4seen.com/
- **SkiaSharp:** https://github.com/mono/SkiaSharp
- **TagLib#:** https://github.com/mono/taglib-sharp

### 2. Audio Visualization References
- **Winamp AVS:** Advanced Visualization Studio documentation
- **FFT Analysis:** Fast Fourier Transform implementation guides
- **Audio Processing:** Real-time audio analysis techniques
- **Color Theory:** Frequency-to-color mapping research

### 3. Design Inspiration
- **Phoenix Mythology:** Cultural and spiritual references
- **Music Visualization:** Historical and contemporary examples
- **UI/UX Design:** Modern application design principles
- **Animation Principles:** Traditional and digital animation techniques

---

## 🚀 Getting Started

### 1. Prerequisites
- **Development Environment:** Visual Studio 2022 or VS Code
- **.NET SDK:** .NET 8.0 or later
- **Audio Files:** Test MP3 files for development
- **Graphics:** Basic understanding of 2D graphics programming

### 2. First Steps
```
1. Clone/create new Avalonia project
2. Install required NuGet packages
3. Set up basic project structure
4. Implement simple audio playback
5. Create basic phoenix rendering
6. Connect audio analysis to visuals
```

### 3. Development Tips
- **Start Simple:** Begin with basic functionality and iterate
- **Test Frequently:** Test audio analysis with various file types
- **Profile Performance:** Monitor FPS and optimize bottlenecks
- **Document Progress:** Keep detailed notes on implementation decisions

---

**This specification is ready for Codex/AI development. Each section provides detailed technical requirements that can be implemented incrementally, building toward the complete Phoenix Visualizer application.**

**The project represents a sophisticated blend of audio engineering, computer graphics, and user experience design, creating a unique and engaging music visualization tool that goes far beyond traditional visualizers.**

---

*Specification created from conversation with Onyx on 2025-08-13*  
*Ready for implementation with OpenAI Codex or similar AI development tools*
