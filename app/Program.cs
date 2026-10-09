namespace Ets2CarPlay;

internal static class Program
{
    /// <summary>
    /// Set through CARPLAY_TEST_NAMESPACE to run a second, fully separate instance for
    /// testing: its own data folder and shared-memory names, so it cannot disturb a
    /// running game. Matches the same switch in the plugin.
    /// </summary>
    public static readonly string Namespace =
        Environment.GetEnvironmentVariable("CARPLAY_TEST_NAMESPACE") is { Length: > 0 } ns ? "." + ns : "";

    public static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ETS2CarPlay" + Namespace);

    public static string SharedName(string name) => @"Local\" + name + Namespace;

    [STAThread]
    private static void Main(string[] args)
    {
        Directory.CreateDirectory(DataDir);
        using var single = new Mutex(true, SharedName("ETS2CarPlayApp"), out bool first);
        if (!first)
            return; // already running; two writers would fight over the screen

        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, e) => Log.Write("UI error: " + e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log.Write("Fatal: " + e.ExceptionObject);
        Application.Run(new MainForm(args));
    }
}

internal static class Log
{
    private static readonly object Gate = new();
    private static readonly string File = Path.Combine(Program.DataDir, "app.log");
    private static bool _started;

    public static void Write(string message)
    {
        lock (Gate)
        {
            try
            {
                string line = $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}";
                if (_started)
                    System.IO.File.AppendAllText(File, line);
                else
                    System.IO.File.WriteAllText(File, line);
                _started = true;
            }
            catch (IOException)
            {
            }
        }
    }
}
