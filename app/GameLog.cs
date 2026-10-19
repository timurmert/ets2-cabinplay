using System.Text;
using System.Text.RegularExpressions;

namespace CabinPlay;

/// <summary>
/// Follows the game's own log (Documents\Euro Truck Simulator 2\game.log.txt) for what the
/// plugin cannot see: the game's version and, each time a profile is loaded, which mods are
/// active in it. Only what was added since the last look is read.
/// </summary>
internal sealed partial class GameLog
{
    /// <summary>The game versions CabinPlay was tried with. Keep in sync with GAME_VERSION in tools/build_mod.py.</summary>
    public const string TestedVersions = "1.61";

    // A test instance (see Program.Namespace) can be given another log to read.
    public static readonly string LogPath =
        Program.Namespace.Length > 0 && Environment.GetEnvironmentVariable("CABINPLAY_TEST_GAME_LOG") is { Length: > 0 } test
            ? test
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Euro Truck Simulator 2", "game.log.txt");

    private readonly string _path;
    private long _offset;
    private DateTime _created;
    private readonly List<byte> _partial = new();
    private int _modsLeft;               // lines of the current "Active N mods" list still to come
    private bool _sawCabinPlay;

    public GameLog(string? path = null) => _path = path ?? LogPath;

    /// <summary>"1.61.1.9", or null until the game has written it.</summary>
    public string? GameVersion { get; private set; }

    /// <summary>The profile loaded last, or null.</summary>
    public string? Profile { get; private set; }

    /// <summary>Whether the CabinPlay mod is active in that profile; null until its mods are listed.</summary>
    public bool? ModActive { get; private set; }

    /// <summary>The version of the CabinPlay mod that is active, as its manifest says.</summary>
    public string? ModVersion { get; private set; }

    /// <summary>Bumped each time a profile's mods have been listed.</summary>
    public int ModLists { get; private set; }

    public bool VersionTested => GameVersion is null || GameVersion == TestedVersions ||
                                 GameVersion.StartsWith(TestedVersions + ".", StringComparison.Ordinal);

    [GeneratedRegex(@"init ver\.(\d+(?:\.\d+)+)")]
    private static partial Regex VersionLine();

    [GeneratedRegex(@"New profile selected: '(.*)'")]
    private static partial Regex ProfileLine();

    [GeneratedRegex(@"\[mods\] Active (\d+) mods")]
    private static partial Regex ModsLine();

    // Package names may have spaces in them ("Obm_v1.5 1.61").
    [GeneratedRegex(@"\[mods\] Active (?:local mod (.+?)|workshop mod ID \S+) \(name: (.*?), version: (.*?), author:")]
    private static partial Regex ModLine();

    /// <summary>Reads what the game has written since the last call. True when anything above changed.</summary>
    public bool Poll()
    {
        try
        {
            var info = new FileInfo(_path);
            if (!info.Exists)
                return false;
            // The game starts a new log each time it starts.
            if (info.Length < _offset || info.CreationTimeUtc != _created)
                Reset(info.CreationTimeUtc);
            if (info.Length == _offset)
                return false;
            var before = (GameVersion, Profile, ModActive, ModVersion, ModLists);
            using (var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                stream.Seek(_offset, SeekOrigin.Begin);
                var buffer = new byte[64 * 1024];
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    _offset += read;
                    int start = 0;
                    for (int i = 0; i < read; i++)
                    {
                        if (buffer[i] != (byte)'\n')
                            continue;
                        _partial.AddRange(new ArraySegment<byte>(buffer, start, i - start));
                        Line(Encoding.UTF8.GetString(_partial.ToArray()).TrimEnd('\r'));
                        _partial.Clear();
                        start = i + 1;
                    }
                    _partial.AddRange(new ArraySegment<byte>(buffer, start, read - start));
                }
            }
            return before != (GameVersion, Profile, ModActive, ModVersion, ModLists);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private void Reset(DateTime created)
    {
        _created = created;
        _offset = 0;
        _partial.Clear();
        _modsLeft = 0;
        GameVersion = Profile = ModVersion = null;
        ModActive = null;
    }

    private void Line(string line)
    {
        if (_modsLeft > 0)
        {
            Match mod = ModLine().Match(line);
            if (mod.Success)
            {
                // Found by its package name (cabinplay.scs, also if renamed to "cabinplay (1).scs"
                // and the like) or by the name in its manifest.
                bool ours = mod.Groups[1].Value.StartsWith("cabinplay", StringComparison.OrdinalIgnoreCase) ||
                            mod.Groups[2].Value == "CabinPlay Screen";
                if (ours && !_sawCabinPlay)
                {
                    _sawCabinPlay = true;
                    ModVersion = mod.Groups[3].Value;
                }
                if (--_modsLeft == 0)
                    EndModList();
                return;
            }
            EndModList(); // a list cut short
        }
        Match m;
        if (GameVersion is null && (m = VersionLine().Match(line)).Success)
        {
            GameVersion = m.Groups[1].Value;
        }
        else if ((m = ProfileLine().Match(line)).Success)
        {
            Profile = m.Groups[1].Value;
            ModActive = null;
            ModVersion = null;
        }
        else if ((m = ModsLine().Match(line)).Success)
        {
            _modsLeft = int.TryParse(m.Groups[1].Value, out int n) ? n : 0;
            _sawCabinPlay = false;
            ModVersion = null;
            if (_modsLeft == 0)
                EndModList();
        }
    }

    private void EndModList()
    {
        _modsLeft = 0;
        ModActive = _sawCabinPlay;
        ModLists++;
    }

    /// <summary>For the error report: what the log says, or why it says nothing.</summary>
    public static string Describe()
    {
        var log = new GameLog();
        log.Poll();
        if (!File.Exists(LogPath))
            return "Game log: not found at " + LogPath;
        return $"Game log: version {log.GameVersion ?? "?"}{(log.VersionTested ? "" : $" (not tested, CabinPlay is for {TestedVersions})")}, " +
               $"profile '{log.Profile ?? "?"}', CabinPlay mod " +
               (log.ModActive switch { true => $"active (version {log.ModVersion})", false => "NOT active in this profile", _ => "not listed yet" });
    }
}
