using System.Diagnostics;
using System.Runtime.InteropServices;

static class PhoenixCli
{
    static string RepoRoot => FindRepoRoot() ?? Directory.GetCurrentDirectory();
    static string SlnPath => Path.Combine(RepoRoot, "PhoenixVisualizer.sln");
    static string AppProj => Path.Combine(RepoRoot, "PhoenixVisualizer.App");
    static string EditorProj => Path.Combine(RepoRoot, "PhoenixVisualizer.Editor");
    static string DownloadBassPs1 => Path.Combine(RepoRoot, "libs_etc", "download_bass.ps1");
    static string RunPhoenixPs1 => Path.Combine(RepoRoot, "libs_etc", "run-phoenix.ps1");

    static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var cfg = "Debug";
        var rid = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win-x64" :
                  RuntimeInformation.IsOSPlatform(OSPlatform.Linux)   ? "linux-x64" :
                  "osx-x64";

        while (true)
        {
            Console.Clear();
            Header();
            Console.WriteLine($"Repo Root: {RepoRoot}");
            Console.WriteLine($"Solution : {(File.Exists(SlnPath) ? SlnPath : "(not found)")}");
            Console.WriteLine($"Config   : {cfg} | RID: {rid}");
            Console.WriteLine();
            Console.WriteLine("A) dotnet --info");
            Console.WriteLine("B) List SDKs / Runtimes");
            Console.WriteLine("C) Restore solution");
            Console.WriteLine("D) Clean solution");
            Console.WriteLine("E) Build solution");
            Console.WriteLine("F) Run PhoenixVisualizer.App");
            Console.WriteLine("G) Run PhoenixVisualizer.Editor (if present)");
            Console.WriteLine("H) Test (solution or */Tests if present)");
            Console.WriteLine("I) Publish App (Release, self-contained=false, single-file=true)");
            Console.WriteLine("J) List projects in solution");
            Console.WriteLine("K) List package refs for App project");
            Console.WriteLine("L) dotnet tool restore (if dotnet-tools.json)");
            Console.WriteLine("M) Windows: download BASS native libs (download_bass.ps1)");
            Console.WriteLine("N) Windows: load PowerShell aliases (run-phoenix.ps1)");
            Console.WriteLine("O) Export codebase structure (for ChatGPT analysis)");
            Console.WriteLine("— — —");
            Console.WriteLine("1) Toggle Config (Debug/Release)");
            Console.WriteLine("2) Change RID (win-x64/linux-x64/osx-x64)");
            Console.WriteLine("?) Show Cheatsheet (commands only)");
            Console.WriteLine("Q) Quit");
            Console.Write("\nSelect: ");

            var key = Console.ReadKey(intercept:true).Key;
            Console.WriteLine();
            switch (key)
            {
                case ConsoleKey.A: Run("dotnet", "--info"); Pause(); break;
                case ConsoleKey.B: Run("dotnet", "--list-sdks"); Run("dotnet", "--list-runtimes"); Pause(); break;
                case ConsoleKey.C: EnsureSln(); Run("dotnet", $"restore \"{SlnPath}\""); Pause(); break;
                case ConsoleKey.D: EnsureSln(); Run("dotnet", $"clean \"{SlnPath}\" -c {cfg}"); Pause(); break;
                case ConsoleKey.E: EnsureSln(); Run("dotnet", $"build \"{SlnPath}\" -c {cfg}"); Pause(); break;
                case ConsoleKey.F: EnsureProj(AppProj); Run("dotnet", $"run --project \"{AppProj}\" -c {cfg}"); Pause(); break;
                case ConsoleKey.G: if (Directory.Exists(EditorProj)) { Run("dotnet", $"run --project \"{EditorProj}\" -c {cfg}"); } else Warn("Editor project not found."); Pause(); break;
                case ConsoleKey.H:
                    if (File.Exists(SlnPath)) Run("dotnet", $"test \"{SlnPath}\" -c {cfg}");
                    else Run("dotnet", "test -c " + cfg);
                    Pause(); break;
                case ConsoleKey.I:
                    EnsureProj(AppProj);
                    Run("dotnet", $"publish \"{AppProj}\" -c Release -r {rid} -p:PublishSingleFile=true --self-contained false");
                    Pause(); break;
                case ConsoleKey.J: EnsureSln(); Run("dotnet", $"sln \"{SlnPath}\" list"); Pause(); break;
                case ConsoleKey.K: EnsureProj(AppProj); Run("dotnet", $"list \"{AppProj}\" package"); Pause(); break;
                case ConsoleKey.L:
                    var toolsJson = Path.Combine(RepoRoot, "dotnet-tools.json");
                    if (File.Exists(toolsJson))
                        Run("dotnet", "tool restore");
                    else
                        Warn("dotnet-tools.json not found in repo root.");
                    Pause(); break;
                case ConsoleKey.M:
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        if (File.Exists(DownloadBassPs1))
                            RunPwsh($"-ExecutionPolicy Bypass -File \"{DownloadBassPs1}\"");
                        else Warn("download_bass.ps1 not found in libs_etc.");
                    }
                    else Warn("Windows-only.");
                    Pause(); break;
                case ConsoleKey.N:
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        if (File.Exists(RunPhoenixPs1))
                            RunPwsh($". \"{RunPhoenixPs1}\"; Write-Host 'Aliases loaded: phoenix, phoenix-editor'", useShell:true);
                        else Warn("run-phoenix.ps1 not found in libs_etc.");
                    }
                    else Warn("Windows-only.");
                    Pause(); break;
                case ConsoleKey.O:
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        var exportScript = Path.Combine(RepoRoot, "libs_etc", "export_script_fixed.ps1");
                        if (File.Exists(exportScript))
                        {
                            Console.WriteLine("📋 Exporting codebase structure for ChatGPT analysis...");
                            RunPwsh($"-ExecutionPolicy Bypass -File \"{exportScript}\"");
                            Console.WriteLine("✅ Export complete! Copy the output to ChatGPT for analysis.");
                        }
                        else Warn("export_script_fixed.ps1 not found in libs_etc.");
                    }
                    else Warn("Windows-only.");
                    Pause(); break;
                case ConsoleKey.D1: cfg = cfg == "Debug" ? "Release" : "Debug"; break;
                case ConsoleKey.D2:
                    Console.Write("RID (win-x64/linux-x64/osx-x64): ");
                    var input = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(input)) rid = input.Trim();
                    break;
                case ConsoleKey.Oem2: ShowCheatsheet(); Pause(); break;
                case ConsoleKey.Q: return 0;
                default: break;
            }
        }
    }

    static void Header()
    {
        Console.WriteLine("=== PhoenixVisualizer .NET CLI Menu ===");
        Console.WriteLine("Common tasks + cheatsheet menu for this repo.");
        Console.WriteLine("-----------------------------------------");
    }

    static void ShowCheatsheet()
    {
        Console.WriteLine("\n# Cheatsheet (general + PhoenixVisualizer)");
        Console.WriteLine("• dotnet --info");
        Console.WriteLine("• dotnet --list-sdks / --list-runtimes");
        Console.WriteLine($"• dotnet restore \"{SlnPath}\"");
        Console.WriteLine($"• dotnet clean   \"{SlnPath}\" -c Debug|Release");
        Console.WriteLine($"• dotnet build   \"{SlnPath}\" -c Debug|Release");
        Console.WriteLine($"• dotnet run --project \"{AppProj}\" -c Debug|Release");
        if (Directory.Exists(EditorProj))
            Console.WriteLine($"• dotnet run --project \"{EditorProj}\" -c Debug|Release");
        Console.WriteLine("• dotnet test [-c Debug|Release]");
        Console.WriteLine($"• dotnet publish \"{AppProj}\" -c Release -r <rid> -p:PublishSingleFile=true --self-contained false");
        Console.WriteLine("• dotnet sln <sln> list");
        Console.WriteLine($"• dotnet list \"{AppProj}\" package");
        Console.WriteLine("• dotnet tool restore");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.WriteLine("• powershell -ExecutionPolicy Bypass -File libs_etc/download_bass.ps1   (Windows)");
            Console.WriteLine("• . .\\libs_etc\\run-phoenix.ps1  # then: phoenix / phoenix-editor       (Windows)");
            Console.WriteLine("• powershell -ExecutionPolicy Bypass -File libs_etc/export_script_fixed.ps1   (Windows)");
        }
    }

    static void EnsureSln()
    {
        if (!File.Exists(SlnPath))
            throw new FileNotFoundException("PhoenixVisualizer.sln not found. Run from repo root or set current dir.");
    }
    static void EnsureProj(string projPath)
    {
        if (!Directory.Exists(projPath))
            throw new DirectoryNotFoundException($"Project folder not found: {projPath}");
    }

    static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "PhoenixVisualizer.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    static void Pause()
    {
        Console.Write("\nPress any key to continue...");
        Console.ReadKey(true);
    }

    static void Warn(string msg)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(msg);
        Console.ForegroundColor = prev;
    }

    static int Run(string fileName, string args, string? cwd = null)
    {
        return Exec(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            WorkingDirectory = cwd ?? RepoRoot,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            UseShellExecute = false
        });
    }

    static int RunPwsh(string args, bool useShell = false)
    {
        var exe = HasPwsh() ? "pwsh" : "powershell";
        return Exec(new ProcessStartInfo
        {
            FileName = exe,
            Arguments = useShell ? $"-NoLogo -NoExit -Command {args}" : args,
            WorkingDirectory = RepoRoot,
            UseShellExecute = false
        });
    }

    static bool HasPwsh()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which",
                Arguments = "pwsh",
                RedirectStandardOutput = true, UseShellExecute = false
            };
            using var p = Process.Start(psi)!;
            p.WaitForExit();
            return p.ExitCode == 0;
        }
        catch { return false; }
    }

    static int Exec(ProcessStartInfo psi)
    {
        using var p = new Process();
        p.StartInfo = psi;
        p.Start();
        p.WaitForExit();
        return p.ExitCode;
    }
}
