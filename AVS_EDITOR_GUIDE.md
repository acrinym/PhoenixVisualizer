# ✏️ AVS Editor Guide

**Complete guide to the AVS Editor system** - A full-featured editor for creating and editing AVS presets with seamless integration to the main PhoenixVisualizer application.

## 🎯 Overview

The AVS Editor is a professional-grade development environment that allows you to:
- **Write and edit AVS code** with real-time validation
- **Preview superscopes** before importing
- **Send code directly** to the main application
- **Export presets** as C# plugins or AVS files
- **Test and validate** superscope syntax

## 🚀 Getting Started

### Opening the Editor
1. **Launch PhoenixVisualizer**
2. **Click "AVS Editor" button** in the main toolbar
3. **Editor window opens** with a code editor and preview panel

### Editor Interface
```
┌─────────────────────────────────────────────────────────────┐
│                    AVS Preset Editor                        │
├─────────────────────────────────────────────────────────────┤
│ AVS Code                    │ Preview & Controls            │
│ ┌─────────────────────────┐ │ ┌─────────────────────────┐   │
│ │ Code Editor             │ │ │ Preview Area            │   │
│ │ (Console font)          │ │ │                         │   │
│ │                         │ │ │ Shows validation        │   │
│ │                         │ │ │ results                 │   │
│ └─────────────────────────┘ │ └─────────────────────────┘   │
│                             │ ┌─────────────────────────┐   │
│ [Load] [Save] [Clear]       │ │ Controls                │   │
│                             │ │ [Test] [Import] [Export]│   │
│                             │ │ [Send to Main Window]   │   │
│                             │ └─────────────────────────┘   │
│                             │ ┌─────────────────────────┐   │
│                             │ │ Status                  │   │
│                             │ │ Ready to edit           │   │
│                             │ └─────────────────────────┘   │
├─────────────────────────────────────────────────────────────┤
│                    [Close] [Apply Preset]                   │
└─────────────────────────────────────────────────────────────┘
```

## ✏️ Code Editor Features

### Syntax Support
- **AVS syntax highlighting** (basic)
- **Consolas monospace font** for code clarity
- **Line wrapping** for long code
- **Accept return** for multi-line editing

### Default Template
```avs
// AVS Preset Code
// Example superscope
superscope("MyScope", "
  // Your code here
  x = sin(t) * 100;
  y = cos(t) * 100;
  red = sin(t) * 0.5 + 0.5;
  green = cos(t) * 0.5 + 0.5;
  blue = 0.5;
");
```

## 🔍 Preview & Validation

### Real-time Preview
The preview panel shows:
- **Superscope count** found in your code
- **Validation status** (valid/invalid)
- **File size** information
- **Ready status** for import

### Validation Process
1. **Code is parsed** using AVS regex patterns
2. **Superscopes are extracted** and validated
3. **Results displayed** in real-time
4. **Error messages** shown for invalid code

## 🎮 Control Buttons

### File Operations
- **Load File**: Open existing `.avs` or `.txt` files
- **Save File**: Save current code as `.avs` or `.txt`
- **Clear**: Reset editor to default template

### Testing & Import
- **Test Preset**: Validate current code without importing
- **Import to Library**: Add preset to the main application library
- **Export C#**: Generate standalone C# plugin code

### Integration
- **Send to Main Window**: Transfer code directly to main app
- **Apply Preset**: Import and execute preset immediately

## 🔗 Seamless Integration

### "Send to Main Window" Workflow
1. **Write/edit AVS code** in the editor
2. **Click "Send to Main Window"** button
3. **Code transfers automatically** to main application
4. **Preset text box updates** with the new content
5. **Preset executes immediately** - no manual steps needed
6. **Editor closes** after successful transfer

### Automatic Execution
When code is sent from the editor:
- **Preset text box populates** with the AVS code
- **AVS plugin is created** and set as active
- **Preset loads and executes** automatically
- **Success message** appears in the status area
- **Visualization starts** immediately

## 📁 File Operations

### Supported File Types
- **`.avs`** - Standard AVS preset files
- **`.txt`** - Text files containing AVS code
- **`.cs`** - C# plugin code export

### File Picker Integration
- **Modern file dialogs** with proper filtering
- **Multiple format support** for import/export
- **Error handling** for file operations
- **User feedback** for all operations

## 🎨 AVS to C# Conversion

