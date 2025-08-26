using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.IO;
using AvaloniaEdit;                // ✨ Syntax highlighting
using AvaloniaEdit.Highlighting;
using System.Collections.Generic; // Added for List<object>
using System.Text.Json;

namespace PhoenixVisualizer.Views
{

public partial class PluginEditorWindow : Window
{
    private string? _currentFile;
    private readonly TextEditor _editor;

    public PluginEditorWindow()
    {
        InitializeComponent();
        _editor = this.FindControl<TextEditor>("CodeEditor");
        if (_editor != null)
        {
            _editor.ShowLineNumbers = true;
            _editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
        }
    }

    private async void OnOpenClick(object? _, RoutedEventArgs __)
    {
        var dlg = new OpenFileDialog { Title = "Open Plugin" };
        dlg.Filters.Add(new FileDialogFilter { Name = "Phoenix Plugins", Extensions = { "phx", "avs", "txt" } });
        var result = await dlg.ShowAsync(this);
        if (result is { Length: > 0 })
        {
            var text = File.ReadAllText(result[0]);
            _editor.Text = text;
            _currentFile = result[0];
            this.Title = $"Phoenix Plugin Editor - {Path.GetFileName(_currentFile)}";
        }
    }

    private async void OnSaveClick(object? _, RoutedEventArgs __)
    {
        if (_editor == null) return;
        var path = _currentFile;
        if (string.IsNullOrEmpty(path))
        {
            var dlg = new SaveFileDialog { Title = "Save Plugin As..." };
            path = await dlg.ShowAsync(this);
        }
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, _editor.Text ?? "");
            _currentFile = path;
            this.Title = $"Phoenix Plugin Editor - {Path.GetFileName(_currentFile)} (saved)";
        }
    }

    private async void OnSaveAsClick(object? _, RoutedEventArgs __)
    {
        var dlg = new SaveFileDialog { Title = "Save Plugin As..." };
        dlg.Filters.Add(new FileDialogFilter { Name = "Phoenix Plugin", Extensions = { "phx" } });
        dlg.Filters.Add(new FileDialogFilter { Name = "Winamp AVS Preset", Extensions = { "avs" } });
        var path = await dlg.ShowAsync(this);
        if (!string.IsNullOrEmpty(path))
        {
            if (path.EndsWith(".avs", StringComparison.OrdinalIgnoreCase))
            {
                AvsConverter.SaveAvs(path, CollectPhx());
            }
            else
            {
                SavePhx(path);
            }
            _currentFile = path;
            this.Title = $"Phoenix Plugin Editor - {Path.GetFileName(path)}";
        }
    }

    private string CollectPhx() => _editor?.Text ?? "{}";

    private void SavePhx(string path)
    {
        File.WriteAllText(path, CollectPhx());
    }
}

// Full AVS binary converter
public static class AvsConverter
{
    public static string LoadAvs(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);

        // Verify header
        var header = new string(br.ReadChars(32)).TrimEnd('\0');
        if (!header.Contains("Nullsoft AVS"))
            throw new InvalidDataException("Not a valid AVS preset.");

        // AVS presets store number of objects, then serialized ops
        int effectCount = br.ReadInt32();
        var effects = new List<object>();
        string init = "", frame = "", point = "", beat = "";
        bool clearEveryFrame = true;

        for (int i = 0; i < effectCount; i++)
        {
            int id = br.ReadInt32();
            int size = br.ReadInt32();
            byte[] blob = br.ReadBytes(size);

            // Known IDs for AVS components
            switch (id)
            {
                case 0x01: // Superscope / point script
                    point = ExtractString(blob);
                    effects.Add(new { type = "superscope" });
                    break;
                case 0x02: // Trans / per frame
                    frame = ExtractString(blob);
                    break;
                case 0x03: // Init code
                    init = ExtractString(blob);
                    break;
                case 0x04: // On beat
                    beat = ExtractString(blob);
                    break;
                case 0x05: // Clear every frame toggle
                    clearEveryFrame = blob[0] != 0;
                    break;
                default:
                    // Generic effect node mapping
                    effects.Add(new { type = $"avs_{id}", data = Convert.ToBase64String(blob) });
                    break;
            }
        }

        // Build PHX JSON schema
        var json = new
        {
            init,
            frame,
            point,
            beat,
            clearEveryFrame,
            effects
        };
        return System.Text.Json.JsonSerializer.Serialize(json, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static string ExtractString(byte[] data)
    {
        try
        {
            return System.Text.Encoding.ASCII.GetString(data).TrimEnd('\0');
        }
        catch
        {
            return "// (unreadable AVS code block)";
        }
    }

    public static void SaveAvs(string path, string phxJson)
    {
        var doc = JsonDocument.Parse(phxJson);
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);

        // Header: 32 bytes, padded
        var header = "Nullsoft AVS Preset";
        var headerBytes = new byte[32];
        System.Text.Encoding.ASCII.GetBytes(header, 0, header.Length, headerBytes, 0);
        bw.Write(headerBytes);

        var effects = new List<JsonElement>();
        if (doc.RootElement.TryGetProperty("effects", out var effs))
        {
            foreach (var e in effs.EnumerateArray()) effects.Add(e);
        }

        bw.Write(effects.Count + 4); // effects + 4 code ops

        // Write each block as id + size + data
        void WriteBlock(int id, string? text)
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes(text ?? "");
            bw.Write(id);
            bw.Write(bytes.Length);
            bw.Write(bytes);
        }

        if (doc.RootElement.TryGetProperty("point", out var point))
            WriteBlock(0x01, point.GetString());
        if (doc.RootElement.TryGetProperty("frame", out var frame))
            WriteBlock(0x02, frame.GetString());
        if (doc.RootElement.TryGetProperty("init", out var init))
            WriteBlock(0x03, init.GetString());
        if (doc.RootElement.TryGetProperty("beat", out var beat))
            WriteBlock(0x04, beat.GetString());

        bool clear = doc.RootElement.TryGetProperty("clearEveryFrame", out var cf) && cf.GetBoolean();
        bw.Write(0x05);
        bw.Write(1);
        bw.Write(new byte[] { clear ? (byte)1 : (byte)0 });

        // Remaining effects
        int idx = 6;
        foreach (var e in effects)
        {
            var type = e.GetProperty("type").GetString() ?? "unknown";
            var bytes = System.Text.Encoding.ASCII.GetBytes(type);
            bw.Write(idx++);
            bw.Write(bytes.Length);
            bw.Write(bytes);
        }
    }
}