using System.Diagnostics;
using System.Text.Json.Nodes;

namespace CabinPlay;

/// <summary>
/// Keeps CabinPlay up to date from the GitHub release page. While the game runs it only
/// looks for a newer release and downloads its setup; the setup runs, without questions, once the
/// game has closed, because the game holds the plugin and the mod open while it runs.
/// Only an install made by that setup is updated this way: one copied into place by hand
/// is told about the new version instead.
/// Modes: "auto" installs by itself, "ask" waits for the player's Update button, "off"
/// does not even look.
/// </summary>
internal sealed class Updater : IDisposable
{
    private const string Repo = "https://github.com/timurmert/ets2-cabinplay";
    // packaging/setup.iss: never changes, Windows finds the install through it.
    private const string UninstallKey =
        @"Software\Microsoft\Windows\CurrentVersion\Uninstall\{EA7CAAE8-A55D-479D-86EB-8C4F281D283D}_is1";
    private const int MaxTries = 3; // setups of one version run at most this often
    // Soon after the start, so even a short drive is long enough to fetch an update; the
    // check is one small request and the download about 55 MB.
    private static readonly TimeSpan FirstCheck = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private static readonly TimeSpan RetryAfterFailure = TimeSpan.FromMinutes(30);

    // A test instance can pretend to be an older version to try the download (it never installs).
    public static readonly Version Current =
        Program.Namespace.Length > 0 && Version.TryParse(Environment.GetEnvironmentVariable("CABINPLAY_TEST_VERSION"), out Version? test)
            ? Normalize(test)
            : Normalize(typeof(Updater).Assembly.GetName().Version);

    private readonly string _folder = Path.Combine(Program.DataDir, "updates");
    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly CancellationTokenSource _stop = new();
    private string _mode;
    private bool _approved;  // "ask": the player pressed Update
    private bool _started;   // the setup is running already
    private bool _busy;

    /// <summary>"idle" (not looked yet), "off", "checking", "current", "downloading", "ready", "manual" or "failed".</summary>
    public string State { get; private set; } = "idle";
    public Version? Latest { get; private set; }
    private string? _setup; // downloaded and checked, waiting for the game to close

    /// <summary>The version this one replaced, when this is the first start after an update.</summary>
    private readonly Version? _updatedFrom;

    /// <summary>Raised on the UI thread whenever <see cref="State"/> changes.</summary>
    public event Action? Changed;

    public Updater(string? mode, string? lastVersion)
    {
        _mode = mode is "ask" or "off" ? mode : "auto";
        if (Version.TryParse(lastVersion, out Version? last) && Normalize(last) < Current)
            _updatedFrom = Normalize(last);
        State = _mode == "off" ? "off" : "idle";
        _timer.Tick += async (_, _) => await CheckAsync();
        _timer.Interval = Program.Namespace.Length > 0 ? 5000 : (int)FirstCheck.TotalMilliseconds;
        if (_mode != "off")
            _timer.Start();
    }

    public string Mode
    {
        get => _mode;
        set
        {
            value = value is "ask" or "off" ? value : "auto";
            if (_mode == value)
                return;
            bool wasOff = _mode == "off";
            _mode = value;
            _approved = false;
            Log.Write("updates: " + value);
            if (value == "off")
            {
                _timer.Stop();
                _setup = null;
                SetState("off");
            }
            else if (wasOff)
            {
                _timer.Interval = (int)Interval.TotalMilliseconds;
                _timer.Start();
                _ = CheckAsync();
            }
            Changed?.Invoke();
        }
    }

    /// <summary>Whether the downloaded setup runs once the game has closed.</summary>
    private bool WillInstall => _mode == "auto" || (_mode == "ask" && _approved);

    /// <summary>What the screen shows: the settings line and the card on the home screen.</summary>
    public JsonObject ToJson() => new()
    {
        ["mode"] = _mode,
        ["state"] = State,
        ["current"] = Current.ToString(3),
        ["latest"] = Latest?.ToString(3),
        ["willInstall"] = WillInstall,
        ["updatedFrom"] = _updatedFrom?.ToString(3),
    };

    private void SetState(string state)
    {
        if (State == state)
            return;
        State = state;
        Changed?.Invoke();
    }

    /// <summary>
    /// The player pressed Update. With the game closed (the app was opened on its own) the
    /// setup runs right away and starts the new app afterwards; true means the app should
    /// now close. While the game runs, the setup runs once it has closed.
    /// </summary>
    public bool UpdateNow(string language)
    {
        if (_mode == "off" || State != "ready" || _setup is null)
            return false;
        _approved = true;
        Changed?.Invoke();
        if (GameRunning || Program.Namespace.Length > 0)
            return false;
        return StartSetup(language, restartApp: true);
    }

