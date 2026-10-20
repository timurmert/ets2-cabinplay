using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace CabinPlay;

/// <summary>
/// The CabinPlay screen: a borderless window exactly the size of the in-cabin display.
/// The shell page (dock, home screen, maps) fills it; each opened app gets its own
/// browser view layered over the area right of the dock.
/// </summary>
internal sealed class MainForm : Form
{
    private const int DockWidth = 88; // keep in sync with --dock-w in ui/style.css
    private const int BarHeight = 46; // the browser's address bar; keep in sync with --bar-h in ui/style.css
    private const int PaneGap = 2;    // between two apps side by side; keep in sync with --pane-w in ui/style.css
    private const string ShellHost = "cabinplay.local";
    private const string WallpaperHost = "wallpapers.cabinplay.local";

    // Chromium stops painting windows it believes are hidden; the game covers this one.
    // The colour profile is pinned so the map key colour reaches the plugin unchanged.
    private const string BrowserArguments =
        "--disable-features=CalculateNativeWinOcclusion " +
        "--disable-backgrounding-occluded-windows --disable-renderer-backgrounding " +
        "--autoplay-policy=no-user-gesture-required --force-color-profile=srgb";

    private sealed class AppView
    {
        public required string Id;
        public required WebView2 View;
        public string? NextSelector, PrevSelector, FullscreenKey;
        public bool Fullscreen;
        public bool Bar; // the shell's address bar stays visible above the page
    }

    private readonly WebView2 _shell = new();
    private readonly Dictionary<string, AppView> _apps = new();
    private readonly System.Windows.Forms.Timer _stateTimer = new() { Interval = 250 };
    private readonly System.Windows.Forms.Timer _inputTimer = new() { Interval = 10 };
    private readonly NotifyIcon _tray = new();
    private readonly string _settingsPath = Path.Combine(Program.DataDir, "settings.json");
    private readonly string _wallpaperDir = Path.Combine(Program.DataDir, "wallpapers");
    private JsonObject _settings = new();
    private readonly string? _dumpPath;
    private readonly string? _startApp;
    private readonly string? _startUrl;
    private readonly int _dumpDelayMs = 6000;
    private readonly bool _background;  // started by the game plugin: stay out of sight, leave with the game
    private readonly string? _startLanguage;
    private bool _offscreen;
    private Point _restoreLocation;
    private bool _everConnected;
    private bool _leftWithGame;     // closing because the game closed (or crashed)
    private long _lastConnectedTick = Environment.TickCount64;
    private readonly ToolStripMenuItem _showItem = new(), _hideItem = new(), _topMostItem = new() { CheckOnClick = true },
                                       _centerItem = new(), _exitItem = new();
    private readonly GameEvent[] _events = new GameEvent[256];

    private CoreWebView2Environment? _environment;
    private FrameWriter? _writer;
    private WindowCapture? _capture;
    private GameLink? _game;
    private GameNotice? _notice;
    private SiteIcons? _icons; // logos on the browser's start page
    private readonly NoticeWatch _watch;
    private readonly Dictionary<string, (string Title, string Text)> _noticeTexts = new(); // from the shell, in its language
    private string? _activeId;          // app shown right now, null while the shell is in front
    private string? _mediaId;           // app the media controls act on
    private string _shellView = "home"; // what the shell shows behind the apps: "home", "maps", "mirrors", "truck", "browser" or "split"
    private (string Left, string Right)? _split; // two apps side by side; _activeId is then the one with the keyboard
    private string? _paneStart;         // side by side, the app whose half shows the shell's start page instead
    private bool _shellKeys;            // the shell's address field is being typed in
    private ushort[]? _navSource, _navTarget; // the mirror views' rectangles, from the shell
    private bool _stateBusy;
    private int _stateTick;
    private bool _holdingMedia;         // media was paused because the game paused or the ignition is off

    // shortcuts: the key that goes with Ctrl+Alt and the controller button of each action
    private readonly Dictionary<string, int> _keys = new();
    private readonly Dictionary<string, (PadButton Button, string Device)> _pads = new();
    private Controllers? _controllers;
    private Updater? _updater;
    private string? _capturing;        // action waiting for a controller button to be pressed
    private long _captureUntil;

    // control mode: where the in-game cursor's button press landed, for drags
    private WebView2? _dragView;
    private Point _dragOrigin;
    private int _buttonsDown;
    private long _lastClickTick;
    private Point _lastClickAt;
    private int _clickCount;

    public MainForm(string[] args)
    {
        _dumpPath = Argument(args, "--dump-frame");
        _startApp = Argument(args, "--open");
        _startUrl = Argument(args, "--url");
        if (int.TryParse(Argument(args, "--dump-delay"), out int ms))
            _dumpDelayMs = ms;
        _background = args.Contains("--background");
        _startLanguage = Argument(args, "--lang");

        Text = "CabinPlay";
        FormBorderStyle = FormBorderStyle.None;
        AutoScaleMode = AutoScaleMode.None;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.Black;
        ClientSize = new Size(FrameWriter.Width, FrameWriter.Height);
        Icon = CreateIcon();
        RestorePlacement();
        LoadBindings();
        // What was drawn over the game on earlier runs is not drawn again.
        _watch = new NoticeWatch(((_settings["noticesSeen"] as JsonArray) ?? new JsonArray())
                                 .Select(n => (string?)n).OfType<string>());
        _watch.InGameEnabled = (bool?)_settings["gameNotices"] ?? true;
        _watch.Seen += () =>
        {
            _settings["noticesSeen"] = new JsonArray(_watch.SeenList.Select(k => (JsonNode?)k).ToArray());
            SaveSettings();
        };
        _restoreLocation = Location;
        if (_background)
        {
            // Decided once, before the window exists: changing it later would recreate the
            // window and break the capture that is attached to it.
            ShowInTaskbar = false;
            MoveOffscreen();
        }

        _shell.DefaultBackgroundColor = Color.Black;
        _shell.Bounds = ClientRectangle;
        Controls.Add(_shell);

        _tray.Icon = Icon;
        _tray.Text = "CabinPlay";
        _tray.Visible = true;
        _tray.DoubleClick += (_, _) => BringToUser();
        var menu = new ContextMenuStrip();
        _showItem.Click += (_, _) => BringToUser();
        _hideItem.Click += (_, _) => MoveOffscreen();
        _topMostItem.Checked = TopMost;
        _topMostItem.CheckedChanged += (_, _) => { TopMost = _topMostItem.Checked; SavePlacement(); };
        _centerItem.Click += (_, _) =>
        {
            Rectangle area = Screen.PrimaryScreen!.WorkingArea;
            _restoreLocation = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
            BringToUser();
        };
        _exitItem.Click += (_, _) => Close();
        menu.Items.AddRange(new ToolStripItem[] { _showItem, _hideItem, _topMostItem, _centerItem, new ToolStripSeparator(), _exitItem });
        _tray.ContextMenuStrip = menu;
        ApplyLanguage(Strings.SystemLanguage);

        _stateTimer.Tick += async (_, _) => await OnStateTickAsync();
        _inputTimer.Tick += (_, _) => PumpGameInput();
    }

    /// <summary>A test run must not pull focus away from a game that is being played.</summary>
    protected override bool ShowWithoutActivation => _dumpPath is not null || _background;

    /// <summary>
    /// Parks the window beyond the edge of the desktop. It has to stay a real, shown window
    /// to keep being captured, so "hidden" means out of sight rather than invisible.
    /// </summary>
    private void MoveOffscreen()
    {
        if (!_offscreen)
            _restoreLocation = Location;
        _offscreen = true;
        Rectangle all = SystemInformation.VirtualScreen;
        Location = new Point(all.Right + 64, all.Bottom + 64);
    }

    private string _language = Strings.SystemLanguage;

    private void ApplyLanguage(string language)
    {
        _language = language;
        _showItem.Text = Strings.Get(language, "show");
        _hideItem.Text = Strings.Get(language, "hide");
        _topMostItem.Text = Strings.Get(language, "topmost");
        _centerItem.Text = Strings.Get(language, "center");
        _exitItem.Text = Strings.Get(language, "exit");
    }

