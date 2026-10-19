using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO.MemoryMappedFiles;

namespace CabinPlay;

/// <summary>
/// A message the plugin draws over the game itself (see draw_notice in
/// plugin/cabinplay_plugin.c), for when there is no CabinPlay screen in the cabin to show it
/// on. Layout mirrors plugin/frame_protocol.h: keep both in sync.
/// </summary>
internal sealed unsafe class GameNotice : IDisposable
{
    private static readonly string MappingName = Program.SharedName("CabinPlayNotice");
    private const uint Magic = 0x544E5043; // "CPNT"
    private const uint Version = 1;
    private const int MaxWidth = 1024, MaxHeight = 256, HeaderSize = 32;
    private const int SequenceOffset = 8, VisibleOffset = 12, WidthOffset = 16, HeightOffset = 20;

    // Drawn for a game 1440 pixels high; the plugin scales it to the game's picture.
    private const int CardWidth = 940, Pad = 26, IconSize = 58;

    private readonly MemoryMappedFile _file;
    private readonly MemoryMappedViewAccessor _view;
    private readonly byte* _base;
    private string? _shown;

    public GameNotice()
    {
        long size = HeaderSize + (long)MaxWidth * MaxHeight * 4;
        _file = MemoryMappedFile.CreateOrOpen(MappingName, size, MemoryMappedFileAccess.ReadWrite);
        _view = _file.CreateViewAccessor(0, size, MemoryMappedFileAccess.ReadWrite);
        byte* p = null;
        _view.SafeMemoryMappedViewHandle.AcquirePointer(ref p);
        _base = p;
        *(uint*)(_base + VisibleOffset) = 0;
        *(uint*)(_base + 0) = Magic;
        *(uint*)(_base + 4) = Version;
    }

    /// <summary>Shows the message over the game, or keeps showing it.</summary>
    public void Show(string title, string text)
    {
        string key = title + "\n" + text;
        if (_shown != key)
        {
            _shown = key;
            using Bitmap picture = Render(title, text);
            Write(picture);
            Log.Write("notice over the game: " + title);
        }
        *(uint*)(_base + VisibleOffset) = 1;
    }

    public void Hide()
    {
        if (*(uint*)(_base + VisibleOffset) != 0)
            Log.Write("notice over the game hidden");
        *(uint*)(_base + VisibleOffset) = 0;
    }

    /// <summary>A dark card: a warning sign, "CabinPlay", the title and up to two lines of text.</summary>
    private static Bitmap Render(string title, string text)
    {
        using var labelFont = new Font("Segoe UI Semibold", 17f, GraphicsUnit.Pixel);
        using var titleFont = new Font("Segoe UI Semibold", 29f, GraphicsUnit.Pixel);
        using var textFont = new Font("Segoe UI", 22f, GraphicsUnit.Pixel);
        int textLeft = Pad + IconSize + 22, textWidth = CardWidth - textLeft - Pad;
        using var format = new StringFormat { Trimming = StringTrimming.EllipsisWord };

        int titleHeight, bodyHeight;
        using (var measure = new Bitmap(1, 1))
        using (Graphics g = Graphics.FromImage(measure))
        {
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            titleHeight = (int)Math.Ceiling(g.MeasureString(title, titleFont, textWidth, format).Height);
            bodyHeight = (int)Math.Ceiling(g.MeasureString(text, textFont, textWidth, format).Height);
        }
        titleHeight = Math.Min(titleHeight, 2 * titleFont.Height);
        bodyHeight = Math.Min(bodyHeight, 2 * textFont.Height + 2);
        int labelHeight = labelFont.Height + 2;
        int height = Math.Min(MaxHeight, Math.Max(Pad * 2 + IconSize, Pad * 2 + labelHeight + titleHeight + 4 + bodyHeight));

        var bitmap = new Bitmap(CardWidth, height, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.FromArgb(255, 28, 28, 34));
            using (var edge = new Pen(Color.FromArgb(255, 255, 159, 10), 4f))
                g.DrawLine(edge, 2, 0, 2, height); // the plugin rounds the corners

            int iconTop = (height - IconSize) / 2;
            using (var orange = new SolidBrush(Color.FromArgb(255, 255, 159, 10)))
                g.FillEllipse(orange, Pad, iconTop, IconSize, IconSize);
            using (var dark = new SolidBrush(Color.FromArgb(255, 28, 28, 34)))
            {
                float cx = Pad + IconSize / 2f;
                g.FillRectangle(dark, cx - 3.5f, iconTop + 13, 7, 21);
                g.FillEllipse(dark, cx - 4.5f, iconTop + 38, 9, 9);
            }

            int y = (height - (labelHeight + titleHeight + 4 + bodyHeight)) / 2;
            using (var accent = new SolidBrush(Color.FromArgb(255, 255, 179, 64)))
                g.DrawString("CabinPlay", labelFont, accent, textLeft, y);
            y += labelHeight;
            using (var white = new SolidBrush(Color.White))
                g.DrawString(title, titleFont, white, new RectangleF(textLeft, y, textWidth, titleHeight), format);
            y += titleHeight + 4;
            using (var grey = new SolidBrush(Color.FromArgb(255, 200, 200, 208)))
                g.DrawString(text, textFont, grey, new RectangleF(textLeft, y, textWidth, bodyHeight), format);
        }
        return bitmap;
    }

    private void Write(Bitmap picture)
    {
        BitmapData data = picture.LockBits(new Rectangle(0, 0, picture.Width, picture.Height), ImageLockMode.ReadOnly,
                                           PixelFormat.Format32bppArgb);
        try
        {
            ref int sequence = ref *(int*)(_base + SequenceOffset);
            Interlocked.Increment(ref sequence); // odd: picture in flux
            if ((sequence & 1) == 0)
                Interlocked.Increment(ref sequence);
            byte* rows = _base + HeaderSize;
            int rowBytes = picture.Width * 4;
            for (int y = 0; y < picture.Height; y++)
                Buffer.MemoryCopy((byte*)data.Scan0 + (long)y * data.Stride, rows + (long)y * MaxWidth * 4, rowBytes, rowBytes);
            *(uint*)(_base + WidthOffset) = (uint)picture.Width;
            *(uint*)(_base + HeightOffset) = (uint)picture.Height;
            Interlocked.Increment(ref sequence); // even: complete
        }
        finally
        {
            picture.UnlockBits(data);
        }
    }

    public void Dispose()
    {
        *(uint*)(_base + VisibleOffset) = 0;
        _view.SafeMemoryMappedViewHandle.ReleasePointer();
        _view.Dispose();
        _file.Dispose();
    }
}

