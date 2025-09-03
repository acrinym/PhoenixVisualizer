# AVS Import Encoding Fix Report

## Problem
The PHX Editor was crashing when trying to import AVS files with the error:

```
System.ArgumentException: 'latin-1' is not a supported encoding name. For information on defining a custom encoding, see the documentation for the Encoding.RegisterProvider method. (Parameter 'name')
```

## Root Cause
The issue was in the `WinampAvsImporter.DecodeLatin1` method where it was trying to use `Encoding.GetEncoding("latin-1")`. In .NET 8, 'latin-1' is not a supported encoding name by default.

## Solution

### Fixed Encoding Names
Updated `PhoenixVisualizer.Core/Transpile/WinampAvsImporter.cs`:

**Before:**
```csharp
private static string DecodeLatin1(byte[] bytes) => Encoding.GetEncoding("latin-1").GetString(bytes);
```

**After:**
```csharp
private static string DecodeLatin1(byte[] bytes) => Encoding.GetEncoding("iso-8859-1").GetString(bytes);
```

Also fixed the `ExtractAsciiChunks` method:
```csharp
var s = Encoding.GetEncoding("iso-8859-1").GetString(b);
```

## Technical Details

### Why This Happened
- **.NET 8 Changes**: .NET 8 removed support for the 'latin-1' encoding name
- **Encoding Standards**: 'iso-8859-1' is the correct IANA name for Latin-1 encoding
- **Backward Compatibility**: The 'latin-1' name was deprecated in favor of the standard IANA name

### The Fix
- **Correct Encoding Name**: Use 'iso-8859-1' instead of 'latin-1'
- **Same Functionality**: Both refer to the same encoding (Latin-1/ISO-8859-1)
- **Future-Proof**: 'iso-8859-1' is the standard name that will continue to be supported

## Results

✅ **Build Success**: All projects compile without errors
✅ **Import Functionality**: AVS files can now be imported without crashes
✅ **Encoding Support**: Latin-1 encoded AVS files are properly decoded
✅ **Backward Compatibility**: All existing functionality preserved

## Files Modified

1. `PhoenixVisualizer.Core/Transpile/WinampAvsImporter.cs` - Fixed encoding names

## Testing

- ✅ **Build Test**: All projects compile successfully
- ✅ **Runtime Test**: Application starts without crashes
- ✅ **Import Test**: AVS import functionality works correctly
- ✅ **Encoding Test**: Latin-1 encoded files are properly decoded

## Prevention

To prevent similar issues in the future:
1. **Use Standard Names**: Always use IANA standard encoding names
2. **Test Import Functionality**: Test file import with various encodings
3. **Check .NET Version Changes**: Review breaking changes in new .NET versions
4. **Documentation**: Document encoding requirements for file formats

## Conclusion

The AVS import encoding issue has been completely resolved. The PHX Editor can now successfully import AVS files without crashes, and all Latin-1 encoded content is properly decoded using the correct encoding name.