    private static string? Argument(string[] args, string name)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    // ------------------------------------------------------------------ startup

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        try
        {
            _environment = await CoreWebView2Environment.CreateAsync(
                null, Path.Combine(Program.DataDir, "WebView2"),
                new CoreWebView2EnvironmentOptions(BrowserArguments));
            _environment.ProcessInfosChanged += (_, _) => RaisePriorities();

            await _shell.EnsureCoreWebView2Async(_environment);
            CoreWebView2 core = _shell.CoreWebView2;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsZoomControlEnabled = false;
            core.Settings.AreBrowserAcceleratorKeysEnabled = false;
            core.Settings.IsNonClientRegionSupportEnabled = true; // lets the dock clock drag the window
            core.SetVirtualHostNameToFolderMapping(
                ShellHost, Path.Combine(AppContext.BaseDirectory, "ui"), CoreWebView2HostResourceAccessKind.Allow);
            Directory.CreateDirectory(_wallpaperDir);
            core.SetVirtualHostNameToFolderMapping(
                WallpaperHost, _wallpaperDir, CoreWebView2HostResourceAccessKind.Allow);
            _icons = new SiteIcons(core.Settings.UserAgent);
            _icons.Changed += () => BeginInvoke(PushConfig);
            core.SetVirtualHostNameToFolderMapping(SiteIcons.Host, _icons.Folder, CoreWebView2HostResourceAccessKind.Allow);
            core.WebMessageReceived += OnShellMessage;
            _shell.ZoomFactor = PixelZoom;
            await PrepareForInjectedInputAsync(core);
            string query = _startApp is null ? "" : "?open=" + Uri.EscapeDataString(_startApp);
            if (_startApp is not null && _startUrl is not null)
                query += "&url=" + Uri.EscapeDataString(_startUrl);
            if (_startLanguage is not null)
                query += (query.Length == 0 ? "?" : "&") + "lang=" + Uri.EscapeDataString(_startLanguage);
            core.Navigate($"https://{ShellHost}/index.html{query}");

            _writer = new FrameWriter();
            ApplyControlOptions();
            _game = new GameLink();
            _notice = new GameNotice();
            _capture = new WindowCapture(_writer);
            _capture.Start(Handle);
            _controllers = new Controllers();
            _controllers.Pressed += (button, device) =>
            {
                if (IsHandleCreated && !IsDisposed)
                    BeginInvoke(() => OnPadButton(button, device));
            };
            _stateTimer.Start();
            _inputTimer.Start();
            // The version that ran last, so the first start after an update can say so.
            _updater = new Updater((string?)_settings["updateMode"], (string?)_settings["lastVersion"]);
            _updater.Changed += PushConfig;
            if (Program.Namespace.Length == 0)
            {
                _settings["lastVersion"] = Updater.Current.ToString(3);
                SaveSettings();
            }
            Log.Write($"started, version {Updater.Current.ToString(3)}");

            if (_dumpPath is not null)
                _ = DumpFrameLaterAsync(_dumpPath);
        }
        catch (Exception ex)
        {
            Log.Write("startup failed: " + ex);
            MessageBox.Show(this, Strings.Get(Strings.SystemLanguage, "startfail") + "\n\n" + ex.Message, Text,
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
        }
    }

    /// <summary>One CSS pixel per screen-texture pixel, whatever the monitor scaling is.</summary>
    private double PixelZoom => 96.0 / DeviceDpi;

    /// <summary>
    /// Input from the game arrives while this window is in the background; pages must
    /// treat it as if they had the keyboard.
    /// </summary>
    private static async Task PrepareForInjectedInputAsync(CoreWebView2 core)
    {
        try
        {
            await core.CallDevToolsProtocolMethodAsync("Emulation.setFocusEmulationEnabled", "{\"enabled\":true}");
        }
        catch (Exception ex)
        {
            Log.Write("focus emulation unavailable: " + ex.Message);
        }
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        ClientSize = new Size(FrameWriter.Width, FrameWriter.Height);
        _shell.ZoomFactor = PixelZoom;
        foreach (AppView app in _apps.Values)
            ApplyZoom(app);
        LayoutViews();
    }

    /// <summary>
    /// The height a YouTube short needs: the site's bar and a player it never makes smaller
    /// than 576 pixels. The screen has 466, so a short loses its bottom part unless the page
    /// is shrunk.
    /// </summary>
    private const double ShortsPageHeight = 648;

    private bool FitShorts => (bool?)_settings["fitShorts"] ?? true;

    /// <summary>
    /// One CSS pixel per screen pixel, except on YouTube Shorts (unless switched off in the
    /// settings): there the page is shrunk until the whole short fits on the screen.
    /// </summary>
    private void ApplyZoom(AppView app)
    {
        double zoom = PixelZoom;
        if (FitShorts && app.View.Height > 0 && IsYouTubeShorts(app.View.CoreWebView2?.Source))
            zoom *= Math.Min(1, app.View.Height / ShortsPageHeight);
        if (Math.Abs(app.View.ZoomFactor - zoom) > 0.001)
            app.View.ZoomFactor = zoom;
    }

    private static bool IsYouTubeShorts(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) &&
        (uri.Host == "youtube.com" || uri.Host.EndsWith(".youtube.com")) && uri.AbsolutePath.StartsWith("/shorts");

    // ------------------------------------------------------------------ shell <-> host

