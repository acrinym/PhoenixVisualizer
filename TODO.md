# TODO

High-level plan: AVS-first runtime, Phoenix as APE plugin, MatPlot-inspired plots, Editor.

## Phase 0 – Contracts and Docs
- Finalize AudioFeatures and IVisualizerPlugin interfaces
- Document plugin families: APE-like and AVS host plugin
- Update README (done)

## Phase 1 – Core AVS and Host Wiring
- Engine: Superscope subset (per-frame/per-point vars, math, conditionals)
- Audio feed: expose FFT (1024/2048), BPM, energy, beat
- Renderer: Skia lines/points, clear/fade stub
- vis_AVS plugin: wrap engine and drive frames from host
- App: Basic transport UI + spectrum panel (no screensaver)

## Phase 2 – Editor + Plots
- Editor UI: preset browser, canvas viewport, properties panel
- Plots lib: LineSeries, Polar/Wheel, Heatmap (spectrogram), Bar/Stem
- Colormaps: viridis/plasma/magma/inferno + genre palettes
- Designer nodes: sources (FFT/BPM), transforms (scale/polar), styles (colormap/stroke), compose (overlay)

## Phase 3 – Phoenix Plugin (APE)
- Phoenix plugin scaffold: reads AudioFeatures; minimal draw stub
- Color/vibe mapping (genre primary; spectrum fallback)
- States: idle/active/cocoon/burst (hooks: beat/quiet/drop)

## Phase 4 – Compatibility & Effects
- AVS: add fade/blur/color ops commonly used by presets
- APE host: managed APE interface; optional native bridge later
- Preset import: loader for common text-based presets

## Nice-to-haves
- Settings: idle timeout, spectrum smoothing window
- TagLib#: ID3 metadata for genre (fallback spectrum mapping)
- Prune OpenTK deps; lean on Skia/Avalonia

## Tracking
- Warnings: NU1701 (OpenTK/Skia.Views) — review and prune
- Windows dev target: .NET 8 SDK confirmed
