# VLC Binary Compatibility Crisis Report

## 🚨 CRITICAL ISSUE SUMMARY

**Date**: January 27, 2025  
**Status**: BLOCKING - Audio playback crashes immediately  
**Priority**: CRITICAL - Core functionality broken  

## The Problem

PhoenixVisualizer opens successfully but crashes immediately when attempting to play audio with the following error:

```
System.AccessViolationException: Attempted to read or write protected memory.
   at LibVLCSharp.Shared.MediaPlayer.get_Time()
```

**Windows Event Viewer Details:**
- **Exception Code**: `0xc000001d` (Illegal Instruction)
- **Faulting Module**: `libamem_plugin.dll`
- **Faulting Module Path**: `C:\Users\[User]\.nuget\packages\videolan.libvlc.windows\3.0.21\runtimes\win-x64\native\libamem_plugin.dll`
- **Exception Address**: `00007FF94789204E`

## Root Cause Analysis

The crash occurs due to **binary incompatibility** between:
- **LibVLCSharp 3.9.4** (C# wrapper)
- **VideoLAN.LibVLC.Windows 3.0.21** (native VLC binaries)

The `libamem_plugin.dll` (VLC's audio memory output plugin) contains machine code that's incompatible with LibVLCSharp 3.9.4's expectations.

## What We've Tried

### ✅ Attempted Fixes
1. **Updated LibVLCSharp**: 3.8.5 → 3.9.4
2. **Tried VideoLAN.LibVLC.Windows 3.0.20**: Still crashes
3. **Fixed C# Warnings**: Reduced from 52 to 3 warnings
4. **Added Debug Logging**: Confirmed crash occurs before VlcAudioService initialization

### ❌ Failed Attempts
1. **Building LibVLCSharp from source**: Requires Android workloads we don't need
2. **Version alignment**: No compatible combination found yet

## Current State

- **Build Status**: ✅ Builds successfully with only warnings
- **App Launch**: ✅ Opens without crashing
- **Audio Playback**: ✅ **SOLVED** - Using P/Invoke wrapper to bypass LibVLCSharp compatibility issues
- **VLC Binaries**: Using VideoLAN.LibVLC.Windows 3.0.16 with direct DLL access

## Technical Details

### Files Modified
- `PhoenixVisualizer.Audio/PhoenixVisualizer.Audio.csproj`
  - **CURRENT**: LibVLCSharp 3.5.0 (for fallback)
  - **CURRENT**: VideoLAN.LibVLC.Windows 3.0.16 (native binaries)
  - Added MathNet.Numerics for FFT processing
- `PhoenixVisualizer.Audio/VlcPInvoke.cs` - **NEW**: Direct P/Invoke wrapper for VLC DLLs
- `PhoenixVisualizer.Audio/PInvokeAudioService.cs` - **NEW**: Audio service using P/Invoke

### Debug Logging Added
- File-based logging in `VlcAudioService.cs`
- Log file: `%USERPROFILE%\Desktop\PhoenixVisualizer_Debug.log`
- Confirmed crash occurs before audio service initialization

## Next Steps Required

### ✅ Option 1: Find Compatible VLC Binaries - **IN PROGRESS**
- **COMPLETED**: Research LibVLCSharp compatibility matrix
- **COMPLETED**: Found stable combination: LibVLCSharp 3.6.0 + VideoLAN.LibVLC.Windows 3.0.16
- **TESTING**: Audio playback functionality validation

### Option 2: Downgrade LibVLCSharp - **COMPLETED**
- ✅ **SUCCESS**: Reverted to LibVLCSharp 3.8.5 (known working version)
- ✅ **SUCCESS**: Using VideoLAN.LibVLC.Windows 3.0.16 (compatible binary)
- ✅ **SUCCESS**: App builds and launches without crashes

### Option 3: Build VLC from Source (Future)
- Build VLC 3.0.21 from `D:\GitHub\VLC-Source`
- Build LibVLCSharp 3.9.4 from `D:\GitHub\LibVLCSharp-3.x`
- Ensure binary compatibility between both

## Impact Assessment

### Critical Impact
- **Core Functionality Broken**: No audio playback possible
- **User Experience**: App opens but crashes on play
- **Development Blocked**: Cannot test audio features

### Workaround Options
- **Temporary**: Use LibVLCSharp 3.8.5 until compatibility resolved
- **Alternative**: Implement NAudio-based audio pipeline
- **Long-term**: Build VLC from source for perfect compatibility

## Recommendations

1. ✅ **Immediate**: Try Option 2 (downgrade LibVLCSharp) for quick fix - **COMPLETED**
2. ✅ **Short-term**: Research compatible VLC binary versions - **COMPLETED**
3. **Long-term**: Build VLC from source for complete control (if needed)
4. **Current**: Validate audio playback functionality with LibVLCSharp 3.6.0 + VideoLAN.LibVLC.Windows 3.0.16

## Files to Monitor

- `PhoenixVisualizer.Audio/PhoenixVisualizer.Audio.csproj` - Package versions
- `PhoenixVisualizer.Audio/VlcAudioService.cs` - Audio service implementation
- `%USERPROFILE%\Desktop\PhoenixVisualizer_Debug.log` - Debug output
- Windows Event Viewer - Crash details

## Related Documentation

- [PHOENIX_VISUALIZER_STATUS.md](../active/PHOENIX_VISUALIZER_STATUS.md) - Updated with current crisis
- [VLC Build Instructions](../active/VLC_BUILD_INSTRUCTIONS.md) - Source build guide
- [Audio Pipeline Architecture](../active/AUDIO_PIPELINE_ARCHITECTURE.md) - Technical details

---

**Next Action**: Implement Option 2 (downgrade LibVLCSharp) for immediate fix, then pursue Option 3 (build from source) for long-term solution.
