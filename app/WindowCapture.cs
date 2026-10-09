using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;

namespace Ets2CarPlay;

/// <summary>
/// Captures this app's own window with Windows.Graphics.Capture (which keeps delivering
/// frames while the window is covered by the game) and hands each frame to the
/// <see cref="FrameWriter"/>.
/// </summary>
internal sealed unsafe class WindowCapture : IDisposable
{
    private readonly FrameWriter _writer;
    private readonly object _gate = new();

    private IntPtr _device;   // ID3D11Device
    private IntPtr _context;  // ID3D11DeviceContext
    private IntPtr _staging;  // ID3D11Texture2D, FrameWriter-sized, CPU readable
    private IDirect3DDevice? _winrtDevice;
    private GraphicsCaptureItem? _item;
    private Direct3D11CaptureFramePool? _pool;
    private GraphicsCaptureSession? _session;
    private SizeInt32 _poolSize;
    private bool _disposed;

    public long FramesCaptured;

    public WindowCapture(FrameWriter writer) => _writer = writer;

    public void Start(IntPtr hwnd)
    {
        if (!GraphicsCaptureSession.IsSupported())
            throw new NotSupportedException("Windows.Graphics.Capture is not available on this system.");

        const uint BgraSupport = 0x20;
        int hr = D3D11CreateDevice(IntPtr.Zero, 1 /* hardware */, IntPtr.Zero, BgraSupport, IntPtr.Zero, 0, 7,
                                   out _device, out _, out _context);
        if (hr < 0)
            hr = D3D11CreateDevice(IntPtr.Zero, 5 /* WARP */, IntPtr.Zero, BgraSupport, IntPtr.Zero, 0, 7,
                                   out _device, out _, out _context);
        Marshal.ThrowExceptionForHR(hr);

        // The capture runtime uses the device from its own threads.
        if (Marshal.QueryInterface(_device, ref IID_ID3D10Multithread, out IntPtr mt) >= 0)
        {
            ((delegate* unmanaged[Stdcall]<IntPtr, int, int>)Vtbl(mt, 5))(mt, 1);
            Marshal.Release(mt);
        }

        var desc = new Texture2DDesc
        {
            Width = FrameWriter.Width,
            Height = FrameWriter.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = 87, // B8G8R8A8_UNORM
            SampleCount = 1,
            Usage = 3, // staging
            CpuAccessFlags = 0x20000, // read
        };
        IntPtr staging;
        hr = ((delegate* unmanaged[Stdcall]<IntPtr, Texture2DDesc*, void*, IntPtr*, int>)Vtbl(_device, 5))(
            _device, &desc, null, &staging);
        Marshal.ThrowExceptionForHR(hr);
        _staging = staging;

        Marshal.ThrowExceptionForHR(Marshal.QueryInterface(_device, ref IID_IDXGIDevice, out IntPtr dxgi));
        try
        {
            Marshal.ThrowExceptionForHR(CreateDirect3D11DeviceFromDXGIDevice(dxgi, out IntPtr inspectable));
            _winrtDevice = MarshalInterface<IDirect3DDevice>.FromAbi(inspectable);
            Marshal.Release(inspectable);
        }
        finally
        {
            Marshal.Release(dxgi);
        }

        var interop = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();
        Guid itemIid = IID_IGraphicsCaptureItem;
        IntPtr itemPtr = interop.CreateForWindow(hwnd, ref itemIid);
        _item = GraphicsCaptureItem.FromAbi(itemPtr);
        Marshal.Release(itemPtr);

        _poolSize = _item.Size;
        _pool = Direct3D11CaptureFramePool.CreateFreeThreaded(
            _winrtDevice, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, _poolSize);
        _pool.FrameArrived += OnFrameArrived;
        _session = _pool.CreateCaptureSession(_item);
        try { _session.IsCursorCaptureEnabled = false; } catch { /* older Windows builds */ }
        try { _session.IsBorderRequired = false; } catch { /* needs Windows 11 */ }
        _session.StartCapture();
    }

    private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            using Direct3D11CaptureFrame? frame = sender.TryGetNextFrame();
            if (frame is null)
                return;

            SizeInt32 size = frame.ContentSize;
            if (size.Width != _poolSize.Width || size.Height != _poolSize.Height)
            {
                _poolSize = size;
                sender.Recreate(_winrtDevice, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, size);
                return;
            }

            var access = frame.Surface.As<IDirect3DDxgiInterfaceAccess>();
            Guid texIid = IID_ID3D11Texture2D;
            IntPtr texture = access.GetInterface(ref texIid);
            try
            {
                // The window is exactly screen-sized; clamp anyway in case it is not.
                var box = new Box
                {
                    Right = (uint)Math.Min(size.Width, FrameWriter.Width),
                    Bottom = (uint)Math.Min(size.Height, FrameWriter.Height),
                    Back = 1,
                };
                ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint, uint, uint, uint, IntPtr, uint, Box*, void>)
                    Vtbl(_context, 46))(_context, _staging, 0, 0, 0, 0, texture, 0, &box);

                MappedSubresource mapped;
                int hr = ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint, int, uint, MappedSubresource*, int>)
                    Vtbl(_context, 14))(_context, _staging, 0, 1 /* read */, 0, &mapped);
                if (hr >= 0)
                {
                    _writer.Write((byte*)mapped.Data, (int)mapped.RowPitch);
                    ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint, void>)Vtbl(_context, 15))(_context, _staging, 0);
                    FramesCaptured++;
                }
            }
            finally
            {
                Marshal.Release(texture);
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            _session?.Dispose();
            _pool?.Dispose();
            _winrtDevice?.Dispose();
            foreach (IntPtr p in new[] { _staging, _context, _device })
                if (p != IntPtr.Zero)
                    Marshal.Release(p);
            _staging = _context = _device = IntPtr.Zero;
        }
    }

    private static void* Vtbl(IntPtr obj, int index) => (*(void***)obj)[index];

    private static Guid IID_ID3D11Texture2D = new("6F15AAF2-D208-4E89-9AB4-489535D34F9C");
    private static Guid IID_IDXGIDevice = new("54EC77FA-1377-44E6-8C32-88FD5F44C84C");
    private static Guid IID_ID3D10Multithread = new("9B7E4E00-342C-4106-A19F-4F2704F689F0");
    private static readonly Guid IID_IGraphicsCaptureItem = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");

    [StructLayout(LayoutKind.Sequential)]
    private struct Texture2DDesc
    {
        public uint Width, Height, MipLevels, ArraySize;
        public int Format;
        public uint SampleCount, SampleQuality;
        public int Usage;
        public uint BindFlags, CpuAccessFlags, MiscFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MappedSubresource
    {
        public IntPtr Data;
        public uint RowPitch, DepthPitch;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Box
    {
        public uint Left, Top, Front, Right, Bottom, Back;
    }

    [ComImport, Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IGraphicsCaptureItemInterop
    {
        IntPtr CreateForWindow([In] IntPtr window, [In] ref Guid iid);
        IntPtr CreateForMonitor([In] IntPtr monitor, [In] ref Guid iid);
    }

    [ComImport, Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDirect3DDxgiInterfaceAccess
    {
        IntPtr GetInterface([In] ref Guid iid);
    }

    [DllImport("d3d11.dll", ExactSpelling = true)]
    private static extern int D3D11CreateDevice(IntPtr adapter, int driverType, IntPtr software, uint flags,
        IntPtr featureLevels, uint featureLevelCount, uint sdkVersion, out IntPtr device, out int featureLevel,
        out IntPtr context);

    [DllImport("d3d11.dll", ExactSpelling = true)]
    private static extern int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);
}