    /// <summary>
    /// True when the setup put this copy of the app where it runs from, so running a newer
    /// setup updates this copy and not another one.
    /// </summary>
    private static bool InstalledBySetup()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(UninstallKey);
            if (key?.GetValue("InstallLocation") is not string place || place.Length == 0)
                return false;
            return string.Equals(Path.GetFullPath(place).TrimEnd('\\'),
                                 Path.GetFullPath(AppContext.BaseDirectory).TrimEnd('\\'),
                                 StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// The version the setup last finished installing. The setup records it only after
    /// copying every file, so a setup cut short (power cut, Windows shutting down) leaves
    /// it older than this app and the update is run again.
    /// </summary>
    private static Version Installed()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(UninstallKey);
            if (InstalledBySetup() && key?.GetValue("DisplayVersion") is string text &&
                Version.TryParse(text, out Version? v) && Normalize(v) < Current)
                return Normalize(v);
        }
        catch (Exception)
        {
        }
        return Current;
    }

    public async Task CheckAsync()
    {
        if (_mode == "off" || _busy)
            return;
        _busy = true;
        try
        {
            if (State is not ("ready" or "downloading"))
                SetState("checking");
            Version? latest = await LatestReleaseAsync(_stop.Token);
            if (latest is null)
            {
                SetState("failed");
                return;
            }
            Latest = latest;
            Version installed = Installed();
            if (installed < Current)
                Log.Write($"updates: the setup of {Current.ToString(3)} did not finish; running it again");
            if (latest <= installed)
            {
                Log.Write($"updates: {Current.ToString(3)} is the latest");
                CleanUp(keep: null);
                SetState("current");
                return;
            }
            if (!InstalledBySetup() && Program.Namespace.Length == 0)
            {
                Log.Write($"updates: {latest.ToString(3)} is out; this copy was not installed by the setup");
                SetState("manual");
                return;
            }

            string name = $"CabinPlay-Setup-{latest.ToString(3)}.exe";
            string file = Path.Combine(_folder, name);
            CleanUp(keep: name);
            if (Tries(file) >= MaxTries)
            {
                Log.Write($"updates: the setup of {latest.ToString(3)} failed {MaxTries} times; leaving it to the player");
                SetState("manual");
                return;
            }
            if (!IsSetupOf(file, latest))
            {
                SetState("downloading");
                Log.Write($"updates: downloading {name}");
                await DownloadAsync($"{Repo}/releases/download/v{latest.ToString(3)}/{name}", file, _stop.Token);
                if (!IsSetupOf(file, latest))
                {
                    Log.Write("updates: the downloaded file is not that setup");
                    File.Delete(file);
                    SetState("failed");
                    return;
                }
            }
            _setup = file;
            Log.Write($"updates: {latest.ToString(3)} is ready" +
                      (WillInstall ? " and installs once the game has closed" : "; waiting for the player's Update"));
            SetState("ready");
        }
        catch (Exception ex) when (!_stop.IsCancellationRequested) // a timeout too, not only closing
        {
            Log.Write("updates: " + ex.Message);
            SetState(_setup is null ? "failed" : "ready");
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _busy = false;
            // No connection for a moment: try again soon rather than in hours.
            if (_mode != "off" && !_stop.IsCancellationRequested)
                _timer.Interval = (int)(State == "failed" ? RetryAfterFailure : Interval).TotalMilliseconds;
        }
    }

    /// <summary>
    /// The release page answers /releases/latest with a redirect to the tag of the newest
    /// release, without the API's hourly limit.
    /// </summary>
    private static async Task<Version?> LatestReleaseAsync(CancellationToken cancel)
    {
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"CabinPlay/{Current.ToString(3)}");
        using var response = await http.GetAsync($"{Repo}/releases/latest", HttpCompletionOption.ResponseHeadersRead, cancel);
        string? tag = response.Headers.Location?.OriginalString.Split('/').LastOrDefault();
        if (tag is null || !tag.StartsWith('v') || !Version.TryParse(tag[1..], out Version? version))
        {
            Log.Write($"updates: no release found ({(int)response.StatusCode})");
            return null;
        }
        return Normalize(version);
    }

    /// <summary>Into a side file first, so a broken download is never taken for a setup.</summary>
    private static async Task DownloadAsync(string url, string file, CancellationToken cancel)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        string part = file + ".part";
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"CabinPlay/{Current.ToString(3)}");
        using (var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancel))
        {
            response.EnsureSuccessStatusCode();
            await using var source = await response.Content.ReadAsStreamAsync(cancel);
            await using var target = File.Create(part);
            await source.CopyToAsync(target, cancel);
        }
        File.Move(part, file, overwrite: true);
    }

    /// <summary>The setup carries its version (VersionInfoVersion in setup.iss).</summary>
    private static bool IsSetupOf(string file, Version version)
    {
        if (!File.Exists(file))
            return false;
        FileVersionInfo info = FileVersionInfo.GetVersionInfo(file);
        return info.FileDescription?.Trim() == "CabinPlay Setup" // Inno Setup pads it with spaces
            && new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart) == version;
    }

    /// <summary>Removes setups of other versions and broken downloads.</summary>
    private void CleanUp(string? keep)
    {
        if (!Directory.Exists(_folder))
            return;
        foreach (string file in Directory.EnumerateFiles(_folder))
        {
            string name = Path.GetFileName(file);
            if (keep is not null && name.StartsWith(keep, StringComparison.OrdinalIgnoreCase) && !name.EndsWith(".part"))
                continue;
            if (name == "setup.log")
                continue;
            try
            {
                File.Delete(file);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    private static int Tries(string setup) =>
        int.TryParse(ReadOrNull(setup + ".tries"), out int n) ? n : 0;

    private static string? ReadOrNull(string file)
    {
        try
        {
            return File.Exists(file) ? File.ReadAllText(file).Trim() : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static bool GameRunning => Process.GetProcessesByName("eurotrucks2").Length > 0;

    /// <summary>
    /// Called as the app closes: runs the downloaded setup once the game has gone.
    /// The setup closes this app itself if it is still on its way out, and puts the new one
    /// where the plugin starts it from with the game next time. <paramref name="gameClosed"/>
    /// is true when the app closes because the game did (the game's process may need a few
    /// more seconds to end); closed by hand while the game runs, the update waits.
    /// </summary>
    public void InstallIfReady(bool gameClosed, string language)
    {
        if (!WillInstall || _started || _setup is null || !File.Exists(_setup) || Program.Namespace.Length > 0)
            return;
        if (ShuttingDown)
        {
            Log.Write("updates: Windows is shutting down; installing next time");
            return;
        }
        if (GameRunning && !gameClosed)
            return;
        // Waiting must not keep a new start of the app (the game started again) from running.
        Program.LeaveSingleInstance();
        // The plugin and the mod stay locked until the game's process has really ended.
        var wait = Stopwatch.StartNew();
        while (GameRunning)
        {
            if (wait.Elapsed > TimeSpan.FromSeconds(60) || ShuttingDown)
            {
                Log.Write("updates: the game is still running; installing next time");
                return;
            }
            Thread.Sleep(1000);
        }
        StartSetup(language, restartApp: false);
    }

    private bool StartSetup(string language, bool restartApp)
    {
        if (_setup is null)
            return false;
        try
        {
            File.WriteAllText(_setup + ".tries", (Tries(_setup) + 1).ToString());
            string log = Path.Combine(_folder, "setup.log");
            // No questions, but a small progress window in the player's language that says it
            // updates CabinPlay from this version (/UPDATE, read by setup.iss), so the player
            // sees what runs. The languages are the setup's too.
            if (language is not ("en" or "tr" or "de" or "pl" or "fr" or "es" or "ru"))
                language = "en";
            string args = $"/SILENT /SUPPRESSMSGBOXES /NORESTART /LANG={language} /UPDATE={Current.ToString(3)}";
            if (restartApp)
                args += " /RESTARTAPP=1";
            Process.Start(new ProcessStartInfo(_setup, $"{args} /LOG=\"{log}\"")
            {
                UseShellExecute = false,
                WorkingDirectory = _folder,
            });
            _started = true;
            Log.Write($"updates: installing {Latest?.ToString(3)}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Write("updates: setup did not start: " + ex.Message);
            return false;
        }
    }

    /// <summary>A setup started now would be cut off half way.</summary>
    private static bool ShuttingDown => GetSystemMetrics(0x2000 /* SM_SHUTTINGDOWN */) != 0;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    private static Version Normalize(Version? v) =>
        v is null ? new Version(0, 0, 0) : new Version(v.Major, v.Minor, Math.Max(0, v.Build));

    public void Dispose()
    {
        _timer.Dispose();
        _stop.Cancel();
        _stop.Dispose();
    }
}
