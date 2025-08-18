using System.IO;

namespace PhoenixVisualizer.Models
{
    public class PresetInfo
    {
        public string Name { get; set; }
        public string FilePath { get; set; }
        public string Type { get; set; }

        public PresetInfo(string filePath, string type)
        {
            FilePath = filePath;
            Name = Path.GetFileNameWithoutExtension(filePath);
            Type = type;
        }
    }
}
