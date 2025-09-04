using System.IO;
using System.Text.Json;

namespace PhoenixVisualizer.App.Services
{
    public sealed class SettingsService
    {
        private const string FileName = "phoenix.settings.json";
        public bool ShowDiagnostics { get; set; }
        public string? LastPresetPath { get; set; }

        public void Load()
        {
            try {
                if (!File.Exists(FileName)) return;
                var json = File.ReadAllText(FileName);
                var s = JsonSerializer.Deserialize<SettingsService>(json);
                if (s != null) { ShowDiagnostics = s.ShowDiagnostics; LastPresetPath = s.LastPresetPath; }
            } catch {}
        }

        public void Save()
        {
            try {
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions{ WriteIndented = true });
                File.WriteAllText(FileName, json);
            } catch {}
        }
    }
}
