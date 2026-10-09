using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json.Nodes;

namespace CabinPlay;

/// <summary>
/// Collects the logs someone helping with a problem needs into one zip on the desktop.
/// </summary>
internal static class Diagnostics
{
    public static void Export()
    {
        try
        {
            string zipPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                $"CabinPlay-diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
            using (ZipArchive zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                Add(zip, Path.Combine(Program.DataDir, "app.log"), "app.log");
                string? plugins = PluginFolder();
                if (plugins is not null)
                {
                    Add(zip, Path.Combine(plugins, "cabinplay.log"), "plugin.log");
                    Add(zip, Path.Combine(plugins, "cabinplay.ini"), "plugin.ini");
                }
                // The game's own log shows whether the mod and the plugin were loaded.
                Add(zip, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                      "Euro Truck Simulator 2", "game.log.txt"), "game.log.txt");
                ZipArchiveEntry info = zip.CreateEntry("system.txt");
                using var writer = new StreamWriter(info.Open());
                writer.WriteLine($"CabinPlay {typeof(Diagnostics).Assembly.GetName().Version}");
                writer.WriteLine($"Windows {Environment.OSVersion.Version}");
                writer.WriteLine($"Plugin folder: {plugins ?? "(unknown)"}");
            }
            Process.Start("explorer.exe", $"/select,\"{zipPath}\"");
            Log.Write("diagnostics written to " + zipPath);
        }
        catch (Exception ex)
        {
            Log.Write("diagnostics failed: " + ex.Message);
        }
    }

    /// <summary>Recorded by the installer in install.json.</summary>
    private static string? PluginFolder()
    {
        try
        {
            string path = Path.Combine(Program.DataDir, "install.json");
            if (!File.Exists(path))
                return null;
            string? game = (string?)JsonNode.Parse(File.ReadAllText(path))?["gameDir"];
            return game is null ? null : Path.Combine(game, "bin", "win_x64", "plugins");
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void Add(ZipArchive zip, string file, string name)
    {
        if (!File.Exists(file))
            return;
        // The game or the plugin may still have the file open for writing.
        using var source = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using Stream target = zip.CreateEntry(name).Open();
        source.CopyTo(target);
    }
}
