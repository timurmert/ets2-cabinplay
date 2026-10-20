using System.IO.MemoryMappedFiles;
using System.Text;

namespace CabinPlay;

internal enum GameEventType : uint
{
    Move = 1,
    Button = 2, // Data: button (0 left, 1 right, 2 middle) | down << 8
    Wheel = 3,  // Data: wheel delta, 120 per notch
    Key = 4,    // X: virtual key, Y: down | modifiers << 8, Data: UTF-16 char or 0
    Shortcut = 5, // X: virtual key pressed with Ctrl + left Alt while the game had the keyboard
    Scroll = 6,   // the wheel while driving (Settings > Controls says with which keys); Data: delta, 120 per notch
}

internal readonly record struct GameEvent(GameEventType Type, int X, int Y, int Data);

/// <summary>Truck, trailer and job as the game reports them. Wear runs from 0 (new) to 1.</summary>
internal sealed record Vehicle(
    bool Truck, bool Engine, bool ParkingBrake, bool Trailer, bool TrailerCoupled, bool Job, bool CargoLoaded,
    bool Special, bool Dollars,
    float FuelL, float FuelCapacityL, float FuelRangeKm, float FuelAverageLPerKm,
    float AdBlueL, float AdBlueCapacityL, float OdometerKm,
    float OilTemperatureC, float WaterTemperatureC, float BatteryVoltage,
    float WearEngine, float WearTransmission, float WearCabin, float WearChassis, float WearWheels,
    float TrailerWearChassis, float TrailerWearWheels, float TrailerWearBody, float CargoDamage,
    float CargoMassKg, uint DeliveryTimeMin, uint PlannedDistanceKm, ulong Income,
    string TruckBrand, string TruckName, string TruckPlate, string TrailerName, string TrailerPlate,
    string Cargo, string SourceCity, string SourceCompany, string DestinationCity, string DestinationCompany,
    string JobMarket, string SourceCityId, string DestinationCityId);

internal readonly record struct GameState(
    bool Connected,   // the plugin is rendering frames right now
    bool Telemetry,   // Paused / Electric / navigation values are meaningful
    bool Paused,
    bool Electric,
    bool Mounted,
    bool Control,
    bool NavReady,
    bool Foreground,  // the game has the keyboard: the plugin passes the shortcuts on
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
    private static readonly string MappingName = Program.SharedName("CabinPlayState");
    private const uint Magic = 0x54535043; // "CPST"
    private const int EventCapacity = 256;
    private const int EventsOffset = 64;
    private const int EventSize = 16;
    private const int VehicleOffset = EventsOffset + EventCapacity * EventSize; // CABINPLAY_VEHICLE_OFFSET
    private const int VehicleSize = 632;
    private const int Size = VehicleOffset + VehicleSize;

    private const uint FlagTelemetry = 0x01, FlagPaused = 0x02, FlagElectric = 0x04, FlagMounted = 0x08,
                       FlagControl = 0x10, FlagNavReady = 0x20, FlagForeground = 0x40;

    // CABINPLAY_VEHICLE_*
    private const uint VehicleTruck = 0x01, VehicleEngine = 0x02, VehicleParkingBrake = 0x04, VehicleTrailer = 0x08,
                       VehicleTrailerCoupled = 0x10, VehicleJob = 0x20, VehicleCargoLoaded = 0x40,
                       VehicleSpecial = 0x80, VehicleAts = 0x100;

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
            connected && (flags & FlagForeground) != 0,
            *(float*)(_base + 24),
            *(float*)(_base + 28),
            *(float*)(_base + 32),
            *(float*)(_base + 36),
            *(uint*)(_base + 40));
    }

    /// <summary>
    /// Truck, trailer and job, or null when the plugin is too old to send them. The plugin
    /// keeps the block's sequence odd while it writes, so a torn read is retried.
    /// </summary>
    public Vehicle? ReadVehicle()
    {
        if (*(uint*)_base != Magic || *(uint*)(_base + 4) < 2)
            return null;
        byte* v = _base + VehicleOffset;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            uint before = Volatile.Read(ref *(uint*)v);
            if ((before & 1) != 0)
            {
                Thread.SpinWait(50);
                continue;
            }
            Vehicle result = ParseVehicle(v);
            Thread.MemoryBarrier();
            if (Volatile.Read(ref *(uint*)v) == before)
                return result;
        }
        return null;
    }

    private static Vehicle ParseVehicle(byte* v)
    {
        uint flags = *(uint*)(v + 4);
        float F(int offset) => *(float*)(v + offset);
        return new Vehicle(
            (flags & VehicleTruck) != 0, (flags & VehicleEngine) != 0, (flags & VehicleParkingBrake) != 0,
            (flags & VehicleTrailer) != 0, (flags & VehicleTrailerCoupled) != 0, (flags & VehicleJob) != 0,
            (flags & VehicleCargoLoaded) != 0, (flags & VehicleSpecial) != 0, (flags & VehicleAts) != 0,
            F(8), F(12), F(16), F(20),
            F(24), F(28), F(32),
            F(36), F(40), F(44),
            F(48), F(52), F(56), F(60), F(64),
            F(68), F(72), F(76), F(80),
            F(84), *(uint*)(v + 88), *(uint*)(v + 92), *(ulong*)(v + 96),
            Text(v + 104, 32), Text(v + 136, 48), Text(v + 184, 24), Text(v + 208, 48), Text(v + 256, 24),
            Text(v + 280, 64), Text(v + 344, 48), Text(v + 392, 48), Text(v + 440, 48), Text(v + 488, 48),
            Text(v + 536, 32), Text(v + 568, 32), Text(v + 600, 32));
    }

    private static string Text(byte* p, int capacity)
    {
        int n = 0;
        while (n < capacity && p[n] != 0)
            n++;
        return Encoding.UTF8.GetString(p, n);
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
