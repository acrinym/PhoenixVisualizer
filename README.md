# Phoenix Visualizer

Cross-platform Avalonia app and visualizer studio. The centerpiece is a living, animated phoenix whose color and motion respond to music in real-time. Each track gets one primary vibe (genre-driven), nuanced by BPM, energy, and frequency bands. Includes a physics/real-world frequency-to-visible-color fallback when genre is missing.

## Features (MVP)

- Music playback: Open file, Play/Pause, Stop, Seek, Volume (MP3 first)
- Real-time analysis: FFT (1024/2048), BPM detection, energy/peaks
- Genre detection: Primary ID3 tag, fallback via spectrum color mapping
- Phoenix visualizer: One vibe per track; animation and effects respond to audio
- Spectrum visualizer: Real-time bars/curve, color-coded to frequency→visible light
- Screensaver mode: Fullscreen on idle; exits on input

## Color and Vibe Logic

- One primary vibe per track (keeps the experience focused and code simple)
- Genre → base palette and animation style (examples):
  - Blues/Jazz: deep blues; smooth, flowing
  - Bluegrass: sky/light blue; lively, bouncy
  - Classical: gold/yellow; elegant, graceful
  - Metal: purple/deep red; sharp, aggressive
  - Love/Trance: pink/gold; gentle, spiraling
  - Hip hop/Rap: silver/green; rippling, rhythmic
  - Pop: orange/bright yellow; peppy, energetic
  - Electronic: neon; strobing, fast
- Frequency bands influence details within the vibe:
  - Bass (20–250 Hz) → body glow/flame intensity
  - Mid (250–2000 Hz) → aura/eyes
  - Treble (2–20 kHz) → feather tips/tail sparkles

### Spectrum-to-Color Fallback (real-world mapping)

If genre is unavailable/ambiguous, compute a weighted color from the spectrum using approximate frequency→visible color mapping:

- 20–250 Hz → reds/oranges
- 250–2000 Hz → yellows/greens
- 2000–20000 Hz → blues/violets

This mapping also colors the spectrum visualizer so users can “see the music.”

## Project Structure

- `PhoenixVisualizer.App` — Avalonia UI, main window, controls, screensaver
- `PhoenixVisualizer.Core` — config, models, genre/vibe mapping, utilities
- `PhoenixVisualizer.Audio` — playback + analysis (ManagedBass/BPM/FFT)
- `PhoenixVisualizer.Visuals` — phoenix rendering/animation (SkiaSharp)
- `libs_etc/WAMPSDK` — Winamp SDK materials (future AVS compatibility)
- `Directory.Build.props` — sets `WinampSdkDir` relative to this folder

## Tech Stack

- .NET 8, Avalonia 11
- ManagedBass (+Fx) for playback/FFT/BPM hooks
- SkiaSharp (+Views) for custom 2D drawing
- Newtonsoft.Json for config (Core)

## Build

```
dotnet build
```

## Run

```
dotnet run --project PhoenixVisualizer.App
```

## Near-term Roadmap

- UI: Replace welcome screen with controls (Open/Play/Pause/Stop/Seek/Volume), info (Title/Artist/Album/BPM/Genre/Vibe), spectrum panel
- Audio: Wire playback; expose FFT/BPM stream; simple peak/energy
- Visuals: First phoenix pass (vector), state machine (idle/active/cocoon/burst)
- Screensaver: Idle timer to fullscreen canvas, input to exit

## Notes

- Windows dev confirmed with .NET SDK 8.x
- All project assets and SDK materials live under `PhoenixVisualizer/`