    private async void OnShellMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            JsonNode? msg = JsonNode.Parse(e.WebMessageAsJson);
            switch ((string?)msg?["type"])
            {
                case "open":
                    await OpenAppAsync(msg!);
                    break;
                case "view":
                    ShowShell((string?)msg!["view"] switch
                    {
                        "maps" => "maps", "mirrors" => "mirrors", "truck" => "truck", "browser" => "browser",
                        "split" => "split", _ => "home",
                    });
                    break;
                case "split":
                    await SplitAsync(msg!["left"], msg["right"]);
                    break;
                case "ready":
                    PushConfig();
                    return;
                case "wallpaper-set":
                    _settings["wallpaper"] = (string?)msg!["name"];
                    SaveSettings();
                    PushConfig();
                    return;
                case "wallpaper-add":
                    AddWallpaper((string?)msg!["file"]);
                    return;
                case "wallpaper-remove":
                    RemoveWallpaper((string?)msg!["name"]);
                    return;
                case "bind-key":
                    BindKey((string?)msg!["action"], (int?)msg["vk"] ?? 0);
                    return;
                case "bind-pad":
                    // "capture" waits for the next button pressed, anything else takes the button away
                    BindPad((string?)msg!["action"], (string?)msg["mode"] == "capture");
                    return;
                case "bind-reset":
                    DefaultBindings();
                    return;
                case "notice-text":
                    if ((string?)msg!["id"] is string noticeId)
                        _noticeTexts[noticeId] = ((string?)msg["title"] ?? "", (string?)msg["text"] ?? "");
                    UpdateNotice();
                    return;
                case "shell-keys":
                    _shellKeys = (bool?)msg!["on"] ?? false;
                    return; // nothing the shell shows has changed
                case "nav-rects":
                    _navSource = Rect(msg!["source"]);
                    _navTarget = Rect(msg!["target"]);
                    LayoutViews();
                    break;
                case "browser":
                    BrowserCommand((string?)msg!["action"], (string?)msg["id"]);
                    break;

                case "media":
                    await MediaCommandAsync((string?)msg!["action"] ?? "");
                    break;
                case "close-app":
                    CloseApp((string?)msg!["id"]);
                    break;
                case "quit":
                    Close();
                    break;
                case "language":
                    ApplyLanguage((string?)msg!["language"] ?? Strings.SystemLanguage);
                    break;
                case "diagnostics":
                    Diagnostics.Export();
                    break;
                case "game-notices":
                    _watch.InGameEnabled = (bool?)msg!["on"] ?? true;
                    _settings["gameNotices"] = _watch.InGameEnabled;
                    SaveSettings();
                    PushConfig();
                    return;
                case "control-option":
                    SetControlOption((string?)msg!["name"], (int?)msg["value"] ?? 0);
                    return;
                case "drive-wheel":
                    SetDriveWheel((string?)msg!["mode"]);
                    return;
                case "fit-shorts":
                    _settings["fitShorts"] = (bool?)msg!["on"] ?? true;
                    SaveSettings();
                    foreach (AppView app in _apps.Values)
                        ApplyZoom(app);
                    PushConfig();
                    return;
                case "site-icons":
                    _icons?.Want(((msg!["urls"] as JsonArray) ?? new JsonArray()).Select(u => (string?)u).OfType<string>());
                    return;
                case "update-mode":
                    if (_updater is null)
                        return;
                    _updater.Mode = (string?)msg!["mode"] ?? "auto";
                    _settings["updateMode"] = _updater.Mode;
                    SaveSettings();
                    PushConfig();
                    return;
                case "update-now":
                    // With the game closed the setup runs now and starts the new app itself.
                    if (_updater?.UpdateNow(_language) == true)
                        Close();
                    return;
            }
            await PushStateAsync(withMedia: true);
        }
        catch (Exception ex)
        {
            Log.Write("shell message failed: " + ex.Message);
        }
    }

    private async Task OpenAppAsync(JsonNode msg)
    {
        AppView? app = await EnsureAppAsync(msg);
        if (app is null)
            return;
        if (app.Bar)
            _shellView = "browser"; // the bar is the shell's
        _split = null;
        _paneStart = null;
        _activeId = _mediaId = app.Id;
        LayoutViews();
    }

    /// <summary>The app's browser view, created and sent to its page on first use.</summary>
    private async Task<AppView?> EnsureAppAsync(JsonNode msg)
    {
        string? id = (string?)msg["id"];
        string? url = (string?)msg["url"];
        if (id is null || url is null || _environment is null)
            return null;

        if (!_apps.TryGetValue(id, out AppView? app))
        {
            var view = new WebView2 { DefaultBackgroundColor = Color.Black, Visible = false };
            Controls.Add(view);
            app = new AppView
            {
                Id = id,
                View = view,
                NextSelector = (string?)msg["next"],
                PrevSelector = (string?)msg["prev"],
                FullscreenKey = (string?)msg["fullscreenKey"],
                Bar = (bool?)msg["bar"] ?? false,
            };
            _apps[id] = app;
            await view.EnsureCoreWebView2Async(_environment);
            CoreWebView2 core = view.CoreWebView2;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsZoomControlEnabled = false;
            // Keep everything inside the one screen instead of spawning browser windows.
            core.NewWindowRequested += (_, a) => { a.Handled = true; core.Navigate(a.Uri); };
            AppView captured = app;
            core.ContainsFullScreenElementChanged += (_, _) =>
            {
                captured.Fullscreen = core.ContainsFullScreenElement;
                LayoutViews();
            };
            // Keeps the dock's back and forward buttons current.
            core.HistoryChanged += async (_, _) => await PushStateAsync(withMedia: false);
            // A start page logo the site would not hand out: the one the browser shows for it.
            core.FaviconChanged += async (_, _) =>
            {
                if (_icons is not null)
                    await _icons.TakeFromBrowserAsync(core);
            };
            // Side by side, the app that was clicked last has the keyboard and the media keys.
            view.GotFocus += (_, _) => FocusPane(captured.Id);
            // YouTube goes to Shorts and back without loading a page, so this is the moment to look.
            core.SourceChanged += (_, _) => ApplyZoom(captured);
            view.ZoomFactor = PixelZoom;
            await PrepareForInjectedInputAsync(core);
            core.Navigate(url);
        }
        else if (((bool?)msg["navigate"] ?? false) && app.View.CoreWebView2 is not null)
        {
            app.View.CoreWebView2.Navigate(url);
        }
        return app;
    }

    /// <summary>
    /// Two apps side by side. A side is either a site (with the fields of an "open" message)
    /// or one of the shell's own views, which the shell draws in that half itself.
    /// </summary>
    private async Task SplitAsync(JsonNode? left, JsonNode? right)
    {
        string? l = (string?)left?["id"], r = (string?)right?["id"];
        if (l is null || r is null || l == r)
            return;
        string? focus = null;
        _paneStart = null;
        foreach (JsonNode side in new[] { left!, right! })
        {
            if (side["url"] is not null)
                await EnsureAppAsync(side);
            if ((bool?)side["navigate"] == true)
                focus = (string?)side["id"]; // a page picked in that half: the keyboard goes there
            if ((bool?)side["start"] == true)
                _paneStart = (string?)side["id"];
        }
        _split = (l, r);
        _shellView = "home"; // where leaving the pair falls back to
        bool InView(string? id) => id is not null && _apps.ContainsKey(id) && id != _paneStart;
        if (InView(focus))
            _activeId = focus;
        else if (_activeId != l && _activeId != r)
            _activeId = InView(r) ? r : InView(l) ? l : null;
        else if (!InView(_activeId))
            _activeId = InView(l) ? l : InView(r) ? r : null;
        if (_activeId is not null && _mediaId != l && _mediaId != r)
            _mediaId = _activeId;
        LayoutViews();
    }

    private void FocusPane(string id)
    {
        if (_split is not { } s || (id != s.Left && id != s.Right) || _activeId == id || !_apps.ContainsKey(id) ||
            id == _paneStart)
            return;
        _activeId = _mediaId = id;
        _ = PushStateAsync(withMedia: true);
    }

    /// <summary>Where each half goes: everything right of the dock, split in two with a gap.</summary>
    private Rectangle PaneBounds(bool right)
    {
        int width = (ClientSize.Width - DockWidth - PaneGap) / 2;
        return new Rectangle(right ? ClientSize.Width - width : DockWidth, 0, width, ClientSize.Height);
    }

    private static ushort[]? Rect(JsonNode? node) =>
        node is JsonArray { Count: 4 } a ? a.Select(v => (ushort)Math.Clamp((int?)v ?? 0, 0, 4096)).ToArray() : null;

    /// <summary>The dock's back and forward buttons, for the app in front, or the address bar's for its page.</summary>
    private void BrowserCommand(string? action, string? id)
    {
        id ??= _activeId;
        if (id is null || !_apps.TryGetValue(id, out AppView? app) || app.View.CoreWebView2 is null)
            return;
        CoreWebView2 core = app.View.CoreWebView2;
        if (action == "back" && core.CanGoBack)
            core.GoBack();
        else if (action == "forward" && core.CanGoForward)
            core.GoForward();
        else if (action == "reload")
            core.Reload();
    }

    /// <summary>Puts the shell in front, on its home screen, its map or the truck page.</summary>
    private void ShowShell(string view)
    {
        _shellView = view;
        _split = null;
        _paneStart = null;
        _activeId = null;
        LayoutViews();
    }

    private void CloseApp(string? id)
    {
        if (id is null || !_apps.Remove(id, out AppView? app))
            return;
        Controls.Remove(app.View);
        app.View.Dispose();
        if (app.Bar && _shellView == "browser")
            _shellView = "home"; // not the start page of a browser that is gone
        if (_split is { } s && (s.Left == id || s.Right == id))
            _split = null;
        if (_paneStart == id)
            _paneStart = null;
        if (_activeId == id)
            _activeId = null;
        if (_mediaId == id)
            _mediaId = null;
        LayoutViews();
    }

    private void LayoutViews()
    {
        _shell.Bounds = ClientRectangle;
        AppView? fullscreen = null;
        foreach (AppView app in _apps.Values)
        {
            bool left = _split?.Left == app.Id, right = _split?.Right == app.Id;
            bool shown = _split is null ? app.Id == _activeId : (left || right) && app.Id != _paneStart;
            if (shown)
            {
                Rectangle area = _split is not null ? PaneBounds(right)
                    : new Rectangle(DockWidth, 0, ClientSize.Width - DockWidth, ClientSize.Height);
                if (app.Bar)
                {
                    // under the shell's address bar, in its half side by side
                    area.Y += BarHeight;
                    area.Height -= BarHeight;
                }
                app.View.Bounds = app.Fullscreen ? ClientRectangle : area;
                ApplyZoom(app); // it depends on the height
                app.View.Visible = true;
                app.View.BringToFront();
                if (app.Fullscreen)
                    fullscreen = app;
            }
            else
            {
                // Hidden views keep playing, so music carries on behind the home screen.
                app.View.Visible = false;
            }
        }
        fullscreen?.View.BringToFront(); // over the other half as well
        // The plugin lays the game's map, or its mirror views, under the shell only while
        // that view is in front.
        if (_writer is not null)
        {
            if (_split is { } s)
            {
                // The map in one half: the middle of the game's picture, at its own scale.
                bool mapRight = s.Right == "maps";
                Rectangle pane = PaneBounds(mapRight);
                int from = (ClientSize.Width - DockWidth - pane.Width) / 2;
                _writer.SetNav(mapRight || s.Left == "maps",
                               new[] { (ushort)from, (ushort)0, (ushort)pane.Width, (ushort)pane.Height },
                               new[] { (ushort)pane.X, (ushort)pane.Y, (ushort)pane.Width, (ushort)pane.Height });
            }
            else
            {
                bool mirrors = _activeId is null && _shellView == "mirrors";
                _writer.SetNav(_activeId is null && (_shellView == "maps" || mirrors),
                               mirrors ? _navSource : null, mirrors ? _navTarget : null);
            }
        }
    }

    private const string MediaStateScript = """
        (() => {
          const all = [...document.querySelectorAll('video,audio')];
          const m = all.find(x => !x.paused && !x.ended) || all.find(x => x.currentTime > 0) || all[0];
          const meta = navigator.mediaSession && navigator.mediaSession.metadata;
          return {
            title: (meta && meta.title) || document.title || '',
            artist: (meta && meta.artist) || '',
            art: (meta && meta.artwork && meta.artwork.length) ? meta.artwork[meta.artwork.length - 1].src : '',
            hasMedia: !!m,
            playing: !!m && !m.paused && !m.ended,
            position: m ? m.currentTime : 0,
            duration: m && isFinite(m.duration) ? m.duration : 0
          };
        })()
        """;

    private JsonNode? _lastMedia;

    private async Task OnStateTickAsync()
    {
        if (LeaveWithGame())
            return;
        await HoldMediaForGameAsync();
        if (_capturing is not null && Environment.TickCount64 > _captureUntil)
        {
            _capturing = null; // nobody pressed a button
            PushConfig();
        }
        if (_stateTick % 40 == 0)
            RaisePriorities();
        if (_stateTick % 120 == 0)
            LogCaptureRate();
        if (_game is not null)
        {
            GameState g = _game.Read();
            _watch.Update(g, g.Connected ? _game.ReadVehicle() : null);
            UpdateNotice();
        }
        // The game state (speed, route) is cheap and refreshed every tick; asking the
        // page about its player is not, so that happens once a second.
        await PushStateAsync(withMedia: _stateTick++ % 4 == 0);
    }

    /// <summary>Lets this app and every browser process go ahead of the game; see <see cref="Priority"/>.</summary>
    private void RaisePriorities()
    {
        if (_environment is null)
            return;
        try
        {
            Priority.Raise(Environment.ProcessId, "app");
            foreach (CoreWebView2ProcessInfo info in _environment.GetProcessInfos())
                Priority.Raise(info.ProcessId, info.Kind.ToString());
        }
        catch (Exception ex)
        {
            Log.Write("raising priority failed: " + ex.Message);
        }
    }

    /// <summary>Draws what the player has to know over the game, in the words the shell sent for it.</summary>
    private void UpdateNotice()
    {
        if (_watch.InGame is { } id && _noticeTexts.TryGetValue(id, out var text))
            _notice?.Show(text.Title, text.Text);
        else
            _notice?.Hide();
    }

    private long _rateFrames, _rateTick;

    /// <summary>A playing video should arrive at its own frame rate; far less means the screen is being starved.</summary>
    private void LogCaptureRate()
    {
        long now = Environment.TickCount64, frames = _capture?.FramesCaptured ?? 0;
        if (_rateTick != 0 && _lastMedia is JsonObject media && (bool?)media["playing"] == true)
            Log.Write($"capture while playing: {(frames - _rateFrames) * 1000.0 / (now - _rateTick):F1} frames/s");
        _rateFrames = frames;
        _rateTick = now;
    }

    private async Task PushStateAsync(bool withMedia)
    {
        if (_stateBusy || _shell.CoreWebView2 is null)
            return;
        _stateBusy = true;
        try
        {
            if (withMedia)
            {
                _lastMedia = null;
                if (_mediaId is not null && _apps.TryGetValue(_mediaId, out AppView? app) && app.View.CoreWebView2 is not null)
                {
                    string json = await app.View.CoreWebView2.ExecuteScriptAsync(MediaStateScript);
                    _lastMedia = JsonNode.Parse(json);
                    if (_lastMedia is JsonObject o)
                        o["app"] = _mediaId;
                }
            }
            GameState g = _game?.Read() ?? default;
            CoreWebView2? front = _activeId is not null && _apps.TryGetValue(_activeId, out AppView? shown)
                ? shown.View.CoreWebView2
                : null;
            var state = new JsonObject
            {
                ["type"] = "state",
                ["active"] = _activeId,
                ["view"] = _shellView,
                ["split"] = _split is { } pair ? new JsonArray(pair.Left, pair.Right) : null,
                ["paneStart"] = _paneStart,
                ["open"] = new JsonArray(_apps.Keys.Select(k => (JsonNode?)JsonValue.Create(k)).ToArray()),
                ["media"] = _lastMedia?.DeepClone(),
                ["browser"] = new JsonObject
                {
                    ["back"] = front?.CanGoBack ?? false,
                    ["forward"] = front?.CanGoForward ?? false,
                    ["url"] = front?.Source,
                    ["title"] = front?.DocumentTitle,
                },
                ["browserPage"] = BrowserPage(),
                ["vehicle"] = g.Connected ? VehicleJson(_game?.ReadVehicle()) : null,
                ["notice"] = _watch.Card is null && _watch.InGame is null ? null : new JsonObject
                {
                    ["card"] = _watch.Card,
                    ["inGame"] = _watch.InGame,
                    ["profile"] = _watch.Profile,
                    ["version"] = _watch.GameVersion,
                    ["tested"] = GameLog.TestedVersions,
                },
                ["game"] = new JsonObject
                {
                    ["connected"] = g.Connected,
                    ["telemetry"] = g.Telemetry,
                    ["paused"] = g.Paused,
                    ["electric"] = g.Electric,
                    ["mounted"] = g.Mounted,
                    ["control"] = g.Control,
                    ["navReady"] = g.NavReady,
                    ["speed"] = Math.Round(g.SpeedKmh),
                    ["limit"] = Math.Round(g.SpeedLimitKmh),
                    ["navDistance"] = Math.Round(g.NavDistanceM),
                    ["navTime"] = Math.Round(g.NavTimeS),
                    ["gameTime"] = g.GameTimeMin,
                },
            };
            _shell.CoreWebView2.PostWebMessageAsJson(state.ToJsonString());
        }
        catch (Exception ex)
        {
            Log.Write("state update failed: " + ex.Message);
        }
        finally
        {
            _stateBusy = false;
        }
    }

    /// <summary>The browser app's page, for its address bar side by side, where it need not be in front.</summary>
    private JsonObject? BrowserPage()
    {
        AppView? browser = _apps.Values.FirstOrDefault(a => a.Bar);
        CoreWebView2? core = browser?.View.CoreWebView2;
        return core is null ? null : new JsonObject { ["url"] = core.Source, ["title"] = core.DocumentTitle };
    }

    /// <summary>What the truck page shows. Wear and damage in whole percent.</summary>
    private static JsonObject? VehicleJson(Vehicle? v)
    {
        if (v is null || !v.Truck)
            return null;
        static double Percent(float wear) => Math.Round(Math.Clamp(wear, 0f, 1f) * 100);
        var o = new JsonObject
        {
            ["brand"] = v.TruckBrand,
            ["name"] = v.TruckName,
            ["plate"] = v.TruckPlate,
            ["engine"] = v.Engine,
            ["parkingBrake"] = v.ParkingBrake,
            ["fuel"] = Math.Round(v.FuelL),
            ["fuelCapacity"] = Math.Round(v.FuelCapacityL),
            ["range"] = Math.Round(v.FuelRangeKm),
            ["average"] = Math.Round(v.FuelAverageLPerKm * 100, 1), // litres per 100 km
            ["adblue"] = Math.Round(v.AdBlueL),
            ["adblueCapacity"] = Math.Round(v.AdBlueCapacityL),
            ["odometer"] = Math.Round(v.OdometerKm),
            ["oilTemperature"] = Math.Round(v.OilTemperatureC),
            ["waterTemperature"] = Math.Round(v.WaterTemperatureC),
            ["battery"] = Math.Round(v.BatteryVoltage, 1),
            ["currency"] = v.Dollars ? "USD" : "EUR",
            ["wear"] = new JsonObject
            {
                ["cabin"] = Percent(v.WearCabin),
                ["chassis"] = Percent(v.WearChassis),
                ["engine"] = Percent(v.WearEngine),
                ["transmission"] = Percent(v.WearTransmission),
                ["wheels"] = Percent(v.WearWheels),
            },
        };
        if (v.Trailer)
        {
            o["trailer"] = new JsonObject
            {
                ["name"] = v.TrailerName,
                ["plate"] = v.TrailerPlate,
                ["coupled"] = v.TrailerCoupled,
                ["wear"] = new JsonObject
                {
                    ["body"] = Percent(v.TrailerWearBody),
                    ["chassis"] = Percent(v.TrailerWearChassis),
                    ["wheels"] = Percent(v.TrailerWearWheels),
                    ["cargo"] = Percent(v.CargoDamage),
                },
            };
        }
        if (v.Job)
        {
            o["job"] = new JsonObject
            {
                ["cargo"] = v.Cargo,
                ["mass"] = Math.Round(v.CargoMassKg),
                ["from"] = v.SourceCity,
                ["fromId"] = v.SourceCityId,
                ["fromCompany"] = v.SourceCompany,
                ["to"] = v.DestinationCity,
                ["toId"] = v.DestinationCityId,
                ["toCompany"] = v.DestinationCompany,
                ["income"] = v.Income,
                ["delivery"] = v.DeliveryTimeMin,
                ["distance"] = v.PlannedDistanceKm,
                ["loaded"] = v.CargoLoaded,
                ["special"] = v.Special,
            };
        }
        return o;
    }

    // ------------------------------------------------------------------ following the game

    /// <summary>
    /// When the plugin started this app it should not outlive the game: close once the
    /// game has gone, or if it never showed up at all.
    /// </summary>
    private bool LeaveWithGame()
    {
        if (!_background || _game is null)
            return false;
        long now = Environment.TickCount64;
        if (_game.Read().Connected)
        {
            _everConnected = true;
            _lastConnectedTick = now;
            return false;
        }
        long patience = _everConnected ? 5_000 : 300_000;
        if (now - _lastConnectedTick < patience)
            return false;
        Log.Write("game is gone: closing");
        _leftWithGame = true;
        Close();
        return true;
    }

    private const string HoldMediaScript = """
        (() => { for (const m of document.querySelectorAll('video,audio'))
                   if (!m.paused && !m.ended) { m.dataset.cabinplayHeld = '1'; m.pause(); } })()
        """;

    private const string ReleaseMediaScript = """
        (() => { for (const m of document.querySelectorAll('video,audio'))
                   if (m.dataset.cabinplayHeld) { delete m.dataset.cabinplayHeld; m.play(); } })()
        """;

    /// <summary>
    /// Pauses whatever is playing while the game is paused or the truck's ignition is off,
    /// and resumes exactly that afterwards. Does nothing unless the game is running with
    /// the plugin, so the app stays usable on its own.
    /// </summary>
    private async Task HoldMediaForGameAsync()
    {
        GameState g = _game?.Read() ?? default;
        // The game closing (or gone) is not the game running again: what was held for its
        // pause menu stays held, rather than playing on until this app closes after it.
        if (!g.Connected)
            return;
        // Control mode pauses the game itself so the screen can be used in peace: that pause
        // must not stop what is playing.
        bool hold = g.Telemetry && ((g.Paused && !g.Control) || !g.Electric);
        if (hold == _holdingMedia)
            return;
        _holdingMedia = hold;
        Log.Write(hold ? "game paused or ignition off: holding media" : "game running: releasing media");
        foreach (AppView app in _apps.Values.ToArray())
        {
            try
            {
                if (app.View.CoreWebView2 is not null)
                    await app.View.CoreWebView2.ExecuteScriptAsync(hold ? HoldMediaScript : ReleaseMediaScript);
            }
            catch (Exception ex)
            {
                Log.Write("hold/release failed: " + ex.Message);
            }
        }
    }

    // ------------------------------------------------------------------ input from the game

    /// <summary>
    /// In control mode the plugin sends the game's mouse and keyboard here. They are fed
    /// to the pages through the browser's debugging protocol, which works without focus
    /// and counts as genuine user input.
    /// </summary>
    private void PumpGameInput()
    {
        if (_game is null)
            return;
        int count = _game.ReadEvents(_events);
        Point? move = null;
        for (int i = 0; i < count; i++)
        {
            GameEvent e = _events[i];
            try
            {
                if (e.Type == GameEventType.Move)
                {
                    move = new Point(e.X, e.Y); // only the newest position matters
                    continue;
                }
                if (move is Point pending)
                {
                    InjectMove(pending);
                    move = null;
                }
                switch (e.Type)
                {
                    case GameEventType.Button:
                        InjectButton(new Point(e.X, e.Y), e.Data & 0xFF, (e.Data >> 8) != 0);
                        break;
                    case GameEventType.Wheel:
                        InjectWheel(new Point(e.X, e.Y), e.Data);
                        break;
                    case GameEventType.Key:
                        InjectKey(e.X, (e.Y & 1) != 0, e.Y >> 8, (char)e.Data);
                        break;
                    case GameEventType.Shortcut:
                        RunShortcut(e.X);
                        break;
                    case GameEventType.Scroll:
                        ScrollFront(e.Data);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Write("input injection failed: " + ex.Message);
            }
        }
        if (move is Point last)
            InjectMove(last);
    }

    /// <summary>The view under a screen position, and that position in the view's own pixels.</summary>
    private (WebView2 View, Point Local) ViewAt(Point screen)
    {
        // A fullscreen view lies over the other half.
        foreach (AppView app in _apps.Values.OrderByDescending(a => a.Fullscreen))
        {
            if (app.View.Visible && app.View.Bounds.Contains(screen))
                return (app.View, new Point(screen.X - app.View.Left, screen.Y - app.View.Top));
        }
        return (_shell, screen);
    }

    private WebView2 KeyboardView =>
        !_shellKeys && _activeId is not null && _apps.TryGetValue(_activeId, out AppView? app) ? app.View : _shell;

    private static readonly string[] ButtonNames = { "left", "right", "middle" };
    private static readonly int[] ButtonMasks = { 1, 2, 4 };

    private void Send(WebView2 view, string method, JsonObject parameters)
    {
        // Mouse positions are given in the page's own pixels, which are smaller than the
        // screen's on a page shrunk to fit (see ApplyZoom).
        double scale = PixelZoom / view.ZoomFactor;
        if (Math.Abs(scale - 1) > 0.001 && parameters["x"] is JsonValue x && parameters["y"] is JsonValue y)
        {
            parameters["x"] = (int)Math.Round(x.GetValue<int>() * scale);
            parameters["y"] = (int)Math.Round(y.GetValue<int>() * scale);
        }
        if (view.CoreWebView2 is not null)
            _ = view.CoreWebView2.CallDevToolsProtocolMethodAsync(method, parameters.ToJsonString());
    }

    private void InjectMove(Point screen)
    {
        // A drag keeps going to the view it started in, even when the cursor leaves it.
        (WebView2 view, Point p) = _dragView is not null
            ? (_dragView, new Point(screen.X - _dragOrigin.X, screen.Y - _dragOrigin.Y))
            : ViewAt(screen);
        Send(view, "Input.dispatchMouseEvent", new JsonObject
        {
            ["type"] = "mouseMoved",
            ["x"] = p.X,
            ["y"] = p.Y,
            ["button"] = (_buttonsDown & 1) != 0 ? "left" : "none",
            ["buttons"] = _buttonsDown,
        });
    }

    private void InjectButton(Point screen, int button, bool down)
    {
        if (button < 0 || button > 2)
            return;
        WebView2 view;
        Point p;
        if (down)
        {
            (view, p) = ViewAt(screen);
            if (_shellKeys && view != _shell)
            {
                // A click into the page takes the keyboard back from the address field.
                _shellKeys = false;
                _shell.CoreWebView2?.PostWebMessageAsJson("{\"type\":\"blur\"}");
            }
            if (_split is not null && _apps.Values.FirstOrDefault(a => a.View == view) is { } pane)
                FocusPane(pane.Id);
            if (_buttonsDown == 0)
            {
                _dragView = view;
                _dragOrigin = new Point(screen.X - p.X, screen.Y - p.Y);
            }
            _buttonsDown |= ButtonMasks[button];
            long now = Environment.TickCount64;
            bool repeat = now - _lastClickTick < 450 && Math.Abs(screen.X - _lastClickAt.X) < 6 &&
                          Math.Abs(screen.Y - _lastClickAt.Y) < 6;
            _clickCount = repeat ? _clickCount + 1 : 1;
            _lastClickTick = now;
            _lastClickAt = screen;
        }
        else
        {
            view = _dragView ?? ViewAt(screen).View;
            p = _dragView is not null ? new Point(screen.X - _dragOrigin.X, screen.Y - _dragOrigin.Y) : ViewAt(screen).Local;
            _buttonsDown &= ~ButtonMasks[button];
            if (_buttonsDown == 0)
                _dragView = null;
        }
        Send(view, "Input.dispatchMouseEvent", new JsonObject
        {
            ["type"] = down ? "mousePressed" : "mouseReleased",
            ["x"] = p.X,
            ["y"] = p.Y,
            ["button"] = ButtonNames[button],
            ["buttons"] = _buttonsDown,
            ["clickCount"] = Math.Max(1, _clickCount),
        });
    }

    private void InjectWheel(Point screen, int delta)
    {
        (WebView2 view, Point p) = ViewAt(screen);
        Send(view, "Input.dispatchMouseEvent", new JsonObject
        {
            ["type"] = "mouseWheel",
            ["x"] = p.X,
            ["y"] = p.Y,
            ["deltaX"] = 0,
            ["deltaY"] = -delta, // wheel up is a positive delta but scrolls the page up
        });
    }

    /// <summary>
    /// Scrolling while driving, with no cursor on the screen: the app in front gets the wheel
    /// in its middle. Side by side, that is the half with the keyboard (the last one clicked),
    /// or else the one a site is in. A feed of short videos goes one video per notch instead.
    /// </summary>
    private void ScrollFront(int delta)
    {
        WebView2 view = _shell;
        if (_activeId is not null && _apps.TryGetValue(_activeId, out AppView? active) && active.View.Visible)
            view = active.View;
        else if (_apps.Values.FirstOrDefault(a => a.View.Visible) is { } shown)
            view = shown.View;
        if (view != _shell && IsShortsFeed(view.CoreWebView2?.Source))
        {
            // A wheel spun fast sends many notches: one video for those that come together.
            long now = Environment.TickCount64;
            if (now - _lastFeedStep < 300)
                return;
            _lastFeedStep = now;
            (string key, int vk) = delta < 0 ? ("ArrowDown", 0x28) : ("ArrowUp", 0x26);
            foreach (string type in new[] { "rawKeyDown", "keyUp" })
                Send(view, "Input.dispatchKeyEvent", new JsonObject
                {
                    ["type"] = type, ["key"] = key, ["code"] = key, ["windowsVirtualKeyCode"] = vk,
                });
            return;
        }
        Send(view, "Input.dispatchMouseEvent", new JsonObject
        {
            ["type"] = "mouseWheel",
            ["x"] = view.Width / 2,
            ["y"] = view.Height / 2,
            ["deltaX"] = 0,
            ["deltaY"] = -delta, // wheel up is a positive delta but scrolls the page up
        });
    }

    private long _lastFeedStep;

    /// <summary>
    /// Feeds of short videos. Their pages take a wheel from here badly (YouTube Shorts misses
    /// every other turn, TikTok and Instagram hardly move), but each goes to the next video
    /// on the down arrow and back on the up arrow.
    /// </summary>
    private static bool IsShortsFeed(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            return false;
        string host = uri.Host.ToLowerInvariant(), path = uri.AbsolutePath;
        bool On(string site) => host == site || host.EndsWith("." + site);
        return (On("youtube.com") && path.StartsWith("/shorts")) ||
               (On("instagram.com") && path.StartsWith("/reel")) ||
               // the For You and Following feeds and a video's own page, not a profile's grid
               (On("tiktok.com") && (path == "/" || path.StartsWith("/foryou") || path.StartsWith("/following") ||
                                     path.Contains("/video/")));
    }

    private static readonly Dictionary<int, string> SpecialKeys = new()
    {
        [0x08] = "Backspace", [0x09] = "Tab", [0x0D] = "Enter", [0x1B] = "Escape",
        [0x21] = "PageUp", [0x22] = "PageDown", [0x23] = "End", [0x24] = "Home",
        [0x25] = "ArrowLeft", [0x26] = "ArrowUp", [0x27] = "ArrowRight", [0x28] = "ArrowDown",
        [0x2D] = "Insert", [0x2E] = "Delete",
    };

    private void InjectKey(int vk, bool down, int modifiers, char ch)
    {
        const int ModShift = 1, ModCtrl = 2, ModAlt = 4;
        // DevTools numbering: Alt 1, Ctrl 2, Shift 8.
        int cdpModifiers = ((modifiers & ModAlt) != 0 ? 1 : 0) | ((modifiers & ModCtrl) != 0 ? 2 : 0) |
                           ((modifiers & ModShift) != 0 ? 8 : 0);
        var p = new JsonObject
        {
            ["windowsVirtualKeyCode"] = vk,
            ["nativeVirtualKeyCode"] = vk,
            ["modifiers"] = cdpModifiers,
        };
        bool typed = ch != 0 && (modifiers & ModCtrl) == 0;
        if (SpecialKeys.TryGetValue(vk, out string? name))
        {
            p["key"] = name;
            p["code"] = name;
        }
        else if (vk >= 0x70 && vk <= 0x7B)
        {
            p["key"] = "F" + (vk - 0x6F);
            p["code"] = "F" + (vk - 0x6F);
        }
        else if (typed)
        {
            p["key"] = ch.ToString();
        }
        else if (vk >= 'A' && vk <= 'Z')
        {
            p["key"] = ((char)(vk + 32)).ToString(); // Ctrl+letter shortcuts
            p["code"] = "Key" + (char)vk;
        }

        if (!down)
        {
            p["type"] = "keyUp";
        }
        else if (vk == 0x0D)
        {
            p["type"] = "keyDown";
            p["text"] = "\r"; // makes Enter submit forms
        }
        else if (typed)
        {
            p["type"] = "keyDown";
            p["text"] = ch.ToString();
            p["unmodifiedText"] = ch.ToString();
        }
        else
        {
            p["type"] = "rawKeyDown";
        }
        Send(KeyboardView, "Input.dispatchKeyEvent", p);
    }

    // ------------------------------------------------------------------ media control

    private async Task MediaCommandAsync(string action)
    {
        if (_mediaId is null || !_apps.TryGetValue(_mediaId, out AppView? app) || app.View.CoreWebView2 is null)
            return;
        CoreWebView2 core = app.View.CoreWebView2;
        switch (action)
        {
            case "playpause":
                await core.ExecuteScriptAsync("""
                    (() => {
                      const all = [...document.querySelectorAll('video,audio')];
                      const m = all.find(x => !x.paused && !x.ended) || all.find(x => x.currentTime > 0) || all[0];
                      if (m) { delete m.dataset.cabinplayHeld; if (m.paused) m.play(); else m.pause(); }
                    })()
                    """);
                break;
            case "next":
                await ClickAsync(core, app.NextSelector);
                break;
            case "prev":
                await ClickAsync(core, app.PrevSelector);
                break;
            case "seek-forward":
            case "seek-back":
                int delta = action == "seek-forward" ? 10 : -10;
                await core.ExecuteScriptAsync(
                    "(() => { const m = [...document.querySelectorAll('video,audio')].find(x => x.currentTime > 0);" +
                    $" if (m) m.currentTime = Math.max(0, m.currentTime + ({delta})); }})()");
                break;
            case "fullscreen":
                await ToggleFullscreenAsync(app);
                break;
        }
    }

    private static Task ClickAsync(CoreWebView2 core, string? selector)
    {
        if (string.IsNullOrEmpty(selector))
            return Task.CompletedTask;
        return core.ExecuteScriptAsync(
            $"(() => {{ const b = document.querySelector({JsonSerializer.Serialize(selector)}); if (b) b.click(); }})()");
    }

    private const string FullscreenVideoScript = """
        (() => {
          const area = (v) => { const r = v.getBoundingClientRect(); return r.width * r.height; };
          const v = [...document.querySelectorAll('video')].sort((a, b) => area(b) - area(a))[0];
          if (v) v.requestFullscreen();
        })()
        """;

    /// <summary>
    /// Pages may only enter fullscreen from a real key press, so one is injected through
    /// the browser's debugging protocol rather than from script.
    /// </summary>
    private async Task ToggleFullscreenAsync(AppView app)
    {
        CoreWebView2 core = app.View.CoreWebView2;
        if (app.Fullscreen)
        {
            await core.ExecuteScriptAsync("document.exitFullscreen && document.exitFullscreen()");
            return;
        }
        if (string.IsNullOrEmpty(app.FullscreenKey) || app.Id != _activeId)
            return;
        if (app.FullscreenKey == "video")
        {
            // Any site: the largest video itself, asked for as if the user had.
            await core.CallDevToolsProtocolMethodAsync("Runtime.evaluate", new JsonObject
            {
                ["expression"] = FullscreenVideoScript,
                ["userGesture"] = true,
            }.ToJsonString());
            return;
        }
        char key = char.ToLowerInvariant(app.FullscreenKey[0]);
        int vk = char.ToUpperInvariant(key);
        string common = $"\"key\":\"{key}\",\"code\":\"Key{char.ToUpperInvariant(key)}\"," +
                        $"\"windowsVirtualKeyCode\":{vk},\"nativeVirtualKeyCode\":{vk}";
        await core.CallDevToolsProtocolMethodAsync(
            "Input.dispatchKeyEvent", $"{{\"type\":\"keyDown\",{common},\"text\":\"{key}\"}}");
        await core.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", $"{{\"type\":\"keyUp\",{common}}}");
    }

    // ------------------------------------------------------------------ global hotkeys

    /// <summary>
    /// What a shortcut can do, with the key that goes with Ctrl+Alt unless the user picked
    /// another. Control mode's key is the plugin's own (control_key in cabinplay.ini); here
    /// it can only be put on a controller button. Keep the list the same as ACTIONS in
    /// ui/shell.js.
    /// </summary>
    private static readonly (string Action, Keys Key)[] Actions =
    {
        ("playpause", Keys.Space),
        ("next", Keys.Right),
        ("prev", Keys.Left),
        ("seek-forward", Keys.Up),
        ("seek-back", Keys.Down),
        ("fullscreen", Keys.F),
        ("home", Keys.H),
        ("maps", Keys.N),
        ("app-prev", Keys.PageUp),
        ("app-next", Keys.PageDown),
        ("split", Keys.S),
        ("split-swap", Keys.X),
        ("scroll-up", Keys.None),   // no key of their own: the arrows are taken
        ("scroll-down", Keys.None),
        ("control", Keys.None),
    };

    /// <summary>How far a scroll shortcut goes: three notches of the wheel, one short video.</summary>
    private const int ScrollStep = 360;

    private static bool IsAction(string? action) => action is not null && Actions.Any(a => a.Action == action);

    /// <summary>Keys a shortcut can be put on: ones every keyboard layout has in the same place.</summary>
    private static bool IsBindableKey(int vk) =>
        vk is (>= '0' and <= '9') or (>= 'A' and <= 'Z') or (>= 0x70 and <= 0x7B) // F1-F12
            or (>= 0x20 and <= 0x28)                                               // space, paging, arrows
            or 0x2D or 0x2E                                                        // Insert, Delete
            or (>= 0x60 and <= 0x69);                                              // number pad

    private void LoadBindings()
    {
        _keys.Clear();
        _pads.Clear();
        JsonObject? saved = _settings["bindings"] as JsonObject;
        foreach ((string action, Keys key) in Actions)
        {
            JsonObject? b = saved?[action] as JsonObject;
            int vk = b?["key"] is JsonValue v && v.TryGetValue(out int n) ? n : (int)key;
            _keys[action] = action != "control" && (vk == 0 || IsBindableKey(vk)) ? vk : (int)key;
            if (b?["pad"] is JsonObject p)
            {
                _pads[action] = (new PadButton((int?)p["vendor"] ?? 0, (int?)p["product"] ?? 0, (int?)p["button"] ?? 0),
                                 (string?)p["device"] ?? "");
            }
        }
    }

    private void SaveBindings()
    {
        var all = new JsonObject();
        foreach ((string action, _) in Actions)
        {
            var b = new JsonObject { ["key"] = _keys[action] };
            if (_pads.TryGetValue(action, out var pad))
            {
                b["pad"] = new JsonObject
                {
                    ["vendor"] = pad.Button.Vendor, ["product"] = pad.Button.Product, ["button"] = pad.Button.Index,
                    ["device"] = pad.Device,
                };
            }
            all[action] = b;
        }
        _settings["bindings"] = all;
        SaveSettings();
    }

    private void BindKey(string? action, int vk)
    {
        // C stays control mode's: the plugin takes it before any shortcut.
        if (!IsAction(action) || action == "control" || vk == 'C' || (vk != 0 && !IsBindableKey(vk)))
            return;
        // One key, one action: whoever had it loses it.
        foreach (string other in _keys.Keys.ToArray())
        {
            if (vk != 0 && _keys[other] == vk)
                _keys[other] = 0;
        }
        _keys[action!] = vk;
        SaveBindings();
        RegisterHotkeys();
        PushConfig();
    }

    private void BindPad(string? action, bool capture)
    {
        if (!IsAction(action))
            return;
        if (capture)
        {
            _capturing = action;
            _captureUntil = Environment.TickCount64 + 10_000;
        }
        else
        {
            _capturing = null;
            _pads.Remove(action!);
            SaveBindings();
        }
        PushConfig();
    }

    private void DefaultBindings()
    {
        _capturing = null;
        _settings.Remove("bindings");
        LoadBindings();
        SaveBindings();
        RegisterHotkeys();
        PushConfig();
    }

    /// <summary>A button on a wheel or gamepad went down: the one being asked for, or one with an action on it.</summary>
    private void OnPadButton(PadButton button, string device)
    {
        if (_capturing is string action)
        {
            _capturing = null;
            foreach (string other in _pads.Where(p => p.Value.Button == button).Select(p => p.Key).ToArray())
                _pads.Remove(other);
            _pads[action] = (button, device);
            SaveBindings();
            PushConfig();
            return;
        }
        foreach ((string bound, var pad) in _pads)
        {
            if (pad.Button == button)
            {
                RunHotkey(bound);
                return;
            }
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        RegisterHotkeys();
    }

    /// <summary>Ctrl+Alt+key for every action, system-wide. Called again whenever a key changes.</summary>
    private void RegisterHotkeys()
    {
        const uint ModAlt = 1, ModControl = 2, ModNoRepeat = 0x4000;
        if (!IsHandleCreated)
            return;
        for (int i = 0; i < Actions.Length; i++)
        {
            UnregisterHotKey(Handle, i);
            int vk = _keys.GetValueOrDefault(Actions[i].Action);
            if (vk != 0 && !RegisterHotKey(Handle, i, ModControl | ModAlt | ModNoRepeat, (uint)vk))
                Log.Write($"hotkey Ctrl+Alt+{(Keys)vk} is taken by another program");
        }
    }

    protected override void WndProc(ref Message m)
    {
        const int WmHotkey = 0x0312;
        if (m.Msg == WmHotkey)
        {
            int id = (int)m.WParam;
            // While the game has the keyboard the plugin passes the shortcuts on itself
            // (Windows usually holds them back from this window then); never run one twice.
            if (id >= 0 && id < Actions.Length && !(_game?.Read().Foreground ?? false))
                RunHotkey(Actions[id].Action);
            return;
        }
        base.WndProc(ref m);
    }

    /// <summary>A Ctrl+Alt shortcut the plugin caught while the game was in front.</summary>
    private void RunShortcut(int vk)
    {
        foreach ((string action, int key) in _keys)
        {
            if (key == vk && vk != 0)
            {
                RunHotkey(action);
                return;
            }
        }
    }

    private async void RunHotkey(string action)
    {
        try
        {
            if (action == "home" || action == "maps")
                ShowShell(action);
            else if (action == "control")
                _writer?.RequestControl();
            else if (action is "app-prev" or "app-next")
                // The order is the dock's, which only the shell knows.
                _shell.CoreWebView2?.PostWebMessageAsJson(
                    new JsonObject { ["type"] = "cycle", ["step"] = action == "app-next" ? 1 : -1 }.ToJsonString());
            else if (action is "split" or "split-swap")
                // Which two apps go side by side is the shell's to say.
                _shell.CoreWebView2?.PostWebMessageAsJson(new JsonObject { ["type"] = action }.ToJsonString());
            else if (action is "scroll-up" or "scroll-down")
                ScrollFront(action == "scroll-up" ? ScrollStep : -ScrollStep);
            else
                await MediaCommandAsync(action);
            await PushStateAsync(withMedia: true);
        }
        catch (Exception ex)
        {
            Log.Write("hotkey failed: " + ex.Message);
        }
    }

    // ------------------------------------------------------------------ settings shown on the screen

    private static readonly string[] PictureTypes = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };

    /// <summary>What the settings page needs besides the state: backgrounds and shortcuts.</summary>
    private void PushConfig()
    {
        if (_shell.CoreWebView2 is null)
            return;
        try
        {
            var pictures = new JsonArray();
            if (Directory.Exists(_wallpaperDir))
            {
                foreach (string file in Directory.EnumerateFiles(_wallpaperDir).OrderBy(File.GetCreationTimeUtc))
                {
                    if (PictureTypes.Contains(Path.GetExtension(file).ToLowerInvariant()))
                        pictures.Add(Path.GetFileName(file));
                }
            }
            var bindings = new JsonArray();
            foreach ((string action, _) in Actions)
            {
                var b = new JsonObject { ["action"] = action, ["key"] = _keys[action] };
                if (_pads.TryGetValue(action, out var pad))
                    b["pad"] = new JsonObject { ["button"] = pad.Button.Index, ["device"] = pad.Device };
                bindings.Add(b);
            }
            _shell.CoreWebView2.PostWebMessageAsJson(new JsonObject
            {
                ["type"] = "config",
                ["wallpaper"] = (string?)_settings["wallpaper"],
                ["wallpapers"] = pictures,
                ["wallpaperHost"] = WallpaperHost,
                ["bindings"] = bindings,
                ["capturing"] = _capturing,
                ["update"] = _updater?.ToJson(),
                ["gameNotices"] = _watch.InGameEnabled,
                ["fitShorts"] = FitShorts,
                ["icons"] = new JsonObject((_icons?.Addresses() ?? new())
                    .Select(i => KeyValuePair.Create(i.Key, (JsonNode?)i.Value))),
                ["control"] = new JsonObject
                {
                    ["overlaySize"] = (int?)_settings["overlaySize"],
                    ["cursorSpeed"] = (int?)_settings["cursorSpeed"],
                    ["driveWheel"] = (string?)_settings["driveWheel"],
                },
            }.ToJsonString());
        }
        catch (Exception ex)
        {
            Log.Write("config update failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Lets the user pick a picture and keeps a copy of it, no larger than the screen can
    /// show, as the background. The file window is Windows' own, on the desktop.
    /// </summary>
    private void AddWallpaper(string? testFile)
    {
        // A test instance is told which file, since nobody is there to pick one.
        string? source = Program.Namespace.Length > 0 ? testFile : null;
        if (source is null)
        {
            using var dialog = new OpenFileDialog
            {
                Title = Strings.Get(_language, "pickimage"),
                Filter = "*.jpg; *.png; *.webp; *.bmp; *.gif|*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.gif",
            };
            // Without an owner: this window may be parked beyond the edge of the desktop.
            if (dialog.ShowDialog() != DialogResult.OK)
                return;
            source = dialog.FileName;
        }
        try
        {
            _settings["wallpaper"] = ImportWallpaper(source);
            SaveSettings();
        }
        catch (Exception ex)
        {
            Log.Write("background not added: " + ex.Message);
        }
        PushConfig();
    }

    /// <summary>Copies a picture into the backgrounds folder and returns its file name there.</summary>
    private string ImportWallpaper(string source)
    {
        Directory.CreateDirectory(_wallpaperDir);
        string name = $"{DateTime.Now:yyyyMMdd-HHmmss}-{Path.GetFileNameWithoutExtension(source)}";
        name = new string(name.Select(c => char.IsAsciiLetterOrDigit(c) || c == '-' ? c : '_').ToArray());
        string target;
        try
        {
            // A photo straight from a camera is far larger than the 1024x512 screen.
            using var image = Image.FromFile(source);
            double scale = Math.Min(1.0, Math.Max(2048.0 / image.Width, 1024.0 / image.Height));
            using var copy = new Bitmap(Math.Max(1, (int)(image.Width * scale)), Math.Max(1, (int)(image.Height * scale)));
            using (Graphics g = Graphics.FromImage(copy))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, 0, 0, copy.Width, copy.Height);
            }
            target = Path.Combine(_wallpaperDir, name + ".jpg");
            var jpeg = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders()
                .First(c => c.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid);
            using var quality = new System.Drawing.Imaging.EncoderParameters(1);
            quality.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 92L);
            copy.Save(target, jpeg, quality);
        }
        catch (Exception ex) when (ex is OutOfMemoryException or ArgumentException or ExternalException)
        {
            // A format Windows' own decoder does not read (WebP): the browser does.
            target = Path.Combine(_wallpaperDir, name + Path.GetExtension(source).ToLowerInvariant());
            File.Copy(source, target, overwrite: true);
        }
        return Path.GetFileName(target);
    }

    private void RemoveWallpaper(string? name)
    {
        // Only a file of ours: the name comes from the page.
        if (string.IsNullOrEmpty(name) || name != Path.GetFileName(name))
            return;
        try
        {
            File.Delete(Path.Combine(_wallpaperDir, name));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Write("background not removed: " + ex.Message);
        }
        if ((string?)_settings["wallpaper"] == name)
        {
            _settings.Remove("wallpaper");
            SaveSettings();
        }
        PushConfig();
    }

    /// <summary>Control mode's screen size or cursor speed, as picked in the settings.</summary>
    private void SetControlOption(string? name, int value)
    {
        bool valid = name switch
        {
            "overlaySize" => value is >= 20 and <= 100,
            "cursorSpeed" => value is >= 25 and <= 400,
            _ => false,
        };
        if (!valid)
            return;
        _settings[name!] = value;
        SaveSettings();
        ApplyControlOptions();
        PushConfig();
    }

    /// <summary>What makes the wheel scroll the screen while driving, by the plugin's numbers.</summary>
    private static readonly string[] DriveWheelModes = { "ctrl-alt", "alt", "wheel", "off" };

    private void SetDriveWheel(string? mode)
    {
        if (mode is null || !DriveWheelModes.Contains(mode))
            return;
        _settings["driveWheel"] = mode;
        SaveSettings();
        ApplyControlOptions();
        PushConfig();
    }

    /// <summary>Hands them to the plugin; unset, it keeps the ones in cabinplay.ini.</summary>
    private void ApplyControlOptions() =>
        _writer?.SetControlOptions((int?)_settings["overlaySize"] ?? 0, (int?)_settings["cursorSpeed"] ?? 0,
            Array.IndexOf(DriveWheelModes, (string?)_settings["driveWheel"]) + 1);

    // ------------------------------------------------------------------ window placement

    private void RestorePlacement()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                JsonNode? s = JsonNode.Parse(File.ReadAllText(_settingsPath));
                if (s is JsonObject all)
                    _settings = all;
                var saved = new Point((int)s!["x"]!, (int)s["y"]!);
                TopMost = (bool?)s["topMost"] ?? false;
                // Ignore positions on a monitor that is no longer connected.
                if (Screen.AllScreens.Any(sc => sc.WorkingArea.IntersectsWith(new Rectangle(saved, Size))))
                {
                    Location = saved;
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Write("settings unreadable: " + ex.Message);
        }
        // First run: prefer a second monitor so the game can keep the main one.
        CenterOnScreen(Screen.AllScreens.FirstOrDefault(s => !s.Primary) ?? Screen.PrimaryScreen!);
    }

    private void CenterOnScreen(Screen screen)
    {
        Rectangle area = screen.WorkingArea;
        Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
    }

    private void SavePlacement()
    {
        Point where = _offscreen ? _restoreLocation : Location;
        _settings["x"] = where.X;
        _settings["y"] = where.Y;
        _settings["topMost"] = TopMost;
        SaveSettings();
    }

    /// <summary>Window position, background and shortcuts, in one file.</summary>
    private void SaveSettings()
    {
        try
        {
            File.WriteAllText(_settingsPath, _settings.ToJsonString());
        }
        catch (IOException ex)
        {
            Log.Write("settings not saved: " + ex.Message);
        }
    }

    private void BringToUser()
    {
        if (WindowState == FormWindowState.Minimized)
            WindowState = FormWindowState.Normal;
        _offscreen = false;
        Location = _restoreLocation;
        Show();
        Activate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        // A minimised window stops being captured, which would freeze the in-cabin screen.
        if (WindowState == FormWindowState.Minimized)
            BeginInvoke(() => WindowState = FormWindowState.Normal);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        SavePlacement();
        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _stateTimer.Stop();
        _inputTimer.Stop();
        _tray.Visible = false;
        _tray.Dispose();
        _controllers?.Dispose();
        _capture?.Dispose();
        _writer?.Dispose();
        _game?.Dispose();
        _notice?.Dispose();
        // Last, with everything else let go: it may wait for the game's process to end.
        _updater?.InstallIfReady(gameClosed: _leftWithGame, _language);
        _updater?.Dispose();
        base.OnFormClosed(e);
    }

    // ------------------------------------------------------------------ misc

    private static Icon CreateIcon()
    {
        using var bmp = new Bitmap(64, 64);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var body = RoundedRect(new RectangleF(2, 12, 60, 40), 9);
            using var bodyBrush = new SolidBrush(Color.FromArgb(24, 24, 28));
            g.FillPath(bodyBrush, body);
            using var screen = RoundedRect(new RectangleF(6, 16, 52, 32), 6);
            using var screenBrush = new LinearGradientBrush(new RectangleF(6, 16, 52, 32),
                Color.FromArgb(30, 60, 150), Color.FromArgb(150, 60, 170), 25f);
            g.FillPath(screenBrush, screen);
            using var play = new SolidBrush(Color.White);
            g.FillPolygon(play, new[] { new PointF(27, 24), new PointF(27, 40), new PointF(41, 32) });
        }
        return Icon.FromHandle(bmp.GetHicon());
    }

    private static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        float d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    /// <summary>Test aid: saves what the plugin would receive, then exits.</summary>
    private async Task DumpFrameLaterAsync(string path)
    {
        await Task.Delay(_dumpDelayMs);
        using var capture = new Bitmap(FrameWriter.Width, FrameWriter.Height);
        long frames = _capture?.FramesCaptured ?? 0;
        using (var reader = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting(FrameWriter.MappingName))
        using (var view = reader.CreateViewStream(64, FrameWriter.Width * FrameWriter.Height * 4))
        {
            var data = capture.LockBits(new Rectangle(Point.Empty, capture.Size),
                System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            var buffer = new byte[FrameWriter.Width * 4];
            for (int y = 0; y < FrameWriter.Height; y++)
            {
                view.ReadExactly(buffer);
                Marshal.Copy(buffer, 0, data.Scan0 + y * data.Stride, buffer.Length);
            }
            capture.UnlockBits(data);
        }
        capture.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        Log.Write($"dumped frame to {path} after {frames} captured frames");
        Close();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
