using System.Runtime.InteropServices;

namespace CabinPlay;

/// <summary>A button on a steering wheel, gamepad or button box, told apart by the device's maker and model.</summary>
internal readonly record struct PadButton(int Vendor, int Product, int Index)
{
    /// <summary>The four directions of the hat switch count as buttons from here on: up, right, down, left.</summary>
    public const int FirstHat = 100;
}

/// <summary>
/// Watches the game controllers for button presses, so that actions can be put on a
/// steering wheel's buttons. Windows' joystick interface is used because it reports
/// presses to a program in the background, which is where this app is while the game is
/// played. It knows 32 buttons and one hat switch per device.
/// </summary>
internal sealed class Controllers : IDisposable
{
    private const int MaxDevices = 16;
    private const uint ReturnButtons = 0x80, ReturnPov = 0x40;

    private sealed class Device
    {
        public int Vendor, Product;
        public string Name = "";
        public uint Buttons;
        public int Hat = -1;
    }

    private readonly Device?[] _devices = new Device?[MaxDevices];
    private readonly Thread _thread;
    private volatile bool _stop;

    /// <summary>A button went down. Raised on a background thread, with the device's name.</summary>
    public event Action<PadButton, string>? Pressed;

    public Controllers()
    {
        _thread = new Thread(Watch) { IsBackground = true, Name = "controllers" };
        _thread.Start();
    }

    private void Watch()
    {
        long nextScan = 0;
        while (!_stop)
        {
            try
            {
                long now = Environment.TickCount64;
                // Asking about a controller that is not plugged in is slow, so that is done rarely.
                bool scan = now >= nextScan;
                if (scan)
                    nextScan = now + 3000;
                for (int id = 0; id < MaxDevices; id++)
                {
                    if (_devices[id] is not null || scan)
                        Poll(id);
                }
            }
            catch (Exception ex)
            {
                Log.Write("controllers: " + ex.Message);
                nextScan = Environment.TickCount64 + 10_000;
            }
            Thread.Sleep(15);
        }
    }

    private void Poll(int id)
    {
        var info = new JoyInfoEx { Size = (uint)Marshal.SizeOf<JoyInfoEx>(), Flags = ReturnButtons | ReturnPov };
        if (joyGetPosEx((uint)id, ref info) != 0)
        {
            _devices[id] = null;
            return;
        }
        // 0xFFFF while the hat is centred, otherwise hundredths of a degree clockwise from up.
        int hat = (info.Pov & 0xFFFF) == 0xFFFF ? -1 : (int)((info.Pov % 36000 + 4500) / 9000 % 4);
        Device? device = _devices[id];
        if (device is null)
        {
            // Newly seen: whatever is held right now is not a press.
            device = new Device { Buttons = info.Buttons, Hat = hat };
            if (joyGetDevCapsW((UIntPtr)(uint)id, out JoyCaps caps, (uint)Marshal.SizeOf<JoyCaps>()) == 0)
            {
                device.Vendor = caps.Mid;
                device.Product = caps.Pid;
            }
            device.Name = ProductName(device.Vendor, device.Product);
            _devices[id] = device;
            Log.Write($"controller {id}: {device.Name} ({device.Vendor:X4}:{device.Product:X4})");
            return;
        }
        uint pressed = info.Buttons & ~device.Buttons;
        device.Buttons = info.Buttons;
        for (int b = 0; pressed != 0 && b < 32; b++)
        {
            if ((pressed & (1u << b)) != 0)
                Pressed?.Invoke(new PadButton(device.Vendor, device.Product, b), device.Name);
        }
        if (hat != device.Hat)
        {
            device.Hat = hat;
            if (hat >= 0)
                Pressed?.Invoke(new PadButton(device.Vendor, device.Product, PadButton.FirstHat + hat), device.Name);
        }
    }

    /// <summary>The name the device's driver registered, as Windows' own controller list shows it.</summary>
    private static string ProductName(int vendor, int product)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                $@"System\CurrentControlSet\Control\MediaProperties\PrivateProperties\Joystick\OEM\VID_{vendor:X4}&PID_{product:X4}");
            if (key?.GetValue("OEMName") is string { Length: > 0 } name)
                return name.Trim();
        }
        catch (Exception)
        {
            // Only costs the name.
        }
        return "Controller";
    }

    public void Dispose()
    {
        _stop = true;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JoyInfoEx
    {
        public uint Size, Flags, X, Y, Z, R, U, V, Buttons, ButtonNumber, Pov, Reserved1, Reserved2;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct JoyCaps
    {
        public ushort Mid, Pid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Name;
        public uint XMin, XMax, YMin, YMax, ZMin, ZMax, NumButtons, PeriodMin, PeriodMax,
                    RMin, RMax, UMin, UMax, VMin, VMax, Caps, MaxAxes, NumAxes, MaxButtons;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string RegKey;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string OemVxD;
    }

    [DllImport("winmm.dll")]
    private static extern uint joyGetPosEx(uint id, ref JoyInfoEx info);

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern uint joyGetDevCapsW(UIntPtr id, out JoyCaps caps, uint size);
}
