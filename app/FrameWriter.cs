using System.IO.MemoryMappedFiles;

namespace CabinPlay;

/// <summary>
/// Publishes frames to the game plugin through shared memory.
/// Layout mirrors plugin/frame_protocol.h: keep both in sync.
/// </summary>
internal sealed unsafe class FrameWriter : IDisposable
{
    public const int Width = 1024;
    public const int Height = 512;

    public static readonly string MappingName = Program.SharedName("CabinPlayFrame");
    private const uint Magic = 0x594C5043; // "CPLY"
    private const uint Version = 1;
    private const int HeaderSize = 64;
    private const int SequenceOffset = 16;
    private const int AppFlagsOffset = 20;
    private const uint AppNavVisible = 0x1, AppNavRects = 0x2;
    private const int HeartbeatOffset = 24;
    private const int NavSourceOffset = 32; // 4 x ushort, then the target's 4 x ushort
    private const int ControlRequestsOffset = 48;

    private readonly MemoryMappedFile _file;
    private readonly MemoryMappedViewAccessor _view;
    private readonly byte* _base;
    private readonly System.Threading.Timer _heartbeat;
    private readonly object _gate = new();
    private bool _disposed;

    public FrameWriter()
    {
        long size = HeaderSize + (long)Width * Height * 4;
        _file = MemoryMappedFile.CreateOrOpen(MappingName, size, MemoryMappedFileAccess.ReadWrite);
        _view = _file.CreateViewAccessor(0, size, MemoryMappedFileAccess.ReadWrite);
        byte* p = null;
        _view.SafeMemoryMappedViewHandle.AcquirePointer(ref p);
        _base = p;

        *(uint*)(_base + 0) = Magic;
        *(uint*)(_base + 4) = Version;
        *(uint*)(_base + 8) = Width;
        *(uint*)(_base + 12) = Height;
        Beat();
        // The plugin switches the screen off when this stops advancing.
        _heartbeat = new System.Threading.Timer(_ => Beat(), null, 500, 500);
    }

    private void Beat()
    {
        lock (_gate)
        {
            if (!_disposed)
                *(long*)(_base + HeartbeatOffset) = Environment.TickCount64;
        }
    }

    /// <summary>
    /// While visible, the plugin shows the game's UI texture through every pixel that has
    /// the map key colour (see --map-key in ui/style.css): by default its map, or with
    /// rectangles (x, y, width, height) the given part of it, such as the mirror views,
    /// stretched over the given part of the screen.
    /// </summary>
    public void SetNav(bool visible, ushort[]? source = null, ushort[]? target = null)
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            bool rects = visible && source is { Length: 4 } && target is { Length: 4 };
            if (rects)
            {
                // Written before the flag, so the plugin never uses half of them.
                for (int i = 0; i < 4; i++)
                {
                    *(ushort*)(_base + NavSourceOffset + i * 2) = source![i];
                    *(ushort*)(_base + NavSourceOffset + 8 + i * 2) = target![i];
                }
            }
            *(uint*)(_base + AppFlagsOffset) = (visible ? AppNavVisible : 0) | (rects ? AppNavRects : 0);
        }
    }

    /// <summary>
    /// Asks the plugin to open or close control mode, as its own Ctrl+Alt key does: for a
    /// controller button, which only this app sees.
    /// </summary>
    public void RequestControl()
    {
        lock (_gate)
        {
            if (!_disposed)
                ++*(uint*)(_base + ControlRequestsOffset);
        }
    }

    /// <summary>Copies a BGRA image of <see cref="Width"/> x <see cref="Height"/> pixels.</summary>
    public void Write(byte* source, int rowPitch)
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            ref int sequence = ref *(int*)(_base + SequenceOffset);
            Interlocked.Increment(ref sequence); // odd: frame in flux
            byte* dst = _base + HeaderSize;
            int rowBytes = Width * 4;
            if (rowPitch == rowBytes)
            {
                Buffer.MemoryCopy(source, dst, (long)rowBytes * Height, (long)rowBytes * Height);
            }
            else
            {
                for (int y = 0; y < Height; y++)
                    Buffer.MemoryCopy(source + (long)y * rowPitch, dst + (long)y * rowBytes, rowBytes, rowBytes);
            }
            *(long*)(_base + HeartbeatOffset) = Environment.TickCount64;
            Interlocked.Increment(ref sequence); // even: frame complete
        }
    }

    public void Dispose()
    {
        _heartbeat.Dispose();
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            *(long*)(_base + HeartbeatOffset) = 0; // lets the plugin blank the screen right away
        }
        _view.SafeMemoryMappedViewHandle.ReleasePointer();
        _view.Dispose();
        _file.Dispose();
    }
}