/// <summary>
/// Decides what CabinPlay has to tell the player about the game: the mod is not active in the
/// profile, the truck has no CabinPlay screen, or the game is a version CabinPlay was not
/// tried with. The home screen shows the first that applies as a card (<see cref="Card"/>);
/// the first two are also drawn over the game for a while (<see cref="InGame"/>), since
/// without a screen in the cabin there is nowhere else to see them.
/// </summary>
internal sealed class NoticeWatch
{
    private const long ModOffInGameMs = 40_000;      // of play once the truck is there
    private const long ModOffInGameMaxMs = 300_000;  // and at most this long in all
    private const long UnfittedAfterMs = 30_000;     // of play without the screen before saying so
    private const long UnfittedInGameMs = 40_000;

    private readonly GameLog _log = new();
    private long _lastPoll, _lastTick;
    private int _modList = -1;
    private long _modOffSince, _modOffPlayed;
    private string? _truck;          // brand, model and plate of the truck being watched
    private long _unfittedPlayed;     // play time without the screen in this truck
    private long _unfittedShown;      // of it, how long it has been drawn over the game

    public string? Card { get; private set; }   // "modOff", "notFitted" or "version"
    public string? InGame { get; private set; } // "modOff" or "notFitted"
    public string? Profile => _log.Profile;
    public string? GameVersion => _log.GameVersion;

    /// <summary>Called four times a second. True when <see cref="Card"/> or <see cref="InGame"/> changed.</summary>
    public bool Update(GameState g, Vehicle? v)
    {
        long now = Environment.TickCount64;
        long dt = _lastTick == 0 ? 0 : Math.Min(now - _lastTick, 1000);
        _lastTick = now;
        var before = (Card, InGame);

        // An old log says nothing about a game that is not running. ATS keeps its log elsewhere.
        if (!g.Connected || v?.Dollars == true)
        {
            Card = InGame = null;
            _truck = null;
            _unfittedPlayed = _unfittedShown = 0;
            return before != (Card, InGame);
        }
        if (now - _lastPoll >= 2000)
        {
            _lastPoll = now;
            _log.Poll();
        }
        bool playing = g.Telemetry && !g.Paused && v?.Truck == true;

        // the mod is off in this profile: from the moment its mod list says so
        bool modOff = _log.ModActive == false;
        if (modOff && _modList != _log.ModLists)
        {
            _modList = _log.ModLists;
            _modOffSince = now;
            _modOffPlayed = 0;
            Log.Write($"the CabinPlay mod is not active in profile '{_log.Profile}'");
        }
        if (modOff && playing)
            _modOffPlayed += dt;
        bool modOffInGame = modOff && now - _modOffSince < ModOffInGameMaxMs && _modOffPlayed < ModOffInGameMs;

        // no screen in this truck: after a while of play, once per truck
        string? truck = v?.Truck == true ? $"{v.TruckBrand} {v.TruckName} {v.TruckPlate}" : null;
        if (truck != _truck || g.Mounted || !g.Telemetry)
        {
            _truck = truck;
            _unfittedPlayed = _unfittedShown = 0;
        }
        bool unfitted = !modOff && truck is not null && !g.Mounted && _unfittedPlayed >= UnfittedAfterMs;
        if (!modOff && truck is not null && !g.Mounted && playing)
        {
            if (_unfittedPlayed < UnfittedAfterMs && _unfittedPlayed + dt >= UnfittedAfterMs)
                Log.Write($"no CabinPlay screen in this truck ({truck}) after {UnfittedAfterMs / 1000} s of play");
            _unfittedPlayed += dt;
            if (unfitted)
                _unfittedShown += dt;
        }
        bool unfittedInGame = unfitted && playing && _unfittedShown < UnfittedInGameMs;

        Card = modOff ? "modOff" : unfitted ? "notFitted" : !_log.VersionTested ? "version" : null;
        InGame = modOffInGame ? "modOff" : unfittedInGame ? "notFitted" : null;
        if (Card == "version" && before.Card != "version")
            Log.Write($"game version {_log.GameVersion} was not tested (CabinPlay is for {GameLog.TestedVersions})");
        return before != (Card, InGame);
    }
}
