using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.IO;
using AvaloniaEdit;                // ✨ Syntax highlighting
using AvaloniaEdit.Highlighting;

namespace PhoenixVisualizer.Views;

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
}