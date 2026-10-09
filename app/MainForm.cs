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
    private const string ShellHost = "cabinplay.local";

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
    }

    private readonly WebView2 _shell = new();
    private readonly Dictionary<string, AppView> _apps = new();
    private readonly System.Windows.Forms.Timer _stateTimer = new() { Interval = 250 };
    private readonly System.Windows.Forms.Timer _inputTimer = new() { Interval = 10 };
    private readonly NotifyIcon _tray = new();
    private readonly string _settingsPath = Path.Combine(Program.DataDir, "settings.json");
    private readonly string? _dumpPath;
    private readonly string? _startApp;
    private readonly string? _startUrl;
    private readonly int _dumpDelayMs = 6000;
    private readonly bool _background;  // started by the game plugin: stay out of sight, leave with the game
    private readonly string? _startLanguage;
    private bool _offscreen;
    private Point _restoreLocation;
    private bool _everConnected;
    private long _lastConnectedTick = Environment.TickCount64;
    private readonly ToolStripMenuItem _showItem = new(), _hideItem = new(), _topMostItem = new() { CheckOnClick = true },
                                       _centerItem = new(), _exitItem = new();
    private readonly GameEvent[] _events = new GameEvent[256];

    private CoreWebView2Environment? _environment;
    private FrameWriter? _writer;
    private WindowCapture? _capture;
    private GameLink? _game;
    private string? _activeId;          // app shown right now, null while the shell is in front
    private string? _mediaId;           // app the media controls act on
    private string _shellView = "home"; // what the shell shows behind the apps: "home" or "maps"
    private bool _stateBusy;
    private int _stateTick;
    private bool _holdingMedia;         // media was paused because the game paused or the ignition is off

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

    private void ApplyLanguage(string language)
    {
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

            await _shell.EnsureCoreWebView2Async(_environment);
            CoreWebView2 core = _shell.CoreWebView2;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.IsZoomControlEnabled = false;
            core.Settings.AreBrowserAcceleratorKeysEnabled = false;
            core.Settings.IsNonClientRegionSupportEnabled = true; // lets the dock clock drag the window
            core.SetVirtualHostNameToFolderMapping(
                ShellHost, Path.Combine(AppContext.BaseDirectory, "ui"), CoreWebView2HostResourceAccessKind.Allow);
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
            _game = new GameLink();
            _capture = new WindowCapture(_writer);
            _capture.Start(Handle);
            _stateTimer.Start();
            _inputTimer.Start();
            Log.Write("started");

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
            app.View.ZoomFactor = PixelZoom;
        LayoutViews();
    }

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
                    ShowShell((string?)msg!["view"] == "maps" ? "maps" : "home");
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
        string? id = (string?)msg["id"];
        string? url = (string?)msg["url"];
        if (id is null || url is null || _environment is null)
            return;

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
            view.ZoomFactor = PixelZoom;
            await PrepareForInjectedInputAsync(core);
            core.Navigate(url);
        }

        _activeId = _mediaId = id;
        LayoutViews();
    }

    /// <summary>Puts the shell in front, on its home screen or its map.</summary>
    private void ShowShell(string view)
    {
        _shellView = view;
        _activeId = null;
        LayoutViews();
    }

    private void CloseApp(string? id)
    {
        if (id is null || !_apps.Remove(id, out AppView? app))
            return;
        Controls.Remove(app.View);
        app.View.Dispose();
        if (_activeId == id)
            _activeId = null;
        if (_mediaId == id)
            _mediaId = null;
        LayoutViews();
    }

    private void LayoutViews()
    {
        _shell.Bounds = ClientRectangle;
        foreach (AppView app in _apps.Values)
        {
            bool active = app.Id == _activeId;
            if (active)
            {
                app.View.Bounds = app.Fullscreen
                    ? ClientRectangle
                    : new Rectangle(DockWidth, 0, ClientSize.Width - DockWidth, ClientSize.Height);
                app.View.Visible = true;
                app.View.BringToFront();
            }
            else
            {
                // Hidden views keep playing, so music carries on behind the home screen.
                app.View.Visible = false;
            }
        }
        // The plugin lays the game's map under the shell only while the map view is in front.
        if (_writer is not null)
            _writer.NavVisible = _activeId is null && _shellView == "maps";
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
        // The game state (speed, route) is cheap and refreshed every tick; asking the
        // page about its player is not, so that happens once a second.
        await PushStateAsync(withMedia: _stateTick++ % 4 == 0);
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
            var state = new JsonObject
            {
                ["type"] = "state",
                ["active"] = _activeId,
                ["view"] = _shellView,
                ["open"] = new JsonArray(_apps.Keys.Select(k => (JsonNode?)JsonValue.Create(k)).ToArray()),
                ["media"] = _lastMedia?.DeepClone(),
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
        if (_activeId is not null && _apps.TryGetValue(_activeId, out AppView? app) && app.View.Visible &&
            app.View.Bounds.Contains(screen))
            return (app.View, new Point(screen.X - app.View.Left, screen.Y - app.View.Top));
        return (_shell, screen);
    }

    private WebView2 KeyboardView =>
        _activeId is not null && _apps.TryGetValue(_activeId, out AppView? app) ? app.View : _shell;

    private static readonly string[] ButtonNames = { "left", "right", "middle" };
    private static readonly int[] ButtonMasks = { 1, 2, 4 };

    private static void Send(WebView2 view, string method, JsonObject parameters)
    {
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
        char key = char.ToLowerInvariant(app.FullscreenKey[0]);
        int vk = char.ToUpperInvariant(key);
        string common = $"\"key\":\"{key}\",\"code\":\"Key{char.ToUpperInvariant(key)}\"," +
                        $"\"windowsVirtualKeyCode\":{vk},\"nativeVirtualKeyCode\":{vk}";
        await core.CallDevToolsProtocolMethodAsync(
            "Input.dispatchKeyEvent", $"{{\"type\":\"keyDown\",{common},\"text\":\"{key}\"}}");
        await core.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", $"{{\"type\":\"keyUp\",{common}}}");
    }

    // ------------------------------------------------------------------ global hotkeys

    private static readonly (Keys Key, string Action)[] Hotkeys =
    {
        (Keys.Space, "playpause"),
        (Keys.Right, "next"),
        (Keys.Left, "prev"),
        (Keys.Up, "seek-forward"),
        (Keys.Down, "seek-back"),
        (Keys.F, "fullscreen"),
        (Keys.H, "home"),
        (Keys.N, "maps"),
    };

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        const uint ModAlt = 1, ModControl = 2, ModNoRepeat = 0x4000;
        for (int i = 0; i < Hotkeys.Length; i++)
            if (!RegisterHotKey(Handle, i, ModControl | ModAlt | ModNoRepeat, (uint)Hotkeys[i].Key))
                Log.Write($"hotkey Ctrl+Alt+{Hotkeys[i].Key} is taken by another program");
    }

    protected override void WndProc(ref Message m)
    {
        const int WmHotkey = 0x0312;
        if (m.Msg == WmHotkey)
        {
            int id = (int)m.WParam;
            if (id >= 0 && id < Hotkeys.Length)
                RunHotkey(Hotkeys[id].Action);
            return;
        }
        base.WndProc(ref m);
    }

    private async void RunHotkey(string action)
    {
        try
        {
            if (action == "home" || action == "maps")
                ShowShell(action);
            else
                await MediaCommandAsync(action);
            await PushStateAsync(withMedia: true);
        }
        catch (Exception ex)
        {
            Log.Write("hotkey failed: " + ex.Message);
        }
    }

    // ------------------------------------------------------------------ window placement

    private void RestorePlacement()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                JsonNode? s = JsonNode.Parse(File.ReadAllText(_settingsPath));
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
        try
        {
            Point where = _offscreen ? _restoreLocation : Location;
            var s = new JsonObject { ["x"] = where.X, ["y"] = where.Y, ["topMost"] = TopMost };
            File.WriteAllText(_settingsPath, s.ToJsonString());
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
        _capture?.Dispose();
        _writer?.Dispose();
        _game?.Dispose();
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
}
