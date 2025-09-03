using System;

namespace PhoenixVisualizer.Core.Transpile
{
    public static class WinampAvsExporter
    {
        public static byte[] Export(UnifiedGraph graph)
        {
            // TODO: Implement actual AVS export logic
            var content = "// Exported Phoenix preset\n";
            foreach (var node in graph.Nodes)
            {
                if (node.TypeKey == "superscope")
                {
                    content += $"// {node.DisplayName}\n";
                    content += $"// INIT\n{node.Parameters.GetValueOrDefault("init", "")}\n";
                    content += $"// FRAME\n{node.Parameters.GetValueOrDefault("frame", "")}\n";
                    content += $"// BEAT\n{node.Parameters.GetValueOrDefault("beat", "")}\n";
                    content += $"// POINT\n{node.Parameters.GetValueOrDefault("point", "")}\n";
                }
            }
            return System.Text.Encoding.UTF8.GetBytes(content);
        }
    }
}
