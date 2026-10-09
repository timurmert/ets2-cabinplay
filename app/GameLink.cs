using System.IO.MemoryMappedFiles;

namespace Ets2CarPlay;

internal enum GameEventType : uint
{
    Move = 1,
    Button = 2, // Data: button (0 left, 1 right, 2 middle) | down << 8
    Wheel = 3,  // Data: wheel delta, 120 per notch
    Key = 4,    // X: virtual key, Y: down | modifiers << 8, Data: UTF-16 char or 0
}

internal readonly record struct GameEvent(GameEventType Type, int X, int Y, int Data);

internal readonly record struct GameState(
    bool Connected,   // the plugin is rendering frames right now
    bool Telemetry,   // Paused / Electric / navigation values are meaningful
    bool Paused,
    bool Electric,
    bool Mounted,
    bool Control,
    bool NavReady,
    float SpeedKmh,
    float SpeedLimitKmh,
    float NavDistanceM,
    float NavTimeS,
    uint GameTimeMin);

/// <summary>
/// Reads what the game plugin publishes: game state and the input it captures in
/// control mode. Layout mirrors plugin/frame_protocol.h: keep both in sync.
/// </summary>
internal sealed unsafe class GameLink : IDisposable
{
    private static readonly string MappingName = Program.SharedName("ETS2CarPlayState");
    private const uint Magic = 0x54535043; // "CPST"
    private const int EventCapacity = 256;
    private const int EventsOffset = 64;
    private const int EventSize = 16;
    private const int Size = EventsOffset + EventCapacity * EventSize;

    private const uint FlagTelemetry = 0x01, FlagPaused = 0x02, FlagElectric = 0x04, FlagMounted = 0x08,
                       FlagControl = 0x10, FlagNavReady = 0x20;

    private readonly MemoryMappedFile _file;
    private readonly MemoryMappedViewAccessor _view;
    private readonly byte* _base;
    private uint _nextEvent;
    private bool _synced;

    public GameLink()
    {
        // Whichever side starts first creates the mapping.
        _file = MemoryMappedFile.CreateOrOpen(MappingName, Size, MemoryMappedFileAccess.ReadWrite);
        _view = _file.CreateViewAccessor(0, Size, MemoryMappedFileAccess.Read);
        byte* p = null;
        _view.SafeMemoryMappedViewHandle.AcquirePointer(ref p);
        _base = p;
    }

    public GameState Read()
    {
        bool connected = *(uint*)_base == Magic &&
                         Environment.TickCount64 - *(long*)(_base + 8) < 2000;
        uint flags = *(uint*)(_base + 16);
        return new GameState(
            connected,
            connected && (flags & FlagTelemetry) != 0,
            (flags & FlagPaused) != 0,
            (flags & FlagElectric) != 0,
            connected && (flags & FlagMounted) != 0,
            connected && (flags & FlagControl) != 0,
            connected && (flags & FlagNavReady) != 0,
            *(float*)(_base + 24),
            *(float*)(_base + 28),
            *(float*)(_base + 32),
            *(float*)(_base + 36),
            *(uint*)(_base + 40));
    }

    /// <summary>Returns the events written since the last call, oldest first.</summary>
    public int ReadEvents(Span<GameEvent> buffer)
    {
        uint written = Volatile.Read(ref *(uint*)(_base + 20));
        if (!_synced)
        {
            // Whatever happened before this app started is not for it.
            _synced = true;
            _nextEvent = written;
            return 0;
        }
        if (written - _nextEvent > EventCapacity)
            _nextEvent = written - EventCapacity; // fell behind; the oldest were overwritten
        int count = 0;
        while (_nextEvent != written && count < buffer.Length)
        {
            byte* e = _base + EventsOffset + (_nextEvent % EventCapacity) * EventSize;
            buffer[count++] = new GameEvent((GameEventType)(*(uint*)e), *(int*)(e + 4), *(int*)(e + 8), *(int*)(e + 12));
            _nextEvent++;
        }
        return count;
    }

    public void Dispose()
    {
        _view.SafeMemoryMappedViewHandle.ReleasePointer();
        _view.Dispose();
        _file.Dispose();
    }
}
