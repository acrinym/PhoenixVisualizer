# PhoenixVisualizer Implementation Details

## Recent Updates and Enhancements

### PHX Editor Stabilization
The PHX Editor has been stabilized with the following improvements:
- Fixed all compilation errors in `PhxEditorWindow.axaml.cs`
- Enhanced `PhxEditorViewModel` with all required properties and commands
- Fixed XAML bindings with proper `x:DataType` directives
- Resolved `EffectStackItem` property type issues
- Improved UI structure and removed problematic elements
- Initialized all ReactiveCommands with default implementations

### Theme Support
Added theme support with the following features:
- Theme selection in settings dialog (Dark, Light, Neon, Minimal)
- Theme persistence through app restarts
- Settings saved to `%APPDATA%\PhoenixVisualizer\settings.json`
- Theme applied at application startup

### Modal Preview Window
Added a modal preview window with:
- Borderless, resizable design
- Custom resize grips in all corners and edges
- Close button to return to the main editor

### UI Element Wiring
Wired up all UI elements including:
- Settings button opens settings dialog
- Undock Preview button opens modal preview window
- Apply/OK/Cancel buttons in settings dialog
- Theme selection ComboBox with binding to settings

## Technical Implementation Details

### Settings Persistence
The settings persistence system uses `System.Text.Json` to serialize and deserialize settings:
```csharp
public void Save()
{
    try
    {
        var dirPath = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrEmpty(dirPath))
        {
            Directory.CreateDirectory(dirPath);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
    }
}

public static PhxEditorSettings Load()
{
    try
    {
        if (File.Exists(SettingsPath))
        {
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<PhxEditorSettings>(json);
            if (settings != null)
                return settings;
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
    }
    
    return new PhxEditorSettings();
}
```

### Theme Application
Themes are applied using Avalonia's theme system:
```csharp
public void ApplyTheme()
{
    // Apply theme based on ThemeName
    var app = Avalonia.Application.Current;
    if (app == null) return;
    
    // Set theme resources based on theme name
    switch (ThemeName)
    {
        case "Light":
            app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
            break;
        case "Dark":
            app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
            break;
        case "Neon":
            app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
            // Additional neon theme resources would be applied here
            break;
        case "Minimal":
            app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;
            // Additional minimal theme resources would be applied here
            break;
    }
}
```

### Modal Preview Window
The modal preview window uses custom resize grips:
```csharp
private void AttachGripHandlers()
{
    foreach (var grip in this.GetVisualDescendants().OfType<Rectangle>().Where(r => r.Classes.Contains("Grip")))
    {
        grip.PointerPressed += OnGripPressed;
    }
}

private void OnGripPressed(object? sender, PointerPressedEventArgs e)
{
    if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
    {
        var rect = sender as Rectangle;
        if (rect is null) return;
        var edge = 0; // WindowEdge.Undefined

        if (rect.Cursor == new Cursor(StandardCursorType.SizeWestEast))
            edge = rect.HorizontalAlignment == HorizontalAlignment.Left ? 8 : 2; // WindowEdge.West : WindowEdge.East
        else if (rect.Cursor == new Cursor(StandardCursorType.SizeNorthSouth))
            edge = rect.VerticalAlignment == VerticalAlignment.Top ? 1 : 4; // WindowEdge.North : WindowEdge.South
        else if (rect.Cursor == new Cursor(StandardCursorType.TopLeftCorner)) edge = 9; // WindowEdge.NorthWest
        else if (rect.Cursor == new Cursor(StandardCursorType.TopRightCorner)) edge = 3; // WindowEdge.NorthEast
        else if (rect.Cursor == new Cursor(StandardCursorType.BottomLeftCorner)) edge = 12; // WindowEdge.SouthWest
        else if (rect.Cursor == new Cursor(StandardCursorType.BottomRightCorner)) edge = 6; // WindowEdge.SouthEast

        if (edge != 0)
            BeginResizeDrag((WindowEdge)edge, e);
    }
}
```

## Future Enhancements
The following enhancements are planned for future updates:
1. Complete preset management functionality
2. Enhance parameter editing experience
3. Optimize preview rendering
4. Add ESP-32 connectivity for hardware integration
5. Implement advanced theme customization options
