using System.Runtime.InteropServices;

namespace CabinPlay;

/// <summary>
/// Keeps the game from starving the screen. On a PC with one graphics card the game in
/// the foreground gets nearly all of it, and Windows slows background processes down on
/// top of that; the browser then stutters or freezes. Its work is tiny next to the game's,
/// so letting it go first costs the game next to nothing.
/// </summary>
internal static class Priority
{
    private static readonly HashSet<int> Reported = new();

    /// <summary>Safe to repeat: the browser lowers its own processes again now and then.</summary>
    public static void Raise(int processId, string what)
    {
        const uint SetInformation = 0x0200, QueryLimitedInformation = 0x1000;
        IntPtr process = OpenProcess(SetInformation | QueryLimitedInformation, false, processId);
        if (process == IntPtr.Zero)
            return;
        try
        {
            const uint AboveNormalPriorityClass = 0x8000;
            bool cpu = SetPriorityClass(process, AboveNormalPriorityClass);

            // Explicitly off, so Windows does not put the process into efficiency mode.
            var throttling = new PowerThrottlingState { Version = 1, ControlMask = 1 /* execution speed */, StateMask = 0 };
            bool power = SetProcessInformation(process, 4 /* ProcessPowerThrottling */, ref throttling,
                                               (uint)sizeof(uint) * 3);

            // High may be refused without administrator rights; above normal still helps.
            const int GpuAboveNormal = 3, GpuHigh = 4;
            int gpu = GpuHigh;
            if (D3DKMTSetProcessSchedulingPriorityClass(process, gpu) < 0)
            {
                gpu = GpuAboveNormal;
                if (D3DKMTSetProcessSchedulingPriorityClass(process, gpu) < 0)
                    gpu = 0;
            }

            if (Reported.Add(processId))
                Log.Write($"priority raised for {what} {processId}: cpu {(cpu ? "ok" : "refused")}, " +
                          $"throttling {(power ? "off" : "unchanged")}, " +
                          $"gpu {(gpu == GpuHigh ? "high" : gpu == GpuAboveNormal ? "above normal" : "refused")}");
        }
        catch (EntryPointNotFoundException)
        {
            // Older Windows without one of these calls: nothing to raise.
        }
        finally
        {
            CloseHandle(process);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PowerThrottlingState
    {
        public uint Version, ControlMask, StateMask;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint access, bool inherit, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetPriorityClass(IntPtr process, uint priorityClass);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetProcessInformation(IntPtr process, int informationClass,
        ref PowerThrottlingState information, uint size);

    [DllImport("gdi32.dll", ExactSpelling = true)]
    private static extern int D3DKMTSetProcessSchedulingPriorityClass(IntPtr process, int priorityClass);
}
