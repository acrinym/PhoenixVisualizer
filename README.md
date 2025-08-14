# Phoenix Visualizer

Cross-platform Avalonia visualizer studio with an AVS-compatible runtime at its core. The first flagship visual is a Phoenix plugin, but the app is designed to host many visualizers (AVS-style presets, APE-style effects, and managed plugins). Each track gets one primary vibe (genre-driven), nuanced by BPM, energy, and frequency bands. Includes a real-world frequency-to-visible-color fallback when genre is missing.

## Features (MVP)

- Music playback: Open file, Play/Pause, Stop, Seek, Volume (MP3 first)
- Real-time analysis: FFT (1024/2048), BPM detection, energy/peaks
- Genre detection: Primary ID3 tag, fallback via spectrum color mapping
- Phoenix visualizer: One vibe per track; animation and effects respond to audio
- Spectrum visualizer: Real-time bars/curve, color-coded to frequency→visible light
- Screensaver mode: Future (leaving out of MVP)

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

- `PhoenixVisualizer.App` — Avalonia UI host app
- `PhoenixVisualizer.Core` — config, models, genre/vibe mapping, utilities
- `PhoenixVisualizer.Audio` — playback + analysis (ManagedBass/BPM/FFT)
- `PhoenixVisualizer.Visuals` — legacy direct-render visuals (if needed)
- `PhoenixVisualizer.PluginHost` — shared plugin interfaces and `AudioFeatures`
- `PhoenixVisualizer.ApeHost` — managed APE-style host interfaces/stubs
- `PhoenixVisualizer.AvsEngine` — AVS runtime (Superscope-first), Skia renderer
- `PhoenixVisualizer.Plugins.Avs` — vis_AVS plugin that wraps the AVS engine
- `PhoenixVisualizer.Plugins.Ape.Phoenix` — Phoenix visual as an APE-style plugin
- `PhoenixVisualizer.Plots` — Matplotlib-inspired plotting primitives (for scopes, wheels, spectrograms)
- `PhoenixVisualizer.Editor` — Avalonia-based visualization editor UI
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

- UI (Host): Replace welcome screen with transport controls + info + spectrum panel
- Audio: Wire playback; expose FFT/BPM/energy to engine
- AVS Engine: Superscope subset (per-frame/point vars, math, conds) + Skia renderer
- vis_AVS plugin: host AVS presets via the engine
- Plugins API: finalize `IVisualizerPlugin` and `AudioFeatures`
- Editor: initial layout (preset browser, canvas, properties), load/run AVS preset
- Phoenix plugin: scaffold (reads features; minimal draw stub)

## Notes

- Windows dev confirmed with .NET SDK 8.x
- All project assets and SDK materials live under `PhoenixVisualizer/`
- Docs index: `PhoenixVisualizer/docs/INDEX.md`


