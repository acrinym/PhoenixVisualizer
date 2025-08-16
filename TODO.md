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

**Current Status**: ⚠️ **PARTIAL** - Infrastructure working, but visualization not rendering

**Known Issues**:
- ❌ **Visualization not appearing** - Black screen despite FFT data and render calls
- 🔍 **Need to audit visualization pipeline** from `AvsEngine.RenderFrame` → `CanvasAdapter` → screen

**Recent Fixes**:
- ✅ **Audio safety** - Play/Pause/Stop now check for loaded audio file
- ✅ **UI feedback** - Settings button shows message, Play button shows status
- ✅ **Stop vs Pause** - Stop now resets to beginning and clears audio buffers; Pause just pauses
- ✅ **Settings Window** - Proper modal dialog with plugin selection, audio settings, display options
- ✅ **Winamp AVS Integration** - Real superscope preset parsing (init:, per_frame:, per_point:, beat:)
- ✅ **Interface contracts** - LoadPreset method properly defined in IAvsHostPlugin
- ✅ **Code cleanup** - Removed old "Welcome to Avalonia" ViewModel code
- ✅ **Proper AVS architecture** - Preset loading follows Winamp SDK pattern
- ✅ **Settings crash fixed** - Added proper ViewModel and DataContext binding
- ✅ **Settings window rebuilt** - Manual control creation to avoid Avalonia code generation issues
- ✅ **Debug logging added** - Enhanced render pipeline debugging for visualization troubleshooting

**New Features**:
- ✅ **Settings Button** - Winamp-style settings access (plugin selection coming soon)
- ✅ **Default Plugin Loading** - AVS Engine loads on startup (configurable later)

## Phase 2 – Editor + Plots
- [x] ~~Editor UI: preset browser, canvas viewport, properties panel~~
- [x] ~~Plots lib: LineSeries, Polar/Wheel, Bar/Stem~~
 - [x] Colormaps: viridis/plasma/magma/inferno + genre palettes
- [ ] Designer nodes: sources (FFT/BPM), transforms (scale/polar), styles (colormap/stroke), compose (overlay)

**Current Status**: ✅ **COMPLETE** - Basic editor UI and plotting primitives working

## Phase 3 – Phoenix Plugin (APE)
- [x] ~~Phoenix plugin scaffold: reads AudioFeatures; minimal draw stub~~
- [x] ~~Color/vibe mapping (genre primary; spectrum fallback)~~
- [x] ~~States: idle/active/cocoon/burst (hooks: beat/quiet/drop)~~

**Current Status**: ✅ **COMPLETE** - Phoenix plugin fully implemented with audio-reactive animation

## Phase 4 – Compatibility & Effects
- [x] ~~AVS: add fade/blur/color ops commonly used by presets~~
- [x] ~~Real Winamp superscope preset parsing (init:, per_frame:, per_point:, beat:)~~
- [ ] APE host: managed APE interface; optional native bridge later
- [x] Preset import: loader for common text-based presets
- [ ] NS-EEL expression evaluator for superscope math

**Current Status**: 🚧 **IN PROGRESS** - AVS engine enhanced with real Winamp preset parsing and beat/energy effects

## Nice-to-haves
- Settings: idle timeout, spectrum smoothing window
- TagLib#: ID3 metadata for genre (fallback spectrum mapping)
- Prune OpenTK deps; lean on Skia/Avalonia

## Known Bugs
- **Stop/Pause behavior**: Both controls currently pause (NAudio limitation - can't reset CurrentTime)
- **Visualization pipeline**: Need to audit render chain from engine to screen

## Tracking
- Warnings: NU1701 (OpenTK/Skia.Views) — review and prune
- Windows dev target: .NET 8 SDK confirmed