### Automatic Syntax Conversion
The editor converts common AVS syntax to C#:

| AVS Syntax | C# Equivalent |
|------------|---------------|
| `sin(`     | `Math.Sin(`   |
| `cos(`     | `Math.Cos(`   |
| `tan(`     | `Math.Tan(`   |
| `sqrt(`    | `Math.Sqrt(`  |
| `pow(`     | `Math.Pow(`   |
| `abs(`     | `Math.Abs(`   |
| `t`        | `_time`       |
| `w`        | `_width`      |
| `h`        | `_height`     |

### Generated C# Structure
```csharp
using PhoenixVisualizer.Visuals;
using PhoenixVisualizer.Audio;
using SkiaSharp;

namespace PhoenixVisualizer.ExportedPresets
{
    public class ExportedPreset : IVisualizerPlugin
    {
        public string Id => "exported_exportedpreset";
        public string DisplayName => "MyScope (Exported)";
        
        private int _width, _height;
        private float _time;
        
        public void Initialize(int width, int height)
        {
            _width = width;
            _height = height;
            _time = 0;
        }
        
        public void RenderFrame(AudioFeatures features, ISkiaCanvas canvas)
        {
            canvas.Clear(0xFF000000);
            _time += 0.02f;
            
            // Converted AVS code:
            // Your converted code here
        }
        
        public void Resize(int width, int height)
        {
            _width = width;
            _height = height;
        }
        
        public void Dispose()
        {
            // Clean up resources if any
        }
    }
}
```

## 🎯 Best Practices

### Code Organization
- **Use descriptive names** for superscopes
- **Add comments** explaining complex math
- **Test incrementally** as you build
- **Validate frequently** with the Test button

### Performance Tips
- **Limit complex calculations** in tight loops
- **Use efficient math functions** when possible
- **Cache calculated values** that don't change often
- **Test with different audio** to ensure stability

### Error Handling
- **Check validation results** before sending
- **Review error messages** in the preview
- **Test with simple code** first, then add complexity
- **Use the Test button** to catch issues early

## 🔧 Troubleshooting

### Common Issues

#### Code Not Validating
- **Check syntax** for missing parentheses or quotes
- **Verify superscope format** follows AVS standards
- **Look for typos** in mathematical functions
- **Ensure proper nesting** of code blocks

#### Integration Problems
- **Verify main app is running** before sending code
- **Check for error messages** in the status area
- **Try the Test button** to validate locally first
- **Restart editor** if connection issues persist

#### Performance Issues
- **Simplify complex calculations** in loops
- **Reduce superscope count** if rendering is slow
- **Use efficient math functions** (Math.Sin vs custom sin)
- **Test with different audio files** to isolate issues

### Getting Help
- **Check the preview panel** for validation errors
- **Review generated C# code** for syntax issues
- **Test with simple examples** to verify functionality
- **Consult the main app logs** for execution errors

## 🚀 Advanced Features

### Custom Functions
While the basic editor handles standard AVS syntax, you can:
- **Create complex mathematical patterns**
- **Use multiple superscopes** in one preset
- **Implement custom color algorithms**
- **Build interactive visualizations**

### Integration Workflow
1. **Develop in editor** with real-time feedback
2. **Test locally** using the Test button
3. **Send to main app** for live testing
4. **Iterate and refine** based on results
5. **Export final version** as C# plugin

## 📚 Related Documentation

- **[SUPERSCOPES_IMPLEMENTATION.md](SUPERSCOPES_IMPLEMENTATION.md)** - Complete superscopes guide
- **[PLUGIN_DEVELOPMENT_GUIDE.md](PLUGIN_DEVELOPMENT_GUIDE.md)** - Plugin development reference
- **[WINAMP_FEATURES_SUMMARY.md](WINAMP_FEATURES_SUMMARY.md)** - Winamp compatibility guide

## 🎉 Summary

The AVS Editor provides a **professional development environment** for creating AVS presets with:

✅ **Real-time editing** with syntax validation  
✅ **Live preview** of superscope content  
✅ **Seamless integration** with main application  
✅ **Multiple export options** for flexibility  
✅ **Error handling** and user feedback  
✅ **Modern UI** with intuitive controls  

This system bridges the gap between **traditional AVS development** and **modern PhoenixVisualizer integration**, making it easy to create, test, and deploy custom visualizations! 🚀
