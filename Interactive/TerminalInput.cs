using System.Runtime.InteropServices;

namespace JInspect.Interactive;

internal static class TerminalInput
{
    public static ConsoleKeyInfo ReadKey()
    {
        if (!Console.IsInputRedirected)
            return Console.ReadKey(intercept: true);

        if (OperatingSystem.IsWindows())
            return WindowsReadKey();

        throw new PlatformNotSupportedException(
            "Interactive query mode with piped input is currently only supported on Windows.");
    }

    private const int STD_INPUT_HANDLE = -10;
    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 0x1;
    private const uint FILE_SHARE_WRITE = 0x2;
    private const uint OPEN_EXISTING = 3;
    private const ushort KEY_EVENT = 0x0001;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateFileW(
        string fileName, uint access, uint share, IntPtr sec,
        uint creation, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadConsoleInputW(
        IntPtr hConsoleInput, [Out] INPUT_RECORD[] lpBuffer,
        uint nLength, out uint lpNumberOfEventsRead);

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUT_RECORD
    {
        [FieldOffset(0)] public ushort EventType;
        [FieldOffset(4)] public KEY_EVENT_RECORD KeyEvent;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEY_EVENT_RECORD
    {
        public int bKeyDown;
        public ushort wRepeatCount;
        public ushort wVirtualKeyCode;
        public ushort wVirtualScanCode;
        public char UnicodeChar;
        public uint dwControlKeyState;
    }

    private static IntPtr _hConin = IntPtr.Zero;

    private static ConsoleKeyInfo WindowsReadKey()
    {
        if (_hConin == IntPtr.Zero)
        {
            _hConin = CreateFileW(
                "CONIN$",
                GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);

            if (_hConin == new IntPtr(-1))
                throw new InvalidOperationException(
                    $"Unable to open console input (CONIN$). Win32 error: {Marshal.GetLastWin32Error()}");
        }

        var buf = new INPUT_RECORD[1];
        while (true)
        {
            if (!ReadConsoleInputW(_hConin, buf, 1, out var read) || read == 0)
                continue;
            if (buf[0].EventType != KEY_EVENT) continue;

            var k = buf[0].KeyEvent;
            if (k.bKeyDown == 0) continue;
            if (k.wVirtualKeyCode == 0) continue;

            var key = (ConsoleKey)k.wVirtualKeyCode;
            if (key is ConsoleKey.LeftWindows or ConsoleKey.RightWindows) continue;
            if (k.wVirtualKeyCode is 0x10 or 0x11 or 0x12) continue;

            var shift = (k.dwControlKeyState & 0x10) != 0;
            var alt = (k.dwControlKeyState & 0x3) != 0;
            var ctrl = (k.dwControlKeyState & 0xC) != 0;

            return new ConsoleKeyInfo(k.UnicodeChar, key, shift, alt, ctrl);
        }
    }
}
